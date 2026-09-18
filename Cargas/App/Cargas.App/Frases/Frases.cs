using System;
using System.Collections.Generic;
using System.Globalization;

namespace Cargas.App.Frases
{
	/// <summary>
	/// EL RENDERIZADOR, uno por idioma. El dominio decide QUE frase y CON QUE valores; esta capa
	/// decide como se dice. La redaccion vive aca y no en <c>Dominio/</c> a proposito: una frase es
	/// texto de presentacion, y el dominio que la escribiera tendria que elegir un idioma.
	/// <para>
	/// La forma es CLAVE MAS VALORES. La clave la pone el dominio y es estable -- es la misma
	/// etiqueta que ya cruza la puerta --; los valores viajan tipados y se meten en los huecos SIN
	/// traducirse, porque son del registro de la estacion: la placa, el numero de transaccion, el
	/// nombre de un surtidor, el premio de un sorteo.
	/// </para>
	/// <para>
	/// El formato de una cifra viaja con el renderizador y no con el dominio: la coma decimal es
	/// una decision del idioma, y dejarla adentro obligaria al dominio a tener uno.
	/// </para>
	/// <para>
	/// EL SIMBOLO DE UNA UNIDAD NO SE TRADUCE: km, L, km/L, mpg y L/100km se escriben igual en los
	/// dos idiomas, y ademas la presentacion elegida vive en la bitacora de la persona.
	/// </para>
	/// </summary>
	public sealed class Frases
	{
		/// <summary>
		/// EL IDIOMA DEL REGISTRO de la estacion, y al que cae toda etiqueta desconocida. Vive aca
		/// y en ningun otro lado: es la subetiqueta con la que se elige catalogo, asi que
		/// escribirla dos veces seria dejar que las dos copias dejen de decir lo mismo sin que
		/// nada avise.
		/// </summary>
		public const string IdiomaDelRegistro = "es";

		/// <summary>El otro idioma que esta app habla. El alcance son dos y no hay un tercero.</summary>
		public const string ElOtroIdioma = "en";

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
		/// El renderizador de esa etiqueta. TOTAL: una etiqueta que esta app no habla cae al idioma
		/// del registro, porque una consulta no refuta una pregunta legitima por el nombre de un
		/// idioma.
		/// </summary>
		public static Frases De(string idioma)
		{
			return SubetiquetaPrimaria(idioma) == ElOtroIdioma ? enIngles : enEspanol;
		}

		/// <summary>Si esta app habla el idioma de esa etiqueta, para quien quiera distinguir la caida.</summary>
		public static bool EsUnaEtiquetaConocida(string idioma)
		{
			string primaria = SubetiquetaPrimaria(idioma);
			return primaria == IdiomaDelRegistro || primaria == ElOtroIdioma;
		}

		/// <summary>
		/// La frase de esa clave con sus huecos llenos. Una clave que falta se dice con su propio
		/// nombre entre llaves y no revienta: una respuesta a la que le falta una palabra sigue
		/// sirviendo, y el hueco se ve.
		/// </summary>
		public string Diga(string clave, params (string Hueco, string Valor)[] huecos)
		{
			if (!catalogo.TryGetValue(clave, out string dicho)) return "{" + clave + "}";

			foreach ((string hueco, string valor) in huecos)
				dicho = dicho.Replace("{" + hueco + "}", valor);

			return dicho;
		}

		/// <summary>Si el catalogo de este idioma tiene esa clave, para quien quiera distinguir el respaldo.</summary>
		public bool Tiene(string clave) => catalogo.ContainsKey(clave);

		/// <summary>
		/// LAS CLAVES QUE ESTE IDIOMA ESCRIBE. Existe para que una comprobacion pueda comparar los
		/// dos catalogos sin abrirlos: una clave que esta en uno y no en el otro es una frase que
		/// llega a medias -- con un {hueco} crudo a la vista -- y ninguna otra comprobacion la ve.
		/// </summary>
		public IEnumerable<string> Claves => catalogo.Keys;

		/// <summary>
		/// UNA CIFRA CON UN DECIMAL: un rendimiento, unos litros, un ritmo. Es la precision que ya
		/// tenia decidida el dominio, y con separador de miles porque en una frase "sobraron 9392"
		/// hay que contarlo con el dedo.
		/// </summary>
		public string Cifra(decimal medida) => medida.ToString("#,0.#", ElSeparador);

		/// <summary>Una cifra entera: kilometros, cuentas.</summary>
		public string Entera(decimal medida) => medida.ToString("#,0", ElSeparador);

		/// <summary>Dinero, hasta dos decimales y sin ceros de relleno, CON el simbolo de su moneda.</summary>
		public string Monto(decimal monto, string codigoDeMoneda) =>
			Simbolo(codigoDeMoneda) + monto.ToString("#,0.##", ElSeparador);

		/// <summary>
		/// EL SIMBOLO DE UNA MONEDA. NO se traduce: es con lo que la estacion cobra. Una moneda que
		/// esta app no conoce se dice con su codigo y un espacio -- mostrar "USD 48" es feo pero
		/// cierto, e inventar un simbolo no lo es.
		/// </summary>
		public static string Simbolo(string codigoDeMoneda) => codigoDeMoneda switch
		{
			"CRC" => "₡",
			"PAB" => "B/.",
			"USD" => "$",
			_ => codigoDeMoneda + " ",
		};

		/// <summary>
		/// Un porcentaje entero, SIN SIGNO: la direccion la dice la clausula del cambio, no el signo,
		/// porque en L/100km una caida de rendimiento es un numero que sube. El simbolo va pegado en
		/// los dos idiomas, que es como lo escribia el dominio.
		/// </summary>
		public string Porcentaje(int porcentaje) =>
			porcentaje.ToString("0", CultureInfo.InvariantCulture) + "%";

		/// <summary>Cuantos dias, dicho entero para que "1 dias" no aparezca nunca en ningun idioma.</summary>
		public string Dias(int dias)
		{
			if (esEspanol) return dias == 1 ? "1 día" : dias + " días";
			return dias == 1 ? "1 day" : dias + " days";
		}

		/// <summary>Cuantos boletos, por la misma razon que los dias.</summary>
		public string Boletos(int boletos)
		{
			if (esEspanol) return boletos == 1 ? "1 boleto" : boletos + " boletos";
			return boletos == 1 ? "1 ticket" : boletos + " tickets";
		}

		/// <summary>Cuantas cargas, por la misma razon que los dias.</summary>
		public string Cargas(int cargas)
		{
			if (esEspanol) return cargas == 1 ? "1 carga" : cargas + " cargas";
			return cargas == 1 ? "1 fill-up" : cargas + " fill-ups";
		}

		/// <summary>Cuantos tramos calculables, por la misma razon que los dias.</summary>
		public string TramosCalculables(int tramos)
		{
			if (esEspanol) return tramos == 1 ? "1 tramo calculable" : tramos + " tramos calculables";
			return tramos == 1 ? "1 computable leg" : tramos + " computable legs";
		}

		/// <summary>Cuantos kilometrajes anotados, por la misma razon que los dias.</summary>
		public string Kilometrajes(int lecturas)
		{
			if (esEspanol) return lecturas == 1 ? "1 kilometraje" : lecturas + " kilometrajes";
			return lecturas == 1 ? "1 odometer reading" : lecturas + " odometer readings";
		}

		/// <summary>
		/// Un dia de calendario en anio-mes-dia, sin hora. NO cambia con el idioma a proposito: el
		/// orden anio-mes-dia no se puede leer al reves, y darle una forma por idioma invitaria a
		/// moverlo.
		/// </summary>
		public string Dia(DateTime dia) => dia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

		/// <summary>
		/// UNA MAGNITUD CON SU UNIDAD: "42 380 km", "9,4 km/L". El simbolo llega tal cual y no se
		/// traduce; lo unico del idioma es como se escribe la cifra.
		/// </summary>
		public string ConUnidad(decimal medida, string simbolo) =>
			simbolo.Length == 0 ? Cifra(medida) : Cifra(medida) + " " + simbolo;

		/// <summary>La misma magnitud dicha entera, para los kilometros.</summary>
		public string ConUnidadEntera(decimal medida, string simbolo) =>
			simbolo.Length == 0 ? Entera(medida) : Entera(medida) + " " + simbolo;

		private NumberFormatInfo ElSeparador => esEspanol ? Colon : Coma;

		private static readonly NumberFormatInfo Colon = new NumberFormatInfo
		{
			NumberGroupSeparator = ".",
			NumberDecimalSeparator = ",",
			NumberGroupSizes = new[] { 3 },
		};

		private static readonly NumberFormatInfo Coma = new NumberFormatInfo
		{
			NumberGroupSeparator = ",",
			NumberDecimalSeparator = ".",
			NumberGroupSizes = new[] { 3 },
		};

		private static string SubetiquetaPrimaria(string etiqueta)
		{
			if (string.IsNullOrWhiteSpace(etiqueta)) return "";
			string limpia = etiqueta.Trim().ToLowerInvariant();
			int guion = limpia.IndexOf('-');
			return guion < 0 ? limpia : limpia.Substring(0, guion);
		}
	}
}
