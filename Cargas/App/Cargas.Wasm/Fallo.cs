namespace Cargas.Wasm;

// COMO SE CUENTA UN FALLO, y son DOS cosas y no una. Estas pantallas corren en el browser de
// otra persona: no hay debugger del otro lado, y el mensaje pelado del motor no dice en que
// paso se rompio ni que lo causo. Por eso el detalle lleva el paso, el tipo, y la cadena
// ENTERA de InnerException, que es donde suele estar la causa de verdad.
//
// PERO ESE DETALLE NO ES PARA SONIA. Antes salia todo junto en un <pre>: «Paso: anotar el
// kilometraje ⏎ System.InvalidOperationException: La app no anotó ese kilometraje…». El nombre
// del tipo delante de la frase humana la vuelve ilegible, y es la frase la que le dice que
// hacer. Asi que el fallo tiene una cara para ella (Que) y otra para quien depura (Tecnico),
// y la pantalla decide como mostrar cada una: ver Problema.razor.
//
// QUE ES HUMANO Y QUE NO. Los InvalidOperationException los tira ESTE codigo, con frases en
// español pensadas para ella: esas van tal cual. Cualquier otro tipo -JSException,
// HttpRequestException, algo del motor- trae un mensaje tecnico en ingles, asi que la cara
// humana dice solo «No se pudo <paso>» y el mensaje va al detalle.
//
// Vive suelto porque lo usan las pantallas y tambien la cascara, y son dos jerarquias
// distintas: las pantallas heredan de ComponentBase y el layout de LayoutComponentBase.
public sealed class Fallo
{
	public static readonly Fallo Ninguno = new Fallo("", "", "");

	public string Paso { get; }
	public string Que { get; }
	public string Tecnico { get; }

	public bool Hay => Que.Length > 0;

	private Fallo(string paso, string que, string tecnico)
	{
		Paso = paso;
		Que = que;
		Tecnico = tecnico;
	}

	public static Fallo Detalle(string paso, Exception ex)
	{
		ArgumentNullException.ThrowIfNull(ex);

		string que = ex is InvalidOperationException && ex.Message.Length > 0
			? ex.Message
			: $"No se pudo {paso}.";

		var tecnico = new System.Text.StringBuilder(
			$"Paso: {paso}\n{ex.GetType().FullName}: {ex.Message}");
		for (Exception? interna = ex.InnerException; interna is not null; interna = interna.InnerException)
			tecnico.Append($"\n  causa: {interna.GetType().FullName}: {interna.Message}");

		return new Fallo(paso, que, tecnico.ToString());
	}
}
