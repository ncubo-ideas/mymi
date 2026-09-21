using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// CONTRA QUE SE MIDIO una respuesta de sobre: lo que la agencia publico, lo que le queda a cada
	/// mitad, y contra que odometro. Es la mitad de la respuesta que no cambia con el motivo, y por
	/// eso se compone una sola vez y la llevan las cuatro variantes.
	/// <para>
	/// Las tres formas son las tres cantidades de clausula que hay, y no un valor especial de una
	/// sola: sin sobre publicado no hay contra que medir, sin entrega no hay desde donde, y con las
	/// dos la clausula va entera.
	/// </para>
	/// </summary>
	internal sealed class AnEnvelopeClause
	{
		/// <summary>No hay sobre publicado: lo unico que hay que decir es eso.</summary>
		internal const string TheEnvelopeAlone = "TheEnvelopeAlone";

		/// <summary>Hay sobre y no hay entrega: se cita lo publicado y no se mide nada.</summary>
		internal const string ThePublishedHalfAlone = "ThePublishedHalfAlone";

		/// <summary>Lo publicado, lo que queda de cada mitad, y contra que odometro.</summary>
		internal const string Whole = "Whole";

		private static readonly int[] NoAmount = new int[0];

		private readonly string _shape;
		private readonly WarrantyEnvelope _envelope;
		private readonly AnOdometerClause _odometer;

		private readonly int _monthsLeft;
		private readonly int _monthsLimit;
		private readonly string _distanceHalfShape;
		private readonly int[] _distanceLeft;
		private readonly int[] _distanceLimit;
		private readonly string _distanceUnit;
		private readonly string _unitThatDidNotMatch;
		private readonly string _whoseDistanceDidNotMatch;
		private readonly string _whichHalfArrivedFirst;

		private AnEnvelopeClause(
			string shape, WarrantyEnvelope envelope, AnOdometerClause odometer,
			int monthsLeft, int monthsLimit, string distanceHalfShape,
			int[] distanceLeft, int[] distanceLimit, string distanceUnit,
			string unitThatDidNotMatch, string whoseDistanceDidNotMatch,
			string whichHalfArrivedFirst)
		{
			_shape = shape;
			_envelope = envelope;
			_odometer = odometer;
			_monthsLeft = monthsLeft;
			_monthsLimit = monthsLimit;
			_distanceHalfShape = distanceHalfShape;
			_distanceLeft = distanceLeft;
			_distanceLimit = distanceLimit;
			_distanceUnit = distanceUnit;
			_unitThatDidNotMatch = unitThatDidNotMatch;
			_whoseDistanceDidNotMatch = whoseDistanceDidNotMatch;
			_whichHalfArrivedFirst = whichHalfArrivedFirst;
		}

		/// <summary>
		/// Lo que la agencia publico, solo. Es lo unico que se puede decir cuando no publico ningun
		/// sobre: sin limites no hay contra que medir ninguna mitad.
		/// </summary>
		internal static AnEnvelopeClause OfTheEnvelopeAlone(WarrantyEnvelope envelope)
		{
			return WithNoRemainder(TheEnvelopeAlone, envelope);
		}

		/// <summary>
		/// Lo que la agencia publico, citado. Es lo que queda cuando hay sobre y nadie registro la
		/// entrega: las dos mitades se miden desde ahi, asi que sin ancla no se mide ninguna.
		/// </summary>
		internal static AnEnvelopeClause OfThePublishedHalfAlone(WarrantyEnvelope envelope)
		{
			return WithNoRemainder(ThePublishedHalfAlone, envelope);
		}

		/// <summary>
		/// La clausula entera: lo publicado, lo que le queda a cada mitad y contra que odometro se
		/// midio. Las tres partes van juntas porque una sola no se puede leer sin las otras.
		/// </summary>
		internal static AnEnvelopeClause Measuring(
			WarrantyEnvelope envelope, RemainingEnvelope remaining, AnOdometerClause odometer)
		{
			if (envelope == null || remaining == null || odometer == null)
				throw new DomainException(
					"Se compuso la clausula entera de un sobre sin alguna de sus tres partes. Decir lo "
					+ "que queda sin decir contra que limite, o contra que odometro, deja un numero "
					+ "suelto que nadie puede comprobar.");

			return new AnEnvelopeClause(
				Whole, envelope, odometer,
				remaining.MonthsLeft, remaining.TheMonthsLimit(), remaining.TheDistanceHalfShape(),
				remaining.TheDistanceLeftCounted(), remaining.TheDistanceLimitCounted(),
				remaining.TheUnitTheDistanceIsSaidIn(), remaining.TheUnitThatDidNotMatch(),
				remaining.WhoseDistanceDidNotMatch(), remaining.WhichHalfArrivedFirst());
		}

		/// <summary>Cuanta clausula hay que decir, por su codigo. TOTAL.</summary>
		internal string TheShapeItHas()
		{
			return _shape;
		}

		/// <summary>Que forma tiene lo publicado, por su codigo. TOTAL.</summary>
		internal string ThePublishedShape()
		{
			return _envelope.TheShapeItHas();
		}

		/// <summary>El plazo publicado, o cero donde no hay sobre. TOTAL.</summary>
		internal int TheMonthsPublished()
		{
			return _envelope.TheMonthsItPublished();
		}

		/// <summary>La distancia publicada, o cero donde no hay sobre. TOTAL.</summary>
		internal int TheDistancePublished()
		{
			return _envelope.TheDistanceItPublished();
		}

		/// <summary>La unidad de esa distancia, por su nombre. TOTAL.</summary>
		internal string TheUnitTheDistanceIsPublishedIn()
		{
			return _envelope.TheUnitItsDistanceIsSaidIn();
		}

		/// <summary>El dia en que la agencia lo publico. TOTAL.</summary>
		internal string TheDayTheEnvelopeWasPublished()
		{
			return _envelope.TheDayItWasPublished();
		}

		/// <summary>Los meses que quedan del plazo, o cero donde no hay resto que decir. TOTAL.</summary>
		internal int TheMonthsLeft()
		{
			return _monthsLeft;
		}

		/// <summary>El plazo contra el que se midio, o cero por lo mismo. TOTAL.</summary>
		internal int TheMonthsLimit()
		{
			return _monthsLimit;
		}

		/// <summary>
		/// Que forma tiene la mitad de distancia, por su codigo, o vacia donde no hay resto. TOTAL.
		/// </summary>
		internal string TheDistanceHalfShape()
		{
			return _distanceHalfShape;
		}

		/// <summary>Lo que queda de distancia, en una fila o en ninguna. TOTAL.</summary>
		internal int[] TheDistanceLeftCounted()
		{
			return _distanceLeft;
		}

		/// <summary>El limite de distancia contra el que se midio, en una fila o en ninguna. TOTAL.</summary>
		internal int[] TheDistanceLimitCounted()
		{
			return _distanceLimit;
		}

		/// <summary>La unidad en que esta dicha esa mitad, por su nombre. TOTAL.</summary>
		internal string TheUnitTheDistanceIsSaidIn()
		{
			return _distanceUnit;
		}

		/// <summary>La unidad que no coincidio con la del limite, por su nombre. TOTAL.</summary>
		internal string TheUnitThatDidNotMatch()
		{
			return _unitThatDidNotMatch;
		}

		/// <summary>Cual de las dos distancias del registro no calzo, por su codigo. TOTAL.</summary>
		internal string WhoseDistanceDidNotMatch()
		{
			return _whoseDistanceDidNotMatch;
		}

		/// <summary>Cual de las dos mitades llego primero, por su codigo. TOTAL.</summary>
		internal string WhichHalfArrivedFirst()
		{
			return _whichHalfArrivedFirst;
		}

		/// <summary>Que forma tiene la clausula del odometro, por su codigo. TOTAL.</summary>
		internal string TheOdometerShape()
		{
			return _odometer.TheShapeItHas();
		}

		/// <summary>La lectura contra la que se midio, o cero donde no hay ninguna. TOTAL.</summary>
		internal int TheReading()
		{
			return _odometer.TheReading();
		}

		/// <summary>La unidad de esa lectura, por su nombre. TOTAL.</summary>
		internal string TheUnitTheReadingIsIn()
		{
			return _odometer.TheUnitTheReadingIsIn();
		}

		/// <summary>El dia en que se observo esa lectura. TOTAL.</summary>
		internal string TheDayTheReadingWasObserved()
		{
			return _odometer.TheDayItWasObserved();
		}

		/// <summary>La lectura registrada en la entrega, desde la que se cuenta. TOTAL.</summary>
		internal int TheDeliveryReading()
		{
			return _odometer.TheDeliveryReading();
		}

		/// <summary>La unidad de la lectura de la entrega, por su nombre. TOTAL.</summary>
		internal string TheUnitTheDeliveryReadingIsIn()
		{
			return _odometer.TheUnitTheDeliveryReadingIsIn();
		}

		/// <summary>El dia en que el vehiculo se entrego. TOTAL.</summary>
		internal string TheDayItWasDelivered()
		{
			return _odometer.TheDayItWasDelivered();
		}

		private static AnEnvelopeClause WithNoRemainder(string shape, WarrantyEnvelope envelope)
		{
			if (envelope == null)
				throw new DomainException(
					"Se compuso la clausula de un sobre sin el sobre. Lo que la agencia publico -- o no "
					+ "publico -- es lo unico que toda respuesta de sobre cita siempre.");

			return new AnEnvelopeClause(
				shape, envelope, AnOdometerClause.NoneToMeasureAgainst(),
				0, 0, "", NoAmount, NoAmount, "", "", "", "");
		}
	}
}
