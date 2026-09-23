using System.Text.Json;


namespace Gnothi.Host;

// THE SALA: the module is cargo -- its .dll files and nothing else -- but it does not run in
// Gnothi's process. It runs inside the web engine, in ONE document that belongs to Gnothi and to
// no institution, with the .NET runtime and the engine compiled to wasm.
//
// WHY IT EXISTS. On a phone, loading a third party's .dll into the app's process leaves the
// institution's code with DIRECT access to the system APIs. The sala takes that away without
// losing what the cargo form gave: the module still weighs 200 KB because Gnothi lends it the
// substrate, and lends it ONCE for everybody.
//
// ── WHAT CAN BE CLAIMED ABOUT THE STORES AND WHAT CANNOT ────────────────────────────────────
//
// THIS COMMENT USED TO SAY MORE THAN WAS KNOWN. It said that "both stores make an exception for
// code a web engine interprets" and that "the sala is that exception". That came from Apple's
// old §3.3.2, WHICH NO LONGER EXISTS WITH THAT WORDING. All three texts read on 2026-09-16, see
// `Documentacion/Jonathan/B-arquitectura/B11-lo-que-dicen-las-tiendas.md`:
//
//   - GOOGLE PLAY is the clear text, and the deciding word is "indirect". Here the institution's
//     code is executed by the .NET runtime compiled to wasm, inside the web engine's document,
//     and its access to the system is indirect: MEASURED on 2026-09-17 with a probe that runs as
//     a module -- it does not see the device's disk, it does not leave the origin, and it has no
//     way to call the bridge. By that text, the sala passes.
//
//   - APPLE IS AN OPEN QUESTION, not a yes. Today it says "interpreted code" with three
//     conditions, naming no engine. And B11 leaves TWO things unknown that are exactly the
//     sala's case: whether wasm counts as interpreted code, and whether an IL .dll run by Mono's
//     interpreter counts. There is no way to answer that by reading: it is answered by
//     submitting.
//
//   - AND 4.7.2 -- not exposing native APIs to software that does not ship in the binary -- hits
//     the hosted form through its bridge. It hits the sala less, because the domain inside has
//     NO bridge: it cannot reach `DotNet.invokeMethodAsync`. Less is not none.
//
//   - And the STOREFRONT clause (3.3.1(B)(c)) hits the whole of Gnothi, not one form.
//
// In other words: the sala is BETTER POSITIONED than the hybrid and, against Google's text, at
// least as well as the hosted one. It is not "the exception the stores name". None of this is a
// verdict: these are texts read, not a review passed.
//
// THE TWO THINGS THIS CLASS SUSTAINS, and neither is optional:
//
//   1 · ONE CALL IN FLIGHT. The module's identity does not travel in the message: it comes from
//       the frame of the call the Host opened. That only identifies while there is a single call
//       at a time, so it is serialised here instead of trusting that nobody crosses it.
//
//   2 · ONE JOURNAL MOUNTED AT A TIME. Measured: with several folders mounted at once, any code
//       inside the sandbox reads the neighbour's. With a single one, the attempt fails by
//       itself. So before loading a module the previous one is evicted -- and in that order,
//       because deleting the folder with the actor alive kills the runtime from a finaliser.
internal sealed class RoomShell : IShell
{
	// THE SALA'S HOST, put there by the platform just as with WebViewShell.Host. The Host does
	// not know there is a WebView on the other side: it asks for text and receives text.
	internal static IRoomHost? Host;

	// Shared by ALL instances, because the sala is one: the Dispatcher creates one Module -- and
	// therefore one shell -- per module, but on the other side there is a single document.
	private static readonly SemaphoreSlim OneAtATime = new(1, 1);
	private static string loaded = "";

	// THE CEILING FOR ONE REQUEST TO THE SALA. Generous on purpose: what was measured on an A54
	// and on the emulator is 36-120 ms to load and to evict, so ten seconds are not reached by
	// working, only by being stuck.
	//
	// It is not `const` so the test can lower it: without that, claiming that a stuck module does
	// not hang the sala would cost ten seconds of suite, and an expensive test ends up not run.
	//
	// ── KNOWN FRAGILITY (2026-09-21), AND IT BELONGS TO NO ONE PLATFORM ──────────────────────
	//
	// THIS CEILING ALSO WRAPS THE DOCUMENT'S START-UP: `Ask` in both salas begins with
	// `await Arrancar(ct)`, so the first verb pays for how long THE ENGINE TAKES TO EXIST out of
	// a budget that exists to measure how long THE MODULE takes to answer.
	//
	// IT DOES NOT PINCH TODAY. Measured on an iPhone 17 Pro simulator / iOS 26.5, five relaunches
	// with the module already installed: the document starts in 0.86 to 1.41 s and `cargar`
	// comes out in 53 to 102 ms. Against 10 s there is an order of magnitude of room, and on an
	// A54 the substrate starts in 439 ms.
	//
	// BUT THE FAILURE MODE IS STABLE, NOT TRANSIENT, and that is why it is written down: if the
	// start-up goes past 10 s, the `Discard` below throws the document away and the retry
	// starts COLD all over again. There is never an attempt that inherits a warm document. It was
	// seen: with this Mac loaded -- other processes eating the memory -- it was four cycles in a
	// row without a single answer, and the symptom is «the sala never answers».
	//
	// WHAT IS PROPOSED FOR DISCUSSION, and why it was not touched here: `Discard` exists
	// because a module can hang its own eviction, which is code the institution chose. A document
	// that HAS NOT LOADED ANY MODULE YET cannot be stuck on that -- it is Gnothi's substrate
	// starting -- so discarding it recovers nothing. The two shapes are not discarding a document
	// that is still starting, or taking the start-up out of the per-verb budget. Raising `Ceiling`
	// is NOT the way out: it fits the contract to the slowest device tested and leaves the
	// feedback standing.
	//
	// The detail, with both sets of numbers, in `Gnothi.iOS/HALLAZGOS-LA-SALA-EN-IOS.md` §4.
	internal static TimeSpan Ceiling = TimeSpan.FromSeconds(10);

	private readonly string name;
	private readonly string entry;
	private readonly string granted;

	public RoomShell(string name, Shell shell, string grantedFolder)
	{
		this.name = name;

		// WHERE THE DLL IS, ACCORDING TO HOW THE MODULE WAS INSTALLED. In the hybrid form the
		// enrolment writes [web, endpoint, dll] -- the web goes first because the whole side that
		// opens the screen reads it, and putting the dll there would make opening the module try
		// to serve a .dll as if it were the bundle's folder. In the cargo form the dll is the only
		// thing and goes at [0].
		//
		// The sala takes THE SAME package in both cases: only which slot the path comes out of
		// changes.
		entry = shell.Arguments.Count > 2 ? shell.Arguments[2]
			: shell.Arguments.Count > 0 ? shell.Arguments[0]
			: "";

		granted = grantedFolder;
	}

	public bool IsRunning => string.Equals(loaded, name, StringComparison.Ordinal);

	public async Task<object> Invoke(string tool, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct)
	{
		await OneAtATime.WaitAsync(ct);
		try
		{
			await Ensure(ct);

			string args = JsonSerializer.Serialize(arguments ?? new Dictionary<string, object?>());
			string raw = await Ask("consultar", name, tool, args, ct);

			using JsonDocument doc = JsonDocument.Parse(raw);
			if (!doc.RootElement.GetProperty("ok").GetBoolean())
				throw new InvalidOperationException(doc.RootElement.GetProperty("motivo").GetString());

			// THE CONTENT TRAVELS OPAQUE, the same as in the other three shells: the Host does not
			// look at it, it forwards it. Here it arrives already flattened into dictionaries and
			// primitives from inside the sala, which is where the module's Type lives.
			return JsonSerializer.Deserialize<object>(
				doc.RootElement.GetProperty("contenido").GetRawText())!;
		}
		finally { OneAtATime.Release(); }
	}

	public async Task<IReadOnlyList<string>> ActualTools(CancellationToken ct)
	{
		await OneAtATime.WaitAsync(ct);
		try
		{
			await Ensure(ct);

			string raw = await Ask("repertorio", name, "", "", ct);
			using JsonDocument doc = JsonDocument.Parse(raw);
			if (!doc.RootElement.GetProperty("ok").GetBoolean()) return [];

			return [.. doc.RootElement.GetProperty("consultas").EnumerateArray()
				.Select(x => x.GetString() ?? "")];
		}
		finally { OneAtATime.Release(); }
	}

	// WHAT THE INSTITUTION'S FACE ASKS FOR, and it is a different set from the assistant's.
	//
	// WITHOUT THIS THE SCREEN DOES NOT OPEN. The default of IShell.View throws
	// NotSupportedException -- "only the hybrid form has a face that asks" -- and the module's
	// face is left with that exception. The sala DOES have something to answer with: the surface
	// running inside brings its [View] just as it brings its [Query], and the substrate already
	// discovers them.
	//
	// They remain two paths and not one parameter, the same as in the contract: with a single
	// one, a screen's query could come in through the assistant's door and the other way round.
	public async Task<object> View(string name, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct)
	{
		await OneAtATime.WaitAsync(ct);
		try
		{
			await Ensure(ct);

			string args = JsonSerializer.Serialize(arguments ?? new Dictionary<string, object?>());
			return Content(await Ask("vista", this.name, name, args, ct));
		}
		finally { OneAtATime.Release(); }
	}

	// WHAT THE POSTMAN LEAVES. Gnothi made the network call with ITS credential and hands the
	// module an envelope already read; in the hybrid form the face has no engine, so the surface
	// is the only one that can record it. In the sala the same happens, one floor further in.
	public async Task<string> Deliver(string envelopeJson, CancellationToken ct)
	{
		await OneAtATime.WaitAsync(ct);
		try
		{
			await Ensure(ct);

			string raw = await Ask("entregar", name, envelopeJson, "", ct);
			await Persist(name, raw, ct);

			object? r = Content(raw);
			return r as string ?? JsonSerializer.Serialize(r);
		}
		finally { OneAtATime.Release(); }
	}

	// HOW FAR IT HAS ALREADY READ, so the postman asks from there on and does not bring the whole
	// history down on every refresh.
	public async Task<long> Cursor(CancellationToken ct)
	{
		await OneAtATime.WaitAsync(ct);
		try
		{
			await Ensure(ct);

			object? r = Content(await Ask("cursor", name, "", "", ct));
			return r is JsonElement j && j.TryGetInt64(out long v) ? v : 0L;
		}
		catch (Exception) { return 0L; }
		finally { OneAtATime.Release(); }
	}

	// THE EVICTION IS ON THE INSIDE. Releasing this shell need not throw the document away: the
	// sala stays alive for the next module, which is the whole gain.
	public ValueTask DisposeAsync() => ValueTask.CompletedTask;

	// THE JOURNAL'S RETURN TRIP.
	//
	// Inside the sala there is no disk: the module writes in memory and does not know there is a
	// folder on the other side. What it wrote travels in the response, and Gnothi saves it. The
	// module does NOT choose where it lands: it gives a relative path and the host anchors it to
	// ITS folder, with the same fence the hosted form's bridge already has.
	//
	// It fails LOUDLY on purpose: if this fell silently, the person would see their refresh
	// applied and on coming back it would be gone, which is the worst way to lose a record.
	private async Task Persist(string module, string raw, CancellationToken ct)
	{
		using JsonDocument doc = JsonDocument.Parse(raw);
		if (!doc.RootElement.TryGetProperty("escrituras", out JsonElement writes)) return;
		if (writes.ValueKind != JsonValueKind.Array || writes.GetArrayLength() == 0) return;

		foreach (JsonElement e in writes.EnumerateArray())
		{
			string path = e.GetProperty("ruta").GetString() ?? "";
			string content = e.GetProperty("contenido").GetString() ?? "";
			if (path.Length == 0) continue;

			await TheHost().Save(module, path, content, ct);
		}
	}

	// THAT THE EVICTION REALLY HAPPENED. Two things, and both have to be true: that the operation
	// did not fail, and that the folder was left emptied. The second is the one that matters,
	// because it is the border.
	private static void Confirm(string farewell, string module)
	{
		using JsonDocument doc = JsonDocument.Parse(farewell);

		if (!doc.RootElement.TryGetProperty("ok", out JsonElement ok) || !ok.GetBoolean())
			throw new InvalidOperationException(
				$"no se pudo desalojar '{module}', asi que no se carga otro encima: "
				+ (doc.RootElement.TryGetProperty("motivo", out JsonElement m) ? m.GetString() : "sin motivo"));

		if (!doc.RootElement.TryGetProperty("vaciada", out JsonElement emptied) || !emptied.GetBoolean())
			throw new InvalidOperationException(
				$"el journal de '{module}' no quedo vaciado, asi que no se carga otro encima: "
				+ (doc.RootElement.TryGetProperty("desmontada", out JsonElement d) ? d.GetString() : "sin detalle"));
	}

	// WHAT IS LEFT OF THE PREVIOUS LOADS, AND WHAT HAPPENS IF SOMETHING IS.
	//
	// The substrate already reported this on loading, because containment is claimed by
	// attempting it. But it was a LOG LINE: if it had ever said more than zero bytes, the module
	// went in all the same and nobody found out until reading the logcat of an old run.
	//
	// Now it is a condition. Zero bytes is the sala's border, and a border that is not checked is
	// not a border: it is an expectation.
	//
	// THE FILES MAY INDEED REMAIN, and the difference matters. Unmounting EMPTIES instead of
	// deleting -- deleting kills the runtime from a finaliser -- so zero-byte folders are left
	// behind. Since the move to neutral names those folders are called `c1`, `c2`: they say how
	// many loads there were, not whose. That is the known and bounded residue; bytes are not.
	private static void Neighbourhood(JsonElement answer, string module)
	{
		if (!answer.TryGetProperty("vecinos", out JsonElement neighbours)
			|| neighbours.ValueKind != JsonValueKind.Array) return;

		long bytes = 0;
		foreach (JsonElement v in neighbours.EnumerateArray())
			if (v.TryGetProperty("bytes", out JsonElement b) && b.TryGetInt64(out long n)) bytes += n;

		if (bytes > 0)
			throw new InvalidOperationException(
				$"no se carga '{module}': quedaron {bytes} bytes de cargas anteriores montados en la sala, "
				+ $"y eso es la frontera rota -- {neighbours.GetRawText()}");
	}

	// The sala's answer is always {ok, content} or {ok:false, reason}. A single place that
	// opens it, so that the four paths fail alike.
	private static object? Content(string raw)
	{
		using JsonDocument doc = JsonDocument.Parse(raw);
		if (!doc.RootElement.GetProperty("ok").GetBoolean())
			throw new InvalidOperationException(doc.RootElement.GetProperty("motivo").GetString());

		return doc.RootElement.TryGetProperty("contenido", out JsonElement c)
			? JsonSerializer.Deserialize<object>(c.GetRawText())
			: null;
	}

	private IRoomHost TheHost() => Host
		?? throw new InvalidOperationException(
			"no hay sala registrada: la app tiene que poner RoomShell.Host antes de despachar.");

	// NO REQUEST TO THE SALA IS UNBOUNDED, AND THAT WAS THE HOLE.
	//
	// Inside the eviction runs code THE MODULE CHOSE: the substrate calls its Dispose(), its
	// Cerrar(), its Close(). An infinite loop there cannot be interrupted from the inside -- the
	// web engine is single-threaded, there is no other thread that could preempt it -- and the
	// damage does not stay in the module: it also keeps `OneAtATime` taken, so EVERYBODY stops
	// answering until somebody kills the app.
	//
	// They are two different failure modes and they had to be fixed separately: the substrate's
	// `finally` covers the one that THROWS, this one covers the one that DOES NOT COME BACK.
	//
	// FROM THE OUTSIDE IT CAN BE DONE, and the right shape is not an ordinary timeout but
	// THROWING THE DOCUMENT AWAY. The web engine runs in its own process, so destroying it takes
	// the loop with it; and what the module had mounted lives INSIDE the document, not on disk,
	// so discarding it leaves nothing to unmount. The border is closed by the platform, and not
	// by the module's cooperation -- which is what was being asked of it.
	//
	// WHAT IS LOST is whatever that module had not returned yet, and it is ITS OWN: the one who
	// jams its own eviction pays with its own record, and not the neighbour's. The query in
	// flight fails loudly -- the same criterion as `Confirm` -- and the next one starts a new
	// sala.
	private async Task<string> Ask(string verb, string module, string a, string b, CancellationToken ct)
	{
		try { return await TheHost().Ask(verb, module, a, b, ct).WaitAsync(Ceiling, ct); }
		catch (TimeoutException)
		{
			// The variable first and the document afterwards: if discarding were to throw, the next
			// attempt has to believe nobody is loaded instead of retrying an impossible eviction.
			loaded = "";

			// Without `ct`: the one that was cancelled may be exactly the request that got stuck,
			// and leaving the document standing dead is worse than any cancellation.
			await TheHost().Discard($"'{module}' no contesto a '{verb}'", CancellationToken.None);

			throw new InvalidOperationException(
				$"la sala no contesto a '{verb}' de '{module}' en {Ceiling.TotalSeconds:0} s; se tiro el "
				+ "documento, y con el todo lo que ese modulo tenia montado");
		}
	}

	// ONE MODULE AT A TIME. The previous one is evicted BEFORE the next one mounts its folder,
	// and that is why the border between journals exists: inside the sandbox there is nothing
	// left of the neighbour to read.
	//
	// ── THE DECISION NOT TO RESERVE THE SLOT ────────────────────────────────────────────────
	//
	// A module's face and the assistant both come in through here, so they evict each other.
	// MEASURED on the emulator: 42 chained queries to the same module cost 728 ms; the same 42
	// with one query for another module interleaved between each cost 8,264 ms. Eleven times
	// more.
	//
	// It was considered that the open screen should HOLD the slot and that a query for another
	// module should fail meanwhile. IT WAS DISCARDED, and for the product's own reason: asking
	// about any module -- and CROSSING two -- is what Gnothi exists for. A question that crosses
	// compras with examenes touches both in the same turn; with the slot reserved by an open
	// screen, that question could not be answered. It would be switching off the product to
	// protect a drawing.
	//
	// And the worst case measured is not the real case: it demands one query between EACH of the
	// 42 round trips. A single query during an opening costs ~450 ms, which is not seen. The cost
	// that IS always paid is the crossing's: one module change per module the question touches,
	// ~200 to 450 ms each, against the seconds the model takes to answer.
	//
	// If some day a screen looks slow, the number is already measured and the place to touch is
	// this one.
	private async Task Ensure(CancellationToken ct)
	{
		if (string.Equals(loaded, name, StringComparison.Ordinal)) return;

		if (loaded.Length > 0)
		{
			// THE EVICTION BRINGS BACK WHAT THE MODULE WROTE, and it has to be saved BEFORE loading
			// the next one. Otherwise what it recorded evaporates when its folder is emptied: the
			// refresh looked like it worked and afterwards went back to the previous state.
			string farewell = await Ask("desalojar", loaded, "", "", ct);
			await Persist(loaded, farewell, ct);

			// AND ONLY NOW DOES IT STOP BEING SERVABLE. Between the eviction and the next one's load
			// there is no module servable by the sala's origin.
			await TheHost().Release(loaded, ct);

			// FAIL CLOSED, AND IT IS A CHANGE OF CRITERION.
			//
			// This used not to look at the answer: it evicted, cleared its variable and loaded the
			// next one even if the eviction had failed. Since the sala's border IS the emptying, a
			// failed eviction left it open IN SILENCE.
			//
			// Now a query fails loudly rather than answer correctly with the border broken. A stuck
			// module blocks the rest, and that is the right thing for a containment property: the
			// alternative is answering as if nothing were wrong while the neighbour is exposed.
			Confirm(farewell, loaded);
			loaded = "";
		}

		await TheHost().Prepare(name, entry, granted, ct);

		string raw = await Ask("cargar", name, "", "", ct);
		using JsonDocument doc = JsonDocument.Parse(raw);
		if (!doc.RootElement.GetProperty("ok").GetBoolean())
			throw new InvalidOperationException(doc.RootElement.GetProperty("motivo").GetString());

		// AND IF THE BORDER IS FOUND BROKEN, THE DOCUMENT GOES WITH IT.
		//
		// `Neighbourhood` throwing is right -- zero bytes IS the border -- but throwing and leaving
		// everything standing left the sala BROKEN FOR GOOD: `loaded` never gets set, so the next
		// request believes nobody is loaded, does not evict, mounts on top, and sees the same bytes
		// again. Every attempt fails the same way and adds one more mount. Nothing recovered it
		// until somebody killed the app.
		//
		// The way out is the one this class already uses for the ceiling, and for the same reason:
		// what is mounted lives in the DOCUMENT'S memory, so throwing the document away is the only
		// thing that actually closes the border -- and the next request starts a clean sala.
		//
		// NOTHING OF THE PERSON'S IS LOST BY DOING IT. Whatever the previous module wrote was
		// already persisted and confirmed above, before this load; the module just loaded has not
		// been asked anything yet. What goes is the residue, which is precisely the problem.
		//
		// How it is reached without anyone misbehaving: `Ask` cancels THE WAIT, not the work --
		// the request already went out to the document. A `cargar` cancelled halfway leaves the
		// module mounted inside with `loaded` empty on this side, and the next load finds a
		// neighbour. Read, not measured.
		try { Neighbourhood(doc.RootElement, name); }
		catch
		{
			loaded = "";
			await TheHost().Discard(
				$"quedaron bytes de cargas anteriores al cargar '{name}'", CancellationToken.None);
			throw;
		}

		loaded = name;
	}
}

// WHAT THE PLATFORM FILLS IN. Two things and nothing else: telling the sala where to get a
// module's bytes from, and making a request of it.
//
// IT RECEIVES NO WEBVIEW AND NOTHING OF THE WEB ENGINE: the Host talks in text, which is the
// same seam it already has with the hosted shell.
internal interface IRoomHost
{
	// Where this module's .dll files live and where its journal is on the device. The sala asks
	// for them through its own origin and this implementation serves them; the module never opens
	// a file.
	Task Prepare(string module, string entry, string grantedFolder, CancellationToken ct);

	Task<string> Ask(string verb, string module, string a, string b, CancellationToken ct);

	// IT STOPS BEING SERVABLE. The host serves the bytes of ONE module through its own origin, and
	// this tells it which one stops being that one. Between one module and the next there is
	// none: if something asked for a journal in that window, the answer is that it does not exist.
	Task Release(string module, CancellationToken ct);

	// WHAT THE MODULE WROTE, back to the person's folder. The path is RELATIVE and the one who
	// anchors it is the host: the module cannot name where it lands, which is the same rule
	// `WebModule.InJournal` sustains on the other side.
	Task Save(string module, string path, string contentBase64, CancellationToken ct);

	// THROWING THE WHOLE DOCUMENT AWAY, and it is the only thing that recovers a sala that does
	// not answer. The shell asks for it when a request went past the ceiling; the host releases
	// the slot -- a module without a document is not servable by the origin --, fails the pending
	// waits, and the next request creates a new document.
	//
	// It does not have to confirm anything or unmount anything: what the module had mounted lives
	// in the document's memory, so it goes with it.
	Task Discard(string reason, CancellationToken ct);
}
