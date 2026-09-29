using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Gnothi.Host;

namespace Gnothi.Desktop;

// THE ROOM ON THE DESKTOP, AND IT IS THE ONLY ONE. It lives as a hidden iframe of Gnothi's page,
// served over loopback.
//
// ── WHY LIKE THIS, AND NOT WITH A DOCUMENT OF ITS OWN ─────────────────────────────────────────
//
// There was a second room, over WebView2, that created its own hidden document and served it
// through a virtual host. **It was deleted on 2026-09-25**: it was for `--cara ventana`, a flag no
// installer, script or doc passes. What follows explains why THIS form could not copy that one,
// and they are two measurements, not tastes:
//
//   1 · **Photino's own scheme does not reach a frame.** A frame's request to a second scheme
//       never reached the handler -- measured 2026-09-14, zero calls. That is why the desktop
//       already serves modules' faces through a loopback listener and not through the scheme. The
//       room cannot live with that unexamined: Blazor downloads `_framework/*` with fetch, and the
//       substrate asks for the module's dlls and its journal with HttpClient, which in wasm IS
//       fetch.
//
//   2 · **A Photino window is a system window**, and the room has no screen. Whether one can be
//       kept hidden is not measured, so building on that would be building on a supposition.
//
//
// WHAT IS PROVEN are the two pieces this class uses: a loopback listener -- the one the desktop
// already serves faces with -- and an iframe with its postMessage bridge, the same post that
// already carries the faces' traffic. **None of this belongs to a platform**: it is HTTP, an
// iframe and a message. That is why it can be verified on Windows and believed on macOS and
// Linux, which is more than can be said of a hidden window.
//
// ── THE ORIGIN IS THE PORT ────────────────────────────────────────────────────────────────────
//
// `http://127.0.0.1:<port>/`. A loopback is a secure context in Chromium and in WebKit, which is
// what the runtime needs; and a real HTTP response carries real headers, so the CSP BundleServer
// composes DOES APPLY -- something Photino's scheme never managed.
internal sealed class PhotinoRoom : IRoomHost
{
	private readonly Action<string> log;
	private readonly string substrate;
	private readonly int port;
	private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> waiting = new(StringComparer.Ordinal);

	// ONE SLOT ONLY, the same correction and the same reason as on both phones: if this accumulated
	// the session's modules, any domain inside the room could ask, from its own origin, for ANOTHER
	// module's journal, straight off the disk.
	private readonly object padlock = new();
	private string inFlight = "";
	private RoomFiles.Prepared? prepared;

	private HttpListener? listener;
	private TaskCompletionSource<bool>? ready;
	private int next;

	// WHAT GNOTHI'S PAGE FILLS IN. They are the exact equivalent of `frames.Show/RunJs/Hide`, and
	// Root.razor wires them: this class does not know there is a Blazor on the other side, it asks
	// it to mount a document and to pass it a message.
	private Func<string, Task>? mount;
	private Func<string, string, string, string, string, Task>? post;
	private Func<Task>? drop;

	// AND IT WAITS FOR THEM, INSTEAD OF THROWING IF THEY ARE NOT THERE.
	//
	// A module's face can afford to demand that the page already be there -- it opens because the
	// person touches something, and by then it has been there a while. The room cannot: the first
	// request may come from the assistant, or from `--sala`'s warm-up, BEFORE Blazor has run its
	// OnInitialized. Measured the first time it ran: `la pagina todavia no cableo la sala`.
	//
	// Waiting and not failing is right because the wait is short and certain: the page is wired at
	// start-up, always. What it cannot do is wait forever, hence the cap below.
	private readonly TaskCompletionSource<bool> page = new(TaskCreationOptions.RunContinuationsAsynchronously);

	internal void WireTo(
		Func<string, Task> mount,
		Func<string, string, string, string, string, Task> post,
		Func<Task> drop)
	{
		this.mount = mount;
		this.post = post;
		this.drop = drop;
		page.TrySetResult(true);
	}

	internal PhotinoRoom(string substrateFolder, Action<string> log)
	{
		this.substrate = substrateFolder;
		this.log = log;
		this.port = FreePort();
	}

	internal string Origin => $"http://127.0.0.1:{port}/";

	// ---------------------------------------------------------------- what RoomShell asks for

	public Task Prepare(string module, string entry, string grantedFolder, CancellationToken ct)
	{
		string domain = Path.GetDirectoryName(entry) ?? "";

		lock (padlock)
		{
			if (inFlight.Length > 0 && !string.Equals(inFlight, module, StringComparison.Ordinal))
				log($"sala: {inFlight} deja de ser servible");

			inFlight = module;
			prepared = new RoomFiles.Prepared(domain, Path.GetFileNameWithoutExtension(entry), grantedFolder);
		}

		log($"sala: {module} preparado desde {domain}");
		return Task.CompletedTask;
	}

	public Task Release(string module, CancellationToken ct)
	{
		lock (padlock)
		{
			if (!string.Equals(inFlight, module, StringComparison.Ordinal)) return Task.CompletedTask;
			inFlight = "";
			prepared = null;
		}

		log($"sala: {module} soltado, ya no se sirve por el origen");
		return Task.CompletedTask;
	}

	private RoomFiles.Prepared? Servable(string module)
	{
		lock (padlock)
		{
			return string.Equals(inFlight, module, StringComparison.Ordinal) ? prepared : null;
		}
	}

	public Task Save(string module, string path, string contentBase64, CancellationToken ct)
	{
		if (Servable(module) is not RoomFiles.Prepared p)
			throw new InvalidOperationException($"'{module}' no esta en vuelo: no se le guarda nada");

		string root = Path.GetFullPath(p.Journal);
		string target = Path.GetFullPath(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)));

		if (!target.StartsWith(root, StringComparison.Ordinal))
			throw new UnauthorizedAccessException($"ruta fuera del journal de {module}: {path}");

		Directory.CreateDirectory(Path.GetDirectoryName(target)!);
		byte[] bytes = Convert.FromBase64String(contentBase64);
		File.WriteAllBytes(target, bytes);

		log($"sala/guardar {module}: {path} · {bytes.Length} bytes");
		return Task.CompletedTask;
	}

	public async Task<string> Ask(string verb, string module, string a, string b, CancellationToken ct)
	{
		await Start(ct);

		string id = Interlocked.Increment(ref next).ToString();
		var wait = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
		waiting[id] = wait;

		await post!(id, verb, module, a, b);

		using CancellationTokenRegistration cancelled = ct.Register(() => wait.TrySetCanceled(ct));
		string answer = await wait.Task;

		if (verb is "cargar" or "desalojar") log($"sala/{verb} {module}: {answer}");

		return answer;
	}

	public Task Discard(string reason, CancellationToken ct)
	{
		log($"sala: se tira el documento -- {reason}");

		lock (padlock)
		{
			inFlight = "";
			prepared = null;
		}

		foreach (string id in waiting.Keys)
			if (waiting.TryRemove(id, out TaskCompletionSource<string>? wait))
				wait.TrySetException(new InvalidOperationException($"se tiro la sala: {reason}"));

		// AND ONLY NOW is the start-up forgotten, so nobody is left waiting on a dead document. The
		// next request mounts a new one; requests arrive serialised by the shell's `OneAtATime`, so
		// there are never two mounting it at once.
		ready = null;
		_ = drop?.Invoke();

		return Task.CompletedTask;
	}

	// ---------------------------------------------------------------- el documento

	internal Task WarmUp(CancellationToken ct) => Start(ct);

	private async Task Start(CancellationToken ct)
	{
		if (ready is not null) { await ready.Task; return; }

		var starting = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		if (Interlocked.CompareExchange(ref ready, starting, null) is not null)
		{
			await ready!.Task;
			return;
		}

		// LA PAGINA PRIMERO. Ver el comentario de `page` arriba: el primer pedido puede llegar antes
		// THE PAGE FIRST. See the comment on `page` above: the first request can arrive before Blazor
		// has run its OnInitialized.

		// THE LISTENER FIRST AND THE DOCUMENT AFTER, because the document asks as soon as it mounts.
		if (listener is null)
		{
			var l = new HttpListener();
			l.Prefixes.Add(Origin);
			l.Start();
			listener = l;
			_ = Task.Run(() => Accept(l));
			log($"sala: sirviendo en {Origin} (solo loopback)");
		}

		log("sala: creando el documento");
		await mount!(Origin);

		await starting.Task.WaitAsync(TimeSpan.FromSeconds(60), ct);
	}

	// ---------------------------------------------------------------- the bridge, coming back

	// WHAT COMES FROM THE IFRAME, already relayed by the page. Same envelope as on the phones:
	// {op, id, a}.
	internal void FromRoom(string raw)
	{
		try
		{
			using JsonDocument envelope = JsonDocument.Parse(raw);
			JsonElement root = envelope.RootElement;

			switch (Field(root, "op"))
			{
				case "lista":
					log("sala: el sustrato contesta");
					ready?.TrySetResult(true);
					return;

				case "contesta":
					if (waiting.TryRemove(Field(root, "id"), out TaskCompletionSource<string>? wait))
						wait.TrySetResult(Field(root, "a"));
					return;

				case "consola": log($"sala-web: {Field(root, "a")}"); return;
				case "sonda": log($"sala: P0 · {Field(root, "a")}"); return;
				default: log($"sala: verbo desconocido del puente: {Field(root, "op")}"); return;
			}
		}
		catch (JsonException ex) { log($"sala: mensaje ilegible del puente: {ex.Message}"); }
	}

	private static string Field(JsonElement o, string field) =>
		o.TryGetProperty(field, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

	// ---------------------------------------------------------------- la cascara de loopback

	private static int FreePort()
	{
		var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
		probe.Start();
		int port = ((IPEndPoint)probe.LocalEndpoint).Port;
		probe.Stop();
		return port;
	}

	private async Task Accept(HttpListener l)
	{
		while (l.IsListening)
		{
			HttpListenerContext ctx;
			try { ctx = await l.GetContextAsync(); }
			catch { return; /* se cerro */ }

			_ = Task.Run(() => Answer(ctx));
		}
	}

	private void Answer(HttpListenerContext ctx)
	{
		string path = (ctx.Request.Url?.AbsolutePath ?? "/").TrimStart('/');
		if (path.Length == 0 || path.EndsWith("/", StringComparison.Ordinal)) path += "index.html";

		try
		{
			// THE SHIM AND THE DOCUMENT ARE THE TWO SPECIAL CASES, and the order matters: the document
			// goes out with the shim's tag inside and its CSP computed AFTER adding it.
			RoomFiles.Served served =
				path == RoomFiles.ShimPath ? Shim() :
				path == "index.html" ? RoomFiles.WithShim(substrate, log) :
				RoomFiles.Serve(substrate, path, Servable, log);

			ctx.Response.StatusCode = served.Status;
			ctx.Response.ContentType = served.Type;
			ctx.Response.Headers["Cache-Control"] = "no-store";
			if (served.Csp is not null) ctx.Response.Headers["Content-Security-Policy"] = served.Csp;

			ctx.Response.ContentLength64 = served.Body.Length;
			if (served.Body.Length > 0) ctx.Response.OutputStream.Write(served.Body, 0, served.Body.Length);
		}
		catch (Exception ex)
		{
			// A substrate that cannot be served MUST NOT fail silently: the engine would show a blank
			// page, the symptom that already cost a whole run on Android.
			log($"sala: 500 {path} -- {ex.GetType().Name}: {ex.Message}");
			try { ctx.Response.StatusCode = 500; } catch { /* ya se fue */ }
		}
		finally { try { ctx.Response.Close(); } catch { /* ya se fue */ } }
	}

	private static RoomFiles.Served Shim() =>
		new(Encoding.UTF8.GetBytes(TheShim), "text/javascript", 200, null);

	// THE BRIDGE, AND WHAT IT DOES NOT HAVE. Two verbs outward: "I am here" and "here is the
	// answer". No read, no write, nothing to ask for -- Gnothi puts the journal in place before
	// asking, and whatever the module writes stays in memory until the eviction gives it back.
	//
	// AND IT IS WHAT THE INSTITUTION'S DOMAIN CANNOT REACH: the module runs inside the substrate's
	// .NET runtime, with no access to this object. Its user is the substrate, which is Gnothi's.
	//
	// IT TRAVELS AS A SAME-ORIGIN FILE, not inline: that way `script-src 'self'` admits it without a
	// hash, and the CSP BundleServer composed over the document does not have to be recomposed. It
	// is the same solution the modules' faces already use.
	private const string TheShim = """
		(function () {
		  var padre = window.parent;
		  var send = function (m) { padre.postMessage({ gnothi: 'puente', m: JSON.stringify(m) }, '*'); };

		  window.SalaPuente = {
		    lista:    function ()         { send({ op: 'lista',    a: '' }); },
		    contesta: function (id, json) { send({ op: 'contesta', id: id, a: json }); }
		  };

		  // REQUESTS ARRIVE AS DATA AND NOT AS JAVASCRIPT. A module's face receives code and evaluates
		  // it, which is why its policy asks for 'unsafe-eval'; the room needs no such concession: it
		  // receives five fields and calls the function the substrate published.
		  window.addEventListener('message', function (e) {
		    if (!e.data || e.data.gnothi !== 'sala') return;
		    if (e.source !== padre) return;
		    try { window.sala.pedido(e.data.id, e.data.verbo, e.data.modulo, e.data.a, e.data.b); }
		    catch (x) { send({ op: 'consola', a: 'error: el pedido no se pudo entregar: ' + x }); }
		  });

		  // THE PROBE, ANSWERED BY THE APP AND NOT BY A PERSON. It goes out before a single line of the
		  // substrate runs, so the answer arrives EVEN IF THE SUBSTRATE NEVER STARTS.
		  try {
		    send({ op: 'sonda', a:
		      'secureContext=' + !!window.isSecureContext +
		      ' crypto.subtle=' + !!(window.crypto && window.crypto.subtle) +
		      ' storage=' + !!navigator.storage +
		      ' wasm=' + (typeof WebAssembly !== 'undefined') +
		      ' wasm.streaming=' + (typeof WebAssembly !== 'undefined' && !!WebAssembly.instantiateStreaming) +
		      ' origin=' + window.location.origin });
		  } catch (x) { }

		  ['log', 'warn', 'error'].forEach(function (level) {
		    var before = console[level].bind(console);
		    console[level] = function () {
		      try { send({ op: 'consola', a: level + ': ' + Array.prototype.join.call(arguments, ' ') }); }
		      catch (x) { }
		      before.apply(console, arguments);
		    };
		  });
		})();
		""";
}

// THE PAGE'S ONLY DOOR INTO C# FOR THE ROOM, twin of `FrameBridge`.
public static class RoomBridge
{
	internal static PhotinoRoom? Current;

	[Microsoft.JSInterop.JSInvokable("FromRoom")]
	public static void FromRoom(string raw) => Current?.FromRoom(raw);
}
