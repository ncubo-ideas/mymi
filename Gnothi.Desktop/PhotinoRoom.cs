using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Gnothi.Host;

namespace Gnothi.Desktop;

// LA SALA EN EL ESCRITORIO, Y ES LA UNICA. Vive como un iframe oculto de la pagina de Gnothi,
// servido por loopback.
//
// ── POR QUE ASI, Y NO CON UN DOCUMENTO PROPIO ──────────────────────────────────────────────────
//
// Hubo una segunda sala, sobre WebView2, que creaba su propio documento oculto y le servia por un
// host virtual. **Se borro el 2026-09-25**: era para `--cara ventana`, una bandera que no pasa
// ningun instalador, script ni doc. Lo que sigue explica por que ESTA forma no podia copiar
// aquella, y son dos medidas y no gustos:
//
//   1 · **El esquema propio de Photino no llega a un marco.** El pedido de un frame a un segundo
//       esquema nunca alcanzo al handler -- medido el 2026-09-14, cero llamadas. Por eso el
//       escritorio ya sirve las caras de los modulos por un listener de loopback, y no por el
//       esquema. La sala no puede vivir con eso sin examinarlo: Blazor baja `_framework/*` con
//       fetch, y el sustrato pide los dll del modulo y su journal con HttpClient, que en wasm ES
//       fetch.
//
//   2 · **Una ventana de Photino es una ventana del sistema**, y la sala no tiene pantalla. Que una
//       se pueda mantener oculta no esta medido, asi que construir sobre eso seria construir sobre
//       una suposicion.
//
// LO QUE SI ESTA PROBADO son las dos piezas que usa esta clase: un listener de loopback -- con el
// que el escritorio ya sirve las caras -- y un iframe con su puente por postMessage, que es el mismo
// correo que ya lleva y trae lo de las caras. **Nada de esto es de una plataforma**: es HTTP, un
// iframe y un mensaje. Por eso se puede verificar en Windows con `--cara marco` y creerle a macOS y
// Linux, que es mas de lo que se puede decir de una ventana oculta.
//
// ── EL ORIGEN ES EL PUERTO ──────────────────────────────────────────────────────────────────────
//
// `http://127.0.0.1:<puerto>/`. Un loopback es contexto seguro en Chromium y en WebKit, que es lo
// que el runtime necesita; y una respuesta HTTP de verdad lleva cabeceras de verdad, asi que la CSP
// que BundleServer compone SE APLICA -- cosa que el esquema de Photino nunca pudo.
internal sealed class PhotinoRoom : IRoomHost
{
	private readonly Action<string> log;
	private readonly string substrate;
	private readonly int port;
	private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> waiting = new(StringComparer.Ordinal);

	// UNA SOLA RANURA, la misma correccion y el mismo motivo que en los dos telefonos: si esto
	// acumulara los modulos de la sesion, cualquier dominio adentro de la sala podria pedir por su
	// propio origen el journal de OTRO modulo, directo del disco.
	private readonly object padlock = new();
	private string inFlight = "";
	private RoomFiles.Prepared? prepared;

	private HttpListener? listener;
	private TaskCompletionSource<bool>? ready;
	private int next;

	// LO QUE LA PAGINA DE GNOTHI LLENA. Son el equivalente exacto de `frames.Show/RunJs/Hide`, y los
	// cablea Root.razor: esta clase no sabe que hay un Blazor del otro lado, le pide que monte un
	// documento y que le pase un mensaje.
	private Func<string, Task>? mount;
	private Func<string, string, string, string, string, Task>? post;
	private Func<Task>? drop;

	// Y SE ESPERA A QUE ESTEN, EN VEZ DE TIRAR SI NO LO ESTAN.
	//
	// La cara de un modulo puede darse el lujo de exigir que la pagina ya este -- se abre porque la
	// persona toca algo, y para entonces hace rato que esta. La sala no: el primer pedido puede
	// llegar del asistente, o del calentado de `--sala`, ANTES de que Blazor haya corrido su
	// OnInitialized. Medido la primera vez que se corrio: `la pagina todavia no cableo la sala`.
	//
	// Esperar y no fallar es lo correcto porque la espera es corta y cierta: la pagina se cablea al
	// arrancar, siempre. Lo que no puede es esperar para siempre, y por eso el tope de abajo.
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

	// ---------------------------------------------------------------- lo que RoomShell pide

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

		// Y RECIEN AHORA se olvida el arranque, para que nadie se quede esperando un documento
		// muerto. El proximo pedido monta uno nuevo; los pedidos vienen serializados por el
		// `OneAtATime` de la cascara, asi que no hay dos montandolo a la vez.
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
		// de que Blazor haya corrido su OnInitialized.
		await page.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);

		// EL LISTENER PRIMERO Y EL DOCUMENTO DESPUES, porque el documento pide apenas se monta.
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

	// ---------------------------------------------------------------- el puente, de vuelta

	// LO QUE LLEGA DEL IFRAME, ya relevado por la pagina. Mismo sobre que en los telefonos: {op, id, a}.
	internal void FromSala(string raw)
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
			// EL SHIM Y EL DOCUMENTO SON LOS DOS CASOS ESPECIALES, y el orden importa: el documento
			// sale con la etiqueta del shim adentro y su CSP calculada DESPUES de agregarla.
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
			// Un sustrato que no se puede servir NO puede fallar en silencio: el motor mostraria una
			// pagina en blanco, que es el sintoma que ya costo una corrida entera en Android.
			log($"sala: 500 {path} -- {ex.GetType().Name}: {ex.Message}");
			try { ctx.Response.StatusCode = 500; } catch { /* ya se fue */ }
		}
		finally { try { ctx.Response.Close(); } catch { /* ya se fue */ } }
	}

	private static RoomFiles.Served Shim() =>
		new(Encoding.UTF8.GetBytes(TheShim), "text/javascript", 200, null);

	// EL PUENTE, Y LO QUE NO TIENE. Dos verbos hacia afuera: «ya estoy» y «aca esta la respuesta». No
	// hay leer, ni escribir, ni pedir nada -- el journal se lo pone Gnothi antes de preguntar, y lo
	// que el modulo escriba se queda en memoria hasta que el desalojo lo devuelva.
	//
	// Y ES LO QUE EL DOMINIO DE LA INSTITUCION NO ALCANZA: el modulo corre adentro del runtime .NET
	// del sustrato, sin acceso a este objeto. Quien lo usa es el sustrato, que es de Gnothi.
	//
	// VIAJA COMO ARCHIVO DEL MISMO ORIGEN, no en linea: asi `script-src 'self'` lo admite sin hash, y
	// la CSP que BundleServer compuso sobre el documento no hay que recomponerla. Es la misma
	// solucion que las caras de los modulos ya usan.
	private const string TheShim = """
		(function () {
		  var padre = window.parent;
		  var send = function (m) { padre.postMessage({ gnothi: 'puente', m: JSON.stringify(m) }, '*'); };

		  window.SalaPuente = {
		    lista:    function ()         { send({ op: 'lista',    a: '' }); },
		    contesta: function (id, json) { send({ op: 'contesta', id: id, a: json }); }
		  };

		  // LOS PEDIDOS LLEGAN COMO DATO Y NO COMO JAVASCRIPT. La cara de un modulo recibe codigo y
		  // lo evalua, y por eso su politica pide 'unsafe-eval'; la sala no necesita esa concesion:
		  // recibe cinco campos y llama a la funcion que el sustrato publico.
		  window.addEventListener('message', function (e) {
		    if (!e.data || e.data.gnothi !== 'sala') return;
		    if (e.source !== padre) return;
		    try { window.sala.pedido(e.data.id, e.data.verbo, e.data.modulo, e.data.a, e.data.b); }
		    catch (x) { send({ op: 'consola', a: 'error: el pedido no se pudo entregar: ' + x }); }
		  });

		  // LA SONDA, CONTESTADA POR LA APP Y NO POR UNA PERSONA. Sale antes de que corra una linea
		  // del sustrato, asi que la respuesta llega INCLUSO SI EL SUSTRATO NO ARRANCA.
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

// LA UNICA PUERTA DE LA PAGINA HACIA C# PARA LA SALA, gemela de `FrameBridge`.
public static class SalaBridge
{
	internal static PhotinoRoom? Current;

	[Microsoft.JSInterop.JSInvokable("FromSala")]
	public static void FromSala(string raw) => Current?.FromSala(raw);
}
