using System.Globalization;

namespace Municipalidad.Wasm
{
	/// <summary>
	/// Las mismas tres formas con que el dominio dice una cifra adentro de sus frases. Tienen que
	/// coincidir: un monto escrito de dos maneras en una misma pantalla -- una en la tabla y otra en la
	/// frase de al lado -- es la frase rival en pequeno.
	/// <para>
	/// Ninguna depende de la cultura de la maquina, y ninguna inventa un simbolo de moneda: este
	/// producto no tiene un tipo de moneda y una caratula no es quien para ponerle uno.
	/// </para>
	/// </summary>
	public static class Formato
	{
		/// <summary>Un monto, con los mismos digitos que trae su columna y sin simbolo de moneda.</summary>
		public static string Monto(decimal monto) =>
			ConComaDecimal(monto.ToString("0.##", CultureInfo.InvariantCulture));

		/// <summary>Una proporcion dicha como porcentaje, con una decimal a lo sumo: 0,8 es "80 %".</summary>
		public static string Porcentaje(decimal proporcion) =>
			ConComaDecimal(
				Math.Round(proporcion * 100m, 1).ToString("0.##", CultureInfo.InvariantCulture)) + " %";

		/// <summary>Cuantos dias, dicho en palabras para que "1 días" no aparezca nunca.</summary>
		public static string Dias(int dias) => dias == 1 ? "1 día" : dias + " días";

		private static string ConComaDecimal(string dicho) => dicho.Replace('.', ',');
	}
}
