using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// EL RITMO DE UN PERIODO dicho como valores TOTALES: en cual de las cinco situaciones esta, y
	/// lo que esa situacion nombra. Existe aparte de <see cref="RateAnswer"/> porque los accesores
	/// de aquel estan GUARDADOS a proposito, y lo que cruza hacia una redaccion tiene que contestar
	/// en las cinco.
	/// <para>
	/// Las lecturas que la respuesta nombra van en columnas paralelas: dos cuando hay ritmo, una
	/// cuando la suspension nombra la lectura que se puede ir a arreglar, y ninguna en las otras
	/// dos. Lo que es de largo desconocido cruza aplanado.
	/// </para>
	/// <para>
	/// La distancia recorrida y lo que leyo cada extremo cruzan como el NUMERO que el dominio
	/// tiene; el ritmo y el tramo cruzan como DIGITOS en cultura invariante, porque el dominio
	/// los tiene como un entero largo y como una fraccion, y donde va la coma es del idioma.
	/// </para>
	/// </summary>
	internal sealed class ARateInAPeriod
	{
		private const string NothingToSay = "";
		private const int NoNumberToSay = 0;

		private static readonly int[] NoValues = new int[0];
		private static readonly string[] NoWords = new string[0];

		private readonly string _shape;
		private readonly string _from;
		private readonly string _to;
		private readonly string _unitOfThePeriod;
		private readonly string _theSecond;
		private readonly int _distance;
		private readonly string _unit;
		private readonly string _perDay;
		private readonly string _spanShape;
		private readonly string _span;
		private readonly int[] _readingValues;
		private readonly string[] _readingUnits;
		private readonly string[] _readingInstants;

		private ARateInAPeriod(
			string shape, string from, string to, string unitOfThePeriod, string theSecond,
			int distance, string unit, string perDay, string spanShape, string span,
			int[] readingValues, string[] readingUnits, string[] readingInstants)
		{
			_shape = shape;
			_from = from;
			_to = to;
			_unitOfThePeriod = unitOfThePeriod;
			_theSecond = theSecond;
			_distance = distance;
			_unit = unit;
			_perDay = perDay;
			_spanShape = spanShape;
			_span = span;
			_readingValues = readingValues;
			_readingUnits = readingUnits;
			_readingInstants = readingInstants;
		}

		/// <summary>
		/// El ritmo que SI se pudo decir: los dos extremos con sus instantes, la distancia entre
		/// ellos con su unidad, el tramo de tiempo y la distancia por dia.
		/// </summary>
		internal static ARateInAPeriod Measured(
			int earlierValue, string earlierUnit, string earlierInstant, int laterValue,
			string laterUnit, string laterInstant, int distance, string unit, string perDay,
			string spanShape, string span)
		{
			return new ARateInAPeriod(
				RateStanding.Measured.Name, NothingToSay, NothingToSay, NothingToSay, NothingToSay,
				distance, unit, perDay, spanShape, span,
				new[] { earlierValue, laterValue },
				new[] { earlierUnit, laterUnit },
				new[] { earlierInstant, laterInstant });
		}

		/// <summary>
		/// El periodo mezcla dos unidades: la del periodo, y la lectura que introdujo la segunda
		/// con la suya y con su instante.
		/// </summary>
		internal static ARateInAPeriod SuspendedByAUnitMismatch(
			string unitOfThePeriod, int value, string unit, string instant)
		{
			return new ARateInAPeriod(
				RateStanding.SuspendedByAUnitMismatch.Name, NothingToSay, NothingToSay,
				unitOfThePeriod, NothingToSay, NoNumberToSay, NothingToSay, NothingToSay,
				NothingToSay, NothingToSay,
				new[] { value }, new[] { unit }, new[] { instant });
		}

		/// <summary>El periodo tiene una sola lectura, nombrada con su instante.</summary>
		internal static ARateInAPeriod SuspendedByASingleReading(
			int value, string unit, string instant)
		{
			return new ARateInAPeriod(
				RateStanding.SuspendedByASingleReading.Name, NothingToSay, NothingToSay,
				NothingToSay, NothingToSay, NoNumberToSay, NothingToSay, NothingToSay,
				NothingToSay, NothingToSay,
				new[] { value }, new[] { unit }, new[] { instant });
		}

		/// <summary>El periodo no tiene ninguna lectura, y se nombra el periodo.</summary>
		internal static ARateInAPeriod SuspendedByAnAbsentReading(string from, string to)
		{
			return new ARateInAPeriod(
				RateStanding.SuspendedByAnAbsentReading.Name, from, to, NothingToSay, NothingToSay,
				NoNumberToSay, NothingToSay, NothingToSay, NothingToSay, NothingToSay,
				NoValues, NoWords, NoWords);
		}

		/// <summary>Los dos extremos caen en el mismo segundo, que se nombra.</summary>
		internal static ARateInAPeriod SuspendedByAZeroLengthSpan(string theSecond)
		{
			return new ARateInAPeriod(
				RateStanding.SuspendedByAZeroLengthSpan.Name, NothingToSay, NothingToSay,
				NothingToSay, theSecond, NoNumberToSay, NothingToSay, NothingToSay, NothingToSay,
				NothingToSay, NoValues, NoWords, NoWords);
		}

		/// <summary>En cual de las cinco situaciones esta, por su codigo. TOTAL.</summary>
		internal string TheShapeItHas()
		{
			return _shape;
		}

		/// <summary>
		/// Donde empieza el periodo por el que se pregunto, como instante con su hora, o vacio
		/// donde la situacion no lo nombra. TOTAL.
		/// </summary>
		internal string TheInstantItStartsAt()
		{
			return _from;
		}

		/// <summary>Donde termina ese periodo, por lo mismo y con la misma forma. TOTAL.</summary>
		internal string TheInstantItEndsAt()
		{
			return _to;
		}

		/// <summary>
		/// La unidad en que estan las lecturas del periodo, o vacia donde no hay dos unidades que
		/// nombrar. TOTAL.
		/// </summary>
		internal string TheUnitThePeriodIsIn()
		{
			return _unitOfThePeriod;
		}

		/// <summary>
		/// El segundo entero en que caen los dos extremos, o vacio donde si hubo tiempo
		/// transcurrido. TOTAL.
		/// </summary>
		internal string TheSecondBothEndsFellIn()
		{
			return _theSecond;
		}

		/// <summary>
		/// La distancia recorrida entre los dos extremos, o cero donde no hay ritmo. TOTAL, y cual
		/// de los dos es lo dice la forma y nunca ese cero.
		/// </summary>
		internal int TheDistanceItTravelled()
		{
			return _distance;
		}

		/// <summary>La unidad de esa distancia y de ese ritmo, o vacia. TOTAL.</summary>
		internal string TheUnitTheDistanceIsIn()
		{
			return _unit;
		}

		/// <summary>
		/// Los DIGITOS de la distancia por dia, en cultura invariante y ya redondeados al entero
		/// por quien es dueno de esa precision. TOTAL, y vacios donde no hay ritmo: un cero aqui se
		/// leeria como que el vehiculo no se movio, que es otro hecho y hasta puede ser cierto.
		/// </summary>
		internal string TheDistancePerDayAsSaid()
		{
			return _perDay;
		}

		/// <summary>Como se dice el tiempo transcurrido, por su codigo, o vacio. TOTAL.</summary>
		internal string TheShapeOfItsSpan()
		{
			return _spanShape;
		}

		/// <summary>
		/// Los DIGITOS del tramo de tiempo, en cultura invariante y en la unidad que dice su forma.
		/// No se redondea: cuantos decimales tiene sale de la division, y donde va la coma es del
		/// idioma. TOTAL, y vacios en las formas que se dicen enteras.
		/// </summary>
		internal string TheSpanAsSaid()
		{
			return _span;
		}

		/// <summary>Que valor leyo cada lectura que nombra, una por fila. TOTAL.</summary>
		internal int[] TheValuesOfTheReadingsItNames()
		{
			return _readingValues;
		}

		/// <summary>La unidad de cada una, en el mismo orden. TOTAL.</summary>
		internal string[] TheUnitsOfTheReadingsItNames()
		{
			return _readingUnits;
		}

		/// <summary>Cuando se observo cada una, en el mismo orden. TOTAL.</summary>
		internal string[] TheInstantsOfTheReadingsItNames()
		{
			return _readingInstants;
		}
	}
}
