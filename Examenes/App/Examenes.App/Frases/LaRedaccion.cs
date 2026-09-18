using System;
using System.Collections.Generic;

namespace Examenes.App.Frases
{
	/// <summary>
	/// COMO SE ARMA CADA RESPUESTA a partir de lo que el dominio mando: su clave, sus claves de
	/// clausula y sus valores. Una funcion por respuesta, y el anidado -- que clausula va en que hueco --
	/// esta escrito una sola vez, aca.
	/// <para>
	/// Ningun valor se traduce al entrar en un hueco: el numero que midio el laboratorio y el simbolo
	/// de su unidad salieron del registro y se muestran tal como se asentaron.
	/// </para>
	/// </summary>
	public static class LaRedaccion
	{
		/// <summary>
		/// COMO SE LLAMA UN ANALITO o un examen. Un codigo sin dicho se dice con su propio codigo, que
		/// es lo que la pantalla mostraba antes de que existiera esta tabla.
		/// </summary>
		public static string Nombre(Frases frases, string codigo) => Dicho(frases, "nombre", codigo);

		/// <summary>El mismo nombre con su articulo, que es lo que entra en el hueco de una frase.</summary>
		public static string ElNombre(Frases frases, string codigo) => Dicho(frases, "elNombre", codigo);

		/// <summary>El mismo nombre como posesivo: "tu ultima hemoglobina".</summary>
		public static string TuUltimo(Frases frases, string codigo) => Dicho(frases, "tuUltimo", codigo);

		/// <summary>COMO SE DICE UNA SEVERIDAD que el dominio eligio de su lista cerrada.</summary>
		public static string Severidad(Frases frases, string codigo) => Dicho(frases, "severidad", codigo);

		/// <summary>COMO SE DICE UN VEREDICTO que el dominio eligio de su lista cerrada.</summary>
		public static string Veredicto(Frases frases, string codigo) => Dicho(frases, "veredicto", codigo);

		/// <summary>COMO SE DICE UNA TENDENCIA que el dominio eligio de su lista cerrada.</summary>
		public static string Tendencia(Frases frases, string codigo) => Dicho(frases, "tendencia", codigo);

		/// <summary>COMO SE DICE UNA PRESENCIA que el dominio eligio de su lista cerrada.</summary>
		public static string Presencia(Frases frases, string codigo) => Dicho(frases, "presencia", codigo);

		/// <summary>COMO SE DICE UNA CONSTANCIA que el dominio eligio de su lista cerrada.</summary>
		public static string Constancia(Frases frases, string codigo) => Dicho(frases, "constancia", codigo);

		/// <summary>COMO SE DICE UN SEXO que el dominio resolvio de su lista cerrada.</summary>
		public static string Sexo(Frases frases, string codigo) => Dicho(frases, "sexo", codigo);

		/// <summary>
		/// COMO SE DICE LO QUE DIO UNA PRUEBA: un color, un positivo, un no realizado. Es la categoria
		/// que el laboratorio informo, resuelta por el dominio a un codigo de su lista cerrada.
		/// </summary>
		public static string Categoria(Frases frases, string codigo) => Dicho(frases, "categoria", codigo);

		/// <summary>
		/// QUE HACER Y CUANDO, dicho entero. Vacio cuando no hay nada que hacer, que es la mayoria de
		/// las veces y es una respuesta.
		/// </summary>
		public static string Accion(Frases frases, string claveDeLaAccion, string claveDelPlazo)
		{
			ArgumentNullException.ThrowIfNull(frases);
			if (string.IsNullOrEmpty(claveDeLaAccion)) return "";

			return frases.Diga("accion." + claveDeLaAccion,
				("plazo", frases.Diga("plazo." + claveDelPlazo)));
		}

		/// <summary>
		/// LA LECTURA DE UNA SERIE, que es la respuesta con mas clausulas de todo el modulo: el marco lo
		/// nombra el analito, y adentro van la meta, el HDL o el grado de anemia segun cual sea.
		/// </summary>
		public static string Lectura(Frases frases, string clave, string claveDelAnalito,
			decimal valorInicial, decimal valorFinal, string unidad, int dias, decimal cambio,
			int cuantas, int cuantasSeguidas, string claveDeLaCategoria,
			decimal nivelPropio, decimal limiteCitado, decimal ritmoPorAno, string claveDeLaColetilla)
		{
			ArgumentNullException.ThrowIfNull(frases);
			if (string.IsNullOrEmpty(clave)) return "";

			string meta = frases.Diga(claveDeLaColetilla,
				("valor", frases.Medida(valorFinal, unidad)),
				("objetivo", frases.Medida(limiteCitado, unidad)),
				("falta", frases.Medida(valorFinal - limiteCitado, unidad)));

			string comoObjetivo = frases.Diga(
				claveDeLaColetilla == "meta.ya-llego"
					? "meta.como-objetivo.ya-llego"
					: "meta.como-objetivo.falta",
				("objetivo", frases.Medida(limiteCitado, unidad)));

			string coletilla = ColetillaSuelta(frases, claveDeLaColetilla, nivelPropio, unidad);

			return frases.Diga(clave,
				("analito", ElNombre(frases, claveDelAnalito)),
				("valor", frases.Medida(valorFinal, unidad)),
				("antes", frases.Numero(valorInicial)),
				("ahora", frases.Medida(valorFinal, unidad)),
				("cambio", frases.Medida(Math.Abs(cambio), unidad)),
				("dias", frases.Dias(dias)),
				("cuantas", frases.Veces(cuantas)),
				("pruebas", frases.Pruebas(cuantas)),
				("seguidas", cuantasSeguidas.ToString(System.Globalization.CultureInfo.InvariantCulture)),
				("positivas", frases.Veces((int)cambio)),
				("noEsperables", frases.Veces((int)cambio)),
				("categoria", Categoria(frases, claveDeLaCategoria)),
				("nivel", frases.Medida(nivelPropio, unidad)),
				("limite", frases.Medida(limiteCitado, unidad)),
				("ritmo", frases.Medida(ritmoPorAno, unidad)),
				("meta", meta),
				("comoObjetivo", comoObjetivo),
				("ComoObjetivo", frases.Mayuscula(comoObjetivo)),
				("hdl", coletilla),
				("coletilla", coletilla));
		}

		/// <summary>
		/// LA CLAUSULA QUE LE CUELGA A LA LECTURA, cuando la clave nombra una. El grado de anemia y el
		/// HDL son las dos que hay, y las dos citan el mismo valor: el nivel propio.
		/// </summary>
		private static string ColetillaSuelta(Frases frases, string clave, decimal nivelPropio,
			string unidad)
		{
			if (string.IsNullOrEmpty(clave)) return "";
			if (clave.StartsWith("meta.", StringComparison.Ordinal)) return "";

			if (clave.StartsWith("anemia.", StringComparison.Ordinal))
				return frases.Diga(clave,
					("grado", frases.Diga("grado." + clave.Substring("anemia.".Length))));

			return frases.Diga(clave, ("valor", frases.Medida(nivelPropio, unidad)));
		}

		/// <summary>CONTRA QUE SE LEE EL VALOR, con los extremos que la frase nombra.</summary>
		public static string Referencia(Frases frases, string clave, decimal minimo, decimal maximo,
			string unidad)
		{
			ArgumentNullException.ThrowIfNull(frases);
			if (string.IsNullOrEmpty(clave)) return "";

			return frases.Diga(clave,
				("piso", frases.Medida(minimo, unidad)),
				("techo", frases.Medida(maximo, unidad)));
		}

		/// <summary>UNA LINEA DEL GRAFICO, explicada con el valor que la marca.</summary>
		public static string Linea(Frases frases, string clave, decimal valor, string unidad)
		{
			ArgumentNullException.ThrowIfNull(frases);
			if (string.IsNullOrEmpty(clave)) return "";

			return frases.Diga(clave, ("valor", frases.Medida(valor, unidad)));
		}

		/// <summary>A QUE RITMO VIENE CAMBIANDO, dicho entero.</summary>
		public static string Velocidad(Frases frases, string clave, decimal porAno, string unidad)
		{
			ArgumentNullException.ThrowIfNull(frases);
			if (string.IsNullOrEmpty(clave)) return "";

			return frases.Diga(clave, ("ritmo", frases.Medida(porAno, unidad)));
		}

		/// <summary>
		/// LO QUE DIO UN EXAMEN, armado con sus lecturas. Cada forma de examen se lee distinto: uno de
		/// sangre es un numero con su unidad, el fraccionado son sus dos valores, y un urianalisis es
		/// su color seguido de las pruebas que el laboratorio corrio.
		/// </summary>
		public static string Resultado(Frases frases, string claveDelExamen,
			IReadOnlyList<(string Analito, string Resultado, decimal Valor, string Unidad, bool EsNumero)> lecturas)
		{
			ArgumentNullException.ThrowIfNull(frases);
			ArgumentNullException.ThrowIfNull(lecturas);

			if (lecturas.Count == 0) return "";

			if (claveDelExamen == "colesterol-detallado" && lecturas.Count >= 2)
				return frases.Diga("lista.colesterol-detallado",
					("ldl", frases.Medida(lecturas[0].Valor, lecturas[0].Unidad)),
					("hdl", frases.Medida(lecturas[1].Valor, lecturas[1].Unidad)));

			var partes = new List<string>();

			for (int cual = 0; cual < lecturas.Count; cual++)
			{
				var lectura = lecturas[cual];
				string dicho = lectura.EsNumero
					? frases.Medida(lectura.Valor, lectura.Unidad)
					: Categoria(frases, lectura.Resultado);

				// LA PRIMERA VA SOLA y las demas nombran que se midio, que es como lo informa
				// el laboratorio: "ambar, nitritos positivo, sangre negativo".
				partes.Add(cual == 0
					? dicho
					: frases.Diga("lista.parte",
						("analito", Dicho(frases, "corto", lectura.Analito)),
						("resultado", dicho)));
			}

			return string.Join(", ", partes);
		}

		/// <summary>CUANTO HACE QUE NO SE MIDE UN ANALITO, y cada cuanto conviene repetirlo.</summary>
		public static string Control(Frases frases, string clave, string claveDelAnalito,
			string claveDeLoQueHace, int anosDesdeLaUltima, int diasDesdeLaUltima,
			string claveDeCadaCuanto, int anosRecomendados, int mesesRecomendados)
		{
			ArgumentNullException.ThrowIfNull(frases);
			if (string.IsNullOrEmpty(clave)) return "";

			string hace = frases.Diga(claveDeLoQueHace,
				("anios", frases.Anios(anosDesdeLaUltima)),
				("dias", frases.Dias(diasDesdeLaUltima)));

			string cada = frases.Diga(claveDeCadaCuanto,
				("anios", frases.Anios(anosRecomendados)),
				("meses", frases.Meses(mesesRecomendados)));

			return frases.Diga(clave,
				("analito", TuUltimo(frases, claveDelAnalito)),
				("hace", hace),
				("cada", cada));
		}

		/// <summary>CUANTOS ESTAN DONDE TIENEN QUE ESTAR. El plural del verbo lo resuelve el idioma.</summary>
		public static string EnRango(Frases frases, string clave, int enSuRango, int analitos)
		{
			ArgumentNullException.ThrowIfNull(frases);
			if (string.IsNullOrEmpty(clave)) return "";

			return frases.Diga(clave + (enSuRango == 1 ? ".uno" : ".varios"),
				("enSuRango", enSuRango.ToString(System.Globalization.CultureInfo.InvariantCulture)),
				("analitos", analitos.ToString(System.Globalization.CultureInfo.InvariantCulture)));
		}

		/// <summary>
		/// EL DATO QUE FALTA, y POR QUE falta. Las dos razones se encadenan con la conjuncion del
		/// idioma y cada una puede faltar: que falte una sola es una respuesta entera.
		/// </summary>
		public static string PedidoDeSexo(Frases frases, string clave, int sinInterpretar, int analitos,
			IReadOnlyList<string> porRango, IReadOnlyList<string> porAplicabilidad)
		{
			ArgumentNullException.ThrowIfNull(frases);
			ArgumentNullException.ThrowIfNull(porRango);
			ArgumentNullException.ThrowIfNull(porAplicabilidad);
			if (string.IsNullOrEmpty(clave)) return "";

			var razones = new List<string>();

			if (porRango.Count > 0)
				razones.Add(frases.Diga(
					porRango.Count == 1 ? "pedidoDeSexo.rango.uno" : "pedidoDeSexo.rango.varios",
					("cuales", frases.Lista(Dichos(frases, porRango)))));

			if (porAplicabilidad.Count > 0)
				razones.Add(frases.Diga(
					porAplicabilidad.Count == 1
						? "pedidoDeSexo.aplicabilidad.uno"
						: "pedidoDeSexo.aplicabilidad.varios",
					("cuales", frases.Lista(Dichos(frases, porAplicabilidad))),
					("cuantos", porAplicabilidad.Count.ToString(
						System.Globalization.CultureInfo.InvariantCulture))));

			return frases.Diga(clave,
				("sinInterpretar", sinInterpretar.ToString(
					System.Globalization.CultureInfo.InvariantCulture)),
				("analitos", analitos.ToString(System.Globalization.CultureInfo.InvariantCulture)),
				("porque", frases.Encadenadas(razones, frases.ElYQueEncadena)));
		}

		/// <summary>UNA MEDICION DEL ANIO, con su cambio contra el anterior cuando lo hay.</summary>
		public static string MedicionDelAnio(Frases frases, string clave, int anio, decimal valor,
			string unidad, decimal cambio)
		{
			ArgumentNullException.ThrowIfNull(frases);
			if (string.IsNullOrEmpty(clave)) return "";

			string conSigno = (cambio > 0m ? "+" : "") + frases.Numero(cambio);

			return frases.Diga(clave,
				("ano", anio.ToString(System.Globalization.CultureInfo.InvariantCulture)),
				("valor", frases.Medida(valor, unidad)),
				("cambio", conSigno),
				("anterior", (anio - 1).ToString(System.Globalization.CultureInfo.InvariantCulture)));
		}

		/// <summary>UNA BANDA DEL HISTOGRAMA, con cuantas mediciones cayeron adentro.</summary>
		public static string Banda(Frases frases, decimal piso, decimal techo, string unidad, int cuantas)
		{
			ArgumentNullException.ThrowIfNull(frases);

			return frases.Diga("banda.expresada",
				("piso", frases.Numero(piso)),
				("techo", frases.Numero(techo)),
				("unidad", unidad),
				("cuantas", cuantas.ToString(System.Globalization.CultureInfo.InvariantCulture)));
		}

		private static IReadOnlyList<string> Dichos(Frases frases, IReadOnlyList<string> codigos)
		{
			var dichos = new List<string>();
			foreach (string codigo in codigos) dichos.Add(ElNombre(frases, codigo));
			return dichos;
		}

		/// <summary>
		/// EL DICHO DE UN CODIGO en su tabla. Un codigo sin dicho se dice con su propio codigo y no
		/// revienta: una pantalla a la que le falta una palabra sigue sirviendo, y el hueco se ve.
		/// <para>
		/// EL CODIGO SE NORMALIZA porque algunos llevan espacio -- <c>no realizado</c>,
		/// <c>amarillo palido</c> -- y una clave de catalogo no.
		/// </para>
		/// </summary>
		private static string Dicho(Frases frases, string tabla, string codigo)
		{
			ArgumentNullException.ThrowIfNull(frases);
			if (string.IsNullOrWhiteSpace(codigo)) return "";

			string dicho = frases.Diga(tabla + "." + codigo.Trim().Replace(' ', '-'));
			return dicho.StartsWith("{", StringComparison.Ordinal) ? codigo : dicho;
		}
	}
}
