using System;
using System.Reflection;
using Choreography.Theater;
using Concesionario.App;
using NUnit.Framework;
using Puppeteer;

namespace Concesionario.Dominio.Pruebas
{
	// EL INSTANTE MAS TEMPRANO QUE EXISTE, 0001-01-01, por el borde de la puerta y por el journal.
	//
	// POR QUE ESTO SE MIDIO EN SH-0 Y POR QUE SE QUEDA. El plan lo condicionaba a que las ventanas de
	// P-11 necesitaran un limite inferior abierto, y no lo necesitan: las dos ventanas de pantalla
	// son los doce meses que terminan en el instante que la pantalla pregunta y los doce anteriores,
	// las dos cerradas. PERO P-3 le da a cada tool del asistente dos limites que "en blanco quieren
	// decir el registro entero", y el registro entero ES un limite inferior abierto. Alguien tiene
	// que decidir con que instante se dice "desde siempre".
	//
	// Y LAS DOS PREGUNTAS SE SEPARAN AQUI: 0001-01-01 CRUZA todo -- normalizacion, parametro,
	// journal, replay -- y sigue SIN SER una respuesta legal, porque la lectura guardada revienta en
	// vez de contestarlo. Las dos mitades quedan pinchadas juntas a proposito: son la misma decision
	// vista desde los dos lados, y separarlas dejaria que alguien leyera la primera como permiso
	// para la segunda.
	//
	// SE MIDE InUtc POR REFLEXION porque es private static y la puerta no lo expone -- el mismo
	// camino por el que las pruebas ya llegan a la constante privada de la puerta.
	[TestFixture]
	public sealed class TheEarliestInstantCrossesTheDoorAndTheJournal
	{
		private const string TheVin = "1HGCM82633A004352";
		private const string TheModel = "YARIS-2019";
		private const string TheRevision = "R0";

		private string folder;

		[SetUp]
		public void Setup()
		{
			folder = AFreshJournalFolder.Make();
		}

		[TearDown]
		public void TearDown()
		{
			AFreshJournalFolder.Sweep(folder);
		}

		// LO QUE EL BORDE LE HACE AL INSTANTE MAS TEMPRANO, EN LAS TRES CLASES DE Kind. InUtc solo
		// convierte cuando el Kind es Local; en Utc y en Unspecified pasa derecho y solo trunca.
		[Test]
		public void TheDoorsNormalizationLeavesTheEarliestInstantWhereItIs()
		{
			Assert.That(InUtcOf(DateTime.MinValue), Is.EqualTo(DateTime.MinValue),
				"Unspecified: pasa derecho. MinValue tiene cero ticks, asi que el truncado a "
				+ "segundo entero no le quita nada.");

			Assert.That(
				InUtcOf(new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
				Is.EqualTo(DateTime.MinValue),
				"Utc: tampoco lo mueve.");

			Assert.That(InUtcOf(DateTime.MinValue).Kind, Is.EqualTo(DateTimeKind.Utc),
				"Y sale marcado Utc, como todo lo que cruza este borde.");
		}

		// LA MITAD QUE NO SE PUEDE RAZONAR: Kind=Local sobre el instante mas temprano llama a
		// ToUniversalTime(), que en una zona al oeste de Greenwich SUMA horas y en una al este
		// RESTARIA por debajo de MinValue. Lo que .NET hace ahi se mide, no se supone.
		[Test]
		public void ALocalEarliestInstantIsWhatTheFrameworkSaysItIsAndTheProbeRecordsIt()
		{
			DateTime crossed = InUtcOf(new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Local));

			Assert.That(crossed, Is.GreaterThanOrEqualTo(DateTime.MinValue),
				"No se desborda por abajo: el framework satura en vez de reventar.");
			Assert.That(crossed.Kind, Is.EqualTo(DateTimeKind.Utc));

			// Lo que se mide de verdad no es un rango sino el numero, y queda escrito en
			// SPIKES-BuildOrder.md porque depende de la zona de la maquina que corre la suite.
			TestContext.Out.WriteLine(
				"MEDIDO: InUtc(MinValue con Kind=Local) = " + crossed.ToString("O")
				+ " en la zona " + TimeZoneInfo.Local.Id);
		}

		// Y AHORA EL JOURNAL: el instante mas temprano como argumento de un COMANDO de verdad,
		// escrito, cerrado, reabierto y leido. Esta es la mitad que decide si "desde siempre" se
		// puede decir con 0001-01-01 o hay que decirlo de otra forma.
		[Test]
		public void TheEarliestInstantSurvivesTheParameterBoundaryAndTheReplay()
		{
			using (AVehicle vehicle = new AVehicle(folder))
			{
				vehicle.Api.OpenRevision(TheRevision, DateTime.MinValue);
				vehicle.Api.PublishFuelGrade(TheRevision, "91 RON", "Minimum");
				vehicle.Api.SealRevision(TheRevision);

				Assert.That(
					vehicle.Api.ReadCurrentRevisionPublishedAt(), Is.EqualTo(DateTime.MinValue),
					"Vivo: el instante mas temprano cruzo la frontera del parametro sin que nadie "
					+ "lo cambiara por un centinela.");
			}

			TestContext.Out.WriteLine("=== COMO EL JOURNAL ESCRIBIO 0001-01-01 ===");
			TestContext.Out.WriteLine(TheTextOfTheJournal());

			using (AVehicle reopened = new AVehicle(folder))
			{
				Assert.That(reopened.Api.HasASealedRevision(), Is.True,
					"El replay no se cayo al volver a ejecutar el comando con 0001-01-01.");

				Assert.That(
					reopened.Api.ReadCurrentRevisionPublishedAt(), Is.EqualTo(DateTime.MinValue),
					"Y despues de reabrir es el MISMO instante. Si el journal lo escribiera en un "
					+ "formato que no sabe decir el ano 1, aca vendria otra cosa o el arranque "
					+ "habria dejado un fallo de rehidratacion.");
			}
		}

		// Y LO QUE LA PUERTA YA DECIDIO SOBRE ESTE INSTANTE, que es distinto de si cruza. El ///
		// de ReadCurrentRevisionPublishedAt dice que un DateTime.MinValue ahi seria "un centinela
		// magico -- un instante que parece un instante y no lo es". Cruzar y ser legal como
		// RESPUESTA son dos preguntas, y esta prueba deja escrito que la segunda ya esta contestada
		// en contra.
		[Test]
		public void AnAbsentInstantIsStillARefusalAndNotTheEarliestInstant()
		{
			using (AVehicle vehicle = new AVehicle(folder))
			{
				Assert.That(vehicle.Api.HasASealedRevision(), Is.False);

				Assert.Throws<InvalidOperationException>(
					() => vehicle.Api.ReadCurrentRevisionPublishedAt(),
					"Sin revision sellada la puerta revienta en vez de contestar MinValue. Que "
					+ "0001-01-01 SOBREVIVA el viaje no lo vuelve una respuesta legal.");
			}
		}

		// Compartido a proposito: el motor no suelta el archivo mientras el proceso vive.
		private string TheTextOfTheJournal()
		{
			System.Text.StringBuilder everything = new System.Text.StringBuilder();

			foreach (string file in System.IO.Directory.GetFiles(
				folder, "*", System.IO.SearchOption.AllDirectories))
			{
				using (System.IO.FileStream open = new System.IO.FileStream(
					file, System.IO.FileMode.Open, System.IO.FileAccess.Read,
					System.IO.FileShare.ReadWrite | System.IO.FileShare.Delete))
				using (System.IO.StreamReader reader = new System.IO.StreamReader(open))
				{
					everything.AppendLine(reader.ReadToEnd());
				}
			}

			return everything.ToString();
		}

		private static DateTime InUtcOf(DateTime instant)
		{
			MethodInfo normalization = typeof(DealershipCarAPI).GetMethod(
				"InUtc", BindingFlags.NonPublic | BindingFlags.Static);

			Assert.That(normalization, Is.Not.Null,
				"InUtc sigue siendo el borde de la puerta. Si se renombro, esta sonda mide otra "
				+ "cosa y hay que volver a apuntarla antes de creerle.");

			return (DateTime)normalization.Invoke(null, new object[] { instant });
		}

		private sealed class AVehicle : IDisposable
		{
			private readonly DealershipCarApp app;

			internal AVehicle(string folder)
			{
				app = new DealershipCarApp(TheVin, "Standard", TheModel);
				app.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
				app.Start();

				Api = new DealershipCarAPI(app);
			}

			internal DealershipCarAPI Api { get; private set; }

			public void Dispose()
			{
				app.Dispose();
			}
		}
	}
}
