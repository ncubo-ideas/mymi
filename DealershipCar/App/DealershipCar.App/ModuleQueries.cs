using System;

namespace DealershipCar.App
{
	// LAS CONSULTAS DEL MODULO, en un solo lugar y sin actor propio.
	//
	// El modulo se carga en hospedajes que abren el journal de forma distinta:
	//
	//   MCP      se abre un actor de SOLO LECTURA contra la carpeta, uno por consulta, porque
	//            la app instalada escribe mientras ese proceso vive.
	//   movil    el modulo YA tiene su actor vivo en el WebView; abrir un segundo seria pagar
	//            el arranque del runtime otra vez por cada pregunta.
	//
	// Los cuerpos viven aca, toman un DealershipCarAPI ya abierto, y cada hospedaje pone el
	// suyo. Asi el mismo modulo no puede contestar distinto segun donde corra.
	//
	// NO SON LAS MISMAS QUE LAS DEL SERVIDOR MCP, y eso es deliberado. Las del MCP piden una
	// operacion y componen respuestas ricas para una CONVERSACION -por que esta vencida, cuantos
	// aplazamientos quedan-. Estas dos son las que una PANTALLA pone arriba sin preguntarle nada
	// a nadie, que es lo que Gnothi necesita para presentar el modulo. Son dos superficies
	// distintas de la misma app, no dos respuestas a la misma pregunta.
	public static class ModuleQueries
	{
		// Los nombres de las tools, tal como los declara el manifiesto. Un solo lugar.
		public const string Summary = "resumen";
		public const string Freshness = "hasta_donde_leyo";

		public static readonly string[] Declared = { Summary, Freshness };

		// COMO VIENE EL VEHICULO, en una linea. El bloque util mas chico.
		public static object MakeSummary(DealershipCarAPI api)
		{
			ArgumentNullException.ThrowIfNull(api);
			return new
			{
				hasData = true,
				vin = api.ReadVin(),
				anythingDue = api.HasAnythingDue(),
				dueCount = api.CountDue(),
				overdueCount = api.CountOverdue(),
				missedCount = api.MissedCount(),
				evaluated = api.HasBeenEvaluated(),
				lastEventApplied = api.LastEventApplied(),
			};
		}

		// HASTA DONDE LEYO LA APP la narrativa del concesionario, y hasta que instante se
		// evaluo la agenda. Es la unica pregunta con la que quien lee puede saber si lo que
		// acaba de contestar esta al dia: si lastEventApplied sube entre dos llamadas, entraron
		// hechos nuevos. Nadie de afuera lo puede hacer subir; solo la app de la persona.
		public static object MakeFreshness(DealershipCarAPI api)
		{
			ArgumentNullException.ThrowIfNull(api);

			bool evaluated = api.HasBeenEvaluated();
			return new
			{
				hasData = true,
				vin = api.ReadVin(),
				lastEventApplied = api.LastEventApplied(),
				evaluated,

				// LECTURA GUARDADA: preguntar el instante sin haber evaluado tira. Se pregunta
				// la presencia antes del valor, que es el contrato del dominio.
				evaluatedUpTo = evaluated ? api.LastEvaluatedAt() : (DateTime?)null,
			};
		}

		// Todavia no hay nada registrado. Se dice DONDE se busco, porque "no hay datos" sin
		// decir donde no se puede accionar.
		public static object NoData(string folder) => new
		{
			hasData = false,
			folder,
			detail = "The app has not recorded anything in this folder yet. Ask the person to open "
				+ "their dealership app and press Refresh.",
		};
	}
}
