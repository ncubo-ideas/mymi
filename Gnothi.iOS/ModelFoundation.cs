using System.Runtime.InteropServices;
using System.Text.Json;
using Gnothi.Assistant;

namespace Gnothi.iOS;

// THE PHONE'S MODEL, AS ONE MORE IModel.
//
// It lives in Gnothi.iOS and not in Gnothi.Assistant on purpose: it carries DllImports against
// symbols that only exist in this executable. Gnothi.Assistant must not know iOS exists -- that is
// the property Gnothi.Assistant.Tests measures -- so the shell INJECTS it and the Shell receives it
// as a factory.
//
// "__Internal": the Swift library goes statically into the executable through the csproj's
// NativeReference, so the symbols are already inside the process. There is no .dylib to load.
//
// C# REMAINS THE OWNER OF THE CONVERSATION. Each step sends the complete exchange and the catalogue,
// and receives ONE step. On the other side there is no live session: the transcript is rebuilt. That
// is what lets AgentLoop stay unaware of any of it.
internal sealed class ModelFoundation : IModel
{
	[DllImport("__Internal", EntryPoint = "gnothi_fm_estado")]
	private static extern int NativeState();

	[DllImport("__Internal", EntryPoint = "gnothi_fm_paso")]
	private static extern IntPtr NativeStep(string conversacion, string tools, string instrucciones);

	[DllImport("__Internal", EntryPoint = "gnothi_fm_liberar")]
	private static extern void NativeFree(IntPtr p);

	// 0 none · 1 claims to be available and does NOT generate · 2 really generates.
	//
	// THERE ARE THREE AND NOT TWO BECAUSE `availability` LIES: on iOS 26.2 it returns `available` and
	// the generation fails with ModelManagerError 1026 -- measured, HALLAZGOS section 8. An `if
	// isAvailable` passes the simulator and breaks on the phone, so the 2 is checked BY GENERATING.
	// It is slow the first time; hence the cache.
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
		// Onto the background thread: on the other side the generation is waited on, and from the UI
		// thread that deadlocks. The first answer of the day can take over 90 s.
		return Task.Run(() =>
		{
			ct.ThrowIfCancellationRequested();

			IntPtr p = IntPtr.Zero;
			try
			{
				p = NativeStep(Conversacion(conversation), Catalogo(tools), instrucciones);
				string crudo = Marshal.PtrToStringUTF8(p) ?? "{}";
				return Leer(crudo);
			}
			finally
			{
				// The string is freed by whoever asked for it: on the other side it is a strdup.
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
							// Text only: Gnothi's arguments are closed values -- an analyte,
							// a term -- and the other side builds the schema out of strings.
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

	// The tool's name does not travel in Returned -- the CallId is enough to pair it up -- but the
	// transcript on the other side asks for it. It is looked up in the Said that made the call.
	private static string NombreDe(IReadOnlyList<Exchange> conversation, string callId)
	{
		foreach (Exchange e in conversation)
			if (e is Said s)
				foreach (ToolCall c in s.Calls)
					if (c.Id == callId) return c.PublicName;

		return "";
	}

	// The catalogue is flattened to name + description + parameters. The SchemaJson ToolForTheModel
	// carries is json-schema; on the other side a DynamicGenerationSchema is built, which today models
	// an object of strings. That is enough for what the modules declare.
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
				// An unreadable schema leaves the tool WITHOUT parameters instead of bringing the
				// question down: the model will still be able to call it if it does not need them.
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

	private static Said Leer(string crudo)
	{
		JsonElement r = Json.Parse(crudo);

		if (r.TryGetProperty("error", out JsonElement error))
			throw new InvalidOperationException($"el modelo del telefono fallo: {error.GetString()}");

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

		// NEITHER TEXT NOR CALLS IS NOT AN ANSWER, it is a lost step. AgentLoop would take it as "it
		// finished without saying anything" and push null to the page: the person sees a blank screen and
		// there is nothing to diagnose with. It really happened -- an expired ceiling returned {} -- and
		// it cost a test and half an hour. Let it blow up here, with a name.
		if (texto is null && llamadas.Count == 0)
			throw new InvalidOperationException(
				"el modelo del telefono devolvio un paso vacio: ni texto ni tool calls");

		return new Said(texto, llamadas);
	}
}
