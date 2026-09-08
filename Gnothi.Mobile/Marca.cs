using System.Text.Json;
using System.Text.RegularExpressions;
using Gnothi.Host;

namespace Gnothi.Mobile;

// LA MARCA DE LA INSTITUCION, leida del bundle que ella misma publico.
//
// POR QUE EXISTE (2026-09-08). Hasta aca la tarjeta de un modulo llevaba un glifo del Host
// -un mapa chico por nombre de modulo- y el nombre de la institucion iba en letra gris. Las
// instituciones ponen su modulo en Gnothi para que la persona vea SU marca: Laboratorio Clinico
// Delfos, no «Mis examenes» con una caja indigo. Asi que el icono y el color dejan de ser
// asunto del Host y pasan a ser lo que la institucion declaro.
//
// DE DONDE SALE, en este orden:
//
//   1. `brand` del manifest.json (formato 1.1): logo -ruta relativa al bundle-, color, slogan
//      y shortName. Es la declaracion explicita, y manda.
//   2. Si no hay `brand`, el `manifest.webmanifest` que la PWA ya trae en `web/`: `theme_color`
//      e `icons`. Es el mismo archivo que el navegador lee al instalarla como icono, asi que
//      los bundles 1.0 se pintan con su marca sin tocarlos.
//
// QUE DEVUELVE. Un diccionario chico que viaja en el estado de la pagina: `logo` (el icono
// como data URI, para que un `file://` pueda pintarlo sin servir nada) y `color`. O null, y la
// pagina cae al glifo de siempre: un modulo sin marca sigue siendo legitimo.
//
// LO QUE SE CUIDA. El color solo pasa si es un `#rrggbb` de verdad -va a un atributo style-,
// la ruta del logo no puede salirse del bundle, y un logo desmedido no se embebe: el estado
// se manda entero a la pagina cada vez que repinta.
//
// DONDE VIVE. Aca, en Gnothi.Mobile, porque en esta rama el estado de la pagina lo arma
// MainActivity. Cuando el merge con master termine de mover ese estado a Gnothi.Movil/Shell.cs
// (compartido con iOS), este archivo se muda con el: no depende de nada de Android.
internal static class Marca
{
	private const long IconoMaximo = 300 * 1024;
	private static readonly Regex ColorValido = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);

	public static Dictionary<string, object?>? Leer(string carpetaDelModulo, Manifest? manifiesto = null)
	{
		Brand? brand = manifiesto?.Brand;
		string? color = Color(brand?.Color);
		string? logo = brand?.Logo is { Length: > 0 } ruta ? DataUri(carpetaDelModulo, carpetaDelModulo, ruta, null) : null;

		// Lo que `brand` no dijo, lo completa el webmanifest de la PWA.
		if (logo is null || color is null)
		{
			var pwa = DelWebmanifest(carpetaDelModulo);
			logo ??= pwa.logo;
			color ??= pwa.color;
		}

		if (logo is null && color is null) return null;
		return new Dictionary<string, object?> { ["logo"] = logo, ["color"] = color };
	}

	private static string? Color(string? c) => c is { } s && ColorValido.IsMatch(s) ? s : null;

	private static (string? logo, string? color) DelWebmanifest(string carpetaDelModulo)
	{
		string web = Path.Combine(carpetaDelModulo, "web");
		string manifiesto = Path.Combine(web, "manifest.webmanifest");
		if (!File.Exists(manifiesto)) return (null, null);

		try
		{
			using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(manifiesto),
				new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
			JsonElement raiz = doc.RootElement;

			string? color = raiz.TryGetProperty("theme_color", out JsonElement c) && c.ValueKind == JsonValueKind.String
				? Color(c.GetString()) : null;

			return (LogoDe(raiz, web), color);
		}
		catch
		{
			// Un webmanifest torcido no tumba la pantalla: ese modulo se pinta con el glifo.
			return (null, null);
		}
	}

	// EL ICONO QUE ELIGE: el de 192 sin `purpose` o con `any`, que es el que el navegador usa
	// para la pestaña y el dialogo de instalar. Los `maskable` van cuadrados y con el simbolo
	// chico porque Android les aplica su propia mascara, y aca no hay mascara. Si no hay de
	// 192, el primero que sirva.
	private static string? LogoDe(JsonElement raiz, string web)
	{
		if (!raiz.TryGetProperty("icons", out JsonElement iconos) || iconos.ValueKind != JsonValueKind.Array) return null;

		var candidatos = new List<(string src, string? type, string sizes, bool any)>();
		foreach (JsonElement i in iconos.EnumerateArray())
		{
			if (!i.TryGetProperty("src", out JsonElement s) || s.ValueKind != JsonValueKind.String) continue;
			string src = s.GetString() ?? "";
			if (src.Length == 0) continue;
			string? type = i.TryGetProperty("type", out JsonElement t) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;
			string sizes = i.TryGetProperty("sizes", out JsonElement z) && z.ValueKind == JsonValueKind.String ? z.GetString() ?? "" : "";
			string purpose = i.TryGetProperty("purpose", out JsonElement p) && p.ValueKind == JsonValueKind.String ? p.GetString() ?? "" : "";
			bool any = purpose.Length == 0 || purpose.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("any");
			candidatos.Add((src, type, sizes, any));
		}

		foreach (var c in candidatos.OrderByDescending(c => c.any).ThenByDescending(c => c.sizes.Contains("192")))
		{
			string? uri = DataUri(web, web, c.src, c.type);
			if (uri is not null) return uri;
		}
		return null;
	}

	// `base` es desde donde se resuelve la ruta; `limite` la carpeta de la que no puede salirse.
	private static string? DataUri(string @base, string limite, string src, string? type)
	{
		// Un logo remoto no se baja: por aca no sale nada a la red.
		if (Uri.TryCreate(src, UriKind.Absolute, out Uri? u) && !u.IsFile) return null;

		string raiz = Path.GetFullPath(limite).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
		string ruta = Path.GetFullPath(Path.Combine(@base, src.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
		if (!ruta.StartsWith(raiz, StringComparison.Ordinal)) return null;

		var info = new FileInfo(ruta);
		if (!info.Exists || info.Length == 0 || info.Length > IconoMaximo) return null;

		string mime = type is { Length: > 0 } ? type : Path.GetExtension(ruta).ToLowerInvariant() switch
		{
			".png" => "image/png",
			".svg" => "image/svg+xml",
			".webp" => "image/webp",
			".jpg" or ".jpeg" => "image/jpeg",
			_ => "application/octet-stream",
		};
		if (!mime.StartsWith("image/", StringComparison.Ordinal)) return null;

		return $"data:{mime};base64,{Convert.ToBase64String(File.ReadAllBytes(ruta))}";
	}
}
