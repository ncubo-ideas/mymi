using System;
using System.Collections.Generic;
using System.Reflection;
using DealershipCar.App;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LA PARTE C DEL SLICE, DEL LADO DEL WIRE. Las cuatro operaciones de la puerta que dicen que
	// precios publico el fabricante: la presencia, la cuenta, el registro de filas y la frase de una
	// clave sin instante.
	//
	// NINGUNA FILA PIDE UN HITO A QUIEN PREGUNTA, y eso es la ley entera: un hito escrito a mano en
	// una caratula pone el numero del fabricante en una capa que no lo publica y no muestra NADA el
	// dia que una revision publique otro. Aca el juego de filas sale de la enumeracion.
	//
	// LA ASIMETRIA DE LAS CUATRO ES DELIBERADA Y NO SE ORDENA: la presencia y la cuenta NO toman el
	// instante que el registro de filas si toma, porque el JUEGO DE FILAS no depende del instante
	// -- sellada es un estado y no un instante -- y solo la frase se mueve. Cuatro firmas iguales
	// dirian lo contrario.
	//
	// TODO SE AFIRMA CONTRA LO QUE SE PUBLICO, POR LITERAL (E49 obligacion 2): el monto, la moneda, la
	// vigencia y el codigo de la revision que se le pasaron a PublishServicePrice. Comparar la puerta
	// contra el dominio compararia dos consumidores de un mismo resolvedor, que se ponen de acuerdo
	// por construccion incluso cuando el resolvedor esta uniformemente equivocado.
	[TestFixture]
	public sealed class ThePublishedPricesReachTheDoorAsParallelColumns
	{
		private const string TheVin = "WBA3A5C51DF000003";
		private const string TheFirstRevision = "REV-2025-01";
		private const string TheSecondRevision = "REV-2025-02";

		private static readonly DateTime PublishedAt =
			new DateTime(2025, 3, 14, 10, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime ValidFrom =
			new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime ValidUntil =
			new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime TheFluidPriceEndsOn =
			new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime WhileBothAreInForce =
			new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime AfterTheFluidPriceLapsed =
			new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime BeforeAnyValidityStarted =
			new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc);

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

		// ===== EL PRIMER ESTADO: NADA SELLADO ============================================

		// CUATRO COLUMNAS VACIAS Y PARALELAS, y NINGUNA frase de ausencia inventada. Que no haya nada
		// publicado se PREGUNTA -- HasAnyPublishedServicePrice -- y esa es la unica ausencia que esta
		// ficha tiene, a proposito.
		[Test]
		public void NothingSealedAnswersFourEmptyParallelColumnsAndNoAbsenceSentenceIsInvented()
		{
			Assert.That(journal.Api.HasAnyPublishedServicePrice(), Is.False);
			Assert.That(journal.Api.PublishedServicePriceCount(), Is.EqualTo(0));

			PublishedPriceRecord none = journal.Api.PublishedServicePricesAsOf(WhileBothAreInForce);

			Assert.That(none.Operation, Is.Empty);
			Assert.That(none.Milestone, Is.Empty);
			Assert.That(none.Says, Is.Empty);
			Assert.That(none.Revision, Is.Empty);

			foreach (MethodInfo each in TheDoorsMembers())
				Assert.That(
					each.Name,
					Is.Not.EqualTo("DescribePublishedPrices").And.Not.EqualTo("DescribeNoPublishedPrice"),
					"La puerta gano una frase de ausencia para los precios. Esta ficha tiene UNA sola "
					+ "clase de ausencia a proposito, y la pregunta ya la contesta.");
		}

		// Y UNA REVISION ABIERTA SIN SELLAR SIGUE SIN PUBLICAR NADA: sellada es lo que hace publicado.
		[Test]
		public void ADraftRevisionPublishesNothingYet()
		{
			journal.Api.OpenRevision(TheFirstRevision, PublishedAt);
			journal.Api.PublishServicePrice(
				TheFirstRevision, "Inspection", 10000, "UnitA", 120.50m, "CRC", ValidFrom, ValidUntil);

			Assert.That(journal.Api.HasAnyPublishedServicePrice(), Is.False);
			Assert.That(journal.Api.PublishedServicePricesAsOf(WhileBothAreInForce).Says, Is.Empty);
		}

		// ===== EL SEGUNDO ESTADO: DOS REVISIONES SELLADAS Y TRES CLAVES ==================

		[Test]
		public void EveryRowSaysWhatWasPublishedByLiteralAndNamesTheRevisionThatGoverns()
		{
			TwoSealedRevisions();

			PublishedPriceRecord rows =
				journal.Api.PublishedServicePricesAsOf(AfterTheFluidPriceLapsed);

			Assert.That(rows.Says, Has.Length.EqualTo(3));

			int inspection = Array.IndexOf(rows.Milestone, "10000 km");
			int fluid = Array.IndexOf(rows.Milestone, "20000 km");
			int repair = Array.IndexOf(rows.Milestone, "30000 mi");

			Assert.That(rows.Says[inspection], Is.EqualTo(
				"Revisión a los 10000 km: 145 CRC, vigente del 2025-01-01 al 2025-12-31. "
				+ "Al 2025-09-01 esa vigencia está rigiendo. "
				+ "Publicado en la revisión REV-2025-02."));
			Assert.That(rows.Revision[inspection], Is.EqualTo(TheSecondRevision));
			Assert.That(rows.Operation[inspection], Is.EqualTo("Inspection"));

			Assert.That(rows.Says[fluid], Is.EqualTo(
				"Cambio de fluido a los 20000 km: 340 CRC, vigente del 2025-01-01 al 2025-06-30. "
				+ "Al 2025-09-01 esa vigencia ya pasó, así que este precio no es el vigente. "
				+ "Publicado en la revisión REV-2025-01."));
			Assert.That(rows.Revision[fluid], Is.EqualTo(TheFirstRevision));
			Assert.That(rows.Operation[fluid], Is.EqualTo("FluidChange"));

			Assert.That(rows.Says[repair], Is.EqualTo(
				"Reparación a los 30000 mi: 900 USD, vigente del 2025-01-01 al 2025-12-31. "
				+ "Al 2025-09-01 esa vigencia está rigiendo. "
				+ "Publicado en la revisión REV-2025-02."));
			Assert.That(rows.Revision[repair], Is.EqualTo(TheSecondRevision));
			Assert.That(rows.Operation[repair], Is.EqualTo("Repair"));
		}

		// LA COLUMNA Operation TRAE EL NOMBRE DEL MIEMBRO -- la mitad direccionable -- y la frase lo
		// dice en palabras. Que la columna sea direccionable se comprueba USANDOLA como direccion.
		[Test]
		public void TheOperationColumnIsAddressableAndTheSentenceSaysItInWords()
		{
			TwoSealedRevisions();

			PublishedPriceRecord rows =
				journal.Api.PublishedServicePricesAsOf(AfterTheFluidPriceLapsed);

			foreach (string operation in rows.Operation)
				Assert.That(
					journal.Api.DescribePublishedServiceInterval(operation), Is.Not.Empty,
					"La columna no sirvio como direccion: la puerta no acepto ese nombre de vuelta.");

			Assert.That(rows.Says[Array.IndexOf(rows.Milestone, "10000 km")],
				Does.StartWith("Revisión a los "));
		}

		// EL HITO SE DICE Y NO SE DIRECCIONA, a proposito: una forma direccionable necesitaria la
		// distancia mas el CODIGO de la unidad, y ese codigo es una tercera copia del mapeo de dos
		// ramas que el modulo ya paga dos veces. Nadie lo necesita porque la enumeracion contesta.
		[Test]
		public void TheMilestoneColumnIsTheMilestoneSaidAndNotAnAddressableForm()
		{
			TwoSealedRevisions();

			PublishedPriceRecord rows =
				journal.Api.PublishedServicePricesAsOf(AfterTheFluidPriceLapsed);

			Assert.That(rows.Milestone, Is.EquivalentTo(new[] { "10000 km", "20000 km", "30000 mi" }));

			foreach (string each in rows.Milestone)
				Assert.That(each, Does.Not.Contain("UnitA").And.Not.Contain("UnitB"),
					"El hito lleva la unidad como la lee una persona, no como la nombra el wire.");
		}

		// ===== EL JUEGO DE FILAS NO SE MUEVE Y LA FRASE SI ===============================

		// Afirma LAS DOS MITADES: que el juego es identico en tres instantes Y que las frases no lo
		// son. Afirmar solo la primera pasaria con una lectura que ignorara el instante por completo.
		[Test]
		public void TheRowSetIsTheSameAtEveryInstantAndOnlyTheSentenceMoves()
		{
			TwoSealedRevisions();

			string[] whileInForce =
				journal.Api.PublishedServicePricesAsOf(WhileBothAreInForce).Milestone;
			string[] afterLapse =
				journal.Api.PublishedServicePricesAsOf(AfterTheFluidPriceLapsed).Milestone;
			string[] beforeAnything =
				journal.Api.PublishedServicePricesAsOf(BeforeAnyValidityStarted).Milestone;

			Assert.That(afterLapse, Is.EqualTo(whileInForce));
			Assert.That(beforeAnything, Is.EqualTo(whileInForce));

			Assert.That(
				journal.Api.PublishedServicePricesAsOf(AfterTheFluidPriceLapsed).Says,
				Is.Not.EqualTo(journal.Api.PublishedServicePricesAsOf(WhileBothAreInForce).Says),
				"Si las frases tampoco se movieran, la lectura estaria ignorando el instante.");

			Assert.That(
				journal.Api.PublishedServicePricesAsOf(BeforeAnyValidityStarted).Says,
				Has.All.Contains("todavía no ha empezado"));
		}

		// Y LA PRESENCIA Y LA CUENTA NO TOMAN EL INSTANTE QUE EL REGISTRO SI TOMA. La asimetria es la
		// afirmacion; ordenarla en cuatro firmas iguales borraria lo que dice.
		[Test]
		public void ThePresenceAndTheCountDoNotTakeTheInstantTheRowsTake()
		{
			Assert.That(TheDoorsMember("HasAnyPublishedServicePrice").GetParameters(), Is.Empty);
			Assert.That(TheDoorsMember("PublishedServicePriceCount").GetParameters(), Is.Empty);

			ParameterInfo[] rows = TheDoorsMember("PublishedServicePricesAsOf").GetParameters();

			Assert.That(rows, Has.Length.EqualTo(1));
			Assert.That(rows[0].ParameterType, Is.EqualTo(typeof(DateTime)));
			Assert.That(rows[0].Name, Is.EqualTo("asOf"));
		}

		// LA CUENTA Y LAS FILAS CONCUERDAN, y las dos salen de la misma enumeracion en cada lectura.
		[Test]
		public void TheCountAgreesWithTheRowsAtEveryInstant()
		{
			TwoSealedRevisions();

			Assert.That(journal.Api.PublishedServicePriceCount(), Is.EqualTo(3));

			foreach (DateTime asked in
				new[] { WhileBothAreInForce, AfterTheFluidPriceLapsed, BeforeAnyValidityStarted })
				Assert.That(
					journal.Api.PublishedServicePricesAsOf(asked).Says,
					Has.Length.EqualTo(journal.Api.PublishedServicePriceCount()));
		}

		// ===== LA HERMANA SIN INSTANTE ==================================================

		// Quien no tenga ningun instante que aportar igual puede citar lo que el fabricante publico.
		// LA AFIRMACION FUERTE ES LA AUSENCIA DE " Al ", que es como abre toda clausula con instante:
		// afirmar que no hay ninguna fecha seria falso, porque la vigencia trae dos y son suyas.
		[Test]
		public void ThePublishedHalfIsQuotableWithNoInstantAtAll()
		{
			TwoSealedRevisions();

			string said = journal.Api.DescribePublishedServicePrice("Inspection", 10000, "UnitA");

			Assert.That(said, Is.EqualTo(
				"Revisión a los 10000 km: 145 CRC, vigente del 2025-01-01 al 2025-12-31. "
				+ "Publicado en la revisión REV-2025-02."));

			Assert.That(said, Does.Not.Contain(" Al "));

			foreach (ParameterInfo each in
				TheDoorsMember("DescribePublishedServicePrice").GetParameters())
				Assert.That(each.ParameterType, Is.Not.EqualTo(typeof(DateTime)),
					"La hermana publicada gano un instante y entonces ya no es la mitad publicada.");
		}

		// Y NO SE INVENTA NADA CUANDO NO HAY NADA: un hito que nadie publico contesta la ausencia con
		// palabras y no una frase vacia.
		[Test]
		public void TheSiblingAnswersTheAbsenceInWordsForAMilestoneNobodyPublished()
		{
			TwoSealedRevisions();

			Assert.That(
				journal.Api.DescribePublishedServicePrice("Inspection", 99000, "UnitA"),
				Is.Not.Empty.And.Not.Contain("99000"));
		}

		// ===== EL APAREJO ===============================================================

		// DOS revisiones selladas y TRES claves: la segunda republica el precio de la revision y
		// publica uno nuevo, asi que la clave que rige no es la primera de la lista de ninguna de las
		// dos.
		private void TwoSealedRevisions()
		{
			journal.Api.OpenRevision(TheFirstRevision, PublishedAt);
			journal.Api.PublishServicePrice(
				TheFirstRevision, "Inspection", 10000, "UnitA", 120.50m, "CRC", ValidFrom, ValidUntil);
			journal.Api.PublishServicePrice(
				TheFirstRevision, "FluidChange", 20000, "UnitA", 340.00m, "CRC",
				ValidFrom, TheFluidPriceEndsOn);
			journal.Api.SealRevision(TheFirstRevision);

			journal.Api.OpenRevision(TheSecondRevision, PublishedAt.AddDays(30));
			journal.Api.PublishServicePrice(
				TheSecondRevision, "Inspection", 10000, "UnitA", 145.00m, "CRC", ValidFrom, ValidUntil);
			journal.Api.PublishServicePrice(
				TheSecondRevision, "Repair", 30000, "UnitB", 900.00m, "USD", ValidFrom, ValidUntil);
			journal.Api.SealRevision(TheSecondRevision);
		}

		private static MethodInfo[] TheDoorsMembers()
		{
			return typeof(DealershipCarAPI).GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
		}

		private static MethodInfo TheDoorsMember(string name)
		{
			var found = new List<MethodInfo>();

			foreach (MethodInfo each in TheDoorsMembers())
				if (each.Name == name) found.Add(each);

			Assert.That(found, Has.Count.EqualTo(1),
				$"La puerta tiene {found.Count} miembros llamados '{name}'.");

			return found[0];
		}
	}
}
