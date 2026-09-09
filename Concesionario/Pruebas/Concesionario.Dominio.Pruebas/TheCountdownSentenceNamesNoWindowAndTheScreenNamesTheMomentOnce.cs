using System;
using System.Collections.Generic;
using System.IO;
using Concesionario.Wasm;
using Concesionario.Wasm.Parts;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LA PREGUNTA DE QUIEN NOMBRA EL MOMENTO, MEDIDA Y NO HEREDADA. La regla que traia el plan dice que
	// toda ventana de pantalla va nombrada en su encabezado. La rebanada anterior midio que su propia
	// frase del dominio nombraba sus dos limites en los DOS estados del registro, y decidio que el
	// encabezado nombra el bloque y la frase lleva el periodo. Esa decision es de ESA rebanada; lo que
	// transfiere es el METODO -- leer la frase compuesta en los dos estados del registro ANTES de
	// escribir el encabezado.
	//
	// Y AQUI DA DOS RESPUESTAS OPUESTAS EN UNA MISMA TAREA, que es lo que hace que valga la pena
	// medirlo:
	//
	//   LA CUENTA ATRAS no tiene ninguna ventana -- no es una lectura por periodo, es por instante -- y
	//   NINGUNA de sus frases nombra el instante en que se pregunto. Asi que la pantalla lo dice UNA
	//   vez, arriba de la lista, y ningun encabezado duplica nada.
	//
	//   LOS PRECIOS traen el instante ADENTRO de cada frase, porque la vigencia se juzga contra el. Asi
	//   que la pantalla no lo dice NUNCA en ese bloque.
	//
	//   EL HISTORIAL no depende de ningun instante y cada fila trae el suyo, asi que no hay ventana
	//   ninguna.
	[TestFixture]
	public sealed class TheCountdownSentenceNamesNoWindowAndTheScreenNamesTheMomentOnce
	{
		private const string TheVin = "KMHD35LE0EU000015";
		private const string TheUnitEverythingIsIn = "UnitA";

		private const string TheKindWithACountdown = "FluidChange";
		private const string TheKindThatIsNotOnTheSchedule = "Repair";

		private static readonly DateTime TheAnchor =
			new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayTheReadingWasObserved =
			new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheInstantTheScreenAsksAt =
			new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, AppState.TheProfileAtDelivery);
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// ===== LA MEDICION, EN LOS DOS ESTADOS DEL REGISTRO ===============================

		// NINGUNA FRASE DE LA CUENTA ATRAS NOMBRA UN PERIODO NI EL INSTANTE EN QUE SE PREGUNTO, ni con
		// la operacion medida ni con la que no esta agendada. Es la medicion que decide que la pantalla
		// tenga que decirlo ella, y una sola vez.
		[Test]
		public void NoCountdownSentenceNamesAPeriodOrTheInstantInEitherStateOfTheRecord()
		{
			AnOperationWithAReadingAfterIt();

			WhereEachOperationStands handed = WhatTheScreenIsHanded();

			foreach (HowOneOperationStands row in new[]
			{
				handed.Of(TheKindWithACountdown), handed.Of(TheKindThatIsNotOnTheSchedule),
			})
				foreach (string said in new[] { row.Says, row.DistanceSays, row.DaysSays })
				{
					Assert.That(said, Does.Not.Contain("2026-02-01"),
						"Una frase de la cuenta atras nombra el instante en que se pregunto. Si el "
						+ "dominio empieza a decirlo, la linea que la pantalla pone arriba de la lista "
						+ "se vuelve una duplicacion y hay que quitarla.");

					Assert.That(said, Does.Not.Contain("Entre el"),
						"Una frase de la cuenta atras nombra un periodo. No es una lectura por periodo.");
				}

			// Y CON LA OPERACION MEDIDA LA FRASE SI LLEVA LOS DOS MONTOS, que es lo que hace que la
			// afirmacion de arriba mida una ausencia y no una frase vacia.
			Assert.That(handed.Of(TheKindWithACountdown).Says, Does.Contain("te faltan 7345 km"));
			Assert.That(handed.Of(TheKindWithACountdown).Says, Does.Contain("te faltan 255 días"));

			// Y CON LA QUE NO ESTA AGENDADA, LA FRASE DICE ESO Y SE DICE UNA VEZ.
			Assert.That(
				handed.Of(TheKindThatIsNotOnTheSchedule).Says,
				Is.EqualTo("no hay ninguna operación de Reparación agendada para este vehículo"));
		}

		// EN CAMBIO LA FRASE DE UN PRECIO SI NOMBRA EL INSTANTE, y es la razon por la que ese bloque no
		// lo dice. La misma pregunta, dos respuestas, medidas y no argumentadas.
		[Test]
		public void AndEveryPriceSentenceDoesNameTheInstantWhichIsWhyThatBlockNeverSaysIt()
		{
			ASealedRevisionWithOnePrice();

			WhatTheServicesCost handed =
				WhatTheServicesCost.ReadThrough(journal.Api, TheInstantTheScreenAsksAt);

			Assert.That(handed.Rows, Is.Not.Empty);

			foreach (APublishedPriceOnTheSheet row in handed.Rows)
				Assert.That(row.Says, Does.Contain("2026-02-01"),
					"La frase de un precio dejo de nombrar el instante contra el que se juzgo su "
					+ "vigencia. Si eso pasa, el bloque de precios se queda sin decir a que momento "
					+ "contesta y hay que ponerlo.");
		}

		// ===== LO QUE LA PANTALLA HACE CON LAS DOS MEDICIONES =============================

		// LA PANTALLA NOMBRA EL MOMENTO UNA SOLA VEZ EN LA LISTA DE SERVICIOS, y NUNCA en el bloque de
		// precios. Las dos mitades se afirman juntas: es una misma decision tomada dos veces al reves.
		[Test]
		public void TheScreenNamesTheMomentOnceInTheServiceListAndNeverInThePricesBlock()
		{
			string services = TheSectionAsItIsWritten("Servicios.razor");
			string sheet = TheSectionAsItIsWritten("Vehiculo.razor");

			Assert.That(TimesItSays(services, "@Operations.AskedAt"), Is.EqualTo(1),
				"La lista de servicios nombra el momento en que se midio " + "una cantidad de veces "
				+ "distinta de UNA. Ninguna frase de la cuenta atras lo nombra, asi que la pantalla "
				+ "tiene que decirlo -- y una vez, no por fila.");

			Assert.That(sheet, Does.Not.Contain("AskedAt"),
				"El bloque de precios nombra el momento. Cada frase de un precio ya lo trae adentro: "
				+ "decirlo tambien en el bloque lo imprime dos veces en una columna.");

			// Y NO ESTA POR FILA. Nueve filas con el mismo instante son nueve veces la misma cosa.
			Assert.That(services, Does.Not.Contain("parada.AskedAt"));

			foreach (System.Reflection.PropertyInfo each in
				typeof(HowOneOperationStands).GetProperties())
				Assert.That(each.Name, Does.Not.Contain("AskedAt"),
					"Una fila de la cuenta atras gano el instante. Va en el portador, una vez.");
		}

		// ===== EL HALLAZGO QUE SALIO DE LA MEDICION, MEDIDO Y NO ARREGLADO ================

		// LA TABLA DE LA CARATULA Y LA PALABRA DEL DOMINIO NO DICEN LO MISMO PARA TODAS LAS CLASES, y
		// esta pantalla es la primera superficie del modulo que las pone adyacentes: el titulo de una
		// fila de servicios sale de la tabla, y la frase que va debajo -- cuando la clase no esta
		// agendada -- nombra la clase con la palabra del dominio.
		//
		// SE MIDE Y NO SE ARREGLA: arreglarlo es editar la tabla de la caratula o la palabra del
		// dominio, y ninguna de las dos esta en esta rebanada. Esta prueba es lo que se pone rojo el dia
		// que alguien alinee las dos, y entonces se lee y se borra.
		[Test]
		public void TheShellsWordForAKindAndTheDomainsWordDisagreeForExactlyTwoOfTheNine()
		{
			var disagreeing = new List<string>();

			foreach (string kind in AppState.TheNineKinds)
			{
				string said = journal.Api.DescribeCountdown(kind, TheInstantTheScreenAsksAt);

				string domain = said
					.Replace("no hay ninguna operación de ", "")
					.Replace(" agendada para este vehículo", "");

				if (domain != ComoSeLee.Operacion(kind)) disagreeing.Add(kind);
			}

			Assert.That(disagreeing, Is.EquivalentTo(new[] { "FluidChange", "WearPartReplacement" }),
				"La tabla de la caratula y la palabra del dominio dejaron de discrepar en las MISMAS dos "
				+ "clases. Si son menos, alguien las alineo y esta prueba ya no hace falta; si son mas, "
				+ "alguien agrego una tercera palabra para una misma cosa. En los dos casos hay que "
				+ "leerla y no volver a contarla.");

			// Y LA FORMA DE LA DISCREPANCIA, dicha: la caratula dice una y el dominio otra, y las dos
			// aparecen en la misma fila de la pantalla.
			Assert.That(ComoSeLee.Operacion("FluidChange"), Is.EqualTo("Cambio de líquidos"));
			Assert.That(
				journal.Api.DescribeCountdown("FluidChange", TheInstantTheScreenAsksAt),
				Does.Contain("Cambio de fluido"));
		}

		// ===== EL APAREJO =================================================================

		private WhereEachOperationStands WhatTheScreenIsHanded()
		{
			return WhereEachOperationStands.ReadThrough(journal.Api, TheInstantTheScreenAsksAt);
		}

		private void AnOperationWithAReadingAfterIt()
		{
			journal.Api.SeedPlanOperation(
				TheKindWithACountdown, "TimeOrDistance", 620, 21345, TheUnitEverythingIsIn,
				true, false, TheAnchor, 21000, TheUnitEverythingIsIn);

			journal.Api.RecordReading(
				35000, TheUnitEverythingIsIn, TheDayTheReadingWasObserved, "Telemetry");
		}

		private void ASealedRevisionWithOnePrice()
		{
			journal.Api.OpenRevision("MY2026-R1", new DateTime(2025, 3, 14, 10, 0, 0, DateTimeKind.Utc));

			journal.Api.PublishServicePrice(
				"MY2026-R1", "Inspection", 10000, "UnitA", 120.50m, "CRC",
				new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
				new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc));

			journal.Api.SealRevision("MY2026-R1");
		}

		private static int TimesItSays(string markup, string what)
		{
			int found = 0;
			int at = markup.IndexOf(what, StringComparison.Ordinal);

			while (at >= 0)
			{
				found++;
				at = markup.IndexOf(what, at + what.Length, StringComparison.Ordinal);
			}

			return found;
		}

		private static string TheSectionAsItIsWritten(string name)
		{
			string folder = Path.Combine(AppContext.BaseDirectory, "SeccionesComoDato");

			Assert.That(Directory.Exists(folder), Is.True, "Las secciones no llegaron al output.");

			foreach (string file in Directory.GetFiles(folder, "*.razor"))
				if (Path.GetFileName(file) == name) return File.ReadAllText(file);

			Assert.Fail("'" + name + "' no llego al output.");
			return "";
		}
	}
}
