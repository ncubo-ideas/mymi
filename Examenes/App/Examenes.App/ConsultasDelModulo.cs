using System;
using System.IO;

namespace Examenes.App
{
	// LAS CONSULTAS DEL MODULO, en un solo lugar y sin actor propio.
	//
	// El modulo se carga en dos hospedajes que abren el journal de forma distinta:
	//
	//   escritorio   ExamenesSurface abre un actor de SOLO LECTURA contra la carpeta, uno
	//                por consulta, porque la app instalada escribe mientras ese proceso vive.
	//   movil        el modulo YA tiene su actor vivo en el WebView; abrir un segundo seria
	//                pagar el arranque del runtime otra vez por cada pregunta.
	//
	// Lo que NO puede cambiar entre los dos es LA RESPUESTA. Si cada hospedaje armara su
	// propio objeto, el mismo modulo contestaria distinto segun donde corra, y eso es
	// exactamente lo que la conformance "declarado vs real" existe para impedir. Por eso los
	// cuerpos viven aca, toman un ExamenesAPI ya abierto, y cada hospedaje pone el suyo.
	public static class ConsultasDelModulo
	{
		// Los nombres de las tools, tal como los declara el manifiesto. Un solo lugar.
		public const string Resumen = "resumen";
		public const string ListaDeExamenes = "lista_de_examenes";

		public static readonly string[] Declaradas = { Resumen, ListaDeExamenes };

		// UNA LINEA DE RESUMEN. El bloque util mas chico, y el que una pantalla pone arriba.
		public static object HacerResumen(ExamenesAPI api)
		{
			ArgumentNullException.ThrowIfNull(api);
			return new
			{
				hayDatos = true,
				examenes = api.CantidadDeExamenes(),
				analitosFueraDeRango = api.AnalitosFueraDeRango(),
				ultimoEventoLeido = api.UltimoEventoProcesado(),
			};
		}

		// LA LISTA DE EXAMENES. Filas planas: exactamente lo que una ventana pinta como tabla.
		public static object HacerListaDeExamenes(ExamenesAPI api)
		{
			ArgumentNullException.ThrowIfNull(api);
			return new { hayDatos = true, examenes = api.Examenes() };
		}

		// Todavia no hay nada registrado. Se dice DONDE se busco, porque "no hay datos" sin
		// decir donde no se puede accionar.
		public static object SinDatos(string donde) => new
		{
			hayDatos = false,
			carpeta = donde,
			detalle = "The app has not recorded anything in this folder yet. Ask the person to open "
				+ "their exams app and press Update.",
		};
	}
}
