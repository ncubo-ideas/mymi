using System;
using System.Reflection;
using DealershipCar.App;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// TEST 15 DEL BRIEF Y LA LEY L7 DEL SLICE DEL SOBRE, en la puerta. Es el hermano de
	// EachHalfStaysSeparatelyAnswerable, que ya defiende lo mismo para las dos mitades publicadas de
	// la especificacion: una lectura que empareja un hecho PUBLICADO con uno RECIBIDO tiene que
	// tener un hermano publicado, contestable por quien no tiene ni un dato de ningun vehiculo -- y
	// las dos en la superficie de lectura, no solo en el dominio.
	//
	// POR QUE ESTA LECTURA EXISTE. La frase compuesta del sobre es INDIVISIBLE: el dominio la
	// compone precisamente para que nadie rio abajo vuelva a derivar sus partes, y lleva adentro su
	// propia procedencia. Asi que un llamador sin la mitad personal no recibe una respuesta a medias
	// -- recibe NINGUNA, incluida la mitad publicada que tenia todo el derecho de mostrar. Que un
	// carro no tenga odometro no puede volver incitable lo que la agencia publico.
	//
	// LO QUE HACE QUE ESTO NO SEA UN Is.Not.Empty (C60). Se afirma la forma POR REFLEXION -- cero
	// parametros, asi que estructuralmente no puede pedir un hecho de nadie -- y se compara la frase
	// entre un registro que tiene TODOS los hechos del vehiculo y uno que no tiene NINGUNO. Si la
	// mitad publicada se dejara colar un hecho personal, las dos frases se separarian.
	[TestFixture]
	public sealed class ThePublishedHalfIsQuotableWithNoVehicleFactAtAll
	{
		private const string TheVin = "1HGCM82633A004352";
		private const string TheUnitTheLimitIsIn = "UnitB";

		private const int ThirtySixMonths = 36;
		private const int SixtyThousand = 60000;

		private static readonly DateTime TheDayItWasPublished =
			new DateTime(2023, 11, 15, 9, 30, 45, DateTimeKind.Utc);

		private static readonly DateTime TheInstantAsked =
			new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);

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

		// UN LLAMADOR QUE NO TIENE NI UN DATO DEL CARRO CITA EL SOBRE ENTERO: las dos mitades, su
		// unidad y cuando se publico. Sin entrega y sin ninguna lectura del odometro.
		[Test]
		public void ACallerHoldingNothingAboutTheVehicleStillGetsBothLimitsTheirUnitAndTheDate()
		{
			journal.Api.PublishWarrantyEnvelope(
				ThirtySixMonths, SixtyThousand, TheUnitTheLimitIsIn, TheDayItWasPublished);

			Assert.That(journal.Api.HasDeliveryBeenRecorded(), Is.False,
				"Sin entrega registrada.");
			Assert.That(journal.Api.HasAnOdometerReading(TheInstantAsked), Is.False,
				"Y sin ninguna lectura del odometro. Es el estado en que la frase compuesta no puede "
				+ "contestar, y es donde esta lectura tiene que contestar igual.");

			string published = journal.Api.DescribePublishedWarrantyEnvelope();

			Assert.That(published, Does.Contain("36 meses"));
			Assert.That(published, Does.Contain("60000 mi"),
				"Las dos mitades con su unidad. 'mi' es UnitB: si el codigo no hubiera cruzado, aca "
				+ "diria 'km', que es el primer miembro del conjunto.");
			Assert.That(published, Does.Contain("2023-11-15"),
				"Y cuando se publico, que es lo que hace que un sobre viejo se pueda ver viejo.");
		}

		// LA FORMA, POR REFLEXION: cero parametros. Es la mitad estructural -- una lectura que no
		// toma nada no puede pedirle un hecho a quien no lo tiene, con ningun nombre.
		[Test]
		public void ThePublishedHalfAsksForNothingAtAll()
		{
			MethodInfo published = typeof(DealershipCarAPI).GetMethod(
				"DescribePublishedWarrantyEnvelope",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

			Assert.That(published, Is.Not.Null);
			Assert.That(published.GetParameters(), Is.Empty,
				"El dia que esta lectura tome un argumento, dejo de ser la mitad publicada: pedirle "
				+ "el odometro a quien pregunta es la misma invencion con un contrato encima.");
		}

		// Y LA FRASE ES LA MISMA CON Y SIN LOS HECHOS DEL VEHICULO, palabra por palabra. Es lo que
		// separa "no los pide" de "no los usa": una mitad publicada que se dejara colar un hecho
		// personal daria dos frases distintas y las dos se verian bien.
		[Test]
		public void ThePublishedHalfSaysTheSameThingWhateverTheRecordHoldsAboutThisVehicle()
		{
			journal.Api.PublishWarrantyEnvelope(
				ThirtySixMonths, SixtyThousand, TheUnitTheLimitIsIn, TheDayItWasPublished);

			string withNoVehicleFactAtAll = journal.Api.DescribePublishedWarrantyEnvelope();

			journal.Api.RecordDelivery(
				new DateTime(2024, 1, 1, 8, 0, 0, DateTimeKind.Utc), 1200, TheUnitTheLimitIsIn);

			journal.Api.RecordReading(
				18450, TheUnitTheLimitIsIn,
				new DateTime(2026, 3, 10, 7, 15, 20, DateTimeKind.Utc), "Telemetry");

			Assert.That(journal.Api.HasDeliveryBeenRecorded(), Is.True);
			Assert.That(journal.Api.HasAnOdometerReading(TheInstantAsked), Is.True);

			Assert.That(
				journal.Api.DescribePublishedWarrantyEnvelope(),
				Is.EqualTo(withNoVehicleFactAtAll),
				"Los dos hechos del vehiculo entraron al registro y la mitad publicada no se movio. "
				+ "Si se moviera, seria porque compone algo personal adentro -- y entonces volveria a "
				+ "ser incitable el dia que a un carro le falte.");
		}

		// SIN SOBRE PUBLICADO TAMBIEN CONTESTA, con palabras y sin un solo digito. Que la agencia no
		// haya publicado ninguno tambien es una respuesta, y darla con un cero adentro se leeria como
		// un limite que alguien publico.
		[Test]
		public void AnUnpublishedEnvelopeIsSaidInWordsAndWithoutADigit()
		{
			string said = journal.Api.DescribePublishedWarrantyEnvelope();

			Assert.That(said, Does.Contain("no ha publicado"));
			Assert.That(said, Does.Not.Match(@"\d"));
		}
	}
}
