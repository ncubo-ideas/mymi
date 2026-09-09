using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gnothi.Host;

// THE MANIFEST BELONGS TO THE MODULE. The institution writes it, and some day will sign it.
// It declares identity, which substrate it needs, which capabilities it asks for, and -the
// part this POC cares about- ITS LIST OF TOOLS.
//
// Why the list comes from here instead of being asked of the module by starting it up:
// reading a json costs microseconds and wakes nothing. The Host has to be able to build the
// catalog of twenty modules without instantiating twenty runtimes. The module only starts
// when somebody calls one of its tools.
//
// And because it is a DECLARATION, it can be verified: later on the Host asks the module for
// its real tools/list and compares. What was declared against what is.
public sealed record Manifest
{
	[JsonPropertyName("manifest")] public string Version { get; init; } = "";
	[JsonPropertyName("module")] public string Module { get; init; } = "";
	[JsonPropertyName("title")] public string Title { get; init; } = "";
	[JsonPropertyName("institution")] public string Institution { get; init; } = "";
	[JsonPropertyName("substrate")] public Substrate? Substrate { get; init; }
	[JsonPropertyName("capabilities")] public Capabilities? Capabilities { get; init; }

	// LO QUE LA INSTITUCION DICE DE SI MISMA (formato 1.1, 2026-09-08). Los cuatro grupos son
	// OPCIONALES: un manifiesto que no los trae instala igual y se ve como se veia. Eso no es
	// cortesia, es lo que permite que el Host los estrene sin huerfanar a nadie.
	//
	// Y `brand` es opcional en un sentido mas fuerte todavia: si no esta, la marca sale del
	// `manifest.webmanifest` que la PWA YA trae en su bundle -- theme_color e icons --, asi que
	// un manifiesto 1.0 se pinta con su marca sin tocarlo. Declarar `brand` es afinar eso, no
	// habilitarlo.
	//
	// VAN AGRUPADOS Y NO SUELTOS EN LA RAIZ porque un grupo se puede ignorar entero, versionar
	// entero y -cuando llegue la firma- firmar entero.
	//
	// LA REGLA QUE FRENA EL PROXIMO CAMPO, y conviene leerla antes de agregar uno: el manifiesto
	// declara HECHOS VERIFICABLES SOBRE LA INSTITUCION. Nunca instrucciones para el Host, y
	// nunca instrucciones para el modelo. Por eso no hay -y no va a haber- un 'assistantHint',
	// un 'tone' ni una 'persona': eso seria inyeccion de prompt con formato de contrato, escrita
	// por un tercero y entregada por Gnothi.
	[JsonPropertyName("brand")] public Brand? Brand { get; init; }
	[JsonPropertyName("publisher")] public Publisher? Publisher { get; init; }
	[JsonPropertyName("about")] public About? About { get; init; }
	[JsonPropertyName("journal")] public JournalDeclaration? Journal { get; init; }

	// Donde esta el publicador de esta institucion, para el cartero. Opcional: un modulo
	// puede vivir solo de lo que ya tiene en su journal.
	[JsonPropertyName("publicador")] public Publicador? Publicador { get; init; }
	[JsonPropertyName("tools")] public IReadOnlyList<DeclaredTool> Tools { get; init; } = [];

	// THE DECLARATIVE VIEW, optional. The name of a json file, shipped inside the module, that
	// describes the screen the institution designed for the PERSON -not for the assistant. The
	// module describes; the Host renders. A module with no view is legitimate: it just answers
	// queries and has no screen of its own. It is a file name, not the view itself, because the
	// catalog is built from manifests alone and must not pay to parse every screen up front.
	[JsonPropertyName("view")] public string View { get; init; } = "";

	// HOW A CARGO MODULE IS LOADED. Present only for cargo modules (two small DLLs + the Host
	// lends the engine); absent for stdio ones, which carry their own server. It says which
	// assembly holds the surface -the Host loads that one and finds the [ModuleSurface] class in
	// it, resolving the domain next door. It belongs to the manifest because "which DLL is me"
	// is identity, declared by the institution, not something the Host grants.
	[JsonPropertyName("load")] public Load? Load { get; init; }

	// LA LINEA QUE IDENTIFICA A ESTA INSTITUCION EN LA PANTALLA, resuelta UNA VEZ.
	//
	// Vive aca y no en cada cara por lo mismo que el icono no deberia haber vivido en un switch:
	// hay tres caras -los dos telefonos y el escritorio- y un fallback implementado tres veces
	// es un fallback que diverge. La regla es simple y no la decide la institucion: si declaro un
	// nombre corto se usa ese, y si no, el 'institution' que siempre estuvo.
	//
	// Lo que NO hace es inventar: si el manifiesto no dice nada, esto devuelve vacio y la cara
	// decide que poner. El Host no le pone nombre a nadie.
	public string Identity
		=> Brand?.ShortName is { Length: > 0 } s ? s : Institution;

	// TODO LO QUE SE PASA DE LOS TOPES, en una lista. Es el mismo verbo que Capabilities.Excesses:
	// el Host no rechaza el modulo por un titulo largo -eso seria dejar a la persona sin su
	// laboratorio por un renglon- pero lo DICE, y lo dice donde ya se dicen estas cosas, en los
	// warnings del catalogo.
	//
	// Que sea una lista y no un bool es la parte que importa: un tope que no se puede nombrar no
	// se puede arreglar, y quien tiene que arreglarlo es la institucion, que no ve este proceso.
	public IEnumerable<string> Problems()
	{
		if (Txt(Title).Length > MaxTitle)
			yield return $"title tiene {Txt(Title).Length} caracteres y el tope es {MaxTitle}";

		foreach (string p in Brand?.Problems() ?? []) yield return p;
		foreach (string p in Publisher?.Problems() ?? []) yield return p;
		foreach (string p in About?.Problems() ?? []) yield return p;
	}

	public const int MaxTitle = 32;

	// UNA PROPIEDAD QUE EL JSON NO TRAE LLEGA NULL, NO CON SU INICIALIZADOR, y por eso todo lo
	// que este archivo toca de un manifiesto pasa por aca.
	//
	// Medido el 2026-09-08: un manifiesto con "about": { "category": "..." } y sin
	// "description" hacia estallar About.Problems() con NullReferenceException en
	// Description.Length, aunque la propiedad declara = "". El catalogo lo atajaba -- un
	// manifiesto que revienta se saltea con un warning -- asi que el sintoma no era un crash,
	// era UN MODULO QUE DESAPARECIA de la pantalla por no haber escrito un campo OPCIONAL. El
	// peor de los dos.
	//
	// Y hay una pista de que esto ya se habia topado antes: Manifest.Read valida el modulo con
	// IsNullOrWhiteSpace y no con Length == 0. Ahora esta dicho en voz alta.
	//
	// LA REGLA PARA EL PROXIMO CAMPO: en un manifiesto, ningun string es de fiar. Ni con
	// inicializador, porque el JSON tambien puede traer null EXPLICITO -- y eso ningun default
	// lo puede evitar.
	internal static string Txt(string? s) => s ?? "";

	public static Manifest Read(string path)
	{
		Manifest? m = JsonSerializer.Deserialize(File.ReadAllText(path), HostJson.Default.Manifest);
		if (m is null) throw new InvalidDataException($"empty or unreadable manifest: {path}");
		if (string.IsNullOrWhiteSpace(m.Module))
			throw new InvalidDataException($"the manifest does not declare 'module': {path}");
		return m;
	}

	internal static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
	};
}

// A RANGE, NOT an exact version. If a module pins one precise version, every Host update
// orphans every installed module.

// DONDE VIVE LA NARRATIVA de esta institucion. Lo declara ELLA en su manifiesto, y Gnothi lo
// registra al dar de alta el modulo: a partir de ahi el endpoint es FIJO y el modulo no lo
// elige ni lo puede cambiar (especificacion 11 §6, 13 §7).
//
// El modulo nunca lo usa: no tiene red. Lo usa el cartero, que es Gnothi.
public sealed record Publicador
{
	// La base del servicio de eventos de la institucion.
	[JsonPropertyName("eventos")] public string Eventos { get; init; } = "";

	// Que recurso de la persona, y cual. La app pide UNA sola forma de URL sobre esto:
	// {recurso}/{id}/events?after=N, y nada mas.
	[JsonPropertyName("recurso")] public string Recurso { get; init; } = "expediente";

	[JsonPropertyName("id")] public string Id { get; init; } = "1";
}

// LA MARCA, Y ES LO UNICO DE ESTE ARCHIVO QUE LLEGA AL OJO SIN LEERSE. Hasta ahora el icono lo
// elegia el Host con un switch por nombre de modulo -tres copias del mismo switch- asi que un
// modulo de una institucion que el Host no conocia entraba anonimo. Eso contradice el alta
// dinamica: agregar un modulo son dos json, no dos json y un release del Host.
//
// EL CORTE: la institucion DECLARA, el Host COMPONE. Ella provee el material; el decide el
// lugar, el tamano y el contraste. Por eso aca no hay ni una medida ni una regla de estilo: hay
// un archivo y un color.
public sealed record Brand
{
	// EL NOMBRE CON EL QUE LA GENTE LA CONOCE -'La Guaria', no 'Supermercado La Guaria S.A.'-.
	// Es la linea que IDENTIFICA en la tarjeta, y por eso lleva tope: un renglon de 28
	// caracteres es lo que cabe sin cortar en la grilla de dos columnas del telefono.
	[JsonPropertyName("shortName")] public string ShortName { get; init; } = "";

	// UN NOMBRE DE ARCHIVO DENTRO DEL BUNDLE -relativo a el, tipo «web/icon-192.png»-, no bytes
	// y NO UNA URL. Una URL seria un beacon: le contaria a la institucion cada vez que la
	// persona abre su lista de modulos, y eso rompe la propiedad central -Gnothi no le pregunta
	// nada a ninguna empresa- por un dibujo.
	//
	// Los bytes los lee el Host, los valida y los pasa ya validados a la pantalla.
	//
	// SVG SI, y esto corrige un argumento que estuvo escrito aca: «el formato ejecuta» es cierto
	// para un svg INLINE en el DOM, y falso para uno que viaja como data URI en un `<img>`, que
	// es como llega -- ahi el navegador lo aisla y no corre scripts. Lo que si cambia con el svg
	// es que no tiene pixeles: es vectorial y lo escala el contenedor, asi que el tope de
	// dimensiones no le aplica y el de peso si.
	[JsonPropertyName("logo")] public string Logo { get; init; } = "";

	// UN COLOR, uno solo, en #rrggbb. El Host lo usa donde decide -hoy el tinte del icono- y le
	// garantiza el contraste. Nunca CSS: eso seria dejar que la institucion escriba estilo en la
	// pantalla donde la persona le da y le quita permisos.
	[JsonPropertyName("color")] public string Color { get; init; } = "";

	// UNA LINEA CORTA EN LA VOZ DE LA INSTITUCION, que va debajo de su nombre en la tarjeta en
	// vez del titulo del modulo: «My invoices» no es prosa de tarjeta. Es lo que la persona lee
	// para saber quien le esta hablando, y por eso lleva tope como todo lo que llega al ojo.
	[JsonPropertyName("slogan")] public string Slogan { get; init; } = "";

	// THE BANNER (2026-09-09): a wide picture the Host puts BEHIND the card, with the institution's
	// name and slogan over it. Same rules as the logo -a file inside the bundle, never a URL, read
	// and validated by the Host- with wider tops because it is a photo, not an icon. No svg: a
	// banner has pixels or it is not a banner.
	[JsonPropertyName("banner")] public string Banner { get; init; } = "";

	// THE TYPEFACE (2026-09-09): the family the institution's name and slogan are set in, on its
	// own card and detail, so La Guaria reads like La Guaria and Sotavento like Sotavento. A font
	// file inside the bundle (woff2, woff, ttf or otf), optionally a second one for bold. The Host
	// embeds it as a data URI under a name of its own: no CSS from the institution ever runs.
	[JsonPropertyName("font")] public BrandFont? Font { get; init; }

	public const int MaxShortName = 28;
	public const int MaxSlogan = 40;
	public const int MaxLogoBytes = 64 * 1024;
	public const int MaxLogoPixels = 512;
	public const int MaxBannerBytes = 200 * 1024;
	public const int MaxBannerPixels = 1600;
	public const int MaxFontBytes = 160 * 1024;
	public const int MaxFontFamily = 40;

	private static readonly string[] AllowedBanner = [".png", ".jpg", ".jpeg", ".webp"];
	private static readonly string[] AllowedFont = [".woff2", ".woff", ".ttf", ".otf"];

	// A path that stays inside the bundle: relative, no '..', no drive, no scheme.
	public static bool InsideBundle(string path)
		=> !(path.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(path) || path.Contains(':'));

	// El svg entra por lo dicho arriba: viaja en un `<img>`, no inline. Y el webmanifest de una
	// PWA suele declarar png, asi que el camino por omision no lo usa.
	private static readonly string[] AllowedLogo = [".png", ".jpg", ".jpeg", ".webp", ".svg"];

	// UN COLOR O NADA, y el que llama decide que hacer con el nada. Devuelve el mismo string si
	// es un #rrggbb de verdad y null si no, porque el color va a terminar en un atributo `style`
	// de la pagina que tiene el puente del consentimiento: ahi no entra nada sin mirar.
	//
	// Vive aca -- y no en la pagina ni en el lector del webmanifest -- porque los tres caminos
	// que traen un color tienen que aplicar la misma regla. Uno solo que la relaje es el agujero.
	public static string? ValidColor(string? c)
		=> c is { Length: 7 } s && s[0] == '#' && s[1..].All(char.IsAsciiHexDigit) ? s : null;

	// Lo que se puede revisar SIN abrir el archivo. Los bytes -el peso, las dimensiones, que el
	// encabezado sea de verdad el que dice la extension- los revisa quien los lee, porque este
	// tipo no sabe donde esta la carpeta del modulo.
	public IEnumerable<string> Problems()
	{
		if (Manifest.Txt(ShortName).Length > MaxShortName)
			yield return $"brand.shortName tiene {Manifest.Txt(ShortName).Length} caracteres y el tope es {MaxShortName}";

		if (Manifest.Txt(Slogan).Length > MaxSlogan)
			yield return $"brand.slogan tiene {Manifest.Txt(Slogan).Length} caracteres y el tope es {MaxSlogan}";

		string logo = Manifest.Txt(Logo);
		if (logo.Length > 0)
		{
			if (logo.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(logo) || logo.Contains(':'))
				yield return $"brand.logo debe ser una ruta dentro del modulo, y '{logo}' no lo es";
			else if (!AllowedLogo.Contains(Path.GetExtension(logo), StringComparer.OrdinalIgnoreCase))
				yield return $"brand.logo es '{Path.GetExtension(logo)}' y solo se aceptan png, jpg y webp -- un svg ejecuta";
		}

		string color = Manifest.Txt(Color);
		if (color.Length > 0 && ValidColor(color) is null)
			yield return $"brand.color debe ser #rrggbb y es '{color}'";

		string banner = Manifest.Txt(Banner);
		if (banner.Length > 0)
		{
			if (!InsideBundle(banner))
				yield return $"brand.banner debe ser una ruta dentro del modulo, y '{banner}' no lo es";
			else if (!AllowedBanner.Contains(Path.GetExtension(banner), StringComparer.OrdinalIgnoreCase))
				yield return $"brand.banner es '{Path.GetExtension(banner)}' y solo se aceptan png, jpg y webp";
		}

		if (Font is not null)
		{
			if (Manifest.Txt(Font.Family).Length == 0)
				yield return "brand.font declara un archivo sin decir la familia (family)";
			else if (Manifest.Txt(Font.Family).Length > MaxFontFamily)
				yield return $"brand.font.family tiene {Manifest.Txt(Font.Family).Length} caracteres y el tope es {MaxFontFamily}";
			foreach ((string key, string file) in new[] { ("file", Manifest.Txt(Font.File)), ("bold", Manifest.Txt(Font.Bold)) })
			{
				if (file.Length == 0) { if (key == "file") yield return "brand.font no dice que archivo (file)"; continue; }
				if (!InsideBundle(file))
					yield return $"brand.font.{key} debe ser una ruta dentro del modulo, y '{file}' no lo es";
				else if (!AllowedFont.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
					yield return $"brand.font.{key} es '{Path.GetExtension(file)}' y solo se aceptan woff2, woff, ttf y otf";
			}
		}
	}
}

// The institution's typeface: a family name and the file(s) inside the bundle that carry it. A
// variable font covers every weight with one file; a static family gives `file` for regular and
// `bold` for the name.
public sealed record BrandFont
{
	[JsonPropertyName("family")] public string Family { get; init; } = "";
	[JsonPropertyName("file")] public string File { get; init; } = "";
	[JsonPropertyName("bold")] public string Bold { get; init; } = "";
}

// DE DONDE VIENE ESTE MODULO Y QUE VERSION ES. Es el grupo que le da sentido a la marca: un logo
// sin procedencia es un dibujo que cualquiera copia.
//
// AQUI VIVE LA RAZON SOCIAL, y no en 'institution', a proposito. Nadie dice "voy a Supermercado
// La Guaria S.A.": el nombre legal y la cedula no son con lo que una persona reconoce a nadie,
// son LO QUE SE COMPRUEBA. Por eso van al lado del dominio, que es la comprobacion.
public sealed record Publisher
{
	// EL UNICO CAMPO CONTRASTABLE DE TODO EL GRUPO, y por eso el mas valioso. Gnothi ya sabe de
	// que host bajo el bundle -lo OBSERVO, no se lo declaro nadie- asi que puede comparar. Es el
	// mismo patron que gnothi_verify_module: declarado contra real.
	[JsonPropertyName("domain")] public string Domain { get; init; } = "";

	[JsonPropertyName("legal")] public string Legal { get; init; } = "";
	[JsonPropertyName("taxId")] public string TaxId { get; init; } = "";
	[JsonPropertyName("jurisdiction")] public string Jurisdiction { get; init; } = "";

	// LA VERSION DEL MODULO, que no es la del formato: 'manifest' es el formato y esto es el
	// modulo. Sin esto, al reinstalar la persona puede saber que ya lo tiene y no si lo que baja
	// es mas nuevo, que es la pregunta que hace.
	[JsonPropertyName("version")] public string Version { get; init; } = "";
	[JsonPropertyName("released")] public string Released { get; init; } = "";
	[JsonPropertyName("license")] public string License { get; init; } = "";

	public const int MaxLegal = 64;

	// SI EL DOMINIO DECLARADO SE CORRESPONDE CON QUIEN SIRVIO EL BUNDLE. Un dominio vacio no
	// afirma nada, y no afirmar nada no es mentir: devuelve true y la pantalla simplemente no
	// tiene nada que mostrar.
	//
	// Se compara por sufijo de etiqueta -y no con Contains- para que 'delfos.cr' acepte
	// 'eventos.delfos.cr' y rechace 'delfos.cr.malo.com', que es justo el caso que un Contains
	// aprobaria.
	public bool MatchesServer(string servedBy)
	{
		string dom = Manifest.Txt(Domain);
		if (dom.Length == 0 || Manifest.Txt(servedBy).Length == 0) return true;
		return servedBy.Equals(dom, StringComparison.OrdinalIgnoreCase)
			|| servedBy.EndsWith("." + dom, StringComparison.OrdinalIgnoreCase);
	}

	public IEnumerable<string> Problems()
	{
		if (Manifest.Txt(Legal).Length > MaxLegal)
			yield return $"publisher.legal tiene {Manifest.Txt(Legal).Length} caracteres y el tope es {MaxLegal}";
		string dom = Manifest.Txt(Domain);
		if (dom.Contains('/') || dom.Contains(':'))
			yield return $"publisher.domain es un dominio, no una url: '{dom}'";
	}
}

// QUE ES ESTE MODULO, EN PALABRAS PARA LA PERSONA. Hoy lo unico que ella puede leer son las
// descripciones de las tools, que estan escritas para el modelo: densas, en tercera persona y
// hablando de "la persona" en vez de con ella. Este grupo es el primer texto del manifiesto que
// se escribe para quien lo va a leer.
public sealed record About
{
	// DE ACA SALE EL ICONO DE RESERVA cuando no hay logo, y eso es lo que le permite al Host
	// dejar de conocer instituciones por nombre. Lista cerrada: una categoria libre volveria a
	// ser un texto de la institucion en la pantalla, y para eso ya estan los otros campos.
	[JsonPropertyName("category")] public string Category { get; init; } = "";

	[JsonPropertyName("description")] public string Description { get; init; } = "";

	// EL ANTI-CLAIM, y es una idea del propio repo: el modulo de compras ya dice "este modulo no
	// juzga la alimentacion de nadie", pero lo dice ENTERRADO en la descripcion de una tool,
	// donde solo lo lee la IA. Aca lo lee la persona.
	[JsonPropertyName("doesNotDo")] public string DoesNotDo { get; init; } = "";

	[JsonPropertyName("locale")] public string Locale { get; init; } = "";

	// LA MONEDA, y es el hueco mas concreto que este grupo cierra. La unica tool de facturas
	// lleva escrito en su descripcion "EL TOTAL NO TIENE MONEDA: la institucion no la reporta en
	// sus eventos, asi que este modulo no la sabe y vos tampoco". Un campo lo cierra, y ademas
	// deja de gastar ese parrafo en tokens de la persona en cada turno.
	[JsonPropertyName("currency")] public string Currency { get; init; } = "";
	[JsonPropertyName("units")] public string Units { get; init; } = "";

	public const int MaxDescription = 180;
	public const int MaxDoesNotDo = 120;

	// LAS CATEGORIAS QUE EL HOST CONOCE. Crece cuando aparece un negocio que no encaja, y la
	// que no esta en la lista cae al icono generico -que es exactamente lo que pasa hoy con
	// TODOS, porque no hay categorias.
	public static readonly string[] Known =
	[
		"laboratorio", "salud", "supermercado", "comercio", "banca", "seguros",
		"combustible", "taller", "telecom", "educacion", "gobierno", "servicios",
	];

	public IEnumerable<string> Problems()
	{
		if (Manifest.Txt(Description).Length > MaxDescription)
			yield return $"about.description tiene {Manifest.Txt(Description).Length} caracteres y el tope es {MaxDescription}";
		if (Manifest.Txt(DoesNotDo).Length > MaxDoesNotDo)
			yield return $"about.doesNotDo tiene {Manifest.Txt(DoesNotDo).Length} caracteres y el tope es {MaxDoesNotDo}";
		string cat = Manifest.Txt(Category);
		if (cat.Length > 0 && !Known.Contains(cat, StringComparer.OrdinalIgnoreCase))
			yield return $"about.category '{cat}' no esta en la lista que el Host conoce, asi que cae al icono generico";
	}
}

// LO QUE LA INSTITUCION PROMETE SOBRE EL REGISTRO que va a vivir en el aparato de la persona.
//
// LOS TRES PRIMEROS SE FIJAN AL CREAR EL JOURNAL y pedir otro modo despues lanza excepcion. Los
// lee quien crea el journal, no el Host, y estan aca porque si la especificacion no lo dice
// alguna institucion va a descubrir a los dos anos que activar cifrado significa EMPEZAR UN
// JOURNAL NUEVO. Llegar tarde a estos tres es lo caro.
//
// LOS TRES SIGUIENTES son para la pantalla del alta: ella ya le dice a la persona DONDE va a
// vivir el registro, y no cuanto va a pesar, cada cuanto se actualiza ni por cuanto tiempo lo
// guardan -que es lo que pregunta antes de aceptar.
public sealed record JournalDeclaration
{
	[JsonPropertyName("compression")] public string Compression { get; init; } = "None";
	[JsonPropertyName("encryption")] public string Encryption { get; init; } = "None";
	[JsonPropertyName("maxFileSize")] public long MaxFileSize { get; init; }

	[JsonPropertyName("retention")] public string Retention { get; init; } = "";
	[JsonPropertyName("expectedSize")] public string ExpectedSize { get; init; } = "";
	[JsonPropertyName("updates")] public string Updates { get; init; } = "";

	// Y EL QUE PESA, que ya existia en Cargas/manifiesto.json antes de que el Host lo leyera:
	// true si el diario se puede volver a traer entero del publicador; false si lleva aportes de
	// la persona que no estan en ningun otro lado, y entonces borrarlo PIERDE DATOS y el Host no
	// puede vaciarlo sin costo.
	//
	// bool? y no bool: null es "la institucion no lo dijo", y eso no es lo mismo que "se puede
	// reconstruir". Un default optimista aca autorizaria a vaciar un diario irrecuperable.
	[JsonPropertyName("reconstruible")] public bool? Reconstruible { get; init; }
}

public sealed record Substrate
{
	[JsonPropertyName("range")] public string Range { get; init; } = "";
}

// WHERE THE CARGO MODULE'S SURFACE LIVES. A path relative to the module's own folder, e.g.
// "dominio/Examenes.App.dll". The Host loads exactly this assembly; its dependencies -the
// domain- are resolved next to it, and the engine is lent by the Host.
public sealed record Load
{
	[JsonPropertyName("entry")] public string Entry { get; init; } = "";
}

// What the module DECLARES it needs. The value of this is not informational: it is that the
// Host can check it against what the module actually asks for at run time. A module that
// declares 'network: false' and opens a socket is a module that lies, and that is detectable.
public sealed record Capabilities
{
	[JsonPropertyName("network")] public bool Network { get; init; }
	[JsonPropertyName("folder")] public string Folder { get; init; } = "own";
	[JsonPropertyName("speaker")] public bool Speaker { get; init; }
	[JsonPropertyName("microphone")] public bool Microphone { get; init; }

	public IEnumerable<string> Excesses()
	{
		if (Network) yield return "network";
		if (Speaker) yield return "speaker";
		if (Microphone) yield return "microphone";
		if (Folder != "own") yield return $"folder='{Folder}'";
	}
}

// A tool exactly as the module declares it. 'input' stays a list of simple parameters: this
// POC covers tools with no parameters and tools with text parameters, which is what the
// invoices module needs. The general case -an arbitrary input schema- calls for handling
// tools/call by hand instead of synthesizing a delegate, and is noted as pending.
public sealed record DeclaredTool
{
	[JsonPropertyName("name")] public string Name { get; init; } = "";
	[JsonPropertyName("title")] public string Title { get; init; } = "";
	[JsonPropertyName("description")] public string Description { get; init; } = "";
	[JsonPropertyName("readOnly")] public bool ReadOnly { get; init; } = true;
	[JsonPropertyName("input")] public IReadOnlyList<DeclaredParameter> Input { get; init; } = [];
}

public sealed record DeclaredParameter
{
	[JsonPropertyName("name")] public string Name { get; init; } = "";
	[JsonPropertyName("type")] public string Type { get; init; } = "string";
	[JsonPropertyName("description")] public string Description { get; init; } = "";

	// THE CLOSED SET OF VALUES, when the parameter is an enum. Optional: a text search or a free
	// number leaves it null and nothing changes. When present, ToolIndex puts it in the schema
	// as a real JSON Schema "enum", which is a far stronger signal to the model than the same
	// values buried in the prose of the description -- the exact gap the InputSchema comment
	// used to name as "a manifest decision". This is that decision: the values are DATA now.
	[JsonPropertyName("enum")] public IReadOnlyList<string>? Enum { get; init; }
}
