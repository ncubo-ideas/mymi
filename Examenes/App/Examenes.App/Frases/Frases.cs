using System;
using System.Collections.Generic;
using System.Globalization;

namespace Examenes.App.Frases
{
	/// <summary>
	/// EL RENDERIZADOR, uno por idioma. El dominio decide QUE frase y CON QUE valores; esta capa decide
	/// como se dice. La redaccion vive aca y no en <c>Dominio/</c> a proposito: una frase es texto de
	/// presentacion, y el dominio que la escribiera tendria que elegir un idioma.
	/// <para>
	/// La forma es CLAVE MAS VALORES. La clave la pone el dominio y es estable -- es la misma etiqueta
	/// que ya cruza la puerta --; los valores viajan tipados y se meten en los huecos SIN traducirse,
	/// porque son del registro del laboratorio: un numero medido y el simbolo de su unidad.
	/// </para>
	/// <para>
	/// El formato de una cifra viaja con el renderizador y no con el dominio: la coma decimal es una
	/// decision del idioma, y dejarla adentro obligaria al dominio a tener uno.
	/// </para>
	/// </summary>
	public sealed class Frases
	{
		/// <summary>
		/// EL IDIOMA DEL REGISTRO del laboratorio, y al que cae toda etiqueta desconocida. Vive aca y
		/// en ningun otro lado: es la subetiqueta con la que se elige catalogo, asi que escribirla dos
		/// veces seria dejar que las dos copias dejen de decir lo mismo sin que nada avise.
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
			if (string.IsNullOrEmpty(clave)) return "";
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
		/// UN VALOR MEDIDO, con los mismos digitos con que lo asento el laboratorio y SIN unidad: la
		/// unidad se pega aparte, porque su simbolo no se traduce y el separador decimal si cambia.
		/// </summary>
		public string Numero(decimal valor)
		{
			return valor.ToString("0.###", esEspanol ? Coma : Punto);
		}

		/// <summary>
		/// UN VALOR MEDIDO CON SU UNIDAD: "280 mg/dL". El simbolo lo asento el laboratorio y NO se
		/// traduce en ningun idioma; lo unico que cambia es el separador decimal del numero.
		/// </summary>
		public string Medida(decimal valor, string unidad)
		{
			string numero = Numero(valor);
			return string.IsNullOrEmpty(unidad) ? numero : numero + " " + unidad;
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

		/// <summary>Cuantos anios, por la misma razon que los dias.</summary>
		public string Anios(int anios)
		{
			if (esEspanol) return anios == 1 ? "1 año" : anios + " años";
			return anios == 1 ? "1 year" : anios + " years";
		}

		/// <summary>Cuantas veces se midio, por la misma razon que los dias.</summary>
		public string Veces(int veces)
		{
			if (esEspanol) return veces == 1 ? "1 vez" : veces + " veces";
			return veces == 1 ? "1 time" : veces + " times";
		}

		/// <summary>Cuantas pruebas se corrieron, por la misma razon que los dias.</summary>
		public string Pruebas(int pruebas)
		{
			if (esEspanol) return pruebas == 1 ? "1 prueba" : pruebas + " pruebas";
			return pruebas == 1 ? "1 test" : pruebas + " tests";
		}

		/// <summary>Cuantos examenes, por la misma razon que los dias.</summary>
		public string Examenes(int examenes)
		{
			if (esEspanol) return examenes == 1 ? "1 examen" : examenes + " exámenes";
			return examenes == 1 ? "1 test" : examenes + " tests";
		}

		/// <summary>
		/// Un dia de calendario en anio-mes-dia, sin hora. NO cambia con el idioma a proposito: el orden
		/// anio-mes-dia no se puede leer al reves, y darle una forma por idioma invitaria a moverlo.
		/// </summary>
		public string Dia(DateTime dia)
		{
			return dia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// UNA ENUMERACION QUE UNA PERSONA LEE DE CORRIDO: "la hemoglobina, el colesterol y el sodio".
		/// La conjuncion es del idioma, asi que la lista se arma aca y no en el dominio, que solo
		/// entrega los codigos.
		/// </summary>
		public string Lista(IReadOnlyList<string> dichos)
		{
			ArgumentNullException.ThrowIfNull(dichos);
			if (dichos.Count == 0) return "";
			if (dichos.Count == 1) return dichos[0];

			var todos = new List<string>(dichos);
			string ultimo = todos[todos.Count - 1];
			todos.RemoveAt(todos.Count - 1);

			return string.Join(", ", todos) + (esEspanol ? " y " : " and ") + ultimo;
		}

		/// <summary>
		/// EL SEPARADOR ENTRE DOS CLAUSULAS de una misma respuesta. Es del idioma igual que la
		/// conjuncion, y por eso no se escribe en ningun catalogo como media frase.
		/// </summary>
		public string Encadenadas(IReadOnlyList<string> clausulas, string separador)
		{
			ArgumentNullException.ThrowIfNull(clausulas);

			var dichas = new List<string>();
			foreach (string clausula in clausulas)
				if (!string.IsNullOrEmpty(clausula)) dichas.Add(clausula);

			return string.Join(separador, dichas);
		}

		/// <summary>La conjuncion con la que se encadenan dos clausulas de la misma respuesta.</summary>
		public string ElYQueEncadena => esEspanol ? ", y " : ", and ";

		/// <summary>
		/// LA MISMA FRASE EMPEZANDO UNA ORACION. Es mecanica de escritura, no redaccion: las dos
		/// lenguas del alcance escriben la primera letra en mayuscula.
		/// </summary>
		public string Mayuscula(string frase)
		{
			if (string.IsNullOrEmpty(frase)) return frase;
			return char.ToUpperInvariant(frase[0]) + frase.Substring(1);
		}

		private static readonly NumberFormatInfo Coma = new NumberFormatInfo
		{
			NumberGroupSeparator = ".",
			NumberDecimalSeparator = ",",
			NumberGroupSizes = new[] { 3 },
		};

		private static readonly NumberFormatInfo Punto = new NumberFormatInfo
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
