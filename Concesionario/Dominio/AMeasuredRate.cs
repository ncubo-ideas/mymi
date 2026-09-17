using System;
using System.Globalization;

namespace Concesionario.Dominio
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
		/// En cual de las cuatro situaciones cae el cambio de este ritmo respecto a ese otro.
		/// Contra un periodo quieto no hay porcentaje, y eso es una situacion con nombre, no un
		/// cero.
		/// </summary>
		internal override string TheShapeOfItsChangeAgainst(RateAnswer other)
		{
			long theirs = TheirDistancePerDay(other);

			if (theirs == 0) return RateChange.NotAPercentage;

			long mine = TheWholeDistancePerDay();

			if (mine == theirs) return RateChange.TheSameRate;

			return mine > theirs ? RateChange.Higher : RateChange.Lower;
		}

		/// <summary>
		/// Los digitos de ese cambio. El porcentaje se calcula sobre las distancias por dia YA
		/// REDONDEADAS, que son las que la frase dice: sacarlo de los ritmos exactos lo dejaria en
		/// desacuerdo con los dos numeros impresos al lado.
		/// </summary>
		internal override string TheChangeAgainstAsSaid(RateAnswer other)
		{
			long theirs = TheirDistancePerDay(other);

			if (theirs == 0) return "";

			decimal proportion = ((decimal)TheWholeDistancePerDay() - theirs) / theirs * 100m;
			long said = (long)Math.Round(Math.Abs(proportion), MidpointRounding.AwayFromZero);

			return said.ToString(CultureInfo.InvariantCulture);
		}

		private static long TheirDistancePerDay(RateAnswer other)
		{
			if (other == null)
				throw new DomainException(
					"No hay con que comparar este ritmo: llego una respuesta ausente.");

			if (!other.CanBeSaid())
				throw new DomainException(
					"El otro periodo no tiene ritmo que decir, asi que no se puede comparar: "
					+ "pregunte CanBeSaid en los dos antes.");

			return other.DistancePerDay();
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
		/// El ritmo entero: los dos extremos con sus instantes, la distancia entre ellos con su
		/// unidad, el tramo de tiempo y la distancia por dia. Los operandos exactos van con el
		/// ritmo, que es lo que deja revisar la division a quien lo lea.
		/// </summary>
		internal override ARateInAPeriod TheClauseOfItsPeriod()
		{
			return ARateInAPeriod.Measured(
				_from.TheValueItRead(), _from.TheNameOfItsUnit(), _from.TheInstantItWasObserved(),
				_to.TheValueItRead(), _to.TheNameOfItsUnit(), _to.TheInstantItWasObserved(),
				_to.DistanceFrom(_from), _to.TheNameOfItsUnit(),
				TheWholeDistancePerDay().ToString(CultureInfo.InvariantCulture),
				TheShapeOfItsSpan(), TheSpanAsSaid());
		}

		private long TheWholeDistancePerDay()
		{
			decimal perDay = _to.DistanceFrom(_from) / _to.DaysFrom(_from);

			return (long)Math.Round(perDay, MidpointRounding.AwayFromZero);
		}

		/// <summary>
		/// En que unidad se dice el tramo: en dias, y en segundos cuando es mas corto que un dia.
		/// El corte no es un umbral elegido -- el dia es la unidad por la que el ritmo se dice, asi
		/// que "mas corto que la unidad" es el unico corte que hay --, y decir "en 0 dias"
		/// seria falso. El entero exacto tiene su propio codigo en cada una de las dos unidades,
		/// porque el singular no se puede elegir mirando unos digitos.
		/// </summary>
		private string TheShapeOfItsSpan()
		{
			decimal days = _to.DaysFrom(_from);

			if (days >= 1m) return days == 1m ? SpanShape.AWholeDay : SpanShape.InDays;

			return _to.SecondsFrom(_from) == 1 ? SpanShape.AWholeSecond : SpanShape.InSeconds;
		}

		private string TheSpanAsSaid()
		{
			decimal days = _to.DaysFrom(_from);

			if (days == 1m) return "";

			if (days > 1m) return AsSaid(days);

			long seconds = _to.SecondsFrom(_from);

			return seconds == 1 ? "" : seconds.ToString(CultureInfo.InvariantCulture);
		}

		private static string AsSaid(decimal days)
		{
			if (days == decimal.Truncate(days))
				return decimal.Truncate(days).ToString(CultureInfo.InvariantCulture);

			return days.ToString(CultureInfo.InvariantCulture);
		}
	}
}
