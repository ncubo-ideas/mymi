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

	// EL MOTIVO, y el nombre del símbolo NO es el de este método: Swift lo exporta como
	// `gnothi_fm_razon` (Intents/Puente.swift). Faltaba esta línea y con ella Gnothi.iOS no
	// compilaba -- `CS0103: el nombre 'Reason' no existe` --, porque la llamada entró con
	// `77f529b` y su declaración no. Devuelve un strdup, así que se libera con Free como las demás.
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
				// El motivo solo se pide cuando hace falta: en el camino bueno no aporta nada.
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
			// ESTA LINEA NO SE TOCA SIN SABER QUE SE PIERDE: es la misma con la que se midio
			// 4 de 4 en iOS 26.6.2 y 0 de 4 en 27, asi que cambiarla rompe la comparacion. El
			// experimento de pedirle explicitamente que llame la tool se corrio aparte y esta
			// en Lab.Siri/HALLAZGOS.md seccion 12: no la hizo llamar, la hizo NARRAR la llamada.
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
