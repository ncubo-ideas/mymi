using Android.App;
using Android.OS;
using Android.Webkit;
using Gnothi.Assistant;
using Gnothi.Host;
using System.Text.Json;

namespace Gnothi.Mobile;

// THE GNOTHI HOST ON A PHONE, with a face.
//
// The face is ONE html file in Assets, rendered by a WebView. Why not Blazor/MAUI, which was
// the natural suggestion: real Blazor on Android needs the MAUI workload, which installs into
// Program Files -- admin on this machine -- and rewrites the project. The WebView gives what
// was wanted from it (web-grade UI, and the model's markdown rendered with real tables, which
// native TextViews cannot do) while the Host code stays untouched.
//
// THE BRIDGE IS window.prompt(). The page calls prompt('gnothi', json) and OnJsPrompt answers
// synchronously; C# pushes back with EvaluateJavascript(gn.push(...)). Old-school on purpose:
// no exported Java methods, no extra packages, works on every WebView.
//
// WHAT THIS FILE OWNS: the screen and the lifecycle (installing what was downloaded). The
// catalog, the consent and the dispatch are the Host's -- the same files the desktop runs.
[Activity(Label = "Gnothi", MainLauncher = true, Theme = "@android:style/Theme.Material.Light.NoActionBar")]
public class MainActivity : Activity
{
	private Dispatcher? dispatcher;
	private ModuleCatalog? catalog;
	private WebView? web;
	private ConsentWatch? watch;
	private readonly CancellationTokenSource stop = new();

	// WHERE THE MODEL LIVES. Two constants, because switching provider must stay this cheap --
	// that is the whole argument for IModel being an interface.
	//
	//   Groq        https://api.groq.com/openai/v1     free tier, no card asked
	//   Cerebras    https://api.cerebras.ai/v1
	//   OpenRouter  https://openrouter.ai/api/v1
	//   LM Studio   http://<the machine's IP>:1234/v1  NO key -- nothing leaves the LAN
	//
	// 'localhost' does NOT work from the phone: on Android that is the phone itself.
	private const string ModelUrl = "https://api.groq.com/openai/v1";
	private const string ModelName = "openai/gpt-oss-120b";

	// The icon per business. A small explicit map: a module that is not here falls to the
	// generic box. It does NOT come from the manifest on purpose -- the icon is the Host's
	// business, not the institution's.
	private static string Icon(string module) => module switch
	{
		"examenes" => "\U0001FA7A",   // stethoscope
		"facturas" => "\U0001F9FE",   // receipt
		"compras"  => "\U0001F6D2",   // trolley
		_          => "\U0001F4E6",   // box
	};

	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);

		web = new WebView(this);
		web.Settings.JavaScriptEnabled = true;
		// The page detects the app by this mark and NEVER calls prompt() outside it: in a plain
		// browser that dialog is modal and freezes the page -- measured in the preview pane.
		web.Settings.UserAgentString += " GnothiHost";
		web.SetBackgroundColor(Android.Graphics.Color.ParseColor("#F5F3FB"));
		web.SetWebChromeClient(new Bridge(this));

		// targetSdk 36 makes edge-to-edge MANDATORY: the page would extend under the status
		// bar and the gesture bar, and the bottom nav ends up beneath the system's. The fix is
		// native and in one place -- pad by the system bars -- BUT the padding goes on a
		// CONTAINER, not on the WebView: a WebView ignores its own padding, which cost one
		// screenshot to find out.
		var frame = new Android.Widget.FrameLayout(this);
		frame.SetBackgroundColor(Android.Graphics.Color.ParseColor("#F5F3FB"));
		frame.AddView(web);
		frame.SetOnApplyWindowInsetsListener(new Insets());

		SetContentView(frame);
		web.LoadUrl("file:///android_asset/ui.html");

		_ = Task.Run(Start);
	}

	private async Task Start()
	{
		// 1 - LIFECYCLE: the Host installs what was downloaded. adb cannot write into the
		//     private area, and that is correct: nobody from outside installs a module.
		string incoming = System.IO.Path.Combine(GetExternalFilesDir(null)!.AbsolutePath, "incoming");
		string root = System.IO.Path.Combine(FilesDir!.AbsolutePath, "Modulos");
		string journals = System.IO.Path.Combine(FilesDir!.AbsolutePath, "journals");

		// Migration: the key used to live in one file for one hardcoded provider. If that old
		// file is here and the per-provider one is not, it becomes the Groq key.
		string old = System.IO.Path.Combine(FilesDir!.AbsolutePath, "model.key");
		if (File.Exists(old) && !File.Exists(KeyPath)) File.Move(old, KeyPath);

		int copied = 0;
		if (System.IO.Directory.Exists(incoming))
		{
			string modulesIn = System.IO.Path.Combine(incoming, "Modulos");
			string journalsIn = System.IO.Path.Combine(incoming, "journals");
			if (System.IO.Directory.Exists(modulesIn)) copied += Copy(modulesIn, root);
			if (System.IO.Directory.Exists(journalsIn)) copied += Copy(journalsIn, journals);
		}

		// 2 - DISCOVER. The very same code the desktop Host runs.
		catalog = new ModuleCatalog(root);
		IReadOnlyList<string> warnings = catalog.Load();
		// THIS GATE PAYS FOR ITS OWN TOKENS. The key is Sonia's, so every character the module
		// wraps its facts in comes out of her pocket -- which is why this is the gate that
		// declares it can also read TOON. What it does not do is DEMAND it: a module answers in
		// whichever of the two suits the shape of that particular answer, and JSON stays valid.
		dispatcher = new Dispatcher(catalog, new Meter(), Accepts.JsonOrToon);

		foreach (string w in warnings) Log($"warning: {w}");
		Log($"{catalog.Modules.Count} modules installed" + (copied > 0 ? $" - {copied} files this launch" : ""));

		PushState();

		// 3 - THE PERSON'S SWITCH, WATCHED. The UI's switches write installation.json and this
		//     is what makes them take effect; and an edit from OUTSIDE the app (adb, another
		//     tool) repaints the screen through the same path. One source of truth: her file.
		watch = new ConsentWatch(dispatcher) { Changed = (_, _) => { PushState(); return Task.CompletedTask; } };
		await watch.Run(Log, stop.Token);
	}

	protected override void OnDestroy()
	{
		stop.Cancel();
		watch?.Dispose();
		base.OnDestroy();
	}

	// THE BACK BUTTON asks the page first: a sheet closes, a detail goes home. Only when the
	// page answers false -- already on home -- does the app step aside, to the BACKGROUND and
	// not to death: finishing would throw away the loaded catalog for no reason.
	//
	// OnBackPressed is the deprecated path, and it is chosen on purpose: the modern
	// OnBackInvokedCallback only takes over when the manifest opts in, which this app does not.
	public override void OnBackPressed() =>
		web!.EvaluateJavascript("gn.back()", new BackAnswer(this));

	private sealed class BackAnswer : Java.Lang.Object, IValueCallback
	{
		private readonly MainActivity host;
		public BackAnswer(MainActivity host) => this.host = host;

		public void OnReceiveValue(Java.Lang.Object? value)
		{
			if (value?.ToString() == "true") return;   // the page went back by itself
			host.MoveTaskToBack(true);
		}
	}

	private void Log(string s) => global::Android.Util.Log.Info("GNOTHI", s);

	// ── what the page asks for ──────────────────────────────────────────────────────────

	// The state as the UI needs it: modules, their prose, where each switch stands, and the
	// journal's real weight -- measured from outside, which is the Host's own verb.
	private string StateJson()
	{
		if (catalog is null) return "{\"loading\":true}";

		var modules = catalog.Modules.Select(m =>
		{
			long bytes = 0; int files = 0;
			try
			{
				foreach (FileInfo f in new DirectoryInfo(m.Installation.GrantedFolder)
					.EnumerateFiles("*", SearchOption.AllDirectories))
				{ bytes += f.Length; files++; }
			}
			catch { /* a module with no journal yet weighs nothing */ }

			return new Dictionary<string, object?>
			{
				["name"] = m.Name,
				["title"] = m.Manifest.Title,
				["institution"] = m.Manifest.Institution,
				["icon"] = Icon(m.Name),
				["enabled"] = m.Installation.Enabled,
				["journalBytes"] = bytes,
				["journalFiles"] = files,
				["tools"] = m.Manifest.Tools.Select(t => new Dictionary<string, object?>
				{
					["name"] = t.Name,
					["title"] = t.Title,
					["description"] = t.Description,
					["enabled"] = !m.Installation.DisabledTools.Contains(t.Name),
				}).ToArray(),
			};
		}).ToArray();

		return JsonSerializer.Serialize(new Dictionary<string, object?>
		{
			["modules"] = modules,
			["settings"] = Settings(),
		});
	}

	// THE SWITCH WRITES THE PERSON'S FILE. Not an in-memory flag: the same installation.json
	// the desktop Host reads, the watcher watches, and the catalog reloads from. The UI has no
	// state of its own -- it repaints from whatever the file says, which is why an external
	// edit shows up on screen too.
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
			System.IO.Path.Combine(m.Directory, "installation.json"),
			JsonSerializer.Serialize(updated, new JsonSerializerOptions { WriteIndented = true }));

		// Applied NOW, not when the watcher's 300ms burst settles: the switch the person just
		// moved has to answer immediately. The watcher still fires -- ReloadConsent finds
		// nothing further to change and stays quiet.
		catalog!.ReloadConsent();
		Log($"consent: {module}{(tool is null ? "" : "." + tool)} -> {(enabled ? "on" : "off")}");
		return StateJson();
	}

	private Dictionary<string, object?> Settings()
	{
		(string name, string url, string model) = Provider();
		return new Dictionary<string, object?>
		{
			["name"] = name,
			["provider"] = new Uri(url).Host,
			["model"] = model,
			// Whether there IS a key for THIS provider, never the key itself.
			["keyPresent"] = File.Exists(KeyPath),
			["root"] = catalog?.Root ?? "",
		};
	}

	// THE PROVIDER, CHOSEN BY THE PERSON. Every preset speaks the OpenAI shape, which is what
	// ModelOpenAiCompatible talks -- so "use ChatGPT instead" is a file with a URL and a model
	// name, not code. The constants above remain only as the default when nothing was chosen.
	private string CfgPath => System.IO.Path.Combine(FilesDir!.AbsolutePath, "model.cfg");

	private (string name, string url, string model) Provider()
	{
		try
		{
			if (File.Exists(CfgPath))
			{
				JsonElement e = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(CfgPath));
				return (e.GetProperty("name").GetString() ?? "Groq",
						e.GetProperty("url").GetString() ?? ModelUrl,
						e.GetProperty("model").GetString() ?? ModelName);
			}
		}
		catch { /* an unreadable choice falls back to the default */ }
		return ("Groq", ModelUrl, ModelName);
	}

	private string SetProvider(string name, string url, string model)
	{
		File.WriteAllText(CfgPath, JsonSerializer.Serialize(new Dictionary<string, string>
		{
			["name"] = name, ["url"] = url, ["model"] = model,
		}));
		Log($"provider: {name} · {model}");
		return StateJson();
	}

	// THE KEY, MANAGED BY THE PERSON. This is what makes BYOK real: until now the file was
	// placed by a developer over a cable, which is not "bring your own key" -- it is bring
	// MINE. Any key of the chosen provider works, free tier or paid: the file does not know
	// the difference.
	//
	// PER PROVIDER, on purpose: a Groq key is not an OpenAI key. Switching provider switches
	// which key file is read, so going to ChatGPT and back to Groq loses nothing.
	//
	// Three rules, enforced here: it lives ONLY in the app's private area, it is NEVER sent
	// back to the page (StateJson carries a boolean, not the value), and the log records the
	// event without the key.
	private string KeyPath
	{
		get
		{
			string safe = new string(Provider().name.Where(char.IsLetterOrDigit).ToArray());
			return System.IO.Path.Combine(FilesDir!.AbsolutePath, $"key-{safe}.key");
		}
	}

	private string SetKey(string key)
	{
		key = key.Trim();
		if (key.Length > 0)
		{
			File.WriteAllText(KeyPath, key);
			Log($"model key: saved ({key.Length} chars)");
		}
		return StateJson();
	}

	private string ClearKey()
	{
		if (File.Exists(KeyPath)) File.Delete(KeyPath);
		Log("model key: cleared");
		return StateJson();
	}

	// ONE QUESTION, answered through Gate 2: same AgentLoop, same dispatcher, same key file.
	// Runs detached; the trace and the answer land in the page as they happen.
	private void Ask(string id, string question)
	{
		_ = Task.Run(async () =>
		{
			try
			{
				(_, string url, string model) = Provider();
				var loop = new AgentLoop(dispatcher!, new ModelOpenAiCompatible(url, model, KeyPath));

				string answer = await loop.Answer(question, line => Push("trace", id, line), stop.Token);
				Push("answer", id, answer);
			}
			catch (Exception e)
			{
				Log($"ask failed: {e.GetType().Name}: {e.Message}");
				Push("fail", id, $"{e.GetType().Name}: {e.Message}");
			}
		});
	}

	private void PushState() => Push("state", "", StateJson());

	private void Push(string kind, string id, string payload) => RunOnUiThread(() =>
		web?.EvaluateJavascript(
			$"gn.push({JsonSerializer.Serialize(kind)},{JsonSerializer.Serialize(id)},{JsonSerializer.Serialize(payload)})",
			null));

	// ── the bridge itself ───────────────────────────────────────────────────────────────
	//
	// The page calls window.prompt('gnothi', json). Anything else is a real prompt and gets
	// the default behaviour.
	private sealed class Bridge : WebChromeClient
	{
		private readonly MainActivity host;
		public Bridge(MainActivity host) => this.host = host;

		public override bool OnJsPrompt(WebView? view, string? url, string? message, string? defaultValue, JsPromptResult? result)
		{
			if (message != "gnothi" || defaultValue is null || result is null)
				return base.OnJsPrompt(view, url, message, defaultValue, result);

			try
			{
				JsonElement req = JsonSerializer.Deserialize<JsonElement>(defaultValue);
				string op = req.GetProperty("op").GetString() ?? "";
				host.Log($"bridge: {op}");

				string answer = op switch
				{
					"modules" => host.StateJson(),
					"setTool" => host.SetConsent(
						req.GetProperty("module").GetString() ?? "",
						req.GetProperty("tool").GetString(),
						req.GetProperty("enabled").GetBoolean()),
					"setModule" => host.SetConsent(
						req.GetProperty("module").GetString() ?? "",
						null,
						req.GetProperty("enabled").GetBoolean()),
					"setProvider" => host.SetProvider(
						req.GetProperty("name").GetString() ?? "",
						req.GetProperty("url").GetString() ?? "",
						req.GetProperty("model").GetString() ?? ""),
					"setKey" => host.SetKey(req.GetProperty("key").GetString() ?? ""),
					"clearKey" => host.ClearKey(),
					"ask" => RunAsk(req),
					_ => "{}",
				};

				result.Confirm(answer);
			}
			catch (Exception e)
			{
				host.Log($"bridge: {e.GetType().Name}: {e.Message}");
				result.Confirm("{}");
			}

			return true;
		}

		private string RunAsk(JsonElement req)
		{
			host.Ask(req.GetProperty("id").GetString() ?? "", req.GetProperty("question").GetString() ?? "");
			return "\"ok\"";
		}
	}

	private sealed class Insets : Java.Lang.Object, Android.Views.View.IOnApplyWindowInsetsListener
	{
		public Android.Views.WindowInsets OnApplyWindowInsets(Android.Views.View v, Android.Views.WindowInsets insets)
		{
			var bars = insets.GetInsets(Android.Views.WindowInsets.Type.SystemBars());
			v.SetPadding(bars.Left, bars.Top, bars.Right, bars.Bottom);
			return Android.Views.WindowInsets.Consumed;
		}
	}

	// Copy from the download area into the private one. Dumb on purpose: the Host does not
	// interpret anything it copies.
	private static int Copy(string source, string target)
	{
		int n = 0;
		System.IO.Directory.CreateDirectory(target);
		foreach (string file in System.IO.Directory.GetFiles(source, "*", System.IO.SearchOption.AllDirectories))
		{
			string destination = file.Replace(source, target);
			System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destination)!);
			System.IO.File.Copy(file, destination, overwrite: true);
			n++;
		}
		return n;
	}
}
