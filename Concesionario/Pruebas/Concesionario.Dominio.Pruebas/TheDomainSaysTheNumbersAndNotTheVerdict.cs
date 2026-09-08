using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LEY L12 de SLICE-CostLedger.md: "'¿Vale la pena seguir arreglando el carro?' se contesta
	// DICIENDO EL GASTO CON LA DISTINCION QUE LO HACE LEGIBLE. El dominio nunca dice si ni no, nunca
	// puntua, nunca marca 'caro'. El juicio es de la persona, y no hay umbral en el que se vuelva del
	// dominio."
	//
	// EL CENSO SE ENUMERA POR REFLEXION Y NO A MANO, porque un censo sobre una lista que alguien
	// escribio es tan completo como la lista: la lectura que alguien agregue manana no esta en ella.
	// Se enumeran las lecturas del agregado que contestan un string o un string[], se invocan TODAS,
	// y se barre cada frase producida.
	//
	// Y SE INVOCAN EN TRES ESTADOS DEL REGISTRO, que es C61: una comparacion es tan fuerte como el
	// momento en que se toma. Las frases de este agregado no son una sola -- la ausencia, la de una
	// moneda con una duradera adentro, y la de dos monedas son tres textos distintos -- asi que un
	// censo en un solo estado deja dos sin barrer.
	//
	// LAS PALABRAS SON LAS CUATRO QUE §8 DEL PLAN NOMBRA -- vale, caro, barato, conviene -- mas las
	// formas que un puntaje tomaria. Un porcentaje entra a la lista por dos razones: es la forma mas
	// comun de un puntaje, y en este slice tambien seria el tercer redondeo que R7 no autoriza.
	//
	// Y UN LIMITE DICHO EN VEZ DE OMITIDO: estas frases llevan TEXTO RECIBIDO adentro -- el asunto lo
	// escribe la sucursal con sus palabras (G3) -- asi que un asunto que dijera "muy caro" pondria el
	// censo rojo por algo que el dominio no dijo. Los asuntos de este archivo son neutros a proposito;
	// el censo mide lo que el dominio COMPONE y no puede distinguirlo de lo que le entregaron.
	[TestFixture]
	public sealed class TheDomainSaysTheNumbersAndNotTheVerdict
	{
		private const string TheVin = "1FTFW1ET5DFC10312";

		private static readonly string[] TheWordsOfAVerdict =
			{
				"vale", "caro", "barato", "conviene", "recomend", "puntaje", "score", "califica",
				"deberia", "debería", "mejor que", "peor que", "excesivo", "alto para", "%"
			};

		private static readonly DateTime TheStart = new DateTime(2026, 1, 1, 0, 0, 0);
		private static readonly DateTime TheEnd = new DateTime(2026, 12, 31, 0, 0, 0);

		[Test]
		public void NoComposedSentenceOfTheLedgerCarriesAVerdictInAnyStateOfTheRecord()
		{
			List<string> swept = new List<string>();

			foreach (string state in new[] { "nothing", "one currency", "two currencies" })
			{
				CostLedger ledger = TheLedgerIn(state);

				foreach (MethodInfo read in TheSentenceReadsOf(typeof(CostLedger)))
				{
					foreach (string said in TheAnswersOf(read, ledger))
					{
						swept.Add(state + " / " + read.Name);

						foreach (string word in TheWordsOfAVerdict)
							Assert.That(said.ToLowerInvariant(), Does.Not.Contain(word),
								"En el estado '" + state + "', " + read.Name + " dijo '" + word
								+ "'. L12: el dominio dice las cifras y la distincion que las hace "
								+ "legibles; el juicio es de la persona.");
					}
				}
			}

			Assert.That(swept, Has.Count.GreaterThanOrEqualTo(36),
				"El censo barrio " + swept.Count + " frases. ONCE lecturas de frase sobre tres "
				+ "estados: el estado vacio contesta solo las CINCO frases enteras -- sus columnas "
				+ "vienen vacias -- y los otros dos contestan cinco frases mas seis columnas por "
				+ "fila. Si el numero baja, o el censo dejo de ver una lectura o el agregado dejo "
				+ "de contestarla. EL PISO SUBIO 30 -> 36 EN BuildOrder/CL-6, por lo que esa tarea "
				+ "agrego -- dos lecturas de frase sobre tres estados -- y no hasta donde llega.");

			TestContext.Out.WriteLine(
				"LAS FRASES BARRIDAS (" + swept.Count + ") -- " + string.Join("; ", swept));
		}

		// Y LO QUE SI DICE, POR VALOR: la pregunta del canon se contesta con las dos cifras y la
		// distincion. Sin esta mitad, un dominio que no dijera nada tambien pasaria el censo.
		[Test]
		public void AndWhatItDoesSayIsTheTwoFiguresAndTheDistinctionBetweenThem()
		{
			CostLedger ledger = TheLedgerIn("one currency");

			string said = ledger.DescribeSpendBetween(TheStart, TheEnd);

			Assert.That(said, Does.Contain("₡265000.00"), "La caja.");
			Assert.That(said, Does.Contain("consumió"), "Y la distincion, dicha con la palabra.");
			Assert.That(said, Does.Contain("línea recta"),
				"Y la regla con que se reparte lo duradero, que es lo que hace la cifra revisable "
				+ "en vez de creible.");
		}

		// NINGUN MIEMBRO SE LLAMA COMO UN VEREDICTO TAMPOCO. La otra mitad del censo: una lectura
		// llamada IsItWorthRepairing que contestara un bool no diria ninguna de las palabras de
		// arriba.
		[TestCase(typeof(CostLedger))]
		[TestCase(typeof(ASpendSummary))]
		[TestCase(typeof(ASpendComparison))]
		[TestCase(typeof(SpendInAPeriod))]
		[TestCase(typeof(ACostEntry))]
		[TestCase(typeof(AnAmortizedShare))]
		[TestCase(typeof(ACostPerDistance))]
		[TestCase(typeof(AMeasuredCostPerDistance))]
		[TestCase(typeof(NoSpanOfUseToDivideBy))]
		public void AndNoMemberOfTheLedgerIsNamedForAVerdict(Type subject)
		{
			foreach (MemberInfo member in subject.GetMembers(
				BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
				| BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
				foreach (string word in new[]
					{ "Worth", "Expensive", "Cheap", "Score", "Verdict", "Advice", "Recommend" })
					Assert.That(member.Name, Does.Not.Contain(word),
						subject.Name + "." + member.Name + " nombra un juicio. No hay umbral en el "
						+ "que el juicio se vuelva del dominio (§7).");
		}

		private static CostLedger TheLedgerIn(string state)
		{
			CostLedger ledger = new CostLedger(TheVin);

			if (state == "nothing") return ledger;

			ledger.RecordDurableCost(
				new DateTime(2026, 1, 1, 0, 0, 0), 240000m, "CRC", "Llantas nuevas", 4);
			ledger.RecordRecurringCost(
				new DateTime(2026, 1, 15, 9, 0, 0), 25000m, "CRC", "Gasolina de enero");

			if (state == "one currency") return ledger;

			ledger.RecordRecurringCost(
				new DateTime(2026, 2, 10, 9, 0, 0), 120m, "USD", "Repuesto importado");

			return ledger;
		}

		private static IEnumerable<MethodInfo> TheSentenceReadsOf(Type aggregate)
		{
			List<MethodInfo> found = new List<MethodInfo>();

			foreach (MethodInfo member in aggregate.GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
				| BindingFlags.DeclaredOnly))
			{
				if (!(member.IsPublic || member.IsAssembly) || member.IsSpecialName) continue;
				if (member.ReturnType != typeof(string) && member.ReturnType != typeof(string[]))
					continue;
				if (member.GetParameters().Length == 0) continue;

				found.Add(member);
			}

			return found;
		}

		// LOS ARGUMENTOS POR ARIDAD, Y LAS DOS DEL CRUCE HAY QUE ARMARLAS APARTE. BuildOrder/CL-6 le
		// agrego al libro dos lecturas de frase que toman los hechos del TRAMO de otro agregado -- el
		// ancla, la distancia y su unidad, o el motivo dicho de que no hay tramo -- asi que sus
		// aridades son CINCO y TRES. Este censo se puso rojo con una TargetParameterCountException
		// cuando llegaron, y el rojo era correcto: barrer "toda frase del libro" y saber armar solo dos
		// aridades es una lista de permitidos disfrazada de barrido. Se amplia POR ARIDAD y no
		// aflojando el filtro.
		private static IEnumerable<string> TheAnswersOf(MethodInfo read, CostLedger ledger)
		{
			ParameterInfo[] slots = read.GetParameters();

			object[] given = TheArgumentsFor(slots.Length);

			object answer = read.Invoke(ledger, given);

			string one = answer as string;

			if (one != null) return new[] { one };

			return (string[])answer;
		}

		// UNA ARIDAD QUE ESTE CENSO NO SEPA ARMAR FALLA EN VEZ DE ADIVINAR, que es la unica manera de
		// que ampliar el libro obligue a pasar por aca.
		private static object[] TheArgumentsFor(int slots)
		{
			if (slots == 2) return new object[] { TheStart, TheEnd };

			if (slots == 4) return new object[] { TheStart, TheEnd, TheStart, TheEnd };

			// LOS DOS EXTREMOS Y EL MOTIVO DICHO POR LA SERIE DEL ODOMETRO. El motivo se pasa como una
			// frase cualquiera: lo que este censo mide es lo que el LIBRO compone alrededor de ella.
			if (slots == 3)
				return new object[] { TheStart, TheEnd, "no se puede decir cuánto se maneja" };

			// LOS DOS EXTREMOS Y LOS TRES HECHOS DEL TRAMO. La distancia y el ancla son literales: el
			// censo no lee ningun reloj y no depende de ninguna serie (C27).
			if (slots == 5)
				return new object[]
				{
					TheStart, TheEnd, TheAnchor, TheDistance, DistanceUnitCode.UnitA,
				};

			throw new ArgumentException(
				"El libro gano una lectura de frase de " + slots + " argumentos y este censo no sabe "
				+ "armarlos. Se arma por ARIDAD a proposito: agregue la aridad nueva y diga con que "
				+ "valores se pregunta.", nameof(slots));
		}

		// EL ANCLA Y LA DISTANCIA CON QUE SE PREGUNTA EL COSTO POR DISTANCIA. Elegidos para que el
		// tramo caiga adentro del periodo y para que las dos entradas de colones queden despues del
		// ancla, que es donde la frase dice cifras y no una ausencia.
		private static readonly DateTime TheAnchor = new DateTime(2026, 1, 1, 0, 0, 0);

		private const int TheDistance = 3200;
	}
}
