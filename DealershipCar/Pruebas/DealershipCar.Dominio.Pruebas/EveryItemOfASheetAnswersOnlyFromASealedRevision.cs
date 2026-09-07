using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LA CORRIDA DE LA CARATULA DE CONSOLA ('ficha', T7), AFIRMADA COMO LEY. La caratula publica la
	// ficha ENTERA en una revision, la sella, abre una segunda que republica SOLO el aceite, la sella
	// y sustituye la primera -- e imprime las mismas respuestas en cada uno de esos cuatro momentos.
	// Lo que esta prueba defiende es exactamente lo que esa salida afirma.
	//
	// POR QUE UN BARRIDO POR LOS SIETE ITEMS Y NO LAS DOS QUE UNO SE ACUERDA DE ESCRIBIR. El filtro
	// "solo contesta una revision sellada" NO esta escrito una vez: cada item lo lleva en SU PROPIO
	// recorrido, y ARevisionAppliesForward tiene ademas una sobrecarga aparte para la lectura que
	// trae la mitad del vehiculo. Son TRECE llamadas iguales en trece lugares de ModelSpecification,
	// y la que se pierde es siempre la del recorrido que se escribio al final.
	//
	// MEDIDO, quitando ese filtro de una llamada por vez y corriendo la suite SIN esta prueba: de
	// dieciocho defectos, TRES dejaban las 786 verdes -- el recorrido del INTERVALO que lleva la
	// mitad del vehiculo, y los DOS del PRECIO. Los otros diez recorridos ya tenian quien los viera.
	//
	// Y LOS TRES SON LA PEOR MITAD DE SU ITEM. En el intervalo, la presencia y la procedencia pasan
	// por la OTRA sobrecarga, que si estaba cubierta: con el defecto puesto, la lectura de presencia
	// contesta "no publicado" y LA FRASE -- la que la persona lee -- cita el intervalo igual. En el
	// precio no hay otra mitad que avise: un monto de una revision sustituida sale con la revision
	// vieja adentro de la frase, que es exactamente lo que la hace parecer vigente.
	[TestFixture]
	public sealed class EveryItemOfASheetAnswersOnlyFromASealedRevision
	{
		private const string TheVin = "1HGCM82633A004352";

		private const string TheFirstRevision = "MY2019-R1";
		private const string TheSecondRevision = "MY2019-R2";

		private const string TheFluidKind = "EngineOil";
		private const string TheTyrePosition = "Front";
		private const string TheOperation = "FluidChange";
		private const string TheSystem = "POWERTRAIN";
		private const string UnitA = "UnitA";

		private const string TheFirstOilGrade = "5W-30 synthetic";
		private const string TheSecondOilGrade = "0W-20 synthetic";

		private const int TheMilestone = 10000;
		private const int TheDistanceRemaining = 2000;

		private const string TheOil = "aceite";

		private static readonly DateTime TheFirstPublication =
			new DateTime(2025, 8, 15, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime TheSecondPublication =
			new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime TheDayTheVehicleAsks =
			new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

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

		// EL PRIMER TABLERO DE LA CARATULA: la ficha entera esta en el disco y la revision sigue en
		// borrador, asi que NINGUNO de los siete items contesta. Es L12 barrida item por item, y es
		// donde se ven los tres recorridos que ninguna otra prueba alcanza.
		[Test]
		public void ADraftHoldingTheWholeSheetLeavesEveryOneOfItsItemsUnpublished()
		{
			TheWholeSheetIsPublishedIntoTheFirstRevision();

			EveryItemIsUnpublished();

			Assert.That(journal.Api.HasASealedRevision(), Is.False);
			Assert.That(journal.Api.ReadRevisionState(TheFirstRevision), Is.EqualTo("Draft"));
		}

		// EL SEGUNDO: sellada, los siete contestan Y CADA UNO NOMBRA LA REVISION -- la ley L1, que es
		// lo unico que separa esta hoja de un PDF. La procedencia va adentro de la frase y ademas en
		// la lectura aparte, porque un lector se puede saltar la segunda.
		[Test]
		public void OnceSealedEveryOneOfTheSevenItemsAnswersAndNamesTheRevision()
		{
			TheWholeSheetIsPublishedIntoTheFirstRevision();
			journal.Api.SealRevision(TheFirstRevision);

			EveryItemAnswersFrom(TheFirstRevision);

			Assert.That(journal.Api.ReadCurrentRevisionCode(), Is.EqualTo(TheFirstRevision));
			Assert.That(journal.Api.ReadMinimumFuelGrade(), Is.EqualTo("91 RON regular"));
			Assert.That(journal.Api.PermittedFuelGrades(), Is.EqualTo(new[] { "95 RON Premium" }));
			Assert.That(journal.Api.ProhibitedFuelGrades(), Is.EqualTo(new[] { "E85 Flex" }));
			Assert.That(journal.Api.CoveredSystems(), Is.EqualTo(new[] { TheSystem }));
			Assert.That(journal.Api.CoverageExclusions().Length, Is.EqualTo(1));
		}

		// EL TERCERO: sellada la segunda revision, que solo republico el aceite, SOLO EL ACEITE se
		// mueve. Corregir un item no borra los otros seis, y cada uno sigue diciendo de donde sale.
		[Test]
		public void ASecondSealedRevisionMovesOnlyTheItemItRepublished()
		{
			TheWholeSheetIsPublishedIntoTheFirstRevision();
			journal.Api.SealRevision(TheFirstRevision);
			TheSecondRevisionChangesTheOilGrade();

			foreach (KeyValuePair<string, Func<string>> each in TheRevisionReadOfEveryItem())
				Assert.That(
					each.Value(),
					Is.EqualTo(each.Key == TheOil ? TheSecondRevision : TheFirstRevision),
					each.Key);

			Assert.That(
				journal.Api.DescribeFluid(TheFluidKind), Does.Contain(TheSecondOilGrade));
			Assert.That(journal.Api.ReadCurrentRevisionCode(), Is.EqualTo(TheSecondRevision));
		}

		// EL CUARTO: sustituida la primera, se retira TODO lo que la sucesora no republico -- los seis
		// items y las cuatro listas. Medido despues del replay, porque la linea de publicacion se
		// vuelve a armar desde el disco y es ahi donde un estado que no sobreviviera se veria.
		[Test]
		public void SupersedingWithdrawsEveryItemTheSuccessorDidNotRepublish()
		{
			TheWholeSheetIsPublishedIntoTheFirstRevision();
			journal.Api.SealRevision(TheFirstRevision);
			TheSecondRevisionChangesTheOilGrade();
			journal.Api.SupersedeRevision(TheFirstRevision, TheSecondRevision);

			journal.CloseAndReopenFromTheJournal();

			foreach (KeyValuePair<string, Func<bool>> each in ThePresenceReadOfEveryItem())
				Assert.That(each.Value(), Is.EqualTo(each.Key == TheOil), each.Key);

			foreach (KeyValuePair<string, Func<string>> each in TheRevisionReadOfEveryItem())
				Assert.That(
					each.Value(), Is.EqualTo(each.Key == TheOil ? TheSecondRevision : ""), each.Key);

			foreach (KeyValuePair<string, Func<string>> each in TheSentenceOfEveryItem())
				Assert.That(
					each.Value(),
					each.Key == TheOil
						? Does.Contain("revisión " + TheSecondRevision)
						: Does.Contain("no se ha publicado nada"),
					each.Key);

			EveryListIsEmpty();

			Assert.That(journal.Api.ReadRevisionState(TheFirstRevision), Is.EqualTo("Superseded"));
			Assert.That(journal.Api.ReadRevisionState(TheSecondRevision), Is.EqualTo("Sealed"));
			Assert.That(journal.Api.ReadCurrentRevisionCode(), Is.EqualTo(TheSecondRevision));
		}

		private void EveryItemAnswersFrom(string revision)
		{
			foreach (KeyValuePair<string, Func<bool>> each in ThePresenceReadOfEveryItem())
				Assert.That(each.Value(), Is.True, each.Key);

			foreach (KeyValuePair<string, Func<string>> each in TheRevisionReadOfEveryItem())
				Assert.That(each.Value(), Is.EqualTo(revision), each.Key);

			foreach (KeyValuePair<string, Func<string>> each in TheSentenceOfEveryItem())
				Assert.That(each.Value(), Does.Contain("revisión " + revision), each.Key);
		}

		private void EveryItemIsUnpublished()
		{
			foreach (KeyValuePair<string, Func<bool>> each in ThePresenceReadOfEveryItem())
				Assert.That(each.Value(), Is.False, each.Key);

			foreach (KeyValuePair<string, Func<string>> each in TheRevisionReadOfEveryItem())
				Assert.That(each.Value(), Is.EqualTo(""), each.Key);

			foreach (KeyValuePair<string, Func<string>> each in TheSentenceOfEveryItem())
				Assert.That(each.Value(), Does.Contain("no se ha publicado nada"), each.Key);

			// Y LA FRASE NO DEJA ESCAPAR EL CONTENIDO por la puerta de atras: una ausencia que igual
			// dice el grado es la hoja citada por otro camino.
			Assert.That(journal.Api.DescribeFluid(TheFluidKind), Does.Not.Contain(TheFirstOilGrade));

			EveryListIsEmpty();
		}

		// LAS CUATRO LECTURAS DE LISTA Y EL GRADO MINIMO. Una frase que se escapa se lee; un arreglo
		// que llega con elementos donde tenia que llegar vacio no se lee, se usa.
		private void EveryListIsEmpty()
		{
			Assert.That(journal.Api.ReadMinimumFuelGrade(), Is.EqualTo(""));
			Assert.That(journal.Api.PermittedFuelGrades(), Is.Empty);
			Assert.That(journal.Api.ProhibitedFuelGrades(), Is.Empty);
			Assert.That(journal.Api.CoveredSystems(), Is.Empty);
			Assert.That(journal.Api.CoverageExclusions(), Is.Empty);
		}

		private Dictionary<string, Func<bool>> ThePresenceReadOfEveryItem()
		{
			return new Dictionary<string, Func<bool>>
			{
				{ TheOil, () => journal.Api.IsFluidPublished(TheFluidKind) },
				{ "llanta", () => journal.Api.IsTyrePublished(TheTyrePosition) },
				{ "combustible", () => journal.Api.IsFuelPublished() },
				{ "intervalo", () => journal.Api.IsServiceIntervalPublished(TheOperation) },
				{ "precio",
					() => journal.Api.IsServicePricePublished(TheOperation, TheMilestone, UnitA) },
				{ "garantia", () => journal.Api.IsCoveragePublished() },
				{ "regla de taller", () => journal.Api.IsServicingPolicyPublished() },
			};
		}

		private Dictionary<string, Func<string>> TheRevisionReadOfEveryItem()
		{
			return new Dictionary<string, Func<string>>
			{
				{ TheOil, () => journal.Api.ReadFluidRevision(TheFluidKind) },
				{ "llanta", () => journal.Api.ReadTyreRevision(TheTyrePosition) },
				{ "combustible", () => journal.Api.ReadFuelRevision() },
				{ "intervalo", () => journal.Api.ReadServiceIntervalRevision(TheOperation) },
				{ "precio",
					() => journal.Api.ReadServicePriceRevision(TheOperation, TheMilestone, UnitA) },
				{ "garantia", () => journal.Api.ReadCoverageRevision() },
				{ "regla de taller", () => journal.Api.ReadServicingPolicyRevision() },
			};
		}

		private Dictionary<string, Func<string>> TheSentenceOfEveryItem()
		{
			return new Dictionary<string, Func<string>>
			{
				{ TheOil, () => journal.Api.DescribeFluid(TheFluidKind) },
				{ "llanta", () => journal.Api.DescribeTyre(TheTyrePosition) },
				{ "combustible", () => journal.Api.DescribeFuel() },
				{ "intervalo",
					() => journal.Api.DescribeServiceInterval(
						TheOperation, TheDistanceRemaining, UnitA) },
				{ "precio",
					() => journal.Api.DescribeServicePrice(
						TheOperation, TheMilestone, UnitA, TheDayTheVehicleAsks) },
				{ "garantia", () => journal.Api.DescribeCoverageSummary() },
				{ "regla de taller", () => journal.Api.DescribeServicingElsewhereRule(2, 1, 0) },
			};
		}

		// La misma hoja que publica la caratula, en el mismo orden: el minimo NO va primero y los tres
		// grados llevan minusculas, para que una lectura que devolviera "el primer grado" o que le
		// pasara el texto por ToUpper no acierte por casualidad.
		private void TheWholeSheetIsPublishedIntoTheFirstRevision()
		{
			journal.Api.OpenRevision(TheFirstRevision, TheFirstPublication);

			journal.Api.PublishFluidSpecification(
				TheFirstRevision, TheFluidKind, TheFirstOilGrade, 4.2m, UnitA,
				TheMilestone, UnitA, 365);

			journal.Api.PublishTyreSpecification(
				TheFirstRevision, TheTyrePosition, "205/55R16", 2.4m, UnitA, "94V");

			journal.Api.PublishFuelGrade(TheFirstRevision, "95 RON Premium", "Permitted");
			journal.Api.PublishFuelGrade(TheFirstRevision, "91 RON regular", "Minimum");
			journal.Api.PublishFuelGrade(TheFirstRevision, "E85 Flex", "Prohibited");

			journal.Api.PublishServiceInterval(
				TheFirstRevision, TheOperation, TheMilestone, UnitA, 365);

			journal.Api.PublishServicePrice(
				TheFirstRevision, TheOperation, TheMilestone, UnitA, 89.90m, "USD",
				TheFirstPublication, new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc));

			journal.Api.PublishCoveredSystem(TheFirstRevision, TheSystem, 0, 0, UnitA);

			journal.Api.PublishCoverageExclusion(
				TheFirstRevision, TheSystem, "wear parts", "when the part is a consumable");

			journal.Api.PublishServicingPolicy(
				TheFirstRevision,
				"service may be done at any qualified workshop",
				"keep the invoices and the parts records");
		}

		private void TheSecondRevisionChangesTheOilGrade()
		{
			journal.Api.OpenRevision(TheSecondRevision, TheSecondPublication);

			journal.Api.PublishFluidSpecification(
				TheSecondRevision, TheFluidKind, TheSecondOilGrade, 4.2m, UnitA,
				TheMilestone, UnitA, 365);

			journal.Api.SealRevision(TheSecondRevision);
		}
	}
}
