using System.Globalization;

namespace Concesionario.Wasm.Parts
{
	/// <summary>
	/// Como se escribe un numero grande para que una persona lo lea de un vistazo. Es formato y no
	/// una cuenta: la frase del dominio trae el suyo y no se toca.
	/// </summary>
	public static class HowANumberReads
	{
		/// <summary>El numero con separador de miles.</summary>
		public static string Grouped(int value) => value.ToString("N0", CultureInfo.InvariantCulture);
	}
}
