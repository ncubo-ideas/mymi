using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// LO QUE UN INTERVALO PUBLICADO TIENE QUE DECIR: de que operacion es, cada cuanto se hace con las
	/// mitades que se publicaron, de que revision sale, y -- cuando se pregunto por un vehiculo --
	/// donde queda la distancia que le queda frente a el.
	/// <para>
	/// Sale como FORMA mas valores ya tipados y nunca como una frase. Que mitades contesta lo dice su
	/// propio codigo: lo que separa la mitad publicada de la del vehiculo es puntuacion, y la
	/// puntuacion es del idioma.
	/// </para>
	/// </summary>
	internal sealed class AServiceIntervalClause
	{
		private readonly string _publication;
		private readonly string _revisionCode;
		private readonly string _operation;
		private readonly string _intervalShape;
		private readonly int _intervalDistance;
		private readonly int _intervalDays;
		private readonly string _intervalUnit;
		private readonly string _halves;
		private readonly string _remainder;
		private readonly int _distanceRemaining;
		private readonly string _remainingUnit;

		private AServiceIntervalClause(
			string publication, string revisionCode, string operation, string intervalShape,
			int intervalDistance, int intervalDays, string intervalUnit, string halves,
			string remainder, int distanceRemaining, string remainingUnit)
		{
			_publication = publication;
			_revisionCode = revisionCode;
			_operation = operation;
			_intervalShape = intervalShape;
			_intervalDistance = intervalDistance;
			_intervalDays = intervalDays;
			_intervalUnit = intervalUnit;
			_halves = halves;
			_remainder = remainder;
			_distanceRemaining = distanceRemaining;
			_remainingUnit = remainingUnit;
		}

		/// <summary>
		/// La clausula cuando ninguna revision sellada publico el intervalo de esa operacion. Lleva la
		/// operacion igual, y no se empareja con nada: una ausencia no tiene contra que parar lo
		/// recibido.
		/// </summary>
		internal static AServiceIntervalClause OfNothingPublished(MaintenanceOperationKind operation)
		{
			if (operation == null)
				throw new DomainException(
					"Se compuso la ausencia de un intervalo de servicio sin decir de que operacion. Una "
					+ "ausencia que no nombra lo que falta no se distingue de cualquier otra.");

			return new AServiceIntervalClause(
				SpecificationPublication.NotPublished, "", operation.Name,
				IntervalHalvesPublished.None, 0, 0, "", HalvesAnswered.ThePublishedHalfAlone,
				RemainderAgainstTheInterval.None, 0, "");
		}

		/// <summary>
		/// La clausula publicada sola: cada cuanto dice el fabricante que se hace, y nada de ningun
		/// vehiculo. La contesta tambien quien no tenga ningun hecho de ninguno.
		/// </summary>
		internal static AServiceIntervalClause Published(
			string revisionCode, MaintenanceOperationKind operation, PublishedInterval interval)
		{
			if (string.IsNullOrWhiteSpace(revisionCode))
				throw new DomainException(
					"Se compuso la clausula de un intervalo de servicio sin nombrar la revision de la "
					+ "que sale. Una respuesta sin procedencia es un rumor: quien la reciba no puede "
					+ "saber si sigue vigente ni contra que compararla cuando cambie.");

			return new AServiceIntervalClause(
				SpecificationPublication.Published, revisionCode, operation.Name,
				interval.TheHalvesItPublished(), interval.TheDistanceItPublished(),
				interval.TheDaysItPublished(), interval.TheUnitItsDistanceIsIn(),
				HalvesAnswered.ThePublishedHalfAlone, RemainderAgainstTheInterval.None, 0, "");
		}

		/// <summary>
		/// La misma clausula emparejada con la posicion de ESTE vehiculo frente a ella, que es el
		/// producto entero. Nada de lo recibido se guarda: entra por parametro y se va con la
		/// respuesta. Si las dos distancias no son comparables la clausula lo dice y no convierte.
		/// </summary>
		internal AServiceIntervalClause PairedWith(
			PublishedInterval interval, int distanceRemaining, DistanceUnit remainingUnit)
		{
			if (!IsPublished())
				throw new DomainException(
					"Se quiso emparejar la posicion de un vehiculo con nada publicado. Sin la mitad "
					+ "publicada no hay contra que pararla, y unos numeros solos no dicen nada.");

			return new AServiceIntervalClause(
				_publication, _revisionCode, _operation, _intervalShape, _intervalDistance,
				_intervalDays, _intervalUnit, HalvesAnswered.GeneralAndPersonalTogether,
				interval.UnitsAreNeverConverted(remainingUnit), distanceRemaining,
				remainingUnit.Name);
		}

		/// <summary>Si hay algo publicado detras de esta respuesta, por su codigo. TOTAL.</summary>
		internal string ThePublicationShape()
		{
			return _publication;
		}

		/// <summary>Si hay algo publicado que contestar. TOTAL.</summary>
		internal bool IsPublished()
		{
			return _publication == SpecificationPublication.Published;
		}

		/// <summary>El codigo de la revision que la publico, o vacio donde no hay ninguna. TOTAL.</summary>
		internal string TheRevisionItCameFrom()
		{
			return _revisionCode;
		}

		/// <summary>De que operacion habla, por el NOMBRE del miembro. TOTAL.</summary>
		internal string TheOperationItIsAbout()
		{
			return _operation;
		}

		/// <summary>Que mitades publico el intervalo, por su codigo. TOTAL.</summary>
		internal string TheIntervalHalvesPublished()
		{
			return _intervalShape;
		}

		/// <summary>Cada cuanta distancia se hace, o cero donde esa mitad no se publico. TOTAL.</summary>
		internal int TheIntervalDistance()
		{
			return _intervalDistance;
		}

		/// <summary>Cada cuantos dias se hace, o cero por lo mismo. TOTAL.</summary>
		internal int TheIntervalDays()
		{
			return _intervalDays;
		}

		/// <summary>La unidad de esa distancia, como se dice, o vacia por lo mismo. TOTAL.</summary>
		internal string TheUnitTheIntervalIsIn()
		{
			return _intervalUnit;
		}

		/// <summary>Que mitades contesta esta clausula, por su codigo. TOTAL.</summary>
		internal string TheHalvesItAnswers()
		{
			return _halves;
		}

		/// <summary>
		/// Donde quedo la distancia que le queda a este vehiculo, por su codigo, o vacio donde nadie
		/// pregunto por ninguno. TOTAL.
		/// </summary>
		internal string TheRemainderShape()
		{
			return _remainder;
		}

		/// <summary>
		/// La distancia que le queda, tal como llego, o cero donde nadie pregunto por ningun vehiculo.
		/// TOTAL.
		/// </summary>
		internal int TheDistanceRemaining()
		{
			return _distanceRemaining;
		}

		/// <summary>La unidad en que esa distancia llego, como se dice, o vacia por lo mismo. TOTAL.</summary>
		internal string TheUnitTheRemainderIsIn()
		{
			return _remainingUnit;
		}
	}
}
