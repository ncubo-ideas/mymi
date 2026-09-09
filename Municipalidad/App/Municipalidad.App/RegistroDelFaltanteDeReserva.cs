using System;

namespace Municipalidad.App
{
	/// <summary>
	/// Lo que le falta a la reserva que la persona adopto para cubrir lo que la app proyecta: las dos
	/// mitades, el faltante, lo que vence en el mes, cuanto apura, y LA FRASE con que el dominio lo
	/// dice. Un DTO con forma de wire hecho de primitivos, nunca un portador del dominio.
	/// <para>
	/// LAS DOS MITADES VIAJAN Y NINGUNA VIAJA SOLA COMO NUMERO PELADO: `Propuesto` lo calcula la app y
	/// `Declarado` lo puso la persona, y la frase dice de quien es cada uno. La mitad general -- lo
	/// propuesto, sin saber nada de lo adoptado -- tambien se puede pedir aparte, en
	/// <see cref="RegistroDeLaReservaPropuesta"/>, para que tener una no sea condicion de leer la otra.
	/// </para>
	/// </summary>
	public sealed class RegistroDelFaltanteDeReserva
	{
		private RegistroDelFaltanteDeReserva(
			decimal propuesto, decimal declarado, decimal faltante, decimal proporcion,
			decimal venceEsteMes, string urgencia, string dice)
		{
			Propuesto = propuesto;
			Declarado = declarado;
			Faltante = faltante;
			Proporcion = proporcion;
			VenceEsteMes = venceEsteMes;
			Urgencia = urgencia;
			Dice = dice;
		}

		/// <summary>Lo que la APP propone apartar por mes.</summary>
		public decimal Propuesto { get; }

		/// <summary>Lo que la PERSONA adopto apartar por mes.</summary>
		public decimal Declarado { get; }

		/// <summary>Lo que le falta a lo adoptado para cubrir lo propuesto. Cero si lo cubre.</summary>
		public decimal Faltante { get; }

		/// <summary>El faltante como proporcion de lo propuesto: 0,1 son diez por ciento.</summary>
		public decimal Proporcion { get; }

		/// <summary>Lo que vence en el mes de la fecha preguntada, que es lo unico que puede volverlo urgente.</summary>
		public decimal VenceEsteMes { get; }

		/// <summary>Cuanto apura, por el nombre del nivel.</summary>
		public string Urgencia { get; }

		/// <summary>
		/// La frase que una persona lee, compuesta por el dominio, que nombra las dos mitades y de
		/// quien es cada una.
		/// </summary>
		public string Dice { get; }

		/// <summary>Arma el registro y refusa una frase o una urgencia en blanco.</summary>
		internal static RegistroDelFaltanteDeReserva Of(
			decimal propuesto, decimal declarado, decimal faltante, decimal proporcion,
			decimal venceEsteMes, string urgencia, string dice)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(urgencia);
			ArgumentException.ThrowIfNullOrWhiteSpace(dice);

			return new RegistroDelFaltanteDeReserva(
				propuesto, declarado, faltante, proporcion, venceEsteMes, urgencia, dice);
		}
	}
}
