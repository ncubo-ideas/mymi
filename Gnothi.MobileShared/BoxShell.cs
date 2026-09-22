using System.Text.Json;

namespace Gnothi.Host;

// THE FOURTH SHELL: the face in the WebView and the surface INSIDE A WASM BOX.
//
// IT IS THE HYBRID ONE WITH THE SURFACE MOVED. The institution's package is the same, the face is
// the same, the journal is the same and the contract's five markers are the same. The only thing
// that changes is WHO EXECUTES the institution's C# and WHAT IT CAN REACH from there:
//
//   hybrid  face (WebView) --consultar--> Gnothi --InProcessShell.View--> dll in Gnothi's process
//   box     face (WebView) --consultar--> Gnothi ------border-----------> dll in a wasm box
//
// Gnothi sits in the middle in both cases, so THE FACE NEVER FINDS OUT. It is the property B8
// describes: `consultar` does not name a module, the page can only ask about its own.
//
// WHY. In-process, the institution's dll runs with Gnothi's permissions: measured on 2026-09-16,
// seven of eight escape attempts work -- including reading and writing another institution's journal
// and going out to the internet. Inside the box it cannot, and not out of discipline: wasm has no
// syscalls, so whatever is not mounted for it DOES NOT EXIST. See B12.
internal sealed class BoxShell : IShell
{
	private readonly WebViewShell face;
	private readonly string module;
	private readonly string wasm;
	private readonly string grantedFolder;
	private readonly string privateFolder;

	private readonly SemaphoreSlim gate = new(1, 1);
	private BoxedInstance? alive;

	// WHERE THE SUBSTRATE'S .wasm COMES FROM, and it is the Host's decision, not the package's: the
	// artifact depends on the wasmtime version and on the processor, so it cannot travel in a .gmod.
	// Whoever assembles the platform provides it; on Android, MainActivity when it copies its assets.
	internal static string SubstrateWasm { get; set; } = "";

	internal static string PrivateFolder { get; set; } = "";

	// WHETHER THIS CUT IS SWITCHED ON. It is this stage's measuring affordance: it lets an
	// already-installed hybrid module run inside the box WITHOUT rewriting the person's
	// installation.json, which is a file of theirs. Whoever assembles the platform switches it on,
	// through Module.ShellFactory.
	//
	// IT IS NOT A PRODUCT SWITCH: the day the box is a form in its own right, the shell comes out of
	// the Host's file like the other three and this goes away.
	internal static bool Preferred { get; set; }

	// WHERE THE MODULE'S PACKAGE IS, and which assembly holds its surface. Both come out of
	// `Shell.Arguments[2]` -- the entry dll -- which is the same field InProcessShell reads in the
	// hybrid form. Splitting it into folder + file name is this class's job because the cage mounts
	// a FOLDER and the guest is told a FILE.
	private readonly string packageFolder;
	private readonly string entry;

	public BoxShell(string name, Shell shell, string granted)
	{
		// The face reads [0] and [1] as they are: to WebViewShell this shell is indistinguishable
		// from a hosted module's, just as in the hybrid one. It finds out nothing.
		face = new WebViewShell(name, shell, granted);

		module = name;
		grantedFolder = granted;
		wasm = SubstrateWasm;
		privateFolder = PrivateFolder;

		// THE SAME ARGUMENT THE HYBRID FORM USES FOR ITS DLL, and reading it here is what lets the
		// SAME package run in the box: nothing in the .gmod changes, nothing is re-declared.
		string dll = shell.Arguments.Count > 2 ? shell.Arguments[2] : "";
		if (dll.Length == 0)
			throw new InvalidOperationException(
				$"caja: '{name}' no declara que ensamblado cargar (Shell.Arguments[2] vacio); "
				+ "un paquete sin superficie no se puede correr en caja");

		packageFolder = Path.GetDirectoryName(dll)
			?? throw new InvalidOperationException($"caja: no pude ubicar el paquete de '{name}' desde '{dll}'");
		entry = Path.GetFileName(dll);
	}

	// IT RUNS IF THERE IS A BOX, and the box opens when somebody actually asks. Same rule as the
	// hybrid one: the face may not be alive and the module still answers the assistant.
	public bool IsRunning => alive is not null;

	// THE BOX OPENS ONCE AND STAYS, which is what makes the actor useful: 288 ms for the first
	// question, 1 ms for the rest. Opening it per query would mean rehydrating 21 times to paint one
	// screen.
	private async Task<BoxedInstance> Box(CancellationToken ct)
	{
		if (alive is not null) return alive;

		await gate.WaitAsync(ct).ConfigureAwait(false);
		try
		{
			if (alive is not null) return alive;
			WasmBox.Prepare(wasm, privateFolder);
			return alive = WasmBox.Open(module, grantedFolder, privateFolder, packageFolder, entry);
		}
		finally { gate.Release(); }
	}

	// THE ASSISTANT'S DOOR. It goes to the `preguntar` verb, which on the other side only reaches
	// methods marked [Query]. A view asked for through here is not denied: it DOES NOT EXIST. Measured.
	public async Task<object> Invoke(
		string tool, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct)
		=> Unwrapped(await (await Box(ct)).Ask("preguntar", tool, Payload(arguments, accepts), ct).ConfigureAwait(false), tool);

	// THE SCREEN'S DOOR, and it is ANOTHER SET. It returns the json AS IT IS: the face's bridge does
	// `answer as string ?? Serialize(answer)`, so a string that is already json passes verbatim and
	// is not serialised twice.
	public async Task<object> View(
		string name, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct)
		=> Raw(await (await Box(ct)).Ask("consultar", name, Payload(arguments, accepts), ct).ConfigureAwait(false), name);

	// WHAT THE POSTMAN DROPS OFF. It is the only verb that writes, and the module has no network with
	// which to have fetched it itself: it was never given wasi-http. Measured: the cursor went from
	// 303 to 401 and the real journal grew from 8 175 to 8 555 bytes.
	public async Task<string> Deliver(string envelopeJson, CancellationToken ct)
		=> Raw(await (await Box(ct)).Ask("entregar", "", envelopeJson, ct).ConfigureAwait(false), "entregar")
			?.ToString() ?? "";

	// HOW FAR IT HAS ALREADY READ, so the postman asks from there onwards. It comes from the same
	// actor that does the recording, which is what makes it trustworthy.
	public async Task<long> Cursor(CancellationToken ct)
	{
		object? itSays = Raw(await (await Box(ct)).Ask("cursor", "", "", ct).ConfigureAwait(false), "cursor");
		return long.TryParse(itSays?.ToString(), out long n) ? n : 0L;
	}

	// WHAT THE MODULE SAYS IT OFFERS, for checking the declared against the real. The difference from
	// the other shells is who extracts it: over there the Host opens the dll and reads it; here it is
	// read by code running INSIDE the box, over the real attributes, and published.
	public async Task<IReadOnlyList<string>> ActualTools(CancellationToken ct)
	{
		using JsonDocument d = JsonDocument.Parse((await Box(ct)).Declaration);
		if (!d.RootElement.TryGetProperty("declara", out JsonElement declares)
			|| !declares.TryGetProperty("consultas", out JsonElement queries))
			return [];

		var names = new List<string>();
		foreach (JsonElement q in queries.EnumerateArray())
			if (q.GetString() is { Length: > 0 } n) names.Add(n);
		return names;
	}

	internal WebViewShell Face => face;

	// A QUESTION'S ENVELOPE, and it has two fields on purpose:
	//
	//     { "args": { … what the asker asked … }, "accepts": "…" }
	//
	// `accepts` goes OUTSIDE `args` and is filled in by the Host. If it travelled inside the asker's
	// dictionary, a query could pick its own wrapper -- and the format does not belong to the
	// question, it belongs to the Host. It is the same rule InProcessShell.Bind applies by reading
	// the reserved parameter BEFORE the arguments.
	private static string Payload(IReadOnlyDictionary<string, object?>? arguments, Accepts accepts)
	{
		string args = arguments is null || arguments.Count == 0 ? "{}" : JsonSerializer.Serialize(arguments);
		return "{\"args\":" + args + ",\"accepts\":" + JsonSerializer.Serialize(accepts.ToString()) + "}";
	}

	// THE BORDER'S ANSWER, which is always {"ok":bool,"dice":...}. An `ok:false` belongs to the
	// MODULE -- a view asked for through the wrong door, a method that threw -- and comes up as an
	// exception, which is how the Host already treats a module that does not answer.
	private static JsonElement Unwrap(string answer, string what)
	{
		using JsonDocument d = JsonDocument.Parse(answer);
		if (d.RootElement.TryGetProperty("ok", out JsonElement ok) && !ok.GetBoolean())
			throw new InvalidOperationException(
				$"caja: '{what}' -- {d.RootElement.GetProperty("dice").GetString()}");
		return d.RootElement.GetProperty("dice").Clone();
	}

	// FOR THE FACE: the json as it is, without going through objects. It gets serialised exactly once.
	private static object? Raw(string answer, string what)
	{
		JsonElement says = Unwrap(answer, what);
		return says.ValueKind == JsonValueKind.String ? says.GetString() : says.GetRawText();
	}

	// FOR THE ASSISTANT: the same flattening InProcessShell does -- dictionaries, lists and
	// primitives -- because the MCP front end expects that and not a string with json inside.
	private static object Unwrapped(string answer, string what) => Flatten(Unwrap(answer, what)) ?? "";

	private static object? Flatten(JsonElement e) => e.ValueKind switch
	{
		JsonValueKind.Object => e.EnumerateObject().ToDictionary(p => p.Name, p => Flatten(p.Value)),
		JsonValueKind.Array => e.EnumerateArray().Select(Flatten).ToList(),
		JsonValueKind.String => e.GetString(),
		JsonValueKind.Number => e.TryGetInt64(out long l) ? l : e.GetDouble(),
		JsonValueKind.True => true,
		JsonValueKind.False => false,
		_ => null,
	};

	public async ValueTask DisposeAsync()
	{
		// The face first, for the same reason as in the hybrid one: holding the WebView up while the
		// box closes helps nobody.
		await face.DisposeAsync().ConfigureAwait(false);
		alive?.Dispose();
		alive = null;
		gate.Dispose();
	}
}
