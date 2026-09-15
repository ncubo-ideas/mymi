using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LEY L10 de SLICE-CostLedger.md, AHORA ENTERA: "ninguna entrada del libro se deriva de un precio
	// publicado, y ningun precio publicado lo corrige una entrada del libro. Los dos se leen uno al
	// lado del otro; ninguno es la fuente del otro."
	//
	// ESTE ARCHIVO ERA ThePriceStaysAPrice Y CL-2 LE PUSO ESE NOMBRE A PROPOSITO. La mitad del precio
	// se podia afirmar y la otra no: no existia ningun tipo del libro contra el que comparar, asi que
	// nombrarlo con la ley entera habria sido el defecto que OD-6 §3 midio -- una prueba nombrada por
	// una ley que solo afirma la mitad. CL-2 dejo la deuda escrita y un CABLE TRAMPA que se ponia rojo
	// el dia que apareciera el primer tipo del libro.
	//
	// CL-3 LA PAGA, Y EL CABLE TRAMPA CUMPLIO SU TRABAJO: se puso rojo en la entrada, con su propio
	// mensaje diciendo que ese rojo era la deuda venciendo. Lo que ocupaba su sitio ahora es la
	// afirmacion que el cable pedia -- un precio publicado y una entrada del libro PARA LA MISMA
	// OPERACION, distintos, los dos legibles, y ninguno derivado del otro -- y el nombre de la clase
	// es el que §8 del plan pidio desde el principio.
	//
	// LA LEY SE AFIRMA POR TRES CAMINOS Y NINGUNO SOBRA:
	//
	//   POR REFLEXION, LADO DEL PRECIO -- ningun miembro de ServicePriceEntry ni de Money nombra,
	//   recibe ni contesta nada del libro. Es la mitad que CL-2 dejo puesta, sin tocar.
	//
	//   POR REFLEXION, LADO DEL LIBRO -- y es nueva: ningun tipo del libro nombra, recibe ni contesta
	//   nada del precio. Sin esta mitad, un ACostEntry que tomara un ServicePriceEntry pasaria el
	//   censo del precio entero, porque el precio no lo sabria.
	//
	//   POR VALOR -- los dos hechos existen a la vez para la misma operacion, dicen cifras distintas,
	//   los dos se leen, y CAMBIAR UNO NO MUEVE AL OTRO. Es la unica de las tres que ve una
	//   derivacion escrita adentro de un cuerpo: dos tipos sin miembros compartidos pueden estar
	//   igualmente fusionados por una linea que copie una cifra.
	//
	// Y §4 DEL BRIEF TIENE UNA LISTA DE OCHO SUSTANTIVOS PROHIBIDOS PARA UNA ENTRADA DEL LIBRO --
	// price, quote, estimate, invoice, payment, charge, transaction, expense -- "cada uno nombra algo
	// que este modulo o no tiene o mantiene aparte a proposito". Se afirma aca, que es donde la
	// separacion vive.
	[TestFixture]
	public sealed class ThePriceStaysAPriceAndTheLedgerStaysALedger
	{
		// §2.2: un costo NO es un precio. Es la prohibicion de DEC-48 y DEC-36 en un tercer sitio --
		// dos hechos de forma distinta, de autores distintos, con vidas distintas. Un precio que
		// llevara un costo adentro se habria vuelto un reporte de variacion contra lo cotizado, y este
		// modulo no tiene tal cosa.
		private static readonly string[] TheWordsOfTheLedger =
			{ "Cost", "Ledger", "Spend", "Spent", "Amortiz", "Incurred" };

		// Los doce nombres del canon de §4 del brief. CL-3 los escribe; ninguno puede aparecer en el
		// lado del precio.
		private static readonly string[] TheTypesTheLedgerBrought =
			{
				"CostLedger", "ACostEntry", "ARecurringCost", "ADurableCost", "AnExpectedLife",
				"ASpendSummary", "AnAmortizedShare", "ASpendComparison", "ACostPerDistance",
				"ASpendNotSaidAcrossCurrencies", "NoCostOnRecord", "CostSubject"
			};

		// Y los del lado del precio, que es la lista simetrica: ningun tipo del libro puede nombrar
		// ninguno de estos, ni la palabra Price ni Quote ni una revision.
		private static readonly string[] TheWordsOfThePrice =
			{
				"Price", "Quote", "Estimate", "Invoice", "Payment", "Charge", "Transaction",
				"Expense", "Milestone", "Revision", "Published", "Specification"
			};

		// Los tipos del libro que este censo recorre, enumerados por reflexion sobre la base y el
		// agregado en vez de escritos a mano: un tipo nuevo entra al censo solo.
		//
		// Y "SOLO" TIENE UN LIMITE QUE ES C57: la enumeracion es por BASE, asi que un tipo de respuesta
		// que no derive de ninguna de las tres bases no entra -- ASpendComparison esta nombrado a mano
		// justo por eso. BuildOrder/CL-6 agrego una CUARTA base, ACostPerDistance, y la agrego aca en
		// el mismo commit: el censo se quedaba verde sin ver los tres tipos nuevos, que es lo que un
		// barrido incompleto siempre hace.
		private static IEnumerable<Type> TheTypesOfTheLedger()
		{
			List<Type> found = new List<Type> { typeof(CostLedger) };

			foreach (Type declared in typeof(CostLedger).Assembly.GetTypes())
				if (typeof(ACostEntry).IsAssignableFrom(declared)
					|| typeof(SpendInAPeriod).IsAssignableFrom(declared)
					|| typeof(ACostPerDistance).IsAssignableFrom(declared)
					|| declared == typeof(ASpendSummary)
					|| declared == typeof(ASpendComparison)
					|| declared == typeof(AnAmortizedShare)
					|| declared == typeof(AnExpectedLife)
					|| declared == typeof(CostSubject)
					|| declared == typeof(CostClassification))
					found.Add(declared);

			return found;
		}

		[TestCase(typeof(ServicePriceEntry))]
		[TestCase(typeof(Money))]
		public void NoMemberOfThePriceSideKnowsAnythingAboutACost(Type subject)
		{
			List<string> read = new List<string>();

			foreach (MemberInfo member in subject.GetMembers(
				BindingFlags.Instance | BindingFlags.Static
				| BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
			{
				read.Add(member.Name);

				foreach (string word in TheWordsOfTheLedger)
					Assert.That(member.Name, Does.Not.Contain(word),
						"El miembro " + subject.Name + "." + member.Name + " nombra el libro de "
						+ "costos. Un precio es lo que el fabricante PUBLICO y una entrada del libro "
						+ "es lo que de verdad se gasto: dos hechos, y ninguno es la fuente del otro.");
			}

			TestContext.Out.WriteLine(
				"LOS MIEMBROS DE " + subject.Name + " (" + read.Count + ") -- "
				+ string.Join(", ", read));
		}

		// Y NI RECIBEN NI CONTESTAN NINGUNO. El nombre del miembro no alcanza: un colaborador del
		// libro entrando por una ranura llamada 'other' pasaria la prueba de arriba entera.
		[TestCase(typeof(ServicePriceEntry))]
		[TestCase(typeof(Money))]
		public void AndNoMemberOfThePriceSideTakesOrAnswersOneEither(Type subject)
		{
			foreach (MethodBase member in TheMethodsAndConstructorsOf(subject))
			{
				foreach (ParameterInfo slot in member.GetParameters())
					TheTypeIsNotOfTheLedger(
						slot.ParameterType, subject.Name + "." + member.Name + "(" + slot.Name + ")");

				MethodInfo answering = member as MethodInfo;

				if (answering != null)
					TheTypeIsNotOfTheLedger(
						answering.ReturnType, subject.Name + "." + member.Name + " contesta");
			}

			foreach (FieldInfo field in subject.GetFields(
				BindingFlags.Instance | BindingFlags.Static
				| BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
				TheTypeIsNotOfTheLedger(
					field.FieldType, subject.Name + "." + field.Name + " guarda");
		}

		// LA MITAD SIMETRICA, NUEVA EN CL-3: el lado del libro no sabe nada del precio. Y la lista de
		// palabras incluye los OCHO SUSTANTIVOS PROHIBIDOS de §4 del brief, que es donde tenian que
		// afirmarse.
		[Test]
		public void AndNoMemberOfTheLedgerSideKnowsAnythingAboutAPublishedPrice()
		{
			List<string> censused = new List<string>();

			foreach (Type subject in TheTypesOfTheLedger())
			{
				censused.Add(subject.Name);

				foreach (MemberInfo member in subject.GetMembers(
					BindingFlags.Instance | BindingFlags.Static
					| BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
					foreach (string word in TheWordsOfThePrice)
						Assert.That(member.Name, Does.Not.Contain(word),
							"El miembro " + subject.Name + "." + member.Name + " nombra el lado del "
							+ "precio. §4 prohibe price, quote, estimate, invoice, payment, charge, "
							+ "transaction y expense como nombres de una entrada del libro: cada uno "
							+ "nombra algo que este modulo o no tiene o mantiene aparte a proposito.");

				foreach (MethodBase member in TheMethodsAndConstructorsOf(subject))
				{
					foreach (ParameterInfo slot in member.GetParameters())
						TheTypeIsNotOfThePrice(
							slot.ParameterType,
							subject.Name + "." + member.Name + "(" + slot.Name + ")");

					MethodInfo answering = member as MethodInfo;

					if (answering != null)
						TheTypeIsNotOfThePrice(
							answering.ReturnType, subject.Name + "." + member.Name + " contesta");
				}

				foreach (FieldInfo field in subject.GetFields(
					BindingFlags.Instance | BindingFlags.Static
					| BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
					TheTypeIsNotOfThePrice(
						field.FieldType, subject.Name + "." + field.Name + " guarda");
			}

			Assert.That(censused, Has.Count.GreaterThanOrEqualTo(12),
				"El censo recorrio " + censused.Count + " tipos del libro: "
				+ string.Join(", ", censused) + ". Si el numero baja, el censo dejo de ver una "
				+ "familia entera y no que el libro haya encogido.");

			TestContext.Out.WriteLine(
				"LOS TIPOS DEL LIBRO CENSADOS (" + censused.Count + ") -- "
				+ string.Join(", ", censused));
		}

		// POR VALOR: LOS DOS HECHOS, PARA LA MISMA OPERACION, A LA VEZ Y DISTINTOS. El fabricante
		// publico ₡120.50 para la revision de los 10000 km; la sucursal cobro ₡145000 por hacerla. Los
		// dos se leen y ninguno se parece al otro.
		[Test]
		public void APublishedPriceAndACostForTheSameOperationBothReadAndTheyDiffer()
		{
			ModelSpecification specification = TheSheetWithThePublishedPrice();
			CostLedger ledger = TheLedgerWithWhatWasActuallySpent();

			string thePrice = TheSentenceTheDoorWouldCompose.TheServicePrice(
				specification, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA,
				new DateTime(2025, 6, 15, 0, 0, 0), TheLanguage.OfTheRecord);

			string theCost = ledger.DescribeSpendBetween(
				new DateTime(2025, 6, 1, 0, 0, 0), new DateTime(2025, 6, 30, 0, 0, 0));

			Assert.That(thePrice, Does.Contain("₡120.50"),
				"El precio publicado, con su vigencia y su revision.");
			Assert.That(theCost, Does.Contain("₡145000.00"),
				"Y lo que de verdad se gasto, que no tiene vigencia ni revision porque no lo "
				+ "publico nadie.");

			Assert.That(thePrice, Does.Not.Contain("₡145000.00"),
				"El precio no se corrige con lo que se gasto: seria un reporte de variacion contra "
				+ "lo cotizado, y este modulo no tiene tal cosa.");
			Assert.That(theCost, Does.Not.Contain("₡120.50"),
				"Y el gasto no se deriva de lo publicado: lo que se pago lo dice quien lo cobro.");
		}

		// Y LA PRUEBA DE QUE NINGUNO SE DERIVA DEL OTRO: mover uno no mueve al otro. Es lo unico que
		// ve una derivacion escrita adentro de un cuerpo, que ningun censo de miembros alcanza.
		[Test]
		public void AndMovingEitherOneLeavesTheOtherLetterForLetterTheSame()
		{
			ModelSpecification specification = TheSheetWithThePublishedPrice();
			CostLedger ledger = TheLedgerWithWhatWasActuallySpent();

			DateTime asOf = new DateTime(2025, 6, 15, 0, 0, 0);
			DateTime from = new DateTime(2025, 6, 1, 0, 0, 0);
			DateTime to = new DateTime(2025, 6, 30, 0, 0, 0);

			string thePriceBefore = TheSentenceTheDoorWouldCompose.TheServicePrice(
				specification, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA, asOf,
				TheLanguage.OfTheRecord);
			string theCostBefore;

			ledger.RecordRecurringCost(
				new DateTime(2025, 6, 20, 9, 0, 0), 33000m, "CRC", "Otra cosa del mismo mes");

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheServicePrice(
					specification, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA,
					asOf, TheLanguage.OfTheRecord),
				Is.EqualTo(thePriceBefore),
				"Se gasto mas y el precio publicado no se movio ni una letra. Un precio que se "
				+ "corrigiera con el gasto seria el fabricante publicando lo que la sucursal cobro.");

			theCostBefore = ledger.DescribeSpendBetween(from, to);

			specification.OpenRevision("REV-2025-02", new DateTime(2025, 4, 1, 10, 0, 0));
			specification.PublishServicePrice(
				"REV-2025-02", OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA,
				999.99m, "CRC", new DateTime(2025, 1, 1, 0, 0, 0),
				new DateTime(2025, 12, 31, 0, 0, 0));
			specification.SealRevision("REV-2025-02");

			Assert.That(ledger.DescribeSpendBetween(from, to), Is.EqualTo(theCostBefore),
				"Y se republico el precio -- casi diez veces mas alto -- y lo gastado no se movio. "
				+ "Un gasto derivado del precio habria seguido a la revision nueva.");
			Assert.That(ledger.DescribeSpendBetween(from, to), Does.Not.Contain("999.99"));
		}

		private static ModelSpecification TheSheetWithThePublishedPrice()
		{
			ModelSpecification specification = new ModelSpecification("YARIS-2019");

			specification.OpenRevision("REV-2025-01", new DateTime(2025, 3, 14, 10, 0, 0));
			specification.PublishServicePrice(
				"REV-2025-01", OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA,
				120.50m, "CRC", new DateTime(2025, 1, 1, 0, 0, 0),
				new DateTime(2025, 12, 31, 0, 0, 0));
			specification.SealRevision("REV-2025-01");

			return specification;
		}

		private static CostLedger TheLedgerWithWhatWasActuallySpent()
		{
			CostLedger ledger = new CostLedger("1FTFW1ET5DFC10312");

			ledger.RecordRecurringCost(
				new DateTime(2025, 6, 12, 11, 0, 0), 145000m, "CRC",
				"Revisión de los 10000 km en el taller");

			return ledger;
		}

		private static void TheTypeIsNotOfTheLedger(Type found, string where)
		{
			foreach (string coming in TheTypesTheLedgerBrought)
				Assert.That(found.Name, Is.Not.EqualTo(coming),
					where + " es del libro de costos. §2.2: los dos hechos se leen uno al lado del "
					+ "otro y no se fusionan nunca.");

			foreach (string word in TheWordsOfTheLedger)
				Assert.That(found.Name, Does.Not.Contain(word),
					where + " nombra el libro de costos.");
		}

		private static void TheTypeIsNotOfThePrice(Type found, string where)
		{
			foreach (string word in TheWordsOfThePrice)
				Assert.That(found.Name, Does.Not.Contain(word),
					where + " nombra el lado del precio. Un costo se lee al lado de un precio y "
					+ "nunca a traves de el.");
		}

		private static IEnumerable<MethodBase> TheMethodsAndConstructorsOf(Type subject)
		{
			List<MethodBase> found = new List<MethodBase>();

			foreach (ConstructorInfo built in subject.GetConstructors(
				BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
				| BindingFlags.DeclaredOnly))
				found.Add(built);

			foreach (MethodInfo answering in subject.GetMethods(
				BindingFlags.Instance | BindingFlags.Static
				| BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
				found.Add(answering);

			return found;
		}
	}
}
