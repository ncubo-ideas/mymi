using System.Runtime.InteropServices;

namespace Gnothi.iOS;

// THE BRIDGE PROBE. It measures that C# can talk to the phone's model, which is what enables
// ModelFoundation : IModel -- and with it, the local model as one more provider in Settings, the only
// one with no URL and no key.
//
// "__Internal" and not a .dylib: the Swift library goes STATICALLY into the executable through the
// csproj's NativeReference, so the symbols are already inside the process.
//
// THE STRING IS FREED BY WHOEVER ASKED FOR IT. Swift returns a strdup and C# calls
// gnothi_fm_liberar; if that is not done, every question leaks. Hence the finally.
internal static class BridgeProbe
{
	[DllImport("__Internal", EntryPoint = "gnothi_fm_estado")]
	private static extern int State();

	// THE REASON, and the symbol name is NOT this method name: Swift exports it as
	// `gnothi_fm_razon` (Intents/Puente.swift). This line was missing and with it Gnothi.iOS did
	// not compile -- `CS0103: the name 'Reason' does not exist` -- because the call came in with
	// `77f529b` and its declaration did not. It returns a strdup, so it is freed with Free like the rest.
	[DllImport("__Internal", EntryPoint = "gnothi_fm_razon")]
	private static extern IntPtr Reason();

	[DllImport("__Internal", EntryPoint = "gnothi_fm_paso")]
	private static extern IntPtr Step(string conversacion, string tools, string instrucciones);

	[DllImport("__Internal", EntryPoint = "gnothi_fm_liberar")]
	private static extern void Free(IntPtr p);

	// NOT on the main thread: inside the Swift the generation is waited on, and from the UI thread
	// that deadlocks.
	public static void Run(Action<string> log) => _ = Task.Run(() =>
	{
		try
		{
			log("puente: preguntando por el modelo del telefono…");

			// 0 unavailable · 1 claims to be available and does NOT generate · 2 really generates.
			// The 1 exists because `availability` lies: on iOS 26.2 it says available and fails.
			int estado = State();
			log($"puente: estado = {estado}  ({(estado == 2 ? "genera" : estado == 1 ? "dice available pero NO genera" : "no disponible")})");
			if (estado != 2)
			{
				// The reason is only asked for when needed: on the good path it adds nothing.
				IntPtr p = Reason();
				try { log($"puente: motivo = {Marshal.PtrToStringUTF8(p)}"); }
				finally { if (p != IntPtr.Zero) Free(p); }
				return;
			}

			const string tools =
				"""
				[{"name":"examenes_evaluar_tendencia",
				  "description":"Dice si un analito de la persona viene subiendo, bajando o estable.",
				  "params":[{"name":"analito","description":"Cual analito. Valores validos: AntigenoProstatico, ColesterolTotal, ColesterolLdl, Hemoglobina."}]}]
				""";
			// THIS LINE IS NOT TOUCHED WITHOUT KNOWING WHAT IS LOST: it is the same one 4 out of 4
			// on iOS 26.6.2 and 0 out of 4 on 27 were measured with, so changing it breaks the
			// comparison. The experiment of explicitly asking it to call the tool was run separately
			// and is in Lab.Siri/HALLAZGOS.md section 12: it did not make it call, it made it
			// NARRATE the call.
			const string instrucciones = "Sos el asistente de Gnothi. Prosa corta, sin markdown.";

			// STEP 1 · the intent: it is expected to ask for the tool, not to answer.
			string uno = Call(
				"""[{"tipo":"asked","texto":"¿Como viene mi colesterol total?"}]""",
				tools, instrucciones);
			log($"puente: paso 1 -> {Short(uno)}");

			// STEP 2 · the Host has already computed the facts and hands them back.
			const string hechos =
				"""{\"hayDatos\":true,\"analito\":\"Colesterol total\",\"direccion\":\"subiendo\",\"ultimo\":{\"valor\":228},\"unidad\":\"mg/dL\",\"umbral\":200,\"lectura\":\"tu colesterol total viene subiendo: de 182 a 228 mg/dL en 29 meses, y paso el umbral de 200\"}""";

			string dos = Call(
				"""
				[{"tipo":"asked","texto":"¿Como viene mi colesterol total?"},
				 {"tipo":"said","texto":"","calls":[{"id":"c1","name":"examenes_evaluar_tendencia","args":{"analito":"ColesterolTotal"}}]},
				 {"tipo":"returned","callId":"c1","toolName":"examenes_evaluar_tendencia","json":"HECHOS"}]
				""".Replace("HECHOS", hechos),
				tools, instrucciones);
			log($"puente: paso 2 -> {Short(dos)}");
		}
		catch (Exception e)
		{
			log($"puente: FALLO {e.GetType().Name}: {e.Message}");
		}
	});

	private static string Call(string conversacion, string tools, string instrucciones)
	{
		IntPtr p = IntPtr.Zero;
		try
		{
			p = Step(conversacion, tools, instrucciones);
			return Marshal.PtrToStringUTF8(p) ?? "(nulo)";
		}
		finally
		{
			if (p != IntPtr.Zero) Free(p);
		}
	}

	private static string Short(string s) => s.Length <= 240 ? s : s[..240] + "…";
}
