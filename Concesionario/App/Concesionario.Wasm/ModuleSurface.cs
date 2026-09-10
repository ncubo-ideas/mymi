using System.Text.Json;
using Concesionario.App;
using Puppets.Portador;
using Microsoft.JSInterop;

namespace Concesionario.Wasm
{
	// LO QUE EL MODULO LE OFRECE A GNOTHI (especificacion 11 §7). Tres verbos, y ninguno le da
	// iniciativa al modulo: los tres los ARRANCA Gnothi.
	//
	//     gnothi.tools()                 que sabe contestar este modulo, de verdad
	//     gnothi.call(tool, argsJson)    una consulta
	//     gnothi.deliver(payloadJson)    el cartero entrega eventos y el modulo los asienta
	//
	// LA OTRA DIRECCION NO VA ACA. `pull` -el modulo pidiendole eventos a Gnothi- viaja por el
	// puente que Gnothi inyecta (`window.gnothiPuente`, ver GnothiBridge), no por esta
	// superficie. Son dos mecanismos distintos: esto se publica desde C# y lo llama el nativo;
	// aquello lo inyecta el nativo y lo llama el modulo.
	//
	// LO QUE TODAVIA NO ESTA: `traspasar`. La migracion de las aserciones de la persona desde la
	// instalacion suelta existe en Examenes, pero cuales aserciones viajan es una decision de
	// diseno POR INSTITUCION -este modulo tiene las suyas: aplazamientos, perfil de uso
	// declarado- y se dejo para despues a proposito.
	//
	// ESTATICO POR OBLIGACION: DotNet.invokeMethodAsync solo alcanza metodos estaticos, y el
	// estado de la app es un singleton del contenedor. Program.cs registra el vivo al arrancar.
	public static class ModuleSurface
	{
		private static AppState? state;

		// La llama Program.cs una vez, con el singleton ya construido.
		public static void Register(AppState live)
		{
			ArgumentNullException.ThrowIfNull(live);
			state = live;
		}

		// DECLARADO VS REAL: esto no lee el manifiesto, contesta lo que el modulo puede
		// efectivamente atender. La conformance del Host compara las dos listas, y si difieren
		// el modulo miente en su manifiesto.
		[JSInvokable]
		public static string Tools() => JsonSerializer.Serialize(ModuleQueries.Declared);

		// UNA CONSULTA. Contra el actor VIVO, no contra un lector nuevo: en el WebView el actor
		// ya esta hidratado y abrir un segundo pagaria el arranque del runtime otra vez por
		// cada pregunta.
		[JSInvokable]
		public static string Call(string tool, string argsJson)
		{
			if (state is null) return Error("the module has not started yet");
			if (string.IsNullOrWhiteSpace(tool)) return Error("the tool name is missing");

			try
			{
				object answer = state.Ask(tool);
				return JsonSerializer.Serialize(answer);
			}
			catch (Exception ex)
			{
				// Un error propio SI queda dicho: es del modulo y es auditable. Lo que no puede
				// pasar es que una tool desconocida se conteste con algo inventado.
				return Error($"{ex.GetType().Name}: {ex.Message}");
			}
		}

		// EL CARTERO ENTREGA. Gnothi trae un sobre del publicador y el modulo lo asienta por el
		// MISMO camino que su propio refresh: misma ruta, mismo Check, mismo flush. Por eso
		// entregar dos veces el mismo sobre no duplica nada.
		[JSInvokable]
		public static async Task<string> Deliver(string payloadJson)
		{
			if (state is null) return Error("the module has not started yet");

			try
			{
				ResultadoDeRefresh result = await state.DeliverAsync(payloadJson);
				return JsonSerializer.Serialize(result);
			}
			catch (Exception ex)
			{
				return Error($"{ex.GetType().Name}: {ex.Message}");
			}
		}

		private static string Error(string reason) => JsonSerializer.Serialize(new { ok = false, reason });
	}
}
