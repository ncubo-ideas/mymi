using System;
using System.Text.Json;
using Puppets.Portador;

namespace Municipalidad.App.Events
{
	/// <summary>
	/// La municipalidad registro un recibo pagado: su numero, el dia y el descuento que trae. Es el
	/// ENCABEZADO, y sus lineas llegan una por una detras de el; una linea cuyo encabezado no llego se
	/// rechaza sin dejar entrada y no se reintenta.
	/// </summary>
	public sealed class PagoRegistrado : IMensajeDeEvento
	{
		public const string ElTipoDelEvento = "PagoRegistrado";

		private PagoRegistrado() { }

		public long NumeroDelEvento { get; private set; }

		public string NumeroDeRecibo { get; private set; }

		public DateTime FechaDelPago { get; private set; }

		public decimal DescuentoDelRecibo { get; private set; }

		long IMensajeDeEvento.Evento { get { return NumeroDelEvento; } }

		string IMensajeDeEvento.Tipo { get { return ElTipoDelEvento; } }

		/// <summary>
		/// Lee la senal y contesta el hecho, o null para DESCARTARLA. Se descarta lo que no se puede
		/// decir: otro tipo, un numero de evento que no avanza, un payload vacio o que no es JSON, un
		/// numero de recibo en blanco, un descuento ausente o que no es un numero, un descuento negativo,
		/// y un dia de pago ausente o ilegible. El descuento se exige PRESENTE aunque valga cero: cero es
		/// un recibo sin descuento, que es una afirmacion, y un evento que omite el campo no la esta
		/// haciendo. Que ese recibo ya estuviera registrado es una pregunta sobre la CUENTA y la contesta
		/// el Check, que tampoco deja entrada al rechazar.
		/// </summary>
		public static PagoRegistrado Interpret(SenalDeEvento senal)
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

					decimal descuento;
					if (!Payload.Monto(raiz, "descuento", out descuento)) return null;
					if (descuento < 0) return null;

					DateTime fechaDelPago;
					if (!Payload.Instante(raiz, "fechaDelPago", out fechaDelPago)) return null;

					return new PagoRegistrado
					{
						NumeroDelEvento = senal.Numero,
						NumeroDeRecibo = numeroDeRecibo,
						FechaDelPago = fechaDelPago,
						DescuentoDelRecibo = descuento,
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
