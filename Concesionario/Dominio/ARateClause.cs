using System;
using System.Collections.Generic;

namespace Concesionario.Dominio
{
	/// <summary>
	/// LO QUE EL RITMO DE USO TIENE QUE DECIR, sea de un periodo o de dos comparados: como se
	/// pregunto, que tiene cada periodo que decir, y -- con dos -- que pudo decirse del cambio.
	/// <para>
	/// Sale como FORMA mas valores ya tipados y nunca como una frase. Las dos lecturas pasan por
	/// aqui porque la diferencia entre ellas es el MARCO y nunca las cifras: cada periodo dice su
	/// ritmo igual de entero en las dos, y un marco es redaccion.
	/// </para>
	/// <para>
	/// Las lecturas que cada periodo nombra viajan APLANADAS con el indice del periodo al que
	/// pertenecen, porque un periodo nombra dos, una o ninguna segun su situacion y por el wire no
	/// cruza nada ragged.
	/// </para>
	/// <para>
	/// El cambio nunca sale solo y nunca sale sin los dos ritmos al lado: un porcentaje suelto
	/// es la mitad de una respuesta compuesta, y quien lo reciba no puede revisarlo.
	/// </para>
	/// </summary>
	internal sealed class ARateClause
	{
		private const string NoChangeToSay = "";

		private readonly string _asked;
		private readonly string _changeShape;
		private readonly string _change;
		private readonly string[] _shapes;
		private readonly string[] _froms;
		private readonly string[] _tos;
		private readonly string[] _unitsOfThePeriod;
		private readonly string[] _seconds;
		private readonly int[] _distances;
		private readonly string[] _units;
		private readonly string[] _perDays;
		private readonly string[] _spanShapes;
		private readonly string[] _spans;
		private readonly List<int> _readingOf = new List<int>();
		private readonly List<int> _readingValues = new List<int>();
		private readonly List<string> _readingUnits = new List<string>();
		private readonly List<string> _readingInstants = new List<string>();

		private ARateClause(
			string asked, string changeShape, string change, List<ARateInAPeriod> periods)
		{
			_asked = asked;
			_changeShape = changeShape;
			_change = change;
			_shapes = new string[periods.Count];
			_froms = new string[periods.Count];
			_tos = new string[periods.Count];
			_unitsOfThePeriod = new string[periods.Count];
			_seconds = new string[periods.Count];
			_distances = new int[periods.Count];
			_units = new string[periods.Count];
			_perDays = new string[periods.Count];
			_spanShapes = new string[periods.Count];
			_spans = new string[periods.Count];

			for (int period = 0; period < periods.Count; period++) Place(period, periods[period]);
		}

		/// <summary>
		/// La clausula de UN periodo, dicho con su propio ritmo y sin ningun cambio: no hay contra
		/// que compararlo, y eso es una situacion con nombre y no un cero.
		/// </summary>
		internal static ARateClause OfOnePeriod(ARateInAPeriod period)
		{
			return new ARateClause(
				RateAsked.AsOnePeriod, RateChange.NotAsked, NoChangeToSay, TheOne(period));
		}

		/// <summary>
		/// La clausula de DOS periodos, en ese orden, cada uno con su ritmo, y el cambio del
		/// primero respecto al segundo con su propia situacion.
		/// </summary>
		internal static ARateClause OfTwoPeriodsCompared(
			ARateInAPeriod first, ARateInAPeriod second, string changeShape, string change)
		{
			if (string.IsNullOrWhiteSpace(changeShape))
				throw new DomainException(
					"Se compuso una comparacion de dos periodos sin decir que paso con el cambio. "
					+ "Un cambio sin situacion no se distingue de un cambio de cero, que es otro "
					+ "hecho.");

			return new ARateClause(
				RateAsked.AsTwoPeriodsCompared, changeShape, change,
				new List<ARateInAPeriod> { ThePeriod(first), ThePeriod(second) });
		}

		/// <summary>Como se pregunto por el ritmo, por su codigo. TOTAL.</summary>
		internal string HowItWasAsked()
		{
			return _asked;
		}

		/// <summary>Que pudo decirse del cambio, por su codigo. TOTAL.</summary>
		internal string TheShapeOfItsChange()
		{
			return _changeShape;
		}

		/// <summary>
		/// Los DIGITOS del cambio en por ciento, en cultura invariante, ya redondeados al entero y
		/// sin signo: a que lado va lo dice la forma. TOTAL, y vacios en las cuatro situaciones que
		/// no son un porcentaje.
		/// </summary>
		internal string TheChangeAsSaid()
		{
			return _change;
		}

		/// <summary>En cual situacion esta cada periodo, uno por fila. TOTAL.</summary>
		internal string[] TheShapesOfItsPeriods()
		{
			return _shapes;
		}

		/// <summary>Donde empieza cada periodo, cuando su situacion lo nombra. TOTAL.</summary>
		internal string[] TheInstantsEachPeriodStartsAt()
		{
			return _froms;
		}

		/// <summary>Donde termina cada uno, en el mismo orden. TOTAL.</summary>
		internal string[] TheInstantsEachPeriodEndsAt()
		{
			return _tos;
		}

		/// <summary>La unidad de cada periodo, cuando hay dos que nombrar. TOTAL.</summary>
		internal string[] TheUnitsEachPeriodIsIn()
		{
			return _unitsOfThePeriod;
		}

		/// <summary>El segundo en que caen los dos extremos, cuando caen juntos. TOTAL.</summary>
		internal string[] TheSecondsBothEndsFellIn()
		{
			return _seconds;
		}

		/// <summary>Cuanto recorrio el vehiculo en cada periodo. TOTAL.</summary>
		internal int[] TheDistancesEachPeriodTravelled()
		{
			return _distances;
		}

		/// <summary>La unidad de esa distancia y de ese ritmo, una por periodo. TOTAL.</summary>
		internal string[] TheUnitsEachDistanceIsIn()
		{
			return _units;
		}

		/// <summary>Los digitos de la distancia por dia de cada periodo. TOTAL.</summary>
		internal string[] TheDistancesPerDayAsSaid()
		{
			return _perDays;
		}

		/// <summary>Como se dice el tramo de tiempo de cada uno, por su codigo. TOTAL.</summary>
		internal string[] TheShapesOfEachSpan()
		{
			return _spanShapes;
		}

		/// <summary>Los digitos del tramo, en la unidad que dice su forma. TOTAL.</summary>
		internal string[] TheSpansAsSaid()
		{
			return _spans;
		}

		/// <summary>De que periodo es cada lectura nombrada, por su indice. TOTAL.</summary>
		internal int[] ThePeriodOfEachReading()
		{
			return _readingOf.ToArray();
		}

		/// <summary>Que valor leyo cada lectura nombrada, en el mismo orden. TOTAL.</summary>
		internal int[] TheValuesOfEachReading()
		{
			return _readingValues.ToArray();
		}

		/// <summary>La unidad de cada una, en el mismo orden. TOTAL.</summary>
		internal string[] TheUnitsOfEachReading()
		{
			return _readingUnits.ToArray();
		}

		/// <summary>Cuando se observo cada una, en el mismo orden. TOTAL.</summary>
		internal string[] TheInstantsOfEachReading()
		{
			return _readingInstants.ToArray();
		}

		private void Place(int period, ARateInAPeriod rate)
		{
			_shapes[period] = rate.TheShapeItHas();
			_froms[period] = rate.TheInstantItStartsAt();
			_tos[period] = rate.TheInstantItEndsAt();
			_unitsOfThePeriod[period] = rate.TheUnitThePeriodIsIn();
			_seconds[period] = rate.TheSecondBothEndsFellIn();
			_distances[period] = rate.TheDistanceItTravelled();
			_units[period] = rate.TheUnitTheDistanceIsIn();
			_perDays[period] = rate.TheDistancePerDayAsSaid();
			_spanShapes[period] = rate.TheShapeOfItsSpan();
			_spans[period] = rate.TheSpanAsSaid();

			int[] values = rate.TheValuesOfTheReadingsItNames();
			string[] units = rate.TheUnitsOfTheReadingsItNames();
			string[] instants = rate.TheInstantsOfTheReadingsItNames();

			for (int reading = 0; reading < values.Length; reading++)
			{
				_readingOf.Add(period);
				_readingValues.Add(values[reading]);
				_readingUnits.Add(units[reading]);
				_readingInstants.Add(instants[reading]);
			}
		}

		private static List<ARateInAPeriod> TheOne(ARateInAPeriod period)
		{
			return new List<ARateInAPeriod> { ThePeriod(period) };
		}

		private static ARateInAPeriod ThePeriod(ARateInAPeriod period)
		{
			if (period == null)
				throw new DomainException(
					"Se compuso el ritmo de uso sin ningun periodo del que hablar. Un periodo "
					+ "ausente no es un periodo sin ritmo.");

			return period;
		}
	}
}
