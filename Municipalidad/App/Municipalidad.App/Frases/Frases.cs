using System;
using System.Collections.Generic;
using System.Globalization;

namespace Municipalidad.App.Frases
{
	/// <summary>
	/// EL RENDERIZADOR, uno por idioma. El dominio decide QUE frase y CON QUE valores; esta capa decide
	/// como se dice. La redaccion vive aca y no en <c>Dominio/</c> a proposito: una frase es texto de
	/// presentacion, y el dominio que la escribiera tendria que elegir un idioma.
	/// <para>
	/// La forma es CLAVE MAS VALORES. La clave la pone el dominio y es estable -- es la misma etiqueta
	/// que ya cruza la puerta --; los valores viajan tipados y se meten en los huecos SIN traducirse,
	/// porque son del registro de la institucion.
	/// </para>
	/// <para>
	/// El formato de una cifra viaja con el renderizador y no con el dominio: una coma decimal es una
	/// decision del idioma, y dejarla adentro obligaria al dominio a tener uno.
	/// </para>
	/// </summary>
	public sealed class Frases
	{
		/// <summary>El idioma del registro de la municipalidad, y al que cae toda etiqueta desconocida.</summary>
		public const string IdiomaDelRegistro = "es";

		private readonly IReadOnlyDictionary<string, string> catalogo;
		private readonly bool esEspanol;

		private Frases(IReadOnlyDictionary<string, string> catalogo, bool esEspanol)
		{
			this.catalogo = catalogo;
			this.esEspanol = esEspanol;
		}

		private static readonly Frases enEspanol = new Frases(CatalogoEnEspanol.ElCatalogo(), true);
		private static readonly Frases enIngles = new Frases(CatalogoEnIngles.ElCatalogo(), false);

		/// <summary>
		/// El renderizador de esa etiqueta. TOTAL: una etiqueta que esta app no habla cae al idioma del
		/// registro, porque una consulta no refuta una pregunta legitima por el nombre de un idioma.
		/// </summary>
		public static Frases De(string idioma)
		{
			return SubetiquetaPrimaria(idioma) == "en" ? enIngles : enEspanol;
		}

		/// <summary>Si esta app habla el idioma de esa etiqueta, para quien quiera distinguir la caida.</summary>
		public static bool EsUnaEtiquetaConocida(string idioma)
		{
			string primaria = SubetiquetaPrimaria(idioma);
			return primaria == "es" || primaria == "en";
		}

		/// <summary>
		/// La frase de esa clave con sus huecos llenos. Una clave que falta se dice con su propio nombre
		/// entre llaves y no revienta: una respuesta a la que le falta una palabra sigue sirviendo, y el
		/// hueco se ve.
		/// </summary>
		public string Diga(string clave, params (string Hueco, string Valor)[] huecos)
		{
			if (!catalogo.TryGetValue(clave, out string dicho)) return "{" + clave + "}";

			foreach ((string hueco, string valor) in huecos)
				dicho = dicho.Replace("{" + hueco + "}", valor);

			return dicho;
		}

		/// <summary>Un monto, con los mismos digitos que lleva su columna tipada y SIN simbolo de moneda.</summary>
		public string Monto(decimal monto)
		{
			return ConElSeparador(monto.ToString("0.##", CultureInfo.InvariantCulture));
		}

		/// <summary>Una proporcion dicha como porcentaje: 0,8 es "80 %" en espanol y "80%" en ingles.</summary>
		public string Porcentaje(decimal proporcion)
		{
			return ConElSeparador(Math.Round(proporcion * 100m, 1).ToString("0.##", CultureInfo.InvariantCulture))
				+ (esEspanol ? " %" : "%");
		}

		/// <summary>Cuantos dias, dicho entero para que "1 dias" no aparezca nunca en ningun idioma.</summary>
		public string Dias(int dias)
		{
			if (esEspanol) return dias == 1 ? "1 día" : dias + " días";
			return dias == 1 ? "1 day" : dias + " days";
		}

		/// <summary>
		/// Un dia de calendario en anio-mes-dia, sin hora. NO cambia con el idioma a proposito: el orden
		/// anio-mes-dia no se puede leer al reves, y darle una forma por idioma invitaria a moverlo.
		/// </summary>
		public string Dia(DateTime dia)
		{
			return dia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}

		private string ConElSeparador(string dicho)
		{
			return esEspanol ? dicho.Replace('.', ',') : dicho;
		}

		private static string SubetiquetaPrimaria(string etiqueta)
		{
			if (string.IsNullOrWhiteSpace(etiqueta)) return "";
			string limpia = etiqueta.Trim().ToLowerInvariant();
			int guion = limpia.IndexOf('-');
			return guion < 0 ? limpia : limpia.Substring(0, guion);
		}
	}
}
