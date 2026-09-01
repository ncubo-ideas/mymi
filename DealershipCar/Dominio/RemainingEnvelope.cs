using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// Lo que le queda al sobre de garantia en sus DOS mitades al instante preguntado: meses contra
	/// el plazo publicado y distancia contra el limite publicado. Se calcula en cada lectura y nunca
	/// se guarda -- es correcto para un solo instante y esta viejo desde el siguiente.
	/// <para>
	/// Las dos mitades no se comparan ENTRE SI: son incomparables, y convertir una en la otra por un
	/// promedio de distancia por mes seria a la vez la conversion que este modulo no hace y un
	/// pronostico. Cada una se compara contra SU limite.
	/// </para>
	/// </summary>
	internal abstract class RemainingEnvelope
	{
		private readonly int _monthsLeft;
		private readonly int _monthsLimit;

		/// <summary>
		/// La mitad de tiempo, que siempre se puede medir porque no depende de ninguna lectura. Se
		/// llega por las dos fabricas, que llevan las guardas.
		/// </summary>
		protected RemainingEnvelope(int monthsLeft, int monthsLimit)
		{
			_monthsLeft = monthsLeft;
			_monthsLimit = monthsLimit;
		}

		/// <summary>
		/// Las dos mitades medidas. Un limite en cero o negativo no es un limite y una distancia sin
		/// unidad no se puede decir ni comparar: las dos son defectos de quien compone.
		/// </summary>
		internal static RemainingEnvelope InBothHalves(
			int monthsLeft, int monthsLimit, int distanceLeft, int distanceLimit, DistanceUnit unit)
		{
			RequireALimitInMonths(monthsLimit);

			if (distanceLimit <= 0)
				throw new DomainException(
					$"Se compuso lo que queda contra un limite de distancia de {distanceLimit}. Sin "
					+ "un limite positivo no hay contra que medir, y decir lo que queda igual seria "
					+ "medir contra un limite que nadie publico.");

			if (unit == null)
				throw new DomainException(
					"Se compuso lo que queda de distancia sin decir en que unidad esta. Una distancia "
					+ "sin unidad no se puede decir ni comparar, y aca las unidades no se convierten "
					+ "entre si.");

			return new ARemainingEnvelope(
				monthsLeft, monthsLimit, distanceLeft, distanceLimit, unit);
		}

		/// <summary>
		/// La mitad de tiempo medida y la de distancia no. Exige DECIR POR QUE: una mitad que no se
		/// pudo medir y no dice el motivo no le deja a nadie arreglarla, y es el motivo lo que
		/// separa "falta la lectura" de "no calzan las unidades".
		/// </summary>
		internal static RemainingEnvelope MeasuredOnlyInTime(
			int monthsLeft, int monthsLimit, string whyTheDistanceHalfCouldNotBeMeasured)
		{
			RequireALimitInMonths(monthsLimit);

			if (string.IsNullOrWhiteSpace(whyTheDistanceHalfCouldNotBeMeasured))
				throw new DomainException(
					"Se compuso una mitad de distancia sin medir y sin decir por que. Decir que no se "
					+ "pudo sin decir que falta deja a quien lea sabiendo que algo fallo y sin saber "
					+ "que arreglar.");

			return new ARemainingEnvelopeMeasuredOnlyInTime(
				monthsLeft, monthsLimit, whyTheDistanceHalfCouldNotBeMeasured);
		}

		/// <summary>
		/// Los meses que quedan del plazo. Cero es el limite alcanzado exacto y negativo es cuanto
		/// se paso; las dos son respuestas, no defectos.
		/// </summary>
		internal int MonthsLeft
		{
			get { return _monthsLeft; }
		}

		/// <summary>
		/// Si la mitad de tiempo llego a su limite. TOTAL: el plazo se mide desde la entrega hasta
		/// el instante preguntado y no necesita ninguna lectura, asi que siempre se puede contestar.
		/// El umbral es INCLUSIVO, como los demas de este modulo.
		/// </summary>
		internal bool TheTimeHalfIsReached
		{
			get { return _monthsLeft <= 0; }
		}

		/// <summary>
		/// Si la mitad de distancia se pudo medir. TOTAL, y es la que hay que hacer antes de pedirla.
		/// </summary>
		internal abstract bool TheDistanceHalfWasMeasured();

		/// <summary>
		/// La distancia que queda del limite, en la unidad en que se publico. Accesor GUARDADO:
		/// revienta si esa mitad no se pudo medir, porque un numero aqui se leeria como un odometro
		/// que alguien leyo.
		/// </summary>
		internal abstract int DistanceLeft { get; }

		/// <summary>
		/// Si la mitad de distancia llego a su limite. Accesor GUARDADO por lo mismo: un "no" para
		/// una mitad que nadie midio se lee como una medicion.
		/// </summary>
		internal abstract bool TheDistanceHalfIsReached { get; }

		/// <summary>
		/// Si con estas dos mitades se puede decidir donde esta parado el vehiculo. TOTAL, y es la
		/// pregunta que hay que hacer antes que las tres de abajo.
		/// <para>
		/// Contesta que si cuando las dos se midieron, y tambien cuando la de tiempo YA LLEGO: una
		/// mitad que no se pudo medir degrada la respuesta solo cuando todavia podia cambiarla.
		/// </para>
		/// </summary>
		internal bool TheStandingCanBeDecided
		{
			get { return TheDistanceHalfWasMeasured() || TheTimeHalfIsReached; }
		}

		/// <summary>
		/// Si el sobre se alcanzo, por cualquiera de sus dos mitades. Accesor GUARDADO: sin poder
		/// decidir, un "no" diria que el vehiculo esta adentro de una garantia que nadie midio.
		/// </summary>
		internal bool TheEnvelopeIsReached
		{
			get
			{
				RequireADecidableStanding();

				return TheTimeHalfIsReached
					|| (TheDistanceHalfWasMeasured() && TheDistanceHalfIsReached);
			}
		}

		/// <summary>
		/// Si la mitad de tiempo participo en decidir donde esta parado el vehiculo. Accesor
		/// GUARDADO. Puede ser verdadero al mismo tiempo que el de distancia: cual decidio tiene
		/// tres respuestas honestas y un booleano solo no puede darlas.
		/// </summary>
		internal bool DecidedByTime
		{
			get
			{
				RequireADecidableStanding();

				if (!TheDistanceHalfWasMeasured()) return true;
				if (TheTimeHalfIsReached == TheDistanceHalfIsReached) return true;

				return TheTimeHalfIsReached;
			}
		}

		/// <summary>
		/// Si la mitad de distancia participo en decidir donde esta parado el vehiculo. Accesor
		/// GUARDADO, y da que no cuando esa mitad no se pudo medir: entonces decidio el tiempo solo.
		/// </summary>
		internal bool DecidedByDistance
		{
			get
			{
				RequireADecidableStanding();

				if (!TheDistanceHalfWasMeasured()) return false;
				if (TheTimeHalfIsReached == TheDistanceHalfIsReached) return true;

				return TheDistanceHalfIsReached;
			}
		}

		/// <summary>
		/// Las dos mitades y cual llego primero, en palabras. TOTAL, y compuesta en UN SOLO lugar a
		/// proposito: es lo que hace que ninguna respuesta pueda decir una mitad y callar la otra.
		/// </summary>
		internal string Describe()
		{
			return TheTimeHalfInWords() + " " + TheDistanceHalfInWords() + " "
				+ WhichArrivedFirstInWords();
		}

		/// <summary>La clausula de la mitad de distancia: su numero, o por que no se pudo medir.</summary>
		protected abstract string TheDistanceHalfInWords();

		/// <summary>Revienta si con estas dos mitades no se puede decidir donde esta parado.</summary>
		protected void RequireADecidableStanding()
		{
			if (TheStandingCanBeDecided) return;

			throw new DomainException(
				"Con estas dos mitades no se puede decidir donde esta parado el vehiculo: la de "
				+ "distancia no se pudo medir y la de tiempo todavia no llego, asi que la de "
				+ "distancia todavia podia cambiar la respuesta. Hay que preguntar primero si se "
				+ "podia decidir. Un si o un no devueltos aca se leerian como una medicion.");
		}

		/// <summary>Como se dice una cantidad de meses, que se pluraliza.</summary>
		protected static string Months(int quantity)
		{
			return quantity + (quantity == 1 ? " month" : " months");
		}

		private static void RequireALimitInMonths(int monthsLimit)
		{
			if (monthsLimit > 0) return;

			throw new DomainException(
				$"Se compuso lo que queda contra un plazo de {monthsLimit} meses. Sin un plazo "
				+ "positivo no hay contra que medir, y un cero se leeria como un plazo cumplido el "
				+ "mismo dia de la entrega.");
		}

		private string TheTimeHalfInWords()
		{
			if (_monthsLeft == 0)
				return "Time: exactly at a limit of " + Months(_monthsLimit) + ".";

			if (_monthsLeft < 0)
				return "Time: " + Months(-_monthsLeft) + " past a limit of "
					+ Months(_monthsLimit) + ".";

			return "Time: " + Months(_monthsLeft) + " left of " + Months(_monthsLimit) + ".";
		}

		private string WhichArrivedFirstInWords()
		{
			if (!TheStandingCanBeDecided)
				return "Which half arrives first cannot be said either, because one of the two was "
					+ "not measured.";

			if (!TheEnvelopeIsReached)
				return "Neither half has been reached, and which one arrives first depends on how "
					+ "much this vehicle is driven from here: the two halves are not comparable to "
					+ "each other, and this record states what it has rather than predicting a "
					+ "rate.";

			if (DecidedByTime && DecidedByDistance)
				return "Reached by time and distance, which arrived together.";

			if (DecidedByDistance) return "Reached by distance, which arrived first.";

			return "Reached by time, which arrived first.";
		}
	}
}
