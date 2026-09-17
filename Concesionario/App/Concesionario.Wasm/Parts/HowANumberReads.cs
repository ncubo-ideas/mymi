using Concesionario.App.Wording;

namespace Concesionario.Wasm.Parts
{
	/// <summary>
	/// Como se escribe un numero grande para que una persona lo lea de un vistazo. Es formato y no una
	/// cuenta: la frase del dominio trae el suyo y no se toca.
	/// <para>
	/// El separador de miles viaja con el idioma y por eso lo pone el renderizador: escribirlo aqui
	/// seria una segunda implementacion de la coma decimal, que es la frase rival en pequeno.
	/// </para>
	/// </summary>
	public static class HowANumberReads
	{
		/// <summary>El numero con el separador de miles del idioma.</summary>
		public static string Grouped(Phrases phrases, int value) => phrases.Grouped(value);
	}
}
