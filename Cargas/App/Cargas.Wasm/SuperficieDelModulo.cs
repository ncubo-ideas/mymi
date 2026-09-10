using System.Text.Json;
using Cargas.App;
using Puppets.Portador;
using Microsoft.JSInterop;

namespace Cargas.Wasm;

// LO QUE EL MODULO LE OFRECE A GNOTHI (especificacion 11 §7). Cuatro verbos, y ninguno le
// da iniciativa al modulo: los cuatro los ARRANCA Gnothi.
//
//     gnothi.tools()                 que sabe contestar este modulo, de verdad
//     gnothi.call(tool, argsJson)    una consulta, la misma que en escritorio
//     gnothi.deliver(payloadJson)    el cartero entrega eventos y el modulo los asienta
//     gnothi.traspasar(sobreJson)    las anotaciones de la persona, desde la app suelta
//
// LA OTRA DIRECCION NO VA ACA. `pull` -el modulo pidiendole eventos a Gnothi- viaja por el
// puente que Gnothi inyecta (`window.gnothiPuente`, ver PuenteDeGnothi), no por esta
// superficie. Son dos mecanismos distintos: esto se publica desde C# y lo llama el nativo;
// aquello lo inyecta el nativo y lo llama el modulo. Mezclarlos en un solo objeto se leia
// como si el modulo pudiera llamarse a si mismo.
//
// CUALES ASERCIONES VIAJAN en `traspasar` es una decision POR INSTITUCION, y la de esta
// esta tomada en Cargas.App.Traspaso: kilometrajes, tanques, la ficha y las unidades; la
// foto se queda. Aca no se decide nada de eso, solo se abre la puerta.
//
// ESTATICO POR OBLIGACION: DotNet.invokeMethodAsync solo alcanza metodos estaticos, y el
// estado de la app es un singleton del contenedor. Program.cs registra el vivo al arrancar.
public static class SuperficieDelModulo
{
	private static EstadoDeLaApp? estado;

	// La llama Program.cs una vez, con el singleton ya construido.
	public static void Registrar(EstadoDeLaApp vivo)
	{
		ArgumentNullException.ThrowIfNull(vivo);
		estado = vivo;
	}

	// DECLARADO VS REAL: esto no lee el manifiesto, contesta lo que el modulo puede
	// efectivamente atender. La conformance del Host compara las dos listas, y si difieren
	// el modulo miente en su manifiesto.
	[JSInvokable]
	public static string Tools() => JsonSerializer.Serialize(ConsultasDelModulo.Declaradas);

	// UNA CONSULTA. Contra el actor VIVO, no contra un lector nuevo: en el WebView el actor
	// ya esta hidratado y abrir un segundo pagaria el arranque del runtime otra vez por cada
	// pregunta. Las respuestas son las mismas que en escritorio porque los cuerpos son los
	// mismos (ConsultasDelModulo).
	[JSInvokable]
	public static string Call(string tool, string argsJson)
	{
		if (estado is null) return Error("el modulo todavia no arranco");
		if (string.IsNullOrWhiteSpace(tool)) return Error("falta el nombre de la tool");

		try
		{
			object respuesta = estado.Consultar(tool);
			return JsonSerializer.Serialize(respuesta);
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
		if (estado is null) return Error("el modulo todavia no arranco");

		try
		{
			ResultadoDeRefresh resultado = await estado.Entregar(payloadJson);
			return JsonSerializer.Serialize(resultado);
		}
		catch (Exception ex)
		{
			return Error($"{ex.GetType().Name}: {ex.Message}");
		}
	}

	// EL TRASPASO de la instalacion suelta. Aserciones de la persona, no eventos de la
	// estacion: por eso no entra por Deliver (ver EstadoDeLaApp.Traspasar). Gnothi lo trae
	// de la hoja de compartir, porque el sobre no cabe en el enlace del handoff.
	//
	// SE CUENTA TODO, no solo lo aplicado. `Esperando` es lo que se guardo al lado del
	// journal porque su carga aun no llego, y `Rechazadas` con sus motivos es lo unico que
	// puede explicar por que una anotacion no aparece: quien llama es Gnothi y lo va a
	// dejar en su log, no en la pantalla.
	[JSInvokable]
	public static async Task<string> Traspasar(string sobreJson)
	{
		if (estado is null) return Error("el modulo todavia no arranco");

		try
		{
			ResultadoDeTraspaso resultado = await estado.Traspasar(sobreJson);
			return JsonSerializer.Serialize(new
			{
				ok = true,
				resultado.Aplicadas,
				resultado.Omitidas,
				resultado.Esperando,
				resultado.Rechazadas,
				motivos = resultado.Motivos,
			});
		}
		catch (Exception ex)
		{
			return Error($"{ex.GetType().Name}: {ex.Message}");
		}
	}

	private static string Error(string motivo) => JsonSerializer.Serialize(new { ok = false, motivo });
}
