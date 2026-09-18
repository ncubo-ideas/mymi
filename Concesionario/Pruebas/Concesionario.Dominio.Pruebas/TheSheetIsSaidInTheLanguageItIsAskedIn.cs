using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LAS SIETE RESPUESTAS DE LA HOJA, DICHAS POR VALOR EN LOS DOS IDIOMAS. Los items que el dominio
	// dejo de redactar en esta vuelta: el fluido, la llanta, el combustible, el intervalo en sus dos
	// mitades y la regla de otro taller en las suyas.
	//
	// POR QUE NO ALCANZA CON TheHelperOfThePureSheetComposesExactlyWhatTheDoorComposes. Ese compara
	// DOS RUTAS, y las dos pasan por el mismo renderizador: una clave que el catalogo no tiene se
	// dice "{clave}" y un hueco con el nombre cambiado se queda "{hueco}" -- por las dos rutas
	// igual. Las dos coinciden y las dos estan mal, que es E49 dicho sobre un catalogo. Lo unico que
	// lo ve es afirmar la frase.
	//
	// Y TAMPOCO ALCANZA CON LA PARIDAD DE CLAVES. TheTwoScreenCataloguesCarryExactlyTheSameKeys ve
	// una clave que falta en un catalogo; no ve una clave que sobra en los dos ni un hueco mal
	// escrito, porque un hueco no es una clave.
	//
	// EL PRECIO NO ESTA AQUI. Su clausula todavia la redacta el dominio en espanol y se cita tal
	// cual: la moneda es su propio turno.
	[TestFixture]
	public sealed class TheSheetIsSaidInTheLanguageItIsAskedIn
	{
		private const string TheVin = "1HGCM82633A004352";
		private const string TheRevision = "R1";
		private const string TheOperation = "FluidChange";
		private const string TheRule = "service may be done at any qualified workshop";
		private const string WhatPreservesCover = "keep the invoices and the parts records";

		private const int TenThousandUnits = 10000;
		private const int TheDistanceRemaining = 8400;

		private static readonly DateTime Published =
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

		[Test]
		public void TheSevenItemsAreSaidInTheLanguageOfTheRecord()
		{
			ASealedSheet();

			Assert.Multiple(() =>
			{
				Assert.That(journal.Api.DescribeFluid("EngineOil", TheLanguage.OfTheRecord),
					Is.EqualTo(
						"Aceite de motor: grado 5W-30, capacidad 4,2 L, se cambia cada 10.000 km o "
						+ "365 días. Publicado en la revisión R1."));

				Assert.That(journal.Api.DescribeTyre("Front", TheLanguage.OfTheRecord),
					Is.EqualTo(
						"Llanta delantera: medida 205/55R16, presión en frío 2,4 bar, clasificaciones "
						+ "94V. Publicado en la revisión R1."));

				Assert.That(journal.Api.DescribeFuel(TheLanguage.OfTheRecord),
					Is.EqualTo(
						"Grados de combustible: 95 RON (mínimo), E85 (prohibido). Publicado en la "
						+ "revisión R1."));

				Assert.That(
					journal.Api.DescribePublishedServiceInterval(
						TheOperation, TheLanguage.OfTheRecord),
					Is.EqualTo(
						"Cambio de fluido: se hace cada 10.000 km o 365 días. Publicado en la "
						+ "revisión R1."));

				Assert.That(
					journal.Api.DescribeServiceInterval(
						TheOperation, TheDistanceRemaining, "UnitA", TheLanguage.OfTheRecord),
					Is.EqualTo(
						"Cambio de fluido: se hace cada 10.000 km o 365 días. A este vehículo le "
						+ "quedan 8.400 km contra él. Publicado en la revisión R1."));

				Assert.That(
					journal.Api.DescribePublishedServicingElsewhereRule(TheLanguage.OfTheRecord),
					Is.EqualTo(
						"Servicio en otro taller: \"" + TheRule + "\" Qué conserva la cobertura: \""
						+ WhatPreservesCover + "\" Publicado en la revisión R1."));

				Assert.That(
					journal.Api.DescribeServicingElsewhereRule(2, 1, 0, TheLanguage.OfTheRecord),
					Is.EqualTo(
						"Servicio en otro taller: \"" + TheRule + "\" Qué conserva la cobertura: \""
						+ WhatPreservesCover + "\" Este vehículo: servicios externos con comprobante "
						+ "2, servicios externos sin comprobante 1, mantenimientos no hechos 0. "
						+ "Publicado en la revisión R1."));
			});
		}

		[Test]
		public void TheSevenItemsAreSaidInTheOtherLanguageToo()
		{
			ASealedSheet();

			Assert.Multiple(() =>
			{
				Assert.That(journal.Api.DescribeFluid("EngineOil", TheLanguage.OfAnEnglishReader),
					Is.EqualTo(
						"Engine oil: grade 5W-30, capacity 4.2 L, changed every 10,000 km or 365 "
						+ "days. Published in revision R1."));

				Assert.That(journal.Api.DescribeTyre("Front", TheLanguage.OfAnEnglishReader),
					Is.EqualTo(
						"Tyre, front: size 205/55R16, cold pressure 2.4 bar, ratings 94V. Published "
						+ "in revision R1."));

				Assert.That(journal.Api.DescribeFuel(TheLanguage.OfAnEnglishReader),
					Is.EqualTo(
						"Fuel grades: 95 RON (minimum), E85 (prohibited). Published in revision R1."));

				Assert.That(
					journal.Api.DescribePublishedServiceInterval(
						TheOperation, TheLanguage.OfAnEnglishReader),
					Is.EqualTo(
						"Fluid change: done every 10,000 km or 365 days. Published in revision R1."));

				Assert.That(
					journal.Api.DescribeServiceInterval(
						TheOperation, TheDistanceRemaining, "UnitA", TheLanguage.OfAnEnglishReader),
					Is.EqualTo(
						"Fluid change: done every 10,000 km or 365 days. This vehicle has 8,400 km "
						+ "left against it. Published in revision R1."));

				Assert.That(
					journal.Api.DescribePublishedServicingElsewhereRule(
						TheLanguage.OfAnEnglishReader),
					Is.EqualTo(
						"Servicing elsewhere: \"" + TheRule + "\" What preserves cover: \""
						+ WhatPreservesCover + "\" Published in revision R1."));

				Assert.That(
					journal.Api.DescribeServicingElsewhereRule(
						2, 1, 0, TheLanguage.OfAnEnglishReader),
					Is.EqualTo(
						"Servicing elsewhere: \"" + TheRule + "\" What preserves cover: \""
						+ WhatPreservesCover + "\" This vehicle: external services with evidence 2, "
						+ "external services without evidence 1, missed maintenance 0. Published in "
						+ "revision R1."));
			});
		}

		// LA MITAD QUE NADIE PUBLICO, que es la que nombra el sujeto. Es la unica parte del item que
		// el marco de la ausencia dice, asi que un sujeto mal cableado solo se ve aqui: con algo
		// publicado el sujeto no aparece en ninguna frase.
		[Test]
		public void AnUnpublishedItemNamesItsSubjectInBothLanguages()
		{
			Assert.That(journal.Api.HasASealedRevision(), Is.False);

			Assert.Multiple(() =>
			{
				Assert.That(journal.Api.DescribeFluid("EngineOil", TheLanguage.OfTheRecord),
					Does.StartWith("Aceite de motor: no se ha publicado nada"));
				Assert.That(journal.Api.DescribeFluid("EngineOil", TheLanguage.OfAnEnglishReader),
					Does.StartWith("Engine oil: nothing has been published"));

				Assert.That(journal.Api.DescribeTyre("Spare", TheLanguage.OfTheRecord),
					Does.StartWith("Llanta de repuesto: no se ha publicado nada"));
				Assert.That(journal.Api.DescribeTyre("Spare", TheLanguage.OfAnEnglishReader),
					Does.StartWith("Tyre, spare: nothing has been published"));

				Assert.That(journal.Api.DescribeFuel(TheLanguage.OfTheRecord),
					Does.StartWith("Grados de combustible: no se ha publicado nada"));
				Assert.That(journal.Api.DescribeFuel(TheLanguage.OfAnEnglishReader),
					Does.StartWith("Fuel grades: nothing has been published"));

				Assert.That(
					journal.Api.DescribePublishedServiceInterval(
						TheOperation, TheLanguage.OfTheRecord),
					Does.StartWith(
						"Intervalo de servicio de cambio de fluido: no se ha publicado nada"));
				Assert.That(
					journal.Api.DescribePublishedServiceInterval(
						TheOperation, TheLanguage.OfAnEnglishReader),
					Does.StartWith("Service interval for fluid change: nothing has been published"));

				Assert.That(
					journal.Api.DescribePublishedServicingElsewhereRule(TheLanguage.OfTheRecord),
					Does.StartWith(
						"La regla de servicio en otro taller: no se ha publicado nada"));
				Assert.That(
					journal.Api.DescribePublishedServicingElsewhereRule(
						TheLanguage.OfAnEnglishReader),
					Does.StartWith(
						"The rule on servicing elsewhere: nothing has been published"));
			});
		}

		// NINGUNA FRASE SE QUEDA CON UN HUECO SIN LLENAR NI CON UNA CLAVE CRUDA. Es el barrido que
		// hace que las dos pruebas de arriba no dependan de que a alguien se le ocurriera el caso:
		// Say() devuelve "{clave}" cuando no la tiene y deja "{hueco}" cuando el nombre no calza, y
		// las dos cosas se ven con una llave en la frase final.
		[Test]
		public void NoSheetSentenceComesBackWithAHoleOrARawKeyInIt()
		{
			ASealedSheet();

			foreach (string language in new[]
				{ TheLanguage.OfTheRecord, TheLanguage.OfAnEnglishReader })
				foreach (string said in EverySheetSentence(language))
					Assert.That(said, Does.Not.Contain("{").And.Not.Contain("}"),
						"Una llave en la frase final es una clave que el catalogo no tiene o un "
						+ "hueco cuyo nombre no calza con el que quien compone le pasa. En " + language
						+ ": " + said);
		}

		private string[] EverySheetSentence(string language)
		{
			return new[]
			{
				journal.Api.DescribeFluid("EngineOil", language),
				journal.Api.DescribeFluid("Coolant", language),
				journal.Api.DescribeTyre("Front", language),
				journal.Api.DescribeTyre("Spare", language),
				journal.Api.DescribeFuel(language),
				journal.Api.DescribePublishedServiceInterval(TheOperation, language),
				journal.Api.DescribePublishedServiceInterval("Inspection", language),
				journal.Api.DescribeServiceInterval(
					TheOperation, TheDistanceRemaining, "UnitA", language),
				journal.Api.DescribeServiceInterval(
					TheOperation, TheDistanceRemaining, "UnitB", language),
				journal.Api.DescribeServiceInterval("TimedComponent", 200, "UnitA", language),
				journal.Api.DescribePublishedServicingElsewhereRule(language),
				journal.Api.DescribeServicingElsewhereRule(2, 1, 0, language),
			};
		}

		private void ASealedSheet()
		{
			journal.Api.OpenRevision(TheRevision, Published);

			journal.Api.PublishFluidSpecification(
				TheRevision, "EngineOil", "5W-30", 4.2m, "UnitA", TenThousandUnits, "UnitA", 365);

			journal.Api.PublishTyreSpecification(
				TheRevision, "Front", "205/55R16", 2.4m, "UnitA", "94V");

			journal.Api.PublishFuelGrade(TheRevision, "95 RON", "Minimum");
			journal.Api.PublishFuelGrade(TheRevision, "E85", "Prohibited");

			journal.Api.PublishServiceInterval(
				TheRevision, TheOperation, TenThousandUnits, "UnitA", 365);

			// Un intervalo de una sola mitad, para que el barrido de huecos recorra las tres formas
			// del intervalo publicado y las tres del remanente.
			journal.Api.PublishServiceInterval(TheRevision, "TimedComponent", 0, "UnitA", 1825);

			journal.Api.PublishServicingPolicy(TheRevision, TheRule, WhatPreservesCover);

			journal.Api.SealRevision(TheRevision);
		}
	}
}
