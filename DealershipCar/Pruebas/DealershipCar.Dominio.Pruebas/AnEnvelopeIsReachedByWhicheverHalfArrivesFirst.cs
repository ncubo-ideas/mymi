using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// TEST 6 DE §11 DEL BRIEF, y la consecuencia que T2 heredo de la decision D2: cual llega
	// primero se contesta con DOS PREDICADOS, siguiendo a DueAssessment, y nunca con un miembro de
	// un conjunto de dos -- porque la pregunta tiene TRES respuestas honestas: tiempo, distancia, y
	// las dos.
	//
	// LOS CUATRO CASOS, y el sobre es el mismo en los cuatro (36 meses / 60000 desde 2024-01-01 con
	// el odometro en 10):
	//
	//   pasado el limite de distancia, adentro del de meses -> Reached, nombra DISTANCIA
	//   pasado el de meses, adentro del de distancia         -> Reached, nombra TIEMPO
	//   pasados los dos                                      -> Reached, nombra LAS DOS
	//   ninguno de los dos                                   -> Inside, y NO se pronostica cual
	//
	// EL CUARTO CASO ES UN HALLAZGO DE T2 y esta escrito aqui porque es donde se ve. OSD-09 pide
	// que el sobre diga "cual de las dos llegara primero". ANTES DE QUE UNA LLEGUE, ESO NO SE PUEDE
	// DECIR SIN CONVERTIR: comparar "12 meses que quedan" con "35010 km que quedan" exige un
	// promedio de km por mes, y ese promedio ES el factor de conversion que la ley L4 prohibe --
	// mas un pronostico sobre una lectura de edad desconocida, que es justo el riesgo que §3 del
	// brief nombra. Asi que la frase dice CON PALABRAS que no lo predice (E48: el hueco se dice, no
	// se rellena), y el precedente del modulo coincide: LimitStanding.IsMoreAdvancedThan compara
	// BALDES -- no numeros -- y dos mitades sin alcanzar estan igual de avanzadas, o sea empatadas,
	// que es el caso en que DueAssessment dice "las dos".
	//
	// LOS PREDICADOS SE PRUEBAN SOBRE EL PAR ARMADO A MANO y la composicion sobre la FRASE del
	// perfil, a proposito: el perfil no expone el par derivado -- §4.2 no tiene ninguna lectura que
	// lo devuelva -- y un miembro cuyo unico llamador es una prueba es superficie que nadie usa.
	[TestFixture]
	public sealed class AnEnvelopeIsReachedByWhicheverHalfArrivesFirst
	{
		private const string TheVin = "3VW2K7AJ9FM123456";
		private const int ThirtySixMonths = 36;
		private const int SixtyThousand = 60000;
		private const int TheDeliveryReading = 10;
		private const int AReadingPastTheDistanceHalf = 75000;
		private const int AReadingWellInsideTheDistanceHalf = 25000;

		private static readonly DateTime PublishedAt = new DateTime(2023, 12, 1);
		private static readonly DateTime DeliveredAt = new DateTime(2024, 1, 1);
		private static readonly DateTime ObservedAt = new DateTime(2026, 1, 1);
		private static readonly DateTime InsideTheTimeHalf = new DateTime(2026, 1, 1);
		private static readonly DateTime PastTheTimeHalf = new DateTime(2027, 6, 1);

		private VehicleProfile profile;

		[SetUp]
		public void Setup()
		{
			profile = new VehicleProfile(TheVin);
			profile.RecordDelivery(DeliveredAt, TheDeliveryReading, DistanceUnitCode.UnitA);
			profile.PublishWarrantyEnvelope(
				ThirtySixMonths, SixtyThousand, DistanceUnitCode.UnitA, PublishedAt);
		}

		// ===== LOS DOS PREDICADOS, SOBRE EL PAR ============================================

		[Test]
		public void TheTwoPredicatesCoverTheThreeHonestAnswers()
		{
			RemainingEnvelope soloDistancia = ThePair(12, -14990);
			RemainingEnvelope soloTiempo = ThePair(-5, 35010);
			RemainingEnvelope lasDos = ThePair(-5, -14990);

			Assert.That(soloDistancia.DecidedByDistance, Is.True);
			Assert.That(soloDistancia.DecidedByTime, Is.False,
				"El tiempo no decidio nada: quedan doce meses.");

			Assert.That(soloTiempo.DecidedByTime, Is.True);
			Assert.That(soloTiempo.DecidedByDistance, Is.False);

			Assert.That(lasDos.DecidedByTime, Is.True);
			Assert.That(lasDos.DecidedByDistance, Is.True,
				"Las dos llegaron, asi que deciden las dos. Elegir una seria inventar cual, y es "
				+ "exactamente lo que un conjunto cerrado de dos miembros obligaria a hacer (D2).");
		}

		// Y CUANDO LA MITAD DE DISTANCIA NO SE PUDO MEDIR, decide el tiempo y solo el tiempo -- la
		// misma degradacion que TimeOrDistance.WhicheverLimitArrivesFirst ya hace.
		[Test]
		public void WhenTheDistanceHalfCouldNotBeMeasuredTimeDecidesAlone()
		{
			RemainingEnvelope degradado = RemainingEnvelope.MeasuredOnlyInTime(
				-5, ThirtySixMonths, "because this test is about the shape.");

			Assert.That(degradado.TheStandingCanBeDecided, Is.True,
				"El tiempo ya llego, asi que la distancia no puede cambiar la respuesta.");
			Assert.That(degradado.DecidedByTime, Is.True);
			Assert.That(degradado.DecidedByDistance, Is.False);
			Assert.That(degradado.TheEnvelopeIsReached, Is.True);
		}

		// Y LO QUE NO SE PUDO DECIDIR NO ENTREGA NINGUNO DE LOS DOS PREDICADOS: un false aqui se
		// leeria como "esa mitad no decidio", que es una medicion, y no como "no se pudo saber".
		// Es el mismo guard que Standing() lleva un piso mas arriba: un booleano tampoco tiene un
		// tercer valor que diga "no lo se".
		[Test]
		public void AnUndecidablePairRefusesToNameWhichHalfDecided()
		{
			RemainingEnvelope indecidible = RemainingEnvelope.MeasuredOnlyInTime(
				12, ThirtySixMonths, "because this test is about the shape.");

			Assert.That(indecidible.TheStandingCanBeDecided, Is.False);
			Assert.Throws<DomainException>(() => { bool _ = indecidible.DecidedByTime; });
			Assert.Throws<DomainException>(() => { bool _ = indecidible.DecidedByDistance; });
			Assert.Throws<DomainException>(() => { bool _ = indecidible.TheEnvelopeIsReached; });
			Assert.Throws<DomainException>(() => { int _ = indecidible.DistanceLeft; });
			Assert.Throws<DomainException>(() => { bool _ = indecidible.TheDistanceHalfIsReached; });
		}

		// ===== LA COMPOSICION, SOBRE LA FRASE Y EL STANDING ================================

		[Test]
		public void PastTheDistanceLimitButInsideTheMonthsLimitNamesDistance()
		{
			Assert.That(TheStandingAt(AReadingPastTheDistanceHalf, InsideTheTimeHalf),
				Is.EqualTo("Reached"));

			string said = TheSentenceAt(AReadingPastTheDistanceHalf, InsideTheTimeHalf);

			Assert.That(said, Does.Contain("Alcanzado por distancia"));
			Assert.That(said, Does.Contain("12 meses de 36 meses"),
				"Ley L3: la otra mitad no se omite. Que el sobre se haya alcanzado por distancia no "
				+ "borra que todavia quedan doce meses de plazo.");
			Assert.That(said, Does.Contain("14990 km pasados de un límite de 60000 km"));
		}

		[Test]
		public void PastTheMonthsLimitButInsideTheDistanceLimitNamesTime()
		{
			Assert.That(TheStandingAt(AReadingWellInsideTheDistanceHalf, PastTheTimeHalf),
				Is.EqualTo("Reached"));

			string said = TheSentenceAt(AReadingWellInsideTheDistanceHalf, PastTheTimeHalf);

			Assert.That(said, Does.Contain("Alcanzado por tiempo"));
			Assert.That(said, Does.Contain("5 meses pasados de un límite de 36 meses"));
			Assert.That(said, Does.Contain("35010 km de 60000 km"),
				"Ley L3, del otro lado: quedan 35010 km y se dicen, aunque no cambien la respuesta.");
		}

		[Test]
		public void PastBothLimitsNamesBothAndNotOne()
		{
			Assert.That(TheSentenceAt(AReadingPastTheDistanceHalf, PastTheTimeHalf),
				Does.Contain("tiempo y distancia"));
		}

		[Test]
		public void InsideBothHalvesTheAnswerRefusesToPredictWhichArrivesFirst()
		{
			Assert.That(TheStandingAt(AReadingWellInsideTheDistanceHalf, InsideTheTimeHalf),
				Is.EqualTo("Inside"));

			string said = TheSentenceAt(AReadingWellInsideTheDistanceHalf, InsideTheTimeHalf);

			Assert.That(said, Does.Contain("no son comparables"),
				"Las dos mitades son incomparables entre si (GAP G4). Convertir una en la otra por "
				+ "un promedio de km por mes es la conversion que L4 prohibe, con un pronostico "
				+ "encima.");
			Assert.That(said, Does.Not.Contain("per month"),
				"Y no aparece ninguna tasa: una tasa aqui seria un pronostico sacado de una lectura "
				+ "de edad desconocida, presentado como si fuera una medicion.");
		}

		// EL LIMITE EXACTO TIENE FRASE PROPIA, como en AMeasuredDimension: "0 pasados del limite"
		// se leeria como un defecto y no como una respuesta. Y esta ALCANZADO: los umbrales de este
		// modulo son inclusivos.
		[Test]
		public void ExactlyAtALimitIsReachedAndSaidWithItsOwnWords()
		{
			int elOdometroExacto = SixtyThousand + TheDeliveryReading;

			Assert.That(TheStandingAt(elOdometroExacto, InsideTheTimeHalf), Is.EqualTo("Reached"));

			string said = TheSentenceAt(elOdometroExacto, InsideTheTimeHalf);

			Assert.That(said, Does.Contain("justo en un límite de 60000 km"));
			Assert.That(said, Does.Not.Contain("0 km past"));
		}

		// Y el mes exacto igual: el aniversario cumplido ya esta alcanzado, y el dia anterior no.
		// Los meses se cuentan ENTEROS, como en ASystemLimits.
		[Test]
		public void TheMonthIsWholeSoTheDayBeforeTheAnniversaryIsStillInside()
		{
			Assert.That(
				TheStandingAt(AReadingWellInsideTheDistanceHalf, new DateTime(2026, 12, 31)),
				Is.EqualTo("Inside"));
			Assert.That(
				TheSentenceAt(AReadingWellInsideTheDistanceHalf, new DateTime(2026, 12, 31)),
				Does.Contain("queda 1 mes de 36 meses"),
				"El 2026-12-31 hay 35 meses cumplidos desde el 2024-01-01: falta uno, y se dice en "
				+ "singular.");

			Assert.That(
				TheStandingAt(AReadingWellInsideTheDistanceHalf, new DateTime(2027, 1, 1)),
				Is.EqualTo("Reached"));
			Assert.That(
				TheSentenceAt(AReadingWellInsideTheDistanceHalf, new DateTime(2027, 1, 1)),
				Does.Contain("justo en un límite de 36 meses"));
		}

		// UN ODOMETRO QUE VA PARA ATRAS NO ES UNA AUSENCIA, ES UN HECHO IMPOSIBLE (E32), y revienta
		// -- el mismo veredicto que LimitStanding.OfDistance ya toma y por la misma razon: cual de
		// las dos lecturas esta mal lo decide quien las ordena, no el sobre. Aca no cuesta un
		// asiento del journal porque es una LECTURA y una lectura no escribe nada.
		[Test]
		public void AnOdometerBelowTheDeliveryReadingIsRefusedAndNotReportedAsDistanceLeft()
		{
			Assert.Throws<DomainException>(
				() => TheSentenceAt(TheDeliveryReading - 1, InsideTheTimeHalf));
		}

		private static RemainingEnvelope ThePair(int monthsLeft, int distanceLeft)
		{
			return RemainingEnvelope.InBothHalves(
				monthsLeft, ThirtySixMonths, distanceLeft, SixtyThousand, DistanceUnit.UnitA);
		}

		private string TheSentenceAt(int reading, DateTime asOf)
		{
			return profile.DescribeWarrantyEnvelope(
				reading, DistanceUnitCode.UnitA, ObservedAt, asOf);
		}

		private string TheStandingAt(int reading, DateTime asOf)
		{
			return profile.ReadEnvelopeStanding(
				reading, DistanceUnitCode.UnitA, ObservedAt, asOf);
		}
	}
}
