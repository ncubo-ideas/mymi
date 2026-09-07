using Gnothi.Host;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Gnothi.Movil;

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

		// El 404 sale por el log del Host, prefijado con el modulo. Es la mitad que el watchdog
		// de la CSP no cubre: aquel dice que se RECHAZO algo, este que NO ESTA. Ver
		// BundleServer.Serve.
		var server = new BundleServer(Bundle, root) { NotFound = m => Log($"{Name}: {m}") };
		await Create(server, OriginFor(root), ct);

		// Wait for the actor to be hydrated. If the module never says so, the question fails
		// saying exactly that instead of hanging forever.
		await ready.Task.WaitAsync(TimeSpan.FromSeconds(60), ct);
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

		if (courier is null)
		{
			Log($"{Name} asked for after={from} and has no registered publisher");
			return JsonSerializer.Serialize(new { events = Array.Empty<object>(), last = from });
		}

		return await courier.Pull(from);
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

	public async Task<IWebModule> Live(
		string module, string bundle, string journal, string publisher, CancellationToken ct)
	{
		if (live.TryGetValue(module, out TModule? already)) return already;

		TModule fresh = Create(module, bundle, journal, publisher);
		if (!live.TryAdd(module, fresh)) { fresh.Evict(); return live[module]; }

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
		string bundle = args.Count > 0 ? args[0] : "";
		string publisher = args.Count > 1 ? args[1] : "";

		await Live(m.Manifest.Module, bundle, m.Installation.GrantedFolder, publisher, ct);
		return live[m.Manifest.Module];
	}

	public void Evict(string module)
	{
		// A module Sonia is looking at is NOT evicted from under her. The evict-on-demand rule
		// stays in force for every other one.
		if (live.TryGetValue(module, out TModule? watching) && watching.Visible) return;

		if (live.TryRemove(module, out TModule? going)) going.Evict();
	}
}
