namespace Cargas.Wasm;

/// <summary>
/// LO QUE ESTA APP SABE QUE SALIO MAL, dicho con una CLAVE y no con una frase.
/// <para>
/// Un rechazo que viajara ya redactado llegaria en el idioma de quien lo escribio, y quien lo
/// escribe -- la puerta, el puente, el interop del browser -- no sabe en cual mira la persona. Asi
/// que cruza la clave, que es identidad, y la frase la pone el catalogo al final.
/// </para>
/// <para>
/// EL TIPO ES LA DISTINCION, y por eso existe en vez de reusar uno de la plataforma: separa un
/// rechazo que esta app entiende y sabe decir de una falla del browser o del motor, que llega en
/// ingles tecnico y no es para la persona.
/// </para>
/// </summary>
public sealed class RechazoDeLaApp : Exception
{
	/// <summary>La clave con que el catalogo dice este rechazo. No se traduce: es identidad.</summary>
	public string Clave { get; }

	/// <summary>
	/// Los valores que van en los huecos de esa frase. Cruzan SIN traducirse: el nombre de un
	/// archivo y el de una carpeta los puso la persona o su sistema, no esta app.
	/// </summary>
	public (string Hueco, string Valor)[] Huecos { get; }

	/// <summary>
	/// El mensaje es la clave a proposito: es lo que conviene leer en el detalle tecnico.
	/// <para>
	/// NO EXIGE NADA, y es deliberado en este tipo y no en otro: quien lo construye ya esta
	/// contando una falla, y un rechazo que reventara al nombrarse taparia la falla que venia a
	/// reportar. Una clave vacia es un motivo que no se supo nombrar, que <see cref="Fallo"/> ya
	/// sabe decir cayendo al paso.
	/// </para>
	/// </summary>
	public RechazoDeLaApp(string clave, params (string Hueco, string Valor)[] huecos)
		: base(clave)
	{
		Clave = string.IsNullOrWhiteSpace(clave) ? "" : clave.Trim();
		Huecos = huecos is null ? [] : huecos;
	}
}
