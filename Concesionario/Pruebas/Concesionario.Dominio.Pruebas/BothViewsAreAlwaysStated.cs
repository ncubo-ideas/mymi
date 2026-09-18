using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LEY L6 de SLICE-CostLedger.md, LA LEY CENTRAL DE ESTE SLICE: "toda respuesta de un periodo dice
	// LA CAJA Y LO CONSUMIDO JUNTOS. Ninguna de las dos esta disponible sola, porque cada una sola es
	// el numero que engana. Quien consulta no puede pedir la mitad."
	//
	// POR QUE HAY DOS MITADES Y LAS DOS SON NECESARIAS. La mitad por VALOR -- las dos cifras en una
	// frase -- pasaria en verde el dia que alguien agregara un `TheCashOut()` al lado, porque la
	// frase seguiria diciendo las dos. La mitad por CENSO -- ningun miembro contesta una vista sola
	// -- pasaria en verde el dia que la frase dejara de decir una de las dos. §15 item 8 del brief
	// existe porque esta es la ley que falla EN SILENCIO: dos cifras en una respuesta son mas
	// incomodas de maquetar que una, y la tentacion es una lectura que devuelva cada una.
	//
	// EL CENSO ES POR CONJUNTO EXACTO Y NO POR PALABRAS. Prohibir los nombres "CashOut" y "Consumed"
	// dejaria pasar un `TheFirstFigure()`. Lo que se afirma es que los miembros de ASpendSummary que
	// contestan un string son EXACTAMENTE los que la lista nombra, asi que cualquier miembro nuevo
	// que conteste algo se pone rojo en la entrada y hay que argumentarlo (C57: enumerar los sitios
	// de llamada de la regla, no los sustantivos de los que habla).
	//
	// EL CASO DE LAS LLANTAS DE MARZO es el test 8 del brief, con los numeros elegidos para que la
	// cuenta sea EXACTA y las dos cifras se puedan afirmar por literal: ₡240000 de llantas con 4
	// meses de vida desde el 1 de enero reparten ₡2000 por dia de los 120 dias de su ventana, asi que
	// los 31 dias de enero le tocan ₡62000. Nada de esto se recalcula en el test: se escribe el
	// numero, que es el ancla que E49 pide.
	[TestFixture]
	public sealed class BothViewsAreAlwaysStated
	{
		private const string TheVin = "1FTFW1ET5DFC10312";
		private const string TheTyres = "Llantas nuevas";
		private const string TheFuel = "Gasolina de enero";

		private static readonly DateTime TheTyresWereBought = new DateTime(2026, 1, 1, 0, 0, 0);
		private static readonly DateTime TheFuelWasBought = new DateTime(2026, 1, 15, 9, 0, 0);

		private static readonly DateTime TheStartOfJanuary = new DateTime(2026, 1, 1, 0, 0, 0);
		private static readonly DateTime TheStartOfFebruary = new DateTime(2026, 2, 1, 0, 0, 0);

		private CostLedger ledger;

		[SetUp]
		public void Setup()
		{
			ledger = new CostLedger(TheVin);

			ledger.RecordDurableCost(TheTyresWereBought, 240000m, "CRC", TheTyres, 4);
			ledger.RecordRecurringCost(TheFuelWasBought, 25000m, "CRC", TheFuel);
		}

		// EL CASO DE LAS LLANTAS DE MARZO, por literal en las dos cifras.
		[Test]
		public void TheMarchTyresCaseSaysBothFiguresAndTheyDiffer()
		{
			string said = TheSentenceTheDoorWouldCompose.TheSpend(ledger, TheStartOfJanuary, TheStartOfFebruary, TheLanguage.OfTheRecord);

			Assert.That(said, Does.Contain("₡265.000,00"),
				"La caja: ₡240000 de llantas mas ₡25000 de gasolina, los dos incurridos adentro.");
			Assert.That(said, Does.Contain("₡87.000,00"),
				"Lo consumido: ₡25000 de gasolina mas ₡62000 de la parte de las llantas que este "
				+ "periodo consumio. §2.1: un total que no separa lo duradero de lo recurrente dice "
				+ "'el carro se puso caro' cuando la verdad es 'compro llantas'.");
			Assert.That("₡265.000,00", Is.Not.EqualTo("₡87.000,00"),
				"Y DIFIEREN, que es lo que hace la distincion una respuesta y no un adorno.");
		}

		// Y LA REGLA ESTA DICHA EN LA FRASE, que es lo que G2 pide: la persona ve la regla en vez de
		// tener que creerle.
		[Test]
		public void AndTheStraightLineRuleIsSaidInsideTheSameAnswer()
		{
			string said = TheSentenceTheDoorWouldCompose.TheSpend(ledger, TheStartOfJanuary, TheStartOfFebruary, TheLanguage.OfTheRecord);

			Assert.That(said, Does.Contain("línea recta"));
			Assert.That(said, Does.Contain("4 meses"));
			Assert.That(said, Does.Contain("2026-01-01"),
				"El ancla de la ventana, para que la cuenta se pueda revisar sin abrir el codigo.");
			Assert.That(said, Does.Contain("₡62.000,00"),
				"Y la parte que a este periodo le toca, dicha.");
		}

		// LA MITAD POR CENSO, PRIMERA PARTE: nada sale de ASpendSummary como numero ni como Money.
		[Test]
		public void NoMemberOfTheSummaryHandsOutAFigureOfAnyKind()
		{
			List<string> guilty = new List<string>();

			foreach (MemberInfo member in TheExposedAnswersOf(typeof(ASpendSummary)))
			{
				MethodInfo answering = member as MethodInfo;

				if (answering == null) continue;

				if (answering.ReturnType == typeof(Money)
					|| answering.ReturnType == typeof(decimal)
					|| answering.ReturnType == typeof(int)
					|| answering.ReturnType == typeof(long))
					guilty.Add(answering.Name + " contesta " + answering.ReturnType.Name);
			}

			Assert.That(guilty, Is.Empty,
				"Miembros de ASpendSummary que entregan una cifra: " + string.Join(", ", guilty)
				+ ". Un Money de la caja entregado solo ES la mitad de la respuesta: quien lo tenga "
				+ "lo va a mostrar sin la otra.");
		}

		// LA MITAD POR CENSO, SEGUNDA PARTE Y LA FUERTE: el conjunto de miembros que contestan algo es
		// EXACTAMENTE este. Un miembro nuevo -- se llame como se llame -- se pone rojo en la entrada.
		[Test]
		public void AndTheSummaryAnswersExactlyOneThingAndItIsTheWholeSentence()
		{
			List<string> answering = new List<string>();

			foreach (MemberInfo member in TheExposedAnswersOf(typeof(ASpendSummary)))
			{
				MethodInfo method = member as MethodInfo;

				if (method != null && method.ReturnType != typeof(void)
					&& !method.IsStatic && method.GetParameters().Length == 0)
					answering.Add(method.Name);
			}

			answering.Sort(StringComparer.Ordinal);

			Assert.That(answering, Is.EqualTo(new[] { "TheClauseItComposes" }),
				"ASpendSummary contesta " + string.Join(", ", answering) + ". L6: una sola "
				+ "respuesta, con las dos vistas adentro. Un segundo miembro es la ley cediendo, y "
				+ "cede en silencio: la frase seguiria diciendo las dos. Que la CLAUSULA entregue "
				+ "despues cada mitad por separado no abre el atajo: la clausula no cruza ninguna "
				+ "puerta, y lo unico que cruza es la respuesta ya redactada, que dice las dos.");
		}

		// LA MITAD POR VALOR, SOBRE TODA RESPUESTA DE PERIODO Y POR REFLEXION, no sobre una lista que
		// alguien escribio: se enumeran las lecturas del agregado que contestan con la CLAUSULA del
		// gasto y se exige que cada una, ya redactada, traiga LAS DOS cifras.
		//
		// EL CENSO SE MUDO DEL TIPO string AL PORTADOR, y con eso entro la comparacion de dos
		// periodos, que antes quedaba afuera por tomar cuatro extremos y no dos. Los argumentos se
		// arman por CANTIDAD de extremos, asi que una lectura nueva entra sola.
		[Test]
		public void AndEveryPeriodAnswerOfTheAggregateCarriesBothFigures()
		{
			List<string> read = new List<string>();

			foreach (MethodInfo member in typeof(CostLedger).GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
				| BindingFlags.DeclaredOnly))
			{
				if (!(member.IsPublic || member.IsAssembly)) continue;
				if (member.ReturnType != typeof(ASpendClause)) continue;

				object[] bounds = member.GetParameters().Length == 2
					? new object[] { TheStartOfJanuary, TheStartOfFebruary }
					: new object[]
					{
						TheStartOfJanuary, TheStartOfFebruary, TheStartOfJanuary,
						TheStartOfFebruary,
					};

				string said = TheSentenceTheDoorWouldCompose.TheSpend(
					(ASpendClause)member.Invoke(ledger, bounds), TheLanguage.OfTheRecord);

				read.Add(member.Name);

				Assert.That(said, Does.Contain("₡265.000,00"),
					member.Name + " no dice la caja del periodo.");
				Assert.That(said, Does.Contain("₡87.000,00"),
					member.Name + " no dice lo que el periodo consumio. Las dos van juntas o L6 ya "
					+ "cedio.");
			}

			Assert.That(read, Has.Count.GreaterThanOrEqualTo(3),
				"El censo encontro " + read.Count + " lectura(s) de gasto de un periodo: "
				+ string.Join(", ", read) + ". Si el numero baja, o el censo dejo de ver algo o el "
				+ "agregado dejo de contestarlo.");

			TestContext.Out.WriteLine(
				"LAS LECTURAS DE GASTO CENSADAS (" + read.Count + ") -- " + string.Join(", ", read));
		}

		// Y NINGUNA LECTURA DEL AGREGADO ENTREGA UN MONEY NI UNA CIFRA SUELTA. Las dos que contestan
		// un numero son el conteo de filas -- un conteo que el dominio compone, la primera categoria
		// del censo numerico del door -- y los MESES de la vida util de cada fila, que no son dinero:
		// son un hecho recibido con la entrada, y cual fila tiene uno lo dice su clasificacion y nunca
		// ese numero. Ninguna de las dos es una mitad de la respuesta de L6, que es la ley de aca.
		[Test]
		public void AndNoReadOfTheLedgerHandsOutAMoneyOrABareFigureExceptTheRowCount()
		{
			List<string> numbers = new List<string>();

			foreach (MethodInfo member in typeof(CostLedger).GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
				| BindingFlags.DeclaredOnly))
			{
				if (!(member.IsPublic || member.IsAssembly) || member.IsSpecialName) continue;

				Assert.That(member.ReturnType, Is.Not.EqualTo(typeof(Money)),
					member.Name + " entrega un Money. Un Money de la caja o de lo consumido es una "
					+ "mitad de la respuesta compuesta.");

				if (member.ReturnType == typeof(decimal) || member.ReturnType == typeof(int)
					|| member.ReturnType == typeof(long) || member.ReturnType == typeof(decimal[])
					|| member.ReturnType == typeof(int[]))
					numbers.Add(member.Name);
			}

			numbers.Sort(StringComparer.Ordinal);

			Assert.That(
				numbers,
				Is.EqualTo(new[] { "CostEntryCountBetween", "CostEntryExpectedLivesBetween" }),
				"Lecturas del libro que contestan un numero: " + string.Join(", ", numbers)
				+ ". El conteo de filas, que el dominio compone, y los meses de la vida util de cada "
				+ "fila, que llegaron con la entrada. Cualquier otra seria un monto, una parte o una "
				+ "diferencia, y las tres son mitades.");
		}

		private static IEnumerable<MemberInfo> TheExposedAnswersOf(Type subject)
		{
			List<MemberInfo> found = new List<MemberInfo>();

			foreach (MethodInfo member in subject.GetMethods(
				BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
				| BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
				if ((member.IsPublic || member.IsAssembly) && !member.IsSpecialName)
					found.Add(member);

			foreach (PropertyInfo member in subject.GetProperties(
				BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
				| BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
			{
				MethodInfo reader = member.GetGetMethod(true);

				if (reader != null && (reader.IsPublic || reader.IsAssembly)) found.Add(reader);
			}

			return found;
		}
	}
}
