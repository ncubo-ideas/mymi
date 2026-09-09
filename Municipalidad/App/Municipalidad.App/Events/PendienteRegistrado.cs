using System;
using System.Text.Json;
using Puppets.Portador;

namespace Municipalidad.App.Events
{
	/// <summary>
	/// Un pendiente que una consulta destapo: de que dia es la consulta, de que rubro y periodo es el
	/// pendiente, cuanto y los intereses que la municipalidad le cuenta. Es UNO, y llega detras de la
	/// consulta que lo destapo.
	/// </summary>
	public sealed class PendienteRegistrado : IMensajeDeEvento
	{
		public const string ElTipoDelEvento = "PendienteRegistrado";

		private PendienteRegistrado() { }

		public long NumeroDelEvento { get; private set; }

		public DateTime FechaDeLaConsulta { get; private set; }

		public string NombreDelRubro { get; private set; }

		public string PeriodoComoDiceElRecibo { get; private set; }

		public decimal MontoPendiente { get; private set; }

		public decimal InteresesPendientes { get; private set; }

		long IMensajeDeEvento.Evento { get { return NumeroDelEvento; } }

		string IMensajeDeEvento.Tipo { get { return ElTipoDelEvento; } }

		/// <summary>
		/// Lee la senal y contesta el hecho, o null para DESCARTARLA. Se descarta lo que no se puede
		/// decir: otro tipo, un numero de evento que no avanza, un payload vacio o que no es JSON, un dia
		/// de consulta ausente o ilegible, un rubro en blanco, un periodo que no esta en el alfabeto del
		/// recibo, y un monto o unos intereses ausentes, que no son numeros, o negativos. Que la consulta
		/// de ese dia haya llegado, que el rubro exista y que ese pendiente ya estuviera son preguntas
		/// sobre la CUENTA y las contesta el Check, que tampoco deja entrada al rechazar.
		/// </summary>
		public static PendienteRegistrado Interpret(SenalDeEvento senal)
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

					return new PendienteRegistrado
					{
						NumeroDelEvento = senal.Numero,
						FechaDeLaConsulta = fechaDeLaConsulta,
						NombreDelRubro = nombreDelRubro,
						PeriodoComoDiceElRecibo = periodo,
						MontoPendiente = monto,
						InteresesPendientes = intereses,
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
