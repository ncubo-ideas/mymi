namespace Cargas.Wasm;

/// <summary>
/// COMO SE CUENTA UN FALLO, y son DOS cosas y no una. Estas pantallas corren en el browser de otra
/// persona: no hay debugger del otro lado, y el mensaje pelado del motor no dice en que paso se
/// rompio ni que lo causo. Por eso el detalle lleva el paso, el tipo y la cadena ENTERA de
/// InnerException, que es donde suele estar la causa de verdad.
/// <para>
/// PERO ESE DETALLE NO ES PARA LA PERSONA. El nombre de un tipo delante de la frase humana la
/// vuelve ilegible, y es la frase la que le dice que hacer. Asi que el fallo tiene una cara para
/// ella y otra para quien depura, y la pantalla decide como muestra cada una.
/// </para>
/// <para>
/// NO GUARDA PROSA, GUARDA CLAVES. Este objeto se arma dentro de un catch, donde no hay catalogo a
/// mano, y redactar ahi ataria la frase al idioma del codigo que atrapo el fallo. Quien redacta es
/// <see cref="Problema"/>, que tiene los textos de la persona. El detalle tecnico es lo unico que
/// viaja escrito, justamente porque no es para ella.
/// </para>
/// <para>
/// Vive suelto porque lo usan las pantallas y tambien la cascara, y son dos jerarquias distintas:
/// las pantallas heredan de ComponentBase y el layout de LayoutComponentBase.
/// </para>
/// </summary>
public sealed class Fallo
{
	public static readonly Fallo Ninguno = new Fallo("", "", [], "");

	/// <summary>
	/// LA CLAVE DEL PASO que no se pudo dar. Siempre esta, y es lo que se dice cuando no hay mas
	/// que decir. Es un codigo estable y no una frase: tambien es lo que se lee en el detalle.
	/// </summary>
	public string ClaveDelPaso { get; }

	/// <summary>La clave del motivo cuando esta app sabe cual fue. Vacia cuando no lo sabe.</summary>
	public string ClaveDelMotivo { get; }

	/// <summary>Los valores de esa frase, que cruzan sin traducirse.</summary>
	public (string Hueco, string Valor)[] Huecos { get; }

	/// <summary>Para quien depura: el paso, el tipo y la cadena entera de causas.</summary>
	public string Tecnico { get; }

	public bool Hay => ClaveDelPaso.Length > 0;

	private Fallo(string claveDelPaso, string claveDelMotivo,
		(string Hueco, string Valor)[] huecos, string tecnico)
	{
		ClaveDelPaso = claveDelPaso;
		ClaveDelMotivo = claveDelMotivo;
		Huecos = huecos;
		Tecnico = tecnico;
	}

	/// <summary>
	/// EL FALLO DE UN PASO. Un <see cref="RechazoDeLaApp"/> es algo que esta app entiende y sabe
	/// decir; cualquier otro tipo -- JSException, HttpRequestException, algo del motor o la
	/// negativa del dominio -- trae un mensaje tecnico que no se le muestra a la persona.
	/// </summary>
	public static Fallo Detalle(string claveDelPaso, Exception ex)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(claveDelPaso);
		ArgumentNullException.ThrowIfNull(ex);

		var tecnico = new System.Text.StringBuilder(
			$"Paso: {claveDelPaso}\n{ex.GetType().FullName}: {ex.Message}");
		for (Exception? interna = ex.InnerException; interna is not null; interna = interna.InnerException)
			tecnico.Append($"\n  causa: {interna.GetType().FullName}: {interna.Message}");

		var rechazo = ex as RechazoDeLaApp;
		return new Fallo(claveDelPaso, rechazo?.Clave ?? "", rechazo?.Huecos ?? [], tecnico.ToString());
	}

	/// <summary>
	/// LA CARA PARA LA PERSONA, dicha por el catalogo de su idioma.
	/// <para>
	/// Un motivo que este catalogo no tiene escrito CAE AL PASO. Pasa con lo que no compuso esta
	/// app -- el nombre de un error del browser, la negativa del dominio -- y la caida es
	/// deliberada: "No se pudo guardar la ficha" dice menos que el motivo y sigue siendo cierto,
	/// mientras que un motivo crudo seria tecnico, o estaria en el idioma equivocado, o las dos
	/// cosas. Lo que no se dice aca no se pierde: esta entero en <see cref="Tecnico"/>.
	/// </para>
	/// </summary>
	public string Que(Textos texto)
	{
		ArgumentNullException.ThrowIfNull(texto);
		if (!Hay) return "";

		string delMotivo = "fallo.motivo." + ClaveDelMotivo;
		return ClaveDelMotivo.Length > 0 && texto.Conoce(delMotivo)
			? texto.Con(delMotivo, Huecos)
			: texto["fallo.paso." + ClaveDelPaso];
	}
}
