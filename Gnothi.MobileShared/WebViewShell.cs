using System.Text.Json;

namespace Gnothi.Host;

// THE THIRD SHELL. The Host already knew how to load a module as a child process (stdio) and
// as an assembly in its own process (in-process). This is the mobile one: the module is the
// institution's PWA running inside a WebView that Gnothi controls (specification 11).
//
// And `Dispatcher` does not change a single line. Consent, metering and provenance are still
// the Host's; the only thing that changes is THROUGH WHAT the module is asked. That is the
// whole point of `IShell` existing.
//
// WHY THIS IS THE ONE THAT GOES ON MOBILE, and not in-process: the stores forbid loading
// downloaded executable code inside the process -- Play says so about dex, JAR and IL; Apple
// about everything that is not WebKit -- and the one exception both of them name is
// JavaScript in a WebView. `InProcessShell` loads the institution's IL by reflection: on a
// phone that cannot be published.
//
// THE HOST REGISTERS ITSELF, it is not built here. `ChooseShell` is static and has no
// Context, and a WebView needs one. The Activity -- or the iOS ViewController -- registers who
// knows how to create them at start-up, the same way the web module registers its surface.
internal sealed class WebViewShell : IShell
{
	internal static IWebModuleHost? Host;

	private readonly string module;
	private readonly string bundle;
	private readonly string journal;
	private readonly string publisher;

	// The shell's first argument is the web bundle's folder. Same field stdio uses for the
	// executable and in-process for the dll, different meaning: here it is what gets SERVED.
	// The manifest's convention is 'web', next to 'domain'.
	public WebViewShell(string name, Shell shell, string grantedFolder)
	{
		module = name;
		bundle = Path.GetFullPath(shell.Arguments.Count > 0 ? shell.Arguments[0] : "web");

		// The granted folder is the module's journal folder, same as in the other shells.
		// Hosted does NOT go to OPFS: the fourth pillar requires Gnothi to be able to look at
		// it from outside the web engine (11 §5.1).
		journal = Path.GetFullPath(grantedFolder);

		// The SECOND argument is the publisher's endpoint for the courier, in the form
		// `baseUrl|resource|id`. Registered at install time: the module neither chooses it nor
		// can change it (11 §6).
		publisher = shell.Arguments.Count > 1 ? shell.Arguments[1] : "";
	}

	public bool IsRunning => Host?.IsLive(module) == true;

	// THE ANSWER COMES BACK OPAQUE and the Host forwards it without looking, exactly as with
	// the other two shells. It is the JSON the institution's domain composed.
	public async Task<object> Invoke(
		string tool, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct)
	{
		IWebModule live = await Live(ct);
		return await live.Ask(tool, JsonSerializer.Serialize(arguments ?? new Dictionary<string, object?>()), ct);
	}

	// What the module ACTUALLY serves, for the contrast against what its manifest declared.
	// Names and nothing else: that is all that gets compared.
	public async Task<IReadOnlyList<string>> ActualTools(CancellationToken ct)
	{
		IWebModule live = await Live(ct);
		string json = await live.Tools(ct);

		try { return JsonSerializer.Deserialize<string[]>(json) ?? []; }
		catch (JsonException) { return []; }
	}

	private async Task<IWebModule> Live(CancellationToken ct)
	{
		if (Host is null)
			throw new InvalidOperationException(
				"no web module host is registered: the app has to set WebViewShell.Host before "
				+ "dispatching. On desktop this shell does not exist.");

		return await Host.Live(module, bundle, journal, publisher, ct);
	}

	// EVICTING IS MANDATORY, not an optimisation: measured, a WebView with the wasm runtime
	// inside weighs ~150 MB of PSS, and fifteen live modules do not fit (11 §12). Nothing is
	// lost by evicting because the module starts nothing: it does not wake up, it does not
	// notify.
	public ValueTask DisposeAsync()
	{
		Host?.Evict(module);
		return ValueTask.CompletedTask;
	}
}

// WHO KNOWS HOW TO CREATE AND HOLD WebViews. Each platform implements it, because it needs the
// Context and the UI thread on Android, and the UIViewController on iOS; the Host only names it.
internal interface IWebModuleHost
{
	bool IsLive(string module);

	// Returns the module READY TO BE ASKED: WebView created, bundle served with its CSP,
	// runtime started and actor hydrated. If it was already live, it returns it as is.
	Task<IWebModule> Live(string module, string bundle, string journal, string publisher, CancellationToken ct);

	void Evict(string module);
}

// A LIVE WEB MODULE. Two verbs, the same two `IShell` needs.
internal interface IWebModule
{
	Task<string> Ask(string tool, string argumentsJson, CancellationToken ct);

	Task<string> Tools(CancellationToken ct);
}
