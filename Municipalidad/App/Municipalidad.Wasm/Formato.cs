using System.Globalization;

namespace Municipalidad.Wasm
{
	/// <summary>
	/// Las mismas tres formas con que el dominio dice una cifra adentro de sus frases. Tienen que
	/// coincidir: un monto escrito de dos maneras en una misma pantalla -- una en la tabla y otra en la
	/// frase de al lado -- es la frase rival en pequeno.
	/// <para>
	/// Ninguna depende de la cultura de la maquina, y ninguna inventa un simbolo de moneda: este
	/// producto no tiene un tipo de moneda y una caratula no es quien para ponerle uno. El idioma entra
	/// por parametro, igual que en el dominio, y por la misma razon: quien elige es la persona.
	/// </para>
	/// </summary>
	public static class Formato
	{
		/// <summary>Un monto, con los mismos digitos que trae su columna y sin simbolo de moneda.</summary>
		public static string Monto(decimal monto, string idioma) =>
			ConElSeparador(monto.ToString("0.##", CultureInfo.InvariantCulture), idioma);

		/// <summary>Una proporcion dicha como porcentaje: 0,8 es "80 %" en espanol y "80%" en ingles.</summary>
		public static string Porcentaje(decimal proporcion, string idioma) =>
			ConElSeparador(
				Math.Round(proporcion * 100m, 1).ToString("0.##", CultureInfo.InvariantCulture), idioma)
			+ (EsEspanol(idioma) ? " %" : "%");

		/// <summary>Cuantos dias, dicho en palabras para que "1 dias" no aparezca nunca.</summary>
		public static string Dias(int dias, string idioma) => EsEspanol(idioma)
			? (dias == 1 ? "1 día" : dias + " días")
			: (dias == 1 ? "1 day" : dias + " days");

		private static string ConElSeparador(string dicho, string idioma) =>
			EsEspanol(idioma) ? dicho.Replace('.', ',') : dicho;

		private static bool EsEspanol(string idioma) =>
			!string.IsNullOrWhiteSpace(idioma) && !idioma.Trim().ToLowerInvariant().StartsWith("en");
	}
}
