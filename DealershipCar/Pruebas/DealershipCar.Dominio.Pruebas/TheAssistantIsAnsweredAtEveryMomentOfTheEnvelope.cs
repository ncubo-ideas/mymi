using System;
using System.Collections.Generic;
using DealershipCar.App;
using DealershipCar.Mcp;
using DealershipCar.Mcp.Tools;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LOS CUATRO MOMENTOS DEL SOBRE, PREGUNTADOS POR LA TOOL Y NO POR LA PUERTA. La consola ya los
	// puso en una pantalla; lo que solo se puede ver aca es que el asistente reciba los cuatro sin
	// haber puesto un solo dato, y que las cuatro respuestas sean CUATRO y no la misma con adornos.
	//
	// LA TOOL LEE EL RELOJ Y LA PRUEBA NO PUEDE ELEGIR EL INSTANTE. Es la segunda forma del instante
	// recibido -- argumento de una consulta, no monotono, truncado al segundo -- y por eso no entra
	// por parametro: un parametro dejaria que un modelo preguntara por una fecha que se invento. Asi
	// que lo que se mueve es el REGISTRO y no el reloj, y cada escenario esta montado para que su
	// respuesta no dependa del dia en que corre la suite:
	//
	//   - la mitad de DISTANCIA no depende del reloj para nada: es el odometro contra el limite;
	//   - un plazo YA CUMPLIDO sigue cumplido para siempre hacia adelante;
	//   - y para que uno siga ADENTRO hace falta un plazo largo. Doscientos cuarenta meses desde
	//     2025 aguantan hasta 2045, y esa es la unica parte de esto que caduca.
	//
	// LAS AFIRMACIONES SIGUEN ESA LINEA: los numeros de distancia van exactos y los de tiempo van por
	// su FORMA -- 'meses que quedan de doscientos cuarenta' -- porque el numero baja solo con el
	// calendario y afirmarlo seria afirmar el dia de la corrida.
	//
	// Y LOS CUATRO MOMENTOS NO CABEN EN UN REGISTRO. Pasada la mitad de distancia y pasada la de
	// tiempo no le pueden pasar al mismo carro bajo el mismo sobre: el odometro no baja y el instante
	// solo avanza, asi que un limite alcanzado sigue alcanzado. La consola resolvio eso publicando el
	// sobre por segunda vez, porque un segundo VIN le dejaria una carpeta de un carro que nadie tiene
	// en el journal de una persona de verdad. ACA NO: cada escenario es su propia carpeta temporal,
	// asi que la ruta que la consola tuvo que rechazar es justamente la correcta.
	[TestFixture]
	public sealed class TheAssistantIsAnsweredAtEveryMomentOfTheEnvelope
	{
		// TODO EN UnitB: 'mi' en la frase es la prueba visible de que la unidad cruzo. Con UnitA --
		// el primer miembro del enum -- una unidad que no cruzara se leeria igual.
		private const string TheUnit = "UnitB";
		private const string TheSystem = "POWERTRAIN";
		private const string TheRevision = "WE2025-R1";

		private const int TheOdometerAtDelivery = 120;

		private static readonly DateTime TheRecentDelivery =
			new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheOldDelivery =
			new DateTime(2020, 3, 1, 0, 0, 0, DateTimeKind.Utc);

		private const int TheLongTerm = 240;
		private const int TheExpiredTerm = 36;

		private const int TheFirstDistance = 20000;
		private const int TheSecondDistance = 60000;

		// EL PRIMER MOMENTO: la agencia no dijo nada todavia. La pregunta NO queda suspendida -- que
		// nadie haya publicado un sobre es algo que el registro sabe con certeza -- asi que se puede
		// evaluar y la respuesta tiene nombre. Se lee raro una sola vez, y por eso se afirma en vez
		// de dejarse pasar.
		[Test]
		public void BeforeTheDealershipHasSaidAnythingTheAnswerIsSettledAndItHasAName()
		{
			using (var bare = new AScheduleOnARealJournal("1FTFW1ET5DFA00001", "Standard"))
			{
				WarrantyStanding standing = TheAssistantAsks(bare);

				Assert.That(standing.EnvelopeKnown, Is.True,
					"El registro es de la app de hoy: lo que falta es lo que la agencia no dijo, no "
					+ "la global.");
				Assert.That(standing.EnvelopePublished, Is.False);
				Assert.That(standing.DeliveryRecorded, Is.False);

				Assert.That(standing.CanBeEvaluated, Is.True,
					"Y esto es lo que se lee raro: un registro vacio NO suspende la pregunta del "
					+ "sobre. Un sobre que nadie publico es una respuesta decidida, no una que no se "
					+ "pudo medir.");
				Assert.That(standing.Standing, Is.EqualTo("NotPublished"));

				Assert.That(
					standing.Says,
					Does.Contain("not the same as this vehicle having no warranty"),
					"Y la frase lo dice ella misma. Sin ese renglon, 'no publicado' se lee como 'sin "
					+ "garantia', que es lo contrario de lo que pasa.");

				Assert.That(standing.PublishedSays, Is.Not.Empty,
					"La mitad publicada contesta aunque no haya nada publicado: por eso la puede "
					+ "citar quien no tiene el odometro de nadie.");
			}
		}

		// EL SEGUNDO: adentro de las dos mitades, y las dos van dichas.
		[Test]
		public void InsideBothHalvesTheAnswerSaysBothAndPredictsNeither()
		{
			using (AScheduleOnARealJournal inside = AVehicleInsideBothHalves())
			{
				WarrantyStanding standing = TheAssistantAsks(inside);

				Assert.That(standing.Standing, Is.EqualTo("Inside"));
				Assert.That(standing.Says, Does.Contain("Distance: 11120 mi left of 20000 mi."));
				Assert.That(standing.Says,
					Does.Contain("months left of " + TheLongTerm + " months."));
				Assert.That(standing.Says, Does.Contain("Neither half has been reached"));
				Assert.That(standing.Says, Does.Contain("rather than predicting a rate"));
			}
		}

		// EL TERCERO: pasada la mitad de DISTANCIA, con la de tiempo todavia de sobra. La comoda va
		// dicha igual: callarla dejaria a la persona sin saber que su plazo no se cumplio.
		[Test]
		public void PastTheDistanceHalfItNamesTheDistanceAndStillSaysTheTime()
		{
			using (AScheduleOnARealJournal past = AVehiclePastTheDistanceHalf())
			{
				WarrantyStanding standing = TheAssistantAsks(past);

				Assert.That(standing.Standing, Is.EqualTo("Reached"));
				Assert.That(standing.Says, Does.Contain("Reached by distance, which arrived first."));
				Assert.That(standing.Says, Does.Not.Contain("Reached by time"));
				Assert.That(standing.Says, Does.Contain("Distance: 5880 mi past a limit of 20000 mi."));
				Assert.That(standing.Says,
					Does.Contain("months left of " + TheLongTerm + " months."));

				Assert.That(
					TheAssistantAsksAboutCoverage(past).Standing, Is.EqualTo("OutsideEnvelope"),
					"Y la cobertura del sistema se mueve con el sobre del vehiculo, porque el "
					+ "sistema cubierto no tiene limites propios.");
			}
		}

		// EL CUARTO: pasada la mitad de TIEMPO, con la de distancia todavia de sobra. Es el espejo, y
		// es lo que hace discriminar al de arriba: una lectura que contestara siempre por la misma
		// mitad pasaria uno de los dos y se pondria roja en el otro.
		[Test]
		public void PastTheTimeHalfItNamesTheTimeAndStillSaysTheDistance()
		{
			using (AScheduleOnARealJournal past = AVehiclePastTheTimeHalf())
			{
				WarrantyStanding standing = TheAssistantAsks(past);

				Assert.That(standing.Standing, Is.EqualTo("Reached"));
				Assert.That(standing.Says, Does.Contain("Reached by time, which arrived first."));
				Assert.That(standing.Says, Does.Not.Contain("Reached by distance"));
				Assert.That(standing.Says,
					Does.Contain("months past a limit of " + TheExpiredTerm + " months."));
				Assert.That(standing.Says, Does.Contain("Distance: 29120 mi left of 60000 mi."));
			}
		}

		// Y LOS CUATRO SON CUATRO FRASES DISTINTAS. Sin este renglon, los cuatro de arriba podrian
		// estar pasando contra una respuesta que dice siempre lo mismo y cuyas palabras resultan
		// contener las cuatro cosas que se le piden.
		[Test]
		public void TheFourMomentsAreFourDifferentAnswers()
		{
			using (var bare = new AScheduleOnARealJournal("1FTFW1ET5DFA00001", "Standard"))
			using (AScheduleOnARealJournal inside = AVehicleInsideBothHalves())
			using (AScheduleOnARealJournal distance = AVehiclePastTheDistanceHalf())
			using (AScheduleOnARealJournal time = AVehiclePastTheTimeHalf())
			{
				var said = new List<string>
				{
					TheAssistantAsks(bare).Says,
					TheAssistantAsks(inside).Says,
					TheAssistantAsks(distance).Says,
					TheAssistantAsks(time).Says,
				};

				Assert.That(said, Is.Unique);
				foreach (string each in said) Assert.That(each, Is.Not.Empty);
			}
		}

		// ------------------------------------------------------ el aparejo

		private static WarrantyStanding TheAssistantAsks(AScheduleOnARealJournal journal)
		{
			return new WarrantyEnvelopeTools(new JournalReader(journal.Folder, ""))
				.IsMyCarStillUnderWarranty();
		}

		private static WarrantyScope TheAssistantAsksAboutCoverage(AScheduleOnARealJournal journal)
		{
			return new ModelSpecificationTools(new JournalReader(journal.Folder, ""))
				.DoesTheWarrantyCover(TheSystem);
		}

		private static AScheduleOnARealJournal AVehicleInsideBothHalves()
		{
			return AVehicle(
				"1HGCM82633A004352", TheLongTerm, TheFirstDistance, TheRecentDelivery,
				new[]
				{
					(6000, new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc)),
					(9000, new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc)),
				});
		}

		private static AScheduleOnARealJournal AVehiclePastTheDistanceHalf()
		{
			return AVehicle(
				"JTDKN3DU0A0123456", TheLongTerm, TheFirstDistance, TheRecentDelivery,
				new[] { (26000, new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc)) });
		}

		private static AScheduleOnARealJournal AVehiclePastTheTimeHalf()
		{
			return AVehicle(
				"WAUZZZ8V5KA000001", TheExpiredTerm, TheSecondDistance, TheOldDelivery,
				new[] { (31000, new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc)) });
		}

		// Un vehiculo entero: el alcance sellado del fabricante, el sobre publicado, la entrega
		// registrada y las lecturas que trae el feed. TODO por comando, que es como llegan de verdad.
		private static AScheduleOnARealJournal AVehicle(
			string vin, int months, int distance, DateTime deliveredAt,
			(int Value, DateTime ObservedAt)[] readings)
		{
			var journal = new AScheduleOnARealJournal(vin, "Standard");

			journal.Api.OpenRevision(TheRevision, deliveredAt);
			journal.Api.PublishCoveredSystem(TheRevision, TheSystem, 0, 0, TheUnit);
			journal.Api.PublishCoverageExclusion(
				TheRevision, TheSystem, "wear parts", "when the part is a consumable");
			journal.Api.SealRevision(TheRevision);

			journal.Api.PublishWarrantyEnvelope(months, distance, TheUnit, deliveredAt);
			journal.Api.RecordDelivery(deliveredAt, TheOdometerAtDelivery, TheUnit);

			foreach ((int value, DateTime observedAt) in readings)
				journal.Api.RecordReading(value, TheUnit, observedAt, "Telemetry");

			return journal;
		}
	}
}
