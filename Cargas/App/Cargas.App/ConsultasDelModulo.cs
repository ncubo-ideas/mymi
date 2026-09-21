using System;

namespace Cargas.App
{
	// LAS CONSULTAS DEL MODULO, en un solo lugar y sin actor propio.
	//
	// El modulo se carga en dos hospedajes que abren el journal de forma distinta:
	//
	//   escritorio / MCP   se abre un actor de SOLO LECTURA contra la carpeta, uno por
	//                      consulta, porque la app instalada escribe mientras ese proceso vive.
	//   movil              el modulo YA tiene su actor vivo en el WebView; abrir un segundo
	//                      seria pagar el arranque del runtime otra vez por cada pregunta.
	//
	// Lo que NO puede cambiar entre los dos es LA RESPUESTA. Si cada hospedaje armara su
	// propio objeto, el mismo modulo contestaria distinto segun donde corra, y eso es
	// exactamente lo que la conformance "declarado vs real" existe para impedir. Por eso los
	// cuerpos viven aca, toman un CargasAPI ya abierto, y cada hospedaje pone el suyo.
	//
	// SON DOS Y SIN PARAMETROS, a proposito. Las tools del servidor MCP piden una placa y
	// contestan derivaciones -autonomia, rendimiento contra el habitual, que dato falta-;
	// esas son para una conversacion. Las de aca son las que una PANTALLA pone arriba sin
	// preguntarle nada a nadie, que es lo que Gnothi necesita para presentar el modulo.
	public static class ConsultasDelModulo
	{
		// Los nombres de las tools, tal como los declara el manifiesto. Un solo lugar.
		public const string Resumen = "resumen";
		public const string ListaDeVehiculos = "lista_de_vehiculos";

		public static readonly string[] Declaradas = { Resumen, ListaDeVehiculos };

		// UNA LINEA DE RESUMEN. El bloque util mas chico, y el que una pantalla pone arriba.
		public static object HacerResumen(CargasAPI api)
		{
			ArgumentNullException.ThrowIfNull(api);
			return new
			{
				hayDatos = true,
				cargas = api.CantidadDeCargas(),
				vehiculos = api.CantidadDeVehiculos(),
				ultimoEventoLeido = api.UltimoEventoProcesado(),
			};
		}

		// LOS VEHICULOS QUE LA BITACORA CONOCE. Es la llave de todo lo demas: sin saber por
		// que placa preguntar, ninguna otra consulta se puede formular.
		public static object HacerListaDeVehiculos(CargasAPI api)
		{
			ArgumentNullException.ThrowIfNull(api);
			return new { hayDatos = true, vehiculos = api.Vehiculos() };
		}

		// Todavia no hay nada registrado. Se dice DONDE se busco, porque "no hay datos" sin
		// decir donde no se puede accionar.
		public static object SinDatos(string donde) => new
		{
			hayDatos = false,
			carpeta = donde,
			detalle = "The app has not recorded anything in this folder yet. Ask the person to open "
				+ "their fuel app and press Update.",
		};
	}
}
