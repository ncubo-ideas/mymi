using System;

namespace Municipalidad.App
{
	/// <summary>
	/// Lo que la app propone apartar por mes para un anio: la proyeccion del anio, el monto mensual que
	/// sale de ella, y LA FRASE con que el dominio lo dice. Un DTO con forma de wire hecho de
	/// primitivos, nunca un portador del dominio.
	/// <para>
	/// Es la mitad GENERAL del faltante de reserva, y se puede pedir sin saber nada de lo que la
	/// persona adopto: por eso vive en su propia lectura y no solo adentro de la respuesta compuesta.
	/// </para>
	/// </summary>
	public sealed class RegistroDeLaReservaPropuesta
	{
		private RegistroDeLaReservaPropuesta(
			int anio, decimal proyeccion, decimal propuestoPorMes, string dice)
		{
			Anio = anio;
			Proyeccion = proyeccion;
			PropuestoPorMes = propuestoPorMes;
			Dice = dice;
		}

		/// <summary>Para que anio es esta propuesta.</summary>
		public int Anio { get; }

		/// <summary>Lo que la app proyecta que ese anio va a costar entero.</summary>
		public decimal Proyeccion { get; }

		/// <summary>La proyeccion del anio entre doce, que es lo que la app propone apartar por mes.</summary>
		public decimal PropuestoPorMes { get; }

		/// <summary>
		/// La frase que una persona lee, compuesta por el dominio. Dice que es una PROPUESTA de la app
		/// y no un monto que la municipalidad haya puesto al cobro, que es la mitad que se pierde si
		/// sale el numero solo.
		/// </summary>
		public string Dice { get; }

		/// <summary>Arma el registro y refusa una frase en blanco.</summary>
		internal static RegistroDeLaReservaPropuesta Of(
			int anio, decimal proyeccion, decimal propuestoPorMes, string dice)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(dice);

			return new RegistroDeLaReservaPropuesta(anio, proyeccion, propuestoPorMes, dice);
		}
	}
}
