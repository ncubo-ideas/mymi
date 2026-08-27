using DealershipCar.App.Events;
using NUnit.Framework;
using Puppets.Portador;

namespace DealershipCar.Dominio.Pruebas
{
	// LA FRONTERA DE LA INGESTA: null DESCARTA, y nunca se inventa un comando para tragarse una
	// senal. Es la mitad barata de la idempotencia -- lo que se descarta aca no llega al motor, y lo
	// que no llega al motor no puede dejar entrada.
	//
	// POR QUE IMPORTA TANTO. El motor escribe la entrada del journal ANTES de correr el cuerpo del
	// comando, asi que una senal mal formada que llegue a un comando revienta DESPUES de haber sido
	// escrita, y esa entrada queda para siempre: el motor loguea un fallo de rehidratacion en cada
	// arranque futuro de esa agenda (C22, medido en T6). Descartar en la frontera es la unica
	// respuesta que cuesta cero.
	//
	// Y ES LA POLARIDAD INVERSA A LA DE LA PUERTA, sobre el mismo codigo. En DealershipCarAPI un
	// codigo que no nombra ningun miembro es un defecto de quien llama y revienta (E32, C25); en un
	// evento RECIBIDO no lo es -- un concesionario publica lo que publica -- y se descarta. Las dos
	// mitades pasan por el mismo resolvedor, asi que no hay forma de que una acepte lo que la otra
	// rechaza.
	[TestFixture]
	public sealed class ASignalThatIsNotOursOrIsMalformedIsDiscarded
	{
		[Test]
		public void ATypeThisAppDoesNotConsumeIsDiscarded()
		{
			SenalDeEvento other = new SenalDeEvento(1, "WarrantyClaimFiled",
				"{\"id\":1,\"tipo\":\"WarrantyClaimFiled\",\"claim\":\"W-9912\"}");

			Assert.That(DealershipEventRoute.Route(other), Is.Null,
				"El concesionario publica su narrativa entera y este modulo modela una rebanada: "
				+ "recibir un tipo que no consume es lo normal, no un defecto.");
		}

		// EL TAG ES EL DISCRIMINADOR, y esta prueba existe porque la pasada de falsificacion de T8
		// demostro que nada lo probaba. Sacarle a OdometerReported.Interpret la linea que compara el
		// tag dejo las 395 pruebas EN VERDE y la consola sin cambios -- no porque el tag sobre, sino
		// porque ningun payload de la historia de hoy tiene la forma de otro mensaje, asi que la
		// validacion de campos alcanzaba a descartarlos por otro motivo.
		//
		// Eso es un accidente de los datos, no una propiedad del diseno. El dia que el concesionario
		// publique un tipo con los mismos campos -- un ping de telemetria con value/unit/observedAt
		// -- se ruteaba como una lectura del odometro y entraba a la agenda como un hecho que nadie
		// mando. Esta senal tiene EXACTAMENTE esa forma.
		[Test]
		public void ASignalWithOurPayloadButAnotherTypeIsStillNotOurs()
		{
			SenalDeEvento lookalike = new SenalDeEvento(1, "TelemetryPing",
				"{\"id\":1,\"tipo\":\"TelemetryPing\",\"value\":13200,\"unit\":\"UnitA\""
				+ ",\"observedAt\":\"2025-10-01T00:00:00Z\",\"source\":\"Telemetry\"}");

			Assert.That(OdometerReported.Interpret(lookalike), Is.Null,
				"El payload calza campo por campo con una lectura, y aun asi no es una: lo que dice "
				+ "QUE hecho es, es el tag.");

			Assert.That(DealershipEventRoute.Route(lookalike), Is.Null);
		}

		[Test]
		public void ASignalWithNoPayloadIsDiscarded()
		{
			Assert.That(
				DealershipEventRoute.Route(new SenalDeEvento(1, "OdometerReported", "")),
				Is.Null);
		}

		[Test]
		public void ASignalWhosePayloadIsNotJsonIsDiscarded()
		{
			Assert.That(
				DealershipEventRoute.Route(
					new SenalDeEvento(1, "OdometerReported", "no soy json")),
				Is.Null);
		}

		// El numero del evento es el punto de control. Uno que no avanza desde cero no se puede
		// journalizar como checkpoint, asi que la senal no sirve ni para descartarla contando.
		[Test]
		public void ASignalWithoutAPositiveEventNumberIsDiscarded()
		{
			Assert.That(
				DealershipEventRoute.Route(
					ADealershipNarrative.Reading(0, 100, "UnitA", "2025-10-01T00:00:00Z", "Telemetry")),
				Is.Null);

			Assert.That(
				DealershipEventRoute.Route(
					ADealershipNarrative.Reading(-3, 100, "UnitA", "2025-10-01T00:00:00Z", "Telemetry")),
				Is.Null);
		}

		[Test]
		public void AReadingInAUnitTheCanonDoesNotWriteIsDiscarded()
		{
			Assert.That(
				DealershipEventRoute.Route(
					ADealershipNarrative.Reading(1, 100, "Miles", "2025-10-01T00:00:00Z", "Telemetry")),
				Is.Null);

			Assert.That(
				DealershipEventRoute.Route(
					ADealershipNarrative.Reading(1, 100, "unita", "2025-10-01T00:00:00Z", "Telemetry")),
				Is.Null,
				"Y es estricta con las mayusculas, igual que la puerta: el canon manda en los "
				+ "nombres, y dejar pasar 'unita' pondria esa grafia en el journal.");
		}

		[Test]
		public void AReadingFromAProvenanceTheCanonDoesNotWriteIsDiscarded()
		{
			Assert.That(
				DealershipEventRoute.Route(
					ADealershipNarrative.Reading(1, 100, "UnitA", "2025-10-01T00:00:00Z", "Guessed")),
				Is.Null);
		}

		// Sin numero no es una lectura; sin instante no se puede ubicar en la linea de observacion,
		// que es donde OINV-54 la pone.
		[Test]
		public void AReadingWithNoValueOrNoInstantIsDiscarded()
		{
			Assert.That(
				OdometerReported.Interpret(new SenalDeEvento(1, "OdometerReported",
					"{\"unit\":\"UnitA\",\"observedAt\":\"2025-10-01T00:00:00Z\",\"source\":\"Telemetry\"}")),
				Is.Null);

			Assert.That(
				OdometerReported.Interpret(new SenalDeEvento(1, "OdometerReported",
					"{\"value\":100,\"unit\":\"UnitA\",\"source\":\"Telemetry\"}")),
				Is.Null);
		}

		// El odometro no cuenta hacia atras desde cero: un negativo no es una lectura baja, es un
		// evento roto.
		[Test]
		public void AReadingWithANegativeValueIsDiscarded()
		{
			Assert.That(
				DealershipEventRoute.Route(
					ADealershipNarrative.Reading(1, -100, "UnitA", "2025-10-01T00:00:00Z", "Telemetry")),
				Is.Null);
		}

		[Test]
		public void ACrossingForAKindTheCanonDoesNotWriteIsDiscarded()
		{
			Assert.That(
				DealershipEventRoute.Route(
					ADealershipNarrative.Crossing(1, "CambioDeAceite", "2026-04-01T00:00:00Z")),
				Is.Null);
		}

		[Test]
		public void ACompletionMissingItsReadingIsDiscarded()
		{
			Assert.That(
				OperationCompleted.Interpret(new SenalDeEvento(1, "OperationCompleted",
					"{\"kind\":\"FluidChange\",\"completedAt\":\"2026-03-05T00:00:00Z\",\"unit\":\"UnitA\"}")),
				Is.Null);
		}

		// Una revision sin codigo no se puede nombrar despues, y nombrarla es para lo que existe.
		[Test]
		public void ARevisionWithNoCodeIsDiscarded()
		{
			Assert.That(DealershipEventRoute.Route(
				ADealershipNarrative.Revision(1, "", "2025-09-01T00:00:00Z")), Is.Null);

			Assert.That(DealershipEventRoute.Route(
				ADealershipNarrative.Revision(1, "   ", "2025-09-01T00:00:00Z")), Is.Null);
		}

		// LOS DOS LIMITES SE EXIGEN PRESENTES aunque valgan cero: cero es "esta dimension no tiene
		// limite", una afirmacion del plan, y un evento que omite el campo no esta diciendo cero --
		// no esta diciendo.
		[Test]
		public void APlanOperationMissingALimitIsDiscarded()
		{
			Assert.That(
				PlanOperationSeeded.Interpret(new SenalDeEvento(1, "PlanOperationSeeded",
					"{\"kind\":\"FluidChange\",\"basis\":\"TimeOrDistance\",\"timeLimitDays\":365"
					+ ",\"unit\":\"UnitA\",\"anchoredAt\":\"2025-09-01T00:00:00Z\""
					+ ",\"anchorReading\":12000,\"anchorUnit\":\"UnitA\"}")),
				Is.Null);
		}

		// Una bandera AUSENTE es falsa -- en el plan del fabricante "obligatoria" y "de seguridad"
		// son afirmaciones, asi que no afirmarlas es negarlas -- pero una PRESENTE con otra forma es
		// un evento malformado y se descarta.
		[Test]
		public void APlanOperationWithoutItsFlagsIsNeitherMandatoryNorSafety()
		{
			PlanOperationSeeded seeded = PlanOperationSeeded.Interpret(
				new SenalDeEvento(1, "PlanOperationSeeded",
					"{\"kind\":\"FluidChange\",\"basis\":\"TimeOrDistance\",\"timeLimitDays\":365"
					+ ",\"distanceLimit\":10000,\"unit\":\"UnitA\""
					+ ",\"anchoredAt\":\"2025-09-01T00:00:00Z\""
					+ ",\"anchorReading\":12000,\"anchorUnit\":\"UnitA\"}"));

			Assert.That(seeded, Is.Not.Null);
			Assert.That(seeded.IsMandatory, Is.False);
			Assert.That(seeded.IsSafety, Is.False);
		}

		[Test]
		public void APlanOperationWhoseFlagIsNotABooleanIsDiscarded()
		{
			Assert.That(
				PlanOperationSeeded.Interpret(new SenalDeEvento(1, "PlanOperationSeeded",
					"{\"kind\":\"FluidChange\",\"basis\":\"TimeOrDistance\",\"timeLimitDays\":365"
					+ ",\"distanceLimit\":10000,\"unit\":\"UnitA\",\"isMandatory\":\"si\""
					+ ",\"anchoredAt\":\"2025-09-01T00:00:00Z\""
					+ ",\"anchorReading\":12000,\"anchorUnit\":\"UnitA\"}")),
				Is.Null);
		}

		// LA RUTA RECONOCE LOS CINCO, y esto es lo que impide que las pruebas de arriba queden
		// verdes para siempre: una ruta que devolviera null a todo las pasaria todas.
		[Test]
		public void TheRouteStillRecognisesEveryTypeThisAppConsumes()
		{
			Assert.That(DealershipEventRoute.Route(
				ADealershipNarrative.Reading(1, 100, "UnitA", "2025-10-01T00:00:00Z", "Telemetry")),
				Is.InstanceOf<OdometerReported>());

			Assert.That(DealershipEventRoute.Route(
				ADealershipNarrative.Crossing(2, "Inspection", "2026-04-01T00:00:00Z")),
				Is.InstanceOf<ConditionThresholdCrossed>());

			Assert.That(DealershipEventRoute.Route(
				ADealershipNarrative.Completion(3, "FluidChange", "2026-03-05T00:00:00Z", 20000, "UnitA")),
				Is.InstanceOf<OperationCompleted>());

			Assert.That(DealershipEventRoute.Route(
				ADealershipNarrative.PlanOperation(4, "FluidChange", "TimeOrDistance", 365, 10000, true, false)),
				Is.InstanceOf<PlanOperationSeeded>());

			Assert.That(DealershipEventRoute.Route(
				ADealershipNarrative.Revision(5, "R1", "2025-09-01T00:00:00Z")),
				Is.InstanceOf<PlanRevisionAdopted>());
		}

		// Y CADA MENSAJE LLEVA SU NUMERO Y SU TAG, que es lo unico que el mecanismo le mira: el
		// numero es el checkpoint que se journaliza, el tag es como se rutea.
		[Test]
		public void EveryMessageCarriesItsEventNumberAndItsType()
		{
			IMensajeDeEvento message = DealershipEventRoute.Route(
				ADealershipNarrative.Reading(42, 100, "UnitA", "2025-10-01T00:00:00Z", "Telemetry"));

			Assert.That(message.Evento, Is.EqualTo(42L));
			Assert.That(message.Tipo, Is.EqualTo(OdometerReported.TheEventType));
		}
	}
}
