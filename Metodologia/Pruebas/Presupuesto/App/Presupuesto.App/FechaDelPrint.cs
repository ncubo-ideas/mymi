using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Presupuesto.App
{
	// CÓMO SE LEE UNA FECHA QUE SALIÓ DE UN `print`.
	//
	// MEDIDO: el `print` del actor serializa las fechas en **`MM/dd/yyyy`**, y eso
	// **`System.Text.Json` no lo puede parsear** — espera ISO 8601. Cualquier superficie que
	// deserialice la salida de una consulta revienta con:
	//
	//     JsonException: The JSON value could not be converted to System.DateTime.
	//      ---> FormatException: The JSON value is not in a supported DateTime format.
	//
	// El error no menciona el formato ni de dónde vino, así que manda a revisar el DTO.
	//
	// Vive acá y no en la superficie porque es una propiedad de **lo que estas consultas
	// emiten**: quien consuma `PresupuestoAPI` necesita esto, sea el servidor MCP, la
	// carátula del browser o una prueba. Escribirlo una vez es lo que evita que cada
	// superficie descubra el mismo error por su cuenta.
	//
	// Es la tercera cara del mismo problema de fechas, y vale verlas juntas:
	//   1. Para una persona, `07/05/2026` es ambiguo — puede leerse 7 de mayo.
	//   2. Para el dominio, formatear una fecha contradecía el campo estructurado al lado.
	//   3. Para una máquina, ese formato directamente **no parsea**.
	public sealed class FechaDelPrint : JsonConverter<DateTime>
	{
		// Los dos que emite el print: con hora y sin hora.
		private static readonly string[] Formatos =
		{
			"MM/dd/yyyy HH:mm:ss",
			"MM/dd/yyyy",
		};

		public override DateTime Read(ref Utf8JsonReader reader, Type tipo, JsonSerializerOptions opciones)
		{
			string texto = reader.GetString();

			if (string.IsNullOrWhiteSpace(texto)) return default;

			if (DateTime.TryParseExact(texto, Formatos, CultureInfo.InvariantCulture,
				DateTimeStyles.None, out DateTime fecha))
				return fecha;

			// El ISO también, por si la superficie mezcla fechas propias con las del print.
			if (DateTime.TryParse(texto, CultureInfo.InvariantCulture,
				DateTimeStyles.RoundtripKind, out fecha))
				return fecha;

			throw new JsonException(
				$"'{texto}' no es una fecha que emita el print. Se esperaba MM/dd/yyyy, "
				+ "opcionalmente con HH:mm:ss.");
		}

		// De salida, ISO. Lo que esta superficie le entrega a quien la consuma NO tiene por
		// qué heredar la ambigüedad del formato de entrada.
		public override void Write(Utf8JsonWriter writer, DateTime valor, JsonSerializerOptions opciones) =>
			writer.WriteStringValue(valor.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
	}
}
