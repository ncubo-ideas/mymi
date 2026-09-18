using System.Runtime.InteropServices;

namespace Gnothi.iOS;

// LA SONDA DEL PUENTE. Mide que el C# puede hablarle al modelo del telefono, que es lo que
// habilita ModelFoundation : IModel -y con eso, el modelo local como un proveedor mas en
// Ajustes, el unico sin URL y sin clave-.
//
// "__Internal" y no un .dylib: la libreria de Swift entra ESTATICA al ejecutable via la
// NativeReference del csproj, asi que los simbolos ya estan adentro del proceso.
//
// LA CADENA LA LIBERA QUIEN LA PIDE. Swift devuelve un strdup y el C# llama a gnothi_fm_liberar;
// si eso no se hace, cada pregunta gotea. Por eso el finally.
internal static class BridgeProbe
{
	[DllImport("__Internal", EntryPoint = "gnothi_fm_estado")]
	private static extern int State();

	[DllImport("__Internal", EntryPoint = "gnothi_fm_paso")]
	private static extern IntPtr Step(string conversacion, string tools, string instrucciones);

	[DllImport("__Internal", EntryPoint = "gnothi_fm_liberar")]
	private static extern void Free(IntPtr p);

	// EL MOTIVO, que el 0 se traga. Tres situaciones distintas caen en el mismo estado y no se
	// atienden igual: una se espera -el modelo bajando-, otra se enciende, y la tercera no tiene
	// arreglo. Sin esto, cada 0 cuesta media hora de hipotesis; ya paso el 2026-09-17.
	[DllImport("__Internal", EntryPoint = "gnothi_fm_razon")]
	private static extern IntPtr Reason();

	// NO en el hilo principal: adentro del Swift se espera a que termine la generacion, y desde
	// el hilo de UI eso se traba.
	public static void Run(Action<string> log) => _ = Task.Run(() =>
	{
		try
		{
			log("puente: preguntando por el modelo del telefono…");

			// 0 no disponible · 1 dice estar disponible y NO genera · 2 genera de verdad.
			// El 1 existe porque `availability` miente: en iOS 26.2 dice available y falla.
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
			const string instrucciones = "Sos el asistente de Gnothi. Prosa corta, sin markdown.";

			// PASO 1 · la intencion: se espera que pida la tool, no que conteste.
			string uno = Call(
				"""[{"tipo":"asked","texto":"¿Como viene mi colesterol total?"}]""",
				tools, instrucciones);
			log($"puente: paso 1 -> {Short(uno)}");

			// PASO 2 · el Host ya calculo los hechos y se los devuelve.
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
