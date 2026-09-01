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

		// LA RUTA RECONOCE LOS DIECISEIS, y esto es lo que impide que las pruebas de arriba queden
		// verdes para siempre: una ruta que devolviera null a todo las pasaria todas.
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
		}

		// EL TAG DE CADA MENSAJE ES SU PROPIO NOMBRE DE TIPO, y esto se enumera POR REFLEXION y no
		// contra una lista escrita a mano: la lista de la narrativa no puede ver un tipo de mensaje
		// que nadie ruteo y que nadie le agrego. Enumerar los tipos es enumerar los sitios donde la
		// regla se decide (C57), y son DIECIOCHO.
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

			Assert.That(messages.Count, Is.EqualTo(18),
				"Son DIECIOCHO tipos de mensaje: cinco de la agenda, once de la especificacion y DOS "
				+ "del perfil de este vehiculo. Si este numero sube, hay un tipo nuevo -- y los "
				+ "barridos que se apoyan en la lista de la narrativa no lo van a ver hasta que "
				+ "alguien lo agregue ahi.");

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

			// Y LOS DIECIOCHO ESTAN EN LA NARRATIVA, que es lo que hace que los barridos de las otras
			// pruebas midan los dieciocho y no diecisiete. Son TRES listas porque el feed le escribe a
			// tres globales de raiz, y el union de las tres es lo que esta prueba mira.
			HashSet<string> inTheNarrative = new HashSet<string>();

			foreach (SenalDeEvento signal in ADealershipNarrative.TheWholeStory())
				inTheNarrative.Add(signal.Tipo);

			foreach (SenalDeEvento signal in ADealershipNarrative.TheWholeSpecSheet())
				inTheNarrative.Add(signal.Tipo);

			foreach (SenalDeEvento signal in ADealershipNarrative.TheDeliveryAndTheEnvelope())
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

		private static string ThePropertyPatternFor(string field)
		{
			return ",\"" + Regex.Escape(field) + "\":(?:\"[^\"]*\"|-?[0-9.]+)";
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
