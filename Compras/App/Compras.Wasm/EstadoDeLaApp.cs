using Compras.App;
using Compras.App.Eventos;
using Compras.App.Vistas;
using Puppeteer;
using Puppets.Portador;

namespace Compras.Wasm;

// El arranque de la app instalada, en el orden que impone el mismatch sincrono/asincrono
// del browser:
//
//   1. el usuario otorga la carpeta            (gesto, asincrono)
//   2. carpeta -> MEMFS                        (asincrono, ANTES de que exista el actor)
//   3. ConfigureStorage(FileSystem, /journal) + Start()
//        -> el replay rehidrata leyendo MEMFS, todo SINCRONO
//   4. cada comando escribe en MEMFS y retorna ya
//   5. flush MEMFS -> carpeta despues de CADA comando
//
// El paso 2 tiene que pasar antes del 3 porque el motor lee el journal de forma
// sincrona y en wasm single-thread no se puede bloquear esperando una promesa.
//
// VENTANA DE PERDIDA, y la UI la dice: entre el commit del actor y el flush a disco
// hay un instante en el que un cierre abrupto pierde entradas. El flush es por comando
// (no por timer) para que sea lo mas angosta posible, pero NO es durabilidad
// transaccional y no hay que presentarla como tal.
public sealed class EstadoDeLaApp : IAsyncDisposable
{
	private const string NombreDelActor = "compras";

	private readonly PuenteDeJournal puente;
	private readonly HttpClient http;
	private readonly ConfiguracionDelProveedor proveedor;

	private ComprasApp? app;
	private ComprasAPI? api;
	private BucleDeRefresh? bucle;

	public EstadoDeLaApp(PuenteDeJournal puente, HttpClient http, ConfiguracionDelProveedor proveedor)
	{
		ArgumentNullException.ThrowIfNull(puente);
		ArgumentNullException.ThrowIfNull(http);
		ArgumentNullException.ThrowIfNull(proveedor);

		this.puente = puente;
		this.http = http;
		this.proveedor = proveedor;
	}

	public bool EstaArriba => app is not null;

	public bool HayCarpeta => puente.HayCarpeta;

	public bool FaltaReotorgarPermiso => puente.FaltaReotorgarPermiso;

	public string NombreDeLaCarpeta => puente.NombreDeLaCarpeta;

	public string UltimoMensaje { get; private set; } = "";

	// Cuando la carpeta rechaza una escritura, lo que se ve en pantalla deja de estar
	// respaldado. La app tiene que DECIRLO, porque el estado de la carpeta es el que
	// sobrevive: al reabrir, la precarga vuelve a partir de ahi y lo no guardado se
	// vuelve a pedir al proveedor.
	public string GuardadoFallando { get; private set; } = "";

	// ---------------------------------------------------------------- la carpeta

	public ValueTask<bool> EstaSoportado() => puente.EstaSoportado();

	// Sin gesto: solo revalida el permiso de un handle ya guardado en IndexedDB.
	public async ValueTask<ResultadoDeCarpeta> RecuperarCarpeta()
	{
		ResultadoDeCarpeta resultado = await puente.RecuperarCarpeta();
		UltimoMensaje = $"recuperar carpeta: {resultado}";
		return resultado;
	}

	// GESTO DE USUARIO. Desde un click, nunca al inicializar.
	public async ValueTask<ResultadoDeCarpeta> ConectarCarpeta()
	{
		ResultadoDeCarpeta resultado = await puente.ElegirCarpeta();
		UltimoMensaje = $"conectar carpeta: {resultado}";
		return resultado;
	}

	// GESTO DE USUARIO. Hace falta tras recargar cuando el permiso quedo en 'prompt'.
	public async ValueTask<ResultadoDeCarpeta> PedirPermiso()
	{
		ResultadoDeCarpeta resultado = await puente.PedirPermiso();
		UltimoMensaje = $"pedir permiso: {resultado}";
		return resultado;
	}

	// ---------------------------------------------------------------- arranque

	public async ValueTask Arrancar()
	{
		if (app is not null) return;

		int precargados = puente.HayCarpeta ? await puente.PrecargarEnMemfs() : 0;

		var arrancada = new ComprasApp(NombreDelActor);
		arrancada.ConfigureStorage(DatabaseType.FileSystem, "path=" + PuenteDeJournal.RaizEnMemfs);
		arrancada.Start();

		app = arrancada;
		api = new ComprasAPI(arrancada);
		bucle = new BucleDeRefresh(
			api,
			new FuenteHttpDeEventos(http, proveedor.Recurso, proveedor.Id),
			RutaDeCompras.Rutear,
			BajarADisco,
			Renovar);

		UltimoMensaje = puente.HayCarpeta
			? $"arriba. {precargados} archivos precargados de la carpeta '{puente.NombreDeLaCarpeta}'"
			: "arriba, pero SIN carpeta: el journal vive solo en memoria y se pierde al cerrar";

		// La app se pone al dia sola: consigue su credencial si no la tiene y lee lo
		// nuevo. Si el proveedor no esta disponible, abrir NO falla: se muestra lo que ya
		// hay en el journal, que es justamente de lo que sirve tener el dato propio.
		try
		{
			await Credencial(CancellationToken.None);
			ResultadoDeRefresh alAbrir = await bucle.Ejecutar(CancellationToken.None);
			UltimoMensaje += $" | al abrir: {alAbrir}";
		}
		catch (Exception ex)
		{
			UltimoMensaje += $" | al abrir no se pudo poner al dia ({ex.GetType().Name}: {ex.Message}). "
			               + "Lo que ya estaba registrado se muestra igual.";
		}
	}

	// El unico camino por el que la app baja a disco. Recuerda si fallo, para que la
	// pantalla pueda decirlo.
	private async Task BajarADisco()
	{
		try
		{
			await puente.BajarDeMemfs();
			GuardadoFallando = "";
		}
		catch (EscrituraRechazadaException ex)
		{
			GuardadoFallando = ex.Message;
			throw;
		}
	}

	// ---------------------------------------------------------------- credencial

	// El token lo pide LA APP, no la persona. El proveedor lo emite contra el device key
	// de esta instalacion, acotado a leer los eventos de un recurso. Las tres piezas
	// viven en el journal, asi que al reabrir la app ya sabe quien es.
	private async ValueTask<string> Credencial(CancellationToken ct)
	{
		Exigir();

		if (api!.DeviceKeyGuardado().Length == 0)
			api.RegistrarDevice(NuevaClaveDeDevice());

		string vigente = api.TokenGuardado();
		if (vigente.Length > 0) return vigente;

		return await PedirCredencialNueva(ct);
	}

	// Tras un 401: primero rotar con el refresh token, que es para lo que existe. Si no
	// hay o el proveedor lo rechaza, pedir una credencial nueva con el device key.
	private async Task<string> Renovar(CancellationToken ct)
	{
		Exigir();

		string refresh = api!.RefreshTokenGuardado();
		if (refresh.Length > 0)
		{
			Credencial rotada = await new ProveedorDeToken(http).Rotar(refresh, ct);
			if (rotada is not null) return await Guardar(rotada, "credencial rotada con el refresh token");
		}

		return await PedirCredencialNueva(ct);
	}

	private async Task<string> PedirCredencialNueva(CancellationToken ct)
	{
		Credencial nueva = await new ProveedorDeToken(http)
			.Pedir(api!.DeviceKeyGuardado(), proveedor.Recurso, proveedor.Id, ct);

		return await Guardar(nueva, $"credencial nueva (scope {nueva.Scope})");
	}

	private async Task<string> Guardar(Credencial credencial, string quePaso)
	{
		api!.GuardarCredencial(credencial.AccessToken, credencial.RefreshToken);
		await BajarADisco();
		UltimoMensaje = quePaso;
		return credencial.AccessToken;
	}

	// Identidad de esta instalacion. Se genera una sola vez y se journaliza como
	// parametro, asi el replay la reusa en vez de inventar otra.
	private static string NuevaClaveDeDevice() => "device-" + Guid.NewGuid().ToString("N")[..16];

	// Donde quedo apuntando el cliente. Se dice en los errores: "no se pudo hablar con el
	// supermercado" sin decir CON CUAL no se puede accionar.
	public string DondeEstaElProveedor => http.BaseAddress?.ToString() ?? "(sin configurar)";

	public async ValueTask<ResultadoDeRefresh> Refrescar(CancellationToken ct = default)
	{
		Exigir();

		// PIDE LA CREDENCIAL PRIMERO, igual que el arranque. Sin esto, si al abrir no se
		// pudo hablar con el proveedor no queda token y CADA Refresh muere para siempre
		// diciendo "la app no tiene web token guardado", porque el unico camino que
		// consigue el token era el arranque, que ya paso. Medido por Renan en un iPhone.
		await Credencial(ct);

		ResultadoDeRefresh resultado = await bucle!.Ejecutar(ct);
		UltimoMensaje = $"refresh: {resultado}";
		return resultado;
	}

	// ---------------------------------------------------------------- consultas

	public IReadOnlyList<CompraEnLista> Compras() { Exigir(); return api!.Compras(); }

	public CompraDetallada Detalle(string numero) { Exigir(); return api!.Detalle(numero); }

	// LA consulta que justifica la app: como cambio la canasta entre dos meses.
	public CanastasComparadas Comparacion(int anioAntes, int mesAntes, int anioDespues, int mesDespues)
	{ Exigir(); return api!.Comparacion(anioAntes, mesAntes, anioDespues, mesDespues); }

	// La lectura por defecto. QUE DOS MESES se comparan lo elige el dominio, no esta
	// pantalla: elegir es parte de la pregunta, no de como se dibuja.
	public CanastasComparadas ComparacionPorDefecto() { Exigir(); return api!.ComparacionConElAnioAnterior(); }

	public CanastaDelMesVista Canasta(int anio, int mes) { Exigir(); return api!.Canasta(anio, mes); }

	// LAS DOS LECTURAS DE LA PORTADA. Cuantas compras se miran para reconocer la firma lo
	// decide la app (ComprasAPI), no esta caratula: es parte de la pregunta.
	public FirmaDeLaSemana Firma() { Exigir(); return api!.Firma(); }

	// LA CANASTA DEL ULTIMO MES CON COMPRAS. Cual es ese mes lo sabe el DOMINIO por su
	// rango; esta pantalla no tiene por que adivinarlo ni pedirle a la persona que elija.
	public CanastaDelMesVista CanastaDelUltimoMes()
	{
		Exigir();
		RangoDelHistorialVista rango = api!.Rango();
		if (rango.MesesConCompras == 0) return new CanastaDelMesVista();

		// El rango lo entrega como aaaa-mm, que es la forma en que viaja: se parte aca.
		string[] partes = rango.UltimoMes.Split('-');
		return api.Canasta(int.Parse(partes[0]), int.Parse(partes[1]));
	}

	// EL PRODUCTO SE NOMBRA. El dominio resuelve el texto igual que para la tool del MCP.
	public ConsumoEnElTiempo ConsumoDe(string producto)
	{ Exigir(); return api!.Consumo(producto); }

	// EN CUALES COMPRAS APARECIO ESTO. La busca el DOMINIO y no la pantalla: la pantalla
	// solo tiene la lista de compras -numero, fecha, lineas, total-, sin las lineas, asi que
	// filtrar aca obligaria a bajar el detalle de las 87 en cada tecla.
	public ComprasHalladas Halladas(string termino)
	{ Exigir(); return api!.Halladas(termino); }

	// EL CORTE ES EL 1 DE ENERO DE ESTE ANO, y antes era "hace un ano" — que con compras
	// semanales resulto estar mal. Con 87 compras, "hace un ano" partia el historial en
	// agosto y comparaba ocho semanas de 2025 contra otras ocho de 2025: dos ventanas del
	// mismo lado del cambio, asi que la app decia "su canasta viene igual" y la tarjeta del
	// quiebre no aparecia. Con seis compras el bug no se veia.
	//
	// El ano corriente tambien es mas facil de explicar -"lo que cambio este ano"- y es el
	// mismo marco que la comparacion de canastas, que compara este mes contra el del ano
	// pasado. El corte lo pone la caratula porque involucra el RELOJ y el dominio no lo lee.
	//
	// LO QUE FALTA, y es una lectura y no un ajuste: que el DOMINIO encuentre el mes del
	// quiebre en vez de recibirlo. La especificacion pide "el quiebre de enero", no "el
	// quiebre desde la fecha que le pases", y hoy si el cambio de una persona fue en
	// setiembre esta pantalla no lo encuentra.
	public CambioDeLaCanasta CambioDelUltimoAnio()
	{ Exigir(); return api!.Cambio(new DateTime(DateTime.Now.Year, 1, 1)); }

	// LOS PRODUCTOS A LOS QUE LES CAMBIO EL RITMO, el mas notorio primero. Mismo corte que
	// el cambio de la canasta -"este ano"- porque las dos tarjetas cuentan el mismo quiebre
	// desde angulos distintos: una dice QUE entro, la otra CADA CUANTO.
	// EL ULTIMO MES CONTRA EL ANTERIOR. Que dos meses se comparan lo elige el DOMINIO, que
	// conoce su propio rango: esta pantalla no sabe cual es el ultimo mes con compras, y la
	// aritmetica de "el mes anterior" se resuelve mal en enero.
	public CanastasComparadas ComparacionConElMesAnterior()
	{ Exigir(); return api!.ComparacionConElMesAnterior(); }

	// SU PROPIA INFLACION, solo de los productos que si lleva.
	public IReadOnlyList<PrecioQueCambio> Precios() { Exigir(); return api!.Precios(); }

	// CADA CUANTO VA AL SUPER, que dia y en que momento. No lleva parametros: el dominio lo
	// mide sobre todo el historial, y no hay reloj de por medio.
	public RitmoDelSuperVista Ritmo() { Exigir(); return api!.Ritmo(); }

	public IReadOnlyList<RitmoQueCambio> RitmosQueCambiaron()
	{ Exigir(); return api!.RitmosQueCambiaron(new DateTime(DateTime.Now.Year, 1, 1)); }

	// El "hoy" lo pone la caratula: el dominio no lee el reloj.
	public IReadOnlyList<GarantiaDeProducto> GarantiasVigentes() { Exigir(); return api!.GarantiasVigentes(DateTime.Now); }

	// LO QUE NO ESTA: lo unico que esta pantalla no podria calcular sola.
	public IReadOnlyList<AbastecimientoEnEspera> AbastecimientosPendientes()
	{ Exigir(); return api!.AbastecimientosPendientes(DateTime.Now); }

	public int Cantidad() { Exigir(); return api!.CantidadDeCompras(); }

	public decimal Total() { Exigir(); return api!.TotalDeCompras(); }

	public long UltimoEvento() { Exigir(); return api!.UltimoEventoProcesado(); }

	public bool TieneToken() { Exigir(); return api!.TokenGuardado().Length > 0; }

	public (int archivos, long bytes) EstadoDeMemfs() => PuenteDeJournal.EstadoDeMemfs();

	private void Exigir()
	{
		if (api is null) throw new InvalidOperationException("La app todavia no arranco.");
	}

	public async ValueTask DisposeAsync()
	{
		app?.Actor.GracefulExit();
		await puente.DisposeAsync();
	}
}

public sealed class ConfiguracionDelProveedor
{
	// El juego de URLs de este negocio. Otro negocio es otro recurso sobre el mismo
	// mecanismo: cambiar estos tres valores y su dominio, nada mas.
	//
	// Relativa a proposito (ver Program.cs): el mismo publish sirve en localhost, por IP
	// y por https sin recompilar. nginx proxea /app/eventos/ al contenedor de URL2.
	public string BaseUrl { get; set; } = "eventos/";
	public string Recurso { get; set; } = "cliente";
	public string Id { get; set; } = "1";
}
