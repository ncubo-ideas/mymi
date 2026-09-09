using System;
using System.Text.Json;
using Puppets.Portador;

namespace Municipalidad.App.Events
{
	/// <summary>
	/// La municipalidad tomo una consulta de la cuenta: el dia de la foto. Sus pendientes llegan uno por
	/// uno detras de ella, porque asi llegan.
	/// </summary>
	public sealed class ConsultaTomada : IMensajeDeEvento
	{
		public const string ElTipoDelEvento = "ConsultaTomada";

		private ConsultaTomada() { }

		public long NumeroDelEvento { get; private set; }

		public DateTime FechaDeLaConsulta { get; private set; }

		long IMensajeDeEvento.Evento { get { return NumeroDelEvento; } }

		string IMensajeDeEvento.Tipo { get { return ElTipoDelEvento; } }

		/// <summary>
		/// Lee la senal y contesta el hecho, o null para DESCARTARLA. Se descarta lo que no se puede
		/// decir: otro tipo, un numero de evento que no avanza, un payload vacio o que no es JSON, y un
		/// dia ausente o ilegible -- una consulta ES su dia, y sin el no hay foto que fechar. Que ya
		/// hubiera una consulta de ese dia es una pregunta sobre la CUENTA y la contesta el Check, que
		/// tampoco deja entrada al rechazar.
		/// </summary>
		public static ConsultaTomada Interpret(SenalDeEvento senal)
		{
			if (senal.Tipo != ElTipoDelEvento) return null;
			if (senal.Numero <= 0) return null;
			if (senal.Contenido.Length == 0) return null;

			try
			{
				using (JsonDocument documento = JsonDocument.Parse(senal.Contenido))
				{
					JsonElement raiz = documento.RootElement;

					DateTime fechaDeLaConsulta;
					if (!Payload.Instante(raiz, "fechaDeLaConsulta", out fechaDeLaConsulta)) return null;

					return new ConsultaTomada
					{
						NumeroDelEvento = senal.Numero,
						FechaDeLaConsulta = fechaDeLaConsulta,
					};
				}
			}
			catch (JsonException)
			{
				return null;
			}
		}
	}
}
