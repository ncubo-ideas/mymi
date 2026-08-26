using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using Gnothi.Host;

namespace Gnothi.Mobile;

// THE GNOTHI HOST ON A PHONE, with a face.
//
// It shows the INSTALLED modules as cards -- each with its icon, who handed it over, and one
// button per query. The point is not the aesthetics: it is that the person can SEE that they
// have modules installed and whose they are, without reading a log.
//
// The Host code is THE SAME as the desktop one: Dispatcher, ModuleCatalog, Module,
// InProcessShell, compiled in from ../Gnothi.Host. The only thing this app owns is the screen
// and the lifecycle -- installing what was downloaded -- which 02-el-host.md makes the Host's
// authority.
[Activity(Label = "Gnothi", MainLauncher = true)]
public class MainActivity : Activity
{
	private Dispatcher? dispatcher;
	private LinearLayout? rootView;

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

		rootView = new LinearLayout(this) { Orientation = Orientation.Vertical };
		rootView.SetPadding(28, 40, 28, 40);
		var scroll = new ScrollView(this);
		scroll.AddView(rootView);
		SetContentView(scroll);

		Heading("Gnothi");
		Note("The modules you installed on this phone");

		_ = Task.Run(Start);
	}

	private async Task Start()
	{
		// 1 - LIFECYCLE: the Host installs what was downloaded. adb cannot write into the
		//     private area, and that is correct: nobody from outside installs a module.
		string incoming = System.IO.Path.Combine(GetExternalFilesDir(null)!.AbsolutePath, "incoming");
		string root = System.IO.Path.Combine(FilesDir!.AbsolutePath, "Modulos");
		string journals = System.IO.Path.Combine(FilesDir!.AbsolutePath, "journals");

		int copied = 0;
		if (System.IO.Directory.Exists(incoming))
		{
			string modulesIn = System.IO.Path.Combine(incoming, "Modulos");
			string journalsIn = System.IO.Path.Combine(incoming, "journals");
			if (System.IO.Directory.Exists(modulesIn)) copied += Copy(modulesIn, root);
			if (System.IO.Directory.Exists(journalsIn)) copied += Copy(journalsIn, journals);
		}

		// 2 - DISCOVER. The very same code the desktop Host runs.
		var catalog = new ModuleCatalog(root);
		IReadOnlyList<string> warnings = catalog.Load();
		dispatcher = new Dispatcher(catalog, new Meter());

		RunOnUiThread(() =>
		{
			foreach (string w in warnings) Note($"warning: {w}");

			if (catalog.Modules.Count == 0)
			{
				Note("No modules installed yet.");
				return;
			}

			Note($"{catalog.Modules.Count} modules installed"
				+ (copied > 0 ? $"  -  {copied} files installed on this launch" : ""));

			foreach (Module m in catalog.Modules) rootView!.AddView(Card(m));
		});

		await Task.CompletedTask;
	}

	// ONE CARD PER MODULE: icon, who handed it over, and one button per query.
	private View Card(Module m)
	{
		var box = new LinearLayout(this) { Orientation = Orientation.Vertical };
		box.SetPadding(28, 28, 28, 28);
		var background = new Android.Graphics.Drawables.ShapeDrawable(new Android.Graphics.Drawables.Shapes.RectShape());
		background.Paint!.Color = Color.ParseColor(m.Installation.Enabled ? "#E9EDEC" : "#F2E7E6");
		box.Background = background;

		var row = new LinearLayout(this) { Orientation = Orientation.Horizontal };
		row.AddView(new TextView(this) { Text = Icon(m.Name), TextSize = 34f });
		var labels = new LinearLayout(this) { Orientation = Orientation.Vertical };
		labels.SetPadding(20, 0, 0, 0);
		labels.AddView(new TextView(this) { Text = m.Manifest.Title, TextSize = 18f, Typeface = Typeface.DefaultBold });
		labels.AddView(new TextView(this) { Text = m.Manifest.Institution, TextSize = 12f });
		labels.AddView(new TextView(this) { Text = m.Installation.Enabled ? "on" : "SWITCHED OFF by the person", TextSize = 11f });
		row.AddView(labels);
		box.AddView(row);

		var output = new TextView(this) { Text = "", TextSize = 10f };
		output.SetPadding(0, 16, 0, 0);

		foreach (DeclaredTool t in m.EnabledTools())
		{
			var button = new Button(this) { Text = string.IsNullOrWhiteSpace(t.Title) ? t.Name : t.Title, TextSize = 13f };
			DeclaredTool which = t;
			button.Click += async (_, _) =>
			{
				output.Text = $"asking {which.Name}...";
				try
				{
					// The parameters the manifest declares get filled with a sample value: this
					// screen is a probe, not the final UI. The one who will decide the real
					// arguments is the model, through Gate 2.
					//
					// 'analito' and 'AntigenoProstatico' stay in Spanish ON PURPOSE: the prose of
					// the manifests was translated, but the parameter names and the enum values
					// are the DOMAIN's contract, and that domain is Spanish.
					Dictionary<string, object?>? args = null;
					if (which.Input.Count > 0)
					{
						args = [];
						foreach (DeclaredParameter p in which.Input)
							args[p.Name] = p.Name == "analito" ? "AntigenoProstatico" : "1";
					}

					Response r = await dispatcher!.Serve(new Request(m.Name, which.Name, args), CancellationToken.None);
					string text = r.Authorized
						? System.Text.Json.JsonSerializer.Serialize(r.Content)
						: "DENIED: " + r.Reason;
					output.Text = text.Length > 700 ? text[..700] + "..." : text;
				}
				catch (System.Exception e) { output.Text = $"{e.GetType().Name}: {e.Message}"; }
			};
			box.AddView(button);
		}

		box.AddView(output);

		var container = new LinearLayout(this) { Orientation = Orientation.Vertical };
		container.SetPadding(0, 0, 0, 28);
		container.AddView(box);
		return container;
	}

	private void Heading(string s)
	{
		var t = new TextView(this) { Text = s, TextSize = 30f, Typeface = Typeface.DefaultBold };
		rootView!.AddView(t);
	}

	private void Note(string s)
	{
		var t = new TextView(this) { Text = s, TextSize = 12f };
		t.SetPadding(0, 6, 0, 18);
		rootView!.AddView(t);
		global::Android.Util.Log.Info("GNOTHI", s);
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
