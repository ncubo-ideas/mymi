using System.Text.Json;
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
	private readonly ModoDeLaApp modo;
	private readonly ConfiguracionDeGnothi gnothi;
	private readonly PuenteDeGnothi puenteDeGnothi;
	private readonly AlmacenDeCredencial almacen;
	private readonly HttpClient http;
	private readonly ConfiguracionDelProveedor proveedor;

	// EN QUE IDIOMA LE PREGUNTA ESTA PANTALLA A LA PUERTA: el que la persona eligio en su
	// dispositivo, negociado una vez al arrancar. La puerta no lo adivina y el dominio no lo
	// conoce -- una frase es texto de presentacion y el idioma se elige aca.
	private readonly Textos textos;

	private ExamenesApp? app;
	private ExamenesAPI? api;
	private BucleDeRefresh? bucle;

	public EstadoDeLaApp(PuenteDeJournal puente, ModoDeLaApp modo, ConfiguracionDeGnothi gnothi, PuenteDeGnothi puenteDeGnothi, AlmacenDeCredencial almacen, HttpClient http, ConfiguracionDelProveedor proveedor, Textos textos)
	{
		ArgumentNullException.ThrowIfNull(textos);
		ArgumentNullException.ThrowIfNull(puente);
		ArgumentNullException.ThrowIfNull(modo);
		ArgumentNullException.ThrowIfNull(gnothi);
		ArgumentNullException.ThrowIfNull(puenteDeGnothi);
		ArgumentNullException.ThrowIfNull(almacen);
		ArgumentNullException.ThrowIfNull(http);
		ArgumentNullException.ThrowIfNull(proveedor);

		this.puente = puente;
		this.modo = modo;
		this.gnothi = gnothi;
		this.puenteDeGnothi = puenteDeGnothi;
		this.almacen = almacen;
		this.http = http;
		this.proveedor = proveedor;
		this.textos = textos;
	}

	public bool EstaArriba => app is not null;

	// Si corre dentro de Gnothi. La pantalla lo mira para no ofrecer elegir carpeta: en este
	// modo no hay nada que elegir, el destino es de Gnothi.
	public bool Hospedada => modo.Hospedada;

	public bool HayCarpeta => puente.HayCarpeta;

	public bool FaltaReotorgarPermiso => puente.FaltaReotorgarPermiso;

	public string NombreDeLaCarpeta => puente.NombreDeLaCarpeta;

	public string UltimoMensaje { get; private set; } = "";

	/// <summary>Cuantas veces se fue a buscar al laboratorio y volvio con algo. Es la llave
	/// que hace que la portada se recree y vuelva a entrar animada.
	///
	/// La animacion se dispara al MONTAR, asi que lo que decide cuando corre no es el CSS:
	/// es que el nodo se recree, y eso lo decide el @key. Sube SOLO aca y no en cada
	/// lectura, para que declarar el sexo o plegar el pedido no vuelva a animar la portada:
	/// eso es respuesta a "fui a buscar y volvi", no a tocar un boton.</summary>
	public int Refrescos { get; private set; }

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

		var arrancada = new ExamenesApp(NombreDelActor);
		arrancada.ConfigureStorage(DatabaseType.FileSystem, "path=" + PuenteDeJournal.RaizEnMemfs);
		arrancada.Start();

		app = arrancada;
		api = new ExamenesAPI(arrancada);
		// LA FUENTE DEPENDE DEL HOSPEDAJE, y es lo unico que depende junto con el destino
		// (11 §2). Suelta hace su propio pull con su credencial; hospedada le pide a Gnothi,
		// que releva con la suya. El cursor lo pone el actor en los dos casos.
		IFuenteDeEventos fuente = modo.Hospedada
			? new FuenteDeEventosPorGnothi((desde, _) => modo.Puente!.Pull(desde).AsTask())
			: new FuenteHttpDeEventos(http, proveedor.Recurso, proveedor.Id, Credencial, Renovar);

		bucle = new BucleDeRefresh(api, fuente, RutaDeExamenes.Rutear, BajarADisco);

		UltimoMensaje = modo.Hospedada
			? $"arriba, hospedada en Mymi. {precargados} archivos precargados"
			: puente.HayDestino
				? $"arriba. {precargados} archivos precargados de la carpeta '{puente.NombreDeLaCarpeta}'"
				: "arriba, pero SIN carpeta: el journal vive solo en memoria y se pierde al cerrar";

		// La app se pone al dia sola: consigue su credencial si no la tiene y lee lo
		// nuevo. Es el "el app puede hacer cada manana o cada vez que abre" del diseno.
		// Si el proveedor no esta disponible, abrir NO falla: se muestra lo que ya hay
		// en el journal, que es justamente de lo que sirve tener el dato propio.
		try
		{
			ResultadoDeRefresh alAbrir = await bucle.Ejecutar(CancellationToken.None);
			if (alAbrir.Cuenta is not null) Cuenta = alAbrir.Cuenta;
			UltimoMensaje += $" | al abrir: {alAbrir}";

			// La portada entra animada al abrir SOLO si llego algo. Sin novedades, la
			// primera pintada ya animo y repetirla al volver del laboratorio es un
			// parpadeo sin motivo.
			if (alAbrir.Aplicados > 0) Refrescos++;
		}
		catch (Exception ex)
		{
			UltimoMensaje += $" | al abrir no se pudo poner al dia ({ex.GetType().Name}: {ex.Message}). "
			               + "Lo que ya estaba registrado se muestra igual.";
		}

		// RECIEN AHORA el modulo puede recibir: el actor esta hidratado. Gnothi estaba
		// esperando esto para entregarle lo que trajo el handoff.
		if (modo.Hospedada) await puenteDeGnothi.Listo();
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
	// Donde quedo apuntando el cliente. Se dice en los errores: "no se pudo hablar con el
	// laboratorio" sin decir CON CUAL es un mensaje que no se puede accionar.
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

		// La credencial la consigue el bucle por el delegado Credencial: si al abrir no se
		// pudo hablar con el laboratorio -sin red, otra maquina, la URL equivocada-, el
		// Refresh la pide ahora. Medido en un iPhone cuando esto no era asi: la app abria,
		// el arranque no alcanzaba el proveedor, y el boton quedaba inutil reportando la
		// consecuencia ("no tiene token") en vez de la causa.

		// MIGRADA: esta instalacion ya no sincroniza. Sigue mostrando lo que tiene -es de
		// ella- pero dejo de ser la que escribe, y pedirle eventos al proveedor seria
		// arrastrar una segunda copia que diverge.
		if (Migrada)
		{
			UltimoMensaje = $"tus examenes nuevos ahora entran por {DondeVive}: esta instalación ya no busca novedades";
			return new ResultadoDeRefresh();
		}

		ResultadoDeRefresh resultado = await bucle!.Ejecutar(ct);
		if (resultado.Cuenta is not null) Cuenta = resultado.Cuenta;
		UltimoMensaje = $"refresh: {resultado}";

		// Aca sube SIEMPRE, aunque no entre nada. Es la diferencia con el arranque: la
		// persona apreto Actualizar y la pantalla le tiene que contestar algo visible,
		// que es justo lo que este @key existia para resolver.
		Refrescos++;
		return resultado;
	}


	// ---------------------------------------------------------------- la migración

	// EL ESTADO DE LA CUENTA ante el proveedor, tal como vino en el último sobre. Null
	// mientras no se haya hablado con el proveedor, o si el proveedor no manda el campo.
	public EstadoDeCuenta? Cuenta { get; private set; }

	// SI ESTA INSTALACION YA NO ES LA QUE ESCRIBE. La marca la pone el proveedor cuando le
	// emitió credencial a Gnothi (especificación 13 §4), y esta app se entera en su próximo
	// pull. Hospedada NUNCA es true: hospedada ES el destino, no la que se retira.
	public bool Migrada => !modo.Hospedada && Cuenta is { Migrada: true };

	// POR DONDE ENTRAN AHORA. Se dice en pantalla -"tus examenes nuevos ahora entran por X"-
	// porque sin decir por donde es un mensaje que no se puede accionar.
	//
	// EL NOMBRE DICE "DONDE VIVE" Y LA PANTALLA YA NO, a proposito: el dato no se mudo a
	// ningun lado, sigue en el telefono de ella. Lo que cambio es quien lo escribe. El campo
	// conserva el nombre porque viaja en el sobre de eventos, que es contrato (13 §10.6).
	public string DondeVive => Cuenta?.Host is { Length: > 0 } donde ? donde : "Mymi";

	// LO QUE SONIA AFIRMÓ POR SÍ MISMA, que es lo único que cruza en el traspaso (13 §5.1).
	// Los hechos del laboratorio NO viajan: el módulo nuevo los vuelve a pedir desde cero.
	// Tampoco viaja ninguna credencial (13 §3.4).
	//
	// Hoy en Exámenes esto es UN dato -el sexo declarado- y por eso cabe en el enlace del
	// handoff. El formato queda genérico igual, porque otras instituciones van a esperar más
	// aserciones de la persona.
	public string AsercionesParaTraspaso()
	{
		Exigir();

		string sexo = api!.SexoDeclaradoPorLaPersona();

		return System.Text.Json.JsonSerializer.Serialize(new
		{
			version = 1,
			publicador = DondeEstaElProveedor,
			aserciones = sexo.Length > 0 ? new { sexoDeclarado = sexo } : null,
		});
	}


	// EL ENLACE QUE ABRE GNOTHI, con las aserciones de Sonia adentro (13 §5.2 y §6.1).
	//
	// Android: intent:// con package y fallback explicito. Si Gnothi esta instalado el SO lo
	// abre; si no, Chrome carga la pagina de Gnothi. EXIGE GESTO: sin un toque, Chrome no
	// lanza la app y se va derecho al fallback.
	//
	// iOS y el resto: un https normal al dominio de Gnothi. Tiene que ser un <a href> TOCADO
	// -no window.location desde JS, que iOS trata como pegar la URL en la barra- y a un
	// dominio distinto al de esta app.
	//
	// Las aserciones viajan en la URL porque hoy son UN dato. Cuando sean muchas no van a
	// entrar, y ahi el camino es el archivo por el share sheet (13 §5.2).
	// Que forma tiene que tener el enlace. Lo contesta el browser, no la app.
	public ValueTask<bool> EsAndroid() => puenteDeGnothi.EsAndroid();

	public string EnlaceParaAbrirGnothi(bool android)
	{
		Exigir();

		string aserciones = Uri.EscapeDataString(AsercionesParaTraspaso());
		string consulta = $"modulo=examenes&aserciones={aserciones}";
		string paginaDeGnothi = $"https://{gnothi.Dominio}/abrir?{consulta}";

		if (!android) return paginaDeGnothi;

		return $"intent://{gnothi.Dominio}/abrir?{consulta}"
			+ $"#Intent;scheme=https;package={gnothi.Paquete}"
			+ $";S.browser_fallback_url={Uri.EscapeDataString(paginaDeGnothi)};end";
	}
	// Si hay algo que pasar. Sin aserciones propias el botón no tiene sentido: el módulo
	// nuevo se completa solo pidiéndole los hechos al laboratorio.
	public bool HayAlgoQuePasar()
	{
		Exigir();
		return api!.SexoDeclaradoPorLaPersona().Length > 0;
	}

	// ---------------------------------------------------------------- hacia Gnothi

	// UNA CONSULTA DEL HOST, contra el actor vivo. Los cuerpos son los mismos que usa la
	// superficie de escritorio (ConsultasDelModulo), asi que el modulo contesta igual corra
	// donde corra. Una tool que no existe se dice; no se contesta con algo inventado.
	public object Consultar(string tool)
	{
		Exigir();

		return tool switch
		{
			ConsultasDelModulo.Resumen => ConsultasDelModulo.HacerResumen(api!),
			ConsultasDelModulo.ListaDeExamenes =>
				ConsultasDelModulo.HacerListaDeExamenes(api!, ExamenesAPI.IdiomaDelRegistro),
			_ => throw new ArgumentException($"El módulo no atiende la tool '{tool}'."),
		};
	}

	// RECIBIR EL TRASPASO. Lo que la instalación suelta le pasó a Gnothi cuando Sonia tocó
	// "Abrir Gnothi", y que Gnothi le entrega al módulo recién creado (especificación 13 §5).
	//
	// NO ENTRA POR `deliver`, y la diferencia importa. `deliver` es el cartero trayendo la
	// narrativa del PROVEEDOR: eventos numerados que mueven el cursor y que la guarda Check
	// hace idempotentes. Esto es otra cosa: son las aserciones de SONIA, comandos locales con
	// procedencia TÚ, que no vienen numerados y no mueven ningún cursor. Meterlos por el
	// mismo verbo obligaría a inventarles un número de evento, que es justo lo que
	// desalinearía la bitácora.
	//
	// LA IDEMPOTENCIA ES DISTINTA, y esto corrige lo que el 13 §5.3 daba por hecho. Los
	// eventos del proveedor la traen de la guarda Check. `DeclararSexo` NO tiene Check, y a
	// propósito: volver a declarar es una operación legítima cuando la persona aprieta el
	// botón, y cada declaración es un hecho fechado que el journal conserva. Pero un traspaso
	// que se repite solo -porque el enlace se abrió dos veces- no es la persona corrigiéndose.
	// Por eso el guard está ACÁ: si el valor ya es el que está, no se asienta nada.
	public async Task<ResultadoDeTraspaso> Traspasar(string asercionesJson, CancellationToken ct = default)
	{
		Exigir();

		var resultado = new ResultadoDeTraspaso();
		if (string.IsNullOrWhiteSpace(asercionesJson)) return resultado;

		using JsonDocument documento = JsonDocument.Parse(asercionesJson);
		if (!documento.RootElement.TryGetProperty("aserciones", out JsonElement aserciones)
			|| aserciones.ValueKind != JsonValueKind.Object)
			return resultado;

		if (aserciones.TryGetProperty("sexoDeclarado", out JsonElement sexo)
			&& sexo.ValueKind == JsonValueKind.String
			&& sexo.GetString() is { Length: > 0 } declarado)
		{
			if (string.Equals(api!.SexoDeclaradoPorLaPersona(), declarado, StringComparison.Ordinal))
			{
				resultado.Omitidas++;
			}
			else
			{
				api.DeclararSexo(declarado);
				resultado.Aplicadas++;
			}
		}

		// Baja a disco una sola vez, al final: son comandos de la persona, no un bloque de
		// eventos que haya que poder reanudar a mitad.
		if (resultado.Aplicadas > 0 && puente.HayDestino) await BajarADisco();

		UltimoMensaje = $"traspaso: {resultado}";
		return resultado;
	}

	// EL CARTERO ENTREGA. Gnothi trae un sobre ya leido -del publicador, o el traspaso de
	// aserciones de la instalacion suelta- y se asienta por EL MISMO camino que el refresh
	// propio: misma ruta, misma guarda Check, mismo flush. De ahi sale gratis que entregar
	// dos veces el mismo sobre no duplique nada.
	public async Task<ResultadoDeRefresh> Entregar(string sobreJson, CancellationToken ct = default)
	{
		Exigir();

		var unaVez = new BucleDeRefresh(
			api!,
			new FuenteDeEventosPorGnothi((_, _) => Task.FromResult(sobreJson ?? ""), "gnothi.deliver"),
			RutaDeExamenes.Rutear,
			BajarADisco);

		ResultadoDeRefresh resultado = await unaVez.Ejecutar(ct);
		UltimoMensaje = $"entrega de Mymi: {resultado}";
		return resultado;
	}

	// ---------------------------------------------------------------- consultas

	public IReadOnlyList<ExamenEnLista> Examenes() { Exigir(); return api!.Examenes(textos.Idioma); }

	// Los analitos de los que hay historia. Es lo que la pantalla recorre para mostrar
	// una lectura por serie, sin conocer las clases del dominio.
	public IReadOnlyList<string> Analitos() { Exigir(); return api!.Analitos(); }

	// LOS PUNTOS de la serie, con su numero, y LAS LINEAS que los acompanan. Es lo que un
	// grafico necesita y el cuadro no da: aquel trae "3.0 ng/mL" ya dicho.
	public IReadOnlyList<PuntoDeSerie> Puntos(string analito)
	{
		Exigir();
		return api!.Puntos(analito);
	}

	public IReadOnlyList<LineaDeReferencia> Referencias(string analito)
	{
		Exigir();
		return api!.Referencias(analito, textos.Idioma);
	}

	// EL CUADRO HISTORICO, plano. La pantalla lo rearma; el dominio decidio que va donde.
	public IReadOnlyList<CeldaEnCuadro> Cuadro() { Exigir(); return api!.Cuadro(textos.Idioma); }

	// LO QUE EL ANALITO CONCLUYE. La pantalla ya no arma el consejo: lo pide.
	public InterpretacionDeAnalito Interpretacion(string analito)
	{
		Exigir();
		return api!.Interpretacion(analito, textos.Idioma);
	}

	// LA consulta que justifica la app. No es el ultimo valor: es la lectura de la serie.
	public TendenciaDeAnalito Tendencia(string analito) { Exigir(); return api!.Tendencia(analito, textos.Idioma); }

	// A QUE RITMO viene cambiando. Es otra pregunta que la tendencia y mira otro tramo: la
	// tendencia mira la serie entera, la velocidad mira el mas reciente. Estaba tipada en
	// la API y no la alcanzaba ninguna caratula del browser, asi que el dominio contestaba
	// y nadie escuchaba.
	//
	// La consulta es TOTAL: se le puede preguntar a cualquier analito y contesta "no
	// aplica" donde no corresponde -hoy la velocidad solo existe en el antigeno-, asi que
	// la pantalla no tiene que saber cual la tiene.
	public VelocidadDeAnalito Velocidad(string analito) { Exigir(); return api!.Velocidad(analito, textos.Idioma); }

	// La pregunta con la que se abre la app. Ver ExamenesAPI.LoMasUrgente.
	public LoMasUrgente Urgente() { Exigir(); return api!.Urgente(textos.Idioma); }

	// ---------------------------------------------------------------- el sexo

	// LO QUE LA PERSONA DICE DE SI MISMA. Es un COMANDO, no una preferencia de la
	// pantalla: queda en su journal con procedencia TU, sobrevive al cierre, y lo lee su
	// IA igual que todo lo demas. Guardar esto en un campo del componente —o en el
	// localStorage— haria que la app mostrara una cosa y el servidor MCP otra.
	//
	// Baja a disco como cualquier otro comando: sin eso, el veredicto que se acaba de
	// desbloquear en pantalla no estaria respaldado.
	public async ValueTask DeclararSexo(string sexo)
	{
		Exigir();

		api!.DeclararSexo(sexo);
		if (puente.HayCarpeta) await BajarADisco();

		UltimoMensaje = $"sexo declarado: {sexo}";
	}

	public string SexoDelPaciente() { Exigir(); return api!.SexoDelPaciente(); }

	// EL DATO QUE FALTA, ya contado y ya dicho por el dominio. La pantalla pone los dos
	// botones; el texto, el conteo y la severidad vienen de aca.
	public FaltaElSexoDelPaciente PedidoDeSexo() { Exigir(); return api!.PedidoDeSexo(textos.Idioma); }

	// LOS BINARIOS SE LEEN POR OTRA PUERTA. Un nitrito no tiene tendencia ni puntos con
	// numero: tiene presencias en fechas. Son dos consultas y no una porque no devuelven
	// lo mismo, igual que en el dominio.
	public PresenciaDeAnalito Presencia(string analito) { Exigir(); return api!.Presencia(analito, textos.Idioma); }

	public IReadOnlyList<PruebaEnSerie> Pruebas(string analito) { Exigir(); return api!.Pruebas(analito, textos.Idioma); }

	// Y LOS NOMINALES POR UNA TERCERA. Un color no tiene tendencia ni un si o un no: tiene
	// una categoria por fecha, y lo que se puede leer de la serie es si volvio a lo
	// esperable.
	public ConstanciaDeAnalito Constancia(string analito) { Exigir(); return api!.Constancia(analito, textos.Idioma); }

	public IReadOnlyList<ObservacionEnSerie> Observaciones(string analito) { Exigir(); return api!.Observaciones(analito, textos.Idioma); }

	public int Cantidad() { Exigir(); return api!.CantidadDeExamenes(); }

	public int AnalitosFueraDeRango() { Exigir(); return api!.AnalitosFueraDeRango(); }

	public LoQueEstaBien LoQueEstaEnRango() { Exigir(); return api!.LoQueEstaEnRango(textos.Idioma); }

	public int EdadDelPaciente() { Exigir(); return api!.EdadDelPaciente(); }

	public long UltimoEvento() { Exigir(); return api!.UltimoEventoProcesado(); }

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
	// El juego de URLs de este negocio. Otro negocio es otro recurso sobre el mismo
	// mecanismo: cambiar estos tres valores y su dominio, nada mas.
	public string BaseUrl { get; set; } = "eventos/";
	public string Recurso { get; set; } = "expediente";
	public string Id { get; set; } = "1";
}
