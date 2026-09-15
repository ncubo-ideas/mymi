using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// PRUEBA 17 DE §11 DEL BRIEF, y la que prueba que G1 se gasto bien: la lectura de DOS argumentos
	// devuelve LA MISMA FRASE que la de nueve a la que se le pasan los mismos hechos a mano. Mismo
	// sistema, mismo instante, misma respuesta.
	//
	// LOS HECHOS ENTRAN POR CAMINOS DISTINTOS A PROPOSITO, y sin eso la prueba no mide nada. Al
	// REGISTRO llegan por COMANDO -- PublishWarrantyEnvelope, RecordDelivery, RecordReading -- y a la
	// lectura de nueve argumentos por LITERAL. Si los nueve salieran de la misma expresion de C#, esto
	// compararia un valor consigo mismo y estaria verde con una composicion que no compone.
	//
	// TODO EN UnitB (C60). UnitA es el primer miembro del enum, asi que una unidad que el motor
	// hubiera dejado por defecto daria la misma respuesta que una que cruzo. El espejo en UnitA vive
	// en AUnitMismatchSuspendsTheDistanceThroughTheDoor.
	//
	// Y LOS NUMEROS SON DE LOS QUE DISCRIMINAN. El limite de distancia es 15000 y no 60000: con 60000
	// un vehiculo que recorrio 10000 esta adentro pase lo que pase con la lectura de la entrega, y las
	// pruebas de abajo que mueven UN hecho por vez no podrian moverlo. Con 15000, cambiar la entrega o
	// el odometro cruza el limite.
	[TestFixture]
	public sealed class TheDoorComposedTheAnswerInsteadOfInventingIt
	{
		private const string TheVin = "WAUZZZ8K9AA123456";
		private const string TheSystem = "POWERTRAIN";
		private const string TheRevision = "R1";

		private const int ThirtySixMonths = 36;
		private const int FifteenThousand = 15000;
		private const string TheUnitEverythingIsIn = "UnitB";

		private const int TheOdometerAtDelivery = 30000;
		private const int TheOdometerToday = 40000;

		private static readonly DateTime ThePublication =
			new DateTime(2023, 11, 15, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItWasHandedOver =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItWasObserved =
			new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItIsAsked =
			new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

		// ===== LAS DOS RUTAS DAN LA MISMA RESPUESTA =======================================

		// LA PRUEBA 17. La de dos argumentos y la de nueve contestan lo mismo en las TRES lecturas:
		// la presencia, el nombre de la posicion y la frase. Que las tres coincidan es lo que dice
		// que la puerta COMPUSO -- saco los siete hechos del registro y se los paso al dominio -- en
		// vez de inventarse alguno.
		[Test]
		public void TheTwoArgumentReadAnswersExactlyWhatTheNineArgumentReadAnswers()
		{
			using (AScheduleOnARealJournal journal = AVehicleWithEverythingRecorded())
			{
				Assert.That(
					journal.Api.CanCoverageBeEvaluatedForThisVehicle(TheSystem, TheDayItIsAsked),
					Is.EqualTo(TheNineArgumentAnswerOf(journal, journal.Api.CanCoverageBeEvaluated)));

				Assert.That(
					journal.Api.ReadCoverageStandingForThisVehicle(TheSystem, TheDayItIsAsked),
					Is.EqualTo(TheNineArgumentAnswerOf(journal, journal.Api.ReadCoverageStanding)));

				Assert.That(
					journal.Api.DescribeCoverageForThisVehicle(TheSystem, TheDayItIsAsked),
					Is.EqualTo(TheNineArgumentAnswerOf(journal, journal.Api.DescribeCoverage)),
					"PALABRA POR PALABRA. La frase la compone el dominio; lo unico que la puerta "
					+ "aporta son los siete hechos, y salen del registro.");
			}
		}

		// Y LA COMPARACION DE ARRIBA TIENE QUE PODER FALLAR. Si las dos rutas contestaran siempre lo
		// mismo -- por ejemplo porque la frase no menciona ningun hecho del vehiculo -- la prueba 17
		// estaria verde para siempre. Con los hechos EQUIVOCADOS a mano, la de nueve dice otra cosa.
		[Test]
		public void TheNineArgumentReadSaysSomethingElseWhenItIsHandedOtherFacts()
		{
			using (AScheduleOnARealJournal journal = AVehicleWithEverythingRecorded())
			{
				Assert.That(
					journal.Api.DescribeCoverage(
						TheSystem, 24, 12000, TheUnitEverythingIsIn,
						TheDayItWasHandedOver, TheOdometerAtDelivery, TheOdometerToday,
						TheUnitEverythingIsIn, TheDayItIsAsked),
					Is.Not.EqualTo(
						journal.Api.DescribeCoverageForThisVehicle(TheSystem, TheDayItIsAsked)),
					"Otros limites, otra frase. Sin este renglon, la igualdad de arriba podria "
					+ "cumplirse por una frase que no nombra ningun hecho.");
			}
		}

		// LOS SIETE HECHOS QUE LA LECTURA COMPUESTA SACA DEL REGISTRO, UNO POR UNO. Cada variacion
		// cambia UN solo hecho del registro y la frase compuesta tiene que MOVERSE. Una lectura que
		// ignorara uno de los siete -- o que lo tomara de un default -- pasaria la prueba 17 y se
		// pondria roja aca.
		//
		// CADA VARIACION VA EN SU PROPIO JOURNAL, y no corrigiendo el anterior: corregir la entrega
		// sobre el mismo registro mide tambien que la correccion funcione, que es otra ley y otra
		// prueba.
		[Test]
		public void EveryFactTheComposedReadUsesComesFromTheRecord()
		{
			string baseline = TheComposedSentenceOf(AVehicleWithEverythingRecorded);

			Assert.That(
				TheComposedSentenceOf(() => AVehicleWhoseEnvelopeLasts(24)),
				Is.Not.EqualTo(baseline),
				"1 de 7: el PLAZO del sobre. La frase nombra los limites que rigen.");

			Assert.That(
				TheComposedSentenceOf(() => AVehicleWhoseEnvelopeReaches(12000)),
				Is.Not.EqualTo(baseline),
				"2 de 7: la DISTANCIA del sobre.");

			Assert.That(
				TheComposedSentenceOf(() => AVehicleWhoseEnvelopeIsMeasuredIn("UnitA")),
				Is.Not.EqualTo(baseline),
				"3 de 7: la UNIDAD del sobre. Contra un odometro en la otra, no hay nada que "
				+ "comparar y aca no se convierte.");

			Assert.That(
				TheComposedSentenceOf(() => AVehicleHandedOverOn(
					new DateTime(2021, 6, 1, 0, 0, 0, DateTimeKind.Utc))),
				Is.Not.EqualTo(baseline),
				"4 de 7: el INSTANTE de la entrega. Es el ancla desde donde se cuenta el plazo.");

			Assert.That(
				TheComposedSentenceOf(() => AVehicleHandedOverAt(20000)),
				Is.Not.EqualTo(baseline),
				"5 de 7: el ODOMETRO DE LA ENTREGA. Lo recorrido se mide desde ahi y no desde cero.");

			Assert.That(
				TheComposedSentenceOf(() => AVehicleWhoseOdometerReads(50000)),
				Is.Not.EqualTo(baseline),
				"6 de 7: el ODOMETRO DE HOY.");

			Assert.That(
				TheComposedSentenceOf(() => AVehicleWhoseOdometerIsMeasuredIn("UnitA")),
				Is.Not.EqualTo(baseline),
				"7 de 7: la UNIDAD del odometro de hoy. Es un hecho distinto del de la unidad del "
				+ "sobre, y la frase dice cual contra cual no calzo.");
		}

		// Y LOS DOS DESAJUSTES DE UNIDAD NO SE DICEN IGUAL. Sin este renglon, las variaciones 3 y 7
		// se podrian estar cumpliendo las dos porque la frase dice "no se pudo comparar" y nada mas.
		[Test]
		public void TheTwoUnitsAreTwoFactsAndTheSentenceSaysWhichAgainstWhich()
		{
			Assert.That(
				TheComposedSentenceOf(() => AVehicleWhoseEnvelopeIsMeasuredIn("UnitA")),
				Is.Not.EqualTo(
					TheComposedSentenceOf(() => AVehicleWhoseOdometerIsMeasuredIn("UnitA"))));
		}

		// ===== LA FRASE CONTESTA DONDE EL NOMBRE NO ======================================

		// LA POSICION DE §4.2 QUE T6 MOVIO, y esta es la prueba que la sostiene. Con las unidades
		// cruzadas la pregunta NO se puede evaluar -- el nombre de la posicion se rechaza -- y la
		// FRASE contesta igual, diciendo con palabras que no se pudo comparar. Si Describe estuviera
		// guardada por Can, esas palabras no llegarian a nadie y el modulo perderia lo unico que
		// tiene para decir en ese caso.
		[Test]
		public void TheSentenceStillAnswersForAQuestionThatCannotBeEvaluated()
		{
			using (AScheduleOnARealJournal journal = AVehicleWhoseOdometerIsMeasuredIn("UnitA"))
			{
				Assert.That(
					journal.Api.CanCoverageBeEvaluatedForThisVehicle(TheSystem, TheDayItIsAsked),
					Is.False);

				Assert.Throws<InvalidOperationException>(
					() => journal.Api.ReadCoverageStandingForThisVehicle(TheSystem, TheDayItIsAsked));

				string said = journal.Api.DescribeCoverageForThisVehicle(TheSystem, TheDayItIsAsked);

				Assert.That(said, Does.Contain("nunca se convierten"),
					"La frase dice por que no se pudo, y es lo mismo que dice la de nueve.");
				Assert.That(
					said,
					Is.EqualTo(journal.Api.DescribeCoverage(
						TheSystem, ThirtySixMonths, FifteenThousand, TheUnitEverythingIsIn,
						TheDayItWasHandedOver, TheOdometerAtDelivery, TheOdometerToday,
						"UnitA", TheDayItIsAsked)));
			}
		}

		// ===== EL ODOMETRO SE RESUELVE AL INSTANTE PREGUNTADO Y NO A OTRO =================

		// LA LEY QUE DICE QUE LAS DOS MITADES HABLAN DE UN SOLO NUMERO, apuntada a la lectura
		// compuesta. Cada uno de los seis scripts pregunta si HAY lectura y despues CUAL es, y las
		// dos preguntas tienen que llevar el MISMO instante: con instantes distintos, la frase
		// nombraria una lectura contra la que la posicion no se calculo.
		//
		// ESTO NECESITA DOS LECTURAS Y NO UNA, y es por lo que este renglon existe. MEDIDO: con una
		// sola lectura, resolver el odometro 90 dias DESPUES del instante preguntado deja la suite
		// ENTERA VERDE -- con una sola, "la ultima observada al instante preguntado" y "la ultima
		// observada tres meses despues" son el mismo numero. Un estado que tiene UNO de algo no
		// puede discriminar una clave.
		//
		// LA SEGUNDA LECTURA VA FECHADA DESPUES DEL INSTANTE PREGUNTADO, que es como un feed las
		// manda de verdad: una lectura observada despues no cuenta para la pregunta de antes.
		[Test]
		public void AReadingObservedAfterTheInstantAskedIsNotTheOneTheComposedAnswersUse()
		{
			using (AScheduleOnARealJournal journal = AVehicleWithASecondReadingDatedLater())
			{
				string said = journal.Api.DescribeWarrantyEnvelope(
					TheDayItIsAsked, TheLanguage.OfTheRecord);

				Assert.That(
					said, Does.Contain("odómetro de 40.000 mi observado el 20 de enero de 2026"));
				Assert.That(said, Does.Not.Contain("46000"),
					"La lectura de marzo existe en el registro y NO cuenta para una pregunta de "
					+ "febrero. Es la misma regla con la que la agenda evalua.");

				Assert.That(
					journal.Api.ReadEnvelopeStanding(TheDayItIsAsked), Is.EqualTo("Inside"));
				Assert.That(
					journal.Api.ReadCoverageStandingForThisVehicle(TheSystem, TheDayItIsAsked),
					Is.EqualTo("CoveredWithExclusion"));
			}
		}

		// Y PREGUNTADO DESPUES DE ESA SEGUNDA LECTURA, LAS SEIS SE MUEVEN. Es el renglon que hace
		// discriminar al de arriba: sin el, una lectura compuesta que ignorara la segunda lectura
		// SIEMPRE tambien pasaria.
		[Test]
		public void AskedAfterThatSecondReadingTheComposedAnswersUseIt()
		{
			using (AScheduleOnARealJournal journal = AVehicleWithASecondReadingDatedLater())
			{
				DateTime afterwards = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);

				string said = journal.Api.DescribeWarrantyEnvelope(
					afterwards, TheLanguage.OfTheRecord);

				Assert.That(
					said, Does.Contain("odómetro de 46.000 mi observado el 1 de marzo de 2026"));
				Assert.That(journal.Api.ReadEnvelopeStanding(afterwards), Is.EqualTo("Reached"));
				Assert.That(
					journal.Api.ReadCoverageStandingForThisVehicle(TheSystem, afterwards),
					Is.EqualTo("OutsideEnvelope"));
			}
		}

		// ===== PRUEBA 18: LOS LIMITES PROPIOS DEL SISTEMA SIGUEN GOBERNANDO ================

		// El sobre del vehiculo llega igual por la ruta compuesta, y sigue perdiendo contra los
		// limites propios del sistema cuando el sistema publico unos. Este slice no cambia esa regla:
		// solo le pone los hechos delante.
		[Test]
		public void ASystemWithItsOwnLimitsIsJudgedByThemAndNotByTheVehicleEnvelope()
		{
			using (AScheduleOnARealJournal journal =
				AVehicleWhoseCoveredSystemHasItsOwnLimits(12, 5000))
			{
				Assert.That(
					journal.Api.ReadCoverageStandingForThisVehicle(TheSystem, TheDayItIsAsked),
					Is.EqualTo("OutsideEnvelope"),
					"Recorrio 10000 de los 15000 del sobre -- adentro -- y de los 5000 propios del "
					+ "sistema -- afuera. Gobierna el mas especifico.");

				Assert.That(
					journal.Api.DescribeCoverageForThisVehicle(TheSystem, TheDayItIsAsked),
					Does.Contain("5000 mi"));

				Assert.That(
					journal.Api.ReadEnvelopeStanding(TheDayItIsAsked), Is.EqualTo("Inside"),
					"Y el sobre del vehiculo, preguntado por su cuenta, sigue diciendo que adentro. "
					+ "Son dos preguntas y dos respuestas, no una contradiccion.");
			}
		}

		// ===== EL HUECO CONOCIDO, MEDIDO Y FIJADO AQUI PARA QUE SE VEA =====================

		// UN REGISTRO QUE SE CONTRADICE A SI MISMO ES LO UNICO QUE LAS LECTURAS COMPUESTAS NO SABEN
		// DECIR, y estas dos pruebas lo fijan como esta, no como deberia estar. El dominio clasifica
		// las dos situaciones de abajo como DEFECTO DE QUIEN LLAMA y revienta con su propia
		// excepcion -- que es correcto para la lectura de nueve argumentos, donde quien llama
		// SUMINISTRA los hechos que se contradicen.
		//
		// EN LA LECTURA COMPUESTA QUIEN LLAMA NO SUMINISTRA NINGUNO DE LOS DOS: los saca del
		// registro. Asi que una contradiccion entre dos hechos que la agencia registro le llega a la
		// caratula como una excepcion INTERNA que no puede nombrar, por una pregunta que no hizo mal.
		//
		// NO SE ARREGLA AQUI, Y EL MOTIVO ES EL MISMO QUE DEJO Dominio/ SIN TOCAR: arreglarlo es
		// darle a la contradiccion un nombre en el dominio, o repetir la regla del dominio en la
		// puerta con nada que las mantenga en paso. Las dos son decisiones de otro nivel. Lo que si
		// cabe es que quede MEDIDO y ROJO el dia que alguien lo cambie.
		[Test]
		public void AnOdometerBelowTheDeliveryReadingIsRefusedAsACallerBugByBothComposedReads()
		{
			using (AScheduleOnARealJournal journal = AVehicleHandedOverAt(50000))
			{
				Assert.Throws<DomainException>(
					() => journal.Api.DescribeWarrantyEnvelope(TheDayItIsAsked, TheLanguage.OfTheRecord),
					"Y esta es TOTAL segun la superficie declarada: contesta siempre, tambien lo que "
					+ "no se pudo evaluar. Contra un registro que se contradice, no.");

				Assert.Throws<DomainException>(
					() => journal.Api.DescribeCoverageForThisVehicle(TheSystem, TheDayItIsAsked));
			}
		}

		// Y PREGUNTAR POR UN INSTANTE ANTERIOR A LA ENTREGA parte a las dos por caminos distintos: la
		// del sobre CONTESTA -- con mas meses de los que el plazo tiene -- y la de cobertura revienta.
		// Las dos respuestas son defendibles por separado y juntas no son una superficie.
		[Test]
		public void AskedBeforeTheDeliveryTheTwoComposedReadsPartCompany()
		{
			using (AScheduleOnARealJournal journal = AVehicleReadBeforeItWasHandedOver())
			{
				DateTime beforeItWasHandedOver = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc);

				Assert.That(
					journal.Api.DescribeWarrantyEnvelope(
						beforeItWasHandedOver, TheLanguage.OfTheRecord),
					Does.Contain("39 meses de 36 meses"),
					"Mas plazo del que el sobre publico, porque los meses cumplidos salen negativos. "
					+ "No revienta y no miente: dice exactamente lo que el registro implica.");

				Assert.Throws<DomainException>(
					() => journal.Api.DescribeCoverageForThisVehicle(
						TheSystem, beforeItWasHandedOver));
			}
		}

		// ===== EL APAREJO =================================================================

		private static string TheComposedSentenceOf(Func<AScheduleOnARealJournal> vehicle)
		{
			using (AScheduleOnARealJournal journal = vehicle())
				return journal.Api.DescribeCoverageForThisVehicle(TheSystem, TheDayItIsAsked);
		}

		private static T TheNineArgumentAnswerOf<T>(
			AScheduleOnARealJournal journal,
			Func<string, int, int, string, DateTime, int, int, string, DateTime, T> read)
		{
			return read(
				TheSystem, ThirtySixMonths, FifteenThousand, TheUnitEverythingIsIn,
				TheDayItWasHandedOver, TheOdometerAtDelivery, TheOdometerToday,
				TheUnitEverythingIsIn, TheDayItIsAsked);
		}

		private static AScheduleOnARealJournal AVehicleWithEverythingRecorded()
		{
			return AVehicleRecordedAs(
				ThirtySixMonths, FifteenThousand, TheUnitEverythingIsIn,
				TheDayItWasHandedOver, TheOdometerAtDelivery, TheUnitEverythingIsIn,
				TheOdometerToday, TheUnitEverythingIsIn, 0, 0);
		}

		private static AScheduleOnARealJournal AVehicleWhoseEnvelopeLasts(int months)
		{
			return AVehicleRecordedAs(
				months, FifteenThousand, TheUnitEverythingIsIn,
				TheDayItWasHandedOver, TheOdometerAtDelivery, TheUnitEverythingIsIn,
				TheOdometerToday, TheUnitEverythingIsIn, 0, 0);
		}

		private static AScheduleOnARealJournal AVehicleWhoseEnvelopeReaches(int distance)
		{
			return AVehicleRecordedAs(
				ThirtySixMonths, distance, TheUnitEverythingIsIn,
				TheDayItWasHandedOver, TheOdometerAtDelivery, TheUnitEverythingIsIn,
				TheOdometerToday, TheUnitEverythingIsIn, 0, 0);
		}

		// La unidad del SOBRE y sola. La entrega se queda en la de siempre a proposito: la pregunta
		// de cobertura no recibe la unidad de la entrega, asi que cambiar las dos aqui moveria dos
		// hechos y la variacion dejaria de ser de uno.
		private static AScheduleOnARealJournal AVehicleWhoseEnvelopeIsMeasuredIn(string unit)
		{
			return AVehicleRecordedAs(
				ThirtySixMonths, FifteenThousand, unit,
				TheDayItWasHandedOver, TheOdometerAtDelivery, TheUnitEverythingIsIn,
				TheOdometerToday, TheUnitEverythingIsIn, 0, 0);
		}

		private static AScheduleOnARealJournal AVehicleHandedOverOn(DateTime deliveredAt)
		{
			return AVehicleRecordedAs(
				ThirtySixMonths, FifteenThousand, TheUnitEverythingIsIn,
				deliveredAt, TheOdometerAtDelivery, TheUnitEverythingIsIn,
				TheOdometerToday, TheUnitEverythingIsIn, 0, 0);
		}

		private static AScheduleOnARealJournal AVehicleHandedOverAt(int reading)
		{
			return AVehicleRecordedAs(
				ThirtySixMonths, FifteenThousand, TheUnitEverythingIsIn,
				TheDayItWasHandedOver, reading, TheUnitEverythingIsIn,
				TheOdometerToday, TheUnitEverythingIsIn, 0, 0);
		}

		private static AScheduleOnARealJournal AVehicleWhoseOdometerReads(int reading)
		{
			return AVehicleRecordedAs(
				ThirtySixMonths, FifteenThousand, TheUnitEverythingIsIn,
				TheDayItWasHandedOver, TheOdometerAtDelivery, TheUnitEverythingIsIn,
				reading, TheUnitEverythingIsIn, 0, 0);
		}

		private static AScheduleOnARealJournal AVehicleWhoseOdometerIsMeasuredIn(string unit)
		{
			return AVehicleRecordedAs(
				ThirtySixMonths, FifteenThousand, TheUnitEverythingIsIn,
				TheDayItWasHandedOver, TheOdometerAtDelivery, TheUnitEverythingIsIn,
				TheOdometerToday, unit, 0, 0);
		}

		// EL MISMO VEHICULO CON UNA SEGUNDA LECTURA FECHADA DESPUES DEL INSTANTE PREGUNTADO. 46000
		// cruza el limite de 15000 desde la entrega y 40000 no, asi que las dos lecturas no solo son
		// numeros distintos: dan POSICIONES distintas, que es lo que hace que la frase se mueva.
		private static AScheduleOnARealJournal AVehicleWithASecondReadingDatedLater()
		{
			AScheduleOnARealJournal journal = AVehicleWithEverythingRecorded();

			journal.Api.RecordReading(
				46000, TheUnitEverythingIsIn,
				new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), "Telemetry");

			return journal;
		}

		// UN VEHICULO CUYA UNICA LECTURA SE OBSERVO ANTES DE QUE LO ENTREGARAN. Llega asi de dos
		// maneras y las dos son ordinarias: el feed manda una lectura de fabrica o de traslado, o la
		// agencia CORRIGE la fecha de entrega hacia adelante de una lectura que ya estaba.
		private static AScheduleOnARealJournal AVehicleReadBeforeItWasHandedOver()
		{
			AScheduleOnARealJournal journal = ASealedScopeFor(0, 0);

			journal.Api.PublishWarrantyEnvelope(
				ThirtySixMonths, FifteenThousand, TheUnitEverythingIsIn, ThePublication);
			journal.Api.RecordDelivery(
				TheDayItWasHandedOver, TheOdometerAtDelivery, TheUnitEverythingIsIn);
			journal.Api.RecordReading(
				31000, TheUnitEverythingIsIn,
				new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), "Telemetry");

			return journal;
		}

		private static AScheduleOnARealJournal AVehicleWhoseCoveredSystemHasItsOwnLimits(
			int months, int distance)
		{
			return AVehicleRecordedAs(
				ThirtySixMonths, FifteenThousand, TheUnitEverythingIsIn,
				TheDayItWasHandedOver, TheOdometerAtDelivery, TheUnitEverythingIsIn,
				TheOdometerToday, TheUnitEverythingIsIn, months, distance);
		}

		// Un vehiculo entero: la hoja del fabricante sellada, el sobre publicado, la entrega
		// registrada y una lectura del odometro llegada por el feed. TODO por comando, que es lo que
		// hace que la ruta compuesta y la de nueve argumentos no compartan ninguna expresion.
		private static AScheduleOnARealJournal AVehicleRecordedAs(
			int months, int distance, string envelopeUnit,
			DateTime deliveredAt, int deliveryReading, string deliveryUnit,
			int odometer, string odometerUnit,
			int theSystemsOwnMonths, int theSystemsOwnDistance)
		{
			AScheduleOnARealJournal journal =
				ASealedScopeFor(theSystemsOwnMonths, theSystemsOwnDistance);

			journal.Api.PublishWarrantyEnvelope(months, distance, envelopeUnit, ThePublication);
			journal.Api.RecordDelivery(deliveredAt, deliveryReading, deliveryUnit);
			journal.Api.RecordReading(odometer, odometerUnit, TheDayItWasObserved, "Telemetry");

			return journal;
		}

		private static AScheduleOnARealJournal ASealedScopeFor(
			int theSystemsOwnMonths, int theSystemsOwnDistance)
		{
			AScheduleOnARealJournal journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.OpenRevision(TheRevision, ThePublication);
			journal.Api.PublishCoveredSystem(
				TheRevision, TheSystem, theSystemsOwnMonths, theSystemsOwnDistance,
				TheUnitEverythingIsIn);
			journal.Api.PublishCoverageExclusion(
				TheRevision, TheSystem, "wear parts", "when the part is consumable");
			journal.Api.SealRevision(TheRevision);

			return journal;
		}
	}
}
