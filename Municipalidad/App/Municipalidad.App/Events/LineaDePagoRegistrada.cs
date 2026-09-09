using System.Text.Json;
using Puppets.Portador;

namespace Municipalidad.App.Events
{
	/// <summary>
	/// Una linea del recibo: de que rubro, de que periodo, cuanto y la mora que la municipalidad cobro.
	/// Es UNA linea y no la lista entera, asi que el recibo llega como su encabezado mas N de estas.
	/// </summary>
	public sealed class LineaDePagoRegistrada : IMensajeDeEvento
	{
		public const string ElTipoDelEvento = "LineaDePagoRegistrada";

		private LineaDePagoRegistrada() { }

		public long NumeroDelEvento { get; private set; }

		public string NumeroDeRecibo { get; private set; }

		public string NombreDelRubro { get; private set; }

		public string PeriodoComoDiceElRecibo { get; private set; }

		public decimal MontoDeLaLinea { get; private set; }

		public decimal InteresesDeLaLinea { get; private set; }

		long IMensajeDeEvento.Evento { get { return NumeroDelEvento; } }

		string IMensajeDeEvento.Tipo { get { return ElTipoDelEvento; } }

		/// <summary>
		/// Lee la senal y contesta el hecho, o null para DESCARTARLA. Se descarta lo que no se puede
		/// decir: otro tipo, un numero de evento que no avanza, un payload vacio o que no es JSON, un
		/// recibo o un rubro en blanco, un periodo que no esta en el alfabeto del recibo, y un monto o
		/// una mora ausentes, que no son numeros, o negativos. La mora se exige PRESENTE aunque valga
		/// cero: cero es una linea que se pago a tiempo. Que el encabezado de ese recibo haya llegado,
		/// que el rubro exista y que ese periodo ya estuviera pagado son preguntas sobre la CUENTA y las
		/// contesta el Check, que tampoco deja entrada al rechazar.
		/// </summary>
		public static LineaDePagoRegistrada Interpret(SenalDeEvento senal)
		{
			if (senal.Tipo != ElTipoDelEvento) return null;
			if (senal.Numero <= 0) return null;
			if (senal.Contenido.Length == 0) return null;

			try
			{
				using (JsonDocument documento = JsonDocument.Parse(senal.Contenido))
				{
					JsonElement raiz = documento.RootElement;

					string numeroDeRecibo = Payload.Texto(raiz, "numeroDeRecibo");
					if (numeroDeRecibo.Trim().Length == 0) return null;

					string nombreDelRubro = Payload.Texto(raiz, "nombreDelRubro");
					if (nombreDelRubro.Trim().Length == 0) return null;

					string periodo = Payload.Texto(raiz, "periodo");
					if (!MunicipalidadAPI.DiceUnPeriodoComoLoEscribeElRecibo(periodo, out _, out _))
						return null;

					decimal monto;
					if (!Payload.Monto(raiz, "monto", out monto)) return null;
					if (monto < 0) return null;

					decimal intereses;
					if (!Payload.Monto(raiz, "intereses", out intereses)) return null;
					if (intereses < 0) return null;

					return new LineaDePagoRegistrada
					{
						NumeroDelEvento = senal.Numero,
						NumeroDeRecibo = numeroDeRecibo,
						NombreDelRubro = nombreDelRubro,
						PeriodoComoDiceElRecibo = periodo,
						MontoDeLaLinea = monto,
						InteresesDeLaLinea = intereses,
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
