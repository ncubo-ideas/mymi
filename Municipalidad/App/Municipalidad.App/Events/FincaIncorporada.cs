using System.Text.Json;
using Puppets.Portador;

namespace Municipalidad.App.Events
{
	/// <summary>
	/// La municipalidad incorporo una finca a la cuenta de esta persona: el numero con que la nombra.
	/// El VALOR de esa finca no viaja aca -- lo aporta la persona y no la institucion.
	/// </summary>
	public sealed class FincaIncorporada : IMensajeDeEvento
	{
		public const string ElTipoDelEvento = "FincaIncorporada";

		private FincaIncorporada() { }

		public long NumeroDelEvento { get; private set; }

		public string NumeroDeFinca { get; private set; }

		long IMensajeDeEvento.Evento { get { return NumeroDelEvento; } }

		string IMensajeDeEvento.Tipo { get { return ElTipoDelEvento; } }

		/// <summary>
		/// Lee la senal y contesta el hecho, o null para DESCARTARLA. Se descarta lo que no se puede
		/// decir: otro tipo, un numero de evento que no avanza, un payload vacio o que no es JSON, y un
		/// numero de finca en blanco. Que esa finca ya este es una pregunta sobre la CUENTA y la contesta
		/// el Check, que tampoco deja entrada al rechazar.
		/// </summary>
		public static FincaIncorporada Interpret(SenalDeEvento senal)
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

					return new FincaIncorporada
					{
						NumeroDelEvento = senal.Numero,
						NumeroDeFinca = numeroDeFinca,
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
