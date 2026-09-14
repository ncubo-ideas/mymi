using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Municipalidad.App.Vistas
{
	/// <summary>
	/// Cuanto apartar por mes: lo que la app PROPONE y lo que la persona ADOPTO, leidos en un momento.
	/// Son dos mitades de dos duenos y cada una se lee sola: tener una no es condicion para leer la otra.
	/// </summary>
	public sealed class LaReservaDeEsteAnio
	{
		private LaReservaDeEsteAnio() { }

		/// <summary>El dia con que se contesto.</summary>
		public string PreguntadoEl { get; private init; } = "";

		/// <summary>El anio que se proyecta, que es el del dia con que se pregunto.</summary>
		public int Anio { get; private init; }

		/// <summary>Cuanto puede quedar lo adoptado por debajo de lo propuesto antes de avisar.</summary>
		public decimal UmbralConQueSeAvisa { get; private init; }

		/// <summary>Si hay al menos un pago del que proyectar. En false no hay propuesta que dar.</summary>
		public bool SePuedeProponer { get; private init; }

		public decimal ProyeccionDelAnio { get; private init; }

		public decimal PropuestoPorMes { get; private init; }

		/// <summary>La frase de la propuesta, o la que dice por que no hay de donde proyectar.</summary>
		public string DiceLaPropuesta { get; private init; } = "";

		/// <summary>Si la persona adopto una reserva. En false no hay con que comparar la propuesta.</summary>
		public bool HayReservaAdoptada { get; private init; }

		public decimal AdoptadoPorMes { get; private init; }

		public decimal Faltante { get; private init; }

		public decimal ProporcionDelFaltante { get; private init; }

		public decimal VenceEsteMes { get; private init; }

		/// <summary>'ninguna', 'aviso' o 'urgente'. Urgente exige las dos cosas a la vez.</summary>
		public string Urgencia { get; private init; } = "";

		/// <summary>La frase del faltante, o la que dice por que todavia no hay faltante que ver.</summary>
		public string DiceElFaltante { get; private init; } = "";

		/// <summary>Las dos mitades leidas en un momento, cada una detras de su propio par.</summary>
		public static LaReservaDeEsteAnio LeerPor(
			MunicipalidadAPI api, DateTime fechaDeCorte, string diaDicho)
		{
			ArgumentNullException.ThrowIfNull(api);

			bool sePuedeProponer = api.SePuedeProyectar();
			bool sePuedeVerElFaltante = api.SePuedeVerElFaltanteDeReserva();

			decimal proyeccion = 0m;
			decimal propuesto = 0m;
			string diceLaPropuesta = SinUnPagoNoHayDeDondeProyectar;

			if (sePuedeProponer)
			{
				RegistroDeLaReservaPropuesta propuesta = api.ReservaPropuestaPara(fechaDeCorte.Year);

				proyeccion = propuesta.Proyeccion;
				propuesto = propuesta.PropuestoPorMes;
				diceLaPropuesta = propuesta.Dice;
			}

			decimal adoptado = 0m;
			decimal faltante = 0m;
			decimal proporcion = 0m;
			decimal venceEsteMes = 0m;
			string urgencia = "";
			string diceElFaltante =
				sePuedeProponer ? TodaviaNoHaAdoptadoNinguna : SinProyeccionNoHayContraQueMedir;

			if (sePuedeVerElFaltante)
			{
				RegistroDelFaltanteDeReserva elFaltante =
					api.FaltanteDeReservaAl(fechaDeCorte, LosJuiciosSinFirmar.UmbralDeReserva);

				adoptado = elFaltante.Declarado;
				faltante = elFaltante.Faltante;
				proporcion = elFaltante.Proporcion;
				venceEsteMes = elFaltante.VenceEsteMes;
				urgencia = elFaltante.Urgencia;
				diceElFaltante = elFaltante.Dice;
			}

			return new LaReservaDeEsteAnio
			{
				PreguntadoEl = diaDicho,
				Anio = fechaDeCorte.Year,
				UmbralConQueSeAvisa = LosJuiciosSinFirmar.UmbralDeReserva,

				SePuedeProponer = sePuedeProponer,
				ProyeccionDelAnio = proyeccion,
				PropuestoPorMes = propuesto,
				DiceLaPropuesta = diceLaPropuesta,

				HayReservaAdoptada = sePuedeVerElFaltante,
				AdoptadoPorMes = adoptado,
				Faltante = faltante,
				ProporcionDelFaltante = proporcion,
				VenceEsteMes = venceEsteMes,
				Urgencia = urgencia,
				DiceElFaltante = diceElFaltante,
			};
		}

		private const string SinUnPagoNoHayDeDondeProyectar =
			"Tu app todavía no ha visto ningún comprobante, así que no hay de dónde proyectar lo que vas "
			+ "a pagar este año. En cuanto entre el primer pago, esta propuesta aparece sola.";

		private const string TodaviaNoHaAdoptadoNinguna =
			"Todavía no adoptaste una reserva, así que no hay con qué comparar la propuesta. Adoptar una "
			+ "es tuyo y no de la municipalidad: es cuánto decidís apartar cada mes.";

		private const string SinProyeccionNoHayContraQueMedir =
			"Sin un pago del que proyectar y sin una reserva adoptada, no hay dos mitades que comparar. "
			+ "Las dos entran solas: la propuesta con el primer comprobante, la adoptada cuando la "
			+ "pongás.";
	}
}
