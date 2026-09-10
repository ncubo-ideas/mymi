using Facturas.App;
using Facturas.App.Eventos;
using Facturas.App.Vistas;
using Puppeteer;
using Puppets.Portador;

namespace Facturas.Wasm;

// El arranque de la app instalada, en el orden que impone el mismatch sincrono/asincrono
// del browser (briefing 5.2):
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
	private const string NombreDelActor = "facturas";

	private readonly PuenteDeJournal puente;
	private readonly AlmacenDeCredencial almacen;
	private readonly HttpClient http;
	private readonly ConfiguracionDelProveedor proveedor;

	private FacturasApp? app;
	private FacturasAPI? api;
	private BucleDeRefresh? bucle;

	public EstadoDeLaApp(PuenteDeJournal puente, AlmacenDeCredencial almacen, HttpClient http, ConfiguracionDelProveedor proveedor)
	{
		ArgumentNullException.ThrowIfNull(puente);
		ArgumentNullException.ThrowIfNull(almacen);
		ArgumentNullException.ThrowIfNull(http);
		ArgumentNullException.ThrowIfNull(proveedor);

		this.puente = puente;
		this.almacen = almacen;
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
	// vuelve a pedir al proveedor. No se pierde el dato, pero mientras tanto lo que
	// muestra la pantalla y lo que lee el servidor MCP no coinciden.
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

		var arrancada = new FacturasApp(NombreDelActor);
		arrancada.ConfigureStorage(DatabaseType.FileSystem, "path=" + PuenteDeJournal.RaizEnMemfs);
		arrancada.Start();

		app = arrancada;
		api = new FacturasAPI(arrancada);
		bucle = new BucleDeRefresh(
			api,
			new FuenteHttpDeEventos(http, proveedor.Recurso, proveedor.Id, Credencial, Renovar),
			RutaDeFacturas.Rutear,
			BajarADisco);

		UltimoMensaje = puente.HayCarpeta
			? $"arriba. {precargados} archivos precargados de la carpeta '{puente.NombreDeLaCarpeta}'"
			: "arriba, pero SIN carpeta: el journal vive solo en memoria y se pierde al cerrar";

		// La app se pone al dia sola: consigue su credencial si no la tiene y lee lo
		// nuevo. Es el "el app puede hacer cada manana o cada vez que abre" del diseno.
		// Si el proveedor no esta disponible, abrir NO falla: se muestra lo que ya hay
		// en el journal, que es justamente de lo que sirve tener el dato propio.
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
	// pantalla pueda decirlo: un flush que falla en silencio deja lo que se ve sin
	// respaldo y hace que el servidor MCP lea un estado viejo sin que nada avise.
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
	// de esta instalacion, acotado a leer los eventos de un recurso. Las tres piezas viven
	// en el ALMACEN DE CREDENCIAL -storage del browser del origen-, NO en el journal: un
	// token es contexto de sesion, no historia de la persona, y en el journal quedaba en
	// claro y viajaba con cualquier copia o traspaso (hasta el 2026-09-03 era asi). El
	// bucle de refresh recibe Credencial y Renovar como delegados y nunca toca el diario
	// para esto.

	private async Task<string> Credencial(CancellationToken ct)
	{
		CredencialGuardada guardada = await almacen.Leer();

		if (guardada.DeviceKey.Length == 0)
		{
			guardada.DeviceKey = NuevaClaveDeDevice();
			await almacen.Guardar(guardada);
		}

		if (guardada.Token.Length > 0) return guardada.Token;

		return await PedirCredencialNueva(ct);
	}

	// Tras un 401: primero rotar con el refresh token, que es para lo que existe. Si no
	// hay o el proveedor lo rechaza, pedir una credencial nueva con el device key.
	private async Task<string> Renovar(CancellationToken ct)
	{
		CredencialGuardada guardada = await almacen.Leer();
		if (guardada.Refresh.Length > 0)
		{
			// Rotar devuelve null si el refresh ya no vale (caducado, rotado por otra
			// instancia, revocado). No es un error: se cae a pedir una credencial nueva.
			Credencial rotada = await new ProveedorDeToken(http).Rotar(guardada.Refresh, ct);
			if (rotada is not null) return await Guardar(rotada, "credencial rotada con el refresh token");
		}

		return await PedirCredencialNueva(ct);
	}

	private async Task<string> PedirCredencialNueva(CancellationToken ct)
	{
		CredencialGuardada guardada = await almacen.Leer();
		Credencial nueva = await new ProveedorDeToken(http)
			.Pedir(guardada.DeviceKey, proveedor.Recurso, proveedor.Id, ct);

		return await Guardar(nueva, $"credencial nueva (scope {nueva.Scope})");
	}

	// Guarda en el almacen, no en el journal: por eso ya no baja nada a disco.
	private async Task<string> Guardar(Credencial nueva, string quePaso)
	{
		CredencialGuardada guardada = await almacen.Leer();
		guardada.Token = nueva.AccessToken;
		guardada.Refresh = nueva.RefreshToken ?? "";
		await almacen.Guardar(guardada);

		UltimoMensaje = almacen.SinRespaldo
			? quePaso + " (solo en memoria: el browser no dejo guardarla; al reabrir se pedira otra)"
			: quePaso;
		return nueva.AccessToken;
	}

	// Identidad de esta instalacion. Se genera una sola vez y se guarda en el almacen de
	// credencial (storage del browser), NO en el journal. Si el browser la desaloja, la app
	// se presenta ante el proveedor como un device nuevo y pide otra concesion; la vieja
	// queda huerfana hasta que el proveedor la revoque.
	private static string NuevaClaveDeDevice() => "device-" + Guid.NewGuid().ToString("N")[..16];

	public async ValueTask<ResultadoDeRefresh> Refrescar(CancellationToken ct = default)
	{
		Exigir();
		ResultadoDeRefresh resultado = await bucle!.Ejecutar(ct);
		UltimoMensaje = $"refresh: {resultado}";
		return resultado;
	}

	// ---------------------------------------------------------------- consultas

	public IReadOnlyList<FacturaEnLista> Facturas() { Exigir(); return api!.Facturas(); }

	public FacturaDetallada Detalle(string numero) { Exigir(); return api!.Detalle(numero); }

	public decimal Total() { Exigir(); return api!.TotalDeFacturas(); }

	public int Cantidad() { Exigir(); return api!.CantidadDeFacturas(); }

	public long UltimoEvento() { Exigir(); return api!.UltimoEventoProcesado(); }

	// Del almacen, no del journal. Sincrono: la pantalla lo pregunta al renderizar y el
	// almacen ya tiene la credencial en cache despues del primer Leer.
	public bool TieneToken() => almacen.TieneToken;

	public (int archivos, long bytes) EstadoDeMemfs() => PuenteDeJournal.EstadoDeMemfs();

	private void Exigir()
	{
		if (api is null) throw new InvalidOperationException("La app todavia no arranco.");
	}

	public async ValueTask DisposeAsync()
	{
		app?.Actor.GracefulExit();
		await puente.DisposeAsync();
		await almacen.DisposeAsync();
	}
}

public sealed class ConfiguracionDelProveedor
{
	// El juego de URLs de APP1. APP2 (hipotecas) es otro recurso sobre el mismo
	// mecanismo: cambiar estos tres valores y su dominio, nada mas.
	public string BaseUrl { get; set; } = "http://localhost:8081/";
	public string Recurso { get; set; } = "clientes";
	public string Id { get; set; } = "1";
}
