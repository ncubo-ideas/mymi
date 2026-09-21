using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La respuesta a donde esta parado este vehiculo contra su sobre de garantia: o esta decidida
	/// y trae uno de los tres <see cref="EnvelopeStanding"/>, o no se pudo evaluar y lo dice
	/// nombrando en su propio tipo por que. Ninguno de los tres quiere decir "no lo se", y
	/// devolver el benigno para una entrada inevaluable es como una app termina repitiendo rio
	/// abajo, como si fuera un hecho, algo que nunca se midio.
	/// <para>
	/// Sale como FORMA mas valores ya tipados y nunca como una frase: la redaccion vive donde se
	/// elige el idioma, y el dominio no elige uno. La clausula que dice CONTRA QUE se midio la
	/// llevan las cuatro variantes y se compone una sola vez.
	/// </para>
	/// </summary>
	internal abstract class EnvelopeAnswer
	{
		/// <summary>La agencia no publico ningun sobre, que el registro sabe con certeza.</summary>
		internal const string NothingWasPublished = "NotPublished";

		/// <summary>Se decidio donde esta parado el vehiculo.</summary>
		internal const string Settled = "Settled";

		/// <summary>Nadie registro la entrega, que es el ancla desde donde se mide todo.</summary>
		internal const string NoDelivery = "NoDelivery";

		/// <summary>No hay ninguna lectura del odometro al instante preguntado ni antes.</summary>
		internal const string NoReading = "NoReading";

		/// <summary>El limite publicado y el odometro estan en unidades distintas.</summary>
		internal const string UnitMismatch = "UnitMismatch";

		private readonly AnEnvelopeClause _clause;

		/// <summary>
		/// Toda respuesta de sobre lleva su clausula: la ley se dice a si misma en la respuesta y no
		/// se pierde en la primera capa que la reformule.
		/// </summary>
		protected EnvelopeAnswer(AnEnvelopeClause clause)
		{
			if (clause == null)
				throw new DomainException(
					"Se compuso una respuesta sobre el sobre de garantia sin la clausula que dice "
					+ "contra que se midio. Una respuesta sin contra que se midio es una afirmacion que "
					+ "nadie puede comprobar.");

			_clause = clause;
		}

		/// <summary>
		/// Si se pudo decidir con lo que hay. TOTAL: contesta siempre, y es la que hay que hacer
		/// antes de leer el resultado.
		/// </summary>
		internal virtual bool CanBeEvaluated()
		{
			return false;
		}

		/// <summary>
		/// Si quedo suspendida porque nadie registro la entrega, que es el ancla desde donde se
		/// miden las dos mitades. TOTAL: es la clase de ausencia, con nombre, y no un codigo de
		/// motivo.
		/// </summary>
		internal virtual bool SuspendedByAnAbsentDelivery()
		{
			return false;
		}

		/// <summary>
		/// Si quedo suspendida porque no hay ninguna lectura del odometro al instante preguntado
		/// ni antes. TOTAL.
		/// </summary>
		internal virtual bool SuspendedByAnAbsentReading()
		{
			return false;
		}

		/// <summary>
		/// Si quedo suspendida porque la distancia del sobre y el odometro estan en unidades
		/// distintas. TOTAL.
		/// </summary>
		internal virtual bool SuspendedByAUnitMismatch()
		{
			return false;
		}

		/// <summary>
		/// El resultado. Accesor GUARDADO: revienta si no se pudo evaluar, porque el conjunto de
		/// tres no tiene un cuarto miembro que diga "no lo se", y devolver cualquiera de los tres
		/// ahi seria contestar una pregunta que no se pudo contestar.
		/// </summary>
		internal virtual EnvelopeStanding Standing()
		{
			throw new DomainException(
				"Esta pregunta sobre el sobre de garantia no se pudo evaluar, asi que no tiene "
				+ "respuesta: hay que preguntar primero si se podia evaluar. Cualquiera de las "
				+ "respuestas posibles devuelta aca se leeria como un hecho medido, y nadie mas "
				+ "abajo podria distinguirlo.");
		}

		/// <summary>
		/// Que forma tiene esta respuesta, por su codigo: la decidida, la que dice que nadie publico
		/// nada, o cual de las tres ausencias la suspendio. TOTAL.
		/// </summary>
		internal abstract string TheShapeItHas();

		/// <summary>
		/// Donde quedo parado el vehiculo, por el nombre del miembro, o vacio donde no se pudo
		/// decidir. TOTAL, a diferencia de <see cref="Standing"/>, que es la lectura guardada.
		/// </summary>
		internal virtual string TheStandingItReached()
		{
			return "";
		}

		/// <summary>
		/// La unidad del odometro que no coincidio con la del limite publicado, por su nombre, o
		/// vacia donde coincidieron. TOTAL.
		/// </summary>
		internal virtual string TheUnitThatDidNotMatch()
		{
			return "";
		}

		/// <summary>Cuanta clausula hay que decir, por su codigo. TOTAL.</summary>
		internal string TheClauseShape()
		{
			return _clause.TheShapeItHas();
		}

		/// <summary>Que forma tiene lo que la agencia publico, por su codigo. TOTAL.</summary>
		internal string ThePublishedShape()
		{
			return _clause.ThePublishedShape();
		}

		/// <summary>El plazo publicado. TOTAL.</summary>
		internal int TheMonthsPublished()
		{
			return _clause.TheMonthsPublished();
		}

		/// <summary>La distancia publicada. TOTAL.</summary>
		internal int TheDistancePublished()
		{
			return _clause.TheDistancePublished();
		}

		/// <summary>La unidad en que se publico esa distancia, por su nombre. TOTAL.</summary>
		internal string TheUnitTheDistanceIsPublishedIn()
		{
			return _clause.TheUnitTheDistanceIsPublishedIn();
		}

		/// <summary>El dia en que la agencia publico el sobre. TOTAL.</summary>
		internal string TheDayTheEnvelopeWasPublished()
		{
			return _clause.TheDayTheEnvelopeWasPublished();
		}

		/// <summary>Los meses que le quedan al plazo. TOTAL.</summary>
		internal int TheMonthsLeft()
		{
			return _clause.TheMonthsLeft();
		}

		/// <summary>El plazo contra el que se midieron. TOTAL.</summary>
		internal int TheMonthsLimit()
		{
			return _clause.TheMonthsLimit();
		}

		/// <summary>Que forma tiene la mitad de distancia, por su codigo. TOTAL.</summary>
		internal string TheDistanceHalfShape()
		{
			return _clause.TheDistanceHalfShape();
		}

		/// <summary>Lo que le queda a la mitad de distancia, en una fila o en ninguna. TOTAL.</summary>
		internal int[] TheDistanceLeftCounted()
		{
			return _clause.TheDistanceLeftCounted();
		}

		/// <summary>El limite contra el que se midio, en una fila o en ninguna. TOTAL.</summary>
		internal int[] TheDistanceLimitCounted()
		{
			return _clause.TheDistanceLimitCounted();
		}

		/// <summary>La unidad en que esta dicha esa mitad, por su nombre. TOTAL.</summary>
		internal string TheUnitTheDistanceIsSaidIn()
		{
			return _clause.TheUnitTheDistanceIsSaidIn();
		}

		/// <summary>
		/// La unidad de la distancia del registro que no calzo con la del limite, por su nombre.
		/// TOTAL, y no es la misma pregunta que <see cref="TheUnitThatDidNotMatch"/>: aquella es la
		/// de la respuesta suspendida y esta la de la mitad que no se pudo medir.
		/// </summary>
		internal string TheUnitTheOtherDistanceIsIn()
		{
			return _clause.TheUnitThatDidNotMatch();
		}

		/// <summary>Cual de las dos distancias del registro no calzo, por su codigo. TOTAL.</summary>
		internal string WhoseDistanceDidNotMatch()
		{
			return _clause.WhoseDistanceDidNotMatch();
		}

		/// <summary>Cual de las dos mitades llego primero, por su codigo. TOTAL.</summary>
		internal string WhichHalfArrivedFirst()
		{
			return _clause.WhichHalfArrivedFirst();
		}

		/// <summary>Que forma tiene la clausula del odometro, por su codigo. TOTAL.</summary>
		internal string TheOdometerShape()
		{
			return _clause.TheOdometerShape();
		}

		/// <summary>La lectura contra la que se midio. TOTAL.</summary>
		internal int TheReading()
		{
			return _clause.TheReading();
		}

		/// <summary>La unidad de esa lectura, por su nombre. TOTAL.</summary>
		internal string TheUnitTheReadingIsIn()
		{
			return _clause.TheUnitTheReadingIsIn();
		}

		/// <summary>El dia en que se observo esa lectura. TOTAL.</summary>
		internal string TheDayTheReadingWasObserved()
		{
			return _clause.TheDayTheReadingWasObserved();
		}

		/// <summary>La lectura registrada en la entrega, desde la que se cuenta. TOTAL.</summary>
		internal int TheDeliveryReading()
		{
			return _clause.TheDeliveryReading();
		}

		/// <summary>La unidad de la lectura de la entrega, por su nombre. TOTAL.</summary>
		internal string TheUnitTheDeliveryReadingIsIn()
		{
			return _clause.TheUnitTheDeliveryReadingIsIn();
		}

		/// <summary>El dia en que el vehiculo se entrego. TOTAL.</summary>
		internal string TheDayItWasDelivered()
		{
			return _clause.TheDayItWasDelivered();
		}
	}
}
