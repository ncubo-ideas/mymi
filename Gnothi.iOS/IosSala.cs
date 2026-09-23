using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using CoreFoundation;
using CoreGraphics;
using Foundation;
using Gnothi.Host;
using UIKit;
using WebKit;

namespace Gnothi.iOS;

// LA SALA EN iOS: EL GEMELO DE AndroidSala.cs. UN documento, propiedad de Gnothi, con el runtime
// .NET y el motor compilados a wasm. Los modulos llegan despues, como bytes, y corren ahi adentro.
//
// LO QUE ESTE ARCHIVO HACE Y LO QUE NO. Sirve los bytes y pasa los pedidos. No decide nada: ni
// que modulo se carga, ni cuando se desaloja, ni quien pregunta. Eso lo decide SalaShell, que es
// quien tiene el marco de la llamada que abrio el Host. Y SalaShell es el MISMO archivo en las
// dos plataformas -- vive en Gnothi.MobileShared -- asi que lo que cambia aca es como se le habla
// a WebKit, y nada mas.
//
// ── LO QUE DIFIERE DE ANDROID, Y ES POCO ──────────────────────────────────────────────────────
//
//   1 · EL ORIGEN ES UN ESQUEMA PROPIO, Y ACA NO CUESTA NADA. Android sirve la sala sobre
//       `https://sala.gnothi.local` porque alla un esquema propio NO es contexto seguro, y sin
//       contexto seguro se cae lo que el runtime necesita. WebKit no deja manejar `http` ni
//       `https` -- `SetUrlSchemeHandler` tira para los esquemas reservados -- asi que un esquema
//       propio no es una preferencia, es lo unico que hay.
//
//       Y SE PUEDE, PORQUE YA ESTA MEDIDO. La sonda P0 de la forma hospedada contesto el
//       2026-09-07 en los cuatro modulos, en un simulador iPhone 17 Pro / iOS 26.5:
//
//           secureContext=true crypto.subtle=true storage=true origin=gnothi-mod://<modulo>
//
//       Ver HALLAZGOS-CORRIENDO-EL-GUION.md §1. O sea que la razon por la que Android usa un host
//       virtual no existe de este lado.
//
//       Y LO QUE ESA MEDICION NO DECIA -- si el SUSTRATO arranca bajo un scheme handler --
//       TAMBIEN ESTA CONTESTADO, porque la sonda de abajo lo volvio a preguntar adentro de la
//       sala. Medido el 2026-09-21, mismo simulador, dos corridas:
//
//           sala: P0 · secureContext=true crypto.subtle=true storage=true wasm=true
//                      wasm.streaming=true origin=gnothi-sala://sala
//           sala: P0 · fetch(mismo origen)=200 basic
//           sala-web: log: la sala esta lista, hospedada por Gnothi. Esperando pedidos.
//
//       Sin un solo 404 y sin un rechazo de CSP. El renglon que decide es el SEGUNDO, no el
//       primero: ver el punto 6.
//
//   2 · EL PUENTE ES DE UNA SOLA VIA. `WKScriptMessageHandler` no devuelve valor, asi que el
//       sustrato contesta con `postMessage` y el nativo responde por `EvaluateJavaScript` con el
//       mismo id. No cuesta nada: el contrato de la sala YA era asi -- `window.sala.pedido(id,
//       ...)` de ida y `SalaPuente.contesta(id, json)` de vuelta -- porque Android lo necesitaba
//       por el motivo inverso, que alla los metodos inyectados son sincronicos.
//
//   3 · EL SHIM NO SE ESCRIBE EN `index.html`, Y ESA ES LA DECISION QUE VALE CONTAR. El sustrato
//       decide si esta hospedado con `!!window.SalaPuente`: si hay puente se queda esperando
//       pedidos, y si no, corre la barrida de medicion del laboratorio. Si el shim viviera en el
//       documento, `SalaPuente` existiria SIEMPRE y el laboratorio suelto dejaria de existir.
//
//       Va como `WKUserScript` a `AtDocumentStart`, que corre antes que una sola linea del
//       sustrato y solo cuando Gnothi hospeda. El documento se queda sin tocar -- es el MISMO
//       archivo que publica el sustrato para las dos plataformas -- y `salaHospedada()` sigue
//       diciendo la verdad. Es la misma forma que `IosWebModule.Shim` ya usa para la hospedada.
//
//   4 · NO HAY WebChromeClient. La consola del sustrato se captura parcheando `console` en ese
//       mismo user script y sale por el mismo canal, igual que en la forma hospedada.
//
//   5 · LOS OBJETOS DEL PROTOCOLO VAN APARTE. Los protocolos de WebKit son protocolos de ObjC,
//       asi que quien los implemente tiene que descender de `NSObject`. Android llega a la misma
//       forma por el motivo espejo: `@JavascriptInterface` pide un `Java.Lang.Object`.
//
//   6 · LO QUE DE VERDAD HABIA QUE MEDIR NO ERA EL CONTEXTO SEGURO: ERA `fetch`.
//
//       Es facil creer que P0 ya lo cubria, y no. La forma hospedada NUNCA hace fetch por su
//       origen: sus subrecursos los pide el documento y leer/escribir/pull van por el puente.
//       El sustrato no puede evitarlo -- Blazor baja `_framework/*` con fetch, y `Sala.cs` pide
//       los .dll del modulo y su journal con HttpClient, que en wasm ES fetch --, asi que si
//       WebKit no entregara el esquema propio a fetch, la sala no arrancaria AUNQUE el origen
//       fuera seguro, y el plan B seria un servidor local en 127.0.0.1 con su puerto y su ciclo
//       de vida.
//
//       Lo entrega: `fetch(mismo origen)=200 basic`. No hace falta el servidor local.
//
// EL DOCUMENTO NO TIENE PANTALLA. No se agrega a ninguna vista: existe para contestar. La cara de
// la institucion sigue siendo la forma hospedada, en su propio origen, y se monta solo cuando la
// persona la abre.
internal sealed class IosSala : ISalaHost
{
	// El origen de la sala. Un esquema propio y un host fijo: hay UNA sala para todos los
	// modulos, no una por modulo -- que es justamente la diferencia con `gnothi-mod://<modulo>`,
	// donde el host es el modulo porque ahi si se quiere un origen por cada uno.
	private const string Esquema = "gnothi-sala";
	private const string Anfitrion = "sala";
	private const string Raiz = Esquema + "://" + Anfitrion + "/";

	// El unico canal: el puente y la consola vuelven por aca.
	private const string Canal = "SalaPuente";

	// DE DONDE SALE EL SUSTRATO DENTRO DEL .app. El csproj lo mete como BundleResource con
	// LogicalName `sala/...`, que es el espejo exacto del `Assets\sala\...` de Android.
	private static string EnElPaquete(string ruta) =>
		Path.Combine(NSBundle.MainBundle.BundlePath, "sala", ruta.Replace('/', Path.DirectorySeparatorChar));

	private readonly Action<string> log;
	private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> esperando = new(StringComparer.Ordinal);

	// UNA SOLA RANURA. Misma correccion y mismo motivo que en Android: si esto acumulara los
	// modulos de la sesion, cualquier dominio adentro de la sala podria pedir por su propio
	// origen el journal de OTRO modulo, directo del disco, sin pasar por la memoria y sin que el
	// desmontaje tuviera nada que ver. Con una sola ranura lo servible es lo del modulo EN VUELO
	// y nada mas; lo demas es 404 aunque se pida con su nombre exacto.
	private readonly object cerrojo = new();
	private string enVuelo = "";
	private Preparado? preparado;

	private WKWebView? web;
	private TaskCompletionSource<bool>? listo;
	private int siguiente;

	internal IosSala(Action<string> log) => this.log = log;

	private sealed record Preparado(string Dominio, string Entrada, string Journal);

	// ---------------------------------------------------------------- lo que SalaShell pide

	public Task Preparar(string modulo, string entrada, string grantedFolder, CancellationToken ct)
	{
		// `entrada` es la ruta del .dll que el manifiesto declara. Su carpeta es de donde salen los
		// demas ensamblados del modulo, que es la convencion del paquete hibrido.
		string dominio = Path.GetDirectoryName(entrada) ?? "";

		lock (cerrojo)
		{
			// PREPARAR AL SIGUIENTE CIERRA AL ANTERIOR. No se acumula: la ranura es una.
			if (enVuelo.Length > 0 && !string.Equals(enVuelo, modulo, StringComparison.Ordinal))
				log($"sala: {enVuelo} deja de ser servible");

			enVuelo = modulo;
			preparado = new Preparado(dominio, Path.GetFileNameWithoutExtension(entrada), grantedFolder);
		}

		log($"sala: {modulo} preparado desde {dominio}");
		return Task.CompletedTask;
	}

	// SOLTAR: despues de guardar lo que el modulo escribio, deja de ser servible. Entre el desalojo
	// y la carga del siguiente NO hay ningun modulo servible, que es lo que se quiere: si en esa
	// ventana algo pidiera un journal, la respuesta es 404.
	public Task Soltar(string modulo, CancellationToken ct)
	{
		lock (cerrojo)
		{
			if (!string.Equals(enVuelo, modulo, StringComparison.Ordinal)) return Task.CompletedTask;
			enVuelo = "";
			preparado = null;
		}

		log($"sala: {modulo} soltado, ya no se sirve por el origen");
		return Task.CompletedTask;
	}

	private Preparado? Servible(string modulo)
	{
		lock (cerrojo)
		{
			return string.Equals(enVuelo, modulo, StringComparison.Ordinal) ? preparado : null;
		}
	}

	// LO QUE EL MODULO ESCRIBIO, ATERRIZANDO EN EL DISCO. El cerco es el mismo que el puente de la
	// forma hospedada: la ruta que el modulo da es RELATIVA, la ancla Gnothi a SU carpeta, y si el
	// resultado se sale de ahi no se escribe. Es responsabilidad del Host, no confianza en el modulo.
	public Task Guardar(string modulo, string ruta, string contenidoBase64, CancellationToken ct)
	{
		// Y SOLO SE LE GUARDA AL QUE ESTA EN VUELO. Guardar para un modulo que ya se solto seria
		// escribir en la carpeta de alguien que no esta, a pedido de quien si esta.
		if (Servible(modulo) is not Preparado p)
			throw new InvalidOperationException($"'{modulo}' no esta en vuelo: no se le guarda nada");

		string raiz = Path.GetFullPath(p.Journal);
		string destino = Path.GetFullPath(Path.Combine(raiz, ruta.Replace('/', Path.DirectorySeparatorChar)));

		if (!destino.StartsWith(raiz, StringComparison.Ordinal))
			throw new UnauthorizedAccessException($"ruta fuera del journal de {modulo}: {ruta}");

		Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
		byte[] bytes = Convert.FromBase64String(contenidoBase64);
		File.WriteAllBytes(destino, bytes);

		log($"sala/guardar {modulo}: {ruta} · {bytes.Length} bytes");
		return Task.CompletedTask;
	}

	public async Task<string> Pedir(string verbo, string modulo, string a, string b, CancellationToken ct)
	{
		await Arrancar(ct);

		string id = Interlocked.Increment(ref siguiente).ToString();
		var espera = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
		esperando[id] = espera;

		string js = $"window.sala.pedido({J(id)}, {J(verbo)}, {J(modulo)}, {J(a)}, {J(b)})";
		DispatchQueue.MainQueue.DispatchAsync(() => web?.EvaluateJavaScript(new NSString(js), null));

		using CancellationTokenRegistration _ = ct.Register(() => espera.TrySetCanceled(ct));
		string respuesta = await espera.Task;

		// LA CONFORMIDAD QUEDA EN EL LOG, no en un comentario. Cargar y desalojar reportan que
		// quedo de los OTROS modulos en memoria; si alguna vez dice mas de cero bytes, la frontera
		// entre journals se rompio y esto lo grita donde se ve.
		if (verbo is "cargar" or "desalojar") log($"sala/{verbo} {modulo}: {respuesta}");

		return respuesta;
	}

	// ---------------------------------------------------------------- el documento

	// TIRAR EL DOCUMENTO, que es lo unico que recupera una sala que no contesta. Lo pide la cascara
	// cuando un pedido paso del tope, y el caso que lo motiva es el desalojo: ahi adentro corre el
	// Dispose() del modulo, o sea codigo que el modulo eligio, y un ciclo infinito no se puede
	// interrumpir desde adentro de un motor web de un solo hilo.
	//
	// DESDE ACA SI, porque el motor corre en OTRO proceso -- en iOS es com.apple.WebKit.WebContent
	// y en Android el del WebView -- asi que soltar la vista se lleva el ciclo puesto.
	//
	// Y SE LLEVA LA FRONTERA CONSIGO: lo que el modulo tenia montado vive en la memoria del
	// documento, no en el disco, asi que aca no hay nada que desmontar ni que confirmar. Ademas
	// suelta la ranura, porque un modulo sin documento no tiene por que seguir siendo servible.
	public Task Descartar(string motivo, CancellationToken ct)
	{
		log($"sala: se tira el documento -- {motivo}");

		lock (cerrojo)
		{
			enVuelo = "";
			preparado = null;
		}

		// Las esperas pendientes no se quedan colgando: la que se trabo, y cualquier otra, fallan
		// con el motivo. El documento donde iban a contestarse ya no existe.
		foreach (string id in esperando.Keys)
			if (esperando.TryRemove(id, out TaskCompletionSource<string>? espera))
				espera.TrySetException(new InvalidOperationException($"se tiro la sala: {motivo}"));

		WKWebView? viejo = Interlocked.Exchange(ref web, null);

		// Y RECIEN AHORA se olvida el arranque, para que nadie se quede esperando un documento
		// muerto. El proximo pedido crea uno nuevo; los pedidos vienen serializados por `UnaPorVez`
		// del lado de la cascara, asi que no hay dos creandolo a la vez.
		listo = null;

		if (viejo is not null)
			DispatchQueue.MainQueue.DispatchAsync(() =>
			{
				try
				{
					viejo.StopLoading();
					// El handler del canal RETIENE al puente, y el puente a esta sala. Sin sacarlo,
					// el documento viejo no se suelta nunca -- es la misma limpieza que
					// `IosWebModule.Teardown` hace con el suyo.
					viejo.Configuration.UserContentController.RemoveScriptMessageHandler(Canal);
					viejo.RemoveFromSuperview();
				}
				catch (Exception e) { log($"sala: al tirar el documento -- {e.GetType().Name}: {e.Message}"); }
			});

		return Task.CompletedTask;
	}

	// ARRANCAR LA SALA SIN QUE NADIE HAYA PREGUNTADO, Y ES UN INSTRUMENTO DE MEDICION.
	//
	// `Arrancar` es privado y lo dispara el primer `Pedir`, que a su vez exige un modulo instalado
	// y forzado a la sala. Pero la pregunta que decide la forma en iOS -- si un documento Blazor
	// wasm arranca bajo un scheme handler -- NO necesita ningun modulo: se contesta con el
	// sustrato solo. Separarlas importa, porque si se midieran juntas un fallo de instalacion
	// leeria igual que un fallo del origen.
	//
	// Solo lo llama el interruptor de medicion de ViewController, que ya va en DEBUG.
	internal Task Calentar(CancellationToken ct) => Arrancar(ct);

	private async Task Arrancar(CancellationToken ct)
	{
		if (listo is not null) { await listo.Task; return; }

		var arrancando = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		if (Interlocked.CompareExchange(ref listo, arrancando, null) is not null)
		{
			await listo!.Task;
			return;
		}

		log("sala: creando el documento");

		DispatchQueue.MainQueue.DispatchAsync(() =>
		{
			try
			{
				var configuracion = new WKWebViewConfiguration();
				configuracion.SetUrlSchemeHandler(new ServidorDeLaSala(this), Esquema);

				WKUserContentController contenido = configuracion.UserContentController;
				contenido.AddScriptMessageHandler(new PuenteDeLaSala(this), Canal);
				contenido.AddUserScript(new WKUserScript(
					new NSString(Shim), WKUserScriptInjectionTime.AtDocumentStart, isForMainFrameOnly: true));

				// NO se agrega a ninguna vista: la sala no tiene pantalla, existe para contestar.
				web = new WKWebView(CGRect.Empty, configuracion);

#if DEBUG
				// Sin esto el inspector de Safari no ve este WebView. Desde iOS 16.4 hay que
				// decirlo, y el valor por omision es false. Solo en DEBUG: un WebView inspeccionable
				// deja leer la memoria de la pagina con un cable, y ahi adentro esta el journal de
				// la persona. Mismo criterio que `IosWebModule`.
				if (OperatingSystem.IsIOSVersionAtLeast(16, 4)) web.Inspectable = true;
#endif

				web.LoadRequest(new NSUrlRequest(new NSUrl(Raiz)));
			}
			catch (Exception e)
			{
				log($"sala: no se pudo crear el documento -- {e.GetType().Name}: {e.Message}");
				arrancando.TrySetException(e);
			}
		});

		// EL TOPE DE ARRANQUE ES DE 60 s COMO EN ANDROID, Y NO ES EL QUE MANDA.
		//
		// `SalaShell.Pedir` envuelve CADA verbo con `SalaShell.Tope`, que son 10 s, y el arranque
		// ocurre adentro del primer verbo. O sea que los 10 de la cascara vencen antes que los 60
		// de aca: a los 10 s la cascara tira el documento y el proximo pedido arranca uno nuevo.
		//
		// SE TEMIO QUE ESO FUERA EL CASO NORMAL EN EL SIMULADOR, porque la forma hospedada midio
		// ahi ~64 s en frio (HALLAZGOS §11.4) y con ese numero la sala entraria en un ciclo de
		// arrancar y ser descartada sin contestar nunca. NO OCURRE. Medido el 2026-09-21 en un
		// simulador iPhone 17 Pro / iOS 26.5, desde `creando el documento` hasta `el sustrato
		// contesta`:
		//
		//     corrida 1 .... 4,18 s        corrida 2 .... 3,84 s
		//
		// Entra en los 10 s de la cascara, y con margen. Que el sustrato adelgazado arranque
		// quince veces mas rapido que un modulo hospedado EN EL MISMO SIMULADOR es un hecho
		// medido; por que, no se midio, asi que no se afirma.
		//
		// NO SE TOCA `Tope` DESDE ACA de todas formas: es un invariante de la cascara, igual en
		// las dos plataformas, y acomodarlo al banco de pruebas seria al reves. Si algun dia un
		// aparato de verdad se acerca a los 10 s, se cambia donde vive el tope y con el numero
		// al lado.
		await arrancando.Task.WaitAsync(TimeSpan.FromSeconds(60), ct);
	}

	internal void Lista()
	{
		log("sala: el sustrato contesta");
		listo?.TrySetResult(true);
	}

	internal void Contesta(string id, string json)
	{
		if (esperando.TryRemove(id, out TaskCompletionSource<string>? espera)) espera.TrySetResult(json);
	}

	// ---------------------------------------------------------------- el puente, de vuelta

	internal void AlLlegarUnMensaje(WKScriptMessage mensaje)
	{
		if (mensaje.Body is not NSString crudo) return;

		try
		{
			using JsonDocument sobre = JsonDocument.Parse(crudo.ToString());
			JsonElement raiz = sobre.RootElement;

			switch (Texto(raiz, "op"))
			{
				case "lista": Lista(); return;
				case "contesta": Contesta(Texto(raiz, "id"), Texto(raiz, "a")); return;

				// La consola del sustrato, que en iOS no tiene WebChromeClient que la recoja. Es
				// por donde se ven las violaciones de CSP y los errores del runtime.
				case "consola": log($"sala-web: {Texto(raiz, "a")}"); return;

				// LA SONDA, CONTESTADA POR LA APP Y NO POR UNA PERSONA. Sale antes de que corra una
				// linea del sustrato, asi que la respuesta llega INCLUSO SI EL SUSTRATO NO ARRANCA
				// -- que es exactamente el caso en que se la iba a necesitar.
				case "sonda": log($"sala: P0 · {Texto(raiz, "a")}"); return;

				default: log($"sala: verbo desconocido del puente: {Texto(raiz, "op")}"); return;
			}
		}
		catch (JsonException e) { log($"sala: mensaje ilegible del puente: {e.Message}"); }
	}

	private static string Texto(JsonElement o, string campo) =>
		o.TryGetProperty(campo, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

	// ---------------------------------------------------------------- de donde salen los bytes

	// TRES ORIGENES PARA UN SOLO ORIGEN WEB. El sustrato sale del .app; los .dll del modulo, de su
	// carpeta instalada; y el journal, de la carpeta de la persona. El modulo no distingue: para el
	// es todo su propio origen, y no abre un archivo en su vida.
	internal void AlPedirUnArchivo(IWKUrlSchemeTask tarea)
	{
		NSUrl? url = tarea.Request.Url;
		if (url is null) return;

		string ruta = (url.Path ?? "/").TrimStart('/');
		if (ruta.Length == 0 || ruta.EndsWith("/", StringComparison.Ordinal)) ruta += "index.html";

		try
		{
			Servida servida = Servir(ruta);

			var cabeceras = new NSMutableDictionary
			{
				["Content-Type"] = new NSString(servida.Tipo),
				["Cache-Control"] = new NSString("no-store"),
			};
			if (servida.Csp is not null) cabeceras["Content-Security-Policy"] = new NSString(servida.Csp);

			tarea.DidReceiveResponse(new NSHttpUrlResponse(url, servida.Estado, "HTTP/1.1", cabeceras));
			// Un 404 no lleva cuerpo, y `DidReceiveData` con cero bytes no es lo mismo que no
			// llamarlo: WebKit acepta que una tarea termine sin datos.
			if (servida.Cuerpo.Length > 0) tarea.DidReceiveData(NSData.FromArray(servida.Cuerpo));
			tarea.DidFinish();
		}
		catch (Exception e)
		{
			// Un sustrato que no se puede servir NO puede fallar en silencio: WebKit mostraria una
			// pagina en blanco, que es el sintoma que ya costo una corrida entera en Android.
			log($"sala: 500 {ruta} -- {e.GetType().Name}: {e.Message}");
			tarea.DidFailWithError(new NSError(new NSString("Gnothi"), 1));
		}
	}

	private sealed record Servida(byte[] Cuerpo, string Tipo, int Estado, string? Csp);

	private Servida Servir(string ruta)
	{
		if (ruta.StartsWith("modulo/", StringComparison.Ordinal))
		{
			string[] partes = ruta.Split('/', 4);
			if (partes.Length < 3) return NoEsta(ruta);

			string modulo = partes[1];
			if (Servible(modulo) is not Preparado p) return NoEsta(ruta);

			// modulo/<m>/indice.json -- que ensamblados trae y cual es la entrada
			if (partes[2] == "indice.json")
			{
				// SOLO LO QUE ES DEL MODULO. Todo lo demas lo presta el sustrato, y no es una
				// optimizacion: un paquete del BCL viene compilado POR PLATAFORMA, y el que esta en
				// la carpeta del modulo es el de este telefono. Medido en Android: cargarlo adentro
				// del motor web revienta con TypeLoadException sobre las intrinsecas de ARM.
				string[] archivos = [.. Directory.EnumerateFiles(p.Dominio, "*.dll")
					.Select(Path.GetFileNameWithoutExtension)
					.Where(n => n is not null && !LoPrestaLaSala(n))
					.Select(n => n!)
					.OrderBy(n => string.Equals(n, p.Entrada, StringComparison.Ordinal) ? 1 : 0)];

				return Texto(JsonSerializer.Serialize(new { entrada = p.Entrada, archivos }), "application/json");
			}

			// modulo/<m>/dominio/<x>.dll
			if (partes[2] == "dominio" && partes.Length == 4)
				return Archivo(Path.Combine(p.Dominio, partes[3]), "application/octet-stream");

			// modulo/<m>/journal/indice.json -- lo que hay en la carpeta de la persona
			if (partes[2] == "journal" && partes.Length == 4 && partes[3] == "indice.json")
			{
				string[] rutas = Directory.Exists(p.Journal)
					? [.. Directory.EnumerateFiles(p.Journal, "*", SearchOption.AllDirectories)
						.Select(f => Path.GetRelativePath(p.Journal, f).Replace(Path.DirectorySeparatorChar, '/'))]
					: [];

				return Texto(JsonSerializer.Serialize(new { rutas }), "application/json");
			}

			// modulo/<m>/journal/<ruta> -- y NO se sale de la carpeta concedida
			if (partes[2] == "journal" && partes.Length == 4)
			{
				string raiz = Path.GetFullPath(p.Journal);
				string pedido = Path.GetFullPath(Path.Combine(raiz, partes[3].Replace('/', Path.DirectorySeparatorChar)));
				if (!pedido.StartsWith(raiz, StringComparison.Ordinal)) return NoEsta(ruta);

				return Archivo(pedido, "application/octet-stream");
			}

			return NoEsta(ruta);
		}

		// todo lo demas es el sustrato, que viaja en el .app
		return DelPaquete(ruta);
	}

	// LA CSP DEL DOCUMENTO DE LA SALA, Y LA CALCULA BundleServer, NO ESTA CLASE. Android tuvo una
	// copia aca y se rompio: el analizador de HTML normaliza CRLF a LF antes de hashear el script
	// en linea, y la copia hasheaba los bytes crudos; con el archivo en CRLF el motor rechazo el
	// script que Gnothi acababa de permitir y el documento quedo EN BLANCO. Hay UNA sola copia, y
	// de paso la sala hereda la misma politica que la forma hospedada.
	private static Servida DelPaquete(string ruta)
	{
		string entero = EnElPaquete(ruta);
		if (!File.Exists(entero)) return new Servida([], "text/plain", 404, null);

		byte[] cuerpo = File.ReadAllBytes(entero);

		// El documento se lee entero porque su CSP depende de su contenido. Los demas se sirven tal
		// cual. Aca no hay streaming: `IWKUrlSchemeTask` entrega NSData, no un Stream.
		return ruta.EndsWith(".html", StringComparison.Ordinal)
			? new Servida(cuerpo, Tipo(ruta), 200, Gnothi.MobileShared.BundleServer.CspFor(cuerpo))
			: new Servida(cuerpo, Tipo(ruta), 200, null);
	}

	private static Servida Archivo(string ruta, string tipo) =>
		File.Exists(ruta)
			? new Servida(File.ReadAllBytes(ruta), tipo, 200, null)
			: new Servida([], "text/plain", 404, null);

	private static Servida Texto(string cuerpo, string tipo) =>
		new(Encoding.UTF8.GetBytes(cuerpo), tipo, 200, null);

	private Servida NoEsta(string ruta)
	{
		log($"sala: 404 {ruta}");
		return new Servida([], "text/plain", 404, null);
	}

	// LA LISTA DE LO PRESTADO, que es la otra cara de la lista de prohibidos del instalador. Lo que
	// esta aca lo pone el sustrato en su build de browser-wasm; lo que no, viaja en el modulo. Si un
	// modulo necesitara algo que no esta, la respuesta NO es cargarselo de su carpeta: es agregarlo
	// al sustrato. Misma lista que AndroidSala, y tiene que seguir siendolo.
	private static bool LoPrestaLaSala(string ensamblado) =>
		ensamblado.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
		|| ensamblado.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase)
		|| ensamblado is "Puppeteer" or "Choreography" or "Puppets.Portador" or "Gnothi.Contract"
		|| ensamblado.StartsWith("ModelContextProtocol", StringComparison.OrdinalIgnoreCase)
		|| ensamblado is "netstandard" or "mscorlib";

	private static string Tipo(string ruta) => Path.GetExtension(ruta).ToLowerInvariant() switch
	{
		".html" => "text/html",
		".js" or ".mjs" => "text/javascript",
		".css" => "text/css",
		".wasm" => "application/wasm",
		".json" => "application/json",
		_ => "application/octet-stream",
	};

	private static string J(string s) => JsonSerializer.Serialize(s);

	// ---------------------------------------------------------------- lo que la pagina ve

	// EL PUENTE, Y LO QUE NO TIENE. Dos verbos: «ya estoy» y «aca esta la respuesta». No hay leer,
	// ni escribir, ni pedir nada -- el journal se lo pone Gnothi antes de preguntar, y lo que el
	// modulo escriba se queda en memoria. Es el mismo par que Android inyecta con
	// `AddJavascriptInterface(..., "SalaPuente")`.
	//
	// Y ES LO QUE EL DOMINIO DE LA INSTITUCION NO ALCANZA: el modulo corre adentro del runtime .NET
	// del sustrato, sin acceso a `DotNet.invokeMethodAsync` ni a este objeto. Quien lo usa es el
	// sustrato, que es de Gnothi.
	private const string Shim = """
		(function () {
		  var send = function (m) { window.webkit.messageHandlers.SalaPuente.postMessage(JSON.stringify(m)); };

		  window.SalaPuente = {
		    lista:    function ()         { send({ op: 'lista',    a: '' }); },
		    contesta: function (id, json) { send({ op: 'contesta', id: id, a: json }); }
		  };

		  // LA SONDA, MEDIDA POR LA APP. Corre antes que una sola linea del sustrato, asi que la
		  // respuesta llega incluso si el sustrato despues no arranca -- que es el caso que
		  // importa.
		  try {
		    send({ op: 'sonda', a:
		      'secureContext=' + !!window.isSecureContext +
		      ' crypto.subtle=' + !!(window.crypto && window.crypto.subtle) +
		      ' storage=' + !!navigator.storage +
		      ' wasm=' + (typeof WebAssembly !== 'undefined') +
		      ' wasm.streaming=' + (typeof WebAssembly !== 'undefined' && !!WebAssembly.instantiateStreaming) +
		      ' origin=' + window.location.origin });
		  } catch (x) { }

		  // Y LA QUE DE VERDAD DECIDE LA FORMA, QUE NO ES EL CONTEXTO SEGURO: SI `fetch` ANDA
		  // CONTRA UN ESQUEMA PROPIO.
		  //
		  // P0 no la contesta, aunque parezca que si. La forma hospedada NUNCA hace fetch por su
		  // origen: sus subrecursos los pide el documento y todo lo demas -- leer, escribir, pull
		  // -- va por el puente. El sustrato no puede hacer eso: Blazor baja `_framework/*` con
		  // fetch, y `Sala.cs` pide los .dll del modulo y su journal con HttpClient, que en wasm
		  // ES fetch. Si WebKit no entrega el esquema propio a fetch, la sala no arranca aunque el
		  // origen sea seguro, y ahi si toca el servidor local en 127.0.0.1.
		  //
		  // Va contra el propio documento, que existe seguro, y llega tarde a proposito: es
		  // asincronica, asi que sale en su propio renglon del log.
		  try {
		    fetch('index.html', { cache: 'no-store' })
		      .then(function (r) { send({ op: 'sonda', a: 'fetch(mismo origen)=' + r.status + ' ' + r.type }); })
		      .catch(function (e) { send({ op: 'sonda', a: 'fetch(mismo origen) FALLA: ' + e }); });
		  } catch (x) { send({ op: 'sonda', a: 'fetch(mismo origen) ni existe: ' + x }); }

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

// EL PUENTE, del lado nativo. Los protocolos de WebKit son protocolos de ObjC, asi que esto tiene
// que descender de NSObject -- que es por lo que es un objeto aparte y no la sala misma. Android
// tiene el mismo par por el motivo espejo.
internal sealed class PuenteDeLaSala(IosSala sala) : NSObject, IWKScriptMessageHandler
{
	public void DidReceiveScriptMessage(WKUserContentController controlador, WKScriptMessage mensaje) =>
		sala.AlLlegarUnMensaje(mensaje);
}

// GNOTHI CONTESTANDOLE AL MOTOR por los bytes del sustrato, de los .dll y del journal. Es el
// equivalente de `ShouldInterceptRequest` en Android, y la diferencia es que aca el esquema es
// nuestro: WebKit no entrega `http` ni `https` a nadie.
internal sealed class ServidorDeLaSala(IosSala sala) : NSObject, IWKUrlSchemeHandler
{
	public void StartUrlSchemeTask(WKWebView web, IWKUrlSchemeTask tarea) => sala.AlPedirUnArchivo(tarea);

	// No hay nada que cancelar: cada respuesta se produce y se termina dentro de
	// StartUrlSchemeTask. El metodo no es opcional en el protocolo, asi que existe y dice por que
	// esta vacio.
	public void StopUrlSchemeTask(WKWebView web, IWKUrlSchemeTask tarea) { }
}
