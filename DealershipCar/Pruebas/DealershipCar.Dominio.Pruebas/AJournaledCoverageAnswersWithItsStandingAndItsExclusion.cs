using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LAS PRUEBAS 11 y 14 de §11 EN EL NIVEL B (leyes L4 y L9). La 11 es la que impide que la app sea
	// tranquilizadora y equivocada; la 14 es la diferencia entre "no nos lo dijeron" y "no esta
	// cubierto", que es para lo que existe L9.
	//
	// QUE AGREGA EL NIVEL B, porque el nivel A ya prueba las dos leyes sobre el agregado directo. La
	// respuesta de cobertura es la unica lectura del modulo que cruza NUEVE hechos recibidos de una
	// vez -- dos instantes, dos lecturas de odometro, dos unidades y el codigo del sistema -- y basta
	// que uno se ate mal para que la respuesta sea otra sin que nada reviente. Y es la lectura cuya
	// respuesta una IA repite como hecho, asi que la que menos puede contestar de mas.
	[TestFixture]
	public sealed class AJournaledCoverageAnswersWithItsStandingAndItsExclusion
	{
		private const string TheVin = "1HGCM82633A004352";
		private const string TheRevision = "R1";
		private const string TheSystem = "TRANSMISSION";
		private const string TheExclusion = "damage from towing above the rated weight";
		private const string TheCondition = "when the vehicle has towed above its rated weight";

		private const int TheEnvelopeMonths = 36;
		private const int TheEnvelopeDistance = 100000;
		private const int OdometerAtDelivery = 10;
		private const int OdometerNow = 12000;

		private static readonly DateTime Published =
			new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime Delivered =
			new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime AsOf =
			new DateTime(2027, 2, 1, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");
			APublishedScope();
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// PRUEBA 11 DE §11, por la puerta: dentro del alcance, dentro del sobre, con una exclusion
		// publicada. La respuesta es CoveredWithExclusion y la frase NOMBRA la exclusion y su
		// condicion.
		[Test]
		public void ACoveredSystemWithAPublishedExclusionIsCoveredWithExclusionAndNamesIt()
		{
			Assert.That(TheStandingOf(TheSystem), Is.EqualTo("CoveredWithExclusion"),
				"Ley L9: ni 'Covered' ni 'NotCovered'. Hay letra chica publicada que la persona "
				+ "deberia leer, y eso es una respuesta propia.");

			string sentence = TheSentenceAbout(TheSystem);

			Assert.That(sentence, Does.Contain(TheExclusion),
				"Ley L4: la exclusion se nombra. Callarla es la peor respuesta posible a una pregunta "
				+ "de cobertura.");
			Assert.That(sentence, Does.Contain(TheCondition),
				"Y con la condicion: este modulo dice que la letra chica existe, no que le toque.");
			Assert.That(sentence, Does.Contain("revisión " + TheRevision),
				"Ley L1, tambien aca: la respuesta nombra de que publicacion salio.");
		}

		// Y CONTESTA LO MISMO DESPUES DEL REPLAY. Son nueve hechos recibidos que cruzan juntos y un
		// alcance que se reconstruye corriendo otra vez sus comandos: cualquiera de las dos mitades
		// puede perderse sin que nada reviente.
		[Test]
		public void TheSameAnswerComesBackAfterTheJournalIsReplayed()
		{
			string live = TheSentenceAbout(TheSystem);

			journal.CloseAndReopenFromTheJournal();

			Assert.That(TheSentenceAbout(TheSystem), Is.EqualTo(live));
			Assert.That(TheStandingOf(TheSystem), Is.EqualTo("CoveredWithExclusion"));
		}

		// PRUEBA 14 DE §11, que es la que la ley L9 existe para separar: un codigo que el catalogo
		// nunca vio contesta NotPublished, NUNCA NotCovered. Uno es "no nos lo dijeron" y el otro es
		// "el fabricante dijo que no". Confundirlos por el wire manda a alguien de vuelta a su casa
		// sin preguntar por una reparacion a la que quiza tenia derecho.
		[Test]
		public void ASystemCodeTheCatalogueHasNeverSeenIsNotPublishedAndNeverNotCovered()
		{
			Assert.That(journal.Api.TheVocabularyIsTheManufacturers("NO-SUCH-SYSTEM"), Is.False);
			Assert.That(TheStandingOf("NO-SUCH-SYSTEM"), Is.EqualTo("NotPublished"));

			Assert.That(journal.Api.TheVocabularyIsTheManufacturers("PAINT"), Is.True,
				"Y un codigo que SOLO aparece nombrado por una exclusion si esta en el catalogo: el "
				+ "catalogo es la union de los dos, y sin esa union este caso contestaria como el de "
				+ "arriba.");
			Assert.That(TheStandingOf("PAINT"), Is.EqualTo("NotCovered"),
				"Que es la otra respuesta, y la diferencia entre las dos es el punto entero de L9.");
		}

		// PRUEBA 12 DE §11 (ley L5), y es lo que hace OBSERVABLE que las dos lecturas de odometro
		// crucen en su ranura. Un sistema con limites PROPIOS de 20000, un vehiculo que ya recorrio
		// 30000 desde la entrega y un sobre de 100000: manda el limite propio y la respuesta es
		// OutsideEnvelope. Si las dos lecturas se ataran a la misma ranura, lo recorrido daria cero y
		// la respuesta seria Covered -- la mas tranquilizadora posible, para alguien que ya no lo
		// esta.
		[Test]
		public void ASystemPastItsOwnLimitsIsOutsideThemEvenInsideTheVehicleEnvelope()
		{
			journal.Api.OpenRevision("R2", Published.AddMonths(1));
			journal.Api.PublishCoveredSystem("R2", "HV-BATTERY", 120, 20000, "UnitA");
			journal.Api.PublishCoveredSystem("R2", TheSystem, 0, 0, "UnitA");
			journal.Api.SealRevision("R2");

			string standing = journal.Api.ReadCoverageStanding(
				"HV-BATTERY", TheEnvelopeMonths, TheEnvelopeDistance, "UnitA",
				Delivered, OdometerAtDelivery, OdometerAtDelivery + 30000, "UnitA", AsOf);

			Assert.That(standing, Is.EqualTo("OutsideEnvelope"),
				"Ley L5: manda el limite propio del sistema, no el sobre del vehiculo. Los 30000 "
				+ "recorridos pasaron los 20000 suyos y no llegan a los 100000 del sobre.");

			Assert.That(
				journal.Api.ReadCoverageStanding(
					TheSystem, TheEnvelopeMonths, TheEnvelopeDistance, "UnitA",
					Delivered, OdometerAtDelivery, OdometerAtDelivery + 30000, "UnitA", AsOf),
				Is.EqualTo("Covered"),
				"Y el que no publico limites propios, con los MISMOS numeros, sigue dentro del sobre. "
				+ "Las dos respuestas juntas son la ley: la diferencia la hace el limite, no el "
				+ "vehiculo.");
		}

		// EL ALCANCE SE DICE EN TODA RESPUESTA DE COBERTURA, incluida la que dice que no hay nada
		// publicado. Es la frase que una IA repite, y sin ella la respuesta mas facil de leer como
		// "entonces no estas cubierto" es justo la que no lo dice.
		[Test]
		public void EveryCoverageAnswerSaysItIsScopeAndNotTheOutcomeOfAClaim()
		{
			foreach (string code in new[] { TheSystem, "PAINT", "NO-SUCH-SYSTEM" })
				Assert.That(
					TheSentenceAbout(code),
					Does.Contain("nada sobre cómo se resolvería un reclamo"),
					"La clausula de alcance falta en la respuesta sobre '" + code + "'.");

			Assert.That(
				journal.Api.DescribeCoverageSummary(),
				Does.Contain("nada sobre cómo se resolvería un reclamo"));
		}

		// UN CODIGO DE SISTEMA SE RECORTA EN LA LECTURA IGUAL QUE EN LA PUBLICACION. Es lo unico que
		// hace que quien publica y quien pregunta nombren la misma cosa: el dominio compara ordinal y
		// no recorta, asi que si una de las dos puntas no lo hiciera, un sistema publicado como
		// " X " no lo encontraria nunca una pregunta por "X" -- y la respuesta seria NotPublished,
		// que se lee como un hecho del fabricante.
		[Test]
		public void ASystemCodeIsTrimmedOnTheReadTheSameWayItIsOnThePublish()
		{
			Assert.That(TheStandingOf("  " + TheSystem + "  "),
				Is.EqualTo("CoveredWithExclusion"));
			Assert.That(journal.Api.TheVocabularyIsTheManufacturers("  " + TheSystem + "  "),
				Is.True);
			Assert.That(journal.Api.ReadRevisionState("  " + TheRevision + "  "),
				Is.EqualTo("Sealed"),
				"Y un codigo de revision igual: sin el recorte, esta lectura reventaria diciendo que "
				+ "la revision no existe.");
		}

		private void APublishedScope()
		{
			journal.Api.OpenRevision(TheRevision, Published);

			journal.Api.PublishCoveredSystem(TheRevision, TheSystem, 0, 0, "UnitA");
			journal.Api.PublishCoverageExclusion(
				TheRevision, TheSystem, TheExclusion, TheCondition);
			journal.Api.PublishCoverageExclusion(
				TheRevision, "PAINT", "paintwork and exterior trim are outside the warranty",
				"from delivery onwards");

			journal.Api.SealRevision(TheRevision);
		}

		private string TheStandingOf(string systemCode)
		{
			return journal.Api.ReadCoverageStanding(
				systemCode, TheEnvelopeMonths, TheEnvelopeDistance, "UnitA",
				Delivered, OdometerAtDelivery, OdometerNow, "UnitA", AsOf);
		}

		private string TheSentenceAbout(string systemCode)
		{
			return journal.Api.DescribeCoverage(
				systemCode, TheEnvelopeMonths, TheEnvelopeDistance, "UnitA",
				Delivered, OdometerAtDelivery, OdometerNow, "UnitA", AsOf);
		}
	}
}
