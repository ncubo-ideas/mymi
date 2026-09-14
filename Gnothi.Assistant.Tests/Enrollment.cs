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

		// THE HYBRID: a face in web/ and a surface in dominio/, with the engine lent by the
		// Host. This used to be the refusal "a module is cargo or hosted, not both".
		PackageInspection hybridOk = Inspect(Zip(
			("manifest.json", ManifestJson("demo", load: "dominio/Demo.App.dll")),
			("dominio/Demo.App.dll", "x"), ("dominio/Demo.Dominio.dll", "x"), ("web/index.html", "<html>")));
		assert("a face in web/ plus a surface in dominio/ passes as hybrid",
			!hybridOk.Rejected && hybridOk.Form == PackageForm.Hybrid && hybridOk.EngineFiles == 0,
			Detail(hybridOk));

		// The promise of the form, and the only rule that is stricter than hosted: a hybrid does
		// not carry the engine ANYWHERE -- not even inside web/, where hosted is allowed to.
		PackageInspection hybridEngine = Inspect(Zip(
			("manifest.json", ManifestJson("demo", load: "dominio/Demo.App.dll")),
			("dominio/Demo.App.dll", "x"), ("web/index.html", "<html>"), ("web/_framework/Puppeteer.wasm", "engine")));
		assert("a hybrid carrying the engine inside web/ is refused",
			hybridEngine.Rejected && hybridEngine.Rejections.Any(r => r.Contains("engine")), Detail(hybridEngine));

		// A hybrid loads from dominio/, and nowhere else outside web/.
		PackageInspection hybridStray = Inspect(Zip(
			("manifest.json", ManifestJson("demo", load: "dominio/Demo.App.dll")),
			("dominio/Demo.App.dll", "x"), ("web/index.html", "<html>"), ("suelto/Otro.dll", "x")));
		assert("a hybrid with a dll outside web/ and dominio/ is refused",
			hybridStray.Rejected && hybridStray.Rejections.Any(r => r.Contains("hybrid module loads nothing")),
			Detail(hybridStray));

		// And a hybrid still has to satisfy BOTH halves: the two checks stopped being
		// alternatives when the form stopped being a binary.
		PackageInspection hybridNoIndex = Inspect(Zip(
			("manifest.json", ManifestJson("demo", load: "dominio/Demo.App.dll")),
			("dominio/Demo.App.dll", "x"), ("web/app.js", "//")));
		assert("a hybrid whose web/ has no index.html is refused",
			hybridNoIndex.Rejected && hybridNoIndex.Rejections.Any(r => r.Contains("index.html")),
			Detail(hybridNoIndex));

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

		const string wrapped = "https://go.gnothi.app/alta?bundle=https%3A%2F%2Fdelfos.cr%2Fexamenes.gmod";
		EnrollmentLink plain = ModuleEnrollment.ParseLink(wrapped);
		assert("the institution's link is unwrapped to the bundle's address",
			plain.Bundle == "https://delfos.cr/examenes.gmod" && plain.Seal is null, $"{plain}");

		EnrollmentLink sealed_ = ModuleEnrollment.ParseLink(wrapped + "&h=abc-DEF_123");
		assert("the seal travels with the link", sealed_.Bundle == "https://delfos.cr/examenes.gmod" && sealed_.Seal == "abc-DEF_123", $"{sealed_}");

		assert("a bundle address pasted as is passes through, trimmed, with no seal",
			ModuleEnrollment.ParseLink(" https://delfos.cr/examenes.gmod ") == new EnrollmentLink("https://delfos.cr/examenes.gmod", null), "");
		assert("text that is not an address passes through, for the enrollment to refuse",
			ModuleEnrollment.ParseLink("hola").Bundle == "hola", "");

		// ── the origin check, over what an origin publishes ──────────────────────────────

		const string lists = "{\"gnothi\":1,\"institution\":\"Delfos\",\"modules\":[{\"module\":\"examenes\",\"bundle\":\"https://delfos.cr/examenes.gmod\",\"seal\":\"x\"}]}";
		const string listsRelative = "{\"modules\":[{\"module\":\"examenes\",\"bundle\":\"/examenes.gmod\"}]}";
		const string listsElsewhere = "{\"modules\":[{\"module\":\"examenes\",\"bundle\":\"https://evil.cr/examenes.gmod\"}]}";

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
			ModuleEnrollment.VerifyOrigin(new Uri("http://127.0.0.1:8096/examenes.gmod"),
				"{\"modules\":[{\"bundle\":\"http://127.0.0.1:8096/examenes.gmod\"}]}", "http://127.0.0.1:8096") == OriginCheck.Verified, "");

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

			// The middle road (NotasGnothiHost/43 §8): a package that claims one origin and was served by
			// another is a contradiction, and a contradiction is refused before any sheet exists -so
			// nothing is unpacked: the temporary folders of the enrollment are the same before and after.
			int stagingBefore = Directory.GetDirectories(Path.GetTempPath(), "alta-*").Length;
			try
			{
				enrolling.Examine(HostedOk, EnrollmentSource.Link, new Uri("http://127.0.0.1:8096/examenes.gmod"));
				assert("a package claiming delfos.cr but served from 127.0.0.1 is refused: it declares another origin", false, "Examine returned a proposal");
			}
			catch (PackageNotBackedException e)
			{
				assert("a package claiming delfos.cr but served from 127.0.0.1 is refused: it declares another origin",
					e.Origin == OriginCheck.DeclaresAnother && e.Message.Contains("127.0.0.1") && e.Message.Contains("delfos.cr"), e.Message);
			}
			assert("a refused package is never unpacked: no temporary folder appears",
				Directory.GetDirectories(Path.GetTempPath(), "alta-*").Length == stagingBefore, "");

			// The other contradiction: the origin publishes its list and this bundle is not on it.
			try
			{
				enrolling.Examine(HostedOk, EnrollmentSource.Link, Delfos, null, listsElsewhere);
				assert("a package the serving origin does not list is refused", false, "Examine returned a proposal");
			}
			catch (PackageNotBackedException e)
			{
				assert("a package the serving origin does not list is refused", e.Origin == OriginCheck.NotListed && e.Message.Contains("delfos.cr"), e.Message);
			}

			try
			{
				enrolling.Examine(HostedOk, EnrollmentSource.Qr, Delfos, "not-the-seal-of-this-one");
				assert("a package that no longer matches the link's seal is refused", false, "Examine returned a proposal");
			}
			catch (PackageSealMismatchException e)
			{
				assert("a package that no longer matches the link's seal is refused", e.Message.Contains(seal), e.Message);
			}

			// ── a file: no address, but a claimed origin that publishes seals ────────────────

			string listsThisSeal = "{\"modules\":[{\"module\":\"demo\",\"bundle\":\"/demo.gmod\",\"seal\":\"" + seal + "\"}]}";
			assert("an origin that publishes exactly this manifest's seal backs the file",
				ModuleEnrollment.VerifyBySeal(seal, listsThisSeal) == OriginCheck.Verified, "");
			assert("an origin that publishes another seal does not",
				ModuleEnrollment.VerifyBySeal(seal, lists) == OriginCheck.NotListed, "");
			assert("no well-known: nothing to compare the seal with",
				ModuleEnrollment.VerifyBySeal(seal, null) == OriginCheck.NoWellKnown, "");

			Uri? asked = null;
			EnrollmentProposal file = enrolling.ExamineFile(HostedOk, "examenes.gmod", CancellationToken.None, null,
				(at, _) => { asked = at; return Task.FromResult<string?>(listsThisSeal); }).GetAwaiter().GetResult();
			assert("a file is asked about at the origin its manifest claims, and comes back backed",
				asked?.ToString() == "https://delfos.cr/" && file.Provenance.Origin == OriginCheck.Verified && file.Provenance.Secure
					&& file.Provenance.Source == EnrollmentSource.File && file.ServedBy == "examenes.gmod" && !file.Provenance.MatchesLink,
				$"asked={asked} {file.Provenance}");
			enrolling.Discard(file);

			// A file whose declared origin publishes another version: the origin spoke and does not
			// recognise this one, so it is refused -the institution keeps old seals listed if it wants
			// old files to keep entering.
			try
			{
				enrolling.ExamineFile(HostedOk, "examenes.gmod", CancellationToken.None, null,
					(_, _) => Task.FromResult<string?>(lists)).GetAwaiter().GetResult();
				assert("a file whose origin publishes another version is refused", false, "ExamineFile returned a proposal");
			}
			catch (PackageNotBackedException e)
			{
				assert("a file whose origin publishes another version is refused", e.Origin == OriginCheck.NotListed && e.Message.Contains("delfos.cr"), e.Message);
			}

			bool askedAnyone = false;
			EnrollmentProposal orphan = enrolling.ExamineFile(HostedNetwork, "demored.gmod", CancellationToken.None, null,
				(_, _) => { askedAnyone = true; return Task.FromResult<string?>(listsThisSeal); }).GetAwaiter().GetResult();
			assert("a file whose manifest declares no origin asks nobody, and is not backed",
				!askedAnyone && orphan.Provenance.Origin == OriginCheck.NotDeclared && !orphan.Provenance.Secure, $"{orphan.Provenance}");
			enrolling.Discard(orphan);

			// ── several files at once: the quick look behind the list ────────────────────────

			PackagePeek peeked = ModuleEnrollment.Peek(HostedOk, "examenes.gmod");
			assert("a quick look at a module names it, counts it and says it is unsigned, without asking anyone",
				peeked.IsModule && peeked.Installable && peeked.Manifest!.Module == "demo" && peeked.Inspection!.Form == PackageForm.Hosted
					&& peeked.Signature == SignatureState.Unsigned && peeked.Problem is null && peeked.Bytes == HostedOk.LongLength && peeked.FileName == "examenes.gmod",
				$"{peeked}");
			PackagePeek notAModule = ModuleEnrollment.Peek(Zip(("fotos/playa.jpg", "jpeg"), ("fotos/cena.jpg", "jpeg")), "fotos-vacaciones.zip");
			assert("a quick look at a zip that is not a module says so, and keeps the file's name",
				!notAModule.IsModule && !notAModule.Installable && notAModule.Problem is not null && notAModule.FileName == "fotos-vacaciones.zip", $"{notAModule.Problem?.Message}");
			PackagePeek garbage = ModuleEnrollment.Peek([1, 2, 3, 4], "roto.gmod");
			assert("a quick look at bytes that are not a zip does not throw",
				!garbage.IsModule && garbage.Problem is not null, "");
			PackagePeek engineInCargo = ModuleEnrollment.Peek(Zip(("manifest.json", ManifestJson("demo", load: "dominio/Demo.App.dll")), ("dominio/Demo.App.dll", "x"), ("dominio/Puppeteer.dll", "engine")), "demo.gmod");
			assert("a quick look at a module the inspection rejects keeps its name and says it would not install",
				engineInCargo.IsModule && !engineInCargo.Installable && engineInCargo.Inspection!.Rejected, $"{engineInCargo.Inspection?.Rejections.Count}");

			// ── updates: what a package is next to what is installed under its name ─────────

			static string Versioned(string module, string version, string publisher, string institution, string? origin, params string[] tools) =>
				"{ \"manifest\": \"1.1\", \"module\": \"" + module + "\", \"title\": \"Demo\", \"institution\": \"" + institution + "\", "
				+ (origin is null ? "" : "\"origin\": \"" + origin + "\", ")
				+ "\"publisher\": { \"domain\": \"delfos.cr\", \"version\": \"" + version + "\" }, "
				+ "\"publicador\": { \"eventos\": \"" + publisher + "\", \"recurso\": \"expediente\", \"id\": \"1\" }, "
				+ "\"tools\": [ " + string.Join(", ", tools.Select(t => "{ \"name\": \"" + t + "\", \"title\": \"" + t + "\", \"description\": \"d\", \"readOnly\": true, \"input\": [] }")) + " ] }";

			byte[] v1 = Zip(("manifest.json", Versioned("actualizable", "1.0.0", "https://delfos.cr/eventos/", "Delfos", DelfosOrigin, "resumen", "viejo")), ("web/index.html", "<html>v1</html>"));
			byte[] v2 = Zip(("manifest.json", Versioned("actualizable", "1.1.0", "https://delfos.cr/eventos2/", "Delfos", DelfosOrigin, "resumen", "nuevo")), ("web/index.html", "<html>v2</html>"));
			byte[] v2b = Zip(("manifest.json", Versioned("actualizable", "1.1.0", "https://delfos.cr/eventos2/", "Delfos", DelfosOrigin, "resumen", "nuevo", "extra")), ("web/index.html", "<html>v2b</html>"));
			byte[] ajeno = Zip(("manifest.json", Versioned("actualizable", "3.0.0", "https://otro.cr/eventos/", "Otro Laboratorio", null, "resumen")), ("web/index.html", "<html>x</html>"));

			EnrollmentProposal first = enrolling.Examine(v1, EnrollmentSource.Link, Delfos);
			assert("a first install plans as fresh", first.Update.Kind == UpdateKind.Fresh && !first.AlreadyThere, first.Update.Describe());
			enrolling.Accept(first);

			// She switches one query off, as the detail lets her.
			string updatable = Path.Combine(root, "Modulos", "actualizable", "installation.json");
			Installation before1 = Installation.Read(updatable);
			File.WriteAllText(updatable, JsonSerializer.Serialize(before1 with { DisabledTools = ["resumen"] }, HostJson.Legible.Installation));

			EnrollmentProposal same = enrolling.Examine(v1, EnrollmentSource.Link, Delfos);
			assert("the very same package again is Same, and already there", same.Update.Kind == UpdateKind.Same && same.AlreadyThere, same.Update.Describe());
			enrolling.Discard(same);

			EnrollmentProposal up = enrolling.Examine(v2, EnrollmentSource.Link, Delfos);
			assert("a newer version is an update, and the plan lists the change",
				up.Update.Kind == UpdateKind.Update && up.Update.Versioned && up.Update.InstalledVersion == "1.0.0" && up.Update.IncomingVersion == "1.1.0"
					&& up.Update.NewTools.Select(t => t.Name).SequenceEqual(["nuevo"]) && up.Update.RemovedTools.Select(t => t.Name).SequenceEqual(["viejo"])
					&& up.Update.PublisherChanged && up.Update.InstalledPublisher.Contains("eventos/") && up.Update.IncomingPublisher.Contains("eventos2/")
					&& !up.Update.OriginChanged && up.Update.HasVisibleChanges,
				up.Update.Describe());
			enrolling.Accept(up);
			Installation after1 = Installation.Read(updatable);
			assert("an update keeps her switches for the tools that survive and drops the ones that went",
				after1.Enabled && after1.DisabledTools.SequenceEqual(["resumen"]), string.Join(",", after1.DisabledTools));
			assert("the publisher's endpoint Gnothi fixed stays unless she accepts the new one",
				after1.Shell.Arguments.Count > 1 && after1.Shell.Arguments[1].StartsWith("https://delfos.cr/eventos/|"), string.Join(" ", after1.Shell.Arguments));

			EnrollmentProposal up2 = enrolling.Examine(v2b, EnrollmentSource.Link, Delfos);
			assert("the same version over other content is said as such",
				up2.Update.Kind == UpdateKind.SameVersionOtherContent && up2.Update.NewTools.Select(t => t.Name).SequenceEqual(["extra"]), up2.Update.Describe());
			enrolling.Accept(up2, acceptNewPublisher: true);
			Installation after2 = Installation.Read(updatable);
			assert("with her yes the endpoint moves", after2.Shell.Arguments[1].StartsWith("https://delfos.cr/eventos2/|"), after2.Shell.Arguments[1]);

			EnrollmentProposal back = enrolling.Examine(v1, EnrollmentSource.Link, Delfos);
			assert("an older version is a downgrade, allowed and said",
				back.Update.Kind == UpdateKind.Downgrade && back.Update.RemovedTools.Count == 2 && back.Update.NewTools.Select(t => t.Name).SequenceEqual(["viejo"]), back.Update.Describe());
			enrolling.Discard(back);

			try
			{
				enrolling.Examine(ajeno, EnrollmentSource.Link, Delfos);
				assert("another institution's package under the same name is refused", false, "Examine returned a proposal");
			}
			catch (PackageCollisionException e)
			{
				assert("another institution's package under the same name is refused",
					e.Module == "actualizable" && e.IncomingInstitution == "Otro Laboratorio" && e.InstalledInstitution == "Delfos", e.Message);
			}

			// The comparison alone, with no files.
			assert("versions parse padded and tolerate a v prefix; garbage is null",
				UpdatePlan.ParseVersion("v2.4") == new Version(2, 4, 0, 0) && UpdatePlan.ParseVersion("2.4.0-beta") == new Version(2, 4, 0, 0)
					&& UpdatePlan.ParseVersion("") is null && UpdatePlan.ParseVersion("beta") is null, "");
			assert("unversioned sides with different content are an update of unknown direction",
				UpdatePlan.Compare(new Manifest { Module = "x", Institution = "A" }, "s1", null, new Manifest { Module = "x", Institution = "A" }, "s2", null) is { Kind: UpdateKind.Update, Versioned: false }, "");
			assert("the pinned key settles who it is, whatever the names say",
				UpdatePlan.Compare(new Manifest { Module = "x", Institution = "A" }, "s1", new PackageSigner { Public = "K" }, new Manifest { Module = "x", Institution = "B" }, "s2", "K").Kind != UpdateKind.Collision, "");

			// ── «hay una versión nueva»: what the origin publishes, next to what is installed ──

			// The well-known names the module with its bundle, seal and version; installed is v2b
			// (1.1.0). Same seal: nothing. Greater version: news, dated, with the bundle resolved
			// against the origin. Not greater: silence, whatever the seal -a rollback is not announced.
			string installedSeal = ModuleEnrollment.InstalledSeal(Path.GetDirectoryName(updatable)!)!;
			Uri origen = new("https://delfos.cr/");
			DateTimeOffset seen = new(2026, 9, 9, 15, 0, 0, TimeSpan.Zero);
			string Lista(string seal, string version) => "{\"modules\":[{\"module\":\"actualizable\",\"bundle\":\"actualizable.gmod\",\"seal\":\"" + seal + "\",\"version\":\"" + version + "\"}]}";
			AvailableUpdate? nueva = ModuleEnrollment.UpdatePublished(Lista("otro-sello", "1.2.0"), "actualizable", installedSeal, "1.1.0", origen, seen);
			assert("the seal installed is the one published: nothing new",
				ModuleEnrollment.UpdatePublished(Lista(installedSeal, "1.2.0"), "actualizable", installedSeal, "1.1.0", origen, seen) is null, "");
			assert("a greater version under another seal is news, with the bundle resolved against the origin",
				nueva is { Version: "1.2.0", Seal: "otro-sello", Bundle: "https://delfos.cr/actualizable.gmod" } && nueva.SeenAt.StartsWith("2026-09-09"), $"{nueva}");
			assert("a version that is not greater is not news, whatever the seal says",
				ModuleEnrollment.UpdatePublished(Lista("otro-sello", "1.0.0"), "actualizable", installedSeal, "1.1.0", origen, seen) is null
					&& ModuleEnrollment.UpdatePublished(Lista("otro-sello", "1.1.0"), "actualizable", installedSeal, "1.1.0", origen, seen) is null, "");
			assert("unversioned sides under another seal are news of unknown version",
				ModuleEnrollment.UpdatePublished(Lista("otro-sello", ""), "actualizable", installedSeal, null, origen, seen) is { Version: "" }, "");
			assert("a module the list does not name, no list, or a broken list: nothing",
				ModuleEnrollment.UpdatePublished(Lista("otro-sello", "9.0.0"), "otro", installedSeal, "1.1.0", origen, seen) is null
					&& ModuleEnrollment.UpdatePublished(null, "actualizable", installedSeal, "1.1.0", origen, seen) is null
					&& ModuleEnrollment.UpdatePublished("{not json", "actualizable", installedSeal, "1.1.0", origen, seen) is null, "");

			// The note in the file: written once, not rewritten for the same news, cleared when asked.
			Installation quiet = Installation.Read(updatable);
			assert("the note goes into installation.json and nothing else moves",
				ModuleEnrollment.WriteUpdateNote(updatable, nueva) && Installation.Read(updatable) is { Update: { Version: "1.2.0" } } noted
					&& noted.Enabled == quiet.Enabled && noted.DisabledTools.SequenceEqual(quiet.DisabledTools) && noted.Shell.Arguments.SequenceEqual(quiet.Shell.Arguments), "");
			assert("the same news again rewrites nothing", !ModuleEnrollment.WriteUpdateNote(updatable, nueva! with { SeenAt = "2026-09-10T00:00:00Z" }), "");
			assert("clearing the note leaves the file without it, once",
				ModuleEnrollment.WriteUpdateNote(updatable, null) && Installation.Read(updatable).Update is null && !ModuleEnrollment.WriteUpdateNote(updatable, null), "");

			// The module in memory takes the same note, once, and its consent does not move.
			var noting = new Module(new Manifest { Module = "actualizable" }, quiet, Path.GetDirectoryName(updatable)!);
			assert("a running module takes the note, and only the note",
				noting.NoteUpdate(nueva) && noting.Installation.Update?.Version == "1.2.0" && noting.Installation.Enabled == quiet.Enabled
					&& !noting.NoteUpdate(nueva) && noting.NoteUpdate(null) && noting.Installation.Update is null, $"{noting.Installation.Update}");

			// Installing what the origin publishes rewrites the installation without the note.
			ModuleEnrollment.WriteUpdateNote(updatable, nueva);
			enrolling.Accept(enrolling.Examine(v2b, EnrollmentSource.Link, Delfos), acceptNewPublisher: true);
			assert("installing what was published leaves no note behind", Installation.Read(updatable).Update is null, "");

			// When the origins are asked again (44 §7, step 2b): at launch always; on coming back to
			// the front only after an hour, and never two at once. The clock is turned by hand.
			var schedule = new ReviewSchedule(TimeSpan.FromHours(1));
			DateTimeOffset t0 = new(2026, 9, 9, 16, 0, 0, TimeSpan.Zero);
			assert("before the launch review begins, a lifecycle hook has nothing to repeat", !schedule.Started && schedule.IsDue(t0), "");
			assert("the first review is due and claims the slot", schedule.TryBegin(t0) && schedule.Started, "");
			assert("while one runs, another is neither due nor allowed", !schedule.IsDue(t0) && !schedule.TryBegin(t0.AddMinutes(5)), "");
			schedule.End(t0.AddSeconds(4));
			assert("right after it ends, the next one waits for the hour",
				!schedule.IsDue(t0.AddMinutes(10)) && !schedule.TryBegin(t0.AddMinutes(59)) && Math.Round(schedule.Until(t0.AddMinutes(30)).TotalMinutes) == 30, $"{schedule.Until(t0.AddMinutes(30))}");
			assert("an hour after it ended, it is due again and may run",
				schedule.IsDue(t0.AddMinutes(61)) && schedule.Until(t0.AddMinutes(61)) == TimeSpan.Zero && schedule.TryBegin(t0.AddMinutes(61)), "");
			schedule.End(t0.AddMinutes(61));

			// ── the signature ────────────────────────────────────────────────────────────────

			SigningKey key = PackageSignature.Generate("demo-202609");
			string signedPath = Path.Combine(root, "signed.gmod");
			File.WriteAllBytes(signedPath, HostedOk);
			PackageSignature.Sign(signedPath, key);
			byte[] signedZip = File.ReadAllBytes(signedPath);

			SignatureVerification good = VerifySignature(signedZip);
			assert("a signed package verifies, and names its key",
				good.State == SignatureState.Valid && good.KeyId == "demo-202609" && good.Public == key.Public, $"{good}");
			assert("an unsigned package is unsigned, not invalid",
				VerifySignature(HostedOk).State == SignatureState.Unsigned, "");

			SignatureVerification touched = VerifySignature(Retouch(signedZip, "web/index.html", "<html>not what was signed</html>"));
			assert("a file changed after signing: invalid, and it says which",
				touched.State == SignatureState.Invalid && touched.Detail.Contains("changed") && touched.Detail.Contains("web/index.html"), touched.Detail);
			SignatureVerification grown = VerifySignature(Retouch(signedZip, "web/extra.js", "alert(1)"));
			assert("a file added after signing: invalid",
				grown.State == SignatureState.Invalid && grown.Detail.Contains("added"), grown.Detail);
			SignatureVerification resigned = VerifySignature(Retouch(signedZip, PackageSignature.FileName, "{ \"alg\": \"ES256\", \"keyId\": \"x\", \"public\": \"" + key.Public + "\", \"files\": {}, \"signature\": \"AAAA\" }"));
			assert("a signature that does not hold under its own key: invalid",
				resigned.State == SignatureState.Invalid, resigned.Detail);

			// Whose key: what the origin publishes against what Gnothi pinned.
			string keysJson = "{\"keys\":[{\"id\":\"demo-202609\",\"alg\":\"ES256\",\"public\":\"" + key.Public + "\"}],\"modules\":[{\"module\":\"demo\",\"bundle\":\"https://delfos.cr/examenes.gmod\"}]}";
			var now = new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.Zero);
			var pinnedSame = new PackageSigner { KeyId = "demo-202609", Public = key.Public, Since = "2026-09-01T00:00:00.0000000+00:00" };
			SigningKey other = PackageSignature.Generate("other-202601");
			var pinnedOther = new PackageSigner { KeyId = "other-202601", Public = other.Public, Since = "2026-01-01T00:00:00.0000000+00:00" };

			(SignerCheck check, PackageSigner? pin, bool sameKey) r;
			r = ModuleEnrollment.ResolveSigner(good, keysJson, null, now);
			assert("first time, key published by the origin: published, and pinned from now", r.check == SignerCheck.Published && r.pin?.KeyId == "demo-202609" && r.pin.Since.StartsWith("2026-09-09") && !r.sameKey, $"{r}");
			r = ModuleEnrollment.ResolveSigner(good, null, null, now);
			assert("first time, nobody publishes the key: unknown, pinned anyway for next time", r.check == SignerCheck.Unknown && r.pin?.Public == key.Public, $"{r}");
			r = ModuleEnrollment.ResolveSigner(good, null, pinnedSame, now);
			assert("same key as last time, origin silent: pinned, and the old date is kept", r.check == SignerCheck.Pinned && r.pin == pinnedSame && r.sameKey, $"{r}");
			r = ModuleEnrollment.ResolveSigner(good, keysJson, pinnedSame, now);
			assert("same key as last time, published too: published, old date kept", r.check == SignerCheck.Published && r.pin == pinnedSame, $"{r}");
			r = ModuleEnrollment.ResolveSigner(good, keysJson, pinnedOther, now);
			assert("a new key that the origin publishes: rotated, re-pinned", r.check == SignerCheck.Rotated && r.pin?.Public == key.Public, $"{r}");
			r = ModuleEnrollment.ResolveSigner(good, null, pinnedOther, now);
			assert("a new key nobody publishes: CHANGED", r.check == SignerCheck.Changed, $"{r}");
			r = ModuleEnrollment.ResolveSigner(VerifySignature(HostedOk), keysJson, pinnedSame, now);
			assert("an unsigned package resolves to unsigned and touches no pin", r.check == SignerCheck.Unsigned && r.pin == pinnedSame, $"{r}");

			// Through the enrollment: the pin is written on accept and read back next time.
			EnrollmentProposal s1 = enrolling.Examine(signedZip, EnrollmentSource.Link, Delfos, null, keysJson);
			assert("a signed package served by its origin, key published, first time",
				s1.Provenance.Signer == SignerCheck.Published && s1.Provenance.SignerId == "demo-202609" && s1.Provenance.SignedSince == "" && s1.Signer is not null,
				$"{s1.Provenance}");
			enrolling.Accept(s1);
			Installation pinnedNow = Installation.Read(Path.Combine(root, "Modulos", "demo", "installation.json"));
			assert("accepting writes the pin into installation.json",
				pinnedNow.Signer?.KeyId == "demo-202609" && pinnedNow.Signer.Public == key.Public && pinnedNow.Signer.Since.Length > 0, $"{pinnedNow.Signer}");

			EnrollmentProposal s2 = enrolling.Examine(signedZip, EnrollmentSource.Link, Delfos, null, keysJson);
			assert("the second time it is the same key, since the date of the first",
				s2.Provenance.Signer == SignerCheck.Published && s2.Provenance.SignedSince == pinnedNow.Signer!.Since, $"{s2.Provenance}");
			enrolling.Discard(s2);

			string otherPath = Path.Combine(root, "other.gmod");
			File.WriteAllBytes(otherPath, HostedOk);
			PackageSignature.Sign(otherPath, other);
			EnrollmentProposal s3 = enrolling.Examine(File.ReadAllBytes(otherPath), EnrollmentSource.Link, Delfos, null, keysJson);
			assert("the same module signed by a key the origin does not publish: CHANGED against the pin",
				s3.Provenance.Signer == SignerCheck.Changed && s3.Provenance.SignerId == "other-202601", $"{s3.Provenance}");
			enrolling.Discard(s3);

			try
			{
				enrolling.Examine(Retouch(signedZip, "web/index.html", "<html>evil</html>"), EnrollmentSource.Link, Delfos, null, keysJson);
				assert("a tampered signed package never becomes a proposal", false, "Examine returned a proposal");
			}
			catch (PackageSignatureInvalidException e)
			{
				assert("a tampered signed package never becomes a proposal", e.Message.Contains("web/index.html"), e.Message);
			}

			// ── revocation: the institution retires the key ──────────────────────────────────

			string revokedJson = "{\"keys\":[],\"revoked\":[{\"id\":\"demo-202609\",\"public\":\"" + key.Public + "\",\"since\":\"2026-09-09T13:00:00Z\"}],\"modules\":[{\"module\":\"demo\",\"bundle\":\"https://delfos.cr/examenes.gmod\"}]}";
			assert("a key the origin lists as retired is revoked", ModuleEnrollment.IsRevoked(revokedJson, key.Public), "");
			assert("a key the origin does not list as retired is not", !ModuleEnrollment.IsRevoked(keysJson, key.Public) && !ModuleEnrollment.IsRevoked(null, key.Public), "");
			r = ModuleEnrollment.ResolveSigner(good, revokedJson, pinnedSame, now);
			assert("retired beats the pin: revoked, even for the key of last time", r.check == SignerCheck.Revoked && r.pin == pinnedSame, $"{r}");

			try
			{
				enrolling.Examine(signedZip, EnrollmentSource.Link, Delfos, null, revokedJson);
				assert("a package signed with a retired key never becomes a proposal", false, "Examine returned a proposal");
			}
			catch (PackageKeyRevokedException e)
			{
				assert("a package signed with a retired key never becomes a proposal", e.Message.Contains("demo-202609"), e.Message);
			}

			// The module already installed under that key: marked, once, and nothing else moves.
			string demoInstallation = Path.Combine(root, "Modulos", "demo", "installation.json");
			Installation before = Installation.Read(demoInstallation);
			string? markedAt = ModuleEnrollment.MarkRevoked(demoInstallation, now);
			assert("an installed module whose pinned key was retired gets marked, and the date comes back",
				markedAt?.StartsWith("2026-09-09") == true, $"MarkRevoked returned {markedAt ?? "null"}");
			Installation after = Installation.Read(demoInstallation);
			assert("the mark is the date, and the rest of the file is as the person left it",
				after.Signer?.RevokedAt == markedAt && after.Signer.KeyId == before.Signer!.KeyId && after.Enabled == before.Enabled
					&& after.GrantedFolder == before.GrantedFolder && after.Shell.Type == before.Shell.Type,
				$"{after.Signer} enabled={after.Enabled}");
			assert("marking twice does nothing the second time", ModuleEnrollment.MarkRevoked(demoInstallation, now) is null, "");

			// The module in memory takes the same mark, once, and its consent does not move.
			var running = new Module(new Manifest { Module = "demo" }, before, Path.GetDirectoryName(demoInstallation)!);
			assert("a running module takes the mark on its pin, and only there",
				running.MarkSignerRevoked(markedAt!) && running.Installation.Signer?.RevokedAt == markedAt && running.Installation.Enabled == before.Enabled
					&& !running.MarkSignerRevoked(markedAt!),
				$"{running.Installation.Signer}");

			// The institution's side: --revoke moves the key into the retired list and renames the file.
			string keyPath = Path.Combine(root, "keys", "demo.key.json");
			string revokedPath = Path.Combine(root, "keys", "demo.revoked.json");
			PackageSignature.WriteKey(keyPath, key);
			RevokedKey retired = PackageSignature.Revoke(keyPath, revokedPath, now, "leaked");
			assert("--revoke lists the key's public half, dated, with the reason, and retires the file",
				retired.Public == key.Public && retired.Reason == "leaked" && File.Exists(revokedPath) && !File.Exists(keyPath)
					&& Directory.GetFiles(Path.Combine(root, "keys"), "demo.key.retirada-*.json").Length == 1
					&& File.ReadAllText(revokedPath).Contains(key.Public),
				$"revoked={File.Exists(revokedPath)} key={File.Exists(keyPath)} files=[{string.Join(", ", Directory.GetFiles(Path.Combine(root, "keys")).Select(Path.GetFileName))}]");
		}
		finally
		{
			try { Directory.Delete(root, recursive: true); } catch { /* temp, best effort */ }
		}
	}

	private const string DelfosOrigin = "https://delfos.cr";
	private static readonly Uri Delfos = new("https://delfos.cr/examenes.gmod");

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

	private static SignatureVerification VerifySignature(byte[] zip)
	{
		using var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read);
		return PackageSignature.Verify(archive);
	}

	// The same zip with one entry replaced -or added-: what a tamperer does.
	private static byte[] Retouch(byte[] zip, string path, string content)
	{
		using var ms = new MemoryStream();
		ms.Write(zip, 0, zip.Length);
		using (var archive = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true))
		{
			archive.GetEntry(path)?.Delete();
			using Stream s = archive.CreateEntry(path).Open();
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
