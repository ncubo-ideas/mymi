using System.Text;
using System.Text.Json;

namespace Gnothi.Desktop;

// LO QUE LA SALA SIRVE, EN UN SOLO LUGAR.
//
// `modulo/<m>/indice.json`, `modulo/<m>/dominio/<x>.dll`, `modulo/<m>/journal/...` y el sustrato.
// Eso es **contrato con el sustrato**, no código de un motor: los nombres de esos campos y de esas
// rutas los lee `Spikes/Lab.Sala/Sustrato/Sala.cs`, que se compila aparte y no se entera de nada.
//
// ── POR QUE ESTA CLASE EXISTE, Y POR QUE RECIEN AHORA ───────────────────────────────────────────
//
// Nacio porque en el escritorio hubo DOS salas por un dia —una sobre WebView2 y `PhotinoRoom` sobre
// un iframe oculto servido por loopback—, y escribir el mismo contrato dos veces en el mismo
// ensamblado es el error que este repo pago dos veces esta semana: una regla escrita dos veces no
// tiene dueño, y las dos copias divergen el dia que alguien toca una.
//
// **LA DE WEBVIEW2 SE BORRO EL 2026-09-25**: servia a `--cara ventana`, que no la pasa nadie. Asi
// que hoy esta clase tiene UN solo llamante en este ensamblado, y eso la deja sin la razon con la
// que nacio. Se queda igual, por la de abajo: los otros dos llamantes son los telefonos.
//
// Y HAY DOS COPIAS MAS, en `AndroidRoom` y `IosRoom`, que **no se tocaron**. No es olvido: `IosRoom`
// se renombro entero hace dos dias y **no lo compilo nadie todavia** -- no hay Mac. Refactorizarlo a
// ciegas encima de un renombre que ningun compilador vio es como una rotura silenciosa se vuelve
// dos. La extraccion de esas dos queda anotada para el dia que la Mac compile una vez. Ver F2 · P26.
internal static class RoomFiles
{
	// LO QUE EL SUSTRATO PIDE Y LO QUE SE LE CONTESTA. El cuerpo, su tipo, el estado y -solo para el
	// documento- la politica que BundleServer compone sobre su contenido.
	internal sealed record Served(byte[] Body, string Type, int Status, string? Csp);

	// DONDE VIVEN LOS BYTES DE UN MODULO: la carpeta de sus dll, cual es la entrada, y su journal.
	internal sealed record Prepared(string Domain, string Entry, string Journal);

	// EL SHIM, SERVIDO COMO ARCHIVO DEL MISMO ORIGEN y no incrustado en el documento.
	//
	// Es la solucion que este repo ya encontro para las caras: `script-src 'self'` admite un
	// `<script src>` del propio origen SIN hash, mientras que un script en linea obligaria a
	// recomponer la CSP que BundleServer ya calculo sobre el documento. Ver FrameWebModule.
	//
	// Solo lo usa la sala de Photino: la de WebView2 lo inyecta con
	// `AddScriptToExecuteOnDocumentCreatedAsync`, que corre antes que el documento y no pasa por
	// ninguna politica.
	internal const string ShimPath = "__gnothi/sala.js";

	// EL DOCUMENTO LO PIDE SOLO SI SE LO INYECTAN, porque `index.html` es EL MISMO ARCHIVO que
	// publican los dos telefonos y ahi el shim entra por la plataforma. Aca se agrega la etiqueta al
	// servirlo, antes de que BundleServer calcule la politica sobre el resultado -- si se calculara
	// antes, la etiqueta quedaria fuera de lo permitido y el documento saldria en blanco.
	private const string ShimTag = "<script src=\"/" + ShimPath + "\"></script>";

	internal static Served Serve(string substrate, string path, Func<string, Prepared?> servable, Action<string> log)
	{
		if (path.StartsWith("modulo/", StringComparison.Ordinal))
		{
			string[] parts = path.Split('/', 4);
			if (parts.Length < 3) return NotFound(path, log);

			string module = parts[1];
			if (servable(module) is not Prepared p) return NotFound(path, log);

			if (parts[2] == "indice.json")
			{
				// SOLO LO QUE ES DEL MODULO. Todo lo demas lo presta el sustrato, y no es una
				// optimizacion: un paquete del BCL viene compilado POR PLATAFORMA, y el que esta en
				// la carpeta del modulo es el de esta maquina.
				string[] files = [.. Directory.EnumerateFiles(p.Domain, "*.dll")
					.Select(Path.GetFileNameWithoutExtension)
					.Where(n => n is not null && !LentByTheRoom(n))
					.Select(n => n!)
					.OrderBy(n => string.Equals(n, p.Entry, StringComparison.Ordinal) ? 1 : 0)];

				// `entrada` y `archivos` son CAMPOS DEL JSON, no propiedades de C#: los lee el
				// sustrato con GetProperty. Van nombrados a proposito para que un renombrado
				// automatico no se los lleve -- ya paso el 2026-09-23.
				return Text(JsonSerializer.Serialize(new { entrada = p.Entry, archivos = files }), "application/json");
			}

			if (parts[2] == "dominio" && parts.Length == 4)
				return FromFile(Path.Combine(p.Domain, parts[3]), "application/octet-stream");

			if (parts[2] == "journal" && parts.Length == 4 && parts[3] == "indice.json")
			{
				string[] paths = Directory.Exists(p.Journal)
					? [.. Directory.EnumerateFiles(p.Journal, "*", SearchOption.AllDirectories)
						.Select(f => Path.GetRelativePath(p.Journal, f).Replace(Path.DirectorySeparatorChar, '/'))]
					: [];

				return Text(JsonSerializer.Serialize(new { rutas = paths }), "application/json");
			}

			// Y NO SE SALE DE LA CARPETA CONCEDIDA.
			if (parts[2] == "journal" && parts.Length == 4)
			{
				string root = Path.GetFullPath(p.Journal);
				string asked = Path.GetFullPath(Path.Combine(root, parts[3].Replace('/', Path.DirectorySeparatorChar)));
				if (!asked.StartsWith(root, StringComparison.Ordinal)) return NotFound(path, log);

				return FromFile(asked, "application/octet-stream");
			}

			return NotFound(path, log);
		}

		return FromSubstrate(substrate, path, log);
	}

	// EL SUSTRATO, Y EL DOCUMENTO CON SU POLITICA. El orden importa y esta explicado arriba: primero
	// se agrega la etiqueta del shim, despues se calcula la CSP sobre el documento resultante.
	private static Served FromSubstrate(string substrate, string path, Action<string> log)
	{
		string full = Path.Combine(substrate, path.Replace('/', Path.DirectorySeparatorChar));
		if (!File.Exists(full)) return NotFound(path, log);

		byte[] body = File.ReadAllBytes(full);

		if (!path.EndsWith(".html", StringComparison.Ordinal))
			return new Served(body, MimeOf(path), 200, null);

		return new Served(body, MimeOf(path), 200, Gnothi.MobileShared.BundleServer.CspFor(body));
	}

	// EL MISMO DOCUMENTO, CON EL SHIM ADENTRO. Solo para la sala de Photino, donde no hay forma
	// nativa de inyectar un script antes del documento.
	internal static Served WithShim(string substrate, Action<string> log)
	{
		string full = Path.Combine(substrate, "index.html");
		if (!File.Exists(full)) return NotFound("index.html", log);

		string html = File.ReadAllText(full);

		// ANTES DE CUALQUIER OTRO SCRIPT, porque el sustrato decide si esta hospedado mirando
		// `window.SalaPuente` apenas arranca. Si el shim llegara despues, correria la barrida del
		// laboratorio suelto en vez de quedarse esperando pedidos.
		int head = html.IndexOf("<head", StringComparison.OrdinalIgnoreCase);
		int close = head >= 0 ? html.IndexOf('>', head) : -1;

		html = close >= 0
			? html[..(close + 1)] + ShimTag + html[(close + 1)..]
			: ShimTag + html;

		byte[] body = Encoding.UTF8.GetBytes(html);
		return new Served(body, "text/html", 200, Gnothi.MobileShared.BundleServer.CspFor(body));
	}

	private static Served FromFile(string path, string type) =>
		File.Exists(path)
			? new Served(File.ReadAllBytes(path), type, 200, null)
			: new Served([], "text/plain", 404, null);

	private static Served Text(string body, string type) =>
		new(Encoding.UTF8.GetBytes(body), type, 200, null);

	private static Served NotFound(string path, Action<string> log)
	{
		log($"sala: 404 {path}");
		return new Served([], "text/plain", 404, null);
	}

	// LA LISTA DE LO PRESTADO, la otra cara de la lista de prohibidos del instalador. **Tiene que
	// seguir siendo la misma que la de AndroidRoom y IosRoom**: lo que esta aca lo pone el sustrato
	// en su build de browser-wasm, y lo que no, viaja en el modulo.
	private static bool LentByTheRoom(string assembly) =>
		assembly.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
		|| assembly.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase)
		|| assembly is "Puppeteer" or "Choreography" or "Puppets.Portador" or "Gnothi.Contract"
		|| assembly.StartsWith("ModelContextProtocol", StringComparison.OrdinalIgnoreCase)
		|| assembly is "netstandard" or "mscorlib";

	internal static string MimeOf(string path) => Path.GetExtension(path).ToLowerInvariant() switch
	{
		".html" => "text/html",
		".js" or ".mjs" => "text/javascript",
		".css" => "text/css",
		".wasm" => "application/wasm",
		".json" => "application/json",
		_ => "application/octet-stream",
	};
}
