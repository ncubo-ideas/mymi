using System.Globalization;

namespace Compras.Dominio
{
	/// <summary>
	/// Cómo se escribe un monto en colones. Un solo lugar, para que la consola, la
	/// carátula y las líneas que escribe el dominio digan la misma cifra igual.
	/// </summary>
	// EL PUNTO SEPARA LOS MILES, no la coma: es como se escribe un monto en Costa Rica, y
	// es lo que ya decia la pagina de la institucion. Antes cada lugar formateaba con
	// InvariantCulture y salia "₡172,630" -que en medio mundo se lee como un decimal-,
	// mientras el landing mostraba "₡32.910". La misma app decia la cifra de dos formas.
	//
	// EL FORMATO SE ARMA A MANO en vez de pedir la cultura "es-CR", y eso es a proposito:
	// la caratula corre en wasm, y si el publish recorta los datos de globalizacion la
	// cultura no existe y el monto se escribiria distinto que en la consola. Un separador
	// explicito no depende de que ICU este ahi.
	internal static class Plata
	{
		internal const string Simbolo = "₡";

		private static readonly NumberFormatInfo Colon = new NumberFormatInfo
		{
			NumberGroupSeparator = ".",
			NumberDecimalSeparator = ",",
			NumberGroupSizes = new[] { 3 },
		};

		/// <summary>Un monto redondeado al colón, con su símbolo: ₡172.630.</summary>
		internal static string Colones(decimal monto) => Simbolo + monto.ToString("N0", Colon);
	}
}
