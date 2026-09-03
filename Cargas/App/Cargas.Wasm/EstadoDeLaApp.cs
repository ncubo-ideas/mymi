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
	private readonly HttpClient http;
	private readonly ConfiguracionDelProveedor proveedor;

	private CargasApp? app;
	private CargasAPI? api;
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

		int precargados = puente.HayCarpeta ? await puente.PrecargarEnMemfs() : 0;

		var arrancada = new CargasApp(NombreDelActor);
		arrancada.ConfigureStorage(DatabaseType.FileSystem, "path=" + PuenteDeJournal.RaizEnMemfs);
		arrancada.Start();

		app = arrancada;
		api = new CargasAPI(arrancada);
		bucle = new BucleDeRefresh(
			api,
			new FuenteHttpDeEventos(http, proveedor.Recurso, proveedor.Id),
			RutaDeCargas.Rutear,
			BajarADisco,
			Renovar);

		UltimoMensaje = puente.HayCarpeta
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
			await Credencial(CancellationToken.None);
			ResultadoDeRefresh alAbrir = await bucle.Ejecutar(CancellationToken.None);
			UltimoMensaje += $" | al abrir: {alAbrir}";
			ProblemaAlPonerseAlDia = "";

			// El cuadro vuelve a entrar animado SOLO si llego algo: es la respuesta a "fui
			// a buscar y volvi con novedades". Sin novedades, la primera pintada ya animo y
			// repetirla dos segundos despues es un parpadeo sin motivo.
			if (alAbrir.Aplicados > 0) Refrescos++;
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
	public string Recurso { get; set; } = "cargas";
	public string Id { get; set; } = "1";
}
