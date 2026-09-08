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
	private readonly PuenteDeTraspaso puenteDeTraspaso;
	private readonly HttpClient http;
	private readonly ConfiguracionDelProveedor proveedor;
	private readonly ConfiguracionDeGnothi gnothi;

	private CargasApp? app;
	private CargasAPI? api;
	private BucleDeRefresh? bucle;

	// UN SOLO CONSTRUCTOR CON LOS DOS JUEGOS DE DEPENDENCIAS. El merge de master trajo dos:
	// el del hospedaje -- modo, puenteDeGnothi, almacen -- y el del traspaso de anotaciones,
	// que agrego puenteDeTraspaso y configuracionDeGnothi. Ninguno reemplaza al otro: la app
	// suelta y la hospedada siguen siendo el mismo binario, y el traspaso es una operacion
	// mas sobre eso. Asi que se unen, y no hay sobrecarga: dos constructores en un servicio
	// que resuelve el contenedor es una forma de que el contenedor elija el equivocado.
	public EstadoDeLaApp(PuenteDeJournal puente, ModoDeLaApp modo, PuenteDeGnothi puenteDeGnothi,
		AlmacenDeCredencial almacen, PuenteDeTraspaso puenteDeTraspaso, HttpClient http,
		ConfiguracionDelProveedor proveedor, ConfiguracionDeGnothi gnothi)
	{
		ArgumentNullException.ThrowIfNull(puente);
		ArgumentNullException.ThrowIfNull(modo);
		ArgumentNullException.ThrowIfNull(puenteDeGnothi);
		ArgumentNullException.ThrowIfNull(almacen);
		ArgumentNullException.ThrowIfNull(puenteDeTraspaso);
		ArgumentNullException.ThrowIfNull(http);
		ArgumentNullException.ThrowIfNull(proveedor);
		ArgumentNullException.ThrowIfNull(gnothi);

		this.puente = puente;
		this.modo = modo;
		this.puenteDeGnothi = puenteDeGnothi;
		this.almacen = almacen;
		this.puenteDeTraspaso = puenteDeTraspaso;
		this.http = http;
		this.proveedor = proveedor;
		this.gnothi = gnothi;
	}

	public bool EstaArriba => app is not null;

	/// <summary>SE DISPARA CUANDO EL ACTOR CAMBIO y las pantallas tienen que releer.
	///
	/// Existe por las pestañas: el boton de Actualizar vive en el layout y lo que hay que
	/// releer vive en la pagina, asi que no alcanza con que quien aprieta repinte. Blazor
	/// no observa servicios inyectados, y este singleton es lo unico que las cuatro
	/// pantallas comparten.
	///
	/// Lo dispara TODO lo que cambia el estado del actor -arrancar, refrescar y cada
	/// aporte-, y no solo el refresh: si un aporte hecho en la Ruta desbloquea un tramo,
	/// el Tablero tiene que enterarse aunque nadie lo haya tocado.</summary>
	// ------------------------------------------------------- el carro que se esta mirando

	// LA PLACA ELEGIDA es una PREFERENCIA DE PANTALLA, no un hecho: vive en IndexedDB al
	// lado del handle de la carpeta y no en el journal. Que carro estaba mirando alguien
	// no es un hecho de la estacion ni un aporte sobre el carro, y si estuviera en el
	// journal el servidor MCP la leeria sin que significara nada para el.
	private string placaElegida = "";

	/// <summary>La placa que se esta mirando, resuelta contra los vehiculos que HAY.
	///
	/// La preferencia guardada es una PISTA, no una verdad: el carro pudo dejar de estar
	/// -otra computadora, otra bitacora importada- y en ese caso se cae al primero en vez
	/// de dejar el tablero en blanco apuntando a una placa que ya no existe. Total a
	/// proposito: sin vehiculos devuelve "".
	///
	/// Recibe la lista en vez de volver a consultarla porque quien llama ya la tiene: es
	/// un PerformQuery, y pedirlo dos veces por repintado no es gratis.</summary>
	public string PlacaEnFoco(IReadOnlyList<VehiculoEnLista> vehiculos)
	{
		ArgumentNullException.ThrowIfNull(vehiculos);
		if (vehiculos.Count == 0) return "";

		foreach (VehiculoEnLista vehiculo in vehiculos)
			if (vehiculo.Placa == placaElegida) return placaElegida;

		return vehiculos[0].Placa;
	}

	/// <summary>Elegir carro. Se recuerda para la proxima vez y se avisa, porque lo que
	/// cambia no es una pantalla: cambian las cuatro.</summary>
	public async ValueTask ElegirVehiculo(string placa)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(placa);
		if (placa == placaElegida) return;

		placaElegida = placa;
		await puente.RecordarPreferencia(ClaveDelCarro, placa);
		Avisar();
	}

	private const string ClaveDelCarro = "carro-en-foco";

	/// <summary>Cuantas veces se fue a buscar datos. Es la llave que hace que el cuadro de
	/// instrumentos se recree y vuelva a entrar animado.
	///
	/// La animacion se dispara al MONTAR, asi que lo que decide cuando corre no es el CSS:
	/// es que el nodo se recree, y eso lo decide el @key. Sube SOLO acá y no en cada
	/// lectura, para que anotar un kilometraje no haga entrar de nuevo a los seis
	/// instrumentos: eso es respuesta a "fui a buscar y volvi", no a teclear un numero.</summary>
	public int Refrescos { get; private set; }

	/// <summary>SI SE ESTA PONIENDO AL DIA con la estacion, sea al abrir o por la tecla.
	///
	/// Vive aca y no en el layout porque el arranque tambien refresca, y el layout no
	/// puede saber cuando empieza ni cuando termina ese refresh: solo ve el aviso. Con esto
	/// la tecla dice "Actualizando…" tambien durante el arranque, que es cuando mas dura.</summary>
	public bool Refrescando { get; private set; }

	/// <summary>EL REFRESH QUE NO LLEGO. Antes esto iba a UltimoMensaje, que ninguna
	/// pantalla lee, asi que una estacion caida y una estacion sin novedades se veian
	/// identicas. No es un error de la app -el journal propio se muestra igual- pero hay
	/// que decirlo, y se limpia solo con el siguiente refresh que si llega.</summary>
	public string ProblemaAlPonerseAlDia { get; private set; } = "";

	public event Action? Cambio;

	private void Avisar() => Cambio?.Invoke();

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

	// Las preferencias de pantalla, tal cual salen del puente. Ver PuenteDeJournal.
	public ValueTask<string> Preferencia(string nombre) => puente.Preferencia(nombre);

	public ValueTask RecordarPreferencia(string nombre, string valor) =>
		puente.RecordarPreferencia(nombre, valor);

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

		// La preferencia se lee ANTES de avisar: asi la primera pintada ya sale con el
		// carro que quedo elegido, en vez de mostrar el primero y saltar despues.
		placaElegida = await puente.Preferencia(ClaveDelCarro);

		// ARRIBA YA, ANTES DE IR A LA ESTACION. El journal propio alcanza para pintar el
		// tablero; ponerse al dia es lo siguiente, no la condicion. Antes el aviso iba
		// despues del refresh, y con la estacion lenta -o caida, hasta el timeout- la
		// pantalla se quedaba en "elegí dónde guardar" mientras el actor ya estaba vivo.
		// Medido: entre 2 y 8 segundos de elegidor para alguien que ya habia elegido.
		Refrescando = true;
		Avisar();

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
			ProblemaAlPonerseAlDia = "";

			// El cuadro vuelve a entrar animado SOLO si llego algo: es la respuesta a "fui
			// a buscar y volvi con novedades". Sin novedades, la primera pintada ya animo y
			// repetirla dos segundos despues es un parpadeo sin motivo.
			if (alAbrir.Aplicados > 0) Refrescos++;

			// Las anotaciones que llegaron antes que sus cargas: ahora que la estacion
			// publico, se vuelven a intentar (ver Traspasar).
			if (alAbrir.Aplicados > 0) await ReaplicarTraspasoPendiente();
		}
		catch (Exception ex)
		{
			UltimoMensaje += $" | al abrir no se pudo poner al dia ({ex.GetType().Name}: {ex.Message}). "
			               + "Lo que ya estaba registrado se muestra igual.";
			ProblemaAlPonerseAlDia = $"{ex.GetType().Name}: {ex.Message}";
		}
		finally
		{
			Refrescando = false;
			Avisar();
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
		// Medido en un iPhone: la app abria, el arranque no alcanzaba el proveedor, y el
		// boton quedaba inutil reportando la consecuencia en vez de la causa.
		// Se avisa al ENTRAR y no solo al salir: la tecla del capo tiene que decir
		// "Actualizando…" en el acto, no cuando el Intentar de la cascara repinte.
		Refrescando = true;
		Avisar();
		try
		{
			await Credencial(ct);

			ResultadoDeRefresh resultado = await bucle!.Ejecutar(ct);
			UltimoMensaje = $"refresh: {resultado}";
			ProblemaAlPonerseAlDia = "";
			Refrescos++;

			if (resultado.Aplicados > 0) await ReaplicarTraspasoPendiente();
			return resultado;
		}
		finally
		{
			Refrescando = false;
			Avisar();
		}
	}

	// ------------------------------------------------------- los aportes de Sonia

	// Los aportes son los UNICOS comandos que dispara la pantalla, y cada uno baja a
	// disco al terminar: el flush es por comando, no por timer, igual que en el bucle
	// de refresh. Devuelven el mensaje del Check ("" si entro): un kilometraje que
	// retrocede se rechaza sin dejar entrada, y la pantalla lo dice.

	/// <summary>El kilometraje que ella leyo, EN LA UNIDAD EN QUE LO LEYO.
	///
	/// La unidad iba clavada en "km", y con el selector de unidades eso pasaba de detalle a
	/// bug silencioso: si el campo muestra millas y el comando dice km, cada lectura entra
	/// 1.6 veces mas grande, ya journalizada, la coherencia por monotonia empieza a
	/// rechazar entradas validas y la linea base queda envenenada sin que nada lo diga.
	///
	/// La preferencia decide el DEFAULT del campo; lo que se journaliza es la unidad en la
	/// que ella lo leyo, que es la que el dominio necesita para normalizar.</summary>
	public async ValueTask<string> AnotarOdometro(string transaccion, decimal kilometraje,
		string unidad, string origen)
	{
		Exigir();
		string rechazo = api!.AnotarOdometro(transaccion, kilometraje, unidad, origen);
		if (rechazo.Length == 0) { await BajarADisco(); Avisar(); }
		return rechazo;
	}

	public async ValueTask<string> ConfirmarTanque(string transaccion)
	{
		Exigir();
		string rechazo = api!.ConfirmarTanque(transaccion);
		if (rechazo.Length == 0) { await BajarADisco(); Avisar(); }
		return rechazo;
	}

	public async ValueTask<string> DeclararTanque(string transaccion, string estado)
	{
		Exigir();
		string rechazo = api!.DeclararTanque(transaccion, estado);
		if (rechazo.Length == 0) { await BajarADisco(); Avisar(); }
		return rechazo;
	}

	public async ValueTask<string> CompletarFicha(string placa, string alias, string marca,
		string modelo, int anio, string color, decimal rendimientoDeFabrica)
	{
		Exigir();
		string rechazo = api!.CompletarFicha(placa, alias, marca, modelo, anio, color,
			rendimientoDeFabrica);
		if (rechazo.Length == 0) { await BajarADisco(); Avisar(); }
		return rechazo;
	}

	// LA FOTO DEL TABLERO, nivel 1 del §7.4: el archivo va a la carpeta de la persona
	// (fotos/) y el journal guarda solo el nombre. Es el respaldo de un dato que, por
	// ser aporte de Sonia, no tiene otra evidencia. La lectura del numero sigue siendo
	// de ella: aca no hay OCR, y si algun dia lo hay, escribe solo tras su visto bueno.
	/// <summary>La foto del tablero, de vuelta de la carpeta, en base64. "" si no se pudo.</summary>
	public ValueTask<string> LeerFoto(string nombreDeArchivo) => puente.LeerArchivo(nombreDeArchivo);

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
		if (rechazo.Length == 0) { await BajarADisco(); Avisar(); }
		return rechazo;
	}

	// ------------------------------------------------- el traspaso de anotaciones a Gnothi

	// LO QUE SONIA ANOTO ELLA MISMA es lo unico que cruza cuando esta app pasa a vivir dentro
	// de Gnothi (especificacion 13 §5). Los hechos de la estacion no viajan -el modulo
	// hospedado los vuelve a pedir con la credencial de Gnothi- y la credencial de esta
	// instalacion tampoco. El sobre lo arma Cargas.App.Traspaso; aca solo se decide como
	// sale (la hoja de compartir) y como entra (Traspasar).
	//
	// POR LA HOJA DE COMPARTIR Y NO POR EL ENLACE. En Examenes la unica asercion es el sexo
	// declarado y cabe en la URL del handoff. Aca son decenas: medido con el guion de 28
	// cargas, el sobre pesa ~9.7 KB (~21 KB escapado para URL). El enlace del handoff abre
	// Gnothi nombrando el modulo; el sobre va como archivo de texto (13 §5.2).

	/// <summary>Cuanto hay para pasar, en piezas, para que la pantalla lo enuncie antes del
	/// toque. Las fotos se cuentan aparte porque NO viajan.</summary>
	public ResumenDeTraspaso QueSePasa() { Exigir(); return Traspaso.Resumir(api!); }

	/// <summary>El sobre entero. Publico para poder mirarlo; lo que la persona toca es
	/// CompartirConGnothi.</summary>
	public string SobreDeTraspaso() { Exigir(); return Traspaso.Armar(api!, DondeEstaElProveedor, DateTime.UtcNow); }

	public ValueTask<bool> EsAndroid() => puenteDeTraspaso.EsAndroid();

	public ValueTask<bool> PuedeCompartirArchivos() => puenteDeTraspaso.PuedeCompartirArchivos();

	// EL ENLACE QUE ABRE GNOTHI, nombrando el modulo y sin las aserciones adentro: no caben.
	//
	// Android: intent:// con package y fallback explicito. Si Gnothi esta instalado el SO lo
	// abre; si no, Chrome carga la pagina de Gnothi. EXIGE GESTO. iOS y el resto: un https
	// normal al dominio de Gnothi, en un <a href> TOCADO -no window.location desde JS, que
	// iOS trata como pegar la URL en la barra- y a un dominio distinto al de esta app.
	public string EnlaceParaAbrirGnothi(bool android)
	{
		string consulta = $"modulo={Traspaso.Modulo}";
		string paginaDeGnothi = $"https://{gnothi.Dominio}/abrir?{consulta}";

		if (!android) return paginaDeGnothi;

		return $"intent://{gnothi.Dominio}/abrir?{consulta}"
			+ $"#Intent;scheme=https;package={gnothi.Paquete}"
			+ $";S.browser_fallback_url={Uri.EscapeDataString(paginaDeGnothi)};end";
	}

	/// <summary>EXIGE GESTO. Arma el sobre y lo saca por la hoja de compartir; donde no hay,
	/// lo descarga y lo dice. Repetirlo es inocuo: el modulo que lo reciba no duplica.</summary>
	public async ValueTask<ResultadoDeCompartir> CompartirConGnothi()
	{
		Exigir();

		string sobre = Traspaso.Armar(api!, DondeEstaElProveedor, DateTime.UtcNow);
		string nombre = $"mis-anotaciones-cargas-{DateTime.UtcNow:yyyy-MM-dd}.txt";
		ResultadoDeCompartir resultado = await puenteDeTraspaso.Compartir(nombre, sobre, "Mis anotaciones de cargas");
		UltimoMensaje = $"traspaso hacia Gnothi: {(resultado.Ok ? resultado.Como : resultado.Motivo)}";
		return resultado;
	}

	// RECIBIR EL TRASPASO. Es el verbo que Gnothi le va a llamar al modulo hospedado
	// -`window.gnothi.traspasar`, como en Examenes- cuando la capa de hospedaje llegue a
	// esta rama; hasta entonces no tiene quien lo llame, y esta aca para que el que la traiga
	// solo tenga que cablearlo.
	//
	// NO ENTRA POR EL CAMINO DE LOS EVENTOS. Los eventos de la estacion vienen numerados,
	// mueven el cursor y la guarda Check los hace idempotentes. Esto son comandos de la
	// persona, sin numero: la idempotencia la pone Traspaso comparando contra lo que ya esta.
	//
	// LO QUE ESPERA SE GUARDA. Gnothi entrega el traspaso apenas el modulo arranca, y puede
	// que las cargas aun no hayan llegado. Las anotaciones cuya carga no esta quedan en un
	// archivo al lado del journal -viaja con la carpeta, igual que las fotos- y se vuelven a
	// aplicar despues de cada refresh que traiga algo, hasta que nada espere.
	public async Task<ResultadoDeTraspaso> Traspasar(string sobreJson)
	{
		Exigir();

		ResultadoDeTraspaso resultado = Traspaso.Aplicar(api!, sobreJson);
		UltimoTraspaso = resultado;

		if (resultado.Pendiente) EscribirTraspasoPendiente(sobreJson);
		else if (HayTraspasoPendiente) EscribirTraspasoPendiente("");

		// Baja a disco una sola vez, al final: son comandos de la persona, no un bloque de
		// eventos que haya que poder reanudar a mitad. Sin carpeta no hace nada.
		await BajarADisco();
		if (resultado.Aplicadas > 0) Avisar();

		UltimoMensaje = $"traspaso recibido: {resultado}";
		return resultado;
	}

	/// <summary>El resultado del ultimo traspaso recibido, para que la pantalla lo cuente.</summary>
	public ResultadoDeTraspaso? UltimoTraspaso { get; private set; }

	/// <summary>Si hay anotaciones recibidas que todavia esperan a sus cargas.</summary>
	public bool HayTraspasoPendiente =>
		File.Exists(RutaDelTraspasoPendiente) && new FileInfo(RutaDelTraspasoPendiente).Length > 0;

	// Al lado del journal, no adentro: `/journal/cargas/` es del actor; esto es de la app.
	// Se SOBREESCRIBE VACIO en vez de borrarse porque el puente no tiene "borrar": un
	// archivo borrado solo en MEMFS volveria de la carpeta en la proxima apertura.
	private static readonly string RutaDelTraspasoPendiente =
		Path.Combine(PuenteDeJournal.RaizEnMemfs, "traspaso", "pendiente.json");

	private static void EscribirTraspasoPendiente(string sobreJson)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(RutaDelTraspasoPendiente)!);
		File.WriteAllText(RutaDelTraspasoPendiente, sobreJson ?? "");
	}

	private async Task ReaplicarTraspasoPendiente()
	{
		if (!HayTraspasoPendiente) return;

		string sobre = File.ReadAllText(RutaDelTraspasoPendiente);
		ResultadoDeTraspaso resultado = Traspaso.Aplicar(api!, sobre);
		UltimoTraspaso = resultado;

		if (!resultado.Pendiente) EscribirTraspasoPendiente("");
		await BajarADisco();
		UltimoMensaje += $" | traspaso pendiente: {resultado}";
	}

	// ---------------------------------------------------------------- consultas

	public IReadOnlyList<VehiculoEnLista> Vehiculos() { Exigir(); return api!.Vehiculos(); }

	public IReadOnlyList<CargaEnLista> Cargas(string placa) { Exigir(); return api!.Cargas(placa); }

	public IReadOnlyList<TramoEnLista> Tramos(string placa) { Exigir(); return api!.Tramos(placa); }

	// LA consulta que justifica la app: la lectura de la serie de tramos, con su
	// veredicto semantico y su accion.
	public RendimientoDelVehiculo Rendimiento(string placa) { Exigir(); return api!.Rendimiento(placa); }

	public AutonomiaDelVehiculo Autonomia(string placa) { Exigir(); return api!.Autonomia(placa); }

	// El "hoy" lo pone la caratula, como en Odometro(): el dominio no lee el reloj.
	public ProximaCargaDelVehiculo ProximaCarga(string placa, DateTime hoy)
	{ Exigir(); return api!.ProximaCarga(placa, hoy); }

	public CompletitudDelVehiculo Completitud(string placa) { Exigir(); return api!.Completitud(placa); }

	public IReadOnlyList<FaltanteEnLista> Faltantes(string placa) { Exigir(); return api!.Faltantes(placa); }

	// El prellenado del formulario de kilometraje. El "hoy" lo pone ESTA caratula: el
	// dominio no lee el reloj.
	public OdometroPropuesto Odometro(string placa, DateTime hoy) { Exigir(); return api!.Odometro(placa, hoy); }

	public IReadOnlyList<CostoEnLista> Costos(string placa) { Exigir(); return api!.Costos(placa); }

	public IReadOnlyList<PrecioEnLista> Precios(string placa) { Exigir(); return api!.Precios(placa); }

	public IReadOnlyList<GastoEnLista> Gastos(int anio, int mes) { Exigir(); return api!.Gastos(anio, mes); }

	/// <summary>El gasto de UN carro. El de la persona -Gastos(anio, mes)- sigue existiendo
	/// para cuando la pregunta sea de la billetera; el tablero usa este.</summary>
	public IReadOnlyList<GastoEnLista> Gastos(string placa, int anio, int mes)
	{ Exigir(); return api!.GastosDe(placa, anio, mes); }

	public EstadoDeLealtad Lealtad() { Exigir(); return api!.Lealtad(); }

	// ---------------------------------------------------------------- las unidades

	public UnidadesElegidas Unidades() { Exigir(); return api!.Unidades(); }

	// LAS TOMBOLAS. El "hoy" lo pone esta caratula, como en Odometro y ProximaCarga.
	public IReadOnlyList<SorteoEnLista> Sorteos(DateTime hoy)
	{ Exigir(); return api!.Sorteos(hoy); }

	public IReadOnlyList<BoletosDeLaCargaEnLista> BoletosDeLasCargas(string placa)
	{ Exigir(); return api!.BoletosDeLasCargas(placa); }

	/// <summary>Elegir en que se mira. Va al journal -cambia lo que la IA DICE, no solo lo
	/// que se ve- y por eso baja a disco como cualquier otro aporte.</summary>
	public async ValueTask<string> ElegirPresentacionDeRendimiento(string simbolo)
	{
		Exigir();
		string rechazo = api!.ElegirPresentacionDeRendimiento(simbolo);
		if (rechazo.Length == 0) { await BajarADisco(); Avisar(); }
		return rechazo;
	}

	public async ValueTask<string> ElegirUnidadDeDistancia(string simbolo)
	{
		Exigir();
		string rechazo = api!.ElegirUnidadDeDistancia(simbolo);
		if (rechazo.Length == 0) { await BajarADisco(); Avisar(); }
		return rechazo;
	}

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
		await puenteDeTraspaso.DisposeAsync();
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
