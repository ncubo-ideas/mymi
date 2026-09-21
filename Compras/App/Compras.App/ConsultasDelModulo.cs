using System;

namespace Compras.App
{
	// LAS CONSULTAS DEL MODULO, en un solo lugar y sin actor propio.
	//
	// El modulo se carga en dos hospedajes que abren el journal de forma distinta:
	//
	//   escritorio   ComprasSurface abre un actor de SOLO LECTURA contra la carpeta, uno
	//                por consulta, porque la app instalada escribe mientras ese proceso vive.
	//   movil        el modulo YA tiene su actor vivo en el WebView; abrir un segundo seria
	//                pagar el arranque del runtime otra vez por cada pregunta.
	//
	// Lo que NO puede cambiar entre los dos es LA RESPUESTA. Si cada hospedaje armara su
	// propio objeto, el mismo modulo contestaria distinto segun donde corra, y eso es
	// exactamente lo que la conformance "declarado vs real" existe para impedir. Por eso los
	// cuerpos viven aca, toman un ComprasAPI ya abierto, y cada hospedaje pone el suyo.
	public static class ConsultasDelModulo
	{
		// Los nombres de las tools, tal como los declara el manifiesto. Un solo lugar.
		public const string Resumen = "resumen";
		public const string ListaDeCompras = "lista_de_compras";

		public static readonly string[] Declaradas = { Resumen, ListaDeCompras };

		// UNA LINEA DE RESUMEN. El bloque util mas chico, y el que una pantalla pone arriba.
		public static object HacerResumen(ComprasAPI api)
		{
			ArgumentNullException.ThrowIfNull(api);
			return new
			{
				hayDatos = true,
				totalGastado = api.TotalDeCompras(),
				cantidadDeCompras = api.CantidadDeCompras(),
				ultimoEventoLeido = api.UltimoEventoProcesado(),
			};
		}

		// LA LISTA DE COMPRAS. Filas planas: exactamente lo que una ventana pinta como tabla.
		public static object HacerListaDeCompras(ComprasAPI api)
		{
			ArgumentNullException.ThrowIfNull(api);
			return new { hayDatos = true, compras = api.Compras() };
		}

		// Todavia no hay nada registrado. Se dice DONDE se busco, porque "no hay datos" sin
		// decir donde no se puede accionar.
		public static object SinDatos(string donde) => new
		{
			hayDatos = false,
			carpeta = donde,
			detalle = "The app has not recorded anything in this folder yet. Ask the person to open "
				+ "their supermarket app and press Update.",
		};
	}
}
