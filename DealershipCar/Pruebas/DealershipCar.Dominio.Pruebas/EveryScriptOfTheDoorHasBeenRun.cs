using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using DealershipCar.App;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LA PRUEBA QUE EXISTE POR LA MEDICION S5. Un error de tipeo en un script del DSL NO se
	// descubre al compilar: se descubre al ejecutarlo, y deja una entrada permanente que hace al
	// motor loguear un fallo de rehidratacion en CADA arranque futuro de esa agenda, para siempre,
	// aunque el estado siga siendo correcto.
	//
	// Asi que ninguna linea de DSL de DealershipCarAPI puede llegar al journal de una persona sin
	// haberse ejecutado antes aca. Esta prueba corre CADA UNA de las NOVENTA Y OCHO operaciones de
	// la puerta al menos una vez -- las veintiuna de la agenda (OS2/T6), las siete de la ingesta
	// (OS2/T8: los dos comandos de la bitacora, sus cuatro lecturas, y el Apply), CanBeDeferred, los
	// once comandos de publicacion de la especificacion (T5), las TREINTA Y SIETE lecturas de la
	// especificacion (T6, mas las dos mitades publicadas que agrego
	// PLAN-ModelSpecification-AmendmentMigrations B1 por la ley L7b), los DOS comandos del perfil
	// del vehiculo (WarrantyEnvelope/T4), las ONCE lecturas sin componer del perfil y del odometro
	// (WarrantyEnvelope/T5) y las SEIS COMPUESTAS (WarrantyEnvelope/T6), mas los DOS CONTEOS de la
	// agenda que BuildOrder/SH-1 agrego al arreglar P-2 -- al dia y agendadas.
	// No mira reglas de negocio -- de eso se ocupan las otras pruebas del nivel B -- mira que el
	// script exista, parsee, ate sus parametros y devuelva lo que dijo que devolvia.
	//
	// LAS SEIS DE T6 SON EL CASO MAS CARO DE ESTA OBLIGACION Y POR ESO SE DICE APARTE. Tres de
	// ellas llevan un IF dentro del script, asi que cada una tiene DOS caminos y correrla una vez
	// deja el otro sin ejecutar; y las tres de cobertura nombran TRES globales de raiz en una sola
	// llamada, que es la linea de DSL mas larga del modulo. Los dos caminos de cada if se corren
	// abajo, en dos estados distintos del registro.
	//
	// LA CUENTA DE ARRIBA SE RE-DERIVO CONTRA LA PUERTA y no se copio. Decia setenta y siete, que era
	// cierto antes de T4 y dejo de serlo SIN QUE ESTE FIXTURE SE PUSIERA ROJO, porque la lista de
	// llamadas esta escrita a mano: C57 exactamente, una lista de permitidos que crece en silencio.
	//
	// T5 LE PUSO EL CRUCE QUE FALTABA, y no es el que T4 dejo anotado. T4 propuso cruzar los
	// miembros VOID; la puerta gano once LECTURAS y ninguna escritura, asi que un cruce del lado
	// void habria vuelto a quedarse verde. El cruce es sobre TODOS los miembros declarados --
	// TheDoorRunsExactlyTheseOperationsAndNothingElseWasAddedWithoutBeingRun -- y es igualdad de
	// conjuntos contra la reflexion, no una lista de permitidos.
	//
	// LO QUE ESE CRUCE PRUEBA Y LO QUE NO, dicho y no disimulado. Prueba que nadie agrego ni quito un
	// miembro de la puerta sin tocar este fixture, que es la mitad que fallo en T4. NO prueba que el
	// script de ese miembro se haya corrido: quien agregue el nombre a la lista sin escribir la
	// llamada rompe la obligacion S5 igual, solo que ahora tiene que hacerlo a proposito y por
	// escrito. Es la misma forma que el censo de escrituras de
	// TheOwnerIsNeverASourceOfAFactAboutTheirOwnVehicle, con el mismo limite.
	//
	// LAS OPERACIONES SON NOVENTA Y OCHO Y LOS SCRIPTS SON MAS, y la ingesta es donde la diferencia
	// se vuelve grande: Apply es UNA operacion de la puerta y tiene DIECINUEVE scripts detras -- uno
	// por cada tipo de hecho recibido, mas el comando suelto que avanza el punto de control cuando
	// el dominio rechaza el hecho. Contar operaciones y contar scripts son dos cuentas distintas, y
	// la que S5 obliga a hacer es la de los scripts.
	//
	// ESTE PARRAFO DECIA NOVENTA CUANDO LA PUERTA YA TENIA NOVENTA Y SEIS, y el de arriba decia
	// noventa y seis: dos cuentas de la misma cosa en el mismo encabezado, una de ellas vieja. Es el
	// mismo defecto que dejo escrito "setenta y siete" cuando la puerta tenia setenta y nueve, y es
	// la tercera vez que aparece en este archivo. Una cuenta afirmada en prosa envejece en silencio,
	// asi que la que manda es TheOperationsAreNinetyEight, que la afirma al lado de la lista que
	// cuenta.
	[TestFixture]
	public sealed class EveryScriptOfTheDoorHasBeenRun
	{
		private const string TheVin = "1HGCM82633A004352";
		private const string TheRevision = "R1";
		private const int TwelveMonthsInDays = 365;
		private const int TenThousandUnits = 10000;

		private static readonly DateTime Delivery = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
		private const int OdometerAtDelivery = 30000;

		private static readonly DateTime ThePublication =
			new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// ===== EL CRUCE QUE T4 DEJO ANOTADO ===============================================

		// TODA OPERACION DECLARADA DE LA PUERTA, ENUMERADA, Y COMO IGUALDAD DE CONJUNTOS contra la
		// reflexion. Es lo que este fixture no tenia cuando T4 le agrego dos comandos a la puerta y
		// se quedo verde afirmando de una puerta mas chica que la que hay.
		//
		// SE ENUMERAN TODAS Y NO SOLO LAS QUE ESCRIBEN. T4 anoto el cruce del lado void; T5 le agrego
		// a la puerta once LECTURAS y ninguna escritura, asi que ese cruce habria vuelto a quedarse
		// verde. Lo que S5 obliga a correr es cada SCRIPT, y una lectura tiene script igual que un
		// comando.
		//
		// Y NO ES UNA LISTA DE PERMITIDOS: es igualdad, asi que tambien se pone rojo si alguien BORRA
		// una operacion, y el rojo dice cual. Su limite esta escrito arriba -- fuerza a que nadie
		// crezca la puerta sin tocar este archivo; no puede comprobar que la llamada se haya escrito.
		private static readonly string[] EveryOperationThisFixtureRuns =
		{
			"AddOwnerOperation", "AdoptPlanRevision", "Apply", "CanBeDeferred",
			"CanCoverageBeEvaluated", "CanCoverageBeEvaluatedForThisVehicle",
			"CanTheEnvelopeBeEvaluated", "CompleteOperation", "CountDue", "CountOverdue",
			"CountScheduled", "CountUpToDate",
			"CoverageExclusions", "CoveredSystems", "DeclareUsageProfile", "DeferOperation",
			"DeferralsUsed", "DescribeCoverage", "DescribeCoverageForThisVehicle",
			"DescribeCoverageSummary", "DescribeFluid",
			"DescribeFuel", "DescribePublishedServiceInterval",
			"DescribePublishedServicingElsewhereRule", "DescribePublishedWarrantyEnvelope",
			"DescribeServiceInterval", "DescribeServicePrice", "DescribeServicingElsewhereRule",
			"DescribeTyre", "DescribeWarrantyEnvelope",
			"Evaluate", "HasASealedRevision", "HasAnOdometerReading",
			"HasAnythingDue", "HasBeenEvaluated", "HasDeliveryBeenRecorded", "IsCoveragePublished",
			"IsFluidPublished", "IsFuelPublished", "IsServiceIntervalPublished",
			"IsServicePricePublished", "IsServicingPolicyPublished", "IsTyrePublished",
			"IsWarrantyEnvelopePublished", "LastEvaluatedAt", "LastEventApplied", "MissedCount",
			"OpenRevision", "PermittedFuelGrades", "ProhibitedFuelGrades",
			"PublishCoverageExclusion", "PublishCoveredSystem", "PublishFluidSpecification",
			"PublishFuelGrade", "PublishServiceInterval", "PublishServicePrice",
			"PublishServicingPolicy", "PublishTyreSpecification", "PublishWarrantyEnvelope",
			"ReadCoverageRevision", "ReadCoverageStanding",
			"ReadCoverageStandingForThisVehicle", "ReadCurrentRevisionCode",
			"ReadCurrentRevisionPublishedAt", "ReadDeliveredAt", "ReadDeliveryReading",
			"ReadDeliveryUnit", "ReadDueReason", "ReadDueState", "ReadEnvelopePublishedAt",
			"ReadEnvelopeStanding",
			"ReadFluidRevision", "ReadFuelRevision", "ReadMinimumFuelGrade", "ReadModel",
			"ReadNextDueKind", "ReadOdometer", "ReadOdometerObservedAt", "ReadOdometerUnit",
			"ReadRevisionState", "ReadServiceIntervalRevision", "ReadServicePriceRevision",
			"ReadServicingPolicyRevision", "ReadTyreRevision", "ReadVin",
			"RecordConditionThreshold", "RecordDelivery", "RecordReading", "RegisterDevice",
			"RemoveOwnerOperation", "SealRevision", "SeedPlanOperation", "StoreCredential",
			"StoredDeviceKey", "StoredRefreshToken", "StoredToken", "SupersedeRevision",
			"TheVocabularyIsTheManufacturers",
		};

		[Test]
		public void TheDoorRunsExactlyTheseOperationsAndNothingElseWasAddedWithoutBeingRun()
		{
			var declared = new List<string>();

			foreach (MethodInfo each in typeof(DealershipCarAPI).GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
				declared.Add(each.Name);

			Assert.That(declared, Is.EquivalentTo(EveryOperationThisFixtureRuns),
				"La puerta gano o perdio una operacion y esta prueba no se entero. Un script de DSL "
				+ "que nunca se ejecuto se descubre al correrlo, y si eso pasa en el journal de una "
				+ "persona la entrada queda ahi haciendo loguear un fallo de rehidratacion en cada "
				+ "arranque futuro. Agregue la operacion nueva a esta lista Y escriba la llamada que "
				+ "corre su script.");
		}

		// Y LA CUENTA DE LA PROSA SE RE-DERIVA CONTRA LA LISTA, no se copia. Es el mismo defecto que
		// dejo escrito "setenta y siete" cuando la puerta ya tenia setenta y nueve: una cuenta
		// afirmada en prosa envejece en silencio, y esta afirmada al lado de lo que cuenta.
		[Test]
		public void TheOperationsAreNinetyEight()
		{
			Assert.That(EveryOperationThisFixtureRuns, Has.Length.EqualTo(98));
		}

		// La semilla corre en OnHydrated, asi que para cuando la prueba arranca la agenda ya
		// existe. Si el upgrade no hubiera corrido, la primera lectura reventaria con "schedule no
		// existe" en vez de contestar el VIN.
		[Test]
		public void TheSeedRanAndLeftTheScheduleAsTheRootGlobal()
		{
			Assert.That(journal.Api.ReadVin(), Is.EqualTo(TheVin));
		}

		[Test]
		public void EveryCommandOfTheDoorParsesBindsAndRuns()
		{
			journal.Api.SeedPlanOperation(
				"FluidChange", "TimeOrDistance", TwelveMonthsInDays, TenThousandUnits, "UnitA",
				true, false,
				Delivery, OdometerAtDelivery, "UnitA");

			journal.Api.AdoptPlanRevision("R2", new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc));

			journal.Api.AddOwnerOperation(
				"Diagnostic", "TimeOnly", TwelveMonthsInDays, 0, "UnitA",
				Delivery, OdometerAtDelivery, "UnitA");

			journal.Api.DeclareUsageProfile(
				"SevereDuty", new DateTime(2025, 8, 1, 0, 0, 0, DateTimeKind.Utc));

			journal.Api.RecordReading(
				40000, "UnitA", new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc), "Telemetry");

			journal.Api.RecordConditionThreshold(
				"Diagnostic", new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc));

			EvaluationOutcome moved = journal.Api.Evaluate(
				new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc));

			Assert.That(moved.WasEvaluated, Is.True);
			Assert.That(moved.TransitionCount, Is.GreaterThan(0));
			Assert.That(moved.AnythingBecameDue, Is.True);

			journal.Api.DeferOperation(
				"FluidChange", "Cost", new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc));

			journal.Api.CompleteOperation(
				"Diagnostic", new DateTime(2026, 1, 21, 0, 0, 0, DateTimeKind.Utc), 40100, "UnitA");

			journal.Api.RemoveOwnerOperation("Diagnostic");

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("Deferred"));
		}

		[Test]
		public void EveryReadOfTheDoorParsesBindsAndComesBackTyped()
		{
			journal.Api.SeedPlanOperation(
				"FluidChange", "TimeOrDistance", TwelveMonthsInDays, TenThousandUnits, "UnitA",
				true, false,
				Delivery, OdometerAtDelivery, "UnitA");

			journal.Api.RecordReading(
				40000, "UnitA", new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc), "Telemetry");
			journal.Api.Evaluate(new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc));

			Assert.That(journal.Api.ReadVin(), Is.EqualTo(TheVin));
			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("Due"));
			Assert.That(journal.Api.CountDue(), Is.EqualTo(1));
			Assert.That(journal.Api.CountOverdue(), Is.EqualTo(0));
			Assert.That(journal.Api.CountScheduled(), Is.EqualTo(1));
			Assert.That(journal.Api.CountUpToDate(), Is.EqualTo(0));
			Assert.That(journal.Api.DeferralsUsed("FluidChange"), Is.EqualTo(0));
			Assert.That(journal.Api.CanBeDeferred("FluidChange"), Is.True);
			Assert.That(journal.Api.HasAnythingDue(), Is.True);
			Assert.That(journal.Api.ReadNextDueKind(), Is.EqualTo("FluidChange"));
			Assert.That(journal.Api.ReadDueReason("FluidChange"), Does.Contain("Vencida"));
			Assert.That(journal.Api.MissedCount(), Is.EqualTo(0));
			Assert.That(journal.Api.HasBeenEvaluated(), Is.True);
			Assert.That(
				journal.Api.LastEvaluatedAt(),
				Is.EqualTo(new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc)));
		}

		// Las dos lecturas guardadas de E41, del lado del wire. El DomainException del dominio es
		// internal y ninguna caratula lo puede nombrar, asi que la puerta lo traduce a una
		// excepcion que si -- y la polaridad se conserva: no se inventa una respuesta vacia.
		[Test]
		public void TheTwoGuardedReadsThrowInsteadOfInventingAnEmptyAnswer()
		{
			Assert.That(journal.Api.HasAnythingDue(), Is.False);
			Assert.Throws<InvalidOperationException>(() => journal.Api.ReadNextDueKind());

			Assert.That(journal.Api.HasBeenEvaluated(), Is.False);
			Assert.Throws<InvalidOperationException>(() => journal.Api.LastEvaluatedAt());
		}

		// LOS ONCE COMANDOS DE PUBLICACION QUE TRAJO T5, cada uno una vez, contra la tercera global
		// de raiz. Se abren DOS revisiones porque sustituir necesita una sucesora sellada: sin eso,
		// SupersedeRevision seria el unico script de la puerta que nunca corrio.
		//
		// El fluido va con un decimal con cero a la derecha y el precio con centavos: los dos
		// caminos que pasan por la normalizacion de la frontera.
		[Test]
		public void EveryPublishCommandOfTheDoorParsesBindsAndRuns()
		{
			journal.Api.OpenRevision("R1", ThePublication);

			journal.Api.PublishFluidSpecification(
				"R1", "EngineOil", "5W-30", 2.40m, "UnitA", TenThousandUnits, "UnitA",
				TwelveMonthsInDays);

			journal.Api.PublishTyreSpecification(
				"R1", "Front", "205/55R16", 2.4m, "UnitA", "94V");

			journal.Api.PublishFuelGrade("R1", "91 RON", "Minimum");

			journal.Api.PublishServiceInterval(
				"R1", "FluidChange", TenThousandUnits, "UnitA", TwelveMonthsInDays);

			journal.Api.PublishServicePrice(
				"R1", "FluidChange", TenThousandUnits, "UnitA", 75.50m, "USD",
				ThePublication, ThePublication.AddYears(1));

			// Un cero en las DOS mitades es como el wire dice que el sistema no publico limites
			// propios, y la traduccion es del dominio: la puerta lo manda tal cual.
			journal.Api.PublishCoveredSystem("R1", "POWERTRAIN", 0, 0, "UnitA");

			journal.Api.PublishCoverageExclusion(
				"R1", "POWERTRAIN", "wear parts", "when the part is consumable");

			journal.Api.PublishServicingPolicy("R1", "service anywhere", "keep the receipts");

			journal.Api.SealRevision("R1");

			journal.Api.OpenRevision("R2", ThePublication.AddMonths(6));
			journal.Api.PublishFuelGrade("R2", "95 RON", "Permitted");
			journal.Api.SealRevision("R2");

			journal.Api.SupersedeRevision("R1", "R2");
		}

		// LOS DOS SCRIPTS DEL PERFIL QUE TRAJO T4, y estan aca por lo que este fixture defiende y no
		// por simetria: cada uno es una linea de DSL que ninguna prueba del nivel A puede ejecutar, y
		// S5 midio que un tipeo en una de ellas se descubre en el journal de una persona.
		//
		// ESTE FIXTURE ESTABA CORTO Y T4 LO DESTAPO. La lista de llamadas esta escrita a mano, asi
		// que dos comandos nuevos en la puerta no lo ponen rojo: se queda verde afirmando de una
		// puerta mas chica que la que hay. Es C57 exactamente -- una lista de permitidos que crece en
		// silencio -- y el arreglo estructural (cruzar lo que este fixture corre contra los miembros
		// void de la puerta, por reflexion) no cabe en T4. Lo que si cabe es no dejar los dos scripts
		// de T4 sin correr, que es lo que este metodo hace.
		//
		// SOLO SE AFIRMA LA FORMA. Que el script parsee, ate sus parametros y no sea rechazado; que
		// los dos hechos se puedan volver a leer es de
		// AnEnvelopeAndADeliveryArrivePublishedThroughTheDoor, porque T4 no le agrega a la puerta
		// ninguna lectura del perfil con que mirarlos desde aca.
		[Test]
		public void BothProfileCommandsOfTheDoorParseBindAndRun()
		{
			Assert.DoesNotThrow(() => journal.Api.PublishWarrantyEnvelope(
				36, 60000, "UnitB", ThePublication));

			Assert.DoesNotThrow(() => journal.Api.RecordDelivery(
				Delivery, OdometerAtDelivery, "UnitB"));

			// Y OTRA VEZ, que es el camino que ninguna primera llamada toca: el sobre re-publicado
			// pasa por la comparacion de instantes y la entrega re-registrada por el reemplazo
			// entero. Los dos son scripts ya corridos, pero cuerpos del dominio que no.
			Assert.DoesNotThrow(() => journal.Api.PublishWarrantyEnvelope(
				24, 40000, "UnitB", ThePublication.AddMonths(6)));

			Assert.DoesNotThrow(() => journal.Api.RecordDelivery(
				Delivery.AddDays(30), OdometerAtDelivery + 100, "UnitB"));
		}

		// LAS ONCE LECTURAS SIN COMPONER QUE TRAJO T5, cada una una vez, contra un registro que tiene
		// los tres hechos: el sobre publicado, la entrega registrada y una lectura del odometro. Es
		// el unico estado en que corren los ONCE scripts -- una guardada sin su hecho rechaza en la
		// puerta y su script no llega a ejecutarse nunca.
		//
		// Lo que se afirma es la FORMA: que el script parsee, ate su parametro y vuelva TIPADO. De
		// que cada una conteste lo suyo se ocupa AProfileIsReadBackThroughTheDoor, y de la polaridad
		// EveryGuardedProfileReadRefusesInsteadOfInventing.
		[Test]
		public void EveryProfileAndOdometerReadOfTheDoorParsesBindsAndComesBackTyped()
		{
			journal.Api.PublishWarrantyEnvelope(36, 60000, "UnitB", ThePublication);
			journal.Api.RecordDelivery(Delivery, OdometerAtDelivery, "UnitB");
			journal.Api.RecordReading(
				40000, "UnitB", new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc), "Telemetry");

			DateTime asked = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

			Assert.That(journal.Api.IsWarrantyEnvelopePublished(), Is.True);
			Assert.That(
				journal.Api.DescribePublishedWarrantyEnvelope(), Does.Contain("60000 mi"));
			Assert.That(journal.Api.ReadEnvelopePublishedAt(), Is.EqualTo(ThePublication));

			Assert.That(journal.Api.HasDeliveryBeenRecorded(), Is.True);
			Assert.That(journal.Api.ReadDeliveredAt(), Is.EqualTo(Delivery));
			Assert.That(journal.Api.ReadDeliveryReading(), Is.EqualTo(OdometerAtDelivery));
			Assert.That(journal.Api.ReadDeliveryUnit(), Is.EqualTo("mi"));

			Assert.That(journal.Api.HasAnOdometerReading(asked), Is.True);
			Assert.That(journal.Api.ReadOdometer(asked), Is.EqualTo(40000));
			Assert.That(journal.Api.ReadOdometerUnit(asked), Is.EqualTo("mi"));
			Assert.That(
				journal.Api.ReadOdometerObservedAt(asked),
				Is.EqualTo(new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc)));
		}

		// LAS SEIS COMPUESTAS QUE TRAJO T6, LOS DOS CAMINOS DE CADA UNA. Tres de los seis scripts
		// llevan un if adentro, asi que un solo estado del registro deja la otra rama sin correr --
		// y una rama de DSL que nunca se ejecuto se descubre igual de tarde que un script entero.
		//
		// SE CORREN EN DOS ESTADOS: sin lectura del odometro (rama else de las tres del sobre, y las
		// tres de cobertura rechazando en la puerta) y con lectura (rama then de las tres del sobre,
		// y las tres de cobertura llegando al script de tres globales).
		//
		// Lo que se afirma es la FORMA. De que la frase compuesta sea la misma que la de nueve
		// argumentos se ocupa TheDoorComposedTheAnswerInsteadOfInventingIt, y de las polaridades en
		// los cuatro estados del registro ThreeAbsencesAreThreeDifferentAnswersThroughTheDoor.
		[Test]
		public void EveryComposedReadOfTheDoorParsesBindsAndRunsOnBothOfItsPaths()
		{
			ASealedSpecSheet();

			journal.Api.PublishWarrantyEnvelope(36, 60000, "UnitA", ThePublication);
			journal.Api.RecordDelivery(Delivery, OdometerAtDelivery, "UnitA");

			DateTime asked = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

			// SIN LECTURA: la rama else de las tres del sobre, y las tres de cobertura rechazadas
			// antes de performar.
			Assert.That(journal.Api.HasAnOdometerReading(asked), Is.False);

			Assert.That(journal.Api.CanTheEnvelopeBeEvaluated(asked), Is.False);
			Assert.Throws<InvalidOperationException>(
				() => journal.Api.ReadEnvelopeStanding(asked));
			Assert.That(
				journal.Api.DescribeWarrantyEnvelope(asked), Does.Contain("ninguna lectura del odómetro"));

			Assert.That(
				journal.Api.CanCoverageBeEvaluatedForThisVehicle("POWERTRAIN", asked), Is.False);
			Assert.Throws<InvalidOperationException>(
				() => journal.Api.ReadCoverageStandingForThisVehicle("POWERTRAIN", asked));
			Assert.Throws<InvalidOperationException>(
				() => journal.Api.DescribeCoverageForThisVehicle("POWERTRAIN", asked));

			// CON LECTURA: la rama then de las tres del sobre, y el script de TRES globales de las
			// tres de cobertura.
			journal.Api.RecordReading(
				40000, "UnitA", new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc), "Telemetry");

			Assert.That(journal.Api.CanTheEnvelopeBeEvaluated(asked), Is.True);
			Assert.That(journal.Api.ReadEnvelopeStanding(asked), Is.EqualTo("Inside"));
			Assert.That(
				journal.Api.DescribeWarrantyEnvelope(asked), Does.Contain("40000 km"));

			Assert.That(
				journal.Api.CanCoverageBeEvaluatedForThisVehicle("POWERTRAIN", asked), Is.True);
			Assert.That(
				journal.Api.ReadCoverageStandingForThisVehicle("POWERTRAIN", asked),
				Is.EqualTo("CoveredWithExclusion"));
			Assert.That(
				journal.Api.DescribeCoverageForThisVehicle("POWERTRAIN", asked),
				Does.Contain("wear parts"));
		}

		// Y NINGUNA DE LAS SEIS ESCRIBE UNA ENTRADA. Mismo defecto de una sola letra que la de las
		// lecturas de la especificacion, y aca importa mas: tres de las seis las va a llamar la
		// pantalla en cada apertura de la app.
		[Test]
		public void NoComposedReadOfTheDoorWritesAnEntry()
		{
			journal.Api.PublishWarrantyEnvelope(36, 60000, "UnitA", ThePublication);
			journal.Api.RecordDelivery(Delivery, OdometerAtDelivery, "UnitA");
			journal.Api.RecordReading(
				40000, "UnitA", new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc), "Telemetry");
			ASealedSpecSheet();

			DateTime asked = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

			string before = TheBytesOf(journal.Folder);

			journal.Api.CanTheEnvelopeBeEvaluated(asked);
			journal.Api.ReadEnvelopeStanding(asked);
			journal.Api.DescribeWarrantyEnvelope(asked);
			journal.Api.CanCoverageBeEvaluatedForThisVehicle("POWERTRAIN", asked);
			journal.Api.ReadCoverageStandingForThisVehicle("POWERTRAIN", asked);
			journal.Api.DescribeCoverageForThisVehicle("POWERTRAIN", asked);

			Assert.That(TheBytesOf(journal.Folder), Is.EqualTo(before),
				"El journal quedo byte a byte como estaba. Una lectura que escribe le agrega una "
				+ "entrada al journal de la persona cada vez que abre la app, para siempre, y el "
				+ "estado sigue siendo correcto -- asi que ninguna otra prueba se pondria roja.");
		}

		// LOS NUEVE SCRIPTS DE LA BITACORA: dos comandos y siete lecturas. Son siete y no cuatro
		// porque las tres lecturas de credencial son GUARDADAS del lado del dominio -- la puerta
		// pregunta la presencia y despues el valor -- asi que cada una son dos scripts, y hay que
		// correrlas con y sin credencial guardada para tocar los dos caminos.
		[Test]
		public void EveryScriptOfTheEventLogParsesBindsAndRuns()
		{
			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(0L));
			Assert.That(journal.Api.StoredDeviceKey(), Is.EqualTo(""));
			Assert.That(journal.Api.StoredToken(), Is.EqualTo(""));
			Assert.That(journal.Api.StoredRefreshToken(), Is.EqualTo(""));

			journal.Api.RegisterDevice("device-de-prueba");
			journal.Api.StoreCredential("at_1234", "rt_5678");

			Assert.That(journal.Api.StoredDeviceKey(), Is.EqualTo("device-de-prueba"));
			Assert.That(journal.Api.StoredToken(), Is.EqualTo("at_1234"));
			Assert.That(journal.Api.StoredRefreshToken(), Is.EqualTo("rt_5678"));
		}

		// LOS ONCE SCRIPTS DE INGESTA QUE TRAJO T9, uno por comando de publicacion de la
		// especificacion. Se barre la ficha entera en vez de escribir once llamadas a mano: la lista
		// trae uno de cada tipo en el orden en que un feed los manda, y el conteo de tipos DISTINTOS
		// es lo que impide que el barrido se acorte sin que nadie lo note (C57).
		//
		// Lo que se afirma es la FORMA -- que el script parsee, ate sus parametros y no sea rechazado
		// -- y no que cada campo caiga en su casillero: de eso se ocupa
		// TheIngestedFactKeepsEveryPartOfWhatWasReceived, y de que la hoja resultante sea la misma
		// que deja la puerta se ocupa ASpecificationRevisionArrivesAsAFeedOfSeparateFacts.
		[Test]
		public void EverySpecificationIngestionScriptOfTheDoorParsesBindsAndRuns()
		{
			HashSet<string> ran = new HashSet<string>();

			foreach (Puppets.Portador.SenalDeEvento signal in ADealershipNarrative.TheWholeSpecSheet())
			{
				Assert.That(Apply(signal), Is.EqualTo(""),
					"El script de " + signal.Tipo + " tenia que correr sin rechazo.");

				ran.Add(signal.Tipo);
			}

			Assert.That(ran.Count, Is.EqualTo(11),
				"Son ONCE scripts de ingesta de la especificacion, uno por comando de publicacion.");

			Assert.That(journal.Api.LastEventApplied(),
				Is.EqualTo(ADealershipNarrative.TheLastSpecificationEvent),
				"Y los once avanzan el punto de control en el MISMO comando que el hecho.");

			Assert.That(journal.Api.HasASealedRevision(), Is.True,
				"La hoja quedo sellada, asi que los scripts no solo parsearon: publicaron.");
		}

		// LOS DOS SCRIPTS DE INGESTA DEL PERFIL DEL VEHICULO: la entrega y el sobre, que aplican por
		// los DOS comandos que la puerta ya tenia y no agregan ninguna operacion.
		//
		// LA DISCRIMINACION ES LO CARO DE ESTA PRUEBA, y sin ella se quedaria verde con los dos
		// scripts escribiendo en el global equivocado: correr los dos y afirmar que nada revento no
		// distingue un script que publica de uno que no. Asi que cada hecho se vuelve a leer por LA
		// LECTURA DE LA PUERTA QUE CONTESTARIA DISTINTO -- la frase del sobre trae sus dos mitades con
		// su unidad, y los tres datos de la entrega salen uno por uno.
		//
		// TODO EN UnitB -- millas -- porque 'mi' en la frase es la unica prueba visible de que la
		// unidad cruzo el cable y no se fabrico del otro lado.
		[Test]
		public void BothProfileIngestionScriptsOfTheDoorParseBindAndRun()
		{
			Assert.That(journal.Api.IsWarrantyEnvelopePublished(), Is.False,
				"Antes de nada no hay sobre: sin esto los renglones de abajo podrian estar mirando un "
				+ "sobre que puso otra prueba.");
			Assert.That(journal.Api.HasDeliveryBeenRecorded(), Is.False);

			foreach (Puppets.Portador.SenalDeEvento signal
				in ADealershipNarrative.TheDeliveryAndTheEnvelope())
			{
				Assert.That(Apply(signal), Is.EqualTo(""),
					"El script de " + signal.Tipo + " tenia que correr sin rechazo.");
			}

			Assert.That(journal.Api.LastEventApplied(),
				Is.EqualTo(ADealershipNarrative.TheLastProfileEvent),
				"Y los tres avanzan el punto de control en el MISMO comando que el hecho.");

			// LA ENTREGA, dato por dato: si el script cruzara la lectura con el instante, o tomara la
			// unidad de otro lado, cada uno de estos tres renglones lo dice por separado.
			Assert.That(journal.Api.HasDeliveryBeenRecorded(), Is.True);
			Assert.That(journal.Api.ReadDeliveredAt(),
				Is.EqualTo(new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc)));
			Assert.That(journal.Api.ReadDeliveryReading(),
				Is.EqualTo(ADealershipNarrative.TheOdometerAtDelivery));
			Assert.That(journal.Api.ReadDeliveryUnit(), Is.EqualTo("mi"));

			// EL SOBRE, y el que rige es el SEGUNDO: la narrativa publica dos y la ley del sobre es por
			// instante de publicacion, asi que leer las mitades de la segunda es lo que prueba que el
			// segundo script corrio y no solo el primero.
			Assert.That(journal.Api.IsWarrantyEnvelopePublished(), Is.True);
			Assert.That(journal.Api.DescribePublishedWarrantyEnvelope(),
				Does.Contain("24 meses").And.Contains("40000 mi"),
				"Las dos mitades del sobre que rige, con su unidad. Cruzar meses con distancia, o "
				+ "quedarse con la primera publicacion, cambia esta frase.");
			Assert.That(journal.Api.ReadEnvelopePublishedAt(),
				Is.EqualTo(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
		}

		// Y LOS SEIS DE LA INGESTA QUE TRAJO T8: los cinco comandos con Check de cada tipo de hecho
		// recibido, mas el comando suelto que avanza el punto de control cuando el dominio rechaza el
		// hecho.
		[Test]
		public void EveryIngestionScriptOfTheDoorParsesBindsAndRuns()
		{
			Assert.That(Apply(ADealershipNarrative.Revision(1, "R1", "2025-09-01T00:00:00Z")),
				Is.EqualTo(""));

			Assert.That(Apply(ADealershipNarrative.PlanOperation(
				2, "FluidChange", "TimeOrDistance", 365, 10000, true, false)), Is.EqualTo(""));

			Assert.That(Apply(ADealershipNarrative.Reading(
				3, 40000, "UnitA", "2026-01-20T00:00:00Z", "Telemetry")), Is.EqualTo(""));

			Assert.That(Apply(ADealershipNarrative.Crossing(
				4, "FluidChange", "2026-01-20T00:00:00Z")), Is.EqualTo(""));

			Assert.That(Apply(ADealershipNarrative.Completion(
				5, "FluidChange", "2026-01-21T00:00:00Z", 40000, "UnitA")), Is.EqualTo(""));

			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(5L));

			// El sexto script: el dominio rechaza el hecho adentro del cuerpo y el punto de control
			// avanza igual, en un comando aparte.
			Assert.That(Apply(ADealershipNarrative.Reading(
				6, 100, "UnitA", "2026-02-01T00:00:00Z", "Telemetry")), Is.Not.Empty);

			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(6L));
		}

		// Un mensaje que la puerta no sabe aplicar es un defecto de quien llama: la ruta debio
		// devolver null para descartarlo, y por eso NO se descarta aca en silencio.
		[Test]
		public void AMessageTheDoorHasNoCommandForIsACallerBug()
		{
			Assert.Throws<ArgumentException>(
				() => journal.Api.Apply(new AMessageThisAppDoesNotApply()));
		}

		private string Apply(Puppets.Portador.SenalDeEvento signal)
		{
			return journal.Api.Apply(DealershipCar.App.Events.DealershipEventRoute.Route(signal));
		}

		private sealed class AMessageThisAppDoesNotApply : Puppets.Portador.IMensajeDeEvento
		{
			public long Evento { get { return 1; } }

			public string Tipo { get { return "AlgoQueNadieAplica"; } }
		}

		// La lectura por clave es TOTAL: una clase que nunca se sembro contesta, no revienta.
		[Test]
		public void AKindThatWasNeverScheduledStillGetsAnAnswer()
		{
			Assert.That(journal.Api.ReadDueState("RecallRemedy"), Is.EqualTo("NotDue"));
			Assert.That(journal.Api.DeferralsUsed("RecallRemedy"), Is.EqualTo(0));
			Assert.That(journal.Api.ReadDueReason("RecallRemedy"), Does.Contain("no tiene agendada"));
			Assert.That(journal.Api.CanBeDeferred("RecallRemedy"), Is.False);
		}

		// LAS TREINTA Y CINCO LECTURAS DE LA ESPECIFICACION, contra una hoja sellada, cada una una
		// vez. Lo que se afirma es la forma -- que el script parsee, ate sus parametros y vuelva
		// TIPADO -- y no la regla: de las leyes se ocupan OnlyAJournaledSealedRevisionAnswers,
		// AJournaledRevisionAppliesForward y AJournaledCoverageAnswersWithItsStandingAndItsExclusion.
		//
		// Las cuatro lecturas de LISTA son las que no se podrian haber escrito antes de T0/S1: un
		// arreglo de primitivos cruza el parametro de vuelta y GetValue lo comprueba de tipo, asi que
		// no hay que partir ninguna frase para recuperar la lista.
		[Test]
		public void EverySpecificationReadOfTheDoorParsesBindsAndComesBackTyped()
		{
			ASealedSpecSheet();

			EveryOneOfThemAgainstASealedSheet();
		}

		private void EveryOneOfThemAgainstASealedSheet()
		{
			Assert.That(
				journal.Api.ReadModel(), Is.EqualTo(AScheduleOnARealJournal.TheModelAtDelivery));

			Assert.That(journal.Api.IsFluidPublished("EngineOil"), Is.True);
			Assert.That(journal.Api.DescribeFluid("EngineOil"), Does.Contain("5W-30"));
			Assert.That(journal.Api.ReadFluidRevision("EngineOil"), Is.EqualTo(TheRevision));

			Assert.That(journal.Api.IsTyrePublished("Front"), Is.True);
			Assert.That(journal.Api.DescribeTyre("Front"), Does.Contain("205/55R16"));
			Assert.That(journal.Api.ReadTyreRevision("Front"), Is.EqualTo(TheRevision));

			Assert.That(journal.Api.IsFuelPublished(), Is.True);
			Assert.That(journal.Api.DescribeFuel(), Does.Contain("91 RON regular"));
			Assert.That(journal.Api.ReadMinimumFuelGrade(), Is.EqualTo("91 RON regular"));
			Assert.That(
				journal.Api.PermittedFuelGrades(), Is.EqualTo(new[] { "95 RON Premium" }));
			Assert.That(journal.Api.ProhibitedFuelGrades(), Is.EqualTo(new[] { "E85 Flex" }));
			Assert.That(journal.Api.ReadFuelRevision(), Is.EqualTo(TheRevision));

			Assert.That(journal.Api.IsServiceIntervalPublished("FluidChange"), Is.True);
			Assert.That(
				journal.Api.DescribeServiceInterval("FluidChange", 8400, "UnitA"),
				Does.Contain("8400"));
			Assert.That(
				journal.Api.ReadServiceIntervalRevision("FluidChange"), Is.EqualTo(TheRevision));

			Assert.That(
				journal.Api.IsServicePricePublished("FluidChange", TenThousandUnits, "UnitA"),
				Is.True);
			Assert.That(
				journal.Api.DescribeServicePrice(
					"FluidChange", TenThousandUnits, "UnitA", ThePublication),
				Does.Contain("USD"));
			Assert.That(
				journal.Api.ReadServicePriceRevision("FluidChange", TenThousandUnits, "UnitA"),
				Is.EqualTo(TheRevision));

			Assert.That(journal.Api.IsCoveragePublished(), Is.True);
			Assert.That(journal.Api.DescribeCoverageSummary(), Does.Contain("POWERTRAIN"));
			Assert.That(journal.Api.ReadCoverageRevision(), Is.EqualTo(TheRevision));
			Assert.That(journal.Api.CoveredSystems(), Is.EqualTo(new[] { "POWERTRAIN" }));
			Assert.That(journal.Api.CoverageExclusions().Length, Is.EqualTo(1));
			Assert.That(journal.Api.TheVocabularyIsTheManufacturers("POWERTRAIN"), Is.True);

			Assert.That(TheCoverageOf("POWERTRAIN", journal.Api.CanCoverageBeEvaluated), Is.True);
			Assert.That(
				TheCoverageOf("POWERTRAIN", journal.Api.ReadCoverageStanding),
				Is.EqualTo("CoveredWithExclusion"));
			Assert.That(
				TheCoverageOf("POWERTRAIN", journal.Api.DescribeCoverage),
				Does.Contain("wear parts"));

			Assert.That(journal.Api.IsServicingPolicyPublished(), Is.True);
			Assert.That(
				journal.Api.DescribeServicingElsewhereRule(2, 0, 0), Does.Contain("service anywhere"));
			Assert.That(journal.Api.ReadServicingPolicyRevision(), Is.EqualTo(TheRevision));

			Assert.That(journal.Api.HasASealedRevision(), Is.True);
			Assert.That(journal.Api.ReadCurrentRevisionCode(), Is.EqualTo(TheRevision));
			Assert.That(
				journal.Api.ReadCurrentRevisionPublishedAt(), Is.EqualTo(ThePublication));
			Assert.That(journal.Api.ReadRevisionState(TheRevision), Is.EqualTo("Sealed"));
		}

		// Y LAS MISMAS TREINTA Y CINCO SIN NADA SELLADO, que es el otro camino de cada script. Cada
		// lectura total contesta -- presencia que no, procedencia vacia, la frase diciendolo con
		// palabras, y las listas VACIAS -- y las guardadas revientan.
		//
		// Las listas vacias son la mitad que no se podia dar por sabida: T0/S1 midio un arreglo de
		// tres elementos, no uno de cero, y un GetValue que no supiera devolver el vacio se
		// descubriria en la caratula y no aca.
		[Test]
		public void EverySpecificationReadStillAnswersWhenNothingIsSealed()
		{
			Assert.That(journal.Api.HasASealedRevision(), Is.False);

			Assert.That(journal.Api.IsFluidPublished("EngineOil"), Is.False);
			Assert.That(journal.Api.DescribeFluid("EngineOil"), Does.Contain("no se ha publicado nada"));
			Assert.That(journal.Api.ReadFluidRevision("EngineOil"), Is.EqualTo(""));

			Assert.That(journal.Api.IsTyrePublished("Front"), Is.False);
			Assert.That(journal.Api.DescribeTyre("Front"), Does.Contain("no se ha publicado nada"));
			Assert.That(journal.Api.ReadTyreRevision("Front"), Is.EqualTo(""));

			Assert.That(journal.Api.IsFuelPublished(), Is.False);
			Assert.That(journal.Api.DescribeFuel(), Does.Contain("no se ha publicado nada"));
			Assert.That(journal.Api.ReadMinimumFuelGrade(), Is.EqualTo(""));
			Assert.That(journal.Api.PermittedFuelGrades(), Is.Empty);
			Assert.That(journal.Api.ProhibitedFuelGrades(), Is.Empty);
			Assert.That(journal.Api.ReadFuelRevision(), Is.EqualTo(""));

			Assert.That(journal.Api.IsServiceIntervalPublished("FluidChange"), Is.False);
			Assert.That(
				journal.Api.DescribeServiceInterval("FluidChange", 8400, "UnitA"),
				Does.Contain("no se ha publicado nada"));
			Assert.That(
				journal.Api.DescribePublishedServiceInterval("FluidChange"),
				Does.Contain("no se ha publicado nada"));
			Assert.That(journal.Api.ReadServiceIntervalRevision("FluidChange"), Is.EqualTo(""));

			Assert.That(
				journal.Api.IsServicePricePublished("FluidChange", TenThousandUnits, "UnitA"),
				Is.False);
			Assert.That(
				journal.Api.DescribeServicePrice(
					"FluidChange", TenThousandUnits, "UnitA", ThePublication),
				Does.Contain("no se ha publicado nada"));
			Assert.That(
				journal.Api.ReadServicePriceRevision("FluidChange", TenThousandUnits, "UnitA"),
				Is.EqualTo(""));

			Assert.That(journal.Api.IsCoveragePublished(), Is.False);
			Assert.That(journal.Api.DescribeCoverageSummary(), Does.Contain("no se ha publicado nada"));
			Assert.That(journal.Api.ReadCoverageRevision(), Is.EqualTo(""));
			Assert.That(journal.Api.CoveredSystems(), Is.Empty);
			Assert.That(journal.Api.CoverageExclusions(), Is.Empty);
			Assert.That(journal.Api.TheVocabularyIsTheManufacturers("POWERTRAIN"), Is.False);

			// Sin nada publicado la pregunta SI se puede contestar, y la respuesta es que no se
			// publico: "no nos lo dijeron" es una de las cinco, y no es "no esta cubierto".
			Assert.That(TheCoverageOf("POWERTRAIN", journal.Api.CanCoverageBeEvaluated), Is.True);
			Assert.That(
				TheCoverageOf("POWERTRAIN", journal.Api.ReadCoverageStanding),
				Is.EqualTo("NotPublished"));
			Assert.That(
				TheCoverageOf("POWERTRAIN", journal.Api.DescribeCoverage),
				Does.Contain("no se ha publicado nada"));

			Assert.That(journal.Api.IsServicingPolicyPublished(), Is.False);
			Assert.That(
				journal.Api.DescribeServicingElsewhereRule(2, 0, 0), Does.Contain("no se ha publicado nada"));
			Assert.That(
				journal.Api.DescribePublishedServicingElsewhereRule(),
				Does.Contain("no se ha publicado nada"));
			Assert.That(journal.Api.ReadServicingPolicyRevision(), Is.EqualTo(""));
		}

		// LAS DOS LECTURAS GUARDADAS QUE TRAJO T6, del lado del wire, y la tercera polaridad que se
		// les parece y no es la misma.
		//
		// La revision vigente se guarda en HasASealedRevision; la posicion de cobertura se guarda en
		// CanCoverageBeEvaluated. Las dos revientan con InvalidOperationException, que es lo que una
		// caratula puede nombrar -- el DomainException del dominio es internal.
		//
		// ReadRevisionState NO es de estas: su clave es una IDENTIDAD del registro, no un item, asi
		// que un codigo que nadie abrio es un defecto de quien llama y el dominio revienta adentro,
		// igual que en los comandos. Contestar Draft ahi inventaria una revision.
		[Test]
		public void TheGuardedSpecificationReadsThrowInsteadOfInventingAnAnswer()
		{
			Assert.That(journal.Api.HasASealedRevision(), Is.False);
			Assert.Throws<InvalidOperationException>(
				() => journal.Api.ReadCurrentRevisionCode());
			Assert.Throws<InvalidOperationException>(
				() => journal.Api.ReadCurrentRevisionPublishedAt());

			Assert.Throws<DomainException>(() => journal.Api.ReadRevisionState("R-QUE-NO-EXISTE"));

			ASealedSpecSheet();

			// La presion se publico en UnitA y el odometro llega en UnitB: no hay nada que comparar,
			// y aca no se convierte. La respuesta no esta entre las cinco.
			Assert.That(
				journal.Api.CanCoverageBeEvaluated(
					"POWERTRAIN", 60, 100000, "UnitA",
					Delivery, OdometerAtDelivery, OdometerAtDelivery + 5000, "UnitB", ThePublication),
				Is.False);

			Assert.Throws<InvalidOperationException>(
				() => journal.Api.ReadCoverageStanding(
					"POWERTRAIN", 60, 100000, "UnitA",
					Delivery, OdometerAtDelivery, OdometerAtDelivery + 5000, "UnitB", ThePublication));

			Assert.That(
				journal.Api.DescribeCoverage(
					"POWERTRAIN", 60, 100000, "UnitA",
					Delivery, OdometerAtDelivery, OdometerAtDelivery + 5000, "UnitB", ThePublication),
				Is.Not.Empty,
				"Pero la FRASE si contesta: lo que no se pudo evaluar tambien se dice con palabras.");
		}

		// NINGUNA DE LAS TREINTA Y CINCO ESCRIBE UNA ENTRADA. Son consultas, no comandos, y el error
		// que esta prueba existe para atrapar es de una sola letra: PerformCommand donde iba
		// PerformQuery. Una lectura que escribe le agrega una entrada al journal de la persona cada
		// vez que abre la app, para siempre, y el estado sigue siendo correcto -- asi que ninguna
		// otra prueba se pondria roja.
		[Test]
		public void NoSpecificationReadOfTheDoorWritesAnEntry()
		{
			ASealedSpecSheet();

			string before = TheBytesOf(journal.Folder);

			EveryOneOfThemAgainstASealedSheet();

			Assert.That(TheBytesOf(journal.Folder), Is.EqualTo(before),
				"El journal quedo byte a byte como estaba.");
		}

		// Una hoja sellada con los seis items, publicada por la puerta. Los tres grados de
		// combustible llevan las TRES posturas: sin las tres, dos de las listas contestarian vacio
		// por una razon que no es la que la prueba cree.
		private void ASealedSpecSheet()
		{
			journal.Api.OpenRevision(TheRevision, ThePublication);

			journal.Api.PublishFluidSpecification(
				TheRevision, "EngineOil", "5W-30", 2.40m, "UnitA", TenThousandUnits, "UnitA",
				TwelveMonthsInDays);

			journal.Api.PublishTyreSpecification(
				TheRevision, "Front", "205/55R16", 2.4m, "UnitA", "94V");

			// El minimo NO se publica primero, y los tres llevan minusculas. Las dos cosas son a
			// proposito: con el minimo primero, una lectura que devolviera "el primer grado" en vez
			// del que lleva esa postura acertaria por casualidad; y con los tres en mayusculas, una
			// que le pasara el texto por ToUpper contestaria igual.
			journal.Api.PublishFuelGrade(TheRevision, "95 RON Premium", "Permitted");
			journal.Api.PublishFuelGrade(TheRevision, "91 RON regular", "Minimum");
			journal.Api.PublishFuelGrade(TheRevision, "E85 Flex", "Prohibited");

			journal.Api.PublishServiceInterval(
				TheRevision, "FluidChange", TenThousandUnits, "UnitA", TwelveMonthsInDays);

			journal.Api.PublishServicePrice(
				TheRevision, "FluidChange", TenThousandUnits, "UnitA", 75.50m, "USD",
				ThePublication, ThePublication.AddYears(1));

			journal.Api.PublishCoveredSystem(TheRevision, "POWERTRAIN", 0, 0, "UnitA");

			journal.Api.PublishCoverageExclusion(
				TheRevision, "POWERTRAIN", "wear parts", "when the part is consumable");

			journal.Api.PublishServicingPolicy(
				TheRevision, "service anywhere", "keep the receipts");

			journal.Api.SealRevision(TheRevision);
		}

		// Los nueve hechos de la pregunta de cobertura, atados una vez para las tres lecturas que la
		// hacen. Van los nueve: la resta entre el limite y el odometro es donde se compara la
		// unidad, y precalcularla aca se llevaria esa comparacion fuera del dominio.
		private static T TheCoverageOf<T>(
			string systemCode,
			Func<string, int, int, string, DateTime, int, int, string, DateTime, T> read)
		{
			return read(
				systemCode, 60, 100000, "UnitA",
				Delivery, OdometerAtDelivery, OdometerAtDelivery + 5000, "UnitA", ThePublication);
		}

		// El arbol entero del journal reducido a una cadena: nombre relativo mas hash del contenido,
		// en orden. Repetido a proposito -- cada fixture de esta suite es dueño de su aparejo.
		private static string TheBytesOf(string folder)
		{
			List<string> files = new List<string>(
				Directory.GetFiles(folder, "*", SearchOption.AllDirectories));
			files.Sort(StringComparer.Ordinal);

			StringBuilder tree = new StringBuilder();

			foreach (string file in files)
			{
				using (FileStream open = new FileStream(
					file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				{
					byte[] content = new byte[open.Length];
					open.ReadExactly(content, 0, content.Length);

					tree.Append(Path.GetRelativePath(folder, file).Replace('\\', '/'));
					tree.Append(' ');
					tree.Append(content.Length);
					tree.Append(' ');
					tree.AppendLine(Convert.ToHexString(MD5.HashData(content)));
				}
			}

			return tree.ToString();
		}
	}
}
