using Examenes.App;
using Examenes.App.Eventos;
using Examenes.App.Vistas;
using Puppeteer;
using Puppets.Portador;

namespace Examenes.Wasm;

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
	private const string NombreDelActor = "examenes";

	private readonly PuenteDeJournal puente;
	private readonly HttpClient http;
	private readonly ConfiguracionDelProveedor proveedor;

	private ExamenesApp? app;
	private ExamenesAPI? api;
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
	// vuelve a pedir al proveedor. No se pierde el dato, pero mientras tanto lo que
	// muestra la pantalla y lo que lee el servidor MCP no coinciden.
	public string GuardadoFallando { get; private set; } = "";

	// ---------------------------------------------------------------- la carpeta

	public ValueTask<bool> EstaSoportado() => puente.EstaSoportado();

	// ---------------------------------------------------------------- los tres modos

	// El modo tiene que ser VISIBLE, no un detalle interno: los tres persisten distinto
	// y la persona necesita saber en cual esta. En 'opfs' el expediente sobrevive al
	// cierre pero NO lo puede leer su IA, y callarlo seria vender algo que no hay.
	public string Modo { get; private set; } = "ninguno";

	public bool EnCarpetaPropia => Modo == "carpeta";
	public bool EnAlmacenamientoDelNavegador => Modo == "opfs";

	public ValueTask<bool> OpfsSoportado() => puente.OpfsSoportado();

	public ValueTask<string> MotivoSinAlmacenamiento() => puente.MotivoSinAlmacenamiento();

	public ValueTask<bool> EstaInstalada() => puente.EstaInstalada();

	// ------------------------------------------------- lo que la IA puede consultar

	public bool ConsultaDeLaIAPermitida(string consulta) =>
		api is null ? true : api.ConsultaDeLaIAPermitida(consulta);

	public int ConsultasDenegadas() => api is null ? 0 : api.ConsultasDenegadas();

	// Cambiar un permiso es un COMANDO: se journaliza y por eso hay que bajarlo a disco,
	// igual que un examen. Si no, el servidor MCP seguiria leyendo el permiso viejo.
	public void PermitirConsultaDeLaIA(string consulta, bool permitida)
	{
		if (api is null) return;

		api.PermitirConsultaDeLaIA(consulta, permitida);
		_ = BajarADisco();
	}

	public ValueTask<string> ModoRecordado() => puente.ModoRecordado();

	public async ValueTask<ResultadoDeCarpeta> UsarAlmacenamientoDelNavegador()
	{
		ResultadoDeCarpeta resultado = await puente.UsarOpfs();
		Modo = await puente.ModoActual();
		UltimoMensaje = $"almacenamiento del navegador: {resultado}";
		return resultado;
	}

	public async ValueTask<ResultadoDeExportacion> Exportar() => await puente.Exportar();

	// Importar REEMPLAZA el expediente y rearranca el actor: el motor rehidrata en
	// ConfigureStorage, asi que escribir MEMFS con un actor ya vivo no cambiaria nada de
	// lo que se ve. Por eso se suelta y se vuelve a abrir.
	public async ValueTask<string> Importar()
	{
		ResultadoDeImportacion abierto = await puente.AbrirExportado();
		if (!abierto.Ok) return abierto.Motivo;

		app?.Actor?.GracefulExit();
		app = null;
		api = null;
		bucle = null;

		int escritos = puente.RestaurarEnMemfs(abierto.Archivos);
		await Arrancar();

		// Lo importado vive en MEMFS. Si hay destino, se baja para que quede persistido
		// tambien ahi; sin destino se pierde al cerrar, igual que todo lo demas.
		if (puente.HayCarpeta) await BajarADisco();

		UltimoMensaje = $"importados {escritos} archivos del expediente";
		return "";
	}

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

	// ---------------------------------------------------------------- arranque

	public async ValueTask Arrancar()
	{
		if (app is not null) return;

		int precargados = puente.HayCarpeta ? await puente.PrecargarEnMemfs() : 0;

		var arrancada = new ExamenesApp(NombreDelActor);
		arrancada.ConfigureStorage(DatabaseType.FileSystem, "path=" + PuenteDeJournal.RaizEnMemfs);
		arrancada.Start();

		app = arrancada;
		api = new ExamenesAPI(arrancada);
		bucle = new BucleDeRefresh(
			api,
			new FuenteHttpDeEventos(http, proveedor.Recurso, proveedor.Id),
			RutaDeExamenes.Rutear,
			BajarADisco,
			Renovar);

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
	// de esta instalacion, acotado a leer los eventos de un recurso. Las tres piezas
	// viven en el journal, asi que al reabrir la app ya sabe quien es.
	// Donde quedo apuntando el cliente. Se dice en los errores: "no se pudo hablar con el
	// laboratorio" sin decir CON CUAL es un mensaje que no se puede accionar.
	public string DondeEstaElProveedor => http.BaseAddress?.ToString() ?? "(sin configurar)";

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
			// Rotar devuelve null si el refresh ya no vale (caducado, rotado por otra
			// instancia, revocado). No es un error: se cae a pedir una credencial nueva.
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

	public async ValueTask<ResultadoDeRefresh> Refrescar(CancellationToken ct = default)
	{
		Exigir();

		// PIDE LA CREDENCIAL PRIMERO, igual que el arranque. Antes iba derecho al bucle, y
		// eso hacia que Refresh no pudiera recuperarse NUNCA: si al abrir no se habia
		// podido hablar con el laboratorio -sin red, otra maquina, la URL equivocada- no
		// quedaba token, y cada Refresh moria diciendo "la app no tiene web token
		// guardado". El unico camino que conseguia el token era el arranque, que ya habia
		// pasado.
		//
		// Medido en un iPhone: la app abria, el arranque no alcanzaba el proveedor, y el
		// boton quedaba inutil reportando la consecuencia en vez de la causa.
		await Credencial(ct);

		ResultadoDeRefresh resultado = await bucle!.Ejecutar(ct);
		UltimoMensaje = $"refresh: {resultado}";
		return resultado;
	}

	// ---------------------------------------------------------------- consultas

	public IReadOnlyList<ExamenEnLista> Examenes() { Exigir(); return api!.Examenes(); }

	// Los analitos de los que hay historia. Es lo que la pantalla recorre para mostrar
	// una lectura por serie, sin conocer las clases del dominio.
	public IReadOnlyList<string> Analitos() { Exigir(); return api!.Analitos(); }

	// EL CUADRO HISTORICO, plano. La pantalla lo rearma; el dominio decidio que va donde.
	public IReadOnlyList<CeldaEnCuadro> Cuadro() { Exigir(); return api!.Cuadro(); }

	// LO QUE EL ANALITO CONCLUYE. La pantalla ya no arma el consejo: lo pide.
	public InterpretacionDeAnalito Interpretacion(string analito)
	{
		Exigir();
		return api!.Interpretacion(analito);
	}

	// LA consulta que justifica la app. No es el ultimo valor: es la lectura de la serie.
	public TendenciaDeAnalito Tendencia(string analito) { Exigir(); return api!.Tendencia(analito); }

	public int Cantidad() { Exigir(); return api!.CantidadDeExamenes(); }

	public int AnalitosFueraDeRango() { Exigir(); return api!.AnalitosFueraDeRango(); }

	public int EdadDelPaciente() { Exigir(); return api!.EdadDelPaciente(); }

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
	public string BaseUrl { get; set; } = "eventos/";
	public string Recurso { get; set; } = "expediente";
	public string Id { get; set; } = "1";
}
