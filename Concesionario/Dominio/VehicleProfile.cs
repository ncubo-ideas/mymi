using System;
using System.Collections.Generic;
using System.Globalization;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Lo que la agencia registra sobre UN carro en particular: cuando lo entrego y con que odometro,
	/// el sobre de garantia que le publico, y las placas que le fue asignando. Es el agregado, uno
	/// por <see cref="Vin"/>, y una raiz global del DSL.
	/// <para>
	/// Guarda un ANCLA y dos LIMITES, y calcula todo lo demas -- lo que queda de cada mitad, cual
	/// llega primero, donde esta parado el vehiculo -- desde un instante recibido y una lectura
	/// recibida. No tiene maquina de estados: pasar de adentro a alcanzado no es una transicion, es
	/// una respuesta que cambia porque el reloj se movio o porque llego una lectura, sin que nada se
	/// haya mutado.
	/// </para>
	/// </summary>
	internal sealed class VehicleProfile
	{
		private readonly Vin _vin;

		private Delivery _delivery = Delivery.None;
		private WarrantyEnvelope _envelope = WarrantyEnvelope.None;
		private readonly List<APlateAssignment> _assignments = new List<APlateAssignment>();

		/// <summary>
		/// El constructor ES la creacion, y toma el vehiculo y nada mas. La entrega y el sobre son
		/// hechos que pueden llegar equivocados y necesitar correccion, asi que llegan por comando:
		/// un argumento de siembra queda fijo en la primera apertura del journal y despues no hay
		/// forma de arreglarlo.
		/// </summary>
		internal VehicleProfile(string vin)
		{
			_vin = new Vin(vin);
		}


		/// <summary>De que vehiculo es este perfil.</summary>
		internal string ReadVin()
		{
			return _vin.Value;
		}

		/// <summary>
		/// Registra la entrega: cuando se entrego el carro y en que odometro estaba, con su unidad.
		/// Registrarla de nuevo la CORRIGE -- gana la ultima -- porque es un hecho historico y no una
		/// regla con vigencia: si la primera quedara fija, una fecha mal puesta dejaria al vehiculo
		/// adentro de un sobre que ya se le vencio y nadie podria arreglarlo.
		/// </summary>
		internal void RecordDelivery(DateTime deliveredAt, int reading, DistanceUnitCode unit)
		{
			_delivery = Delivery.Of(deliveredAt, reading, DistanceUnit.OfCode(unit));
		}

		/// <summary>
		/// Publica el sobre de garantia: sus dos limites, la unidad del de distancia, y cuando la
		/// agencia lo publico. Rige desde SU instante de publicacion hacia adelante, asi que uno
		/// fechado antes del que rige no lo desplaza -- y tampoco se rechaza, porque un sobre viejo
		/// que llega fuera de orden es una entrega legitima y no un defecto de quien llama.
		/// </summary>
		internal void PublishWarrantyEnvelope(
			int months, int distance, DistanceUnitCode unit, DateTime publishedAt)
		{
			WarrantyEnvelope published = WarrantyEnvelope.PublishedAs(
				months, distance, DistanceUnit.OfCode(unit), publishedAt);

			if (_envelope.Exists() && publishedAt < _envelope.PublishedAt) return;

			_envelope = published;
		}

		/// <summary>
		/// Registra la placa que el concesionario le publico a este vehiculo, con el instante desde
		/// el que rige. Una segunda placa es un HECHO NUEVO y no una correccion de la primera:
		/// re-placar es ordinario, asi que la anterior se sigue pudiendo leer por su propio instante.
		/// <para>
		/// Una asignacion que llega despues pero esta fechada antes se COLOCA donde su instante manda
		/// y no desplaza a la vigente; con dos instantes iguales rige la ultima en llegar; y la misma
		/// placa asignada dos veces deja dos filas, porque descartar una seria borrar un hecho que el
		/// concesionario mando.
		/// </para>
		/// </summary>
		internal void RecordPlateAssignment(string plate, DateTime assignedAt)
		{
			Place(new APlateAssignment(new LicensePlate(plate), assignedAt));
		}

		/// <summary>Si la agencia publico un sobre. TOTAL: nunca revienta.</summary>
		internal bool IsWarrantyEnvelopePublished()
		{
			return _envelope.Exists();
		}

		/// <summary>
		/// El sobre publicado en palabras: sus dos limites, su unidad y cuando se publico, SIN NINGUN
		/// hecho del vehiculo adentro. TOTAL, tambien cuando no hay sobre -- es la lectura que puede
		/// citar quien no tiene ni un dato de ningun carro, y no tenerla haria que faltar el odometro
		/// de alguien volviera incitable lo que la agencia publico para todos.
		/// </summary>
		internal string DescribePublishedWarrantyEnvelope()
		{
			return _envelope.Describe();
		}

		/// <summary>
		/// Cuando la agencia publico el sobre que rige. Accesor GUARDADO, en par con la pregunta de
		/// presencia.
		/// </summary>
		internal DateTime ReadEnvelopePublishedAt()
		{
			return _envelope.PublishedAt;
		}

		/// <summary>El plazo publicado, en meses. Accesor GUARDADO.</summary>
		internal int EnvelopeMonths()
		{
			return _envelope.Months;
		}

		/// <summary>La distancia publicada. Accesor GUARDADO.</summary>
		internal int EnvelopeDistance()
		{
			return _envelope.Distance;
		}

		/// <summary>
		/// La unidad de la distancia publicada, dicha con su CODIGO para que un script pueda pasarla
		/// como argumento al metodo de otro agregado. Accesor GUARDADO. No sale hacia ninguna vista:
		/// el valor va directo a otro metodo de dominio adentro del mismo script.
		/// </summary>
		internal DistanceUnitCode EnvelopeUnit()
		{
			return _envelope.TheCodeOfItsDistanceUnit;
		}

		/// <summary>Si la agencia registro la entrega. TOTAL: nunca revienta.</summary>
		internal bool HasDeliveryBeenRecorded()
		{
			return _delivery.Exists();
		}

		/// <summary>Cuando se entrego el carro. Accesor GUARDADO.</summary>
		internal DateTime ReadDeliveredAt()
		{
			return _delivery.DeliveredAt;
		}

		/// <summary>El odometro del dia de la entrega. Accesor GUARDADO.</summary>
		internal int ReadDeliveryReading()
		{
			return _delivery.Reading;
		}

		/// <summary>La unidad de la lectura de la entrega, por su nombre. Accesor GUARDADO.</summary>
		internal string ReadDeliveryUnit()
		{
			return _delivery.Unit.Name;
		}

		/// <summary>
		/// Si la pregunta del sobre se puede decidir con lo que hay y con la lectura recibida. TOTAL,
		/// y es la que hay que hacer antes de leer el resultado.
		/// </summary>
		internal bool CanTheEnvelopeBeEvaluated(
			int reading, DistanceUnitCode readingUnit, DateTime observedAt, DateTime asOf)
		{
			return TheStandingAsOf(reading, readingUnit, observedAt, asOf).CanBeEvaluated();
		}

		/// <summary>
		/// Donde esta parado el vehiculo, por el nombre de la respuesta. Accesor GUARDADO: ninguno de
		/// los tres miembros quiere decir "no se puede saber".
		/// </summary>
		internal string ReadEnvelopeStanding(
			int reading, DistanceUnitCode readingUnit, DateTime observedAt, DateTime asOf)
		{
			return TheStandingAsOf(reading, readingUnit, observedAt, asOf).Standing().Name;
		}

		/// <summary>
		/// La respuesta del sobre en una frase: donde esta parado, sus DOS mitades, cual llego
		/// primero, contra que odometro se midio y cuando se observo, y cuando se publico el sobre --
		/// o el motivo con nombre por el que no se pudo decidir. TOTAL.
		/// </summary>
		internal string DescribeWarrantyEnvelope(
			int reading, DistanceUnitCode readingUnit, DateTime observedAt, DateTime asOf)
		{
			return TheStandingAsOf(reading, readingUnit, observedAt, asOf).Describe();
		}

		/// <summary>
		/// Lo mismo cuando no hay ninguna lectura del odometro al instante preguntado. TOTAL. Es una
		/// operacion aparte y no un valor especial en la de arriba: un cero o una lectura por defecto
		/// serian una medicion que nadie tomo.
		/// </summary>
		internal bool CanTheEnvelopeBeEvaluatedWithNoOdometer(DateTime asOf)
		{
			return TheStandingWithNoOdometerAsOf(asOf).CanBeEvaluated();
		}

		/// <summary>Donde esta parado sin ninguna lectura. Accesor GUARDADO, por lo mismo.</summary>
		internal string ReadEnvelopeStandingWithNoOdometer(DateTime asOf)
		{
			return TheStandingWithNoOdometerAsOf(asOf).Standing().Name;
		}

		/// <summary>La frase sin ninguna lectura, que dice que falta y no da ningun numero. TOTAL.</summary>
		internal string DescribeWarrantyEnvelopeWithNoOdometer(DateTime asOf)
		{
			return TheStandingWithNoOdometerAsOf(asOf).Describe();
		}

		/// <summary>
		/// Compone donde esta parado el vehiculo contra su sobre, con la lectura recibida. El ORDEN de
		/// los pasos es la ley: el sobre se pregunta antes que la entrega, la entrega antes que las
		/// unidades, y lo que no se puede medir suspende la respuesta solo cuando todavia podia
		/// cambiarla.
		/// </summary>
		internal EnvelopeAnswer TheStandingAsOf(
			int reading, DistanceUnitCode readingUnit, DateTime observedAt, DateTime asOf)
		{
			if (!_envelope.Exists()) return TheUnpublishedAnswer();
			if (!_delivery.Exists()) return TheAnswerWithNoAnchor();

			DistanceUnit limitUnit = _envelope.TheUnitItsDistanceIsMeasuredIn;
			DistanceUnit odometerUnit = DistanceUnit.OfCode(readingUnit);
			string odometer = TheOdometerClause(reading, odometerUnit, observedAt);

			if (!_delivery.IsExpressedIn(limitUnit))
				return TheAnswerWithAUnitMismatch(
					limitUnit, _delivery.Unit, "la lectura del odómetro registrada en la entrega",
					odometer, asOf);

			if (!_envelope.MeasuresDistanceIn(odometerUnit))
				return TheAnswerWithAUnitMismatch(
					limitUnit, odometerUnit, "el odómetro de este vehículo", odometer, asOf);

			return TheSettledAnswerFor(
				RemainingEnvelope.InBothHalves(
					_envelope.MonthsLeftAsOf(_delivery.DeliveredAt, asOf),
					_envelope.Months,
					_envelope.DistanceLeftFrom(_delivery.Reading, reading),
					_envelope.Distance,
					limitUnit),
				odometer);
		}

		/// <summary>
		/// Compone donde esta parado el vehiculo cuando no hay ninguna lectura. La mitad de tiempo se
		/// mide igual -- no depende de ninguna lectura -- asi que la respuesta se decide si el plazo
		/// ya llego, y si no se suspende nombrando la lectura que falta.
		/// </summary>
		internal EnvelopeAnswer TheStandingWithNoOdometerAsOf(DateTime asOf)
		{
			if (!_envelope.Exists()) return TheUnpublishedAnswer();
			if (!_delivery.Exists()) return TheAnswerWithNoAnchor();

			RemainingEnvelope remaining = RemainingEnvelope.MeasuredOnlyInTime(
				_envelope.MonthsLeftAsOf(_delivery.DeliveredAt, asOf),
				_envelope.Months,
				"porque no hay ninguna lectura del odómetro de este vehículo al instante preguntado "
					+ "ni antes, y la mitad de distancia se mide desde una.");

			string odometer = "No hay ninguna lectura del odómetro contra la cual medir la mitad de "
				+ "distancia, y quien trae una es el feed del concesionario.";

			if (remaining.TheStandingCanBeDecided)
				return TheSettledAnswerFor(remaining, odometer);

			return new AnEnvelopeSuspendedByAnAbsentReading(TheClause(remaining, odometer));
		}

		/// <summary>
		/// Si hay una placa publicada al instante preguntado. TOTAL: nunca revienta, y es la que hay
		/// que hacer antes de leer cualquiera de los dos textos.
		/// </summary>
		internal bool IsAPlatePublished(DateTime asOf)
		{
			return ThePlateAsOf(asOf).Exists();
		}

		/// <summary>
		/// La placa dicha entera, o la constancia con nombre de que no hay ninguna. TOTAL, y la frase
		/// es del dominio.
		/// </summary>
		internal string DescribePlate(DateTime asOf)
		{
			return ThePlateAsOf(asOf).Describe();
		}

		/// <summary>
		/// El texto tal como lo publico el concesionario. Accesor GUARDADO, en par con la pregunta de
		/// presencia.
		/// </summary>
		internal string ReadPlateAsPublished(DateTime asOf)
		{
			return ThePlateAsOf(asOf).AsPublished;
		}

		/// <summary>
		/// El texto normalizado con el que se cruza. Accesor GUARDADO por lo mismo.
		/// </summary>
		internal string ReadPlateJoinKey(DateTime asOf)
		{
			return ThePlateAsOf(asOf).JoinKey;
		}

		/// <summary>
		/// El texto publicado de cada asignacion, una por fila, en orden de la linea de tiempo.
		/// TOTAL: sin ninguna asignacion contesta cero filas.
		/// </summary>
		internal string[] EachPlateAsPublished()
		{
			string[] said = new string[_assignments.Count];

			for (int row = 0; row < _assignments.Count; row++)
				said[row] = _assignments[row].Plate.AsPublished;

			return said;
		}

		/// <summary>
		/// La llave de cada asignacion, una por fila y en el mismo orden. TOTAL. Sale ya normalizada:
		/// ninguna caratula la deriva del texto publicado.
		/// </summary>
		internal string[] EachPlateJoinKey()
		{
			string[] said = new string[_assignments.Count];

			for (int row = 0; row < _assignments.Count; row++)
				said[row] = _assignments[row].Plate.JoinKey;

			return said;
		}

		/// <summary>
		/// Desde cuando rige cada asignacion, una por fila y en el mismo orden. TOTAL.
		/// </summary>
		internal string[] EachPlateAssignedAt()
		{
			string[] said = new string[_assignments.Count];

			for (int row = 0; row < _assignments.Count; row++)
				said[row] = TheDay(_assignments[row].AssignedAt);

			return said;
		}

		/// <summary>
		/// Cual asignacion es la vigente, una por fila y en el mismo orden. TOTAL, y con al menos una
		/// fila hay exactamente una vigente: la ultima de la linea de tiempo.
		/// </summary>
		internal string[] EachPlateIsCurrent()
		{
			string[] said = new string[_assignments.Count];

			for (int row = 0; row < _assignments.Count; row++)
				said[row] = SaidAsATruth(row == _assignments.Count - 1);

			return said;
		}

		private EnvelopeAnswer TheUnpublishedAnswer()
		{
			return new ASettledEnvelopeAnswer(
				EnvelopeStanding.NotPublished, _envelope.Describe());
		}

		private EnvelopeAnswer TheAnswerWithNoAnchor()
		{
			return new AnEnvelopeSuspendedByAnAbsentDelivery(ThePublishedHalf());
		}

		private EnvelopeAnswer TheAnswerWithAUnitMismatch(
			DistanceUnit limitUnit, DistanceUnit other, string whoseUnit,
			string odometer, DateTime asOf)
		{
			RemainingEnvelope remaining = RemainingEnvelope.MeasuredOnlyInTime(
				_envelope.MonthsLeftAsOf(_delivery.DeliveredAt, asOf),
				_envelope.Months,
				"porque el límite de distancia publicado está en " + limitUnit.Name + " y "
					+ whoseUnit + " está en " + other.Name + ", y las unidades nunca se "
					+ "convierten.");

			if (remaining.TheStandingCanBeDecided)
				return TheSettledAnswerFor(remaining, odometer);

			return new AnEnvelopeSuspendedByAUnitMismatch(
				limitUnit, other, TheClause(remaining, odometer));
		}

		private EnvelopeAnswer TheSettledAnswerFor(RemainingEnvelope remaining, string odometer)
		{
			return new ASettledEnvelopeAnswer(
				remaining.TheEnvelopeIsReached ? EnvelopeStanding.Reached : EnvelopeStanding.Inside,
				TheClause(remaining, odometer));
		}

		private string TheClause(RemainingEnvelope remaining, string odometer)
		{
			return ThePublishedHalf() + " " + remaining.Describe() + " " + odometer;
		}

		private string ThePublishedHalf()
		{
			return "Sobre publicado: " + _envelope.Describe();
		}

		private string TheOdometerClause(int reading, DistanceUnit unit, DateTime observedAt)
		{
			return "Medido contra un odómetro de " + reading + " " + unit.Name + " observado el "
				+ TheDay(observedAt) + ", contando desde los " + _delivery.Reading + " "
				+ _delivery.Unit.Name + " registrados cuando el vehículo se entregó el "
				+ TheDay(_delivery.DeliveredAt) + ".";
		}

		private static string TheDay(DateTime instant)
		{
			return instant.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}

		/// <summary>Un si o un no dicho como columna, en la grafia que el wire lleva.</summary>
		private static string SaidAsATruth(bool said)
		{
			return said ? "true" : "false";
		}

		private void Place(APlateAssignment arriving)
		{
			_assignments.Insert(ItsPlaceOnTheAssignmentTimeline(arriving), arriving);
		}

		private int ItsPlaceOnTheAssignmentTimeline(APlateAssignment arriving)
		{
			int place = _assignments.Count;

			while (place > 0 && _assignments[place - 1].AssignedAt > arriving.AssignedAt) place--;

			return place;
		}

		private PlateOnRecord ThePlateAsOf(DateTime asOf)
		{
			PlateOnRecord found = PlateOnRecord.None;

			foreach (APlateAssignment each in _assignments)
				if (each.AssignedAt <= asOf) found = PlateOnRecord.Of(each);

			return found;
		}
	}
}
