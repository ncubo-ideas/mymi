using System.Security.Cryptography;

namespace Gnothi.Host;

// WHAT A PACKAGE IS, NEXT TO WHAT IS ALREADY INSTALLED UNDER ITS NAME (NotasGnothiHost/44).
//
// Until 2026-09-09 the enrollment knew one thing about an incoming package with a known name: that
// the name was taken. The sheet said "you already have this module" and accepting replaced the app,
// rewrote the installation from the new manifest -consent reset, the fixed endpoint moved to
// wherever the new manifest pointed- and kept nothing to go back to. This is the comparison that
// was missing, shared by the phone's enrollment and the desktop installer so the two say the same.
//
// A MODULE'S IDENTITY HAS TWO PARTS, and both already travel: its NAME (manifest.module) and WHO
// ANSWERS FOR IT -the key it was accepted with (the pin) or the origin that publishes it-. Two more
// things say WHICH package it is: the SEAL of the manifest and the VERSION the publisher declares.
// From those four the cases separate themselves: the same package, a newer one, an older one, the
// same version over other content, and the one that is not an update at all -another institution
// using the same name-, which is refused before anything is shown.
//
// THE PLAN LISTS THE CHANGE, NOT THE MODULE: which tools appeared or went, which capabilities are
// new, whether the publisher's endpoint, the declared origin, the journal promise or the substrate
// moved. That is what the sheet shows, and what the installation keeps or refuses to move.

public enum UpdateKind
{
	// Nothing installed under this name: a first install.
	Fresh,

	// The very same package -same seal- as the one installed. Reinstalling only repairs the app.
	Same,

	// A newer version of the same module, by the same institution.
	Update,

	// An older version than the one installed: going back. Allowed, said out loud.
	Downgrade,

	// The same version number over different content: the institution changed the package
	// without saying so, or does not version at all.
	SameVersionOtherContent,

	// The same name, another institution. Not an update: a collision, refused.
	Collision,
}

public sealed record UpdatePlan
{
	public UpdateKind Kind { get; init; }

	// Both sides declare a version that can be compared. When they do not, the direction is not
	// known and the sheet says the institution does not version its packages.
	public bool Versioned { get; init; }
	public string InstalledVersion { get; init; } = "";
	public string IncomingVersion { get; init; } = "";

	public string InstalledInstitution { get; init; } = "";
	public string IncomingInstitution { get; init; } = "";

	public IReadOnlyList<DeclaredTool> NewTools { get; init; } = [];
	public IReadOnlyList<DeclaredTool> RemovedTools { get; init; } = [];

	// Capabilities the incoming package asks for and the installed one did not ("network"...).
	public IReadOnlyList<string> NewCapabilities { get; init; } = [];

	// The publisher's endpoint each side would fix, in the form the sheet shows. Gnothi fixed the
	// installed one at the first install; moving it needs the person's own yes.
	public string InstalledPublisher { get; init; } = "";
	public string IncomingPublisher { get; init; } = "";
	public bool PublisherChanged => !string.Equals(InstalledPublisher, IncomingPublisher, StringComparison.Ordinal);

	public string InstalledOrigin { get; init; } = "";
	public string IncomingOrigin { get; init; } = "";
	public bool OriginChanged => !string.Equals(InstalledOrigin, IncomingOrigin, StringComparison.OrdinalIgnoreCase);

	// What the institution promises about the journal: retention, expected size, refresh.
	public bool JournalChanged { get; init; }

	public string InstalledSubstrate { get; init; } = "";
	public string IncomingSubstrate { get; init; } = "";
	public bool SubstrateChanged => !string.Equals(InstalledSubstrate, IncomingSubstrate, StringComparison.Ordinal);

	// Anything the sheet would have to list. False for the same package, and for an update whose
	// only change is inside the app.
	public bool HasVisibleChanges => NewTools.Count > 0 || RemovedTools.Count > 0 || NewCapabilities.Count > 0
		|| PublisherChanged || OriginChanged || JournalChanged || SubstrateChanged;

	public static UpdatePlan Fresh(Manifest incoming) => new()
	{
		Kind = UpdateKind.Fresh,
		IncomingVersion = incoming.Publisher?.Version ?? "",
		IncomingInstitution = incoming.Institution ?? "",
		IncomingPublisher = PublisherOf(incoming),
		IncomingOrigin = OriginOf(incoming),
		IncomingSubstrate = incoming.Substrate?.Range ?? "",
		NewTools = incoming.Tools,
		NewCapabilities = [.. incoming.Capabilities?.Excesses() ?? []],
	};

	// THE COMPARISON. `pin` is the key the installed module was accepted with, `incomingKey` the
	// public key the incoming package is signed with when it is; both may be null.
	public static UpdatePlan Compare(Manifest? installed, string? installedSeal, PackageSigner? pin, Manifest incoming, string incomingSeal, string? incomingKey)
	{
		if (installed is null) return Fresh(incoming);

		string installedVersion = installed.Publisher?.Version ?? "";
		string incomingVersion = incoming.Publisher?.Version ?? "";
		Version? was = ParseVersion(installedVersion);
		Version? now = ParseVersion(incomingVersion);

		var plan = new UpdatePlan
		{
			Kind = UpdateKind.Update,
			Versioned = was is not null && now is not null,
			InstalledVersion = installedVersion,
			IncomingVersion = incomingVersion,
			InstalledInstitution = installed.Institution ?? "",
			IncomingInstitution = incoming.Institution ?? "",
			NewTools = [.. incoming.Tools.Where(t => !installed.Tools.Any(i => string.Equals(i.Name, t.Name, StringComparison.Ordinal)))],
			RemovedTools = [.. installed.Tools.Where(i => !incoming.Tools.Any(t => string.Equals(t.Name, i.Name, StringComparison.Ordinal)))],
			NewCapabilities = [.. (incoming.Capabilities?.Excesses() ?? []).Except(installed.Capabilities?.Excesses() ?? [], StringComparer.Ordinal)],
			InstalledPublisher = PublisherOf(installed),
			IncomingPublisher = PublisherOf(incoming),
			InstalledOrigin = OriginOf(installed),
			IncomingOrigin = OriginOf(incoming),
			JournalChanged = !string.Equals(JournalPromise(installed), JournalPromise(incoming), StringComparison.Ordinal),
			InstalledSubstrate = installed.Substrate?.Range ?? "",
			IncomingSubstrate = incoming.Substrate?.Range ?? "",
		};

		// WHO ANSWERS FOR IT, first. The same key as the pin settles it; failing that, the same
		// institution by name; failing that, the same declared origin. Another institution under
		// the same name is a collision, whatever the versions say.
		if (!SameInstitution(installed, pin, incoming, incomingKey))
			return plan with { Kind = UpdateKind.Collision };

		if (string.Equals(installedSeal, incomingSeal, StringComparison.Ordinal))
			return plan with { Kind = UpdateKind.Same };

		if (was is null || now is null)
			return plan; // different content, direction unknown: an update the sheet cannot date

		int order = now.CompareTo(was);
		return plan with
		{
			Kind = order > 0 ? UpdateKind.Update
				: order < 0 ? UpdateKind.Downgrade
				: UpdateKind.SameVersionOtherContent,
		};
	}

	private static bool SameInstitution(Manifest installed, PackageSigner? pin, Manifest incoming, string? incomingKey)
	{
		if (pin is not null && incomingKey is not null && string.Equals(pin.Public, incomingKey, StringComparison.Ordinal)) return true;

		string a = (installed.Institution ?? "").Trim(), b = (incoming.Institution ?? "").Trim();
		if (a.Length > 0 && b.Length > 0 && string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return true;

		string oa = OriginOf(installed), ob = OriginOf(incoming);
		if (oa.Length > 0 && ob.Length > 0 && string.Equals(oa, ob, StringComparison.OrdinalIgnoreCase)) return true;

		// Neither side says who it is, or they say different things: a package that names no
		// institution and no origin cannot claim to be the update of anything but itself.
		return a.Length == 0 && b.Length == 0 && oa.Length == 0 && ob.Length == 0;
	}

	// "2.4.0", "2.4", "v2.4.0-beta": the numeric dotted prefix, padded to four parts.
	public static Version? ParseVersion(string? declared)
	{
		if (string.IsNullOrWhiteSpace(declared)) return null;
		string s = declared.Trim().TrimStart('v', 'V');
		int end = 0;
		while (end < s.Length && (char.IsDigit(s[end]) || s[end] == '.')) end++;
		string[] parts = s[..end].Split('.', StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0) return null;

		var numbers = new int[4];
		for (int i = 0; i < Math.Min(4, parts.Length); i++)
			if (!int.TryParse(parts[i], out numbers[i])) return null;
		return new Version(numbers[0], numbers[1], numbers[2], numbers[3]);
	}

	// The publisher's endpoint as the sheet shows it and the installation fixes it.
	public static string PublisherOf(Manifest m)
	{
		Publicador? p = m.Publicador;
		return p is null || p.Eventos.Length == 0 ? "" : $"{p.Eventos}{p.Recurso}/{p.Id}";
	}

	// The declared origin, normalised to scheme://host[:port], or "" when it declares none.
	public static string OriginOf(Manifest m)
	{
		string declared = m.Origin ?? "";
		if (!Uri.TryCreate(declared.Trim(), UriKind.Absolute, out Uri? u)) return "";
		return u.IsDefaultPort ? $"{u.Scheme}://{u.Host}" : $"{u.Scheme}://{u.Host}:{u.Port}";
	}

	private static string JournalPromise(Manifest m)
		=> $"{m.Journal?.Retention ?? ""}|{m.Journal?.ExpectedSize ?? ""}|{m.Journal?.Updates ?? ""}";

	// One line for logs and the desktop's message: what this package is next to the installed one.
	// A CLOSED SET OF SEVEN OUTCOMES, and that is why it GOES TO THE CATALOGUE. It is not a datum
	// of anybody record nor an institution voice: it is the Host vocabulary, which is exactly what
	// rule 4 says to translate. What passes straight through inside are the version numbers and the
	// institution names, which ARE theirs.
	//
	// The `_ =>` keeps the enum name on purpose: if a new Kind appears with no sentence, let it
	// show raw on the screen instead of hiding in an empty string.
	public string Describe() => Kind switch
	{
		UpdateKind.Fresh => Texts.T("actualizacion.primera"),
		UpdateKind.Same => Texts.T("actualizacion.misma", ("version", IncomingVersion ?? "")),
		UpdateKind.Update => (Versioned
			? Texts.T("actualizacion.sube", ("instalada", InstalledVersion ?? ""), ("entrante", IncomingVersion ?? ""))
			: Texts.T("actualizacion.sube.sin-version")) + Changes(),
		UpdateKind.Downgrade => Texts.T("actualizacion.baja", ("instalada", InstalledVersion ?? ""), ("entrante", IncomingVersion ?? "")) + Changes(),
		UpdateKind.SameVersionOtherContent => Texts.T("actualizacion.otro-contenido", ("version", IncomingVersion ?? "")) + Changes(),
		UpdateKind.Collision => Texts.T("actualizacion.colision", ("entrante", IncomingInstitution ?? ""), ("instalada", InstalledInstitution ?? "")),
		_ => Kind.ToString(),
	};

	private string Changes()
	{
		List<string> parts = [];
		if (NewTools.Count > 0)
			parts.Add(NewTools.Count == 1 ? Texts.T("actualizacion.cambio.nuevas.una")
				: Texts.T("actualizacion.cambio.nuevas.varias", ("n", NewTools.Count.ToString())));
		if (RemovedTools.Count > 0)
			parts.Add(RemovedTools.Count == 1 ? Texts.T("actualizacion.cambio.quitadas.una")
				: Texts.T("actualizacion.cambio.quitadas.varias", ("n", RemovedTools.Count.ToString())));
		// THE CAPABILITIES PASS STRAIGHT THROUGH: they are protocol names, the same ones the
		// manifest declares. Translating them would break the correspondence with what the institution wrote.
		if (NewCapabilities.Count > 0)
			parts.Add(Texts.T("actualizacion.cambio.pide", ("lista", string.Join(", ", NewCapabilities))));
		if (PublisherChanged) parts.Add(Texts.T("actualizacion.cambio.editor"));
		if (OriginChanged) parts.Add(Texts.T("actualizacion.cambio.origen"));
		if (JournalChanged) parts.Add(Texts.T("actualizacion.cambio.journal"));
		if (SubstrateChanged) parts.Add(Texts.T("actualizacion.cambio.sustrato"));
		return parts.Count == 0 ? "" : " (" + string.Join(", ", parts) + ")";
	}
}

// THE SEAL OF A MANIFEST: the first 16 bytes of the SHA-256 of manifest.json exactly as it sits in
// the package, base64url without padding. The institution's page publishes it next to each module
// and the link carries it as &h=; here it is also what tells two packages of the same version
// apart. Lives in the Host because the desktop installer and the phone's enrollment both need it.
public static class PackageSeal
{
	public static string Of(byte[] manifestBytes)
		=> Convert.ToBase64String(SHA256.HashData(manifestBytes), 0, 16).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
