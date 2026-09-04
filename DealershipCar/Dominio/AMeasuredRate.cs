using System;
using System.Globalization;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El ritmo que SI se pudo decir: los dos extremos del periodo, la distancia entre ellos, el
	/// tiempo transcurrido y la distancia por dia.
	/// <para>
	/// La frase nombra los dos extremos con sus instantes y su unidad, porque un ritmo cuyo
	/// intervalo no se ve no lo puede revisar quien lo lee.
	/// </para>
	/// </summary>
	internal sealed class AMeasuredRate : RateAnswer
	{
		private readonly ASeriesRow _from;
		private readonly ASeriesRow _to;

		/// <summary>Se llega por <see cref="RateAnswer.Measured"/>, que la compone el tramo.</summary>
		internal AMeasuredRate(ASeriesRow from, ASeriesRow to)
		{
			if (from == null || to == null)
				throw new DomainException(
					"Un ritmo necesita sus dos extremos: uno solo es una lectura y no tiene ritmo.");

			_from = from;
			_to = to;
		}

		/// <summary>Si se puede decir. En esta variante, siempre.</summary>
		internal override bool CanBeSaid()
		{
			return true;
		}

		/// <summary>La situacion: medido.</summary>
		internal override RateStanding Standing()
		{
			return RateStanding.Measured;
		}

		/// <summary>
		/// La distancia recorrida por dia, redondeada al entero mas cercano y alejandose del cero.
		/// Es el UNICO numero que este slice inventa, y sale dicho: la distancia y el tiempo se
		/// dicen exactos antes que el.
		/// </summary>
		internal override long DistancePerDay()
		{
			return TheWholeDistancePerDay();
		}

		/// <summary>
		/// Como se dice el cambio de este ritmo respecto a ese otro. El porcentaje se calcula sobre
		/// las distancias por dia YA REDONDEADAS, que son las que la frase dice: sacarlo de los
		/// ritmos exactos lo dejaria en desacuerdo con los dos numeros impresos al lado.
		/// </summary>
		internal override string HowItComparesTo(RateAnswer other)
		{
			if (other == null)
				throw new DomainException(
					"No hay con que comparar este ritmo: llego una respuesta ausente.");

			if (!other.CanBeSaid())
				throw new DomainException(
					"El otro periodo no tiene ritmo que decir, asi que no se puede comparar: "
					+ "pregunte CanBeSaid en los dos antes.");

			long theirs = other.DistancePerDay();

			if (theirs == 0)
				return "no se puede decir como porcentaje, porque en el otro periodo el vehículo "
					+ "no se movió";

			long mine = DistancePerDay();

			if (mine == theirs) return "el mismo ritmo";

			decimal proportion = ((decimal)mine - theirs) / theirs * 100m;
			long said = (long)Math.Round(Math.Abs(proportion), MidpointRounding.AwayFromZero);

			return mine > theirs ? "un " + said + "% más" : "un " + said + "% menos";
		}

		/// <summary>Cuando se observo la primera de las dos lecturas del tramo.</summary>
		internal override DateTime TheInstantOfItsFirstEnd()
		{
			return _from.WhenItWasObserved();
		}

		/// <summary>La distancia entre los dos extremos, exacta y sin redondear.</summary>
		internal override int TheDistanceBetweenItsEnds()
		{
			return _to.DistanceFrom(_from);
		}

		/// <summary>El codigo de la unidad que los dos extremos comparten.</summary>
		internal override DistanceUnitCode TheCodeOfItsUnit()
		{
			return _to.TheCodeOfItsUnit();
		}

		/// <summary>
		/// El ritmo entero en una frase: de donde a donde, cuanto y en cuanto tiempo, y la
		/// distancia por dia introducida por "unos", que es lo que dice que ese ultimo numero esta
		/// redondeado.
		/// </summary>
		internal override string Describe()
		{
			string unit = _to.TheNameOfItsUnit();

			return "de " + _from.TheReadingItSays() + " el " + _from.TheInstantItWasObserved()
				+ " a " + _to.TheReadingItSays() + " el " + _to.TheInstantItWasObserved()
				+ ": " + _to.DistanceFrom(_from) + " " + unit
				+ " en " + TheSpanInWords()
				+ ", unos " + TheWholeDistancePerDay() + " " + unit + " por día";
		}

		private long TheWholeDistancePerDay()
		{
			decimal perDay = _to.DistanceFrom(_from) / _to.DaysFrom(_from);

			return (long)Math.Round(perDay, MidpointRounding.AwayFromZero);
		}

		// EL TRAMO SE DICE EN DIAS, Y EN SEGUNDOS CUANDO ES MAS CORTO QUE UN DIA. El corte no es un
		// umbral elegido: el dia es la unidad por la que el ritmo se dice (G1), asi que "mas corto
		// que la unidad" es el unico corte estructural que hay. Decir "en 0 dias" seria falso, y
		// esconder el tramo corto es justo la constante que §6 prohibe.
		private string TheSpanInWords()
		{
			decimal days = _to.DaysFrom(_from);

			if (days < 1m)
			{
				long seconds = _to.SecondsFrom(_from);

				return seconds == 1 ? "1 segundo" : seconds + " segundos";
			}

			if (days == 1m) return "1 día";

			return AsSaid(days) + " días";
		}

		private static string AsSaid(decimal days)
		{
			if (days == decimal.Truncate(days))
				return decimal.Truncate(days).ToString(CultureInfo.InvariantCulture);

			return days.ToString(CultureInfo.InvariantCulture);
		}
	}
}
