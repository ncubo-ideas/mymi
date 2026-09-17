using System;
using System.Collections.Generic;
using System.Globalization;

namespace Compras.App.Frases
{
	/// <summary>
	/// EL RENDERIZADOR, uno por idioma. El dominio decide QUE frase y CON QUE valores; esta capa decide
	/// como se dice. La redaccion vive aca y no en <c>Dominio/</c> a proposito: una frase es texto de
	/// presentacion, y el dominio que la escribiera tendria que elegir un idioma.
	/// <para>
	/// La forma es CLAVE MAS VALORES. La clave la pone el dominio y es estable -- es la misma etiqueta
	/// que ya cruza la puerta --; los valores viajan tipados y se meten en los huecos SIN traducirse,
	/// porque son del registro del supermercado: la descripcion de un producto, el nombre de una
	/// sucursal, el numero de un tiquete.
	/// </para>
	/// <para>
	/// El formato de una cifra viaja con el renderizador y no con el dominio: el punto de miles es una
	/// decision del idioma, y dejarla adentro obligaria al dominio a tener uno.
	/// </para>
	/// </summary>
	public sealed class Frases
	{
		/// <summary>
		/// EL IDIOMA DEL REGISTRO del supermercado, y al que cae toda etiqueta desconocida. Vive aca y
		/// en ningun otro lado: es la subetiqueta con la que se elige catalogo, asi que escribirla dos
		/// veces seria dejar que las dos copias dejen de decir lo mismo sin que nada avise.
		/// </summary>
		public const string IdiomaDelRegistro = "es";

		/// <summary>El otro idioma que esta app habla. El alcance son dos y no hay un tercero.</summary>
		public const string ElOtroIdioma = "en";

		/// <summary>El simbolo del colon. NO se traduce: es la moneda con la que el super cobra.</summary>
		public const string SimboloDeLaMoneda = "₡";

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
			return SubetiquetaPrimaria(idioma) == ElOtroIdioma ? enIngles : enEspanol;
		}

		/// <summary>Si esta app habla el idioma de esa etiqueta, para quien quiera distinguir la caida.</summary>
		public static bool EsUnaEtiquetaConocida(string idioma)
		{
			string primaria = SubetiquetaPrimaria(idioma);
			return primaria == IdiomaDelRegistro || primaria == ElOtroIdioma;
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

		/// <summary>Si el catalogo de este idioma tiene esa clave, para quien quiera distinguir el respaldo.</summary>
		public bool Tiene(string clave) => catalogo.ContainsKey(clave);

		/// <summary>
		/// LAS CLAVES QUE ESTE IDIOMA ESCRIBE. Existe para que una comprobacion pueda comparar los dos
		/// catalogos sin abrirlos: una clave que esta en uno y no en el otro es una frase que llega a
		/// medias -- con un {hueco} crudo a la vista -- y ninguna otra comprobacion la ve.
		/// </summary>
		public IEnumerable<string> Claves => catalogo.Keys;

		/// <summary>
		/// UN MONTO EN COLONES, redondeado al colon y con su simbolo. El separador de miles es del
		/// idioma: en Costa Rica es el punto, y en ingles la coma.
		/// </summary>
		public string Monto(decimal monto)
		{
			return SimboloDeLaMoneda + Math.Round(monto, 0, MidpointRounding.AwayFromZero)
				.ToString("#,##0", esEspanol ? Colon : Coma);
		}

		/// <summary>
		/// Una proporcion dicha como porcentaje: 0,062 es "6,2 %" en espanol y "6.2%" en ingles. El
		/// separador decimal es del idioma igual que el de miles, y por eso no se escribe invariante.
		/// </summary>
		public string Porcentaje(decimal proporcion)
		{
			return Math.Round(proporcion * 100m, 1).ToString("0.#", esEspanol ? Colon : Coma)
				+ (esEspanol ? " %" : "%");
		}

		/// <summary>
		/// UNA CANTIDAD CON SU PARTE DECIMAL, la que lleva el contenido de una presentacion: "1,5" en
		/// espanol y "1.5" en ingles. No lleva separador de miles porque no hay presentacion de mil
		/// litros; lo que lleva es el separador decimal del idioma.
		/// </summary>
		public string Cantidad(decimal cantidad)
		{
			return cantidad.ToString("0.###", esEspanol ? Colon : Coma);
		}

		/// <summary>Cuantos dias, dicho entero para que "1 dias" no aparezca nunca en ningun idioma.</summary>
		public string Dias(int dias)
		{
			if (esEspanol) return dias == 1 ? "1 día" : dias + " días";
			return dias == 1 ? "1 day" : dias + " days";
		}

		/// <summary>Cuantos meses, por la misma razon que los dias.</summary>
		public string Meses(int meses)
		{
			if (esEspanol) return meses == 1 ? "1 mes" : meses + " meses";
			return meses == 1 ? "1 month" : meses + " months";
		}

		/// <summary>Cuantas compras, por la misma razon que los dias.</summary>
		public string Compras(int compras)
		{
			if (esEspanol) return compras == 1 ? "1 compra" : compras + " compras";
			return compras == 1 ? "1 purchase" : compras + " purchases";
		}

		/// <summary>
		/// Un dia de calendario en anio-mes-dia, sin hora. NO cambia con el idioma a proposito: el orden
		/// anio-mes-dia no se puede leer al reves, y darle una forma por idioma invitaria a moverlo.
		/// </summary>
		public string Dia(DateTime dia)
		{
			return dia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}

		/// <summary>Un mes de calendario en anio-mes, por la misma razon que el dia.</summary>
		public string Mes(DateTime mes)
		{
			return mes.ToString("yyyy-MM", CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// UNA ENUMERACION QUE UNA PERSONA LEE DE CORRIDO: "leche, arroz y galletas". La conjuncion es
		/// del idioma, asi que la lista se arma aca y no en el dominio, que solo entrega los nombres.
		/// <para>
		/// Los nombres vienen del registro del supermercado y NO se traducen; lo unico que esta capa
		/// pone son las comas y la conjuncion.
		/// </para>
		/// </summary>
		public string Lista(IReadOnlyList<string> nombres)
		{
			ArgumentNullException.ThrowIfNull(nombres);
			if (nombres.Count == 0) return "";
			if (nombres.Count == 1) return nombres[0];

			var todos = new List<string>(nombres);
			string ultimo = todos[todos.Count - 1];
			todos.RemoveAt(todos.Count - 1);

			return string.Join(", ", todos) + (esEspanol ? " y " : " and ") + ultimo;
		}

		/// <summary>
		/// La misma enumeracion con un TOPE: una frase que enumera veinte productos no la lee nadie, y
		/// lo que sobra se dice contando. El tope lo elige quien redacta, porque es cuantos nombres
		/// hacen falta para reconocer eso que se esta nombrando.
		/// </summary>
		public string Lista(IReadOnlyList<string> nombres, int tope)
		{
			ArgumentNullException.ThrowIfNull(nombres);
			if (nombres.Count <= tope) return Lista(nombres);

			var primeros = new List<string>();
			for (int cual = 0; cual < tope; cual++) primeros.Add(nombres[cual]);

			return Diga("comun.yMas", ("lista", Lista(primeros)), ("resto", (nombres.Count - tope).ToString(
				CultureInfo.InvariantCulture)));
		}

		/// <summary>
		/// EL SEPARADOR ENTRE DOS CLAUSULAS de una misma respuesta. Es del idioma igual que la
		/// conjuncion, y por eso no se escribe en ningun catalogo como media frase.
		/// </summary>
		public string Encadenadas(IReadOnlyList<string> clausulas, string separador)
		{
			var dichas = new List<string>();
			foreach (string clausula in clausulas)
				if (!string.IsNullOrEmpty(clausula)) dichas.Add(clausula);

			return string.Join(separador, dichas);
		}

		/// <summary>La conjuncion con la que se encadenan dos clausulas de la misma respuesta.</summary>
		public string ElYQueEncadena => esEspanol ? ", y " : ", and ";

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
