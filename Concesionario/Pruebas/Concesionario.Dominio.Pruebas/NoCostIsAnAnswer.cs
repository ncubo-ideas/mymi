using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LEY L8 de SLICE-CostLedger.md: "un periodo sin ninguna entrada contesta NO HAY GASTO
	// REGISTRADO, con el periodo nombrado. Nunca cero -- cero es un gasto, y 'nada registrado' no lo
	// es."
	//
	// Y ESTE ARCHIVO ES DONDE SE RESPONDE CUANTAS CLASES DE AUSENCIA TIENE ESTE AGREGADO. E47 dice
	// que cuando la ausencia tiene CLASES, cada clase es su propio tipo con nombre. §4 del brief
	// nombra dos candidatos -- NoCostOnRecord (un periodo sin entradas) y
	// ASpendNotSaidAcrossCurrencies (un periodo con dos monedas) -- y no son dos clases de lo mismo:
	//
	//   NoCostOnRecord es una AUSENCIA: no hay nada que evaluar, y ninguna cifra existe.
	//   ASpendNotSaidAcrossCurrencies es un ESTADO: hay gasto, esta dicho entero, cada moneda por su
	//   lado, y lo unico que no existe es un total entre monedas -- que este modulo no ofrece nunca.
	//
	// Asi que el agregado tiene UNA sola clase de ausencia, deliberadamente, que es lo que DEC-12
	// decidio para ModelSpecification y por lo que MS-3 se nego a construir NoPublishedPrice. La
	// forma de afirmar una negativa es por reflexion, porque una ausencia no se puede probar
	// llamando: el dia que alguien agregue una segunda, se pone rojo.
	//
	// EL DISCRIMINADOR ENTRE LAS DOS ES HasAnyCost, y esta afirmado en los dos sentidos. Sin eso,
	// "una ausencia y un estado" es un argumento; con eso, es una diferencia observable.
	[TestFixture]
	public sealed class NoCostIsAnAnswer
	{
		private const string TheVin = "1FTFW1ET5DFC10312";

		private static readonly DateTime TheStart = new DateTime(2026, 1, 1, 0, 0, 0);
		private static readonly DateTime TheEnd = new DateTime(2026, 3, 31, 0, 0, 0);

		// Los tipos que una segunda clase de ausencia tomaria por nombre. Ninguno existe, y §4 del
		// brief no nombra ninguno: los inventaria el reflejo, no el canon.
		private static readonly string[] TheAbsencesNobodyIsAllowedToInvent =
			{
				"NoSpendOnRecord", "NoSpendSummary", "NoAmortizedShare", "NoExpectedLife",
				"NoCostSubject", "NoCostEntry", "NoSpendComparison"
			};

		private CostLedger ledger;

		[SetUp]
		public void Setup()
		{
			ledger = new CostLedger(TheVin);
		}

		[Test]
		public void APeriodWithNoEntryAnswersWithWordsAndNamesThePeriod()
		{
			string said = TheSentenceTheDoorWouldCompose.TheSpend(ledger, TheStart, TheEnd, TheLanguage.OfTheRecord);

			Assert.That(said, Does.Contain("2026-01-01"),
				"El periodo nombrado: sin el, todos los periodos vacios dirian la misma frase.");
			Assert.That(said, Does.Contain("2026-03-31"));
			Assert.That(said, Does.Contain("no hay ningún gasto registrado"));
		}

		// NUNCA CERO, y las tres formas en que un cero se colaria: en la frase entera, en la de por
		// moneda, y en la comparacion.
		[Test]
		public void AndNeverZeroInAnyOfTheAnswers()
		{
			Assert.That(TheSentenceTheDoorWouldCompose.TheSpend(ledger, TheStart, TheEnd, TheLanguage.OfTheRecord),
				Does.Not.Contain("₡0").And.Not.Contain("0.00"));
			Assert.That(TheSentenceTheDoorWouldCompose.TheSpendPerCurrency(ledger, TheStart, TheEnd, TheLanguage.OfTheRecord),
				Does.Not.Contain("₡0").And.Not.Contain("0.00"));
			Assert.That(
				TheSentenceTheDoorWouldCompose.TheTwoPeriodsOfSpendCompared(
				ledger, TheStart, TheEnd, TheStart, TheEnd, TheLanguage.OfTheRecord),
				Does.Not.Contain("₡0").And.Not.Contain("0.00"));
		}

		// LA AUSENCIA CONTESTA TODAS LAS PREGUNTAS TOTALES, ninguna revienta, y ninguna contesta que
		// si: es E47 -- la base contesta TOTALMENTE y nadie ramifica por el tipo concreto.
		[Test]
		public void AndEveryTotalQuestionStillAnswers()
		{
			Assert.That(ledger.HasAnyCostBetween(TheStart, TheEnd), Is.False);
			Assert.That(ledger.IsSpendSaidInOneCurrencyBetween(TheStart, TheEnd), Is.False,
				"Un periodo sin gasto no esta dicho en una moneda: no hay ninguna en que decirlo. "
				+ "Contestar que si seria dar por respuesta un miembro benigno del conjunto de "
				+ "salida, que es la tercera antipatron de E47.");
			Assert.That(ledger.CostEntryCountBetween(TheStart, TheEnd), Is.EqualTo(0));
			Assert.That(TheSentenceTheDoorWouldCompose.EachCostEntryMoney(
				ledger, TheStart, TheEnd, TheLanguage.OfTheRecord), Is.Empty);
			Assert.That(TheSentenceTheDoorWouldCompose.EachCostEntry(
				ledger, TheStart, TheEnd, TheLanguage.OfTheRecord), Is.Empty);
		}

		// Y LA FRASE DE POR MONEDA DICE POR QUE NO HAY NINGUNA, en vez de devolver la lista vacia de
		// monedas y dejar que quien lea concluya.
		[Test]
		public void AndThePerCurrencyAnswerSaysWhyThereIsNoCurrencyToSayItIn()
		{
			Assert.That(TheSentenceTheDoorWouldCompose.TheSpendPerCurrency(ledger, TheStart, TheEnd, TheLanguage.OfTheRecord),
				Does.Contain("no hay ningún gasto registrado").And.Contain("ninguna moneda"));
		}

		// EL DISCRIMINADOR: el estado de dos monedas NO es la ausencia, y lo que lo prueba es que
		// contesta que SI hay gasto.
		[Test]
		public void TheTwoCurrencyStateIsNotAnAbsenceBecauseItStillHasCost()
		{
			ledger.RecordRecurringCost(
				new DateTime(2026, 1, 15, 9, 0, 0), 85000m, "CRC", "Gasolina de enero");
			ledger.RecordRecurringCost(
				new DateTime(2026, 2, 10, 9, 0, 0), 120m, "USD", "Repuesto importado");

			Assert.That(ledger.HasAnyCostBetween(TheStart, TheEnd), Is.True,
				"Hay gasto: lo que no hay es un total entre dos monedas, y eso es un estado y no "
				+ "una ausencia.");
			Assert.That(TheSentenceTheDoorWouldCompose.TheSpend(ledger, TheStart, TheEnd, TheLanguage.OfTheRecord),
				Does.Not.Contain("no hay ningún gasto registrado"));
		}

		// UNA SOLA CLASE DE AUSENCIA, POR REFLEXION. DEC-12 hizo esta misma eleccion en
		// ModelSpecification y MS-3 la sostuvo negandose a construir NoPublishedPrice: una variante
		// cuyo unico comportamiento es contestar falso, cero o vacio no gano su tipo (E39).
		[Test]
		public void AndNoSecondKindOfAbsenceWasInvented()
		{
			List<string> found = new List<string>();

			foreach (Type declared in typeof(CostLedger).Assembly.GetTypes())
				foreach (string invented in TheAbsencesNobodyIsAllowedToInvent)
					if (declared.Name == invented) found.Add(declared.Name);

			Assert.That(found, Is.Empty,
				"Aparecieron ausencias nuevas: " + string.Join(", ", found) + ". Este agregado "
				+ "tiene UNA clase de ausencia a proposito -- un periodo sin entradas -- y las "
				+ "demas 'ausencias' que se pueden imaginar son estados de una respuesta que si "
				+ "existe o defectos de quien llama.");
		}

		// Y LA AUSENCIA NO SE ALCANZA RAMIFICANDO POR EL TIPO: la base contesta las dos preguntas
		// totales y ningun llamador pregunta que tipo tiene en la mano. La mitad que se puede
		// afirmar es que el agregado no expone el tipo concreto a nadie.
		[Test]
		public void AndNoCallerCanBranchOnTheConcreteTypeBecauseNothingHandsItOut()
		{
			foreach (System.Reflection.MethodInfo member in typeof(CostLedger).GetMethods(
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public
				| System.Reflection.BindingFlags.NonPublic
				| System.Reflection.BindingFlags.DeclaredOnly))
			{
				if (!(member.IsPublic || member.IsAssembly)) continue;

				Assert.That(typeof(SpendInAPeriod).IsAssignableFrom(member.ReturnType), Is.False,
					"CostLedger." + member.Name + " entrega la respuesta del periodo como objeto. "
					+ "E47: la base contesta totalmente y nadie ramifica por el tipo concreto -- "
					+ "entregarlo es justo lo que deja ramificar.");
			}
		}
	}
}
