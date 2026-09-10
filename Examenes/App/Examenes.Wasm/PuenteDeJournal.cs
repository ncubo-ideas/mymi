using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace Examenes.Wasm;

// El puente entre el journal en MEMFS y una carpeta real del usuario.
//
// Es la pieza que hace que la carpeta del journal sea la superficie compartida: el
// browser escribe ahi, y el proceso MCP lee de ahi. No hay protocolo inventado entre
// los dos - el journal ya es el protocolo.
//
// Reparto con journal-interop.js: JS habla con el browser (picker, IndexedDB, la
// carpeta) y NO toca MEMFS; C# lee y escribe MEMFS con System.IO, la misma superficie
// que usa el motor. Los bytes cruzan en base64. Asi el puente no depende de APIs
// internas del runtime .NET.
//
// El mismatch de fondo: el storage del motor es SINCRONO, las APIs de archivos del
// browser son ASINCRONAS, y en wasm single-thread no se puede bloquear esperando una
// promesa. De ahi la forma: PRECARGAR sincrono antes de tocar el motor, PERSISTIR
// asincrono despues de cada comando.
public sealed class PuenteDeJournal : IDestinoDelJournal, IAsyncDisposable
{
	// La raiz en MEMFS. Es lo que se le pasa al motor como path=, y no tiene nada que
	// ver con donde el usuario puso la carpeta.
	public const string RaizEnMemfs = "/journal";

	private readonly IJSRuntime js;
	private readonly IDestinoDelJournal destino;
	private IJSObjectReference? modulo;

	// MODO SUELTO: el destino es el storage del browser, y de eso se ocupa esta misma
	// clase (ver la region "el destino" al final). Es el constructor de siempre.
	public PuenteDeJournal(IJSRuntime js)
	{
		ArgumentNullException.ThrowIfNull(js);
		this.js = js;
		destino = this;
	}

	// MODO HOSPEDADO: el destino son los archivos de Gnothi, por el puente nativo. El
	// movimiento MEMFS <-> destino es EL MISMO; lo unico que cambia es donde aterrizan los
	// bytes. La plomeria de browser que queda abajo -picker, permisos, OPFS- no se usa en
	// este modo: Gnothi no le pregunta a nadie donde escribir.
	public PuenteDeJournal(IJSRuntime js, IDestinoDelJournal destino)
	{
		ArgumentNullException.ThrowIfNull(js);
		ArgumentNullException.ThrowIfNull(destino);
		this.js = js;
		this.destino = destino;
	}

	// TENER la carpeta y TENER PERMISO sobre ella son dos cosas distintas, y confundirlas
	// fue un bug: al reabrir la app, el handle vuelve de IndexedDB pero el browser degrada
	// el permiso a 'prompt', asi que hay carpeta y no hay permiso. Guardar un solo dato
	// obligaba a borrar el nombre justo cuando hacia falta para decir "volve a dar permiso
	// a tal carpeta", y el boton salia con el nombre vacio.
	public string NombreDeLaCarpeta { get; private set; } = "";

	public bool PermisoOtorgado { get; private set; }

	public bool HayCarpeta => NombreDeLaCarpeta.Length > 0 && PermisoOtorgado;

	// SI HAY DONDE ESCRIBIR, sea el storage del browser o los archivos de Gnothi. Es lo que
	// mira el arranque para decidir si precarga: "carpeta" es vocabulario del modo suelto y
	// hospedado no aplica.
	public bool HayDestino => destino.Disponible;

	// Hay handle recuperado pero el permiso caduco: se recupera con un click, sin volver
	// a elegir la carpeta.
	public bool FaltaReotorgarPermiso => NombreDeLaCarpeta.Length > 0 && !PermisoOtorgado;

	private async ValueTask<IJSObjectReference> Modulo() =>
		modulo ??= await js.InvokeAsync<IJSObjectReference>("import", "./journal-interop.js");

	// ---------------------------------------------------------------- permiso

	public async ValueTask<bool> EstaSoportado() =>
		await (await Modulo()).InvokeAsync<bool>("estaSoportado");

	// EXIGE GESTO DE USUARIO: llamalo desde un click, nunca al inicializar.
	public async ValueTask<ResultadoDeCarpeta> ElegirCarpeta() => await Pedir("elegirCarpeta");

	// Tras recargar, el handle vuelve de IndexedDB pero el permiso hay que revalidarlo.
	// Si devuelve permiso 'prompt', hace falta PedirPermiso() desde un click.
	public async ValueTask<ResultadoDeCarpeta> RecuperarCarpeta() => await Pedir("recuperarCarpeta");

	// EXIGE GESTO DE USUARIO.
	public async ValueTask<ResultadoDeCarpeta> PedirPermiso() => await Pedir("pedirPermiso");

	public async ValueTask OlvidarCarpeta()
	{
		await (await Modulo()).InvokeAsync<ResultadoDeEscritura>("olvidarCarpeta");
		NombreDeLaCarpeta = "";
		PermisoOtorgado = false;
	}

	// El nombre se conserva siempre que el browser lo haya devuelto, tenga o no permiso.
	// Solo se limpia cuando de verdad no hay handle.
	private async ValueTask<ResultadoDeCarpeta> Pedir(string funcion)
	{
		ResultadoDeCarpeta resultado = await (await Modulo()).InvokeAsync<ResultadoDeCarpeta>(funcion);

		// Si el browser no devolvio nombre, no hubo handle en juego: tipicamente la
		// persona cerro el dialogo del picker. Eso NO debe borrar la carpeta que ya
		// estaba recuperada, o cancelar por error obligaria a volver a elegirla.
		if (!string.IsNullOrEmpty(resultado.Nombre))
		{
			NombreDeLaCarpeta = resultado.Nombre;
			PermisoOtorgado = resultado.Ok;
		}
		return resultado;
	}

	// ---------------------------------------------------------------- los tres modos

	// AGREGADO SOBRE LA PIEZA ORIGINAL, y a proposito solo por agregado: el orden
	// precarga-MEMFS -> actor -> flush-por-comando NO se toca. Lo unico que cambia es a
	// que raiz apunta el destino, y eso pasa entero del lado de JavaScript.
	//
	// Funciona barato porque OPFS devuelve un FileSystemDirectoryHandle, el MISMO tipo
	// que el picker: listar/leer/escribir no distinguen uno del otro.
	//
	// Por que hacen falta (briefing §6.2): "carpeta real elegida por el usuario" es hoy
	// desktop Chromium. En Safari, Firefox y movil no hay picker, y la app NO puede
	// quedarse sin persistir - degrada a OPFS y ofrece exportar.

	public async ValueTask<bool> OpfsSoportado() =>
		await (await Modulo()).InvokeAsync<bool>("opfsSoportado");

	// '' si hay algun almacenamiento; 'navegador' o 'inseguro' si no hay ninguno. Son dos
	// causas que se arreglan distinto y la deteccion cruda las confunde en un solo false.
	public async ValueTask<string> MotivoSinAlmacenamiento() =>
		await (await Modulo()).InvokeAsync<string>("motivoSinAlmacenamiento");

	// Si corre como app instalada y no como pestana. No cambia nada del mecanismo: cambia
	// como se le habla a la persona.
	public async ValueTask<bool> EstaInstalada() =>
		await (await Modulo()).InvokeAsync<bool>("estaInstalada");

	// Entra por el MISMO Pedir que la carpeta, asi que deja el estado del puente
	// coherente -NombreDeLaCarpeta y PermisoOtorgado- sin una segunda ruta que mantener.
	public async ValueTask<ResultadoDeCarpeta> UsarOpfs() => await Pedir("usarOpfs");

	// 'carpeta', 'opfs' o 'ninguno'.
	public async ValueTask<string> ModoActual() =>
		await (await Modulo()).InvokeAsync<string>("modoActual");

	// Que modo eligio la persona la vez pasada. Se consulta al arrancar: si ya opto por
	// OPFS, se reengancha sola y sin pedirle un gesto.
	public async ValueTask<string> ModoRecordado() =>
		await (await Modulo()).InvokeAsync<string>("modoRecordado");

	// El tercer modo: un archivo que la persona se lleva. No depende de ninguna API de
	// almacenamiento, asi que funciona en cualquier navegador.
	public async ValueTask<ResultadoDeExportacion> Exportar() =>
		await (await Modulo()).InvokeAsync<ResultadoDeExportacion>("exportarJournal");

	// Abre el selector y devuelve el contenido, SIN escribir. Quien decide que hacer con
	// el es la caratula: importar exige un destino activo y rearrancar el actor.
	public async ValueTask<ResultadoDeImportacion> AbrirExportado() =>
		await (await Modulo()).InvokeAsync<ResultadoDeImportacion>("abrirJournalExportado");

	// Escribe en MEMFS lo que vino del archivo. Se hace ACA y no en JS por la misma razon
	// que la precarga: MEMFS lo maneja C# con System.IO, que es la superficie que usa el
	// motor. Despues hay que rearrancar el actor para que lo rehidrate.
	public int RestaurarEnMemfs(ArchivoExportado[] archivos)
	{
		ArgumentNullException.ThrowIfNull(archivos);

		if (Directory.Exists(RaizEnMemfs)) Directory.Delete(RaizEnMemfs, recursive: true);
		Directory.CreateDirectory(RaizEnMemfs);

		int escritos = 0;
		foreach (ArchivoExportado archivo in archivos)
		{
			string destino = Path.Combine(RaizEnMemfs, archivo.Ruta.Replace('/', Path.DirectorySeparatorChar));
			Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
			File.WriteAllBytes(destino, Convert.FromBase64String(archivo.Contenido));
			escritos++;
		}
		return escritos;
	}

	// ---------------------------------------------------------------- precarga

	// ARRANQUE: carpeta -> MEMFS. Corre ANTES de que exista el actor, que es lo que
	// permite que el replay del motor lea todo sincrono.
	public async ValueTask<int> PrecargarEnMemfs()
	{
		ResultadoDeListado listado = await destino.Listar();

		// Handle colgando: la carpeta se movio, se renombro o se borro. El permiso puede
		// seguir figurando como otorgado, asi que el unico momento en que se descubre es
		// al intentar leerla. Se olvida el handle para que la app vuelva a ofrecer elegir
		// carpeta en vez de reintentar contra algo que ya no esta.
		//
		// Solo en el modo suelto: hospedado no hay handle que olvidar ni carpeta que
		// volver a elegir, y un fallo al listar es un error de verdad de Gnothi.
		if (!listado.Ok)
		{
			if (ReferenceEquals(destino, this)) await OlvidarCarpeta();
			throw new CarpetaNoDisponibleException(
				$"No se pudo leer '{destino.Nombre}': {listado.Motivo}. Elegí una carpeta de nuevo.");
		}

		int copiados = 0;
		foreach (string ruta in listado.Rutas)
		{
			ResultadoDeLectura leido = await destino.Leer(ruta);

			// Un archivo del journal que no se puede leer NO se puede saltar: la
			// rehidratacion quedaria incompleta y el total saldria mal, sin aviso.
			if (!leido.Ok)
				throw new CarpetaNoDisponibleException(
					$"No se pudo leer '{ruta}' de '{destino.Nombre}': {leido.Motivo}. "
					+ "Abrir con el registro a medias daria totales equivocados.");

			string destinoEnMemfs = Path.Combine(RaizEnMemfs, ruta.Replace('/', Path.DirectorySeparatorChar));
			string? directorio = Path.GetDirectoryName(destinoEnMemfs);
			if (!string.IsNullOrEmpty(directorio)) Directory.CreateDirectory(directorio);

			File.WriteAllBytes(destinoEnMemfs, Convert.FromBase64String(leido.Contenido));
			copiados++;
		}
		return copiados;
	}

	// ---------------------------------------------------------------- persistencia

	// OPERACION: MEMFS -> carpeta. Se llama DESPUES DE CADA COMANDO, no por timer:
	// angosta al maximo la ventana en la que un cierre abrupto pierde entradas ya
	// commiteadas en el actor pero todavia no bajadas al disco.
	//
	// Escribe el arbol completo en vez de detectar cambios. Medido: semilla + una
	// examenes son unos pocos KB en 2 archivos, asi que el ahorro de un diff no paga el
	// riesgo de un diff mal hecho. Si el journal creciera a megabytes, ACA es donde
	// hay que comparar tamano y fecha antes de escribir.
	public async ValueTask<int> BajarDeMemfs()
	{
		if (!destino.Disponible || !Directory.Exists(RaizEnMemfs)) return 0;

		int escritos = 0;
		var fallidos = new List<string>();

		foreach (string archivo in Directory.EnumerateFiles(RaizEnMemfs, "*", SearchOption.AllDirectories))
		{
			string relativa = Path.GetRelativePath(RaizEnMemfs, archivo).Replace(Path.DirectorySeparatorChar, '/');
			string base64 = Convert.ToBase64String(File.ReadAllBytes(archivo));

			ResultadoDeEscritura resultado = await destino.Escribir(relativa, base64);
			if (resultado.Ok) escritos++;
			else fallidos.Add($"{relativa} ({resultado.Motivo})");
		}

		// Un flush que falla NO puede pasar en silencio. Antes se contaban los exitos y
		// se descartaban los fallos: la app seguia mostrando lo que tenia en memoria, el
		// journal del destino se quedaba atras, y quien lo leyera despues - el servidor
		// MCP, Gnothi, o la proxima apertura de la app - veia un estado viejo sin que nada
		// avisara. Eso no es la ventana de perdida documentada, que es de milisegundos:
		// es una divergencia permanente y muda. En el modo suelto la causa tipica es que
		// el permiso de escritura sobre la carpeta caduco, y eso se arregla volviendolo a
		// otorgar.
		if (fallidos.Count > 0)
			throw new EscrituraRechazadaException(
				$"No se pudo guardar en '{destino.Nombre}': fallaron {fallidos.Count} de "
				+ $"{fallidos.Count + escritos} archivos. {string.Join("; ", fallidos)}");

		return escritos;
	}

	// El permiso vigente sobre la carpeta, sin pedir nada. Sirve para avisar ANTES de
	// aplicar comandos que no se van a poder guardar.

	// ---------------------------------------------------------------- el destino, modo suelto

	// EL STORAGE DEL BROWSER como destino del journal. Esta clase ya tenia las tres
	// operaciones y el estado del permiso, asi que las expone por la costura en vez de
	// mudarlas: el modo suelto no cambia una linea de comportamiento.
	//
	// Implementacion EXPLICITA a proposito: son para el mecanismo, no para la caratula. La
	// pantalla sigue hablando de carpeta y permiso, que es lo que la persona entiende.
	string IDestinoDelJournal.Nombre => NombreDeLaCarpeta;

	bool IDestinoDelJournal.Disponible => HayCarpeta;

	async ValueTask<ResultadoDeListado> IDestinoDelJournal.Listar() =>
		await (await Modulo()).InvokeAsync<ResultadoDeListado>("listar");

	async ValueTask<ResultadoDeLectura> IDestinoDelJournal.Leer(string ruta) =>
		await (await Modulo()).InvokeAsync<ResultadoDeLectura>("leer", ruta);

	async ValueTask<ResultadoDeEscritura> IDestinoDelJournal.Escribir(string ruta, string contenidoBase64) =>
		await (await Modulo()).InvokeAsync<ResultadoDeEscritura>("escribir", ruta, contenidoBase64);

	public async ValueTask<string> PermisoActual() =>
		await (await Modulo()).InvokeAsync<string>("permisoActual");

	// Lo que hay en MEMFS ahora, para reportarlo en la UI sin adivinar.
	public static (int archivos, long bytes) EstadoDeMemfs()
	{
		if (!Directory.Exists(RaizEnMemfs)) return (0, 0);

		int archivos = 0;
		long bytes = 0;
		foreach (string archivo in Directory.EnumerateFiles(RaizEnMemfs, "*", SearchOption.AllDirectories))
		{
			archivos++;
			bytes += new FileInfo(archivo).Length;
		}
		return (archivos, bytes);
	}

	public async ValueTask DisposeAsync()
	{
		if (modulo is not null)
		{
			try { await modulo.DisposeAsync(); } catch (JSDisconnectedException) { }
			modulo = null;
		}
	}
}

public sealed class ResultadoDeCarpeta
{
	[JsonPropertyName("ok")]      public bool Ok { get; set; }
	[JsonPropertyName("nombre")]  public string Nombre { get; set; } = "";
	[JsonPropertyName("permiso")] public string Permiso { get; set; } = "";
	[JsonPropertyName("motivo")]  public string Motivo { get; set; } = "";

	public override string ToString() =>
		Ok ? $"ok, carpeta '{Nombre}' (permiso {Permiso})"
		   : $"no: {(Motivo.Length > 0 ? Motivo : Permiso)}";
}

public sealed class ResultadoDeEscritura
{
	[JsonPropertyName("ok")]     public bool Ok { get; set; }
	[JsonPropertyName("motivo")] public string Motivo { get; set; } = "";
}

// La carpeta rechazo la escritura. Tipo propio porque la app puede reaccionar: casi
// siempre es el permiso caducado, y se recupera volviendolo a otorgar con un click.
public sealed class EscrituraRechazadaException : Exception
{
	public EscrituraRechazadaException(string mensaje) : base(mensaje) { }
}

// La carpeta guardada ya no se puede leer: se movio, se renombro o se borro. Se resuelve
// eligiendo otra, no reintentando.
public sealed class CarpetaNoDisponibleException : Exception
{
	public CarpetaNoDisponibleException(string mensaje) : base(mensaje) { }
}

public sealed class ResultadoDeListado
{
	[JsonPropertyName("ok")]     public bool Ok { get; set; }
	[JsonPropertyName("rutas")]  public string[] Rutas { get; set; } = Array.Empty<string>();
	[JsonPropertyName("motivo")] public string Motivo { get; set; } = "";
}

public sealed class ResultadoDeLectura
{
	[JsonPropertyName("ok")]        public bool Ok { get; set; }
	[JsonPropertyName("contenido")] public string Contenido { get; set; } = "";
	[JsonPropertyName("motivo")]    public string Motivo { get; set; } = "";
}

public sealed class ResultadoDeExportacion
{
	public bool Ok { get; set; }
	public string Motivo { get; set; } = "";
	public int Archivos { get; set; }
}

public sealed class ResultadoDeImportacion
{
	public bool Ok { get; set; }
	public string Motivo { get; set; } = "";
	public ArchivoExportado[] Archivos { get; set; } = Array.Empty<ArchivoExportado>();
}

public sealed class ArchivoExportado
{
	public string Ruta { get; set; } = "";
	public string Contenido { get; set; } = "";
}
