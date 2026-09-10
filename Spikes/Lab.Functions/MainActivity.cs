using Android.App;
using Android.App.AppFunctions;
using Android.Content;
using Android.OS;
using Android.Widget;

[assembly: Android.App.UsesPermission("android.permission.EXECUTE_APP_FUNCTIONS")]

namespace Lab.Functions;

// THE CALLER APP for the Puerta 3 spikes. Driven by intent extras so no screen taps are
// needed:  adb shell am start -n com.ncubo.lab.functions/.MainActivity --es run spike1|spike2
// Everything lands in logcat tag SPIKE and on screen.
[Activity(Label = "Lab.Functions", MainLauncher = true, Exported = true)]
public class MainActivity : Activity
{
	private TextView? output;
	private readonly System.Text.StringBuilder log = new();

	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);
		output = new TextView(this) { TextSize = 12f };
		output.SetPadding(30, 90, 30, 30);
		var scroll = new ScrollView(this);
		scroll.AddView(output);
		SetContentView(scroll);

		string run = Intent?.GetStringExtra("run") ?? "both";
		Say($"run = {run}");
		if (run is "spike1" or "both") Spike1();
		if (run is "spike2" or "both") Spike2();
	}

	private void Say(string s)
	{
		global::Android.Util.Log.Info("SPIKE", s);
		log.AppendLine(s);
		RunOnUiThread(() => output!.Text = log.ToString());
	}

	// ── SPIKE 1 · can an ordinary app reach the AppFunctions manager? ──────────────────
	//
	// On THIS device we already measured EXECUTE_APP_FUNCTIONS = internal|privileged (the
	// AOSP 16 fallback path), so the EXPECTED result is a refusal. The spike's deliverable
	// is the exact shape of that refusal: where it fails, with which exception, saying what.
	private void Spike1()
	{
		Say("── spike1: AppFunctions desde una app comun ──");
		try
		{
			var mgr = (AppFunctionManager?)GetSystemService(
				Java.Lang.Class.FromType(typeof(AppFunctionManager)));
			Say($"  manager: {(mgr is null ? "NULL — el servicio no existe para esta app" : "obtenido")}");
			if (mgr is null) return;

			// The narrowest permission-gated call: is a (bogus) function enabled?
			mgr.IsAppFunctionEnabled(
				"total_de_facturas",             // functionIdentifier
				"com.ncubo.gnothi.host",         // targetPackage
				MainExecutor!,
				new Outcome(this, "isEnabled"));
			Say("  isAppFunctionEnabled: despachado, esperando callback…");

			// Y la invocacion de verdad: la cadena completa de la Puerta 3.
			var req = new ExecuteAppFunctionRequest.Builder(
				"com.ncubo.lab.caratula",
				"com.ncubo.lab.caratula#facturas_total_de_facturas").Build();
			mgr.ExecuteAppFunction(req, MainExecutor!, new Android.OS.CancellationSignal(),
				new Outcome(this, "executeAppFunction"));
			Say("  executeAppFunction: despachado…");
		}
		catch (Java.Lang.SecurityException e) { Say($"  SecurityException: {e.Message}"); }
		catch (Exception e) { Say($"  {e.GetType().Name}: {e.Message}"); }
	}

	private sealed class Outcome : Java.Lang.Object, IOutcomeReceiver
	{
		private readonly MainActivity host;
		private readonly string what;
		public Outcome(MainActivity host, string what) { this.host = host; this.what = what; }
		public void OnResult(Java.Lang.Object? result) => host.Say($"  {what} → resultado: {result}");
		public void OnError(Java.Lang.Throwable error) => host.Say($"  {what} → ERROR {error.GetType().Name}: {error.Message}");
	}

	// ── SPIKE 2 · the jump: another app → bound service → Dispatcher.Serve, Gnothi CLOSED ──
	private void Spike2()
	{
		Say("── spike2: el salto a Gnothi cerrado ──");
		var t0 = System.Diagnostics.Stopwatch.StartNew();

		var intent = new Intent();
		intent.SetComponent(new ComponentName("com.ncubo.gnothi.host", "com.ncubo.gnothi.host.IpcService"));

		bool ok = BindService(intent, new Link(this, t0), Bind.AutoCreate);
		Say($"  bindService devolvio {ok} a los {t0.ElapsedMilliseconds} ms");
		if (!ok) Say("  no se pudo ligar — ¿Gnothi instalado? ¿service exportado?");
	}

	private sealed class Link : Java.Lang.Object, IServiceConnection
	{
		private readonly MainActivity host;
		private readonly System.Diagnostics.Stopwatch clock;
		private int round;

		public Link(MainActivity host, System.Diagnostics.Stopwatch clock)
		{ this.host = host; this.clock = clock; }

		public void OnServiceConnected(ComponentName? name, IBinder? service)
		{
			host.Say($"  conectado a los {clock.ElapsedMilliseconds} ms (Gnothi ya corre)");
			var gnothi = new Messenger(service);
			Ask(gnothi);
		}

		private void Ask(Messenger gnothi)
		{
			round++;
			long asked = clock.ElapsedMilliseconds;

			Message msg = Message.Obtain()!;
			var data = new Bundle();
			data.PutString("module", "facturas");
			data.PutString("tool", "total_de_facturas");
			msg.Data = data;
			msg.ReplyTo = new Messenger(new Reply(host, clock, asked, round, this, gnothi));
			gnothi.Send(msg);
		}

		private sealed class Reply : Handler
		{
			private readonly MainActivity host;
			private readonly System.Diagnostics.Stopwatch clock;
			private readonly long asked;
			private readonly int round;
			private readonly Link link;
			private readonly Messenger gnothi;

			public Reply(MainActivity host, System.Diagnostics.Stopwatch clock, long asked, int round, Link link, Messenger gnothi)
				: base(Looper.MainLooper!)
			{ this.host = host; this.clock = clock; this.asked = asked; this.round = round; this.link = link; this.gnothi = gnothi; }

			public override void HandleMessage(Message msg)
			{
				Bundle d = msg.Data ?? new Bundle();
				long total = clock.ElapsedMilliseconds;
				string json = d.GetString("json") ?? "";
				host.Say($"  ronda {round}: respuesta a los {total} ms del arranque "
					+ $"(ida-y-vuelta {total - asked} ms · Serve {d.GetLong("serveMs")} ms · catalogo {d.GetLong("catalogMs")} ms)");
				host.Say($"  authorized={d.GetBoolean("authorized")} json={(json.Length > 120 ? json[..120] + "…" : json)}");

				// round 2 = the WARM call, right behind the cold one
				if (round == 1) link.Ask(gnothi);
			}
		}

		public void OnServiceDisconnected(ComponentName? name) => host.Say("  desconectado");
	}
}
