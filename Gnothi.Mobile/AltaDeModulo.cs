using System.IO.Compression;
using System.Text.Json;

namespace Gnothi.Mobile;

// EL ALTA DE UN MÓDULO DESDE UN ENLACE. Es como una persona consigue un módulo de verdad: la
// institución pone un botón en su página, Sonia lo toca, y Gnothi baja el bundle y lo instala
// (especificación 11 §3, 13 §7). Hasta ahora los módulos solo entraban empujando archivos con
// adb, que no es un camino que exista para nadie.
//
// EL BUNDLE ES UN ZIP y trae dos cosas: el `manifest.json` de la institución y la carpeta
// `web/` con su app publicada. Nada más. No trae ejecutable, no trae instalación: la
// instalación la escribe GNOTHI, porque es su archivo y su autoridad.
//
// LO QUE GNOTHI DECIDE Y LA INSTITUCIÓN NO: dónde se guarda el journal, con qué cáscara corre
// el módulo, y que el endpoint del publicador quede fijo. El manifiesto DECLARA dónde está su
// publicador; Gnothi lo registra una vez y a partir de ahí el módulo no lo puede cambiar.
internal sealed class AltaDeModulo
{
	// Un bundle de un módulo pesa unos 11 MB medidos. Este techo no es una optimización: es
	// para que una URL equivocada no llene el aparato antes de que nadie se entere.
	private const long TechoDelBundle = 64L * 1024 * 1024;

	private static readonly HttpClient http = new() { Timeout = TimeSpan.FromMinutes(2) };

	private readonly string raizDeModulos;
	private readonly string raizDeJournals;

	internal AltaDeModulo(string raizDeModulos, string raizDeJournals)
	{
		this.raizDeModulos = raizDeModulos;
		this.raizDeJournals = raizDeJournals;
	}

	// EL ALTA SON DOS ACTOS, Y LA PERSONA DECIDE EN EL MEDIO (especificacion 13 §7).
	//
	//     Examinar   baja el bundle y lee su manifiesto. NO instala nada.
	//     Aceptar    recien ahi el modulo ocupa un lugar en el aparato.
	//
	// Por que partido: instalar un modulo es el acto mas grande que la persona hace en Gnothi
	// -le da a una institucion una carpeta suya y un endpoint registrado- y hasta el 2026-09-04
	// era un EFECTO SECUNDARIO de tocar un enlace. Para que el asistente vea una consulta hay
	// un interruptor por tool; para que exista el modulo entero no habia nada.
	//
	// Y bajar no es instalar: para poder decirle a la persona QUIEN es la institucion y QUE
	// ofrece, hay que leer el manifiesto, y el manifiesto viene adentro del zip. Asi que se
	// baja, se desempaqueta en un temporal, se muestra, y se instala solo si ella acepta. Si
	// dice que no, el temporal se borra y no quedo nada.
	internal async Task<PropuestaDeAlta> Examinar(string url, CancellationToken ct, Action<string>? avisar = null)
	{
		if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? donde))
			throw new ArgumentException($"el enlace del módulo no es una dirección válida: {url}");

		Android.Util.Log.Info("gnothi", $"alta: bajando el bundle de {donde.Host}");
		avisar?.Invoke($"Bajando el módulo de {donde.Host}…");

		byte[] zip = await Bajar(donde, ct);
		string temporal = Path.Combine(Path.GetTempPath(), "alta-" + Guid.NewGuid().ToString("N")[..8]);

		try
		{
			avisar?.Invoke("Revisando qué trae…");
			Desempaquetar(zip, temporal);

			Gnothi.Host.Manifest manifiesto = LeerManifiesto(temporal);
			string modulo = manifiesto.Module;

			// La institución nombra su módulo, y ese nombre es una ruta: no puede escaparse de
			// la carpeta de módulos ni pisar otra cosa.
			if (modulo.Length == 0 || modulo.Contains('/') || modulo.Contains('\\') || modulo.Contains(".."))
				throw new InvalidOperationException($"el manifiesto declara un nombre de módulo inválido: '{modulo}'");

			return new PropuestaDeAlta(temporal, manifiesto, donde.Host,
				Path.Combine(raizDeJournals, modulo), Directory.Exists(Path.Combine(raizDeModulos, modulo)));
		}
		catch
		{
			// Un bundle que no se puede ni leer no deja basura esperando una decision que
			// nunca va a llegar.
			if (Directory.Exists(temporal)) Directory.Delete(temporal, recursive: true);
			throw;
		}
	}

	// LA PERSONA DIJO QUE SI. Recien acá el módulo ocupa un lugar: su carpeta, su journal y su
	// endpoint fijo.
	internal string Aceptar(PropuestaDeAlta propuesta)
	{
		string modulo = propuesta.Manifiesto.Module;
		string destino = Path.Combine(raizDeModulos, modulo);

		if (Directory.Exists(destino)) Directory.Delete(destino, recursive: true);
		Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
		Directory.Move(propuesta.Carpeta, destino);

		EscribirInstalacion(destino, modulo, propuesta.Manifiesto);

		Android.Util.Log.Info("gnothi", $"alta: '{modulo}' instalado en {destino}");
		return modulo;
	}

	// LA PERSONA DIJO QUE NO, y entonces no queda nada: ni la carpeta, ni el journal, ni una
	// entrada en el catálogo. Bajar el bundle no fue instalarlo.
	internal static void Descartar(PropuestaDeAlta propuesta)
	{
		if (Directory.Exists(propuesta.Carpeta)) Directory.Delete(propuesta.Carpeta, recursive: true);
		Android.Util.Log.Info("gnothi", $"alta: '{propuesta.Manifiesto.Module}' descartado por la persona");
	}

	private static async Task<byte[]> Bajar(Uri donde, CancellationToken ct)
	{
		using HttpResponseMessage respuesta = await http.GetAsync(donde, HttpCompletionOption.ResponseHeadersRead, ct);
		respuesta.EnsureSuccessStatusCode();

		if (respuesta.Content.Headers.ContentLength is long largo && largo > TechoDelBundle)
			throw new InvalidOperationException($"el bundle pesa {largo / 1048576} MB y el techo es {TechoDelBundle / 1048576} MB");

		using var buffer = new MemoryStream();
		await using (Stream cuerpo = await respuesta.Content.ReadAsStreamAsync(ct))
		{
			var trozo = new byte[81920];
			int leidos;
			while ((leidos = await cuerpo.ReadAsync(trozo, ct)) > 0)
			{
				buffer.Write(trozo, 0, leidos);

				// El Content-Length puede mentir o no venir; el techo se aplica igual mientras baja.
				if (buffer.Length > TechoDelBundle)
					throw new InvalidOperationException($"el bundle pasó el techo de {TechoDelBundle / 1048576} MB mientras bajaba");
			}
		}

		return buffer.ToArray();
	}

	// ZIP SLIP: una entrada del zip puede llamarse "../../algo" y escribir fuera de donde uno
	// cree. Se resuelve cada ruta y se exige que caiga adentro. Es responsabilidad de Gnothi,
	// no confianza en la institución.
	private static void Desempaquetar(byte[] zip, string destino)
	{
		Directory.CreateDirectory(destino);
		string raiz = Path.GetFullPath(destino) + Path.DirectorySeparatorChar;

		using var flujo = new MemoryStream(zip);
		using var archivo = new ZipArchive(flujo, ZipArchiveMode.Read);

		foreach (ZipArchiveEntry entrada in archivo.Entries)
		{
			// El estandar del ZIP dice barra normal, pero las herramientas de Windows escriben
			// barra invertida y en Android eso NO separa carpetas: sin normalizar, el bundle se
			// desempaqueta como archivos con barras en el nombre. Medido con un zip hecho por
			// Compress-Archive.
			string nombre = entrada.FullName.Replace("\\", "/");
			if (nombre.EndsWith('/') || entrada.Name.Length == 0) continue;

			string completa = Path.GetFullPath(Path.Combine(destino, nombre.Replace('/', Path.DirectorySeparatorChar)));
			if (!completa.StartsWith(raiz, StringComparison.Ordinal))
				throw new InvalidOperationException($"el bundle intenta escribir fuera de su carpeta: '{entrada.FullName}'");

			Directory.CreateDirectory(Path.GetDirectoryName(completa)!);
			entrada.ExtractToFile(completa, overwrite: true);
		}
	}

	private static Gnothi.Host.Manifest LeerManifiesto(string carpeta)
	{
		string ruta = Path.Combine(carpeta, "manifest.json");
		if (!File.Exists(ruta))
			throw new InvalidOperationException("el bundle no trae manifest.json: no dice quién es ni qué ofrece");

		Gnothi.Host.Manifest? manifiesto = JsonSerializer.Deserialize<Gnothi.Host.Manifest>(File.ReadAllText(ruta));
		return manifiesto ?? throw new InvalidOperationException("el manifest.json del bundle no se pudo leer");
	}

	// ESTE ARCHIVO ES DEL HOST, y por eso lo escribe Gnothi y no viene en el bundle. Acá se
	// decide con qué cáscara corre el módulo, dónde guarda su journal y contra qué endpoint
	// habla el cartero. La institución declaró el endpoint; elegirlo fijo es de Gnothi.
	private void EscribirInstalacion(string carpeta, string modulo, Gnothi.Host.Manifest manifiesto)
	{
		Gnothi.Host.Publicador? p = manifiesto.Publicador;
		string endpoint = p is null || p.Eventos.Length == 0 ? "" : $"{p.Eventos}|{p.Recurso}|{p.Id}";

		var instalacion = new
		{
			shell = new
			{
				type = "webview",
				command = "",
				arguments = new[] { Path.Combine(carpeta, "web"), endpoint },
			},
			grantedFolder = Path.Combine(raizDeJournals, modulo),
			enabled = true,
			disabledTools = Array.Empty<string>(),
		};

		File.WriteAllText(Path.Combine(carpeta, "installation.json"),
			JsonSerializer.Serialize(instalacion, new JsonSerializerOptions { WriteIndented = true }));
	}
}

// LO QUE GNOTHI LE MUESTRA A LA PERSONA ANTES DE INSTALAR NADA. Sale del manifiesto del bundle
// -o sea, de lo que la institucion DECLARA de si misma- mas las dos cosas que decide Gnothi y
// ella tiene derecho a saber antes de aceptar: donde va a vivir el journal, y contra que
// endpoint va a hablar el cartero.
//
// `Carpeta` es el temporal donde el bundle ya esta desempaquetado. Existe hasta que ella decide:
// Aceptar lo mueve a su lugar, Descartar lo borra.
internal sealed record PropuestaDeAlta(
	string Carpeta,
	Gnothi.Host.Manifest Manifiesto,
	string Servido,
	string DondeVaElJournal,
	bool YaEstaba)
{
	// El endpoint del publicador, tal como quedaria fijado. Se dice en la pantalla porque es la
	// unica red que ese modulo va a poder tocar -- por mano de Gnothi, no propia.
	internal string Publicador
	{
		get
		{
			Gnothi.Host.Publicador? p = Manifiesto.Publicador;
			return p is null || p.Eventos.Length == 0 ? "" : $"{p.Eventos}{p.Recurso}/{p.Id}";
		}
	}
}
