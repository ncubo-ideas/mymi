using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
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

		// LA RUTA RECONOCE LOS VEINTITRES, y esto es lo que impide que las pruebas de arriba queden
		// verdes para siempre: una ruta que devolviera null a todo las pasaria todas. Son CINCO de la
		// agenda escritos a mano, ONCE de la especificacion, DOS del perfil del vehiculo, TRES de las
		// estadias y DOS del libro de costos, y cada grupo se cuenta APARTE -- meterlos en una sola
		// cuenta haria que ninguna de las cinco dijera lo que dice.
		//
		// LOS ONCE DE LA ESPECIFICACION SE BARREN CON LA FICHA ENTERA, que es la lista que ya tiene
		// uno de cada tipo en el orden en que un feed los manda. Barrer la lista en vez de escribir
		// once renglones a mano es lo que hace que un tipo nuevo sin ruta no pueda pasar
		// desapercibido: la ficha lo trae y este barrido lo pide.
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

			HashSet<string> recognised = new HashSet<string>();

			foreach (SenalDeEvento signal in ADealershipNarrative.TheWholeSpecSheet())
			{
				IMensajeDeEvento message = DealershipEventRoute.Route(signal);

				Assert.That(message, Is.Not.Null,
					"La ruta no reconoce " + signal.Tipo + ", asi que un feed que lo mande lo "
					+ "descartaria en silencio.");
				Assert.That(message.Tipo, Is.EqualTo(signal.Tipo),
					"Y lo reconoce como lo que es, no como el tipo de al lado.");

				recognised.Add(signal.Tipo);
			}

			Assert.That(recognised.Count, Is.EqualTo(11),
				"Son ONCE tipos de hecho de la especificacion, uno por comando de publicacion de la "
				+ "puerta. Si este numero baja, la ficha de la narrativa dejo de traer uno de cada "
				+ "tipo y el barrido de arriba mide menos de lo que dice.");

			// Y LOS DOS DEL PERFIL DE ESTE VEHICULO SE CUENTAN APARTE, no adentro de los once. Son
			// hechos de OTRO global de raiz -- este carro, no el modelo -- y meterlos en la cuenta de
			// arriba haria que ese once dejara de decir lo que dice.
			HashSet<string> ofThisVehicle = new HashSet<string>();

			foreach (SenalDeEvento signal in ADealershipNarrative.TheDeliveryAndTheEnvelope())
			{
				IMensajeDeEvento message = DealershipEventRoute.Route(signal);

				Assert.That(message, Is.Not.Null,
					"La ruta no reconoce " + signal.Tipo + ", asi que un feed que lo mande lo "
					+ "descartaria en silencio.");
				Assert.That(message.Tipo, Is.EqualTo(signal.Tipo),
					"Y lo reconoce como lo que es, no como el tipo de al lado.");

				ofThisVehicle.Add(signal.Tipo);
			}

			Assert.That(ofThisVehicle.Count, Is.EqualTo(2),
				"Son DOS tipos de hecho del perfil del vehiculo, uno por comando de la agencia: la "
				+ "entrega y el sobre.");

			// Y LOS TRES DE LAS ESTADIAS SE CUENTAN APARTE POR LO MISMO: son hechos de una CUARTA
			// global de raiz -- el registro de estadias de este carro -- y sumarlos a cualquiera de
			// las tres cuentas de arriba haria que esa cuenta dejara de decir lo que dice.
			HashSet<string> ofTheShop = new HashSet<string>();

			foreach (SenalDeEvento signal in ADealershipNarrative.TheStays())
			{
				IMensajeDeEvento message = DealershipEventRoute.Route(signal);

				Assert.That(message, Is.Not.Null,
					"La ruta no reconoce " + signal.Tipo + ", asi que un feed que lo mande lo "
					+ "descartaria en silencio.");
				Assert.That(message.Tipo, Is.EqualTo(signal.Tipo),
					"Y lo reconoce como lo que es, no como el tipo de al lado.");

				ofTheShop.Add(signal.Tipo);
			}

			Assert.That(ofTheShop.Count, Is.EqualTo(3),
				"Son TRES tipos de hecho de las estadias, uno por comando de la sucursal: la entrada, "
				+ "la entrega y la estadia entera de un solo acto.");

			// Y LOS DOS DEL LIBRO DE COSTOS SE CUENTAN APARTE POR LO MISMO: son hechos de una QUINTA
			// global de raiz -- lo que se ha gastado en este carro -- y sumarlos a cualquiera de las
			// cuatro cuentas de arriba haria que esa cuenta dejara de decir lo que dice.
			HashSet<string> ofTheLedger = new HashSet<string>();

			foreach (SenalDeEvento signal in ADealershipNarrative.TheCosts())
			{
				IMensajeDeEvento message = DealershipEventRoute.Route(signal);

				Assert.That(message, Is.Not.Null,
					"La ruta no reconoce " + signal.Tipo + ", asi que un feed que lo mande lo "
					+ "descartaria en silencio.");
				Assert.That(message.Tipo, Is.EqualTo(signal.Tipo),
					"Y lo reconoce como lo que es, no como el tipo de al lado.");

				ofTheLedger.Add(signal.Tipo);
			}

			Assert.That(ofTheLedger.Count, Is.EqualTo(2),
				"Son DOS tipos de hecho del libro: el gasto que se consume en su periodo y el que sigue "
				+ "dando valor despues, uno por comando de la sucursal. La clase la dice CUAL de los dos "
				+ "es, asi que no hay ningun campo de clasificacion que un tercer tipo pudiera traer.");
		}

		// EL TAG DE CADA MENSAJE ES SU PROPIO NOMBRE DE TIPO, y esto se enumera POR REFLEXION y no
		// contra una lista escrita a mano: la lista de la narrativa no puede ver un tipo de mensaje
		// que nadie ruteo y que nadie le agrego. Enumerar los tipos es enumerar los sitios donde la
		// regla se decide (C57), y son VEINTITRES.
		//
		// POR QUE IMPORTA EL NOMBRE. El tag viaja por tres sitios mas: la ruta lo compara, el mock lo
		// lista en TiposConocidos, y el inyector lo escribe en el guion. Ninguno de esos tres tiene
		// prueba (el mock y el inyector no los referencia nadie de Pruebas/), asi que la unica forma
		// de que un tag no se despegue de su tipo es que sea derivable del tipo.
		[Test]
		public void EveryMessageTypeThisAppConsumesTagsItselfWithItsOwnTypeName()
		{
			List<Type> messages = new List<Type>();

			foreach (Type candidate in typeof(OdometerReported).Assembly.GetTypes())
				if (typeof(IMensajeDeEvento).IsAssignableFrom(candidate) && !candidate.IsInterface)
					messages.Add(candidate);

			Assert.That(messages.Count, Is.EqualTo(23),
				"Son VEINTITRES tipos de mensaje: cinco de la agenda, once de la especificacion, DOS "
				+ "del perfil de este vehiculo, TRES de las estadias en el taller y DOS del libro de "
				+ "costos. Si este numero sube, hay un tipo nuevo -- y los barridos que se apoyan en la "
				+ "lista de la narrativa no lo van a ver hasta que alguien lo agregue ahi.");

			Assert.Multiple(() =>
			{
				foreach (Type message in messages)
				{
					FieldInfo tag = message.GetField(
						"TheEventType", BindingFlags.Public | BindingFlags.Static);

					Assert.That(tag, Is.Not.Null,
						message.Name + " no declara TheEventType, asi que su tag no sale de su tipo.");

					Assert.That(tag.GetRawConstantValue(), Is.EqualTo(message.Name),
						"El tag de " + message.Name + " tiene que ser su propio nombre de tipo: es lo "
						+ "que impide que el tag y el tipo se despeguen en el mock, en el inyector o "
						+ "en la ruta, que son tres sitios sin prueba.");

					Assert.That(
						message.GetMethod("Interpret", BindingFlags.Public | BindingFlags.Static),
						Is.Not.Null,
						message.Name + " no declara Interpret, asi que ninguna senal puede volverse "
						+ "ese hecho.");
				}
			});

			// Y LOS VEINTITRES ESTAN EN LA NARRATIVA, que es lo que hace que los barridos de las otras
			// pruebas midan los veintitres y no veintidos. Son CINCO listas porque el feed le escribe a
			// cinco globales de raiz, y el union de las cinco es lo que esta prueba mira.
			HashSet<string> inTheNarrative = new HashSet<string>();

			foreach (SenalDeEvento signal in ADealershipNarrative.TheWholeStory())
				inTheNarrative.Add(signal.Tipo);

			foreach (SenalDeEvento signal in ADealershipNarrative.TheWholeSpecSheet())
				inTheNarrative.Add(signal.Tipo);

			foreach (SenalDeEvento signal in ADealershipNarrative.TheDeliveryAndTheEnvelope())
				inTheNarrative.Add(signal.Tipo);

			foreach (SenalDeEvento signal in ADealershipNarrative.TheStays())
				inTheNarrative.Add(signal.Tipo);

			foreach (SenalDeEvento signal in ADealershipNarrative.TheCosts())
				inTheNarrative.Add(signal.Tipo);

			Assert.Multiple(() =>
			{
				foreach (Type message in messages)
					Assert.That(inTheNarrative, Does.Contain(message.Name),
						"La narrativa no trae ninguna senal de tipo " + message.Name + ", asi que "
						+ "ningun barrido de esta suite lo visita: un tipo de mensaje sin senal es "
						+ "codigo que nunca corrio contra un journal.");
			});
		}

		// ---------------------------------------- la frontera de los once hechos de la ficha
		//
		// LA POLARIDAD, OTRA VEZ Y SOBRE CODIGO NUEVO: en DealershipCarAPI un texto en blanco o un
		// codigo que no nombra ningun miembro es un defecto de quien llama y revienta; en un evento
		// RECIBIDO no lo es -- un concesionario publica lo que publica -- y se descarta.
		//
		// EL BARRIDO ES POR SITIO DE VALIDACION Y NO POR TIPO DE HECHO (C57). Once clases de mensaje
		// tienen VEINTICUATRO textos obligatorios, ONCE nombres de conjunto cerrado, DIEZ numeros
		// obligatorios y TRES instantes, porque un hecho trae varios de cada cosa. Un barrido que
		// visitara los once tipos y les mirara un campo a cada uno dejaria el resto sin testigo.
		//
		// LA CIRUGIA DEL PAYLOAD REVIENTA SI NO CAMBIO NADA. Un nombre de campo mal escrito en las
		// tablas de abajo dejaria la senal intacta -- y una senal intacta rutea, asi que el renglon
		// se pondria rojo de todas formas -- pero reventar dice cual es el renglon en vez de dejarlo
		// leer como una regla que no se cumple.

		[Test]
		public void EveryRequiredTextOfASpecificationFactIsRequired()
		{
			(string Where, SenalDeEvento Signal)[] blanked =
			{
				("opened.revisionCode",     Blank(AnOpening(), "revisionCode")),
				("sealed.revisionCode",     Blank(ASealing(), "revisionCode")),
				("superseded.revisionCode", Blank(ASuperseding(), "revisionCode")),
				("superseded.supersededByCode",
					Blank(ASuperseding(), "supersededByCode")),
				("fluid.revisionCode",      Blank(AFluid(), "revisionCode")),
				("fluid.grade",             Blank(AFluid(), "grade")),
				("tyre.revisionCode",       Blank(ATyre(), "revisionCode")),
				("tyre.size",               Blank(ATyre(), "size")),
				("tyre.ratings",            Blank(ATyre(), "ratings")),
				("fuel.revisionCode",       Blank(AFuel(), "revisionCode")),
				("fuel.grade",              Blank(AFuel(), "grade")),
				("interval.revisionCode",   Blank(AnInterval(), "revisionCode")),
				("price.revisionCode",      Blank(APrice(), "revisionCode")),
				("price.currency",          Blank(APrice(), "currency")),
				("system.revisionCode",     Blank(ASystem(), "revisionCode")),
				("system.systemCode",       Blank(ASystem(), "systemCode")),
				("exclusion.revisionCode",  Blank(AnExclusion(), "revisionCode")),
				("exclusion.systemCode",    Blank(AnExclusion(), "systemCode")),
				("exclusion.exclusion",     Blank(AnExclusion(), "exclusion")),
				("exclusion.condition",     Blank(AnExclusion(), "condition")),
				("policy.revisionCode",     Blank(APolicy(), "revisionCode")),
				("policy.rule",             Blank(APolicy(), "rule")),
				("policy.whatPreservesCover",
					Blank(APolicy(), "whatPreservesCover")),
			};

			Assert.That(blanked.Length, Is.EqualTo(23),
				"Son VEINTITRES sitios donde un texto obligatorio se exige, mas el campo AUSENTE de "
				+ "abajo: veinticuatro en total. Contarlos es lo que impide que el barrido se acorte "
				+ "sin que nadie lo note.");

			Assert.Multiple(() =>
			{
				foreach ((string where, SenalDeEvento signal) in blanked)
					Assert.That(DealershipEventRoute.Route(signal), Is.Null,
						"Un texto en blanco en " + where + " tiene que DESCARTAR la senal. Llegando "
						+ "al comando, la puerta lo rechazaria con una excepcion que sale del bucle "
						+ "de refresh entero.");
			});
		}

		// EL CAMPO AUSENTE ES EL VEINTICUATRO, y va aparte porque es otro camino: Payload.Text
		// contesta cadena vacia cuando la propiedad NO ESTA, y es esa cadena vacia la que el guardia
		// mira. Un blanco prueba el recorte; una ausencia prueba que no hay que buscar la propiedad
		// dos veces.
		[Test]
		public void ASpecificationFactWithNoRevisionCodeAtAllIsDiscarded()
		{
			Assert.That(DealershipEventRoute.Route(Without(AFluid(), "revisionCode")), Is.Null);
			Assert.That(DealershipEventRoute.Route(Without(ASealing(), "revisionCode")), Is.Null);
		}

		[Test]
		public void EveryClosedSetNameOfASpecificationFactMustNameAMember()
		{
			(string Where, SenalDeEvento Signal)[] notAMember =
			{
				("fluid.kind",           Named(AFluid(), "kind", "AceiteDeMotor")),
				("fluid.capacityUnit",   Named(AFluid(), "capacityUnit", "Litros")),
				("fluid.intervalUnit",   Named(AFluid(), "intervalUnit", "Miles")),
				("tyre.position",        Named(ATyre(), "position", "Delantera")),
				("tyre.pressureUnit",    Named(ATyre(), "pressureUnit", "Bar")),
				("fuel.standing",        Named(AFuel(), "standing", "Recomendado")),
				("interval.kind",        Named(AnInterval(), "kind", "CambioDeAceite")),
				("interval.intervalUnit", Named(AnInterval(), "intervalUnit", "Miles")),
				("price.kind",           Named(APrice(), "kind", "CambioDeAceite")),
				("price.milestoneUnit",  Named(APrice(), "milestoneUnit", "Miles")),
				("system.ownLimitUnit",  Named(ASystem(), "ownLimitUnit", "Miles")),
			};

			Assert.That(notAMember.Length, Is.EqualTo(11),
				"Son ONCE nombres de conjunto cerrado en los once hechos, y no once porque haya once "
				+ "hechos: el fluido trae tres y el sello ninguno.");

			Assert.Multiple(() =>
			{
				foreach ((string where, SenalDeEvento signal) in notAMember)
					Assert.That(DealershipEventRoute.Route(signal), Is.Null,
						"Un nombre que el canon no escribe en " + where + " tiene que DESCARTAR la "
						+ "senal, no reventar adentro del comando.");
			});
		}

		// Y ES ESTRICTA CON LAS MAYUSCULAS, igual que la puerta: el canon manda en los nombres, y
		// dejar pasar 'engineoil' pondria esa grafia en el journal de la persona para siempre.
		[Test]
		public void ASpecificationFactWhoseClosedSetNameIsMiscasedIsDiscarded()
		{
			Assert.That(DealershipEventRoute.Route(Named(AFluid(), "kind", "engineoil")), Is.Null);
			Assert.That(DealershipEventRoute.Route(Named(ATyre(), "position", "FRONT")), Is.Null);
			Assert.That(
				DealershipEventRoute.Route(Named(AFuel(), "standing", "minimum")), Is.Null);
		}

		// UN NUMERO AUSENTE NO ES UN CERO. Cero es una afirmacion -- "esta mitad no se publico", "el
		// sistema no tiene limites propios" -- y un evento que omite el campo no esta diciendo cero:
		// no esta diciendo.
		[Test]
		public void EveryRequiredNumberOfASpecificationFactMustBePresent()
		{
			(string Where, SenalDeEvento Signal)[] missing =
			{
				("fluid.capacity",         Without(AFluid(), "capacity")),
				("fluid.intervalDistance", Without(AFluid(), "intervalDistance")),
				("fluid.intervalDays",     Without(AFluid(), "intervalDays")),
				("tyre.coldPressure",      Without(ATyre(), "coldPressure")),
				("interval.intervalDistance",
					Without(AnInterval(), "intervalDistance")),
				("interval.intervalDays",  Without(AnInterval(), "intervalDays")),
				("price.milestoneDistance",
					Without(APrice(), "milestoneDistance")),
				("price.amount",           Without(APrice(), "amount")),
				("system.ownLimitMonths",  Without(ASystem(), "ownLimitMonths")),
				("system.ownLimitDistance",
					Without(ASystem(), "ownLimitDistance")),
			};

			Assert.That(missing.Length, Is.EqualTo(10),
				"Son DIEZ numeros obligatorios repartidos en cinco de los once hechos.");

			Assert.Multiple(() =>
			{
				foreach ((string where, SenalDeEvento signal) in missing)
					Assert.That(DealershipEventRoute.Route(signal), Is.Null,
						"Un " + where + " ausente tiene que DESCARTAR la senal, no entrar como cero.");
			});
		}

		// Y UN NUMERO QUE LLEGA COMO TEXTO tampoco pasa: una capacidad entre comillas es una senal
		// malformada, no una capacidad.
		//
		// EL VALOR VA COMO CADENA JSON VALIDA a proposito, y esa es la correccion que la pasada de
		// falsificacion de T9 forzo. La primera version metia el texto SIN comillas, o sea que
		// producia un payload que no es JSON -- y entonces lo descartaba el catch del parseo, no la
		// comprobacion de ValueKind. La prueba pasaba, y pasaba por la razon equivocada: quitarle a
		// Payload.Amount la comprobacion de que el valor es un NUMERO dejaba la suite en verde.
		//
		// Un numero como texto es lo que un feed manda de verdad -- un serializador que escribe todo
		// entre comillas es un error comun -- asi que este es el caso que hay que medir, no el de un
		// JSON roto.
		[Test]
		public void ASpecificationFactWhoseNumberArrivesAsTextIsDiscarded()
		{
			(string Where, SenalDeEvento Signal)[] asText =
			{
				("fluid.capacity",         AsText(AFluid(), "capacity")),
				("fluid.intervalDistance", AsText(AFluid(), "intervalDistance")),
				("tyre.coldPressure",      AsText(ATyre(), "coldPressure")),
				("price.amount",           AsText(APrice(), "amount")),
				("price.milestoneDistance", AsText(APrice(), "milestoneDistance")),
				("system.ownLimitMonths",  AsText(ASystem(), "ownLimitMonths")),
			};

			Assert.Multiple(() =>
			{
				foreach ((string where, SenalDeEvento signal) in asText)
				{
					Assert.That(signal.Contenido, Does.Contain("\"" + where.Split('.')[1] + "\":\""),
						"La cirugia tiene que dejar un JSON VALIDO con el numero entre comillas: si "
						+ "deja un JSON roto, lo descarta el catch del parseo y este renglon mide otra "
						+ "cosa.");

					Assert.That(DealershipEventRoute.Route(signal), Is.Null,
						"Un numero que llega entre comillas en " + where + " tiene que DESCARTAR la "
						+ "senal, no entrar como cero.");
				}
			});
		}

		// UN NEGATIVO EN UNA ESCALA QUE NO LOS TIENE. Es la mitad de la validacion que se puede
		// adelantar a la frontera: no mira nada del estado, solo los valores de la senal, asi que el
		// rechazo cuesta cero en vez de una cicatriz. Lo que NO se adelanta es lo que depende de una
		// combinacion -- un intervalo sin ninguna de sus dos mitades, una vigencia que termina antes
		// de empezar -- porque ahi la ley es del dominio y copiarla aca seria una segunda fuente de
		// verdad.
		[Test]
		public void ANegativeWhereTheScaleHasNoNegativesIsDiscarded()
		{
			(string Where, SenalDeEvento Signal)[] negative =
			{
				("fluid.capacity",         Named(AFluid(), "capacity", "-4.2")),
				("fluid.intervalDistance", Named(AFluid(), "intervalDistance", "-10000")),
				("fluid.intervalDays",     Named(AFluid(), "intervalDays", "-365")),
				("tyre.coldPressure",      Named(ATyre(), "coldPressure", "-2.4")),
				("interval.intervalDistance",
					Named(AnInterval(), "intervalDistance", "-10000")),
				("interval.intervalDays",  Named(AnInterval(), "intervalDays", "-365")),
				("price.milestoneDistance",
					Named(APrice(), "milestoneDistance", "-10000")),
				("price.amount",           Named(APrice(), "amount", "-89.90")),
				("system.ownLimitMonths",  Named(ASystem(), "ownLimitMonths", "-60")),
				("system.ownLimitDistance",
					Named(ASystem(), "ownLimitDistance", "-100000")),
			};

			Assert.That(negative.Length, Is.EqualTo(10),
				"Son las MISMAS diez posiciones numericas de arriba: cada una se exige presente Y se "
				+ "exige no negativa, que son dos guardias y no uno.");

			Assert.Multiple(() =>
			{
				foreach ((string where, SenalDeEvento signal) in negative)
					Assert.That(DealershipEventRoute.Route(signal), Is.Null,
						"Un " + where + " negativo tiene que DESCARTAR la senal.");
			});
		}

		[Test]
		public void EveryRequiredInstantOfASpecificationFactMustBePresent()
		{
			(string Where, SenalDeEvento Signal)[] missing =
			{
				("opened.publishedAt", Without(AnOpening(), "publishedAt")),
				("price.validFrom",    Without(APrice(), "validFrom")),
				("price.validUntil",   Without(APrice(), "validUntil")),
			};

			Assert.That(missing.Length, Is.EqualTo(3),
				"Son TRES instantes obligatorios: el de la publicacion y los dos de la vigencia.");

			Assert.Multiple(() =>
			{
				foreach ((string where, SenalDeEvento signal) in missing)
					Assert.That(DealershipEventRoute.Route(signal), Is.Null,
						"Un " + where + " ausente tiene que DESCARTAR la senal: sin instante la "
						+ "procedencia o la vigencia se leerian como el ano cero.");
			});
		}

		// LOS TRES DESCARTES QUE TODO HECHO COMPARTE, barridos sobre los once. El tag es el
		// discriminador, el numero de evento es el punto de control, y un payload que no es JSON no
		// se puede leer -- y ninguno de los tres puede quedar cubierto por casualidad en diez de los
		// once.
		[Test]
		public void EverySpecificationFactChecksItsTagItsEventNumberAndItsPayload()
		{
			Assert.Multiple(() =>
			{
				foreach (SenalDeEvento signal in ADealershipNarrative.TheWholeSpecSheet())
				{
					Assert.That(
						DealershipEventRoute.Route(
							new SenalDeEvento(signal.Numero, "TelemetryPing", signal.Contenido)),
						Is.Null,
						"El payload de " + signal.Tipo + " calza campo por campo y aun asi no es "
						+ "ese hecho: lo que dice QUE hecho es, es el tag.");

					Assert.That(
						DealershipEventRoute.Route(
							new SenalDeEvento(0, signal.Tipo, signal.Contenido)),
						Is.Null,
						"Un numero de evento que no avanza desde cero no se puede journalizar como "
						+ "punto de control: " + signal.Tipo);

					Assert.That(
						DealershipEventRoute.Route(new SenalDeEvento(signal.Numero, signal.Tipo, "")),
						Is.Null,
						"Sin payload no hay hecho: " + signal.Tipo);

					Assert.That(
						DealershipEventRoute.Route(
							new SenalDeEvento(signal.Numero, signal.Tipo, "no soy json")),
						Is.Null,
						"Y un payload que no parsea se descarta en vez de reventar: " + signal.Tipo);
				}
			});
		}

		// ------------------------------- la frontera de los dos hechos del perfil del vehiculo
		//
		// LA MISMA POLARIDAD Y LA MISMA LINEA, sobre los dos tipos que trajo T9. Se descarta lo que no
		// se puede DECIR -- un instante ausente, un numero ausente, un nombre que no es de ningun
		// miembro, un negativo en una escala que no los tiene -- y se le deja al dominio lo que es una
		// LEY sobre valores que si se pueden decir.
		//
		// EL BARRIDO ES POR SITIO DE VALIDACION (C57): dos clases de mensaje tienen DOS instantes
		// obligatorios, TRES numeros obligatorios y DOS nombres de conjunto cerrado, porque el sobre
		// trae dos numeros y una unidad y la entrega uno de cada.

		[Test]
		public void EveryRequiredNumberOfAVehicleRecordFactMustBePresent()
		{
			(string Where, SenalDeEvento Signal)[] missing =
			{
				("delivered.reading",  Without(ADelivery(), "reading")),
				("envelope.months",    Without(AnEnvelope(), "months")),
				("envelope.distance",  Without(AnEnvelope(), "distance")),
			};

			Assert.That(missing.Length, Is.EqualTo(3),
				"Son TRES numeros obligatorios: la lectura de la entrega y las dos mitades del sobre.");

			Assert.Multiple(() =>
			{
				foreach ((string where, SenalDeEvento signal) in missing)
					Assert.That(DealershipEventRoute.Route(signal), Is.Null,
						"Un " + where + " ausente tiene que DESCARTAR la senal, no entrar como cero. En "
						+ "estos tres el cero SIGNIFICA algo -- un carro nuevo, un limite ya cumplido -- "
						+ "asi que fabricarlo aca seria afirmar una de esas dos cosas.");
			});
		}

		[Test]
		public void EveryRequiredInstantOfAVehicleRecordFactMustBePresent()
		{
			(string Where, SenalDeEvento Signal)[] missing =
			{
				("delivered.deliveredAt", Without(ADelivery(), "deliveredAt")),
				("envelope.publishedAt",  Without(AnEnvelope(), "publishedAt")),
			};

			Assert.That(missing.Length, Is.EqualTo(2),
				"Son DOS instantes obligatorios: el de la entrega y el de la publicacion del sobre.");

			Assert.Multiple(() =>
			{
				foreach ((string where, SenalDeEvento signal) in missing)
					Assert.That(DealershipEventRoute.Route(signal), Is.Null,
						"Un " + where + " ausente tiene que DESCARTAR la senal: sin el instante de la "
						+ "entrega no hay ancla desde donde medir, y sin el de la publicacion un sobre "
						+ "viejo no se puede ver viejo. " + where);
			});
		}

		[Test]
		public void EveryClosedSetNameOfAVehicleRecordFactMustNameAMember()
		{
			(string Where, SenalDeEvento Signal)[] notAMember =
			{
				("delivered.unit", Named(ADelivery(), "unit", "Miles")),
				("envelope.unit",  Named(AnEnvelope(), "unit", "Miles")),
			};

			Assert.That(notAMember.Length, Is.EqualTo(2),
				"Son DOS unidades de distancia: la en que se leyo el odometro el dia de la entrega y la "
				+ "en que se publico la distancia del sobre. Son dos hechos y no uno.");

			Assert.Multiple(() =>
			{
				foreach ((string where, SenalDeEvento signal) in notAMember)
					Assert.That(DealershipEventRoute.Route(signal), Is.Null,
						"Un nombre que el canon no escribe en " + where + " tiene que DESCARTAR la "
						+ "senal, no reventar adentro del comando.");
			});
		}

		// Y ES ESTRICTA CON LAS MAYUSCULAS, igual que la puerta.
		[Test]
		public void AVehicleRecordFactWhoseUnitIsMiscasedIsDiscarded()
		{
			Assert.That(DealershipEventRoute.Route(Named(ADelivery(), "unit", "unitb")), Is.Null);
			Assert.That(DealershipEventRoute.Route(Named(AnEnvelope(), "unit", "UNITB")), Is.Null);
		}

		[Test]
		public void ANegativeWhereAVehicleRecordScaleHasNoNegativesIsDiscarded()
		{
			(string Where, SenalDeEvento Signal)[] negative =
			{
				("delivered.reading", Named(ADelivery(), "reading", "-120")),
				("envelope.months",   Named(AnEnvelope(), "months", "-36")),
				("envelope.distance", Named(AnEnvelope(), "distance", "-60000")),
			};

			Assert.That(negative.Length, Is.EqualTo(3),
				"Son las MISMAS tres posiciones numericas de arriba: cada una se exige presente Y se "
				+ "exige no negativa, que son dos guardias y no uno.");

			Assert.Multiple(() =>
			{
				foreach ((string where, SenalDeEvento signal) in negative)
					Assert.That(DealershipEventRoute.Route(signal), Is.Null,
						"Un " + where + " negativo tiene que DESCARTAR la senal: un odometro no cuenta "
						+ "hacia atras y un plazo negativo es un limite que no se puede cumplir nunca.");
			});
		}

		[Test]
		public void AVehicleRecordFactWhoseNumberArrivesAsTextIsDiscarded()
		{
			(string Where, SenalDeEvento Signal)[] asText =
			{
				("delivered.reading", AsText(ADelivery(), "reading")),
				("envelope.months",   AsText(AnEnvelope(), "months")),
				("envelope.distance", AsText(AnEnvelope(), "distance")),
			};

			Assert.Multiple(() =>
			{
				foreach ((string where, SenalDeEvento signal) in asText)
				{
					Assert.That(signal.Contenido, Does.Contain("\"" + where.Split('.')[1] + "\":\""),
						"La cirugia tiene que dejar un JSON VALIDO con el numero entre comillas: si deja "
						+ "un JSON roto, lo descarta el catch del parseo y este renglon mide otra cosa.");

					Assert.That(DealershipEventRoute.Route(signal), Is.Null,
						"Un numero que llega entre comillas en " + where + " tiene que DESCARTAR la "
						+ "senal, no entrar como cero.");
				}
			});
		}

		// LOS DOS SI ACEPTAN UN CERO, y decirlo es la otra mitad de la linea: lo que la frontera
		// descarta es lo que NO SE PUEDE DECIR, y cero se puede decir. Que una lectura en cero sea un
		// carro nuevo, y que una mitad del sobre en cero sea un limite ya cumplido el dia de la
		// entrega, son dos leyes de dos agregados -- y copiarlas aca seria una segunda fuente de
		// verdad para cada una. Lo que el dominio hace con cada cero esta en
		// AFactTheDomainRefusesDoesNotBlockTheNarrative.
		[Test]
		public void AZeroIsSayableInBothOfThemAndTheBoundaryLetsItThrough()
		{
			Assert.That(
				DealershipEventRoute.Route(Named(ADelivery(), "reading", "0")),
				Is.InstanceOf<VehicleDelivered>(),
				"Un carro entregado con el odometro en cero es un carro NUEVO, que es el caso normal.");

			Assert.That(
				DealershipEventRoute.Route(Named(AnEnvelope(), "months", "0")),
				Is.InstanceOf<WarrantyEnvelopePublished>(),
				"Y una mitad del sobre en cero pasa la frontera para que la refuse EL SOBRE, con sus "
				+ "palabras. Un cero en un limite es un valor publicado legitimo en este mismo feed un "
				+ "tipo de mensaje mas alla -- asi dice el wire que un sistema cubierto no publico "
				+ "limites propios -- asi que la frontera no puede decidir por el.");
		}

		// LOS TRES DESCARTES QUE TODO HECHO COMPARTE, sobre los dos nuevos. Mismo barrido que el de
		// los once, y hace falta aparte por lo mismo: ninguno de los tres puede quedar cubierto por
		// casualidad en uno de los dos.
		[Test]
		public void EveryVehicleRecordFactChecksItsTagItsEventNumberAndItsPayload()
		{
			Assert.Multiple(() =>
			{
				foreach (SenalDeEvento signal in ADealershipNarrative.TheDeliveryAndTheEnvelope())
				{
					Assert.That(
						DealershipEventRoute.Route(
							new SenalDeEvento(signal.Numero, "TelemetryPing", signal.Contenido)),
						Is.Null,
						"El payload de " + signal.Tipo + " calza campo por campo y aun asi no es ese "
						+ "hecho: lo que dice QUE hecho es, es el tag.");

					Assert.That(
						DealershipEventRoute.Route(
							new SenalDeEvento(0, signal.Tipo, signal.Contenido)),
						Is.Null,
						"Un numero de evento que no avanza desde cero no se puede journalizar como "
						+ "punto de control: " + signal.Tipo);

					Assert.That(
						DealershipEventRoute.Route(new SenalDeEvento(signal.Numero, signal.Tipo, "")),
						Is.Null,
						"Sin payload no hay hecho: " + signal.Tipo);

					Assert.That(
						DealershipEventRoute.Route(
							new SenalDeEvento(signal.Numero, signal.Tipo, "no soy json")),
						Is.Null,
						"Y un payload que no parsea se descarta en vez de reventar: " + signal.Tipo);
				}
			});
		}

		// Y LOS DOS PAYLOADS NO SON INTERCAMBIABLES ENTRE SI, que es el hueco que el barrido del tag
		// de arriba no ve: ahi el tag ajeno es de un tipo que la app no consume, asi que descartarlo
		// no prueba que estos dos se distingan ENTRE ELLOS. Los dos traen unit y un instante, asi que
		// se parecen mas de lo que ningun otro par de esta app se parece.
		[Test]
		public void TheTwoVehicleRecordFactsDoNotAnswerToEachOthersPayload()
		{
			Assert.That(
				DealershipEventRoute.Route(new SenalDeEvento(
					1, WarrantyEnvelopePublished.TheEventType, ADelivery().Contenido)),
				Is.Null,
				"El payload de la entrega con el tag del sobre no trae months ni distance ni "
				+ "publishedAt: no es un sobre.");

			Assert.That(
				DealershipEventRoute.Route(new SenalDeEvento(
					1, VehicleDelivered.TheEventType, AnEnvelope().Contenido)),
				Is.Null,
				"Y el payload del sobre con el tag de la entrega no trae reading ni deliveredAt: no es "
				+ "una entrega.");
		}

		// -------------------------------- la frontera de los tres hechos de las estadias en el taller
		//
		// LA MISMA POLARIDAD Y LA MISMA LINEA, sobre los tres tipos que trajo SH-5. Se descarta lo que
		// no se puede DECIR -- una sucursal en blanco, un asunto en blanco, un instante ausente, unos
		// dias que no nombran ningun dia -- y se le deja al dominio lo que es una LEY sobre valores
		// que si se pueden decir: que haya o no otra estadia abierta, y si los dias son contiguos.
		//
		// EL BARRIDO ES POR SITIO DE VALIDACION (C57): tres clases de mensaje tienen CUATRO textos
		// obligatorios, CUATRO instantes obligatorios y DOS listas de dias, porque la entrada trae dos
		// textos y un instante, la entrega un instante y una lista, y la estadia entera los dos textos,
		// los dos instantes y la lista.

		[Test]
		public void EveryRequiredTextOfAShopFactIsRequired()
		{
			(string Where, SenalDeEvento Blanked, SenalDeEvento Absent)[] required =
			{
				("entry.branch",  Blank(AnEntry(), "branch"),  Without(AnEntry(), "branch")),
				("entry.subject", Blank(AnEntry(), "subject"), Without(AnEntry(), "subject")),
				("stay.branch",   Blank(ACompletedStay(), "branch"),    Without(ACompletedStay(), "branch")),
				("stay.subject",  Blank(ACompletedStay(), "subject"),   Without(ACompletedStay(), "subject")),
			};

			Assert.That(required.Length, Is.EqualTo(4),
				"Son CUATRO textos obligatorios: la sucursal y el asunto de la entrada, y los mismos "
				+ "dos de la estadia entera. La entrega no trae ninguno -- cierra la que ya esta "
				+ "abierta, y esa ya los dijo.");

			Assert.Multiple(() =>
			{
				foreach ((string where, SenalDeEvento blanked, SenalDeEvento absent) in required)
				{
					Assert.That(DealershipEventRoute.Route(blanked), Is.Null,
						"Un " + where + " en blanco tiene que DESCARTAR la senal: una estadia sin "
						+ "sucursal no dice donde estuvo el carro, y una sin asunto no dice por que.");

					Assert.That(DealershipEventRoute.Route(absent), Is.Null,
						"Y ausente tambien, que es el OTRO camino: Payload.Text contesta cadena vacia "
						+ "cuando la propiedad no esta, y es esa cadena la que el guardia mira. "
						+ where);
				}
			});
		}

		[Test]
		public void EveryRequiredInstantOfAShopFactMustBePresent()
		{
			(string Where, SenalDeEvento Signal)[] missing =
			{
				("entry.enteredAt",        Without(AnEntry(), "enteredAt")),
				("collection.collectedAt", Without(ACollection(), "collectedAt")),
				("stay.enteredAt",         Without(ACompletedStay(), "enteredAt")),
				("stay.collectedAt",       Without(ACompletedStay(), "collectedAt")),
			};

			Assert.That(missing.Length, Is.EqualTo(4),
				"Son CUATRO instantes obligatorios: la entrada, la entrega, y los dos extremos de la "
				+ "estadia entera.");

			Assert.Multiple(() =>
			{
				foreach ((string where, SenalDeEvento signal) in missing)
					Assert.That(DealershipEventRoute.Route(signal), Is.Null,
						"Un " + where + " ausente tiene que DESCARTAR la senal: una estadia es un "
						+ "intervalo y sin uno de sus extremos no hay intervalo que registrar.");
			});

			// Y UN INSTANTE QUE NO ES UN INSTANTE tampoco pasa. Va con los de arriba y no aparte:
			// Payload.Instant mira las dos cosas -- que la propiedad este y que diga un instante -- y
			// una senal que trae 'ayer' no esta diciendo uno menos preciso, no esta diciendo ninguno.
			Assert.That(
				DealershipEventRoute.Route(Named(AnEntry(), "enteredAt", "ayer por la tarde")),
				Is.Null);
		}

		// LOS DIAS SON EL HECHO, asi que las cinco formas de no decirlos se barren juntas sobre las dos
		// senales que los traen. Una lista vacia va en esta lista y no en la del dominio: es el blanco
		// de una lista, y un blanco es 'presente y no dice nada', que es exactamente lo que la frontera
		// descarta en un texto.
		[Test]
		public void EveryDayListOfAShopFactHasToSayItsDays()
		{
			(string Where, SenalDeEvento Signal)[] saysNoDay =
			{
				("collection.coveredDays ausente",     Without(ACollection(), "coveredDays")),
				("collection.coveredDays no es lista", Named(ACollection(), "coveredDays", "\"2026-05-11\"")),
				("collection.coveredDays vacia",       Named(ACollection(), "coveredDays", "[]")),
				("collection.coveredDays con un numero", Named(ACollection(), "coveredDays", "[20260511]")),
				("collection.coveredDays en otra forma", Named(ACollection(), "coveredDays", "[\"11/05/2026\"]")),

				("stay.coveredDays ausente",           Without(ACompletedStay(), "coveredDays")),
				("stay.coveredDays no es lista",       Named(ACompletedStay(), "coveredDays", "\"2026-05-11\"")),
				("stay.coveredDays vacia",             Named(ACompletedStay(), "coveredDays", "[]")),
				("stay.coveredDays con un numero",     Named(ACompletedStay(), "coveredDays", "[20260511]")),
				("stay.coveredDays en otra forma",     Named(ACompletedStay(), "coveredDays", "[\"2026-5-11\"]")),
			};

			Assert.That(saysNoDay.Length, Is.EqualTo(10),
				"Son DOS senales que traen dias por CINCO formas de no decirlos: ausente, que no sea "
				+ "una lista, vacia, con un elemento que no es texto y con un elemento que no dice un "
				+ "dia en yyyy-MM-dd. Cada forma es su propio sitio.");

			Assert.Multiple(() =>
			{
				foreach ((string where, SenalDeEvento signal) in saysNoDay)
					Assert.That(DealershipEventRoute.Route(signal), Is.Null,
						"'" + where + "' tiene que DESCARTAR la senal. Un dia de calendario llega "
						+ "RECIBIDO y no sale de ningun instante, asi que una forma que este registro "
						+ "no lee no es un dia menos preciso: no es ninguno.");
			});
		}

		// Y LOS DIAS QUE SI DICEN UN DIA PASAN, aunque el dominio despues los rechace. Es la otra mitad
		// de la linea, y hace falta escribirla: sin ella todo lo de arriba quedaria verde con un
		// Payload.Days que contestara que no a todo.
		[Test]
		public void ADayListThatSaysItsDaysCrossesEvenWhenTheDomainWillRefuseIt()
		{
			Assert.That(
				DealershipEventRoute.Route(ACollection()),
				Is.InstanceOf<ShopCollectionRecorded>());

			Assert.That(
				DealershipEventRoute.Route(
					Named(ACollection(), "coveredDays", "[\"2026-05-11\",\"2026-05-13\"]")),
				Is.InstanceOf<ShopCollectionRecorded>(),
				"Dos dias con un hueco entre ellos SI cruzan: los dos dicen un dia. Que una estadia sea "
				+ "continua es ley del dominio, y copiarla aca seria una segunda fuente de verdad. Lo "
				+ "que el dominio hace con ese hueco esta en "
				+ "AFactTheDomainRefusesDoesNotBlockTheNarrative.");
		}

		// UNA SALIDA ANTERIOR A SU ENTRADA SE DESCARTA EN LA ESTADIA ENTERA Y NO SE PUEDE DESCARTAR EN
		// LA ENTREGA, y las dos mitades van juntas porque una sola de ellas se leeria como una regla
		// que no se aplica en todas partes.
		//
		// LA ESTADIA ENTERA TRAE LOS DOS EXTREMOS, asi que compararlos no mira nada del registro: es
		// una funcion pura de la senal, y descartar cuesta cero. LA ENTREGA NO TRAE LA ENTRADA -- la
		// entrada esta en la estadia abierta, adentro del dominio -- asi que por ese camino la misma
		// ley solo la puede contestar el dominio, con su marca.
		[Test]
		public void AStayWhoseExitComesBeforeItsEntryIsDiscardedAndACollectionsCannotBe()
		{
			Assert.That(
				DealershipEventRoute.Route(Named(ACompletedStay(), "collectedAt", "2026-05-10T08:00:00Z")),
				Is.Null,
				"La estadia dice que salio el 10 y que entro el 11: es un registro que se contradice a "
				+ "si mismo, y los dos extremos vienen en la senal.");

			Assert.That(
				DealershipEventRoute.Route(Named(ACompletedStay(), "collectedAt", ADealershipNarrative.TheFirstShopEntry)),
				Is.InstanceOf<ShopStayRecorded>(),
				"Y salir en el MISMO instante en que entro si cruza: el journal recorta a segundos "
				+ "enteros, asi que rechazar la igualdad aceptaria un registro vivo y lo rechazaria al "
				+ "reproducirlo.");

			foreach (PropertyInfo each in typeof(ShopCollectionRecorded).GetProperties())
				Assert.That(each.Name, Does.Not.Contain("Entered"),
					"ShopCollectionRecorded gano un instante de entrada. El dia que lo tenga, esta "
					+ "frontera SI puede comparar los dos extremos y el comentario de arriba deja de "
					+ "ser cierto.");
		}

		// LOS TRES DESCARTES QUE TODO HECHO COMPARTE, sobre los tres nuevos. Mismo barrido que el de
		// los once y el de los dos, y hace falta aparte por lo mismo: ninguno de los tres puede quedar
		// cubierto por casualidad en dos de los tres.
		[Test]
		public void EveryShopFactChecksItsTagItsEventNumberAndItsPayload()
		{
			Assert.Multiple(() =>
			{
				foreach (SenalDeEvento signal in ADealershipNarrative.TheStays())
				{
					Assert.That(
						DealershipEventRoute.Route(
							new SenalDeEvento(signal.Numero, "TelemetryPing", signal.Contenido)),
						Is.Null,
						"El payload de " + signal.Tipo + " calza campo por campo y aun asi no es ese "
						+ "hecho: lo que dice QUE hecho es, es el tag.");

					Assert.That(
						DealershipEventRoute.Route(
							new SenalDeEvento(0, signal.Tipo, signal.Contenido)),
						Is.Null,
						"Un numero de evento que no avanza desde cero no se puede journalizar como "
						+ "punto de control: " + signal.Tipo);

					Assert.That(
						DealershipEventRoute.Route(new SenalDeEvento(signal.Numero, signal.Tipo, "")),
						Is.Null,
						"Sin payload no hay hecho: " + signal.Tipo);

					Assert.That(
						DealershipEventRoute.Route(
							new SenalDeEvento(signal.Numero, signal.Tipo, "no soy json")),
						Is.Null,
						"Y un payload que no parsea se descarta en vez de reventar: " + signal.Tipo);
				}
			});
		}

		// Y LOS TRES PAYLOADS NO SON INTERCAMBIABLES ENTRE SI, que es el hueco que el barrido del tag
		// de arriba no ve: ahi el tag ajeno es de un tipo que la app no consume. Estos tres se parecen
		// mucho mas entre ellos que a nada -- el de la estadia entera es la union de los otros dos --
		// asi que las SEIS combinaciones se escriben.
		[Test]
		public void TheThreeShopFactsDoNotAnswerToEachOthersPayload()
		{
			(string Says, string Tag, string Payload)[] crossed =
			{
				("el payload de la entrada con el tag de la entrega no trae coveredDays",
					ShopCollectionRecorded.TheEventType, AnEntry().Contenido),
				("el payload de la entrada con el tag de la estadia entera no trae collectedAt",
					ShopStayRecorded.TheEventType, AnEntry().Contenido),

				("el payload de la entrega con el tag de la entrada no trae branch ni enteredAt",
					ShopEntryRecorded.TheEventType, ACollection().Contenido),
				("el payload de la entrega con el tag de la estadia entera no trae branch ni enteredAt",
					ShopStayRecorded.TheEventType, ACollection().Contenido),
			};

			Assert.Multiple(() =>
			{
				foreach ((string says, string tag, string payload) in crossed)
					Assert.That(
						DealershipEventRoute.Route(new SenalDeEvento(1, tag, payload)),
						Is.Null, says);
			});

			// Y LAS DOS QUE FALTAN VAN APARTE porque son las UNICAS dos que no se descartan: el payload
			// de la estadia entera CONTIENE los de los otros dos, asi que con cualquiera de sus dos tags
			// se lee como ese hecho. No es un defecto -- el tag decide, y lo que el tag pidio esta ahi
			// -- pero es la asimetria que un lector supondria al reves.
			Assert.That(
				DealershipEventRoute.Route(new SenalDeEvento(
					1, ShopEntryRecorded.TheEventType, ACompletedStay().Contenido)),
				Is.InstanceOf<ShopEntryRecorded>(),
				"Con el tag de la entrada, el payload de la estadia entera ES una entrada: trae la "
				+ "sucursal, el asunto y el instante de entrada, y lo demas sobra.");

			Assert.That(
				DealershipEventRoute.Route(new SenalDeEvento(
					1, ShopCollectionRecorded.TheEventType, ACompletedStay().Contenido)),
				Is.InstanceOf<ShopCollectionRecorded>(),
				"Y con el tag de la entrega, ES una entrega: trae el instante de entrega y los dias.");
		}

		// ---------------------------------- la frontera de los dos hechos del libro de costos
		//
		// LA MISMA POLARIDAD Y LA MISMA LINEA, sobre los dos tipos que trae CL-7. Se descarta lo que no
		// se puede DECIR -- un asunto en blanco, un monto que no es un numero o que es negativo, una
		// vida util que no deja ninguna ventana, un instante ausente, y un codigo de moneda que este
		// modulo no nombra -- y se le deja al dominio lo que es una LEY sobre valores que si se pueden
		// decir.
		//
		// LA MONEDA ES EL ITEM DE DISENO DE ESTA TAREA Y NO ES UNA GUARDA (L14). Un codigo fuera del
		// conjunto cerrado lo contesta una CONSULTA DE RECONOCIMIENTO TOTAL EN EL BORDE, asi que
		// Interpret devuelve null y la senal se descarta. Nunca una guarda que reviente a traves de
		// Apply: el dominio rechaza el HECHO, y el borde decide si hay hecho que ofrecer.
		//
		// Y EL BORDE NO GUARDA NINGUNA COPIA DE LA LISTA DE CODIGOS. La pregunta se la hace al dominio,
		// que es el unico sitio donde esa lista se escribe, y el renglon de abajo lo afirma comparando
		// las dos decisiones codigo por codigo en vez de creerlo.
		//
		// EL BARRIDO ES POR SITIO DE VALIDACION Y NO POR TIPO DE HECHO (C57): dos clases de mensaje
		// tienen CUATRO textos obligatorios, DOS instantes obligatorios y TRES numeros -- los dos montos
		// y la vida util del duradero -- porque los dos hechos traen varios de cada cosa.

		[Test]
		public void EveryRequiredTextOfACostFactIsRequired()
		{
			(string Where, SenalDeEvento Blanked, SenalDeEvento Absent)[] required =
			{
				("recurring.subject",  Blank(ARecurringCost(), "subject"),
					Without(ARecurringCost(), "subject")),
				("recurring.currency", Blank(ARecurringCost(), "currency"),
					Without(ARecurringCost(), "currency")),
				("durable.subject",    Blank(ADurableCost(), "subject"),
					Without(ADurableCost(), "subject")),
				("durable.currency",   Blank(ADurableCost(), "currency"),
					Without(ADurableCost(), "currency")),
			};

			Assert.That(required.Length, Is.EqualTo(4),
				"Son CUATRO textos obligatorios: el asunto y la moneda de cada uno de los dos hechos. La "
				+ "moneda en blanco la descarta el mismo reconocedor que descarta un codigo que el "
				+ "conjunto no nombra -- una cadena en blanco no nombra ninguna moneda -- asi que aca "
				+ "hay un solo guardia y no dos.");

			Assert.Multiple(() =>
			{
				foreach ((string where, SenalDeEvento blanked, SenalDeEvento absent) in required)
				{
					Assert.That(DealershipEventRoute.Route(blanked), Is.Null,
						"Un " + where + " en blanco tiene que DESCARTAR la senal: un gasto sin asunto no "
						+ "dice en que se fue la plata, y un monto sin moneda no se puede leer.");

					Assert.That(DealershipEventRoute.Route(absent), Is.Null,
						"Y ausente tambien, que es el OTRO camino: Payload.Text contesta cadena vacia "
						+ "cuando la propiedad no esta, y es esa cadena la que el guardia mira. " + where);
				}
			});
		}

		[Test]
		public void EveryRequiredInstantOfACostFactMustBePresent()
		{
			(string Where, SenalDeEvento Signal)[] missing =
			{
				("recurring.incurredAt", Without(ARecurringCost(), "incurredAt")),
				("durable.incurredAt",   Without(ADurableCost(), "incurredAt")),
			};

			Assert.That(missing.Length, Is.EqualTo(2),
				"Son DOS instantes obligatorios: el instante en que se incurrio cada uno de los dos "
				+ "gastos. Sin el, un gasto caeria en el ano cero y ningun periodo lo alcanzaria.");

			Assert.Multiple(() =>
			{
				foreach ((string where, SenalDeEvento signal) in missing)
					Assert.That(DealershipEventRoute.Route(signal), Is.Null,
						"Un " + where + " ausente tiene que DESCARTAR la senal: un gasto se ubica por el "
						+ "instante en que se incurrio, y la ventana de un duradero arranca ahi.");
			});

			// Y UN INSTANTE QUE NO ES UN INSTANTE tampoco pasa. Va con los de arriba y no aparte:
			// Payload.Instant mira las dos cosas -- que la propiedad este y que diga un instante.
			Assert.That(
				DealershipEventRoute.Route(Named(ARecurringCost(), "incurredAt", "el mes pasado")),
				Is.Null);
		}

		// LOS TRES NUMEROS, y cada uno con sus TRES caminos de no decir un numero: ausente, entre
		// comillas, y presente pero fuera de la escala. Son tres guardias por posicion y no uno.
		//
		// Y DE LAS TRES POSICIONES, LA DE LA VIDA UTIL NO SE PUEDE FALSIFICAR POR SEPARADO -- medido, y
		// dicho aca en vez de dejarlo como un verde que parece cobertura (C56). Payload.Whole deja el
		// valor en CERO cuando el campo falta o no es un entero, y la guarda de la linea siguiente
		// descarta el cero: asi que quitarle a la vida util su guardia de PRESENCIA deja la suite entera
		// EN VERDE, porque el guardia de VALOR la cubre. Los dos se quedan igual, y por dos razones: la
		// forma es la misma que los once hechos de la ficha usan, y en las posiciones donde CERO ES
		// LEGAL -- los dos montos, el hito de un precio -- el guardia de presencia SI es el unico que
		// discrimina. Falsificado: el del monto recurrente pone este renglon rojo, el de la vida util no.
		[Test]
		public void EveryRequiredNumberOfACostFactMustBePresent()
		{
			(string Where, SenalDeEvento Absent, SenalDeEvento AsText)[] numbers =
			{
				("recurring.amount", Without(ARecurringCost(), "amount"),
					AsText(ARecurringCost(), "amount")),
				("durable.amount", Without(ADurableCost(), "amount"),
					AsText(ADurableCost(), "amount")),
				("durable.expectedLifeMonths", Without(ADurableCost(), "expectedLifeMonths"),
					AsText(ADurableCost(), "expectedLifeMonths")),
			};

			Assert.That(numbers.Length, Is.EqualTo(3),
				"Son TRES posiciones numericas: el monto de cada uno de los dos hechos, y la vida util "
				+ "del duradero. El recurrente no trae ninguna vida util: la clase la dice CUAL de los "
				+ "dos tipos es, y no hay ranura por donde decirla.");

			Assert.Multiple(() =>
			{
				foreach ((string where, SenalDeEvento absent, SenalDeEvento asText) in numbers)
				{
					Assert.That(DealershipEventRoute.Route(absent), Is.Null,
						"Un " + where + " ausente tiene que DESCARTAR la senal, no entrar como cero: un "
						+ "cero fabricado en el borde seria un monto o una vida que nadie dijo.");

					Assert.That(DealershipEventRoute.Route(asText), Is.Null,
						"Y uno que llega ENTRE COMILLAS tambien: un serializador que escribe todo como "
						+ "texto es un error comun del feed, y es el caso que hay que medir. " + where);
				}
			});
		}

		// UN MONTO QUE NO ES DINERO Y UNA VIDA UTIL QUE NO DEJA VENTANA. Las dos son una funcion pura de
		// la senal -- no miran nada del registro -- asi que descartarlas cuesta CERO en vez de una
		// cicatriz, y es el mismo renglon que la puerta hace con sus dos guardas antes de performar.
		//
		// LO QUE NO SE ADELANTA, y va dicho aca al lado para que la omision no se lea como un olvido: la
		// ventana de un duradero incurrido tan adelante que su fin se pasa del tiempo que se puede decir
		// es una INTERACCION entre dos valores, y adelantarla pide reproducir la aritmetica del dominio
		// en un shell. Esa cicatriza, y quien la mide es
		// AFactTheDomainRefusesDoesNotBlockTheNarrative.
		[Test]
		public void AnAmountThatIsNotMoneyAndALifeThatIsNoWindowAreDiscardedBeforeTheyCanScar()
		{
			(string Where, SenalDeEvento Signal)[] outsideTheScale =
			{
				("recurring.amount negativo", Named(ARecurringCost(), "amount", "-45000.00")),
				("durable.amount negativo",   Named(ADurableCost(), "amount", "-240000.00")),
				("durable.expectedLifeMonths en cero", Named(ADurableCost(), "expectedLifeMonths", "0")),
				("durable.expectedLifeMonths negativa",
					Named(ADurableCost(), "expectedLifeMonths", "-48")),
			};

			Assert.That(outsideTheScale.Length, Is.EqualTo(4),
				"Son CUATRO: los dos montos negativos, y la vida util en cero y negativa. Un monto "
				+ "negativo no es un gasto -- es la sucursal pagandole a la persona -- y una vida que no "
				+ "es positiva no deja ninguna ventana sobre la que repartir el gasto.");

			Assert.Multiple(() =>
			{
				foreach ((string where, SenalDeEvento signal) in outsideTheScale)
					Assert.That(DealershipEventRoute.Route(signal), Is.Null,
						"'" + where + "' tiene que DESCARTAR la senal.");
			});

			// Y UN CERO EN EL MONTO SI CRUZA, que es la otra mitad de la linea: cero es un gasto que se
			// dijo, y sin este renglon todo lo de arriba quedaria verde con un guardia que exigiera
			// positivo. Lo que el borde descarta es lo que no se puede DECIR, y cero se dice.
			Assert.That(
				DealershipEventRoute.Route(Named(ARecurringCost(), "amount", "0")),
				Is.InstanceOf<RecurringCostRecorded>(),
				"Un gasto de cero es un gasto de cero: se registro algo que no costo nada, y eso no es "
				+ "lo mismo que no haber registrado nada.");
		}

		// L14 COMO PRUEBA, y es el item de diseno de esta tarea. Un codigo que el conjunto cerrado no
		// nombra se DESCARTA en el borde: Interpret contesta null y no llega a haber comando, asi que no
		// hay entrada que reventar.
		//
		// Y LA DECISION DEL BORDE ES LA DEL DOMINIO, CODIGO POR CODIGO. El renglon de abajo pregunta las
		// dos cosas sobre la misma lista de codigos -- si la ruta la deja cruzar, y si el dominio la
		// nombra -- y exige que contesten lo mismo. Una segunda copia de la lista en la frontera pondria
		// esto rojo el dia que las dos listas se separaran, que es lo unico que puede pasarle a dos
		// listas de lo mismo en dos sitios.
		[Test]
		public void ACurrencyThisModuleDoesNotNameIsDiscardedAtTheEdgeAndTheDomainNeverSeesIt()
		{
			string[] codes = { "CRC", "USD", "XYZ", "EUR", "crc", "usd", " CRC ", "", "   ", "C" };

			// LAS SENALES SE ARMAN, no se le hace cirugia a una: la cirugia REVIENTA cuando no cambio
			// nada, y reemplazar CRC por CRC no cambia nada. Eso no es un estorbo del aparejo -- es el
			// aparejo diciendo que ese renglon no mediria lo que dice.
			Assert.Multiple(() =>
			{
				foreach (string code in codes)
				{
					bool theDomainNamesIt = Currency.IsNamed(code);

					SenalDeEvento recurring = ADealershipNarrative.RecurringCost(
						1, ADealershipNarrative.TheOilChangeIncurred,
						ADealershipNarrative.TheOilChangeAmount, code,
						ADealershipNarrative.TheOilChangeSubject);

					SenalDeEvento durable = ADealershipNarrative.DurableCost(
						1, ADealershipNarrative.TheTyresIncurred, ADealershipNarrative.TheTyresAmount,
						code, ADealershipNarrative.TheTyresSubject,
						ADealershipNarrative.TheTyresLifeInMonths);

					Assert.That(
						DealershipEventRoute.Route(recurring) != null, Is.EqualTo(theDomainNamesIt),
						"El borde y el dominio no contestan lo mismo sobre el codigo '" + code + "'. La "
						+ "frontera no guarda ninguna lista de codigos: le pregunta al dominio, que es "
						+ "el unico sitio donde esa lista se escribe.");

					Assert.That(
						DealershipEventRoute.Route(durable) != null, Is.EqualTo(theDomainNamesIt),
						"Y lo mismo por el hecho duradero, sobre el codigo '" + code + "': un guardia "
						+ "que estuviera en un solo Interpret dejaria el otro camino abierto.");
				}
			});

			Assert.That(codes.Length, Is.EqualTo(10),
				"Son DIEZ codigos y no dos: los dos que el conjunto nombra, dos que no, los dos en "
				+ "minuscula, uno con espacios alrededor, dos blancos y uno truncado. Con solo los dos "
				+ "buenos y uno malo, un reconocedor que contestara si a todo lo que empieza con C "
				+ "pasaria.");

			// Y LA MONEDA SE COMPRUEBA SOBRE EL CAMPO DE LA MONEDA Y NO SOBRE OTRO. Con el reconocedor
			// mirando el asunto, todo lo de arriba seguiria pasando -- ninguno de los diez codigos es un
			// asunto valido -- asi que hace falta el renglon que cruza los dos: el asunto dice 'XYZ' y
			// la moneda dice 'CRC', y la senal SI cruza.
			Assert.That(
				DealershipEventRoute.Route(ADealershipNarrative.RecurringCost(
					1, ADealershipNarrative.TheOilChangeIncurred,
					ADealershipNarrative.TheOilChangeAmount, "CRC", "XYZ")),
				Is.InstanceOf<RecurringCostRecorded>(),
				"Un asunto que se llama 'XYZ' es un asunto: el reconocedor mira la moneda, y la de esta "
				+ "senal es una de las que el conjunto nombra.");
		}

		// LOS TRES DESCARTES QUE TODO HECHO COMPARTE, sobre los dos nuevos. Mismo barrido que el de los
		// once, el de los dos y el de los tres, y hace falta aparte por lo mismo: ninguno de los dos
		// puede quedar cubierto por casualidad en el otro.
		[Test]
		public void EveryCostFactChecksItsTagItsEventNumberAndItsPayload()
		{
			Assert.Multiple(() =>
			{
				foreach (SenalDeEvento signal in ADealershipNarrative.TheCosts())
				{
					Assert.That(
						DealershipEventRoute.Route(
							new SenalDeEvento(signal.Numero, "TelemetryPing", signal.Contenido)),
						Is.Null,
						"El payload de " + signal.Tipo + " calza campo por campo y aun asi no es ese "
						+ "hecho: lo que dice QUE hecho es, es el tag.");

					Assert.That(
						DealershipEventRoute.Route(
							new SenalDeEvento(0, signal.Tipo, signal.Contenido)),
						Is.Null,
						"Un numero de evento que no avanza desde cero no se puede journalizar como "
						+ "punto de control: " + signal.Tipo);

					Assert.That(
						DealershipEventRoute.Route(new SenalDeEvento(signal.Numero, signal.Tipo, "")),
						Is.Null,
						"Sin payload no hay hecho: " + signal.Tipo);

					Assert.That(
						DealershipEventRoute.Route(
							new SenalDeEvento(signal.Numero, signal.Tipo, "no soy json")),
						Is.Null,
						"Y un payload que no parsea se descarta en vez de reventar: " + signal.Tipo);
				}
			});
		}

		// Y LOS DOS PAYLOADS NO SON INTERCAMBIABLES, con la MISMA asimetria que las estadias: el del
		// duradero CONTIENE el del recurrente -- es el mismo hecho mas la vida util -- asi que con el tag
		// del recurrente SI se lee como un recurrente, y no es un defecto: el tag decide, y lo que el tag
		// pidio esta ahi. Al reves no: al payload del recurrente le falta la vida util, que es
		// obligatoria y no tiene valor por defecto.
		[Test]
		public void TheTwoCostFactsDoNotAnswerToEachOthersPayload()
		{
			Assert.That(
				DealershipEventRoute.Route(new SenalDeEvento(
					1, DurableCostRecorded.TheEventType, ARecurringCost().Contenido)),
				Is.Null,
				"El payload del recurrente con el tag del duradero no trae expectedLifeMonths, y una "
				+ "vida util que no llego no se inventa: es la segunda invencion prohibida.");

			Assert.That(
				DealershipEventRoute.Route(new SenalDeEvento(
					1, RecurringCostRecorded.TheEventType, ADurableCost().Contenido)),
				Is.InstanceOf<RecurringCostRecorded>(),
				"Y con el tag del recurrente, el payload del duradero ES un recurrente: trae el "
				+ "instante, el monto, la moneda y el asunto, y la vida util sobra. Es la asimetria que "
				+ "un lector supondria al reves.");
		}

		// LA IDENTIDAD DEL VEHICULO ES DEL FEED Y NO DEL HECHO, y esto se escribe como prueba porque es
		// una AUSENCIA: 'un VIN que no es el de este carro' no es un descarte que esta frontera pueda
		// hacer, porque ninguna senal trae un VIN. El feed se pide por recurso -- /{recurso}/{id}/events
		// -- asi que el vehiculo es la URL, y un hecho que llegara con un VIN adentro estaria diciendo
		// de que carro es por segunda vez, con la posibilidad de contradecir a la primera.
		//
		// SE ENUMERAN LOS VEINTITRES Y NO SOLO LOS DOS NUEVOS: la regla es de la app entera, y un tipo
		// que la rompiera podria ser cualquiera.
		[Test]
		public void TheIdentityOfTheVehicleIsTheFeedsAndNoFactCarriesAVin()
		{
			List<Type> messages = new List<Type>();

			foreach (Type candidate in typeof(OdometerReported).Assembly.GetTypes())
				if (typeof(IMensajeDeEvento).IsAssignableFrom(candidate) && !candidate.IsInterface)
					messages.Add(candidate);

			Assert.That(messages.Count, Is.EqualTo(23),
				"El mismo VEINTITRES de arriba, re-derivado aca: si este barrido midiera menos tipos que "
				+ "aquel, estaria diciendo de menos hechos que los que hay.");

			Assert.Multiple(() =>
			{
				foreach (Type message in messages)
					foreach (PropertyInfo each in message.GetProperties())
						Assert.That(each.Name, Does.Not.Contain("Vin"),
							message.Name + " gano un VIN. El vehiculo lo dice el recurso del feed, no "
							+ "el hecho: un hecho con VIN adentro puede contradecir a la URL por la que "
							+ "llego, y entonces habria que decidir cual de los dos manda.");
			});
		}

		// ------------------------------------------------------ el aparejo

		private static SenalDeEvento ADelivery()
		{
			return ADealershipNarrative.Delivered(1, TheInstant, 120, "UnitB");
		}

		private static SenalDeEvento AnEnvelope()
		{
			return ADealershipNarrative.Envelope(1, 36, 60000, "UnitB", TheInstant);
		}

		private static SenalDeEvento AnOpening()
		{
			return ADealershipNarrative.RevisionOpened(1, "R1", TheInstant);
		}

		private static SenalDeEvento ASealing()
		{
			return ADealershipNarrative.RevisionSealed(1, "R1");
		}

		private static SenalDeEvento ASuperseding()
		{
			return ADealershipNarrative.RevisionSuperseded(1, "R1", "R2");
		}

		private static SenalDeEvento AFluid()
		{
			return ADealershipNarrative.Fluid(
				1, "R1", "EngineOil", "5W-30", "4.2", "UnitA", 10000, "UnitA", 365);
		}

		private static SenalDeEvento ATyre()
		{
			return ADealershipNarrative.Tyre(1, "R1", "Front", "205/55R16", "2.4", "UnitA", "94V");
		}

		private static SenalDeEvento AFuel()
		{
			return ADealershipNarrative.Fuel(1, "R1", "91 RON regular", "Minimum");
		}

		private static SenalDeEvento AnInterval()
		{
			return ADealershipNarrative.Interval(1, "R1", "FluidChange", 10000, "UnitA", 365);
		}

		private static SenalDeEvento APrice()
		{
			return ADealershipNarrative.Price(
				1, "R1", "FluidChange", 10000, "UnitA", "89.90", "USD",
				TheInstant, "2026-12-31T00:00:00Z");
		}

		private static SenalDeEvento ASystem()
		{
			return ADealershipNarrative.CoveredSystem(1, "R1", "POWERTRAIN", 0, 0, "UnitA");
		}

		private static SenalDeEvento AnExclusion()
		{
			return ADealershipNarrative.Exclusion(
				1, "R1", "POWERTRAIN", "wear parts", "when the part is a consumable");
		}

		private static SenalDeEvento APolicy()
		{
			return ADealershipNarrative.ServicingPolicy(
				1, "R1", "service anywhere", "keep the receipts");
		}

		private static SenalDeEvento AnEntry()
		{
			return ADealershipNarrative.ShopEntry(
				1, ADealershipNarrative.TheFirstShopBranch, ADealershipNarrative.TheFirstShopSubject,
				ADealershipNarrative.TheFirstShopEntry);
		}

		private static SenalDeEvento ACollection()
		{
			return ADealershipNarrative.ShopCollection(
				1, ADealershipNarrative.TheFirstShopCollection, ADealershipNarrative.TheFirstShopDays);
		}

		private static SenalDeEvento ACompletedStay()
		{
			return ADealershipNarrative.CompletedStay(
				1, ADealershipNarrative.TheFirstShopBranch, ADealershipNarrative.TheFirstShopSubject,
				ADealershipNarrative.TheFirstShopEntry, ADealershipNarrative.TheFirstShopCollection,
				ADealershipNarrative.TheFirstShopDays);
		}

		private static SenalDeEvento ARecurringCost()
		{
			return ADealershipNarrative.RecurringCost(
				1, ADealershipNarrative.TheOilChangeIncurred, ADealershipNarrative.TheOilChangeAmount,
				ADealershipNarrative.TheOilChangeCurrency, ADealershipNarrative.TheOilChangeSubject);
		}

		private static SenalDeEvento ADurableCost()
		{
			return ADealershipNarrative.DurableCost(
				1, ADealershipNarrative.TheTyresIncurred, ADealershipNarrative.TheTyresAmount,
				ADealershipNarrative.TheTyresCurrency, ADealershipNarrative.TheTyresSubject,
				ADealershipNarrative.TheTyresLifeInMonths);
		}

		private const string TheInstant = "2025-08-15T00:00:00Z";

		// LAS TRES CIRUGIAS, y las tres REVIENTAN si no cambiaron nada: un nombre de campo mal
		// escrito en una tabla dejaria la senal intacta, y una senal intacta contesta un hecho -- el
		// renglon se pondria rojo, pero por una razon que no es la que dice.
		private static SenalDeEvento Without(SenalDeEvento signal, string field)
		{
			return Rewritten(signal, field, "", "quitando");
		}

		private static SenalDeEvento Blank(SenalDeEvento signal, string field)
		{
			return Rewritten(signal, field, ",\"" + field + "\":\"   \"", "blanqueando");
		}

		private static SenalDeEvento Named(SenalDeEvento signal, string field, string rawValue)
		{
			string quoted = TheValueOf(signal, field).StartsWith("\"", StringComparison.Ordinal)
				? "\"" + rawValue + "\""
				: rawValue;

			return Rewritten(signal, field, ",\"" + field + "\":" + quoted, "reemplazando");
		}

		// El MISMO valor que ya traia, pero entre comillas. Es la unica forma de mandar un numero
		// como texto sin romper el JSON, y romperlo mediria el catch del parseo en vez del tipo.
		private static SenalDeEvento AsText(SenalDeEvento signal, string field)
		{
			string number = TheValueOf(signal, field);

			if (number.StartsWith("\"", StringComparison.Ordinal))
				throw new InvalidOperationException(
					$"El campo '{field}' de un {signal.Tipo} ya viaja como texto, asi que ponerlo "
					+ "entre comillas no cambia nada y el renglon no mediria nada.");

			return Rewritten(
				signal, field, ",\"" + field + "\":\"" + number + "\"", "entrecomillando");
		}

		private static SenalDeEvento Rewritten(
			SenalDeEvento signal, string field, string replacement, string what)
		{
			string payload = Regex.Replace(
				signal.Contenido, ThePropertyPatternFor(field), replacement);

			if (payload == signal.Contenido)
				throw new InvalidOperationException(
					$"La cirugia del payload no cambio nada {what} '{field}' de un "
					+ $"{signal.Tipo}. El campo no se llama asi, y la senal quedo intacta.");

			return new SenalDeEvento(signal.Numero, signal.Tipo, payload);
		}

		private static string TheValueOf(SenalDeEvento signal, string field)
		{
			Match found = Regex.Match(signal.Contenido, ThePropertyPatternFor(field));

			if (!found.Success)
				throw new InvalidOperationException(
					$"El payload de un {signal.Tipo} no trae ningun campo '{field}'.");

			return found.Value.Substring(found.Value.IndexOf(':') + 1);
		}

		// LA ALTERNATIVA DE LA LISTA ES DE SH-5. Los dias de una estadia son el unico campo de esta app
		// que viaja como LISTA de JSON, y sin este renglon las tres cirugias no lo veian -- que no las
		// dejaba pasar en silencio: Rewritten revienta cuando no cambio nada, asi que el rojo decia que
		// la senal quedo intacta en vez de dejar leer un descarte que no ocurrio.
		private static string ThePropertyPatternFor(string field)
		{
			return ",\"" + Regex.Escape(field) + "\":(?:\"[^\"]*\"|\\[[^\\]]*\\]|-?[0-9.]+)";
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
