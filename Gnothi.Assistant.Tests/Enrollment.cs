using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Gnothi.Host;
using Gnothi.Movil;

namespace Gnothi.Tests;

// THE INSPECTION AND THE ENROLLMENT, OVER SYNTHETIC PACKAGES BUILT IN MEMORY.
//
// Why synthetic: the property under test is what Gnothi REFUSES and what it REPORTS, and the
// packages the repo ships are all well-formed -- none of them carries an engine in the wrong
// place, an executable, or a sample journal. The interesting packages are the ones nobody would
// publish, so they are built here, entry by entry.
//
// Why here and not in a phone: ModuleEnrollment is compiled into this suite from the same file
// the phones compile (see the csproj), so what runs here is the enrollment itself, not a copy.
internal static class Enrollment
{
	public static void Run(Action<string, bool, string> assert)
	{
		// ── the inspection ───────────────────────────────────────────────────────────────

		PackageInspection cargoOk = Inspect(Zip(
			("manifest.json", ManifestJson("demo", load: "dominio/Demo.App.dll")),
			("dominio/Demo.App.dll", "x"), ("dominio/Demo.Dominio.dll", "x")));
		assert("a clean cargo package passes as cargo",
			!cargoOk.Rejected && cargoOk.Form == PackageForm.Cargo && cargoOk.Files == 3 && cargoOk.EngineFiles == 0,
			Detail(cargoOk));

		PackageInspection cargoEngine = Inspect(Zip(
			("manifest.json", ManifestJson("demo", load: "dominio/Demo.App.dll")),
			("dominio/Demo.App.dll", "x"), ("dominio/Puppeteer.dll", "engine")));
		assert("a cargo package carrying the engine is refused",
			cargoEngine.Rejected && cargoEngine.Rejections.Any(r => r.Contains("engine")), Detail(cargoEngine));

		PackageInspection hostedOk = Inspect(HostedOk);
		assert("a hosted bundle passes, and its engine is counted instead of refused",
			!hostedOk.Rejected && hostedOk.Form == PackageForm.Hosted && hostedOk.EngineFiles == 2 && hostedOk.EngineBytes > 0,
			Detail(hostedOk));

		PackageInspection both = Inspect(Zip(
			("manifest.json", ManifestJson("demo", load: "dominio/Demo.App.dll")),
			("dominio/Demo.App.dll", "x"), ("web/index.html", "<html>")));
		assert("cargo AND hosted in one package is refused", both.Rejected, Detail(both));

		PackageInspection neither = Inspect(Zip(("manifest.json", ManifestJson("demo")), ("readme.txt", "?")));
		assert("a package with nothing to load is refused", neither.Rejected, Detail(neither));

		PackageInspection missingEntry = Inspect(Zip(
			("manifest.json", ManifestJson("demo", load: "dominio/Demo.App.dll")), ("dominio/Other.dll", "x")));
		assert("a cargo package whose load.entry is not inside is refused", missingEntry.Rejected, Detail(missingEntry));

		PackageInspection installation = Inspect(Zip(
			("manifest.json", ManifestJson("demo", load: "dominio/Demo.App.dll")),
			("dominio/Demo.App.dll", "x"), ("installation.json", "{}")));
		assert("a package that brings its own installation.json is refused",
			installation.Rejected && installation.Rejections.Any(r => r.Contains("installation.json")), Detail(installation));

		PackageInspection journal = Inspect(Zip(
			("manifest.json", ManifestJson("demo", load: "dominio/Demo.App.dll")),
			("dominio/Demo.App.dll", "x"), ("journal/meta.bin", "x")));
		assert("a package that ships a sample journal is refused", journal.Rejected, Detail(journal));

		PackageInspection slip = Inspect(Zip(
			("manifest.json", ManifestJson("demo", load: "dominio/Demo.App.dll")),
			("dominio/Demo.App.dll", "x"), ("../evil.txt", "x")));
		assert("an entry that points outside its folder is refused", slip.Rejected, Detail(slip));

		PackageInspection exe = Inspect(Zip(
			("manifest.json", ManifestJson("demo")), ("web/index.html", "<html>"), ("web/run.exe", "MZ")));
		assert("an executable is refused in a hosted bundle too", exe.Rejected, Detail(exe));

		PackageInspection badName = Inspect(Zip(("manifest.json", ManifestJson("../x")), ("web/index.html", "<html>")));
		assert("a module name that is a path is refused", badName.Rejected, Detail(badName));

		PackageInspection network = Inspect(HostedNetwork);
		assert("capabilities beyond a data module are reported, and the module installs OFF",
			!network.Rejected && network.Excesses.Contains("network") && !network.InstallEnabled, Detail(network));

		// ── the link, taken apart ────────────────────────────────────────────────────────

		const string wrapped = "https://go.gnothi.app/alta?bundle=https%3A%2F%2Fdelfos.cr%2Fexamenes.zip";
		EnrollmentLink plain = ModuleEnrollment.ParseLink(wrapped);
		assert("the institution's link is unwrapped to the bundle's address",
			plain.Bundle == "https://delfos.cr/examenes.zip" && plain.Seal is null, $"{plain}");

		EnrollmentLink sealed_ = ModuleEnrollment.ParseLink(wrapped + "&h=abc-DEF_123");
		assert("the seal travels with the link", sealed_.Bundle == "https://delfos.cr/examenes.zip" && sealed_.Seal == "abc-DEF_123", $"{sealed_}");

		assert("a bundle address pasted as is passes through, trimmed, with no seal",
			ModuleEnrollment.ParseLink(" https://delfos.cr/examenes.zip ") == new EnrollmentLink("https://delfos.cr/examenes.zip", null), "");
		assert("text that is not an address passes through, for the enrollment to refuse",
			ModuleEnrollment.ParseLink("hola").Bundle == "hola", "");

		// ── the origin check, over what an origin publishes ──────────────────────────────

		const string lists = "{\"gnothi\":1,\"institution\":\"Delfos\",\"modules\":[{\"module\":\"examenes\",\"bundle\":\"https://delfos.cr/examenes.zip\",\"seal\":\"x\"}]}";
		const string listsRelative = "{\"modules\":[{\"module\":\"examenes\",\"bundle\":\"/examenes.zip\"}]}";
		const string listsElsewhere = "{\"modules\":[{\"module\":\"examenes\",\"bundle\":\"https://evil.cr/examenes.zip\"}]}";

		assert("origin publishes the bundle and the manifest declares it: verified",
			ModuleEnrollment.VerifyOrigin(Delfos, lists, DelfosOrigin) == OriginCheck.Verified, "");
		assert("a relative bundle in the list resolves against the origin, and a trailing slash in the declaration is fine",
			ModuleEnrollment.VerifyOrigin(Delfos, listsRelative, DelfosOrigin + "/") == OriginCheck.Verified, "");
		assert("no well-known at the origin: not verified, and the sheet says the origin publishes nothing",
			ModuleEnrollment.VerifyOrigin(Delfos, null, DelfosOrigin) == OriginCheck.NoWellKnown, "");
		assert("a well-known that is not json counts as no well-known",
			ModuleEnrollment.VerifyOrigin(Delfos, "not json at all", DelfosOrigin) == OriginCheck.NoWellKnown, "");
		assert("a list that names the bundle on ANOTHER origin does not list this one",
			ModuleEnrollment.VerifyOrigin(Delfos, listsElsewhere, DelfosOrigin) == OriginCheck.NotListed, "");
		assert("listed, but the manifest declares no origin: it cannot be tied down",
			ModuleEnrollment.VerifyOrigin(Delfos, lists, "") == OriginCheck.NotDeclared, "");
		assert("the manifest claims an origin other than the one that served it: the loudest signal, before the list",
			ModuleEnrollment.VerifyOrigin(Delfos, lists, "https://evil.cr") == OriginCheck.DeclaresAnother, "");
		// A manifest from before the field existed: the generated reader leaves the property NULL,
		// not at its initializer -- measured here, and the check has to survive it.
		Manifest bare = JsonSerializer.Deserialize("{ \"module\": \"x\" }", HostJson.Default.Manifest)!;
		assert("a manifest without 'origin' does not break the check (declares nothing)",
			ModuleEnrollment.VerifyOrigin(Delfos, lists, bare.Origin) == OriginCheck.NotDeclared,
			bare.Origin is null ? "Origin read back as null" : $"Origin read back as '{bare.Origin}'");
		assert("a development origin with a port verifies the same way, over http",
			ModuleEnrollment.VerifyOrigin(new Uri("http://127.0.0.1:8096/examenes.zip"),
				"{\"modules\":[{\"bundle\":\"http://127.0.0.1:8096/examenes.zip\"}]}", "http://127.0.0.1:8096") == OriginCheck.Verified, "");

		// ── the enrollment over bytes ────────────────────────────────────────────────────

		string root = Path.Combine(Path.GetTempPath(), "gnothi-alta-" + Guid.NewGuid().ToString("N")[..8]);
		try
		{
			var enrolling = new ModuleEnrollment(Path.Combine(root, "Modulos"), Path.Combine(root, "journals"), _ => { });

			EnrollmentProposal p = enrolling.Examine(HostedOk, EnrollmentSource.PastedLink, Delfos);
			assert("examining bytes keeps the provenance for the sheet",
				p.ServedBy == "delfos.cr" && p.Provenance.Source == EnrollmentSource.PastedLink && !p.Provenance.MatchesLink,
				$"{p.Provenance}");
			assert("with nothing published by the origin, the module is not backed and the sheet is told why",
				p.Provenance.Origin == OriginCheck.NoWellKnown && p.Provenance.Secure && p.Provenance.DeclaredOrigin == DelfosOrigin,
				$"{p.Provenance}");
			assert("the proposal carries the inspection",
				p.Inspection.Form == PackageForm.Hosted && !p.Inspection.Rejected && p.Inspection.EngineFiles == 2, Detail(p.Inspection));
			assert("nothing is installed before she decides",
				Directory.Exists(p.Folder) && !Directory.Exists(Path.Combine(root, "Modulos", "demo")), p.Folder);

			enrolling.Discard(p);
			assert("discarding leaves nothing behind",
				!Directory.Exists(p.Folder) && !Directory.Exists(Path.Combine(root, "Modulos")), p.Folder);

			p = enrolling.Examine(HostedOk, EnrollmentSource.PastedLink, Delfos);
			string module = enrolling.Accept(p);
			Installation on = Installation.Read(Path.Combine(root, "Modulos", module, "installation.json"));
			assert("accepting writes the Host's installation, switched on",
				on.Enabled && on.Shell.Type == "webview" && on.GrantedFolder.EndsWith("demo"),
				$"enabled={on.Enabled} shell={on.Shell.Type} folder={on.GrantedFolder}");

			string red = enrolling.Accept(enrolling.Examine(HostedNetwork, EnrollmentSource.PastedLink, Delfos));
			Installation off = Installation.Read(Path.Combine(root, "Modulos", red, "installation.json"));
			assert("a module asking for network is installed switched OFF, the desktop's rule",
				!off.Enabled, $"enabled={off.Enabled}");

			try
			{
				enrolling.Examine(Zip(
					("manifest.json", ManifestJson("demo", load: "dominio/Demo.App.dll")),
					("dominio/Demo.App.dll", "x"), ("dominio/Puppeteer.dll", "engine")), EnrollmentSource.PastedLink, Delfos);
				assert("a refused package never becomes a proposal", false, "Examine returned a proposal");
			}
			catch (InvalidOperationException e)
			{
				assert("a refused package never becomes a proposal", e.Message.Contains("engine"), e.Message);
			}

			// ── the seal ─────────────────────────────────────────────────────────────────

			// The seal is over the manifest's bytes exactly as they sit in the zip: the same
			// string the package was built from, hashed the same way the institution's page does.
			string seal = ModuleEnrollment.SealOf(Encoding.UTF8.GetBytes(HostedOkManifest));
			assert("the seal is 16 bytes of SHA-256, base64url, no padding",
				seal.Length == 22 && !seal.Contains('=') && !seal.Contains('+') && !seal.Contains('/'), seal);

			EnrollmentProposal scanned = enrolling.Examine(HostedOk, EnrollmentSource.Qr, Delfos, seal, lists);
			assert("a code whose seal matches the package is marked as such, and remembered as a code",
				scanned.Provenance.MatchesLink && scanned.Provenance.Source == EnrollmentSource.Qr, $"{scanned.Provenance}");
			assert("served by an origin that publishes it, declaring that origin: backed, over TLS",
				scanned.Provenance.Origin == OriginCheck.Verified && scanned.Provenance.Secure, $"{scanned.Provenance}");
			enrolling.Discard(scanned);

			EnrollmentProposal elsewhere = enrolling.Examine(HostedOk, EnrollmentSource.Link, new Uri("http://127.0.0.1:8096/examenes.zip"));
			assert("a package claiming delfos.cr but served from 127.0.0.1 declares another origin, and the transport was not secure",
				elsewhere.Provenance.Origin == OriginCheck.DeclaresAnother && !elsewhere.Provenance.Secure, $"{elsewhere.Provenance}");
			enrolling.Discard(elsewhere);

			try
			{
				enrolling.Examine(HostedOk, EnrollmentSource.Qr, Delfos, "not-the-seal-of-this-one");
				assert("a package that no longer matches the link's seal is refused", false, "Examine returned a proposal");
			}
			catch (PackageSealMismatchException e)
			{
				assert("a package that no longer matches the link's seal is refused", e.Message.Contains(seal), e.Message);
			}
		}
		finally
		{
			try { Directory.Delete(root, recursive: true); } catch { /* temp, best effort */ }
		}
	}

	private const string DelfosOrigin = "https://delfos.cr";
	private static readonly Uri Delfos = new("https://delfos.cr/examenes.zip");

	// A hosted bundle as armar-bundle.ps1 would produce it: the app, its engine with fingerprints,
	// and a piece of the runtime, all under web/, and a manifest that declares its origin.
	private static readonly string HostedOkManifest = ManifestJson("demo", origin: DelfosOrigin);
	private static readonly byte[] HostedOk = Zip(
		("manifest.json", HostedOkManifest),
		("web/index.html", "<html></html>"),
		("web/_framework/Demo.App.abc123.wasm", "app"),
		("web/_framework/Puppeteer.hfyg9by0r8.wasm", "engine-engine"),
		("web/_framework/Choreography.t8tjytnusw.wasm", "engine"),
		("web/_framework/System.Runtime.k2j3h4.wasm", "bcl"));

	private static readonly byte[] HostedNetwork = Zip(
		("manifest.json", ManifestJson("demored", caps: "\"capabilities\": { \"network\": true, \"folder\": \"own\" }, ")),
		("web/index.html", "<html></html>"));

	private static string ManifestJson(string module, string? load = null, string caps = "", string? origin = null) =>
		"{ \"manifest\": \"1.0\", \"module\": \"" + module + "\", \"title\": \"Demo\", \"institution\": \"Demo (test)\", "
		+ (origin is null ? "" : "\"origin\": \"" + origin + "\", ")
		+ caps
		+ (load is null ? "" : "\"load\": { \"entry\": \"" + load + "\" }, ")
		+ "\"tools\": [ { \"name\": \"resumen\", \"title\": \"Resumen\", \"description\": \"d\", \"readOnly\": true, \"input\": [] } ] }";

	private static byte[] Zip(params (string path, string content)[] entries)
	{
		using var ms = new MemoryStream();
		using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
			foreach ((string path, string content) in entries)
			{
				using Stream s = zip.CreateEntry(path).Open();
				byte[] bytes = Encoding.UTF8.GetBytes(content);
				s.Write(bytes, 0, bytes.Length);
			}
		return ms.ToArray();
	}

	private static PackageInspection Inspect(byte[] zip)
	{
		using var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read);
		using var reader = new StreamReader(archive.GetEntry("manifest.json")!.Open());
		Manifest manifest = JsonSerializer.Deserialize(reader.ReadToEnd(), HostJson.Default.Manifest)!;
		return PackageInspector.Inspect(archive, manifest);
	}

	private static string Detail(PackageInspection i) =>
		$"form={i.Form} files={i.Files} engine={i.EngineFiles} excesses=[{string.Join(",", i.Excesses)}] rejections=[{string.Join(" | ", i.Rejections)}]";
}
