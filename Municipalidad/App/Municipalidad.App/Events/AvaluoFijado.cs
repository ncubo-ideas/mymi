using System;
using System.Text.Json;
using Puppets.Portador;

namespace Municipalidad.App.Events
{
	/// <summary>
	/// La municipalidad fijo el avaluo de una finca de esta persona: de cual finca, desde cuando y por
	/// cuanto. Es el valor con que ella cobra los rubros sobre base imponible, asi que es un hecho de la
	/// institucion — la persona puede declarar el suyo, y ese es otro hecho y de otro dueno.
	/// </summary>
	public sealed class AvaluoFijado : IMensajeDeEvento
	{
		public const string ElTipoDelEvento = "AvaluoFijado";

		private AvaluoFijado() { }

		public long NumeroDelEvento { get; private set; }

		public string NumeroDeFinca { get; private set; }

		public DateTime FechaDelAvaluo { get; private set; }

		public decimal ValorAvaluado { get; private set; }

		long IMensajeDeEvento.Evento { get { return NumeroDelEvento; } }

		string IMensajeDeEvento.Tipo { get { return ElTipoDelEvento; } }

		/// <summary>
		/// Lee la senal y contesta el hecho, o null para DESCARTARLA. Se descarta lo que no se puede
		/// decir: otro tipo, un numero de evento que no avanza, un payload vacio o que no es JSON, un
		/// numero de finca en blanco, un instante ausente o ilegible, y un monto ausente o que no es un
		/// numero. Que la finca este, que el monto sea positivo y que no haya ya un avaluo de esa fecha
		/// son preguntas sobre la CUENTA y las contesta el Check, que tampoco deja entrada al rechazar.
		/// </summary>
		public static AvaluoFijado Interpret(SenalDeEvento senal)
		{
			if (senal.Tipo != ElTipoDelEvento) return null;
			if (senal.Numero <= 0) return null;
			if (senal.Contenido.Length == 0) return null;

			try
			{
				using (JsonDocument documento = JsonDocument.Parse(senal.Contenido))
				{
					JsonElement raiz = documento.RootElement;

					string numeroDeFinca = Payload.Texto(raiz, "numeroDeFinca");
					if (numeroDeFinca.Trim().Length == 0) return null;

					DateTime fechaDelAvaluo;
					if (!Payload.Instante(raiz, "fechaDelAvaluo", out fechaDelAvaluo)) return null;

					decimal valorAvaluado;
					if (!Payload.Monto(raiz, "valor", out valorAvaluado)) return null;

					return new AvaluoFijado
					{
						NumeroDelEvento = senal.Numero,
						NumeroDeFinca = numeroDeFinca,
						FechaDelAvaluo = fechaDelAvaluo,
						ValorAvaluado = valorAvaluado,
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
