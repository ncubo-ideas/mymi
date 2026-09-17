namespace Compras.Wasm;

/// <summary>
/// UNA LINEA QUE LA PANTALLA DICE CON SUS PROPIAS PALABRAS, como clave mas valores y nunca como
/// frase armada: la clave nombra la frase entera y los huecos llevan lo que va adentro.
/// <para>
/// <see cref="Ninguno"/> es la ausencia, y se pregunta por <see cref="HayQueDecirlo"/>: una clave
/// vacia no se busca en ningun catalogo.
/// </para>
/// </summary>
public sealed class Apunte
{
	/// <summary>No hay nada que decir.</summary>
	public static readonly Apunte Ninguno = new Apunte("");

	private Apunte(string clave, params (string Hueco, string Valor)[] huecos)
	{
		Clave = clave;
		Huecos = huecos;
	}

	/// <summary>La clave de la frase, vacia cuando no hay nada que decir.</summary>
	public string Clave { get; }

	/// <summary>Si esta linea tiene algo que decir.</summary>
	public bool HayQueDecirlo => Clave.Length > 0;

	/// <summary>Los valores de los huecos, en el orden en que se dieron.</summary>
	public (string Hueco, string Valor)[] Huecos { get; }

	/// <summary>El apunte de esa clave con los valores de sus huecos.</summary>
	public static Apunte De(string clave, params (string Hueco, string Valor)[] huecos) =>
		new Apunte(clave, huecos);
}
