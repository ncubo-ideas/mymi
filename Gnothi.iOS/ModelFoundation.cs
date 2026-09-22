using System.Runtime.InteropServices;
using System.Text.Json;
using Gnothi.Assistant;
using Gnothi.Host;

namespace Gnothi.iOS;

// EL MODELO DEL TELEFONO, COMO UN IModel MAS.
//
// Vive en Gnothi.iOS y no en Gnothi.Assistant a proposito: lleva DllImport contra simbolos que
// solo existen en este ejecutable. Gnothi.Assistant no debe saber que iOS existe -esa es la
// propiedad que mide Gnothi.Assistant.Tests-, asi que la cascara lo INYECTA y el Shell lo recibe
// como una fabrica.
//
// "__Internal": la libreria de Swift entra estatica al ejecutable via la NativeReference del
// csproj, asi que los simbolos ya estan adentro del proceso. No hay .dylib que cargar.
//
// EL C# SIGUE SIENDO DUENO DE LA CONVERSACION. Cada paso manda el intercambio completo y el
// catalogo, y recibe UN paso. Del otro lado no hay sesion viva: el transcript se rearma. Eso es
// lo que permite que AgentLoop no se entere de nada.
internal sealed class ModelFoundation : IModel
{
	[DllImport("__Internal", EntryPoint = "gnothi_fm_estado")]
	private static extern int NativeState();

	[DllImport("__Internal", EntryPoint = "gnothi_fm_paso")]
	private static extern IntPtr NativeStep(string conversacion, string tools, string instrucciones);

	[DllImport("__Internal", EntryPoint = "gnothi_fm_liberar")]
	private static extern void NativeFree(IntPtr p);

	// 0 no hay · 1 dice estar disponible y NO genera · 2 genera de verdad.
	//
	// SON TRES Y NO DOS PORQUE `availability` MIENTE: en iOS 26.2 devuelve `available` y la
	// generacion falla con ModelManagerError 1026 -medido, HALLAZGOS seccion 8-. Un `if
	// isAvailable` pasa el simulador y se rompe en el telefono, asi que el 2 se comprueba
	// GENERANDO. Es lento la primera vez; por eso se cachea.
	private static int? estado;

	public static int State() => estado ??= NativeState();

	public static bool Works() => State() == 2;

	public static string Diagnosis() => State() switch
	{
		2 => "el modelo de este telefono contesta",
		1 => "este telefono dice tener modelo pero no genera: hace falta iOS 26.5 o superior",
		_ => "este telefono no tiene modelo propio",
	};

	private readonly string instrucciones;

	public ModelFoundation(string instrucciones = "") => this.instrucciones = instrucciones;

	public Task<Said> Next(
		IReadOnlyList<Exchange> conversation, IReadOnlyList<ToolForTheModel> tools, CancellationToken ct)
	{
		// Al hilo de fondo: del otro lado se espera a que termine la generacion, y desde el hilo
		// de UI eso se traba. La primera respuesta del dia puede pasar los 90 s.
		return Task.Run(() =>
		{
			ct.ThrowIfCancellationRequested();

			IntPtr p = IntPtr.Zero;
			try
			{
				p = NativeStep(Conversacion(conversation), Catalogo(tools), instrucciones);
				string crudo = Marshal.PtrToStringUTF8(p) ?? "{}";
				return Leer(crudo, conversation);
			}
			finally
			{
				// La cadena la libera quien la pide: del otro lado es un strdup.
				if (p != IntPtr.Zero) NativeFree(p);
			}
		}, ct);
	}

	private static string Conversacion(IReadOnlyList<Exchange> conversation)
	{
		var salida = new List<object?>();

		foreach (Exchange e in conversation)
		{
			switch (e)
			{
				case Asked a:
					salida.Add(new Dictionary<string, object?> { ["tipo"] = "asked", ["texto"] = a.Question });
					break;

				case Said s:
					salida.Add(new Dictionary<string, object?>
					{
						["tipo"] = "said",
						["texto"] = s.Text ?? "",
						["calls"] = s.Calls.Select(c => new Dictionary<string, object?>
						{
							["id"] = c.Id,
							["name"] = c.PublicName,
							// Solo texto: los argumentos de Gnothi son valores cerrados -un
							// analito, un termino-, y el otro lado arma el esquema con strings.
							["args"] = c.Arguments.ToDictionary(x => x.Key, x => x.Value?.ToString() ?? ""),
						}).ToList(),
					});
					break;

				case Returned r:
					salida.Add(new Dictionary<string, object?>
					{
						["tipo"] = "returned",
						["callId"] = r.CallId,
						["toolName"] = NombreDe(conversation, r.CallId),
						["json"] = r.Json,
					});
					break;
			}
		}

		return Json.Write(salida);
	}

	// El nombre de la tool no viaja en Returned -alcanza con el CallId para aparearlo-, pero el
	// transcript del otro lado lo pide. Se busca en el Said que hizo la llamada.
	private static string NombreDe(IReadOnlyList<Exchange> conversation, string callId)
	{
		foreach (Exchange e in conversation)
			if (e is Said s)
				foreach (ToolCall c in s.Calls)
					if (c.Id == callId) return c.PublicName;

		return "";
	}

	// El catalogo se aplana a nombre + descripcion + parametros. El SchemaJson que trae
	// ToolForTheModel es json-schema; del otro lado se arma un DynamicGenerationSchema, que hoy
	// modela un objeto de strings. Alcanza para lo que los modulos declaran.
	private static string Catalogo(IReadOnlyList<ToolForTheModel> tools)
	{
		var salida = new List<object?>();

		foreach (ToolForTheModel t in tools)
		{
			var parametros = new List<object?>();

			try
			{
				JsonElement esquema = Json.Parse(t.SchemaJson);
				if (esquema.TryGetProperty("properties", out JsonElement props))
					foreach (JsonProperty prop in props.EnumerateObject())
						parametros.Add(new Dictionary<string, object?>
						{
							["name"] = prop.Name,
							["description"] = prop.Value.TryGetProperty("description", out JsonElement d)
								? d.GetString() ?? "" : "",
						});
			}
			catch
			{
				// Un esquema ilegible deja la tool SIN parametros en vez de tumbar la pregunta:
				// el modelo la va a poder llamar igual si no los necesita.
			}

			salida.Add(new Dictionary<string, object?>
			{
				["name"] = t.Name,
				["description"] = t.Description,
				["params"] = parametros,
			});
		}

		return Json.Write(salida);
	}

	// EL TOPE DEL MODELO DE APPLE, en tokens. Medido igual en iOS 26 y en macOS 27; se le dice a
	// la persona porque es el numero contra el que tiene que razonar, y va como valor y no
	// escrito en la frase para que el catalogo no lo repita en dos idiomas.
	private const string Tope = "4096";

	private static Said Leer(string crudo, IReadOnlyList<Exchange> conversation)
	{
		JsonElement r = Json.Parse(crudo);

		if (r.TryGetProperty("error", out JsonElement error))
		{
			string e = error.GetString() ?? "";

			// NO ENTRA. Medido en la Mac el 2026-09-08 (32 consultas = 7 307 tokens) y de nuevo
			// el 2026-09-21 (48 consultas = 14 730; Compras solo = 5 925; y en Cargas los hechos
			// de una sola consulta, 4 154 caracteres, dejan la segunda vuelta en 4 097). El error
			// crudo no le dice nada a la persona; lo que si puede hacer depende de QUE es lo que
			// no entra, y eso lo sabe esta clase y no el Swift:
			//
			//   - Sin ningun Returned en la conversacion, el modelo recibio catalogo + pregunta y
			//     ya no entro: es el CATALOGO, y la palanca es apagar consultas (el detalle de cada
			//     institucion ya la tiene; ver detalle.consultas.apagada).
			//   - Con un Returned, el catalogo YA ENTRO una vez -si no, el primer paso habria
			//     reventado aca mismo-: lo que desborda es la RESPUESTA de esa consulta, y eso no
			//     lo arregla la persona apagando nada.
			//
			// El texto del error lo corrobora -macOS 27 dice «Provided N tokens» en la primera y
			// «Content contains N tokens» en la segunda-, pero la conversacion es el hecho.
			if (NoEntra(e))
			{
				string? consulta = UltimaConsulta(conversation);
				throw new InvalidOperationException(consulta is null
					? Textos.T("asistente.modelo.no-entra.catalogo", ("tope", Tope), ("detalle", e))
					: Textos.T("asistente.modelo.no-entra.respuesta",
						("consulta", consulta), ("tope", Tope), ("detalle", e)));
			}

			throw new InvalidOperationException(Textos.T("asistente.modelo.fallo", ("detalle", e)));
		}

		var llamadas = new List<ToolCall>();

		if (r.TryGetProperty("calls", out JsonElement calls) && calls.ValueKind == JsonValueKind.Array)
		{
			foreach (JsonElement c in calls.EnumerateArray())
			{
				var args = new Dictionary<string, object?>();

				if (c.TryGetProperty("args", out JsonElement a) && a.ValueKind == JsonValueKind.String)
				{
					try
					{
						JsonElement objeto = Json.Parse(a.GetString() ?? "{}");
						if (objeto.ValueKind == JsonValueKind.Object)
							foreach (JsonProperty prop in objeto.EnumerateObject())
								args[prop.Name] = prop.Value.ValueKind == JsonValueKind.String
									? prop.Value.GetString() : prop.Value.ToString();
					}
					catch { /* sin argumentos legibles, la tool decide que hacer */ }
				}

				llamadas.Add(new ToolCall(
					c.TryGetProperty("id", out JsonElement id) ? id.GetString() ?? "" : "",
					c.TryGetProperty("name", out JsonElement n) ? n.GetString() ?? "" : "",
					args));
			}
		}

		string? texto = r.TryGetProperty("texto", out JsonElement t) ? t.GetString() : null;

		// NI TEXTO NI LLAMADAS NO ES UNA RESPUESTA, es un paso perdido. AgentLoop lo tomaria como
		// "termino sin decir nada" y le empujaria null a la pagina: la persona ve la pantalla en
		// blanco y no hay con que diagnosticar. Paso de verdad -un techo vencido devolvia {}- y
		// costo una prueba y media hora. Que reviente aca, con nombre.
		if (texto is null && llamadas.Count == 0)
			throw new InvalidOperationException(Textos.T("asistente.modelo.paso-vacio"));

		return new Said(texto, llamadas);
	}

	// LAS TRES FORMAS EN QUE EL MODELO DICE QUE NO ENTRA, una por sistema y ninguna documentada:
	//
	//   iOS 26     "exceededContextWindowSize(...7307 tokens...)"
	//   macOS 27   "GenerativeError: Provided 14,730 tokens, but the maximum allowed is 4,096."
	//   macOS 27   "GenerationError: Content contains 4097 tokens, which exceeds the maximum
	//               allowed context size of 4096."
	//
	// Se busca la FRASE y no el tipo: GenerationError tambien es como se llama el rechazo de
	// las barandas y el de un idioma que el modelo no habla, y esos no son «no entra». El Swift
	// arma la cadena como "\(type(of: error)): \(error)", asi que el texto viene entero.
	private static bool NoEntra(string e) =>
		e.Contains("exceededContextWindowSize", StringComparison.Ordinal)
		|| e.Contains("but the maximum allowed is", StringComparison.Ordinal)
		|| e.Contains("exceeds the maximum allowed context size", StringComparison.Ordinal);

	// LA ULTIMA CONSULTA QUE CONTESTO, o null si todavia ninguna: es lo que separa «no entra el
	// catalogo» de «no entra lo que devolvio X». Se nombra la ultima porque es la que acaba de
	// sumarse al transcript; las anteriores ya habian entrado.
	private static string? UltimaConsulta(IReadOnlyList<Exchange> conversation)
	{
		string? nombre = null;
		foreach (Exchange e in conversation)
			if (e is Returned r) nombre = NombreDe(conversation, r.CallId);
		return nombre;
	}
}
