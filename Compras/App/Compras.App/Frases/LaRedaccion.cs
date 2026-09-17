using System;
using System.Collections.Generic;
using System.Globalization;

namespace Compras.App.Frases
{
	/// <summary>
	/// COMO SE ARMA CADA RESPUESTA a partir de lo que el dominio mando: su clave, sus claves de
	/// clausula y sus valores. Una funcion por respuesta, y el anidado -- que clausula va en que hueco --
	/// esta escrito una sola vez, aca.
	/// <para>
	/// Ningun valor se traduce al entrar en un hueco: la descripcion de un producto, el nombre de una
	/// sucursal, el numero de un tiquete y el mes de una serie salieron del registro del supermercado y
	/// se muestran tal como se asentaron.
	/// </para>
	/// </summary>
	public static class LaRedaccion
	{
		/// <summary>Cuantos productos se nombran en una firma antes de que deje de ser firma y sea inventario.</summary>
		private const int ProductosQueSeNombranEnLaFirma = 5;

		/// <summary>Cuantos nombres se enumeran antes de que la frase deje de leerse de corrido.</summary>
		private const int NombresQueSeEnumeran = 4;

		/// <summary>Cuantos pasillos alcanzan para explicar un cambio: mas que eso ya no explica, enumera.</summary>
		private const int PasillosQueExplican = 3;

		/// <summary>
		/// CUANTO HACE QUE NO ENTRA UN DEPARTAMENTO. Tres claves anidadas: el marco, cuanto hace, y de
		/// donde sale el numero con el que se lo esta juzgando -- que no es lo mismo si lo midio la
		/// persona o si sale del catalogo.
		/// </summary>
		public static string Abastecimiento(Frases frases, string clave, string categoria,
			int diasDesdeLaUltima, int diasEsperados, bool esRitmoObservado)
		{
			ArgumentNullException.ThrowIfNull(frases);

			string hace = diasDesdeLaUltima >= DiasDesdeLosCualesSeCuentaEnMeses
				? frases.Diga("abastecimiento.hace.meses", ("meses", frases.Meses(diasDesdeLaUltima / 30)))
				: frases.Diga("abastecimiento.hace.dias", ("dias", frases.Dias(diasDesdeLaUltima)));

			string cada = frases.Diga(
				esRitmoObservado ? "abastecimiento.cada.medido" : "abastecimiento.cada.catalogo",
				("dias", frases.Dias(diasEsperados)));

			return frases.Diga("abastecimiento." + clave,
				("mencion", Mencion(frases, categoria)),
				("hace", hace),
				("cada", cada));
		}

		/// <summary>Desde cuantos dias una ausencia deja de contarse en dias y se cuenta en meses.</summary>
		private const int DiasDesdeLosCualesSeCuentaEnMeses = 60;

		/// <summary>COMO CAMBIO EL RITMO DE UN PRODUCTO: de que cadencia a que cadencia.</summary>
		public static string CambioDeRitmo(Frases frases, string clave, string producto,
			string claveDeAntes, string claveDeAhora)
		{
			ArgumentNullException.ThrowIfNull(frases);

			return frases.Diga("cambioDeRitmo." + clave,
				("producto", producto),
				("antes", Cadencia(frases, claveDeAntes)),
				("ahora", Cadencia(frases, claveDeAhora)));
		}

		/// <summary>
		/// QUE ENTRO Y QUE SE CAYO DE LA CANASTA. Las dos clausulas se encadenan con la conjuncion del
		/// idioma, y cada una puede faltar: entrar sin que se caiga nada es una respuesta entera.
		/// </summary>
		public static string CambioEnLaCanasta(Frases frases, string clave, string mesDelCambio,
			IReadOnlyList<string> entraron, IReadOnlyList<string> salieron)
		{
			ArgumentNullException.ThrowIfNull(frases);

			if (clave != "hay-cambio") return frases.Diga("cambioEnLaCanasta." + clave);

			var partes = new List<string>();

			if (entraron.Count > 0)
				partes.Add(frases.Diga("cambioEnLaCanasta.entraron",
					("mes", mesDelCambio),
					("productos", frases.Lista(entraron, NombresQueSeEnumeran))));

			if (salieron.Count > 0)
				partes.Add(frases.Diga("cambioEnLaCanasta.salieron",
					("productos", frases.Lista(salieron, NombresQueSeEnumeran))));

			return frases.Encadenadas(partes, frases.ElYQueEncadena);
		}

		/// <summary>CUANTO CUESTA HOY LA MISMA CANASTA que hace unos meses, sumada en colones.</summary>
		public static string CanastaFrenteAlPasado(Frases frases, string clave, int mesesDeVentana,
			int cuantosProductos, decimal cambio)
		{
			ArgumentNullException.ThrowIfNull(frases);

			return frases.Diga("canastaFrenteAlPasado." + clave,
				("meses", frases.Meses(mesesDeVentana)),
				("cuantos", Numero(cuantosProductos)),
				("monto", frases.Monto(Math.Abs(cambio))));
		}

		/// <summary>
		/// COMO CAMBIO LA CANASTA ENTRE DOS MESES, y de que pasillos sale la diferencia. El pasillo que
		/// ENTRO se marca en el mismo lugar donde se lo nombra: nombrarlo dos veces hacia que la linea
		/// dijera dos veces lo mismo.
		/// </summary>
		public static string Comparacion(Frases frases, string clave,
			string mesAntes, string mesDespues, decimal gastoAntes, decimal gastoDespues,
			IReadOnlyList<string> subieron, IReadOnlyList<bool> sonNuevos, IReadOnlyList<string> bajaron)
		{
			ArgumentNullException.ThrowIfNull(frases);

			if (clave == "sin-comparacion") return frases.Diga("comparacion.sin-comparacion");

			string totales = frases.Diga("comparacion.totales",
				("mesDespues", mesDespues),
				("gastoDespues", frases.Monto(gastoDespues)),
				("mesAntes", mesAntes),
				("gastoAntes", frases.Monto(gastoAntes)));

			if (clave == "repartida")
				return frases.Diga("comparacion.repartida", ("totales", totales));

			var partes = new List<string> { totales };

			if (subieron.Count > 0)
			{
				var titulos = new List<string>();
				for (int cual = 0; cual < subieron.Count; cual++)
					titulos.Add(sonNuevos[cual]
						? frases.Diga("comparacion.nuevo", ("titulo", subieron[cual]))
						: subieron[cual]);

				partes.Add(frases.Diga("comparacion.explican",
					("titulos", frases.Lista(titulos, PasillosQueExplican))));
			}

			if (bajaron.Count > 0)
				partes.Add(frases.Diga(
					bajaron.Count == 1 ? "comparacion.bajaron.uno" : "comparacion.bajaron.varios",
					("titulos", frases.Lista(bajaron, PasillosQueExplican))));

			return frases.Encadenadas(partes, "; ");
		}

		/// <summary>
		/// EN CUALES COMPRAS APARECIO ESTO. Se nombra el producto ENCONTRADO y no lo escrito cuando son
		/// distintos: quien busco "leche" tiene que poder confirmar que la app entendio otra cosa.
		/// </summary>
		public static string Hallazgos(Frases frases, string clave, string buscado,
			IReadOnlyList<string> productos, int cuantasCompras, int totalUnidades, decimal totalGastado,
			string ultimaFecha)
		{
			ArgumentNullException.ThrowIfNull(frases);

			if (clave == "ninguna")
				return frases.Diga("hallazgos.ninguna", ("buscado", buscado));

			string que = productos.Count == 1
				? frases.Diga("hallazgos.que.uno", ("producto", productos[0]))
				: frases.Diga("hallazgos.que.varios",
					("cuantos", Numero(productos.Count)), ("buscado", buscado));

			return frases.Diga("hallazgos.marco",
				("que", que),
				("compras", frases.Compras(cuantasCompras)),
				("unidades", Numero(totalUnidades)),
				("monto", frases.Monto(totalGastado)),
				("fecha", ultimaFecha));
		}

		/// <summary>CUANTO Y CUANDO ENTRO ALGO, mes a mes. El termino se entrecomilla solo cuando es un texto.</summary>
		public static string Consumo(Frases frases, string clave, string termino, bool esCategoria,
			string primerMes, string ultimoMes, string mesDelPico,
			decimal gastoEnElPrimerMes, decimal gastoEnElUltimoMes, decimal totalGastado,
			int mesesConConsumo)
		{
			ArgumentNullException.ThrowIfNull(frases);

			if (clave == "sin-consumo")
				return frases.Diga("consumo.sin-consumo", ("termino", termino));

			string que = esCategoria
				? frases.Diga("consumo.que.categoria", ("termino", TituloDelDepartamento(frases, termino)))
				: frases.Diga("consumo.que.texto", ("termino", termino));

			if (clave == "un-solo-mes")
				return frases.Diga("consumo.un-solo-mes",
					("que", que), ("mes", primerMes), ("monto", frases.Monto(totalGastado)));

			string tramo = frases.Diga("consumo.tramo",
				("antes", frases.Monto(gastoEnElPrimerMes)),
				("primerMes", primerMes),
				("ahora", frases.Monto(gastoEnElUltimoMes)),
				("ultimoMes", ultimoMes));

			return frases.Diga("consumo." + clave,
				("que", que),
				("tramo", tramo),
				("meses", frases.Meses(mesesConConsumo)),
				("pico", mesDelPico));
		}

		/// <summary>LO QUE NO FALLA en la canasta. Cinco nombres alcanzan para reconocerse.</summary>
		public static string Firma(Frases frases, string clave, int comprasMiradas,
			IReadOnlyList<string> habituales)
		{
			ArgumentNullException.ThrowIfNull(frases);

			return frases.Diga("firma." + clave,
				("cuantas", Numero(comprasMiradas)),
				("productos", frases.Lista(habituales, ProductosQueSeNombranEnLaFirma)));
		}

		/// <summary>LO QUE VA A FALTAR EN LA PROXIMA COMPRA. Ninguno atrasado es una respuesta, no una lista vacia.</summary>
		public static string ProximaCompra(Frases frases, string clave, int cuantosTocan,
			IReadOnlyList<string> productos)
		{
			ArgumentNullException.ThrowIfNull(frases);

			return frases.Diga("proximaCompra." + clave,
				("cuantos", Numero(cuantosTocan)),
				("productos", frases.Lista(productos, NombresQueSeEnumeran)));
		}

		/// <summary>UNA FILA DE ESA LISTA: cada cuanto vuelve y cuanto hace que no entra.</summary>
		public static string ProductoQueVaAFaltar(Frases frases, string clave, string producto,
			string claveDeCadencia, int diasDesdeLaUltima, int diasQueFaltan)
		{
			ArgumentNullException.ThrowIfNull(frases);

			return frases.Diga("vaAFaltar." + clave,
				("producto", producto),
				("cadencia", Cadencia(frases, claveDeCadencia)),
				("desdeLaUltima", frases.Dias(diasDesdeLaUltima)),
				("faltan", frases.Dias(diasQueFaltan)));
		}

		/// <summary>COMO LE CAMBIO EL PRECIO a la persona: lo que ELLA pago por la misma cosa.</summary>
		public static string Precio(Frases frases, string clave, string producto, string sku,
			string unidad, string desde, string hasta,
			decimal primerPrecio, decimal ultimoPrecio, decimal proporcion)
		{
			ArgumentNullException.ThrowIfNull(frases);

			if (clave == "sin-precios")
				return frases.Diga("precio.sin-precios", ("sku", sku));

			if (clave == "una-sola-vez")
				return frases.Diga("precio.una-sola-vez",
					("producto", producto),
					("mes", desde),
					("monto", frases.Monto(ultimoPrecio)),
					("unidad", unidad));

			string tramo = frases.Diga("precio.tramo",
				("antes", frases.Monto(primerPrecio)),
				("ahora", frases.Monto(ultimoPrecio)),
				("unidad", unidad),
				("desde", desde),
				("hasta", hasta));

			return frases.Diga("precio." + clave,
				("producto", producto),
				("porcentaje", frases.Porcentaje(Math.Abs(proporcion))),
				("tramo", tramo));
		}

		/// <summary>UNA PROMOCION DEL SUPER. El gancho y el detalle son SUYOS y no se traducen.</summary>
		public static string Promocion(Frases frases, string gancho, string detalle, string validaHasta)
		{
			ArgumentNullException.ThrowIfNull(frases);

			return frases.Diga("promocion",
				("gancho", gancho), ("detalle", detalle), ("fecha", validaHasta));
		}

		/// <summary>LA BUENA NOTICIA DE UN RECIBO: que, de esa compra, esta hoy mas barato.</summary>
		public static string Rebaja(Frases frases, string clave, string producto, decimal cuantoMenos,
			string numeroDeCompra)
		{
			ArgumentNullException.ThrowIfNull(frases);

			return frases.Diga("rebaja." + clave,
				("producto", producto),
				("monto", frases.Monto(cuantoMenos)),
				("numero", numeroDeCompra));
		}

		/// <summary>
		/// EN QUE SUCURSALES COMPRA, y desde cuando se mueve. La clausula de las compras que no dicen
		/// sucursal se pega al final de las dos formas que si tienen datos, y falta cuando no hay ninguna.
		/// </summary>
		public static string Sucursales(Frases frases, string clave, string laDeSiempre,
			int comprasDeLaDeSiempre, int comprasConSucursal, int comprasSinSucursal,
			string seMueveDesde, IReadOnlyList<string> otras)
		{
			ArgumentNullException.ThrowIfNull(frases);

			if (clave == "sin-datos")
				return frases.Diga(
					comprasSinSucursal > 0 ? "sucursales.sin-datos.conCompras" : "sucursales.sin-datos.sinCompras",
					("compras", frases.Compras(comprasSinSucursal)));

			string sinDato = comprasSinSucursal > 0
				? frases.Diga("sucursales.sinDato", ("compras", frases.Compras(comprasSinSucursal)))
				: "";

			return frases.Diga("sucursales." + clave,
				("sucursal", laDeSiempre),
				("compras", Numero(comprasDeLaDeSiempre)),
				("total", Numero(comprasConSucursal)),
				("desde", seMueveDesde),
				("otras", frases.Lista(otras)),
				("sinDato", sinDato));
		}

		/// <summary>HASTA CUANDO TIENE GARANTIA UN PRODUCTO, con su respaldo.</summary>
		public static string Garantia(Frases frases, string clave, string buscado, string producto,
			string venceEl, string fechaDeCompra, int diasParaVencer)
		{
			ArgumentNullException.ThrowIfNull(frases);

			return frases.Diga("garantia." + clave,
				("buscado", buscado),
				("producto", producto),
				("fecha", venceEl),
				("compra", fechaDeCompra),
				("dias", frases.Dias(diasParaVencer)));
		}

		/// <summary>CADA CUANTO ENTRA UN DEPARTAMENTO, medido en el historial de la persona.</summary>
		public static string RitmoDeCompra(Frases frases, string clave, string categoria,
			int diasTipicos, int intervalosMedidos, int diasMasCorto, int diasMasLargo,
			int diasDelCatalogo)
		{
			ArgumentNullException.ThrowIfNull(frases);

			return frases.Diga("ritmoDeCompra." + clave,
				("categoria", TituloDelDepartamento(frases, categoria)),
				("cada", frases.Dias(diasTipicos)),
				("intervalos", Numero(intervalosMedidos)),
				("corto", Numero(diasMasCorto)),
				("largo", Numero(diasMasLargo)),
				("catalogo", frases.Dias(diasDelCatalogo)));
		}

		/// <summary>COMO VA AL SUPER: cada cuantos dias, que dia y en que momento.</summary>
		public static string RitmoDelSuper(Frases frases, string clave, int comprasMiradas,
			int diasTipicos, string claveDelDia, string claveDelMomento)
		{
			ArgumentNullException.ThrowIfNull(frases);

			return frases.Diga("ritmoDelSuper." + clave,
				("compras", frases.Compras(comprasMiradas)),
				("cada", frases.Dias(diasTipicos)),
				("dia", DiaDeLaSemana(frases, claveDelDia)),
				("momento", Momento(frases, claveDelMomento)));
		}

		/// <summary>UNA FILA DE LOS MOMENTOS DEL DIA: "tarde · 7 compras".</summary>
		public static string ComprasEnUnMomento(Frases frases, string claveDelMomento, int cuantas)
		{
			ArgumentNullException.ThrowIfNull(frases);

			return frases.Diga("momento.fila",
				("momento", Momento(frases, claveDelMomento)),
				("compras", frases.Compras(cuantas)));
		}

		/// <summary>COMO SE DICE UNA CADENCIA en una frase que encadena: "compra leche todas las semanas".</summary>
		public static string Cadencia(Frases frases, string clave) => Dicho(frases, "cadencia.", clave);

		/// <summary>La misma cadencia en la forma corta de una pastilla: "1 vez/semana".</summary>
		public static string CadenciaCompacta(Frases frases, string clave) =>
			Dicho(frases, "cadencia.", clave, ".compacta");

		/// <summary>COMO SE DICE UNA CONSTANCIA: "casi nunca falta".</summary>
		public static string Constancia(Frases frases, string clave) => Dicho(frases, "constancia.", clave);

		/// <summary>COMO SE DICE UNA SEVERIDAD: "conviene revisar".</summary>
		public static string Severidad(Frases frases, string clave) => Dicho(frases, "severidad.", clave);

		/// <summary>QUE HACER con un hallazgo de esa severidad. Vacio en el informativo, y eso es una respuesta.</summary>
		public static string QueHacer(Frases frases, string clave)
		{
			ArgumentNullException.ThrowIfNull(frases);
			if (string.IsNullOrWhiteSpace(clave)) return "";

			string llave = "severidad." + clave + ".queHacer";
			return frases.Tiene(llave) ? frases.Diga(llave) : "";
		}

		/// <summary>EL NOMBRE DE UN DEPARTAMENTO como lo lee una persona: "frutas y verduras".</summary>
		public static string TituloDelDepartamento(Frases frases, string nombre) =>
			Dicho(frases, "categoria.", nombre, ".titulo");

		/// <summary>COMO SE MENCIONA UNO de sus productos dentro de una frase: "una fruta o verdura".</summary>
		public static string Mencion(Frases frases, string nombre) =>
			Dicho(frases, "categoria.", nombre, ".mencion");

		/// <summary>COMO SE DICE UN MOMENTO DEL DIA: "mañana".</summary>
		public static string Momento(Frases frases, string clave) => Dicho(frases, "momento.", clave);

		/// <summary>COMO SE DICE UN DIA DE LA SEMANA: "sábado".</summary>
		public static string DiaDeLaSemana(Frases frases, string clave) => Dicho(frases, "dia.", clave);

		/// <summary>
		/// COMO SE DICE UNA PRESENTACION: "unidad", "1 L", "12 u por paquete". El simbolo de la unidad
		/// de medida NO se traduce -- el litro se escribe L en los dos idiomas.
		/// </summary>
		public static string Presentacion(Frases frases, string clave, decimal contenido, string simbolo,
			int piezas)
		{
			ArgumentNullException.ThrowIfNull(frases);

			return frases.Diga("presentacion." + clave,
				("contenido", frases.Cantidad(contenido)),
				("simbolo", simbolo),
				("piezas", Numero(piezas)));
		}

		/// <summary>
		/// COMO SE DICE EL NOMBRE DE UN ESTADO que el dominio eligio de una lista cerrada. El codigo es
		/// identidad y cruza el wire sin tocarse; esto es lo que una persona lee. Un codigo sin dicho se
		/// dice con su propio codigo, que es lo que la pantalla mostraba antes de que existiera la tabla.
		/// </summary>
		private static string Dicho(Frases frases, string prefijo, string clave, string sufijo = "")
		{
			ArgumentNullException.ThrowIfNull(frases);
			if (string.IsNullOrWhiteSpace(clave)) return "";

			string llave = prefijo + clave + sufijo;
			return frases.Tiene(llave) ? frases.Diga(llave) : clave;
		}

		/// <summary>
		/// UN CONTEO, escrito con los mismos digitos en los dos idiomas. Un numero de cosas no lleva
		/// separador de miles: nadie cuenta 1.200 productos en una firma.
		/// </summary>
		private static string Numero(int cuantos) => cuantos.ToString(CultureInfo.InvariantCulture);
	}
}
