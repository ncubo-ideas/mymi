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
	private readonly ModoDeLaApp modo;
	private readonly PuenteDeGnothi puenteDeGnothi;
	private readonly AlmacenDeCredencial almacen;
	private readonly HttpClient http;
	private readonly ConfiguracionDelProveedor proveedor;
	private readonly Textos textos;

	private ComprasApp? app;
	private ComprasAPI? api;
	private BucleDeRefresh? bucle;

	public EstadoDeLaApp(PuenteDeJournal puente, ModoDeLaApp modo, PuenteDeGnothi puenteDeGnothi,
		AlmacenDeCredencial almacen, HttpClient http, ConfiguracionDelProveedor proveedor, Textos textos)
	{
		ArgumentNullException.ThrowIfNull(puente);
		ArgumentNullException.ThrowIfNull(modo);
		ArgumentNullException.ThrowIfNull(puenteDeGnothi);
		ArgumentNullException.ThrowIfNull(almacen);
		ArgumentNullException.ThrowIfNull(http);
		ArgumentNullException.ThrowIfNull(proveedor);
		ArgumentNullException.ThrowIfNull(textos);

		this.puente = puente;
		this.modo = modo;
		this.puenteDeGnothi = puenteDeGnothi;
		this.almacen = almacen;
		this.http = http;
		this.proveedor = proveedor;
		this.textos = textos;
	}

	/// <summary>
	/// EL IDIOMA CON QUE ESTA CARATULA LE PIDE SUS FRASES AL DOMINIO: el de la persona, negociado al
	/// arrancar. No es el del registro: la caratula la lee ella, no el supermercado.
	/// </summary>
	private string ElIdioma => textos.Idioma;

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

		int precargados = puente.HayDestino ? await puente.PrecargarEnMemfs() : 0;

		var arrancada = new ComprasApp(NombreDelActor);
		arrancada.ConfigureStorage(DatabaseType.FileSystem, "path=" + PuenteDeJournal.RaizEnMemfs);
		arrancada.Start();

		app = arrancada;
		api = new ComprasAPI(arrancada);
		// LA FUENTE DEPENDE DEL HOSPEDAJE, y es lo unico que depende junto con el destino
		// (11 §2). Suelta hace su propio pull con su credencial; hospedada le pide a Gnothi,
		// que releva con la suya. El cursor lo pone el actor en los dos casos.
		IFuenteDeEventos fuente = modo.Hospedada
			? new FuenteDeEventosPorGnothi((desde, _) => modo.Puente!.Pull(desde).AsTask())
			: new FuenteHttpDeEventos(http, proveedor.Recurso, proveedor.Id, Credencial, Renovar);

		bucle = new BucleDeRefresh(api, fuente, RutaDeCompras.Rutear, BajarADisco);

		UltimoMensaje = modo.Hospedada
			? $"arriba, hospedada en Mymi. {precargados} archivos precargados"
			: puente.HayDestino
				? $"arriba. {precargados} archivos precargados de la carpeta '{puente.NombreDeLaCarpeta}'"
				: "arriba, pero SIN carpeta: el journal vive solo en memoria y se pierde al cerrar";

		// La app se pone al dia sola: consigue su credencial si no la tiene y lee lo
		// nuevo. Si el proveedor no esta disponible, abrir NO falla: se muestra lo que ya
		// hay en el journal, que es justamente de lo que sirve tener el dato propio.
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

	public string DondeVive => Cuenta?.Host is { Length: > 0 } donde ? donde : "Mymi";

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
			ConsultasDelModulo.ListaDeCompras => ConsultasDelModulo.HacerListaDeCompras(api!),
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
			RutaDeCompras.Rutear,
			BajarADisco);

		ResultadoDeRefresh resultado = await unaVez.Ejecutar(ct);
		UltimoMensaje = $"entrega de Mymi: {resultado}";
		return resultado;
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

	// Donde quedo apuntando el cliente. Se dice en los errores: "no se pudo hablar con el
	// supermercado" sin decir CON CUAL no se puede accionar.
	public string DondeEstaElProveedor => http.BaseAddress?.ToString() ?? "(sin configurar)";

	public async ValueTask<ResultadoDeRefresh> Refrescar(CancellationToken ct = default)
	{
		Exigir();

		// LA CREDENCIAL LA CONSIGUE EL BUCLE, POR EL DELEGADO, y esta linea no vuelve.
		//
		// Aca habia un `await Credencial(ct)` sin condicion. Estaba puesto por una razon
		// buena -medida en un iPhone: si al abrir no se podia hablar con el proveedor no
		// quedaba token, y cada Refresh moria para siempre diciendo "la app no tiene web
		// token guardado", porque el unico camino que lo conseguia era el arranque, que ya
		// habia pasado-. Pero el arreglo estaba en el lugar equivocado.
		//
		// POR QUE ROMPIA HOSPEDADA. Pedir la credencial es HTTP contra el publicador, y
		// `proveedor.BaseUrl` es RELATIVA -"eventos/"-, asi que hospedada la peticion sale
		// a `<su origen>/app/eventos/device/token`: el propio origen del modulo, servido
		// por Gnothi, donde ese archivo no existe. 404. Y hospedada el modulo NO TIENE NI
		// NECESITA credencial: el cartero del Host tiene la suya.
		//
		// Medido en un iPhone por Renan el 2026-09-08: con el journal lleno, tocar
		// "Buscar compras nuevas" devolvia el 404 sin que hubiera un solo pull por el
		// puente. La URL decia `eventos/` y por eso parecia la llamada de eventos; era
		// esta.
		//
		// DONDE VA. `FuenteHttpDeEventos.LeerDesde` ya pide la credencial POR PULL, por el
		// delegado que se le pasa al construirla, y su propio comentario dice por que:
		// "quien lee por el puente de Gnothi no [la necesita]. Poner eso en el bucle lo
		// obligaba, hospedada, a inventarse un token que nadie usa". Asi que el caso suelto
		// que motivo esta linea ya estaba cubierto, y hospedada no se pide nada.
		//
		// Es lo que Examenes ya hacia. Los cuatro modulos quedan iguales.
		ResultadoDeRefresh resultado = await bucle!.Ejecutar(ct);
		UltimoMensaje = $"refresh: {resultado}";
		return resultado;
	}

	// ---------------------------------------------------------------- consultas

	public IReadOnlyList<CompraEnLista> Compras() { Exigir(); return api!.Compras(); }

	public CompraDetallada Detalle(string numero) { Exigir(); return api!.Detalle(numero, ElIdioma); }

	// LA consulta que justifica la app: como cambio la canasta entre dos meses.
	public CanastasComparadas Comparacion(int anioAntes, int mesAntes, int anioDespues, int mesDespues)
	{ Exigir(); return api!.Comparacion(anioAntes, mesAntes, anioDespues, mesDespues, ElIdioma); }

	// La lectura por defecto. QUE DOS MESES se comparan lo elige el dominio, no esta
	// pantalla: elegir es parte de la pregunta, no de como se dibuja.
	public CanastasComparadas ComparacionPorDefecto() { Exigir(); return api!.ComparacionConElAnioAnterior(ElIdioma); }

	public CanastaDelMesVista Canasta(int anio, int mes) { Exigir(); return api!.Canasta(anio, mes, ElIdioma); }

	// LAS DOS LECTURAS DE LA PORTADA. Cuantas compras se miran para reconocer la firma lo
	// decide la app (ComprasAPI), no esta caratula: es parte de la pregunta.
	public FirmaDeLaSemana Firma() { Exigir(); return api!.Firma(ElIdioma); }

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
		return api.Canasta(int.Parse(partes[0]), int.Parse(partes[1]), ElIdioma);
	}

	// EL PRODUCTO SE NOMBRA. El dominio resuelve el texto igual que para la tool del MCP.
	public ConsumoEnElTiempo ConsumoDe(string producto)
	{ Exigir(); return api!.Consumo(producto, ElIdioma); }

	// LO QUE VA A FALTAR EN LA PROXIMA COMPRA. El "hoy" lo pone la caratula porque involucra
	// el RELOJ, y el dominio no lo lee: si lo leyera, esta lectura daria distinto en cada
	// replay del journal y dejaria de ser reproducible.
	public ListaDeLaProximaCompra Lista()
	{ Exigir(); return api!.Lista(DateTime.Now, ElIdioma); }

	// LAS PROMOCIONES VIGENTES. El "hoy" lo pone la caratula: vigente-o-no depende de
	// cuando se mira, y el dominio no lee el reloj.
	public IReadOnlyList<PromocionVista> Promociones()
	{ Exigir(); return api!.Promociones(DateTime.Now, ElIdioma); }

	// EN CUALES COMPRAS APARECIO ESTO. La busca el DOMINIO y no la pantalla: la pantalla
	// solo tiene la lista de compras -numero, fecha, lineas, total-, sin las lineas, asi que
	// filtrar aca obligaria a bajar el detalle de las 87 en cada tecla.
	public ComprasHalladas Halladas(string termino)
	{ Exigir(); return api!.Halladas(termino, ElIdioma); }

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
	{ Exigir(); return api!.CambioDelQuiebre(ElIdioma); }

	// LOS PRODUCTOS A LOS QUE LES CAMBIO EL RITMO, el mas notorio primero. EL MISMO quiebre
	// que la tarjeta de arriba, y ahora lo garantiza el dominio: las dos cuentan el mismo
	// cambio desde angulos distintos -una dice QUE entro, la otra CADA CUANTO-, y antes esa
	// coordinacion era que las dos llamadas de aca pasaran la misma fecha.
	// EL ULTIMO MES CONTRA EL ANTERIOR. Que dos meses se comparan lo elige el DOMINIO, que
	// conoce su propio rango: esta pantalla no sabe cual es el ultimo mes con compras, y la
	// aritmetica de "el mes anterior" se resuelve mal en enero.
	public CanastasComparadas ComparacionConElMesAnterior()
	{ Exigir(); return api!.ComparacionConElMesAnterior(ElIdioma); }

	// SU PROPIA INFLACION, solo de los productos que si lleva.
	public IReadOnlyList<PrecioQueCambio> Precios() { Exigir(); return api!.Precios(ElIdioma); }

	// LA CANASTA FRENTE A HACE UNOS MESES. El "hoy" lo pone la caratula, y la ventana la
	// pone el dominio (3 meses): la pantalla no inventa la suya.
	public CanastaFrenteAlPasadoVista FrenteAlPasado()
	{ Exigir(); return api!.FrenteAlPasado(DateTime.Now, ElIdioma); }

	public RebajaVista Rebaja(string numero) { Exigir(); return api!.Rebaja(numero, ElIdioma); }

	// EN QUE SUCURSALES COMPRA. La portada lo pinta plegado en una fila; el detalle
	// entero es del asistente (donde_compro).
	public RecorridoDeSucursalesVista Recorrido() { Exigir(); return api!.Recorrido(ElIdioma); }

	// CADA CUANTO VA AL SUPER, que dia y en que momento. No lleva parametros: el dominio lo
	// mide sobre todo el historial, y no hay reloj de por medio.
	public RitmoDelSuperVista Ritmo() { Exigir(); return api!.Ritmo(ElIdioma); }

	public IReadOnlyList<RitmoQueCambio> RitmosQueCambiaron()
	{ Exigir(); return api!.RitmosDelQuiebre(ElIdioma); }

	// El "hoy" lo pone la caratula: el dominio no lee el reloj.
	public IReadOnlyList<GarantiaDeProducto> GarantiasVigentes() { Exigir(); return api!.GarantiasVigentes(DateTime.Now, ElIdioma); }

	// LO QUE NO ESTA: lo unico que esta pantalla no podria calcular sola.
	public IReadOnlyList<AbastecimientoEnEspera> AbastecimientosPendientes()
	{ Exigir(); return api!.AbastecimientosPendientes(DateTime.Now, ElIdioma); }

	public int Cantidad() { Exigir(); return api!.CantidadDeCompras(); }

	public decimal Total() { Exigir(); return api!.TotalDeCompras(); }

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
