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
		Modo = await puente.ModoActual();
		UltimoMensaje = $"recuperar carpeta: {resultado}";
		return resultado;
	}

	// GESTO DE USUARIO. Desde un click, nunca al inicializar.
	public async ValueTask<ResultadoDeCarpeta> ConectarCarpeta()
	{
		ResultadoDeCarpeta resultado = await puente.ElegirCarpeta();
		Modo = await puente.ModoActual();
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

	// ---------------------------------------------------------------- el modo degradado

	// DONDE NO HAY PICKER. El briefing §6.2 lo pide con todas las letras: "no hagas que la
	// app falle donde no hay picker, degrada a OPFS". showDirectoryPicker es hoy desktop
	// Chromium; en Safari, Firefox y CUALQUIER telefono no existe, y sin esto la app se
	// quedaba en memoria y perdia el registro al cerrar la pestaña.

	public ValueTask<bool> OpfsSoportado() => puente.OpfsSoportado();

	// '' si hay algun almacenamiento; 'navegador' o 'inseguro' si no hay ninguno.
	public ValueTask<string> MotivoSinAlmacenamiento() => puente.MotivoSinAlmacenamiento();

	public ValueTask<bool> EstaInstalada() => puente.EstaInstalada();

	/// <summary>'carpeta', 'opfs' o 'ninguno'.</summary>
	public string Modo { get; private set; } = "ninguno";

	public bool EnAlmacenamientoDelNavegador => Modo == "opfs";

	// NO EXIGE GESTO: el origen ya es dueño de su propio almacenamiento.
	public async ValueTask<ResultadoDeCarpeta> UsarAlmacenamientoDelNavegador()
	{
		ResultadoDeCarpeta resultado = await puente.UsarOpfs();
		Modo = await puente.ModoActual();
		UltimoMensaje = $"almacenamiento del navegador: {resultado}";
		return resultado;
	}

	// SE REENGANCHA SOLA al modo de la vez pasada, y por eso existe. Sin esto, quien abre
	// la app en un telefono tiene que apretar "guardar en este navegador" CADA VEZ, y el
	// modo recordado no sirve de nada: se guarda y nadie lo lee.
	//
	// Solo reengancha OPFS. La carpeta no se puede reenganchar sola aunque el handle este
	// guardado: el browser puede haber degradado el permiso a 'prompt', y volver a pedirlo
	// exige un gesto — por eso ese camino pasa por RecuperarCarpeta y, si hace falta, por
	// el boton.
	public async ValueTask<bool> ReengancharModo()
	{
		if (await puente.ModoRecordado() != "opfs") return false;

		bool ok = (await puente.UsarOpfs()).Ok;
		Modo = await puente.ModoActual();
		return ok;
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

	// DESDE CUANDO HASTA CUANDO hay compras. La pantalla lo usa para decir "87 compras desde
	// enero de 2025" en vez de un numero suelto.
	public RangoDelHistorialVista Rango() { Exigir(); return api!.Rango(); }

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

	// LO QUE VA A FALTAR EN LA PROXIMA COMPRA. El "hoy" lo pone la caratula porque involucra
	// el RELOJ, y el dominio no lo lee: si lo leyera, esta lectura daria distinto en cada
	// replay del journal y dejaria de ser reproducible.
	public ListaDeLaProximaCompra Lista()
	{ Exigir(); return api!.Lista(DateTime.Now); }

	// EN CUALES COMPRAS APARECIO ESTO. La busca el DOMINIO y no la pantalla: la pantalla
	// solo tiene la lista de compras -numero, fecha, lineas, total-, sin las lineas, asi que
	// filtrar aca obligaria a bajar el detalle de las 87 en cada tecla.
	public ComprasHalladas Halladas(string termino)
	{ Exigir(); return api!.Halladas(termino); }

	// EL QUIEBRE LO ENCUENTRA EL DOMINIO, y esta pantalla ya no elige una fecha.
	//
	// Antes cortaba el 1 de enero del ano corriente, y funcionaba POR SUERTE: el guion de la
	// demo quiebra en enero. A una persona que cambio en setiembre le decia "su canasta
	// viene igual", que es una respuesta FALSA y no un hueco visible — el peor de los dos
	// errores, porque nadie va a ir a buscar lo que la app ya contesto.
	//
	// Y no es solo por acertar: elegir cuando partir la historia es un JUICIO, y un juicio
	// que vive en una caratula no lo audita nadie. Ahora la pantalla pide "el cambio" y el
	// dominio dice cual y desde cuando.
	public CambioDeLaCanasta CambioDelUltimoAnio()
	{ Exigir(); return api!.CambioDelQuiebre(); }

	// LOS PRODUCTOS A LOS QUE LES CAMBIO EL RITMO, el mas notorio primero. EL MISMO quiebre
	// que la tarjeta de arriba, y ahora lo garantiza el dominio: las dos cuentan el mismo
	// cambio desde angulos distintos -una dice QUE entro, la otra CADA CUANTO-, y antes esa
	// coordinacion era que las dos llamadas de aca pasaran la misma fecha.
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
	{ Exigir(); return api!.RitmosDelQuiebre(); }

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

/// <summary>Cómo se llega al súper: su sitio y su WhatsApp.</summary>
// VA EN LA CONFIGURACION Y NO EN EL CODIGO porque son datos DE LA INSTITUCION, no de la
// app: un numero de telefono cambia, y cambiarlo no puede exigir recompilar. Es la misma
// razon por la que el proveedor tampoco esta compilado.
//
// TODOS VACIOS POR DEFECTO, y el pie no se dibuja si no hay ninguno. Un supermercado que
// despliegue esto sin WhatsApp no tiene que ver un boton roto ni tocar nuestro codigo:
// deja el campo afuera del appsettings y el pie se ajusta solo.
public sealed class ConfiguracionDeLaInstitucion
{
	/// <summary>El sitio del súper. Vacío si no tiene.</summary>
	public string Sitio { get; set; } = "";

	/// <summary>El número en formato internacional sin signos: 50622001100.</summary>
	// SE GUARDA EL NUMERO Y NO LA URL para que quien configure no tenga que saber que
	// wa.me existe ni como se arma. La app compone el enlace.
	public string WhatsApp { get; set; } = "";

	/// <summary>Lo que se le muestra a la persona del número: "2200-1100".</summary>
	public string TelefonoParaMostrar { get; set; } = "";

	public bool HaySitio => Sitio.Length > 0;
	public bool HayWhatsApp => WhatsApp.Length > 0;
	public bool HayAlguno => HaySitio || HayWhatsApp;

	/// <summary>El enlace de WhatsApp, armado. Vacío cuando no hay número.</summary>
	public string EnlaceDeWhatsApp => HayWhatsApp ? $"https://wa.me/{WhatsApp}" : "";
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
