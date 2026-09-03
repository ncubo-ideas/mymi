namespace Cargas.Wasm;

// COMO SE CUENTA UN FALLO. Estas pantallas corren en el browser de otra persona: no hay
// debugger del otro lado, y el mensaje pelado del motor no dice en que paso se rompio ni
// que lo causo. Por eso el detalle lleva el paso, el tipo, y la cadena ENTERA de
// InnerException — que es donde suele estar la causa de verdad.
//
// Vive suelto porque lo usan las pantallas y tambien la cascara, y son dos jerarquias
// distintas: las pantallas heredan de ComponentBase y el layout de LayoutComponentBase.
public static class Fallo
{
	public static string Detalle(string paso, Exception ex)
	{
		var detalle = new System.Text.StringBuilder(
			$"Paso: {paso}\n{ex.GetType().FullName}: {ex.Message}");

		for (Exception? interna = ex.InnerException; interna is not null; interna = interna.InnerException)
			detalle.Append($"\n  causa: {interna.GetType().FullName}: {interna.Message}");

		return detalle.ToString();
	}
}
