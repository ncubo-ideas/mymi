using Gnothi.Assistant;
using Gnothi.Host;
using System.Text.Json;

namespace Gnothi.MobileShared;

// THE PHONE'S SHELL, SHARED BY ANDROID AND BY iOS.
//
// WHERE IT COMES FROM. All of this used to live inside MainActivity.cs, and it worked there. It was
// taken out when the iOS version had to be written: the alternative was copying 250 lines and
// maintaining them in two places, which is exactly how they go out of sync.
//
// THE DIVIDING LINE, and it is the same criterion that separates the Dispatcher from Door 1: here is
// what does NOT depend on the platform -- the state the page needs, the consent, the key, the
// provider, and the switch that serves what the page asks for -- and outside is only what knows about
// WebViews, UI threads and system logs. Measured in lines: each platform's part came out at ~60.
//
// WHY NOT MAUI, which is the natural question. MainActivity.cs already carries the answer written
// down: the MAUI workload installs into Program Files -- administrator on this machine -- and
// rewrites the project. But there is a stronger reason, and it is a design one: GNOTHI'S FACE IS AN
// HTML. One single Assets/ui.html, the SAME file on both phones, rendered by a WebView. MAUI would
// share its own UI framework -- which is not used here -- and would NOT share the WebView bridge,
// which is each platform's native API anyway. Which means: a high price for sharing what was already
// shared, without solving what actually differs.
//
// WHAT IS SHARED, COUNTED:
//
//     Gnothi.Host/*.cs      the core          identical, already shared
//     Gnothi.Assistant/*.cs Door 2            identical, already shared
//     Gnothi.MobileShared/Shell.cs this shell        identical, NEW
//     Assets/ui.html        the face          identical, one single file
//     ---------------------------------------------------------------
//     MainActivity.cs       Android WebView   ~60 lines
//     ViewController.cs     iOS WKWebView     ~60 lines
public sealed class Shell : IDisposable
{
	// THE TWO FOLDERS, AND THE PLATFORM CHOOSES THEM. It is the same principle as a module's
	// grantedFolder: the one who grants is not the one who uses.
	//
	//   Files      the app's private area. Nobody from outside writes here, and that is right:
	//              nobody from outside installs a module.
	//   Incoming   where what was downloaded lands, for the Host to install at start-up.
	//
	// On Android they are FilesDir and GetExternalFilesDir(null)/incoming.
	// On iOS they are Library/Application Support and Documents/incoming -- and there is an advantage
	// there that Android does not have: Documents/ IS VISIBLE in the Files app, so the person can drop
	// the package in themselves, with no cable and no adb.
	/// <param name="ReescribirRutas">
	/// Whether the installation.json arriving in Incoming carries paths from ANOTHER machine and they
	/// have to be made local while copying it.
	///
	/// ANDROID: false. armar-paquete.ps1 already wrote the right paths, because on Android an app's
	/// private area is predictable: /data/user/0/&lt;applicationId&gt;/files. It is known when the
	/// package is assembled.
	///
	/// iOS: true, and it is not a whim. The container is
	/// /var/mobile/Containers/Data/Application/&lt;GUID&gt;/... and THAT GUID CHANGES ON EVERY
	/// INSTALL. There is no path that can be written down in advance: the only one that exists is the
	/// one the process finds out once it is already running.
	/// </param>
	/// <param name="Instantaneas">
	/// The SHARED container where the Host leaves the snapshot for the caratula, or null if this
	/// platform does not need it.
	///
	/// ANDROID: null. Over there the caratula is a separate APK that calls the Host LIVE over binder,
	/// with the identity gate in between. There is nothing to write down.
	///
	/// iOS: the App Group's container. Over there App Intents only exist in Swift and the Host is in
	/// .NET, so they never touch: the Host leaves the snapshot and the caratula reads it. See
	/// Snapshot.cs.
	/// </param>
	/// <param name="FotoLocal">
	/// Whether the snapshot also has to be left in the app's OWN folder, in Files/instantanea/.
	///
	/// It is for an intent living INSIDE this very app (Gnothi.iOS/Intents/): there,
	/// no App Group is needed, nor entitlement, nor Apple portal · it is a file in one's own folder.
	/// Android leaves it false: over there the caratula is a separate APK that asks live.
	/// </param>
	public sealed record Paths(
		string Files,
		string Incoming,
		bool ReescribirRutas = false,
		string? Instantaneas = null,
		bool FotoLocal = false);

	private readonly Paths paths;
	private readonly Action<string> log;
	private readonly Action<string, string, string> push;
	private readonly CancellationTokenSource stop = new();

	// THE ORIGINS ARE ASKED AGAIN WHEN GNOTHI COMES BACK TO THE FRONT, at most once an hour
	// (NotasGnothiHost/44 §7, step 2b): the card is fresh when she returns after a while, and an
	// institution hears from this phone no more than once an hour however often she switches apps.
	public static readonly TimeSpan ReviewEvery = TimeSpan.FromHours(1);
	private readonly ReviewSchedule reviews = new(ReviewEvery);

	private Dispatcher? dispatcher;
	private ModuleCatalog? catalog;
	private ConsentWatch? watch;

	/// The queue of snapshot writes. It exists to SERIALISE them: two Fotografiar() at once fight over
	/// the same temporary file. See SetConsent.
	private Task fotos = Task.CompletedTask;

	// WHERE THE MODEL LIVES. Two constants, because changing provider has to stay this cheap -- that is
	// the whole argument for IModel being an interface.
	//
	//   Groq        https://api.groq.com/openai/v1     free tier, no card
	//   Cerebras    https://api.cerebras.ai/v1
	//   OpenRouter  https://openrouter.ai/api/v1
	//   LM Studio   http://<machine IP>:1234/v1        NO key -- nothing leaves the LAN
	//
	// 'localhost' does NOT work from the phone: there, localhost is the phone.
	private const string ModelUrl = "https://api.groq.com/openai/v1";
	private const string ModelName = "openai/gpt-oss-120b";

	/// <param name="push">
	/// How something gets pushed to the page. The platform takes care of putting it on the UI thread:
	/// on Android that is RunOnUiThread + EvaluateJavascript, on iOS InvokeOnMainThread +
	/// EvaluateJavaScript. The shell does not want to know which.
	/// </param>
	// THE LOCAL MODEL'S FACTORY IS PROVIDED BY THE SHELL, which is why it is optional. The phone's
	// model is spoken to through DllImports against symbols in the executable itself, so only iOS can
	// build it: Android passes null and that provider is not offered. This file compiles on both
	// platforms and must not know which is which.
	public Shell(Paths paths, Action<string> log, Action<string, string, string> push,
		Func<string, IModel>? modeloLocal = null)
	{
		this.paths = paths;
		this.log = log;
		this.push = push;
		this.modeloLocal = modeloLocal;
	}

	private readonly Func<string, IModel>? modeloLocal;

	/// <summary>The provider that runs on the device, with no url and no key.</summary>
	public const string ProveedorLocal = "En este teléfono";

	/// <summary>Como se llama el modelo del aparato en la pantalla y en model.cfg.</summary>
	public const string ModeloLocal = "apple-on-device";

	// THE ICON BY SECTOR, not by module name.
	//
	// THIS SWITCH USED TO SAY 'examenes', 'facturas' and 'compras', with a comment explaining that the
	// icon deliberately did NOT come from the manifest, because it was the Host's business. The
	// position was defensible and the cost was visible: the Host had to KNOW every institution by name
	// in order to paint it, so a module from a new institution came in anonymous -- and that
	// contradicts dynamic enrolment, where adding a module is two json files and not two json files
	// and a Host release. The dealership and the petrol station, which were already in the repo, both
	// fell into the same box.
	//
	// THE NEW DIVIDING LINE: the institution declares its SECTOR -- a closed list, in About.Known --
	// and the Host picks the drawing. The institution does not pick the icon; it picks what kind of
	// business it is, which is a fact about it and not a design decision. And the one still picking the
	// drawing is the Host, which is what the previous position wanted to protect.
	//
	// IT IS STILL AN EMOJI because its only consumer is the SNAPSHOT -- the one Siri's intent reads,
	// which cannot paint an svg. The phone's face uses the institution's logo if there is one, and
	// otherwise a drawing of its own by sector.
	public static string Icon(Gnothi.Host.Manifest m) => (m.About?.Category ?? "") switch
	{
		"laboratorio" or "salud"        => "\U0001FA7A",   // stethoscope
		"supermercado"                  => "\U0001F6D2",   // trolley
		"comercio"                      => "\U0001F9FE",   // receipt
		"banca" or "seguros"            => "\U0001F3E6",   // bank
		"combustible"                   => "⛽",       // pump
		"taller"                        => "\U0001F527",   // spanner
		"telecom"                       => "\U0001F4F1",   // phone
		"educacion"                     => "\U0001F393",   // mortarboard
		"gobierno"                      => "\U0001F3DB",   // building
		_                               => "\U0001F4E6",   // box
	};

	// ── the start-up ────────────────────────────────────────────────────────────────────

	public async Task Run()
	{
		// 1 - LIFECYCLE: the Host installs whatever was downloaded.
		string root = Path.Combine(paths.Files, "Modulos");
		string journals = Path.Combine(paths.Files, "journals");

		// Migration: the key used to live in a file for a single hardcoded provider. If that old file is
		// there and the per-provider one is not, it becomes Groq's key.
		string old = Path.Combine(paths.Files, "model.key");
		if (File.Exists(old) && !File.Exists(KeyPath)) File.Move(old, KeyPath);

		int copied = 0;
		if (Directory.Exists(paths.Incoming))
		{
			string modulesIn = Path.Combine(paths.Incoming, "Modulos");
			string journalsIn = Path.Combine(paths.Incoming, "journals");
			if (Directory.Exists(modulesIn)) copied += Copy(modulesIn, root);
			if (Directory.Exists(journalsIn)) copied += Copy(journalsIn, journals);

			if (paths.ReescribirRutas) Localizar(root, journals);
		}

		// 2 - DISCOVER. The very same code the desktop Host runs.
		catalog = new ModuleCatalog(root);
		IReadOnlyList<string> warnings = catalog.Load();
		// THIS DOOR PAYS ITS OWN TOKENS. The key is Sonia's, so every character a module wraps its facts
		// in comes out of her pocket -- which is why this is the door that declares it also reads TOON.
		// What it does not do is DEMAND it: the module answers in whichever suits that answer, and JSON
		// still counts.
		dispatcher = new Dispatcher(catalog, new Meter(), Accepts.JsonOrToon);

		foreach (string w in warnings) log($"warning: {w}");
		log($"{catalog.Modules.Count} modules installed" + (copied > 0 ? $" - {copied} files this launch" : ""));

		PushState();
		await Fotografiar();

		// THE CHECK ON NEW MODULES. It goes AFTER PushState so the screen is already painted: on mobile,
		// checking a web module costs bringing up its WebView.
		await VerifyNewModules(stop.Token);

		// THE KEYS OF WHAT IS INSTALLED, against what their origins say now. In the background: it
		// is network, it is a warning, and the screen is already painted.
		_ = Task.Run(() => ReviewOrigins(stop.Token));

		// AND THE REHYDRATION, MOVED OFF THE PERSON'S WAIT.
		//
		// A module's actor rebuilds itself from the journal the first time anybody asks it anything,
		// behind a `Lazy<T>` -- and that is right, because listing a module's tools must not cost
		// reading its history. But it puts the cost on the FIRST QUESTION, which is the moment the
		// person is looking at an empty screen.
		//
		// Measured on the phone, 2026-09-17: exámenes ~694 ms, compras ~337 ms. Once per launch, not
		// per opening -- reopening a module costs nothing. And it is not the journal's length: compras
		// has 390 events against exámenes' 225 and takes half the time, so what costs is what each
		// domain REBUILDS, not what it reads.
		//
		// So this does not make it faster. It makes it EARLIER: between Gnothi opening and somebody
		// tapping «Abrir» there are seconds going spare, and the screen is already painted by now.
		//
		// WHY IT IS A LIST AND NOT «ALL OF THEM». A live module costs ~26 MB, and Gnothi with the five
		// reached 482 MB: warming everything makes the process bigger and more killable by Android --
		// and when the system kills it, the rehydration comes back. Warming too much can cost exactly
		// what it means to save. Who goes on the list is a product decision and it is not made here.
		if (WarmUp.Count > 0) _ = Task.Run(() => Rehydrate(stop.Token));

		// 3 - THE PERSON'S SWITCH, WATCHED. The UI's switches write installation.json and this is what
		//     makes them effective; and an edit from OUTSIDE (adb, another tool) repaints the screen by
		//     the same path. One single source of truth: their file.
		//
		//     THE SNAPSHOT IS REMADE HERE TOO, and that is what closes the consent hole: if the person
		//     switches a module off, the snapshot is rewritten WITHOUT it, so the caratula stops being
		//     able to answer for that module. Without this line a stale snapshot would be left answering
		//     something she had already revoked.
		watch = new ConsentWatch(dispatcher)
		{
			Changed = async (_, _) => { PushState(); await Fotografiar(); },
		};
		await watch.Run(log, stop.Token);
	}

	// GNOTHI CHECKS A MODULE THE FIRST TIME IT SEES IT, and leaves a record.
	//
	// Why at install time and not when Sonia asks: a module that declares tools and then does not
	// answer is a broken module, and the moment to discover that is before offering them to the
	// assistant, not once she has already asked. It is the fourth pillar applied to itself -- Gnothi
	// answers what no module can answer about itself -- and it is the conformance the specification
	// asks for (11 §15.7).
	//
	// Why ONCE: on mobile a web module costs a WebView with the runtime inside, measured at
	// 216-248 MB. Checking it on every start-up would be paying that for nothing. The mark stays in the
	// module's folder, so reinstalling it checks it again.
	//
	// IT USED TO BE IN MainActivity AND USED NOTHING OF ANDROID -- Dispatcher, catalogue, files and
	// json. Bringing it here means iOS also checks its modules when installing them, without a single
	// extra line.
	private async Task VerifyNewModules(CancellationToken ct)
	{
		if (dispatcher is null || catalog is null) return;

		// A NEW VERSION THAT DOES NOT ANSWER GOES BACK (44 §3.5): while the update kept the
		// previous version parked next to it, a failed check puts that one back and the catalog
		// is reloaded. Collected here and applied after the loop, which is over the catalog's list.
		List<string> rolledBack = [];

		foreach (Module m in catalog.Modules)
		{
			string mark = Path.Combine(m.Directory, "comprobado.json");
			if (File.Exists(mark)) continue;

			try
			{
				// 1. WHAT IT SAYS AGAINST WHAT IT DOES. The Host already knows how to do this.
				var verify = new Request(Dispatcher.HostModule, "verify_module",
					new Dictionary<string, object?> { ["module"] = m.Manifest.Module });
				Response said = await dispatcher.Serve(verify, ct);
				log($"comprobación de {m.Manifest.Module}: {JsonSerializer.Serialize(said.Content)}");

				// 2. AND THAT IT REALLY ANSWERS. Declaring correctly and failing on the first request is
				//    another way of being broken, and comparing lists does not detect it.
				DeclaredTool? first = m.Manifest.Tools.FirstOrDefault(t => t.ReadOnly && t.Input.Count == 0);
				if (first is not null)
				{
					Response answer = await dispatcher.Serve(new Request(m.Manifest.Module, first.Name, null), ct);
					string text = JsonSerializer.Serialize(answer.Content);
					log($"  {first.Name} contestó {text.Length} bytes: {text[..Math.Min(160, text.Length)]}");
				}

				File.WriteAllText(mark, JsonSerializer.Serialize(new { cuando = DateTime.UtcNow.ToString("O") }));
			}
			catch (Exception ex)
			{
				// A module that fails the check does NOT uninstall itself: that is Sonia's decision. It is
				// said out loud, and left unmarked, so it gets tried again.
				log($"comprobación de {m.Manifest.Module} FALLÓ: {ex.GetType().Name}: {ex.Message}");

				// UNLESS it is a new version with the previous one still parked next to it: then the one
				// that does not answer goes and the one that did comes back. The check on the one coming
				// back is already done (its mark travelled with the folder), so it is not repeated.
				if (ModuleEnrollment.Rollback(catalog.Root, m.Manifest.Module, log))
				{
					CloseModule?.Invoke(m.Manifest.Module);
					rolledBack.Add(m.Manifest.Module);
				}
			}
		}

		if (rolledBack.Count > 0)
		{
			foreach (string w in catalog.Load()) log($"warning: {w}");
			PushState();
			foreach (string module in rolledBack)
				PushEnrollment("error", Textos.T("alta.volvio-la-anterior", ("titulo", TitleOf(module))));
		}
	}

	// WHAT AN INSTALLED MODULE'S ORIGIN SAYS NOW ABOUT THE KEY IT WAS ACCEPTED WITH. A signature
	// is checked when a package arrives; a revocation happens later, to something already here,
	// and the only way to learn of it is to ask. So every launch, for each module that came signed
	// and whose manifest names an origin, the origin's well-known is fetched and its retired keys
	// compared with the pin. A hit is written into the pin (RevokedAt) -nothing else in the file
	// moves: the switches are the person's- and the card says it. The screen is repainted from
	// here on purpose: the consent watcher only wakes for a switch that moved, and no switch did.
	//
	// AND, WITH THE SAME ANSWER, WHETHER THE ORIGIN PUBLISHES SOMETHING NEWER (NotasGnothiHost/44
	// §4): the list carries each module's seal and version; when the published one is newer than
	// what is installed, the note goes into installation.json and the card says «hay una versión
	// nueva». A note, never an action: nothing downloads or installs until she taps it. When the
	// installed package matches what is published again, the note is cleared.
	private async Task ReviewOrigins(CancellationToken ct)
	{
		if (catalog is null) return;
		DateTimeOffset started = DateTimeOffset.UtcNow;
		if (!reviews.TryBegin(started)) return;
		try
		{
			bool changed = await ReviewEachOrigin(ct);
			if (changed) PushState();
		}
		finally
		{
			reviews.End(DateTimeOffset.UtcNow);
		}
	}

	// GNOTHI CAME BACK TO THE FRONT -or a module was closed and the home is on screen again-: ask
	// the origins again if the last time was long enough ago. Cheap to call from every lifecycle
	// hook: the schedule says no when it is not due -the launch review counts- and never runs two
	// at once. The line in the log says which of the two happened.
	public void ReviewOriginsIfDue()
	{
		// Before the launch review has begun there is nothing to repeat -and on Android OnResume
		// fires right after OnCreate, while the catalog may still be loading. The launch owns
		// the first one; this only ever repeats it.
		if (catalog is null || stop.IsCancellationRequested || !reviews.Started) return;
		DateTimeOffset now = DateTimeOffset.UtcNow;
		if (!reviews.IsDue(now))
		{
			log($"revisión: la última consulta a los orígenes fue hace poco; la próxima, en {Math.Ceiling(reviews.Until(now).TotalMinutes):0} min");
			return;
		}
		log("revisión: se vuelve a consultar a los orígenes");
		_ = Task.Run(() => ReviewOrigins(stop.Token));
	}

	// One pass over the installed modules that declare an origin. True when something was marked
	// or noted -a retired key, a newer package, or a note cleared- and the screen must repaint.
	private async Task<bool> ReviewEachOrigin(CancellationToken ct)
	{
		if (catalog is null) return false;
		bool changed = false;

		foreach (Module m in catalog.Modules)
		{
			Uri? origin = ModuleEnrollment.DeclaredOrigin(m.Manifest.Origin);
			if (origin is null) continue;

			try
			{
				string? published = await ModuleEnrollment.FetchWellKnown(origin, ct);
				if (published is null) continue;
				string installationPath = Path.Combine(m.Directory, "installation.json");

				// The key it was accepted with, against the keys the origin retired.
				if (m.Installation.Signer is { RevokedAt: null } signer && ModuleEnrollment.IsRevoked(published, signer.Public))
				{
					string? at = ModuleEnrollment.MarkRevoked(installationPath, DateTimeOffset.UtcNow);
					if (at is not null)
					{
						m.MarkSignerRevoked(at);
						changed = true;
						log($"revocación: '{m.Name}': {origin.Host} retiró la llave '{signer.KeyId}' con la que se aceptó");
					}
				}

				// The package installed, against the one the origin publishes now.
				AvailableUpdate? newer = ModuleEnrollment.UpdatePublished(published, m.Name,
					ModuleEnrollment.InstalledSeal(m.Directory), m.Manifest.Publisher?.Version, origin, DateTimeOffset.UtcNow);
				if (ModuleEnrollment.WriteUpdateNote(installationPath, newer))
				{
					m.NoteUpdate(newer);
					changed = true;
					log(newer is null
						? $"actualización: '{m.Name}' está al día con {origin.Host}"
						: $"actualización: '{m.Name}': {origin.Host} publica {(newer.Version.Length > 0 ? newer.Version : "otro paquete")}; instalada {(m.Manifest.Publisher?.Version is { Length: > 0 } v ? v : "sin versión")}");
				}
			}
			catch (Exception ex)
			{
				log($"revisión: no se pudo consultar a {origin.Host} por '{m.Name}': {ex.GetType().Name}: {ex.Message}");
			}
		}

		return changed;
	}

	// SHE TAPPED «HAY UNA VERSIÓN NUEVA»: the package the origin publishes is fetched from the address
	// the note kept, with its seal, and goes through the whole examination like a link that landed.
	// The sheet is the update sheet, and only her yes there installs anything.
	// QUITAR UN MODULO ES DECISION DE LA PERSONA, y hasta 2026-09-11 no tenia como tomarla: podia
	// apagarlo, pero no sacarlo del telefono, y «volver a instalar» era borrar Mymi entera. Dos
	// cosas distintas se van o se quedan: la app de la institucion -Modulos/<modulo>, y su
	// .anterior si quedo de una actualizacion- se va siempre; la bitacora -journals/<modulo>, que
	// es tambien la carpeta concedida- es de la persona y se queda salvo que ella lo pida. Un alta
	// posterior la encuentra ahi, igual que una actualizacion. El modulo vivo se cierra antes,
	// por la misma razon que en una actualizacion: su WebView esta parado sobre la carpeta.
	private string RemoveModule(string module, bool journalToo)
	{
		Module? m = Find(module);
		if (m is null) return StateJson();

		if (CloseModule?.Invoke(module) == true)
			log($"quitar: '{module}' estaba vivo; se cerró antes de borrarlo");

		string folder = Path.Combine(paths.Files, "Modulos", module);
		string previous = folder + ModuleCatalog.PreviousSuffix;
		string journal = Path.Combine(paths.Files, "journals", module);

		if (Directory.Exists(previous)) Directory.Delete(previous, recursive: true);
		if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
		if (journalToo && Directory.Exists(journal)) Directory.Delete(journal, recursive: true);

		log($"quitar: '{module}' fuera del teléfono" + (journalToo ? ", con su bitácora" : "; la bitácora se queda"));

		foreach (string w in ReloadAfterInstall()) log($"warning: {w}");
		return StateJson();
	}

	private string StartUpdate(string module)
	{
		Module? m = Find(module);
		if (m?.Installation.Update is not { } update || update.Bundle.Length == 0) return "{}";

		log($"alta: actualización de '{module}' pedida desde la tarjeta: {update.Bundle}");
		_ = Enroll(update.Bundle, EnrollmentSource.Link, update.Seal.Length > 0 ? update.Seal : null, stop.Token);
		return "\"ok\"";
	}

	// ---------------------------------------------------------------- enrolment from the link

	// THE PROPOSAL IN FLIGHT, waiting for the person to say yes or no. ONE at a time: if another link
	// arrives while one is on screen, the previous one is discarded -- decisions she never saw do not
	// pile up.
	//
	// THIS LIVES HERE AND NOT IN THE PLATFORM because none of it belongs to a platform: downloading a
	// zip, reading a manifest, showing a proposal and waiting. It was in MainActivity until the iOS
	// version had to be written, and then it showed: the ViewController had to copy it whole. Two
	// copies of the most important screen Gnothi shows the person.
	private EnrollmentProposal? proposal;
	private ModuleEnrollment? enrolling;

	/// CLOSE A MODULE IF IT IS OPEN, and say whether it was. An update moves the module's folder
	/// aside while its WebView may be sitting on it (44 §3.6), so the screen has to let go first.
	/// The platform wires it where it registers its host of WebViews -Android and iOS each have
	/// one-; the Shell only names it, so that this file compiles wherever the suite compiles it.
	public Func<string, bool>? CloseModule { get; set; }

	/// WHAT ARRIVES FROM THE INSTITUTION'S LINK. It downloads the bundle and reads its manifest; it
	/// installs NOTHING. The person decides afterwards, and until then the bundle lives in a temporary
	/// folder.
	///
	/// Both platforms call it when their link lands: an App Link on Android, a universal link on iOS.
	/// The only thing that changes is how the URL arrives.
	public Task Enroll(string url, CancellationToken ct = default)
	{
		// The platforms hand over the link as it landed -go.gnothi.app/alta?bundle=…&h=…- or
		// just the bundle's address. Either way the seal, when there is one, travels with it.
		EnrollmentLink link = ModuleEnrollment.ParseLink(url);
		return Enroll(link.Bundle, EnrollmentSource.Link, link.Seal, ct);
	}

	/// THE PACKAGE ARRIVED AS A FILE: opened from Files or Downloads, shared from another app, or
	/// picked from inside Mymi. The platform read the bytes and knows the file's name; nothing
	/// else about where it came from is known, and the sheet says so -unless the package claims an
	/// origin that publishes exactly this manifest, which the enrollment asks.
	public Task EnrollFile(byte[] zip, string fileName, CancellationToken ct = default)
		=> Propose(e => e.ExamineFile(zip, fileName, ct, PushEnrollment), fileName);

	// ── SEVERAL FILES AT ONCE ────────────────────────────────────────────────────────────

	// WHAT SHE PICKED, when she picked more than one: the bytes wait here, with the quick look
	// Mymi took at each, until she chooses which one to examine for real. The list on the screen
	// is repainted as she installs them one by one; a single file skips all this and goes straight
	// to its proposal, as before.
	private List<(byte[] Bytes, PackagePeek Peek)>? picked;
	private readonly HashSet<int> pickedInstalled = [];
	private readonly Dictionary<int, string> pickedProblems = [];
	private int pickedChosen = -1;

	/// THE PICKER LET HER CHOOSE SEVERAL, so first the screen shows what Mymi found in them -which
	/// are modules, of whom, which are not- and she chooses which to install. Each choice runs the
	/// whole examination, origin and signature included; the list only looks inside the zips.
	public Task EnrollFiles(IReadOnlyList<(byte[] Bytes, string Name)> files, CancellationToken ct = default)
	{
		if (files.Count == 1) return EnrollFile(files[0].Bytes, files[0].Name, ct);

		picked = [.. files.Select(f => (f.Bytes, ModuleEnrollment.Peek(f.Bytes, f.Name)))];
		pickedInstalled.Clear();
		pickedProblems.Clear();
		pickedChosen = -1;
		log($"alta: la persona eligió {files.Count} archivos; {picked.Count(p => p.Peek.IsModule)} son módulos");
		PushPicked();
		return Task.CompletedTask;
	}

	// The list as the page paints it: one row per file, in the person's words. `problema` is why a
	// row cannot be chosen -not a module, rejected by the inspection, or refused when she tried-.
	private void PushPicked()
	{
		if (picked is null) return;

		var rows = picked.Select((p, i) =>
		{
			PackagePeek peek = p.Peek;
			string problem = pickedProblems.TryGetValue(i, out string? refused) ? refused
				: peek.Manifest is null ? Textos.T("alta.sin-manifiesto")
				: peek.Inspection is { Rejected: true } inspection ? Textos.T("alta.no-instalaria", ("detalle", string.Join("; ", inspection.Rejections)))
				: "";
			return new
			{
				indice = i,
				archivo = peek.FileName,
				bytes = peek.Bytes,
				modulo = peek.Manifest?.Module ?? "",
				titulo = peek.Manifest?.Title ?? "",
				institucion = peek.Manifest?.Institution ?? "",
				nombreCorto = peek.Manifest?.Brand?.ShortName ?? "",
				eslogan = peek.Manifest?.Brand?.Slogan ?? "",
				color = Brand.ValidColor(peek.Manifest?.Brand?.Color) ?? "",
				categoria = peek.Manifest?.About?.Category ?? "",
				firmado = peek.Signature == SignatureState.Valid,
				problema = problem,
				instalado = pickedInstalled.Contains(i),
				yaEstaba = peek.Manifest is not null && !pickedInstalled.Contains(i) && Find(peek.Manifest.Module) is not null,
			};
		}).ToArray();

		Push("hallazgos", "", JsonSerializer.Serialize(new { archivos = rows }));
	}

	// She chose one of the list: the whole examination, as if she had opened that file alone.
	private string ChoosePicked(int index)
	{
		if (picked is null || index < 0 || index >= picked.Count) return "{}";
		pickedChosen = index;
		_ = EnrollFile(picked[index].Bytes, picked[index].Peek.FileName, stop.Token);
		return "\"ok\"";
	}

	// She is done with the list: the bytes go, nothing else was kept.
	private string ClosePicked()
	{
		picked = null;
		pickedInstalled.Clear();
		pickedProblems.Clear();
		pickedChosen = -1;
		return "\"ok\"";
	}

	// After a proposal from the list ends -installed, declined or refused- the list comes back with
	// that row marked, so she can go on with the next one.
	private void PickedResolved(bool installed, string? refusal = null)
	{
		if (picked is null) return;
		if (pickedChosen >= 0)
		{
			if (installed) pickedInstalled.Add(pickedChosen);
			if (refusal is not null) pickedProblems[pickedChosen] = refusal;
		}
		pickedChosen = -1;
		PushPicked();
	}

	/// EL ARCHIVO QUE LA PLATAFORMA NO PUDO LEER, dicho en la pantalla del alta. Misma tira roja
	/// que cualquier alta fallida.
	///
	/// RECIBE EL HECHO -que archivo- Y NO LA FRASE, y ese es el cambio que importa: cuando
	/// recibia un `message`, las dos plataformas llamaban con el MISMO literal escrito dos veces,
	/// una en MainActivity y otra en ViewController. Era el unico sitio del repo donde una
	/// cascara de plataforma escribia texto de pantalla, y por lo tanto el unico que habria que
	/// traducir dos veces. La frase se compone aca, una vez, desde el catalogo.
	public void ReportUnreadableFile(string fileName) =>
		PushEnrollment("error", Textos.T("alta.no-se-pudo-leer", ("archivo", fileName)));

	private Task Enroll(string bundle, EnrollmentSource source, string? seal, CancellationToken ct)
		=> Propose(e => e.Examine(bundle, source, seal, ct, PushEnrollment), bundle);

	// ONE ENTRY FOR EVERY WAY IN; only how the bytes are obtained differs, and the provenance
	// that comes back travels to the sheet. `what` is what to blame in the log: an address, a file.
	private async Task Propose(Func<ModuleEnrollment, Task<EnrollmentProposal>> examine, string what)
	{
		try
		{
			if (proposal is not null) { enrolling?.Discard(proposal); proposal = null; }

			enrolling = new ModuleEnrollment(
				Path.Combine(paths.Files, "Modulos"),
				Path.Combine(paths.Files, "journals"),
				log);

			// DOWNLOADING IS NOT INSTALLING. This fetches the bundle, inspects it and reads its manifest;
			// the module still takes up no space at all on the device.
			proposal = await examine(enrolling);

			log($"alta: propuesta de '{proposal.Manifest.Module}' servida por {proposal.ServedBy}");
			PushProposal(proposal);
		}
		catch (Exception ex)
		{
			// AN ENROLMENT THAT FAILS HAS TO SAY SO, AND ON THE SCREEN. When this only wrote to the log,
			// the person tapped their institution's button, came back to Gnothi and nothing happened,
			// with no hint at all as to why.
			log($"alta: no se pudo leer el módulo de {what}: {ex.GetType().Name}: {ex.Message}");
			PushEnrollment("error", InPlainWords(ex));
			PickedResolved(installed: false, refusal: InPlainWords(ex));
		}
	}

	// THE PERSON ACCEPTED. Only here does the module exist.
	// `acceptNewPublisher`: on an update whose manifest moved the publisher's endpoint, whether she
	// said yes to the new one on the sheet. Otherwise the endpoint Mymi fixed stays.
	private string AcceptEnrollment(bool acceptNewPublisher = false)
	{
		if (proposal is null || enrolling is null) return "{}";

		try
		{
			// ON AN UPDATE the module may be open: its WebView sits on the folder about to move
			// aside. It is evicted first; opening it again loads the new version (44 §3.6).
			//
			// AND THE FACE IS NOT THE WHOLE MODULE, which is what was missing. `CloseModule` takes the
			// WebView apart; in the HYBRID form what sits on top of that folder is also the
			// AssemblyLoadContext with the institution's dll -- and, with `keepLoaded`, its actor open
			// over the journal. Closing only the face left the old assembly loaded while the folder
			// moved out from under it.
			//
			// It is closed BEFORE the move and it is AWAITED, unlike the eviction the catalogue does
			// when republishing: here nothing can go on until the module has let go of what it had
			// open, because what comes next is moving its folder.
			enrolling.BeforeReplacing = m =>
			{
				bool cerroLaCara = CloseModule?.Invoke(m) == true;

				Module? cargado = catalog?.Find(m);
				if (cargado is not null)
				{
					try { cargado.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
					catch (Exception ex) { log($"alta: '{m}' no se pudo cerrar del todo: {ex.GetType().Name}"); }
				}

				if (cerroLaCara || cargado is not null)
					log($"alta: '{m}' estaba abierto; se cerró antes de reemplazarlo");
			};

			string module = enrolling.Accept(proposal, acceptNewPublisher);
			proposal = null;

			// The catalogue is re-read whole -- and with that the new module gets checked.
			foreach (string w in ReloadAfterInstall()) log($"warning: {w}");
			log($"alta: '{module}' quedó instalado; {ModuleCount} módulos");

			PushEnrollment("listo", Textos.T("alta.listo", ("titulo", TitleOf(module))));
			PickedResolved(installed: true);
		}
		catch (Exception ex)
		{
			log($"alta: no se pudo instalar: {ex.GetType().Name}: {ex.Message}");
			PushEnrollment("error", InPlainWords(ex));
			PickedResolved(installed: false, refusal: InPlainWords(ex));
		}

		return "\"ok\"";
	}

	// THE PERSON SAID NO. Nothing is left: no folder, no journal, no entry in the catalogue.
	private string DiscardEnrollment()
	{
		if (proposal is not null)
		{
			enrolling?.Discard(proposal);
			proposal = null;
		}
		PushEnrollment("nada", "");
		PickedResolved(installed: false);
		return "\"ok\"";
	}

	// The page brought a link: pasted by the person, or read off the institution's code. It may
	// be the link to Mymi's domain with the bundle and its seal as parameters, or the bundle's
	// address itself; both are accepted. Fire and forget, like RunAsk: the page hears back
	// through the same "alta"/"propuesta" pushes as a link that landed.
	private string EnrollFromPage(JsonElement req)
	{
		string text = req.GetProperty("url").GetString() ?? "";
		bool scanned = req.TryGetProperty("via", out JsonElement via) && via.GetString() == "qr";
		EnrollmentSource source = scanned ? EnrollmentSource.Qr : EnrollmentSource.PastedLink;

		EnrollmentLink link = ModuleEnrollment.ParseLink(text);
		if (link.Bundle.Length == 0)
		{
			PushEnrollment("error", scanned ? Textos.T("alta.codigo-sin-enlace") : Textos.T("alta.pega-el-enlace"));
			return "\"ok\"";
		}

		_ = Enroll(link.Bundle, source, link.Seal, stop.Token);
		return "\"ok\"";
	}

	// HOW THE ENROLMENT IS TOLD ON THE SCREEN. It is the first thing the person sees Gnothi do for them
	// -- they tapped a button on their institution's page and came back here -- and it used to be a
	// 12 MB silence. Four states: working, done, error, nothing.
	private void PushEnrollment(string estado, string detalle) =>
		Push("alta", "", JsonSerializer.Serialize(new { estado, detalle }));

	// WHILE IT WORKS, with the numbers: how many bytes of how many came down, so the screen draws a
	// bar and says "4,1 de 12,6 MB" instead of spinning in silence over twelve megabytes. The
	// fraction is null before the server says how much is coming, and for the steps that are not
	// a download.
	private void PushEnrollment(EnrollmentProgress step) =>
		Push("alta", "", JsonSerializer.Serialize(new { estado = "trabajando", detalle = step.Text, progreso = step.Fraction, bytes = step.Bytes, total = step.Total }));

	// WHAT THE INSTITUTION PROPOSES, plus the two things Gnothi decides and she has a right to know
	// before accepting: where the journal is going to live and which endpoint it is going to talk to.
	// THE INSTITUTION'S FACE IN THE SHAPE THE SCREEN EXPECTS: an object with a logo and a colour, or
	// null when it declared neither -- and then the card falls back to its sector's drawing, which is
	// an unbranded module and is legitimate.
	//
	// IT EXISTS FOR THE ENROLMENT PROPOSAL, where the module is not installed yet: the bundle is
	// unpacked into a temporary folder and there is no Module to ask.
	private static string InstalledAt(Module m)
	{
		string at = m.Installation.Provenance?.At ?? "";
		if (at.Length > 0) return at;
		try { return File.GetLastWriteTimeUtc(Path.Combine(m.Directory, "installation.json")).ToString("yyyy-MM-ddTHH:mm:ssZ"); }
		catch { return ""; }
	}

	private static Dictionary<string, object?>? Marca(Gnothi.Host.Manifest manifest, string folder)
		=> BrandOf(Gnothi.Host.Module.LookOf(manifest, folder, out _));

	// THE BRAND AS THE PAGE EXPECTS IT: logo and color as before, plus the banner behind the card
	// and the typeface (2026-09-09), all as data URIs already through the Host's customs. Null when
	// the institution declared none of it, and the card falls to the drawing of its category.
	private static Dictionary<string, object?>? BrandOf(Gnothi.Host.Module.BrandLook look)
	{
		if (!look.Any) return null;
		return new Dictionary<string, object?>
		{
			["logo"] = look.Logo,
			["color"] = look.Color,
			["banner"] = look.Banner,
			["font"] = look.FontFamily.Length > 0
				? new Dictionary<string, object?> { ["family"] = look.FontFamily, ["regular"] = look.FontRegular, ["bold"] = look.FontBold }
				: null,
		};
	}

	private void PushProposal(EnrollmentProposal p) => Push("propuesta", "", JsonSerializer.Serialize(new
	{
		modulo = p.Manifest.Module,
		identidad = p.Manifest.Identity,
		titulo = p.Manifest.Title,
		institucion = p.Manifest.Institution,

		// THE LOGO IS READ FROM THE TEMPORARY FOLDER, which is where the bundle already sits unpacked and
		// waiting for the person's decision. It goes through the SAME customs house as after installing
		// -- there is no relaxed version for the enrolment screen, which would be exactly the worse of
		// the two places to relax it.
		marca = Marca(p.Manifest, p.Folder),
		eslogan = p.Manifest.Brand?.Slogan ?? "",
		categoria = p.Manifest.About?.Category ?? "",
		descripcion = p.Manifest.About?.Description ?? "",
		noHace = p.Manifest.About?.DoesNotDo ?? "",

		// THE PROVENANCE, AND THE ONLY CHECK GNOTHI CAN MAKE ON ITS OWN. 'servido' is an OBSERVED fact --
		// which host the bundle came down from -- and 'dominio' is what the institution DECLARES.
		// Comparing them is the same pattern as gnothi_verify_module: declared against real. When they
		// do not match, the screen says so BEFORE the yes.
		razonSocial = p.Manifest.Publisher?.Legal ?? "",
		cedula = p.Manifest.Publisher?.TaxId ?? "",
		dominio = p.Manifest.Publisher?.Domain ?? "",
		version = p.Manifest.Publisher?.Version ?? "",
		// A file came from no server: nothing to compare, and the sheet says so instead of painting
		// a verdict. Null is that third state, the same one Provenance.Matches gives the detail.
		coincide = p.Provenance.Source == EnrollmentSource.File ? null : (bool?)(p.Manifest.Publisher?.MatchesServer(p.ServedBy) ?? true),

		servido = p.ServedBy,
		publicador = p.Publisher,
		journal = p.JournalGoesTo,

		// WHAT THE SCREEN COULD NOT ANSWER: it already said WHERE the record is going to live, and not how
		// much it is going to weigh, how often it updates or how long they keep it -- which is what the
		// person asks before accepting.
		retencion = p.Manifest.Journal?.Retention ?? "",
		tamano = p.Manifest.Journal?.ExpectedSize ?? "",
		refresco = p.Manifest.Journal?.Updates ?? "",

		yaEstaba = p.AlreadyThere,

		// WHAT THIS PACKAGE IS NEXT TO WHAT SHE HAS (Gnothi.Host/UpdatePlan.cs), by name, so the
		// sheet says "update from 2.4.0 to 2.5.0" and lists the change instead of "you already
		// have this". Null the first time.
		actualizacion = p.Update.Kind == UpdateKind.Fresh ? null : new
		{
			tipo = p.Update.Kind switch
			{
				UpdateKind.Same => "misma",
				UpdateKind.Downgrade => "anterior",
				UpdateKind.SameVersionOtherContent => "otro-contenido",
				_ => "actualizacion",
			},
			versionada = p.Update.Versioned,
			versionInstalada = p.Update.InstalledVersion,
			versionNueva = p.Update.IncomingVersion,
			consultasNuevas = p.Update.NewTools.Select(t => new { nombre = t.Name, titulo = t.Title }).ToArray(),
			consultasQuitadas = p.Update.RemovedTools.Select(t => new { nombre = t.Name, titulo = t.Title }).ToArray(),
			capacidadesNuevas = p.Update.NewCapabilities,
			publicadorCambia = p.Update.PublisherChanged,
			publicadorInstalado = p.Update.InstalledPublisher,
			publicadorNuevo = p.Update.IncomingPublisher,
			origenCambia = p.Update.OriginChanged,
			origenInstalado = p.Update.InstalledOrigin,
			origenNuevo = p.Update.IncomingOrigin,
			registroCambia = p.Update.JournalChanged,
			sustratoCambia = p.Update.SubstrateChanged,
			sustratoNuevo = p.Update.IncomingSubstrate,
			hayCambios = p.Update.HasVisibleChanges,
		},
		tools = p.Manifest.Tools.Select(t => new { nombre = t.Name, titulo = t.Title, descripcion = t.Description }),

		// WHERE IT CAME FROM: which way in, which host answered, whether the package still
		// matches the seal the link carried, and how far that host could be tied to the
		// institution -the origin check, by name, so the sheet can say which piece failed.
		procedencia = new
		{
			origen = p.Provenance.Source switch
			{
				EnrollmentSource.Qr => "qr",
				EnrollmentSource.PastedLink => "pegado",
				EnrollmentSource.File => "archivo",
				_ => "enlace",
			},
			servido = p.Provenance.ServedBy,
			sello = p.Provenance.MatchesLink,
			respaldo = p.Provenance.Origin switch
			{
				OriginCheck.Verified => "verificado",
				OriginCheck.NoWellKnown => "sin-publicacion",
				OriginCheck.NotListed => "no-publicado",
				OriginCheck.NotDeclared => "sin-origen-declarado",
				OriginCheck.DeclaresAnother => "otro-origen",
				_ => "sin-origen",
			},
			seguro = p.Provenance.Secure,
			origenDeclarado = p.Provenance.DeclaredOrigin,

			// Whose key signed the package, by name, and since when Mymi knows that key.
			firma = p.Provenance.Signer switch
			{
				SignerCheck.Published => "publicada",
				SignerCheck.Pinned => "conocida",
				SignerCheck.Rotated => "rotada",
				SignerCheck.Unknown => "desconocida",
				SignerCheck.Changed => "cambiada",
				SignerCheck.Revoked => "retirada",
				_ => "sin-firma",
			},
			firmante = p.Provenance.SignerId,
			firmaDesde = p.Provenance.SignedSince,
		},

		// WHAT GNOTHI CHECKED before showing this, so she decides with it in view: the form, the
		// weight, whether it carries an engine, and what it asks for beyond a data module.
		revision = new
		{
			forma = p.Inspection.Form == PackageForm.Hosted ? "hospedado" : "cargo",
			archivos = p.Inspection.Files,
			bytes = p.Inspection.Bytes,
			motorArchivos = p.Inspection.EngineFiles,
			motorBytes = p.Inspection.EngineBytes,
			excesos = p.Inspection.Excesses,
			encendido = p.Inspection.InstallEnabled,
		},
	}));

	// THE FAILURE, SAID IN PLAIN SPANISH AND NOT IN .NET. The person has no reason to read the name of
	// an exception in order to understand that their institution moved the file.
	private static string InPlainWords(Exception ex) => ex switch
	{
		HttpRequestException http when http.StatusCode == System.Net.HttpStatusCode.NotFound =>
			Textos.T("alta.error.no-esta"),

		HttpRequestException => Textos.T("alta.error.sin-conexion"),

		TaskCanceledException or OperationCanceledException => Textos.T("alta.error.tardo"),

		ArgumentException => Textos.T("alta.error.enlace-invalido"),

		// The link carried the seal of the manifest it was made for, and the package served now
		// does not hash to it: what the address serves changed after the code was made. Before
		// the InvalidOperationException arm on purpose: it derives from it.
		PackageSealMismatchException => Textos.T("alta.error.sello"),

		// Signed, and the signature does not hold: somebody touched the package after it was
		// signed. Not "broken": tampered, and that is the word.
		PackageSignatureInvalidException => Textos.T("alta.error.firma"),

		// The institution retired the key this package is signed with. Not installed, no sheet.
		PackageKeyRevokedException => Textos.T("alta.error.llave-retirada"),

		// Another institution's package under the name of a module she has: refused, no sheet.
		PackageCollisionException col => Textos.T("alta.error.colision",
			("modulo", col.Module),
			("instalada", UnaInstitucion(col.InstalledInstitution)),
			("entrante", UnaInstitucion(col.IncomingInstitution))),

		// The origin contradicts the package: refused, no sheet. Which contradiction, in her words.
		PackageNotBackedException nb => Textos.T(nb.Origin == OriginCheck.DeclaresAnother
			? "alta.error.otro-origen"
			: "alta.error.no-reconocido"),

		// What ModuleEnrollment throws -- a bundle with no manifest, an invalid name, a zip that writes
		// outside, a size ceiling -- already comes written to be read. It is passed through as it is.
		InvalidOperationException =>
			"No se pudo instalar el módulo: " + ex.Message,

		_ => "No se pudo instalar el módulo. " + ex.Message,
	};

	/// El nombre de una institucion, o como se la nombra cuando el paquete no lo dice. El
	/// nombre en si es VOZ DE LA INSTITUCION y pasa de largo sin traducir; lo unico que sale
	/// del catalogo es el hueco.
	private static string UnaInstitucion(string nombre) =>
		nombre.Length > 0 ? nombre : Textos.T("alta.error.otra-institucion");

	/// A module from the catalogue by name, or null. It is needed by the screen that puts a web module
	/// on screen, which is the only part of this that is still the platform's.
	/// <summary>
	/// QUÉ MÓDULOS SE REHIDRATAN ANTES DE QUE ALGUIEN PREGUNTE. Vacío = ninguno, que es lo de siempre.
	/// Lo pone quien arma la plataforma, no esta clase: cuáles vale la pena calentar depende del
	/// aparato y de a qué entra la persona, y eso el núcleo no lo sabe.
	/// </summary>
	public IReadOnlyList<string> WarmUp { get; set; } = [];

	/// <summary>
	/// Les pregunta el cursor, que es lo más barato que fuerza el arranque del actor — y es la misma
	/// pregunta que el cartero hace después, así que no inventa trabajo: lo adelanta.
	/// <para>
	/// UNO A LA VEZ, a propósito: rehidratar dos actores en paralelo compite por el disco justo
	/// cuando la pantalla de inicio todavía se está armando, y lo que se busca es que no se note.
	/// </para>
	/// <para>
	/// Nada de esto puede tumbar Gnothi: un módulo que falla al calentarse se salta, y la persona lo
	/// abre como siempre — pagando el arranque, que es exactamente lo que pasaba antes.
	/// </para>
	/// </summary>
	private async Task Rehydrate(CancellationToken ct)
	{
		foreach (string name in WarmUp)
		{
			if (ct.IsCancellationRequested) return;

			Module? m = catalog?.Find(name);
			if (m is null) { log($"calentado: '{name}' no está instalado; se salta"); continue; }

			long started = System.Diagnostics.Stopwatch.GetTimestamp();
			try
			{
				await m.Cursor(ct);
				log($"calentado: {name} listo en "
					+ $"{(System.Diagnostics.Stopwatch.GetTimestamp() - started) * 1000 / System.Diagnostics.Stopwatch.Frequency} ms");
			}
			catch (Exception e) { log($"calentado: {name} no se pudo ({e.GetType().Name}); se abre como siempre"); }
		}
	}

	public Module? Find(string name) => catalog?.Find(name);

	/// The installed modules, so the platform can walk them without seeing the catalogue.
	public IReadOnlyList<Module> Modules => catalog?.Modules ?? [];

	/// The snapshot for the caratula, if this platform asks for it. Android passes null and this does
	/// nothing -- over there the caratula asks live.
	private async Task Fotografiar()
	{
		if (dispatcher is null) return;

		// LOCAL: for the intent that lives inside this app. No App Group. The path has to match
		// FotoLocal.ruta in Intents/GnothiIntents.swift.
		if (paths.FotoLocal)
			await Snapshot.Write(dispatcher, Path.Combine(paths.Files, "instantanea"), log, stop.Token);

		// SHARED: for a sibling app (the Swift spike). It may not exist if the App Group is not
		// provisioned, and that does not stop the local one.
		if (paths.Instantaneas is not null)
			await Snapshot.Write(dispatcher, paths.Instantaneas, log, stop.Token);
	}

	public void Dispose()
	{
		stop.Cancel();
		watch?.Dispose();
		stop.Dispose();
	}

	// ── THE BRIDGE, on the side that knows nothing about platforms ──────────────────────
	//
	// The page calls window.prompt('gnothi', json). Who translates that prompt into a call is the
	// platform; WHAT is done with the request is this, and it is identical on both.
	public string Handle(string json)
	{
		try
		{
			JsonElement req = Json.Parse(json);
			string op = req.GetProperty("op").GetString() ?? "";
			log($"bridge: {op}");

			return op switch
			{
				// LO QUE LA PAGINA DICE CON SUS PROPIAS PALABRAS, en el idioma que la cascara ya
				// resolvio del aparato. La pagina lo PIDE al arrancar en vez de esperar un push, y
				// eso no es una preferencia: el puente es SINCRONO -- window.prompt devuelve ahi
				// mismo -- asi que pidiendolo no hay un instante en que la pantalla este pintada en
				// el idioma equivocado.
				//
				// Tampoco puede ir a buscar el archivo: ui.html se carga como file:// en las dos
				// plataformas, y un fetch a otro file:// lo rechaza el navegador. El escritorio no
				// pasa por aca: lee Textos.T directo, por referencia de proyecto.
				//
				// ESTA RAMA ESTUVO DUPLICADA unas horas, con el mismo nombre y el mismo cuerpo:
				// Geaninna la agrego para la pantalla de inicio vacio el mismo dia que esta rama la
				// agregaba para la pagina entera. Es la misma necesidad, y ahora es una sola.
				"textos" => Textos.Json,

				"modules" => StateJson(),
				"setTool" => SetConsent(
					req.GetProperty("module").GetString() ?? "",
					req.GetProperty("tool").GetString(),
					req.GetProperty("enabled").GetBoolean()),
				"setModule" => SetConsent(
					req.GetProperty("module").GetString() ?? "",
					null,
					req.GetProperty("enabled").GetBoolean()),
				"setProvider" => SetProvider(
					req.GetProperty("name").GetString() ?? "",
					req.GetProperty("url").GetString() ?? "",
					req.GetProperty("model").GetString() ?? ""),
				"setKey" => SetKey(req.GetProperty("key").GetString() ?? ""),
				"clearKey" => ClearKey(),
				"ask" => RunAsk(req),

				// THE TWO ENROLMENT VERBS. They used to be intercepted in each platform because the
				// proposal was state of its screen; now that it lives here, the bridges keep only what
				// really needs a screen and nothing else.
				"altaAceptar" => AcceptEnrollment(req.TryGetProperty("publicadorNuevo", out JsonElement pn) && pn.ValueKind == JsonValueKind.True),
				"altaDescartar" => DiscardEnrollment(),

				// THE LINK FROM INSIDE GNOTHI: pasted by hand, or read off the institution's
				// code by the page's own reader ("via": "qr"). Until 2026-09-08 every enrollment
				// began outside, on the institution's page, and the "Agregar módulo" card only
				// explained. It is also what makes the enrollment testable on a phone while
				// go.gnothi.app does not resolve.
				"alta" => EnrollFromPage(req),

				// THE LIST OF WHAT SHE PICKED: one row chosen -the whole examination follows- or
				// the list closed. The picker itself is the platform's; the list is not.
				"altaElegido" => ChoosePicked(req.GetProperty("indice").GetInt32()),
				"altaCerrarLista" => ClosePicked(),

				// THE CARD'S «HAY UNA VERSIÓN NUEVA»: fetch what the origin publishes and open the
				// update sheet. Her yes on that sheet is what installs, as with any package.
				"actualizar" => StartUpdate(req.GetProperty("module").GetString() ?? ""),
				"quitar" => RemoveModule(
					req.GetProperty("module").GetString() ?? "",
					req.TryGetProperty("journal", out JsonElement qj) && qj.ValueKind == JsonValueKind.True),

				_ => "{}",
			};
		}
		catch (Exception e)
		{
			log($"bridge: {e.GetType().Name}: {e.Message}");
			return "{}";
		}
	}

	private string RunAsk(JsonElement req)
	{
		Ask(req.GetProperty("id").GetString() ?? "", req.GetProperty("question").GetString() ?? "");
		return "\"ok\"";
	}

	// ── what the page asks for ──────────────────────────────────────────────────────────

	// The state as the UI needs it: the modules, their prose, where each switch stands, and the real
	// weight of the journal -- measured from outside, which is the Host's own verb.
	public string StateJson()
	{
		if (catalog is null) return "{\"loading\":true}";

		// OVER A COPY OF THE LIST, AND NOT OVER THE LIST.
		//
		// Measured on the emulator on 2026-09-08: `bridge: InvalidOperationException: Collection was
		// modified; enumeration operation may not execute`. Accepting an enrolment reloads the catalogue
		// -- ModuleCatalog.Load used to Clear() and refill the list -- and if the page asks for the state
		// at that instant, this walk blows up and the screen is left with no modules.
		//
		// The race existed before and was narrow. This method now READS EVERY MODULE'S LOGO off the
		// disk, so it takes longer and the window got wider: the symptom only showed up once the
		// branding came in. Copying the list costs four pointers and closes it.
		var modules = catalog.Modules.ToArray().Select(m =>
		{
			long bytes = 0; int files = 0;
			try
			{
				foreach (FileInfo f in new DirectoryInfo(m.Installation.GrantedFolder)
					.EnumerateFiles("*", SearchOption.AllDirectories))
				{ bytes += f.Length; files++; }
			}
			catch { /* un modulo sin journal todavia no pesa nada */ }

			// THE INSTITUTION'S FACE, resolved once per module: its `brand` if it declared one, and
			// otherwise the manifest.webmanifest its PWA already carries in the bundle. Both paths go
			// through the same customs house.
			Module.BrandLook look = Module.LookOf(m.Manifest, m.Directory, out _);

			// WHAT THE INSTITUTION DECLARED ABOUT ITSELF, for the screen.
			//
			// 'identity' is THE BIG LINE and it is resolved by the manifest -- Manifest.Identity -- and
			// not by this page, because there are three faces and a fallback implemented three times is a
			// fallback that diverges. This line used to be decided by a NAME map inside the html itself,
			// which COVERED the institution when the Host knew it and left it raw when it did not.
			//
			// 'logo' travels as a data: URI already validated by the Host's customs house --
			// Module.LogoDataUri --, not as a path: the page is served as file:// and cannot read other
			// file://, and giving it a path of the module's would be letting the module choose which file
			// gets opened.
			//
			// THE LOGO'S WEIGHT IS PAID ON EVERY REPAINT, because the state is requested whole. With the
			// 64 KB limit and four modules that is around 350 KB in the worst case and ~40 in the typical
			// one. It is the practical reason for the limit, on top of the security one.
			return new Dictionary<string, object?>
			{
				["name"] = m.Name,
				["identity"] = m.Manifest.Identity,
				["title"] = m.Manifest.Title,
				["institution"] = m.Manifest.Institution,
				["icon"] = Icon(m.Manifest),
				["category"] = m.Manifest.About?.Category ?? "",
				// The institution's face, resolved by the Host: its `brand` if it declared one, and
				// otherwise the manifest.webmanifest its PWA already carries in the bundle. Both paths
				// go through the same customs house.
				["shortName"] = m.Manifest.Brand?.ShortName ?? "",
				["slogan"] = m.Manifest.Brand?.Slogan ?? "",
				["brand"] = BrandOf(look),
				["description"] = m.Manifest.About?.Description ?? "",
				["doesNotDo"] = m.Manifest.About?.DoesNotDo ?? "",
				["publisher"] = new Dictionary<string, object?>
				{
					["domain"] = m.Manifest.Publisher?.Domain ?? "",
					["legal"] = m.Manifest.Publisher?.Legal ?? "",
					["taxId"] = m.Manifest.Publisher?.TaxId ?? "",
					["version"] = m.Manifest.Publisher?.Version ?? "",
					["released"] = m.Manifest.Publisher?.Released ?? "",
				},

				// THE ENROLMENT RECORD, so the detail can CONTRAST and not merely repeat.
				//
				// 'matches' comes in three states and not two: true, false and NULL when there is nothing
				// to compare against -- a .gmod imported from a file has no server. The screen has to be
				// able to say "it cannot be known" instead of painting a tick, which is exactly the bug
				// this fixes.
				// WHEN IT ARRIVED, for the order of the home: the most recently added module is the hero
				// and stays there until the next one arrives (Geaninna, 2026-09-09). The enrollment act
				// says when; a module that came in by other means falls back to its installation file.
				["installedAt"] = InstalledAt(m),
				["provenance"] = new Dictionary<string, object?>
				{
					["servedBy"] = m.Installation.Provenance?.ServedBy ?? "",
					["at"] = m.Installation.Provenance?.At ?? "",
					["version"] = m.Installation.Provenance?.Version ?? "",
					["matches"] = m.Installation.Provenance?.Matches(m.Manifest.Publisher),
				},
				["journal"] = new Dictionary<string, object?>
				{
					["retention"] = m.Manifest.Journal?.Retention ?? "",
					["expectedSize"] = m.Manifest.Journal?.ExpectedSize ?? "",
					["updates"] = m.Manifest.Journal?.Updates ?? "",
				},
				["enabled"] = m.Installation.Enabled,
				["journalBytes"] = bytes,
				["journalFiles"] = files,
				// The key it was accepted with, and whether its origin has since retired it.
				["signerKey"] = m.Installation.Signer?.KeyId,
				["signerRevokedAt"] = m.Installation.Signer?.RevokedAt,
				// What the origin publishes and she does not have yet, if Mymi saw it.
				["updateAvailable"] = m.Installation.Update is { } up
					? new Dictionary<string, object?> { ["version"] = up.Version, ["seenAt"] = up.SeenAt }
					: null,
				["tools"] = m.Manifest.Tools.Select(t => new Dictionary<string, object?>
				{
					["name"] = t.Name,
					["title"] = t.Title,
					["description"] = t.Description,
					["enabled"] = !m.Installation.DisabledTools.Contains(t.Name),
				}).ToArray(),
			};
		}).ToArray();

		return Json.Write(new Dictionary<string, object?>
		{
			["modules"] = modules,
			["settings"] = Settings(),
		});
	}

	// THE SWITCH WRITES THE PERSON'S FILE. Not a flag in memory: the very installation.json the desktop
	// Host reads, the watcher watches, and the catalogue reloads from. The UI has no state of its own
	// -- it repaints from whatever the file says, which is why an external edit also shows up on the
	// screen.
	private string SetConsent(string module, string? tool, bool enabled)
	{
		Module? m = catalog?.Modules.FirstOrDefault(x => x.Name == module);
		if (m is null) return StateJson();

		Installation updated;
		if (tool is null)
		{
			updated = m.Installation with { Enabled = enabled };
		}
		else
		{
			List<string> off = [.. m.Installation.DisabledTools.Where(t => t != tool)];
			if (!enabled) off.Add(tool);
			updated = m.Installation with { DisabledTools = off };
		}

		File.WriteAllText(
			Path.Combine(m.Directory, "installation.json"),
			JsonSerializer.Serialize(updated, HostJson.Legible.Installation));

		// Applied NOW, not when the watcher's 300 ms burst settles: the switch the person has just moved
		// has to answer immediately. The watcher fires all the same -- ReloadConsent finds nothing else
		// to change and stays quiet.
		catalog!.ReloadConsent();
		log($"consent: {module}{(tool is null ? "" : "." + tool)} -> {(enabled ? "on" : "off")}");

		// AND THE SNAPSHOT IS REWRITTEN FROM HERE. This line looked redundant -- ConsentWatch.Changed
		// already calls Fotografiar() -- and it is not: it is the only one that runs when the person moves
		// the switch INSIDE the app, which is the normal case.
		//
		// Why the watcher is not enough, and it takes some seeing: the line above has already consumed
		// the change. When the watcher wakes it calls ReloadConsent() again, which now returns ZERO
		// changes, and ConsentWatch stops there -- «if (changes.Count == 0) continue». Which means Changed
		// does not fire and Fotografiar() does not run. The snapshot was left with the old state until the
		// app's next start-up, and Siri and the Shortcut went on answering for a tool the person had
		// already switched off. The watcher is still needed for the other case: the file being edited by
		// something outside.
		//
		// IN THE BACKGROUND on purpose: Fotografiar() asks for every fact through Dispatcher.Serve, which
		// means it wakes modules, and the switch has to answer now.
		//
		// AND CHAINED, not loose: three switches in a row would be three writes fighting over the same
		// temporary file. Chaining them serialises them without blocking anybody.
		fotos = fotos.ContinueWith(_ => Fotografiar(), TaskScheduler.Default).Unwrap();

		return StateJson();
	}

	private Dictionary<string, object?> Settings()
	{
		(string name, string url, string model) = Provider();
		return new Dictionary<string, object?>
		{
			["name"] = name,
			// WITH NO URL THERE IS NO HOST, and this device's provider has no server. `new Uri("")` throws,
			// and since this is serialised INSIDE the response the page is waiting for, the exception did
			// not look like an error: the page only repaints if state comes back, so choosing the local
			// model saved correctly and was never reflected. And TryCreate also covers the url the person
			// types by hand under "Otro".
			["provider"] = string.IsNullOrWhiteSpace(url)
				? "este aparato"
				: Uri.TryCreate(url, UriKind.Absolute, out Uri? u) ? u.Host : url,

			// So the page does not ask for a key where none is needed.
			["local"] = name == ProveedorLocal,

			// Y PARA QUE SE PUEDA VOLVER. El del aparato era el default cuando no habia model.cfg,
			// pero NO estaba en la lista del selector: quien elegia otro proveedor una vez perdia
			// para siempre el unico donde el dato no sale del telefono, que es justamente la puerta
			// que este producto persigue. Se manda la terna entera -identidad, url vacia y modelo-
			// para que la pagina no tenga que saberse ninguna de las tres.
			["proveedorLocal"] = modeloLocal is null ? null : new Dictionary<string, object?>
			{
				["name"] = ProveedorLocal,
				["url"] = "",
				["model"] = ModeloLocal,
			},
			["model"] = model,
			// WHETHER there is a key for THIS provider, never the key itself.
			["keyPresent"] = File.Exists(KeyPath),
			["root"] = catalog?.Root ?? "",
		};
	}

	// THE PROVIDER, CHOSEN BY THE PERSON. Every preset speaks OpenAI's shape, which is what
	// ModelOpenAiCompatible speaks -- so "use ChatGPT instead of that" is a file with a URL and a model
	// name, not code. The constants above are left only as the default when nothing was chosen.
	private string CfgPath => Path.Combine(paths.Files, "model.cfg");

	// WHO DECIDES THE IMPLEMENTATION. Before, nobody did: ModelOpenAiCompatible was always built and
	// `Provider()` only changed the url. Choosing "Claude (Anthropic)" in Settings sent an
	// OpenAI-shaped body to an API that does not speak it, and ModelClaude -- which exists -- was never
	// used from the app at all.
	private IModel Modelo(string proveedor, string url, string model)
	{
		if (proveedor == ProveedorLocal)
			return modeloLocal is not null
				? modeloLocal(Encuadre)
				: throw new InvalidOperationException("este aparato no ofrece modelo propio");

		// By name and not by url: the url can be edited by the person, the provider is what they chose.
		if (proveedor.Contains("Anthropic", StringComparison.OrdinalIgnoreCase))
			return new ModelClaude(KeyPath, model);

		return new ModelOpenAiCompatible(url, model, KeyPath);
	}

	// THE FRAMING, in one single place. It travels to the model exactly as it is: it is not
	// documentation, it is the only thing separating "I read you your numbers" from "I diagnose you".
	// Measured: a capable model respects it 7 out of 7; the phone's slips out of it 2 out of 4 -- see
	// Spikes/Lab.Cruce.
	private const string Encuadre =
		"Sos el asistente de Gnothi. Los datos que devuelven las herramientas son los datos REALES " +
		"de la persona que te pregunta, leidos de los registros que sus instituciones le entregaron " +
		"y que viven en su propio telefono. No vienen de ningun servidor. Contestale sobre SUS " +
		"numeros, en su idioma, en prosa corrida y sin markdown. El veredicto clinico ya viene hecho " +
		"y no es tuyo: no diagnostiques ni recomendes tratamiento.";

	private (string name, string url, string model) Provider()
	{
		try
		{
			if (File.Exists(CfgPath))
			{
				JsonElement e = Json.Parse(File.ReadAllText(CfgPath));
				return (e.GetProperty("name").GetString() ?? "Groq",
						e.GetProperty("url").GetString() ?? ModelUrl,
						e.GetProperty("model").GetString() ?? ModelName);
			}
		}
		catch { /* an unreadable choice falls through to the default */ }

		// THE DEFAULT IS THE DEVICE whenever the device can. It is the only provider where the data never
		// leaves the phone, so starting on another one would be choosing the less private option on the
		// person's behalf. If there is no model of its own, the usual one.
		return modeloLocal is not null
			? (ProveedorLocal, "", ModeloLocal)
			: ("Groq", ModelUrl, ModelName);
	}

	private string SetProvider(string name, string url, string model)
	{
		File.WriteAllText(CfgPath, Json.Write(new Dictionary<string, string>
		{
			["name"] = name, ["url"] = url, ["model"] = model,
		}));
		log($"provider: {name} · {model}");
		return StateJson();
	}

	// THE KEY, MANAGED BY THE PERSON. This is what makes BYOK real: until now the file was put there by
	// a developer over a cable, which is not "bring your own key" -- it is bring MINE. Any key from the
	// chosen provider works, free tier or paid: the file does not notice the difference.
	//
	// PER PROVIDER, on purpose: a Groq key is not an OpenAI one. Changing provider changes which key
	// file gets read, so going to ChatGPT and back loses nothing.
	//
	// Three rules, and they are honoured here: it lives ONLY in the app's private area, it is NEVER
	// sent back to the page (StateJson carries a boolean, not the value), and the log records the event
	// without the key.
	private string KeyPath
	{
		get
		{
			string safe = new string(Provider().name.Where(char.IsLetterOrDigit).ToArray());
			return Path.Combine(paths.Files, $"key-{safe}.key");
		}
	}

	private string SetKey(string key)
	{
		key = key.Trim();
		if (key.Length > 0)
		{
			File.WriteAllText(KeyPath, key);
			log($"model key: saved ({key.Length} chars)");
		}
		return StateJson();
	}

	private string ClearKey()
	{
		if (File.Exists(KeyPath)) File.Delete(KeyPath);
		log("model key: cleared");
		return StateJson();
	}

	// ONE QUESTION, answered by Door 2: the same AgentLoop, the same dispatcher, the same key file. It
	// runs loose; the trace and the answer land on the page as they happen.
	private void Ask(string id, string question)
	{
		_ = Task.Run(async () =>
		{
			try
			{
				(string proveedor, string url, string model) = Provider();
				var loop = new AgentLoop(dispatcher!, Modelo(proveedor, url, model));

				string answer = await loop.Answer(question, line => Push("trace", id, line), stop.Token);
				Push("answer", id, answer);
			}
			catch (Exception e)
			{
				log($"ask failed: {e.GetType().Name}: {e.Message}");
				Push("fail", id, $"{e.GetType().Name}: {e.Message}");
			}
		});
	}

	public void PushState() => Push("state", "", StateJson());

	/// AFTER INSTALLING A MODULE, and both platforms do it by calling in here.
	///
	/// The catalogue is re-read WHOLE: it is the same path by which a change made from outside repaints
	/// the screen. One single source of truth, their files -- and not a catalogue somebody adds an entry
	/// to by hand, which is how two truths get started.
	///
	/// It returns the load's warnings so the caller writes them to ITS log, which is the only thing that
	/// differs between Android and iOS.
	public IReadOnlyList<string> ReloadAfterInstall()
	{
		IReadOnlyList<string> warnings = catalog?.Load() ?? [];
		PushState();

		// AND THE NEW MODULE GETS CHECKED, in the background. Without this line a freshly installed module
		// would not be checked until the app's next start-up -- which is exactly when it helps LEAST,
		// because the moment to discover it declares wrongly is before offering it to the assistant.
		_ = Task.Run(() => VerifyNewModules(stop.Token));

		return warnings;
	}

	/// How many modules the Host sees right now. So it can be said in the log after an enrolment.
	public int ModuleCount => catalog?.Modules.Count ?? 0;

	/// The title the person read on the proposal, so it can be named when confirming.
	public string TitleOf(string module) =>
		catalog?.Modules.FirstOrDefault(m => m.Name == module)?.Manifest.Title ?? module;

	private void Push(string kind, string id, string payload) => push(kind, id, payload);

	/// The javascript the platform has to evaluate in order to push something to the page. It is
	/// assembled here -- and not in each platform -- because whoever serialises the quotes wrongly
	/// breaks the page with no error visible anywhere.
	public static string PushScript(string kind, string id, string payload) =>
		$"gn.push({Json.Write(kind)},{Json.Write(id)},{Json.Write(payload)})";

	// MAKING THE installation.json PATHS LOCAL. Only where the platform asks for it.
	//
	// THE PROBLEM, and it is iOS's alone: armar-paquete.ps1 writes absolute Android paths
	// -- /data/user/0/com.ncubo.gnothi.host/files/... -- because over there they can be known when the
	// package is assembled. On iOS the container carries a GUID that CHANGES ON EVERY INSTALL, so there
	// is no path that can be written down at all: the only valid one is what the process finds out once
	// it is running.
	//
	// TWO FIELDS ARE TOUCHED AND NOTHING ELSE, and both belong to the Host, not to the module:
	//
	//   shell.arguments[0]   where the thing to load is   the path's LEAF is kept, and the mould
	//                                                  depends on the shell type:
	//                                                  <dir>/bin/<dll> a cargo module
	//                                                  <dir>/web       a hosted one
	//   grantedFolder        which folder it is granted  the business name is kept
	//
	// The arguments AFTER the first are not touched: they are not addresses on this machine.
	//
	// The manifest is NEVER touched: that is what the institution declares. And of the installation,
	// only what is an address on this machine gets touched; the switch, the disabled tools and the
	// caratula stay exactly as they came, because those are the person's decisions.
	//
	// IT WOULD BE BETTER IF THIS DID NOT EXIST. The underlying fix is for installation.json to admit
	// RELATIVE paths and for the Host to resolve them against the module's folder -- then the person's
	// file stops carrying a machine's address inside it, which is a property that holds on all three
	// platforms. It was deliberately left for later: it changes the core, and the core has to be run on
	// the A54 again before being called good.
	private void Localizar(string root, string journals)
	{
		if (!Directory.Exists(root)) return;

		foreach (string dir in Directory.GetDirectories(root))
		{
			string file = Path.Combine(dir, "installation.json");
			if (!File.Exists(file)) continue;

			try
			{
				string negocio = Path.GetFileName(dir);
				Installation vieja = JsonSerializer.Deserialize(File.ReadAllText(file), HostJson.Default.Installation)!;

				// The old path's LEAF is the only thing rescued: WHAT gets loaded was decided by whoever
				// assembled the package, and that does not change by moving to another device. Where it is,
				// does.
				string hoja = vieja.Shell.Arguments.Count > 0 ? Path.GetFileName(vieja.Shell.Arguments[0]) : "";
				if (hoja.Length == 0) { log($"localizar: {negocio} no declara que cargar; se deja como esta"); continue; }

				// DONDE GUARDA EL PAQUETE LO QUE `Arguments[0]` NOMBRA, Y CONFUNDIRLO DEJA LA
				// PANTALLA EN BLANCO SIN UN SOLO ERROR. Un modulo `in-process` guarda su dll bajo
				// bin/; `webview`, `hybrid` y `sala` guardan su cara en web/, al lado del
				// installation.json. Componer bin/web da una ruta que no existe,
				// BundleServer.BaseOf no encuentra el index.html y devuelve "/" por omision, y el
				// bundle -publicado para /app/- se sirve en la raiz: 404 en todo, window.gnothi
				// nunca se define, y la pantalla queda en blanco. Medido en el simulador el
				// 2026-09-07 con los cuatro modulos del paquete.
				//
				// VOLVIO A MORDER EL 2026-09-21, con la misma ruta y el mismo sintoma:
				//
				//     examenes: 404 / -> .../Modulos/examenes/bin/web/index.html
				//
				// El arreglo de septiembre 7 preguntaba `Type == "webview"` y listo. Cuando
				// llegaron `hybrid` y `sala` -- que traen cara igual -- cayeron en la rama de
				// bin/. Por eso ahora se enumeran LAS QUE GUARDAN AL LADO en vez de preguntar por
				// una sola: agregar una forma con cara y no ponerla aca la rompe, y el sintoma no
				// dice por que.
				//
				// Y OJO CON LA TENTACION DE ESCRIBIRLO AL REVES -- «todo lo que no sea cargo» --,
				// que es lo primero que se intento: la forma cargo NO se llama "cargo" en este
				// campo, se llama **"in-process"**. Con la cadena equivocada la condicion se
				// invierte en silencio y rompe justamente la que se queria dejar quieta.
				//
				// Tampoco es inofensivo porque «se arregla al reinstalar»: no se arregla. `hoja`
				// sale de `Path.GetFileName(Arguments[0])`, asi que una vez compuesto bin/web
				// vuelve a dar "web" y se recompone igual. El alta deja la ruta BIEN y este
				// metodo la rompe en el arranque siguiente.
				bool guardaSuCaraAlLado = vieja.Shell.Type.ToLowerInvariant() is "webview" or "hybrid" or "sala";
				string local = guardaSuCaraAlLado ? Path.Combine(dir, hoja) : Path.Combine(dir, "bin", hoja);

				// EL SEGUNDO ARGUMENTO SE PASA TAL CUAL, Y ESO SI ES CORRECTO. En un modulo con
				// cara el segundo es "<url del publicador>|<recurso>|<id>", que no es una
				// direccion de esta maquina sino lo que la institucion metio en su manifiesto.
				// Reemplazar el array entero se lo comia, y el cartero quedaba sin publicador al
				// que pedirle.
				//
				// EL TERCERO SI ES UNA RUTA DE ESTA MAQUINA, Y SE QUEDABA VIEJO. La hibrida y la
				// sala escriben [web, publicador, dll], y ese dll vive en el contenedor igual que
				// el web/. Al reescribir solo el primero, el tercero seguia apuntando al
				// contenedor ANTERIOR -- que en iOS cambia de GUID en cada instalacion, que es
				// justamente por lo que este metodo existe.
				//
				// Medido el 2026-09-21, despues de reinstalar: arg0 en el contenedor nuevo, arg2
				// en el viejo, y la sala sin nada que servir --
				//
				//     sala: 500 modulo/examenes/indice.json -- DirectoryNotFoundException
				//     sala/cargar examenes: {"ok":false,"motivo":"HttpRequestException: TypeError: Load failed"}
				//
				// Se rescatan las DOS ultimas hojas -- la carpeta y el archivo, o sea
				// `dominio/<X>.dll` -- por el mismo criterio que el primero: QUE se carga lo
				// decidio quien armo el paquete, DONDE esta lo decide este aparato. Asi no hay
				// que quemar aca el nombre de la carpeta que la convencion del paquete elige.
				string[] args = [.. vieja.Shell.Arguments];
				if (args.Length > 2 && args[2].Length > 0)
				{
					string archivo = Path.GetFileName(args[2]);
					string carpeta = Path.GetFileName(Path.GetDirectoryName(args[2]) ?? "");
					if (archivo.Length > 0)
						args[2] = carpeta.Length > 0
							? Path.Combine(dir, carpeta, archivo)
							: Path.Combine(dir, archivo);
				}

				Installation nueva = vieja with
				{
					Shell = vieja.Shell with { Arguments = [local, .. args.Skip(1)] },
					GrantedFolder = Path.Combine(journals, negocio),
				};

				File.WriteAllText(file, JsonSerializer.Serialize(nueva, HostJson.Legible.Installation));
				log($"localizar: {negocio} -> {nueva.GrantedFolder}");
			}
			catch (Exception e)
			{
				// A module that could not be localised is going to answer "no data", and that shows up on
				// the screen. Bringing the whole start-up down over one of them would be worse.
				log($"localizar: {Path.GetFileName(dir)} FALLO {e.GetType().Name}: {e.Message}");
			}
		}
	}

	// Copying from the download area into the private one. Deliberately dumb: the Host interprets
	// nothing of what it copies.
	private static int Copy(string source, string target)
	{
		int n = 0;
		Directory.CreateDirectory(target);
		foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
		{
			string destination = file.Replace(source, target);
			Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
			File.Copy(file, destination, overwrite: true);
			n++;
		}
		return n;
	}
}
