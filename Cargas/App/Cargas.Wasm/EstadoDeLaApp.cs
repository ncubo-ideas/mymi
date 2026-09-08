using Cargas.App;
using Cargas.App.Eventos;
using Cargas.App.Vistas;
using Puppeteer;
using Puppets.Portador;

namespace Cargas.Wasm;

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
	private const string NombreDelActor = "cargas";

	private readonly PuenteDeJournal puente;
	private readonly ModoDeLaApp modo;
	private readonly PuenteDeGnothi puenteDeGnothi;
	private readonly AlmacenDeCredencial almacen;
	private readonly HttpClient http;
	private readonly ConfiguracionDelProveedor proveedor;

	private CargasApp? app;
	private CargasAPI? api;
	private BucleDeRefresh? bucle;

	public EstadoDeLaApp(PuenteDeJournal puente, ModoDeLaApp modo, PuenteDeGnothi puenteDeGnothi,
		AlmacenDeCredencial almacen, HttpClient http, ConfiguracionDelProveedor proveedor)
	{
		ArgumentNullException.ThrowIfNull(puente);
		ArgumentNullException.ThrowIfNull(modo);
		ArgumentNullException.ThrowIfNull(puenteDeGnothi);
		ArgumentNullException.ThrowIfNull(almacen);
		ArgumentNullException.ThrowIfNull(http);
		ArgumentNullException.ThrowIfNull(proveedor);

		this.puente = puente;
		this.modo = modo;
		this.puenteDeGnothi = puenteDeGnothi;
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

		int precargados = puente.HayDestino ? await puente.PrecargarEnMemfs() : 0;

		var arrancada = new CargasApp(NombreDelActor);
		arrancada.ConfigureStorage(DatabaseType.FileSystem, "path=" + PuenteDeJournal.RaizEnMemfs);
		arrancada.Start();

		app = arrancada;
		api = new CargasAPI(arrancada);
		// LA FUENTE DEPENDE DEL HOSPEDAJE, y es lo unico que depende junto con el destino
		// (11 §2). Suelta hace su propio pull con su credencial; hospedada le pide a Gnothi,
		// que releva con la suya. El cursor lo pone el actor en los dos casos.
		IFuenteDeEventos fuente = modo.Hospedada
			? new FuenteDeEventosPorGnothi((desde, _) => modo.Puente!.Pull(desde).AsTask())
			: new FuenteHttpDeEventos(http, proveedor.Recurso, proveedor.Id, Credencial, Renovar);

		bucle = new BucleDeRefresh(api, fuente, RutaDeCargas.Rutear, BajarADisco);

		UltimoMensaje = modo.Hospedada
			? $"arriba, hospedada en Gnothi. {precargados} archivos precargados"
			: puente.HayDestino
				? $"arriba. {precargados} archivos precargados de la carpeta '{puente.NombreDeLaCarpeta}'"
				: "arriba, pero SIN carpeta: el journal vive solo en memoria y se pierde al cerrar";

		// La app se pone al dia sola: consigue su credencial si no la tiene y lee lo
		// nuevo. Es el "el app puede hacer cada manana o cada vez que abre" del diseno.
		// Si el proveedor no esta disponible, abrir NO falla: se muestra lo que ya hay
		// en el journal, que es justamente de lo que sirve tener el dato propio.
		try
		{
			// LA CREDENCIAL SOLO EN EL MODO SUELTO. Hospedada la sostiene Gnothi del otro
			// lado del puente y este modulo no ve ningun token: pedirla aca lo mandaria a
			// hablar con el publicador, que es justo lo que la CSP le niega.
			if (!modo.Hospedada) await Credencial(CancellationToken.None);

			ResultadoDeRefresh alAbrir = await bucle.Ejecutar(CancellationToken.None);
			if (alAbrir.Cuenta is not null) Cuenta = alAbrir.Cuenta;
			UltimoMensaje += $" | al abrir: {alAbrir}";
		}
		catch (Exception ex)
		{
			UltimoMensaje += $" | al abrir no se pudo poner al dia ({ex.GetType().Name}: {ex.Message}). "
			               + "Lo que ya estaba registrado se muestra igual.";
		}

		// RECIEN AHORA el modulo puede recibir: el actor esta hidratado. Gnothi no puede
		// adivinarlo mirando la pantalla, que se dibuja antes.
		if (modo.Hospedada) await puenteDeGnothi.Listo();
	}


	// ---------------------------------------------------------------- hospedada en Gnothi

	// EN QUE ESTADO ESTA LA CUENTA ante el proveedor, tal como vino en el ultimo sobre. Null
	// hasta el primer refresh, o si el publicador todavia no manda el bloque `account`.
	public EstadoDeCuenta? Cuenta { get; private set; }

	// La instalacion SUELTA cuyo registro ya se mudo a Gnothi. Hospedada nunca esta migrada:
	// es el destino, no el origen.
	public bool Migrada => !modo.Hospedada && Cuenta is { Migrada: true };

	public string DondeVive => Cuenta?.Host is { Length: > 0 } donde ? donde : "Gnothi";

	public bool Hospedada => modo.Hospedada;

	// UNA CONSULTA, la que Gnothi pide por la superficie del modulo. Contra el actor VIVO y
	// con los MISMOS cuerpos que usa el escritorio (ConsultasDelModulo): si cada hospedaje
	// armara su propia respuesta, el mismo modulo contestaria distinto segun donde corra.
	public object Consultar(string tool)
	{
		Exigir();

		return tool switch
		{
			ConsultasDelModulo.Resumen => ConsultasDelModulo.HacerResumen(api!),
			ConsultasDelModulo.ListaDeVehiculos => ConsultasDelModulo.HacerListaDeVehiculos(api!),
			_ => throw new ArgumentException($"El módulo no atiende la tool '{tool}'."),
		};
	}

	// EL CARTERO ENTREGA. Gnothi trae un sobre ya leido del publicador y se asienta por EL
	// MISMO camino que el refresh propio: misma ruta, misma guarda Check, mismo flush. De ahi
	// sale gratis que entregar dos veces el mismo sobre no duplique nada.
	public async Task<ResultadoDeRefresh> Entregar(string sobreJson, CancellationToken ct = default)
	{
		Exigir();

		var unaVez = new BucleDeRefresh(
			api!,
			new FuenteDeEventosPorGnothi((_, _) => Task.FromResult(sobreJson ?? ""), "gnothi.deliver"),
			RutaDeCargas.Rutear,
			BajarADisco);

		ResultadoDeRefresh resultado = await unaVez.Ejecutar(ct);
		UltimoMensaje = $"entrega de Gnothi: {resultado}";
		return resultado;
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
	// Donde quedo apuntando el cliente. Se dice en los errores: "no se pudo hablar con la
	// estacion" sin decir CON CUAL es un mensaje que no se puede accionar.
	public string DondeEstaElProveedor => http.BaseAddress?.ToString() ?? "(sin configurar)";

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

		// LA CREDENCIAL LA CONSIGUE EL BUCLE, POR EL DELEGADO, y esta linea no vuelve.
		//
		// Aca habia un `await Credencial(ct)` sin condicion, puesto por una razon buena
		// -medida en un iPhone: sin el, si al abrir no se podia hablar con la estacion no
		// quedaba token y cada Refresh moria para siempre diciendo "la app no tiene web
		// token guardado"-. Pero el arreglo estaba en el lugar equivocado.
		//
		// POR QUE ROMPIA HOSPEDADA. Pedir la credencial es HTTP contra el publicador, y
		// `proveedor.BaseUrl` es RELATIVA -"eventos/"-, asi que hospedada la peticion sale
		// a `<su origen>/app/eventos/device/token`: el propio origen del modulo, servido
		// por Gnothi, donde ese archivo no existe. 404. Y hospedada el modulo NO TIENE NI
		// NECESITA credencial: el cartero del Host tiene la suya.
		//
		// DONDE VA. `FuenteHttpDeEventos.LeerDesde` ya pide la credencial POR PULL, por el
		// delegado que se le pasa al construirla, y su comentario dice por que: "quien lee
		// por el puente de Gnothi no [la necesita]. Poner eso en el bucle lo obligaba,
		// hospedada, a inventarse un token que nadie usa". El caso suelto que motivo esta
		// linea ya estaba cubierto ahi.
		//
		// Es lo que Examenes ya hacia. Los cuatro modulos quedan iguales.
		ResultadoDeRefresh resultado = await bucle!.Ejecutar(ct);
		UltimoMensaje = $"refresh: {resultado}";
		return resultado;
	}

	// ------------------------------------------------------- los aportes de Sonia

	// Los aportes son los UNICOS comandos que dispara la pantalla, y cada uno baja a
	// disco al terminar: el flush es por comando, no por timer, igual que en el bucle
	// de refresh. Devuelven el mensaje del Check ("" si entro): un kilometraje que
	// retrocede se rechaza sin dejar entrada, y la pantalla lo dice.

	public async ValueTask<string> AnotarOdometro(string transaccion, decimal kilometros,
		string origen)
	{
		Exigir();
		string rechazo = api!.AnotarOdometro(transaccion, kilometros, "km", origen);
		if (rechazo.Length == 0) await BajarADisco();
		return rechazo;
	}

	public async ValueTask<string> ConfirmarTanque(string transaccion)
	{
		Exigir();
		string rechazo = api!.ConfirmarTanque(transaccion);
		if (rechazo.Length == 0) await BajarADisco();
		return rechazo;
	}

	public async ValueTask<string> DeclararTanque(string transaccion, string estado)
	{
		Exigir();
		string rechazo = api!.DeclararTanque(transaccion, estado);
		if (rechazo.Length == 0) await BajarADisco();
		return rechazo;
	}

	public async ValueTask<string> CompletarFicha(string placa, string alias, string marca,
		string modelo, int anio, string color, decimal rendimientoDeFabrica)
	{
		Exigir();
		string rechazo = api!.CompletarFicha(placa, alias, marca, modelo, anio, color,
			rendimientoDeFabrica);
		if (rechazo.Length == 0) await BajarADisco();
		return rechazo;
	}

	// LA FOTO DEL TABLERO, nivel 1 del §7.4: el archivo va a la carpeta de la persona
	// (fotos/) y el journal guarda solo el nombre. Es el respaldo de un dato que, por
	// ser aporte de Sonia, no tiene otra evidencia. La lectura del numero sigue siendo
	// de ella: aca no hay OCR, y si algun dia lo hay, escribe solo tras su visto bueno.
	public async ValueTask<string> AdjuntarFoto(string transaccion, string extension,
		byte[] contenido)
	{
		Exigir();
		if (!puente.HayCarpeta)
			return "para guardar la foto hace falta una carpeta tuya";

		string nombre = $"fotos/{transaccion}{extension}";
		string motivo = await puente.GuardarArchivo(nombre, contenido);
		if (motivo.Length > 0) return motivo;

		string rechazo = api!.AdjuntarFoto(transaccion, nombre);
		if (rechazo.Length == 0) await BajarADisco();
		return rechazo;
	}

	// ---------------------------------------------------------------- consultas

	public IReadOnlyList<VehiculoEnLista> Vehiculos() { Exigir(); return api!.Vehiculos(); }

	public IReadOnlyList<CargaEnLista> Cargas(string placa) { Exigir(); return api!.Cargas(placa); }

	public IReadOnlyList<TramoEnLista> Tramos(string placa) { Exigir(); return api!.Tramos(placa); }

	// LA consulta que justifica la app: la lectura de la serie de tramos, con su
	// veredicto semantico y su accion.
	public RendimientoDelVehiculo Rendimiento(string placa) { Exigir(); return api!.Rendimiento(placa); }

	public AutonomiaDelVehiculo Autonomia(string placa) { Exigir(); return api!.Autonomia(placa); }

	public CompletitudDelVehiculo Completitud(string placa) { Exigir(); return api!.Completitud(placa); }

	public IReadOnlyList<FaltanteEnLista> Faltantes(string placa) { Exigir(); return api!.Faltantes(placa); }

	// El prellenado del formulario de kilometraje. El "hoy" lo pone ESTA caratula: el
	// dominio no lee el reloj.
	public OdometroPropuesto Odometro(string placa, DateTime hoy) { Exigir(); return api!.Odometro(placa, hoy); }

	public IReadOnlyList<CostoEnLista> Costos(string placa) { Exigir(); return api!.Costos(placa); }

	public IReadOnlyList<PrecioEnLista> Precios(string placa) { Exigir(); return api!.Precios(placa); }

	public IReadOnlyList<GastoEnLista> Gastos(int anio, int mes) { Exigir(); return api!.Gastos(anio, mes); }

	public EstadoDeLealtad Lealtad() { Exigir(); return api!.Lealtad(); }

	public int Cantidad() { Exigir(); return api!.CantidadDeCargas(); }

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
		await puenteDeGnothi.DisposeAsync();
	}
}

public sealed class ConfiguracionDelProveedor
{
	// El juego de URLs de este negocio. Otro negocio es otro recurso sobre el mismo
	// mecanismo: cambiar estos tres valores y su dominio, nada mas.
	public string BaseUrl { get; set; } = "eventos/";
	public string Recurso { get; set; } = "cargas";
	public string Id { get; set; } = "1";
}
