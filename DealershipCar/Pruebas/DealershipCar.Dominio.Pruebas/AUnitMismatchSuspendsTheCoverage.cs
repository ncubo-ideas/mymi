using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L8 del brief, UnitsAreNeverConverted, sobre el camino de cobertura, y la mitad de
	// cobertura del TEST 9 DE §11: "la respuesta a una comparacion entre unidades distintas es NO
	// SE PUEDE EVALUAR -- nunca un numero convertido."
	//
	// ES LA DEPARTURE D3 DEL PLAN Y LA CONTRADICCION C47. CoverageStanding tiene cinco miembros y
	// NINGUNO quiere decir "no lo se". Devolver Covered ahi es el tercer anti-patron de E47 y el
	// mas caro: una respuesta benigna para una entrada inevaluable, que ninguna capa de abajo
	// puede distinguir de un hecho y que una vista, un reporte o un asistente repiten como tal.
	// "No pudimos medirlo" se convierte en "esta cubierto", para siempre.
	//
	// POR ESO LA LECTURA DEVUELVE UNA JERARQUIA E47 y no un miembro: base abierta CoverageAnswer,
	// una variante que si contesta y otra que NOMBRA EN SU PROPIO TIPO por que no puede. Las
	// preguntas de la base son TOTALES -- se puede evaluar, esta suspendida por unidades -- y el
	// accesor del resultado esta GUARDADO. L9 queda intacta: el conjunto de cinco sigue siendo el
	// conjunto de resultados, y lo inevaluable vive AFUERA de el, con nombre propio.
	//
	// EL ORDEN DEL PROCEDIMIENTO ES LA LEY, y aca se ve: si la mitad de TIEMPO ya decidio en
	// contra, la respuesta esta decidida y no se suspende. Nada que medir en kilometros cambia
	// que el plazo se cumplio.
	[TestFixture]
	public sealed class AUnitMismatchSuspendsTheCoverage
	{
		private const string TheModel = "SM-SEDAN-2025";
		private const string TheRevision = "REV-2025-01";
		private const string TheSystem = "GEARBOX";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);
		private static readonly DateTime DeliveredAt = new DateTime(2024, 1, 15, 9, 0, 0);
		private static readonly DateTime AsOf = new DateTime(2024, 7, 15, 9, 0, 0);
		private static readonly DateTime LongAfterTheEnvelope = new DateTime(2040, 7, 15, 9, 0, 0);

		private ModelSpecification specification;

		[SetUp]
		public void Setup()
		{
			specification = new ModelSpecification(TheModel);
			specification.OpenRevision(TheRevision, PublishedAt);
		}

		[Test]
		public void AGoverningLimitInOneUnitAndAnOdometerInAnotherCannotBeEvaluated()
		{
			ASystemWhoseOwnDistanceLimitIsIn(DistanceUnitCode.UnitB);

			Assert.That(CanItBeEvaluated(), Is.False,
				"Ley L8: dos valores en unidades distintas no son comparables, y aca no se "
				+ "convierten.");
		}

		// EL ANTI-PATRON QUE ESTE TIPO EXISTE PARA IMPEDIR. Ninguna de las dos respuestas
		// benignas puede salir de aca.
		[Test]
		public void AnUnevaluableComparisonAnswersNeitherCoveredNorNotCovered()
		{
			ASystemWhoseOwnDistanceLimitIsIn(DistanceUnitCode.UnitB);

			string sentence = TheSentence();

			Assert.That(sentence, Does.Not.Contain("Covered"),
				"E47, tercer anti-patron: devolver el miembro benigno del conjunto para una entrada "
				+ "que no se pudo evaluar. Nadie rio abajo puede distinguirlo de un hecho.");
			Assert.That(sentence, Does.Not.Contain("NotCovered"));
		}

		// EL ACCESOR ESTA GUARDADO. Es la unica lectura guardada del camino de cobertura y la
		// razon de C47: leer el resultado sin haber preguntado si se podia evaluar es un defecto
		// de quien llama, no un valor por defecto.
		[Test]
		public void TheStandingCannotBeReadWithoutAskingWhetherItCouldBeEvaluated()
		{
			ASystemWhoseOwnDistanceLimitIsIn(DistanceUnitCode.UnitB);

			DomainException error = Assert.Throws<DomainException>(() => TheStanding());

			Assert.That(error.Message, Is.Not.Empty,
				"Y el mensaje dice que hay que preguntar primero, en palabras: un mensaje que cita "
				+ "un documento no se puede leer donde revienta.");
		}

		// La frase SI contesta, y dice en palabras por que no se pudo: nombra las dos unidades y
		// dice que no se convierten. Callarlo dejaria a la persona sin saber que la pregunta
		// quedo sin contestar.
		[Test]
		public void TheSentenceSaysItCannotBeEvaluatedAndNamesBothUnits()
		{
			ASystemWhoseOwnDistanceLimitIsIn(DistanceUnitCode.UnitB);

			string sentence = TheSentence();

			Assert.That(sentence, Does.Contain("no se puede evaluar"));
			Assert.That(sentence, Does.Contain("límite de distancia que rige está en mi"));
			Assert.That(sentence, Does.Contain("odómetro de este vehículo está en km"));
			Assert.That(sentence, Does.Contain("nunca se convierten"));
			Assert.That(sentence, Does.Contain(TheSystem));
		}

		// El sobre del vehiculo tambien puede ser el que no calza, cuando el sistema no tiene
		// limites propios: lo que se compara es SIEMPRE el limite que rige.
		[Test]
		public void TheVehiclesEnvelopeCanBeTheHalfThatDoesNotMatch()
		{
			specification.PublishCoveredSystem(TheRevision, TheSystem, 0, 0, DistanceUnitCode.UnitA);
			specification.SealRevision(TheRevision);

			Assert.That(
				specification.CanCoverageBeEvaluated(
					TheSystem, 36, 100000, DistanceUnitCode.UnitB,
					DeliveredAt, 10, 12000, DistanceUnitCode.UnitA, AsOf),
				Is.False);
		}

		// EL ORDEN. Si el plazo ya se cumplio, la respuesta esta decidida: OutsideEnvelope, y no
		// se suspende por una mitad de distancia que no se puede medir. Lo inevaluable degrada la
		// respuesta solo cuando todavia podia cambiarla.
		[Test]
		public void ATimeHalfThatAlreadyDecidedAgainstCoverIsNotSuspendedByTheOtherHalf()
		{
			ASystemWhoseOwnDistanceLimitIsIn(DistanceUnitCode.UnitB);

			Assert.That(
				specification.CanCoverageBeEvaluated(
					TheSystem, 36, 100000, DistanceUnitCode.UnitA,
					DeliveredAt, 10, 12000, DistanceUnitCode.UnitA, LongAfterTheEnvelope),
				Is.True,
				"El plazo se cumplio: la mitad que no se puede medir ya no puede cambiar la "
				+ "respuesta, asi que no hay nada que suspender.");
			Assert.That(
				specification.ReadCoverageStanding(
					TheSystem, 36, 100000, DistanceUnitCode.UnitA,
					DeliveredAt, 10, 12000, DistanceUnitCode.UnitA, LongAfterTheEnvelope),
				Is.EqualTo("OutsideEnvelope"));
		}

		// Y EL ORDEN OTRA VEZ, del otro lado: un codigo que el catalogo no vio nunca contesta
		// NotPublished aunque las unidades no calcen. El paso 1 decide antes de que haya nada que
		// medir.
		[Test]
		public void AnUnknownCodeAnswersNotPublishedEvenWhenTheUnitsWouldNotMatch()
		{
			ASystemWhoseOwnDistanceLimitIsIn(DistanceUnitCode.UnitB);

			Assert.That(
				specification.ReadCoverageStanding(
					"NO-SUCH", 36, 100000, DistanceUnitCode.UnitB,
					DeliveredAt, 10, 12000, DistanceUnitCode.UnitA, AsOf),
				Is.EqualTo("NotPublished"));
		}

		// Y el paso 2: un sistema que el alcance solo excluye contesta NotCovered aunque las
		// unidades no calcen. Suspender ahi diria que no se sabe algo que si se sabe.
		[Test]
		public void ASystemNamedOnlyByAnExclusionAnswersNotCoveredEvenWithMismatchedUnits()
		{
			specification.PublishCoverageExclusion(
				TheRevision, "PAINT", "paintwork is outside the warranty", "from delivery onwards");
			specification.SealRevision(TheRevision);

			Assert.That(
				specification.ReadCoverageStanding(
					"PAINT", 36, 100000, DistanceUnitCode.UnitB,
					DeliveredAt, 10, 12000, DistanceUnitCode.UnitA, AsOf),
				Is.EqualTo("NotCovered"));
		}

		// Un limite que NO tiene mitad de distancia no se puede suspender por unidades: no hay
		// nada que comparar. Suspender ahi seria inventar una comparacion que nadie publico.
		[Test]
		public void ALimitWithNoDistanceHalfIsNeverSuspendedByAUnitMismatch()
		{
			specification.PublishCoveredSystem(TheRevision, TheSystem, 60, 0, DistanceUnitCode.UnitB);
			specification.SealRevision(TheRevision);

			Assert.That(
				specification.CanCoverageBeEvaluated(
					TheSystem, 36, 100000, DistanceUnitCode.UnitB,
					DeliveredAt, 10, 12000, DistanceUnitCode.UnitA, AsOf),
				Is.True);
			Assert.That(
				specification.ReadCoverageStanding(
					TheSystem, 36, 100000, DistanceUnitCode.UnitB,
					DeliveredAt, 10, 12000, DistanceUnitCode.UnitA, AsOf),
				Is.EqualTo("Covered"));
		}

		// Cuando SI se puede evaluar, la pregunta total contesta que si y el accesor no revienta.
		// Sin esto, la prueba de arriba pasaria con un accesor que revienta siempre.
		[Test]
		public void AnEvaluableComparisonAnswersAndDoesNotThrow()
		{
			ASystemWhoseOwnDistanceLimitIsIn(DistanceUnitCode.UnitA);

			Assert.That(CanItBeEvaluated(), Is.True);
			Assert.That(TheStanding(), Is.EqualTo("Covered"));
		}

		// La suspension sigue nombrando la exclusion publicada (D7): la respuesta no se decidio,
		// pero la letra chica que hay sobre ese sistema es un hecho publicado igual.
		[Test]
		public void ASuspendedAnswerStillNamesAnApplicableExclusion()
		{
			specification.PublishCoveredSystem(TheRevision, TheSystem, 0, 60000, DistanceUnitCode.UnitB);
			specification.PublishCoverageExclusion(
				TheRevision, TheSystem, "wear of the synchros", "at any time, as a wear item");
			specification.SealRevision(TheRevision);

			Assert.That(TheSentence(), Does.Contain("wear of the synchros"));
		}

		private void ASystemWhoseOwnDistanceLimitIsIn(DistanceUnitCode unit)
		{
			specification.PublishCoveredSystem(TheRevision, TheSystem, 60, 60000, unit);
			specification.SealRevision(TheRevision);
		}

		private bool CanItBeEvaluated()
		{
			return specification.CanCoverageBeEvaluated(
				TheSystem, 36, 100000, DistanceUnitCode.UnitA,
				DeliveredAt, 10, 12000, DistanceUnitCode.UnitA, AsOf);
		}

		private string TheStanding()
		{
			return specification.ReadCoverageStanding(
				TheSystem, 36, 100000, DistanceUnitCode.UnitA,
				DeliveredAt, 10, 12000, DistanceUnitCode.UnitA, AsOf);
		}

		private string TheSentence()
		{
			return specification.DescribeCoverage(
				TheSystem, 36, 100000, DistanceUnitCode.UnitA,
				DeliveredAt, 10, 12000, DistanceUnitCode.UnitA, AsOf);
		}
	}
}
