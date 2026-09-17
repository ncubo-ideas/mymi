using System;

namespace Concesionario.Dominio
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
	/// <para>
	/// Sale como FORMA mas valores ya tipados y nunca como una frase: la redaccion vive donde se
	/// elige el idioma, y el dominio no elige uno.
	/// </para>
	/// </summary>
	internal abstract class RemainingEnvelope
	{
		private static readonly int[] NoAmount = new int[0];

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
		/// La mitad de tiempo medida y la de distancia no, por no haber ninguna lectura usable.
		/// </summary>
		internal static RemainingEnvelope MeasuredOnlyForWantOfAReading(
			int monthsLeft, int monthsLimit)
		{
			RequireALimitInMonths(monthsLimit);

			return new ARemainingEnvelopeMeasuredOnlyInTime(
				monthsLeft, monthsLimit, EnvelopeDistanceHalf.AbsentReading, "", "");
		}

		/// <summary>
		/// La mitad de tiempo medida y la de distancia no, por no calzar las unidades. Exige nombrar
		/// CUAL distancia no calzo y en que unidad esta: decir que no se pudo medir sin decir cual
		/// contra cual no le deja a nadie corregirlo.
		/// </summary>
		internal static RemainingEnvelope MeasuredOnlyForAUnitMismatch(
			int monthsLeft, int monthsLimit, string whoseDistance, DistanceUnit itsUnit)
		{
			RequireALimitInMonths(monthsLimit);

			if (string.IsNullOrWhiteSpace(whoseDistance))
				throw new DomainException(
					"Se compuso una mitad de distancia suspendida por unidades sin decir cual de las "
					+ "dos distancias del registro no calzo. Sin eso, quien lea sabe que algo fallo y "
					+ "no sabe que arreglar.");

			if (itsUnit == null)
				throw new DomainException(
					"Se compuso una mitad de distancia suspendida por unidades sin nombrar la unidad "
					+ "que no coincidio con la del limite. Nombrar una sola de las dos no dice cual "
					+ "contra cual.");

			return new ARemainingEnvelopeMeasuredOnlyInTime(
				monthsLeft, monthsLimit, EnvelopeDistanceHalf.UnitMismatch, whoseDistance,
				itsUnit.Name);
		}

		/// <summary>
		/// Los meses que quedan del plazo. Cero es el limite alcanzado exacto y negativo es cuanto
		/// se paso; las dos son respuestas, no defectos.
		/// </summary>
		internal int MonthsLeft
		{
			get { return _monthsLeft; }
		}

		/// <summary>El plazo contra el que se midio, en meses. TOTAL: la mitad de tiempo siempre lo tiene.</summary>
		internal int TheMonthsLimit()
		{
			return _monthsLimit;
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
		/// Que forma tiene la mitad de distancia, por su codigo: medida, o cual de las dos razones
		/// impidio medirla. TOTAL, y es lo que separa "falta la lectura" de "no calzan las unidades".
		/// </summary>
		internal abstract string TheDistanceHalfShape();

		/// <summary>
		/// Lo que queda de distancia, en UNA fila, o en CERO filas donde esa mitad no se midio.
		/// TOTAL, y la ausencia nunca se dice con un cero: un cero se leeria como un odometro justo
		/// en el limite.
		/// </summary>
		internal virtual int[] TheDistanceLeftCounted()
		{
			return NoAmount;
		}

		/// <summary>
		/// El limite de distancia contra el que se midio, en UNA fila, o en CERO filas donde esa
		/// mitad no se midio. TOTAL, y por la misma razon.
		/// </summary>
		internal virtual int[] TheDistanceLimitCounted()
		{
			return NoAmount;
		}

		/// <summary>
		/// La unidad en que estan lo que queda y el limite, por su nombre, o vacia donde esa mitad no
		/// se midio. TOTAL, y es la que hace que este resto nunca se diga sin su unidad.
		/// </summary>
		internal virtual string TheUnitTheDistanceIsSaidIn()
		{
			return "";
		}

		/// <summary>
		/// La unidad de la distancia que no calzo con la del limite, por su nombre, o vacia donde
		/// calzaron. TOTAL.
		/// </summary>
		internal virtual string TheUnitThatDidNotMatch()
		{
			return "";
		}

		/// <summary>
		/// Cual de las dos distancias del registro no calzo, por su codigo, o vacio donde calzaron.
		/// TOTAL.
		/// </summary>
		internal virtual string WhoseDistanceDidNotMatch()
		{
			return "";
		}

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
		/// Cual de las dos mitades llego primero, por su codigo. TOTAL, y decidido en UN SOLO lugar a
		/// proposito: es lo que hace que ninguna respuesta pueda decir una mitad y callar la otra.
		/// </summary>
		internal string WhichHalfArrivedFirst()
		{
			if (!TheStandingCanBeDecided) return EnvelopeArrival.NeitherHalfCanBeSaid;
			if (!TheEnvelopeIsReached) return EnvelopeArrival.NeitherHalfIsReached;
			if (DecidedByTime && DecidedByDistance) return EnvelopeArrival.BothHalvesTogether;
			if (DecidedByDistance) return EnvelopeArrival.TheDistanceHalf;

			return EnvelopeArrival.TheTimeHalf;
		}

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

		/// <summary>Una fila con ese monto, que es como se dice un monto presente.</summary>
		protected static int[] OneAmountOf(int quantity)
		{
			return new[] { quantity };
		}

		private static void RequireALimitInMonths(int monthsLimit)
		{
			if (monthsLimit > 0) return;

			throw new DomainException(
				$"Se compuso lo que queda contra un plazo de {monthsLimit} meses. Sin un plazo "
				+ "positivo no hay contra que medir, y un cero se leeria como un plazo cumplido el "
				+ "mismo dia de la entrega.");
		}
	}
}
