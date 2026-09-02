using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using DealershipCar.App;
using DealershipCar.Mcp;
using DealershipCar.Mcp.Tools;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LA LEY, DADA VUELTA POR EL SLICE DEL SOBRE, Y CON LA MITAD QUE NADIE HABIA ESCRITO.
	//
	// ESTE FIXTURE SE LLAMABA ThePublishedScopeReachesTheAssistantAndTheStandingDoesNot y ese nombre
	// afirmaba una ley que hoy es FALSA. Tres de sus pruebas afirmaban lo contrario de lo que el
	// slice del sobre hace cierto, asi que se reescribieron; las que siguen valiendo se quedaron
	// donde estaban. Un nombre de clase que enuncia una ley caida es el defecto que este modulo ya
	// encontro dos veces adentro de la prosa de una prueba, asi que el nombre se movio con la ley.
	//
	// LO QUE CAMBIO Y POR QUE. Antes, las tres lecturas de cobertura pedian NUEVE hechos y OCHO eran
	// del vehiculo, y DealershipCarAPI no tenia lectura para ninguno: ni odometro, ni entrega, ni
	// sobre. La consola podia inventarlos porque monta un escenario; una tool MCP no, y un parametro
	// que se los pidiera AL ASISTENTE era el mismo defecto con un esquema encima. Hoy el registro
	// LLEVA los siete hechos -- los pone la agencia, por comando y por feed -- y la puerta compone la
	// respuesta con el codigo del sistema y el instante y nada mas. El arreglo de un llamador que no
	// puede saber un dato no fue pedirselo: fue REGISTRARLO.
	//
	// LAS DOS MITADES QUE ESTE FIXTURE AFIRMA:
	//   1. la posicion SI le llega al asistente, por las dos preguntas -- si el carro sigue adentro
	//      de su sobre, y donde esta parado un sistema contra el alcance publicado;
	//   2. y NINGUN parametro de NINGUNA tool acepta un odometro, un hecho de la entrega ni un
	//      limite del sobre. La mitad 2 es un censo NUEVO: el que ya existia --
	//      TheOwnerIsNeverASourceOfAFactAboutTheirOwnVehicle -- enumera los parametros de la PUERTA,
	//      y un parametro de tool es otra superficie: ese censo se quedaria verde con un
	//      'currentOdometer' recien puesto en una tool.
	//
	// Y LO QUE NO CAMBIO: la regla de servicio en otro taller. Sus dos conteos de servicio externo
	// no existen en ninguna parte -- ni tipo, ni lectura, ni evento -- asi que su posicion sigue sin
	// viajar, exactamente como estaba. Eso tambien se afirma aca, y se RE-DERIVA por reflexion en
	// vez de citarse.
	[TestFixture]
	public sealed class TheStandingReachesTheAssistantAndNoToolAsksForIt
	{
		private const string TheVin = "1HGCM82633A004352";

		private const string TheRevision = "MY2025-R1";
		private const string TheSystemInside = "POWERTRAIN";
		private const string TheSystemOnlyExcluded = "INFOTAINMENT";

		// TODO EN UnitB (C60): UnitA es el primer miembro del enum, asi que una unidad que no cruzara
		// contestaria igual que la primera y el 'mi' de las frases no probaria nada.
		private const string TheUnitEverythingIsIn = "UnitB";

		private static readonly DateTime ThePublication =
			new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime TheDelivery =
			new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private const int TheOdometerAtDelivery = 120;

		// EL PLAZO ES DELIBERADAMENTE MAS LARGO QUE CUALQUIER GARANTIA DE VERDAD, y esa es la unica
		// forma honesta de montar este escenario: las tools leen el reloj del anfitrion -- son la
		// SEGUNDA forma del instante recibido y no pueden recibirlo por parametro -- asi que la
		// prueba no puede elegir el instante y tiene que mover el REGISTRO en vez del reloj. Un
		// vehiculo entregado en 2025 con doscientos cuarenta meses de plazo sigue contestando
		// 'adentro' hasta 2045.
		private const int TheMonths = 240;
		private const int TheDistance = 20000;

		private const int TheOdometerToday = 9000;

		private static readonly DateTime TheDayItWasObserved =
			new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc);

		// LA SEGUNDA LECTURA, y hace falta: un registro que lleva UNA sola de algo no puede
		// discriminar una clave. Con dos, resolver el odometro al instante equivocado se ve.
		private const int TheEarlierOdometer = 6000;

		private static readonly DateTime TheDayTheEarlierOneWasObserved =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void ASealedScopeAndAPublishedEnvelope()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.OpenRevision(TheRevision, ThePublication);
			journal.Api.PublishCoveredSystem(
				TheRevision, TheSystemInside, 0, 0, TheUnitEverythingIsIn);
			journal.Api.PublishCoverageExclusion(
				TheRevision, TheSystemInside, "wear parts", "when the part is a consumable");
			journal.Api.PublishCoverageExclusion(
				TheRevision, TheSystemOnlyExcluded, "screen burn-in", "at any mileage");
			journal.Api.PublishServicingPolicy(
				TheRevision,
				"service may be done at any qualified workshop",
				"keep the invoices and the parts records");
			journal.Api.SealRevision(TheRevision);

			journal.Api.PublishWarrantyEnvelope(
				TheMonths, TheDistance, TheUnitEverythingIsIn, ThePublication);
			journal.Api.RecordDelivery(
				TheDelivery, TheOdometerAtDelivery, TheUnitEverythingIsIn);
			journal.Api.RecordReading(
				TheEarlierOdometer, TheUnitEverythingIsIn, TheDayTheEarlierOneWasObserved,
				"Telemetry");
			journal.Api.RecordReading(
				TheOdometerToday, TheUnitEverythingIsIn, TheDayItWasObserved, "Telemetry");
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// ===== MITAD 1: LA POSICION SI LLEGA ==============================================

		// EL ALCANCE SIGUE LLEGANDO ENTERO: los sistemas adentro, TODAS las exclusiones con su
		// condicion, la revision, y la clausula del propio dominio que dice que esto es alcance y no
		// la resolucion de un reclamo. Callar una exclusion que el registro conoce es la peor
		// respuesta posible a una pregunta de cobertura, asi que se comprueba que salgan las dos.
		[Test]
		public void ThePublishedScopeArrivesWholeWithItsExclusionsAndItsRevision()
		{
			WarrantyScope scope = TheAssistantAsksAbout(TheSystemInside);

			Assert.That(scope.Published, Is.True);
			Assert.That(scope.Revision, Is.EqualTo(TheRevision));
			Assert.That(scope.CurrentRevision, Is.EqualTo(TheRevision));
			Assert.That(scope.CoveredSystems, Is.EqualTo(new[] { TheSystemInside }));
			Assert.That(scope.Exclusions, Has.Count.EqualTo(2),
				"Las DOS exclusiones. Quedarse con la primera elegiria por la persona cual le "
				+ "importa.");
			Assert.That(scope.Says, Does.Contain("wear parts"));
			Assert.That(scope.Says, Does.Contain("screen burn-in"));
			Assert.That(scope.Says, Does.Contain("revisión " + TheRevision));
			Assert.That(scope.Says, Does.Contain("nada sobre cómo se resolvería un reclamo"),
				"La clausula la compone el dominio y viaja adentro de la frase, para que la ley se "
				+ "diga a si misma y no se pierda en la primera capa que reformule la respuesta.");
		}

		// LA PREGUNTA 1 Y LA PREGUNTA 2 -- si el carro sigue bajo garantia, y hasta cuando -- LLEGAN
		// COMPLETAS, y el asistente no puso ni un dato. La frase la compone el registro y trae las
		// DOS mitades, cual llega primero, el sobre contra el que se midio y el odometro con el dia
		// en que se observo.
		[Test]
		public void WhetherThisCarIsStillUnderWarrantyIsAnsweredWithNothingAskedOfAnyone()
		{
			WarrantyStanding standing = TheAssistantAsksIfItIsStillUnderWarranty();

			Assert.That(standing.HasData, Is.True);
			Assert.That(standing.EnvelopeKnown, Is.True);
			Assert.That(standing.Vin, Is.EqualTo(TheVin));

			Assert.That(standing.EnvelopePublished, Is.True);
			Assert.That(standing.DeliveryRecorded, Is.True);
			Assert.That(standing.OdometerKnown, Is.True);
			Assert.That(standing.CanBeEvaluated, Is.True);
			Assert.That(standing.Standing, Is.EqualTo("Inside"));

			// LAS DOS MITADES, dichas las dos. La de distancia es exacta y no depende del reloj; la
			// de tiempo se afirma por su FORMA, porque los meses que quedan bajan solos con el
			// calendario y afirmar el numero seria afirmar el dia en que corre la suite.
			Assert.That(standing.Says, Does.Contain("Distancia: quedan 11120 mi de 20000 mi."),
				"La distancia se cuenta desde el odometro de la entrega y no desde cero.");
			Assert.That(standing.Says, Does.Contain("meses de " + TheMonths + " meses."),
				"Si esto se pone rojo despues de 2045, el escenario cumplio su plazo y hay que "
				+ "volver a montarlo: el plazo esta puesto largo justo porque la tool lee el reloj.");
			Assert.That(standing.Says, Does.Contain("Ninguna de las dos mitades se ha alcanzado"));
			Assert.That(standing.Says, Does.Contain("en vez de pronosticar un ritmo"),
				"Y cual llega primero NO se predice: compararlas necesitaria una tasa, y esa tasa es "
				+ "la conversion que este modulo no hace.");

			// LA LINEA DE HONESTIDAD: contra que odometro se midio y cuando se OBSERVO.
			Assert.That(
				standing.Says,
				Does.Contain("odómetro de 9000 mi observado el 2025-09-01"));
			Assert.That(standing.OdometerObservedAt, Does.StartWith("2025-09-01"));
			Assert.That(standing.Odometer, Is.EqualTo(TheOdometerToday),
				"Y es la SEGUNDA lectura, la vigente al instante preguntado, no la primera que "
				+ "entro. Con una sola lectura en el registro esta linea no discriminaria nada.");
			Assert.That(standing.OdometerUnit, Is.EqualTo("mi"));
			Assert.That(standing.DeliveryOdometer, Is.EqualTo(TheOdometerAtDelivery));
		}

		// LA PREGUNTA 7 -- si la garantia cubre esta falla -- TAMBIEN LLEGA, y en la misma respuesta
		// que el alcance publicado. Son dos frases, cada una entera y cada una del dominio.
		[Test]
		public void WhereThisVehicleStandsOnASystemAlsoReachesTheAssistant()
		{
			WarrantyScope scope = TheAssistantAsksAbout(TheSystemInside);

			Assert.That(scope.EnvelopeKnown, Is.True);
			Assert.That(scope.EnvelopePublished, Is.True);
			Assert.That(scope.DeliveryRecorded, Is.True);
			Assert.That(scope.OdometerKnown, Is.True);

			Assert.That(scope.StandingCanBeEvaluated, Is.True);
			Assert.That(scope.Standing, Is.EqualTo("CoveredWithExclusion"));

			Assert.That(scope.StandingSays, Does.Contain("'POWERTRAIN': Cubierto con exclusión"));
			Assert.That(
				scope.StandingSays,
				Does.Contain("Límites que rigen: " + TheMonths + " meses o " + TheDistance + " mi."),
				"Los limites que rigen son los del SOBRE de este vehiculo, que el sistema cubierto "
				+ "no tiene propios. Ese numero solo puede haber salido del registro.");
			Assert.That(scope.StandingSays, Does.Contain("Este vehículo está dentro de ellos."));
			Assert.That(scope.StandingSays, Does.Contain("revisión " + TheRevision));

			Assert.That(scope.ThisVehicleSays, Does.Contain("Distancia: quedan 11120 mi de 20000 mi."),
				"Y la mitad del vehiculo va aparte y entera, que es lo que deja citarla cuando la "
				+ "compuesta no se puede.");
		}

		// LOS CINCO NOMBRES DEL CONJUNTO YA NO ESTAN AUSENTES -- ES LA PRUEBA QUE SE DIO VUELTA. Y no
		// alcanza con que llegue UNO: se comprueba que el que llega sea el del registro, montando el
		// espejo. Un vehiculo pasado de su limite de distancia contesta OTRO de los cinco, asi que
		// esto no puede estar pasando contra una constante.
		[Test]
		public void TheStandingThatArrivesIsTheRecordsOwnAndNotAConstant()
		{
			List<string> theFive = TheFiveStandingsTheDomainDeclares();

			Assert.That(theFive, Does.Contain(TheAssistantAsksAbout(TheSystemInside).Standing));

			using (AScheduleOnARealJournal past = AVehicleWhoseOdometerReads(46000))
			{
				WarrantyScope mirror =
					new ModelSpecificationTools(new JournalReader(past.Folder, ""))
						.DoesTheWarrantyCover(TheSystemInside);

				Assert.That(mirror.Standing, Is.EqualTo("OutsideEnvelope"));
				Assert.That(mirror.Standing, Does.Not.EqualTo(
					TheAssistantAsksAbout(TheSystemInside).Standing),
					"Dos registros distintos, dos posiciones distintas. Sin este renglon, una tool "
					+ "que devolviera siempre el mismo nombre pasaria la mitad de arriba.");

				Assert.That(
					new WarrantyEnvelopeTools(new JournalReader(past.Folder, ""))
						.IsMyCarStillUnderWarranty().Standing,
					Is.EqualTo("Reached"),
					"Y la del sobre se mueve con el mismo registro.");
			}
		}

		// CADA FRASE QUE EL ASISTENTE RECIBE ES LA FRASE DEL DOMINIO, LETRA POR LETRA. La caratula
		// formatea y no vuelve a derivar: si compusiera una tercera con pedazos de las otras dos, o
		// recalculara una mitad, esto se pondria rojo. El instante se toma de la propia respuesta,
		// porque la tool lee el reloj y nadie mas puede saber cual uso.
		[Test]
		public void EverySentenceTheAssistantGetsIsTheDoorsOwnLetterForLetter()
		{
			WarrantyStanding standing = TheAssistantAsksIfItIsStillUnderWarranty();
			WarrantyScope scope = TheAssistantAsksAbout(TheSystemInside);

			DateTime asOf = DateTime.Parse(
				standing.AskedAt, null, System.Globalization.DateTimeStyles.RoundtripKind);

			Assert.That(standing.Says,
				Is.EqualTo(journal.Api.DescribeWarrantyEnvelope(asOf)));
			Assert.That(standing.PublishedSays,
				Is.EqualTo(journal.Api.DescribePublishedWarrantyEnvelope()));
			Assert.That(scope.Says,
				Is.EqualTo(journal.Api.DescribeCoverageSummary()));
			Assert.That(scope.StandingSays,
				Is.EqualTo(journal.Api.DescribeCoverageForThisVehicle(TheSystemInside, asOf)));
			Assert.That(scope.ThisVehicleSays,
				Is.EqualTo(journal.Api.DescribeWarrantyEnvelope(asOf)));
		}

		// Y SE LE DICE AL ASISTENTE, EN PALABRAS, CONTRA QUE SE MIDIO. La respuesta ya no dice "la
		// posicion no esta aca"; dice donde esta y de cuando es la lectura con que se midio, que es
		// el unico dato que un modelo no puede sacar de la frase sin que se lo pidan.
		[Test]
		public void TheAnswerSaysOutLoudWhatTheStandingWasMeasuredAgainst()
		{
			WarrantyScope scope = TheAssistantAsksAbout(TheSystemInside);

			Assert.That(scope.Detail, Does.Contain("standingSays es dónde está parado ESTE vehículo"));
			Assert.That(scope.Detail, Does.Contain("diga esa fecha"));
			Assert.That(scope.Detail, Does.Not.Contain("no está en standingSays"),
				"La frase vieja se fue con la ley vieja.");

			WarrantyStanding standing = TheAssistantAsksIfItIsStillUnderWarranty();

			Assert.That(standing.Detail, Does.Contain("odometerObservedAt"));
			Assert.That(standing.Detail, Does.Contain("no contra donde está el carro hoy"));
		}

		// Y LAS TRES LECTURAS DE NUEVE ARGUMENTOS SIGUEN PIDIENDO LOS NUEVE. Se quedan porque quien
		// MONTA UN ESCENARIO las puede llamar -- que es la consola -- y ninguna tool las llama. El
		// dia que alguna acepte menos, o que aparezca una decima, esto se pone rojo.
		[Test]
		public void TheThreeCoverageReadsStillTakeTheWholeVehicleWithThem()
		{
			foreach (string each in new[]
			{
				"CanCoverageBeEvaluated", "ReadCoverageStanding", "DescribeCoverage",
			})
			{
				MethodInfo[] found = TheDoorsOverloadsOf(each);

				Assert.That(found, Has.Length.EqualTo(1), $"'{each}'");
				Assert.That(found[0].GetParameters(), Has.Length.EqualTo(9),
					$"'{each}' cambio de forma. Piden el escenario COMPLETO porque quien las llama "
					+ "monta uno entero; una que pidiera ocho y derivara el noveno estaria "
					+ "componiendo con un hecho que nadie le dio.");
			}
		}

		// ===== MITAD 2: EL CENSO DE PARAMETROS DE LAS TOOLS ===============================

		// EL CENSO NUEVO, Y POR QUE HACIA FALTA UNO. TheOwnerIsNeverASourceOfAFactAboutTheirOwnVehicle
		// enumera los parametros de DealershipCarAPI -- la PUERTA -- y ahi las nueve mediciones que
		// cruzan son legitimas porque quien las pasa monta un escenario. Un parametro de TOOL es otra
		// superficie: el que pregunta es un asistente, que no es fuente de ningun hecho de este carro,
		// y el censo de la puerta se quedaria verde con un 'currentOdometer' recien puesto en una tool.
		//
		// SE ARMA IGUAL QUE EL DE LA PUERTA -- POR ELIMINACION SOBRE LA FORMA Y NO POR VOCABULARIO.
		// Quien agregue el parametro de comodidad elige el nombre, asi que reconocer nombres es un
		// proxy que se rompe justo cuando importa. En esta superficie las preguntas se hacen con
		// NOMBRES -- cadenas y miembros de conjuntos cerrados -- y las MEDICIONES son numeros e
		// instantes. Hay UNA excepcion y es una clave de catalogo y no un hecho de ningun carro:
		// 'milestone...', que dice CUAL precio publicado se esta pidiendo.
		//
		// HOY EL CONJUNTO DE MEDICIONES ES VACIO, y por eso hace falta el control de mas abajo: un
		// conjunto vacio tambien lo cumple un barrido que dejo de encontrar las tools.
		private static readonly string[] EveryMeasurementAToolTakes = new string[0];

		[Test]
		public void NoParameterOfAnyToolAnywhereIsAMeasurement()
		{
			Assert.That(EveryMeasurementOfEveryTool(), Is.EquivalentTo(EveryMeasurementAToolTakes),
				"UNA TOOL LE PIDE UNA MEDICION A QUIEN PREGUNTA. Un odometro, una fecha de entrega o "
				+ "un limite del sobre pedidos al asistente son el mismo invento con un esquema "
				+ "encima: el asistente no es fuente de un hecho de un carro que nunca vio. El "
				+ "registro ya lleva los siete hechos desde que la agencia los pone, y la lectura los "
				+ "saca de ahi. Si el parametro nuevo es de verdad una clave de catalogo, su forma se "
				+ "agrega a TheOneCatalogueKey y se dice por que.");
		}

		// EL CONJUNTO ENTERO DE PARAMETROS, CON SU TIPO, Y COMO IGUALDAD DE CONJUNTOS. El de arriba
		// atrapa una medicion; este atrapa CUALQUIER parametro nuevo, de cualquier tipo, incluida la
		// forma que el discriminador no ve -- un numero pasado como TEXTO. No lo clasifica solo: lo
		// pone rojo y obliga a escribirlo, que es donde alguien tiene que decidir si es una clave o
		// un hecho.
		private static readonly string[] EveryParameterEveryToolTakes =
		{
			"does_the_warranty_cover/1(system):String",
			"what_the_service_costs/2(operation):MaintenanceOperation",
			"what_the_service_costs/2(milestoneDistance):Int32",
			"check_one_operation/1(operation):MaintenanceOperation",
		};

		[Test]
		public void TheToolsTakeTheseParametersAndNoOthers()
		{
			Assert.That(EveryParameterOfEveryTool(), Is.EquivalentTo(EveryParameterEveryToolTakes),
				"Una tool gano o perdio un parametro. Se escribe aca a proposito: es igualdad de "
				+ "conjuntos, asi que un decimo argumento en una tool que ya esta en la lista tambien "
				+ "se pone rojo, que es justo lo que una lista de miembros permitidos no veria.");
		}

		// LA UNICA CLAVE DE CATALOGO SIGUE SIENDO UNA. Es lo que impide que el censo de mediciones se
		// vacie por la puerta de atras: agrandar la lista de claves esconde un hecho en vez de
		// censarlo, y agrandarla tiene que costar un rojo.
		[Test]
		public void TheOnlyNumberAToolMayTakeWithoutBeingAMeasurementIsOne()
		{
			Assert.That(TheOneCatalogueKey, Has.Length.EqualTo(1),
				"Si esto crecio, alguien amplio lo que cuenta como clave y no como hecho. Se decide a "
				+ "proposito, con el motivo escrito, o no se decide.");

			Assert.That(TheOneCatalogueKey[0], Is.EqualTo("milestone"));
		}

		// EL CONTROL DE LOS DOS CENSOS, Y CON EL CONJUNTO DE MEDICIONES VACIO ES EL RENGLON QUE LOS
		// SOSTIENE. Prueba dos cosas: que el barrido de verdad encuentra las tools -- las SIETE, en
		// TRES clases -- y que el discriminador SI dispara contra un hecho de vehiculo de verdad. Las
		// cinco mediciones de la lectura de nueve argumentos de la puerta son exactamente los hechos
		// que aparecerian en una tool si alguien agregara el parametro de comodidad.
		[Test]
		public void TheSweepFindsTheToolsAndTheDiscriminatorFiresOnARealVehicleFact()
		{
			Assert.That(EveryToolMethod(), Has.Count.EqualTo(7),
				"El barrido dejo de encontrar las tools y los dos censos no defienden nada.");
			Assert.That(EveryParameterOfEveryTool(), Is.Not.Empty);

			var wouldBeCaught = new List<string>();

			foreach (ParameterInfo each in TheDoorsOverloadsOf("DescribeCoverage")[0].GetParameters())
				if (IsAMeasurement(each)) wouldBeCaught.Add(each.Name);

			Assert.That(wouldBeCaught, Is.EquivalentTo(new[]
			{
				"envelopeMonths", "envelopeDistance", "deliveredAt", "deliveryReading",
				"currentReading", "asOf",
			}), "El discriminador tiene que reconocer un hecho de vehiculo cuando lo ve. Cinco de "
				+ "estos seis son los hechos del carro que la lectura de nueve argumentos recibe, y "
				+ "son los que se pondrian rojos el dia que alguien los ponga en una tool con el "
				+ "nombre que sea. EL SEXTO ES 'asOf' Y ESTA A PROPOSITO: en la puerta es una clave "
				+ "de catalogo, porque quien monta un escenario elige el instante; en una TOOL no lo "
				+ "es, porque el instante se lee del reloj del anfitrion y un parametro dejaria que "
				+ "un modelo preguntara por una fecha que se invento. Es la misma forma juzgada por "
				+ "dos superficies, y este renglon es donde se ve que las dos listas de claves no son "
				+ "la misma lista.");
		}

		// ===== LO QUE NO CAMBIO: LA REGLA DE SERVICIO EN OTRO TALLER ======================

		// SU TEXTO SI SE CITA -- la mitad publicada tiene lectura propia -- y lo que sigue sin viajar
		// es la posicion, porque dos de sus tres conteos no existen en ninguna lectura. La resolucion
		// vieja sigue siendo correcta para todo hecho que el registro TODAVIA no lleva, y este es uno.
		[Test]
		public void ThePublishedServicingRuleIsQuotedAndTheVehiclesStandingIsNot()
		{
			WarrantyScope scope = TheAssistantAsksAbout(TheSystemInside);

			Assert.That(scope.ServicingPolicyPublished, Is.True);
			Assert.That(scope.ServicingPolicyRevision, Is.EqualTo(TheRevision));

			Assert.That(
				scope.ServicingPolicySays,
				Does.Contain("service may be done at any qualified workshop"));
			Assert.That(
				scope.ServicingPolicySays,
				Does.Contain("keep the invoices and the parts records"));
			Assert.That(scope.ServicingPolicySays, Does.Contain(TheRevision));

			Assert.That(scope.Detail, Does.Contain("La regla de servicio en otro taller está en esta respuesta"));
			Assert.That(scope.Detail, Does.Contain("guarda solo el último de los tres"));

			// Y NINGUN CONTEO LLEGA POR NINGUN CAMPO.
			Assert.That(
				JsonSerializer.Serialize(scope),
				Does.Not.Contain("servicios externos con comprobante"));
		}

		// Y LA RAZON SE RE-DERIVA EN VEZ DE CITARSE: los dos conteos de servicio externo no tienen
		// tipo en el dominio ni lectura en la puerta. El dia que alguno aparezca, esto se pone rojo y
		// la frase de arriba se vuelve a mirar en vez de quedar como costumbre.
		[Test]
		public void TheTwoExternalServiceCountsStillHaveNoTypeAndNoRead()
		{
			var found = new List<string>();

			foreach (Type each in typeof(MaintenanceSchedule).Assembly.GetTypes())
				if (each.Name.Contains("ExternalService", StringComparison.Ordinal))
					found.Add(each.Name);

			foreach (MethodInfo each in typeof(DealershipCarAPI).GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
				if (each.Name.Contains("ExternalService", StringComparison.Ordinal))
					found.Add(each.Name);

			Assert.That(found, Is.Empty,
				"Aparecio algo sobre el servicio en otro taller. Si ya se puede saber cuantos hubo y "
				+ "cuantos traen comprobante, la posicion de esa regla se puede contestar y esta tool "
				+ "tiene que contestarla en vez de anunciarla.");
		}

		// Y CUANDO NO HAY REGLA PUBLICADA NO SE ANUNCIA NADA. Sin esto, la frase de arriba podria ser
		// una constante que sale siempre y no diria nada de lo publicado.
		[Test]
		public void AndWithNoServicingRulePublishedNothingIsAnnouncedAboutIt()
		{
			using (var bare = new AScheduleOnARealJournal("1FTFW1ET5DFA00001", "Standard"))
			{
				WarrantyScope scope =
					new ModelSpecificationTools(new JournalReader(bare.Folder, ""))
						.DoesTheWarrantyCover("");

				Assert.That(scope.ServicingPolicyPublished, Is.False);
				Assert.That(scope.Detail, Does.Not.Contain("servicio en otro taller"));
			}
		}

		// "NO NOS LO DIJERON" NO ES "NO LO CUBRE", que es el punto entero del conjunto de cinco. Los
		// tres casos, en una prueba, porque la ley esta en la diferencia entre ellos.
		[Test]
		public void NeverToldAboutItIsNotTheScopeLeavingItOut()
		{
			Assert.That(TheAssistantAsksAbout(TheSystemInside).SystemInTheCatalogue, Is.True,
				"Declarado adentro: en el catalogo, y ademas en coveredSystems.");
			Assert.That(TheAssistantAsksAbout(TheSystemInside).CoveredSystems,
				Does.Contain(TheSystemInside));

			Assert.That(TheAssistantAsksAbout(TheSystemOnlyExcluded).SystemInTheCatalogue, Is.True,
				"Nombrado SOLO por una exclusion: sigue estando en el catalogo. Sin la union de los "
				+ "dos, un sistema que el fabricante excluyo expresamente contestaria que nadie "
				+ "publico nada sobre el, que es lo contrario de lo que pasa.");
			Assert.That(TheAssistantAsksAbout(TheSystemOnlyExcluded).CoveredSystems,
				Does.Not.Contain(TheSystemOnlyExcluded),
				"Y NO esta entre los que el alcance declara adentro. Esa es la diferencia que el "
				+ "asistente tiene que poder ver.");

			Assert.That(TheAssistantAsksAbout("SUSPENSION").SystemInTheCatalogue, Is.False,
				"Y de este no dijeron nada. Reportarlo como que no esta cubierto es como se manda a "
				+ "alguien de vuelta a su casa sin una reparacion a la que tenia derecho a lo "
				+ "menos a preguntar.");
		}

		// EL VOCABULARIO ES DEL FABRICANTE Y LA COMPARACION ES EXACTA, asi que un codigo con otra caja
		// contesta que no lo conocen -- no revienta, y no acierta de casualidad. Se comprueban las
		// dos mitades porque las dos son formas de mentir.
		[Test]
		public void AnUnknownOrDifferentlyCasedCodeIsAnsweredAndNeverThrown()
		{
			Assert.That(TheAssistantAsksAbout("powertrain").SystemInTheCatalogue, Is.False,
				"Minusculas: el codigo es del fabricante y aca no se le arregla la ortografia a "
				+ "nadie.");
			Assert.That(TheAssistantAsksAbout("POWER TRAIN").SystemInTheCatalogue, Is.False);
			Assert.That(TheAssistantAsksAbout("  " + TheSystemInside + "  ").SystemInTheCatalogue,
				Is.True,
				"Los espacios de los extremos si se recortan, que es lo que hace que quien publica y "
				+ "quien pregunta nombren la misma cosa.");

			// Y EL CODIGO EN BLANCO NO LLEGA A LA PUERTA. TheText de DealershipCarAPI revienta con
			// una cadena vacia, y un asistente que llame sin sistema esta preguntando por el alcance
			// entero, no cometiendo un error.
			WarrantyScope whole = TheAssistantAsksAbout("   ");

			Assert.That(whole.System, Is.EqualTo(""));
			Assert.That(whole.SystemInTheCatalogue, Is.False);
			Assert.That(whole.Published, Is.True, "Y el alcance entero contesta igual.");
			Assert.That(whole.StandingSays, Is.EqualTo(""),
				"Sin sistema no hay posicion de un sistema que buscar.");
			Assert.That(whole.ThisVehicleSays, Does.Contain("Distancia:"),
				"Pero la mitad del vehiculo si contesta: no depende de cual sistema se pregunte.");
			Assert.That(whole.Detail, Does.Contain("No se nombró ningún sistema"));

			Assert.Throws<ArgumentException>(
				() => journal.Api.TheVocabularyIsTheManufacturers(""),
				"EL CONTROL: la puerta SI revienta con el codigo en blanco. Si dejara de hacerlo, la "
				+ "guarda de la tool estaria protegiendo de nada.");
		}

		// ------------------------------------------------------ el aparejo

		private WarrantyScope TheAssistantAsksAbout(string system)
		{
			return new ModelSpecificationTools(new JournalReader(journal.Folder, ""))
				.DoesTheWarrantyCover(system);
		}

		private WarrantyStanding TheAssistantAsksIfItIsStillUnderWarranty()
		{
			return new WarrantyEnvelopeTools(new JournalReader(journal.Folder, ""))
				.IsMyCarStillUnderWarranty();
		}

		// El mismo escenario con OTRO odometro, en su propia carpeta, para que el espejo de la
		// posicion no comparta registro con el original.
		private static AScheduleOnARealJournal AVehicleWhoseOdometerReads(int odometer)
		{
			var other = new AScheduleOnARealJournal("JTDKN3DU0A0123456", "Standard");

			other.Api.OpenRevision(TheRevision, ThePublication);
			other.Api.PublishCoveredSystem(TheRevision, TheSystemInside, 0, 0, TheUnitEverythingIsIn);
			other.Api.SealRevision(TheRevision);

			other.Api.PublishWarrantyEnvelope(
				TheMonths, TheDistance, TheUnitEverythingIsIn, ThePublication);
			other.Api.RecordDelivery(TheDelivery, TheOdometerAtDelivery, TheUnitEverythingIsIn);
			other.Api.RecordReading(
				odometer, TheUnitEverythingIsIn, TheDayItWasObserved, "Telemetry");

			return other;
		}

		// Los cinco nombres, sacados del propio conjunto cerrado del dominio.
		private static List<string> TheFiveStandingsTheDomainDeclares()
		{
			var names = new List<string>();

			foreach (FieldInfo each in typeof(CoverageStanding).GetFields(
				BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
				if (each.FieldType == typeof(CoverageStanding))
					names.Add(((CoverageStanding)each.GetValue(null)).Name);

			Assert.That(names, Has.Count.EqualTo(5),
				"Si no son cinco, este barrido dejo de enumerar el conjunto y no defiende nada.");

			return names;
		}

		private static MethodInfo[] TheDoorsOverloadsOf(string name)
		{
			var found = new List<MethodInfo>();

			foreach (MethodInfo each in typeof(DealershipCarAPI).GetMethods())
				if (each.Name == name) found.Add(each);

			return found.ToArray();
		}

		// 'milestone...' dice CUAL precio publicado del catalogo se esta pidiendo: es de la pregunta y
		// no del vehiculo, y es la unica.
		private static readonly string[] TheOneCatalogueKey = { "milestone" };

		private static bool IsACatalogueKey(ParameterInfo parameter)
		{
			return parameter.Name.StartsWith(TheOneCatalogueKey[0], StringComparison.Ordinal);
		}

		// Una MEDICION es un numero o un instante que no es clave de catalogo. En esta superficie los
		// NOMBRES son cadenas y miembros de conjuntos cerrados, asi que el tipo discrimina y el
		// nombre no.
		private static bool IsAMeasurement(ParameterInfo parameter)
		{
			if (IsACatalogueKey(parameter)) return false;

			return parameter.ParameterType == typeof(int)
				|| parameter.ParameterType == typeof(long)
				|| parameter.ParameterType == typeof(decimal)
				|| parameter.ParameterType == typeof(double)
				|| parameter.ParameterType == typeof(DateTime);
		}

		// SE BARRE EL ENSAMBLADO Y NO UNA CLASE, por lo mismo que el barrido de solo-lectura: hay tres
		// clases de tools y la que uno se acuerda de escribir no es el censo.
		private static List<MethodInfo> EveryToolMethod()
		{
			var found = new List<MethodInfo>();

			foreach (Type each in typeof(DealershipCarTools).Assembly.GetTypes())
				foreach (MethodInfo method in each.GetMethods())
					foreach (object attribute in method.GetCustomAttributes(true))
						if (attribute.GetType().Name == "McpServerToolAttribute") found.Add(method);

			return found;
		}

		private static string TheNameOfTheToolOn(MethodInfo method)
		{
			foreach (object attribute in method.GetCustomAttributes(true))
				if (attribute.GetType().Name == "McpServerToolAttribute")
					return (string)attribute.GetType().GetProperty("Name").GetValue(attribute);

			return method.Name;
		}

		private static List<string> EveryParameterOfEveryTool()
		{
			var found = new List<string>();

			foreach (MethodInfo each in EveryToolMethod())
			{
				ParameterInfo[] parameters = each.GetParameters();

				foreach (ParameterInfo parameter in parameters)
					found.Add(TheNameOfTheToolOn(each) + "/" + parameters.Length
						+ "(" + parameter.Name + "):" + parameter.ParameterType.Name);
			}

			return found;
		}

		private static List<string> EveryMeasurementOfEveryTool()
		{
			var found = new List<string>();

			foreach (MethodInfo each in EveryToolMethod())
			{
				ParameterInfo[] parameters = each.GetParameters();

				foreach (ParameterInfo parameter in parameters)
					if (IsAMeasurement(parameter))
						found.Add(TheNameOfTheToolOn(each) + "/" + parameters.Length
							+ "(" + parameter.Name + ")");
			}

			return found;
		}
	}
}
