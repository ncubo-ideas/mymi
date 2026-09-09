using System;

namespace Municipalidad.Dominio
{
	/// <summary>
	/// Lo que la app propone apartar por mes: la proyeccion del anio dividida en doce. ES UN NUMERO QUE
	/// LA APP CALCULO y no una intencion de la persona, asi que nunca se journaliza: se recalcula cada
	/// vez. Lo que la persona adopta, ajustado o no, es otra cosa en otra casilla.
	/// <para>
	/// La superficie la muestra como propuesta; solo lo adoptado se muestra como suyo.
	/// </para>
	/// </summary>
	internal sealed class ReservaPropuesta
	{
		internal int Anio { get; }
		internal decimal ProyeccionDelAnio { get; }

		internal ReservaPropuesta(int anio, decimal proyeccionDelAnio)
		{
			if (proyeccionDelAnio <= 0)
				throw new ArgumentOutOfRangeException(nameof(proyeccionDelAnio), proyeccionDelAnio,
					"Sin proyeccion no hay que proponer. Preguntar SePuedeProyectar antes.");
			Anio = anio;
			ProyeccionDelAnio = proyeccionDelAnio;
		}

		internal decimal MontoMensual { get { return Math.Round(ProyeccionDelAnio / 12m, 2); } }

		/// <summary>
		/// La frase que una persona lee sobre esta propuesta: para que anio, la proyeccion de la que
		/// sale, y lo que propone apartar por mes. Dice que es una PROPUESTA de la app y no un monto
		/// que la municipalidad haya puesto al cobro, que es la mitad que se pierde si sale sola.
		/// </summary>
		internal string Dice()
		{
			return "Para " + Anio + " esta app proyecta " + Dichos.Monto(ProyeccionDelAnio)
				+ " en el año, así que propone apartar " + Dichos.Monto(MontoMensual)
				+ " por mes. Es una proyección de esta app sobre lo que ya pagaste, no un monto que la "
				+ "municipalidad haya puesto al cobro.";
		}
	}
}
