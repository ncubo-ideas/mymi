using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gnothi.Host;

// THE INSTALLATION BELONGS TO THE HOST. Deliberately kept apart from the manifest, and it is
// not cosmetic: these are two different authorities writing two different files.
//
//   manifest.json      the INSTITUTION writes it: who I am, what I need, what I offer.
//   installation.json  the HOST writes it:        where I put your binary, which folder I
//                                                granted you, and whether you are enabled.
//
// If the shell and the folder came in the manifest, the module would choose how it is run and
// where it writes. Both belong to the Host by definition: granting is an act of the granter.
public sealed record Installation
{
	// How the Host brings this module up. In this POC: child process + stdio.
	// Stage 2 changes ONLY this for a wasm runtime with --dir, and neither the manifest nor
	// the module notices. That is what we want to show: one artifact, two hostings.
	[JsonPropertyName("shell")] public Shell Shell { get; init; } = new();

	// The folder the Host GRANTED it. The module does not choose it.
	[JsonPropertyName("grantedFolder")] public string GrantedFolder { get; init; } = "";

	// The switch. It belongs to the Host: if the module implemented it, it would not be a
	// switch.
	[JsonPropertyName("enabled")] public bool Enabled { get; init; } = true;

	// Tools disabled one by one. Disabling the whole module is the coarse case; this is the
	// fine one, which is the one actually needed -"the total yes, the detail no".
	[JsonPropertyName("disabledTools")] public IReadOnlyList<string> DisabledTools { get; init; } = [];

	// WHO MAY SPEAK FOR THIS MODULE FROM OUTSIDE THE PROCESS. Only meaningful where a gate
	// lets another app in -today Gate 3 on Android, where a per-module "caratula" APK declares
	// the app functions to the OS and forwards here. Everywhere else it is inert.
	//
	// It lives in the INSTALLATION and not in the manifest for the same reason the granted
	// folder does: the module does not get to say who may impersonate it. Granting is an act
	// of the granter.
	//
	// ABSENT MEANS NOBODY. A module with no caratula declared cannot be reached from another
	// process at all. That default is deliberate: forgetting to declare it must fail closed,
	// never open.
	[JsonPropertyName("caratula")] public AllowedCaller? AllowedCaller { get; init; }

	// LO QUE EL HOST OBSERVO AL DAR EL ALTA, y por eso vive aca y no en el manifiesto: no lo
	// declara nadie, lo VIO Gnothi. Es la misma razon que la carpeta concedida y que la
	// caratula -- conceder, y atestiguar, son actos del que concede.
	//
	// PARA QUE HACE FALTA. La pantalla del alta puede comparar el dominio que la institucion
	// declara contra el host del que bajo el bundle, porque en ese momento tiene las dos cosas.
	// Despues NO: 'servedBy' era un dato del instante y se perdia, asi que el detalle del
	// modulo mostraba la procedencia sin poder contrastarla -- y estuvo un rato pintandola de
	// verde, que afirmaba una comprobacion que no habia ocurrido. Guardarlo convierte esa
	// franja en lo que decia ser.
	//
	// LAS TRES SON DEL MISMO ACTO y una vez escritas no cambian nunca. Eso las separa de
	// 'enabled' y 'disabledTools', que son lo unico mutable de este archivo: ReloadConsent
	// relee esas dos y no toca esto.
	[JsonPropertyName("provenance")] public Provenance? Provenance { get; init; }

	// Por HostJson y no por Deserialize<T>: el lector se genera al compilar, sin reflexion.
	// Ver HostJson.cs -- de eso depende que el Host pueda embeberse bajo NativeAOT.
	public static Installation Read(string path)
		=> JsonSerializer.Deserialize(File.ReadAllText(path), HostJson.Default.Installation)
		   ?? throw new InvalidDataException($"empty or unreadable installation: {path}");
}

// THE FRONT THAT IS ALLOWED TO SPEAK FOR A MODULE, as data. The Host core declares WHO is
// allowed; it does NOT verify -verifying means asking the platform's package manager, which
// is Android's business and would drag the OS into a core that has to stay portable.
//
// The split matters: the same declaration will serve iOS, where the check is a different API
// and the same three fields still name the same thing.
public sealed record AllowedCaller
{
	// The calling app's package name. Necessary and NOT sufficient: a package name is a
	// string anyone can claim by naming their app the same.
	[JsonPropertyName("package")] public string Package { get; init; } = "";

	// SHA-256 of the caller's signing certificate, hex, uppercase, no separators.
	//
	// THIS is the half that cannot be faked: the Host asks the system for the signature of the
	// package that actually made the call. An impostor can copy the package name; it cannot
	// produce the institution's signature without its private key.
	//
	// Empty means the check cannot pass. Same reasoning as an absent caratula: fail closed.
	[JsonPropertyName("signatureSha256")] public string SignatureSha256 { get; init; } = "";

	// What the front was allowed to publish when it was installed, so a later release that
	// silently grew tools does not inherit the grant. Optional today, and left here because
	// leaving room is cheaper than migrating the file later.
	[JsonPropertyName("releaseId")] public string ReleaseId { get; init; } = "";

	public bool IsDeclared => Package.Length > 0 && SignatureSha256.Length > 0;

	// Case and separators are normalized because every tool prints digests differently:
	// keytool uses colons, apksigner does not, and both mix case. Comparing raw would reject
	// a correct signature over punctuation.
	public bool Accepts(string package, string signatureSha256) =>
		IsDeclared
		&& string.Equals(Package, package, StringComparison.Ordinal)
		&& string.Equals(Normalize(SignatureSha256), Normalize(signatureSha256), StringComparison.OrdinalIgnoreCase);

	public static string Normalize(string digest) =>
		string.Concat(digest.Where(char.IsAsciiHexDigit)).ToUpperInvariant();
}

// DE DONDE VINO ESTE MODULO, atestiguado por el Host en el momento del alta.
public sealed record Provenance
{
	// EL HOST DEL QUE BAJO EL BUNDLE. Vacio es legitimo y significa "no vino de la red": un
	// .gmod importado de un archivo no tiene servidor, y en ese caso no hay nada que contrastar
	// -- que es distinto de contrastar y que no coincida.
	[JsonPropertyName("servedBy")] public string ServedBy { get; init; } = "";

	// Cuando la persona acepto. ISO 8601 en UTC.
	[JsonPropertyName("at")] public string At { get; init; } = "";

	// QUE VERSION ACEPTO, que es lo que permite decir despues "aceptaste la 2.4.0 y este bundle
	// declara la 2.5.0". Sin esto, reinstalar solo puede decir "ya tenes este modulo".
	[JsonPropertyName("version")] public string Version { get; init; } = "";

	// SI LO QUE LA INSTITUCION DECLARA SE CORRESPONDE CON QUIEN LE ENTREGO EL BUNDLE.
	//
	// Devuelve null -- NO true -- cuando no hay con que comparar, y esa distincion es el punto
	// entero: "coincide", "no coincide" y "no se puede saber" son tres cosas, y la pantalla
	// tiene que poder decir la tercera en vez de pintar un visto bueno.
	public bool? Matches(Publisher? publisher)
	{
		if (ServedBy.Length == 0) return null;
		if (publisher is null || Manifest.Txt(publisher.Domain).Length == 0) return null;
		return publisher.MatchesServer(ServedBy);
	}
}

public sealed record Shell
{
	[JsonPropertyName("type")] public string Type { get; init; } = "stdio";
	[JsonPropertyName("command")] public string Command { get; init; } = "";
	[JsonPropertyName("arguments")] public IReadOnlyList<string> Arguments { get; init; } = [];
}
