namespace Gnothi.Host;

// ONE MODULE INSTANCE: what the institution declared + what the Host granted it + the shell
// it is run with.
//
// This is the unit of isolation. In this POC the real isolation is whatever the operating
// system gives a child process -which is little- plus the folder of its own. Hard isolation
// (its own linear memory, --dir) arrives with Stage 2. Better not to claim that this POC
// demonstrates confinement: it demonstrates the aggregation link, which is another thing.
public sealed class Module : IAsyncDisposable
{
	private readonly IShell shell;

	public Module(Manifest manifest, Installation installation, string directory)
	{
		Manifest = manifest;
		Installation = installation;
		Directory = directory;
		shell = ChooseShell(manifest.Module, installation);
	}

	public Manifest Manifest { get; }
	// Three things about a module change while it is running, and each has its one named write
	// path: consent (Reconsent), the mark on its pin when the origin retires the key it was
	// accepted with (MarkSignerRevoked), and the note that the origin publishes a newer package
	// (NoteUpdate). Private setter. The shell and the granted folder are not reloaded, because
	// changing those is reinstalling, not reconsenting.
	public Installation Installation { get; private set; }
	public string Directory { get; }
	public string Name => Manifest.Module;
	public bool IsRunning => shell.IsRunning;

	// The name the tool travels under towards the assistant. Prefixed by module because two
	// institutions are going to call two different things 'total', and the assistant picks by
	// name and description.

	// THE FACTORY. This is where `Shell.Type` starts being read -- it was declared in
	// Installation.cs from the beginning and nobody consulted it: the shell was hardcoded.
	// Now the choice is PER MODULE, and it lives in installation.json, which is the Host's.
	//
	// Practical consequence: facturas can stay on stdio while examenes runs in-process, both
	// over the same journals. Migrating is changing one word in a json.
	private static IShell ChooseShell(string name, Installation installation) =>
		installation.Shell.Type.ToLowerInvariant() switch
		{
			// MOVIL: el modulo es la PWA de la institucion dentro de un WebView. Es la unica
			// forma que pasa el TEXTO de Google Play para codigo que llega de afuera, y por eso
			// en un telefono "in-process" probablemente no se pueda publicar aunque funcione.
			//
			// ESTE COMENTARIO DECIA MAS DE LO QUE SE SABIA. Decia que "Play y Apple nombran
			// JavaScript en un webview como LA excepcion". Eso venia del viejo §3.3.2 del acuerdo
			// de Apple, que YA NO EXISTE con esa redaccion. Leidos los tres textos el 2026-09-16
			// (ver Documentacion/Jonathan/B-arquitectura/B11-lo-que-dicen-las-tiendas.md):
			//
			//   - Google Play SI es claro, y la palabra que decide es "indirect": el WebView da
			//     acceso INDIRECTO a las APIs de Android y esta permitido; un dll cargado por
			//     Mono en este proceso da acceso DIRECTO y no lo esta.
			//   - Apple ya no nombra motor: dice "interpreted code", con tres condiciones. Es mas
			//     ancho, y la tercera -no crear una tienda o escaparate para otras aplicaciones-
			//     le pega a Gnothi entero, no a una forma.
			//   - Y el 4.7.2 de Apple -no exponer APIs nativas al software que no viene en el
			//     binario sin permiso previo- le pega TAMBIEN a la forma hospedada, que este
			//     comentario daba por segura. El puente es justo eso.
			//
			// O sea: "webview" es la mejor posicionada, no la limpia. Nada de esto es veredicto:
			// son textos leidos, no una revision pasada.
			"webview"    => new WebViewShell(name, installation.Shell, installation.GrantedFolder),

			// HIBRIDA: la cara en el WebView y la superficie en proceso, con el motor prestado.
			// Es la unica de las tres en la que el WebView NO contesta: pregunta. Hereda la
			// objecion de arriba entera, porque el dll de la institucion corre nativo.
			"hybrid"     => new HybridShell(name, installation.Shell, installation.GrantedFolder),

			"in-process" => new InProcessShell(name, installation.Shell, installation.GrantedFolder),
			_            => new StdioShell(name, installation.Shell, installation.GrantedFolder),
		};

	public static string PublicName(string module, string tool) => $"{module}_{tool}";

	// The tools this module CONTRIBUTES to the catalog, already filtered by consent.
	//
	// Disabled returns empty: the module never finds out, and cannot tell "I was switched off"
	// apart from "nobody asked". That indistinguishability is the property, not a side effect.
	public IEnumerable<DeclaredTool> EnabledTools()
	{
		if (!Installation.Enabled) return [];
		return Manifest.Tools.Where(t => !Installation.DisabledTools.Contains(t.Name));
	}

	// RECONSENT: the person moved a switch and the Host found out by reading their file.
	//
	// Returns whether anything really changed, because a text editor writes the same file
	// three times and the assistant does not need to be told three times that its catalog
	// changed.
	//
	// Only the two consent fields come in. That the signature does not accept a whole
	// Installation is on purpose: it makes it IMPOSSIBLE for a shell change to sneak in
	// through here and pull the floor out from under a module that is already running.
	public bool Reconsent(bool enabled, IReadOnlyList<string> disabledTools)
	{
		bool sameSwitch = Installation.Enabled == enabled;
		bool sameDisabled = Installation.DisabledTools.Count == disabledTools.Count
			&& !Installation.DisabledTools.Except(disabledTools, StringComparer.Ordinal).Any();

		if (sameSwitch && sameDisabled) return false;

		Installation = Installation with { Enabled = enabled, DisabledTools = disabledTools };
		return true;
	}

	// THE ORIGIN RETIRED THE KEY THIS MODULE WAS ACCEPTED WITH, and Gnothi learnt it now: the mark
	// goes on the pin, in memory, mirroring what was just written to installation.json. Nothing
	// else moves -the switches stay as the person left them- and the screen repaints from here.
	// False when there is no pin, or it was already marked.
	public bool MarkSignerRevoked(string at)
	{
		if (Installation.Signer is not { RevokedAt: null } signer) return false;
		Installation = Installation with { Signer = signer with { RevokedAt = at } };
		return true;
	}

	// THE ORIGIN PUBLISHES A NEWER PACKAGE -or stopped doing so-, and Gnothi learnt it now: the note
	// goes on the installation in memory, mirroring what was just written to installation.json, and
	// the card shows it. Nothing downloads, nothing installs: that is hers to start. False when the
	// note did not change -the same version and seal already noted, or nothing to clear.
	public bool NoteUpdate(AvailableUpdate? update)
	{
		if (update is null ? Installation.Update is null : update.SameAs(Installation.Update)) return false;
		Installation = Installation with { Update = update };
		return true;
	}

	public bool Allows(string tool)
		=> Installation.Enabled && !Installation.DisabledTools.Contains(tool);

	public Task<object> Invoke(
		string tool, IReadOnlyDictionary<string, object?>? args, Accepts accepts, CancellationToken ct)
		=> shell.Invoke(tool, args, accepts, ct);

	// For the declared-against-real verification.
	public Task<IReadOnlyList<string>> ActualToolNames(CancellationToken ct)
		=> shell.ActualTools(ct);

	// LO QUE EL CARTERO TRAJO. Gnothi hizo la red y el modulo lo asienta -- o no puede, y la
	// cascara lo dice. No pasa por el consentimiento de las tools a proposito: apagar una
	// consulta decide QUE SE CONTESTA, no si el registro de la persona se sigue llenando.
	public Task<string> Deliver(string envelopeJson, CancellationToken ct)
		=> shell.Deliver(envelopeJson, ct);

	// HASTA DONDE YA LEYO. Tampoco pasa por el consentimiento, y por la misma razon que la
	// entrega: es el cursor del registro de la persona, no una consulta sobre ella.
	public Task<long> Cursor(CancellationToken ct) => shell.Cursor(ct);

	// LO QUE PIDE SU PROPIA PANTALLA. No pasa por Allows y no es un descuido: apagar una consulta
	// decide que puede preguntar EL ASISTENTE sobre la persona, no le prohibe a la institucion
	// mostrarle su expediente en la pantalla que ella acaba de abrir. Son decisiones con sujetos
	// distintos.
	public Task<object> View(
		string name, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct)
		=> shell.View(name, arguments, accepts, ct);

	// ── LA ADUANA DEL LOGO ──────────────────────────────────────────────────────────────
	//
	// EL LOGO DE LA INSTITUCION, LEIDO Y VALIDADO POR EL HOST, devuelto como un data: URI listo
	// para una pantalla -- o vacio, y entonces la cara pone el icono de la categoria.
	//
	// POR QUE VIVE ACA: es el mismo verbo que BytesOnDisk. El modulo no dice cuanto pesa ni
	// entrega su logo; el Host va, lo mide y lo mira. Y esta en Gnothi.Host y no en la cascara
	// del telefono porque las TRES caras -los dos telefonos y el escritorio- necesitan
	// exactamente esta aduana, y una aduana implementada tres veces es una aduana con dos
	// agujeros.
	//
	// POR QUE UN data: URI Y NO UNA RUTA: la pagina de la UI se sirve como file:// y no puede
	// leer otros file:// -- y aunque pudiera, darle a la pantalla una ruta del modulo seria
	// dejar que el modulo elija que archivo se abre. Asi los bytes que llegan a la pantalla son
	// los que este metodo ya aprobo, y nada mas.
	//
	// FAIL CLOSED, y es la decision que hace corto este codigo: si algo no se puede determinar
	// -el tipo, el ancho, el alto- NO PASA. Un parser que cubre todos los casos exoticos de
	// WebP es una superficie de ataque; uno que rechaza lo que no entiende es una linea.
	// ── LA CARA DE LA INSTITUCION, RESUELTA ─────────────────────────────────────────────
	//
	// Lo que la pantalla necesita para pintar un modulo: su logo -- ya validado, como data URI
	// -- y su color. En ese orden de preferencia:
	//
	//   1. `brand` del manifiesto: la declaracion explicita, y manda.
	//   2. EL `manifest.webmanifest` QUE LA PWA YA TRAE EN SU BUNDLE: `icons` y `theme_color`.
	//
	// EL SEGUNDO ES LA MEJOR PARTE Y NO ES NUESTRA IDEA: es de la rama de la marca, y hace algo
	// que la version que exigia declarar `brand.logo` no hacia -- un bundle 1.0 se pinta con su
	// marca SIN TOCARLO, porque toda PWA ya declara sus iconos y su color para que el navegador
	// la instale. Declarar `brand` pasa a ser afinar eso, no habilitarlo.
	//
	// Lo que esta version agrega del otro lado: los iconos del webmanifest pasan por la MISMA
	// aduana que un logo declarado. El `type` que el webmanifest declara es una afirmacion de
	// quien lo escribio, y aca el tipo sale de los bytes.
	public static BrandLook LookOf(Manifest manifest, string directory, out string problem)
	{
		string color = Brand.ValidColor(manifest.Brand?.Color) ?? "";
		string logo = LogoDataUri(manifest, directory, out problem);

		// The banner and the typeface only come declared: the webmanifest has nothing to say about
		// them. A file that does not pass leaves the card without it, and the reason is kept.
		string banner = BannerDataUri(manifest, directory, out string bannerProblem);
		string family = "", regular = "", bold = "";
		if (manifest.Brand?.Font is { } font && Manifest.Txt(font.Family).Length > 0)
		{
			regular = FontDataUri(directory, font.File, out string fontProblem);
			bold = FontDataUri(directory, font.Bold, out _);
			if (regular.Length > 0) family = Manifest.Txt(font.Family);
			else if (fontProblem.Length > 0) bannerProblem = (bannerProblem.Length > 0 ? bannerProblem + "; " : "") + "brand.font " + fontProblem;
		}
		if (problem.Length == 0) problem = bannerProblem;

		if (logo.Length > 0 && color.Length > 0) return new BrandLook(logo, color, banner, family, regular, bold);

		// Lo que `brand` no dijo -o dijo mal-, lo completa el webmanifest de la PWA.
		(string? webLogo, string? webColor) = FromWebManifest(directory);
		if (logo.Length == 0 && webLogo is not null)
		{
			logo = webLogo;
			// Un logo declarado que no paso la aduana Y un webmanifest que si: el problema del
			// primero deja de importarle a la pantalla, pero no se borra -- la institucion tiene
			// que poder enterarse de que su archivo no entro.
		}
		if (color.Length == 0 && webColor is not null) color = webColor;

		return new BrandLook(logo, color, banner, family, regular, bold);
	}

	// EL WEBMANIFEST DE LA PWA, que es de la institucion y ya viaja en el bundle. Un archivo
	// torcido no tumba nada: ese modulo se pinta con el dibujo de su rubro.
	private static (string? logo, string? color) FromWebManifest(string directory)
	{
		string web = Path.Combine(directory, "web");
		string path = Path.Combine(web, "manifest.webmanifest");
		if (!File.Exists(path)) return (null, null);

		try
		{
			using System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(
				File.ReadAllText(path),
				new System.Text.Json.JsonDocumentOptions
				{
					CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
					AllowTrailingCommas = true,
				});

			System.Text.Json.JsonElement root = doc.RootElement;

			string? color = root.TryGetProperty("theme_color", out System.Text.Json.JsonElement c)
				&& c.ValueKind == System.Text.Json.JsonValueKind.String
					? Brand.ValidColor(c.GetString())
					: null;

			return (PickIcon(root, web), color);
		}
		catch
		{
			return (null, null);
		}
	}

	// EL ICONO QUE ELIGE: el de 192 sin `purpose` o con `any`, que es el que el navegador usa
	// para la pestana y el dialogo de instalar. Los `maskable` van cuadrados y con el simbolo
	// chico porque Android les aplica su propia mascara, y aca no hay mascara. Si no hay de 192,
	// el primero que pase la aduana.
	private static string? PickIcon(System.Text.Json.JsonElement root, string web)
	{
		if (!root.TryGetProperty("icons", out System.Text.Json.JsonElement icons)
			|| icons.ValueKind != System.Text.Json.JsonValueKind.Array) return null;

		List<(string src, bool any, bool is192)> candidates = [];
		foreach (System.Text.Json.JsonElement i in icons.EnumerateArray())
		{
			if (!i.TryGetProperty("src", out System.Text.Json.JsonElement s)
				|| s.ValueKind != System.Text.Json.JsonValueKind.String) continue;

			string src = s.GetString() ?? "";
			if (src.Length == 0) continue;

			string purpose = i.TryGetProperty("purpose", out System.Text.Json.JsonElement p)
				&& p.ValueKind == System.Text.Json.JsonValueKind.String ? p.GetString() ?? "" : "";
			string sizes = i.TryGetProperty("sizes", out System.Text.Json.JsonElement z)
				&& z.ValueKind == System.Text.Json.JsonValueKind.String ? z.GetString() ?? "" : "";

			bool any = purpose.Length == 0
				|| purpose.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("any");
			candidates.Add((src, any, sizes.Contains("192", StringComparison.Ordinal)));
		}

		foreach ((string src, _, _) in candidates.OrderByDescending(x => x.any).ThenByDescending(x => x.is192))
		{
			// POR LA MISMA ADUANA. El `type` del webmanifest lo declara quien lo escribio; el
			// tipo de verdad sale de los bytes.
			string uri = ReadImage(web, src, out _);
			if (uri.Length > 0) return uri;
		}
		return null;
	}

	public string LogoDataUri() => LogoDataUri(Manifest, Directory, out _);

	// LA FORMA ESTATICA EXISTE POR EL ALTA. Cuando Gnothi le muestra a la persona lo que una
	// institucion le propone, el modulo TODAVIA NO ESTA INSTALADO: el bundle esta desempaquetado
	// en una carpeta temporal y no hay ningun Module. Y esa pantalla -- la del si o no -- es
	// justamente donde el logo mas importa.
	//
	// Tambien dice POR QUE no paso, para que la razon llegue al warning y de ahi a la
	// institucion, que es la unica que puede arreglarlo.
	public static string LogoDataUri(Manifest manifest, string directory, out string problem)
	{
		problem = "";
		string declared = Manifest.Txt(manifest.Brand?.Logo);
		if (declared.Length == 0) return "";

		string uri = ReadImage(directory, declared, out problem);
		if (problem.Length > 0) problem = "brand.logo " + problem;
		return uri;
	}

	// LA ADUANA, sobre un archivo cualquiera de la carpeta del modulo. La usan las dos entradas
	// -- el logo declarado y los iconos del webmanifest -- porque el riesgo es el mismo y una
	// aduana con dos puertas es una aduana con una puerta.
	private static string ReadImage(string baseDir, string src, out string problem)
		=> ReadImage(baseDir, src, Brand.MaxLogoBytes, Brand.MaxLogoPixels, out problem);

	// THE BANNER, through the same customs as the logo but with a photo's tops. Empty when the
	// institution declared none, and `problem` says why one that was declared did not get in.
	public static string BannerDataUri(Manifest manifest, string directory, out string problem)
	{
		problem = "";
		string declared = Manifest.Txt(manifest.Brand?.Banner);
		if (declared.Length == 0) return "";
		string uri = ReadImage(directory, declared, Brand.MaxBannerBytes, Brand.MaxBannerPixels, out problem);
		if (problem.Length > 0) problem = "brand.banner " + problem;
		return uri;
	}

	// THE TYPEFACE. The file is resolved inside the bundle like any other asset, weighed, and
	// its type comes from its first bytes -woff2, woff, TrueType or OpenType- not from its
	// extension. It leaves as a data URI the page can put in an @font-face of its own.
	public static string FontDataUri(string directory, string src, out string problem)
	{
		problem = "";
		if (Manifest.Txt(src).Length == 0) return "";
		string full = ResolveInside(directory, src, out problem);
		if (full.Length == 0) return "";

		var info = new FileInfo(full);
		if (info.Length == 0 || info.Length > Brand.MaxFontBytes)
		{
			problem = $"pesa {info.Length / 1024} KB y el tope es {Brand.MaxFontBytes / 1024} KB";
			return "";
		}

		byte[] bytes = File.ReadAllBytes(full);
		string? mime = SniffFont(bytes);
		if (mime is null)
		{
			problem = "no es un woff2, woff, ttf ni otf legible -- se rechaza por lo que dicen sus bytes";
			return "";
		}
		return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
	}

	private static string? SniffFont(byte[] b)
	{
		if (b.Length < 4) return null;
		string tag = System.Text.Encoding.ASCII.GetString(b, 0, 4);
		return tag switch
		{
			"wOF2" => "font/woff2",
			"wOFF" => "font/woff",
			"OTTO" => "font/otf",
			"true" => "font/ttf",
			_ => b[0] == 0 && b[1] == 1 && b[2] == 0 && b[3] == 0 ? "font/ttf" : null,
		};
	}

	// A path declared by the institution, resolved inside its own folder or refused. Shared by
	// every asset that comes in through the manifest: one door, one rule.
	private static string ResolveInside(string baseDir, string src, out string problem)
	{
		problem = "";
		if (Uri.TryCreate(src, UriKind.Absolute, out Uri? u) && !u.IsFile)
		{
			problem = $"apunta a la red ('{src}') y por aca no sale nada a la red";
			return "";
		}
		string full = Path.GetFullPath(Path.Combine(baseDir, src.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
		string root = Path.GetFullPath(baseDir) + Path.DirectorySeparatorChar;
		if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
		{
			problem = $"apunta fuera de la carpeta del modulo: '{src}'";
			return "";
		}
		if (!File.Exists(full)) { problem = $"declara '{src}' y ese archivo no esta en el bundle"; return ""; }
		return full;
	}

	private static string ReadImage(string baseDir, string src, long maxBytes, int maxPixels, out string problem)
	{
		problem = "";

		// Un logo remoto no se baja: por aca no sale nada a la red.
		if (Uri.TryCreate(src, UriKind.Absolute, out Uri? u) && !u.IsFile)
		{
			problem = $"apunta a la red ('{src}') y por aca no sale nada a la red";
			return "";
		}

		// LA RUTA SE VUELVE A VALIDAR ACA, aunque Brand.Problems() ya la mira. No es
		// redundancia: Problems solo AVISA -- un tope no tumba un modulo -- asi que un
		// '../../otra/cosa.png' sigue llegando hasta aca. El que lee bytes es el que tiene que
		// decir no. Y el webmanifest, que ni pasa por Problems, entra por la misma puerta.
		string full = Path.GetFullPath(Path.Combine(baseDir,
			src.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
		string root = Path.GetFullPath(baseDir) + Path.DirectorySeparatorChar;
		if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
		{
			problem = $"apunta fuera de la carpeta del modulo: '{src}'";
			return "";
		}

		if (!File.Exists(full)) { problem = $"declara '{src}' y ese archivo no esta en el bundle"; return ""; }

		var info = new FileInfo(full);
		if (info.Length == 0 || info.Length > maxBytes)
		{
			problem = $"pesa {info.Length / 1024} KB y el tope es {maxBytes / 1024} KB";
			return "";
		}

		byte[] bytes = File.ReadAllBytes(full);

		// EL TIPO SALE DE LOS BYTES, NO DE LA EXTENSION NI DEL `type` DEL WEBMANIFEST. Los dos
		// los declara quien escribio el archivo, y este es justo el lugar donde no se le cree.
		string? mime = SniffImage(bytes, out int width, out int height);
		if (mime is null)
		{
			problem = "no es un png, jpeg, webp ni svg legible -- se rechaza por lo que dicen sus bytes";
			return "";
		}

		// EL TOPE DE PIXELES NO ES COSMETICO: un png de 3000x3000 de un solo color entra en 25 KB
		// y al decodificarlo pide gigabytes. El tope de peso no lo ataja; este si.
		//
		// Un SVG no tiene pixeles -- es vectorial y lo escala el contenedor -- asi que ahi el
		// tope no aplica y SniffImage devuelve 0x0 a proposito.
		if (width > maxPixels || height > maxPixels)
		{
			problem = $"mide {width}x{height} y el tope es {maxPixels} de lado";
			return "";
		}

		return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
	}

	// QUE ES ESTO Y CUANTO MIDE, leido del encabezado. Devuelve null para todo lo que no sea uno
	// de los formatos que se aceptan, o cuyo tamano no se pueda leer.
	private static string? SniffImage(byte[] b, out int width, out int height)
	{
		width = height = 0;

		// PNG: firma de 8 bytes y despues el chunk IHDR, que SIEMPRE es el primero y trae el
		// ancho y el alto en big-endian.
		if (b.Length >= 24
			&& b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47
			&& b[12] == 'I' && b[13] == 'H' && b[14] == 'D' && b[15] == 'R')
		{
			width = Be32(b, 16);
			height = Be32(b, 20);
			return width > 0 && height > 0 ? "image/png" : null;
		}

		// JPEG: hay que caminar los segmentos hasta el SOF, que es el que lleva las medidas.
		if (b.Length >= 4 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
		{
			int i = 2;
			while (i + 9 < b.Length)
			{
				if (b[i] != 0xFF) return null;              // desincronizado: no se adivina
				byte marker = b[i + 1];
				if (marker == 0xFF) { i++; continue; }        // relleno legitimo
				int len = Be16(b, i + 2);
				if (len < 2) return null;

				// SOF0..SOF15 menos los cuatro que no son SOF (C4 DHT, C8, CC DAC).
				if (marker >= 0xC0 && marker <= 0xCF
					&& marker != 0xC4 && marker != 0xC8 && marker != 0xCC)
				{
					height = Be16(b, i + 5);
					width = Be16(b, i + 7);
					return width > 0 && height > 0 ? "image/jpeg" : null;
				}

				i += 2 + len;
			}
			return null;                                     // sin SOF: no se sabe cuanto mide
		}

		// WebP: RIFF....WEBP y despues el chunk, que dice en cual de los tres sabores viene.
		if (b.Length >= 30
			&& b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F'
			&& b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P')
		{
			string chunk = System.Text.Encoding.ASCII.GetString(b, 12, 4);

			if (chunk == "VP8X")                              // extendido: 24 bits, menos uno
			{
				width = (b[24] | b[25] << 8 | b[26] << 16) + 1;
				height = (b[27] | b[28] << 8 | b[29] << 16) + 1;
			}
			else if (chunk == "VP8 ")                         // con perdida: 14 bits utiles
			{
				width = (b[26] | b[27] << 8) & 0x3FFF;
				height = (b[28] | b[29] << 8) & 0x3FFF;
			}
			else if (chunk == "VP8L")                         // sin perdida: 14 bits, menos uno
			{
				int bits = b[21] | b[22] << 8 | b[23] << 16 | b[24] << 24;
				width = (bits & 0x3FFF) + 1;
				height = (bits >> 14 & 0x3FFF) + 1;
			}
			else return null;

			return width > 0 && height > 0 ? "image/webp" : null;
		}

		// SVG: es texto, asi que no hay magic bytes -- lo que se mira es que EMPIECE como un
		// documento svg, saltando el BOM y el espacio. Sale con 0x0 a proposito: no tiene
		// pixeles, y el tope de dimensiones no le aplica.
		//
		// Y viaja en un `<img>`, no inline: ahi el navegador lo aisla y no corre scripts. Ese
		// es todo el permiso que tiene.
		int at = b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF ? 3 : 0;
		while (at < b.Length && (b[at] == ' ' || b[at] == '\n' || b[at] == '\r' || b[at] == '\t')) at++;
		if (at + 5 <= b.Length)
		{
			string head = System.Text.Encoding.ASCII.GetString(b, at, Math.Min(512, b.Length - at));
			if (head.StartsWith("<svg", StringComparison.OrdinalIgnoreCase)
				|| (head.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)
					&& head.Contains("<svg", StringComparison.OrdinalIgnoreCase)))
				return "image/svg+xml";
		}

		return null;
	}

	private static int Be32(byte[] b, int at) => b[at] << 24 | b[at + 1] << 16 | b[at + 2] << 8 | b[at + 3];
	private static int Be16(byte[] b, int at) => b[at] << 8 | b[at + 1];

	// El logo y el color que la pantalla va a usar, ya resueltos. Vacio en cualquiera de los dos
	// significa "no hay", y la cara cae a lo que tenga -- el dibujo del rubro, el acento del
	// Host. El Host no inventa una marca que la institucion no dio.
	public sealed record BrandLook(string Logo, string Color, string Banner = "", string FontFamily = "",
		string FontRegular = "", string FontBold = "")
	{
		public bool Any => Logo.Length > 0 || Color.Length > 0 || Banner.Length > 0 || FontFamily.Length > 0;
	}

	// Measured FROM OUTSIDE, with stat over the folder the Host granted. The module is not
	// asked how much it takes up: a module that reports 4 MB while taking up 400 cannot exist
	// if nobody asks it.
	public long BytesOnDisk()
	{
		var granted = new DirectoryInfo(Installation.GrantedFolder);
		if (!granted.Exists) return 0;
		return granted
			.EnumerateFiles("*", SearchOption.AllDirectories)
			.Sum(f => f.Length);
	}

	public ValueTask DisposeAsync() => shell.DisposeAsync();
}
