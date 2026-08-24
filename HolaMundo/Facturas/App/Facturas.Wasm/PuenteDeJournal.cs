using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace Facturas.Wasm;

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
public sealed class PuenteDeJournal : IAsyncDisposable
{
	// La raiz en MEMFS. Es lo que se le pasa al motor como path=, y no tiene nada que
	// ver con donde el usuario puso la carpeta.
	public const string RaizEnMemfs = "/journal";

	private readonly IJSRuntime js;
	private IJSObjectReference? modulo;

	public PuenteDeJournal(IJSRuntime js)
	{
		ArgumentNullException.ThrowIfNull(js);
		this.js = js;
	}

	// TENER la carpeta y TENER PERMISO sobre ella son dos cosas distintas, y confundirlas
	// fue un bug: al reabrir la app, el handle vuelve de IndexedDB pero el browser degrada
	// el permiso a 'prompt', asi que hay carpeta y no hay permiso. Guardar un solo dato
	// obligaba a borrar el nombre justo cuando hacia falta para decir "volve a dar permiso
	// a tal carpeta", y el boton salia con el nombre vacio.
	public string NombreDeLaCarpeta { get; private set; } = "";

	public bool PermisoOtorgado { get; private set; }

	public bool HayCarpeta => NombreDeLaCarpeta.Length > 0 && PermisoOtorgado;

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

	// ---------------------------------------------------------------- precarga

	// ARRANQUE: carpeta -> MEMFS. Corre ANTES de que exista el actor, que es lo que
	// permite que el replay del motor lea todo sincrono.
	public async ValueTask<int> PrecargarEnMemfs()
	{
		IJSObjectReference modulo = await Modulo();
		ResultadoDeListado listado = await modulo.InvokeAsync<ResultadoDeListado>("listar");

		// Handle colgando: la carpeta se movio, se renombro o se borro. El permiso puede
		// seguir figurando como otorgado, asi que el unico momento en que se descubre es
		// al intentar leerla. Se olvida el handle para que la app vuelva a ofrecer elegir
		// carpeta en vez de reintentar contra algo que ya no esta.
		if (!listado.Ok)
		{
			await OlvidarCarpeta();
			throw new CarpetaNoDisponibleException(
				$"No se pudo leer la carpeta guardada: {listado.Motivo}. Elegí una carpeta de nuevo.");
		}

		int copiados = 0;
		foreach (string ruta in listado.Rutas)
		{
			ResultadoDeLectura leido = await modulo.InvokeAsync<ResultadoDeLectura>("leer", ruta);

			// Un archivo del journal que no se puede leer NO se puede saltar: la
			// rehidratacion quedaria incompleta y el total saldria mal, sin aviso.
			if (!leido.Ok)
				throw new CarpetaNoDisponibleException(
					$"No se pudo leer '{ruta}' de la carpeta '{NombreDeLaCarpeta}': {leido.Motivo}. "
					+ "Abrir con el registro a medias daria totales equivocados.");

			string destino = Path.Combine(RaizEnMemfs, ruta.Replace('/', Path.DirectorySeparatorChar));
			string? directorio = Path.GetDirectoryName(destino);
			if (!string.IsNullOrEmpty(directorio)) Directory.CreateDirectory(directorio);

			File.WriteAllBytes(destino, Convert.FromBase64String(leido.Contenido));
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
	// factura son 651 bytes en 2 archivos, asi que el ahorro de un diff no paga el
	// riesgo de un diff mal hecho. Si el journal creciera a megabytes, ACA es donde
	// hay que comparar tamano y fecha antes de escribir.
	public async ValueTask<int> BajarDeMemfs()
	{
		if (!HayCarpeta || !Directory.Exists(RaizEnMemfs)) return 0;

		IJSObjectReference modulo = await Modulo();
		int escritos = 0;
		var fallidos = new List<string>();

		foreach (string archivo in Directory.EnumerateFiles(RaizEnMemfs, "*", SearchOption.AllDirectories))
		{
			string relativa = Path.GetRelativePath(RaizEnMemfs, archivo).Replace(Path.DirectorySeparatorChar, '/');
			string base64 = Convert.ToBase64String(File.ReadAllBytes(archivo));

			ResultadoDeEscritura resultado = await modulo.InvokeAsync<ResultadoDeEscritura>("escribir", relativa, base64);
			if (resultado.Ok) escritos++;
			else fallidos.Add($"{relativa} ({resultado.Motivo})");
		}

		// Un flush que falla NO puede pasar en silencio. Antes se contaban los exitos y
		// se descartaban los fallos: la app seguia mostrando lo que tenia en memoria, el
		// journal de la carpeta se quedaba atras, y quien lo leyera despues - el servidor
		// MCP, o la proxima apertura de la app - veia un estado viejo sin que nada
		// avisara. Eso no es la ventana de perdida documentada, que es de milisegundos:
		// es una divergencia permanente y muda. La causa tipica es que el permiso de
		// escritura sobre la carpeta caduco, y eso se arregla volviendolo a otorgar.
		if (fallidos.Count > 0)
			throw new EscrituraRechazadaException(
				$"No se pudo guardar en la carpeta '{NombreDeLaCarpeta}': fallaron {fallidos.Count} de "
				+ $"{fallidos.Count + escritos} archivos. {string.Join("; ", fallidos)}");

		return escritos;
	}

	// El permiso vigente sobre la carpeta, sin pedir nada. Sirve para avisar ANTES de
	// aplicar comandos que no se van a poder guardar.
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
