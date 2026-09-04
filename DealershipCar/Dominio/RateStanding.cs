using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// En que situacion esta el ritmo de uso de un periodo. Conjunto cerrado de cinco, porque son
	/// cinco respuestas distinguibles y solo una de ellas trae numeros. Se elige siempre adentro --
	/// es una RESPUESTA, nunca una entrada -- y sale solo como proyeccion escalar de su nombre.
	/// <para>
	/// Ninguno de los cinco quiere decir "no lo se": lo que no se puede decir se dice nombrando la
	/// razon, que es para lo que hay cuatro suspensiones en vez de una.
	/// </para>
	/// </summary>
	internal sealed class RateStanding
	{
		/// <summary>El periodo tiene dos extremos comparables y el ritmo se puede decir.</summary>
		internal static readonly RateStanding Measured =
			new RateStanding("Measured", "Se puede decir");

		/// <summary>
		/// El periodo tiene lecturas en mas de una unidad de distancia, y las unidades no se
		/// convierten. La resta entre dos escalas distintas no es una distancia.
		/// </summary>
		internal static readonly RateStanding SuspendedByAUnitMismatch =
			new RateStanding("SuspendedByAUnitMismatch", "Unidades distintas");

		/// <summary>
		/// El periodo tiene una sola lectura usable. Un ritmo necesita dos puntos, y un punto es un
		/// todavia no: no es un cero.
		/// </summary>
		internal static readonly RateStanding SuspendedByASingleReading =
			new RateStanding("SuspendedByASingleReading", "Falta una segunda lectura");

		/// <summary>El periodo no tiene ninguna lectura del odometro.</summary>
		internal static readonly RateStanding SuspendedByAnAbsentReading =
			new RateStanding("SuspendedByAnAbsentReading", "Sin lecturas en el periodo");

		/// <summary>
		/// Los dos extremos del periodo se observaron en el mismo segundo. El journal mide en
		/// segundos enteros, asi que no hay tiempo transcurrido por el que dividir.
		/// </summary>
		internal static readonly RateStanding SuspendedByAZeroLengthSpan =
			new RateStanding("SuspendedByAZeroLengthSpan", "Sin tiempo transcurrido");

		/// <summary>El nombre. Es lo unico que cruza hacia afuera; el objeto no se entrega.</summary>
		internal string Name { get; }

		/// <summary>
		/// Como se dice esta situacion en la frase que alguien lee. NO es lo que cruza la puerta:
		/// eso sigue siendo <see cref="Name"/>.
		/// </summary>
		internal string HowItIsSaid { get; }

		private RateStanding(string name, string howItIsSaid)
		{
			Name = name;
			HowItIsSaid = howItIsSaid;
		}
	}
}
