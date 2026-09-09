using System;
using System.Text.Json;
using Puppets.Portador;

namespace Municipalidad.App.Events
{
	/// <summary>
	/// La municipalidad puso al cobro un periodo de un rubro: cuanto y desde cuando. El periodo viaja
	/// como lo escribe el recibo -- AAAA.NN --, porque ese alfabeto es del portal y quien lo resuelve
	/// es la puerta.
	/// </summary>
	public sealed class CobroPuesto : IMensajeDeEvento
	{
		public const string ElTipoDelEvento = "CobroPuesto";

		private CobroPuesto() { }

		public long NumeroDelEvento { get; private set; }

		public string NombreDelRubro { get; private set; }

		public string PeriodoComoDiceElRecibo { get; private set; }

		public decimal MontoAlCobro { get; private set; }

		public DateTime PuestoEl { get; private set; }

		long IMensajeDeEvento.Evento { get { return NumeroDelEvento; } }

		string IMensajeDeEvento.Tipo { get { return ElTipoDelEvento; } }

		/// <summary>
		/// Lee la senal y contesta el hecho, o null para DESCARTARLA. Se descarta lo que no se puede
		/// decir: otro tipo, un numero de evento que no avanza, un payload vacio o que no es JSON, un
		/// rubro en blanco, un periodo que no esta en el alfabeto del recibo, un monto ausente o que no
		/// es un numero, un monto negativo, y un instante ausente o ilegible. El periodo se descarta aca
		/// y no adentro del comando porque la puerta lo parte ANTES del perform y reventaria el bloque de
		/// refresh entero. Que el rubro exista, que su cadencia lea ese periodo, que el monto sea
		/// positivo y que el cobro ya estuviera puesto son preguntas sobre la CUENTA y las contesta el
		/// Check, que tampoco deja entrada al rechazar.
		/// </summary>
		public static CobroPuesto Interpret(SenalDeEvento senal)
		{
			if (senal.Tipo != ElTipoDelEvento) return null;
			if (senal.Numero <= 0) return null;
			if (senal.Contenido.Length == 0) return null;

			try
			{
				using (JsonDocument documento = JsonDocument.Parse(senal.Contenido))
				{
					JsonElement raiz = documento.RootElement;

					string nombreDelRubro = Payload.Texto(raiz, "nombreDelRubro");
					if (nombreDelRubro.Trim().Length == 0) return null;

					string periodo = Payload.Texto(raiz, "periodo");
					if (!MunicipalidadAPI.DiceUnPeriodoComoLoEscribeElRecibo(periodo, out _, out _))
						return null;

					decimal montoAlCobro;
					if (!Payload.Monto(raiz, "monto", out montoAlCobro)) return null;
					if (montoAlCobro < 0) return null;

					DateTime puestoEl;
					if (!Payload.Instante(raiz, "puestoEl", out puestoEl)) return null;

					return new CobroPuesto
					{
						NumeroDelEvento = senal.Numero,
						NombreDelRubro = nombreDelRubro,
						PeriodoComoDiceElRecibo = periodo,
						MontoAlCobro = montoAlCobro,
						PuestoEl = puestoEl,
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
