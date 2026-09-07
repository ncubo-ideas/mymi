using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L2, Y ES LA PRUEBA QUE MAS IMPORTA DEL SLICE. Tests 3 y 4 de §11 del brief.
	//
	// "El agregado guarda los dos LIMITES y el ancla de la entrega. Lo que QUEDA y cual llega
	// primero se calculan al instante preguntado, desde un instante recibido y una lectura
	// recibida. Un remaining guardado es correcto para un solo instante y esta equivocado desde el
	// siguiente." (E35, y el /// de LimitStanding es el precedente del propio modulo: "una
	// respuesta calculada en cada evaluacion, nunca estado guardado".)
	//
	// EL ESCENARIO ES EL DEL BRIEF, y esta elegido para que la respuesta EQUIVOCADA se vea
	// distinta (C60): sobre de 36 meses / 60000, entrega el 2024-01-01 con el odometro en 10,
	// lectura de 25000 observada el 2026-01-01.
	//
	//   preguntado el 2026-01-01 -> 24 meses cumplidos, quedan 12; recorridos 24990, quedan 35010
	//   preguntado el 2026-07-01 -> 30 meses cumplidos, quedan  6; recorridos 24990, quedan 35010
	//
	// LA MITAD DE TIEMPO SE MOVIO Y LA DE DISTANCIA NO, y entre las dos llamadas no se publico
	// nada y no llego ninguna lectura. Los cuatro numeros son distintos entre si, asi que un slot
	// defaulteado o una mitad congelada se ven.
	//
	// TEST 4 -- "ningun miembro guarda un remaining" -- tiene DOS mitades y las dos hacen falta:
	// la conductual es la de arriba (si estuviera guardado, la mitad de tiempo no se moveria), y la
	// estructural es por reflexion, porque un campo guardado que ADEMAS se recalculara pasaria la
	// conductual sin que nadie lo note.
	[TestFixture]
	public sealed class RemainingIsDerivedNeverStored
	{
		private const string TheVin = "3VW2K7AJ9FM123456";
		private const int ThirtySixMonths = 36;
		private const int SixtyThousand = 60000;
		private const int TheDeliveryReading = 10;
		private const int TheReadingObserved = 25000;

		private static readonly DateTime PublishedAt = new DateTime(2023, 12, 1);
		private static readonly DateTime DeliveredAt = new DateTime(2024, 1, 1);
		private static readonly DateTime ObservedAt = new DateTime(2026, 1, 1);
		private static readonly DateTime TwoYearsIn = new DateTime(2026, 1, 1);
		private static readonly DateTime SixMonthsLater = new DateTime(2026, 7, 1);

		private VehicleProfile profile;

		[SetUp]
		public void Setup()
		{
			profile = new VehicleProfile(TheVin);
			profile.RecordDelivery(DeliveredAt, TheDeliveryReading, DistanceUnitCode.UnitA);
			profile.PublishWarrantyEnvelope(
				ThirtySixMonths, SixtyThousand, DistanceUnitCode.UnitA, PublishedAt);
		}

		// TEST 3, LA VERSION CONDUCTUAL DE TODO ESTO.
		[Test]
		public void TheTimeHalfMovesWithTheClockAndTheDistanceHalfDoesNot()
		{
			string atTwoYears = TheSentenceAt(TwoYearsIn);
			string sixMonthsLater = TheSentenceAt(SixMonthsLater);

			Assert.That(atTwoYears, Does.Contain("quedan 12 meses"));
			Assert.That(sixMonthsLater, Does.Contain("quedan 6 meses"),
				"L2: la mitad de tiempo se recalcula contra el instante preguntado. Guardada, "
				+ "seguiria diciendo 12 seis meses despues.");

			Assert.That(atTwoYears, Does.Contain("quedan 35010 km"));
			Assert.That(sixMonthsLater, Does.Contain("quedan 35010 km"),
				"Y la de distancia NO se movio: no llego ninguna lectura nueva. Si se hubiera "
				+ "movido, algo la estaria derivando del reloj -- que es la mitad que no depende "
				+ "del reloj.");
		}

		// Las dos mitades se dicen SIEMPRE, tambien cuando una esta comoda y la otra no. Es L3, y
		// aca solo se afirma que las dos aparecen en las dos llamadas: la ley entera vive en
		// BothDimensionsAreAlwaysSaid.
		[Test]
		public void BothHalvesAppearInBothAnswers()
		{
			foreach (DateTime moment in new[] { TwoYearsIn, SixMonthsLater })
			{
				Assert.That(TheSentenceAt(moment), Does.Contain("Tiempo:"), moment.ToString());
				Assert.That(TheSentenceAt(moment), Does.Contain("Distancia:"), moment.ToString());
			}
		}

		// Nada se publico entre las dos preguntas, y la frase lo dice: las dos nombran el mismo
		// instante de publicacion (L9). Sin esto, la prueba de arriba seria compatible con que la
		// segunda llamada hubiera leido otro sobre.
		[Test]
		public void NothingWasPublishedBetweenTheTwoQuestionsAndBothNameTheSamePublication()
		{
			Assert.That(TheSentenceAt(TwoYearsIn), Does.Contain("2023-12-01"));
			Assert.That(TheSentenceAt(SixMonthsLater), Does.Contain("2023-12-01"));
		}

		// La respuesta es un OBJETO INMUTABLE, no una ventana al agregado: se captura, se mueve el
		// reloj, y la capturada sigue diciendo lo que decia. Es la mitad falsificable del test 3 --
		// si EnvelopeAnswer guardara el perfil y volviera a resolver, esto lo diria.
		[Test]
		public void AnAnswerAlreadyGivenDoesNotMoveWhenTheNextOneIsAsked()
		{
			EnvelopeAnswer captured = TheAnswerAt(TwoYearsIn);

			string later = TheSentenceAt(SixMonthsLater);

			Assert.That(captured.Describe(), Does.Contain("quedan 12 meses"));
			Assert.That(later, Does.Contain("quedan 6 meses"));
		}

		// ===== TEST 4, LA MITAD ESTRUCTURAL ================================================

		// NADA DE LO QUE SOBREVIVE ENTRE DOS LECTURAS GUARDA UN REMAINING. El barrido es sobre lo
		// que el agregado RETIENE -- el perfil, la entrega y el sobre -- y no sobre el par derivado
		// ni sobre la respuesta: esos dos se construyen de nuevo en cada lectura y guardan lo que
		// acabaron de calcular, que es justamente lo contrario del defecto.
		//
		// Y se afirma sobre los campos del ensamblado en vez de leyendo el codigo, porque un campo
		// llamado _left o _standing es el mismo defecto con otro nombre.
		[Test]
		public void NothingThatSurvivesBetweenTwoReadsHoldsWhatIsLeftOrTheStanding()
		{
			List<string> culpables = new List<string>();

			foreach (Type tipo in LosTiposDelSobre())
			{
				foreach (FieldInfo campo in tipo.GetFields(
					BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
				{
					string nombre = campo.Name.ToLowerInvariant();

					if (nombre.Contains("remaining") || nombre.Contains("left")
						|| nombre.Contains("standing"))
						culpables.Add(tipo.Name + "." + campo.Name);
				}
			}

			Assert.That(culpables, Is.Empty,
				"L2: aparecio estado con forma de remaining o de standing: "
				+ string.Join(", ", culpables) + ". Lo que queda es correcto para un instante y "
				+ "esta viejo desde el siguiente; guardarlo deja al agregado equivocado entre el "
				+ "instante en que el sobre se alcanzo y la proxima vez que alguien pregunte.");
		}

		// Y EL AGREGADO NO GUARDA MAS QUE EL ANCLA, LOS LIMITES Y LAS PLACAS RECIBIDAS (§3.2 del plan:
		// "nada mas"). Un campo de mas es donde una respuesta cacheada entra sin que nadie lo llame
		// cache.
		//
		// LP-1 LO ENSANCHO, Y ES LO QUE UN CENSO CON CONJUNTO EXACTO TIENE QUE HACER CUANDO EL
		// AGREGADO CRECE POR DECISION. SLICE-LicensePlate.md §13 G2 decidio que la placa es un hecho
		// por Vin y vive en este agregado, asi que PLAN-PlateAndCanon.md §4.4 le da una lista de
		// asignaciones recibidas. La lista NO es una respuesta cacheada: guarda lo que la sucursal
		// mando, igual que ShopStay guarda "_stays : List<AStay>", y lo derivado -- cual rige a un
		// instante -- se vuelve a resolver en cada lectura. El nombre se renombro con el censo: uno
		// que enumera y no se renombra al ensancharse queda MINTIENDO, que es peor que quedarse corto.
		//
		// SE COMPARAN TIPOS Y NO NOMBRES DE TIPO. Un "List`1" pelado habria dejado pasar una lista de
		// cualquier cosa -- incluida una de respuestas cacheadas, que es justo el defecto que esta
		// prueba existe para ver -- y ademas el compilador ahora defiende cada renglon.
		[Test]
		public void TheAggregateHoldsTheVinTheAnchorTheLimitsAndThePlatesAndNothingElse()
		{
			List<Type> campos = typeof(VehicleProfile)
				.GetFields(BindingFlags.Instance | BindingFlags.NonPublic
					| BindingFlags.Public | BindingFlags.DeclaredOnly)
				.Select(campo => campo.FieldType)
				.OrderBy(tipo => tipo.Name, StringComparer.Ordinal)
				.ToList();

			Assert.That(campos, Is.EqualTo(new[]
			{
				typeof(Delivery), typeof(List<APlateAssignment>), typeof(Vin),
				typeof(WarrantyEnvelope),
			}),
				"§3.2: el perfil tiene el ancla, los dos limites y las placas recibidas, y calcula "
				+ "todo lo demas.");
		}

		// Ningun miembro del par derivado MUTA nada: son todos de lectura. Un setter aqui seria la
		// puerta por la que un remaining se vuelve estado.
		[Test]
		public void NothingOnTheDerivedPairCanBeSet()
		{
			List<string> culpables = typeof(RemainingEnvelope)
				.GetProperties(BindingFlags.Instance | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
				.Where(propiedad => propiedad.GetSetMethod(true) != null)
				.Select(propiedad => propiedad.Name)
				.ToList();

			Assert.That(culpables, Is.Empty,
				"El par derivado se calcula y se dice; no se le escribe: " + string.Join(", ", culpables));
		}

		private string TheSentenceAt(DateTime asOf)
		{
			return profile.DescribeWarrantyEnvelope(
				TheReadingObserved, DistanceUnitCode.UnitA, ObservedAt, asOf);
		}

		private EnvelopeAnswer TheAnswerAt(DateTime asOf)
		{
			return profile.TheStandingAsOf(
				TheReadingObserved, DistanceUnitCode.UnitA, ObservedAt, asOf);
		}

		private static List<Type> LosTiposDelSobre()
		{
			return typeof(EnvelopeAnswer).Assembly.GetTypes()
				.Where(tipo => tipo == typeof(VehicleProfile)
					|| tipo == typeof(WarrantyEnvelope)
					|| tipo.IsSubclassOf(typeof(WarrantyEnvelope))
					|| tipo == typeof(Delivery)
					|| tipo.IsSubclassOf(typeof(Delivery)))
				.ToList();
		}
	}
}
