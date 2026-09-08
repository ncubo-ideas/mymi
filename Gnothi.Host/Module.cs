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
	// Consent is the ONLY thing about a module that is mutable while it is running: private
	// setter and a single write path, Reconsent. The shell and the granted folder are not
	// reloaded, because changing those is reinstalling, not reconsenting.
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
			// forma que las tiendas permiten para codigo que llega de afuera -Play y Apple
			// nombran JavaScript en un webview como la excepcion-, y por eso en un telefono
			// "in-process" no se puede publicar aunque funcione.
			"webview"    => new WebViewShell(name, installation.Shell, installation.GrantedFolder),
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

	public bool Allows(string tool)
		=> Installation.Enabled && !Installation.DisabledTools.Contains(tool);

	public Task<object> Invoke(
		string tool, IReadOnlyDictionary<string, object?>? args, Accepts accepts, CancellationToken ct)
		=> shell.Invoke(tool, args, accepts, ct);

	// For the declared-against-real verification.
	public Task<IReadOnlyList<string>> ActualToolNames(CancellationToken ct)
		=> shell.ActualTools(ct);

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

		// LA RUTA SE VUELVE A VALIDAR ACA, aunque Brand.Problems() ya la mira. No es
		// redundancia: Problems solo AVISA -- un tope no tumba un modulo -- asi que un
		// 'logo': '../../otra/cosa.png' sigue llegando hasta aca. El que lee bytes es el que
		// tiene que decir no.
		string full = Path.GetFullPath(Path.Combine(directory, declared));
		string root = Path.GetFullPath(directory) + Path.DirectorySeparatorChar;
		if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
		{
			problem = $"brand.logo apunta fuera de la carpeta del modulo: '{declared}'";
			return "";
		}

		if (!File.Exists(full)) { problem = $"brand.logo declara '{declared}' y ese archivo no esta en el bundle"; return ""; }

		var info = new FileInfo(full);
		if (info.Length > Brand.MaxLogoBytes)
		{
			problem = $"brand.logo pesa {info.Length / 1024} KB y el tope es {Brand.MaxLogoBytes / 1024} KB";
			return "";
		}

		byte[] bytes = File.ReadAllBytes(full);

		// EL TIPO SALE DE LOS BYTES, NO DE LA EXTENSION. Un .png que adentro es un svg es
		// exactamente el ataque que importa: el svg EJECUTA, y la pagina que lo pintaria es la
		// que tiene el puente del consentimiento de Gnothi.
		string? mime = SniffImage(bytes, out int width, out int height);
		if (mime is null)
		{
			problem = $"brand.logo no es un png, jpeg ni webp legible -- se rechaza por lo que dicen sus bytes, no por su extension";
			return "";
		}

		// EL TOPE DE PIXELES NO ES COSMETICO: un png de 20.000 x 20.000 de un solo color entra
		// en pocos KB y al decodificarlo pide gigabytes. El tope de peso no lo ataja; este si.
		if (width > Brand.MaxLogoPixels || height > Brand.MaxLogoPixels)
		{
			problem = $"brand.logo mide {width}x{height} y el tope es {Brand.MaxLogoPixels} de lado";
			return "";
		}

		return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
	}

	// QUE ES ESTO Y CUANTO MIDE, leido del encabezado. Devuelve null para todo lo que no sea uno
	// de los tres formatos que se aceptan, o cuyo tamano no se pueda leer.
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

		return null;
	}

	private static int Be32(byte[] b, int at) => b[at] << 24 | b[at + 1] << 16 | b[at + 2] << 8 | b[at + 3];
	private static int Be16(byte[] b, int at) => b[at] << 8 | b[at + 1];

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
