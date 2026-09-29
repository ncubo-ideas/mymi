using Gnothi.Host;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Gnothi.MobileShared;

// A LIVE WEB MODULE, minus the engine. Everything that is a decision lives here; what each
// platform fills in is four things, and all four are "how do I talk to this engine":
// create it, run a line of JS in it, tear it down, and say where its origin is.
//
// THE POINT OF THE SPLIT is not tidiness. The bridge protocol, the journal guard, the cursor
// and the request correlation are the module's CONTRACT: if Android and iOS each had their
// own copy, the day one of them changed the module would behave differently on two phones and
// nothing would say so. There is one copy, and the platform cannot reach it.
//
// WHAT IS DELIBERATELY NOT TRANSLATED: everything the module sees. `gnothiPuente`, `contesta`,
// `listo`, `window.gnothi.*`, and the json keys the journal answers with are the institution's
// API -- already published, already running in four modules. Renaming them here would break
// every bundle that exists. The code around them is English; the wire is not.
internal abstract class WebModule : IWebModule
{
	// THE MODULE'S SIDE OF THE BRIDGE. The same names on both platforms, because the module is
	// the same file on both platforms.
	private const string BridgeObject = "gnothiPuente";
	private const string BridgeAnswer = "contesta";

	protected string Name { get; }

	protected string Bundle { get; }

	protected Action<string> Log { get; }

	private readonly string journal;
	private readonly GnothiCourier? courier;

	// The module says so when its actor is hydrated. Until then nothing can be asked of it:
	// the screen is drawn before the actor exists, and guessing by pixels already failed once.
	private readonly TaskCompletionSource ready =
		new(TaskCreationOptions.RunContinuationsAsynchronously);

	private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> pending = new();
	private int next;

	// The verbs that came in through the bridge, as a set. See Serve and Faltó.
	private readonly ConcurrentDictionary<string, byte> oídas = new();

	// While it is on screen it is not evicted, even at its ~200 MB. It is the one concession to
	// the evict-on-demand rule, and it lasts as long as the person is looking.
	internal bool Visible { get; set; }

	protected WebModule(
		string name, string bundle, string journal, string publisher, ISecretStore store, Action<string> log)
	{
		Name = name;
		Bundle = bundle;
		Log = log;
		this.journal = Path.GetFullPath(journal);
		Directory.CreateDirectory(this.journal);

		// With no endpoint there is no courier, and that is legitimate: a module can live off
		// what its journal already holds. What it CANNOT do is go and fetch it on its own.
		courier = publisher.Length > 0
			? new GnothiCourier(name, publisher, new GnothiCredential(name, store, log), log)
			: null;
	}

	// ---------------------------------------------------------------- what the platform fills in

	// Create the engine, wire `server` as the thing that answers its requests, and load `url`.
	// It returns when the view exists and the navigation was ordered -- not when the module is
	// ready; that is what `ready` is for.
	protected abstract Task Create(BundleServer server, string url, CancellationToken ct);

	// Run one line of JavaScript in the module. Fire and forget: every answer comes back
	// through the bridge, never through a return value.
	protected abstract void RunJs(string js);

	protected abstract void Teardown();

	protected abstract bool Alive { get; }

	// THE MODULE'S ORIGIN, which is the one place the two platforms genuinely disagree.
	// Android intercepts a virtual https host before the network. iOS has to use a scheme
	// handler, and whether that origin is a secure context is the open question that decides
	// the shape of the whole thing (03 §2).
	protected abstract string OriginFor(string basePath);

	// ---------------------------------------------------------------- the neutral part

	internal async Task Start(CancellationToken ct)
	{
		string root = BundleServer.BaseOf(Bundle);
		Log($"{Name}: the bundle declares its base at {root}");

		// The 404 goes out through the Host's log, prefixed with the module. It is the half the CSP's
		// watchdog does not cover: that one says something was REJECTED, this one that it IS NOT THERE.
		// See BundleServer.Serve.
		var server = new BundleServer(Bundle, root) { NotFound = m => Log($"{Name}: {m}") };
		await Create(server, OriginFor(root), ct);

		// Wait for the actor to be hydrated. If the module never says so, the question fails
		// saying exactly that instead of hanging forever.
		try
		{
			await ready.Task.WaitAsync(TimeSpan.FromSeconds(60), ct);
		}
		catch (TimeoutException)
		{
			throw new TimeoutException($"'{Name}' nunca dijo 'listo': {Faltó(server)}");
		}
	}

	// WHAT WAS MISSING, said with the only things the Host really knows: how much it served and what
	// came in through the bridge. It does not guess the cause; it splits the silence into the three
	// cases that used to look the same.
	//
	// WHY IT EXISTS. On 2026-09-16 a hosted module stopped opening and all the Host could say was
	// «could not query the module: TimeoutException». Finding the cause cost patching the module`s
	// interop ON THE PHONE to see that the app loaded whole -- 227 files -- and never called the
	// bridge: it was asking the browser where to store instead of asking Gnothi whether it was hosted.
	// That one line -- «it loaded N files and never called the bridge» -- is exactly what the Host had
	// in front of it and did not say.
	//
	// WHAT IT DOES NOT DO: blame. «It never called the bridge» is a fact; why it did not is the app's
	// business.
	private string Faltó(BundleServer server)
	{
		if (server.Documents == 0)
			return "no pidió ni su propio documento: la vista no llegó a navegar al bundle";

		if (oídas.IsEmpty)
			return $"cargó {server.Served} archivo(s) y NUNCA llamó al puente: su app no preguntó "
				+ "si estaba hospedada, así que no está hablando con Gnothi";

		return $"cargó {server.Served} archivo(s) y habló por el puente ({string.Join(", ", oídas.Keys)}), "
			+ "pero su actor nunca terminó de hidratarse";
	}

	public Task<string> Ask(string tool, string argumentsJson, CancellationToken ct) =>
		Request($"window.gnothi.call({Js(tool)}, {Js(argumentsJson)})", ct);

	public Task<string> Tools(CancellationToken ct) => Request("window.gnothi.tools()", ct);

	// THE HANDOFF from the standalone installation: Sonia's assertions, not the provider's
	// events. It comes in through its own verb and NOT through deliver, because these are local
	// commands with no event number: pushing them down the same path would force us to invent
	// one and would knock the log out of line. Idempotency differs too, and lives on the
	// module's side (13 §5.3).
	internal Task<string> Handoff(string assertionsJson, CancellationToken ct) =>
		Request($"window.gnothi.traspasar({Js(assertionsJson)})", ct);

	// EVERYTHING THE MODULE ANSWERS COMES BACK BY CALLBACK, and not as the return value of the
	// evaluate call, for a concrete reason: the module's surface returns promises -- it enters
	// C# through DotNet.invokeMethodAsync -- and neither engine's evaluate awaits them: both
	// would hand back an unresolved Promise.
	private async Task<string> Request(string expression, CancellationToken ct)
	{
		if (!Alive) throw new InvalidOperationException($"module '{Name}' is not live");

		string id = Interlocked.Increment(ref next).ToString();
		var wait = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
		pending[id] = wait;

		string js = $"Promise.resolve({expression})"
			+ $".then(function (r) {{ {BridgeObject}.{BridgeAnswer}({Js(id)}, r, null); }})"
			+ $".catch(function (e) {{ {BridgeObject}.{BridgeAnswer}({Js(id)}, null, String(e)); }});";

		RunJs(js);

		try { return await wait.Task.WaitAsync(TimeSpan.FromSeconds(30), ct); }
		finally { pending.TryRemove(id, out _); }
	}

	// Called by the bridge when the module has answered.
	internal void Answer(string id, string? json, string? error)
	{
		if (!pending.TryRemove(id, out TaskCompletionSource<string>? wait)) return;

		if (error is not null) wait.TrySetException(new InvalidOperationException(error));
		else wait.TrySetResult(json ?? "");
	}

	// The answer to a request the MODULE made. It goes back through the channel its interop
	// listens on.
	internal void Reply(string id, string? json, string? error)
	{
		if (!Alive) return;

		string body = json is null ? "null" : Js(json);
		string reason = error is null ? "null" : Js(error);

		RunJs($"window.gnothi.respuesta({Js(id)}, {body}, {reason})");
	}

	internal void Ready() => ready.TrySetResult();

	// THIS MODULE'S SURFACE, when it runs in process. It is set by whoever opens the face, and stays
	// null in a hosted module -- there the page IS the surface, and asking Gnothi would be asking it to
	// answer itself.
	//
	// It is a delegate and not a reference to the dispatcher on purpose: WebModule knows neither the
	// Dispatcher nor Module, and has no reason to. It receives a function that answers.
	internal Func<string, string, CancellationToken, Task<string>>? Surface { get; set; }

	// AND WHERE WHAT THE POSTMAN BRINGS GETS DROPPED OFF. Null in a hosted module: there the page
	// carries the engine and records it itself, which is what `window.gnothi.deliver` does.
	internal Func<string, CancellationToken, Task<string>>? Entrega { get; set; }

	// HOW FAR THE SURFACE HAS ALREADY READ. Only the hybrid form wires it: in the hosted one the page
	// has its own actor and sends its cursor, which is correct because there it DOES know.
	internal Func<CancellationToken, Task<long>>? Cursor { get; set; }

	private Task<string> Query(string tool, string argsJson)
	{
		if (Surface is null)
			throw new InvalidOperationException(
				"este modulo no tiene superficie en proceso: en la forma hospedada la pagina ES "
				+ "la superficie. 'consultar' solo existe en la forma hibrida.");

		if (tool.Length == 0) throw new ArgumentException("'consultar' sin tool");

		return Surface(tool, argsJson, CancellationToken.None);
	}

	internal void Evict()
	{
		Teardown();
	}

	// ---------------------------------------------------------------- the bridge, dispatched once

	// EVERYTHING THE MODULE CAN ASK GNOTHI, in one switch. Each platform's bridge is reduced to
	// parsing its own transport and calling this: `@JavascriptInterface` methods on Android, a
	// `postMessage` payload on iOS.
	//
	// It lives here and not in the platform for the same reason the CSP does: this list IS the
	// module's API. Two copies would be two APIs the day one of them grows a verb.
	internal void Serve(string op, string id, string? a, string? b)
	{
		// WHICH VERBS WERE HEARD, and nothing else: no counts, no arguments, no order. It is so the
		// timeout can say «it spoke through the bridge and never said listo» instead of «it did not
		// answer». `violation` does NOT count: it is sent by the watchdog Gnothi injects, not by the
		// bundle, and if it counted, a module that never called the bridge would look as if it had. See
		// Faltó.
		if (op != "violation") oídas[op] = 0;

		switch (op)
		{
			// The module says its actor is hydrated and can take questions.
			case "listo": Ready(); return;

			// The module's answer to a question Gnothi asked it.
			case "contesta": Answer(id, a, b); return;

			case "listar": Attend(id, List); return;
			case "leer": Attend(id, () => Read(a ?? "")); return;
			case "escribir": Attend(id, () => Write(a ?? "", b ?? "")); return;

			// The only one that does network, and the reason none of these can be synchronous.
			case "pull": AttendAsync(id, () => Pull(a ?? "0")); return;

			// THE SEVENTH OF THE CONTRACT, and the only one that runs the arrow backwards.
			//
			// In a hosted module the page IS the surface: Gnothi asks it (`window.gnothi.call`)
			// and it answers. In a HYBRID the page is only the face -- it carries no engine, by
			// customs rule -- so it asks, and the answer comes from the module's own surface
			// loaded in process, on Gnothi's engine.
			//
			// IT DOES NOT NAME A MODULE, on purpose. The page can only ask about the module it
			// belongs to. A `module` parameter here would let a face ask about somebody else's
			// journal, and no rule downstream would stop it -- the Host would see a legitimate
			// question from a legitimate module.
			case "consultar": AttendAsync(id, () => Query(a ?? "", b ?? "{}")); return;

			// NOT PART OF THE MODULE'S API, even though it arrives by the same door. No bundle
			// calls this: it is the watchdog Gnothi injects into the document (BundleServer)
			// talking back to Gnothi. The six above are contract and run in four published
			// bundles; this one is ours and its name is not frozen the way theirs are.
			case "violation": Violation(a ?? ""); return;

			default: Reply(id, null, $"unknown bridge verb: {op}"); return;
		}
	}

	// HOW MANY TIMES THE ENGINE REFUSED THIS MODULE something, since it was opened. It is the
	// difference between a policy that is declared and one that is known to be applying: a
	// module whose count never moves is either well behaved or NOT BEING WATCHED, and until now
	// there was no way to tell those apart.
	internal int Violations => violations;

	private int violations;

	// A REFUSAL, AS A FACT. The engine already blocked it; nothing here decides anything. What
	// this adds is that Gnothi SAW it, with the directive, what was blocked and where.
	//
	// IT DOES NOT GO IN THE JOURNAL, on purpose. The journal is the person's record as the
	// institution wrote it; a containment event is Gnothi's own and belongs to the Host, not to
	// the institution's history. Writing it there would put Gnothi's opinion inside the
	// institution's book.
	private void Violation(string report)
	{
		Interlocked.Increment(ref violations);

		try
		{
			using JsonDocument parsed = JsonDocument.Parse(report);
			JsonElement r = parsed.RootElement;

			Log($"{Name}: REFUSED by {Field(r, "directive")} -- {Field(r, "blocked")}"
				+ $" (from {Field(r, "source")}:{Field(r, "line")}); {violations} so far");
		}
		catch (JsonException)
		{
			// An unreadable report is still a refusal, and losing the count would be worse than
			// losing the detail.
			Log($"{Name}: REFUSED, and the report was unreadable; {violations} so far");
		}
	}

	private static string Field(JsonElement o, string name) =>
		o.TryGetProperty(name, out JsonElement v) && v.ValueKind is not JsonValueKind.Null
			? v.ToString()
			: "?";

	// RETURNS NOW AND ANSWERS LATER. A failure travels as an error, not as a JS exception.
	//
	// Android needs this because the methods of an injected object are SYNCHRONOUS and the page
	// freezes until they return. iOS does not have that problem -- `postMessage` is already one
	// way -- but it keeps the same shape so that the module cannot tell the two apart, which is
	// the whole rule: one architecture, two platforms.
	private void Attend(string id, Func<string> work) => _ = Task.Run(() =>
	{
		try { Reply(id, work(), null); }
		catch (Exception ex) { Reply(id, null, $"{ex.GetType().Name}: {ex.Message}"); }
	});

	private void AttendAsync(string id, Func<Task<string>> work) => _ = Task.Run(async () =>
	{
		try { Reply(id, await work(), null); }
		catch (Exception ex) { Reply(id, null, $"{ex.GetType().Name}: {ex.Message}"); }
	});

	// ---------------------------------------------------------------- what the module asks for

	// THE JOURNAL, against the files in Gnothi's container. One directory per module, and NOT
	// in OPFS: the fourth pillar -- Gnothi answers what no module can answer about itself --
	// requires being able to look at it from outside the web engine (11 §5.1).
	//
	// GNOTHI COMPOSES NOTHING: it writes the bytes the module already wrote, as they are. It is
	// the module's arm, not a second author.
	internal string List()
	{
		var rutas = new List<string>();
		foreach (string file in Directory.EnumerateFiles(journal, "*", SearchOption.AllDirectories))
			rutas.Add(Path.GetRelativePath(journal, file).Replace(Path.DirectorySeparatorChar, '/'));

		return JsonSerializer.Serialize(new { ok = true, rutas, motivo = "" });
	}

	internal string Read(string path)
	{
		string full = InJournal(path);
		if (!File.Exists(full))
			return JsonSerializer.Serialize(new { ok = false, contenido = "", motivo = "no existe" });

		return JsonSerializer.Serialize(new
		{
			ok = true,
			contenido = Convert.ToBase64String(File.ReadAllBytes(full)),
			motivo = "",
		});
	}

	internal string Write(string path, string contentBase64)
	{
		string full = InJournal(path);
		Directory.CreateDirectory(Path.GetDirectoryName(full)!);
		File.WriteAllBytes(full, Convert.FromBase64String(contentBase64));

		return JsonSerializer.Serialize(new { ok = true, motivo = "" });
	}

	// THE COURIER, wired. The cursor is put there by the actor and Gnothi relays it with ITS
	// credential against ITS endpoint (13 §8).
	//
	// With no endpoint registered an EMPTY envelope is returned and not an error, deliberately:
	// it is already measured that the module starts without being able to talk to the
	// publisher, and this way it still opens showing what its journal already holds -- which is
	// exactly what owning the data is for. An error here would leave it unopened.
	internal async Task<string> Pull(string after)
	{
		if (!long.TryParse(after, out long from) || from < 0)
			throw new ArgumentException($"invalid cursor: {after}");

		// IN THE HYBRID FORM THE CURSOR IS NOT SET BY THE PAGE, AND THAT IS NOT AN OPTIMISATION.
		//
		// The face has neither engine nor actor: it CANNOT know how far it had got, so it sent zero and
		// the postman pulled the whole history on every refresh. Measured on the phone on 2026-09-14
		// over cargas: `leidos=114 aplicados=0 rechazados=114`. The Check guard meant nothing got
		// duplicated, which is why the symptom was not bad data -- it was traffic and battery, the kind
		// of defect you cannot see by looking at the screen.
		//
		// AND THE UNDERLYING REASON IS CONTAINMENT, not efficiency: if the cursor came from the face, it
		// WOULD BELONG TO THE FACE. A page could ask from zero -- pulling everything -- or from a high
		// number -- skipping events that would never be applied, and keeping an incomplete record with
		// nothing to say so. By asking the surface, the face asks «bring me what is new» and what is new
		// is decided by whoever keeps the record.
		//
		// `Cursor` is optional: a module that does not declare it answers zero and behaves as before.
		if (Cursor is not null)
		{
			long suyo = await Cursor(CancellationToken.None);
			if (suyo != from) Log($"{Name}: la cara pidio desde {from}; la superficie va en {suyo}");
			from = suyo;
		}

		if (courier is null)
		{
			Log($"{Name} asked for after={from} and has no registered publisher");
			return JsonSerializer.Serialize(new { events = Array.Empty<object>(), last = from });
		}

		string sobre = await courier.Pull(from);

		// IN THE HYBRID FORM THE ENVELOPE DOES NOT GO BACK TO THE PAGE, and that is the only difference
		// this path has between the two forms.
		//
		// The face has no engine: handing it the events would be giving it something it cannot record. So
		// they are delivered to the surface -- the same envelope, the same postman, the same endpoint
		// pinned at install time -- and the face is told HOW MUCH came in, which is the only thing it
		// needs in order to know whether asking again is worth it.
		if (Entrega is null) return sobre;

		string resultado = await Entrega(sobre, CancellationToken.None);
		Log($"{Name}: entrega a la superficie -> {resultado}");

		return JsonSerializer.Serialize(new { entregado = true, resultado });
	}

	// A module's path CANNOT climb out of its journal. That is Gnothi's responsibility, not
	// trust in the module.
	private string InJournal(string path)
	{
		string full = Path.GetFullPath(
			Path.Combine(journal, path.Replace('/', Path.DirectorySeparatorChar)));

		if (!full.StartsWith(journal, StringComparison.Ordinal))
			throw new UnauthorizedAccessException($"path outside the module: {path}");

		return full;
	}

	protected static string Js(string value) => JsonSerializer.Serialize(value);
}

// THE HOST OF WEB MODULES: one engine per module, created ON DEMAND and evictable.
//
// On demand and not at start-up, because measured each one weighs ~150 MB with the runtime
// inside (11 §12). And evicting them loses nothing: the module does not wake up on its own,
// does not notify, has no process of its own. No network, no service worker, no worker.
//
// The bookkeeping is the same on both platforms; what changes is which WebModule gets built.
//
// IT IS GENERIC IN THE MODULE TYPE so that the screen can ask for the module it knows how to
// show -- putting a view on screen is the one thing that is genuinely per-platform -- without
// a cast. A cast here would compile and then throw at runtime the day someone registers the
// wrong host, which is exactly the kind of failure this codebase keeps refusing to accept.
internal abstract class WebModuleHost<TModule> : IWebModuleHost where TModule : WebModule
{
	private readonly ConcurrentDictionary<string, TModule> live = new();

	protected abstract TModule Create(string module, string bundle, string journal, string publisher);

	public bool IsLive(string module) => live.ContainsKey(module);

	// `cablear` RUNS BETWEEN Create AND Start, AND THAT ORDER IS THE POINT.
	//
	// Start waits for the page to say `listo()`, and a hybrid face says listo and ASKS in the same
	// tick -- it has no actor to hydrate, so there is nothing to hold it up. If the surface were wired
	// after Start, those first questions would arrive with the delegate still null and the screen would
	// open with "this module has no in-process surface". Measured on the phone on 2026-09-11: both
	// cards showed exactly that.
	// The interface one, for whoever has nothing to wire: the shell, when the assistant asks and the
	// face is not even open.
	public Task<IWebModule> Live(
		string module, string bundle, string journal, string publisher, CancellationToken ct)
		=> Live(module, bundle, journal, publisher, ct, null);

	public async Task<IWebModule> Live(
		string module, string bundle, string journal, string publisher, CancellationToken ct,
		Action<TModule>? cablear)
	{
		if (live.TryGetValue(module, out TModule? already)) return already;

		TModule fresh = Create(module, bundle, journal, publisher);
		if (!live.TryAdd(module, fresh)) { fresh.Evict(); return live[module]; }

		cablear?.Invoke(fresh);

		await fresh.Start(ct);
		return fresh;
	}

	// OPEN A MODULE FROM THE CATALOG. The shell knows the bundle and the publisher because it
	// reads them from the installation; the screen does not, so it takes them from there by the
	// same route. If it was already live -- because the assistant asked it something -- it is
	// returned as is and opening is instant.
	internal async Task<TModule> Open(Gnothi.Host.Module m, CancellationToken ct)
	{
		IReadOnlyList<string> args = m.Installation.Shell.Arguments;
		// NORMALISED, the same way WebViewShell already does it. BundleServer.Resolve compares
		// every requested file against Path.GetFullPath, which on Windows answers with
		// backslashes: a bundle written with forward slashes in installation.json made every
		// file "outside the module" and the face died at HostNameNotResolved, with nothing
		// naming the cause. Measured on the desktop 2026-09-14; on Android the two forms never
		// differed, which is why it took a third platform to see it.
		string bundle = args.Count > 0 ? Path.GetFullPath(args[0]) : "";
		string publisher = args.Count > 1 ? args[1] : "";

		// THE FACE ASKS IN BOTH FORMS THAT CARRY NO ENGINE: the hybrid and the box. The only thing that
		// changes between them is WHERE the question lands -- in the Host's process or inside a wasm --
		// and the face neither knows that nor needs to. See B12.
		// AND IT IS DECIDED BY THE MODULE'S FORM, NOT BY WHETHER THE BOX IS SWITCHED ON. There used
		// to be a `|| BoxShell.Preferred` here -it came in with the box, in `dac6af6`- and it made
		// `--ez caja true` give EVERY module the hybrid's wiring, including a HOSTED one. Measured
		// on the phone on 2026-09-18, municipalidad hosted with the box on:
		//
		//     municipalidad: la cara pidio desde 23; la superficie va en 0
		//     municipalidad: pull after=0 -> 3126 bytes
		//     NotSupportedException: this shell does not take deliveries
		//
		// Two damages in one: the face's cursor (23) was replaced by the 0 that `IShell.Cursor`'s
		// default returns -that is, the postman went back to bringing the whole history on every
		// refresh, which is exactly the defect `Pull`'s comment claims to have fixed- and then the
		// envelope was handed to a shell that takes no deliveries, with the exception in plain sight
		// of the person. With the box off, the same module did `pull after=23 -> 23 bytes` and did
		// not fail.
		//
		// And the disjunct was not needed: a module IN A BOX still declares `hybrid` in its
		// installation.json -the box is the host, not a form of the package, see C10- so the first
		// clause already covered it.
		// AND `sala` IS HERE FOR THE SAME REASON AS `caja` AND NOT AS A FAVOUR: RoomShell implements
		// View, Deliver and Cursor -- the three things `Wire` wires -- and its own comment says
		// that without them "la pantalla no abre", because the default of `IShell.View` throws. A
		// module that declares `"type": "sala"` reaches ChooseShell's case (Module.cs) and would have
		// got the hosted wiring: its face asks and gets the exception, on the person's screen.
		//
		// IT HAD NOT FIRED YET BY LUCK, NOT BY DESIGN. The sala is switched on today by forcing it
		// over modules whose installation.json says `hybrid`, so the first clause covered them, and
		// no installation declares `sala` yet. That the omission was an oversight and not a decision
		// is settled by Shell.cs, which DOES name `sala` alongside webview and hybrid in
		// `guardaSuCaraAlLado`: the file that saves the face knew, the file that wires it did not.
		string forma = m.Installation.Shell.Type;
		bool asksTheHost = forma.Equals("hybrid", StringComparison.OrdinalIgnoreCase)
			|| forma.Equals("caja", StringComparison.OrdinalIgnoreCase)
			|| forma.Equals("sala", StringComparison.OrdinalIgnoreCase);

		// IT IS WIRED BEFORE STARTING, not after: a hybrid face says `listo()` and asks in the same tick,
		// because it has no actor to hydrate. See Live.
		await Live(m.Manifest.Module, bundle, m.Installation.GrantedFolder, publisher, ct,
			fresco => { if (asksTheHost) Wire(fresco, m); });

		return live[m.Manifest.Module];
	}

	// WHAT A HYBRID FACE MAY ASK THE HOST FOR, and they are two different things with different rules:
	// asking for its views, and asking the postman to bring what is new.
	private static void Wire(TModule abierto, Gnothi.Host.Module m)
	{
		abierto.Surface = async (vista, argsJson, c) =>
			{
				// IT GOES THROUGH View AND NOT THROUGH Invoke, and that is the most important correction on
				// this whole path: the declared tools are what the ASSISTANT may ask -- coarse, and
				// switchable off one by one -- whereas a screen needs series, points and reference lines.
				// Measured over exámenes: 2 tools against 21 reads of the domain. Routed through Invoke, the
				// face could only ask for the two, and could never draw a trend.
				//
				// AND IT DOES NOT GO THROUGH Allows: switching off a query decides what the assistant may
				// ask, it does not forbid the institution from showing the person their own file on the
				// screen they opened.
				//
				// JsonOnly and not whatever the page asks for: a face paints json, and letting it choose the
				// format would give it a lever the other doors do not.
				object answer = await m.View(vista, Argumentos(argsJson), Accepts.JsonOnly, c);
				return answer as string ?? JsonSerializer.Serialize(answer);
			};

		// AND THE DELIVERY, which does NOT go through Allows, and that is on purpose: switching off a
		// query is deciding WHAT GETS ANSWERED, not stopping what the institution already published from
		// arriving. A module with every query switched off is still the person's record, and going on
		// filling it is what makes switching them back on worth anything.
		abierto.Entrega = (sobre, c) => m.Deliver(sobre, c);
		abierto.Cursor = c => m.Cursor(c);
	}

	// The arguments arrive as the json the page wrote. An empty object and a broken json are the same
	// thing to the dispatcher: no tool receives anything.
	private static IReadOnlyDictionary<string, object?>? Argumentos(string argsJson)
	{
		if (string.IsNullOrWhiteSpace(argsJson)) return null;

		try { return JsonSerializer.Deserialize<Dictionary<string, object?>>(argsJson); }
		catch (JsonException) { return null; }
	}

	public void Evict(string module)
	{
		// A module Sonia is looking at is NOT evicted from under her. The evict-on-demand rule
		// stays in force for every other one.
		if (live.TryGetValue(module, out TModule? watching) && watching.Visible) return;

		if (live.TryRemove(module, out TModule? going)) going.Evict();
	}
}
