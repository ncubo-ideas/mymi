using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// SLICE-CostLedger L13 y §10.3: el costo por distancia se dice sobre lo que se gasto DESPUES de la
	// primera lectura del periodo, sobre la distancia recorrida desde ella, y la frase nombra esa
	// lectura y su fecha para que la regla se VEA. Lo que se pago en esa lectura o antes movio los
	// kilometros del periodo anterior.
	//
	// POR QUE ES UNA LEY Y NO UN DETALLE. Incluir el gasto del ancla infla la respuesta y la infla en
	// silencio: el numero sigue pareciendo plausible. El defecto no se ve mirando la frase, se ve
	// comparando dos respuestas que difieren por exactamente el gasto del ancla.
	//
	// LA ENTRADA SE MONTA EN EL INSTANTE EXACTO DEL ANCLA, no cerca (C61). El limite ES la prueba: una
	// entrada un segundo despues del ancla SI cuenta, y las dos afirmaciones estan juntas porque una
	// sola de ellas pasaria con un filtro que usara >= en vez de >.
	//
	// NIVEL A: el libro directo, sin motor y sin disco, con la distancia y el ancla pasadas como
	// LITERALES. Es la mitad que puede afirmar la aritmetica exacta. Que el ancla venga de verdad de la
	// agenda -- que es el CRUCE -- lo afirma la mitad de abajo, por la puerta.
	[TestFixture]
	public sealed class TheFirstReadingIsAnAnchorAndNotASpend
	{
		private const string TheVin = "1FTFW1ET5DFC12345";

		private const string TheColon = "CRC";
		private const string TheDollar = "USD";

		// EL PERIODO. Los dos limites son INSTANTES, como todos los del libro (G4).
		private static readonly DateTime TheYearFrom =
			new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheYearTo =
			new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);

		// EL ANCLA: la primera lectura del odometro que cae dentro del periodo.
		private static readonly DateTime TheAnchor =
			new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc);

		// LA DISTANCIA RECORRIDA DESDE EL ANCLA, en la unidad de la serie. Elegida para que las dos
		// divisiones den cifras exactas en las unidades menores de cada moneda.
		private const int TheDistance = 3200;

		// LO QUE SE PAGO EN EL INSTANTE DEL ANCLA: pago el recorrido de antes y L13 lo excluye.
		private const decimal TheTyresOfLastYear = 160000m;

		// LO QUE SE GASTO DESPUES: esto SI cuenta.
		private const decimal TheOilChange = 285000m;

		private static readonly DateTime TheOilChangeIncurred =
			new DateTime(2026, 5, 20, 10, 30, 0, DateTimeKind.Utc);

		private const decimal TheImportedPart = 6400m;

		private static readonly DateTime TheImportedPartIncurred =
			new DateTime(2026, 8, 5, 14, 15, 0, DateTimeKind.Utc);

		// ===== L13, LA MITAD EXACTA ========================================================

		// UNA ENTRADA EN EL INSTANTE DEL ANCLA QUEDA AFUERA, Y LA RESPUESTA ES MENOR POR EXACTAMENTE
		// ELLA. Las dos respuestas salen del MISMO libro y de la MISMA distancia: lo unico que cambia
		// es donde esta el ancla, asi que la diferencia entre ellas es el gasto que L13 excluye y nada
		// mas.
		[Test]
		public void AnEntryAtTheAnchorsInstantIsExcludedAndTheAnswerIsLowerByExactlyIt()
		{
			CostLedger ledger = ALedgerWithTheAnchorSpendAndTheOilChange();

			string withTheAnchorSpendCounted = TheSentenceTheDoorWouldCompose.TheCostPerDistance(
				ledger.TheCostPerDistanceClause(
				TheYearFrom, TheYearTo, TheYearFrom, TheDistance, DistanceUnitCode.UnitA), TheLanguage.OfTheRecord);

			string asL13SaysIt = TheSentenceTheDoorWouldCompose.TheCostPerDistance(
				ledger.TheCostPerDistanceClause(
				TheYearFrom, TheYearTo, TheAnchor, TheDistance, DistanceUnitCode.UnitA), TheLanguage.OfTheRecord);

			Assert.That(
				withTheAnchorSpendCounted, Does.Contain("₡139,06 por km"),
				"Con el ancla puesta antes de las llantas del año pasado, las llantas cuentan: "
				+ "445000 sobre 3200 son 139.0625, que en colones se dicen 139.06.");

			Assert.That(
				asL13SaysIt, Does.Contain("₡89,06 por km"),
				"Con el ancla en el instante de las llantas, las llantas NO cuentan: 285000 sobre "
				+ "3200 son 89.0625, que en colones se dicen 89.06. Es la MISMA distancia y el MISMO "
				+ "libro: lo unico que cambio es donde esta el ancla.");

			Assert.That(
				TheColonesPerUnitInside(withTheAnchorSpendCounted)
					- TheColonesPerUnitInside(asL13SaysIt),
				Is.EqualTo(TheTyresOfLastYear / TheDistance),
				"Y la diferencia entre las dos es EXACTAMENTE el gasto del ancla sobre la misma "
				+ "distancia: 160000 sobre 3200 son 50.00. Sin L13 la respuesta se infla en silencio, "
				+ "porque 139.06 tambien parece plausible.");
		}

		// Y UN SEGUNDO DESPUES DEL ANCLA SI CUENTA, que es la otra mitad del limite. Con esta y la de
		// arriba juntas, un filtro escrito con >= en vez de > se pone rojo; con una sola, no.
		[Test]
		public void AndAnEntryOneSecondAfterTheAnchorIsCountedInFull()
		{
			CostLedger ledger = new CostLedger(TheVin);

			ledger.RecordRecurringCost(
				TheAnchor.AddSeconds(1), TheTyresOfLastYear, TheColon, "Llantas nuevas");

			Assert.That(
				TheSentenceTheDoorWouldCompose.TheCostPerDistance(
				ledger.TheCostPerDistanceClause(
					TheYearFrom, TheYearTo, TheAnchor, TheDistance, DistanceUnitCode.UnitA), TheLanguage.OfTheRecord),
				Does.Contain("₡50,00 por km"),
				"160000 sobre 3200 son 50.00. Un segundo despues del ancla es despues del ancla.");
		}

		// Y LA FRASE NOMBRA EL ANCLA Y SU FECHA, que es lo que hace la regla VISIBLE en vez de creible.
		// Sin el instante, dos periodos con anclas distintas dirian la misma frase con cifras
		// distintas y nadie podria revisar por que.
		[Test]
		public void AndTheSentenceNamesTheAnchorReadingAndItsDate()
		{
			CostLedger ledger = ALedgerWithTheAnchorSpendAndTheOilChange();

			string said = TheSentenceTheDoorWouldCompose.TheCostPerDistance(
				ledger.TheCostPerDistanceClause(
				TheYearFrom, TheYearTo, TheAnchor, TheDistance, DistanceUnitCode.UnitA), TheLanguage.OfTheRecord);

			Assert.That(said, Does.Contain("2026-01-10 08:00:00"),
				"El instante del ancla, dicho como instante con su hora: de que dia local es un "
				+ "instante necesitaria una zona que este modulo no tiene.");
			Assert.That(said, Does.Contain("primera lectura del odómetro"),
				"Y dicho por lo que es, no como una fecha suelta.");
			Assert.That(said, Does.Contain("3.200 km"),
				"Y la distancia con su unidad, para que la division se pueda revisar.");
			Assert.That(said, Does.Contain("2026-01-01 00:00:00").And.Contains("2026-12-31 23:59:59"),
				"Y los dos limites por los que se pregunto.");
		}

		// ===== §7.2: POR MONEDA, Y CON LAS DOS UNIDADES DICHAS =============================

		// DOS MONEDAS SOBRE UN MISMO TRAMO ES DONDE "POR MONEDA" DEJA DE SER ADORNO. Cada una sale con
		// su propio signo y con la unidad de distancia al lado, y no hay ningun total entre ellas (L3).
		[Test]
		public void CostPerDistanceIsSaidWithBothUnitsAndPerCurrency()
		{
			CostLedger ledger = ALedgerWithTheAnchorSpendAndTheOilChange();

			ledger.RecordRecurringCost(
				TheImportedPartIncurred, TheImportedPart, TheDollar, "Repuesto importado");

			string said = TheSentenceTheDoorWouldCompose.TheCostPerDistance(
				ledger.TheCostPerDistanceClause(
				TheYearFrom, TheYearTo, TheAnchor, TheDistance, DistanceUnitCode.UnitA), TheLanguage.OfTheRecord);

			Assert.That(said, Does.Contain("₡89,06 por km"),
				"La de colones, con el signo de su moneda Y la unidad de la distancia: las DOS "
				+ "unidades dichas.");
			Assert.That(said, Does.Contain("$2,00 por km"),
				"Y la de dolares por su lado: 6400 sobre 3200 son 2.00.");
			Assert.That(said, Does.Not.Contain("291400").And.Not.Contain("₡91,06"),
				"Y NINGUN total entre las dos: 285000 colones y 6400 dolares no se suman, y "
				+ "89.06 + 2.00 no es una cifra de nada.");
		}

		// ===== LAS DOS SITUACIONES QUE NO SON UNA CIFRA ====================================

		// NADA GASTADO DESPUES DEL ANCLA SE DICE CON PALABRAS Y NUNCA COMO UN CERO. El periodo SI tiene
		// gasto -- por eso CanCostPerDistanceBeSaid contesta que si -- y lo que no tiene es gasto
		// DESPUES del ancla, que es otra cosa.
		[Test]
		public void NothingIncurredAfterTheAnchorIsSaidInWordsAndNeverAsZero()
		{
			CostLedger ledger = new CostLedger(TheVin);

			ledger.RecordRecurringCost(TheAnchor, TheTyresOfLastYear, TheColon, "Llantas nuevas");

			string said = TheSentenceTheDoorWouldCompose.TheCostPerDistance(
				ledger.TheCostPerDistanceClause(
				TheYearFrom, TheYearTo, TheAnchor, TheDistance, DistanceUnitCode.UnitA), TheLanguage.OfTheRecord);

			Assert.That(said, Does.Contain("no se gastó nada"),
				"Dicho con palabras. Un ₡0,00 por km se leeria como un vehiculo que anduvo gratis.");
			Assert.That(said, Does.Not.Contain("0.00 por km").And.Not.Contain("₡0"));
			Assert.That(said, Does.Contain("2026-01-10 08:00:00").And.Contains("3.200 km"),
				"Y sigue nombrando el ancla y la distancia: lo que falta es el gasto, no el tramo.");
		}

		// Y UNA DISTANCIA DE CERO TAMPOCO ES UN COSTO DE CERO. El tramo SI se pudo medir -- por eso el
		// script eligio esta rama -- y lo que no hay es distancia por la que dividir. Un cero aca seria
		// una division por cero disfrazada de respuesta.
		[Test]
		public void AZeroDistanceIsSaidInWordsAndNeverAsACostOfZero()
		{
			CostLedger ledger = ALedgerWithTheAnchorSpendAndTheOilChange();

			string said = TheSentenceTheDoorWouldCompose.TheCostPerDistance(
				ledger.TheCostPerDistanceClause(
				TheYearFrom, TheYearTo, TheAnchor, 0, DistanceUnitCode.UnitA), TheLanguage.OfTheRecord);

			Assert.That(said, Does.Contain("no hay distancia recorrida"),
				"Dicho con palabras, y nombrando que lo que falta es la distancia.");
			Assert.That(said, Does.Not.Contain("por km").Or.Contains("de cero por km"),
				"Y ninguna cifra por unidad: dividir entre cero no da una respuesta grande, no da "
				+ "ninguna.");
			Assert.That(said, Does.Contain("2026-01-10 08:00:00"),
				"Y sigue nombrando el ancla, que es la lectura contra la que el odometro no se movio.");
		}

		// ===== §10.3: L3 SE CONSUME, NO SE REESCRIBE =======================================

		// UN TRAMO EN DOS UNIDADES SUSPENDE EL COSTO POR DISTANCIA, Y LA FRASE QUE LO DICE ES LA DEL
		// RITMO DE USO. Se afirma COMPARANDO LAS DOS LECTURAS y no reescribiendo las palabras: una
		// copia de la frase aca seria una segunda version de la ley de otro slice, y las dos podrian
		// discrepar sin que nada lo viera (DEC-84 midio el precio de esa segunda copia).
		[Test]
		public void ASpanInTwoUnitsSuspendsTheCostPerDistanceNamingBothUnits()
		{
			using (AScheduleOnARealJournal journal = new AScheduleOnARealJournal(TheVin, "Standard"))
			{
				AMixedUnitSeriesAndOneSpend(journal);

				Assert.That(
					journal.Api.CanCostPerDistanceBeSaid(TheYearFrom, TheYearTo), Is.False,
					"El tramo mezcla unidades, asi que no hay distancia recorrida que medir.");

				string theRate = journal.Api.DescribeRateOfUse(TheYearFrom, TheYearTo, TheLanguage.OfTheRecord);
				string theCost = journal.Api.DescribeCostPerDistanceBetween(TheYearFrom, TheYearTo, TheLanguage.OfTheRecord);

				Assert.That(theCost, Does.Contain(theRate),
					"La frase del ritmo entra ENTERA en la del costo. Comparar las dos lecturas es lo "
					+ "que hace que este renglon no sea una segunda copia de la ley del odometro.");

				Assert.That(theRate, Does.Contain("km").And.Contains("mi"),
					"Y esa frase nombra las DOS unidades, que es lo que L3 pide -- dicho por su dueño.");
				Assert.That(theCost, Does.Not.Contain("por km").And.Not.Contain("por mi"),
					"Y el costo no dice ninguna cifra por unidad: una distancia en dos escalas no es "
					+ "una distancia.");
			}
		}

		// ===== EL CRUCE: EL ANCLA VIENE DE LA AGENDA, NO DE QUIEN PREGUNTA =================

		// LA MITAD QUE EL NIVEL A NO PUEDE DAR (G2/E49). Arriba el ancla y la distancia son literales,
		// asi que la aritmetica se puede afirmar exacta y el resolvedor no se mide. Aca los tres hechos
		// del tramo salen de la SERIE que la agenda recibio, y las cifras se anclan a las lecturas
		// RECIBIDAS: si el resolvedor se corriera de la linea de observacion, la cifra cambiaria.
		[Test]
		public void TheAnchorAndTheDistanceComeFromTheSeriesTheScheduleReceived()
		{
			using (AScheduleOnARealJournal journal = new AScheduleOnARealJournal(TheVin, "Standard"))
			{
				// LA SERIE, CON LITERALES Y SIN NINGUN RELOJ (C27). La primera lectura del periodo es
				// la del 10 de enero; la ultima, la del 20 de junio. 23200 - 20000 = 3200 km.
				journal.Api.RecordReading(20000, "UnitA", TheAnchor, "Telemetry");
				journal.Api.RecordReading(
					23200, "UnitA", new DateTime(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc), "Inspection");

				// UNA LECTURA DE 2025 QUE NO ES EL ANCLA DE ESTE PERIODO: si el resolvedor tomara la
				// primera de la SERIE en vez de la primera del PERIODO, el ancla seria esta.
				journal.Api.RecordReading(
					18000, "UnitA", new DateTime(2025, 11, 1, 9, 0, 0, DateTimeKind.Utc), "Telemetry");

				journal.Api.RecordRecurringCost(
					TheAnchor, TheTyresOfLastYear, TheColon, "Llantas nuevas");
				journal.Api.RecordRecurringCost(
					TheOilChangeIncurred, TheOilChange, TheColon, "Cambio de aceite");

				Assert.That(
					journal.Api.CanCostPerDistanceBeSaid(TheYearFrom, TheYearTo), Is.True,
					"Hay tramo de uso y hay gasto: las dos mitades de la medibilidad.");

				string said = journal.Api.DescribeCostPerDistanceBetween(TheYearFrom, TheYearTo, TheLanguage.OfTheRecord);

				Assert.That(said, Does.Contain("₡89,06 por km"),
					"285000 sobre los 3200 km que la SERIE dice, con las llantas del instante del "
					+ "ancla excluidas. Ni la distancia ni el ancla se le pidieron a quien pregunta.");
				Assert.That(said, Does.Contain("2026-01-10 08:00:00"),
					"Y el ancla es la primera lectura DEL PERIODO y no la primera de la serie: la del "
					+ "2025-11-01 esta antes del limite de arranque.");
				Assert.That(said, Does.Not.Contain("2025-11-01"));
			}
		}

		// Y CUANDO NADA SE GASTO DESPUES DEL ANCLA, POR LA PUERTA, LA RAMA DEL SI SIGUE SIENDO LA QUE
		// CORRE: el tramo se pudo medir y el gasto existe, y lo que falta es gasto despues del ancla.
		[Test]
		public void AndThroughTheDoorNothingSpentAfterTheAnchorStillTakesTheThenBranch()
		{
			using (AScheduleOnARealJournal journal = new AScheduleOnARealJournal(TheVin, "Standard"))
			{
				journal.Api.RecordReading(
					20000, "UnitA", new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc), "Telemetry");
				journal.Api.RecordReading(
					23200, "UnitA", new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc), "Inspection");

				journal.Api.RecordRecurringCost(
					new DateTime(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc), TheOilChange, TheColon,
					"Cambio de aceite");

				Assert.That(journal.Api.CanCostPerDistanceBeSaid(TheYearFrom, TheYearTo), Is.True);

				string said = journal.Api.DescribeCostPerDistanceBetween(TheYearFrom, TheYearTo, TheLanguage.OfTheRecord);

				Assert.That(said, Does.Contain("no se gastó nada"));
				Assert.That(said, Does.Contain("2026-06-01 09:00:00").And.Contains("3.200 km"),
					"Y nombra el ancla y la distancia que la serie dijo.");
			}
		}

		// ===== LA MEDIBILIDAD SON DOS MITADES, Y CADA UNA TIENE QUE PODER DECIR NO SOLA ====

		// UN PERIODO CON TRAMO DE USO Y SIN NINGUN GASTO TAMPOCO PUEDE DECIRLO, y este renglon existe
		// porque una falsificacion lo pidio: quitarle el SEGUNDO operando al && de
		// CanCostPerDistanceBeSaid dejaba la suite ENTERA verde (CL-6, falsificacion 8). Todas las
		// afirmaciones sobre esa lectura estaban tomadas en estados donde las dos mitades coinciden --
		// las dos ciertas, o la del tramo falsa -- y ahi el segundo operando no discrimina nada.
		//
		// ES C60 OTRA VEZ, y es la misma forma que CL-5 encontro en su propio fixture: un discriminador
		// barato tiene que ser algo que SOLO esa mitad diga, y donde las dos coinciden por como esta
		// montado el escenario, el estado es lo que hay que mover.
		[Test]
		public void AndAPeriodWithASpanOfUseButNoCostCannotSayItEither()
		{
			using (AScheduleOnARealJournal journal = new AScheduleOnARealJournal(TheVin, "Standard"))
			{
				journal.Api.RecordReading(20000, "UnitA", TheAnchor, "Telemetry");
				journal.Api.RecordReading(
					23200, "UnitA", new DateTime(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc), "Inspection");

				Assert.That(
					journal.Api.CanARateOfUseBeSaid(TheYearFrom, TheYearTo), Is.True,
					"La mitad del TRAMO contesta que si: hay dos lecturas en una sola unidad.");
				Assert.That(
					journal.Api.HasAnyCostBetween(TheYearFrom, TheYearTo), Is.False,
					"Y la mitad del GASTO contesta que no: el libro esta vacio.");

				Assert.That(
					journal.Api.CanCostPerDistanceBeSaid(TheYearFrom, TheYearTo), Is.False,
					"Asi que el par contesta que NO. Con las dos mitades nombradas al lado, el rojo "
					+ "dice cual de las dos se cayo.");

				Assert.That(
					journal.Api.DescribeCostPerDistanceBetween(TheYearFrom, TheYearTo, TheLanguage.OfTheRecord),
					Does.Contain("no se gastó nada").And.Contains("3.200 km"),
					"Y la frase sigue siendo TOTAL: el tramo se pudo medir, asi que la rama del si es "
					+ "la que corre, y lo que falta -- el gasto -- se dice con palabras.");
			}
		}

		// ===== Y NINGUNA CIFRA SUELTA SALE DE ESTO ==========================================

		// NINGUN MIEMBRO DEL LIBRO ENTREGA EL COSTO POR DISTANCIA COMO NUMERO, ni el monto de arriba
		// suelto. Es la misma ley que L6 y L11 ya tienen, aplicada a la respuesta que este cruce
		// agrega: quien tuviera la cifra y la distancia armaria un total que el dominio no dijo.
		//
		// LA RESPUESTA YA NO ES UNA FRASE SINO UN PORTADOR, asi que lo que se prohibe se dice por lo
		// que es y no por lo que no es: una cifra. El portador entrega los DIGITOS de cada moneda
		// como texto y junto a la forma y el codigo de esa moneda, que es lo minimo con lo que se
		// puede leer; lo que no entrega -- ni el, ni el libro -- es un numero que alguien pueda
		// multiplicar por la distancia.
		[Test]
		public void AndNoMemberOfTheLedgerHandsOutTheCostPerDistanceAsANumber()
		{
			var offending = new List<string>();

			foreach (MethodInfo member in TheMembersOf(typeof(CostLedger)))
			{
				if (!member.Name.Contains("PerDistance", StringComparison.Ordinal)) continue;

				if (ItIsANumber(member.ReturnType) || member.ReturnType == typeof(Money))
					offending.Add(member.Name + " -> " + member.ReturnType.Name);
			}

			foreach (MethodInfo member in TheMembersOf(typeof(ACostPerDistanceClause)))
				if (ItIsANumber(member.ReturnType) && member.Name != "TheDistanceTravelled")
					offending.Add(member.Name + " -> " + member.ReturnType.Name);

			Assert.That(offending, Is.Empty,
				"Un miembro del costo por distancia entrega una cifra: "
				+ string.Join(", ", offending) + ". La cifra sale DENTRO de la respuesta, con la "
				+ "forma y el codigo de su moneda y su unidad de distancia al lado. La distancia "
				+ "recorrida si sale como numero y es otra cosa: es un hecho que la serie del "
				+ "odometro midio y que la frase nombra para que la division se pueda revisar.");

			Assert.That(
				typeof(ACostPerDistance).GetMethod(
					"TheCostPerUnit",
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
				Is.Null,
				"Y el tipo de la respuesta tampoco tiene un miembro que entregue la cifra sola.");
		}

		private static IEnumerable<MethodInfo> TheMembersOf(Type subject)
		{
			foreach (MethodInfo member in subject.GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
				| BindingFlags.DeclaredOnly))
				if ((member.IsPublic || member.IsAssembly) && !member.IsSpecialName) yield return member;
		}

		private static bool ItIsANumber(Type answered)
		{
			return answered == typeof(decimal) || answered == typeof(int)
				|| answered == typeof(long) || answered == typeof(decimal[])
				|| answered == typeof(int[]) || answered == typeof(long[]);
		}

		// ===== [REPLAY]: LAS DOS MITADES, Y ESTA VEZ LA DE C53 SI PUEDE DISCRIMINAR ========

		// C23: DOS CORRIDAS DE LA MISMA SECUENCIA RECIBIDA DEJAN EL MISMO SEGUNDO PROGRAMA -- el texto
		// del DSL y sus argumentos -- Y EL CRUCE CONTESTA LO MISMO. NO byte a byte: cada corrida anota
		// su propio instante de ocurrencia y tiene que diferir ahi.
		[Test]
		public void TwoRunsOfTheSameSequenceLeaveTheSameSecondProgramAndTheCrossingAnswersTheSame()
		{
			using (AScheduleOnARealJournal first = new AScheduleOnARealJournal(TheVin, "Standard"))
			{
				TheSequenceThatPutsACostOnEachSideOfTheAnchor(first);

				string firstProgram = TheSecondProgramOf(first.Folder);
				string firstAnswer = TheCrossingAnswerOf(first);

				using (AScheduleOnARealJournal second =
					new AScheduleOnARealJournal(TheVin, "Standard"))
				{
					TheSequenceThatPutsACostOnEachSideOfTheAnchor(second);

					Assert.That(TheSecondProgramOf(second.Folder), Is.EqualTo(firstProgram),
						"Las dos corridas dijeron dos programas distintos. Lo que tiene que ser "
						+ "identico es el TEXTO del DSL y sus argumentos: los dos scripts del cruce "
						+ "son consultas y no escriben nada.");

					Assert.That(TheCrossingAnswerOf(second), Is.EqualTo(firstAnswer));
				}
			}
		}

		// C53: UNA RESPUESTA VIVA CONTRA UNA REPRODUCIDA, Y ESTA SI PUEDE IR EN ROJO. La frase del
		// cruce esta hecha de INSTANTES y de una DISTANCIA, no de un decimal: el ancla es un instante
		// que el journal trunca al segundo entero, y la regla L13 es una COMPARACION contra el.
		//
		// LA COMPARACION SE TOMA JUSTO EN EL LIMITE (C61), que es lo que la hace capaz de discriminar:
		// un gasto en el MISMO segundo del ancla queda afuera y uno un segundo despues queda adentro.
		// Si el instante del ancla perdiera o ganara precision al reproducirse, la cifra cambiaria de
		// una a la otra. La comparacion de CL-5 sobre un decimal NO podia ir en rojo -- Money.Expressed
		// redondea y WireName.Currency refusa lo que no nombra -- y esta no tiene ninguna de esas dos
		// razones detras.
		[Test]
		public void TheCrossingSaysTheSameLiveAndReplayedAndTheBoundaryIsWhereTheComparisonIsTaken()
		{
			using (AScheduleOnARealJournal journal = new AScheduleOnARealJournal(TheVin, "Standard"))
			{
				TheSequenceThatPutsACostOnEachSideOfTheAnchor(journal);

				string live = TheCrossingAnswerOf(journal);

				journal.CloseAndReopenFromTheJournal();

				Assert.That(TheCrossingAnswerOf(journal), Is.EqualTo(live),
					"El cruce contesto distinto despues de reabrir el journal.");

				Assert.That(live, Does.Contain("₡50,00 por km"),
					"Y la comparacion se toma donde puede discriminar: 160000 sobre 3200 son 50.00, "
					+ "que es SOLO el gasto de un segundo DESPUES del ancla. El del mismo segundo del "
					+ "ancla quedo afuera, y si el instante del ancla se moviera aunque fuera un "
					+ "segundo, esta cifra seria otra.");
				Assert.That(live, Does.Contain("2026-06-01 09:00:00"),
					"Y el ancla se dice como el journal la guarda: al segundo entero.");
			}
		}

		// Y NINGUNA DE LAS DOS ESCRIBE UNA ENTRADA. Aca SI es byte a byte, que es la comprobacion
		// correcta para una operacion que no debe escribir NADA.
		[Test]
		public void NeitherCrossingReadWritesAnEntry()
		{
			using (AScheduleOnARealJournal journal = new AScheduleOnARealJournal(TheVin, "Standard"))
			{
				TheSequenceThatPutsACostOnEachSideOfTheAnchor(journal);

				string before = TheBytesOf(journal.Folder);

				journal.Api.CanCostPerDistanceBeSaid(TheYearFrom, TheYearTo);
				journal.Api.DescribeCostPerDistanceBetween(TheYearFrom, TheYearTo, TheLanguage.OfTheRecord);

				Assert.That(TheBytesOf(journal.Folder), Is.EqualTo(before),
					"El journal quedo byte a byte como estaba. Una lectura que escribe le agrega una "
					+ "entrada al journal de la persona cada vez que abre la app, para siempre, y el "
					+ "estado sigue siendo correcto -- asi que ninguna otra prueba se pondria roja.");
			}
		}

		// LA SECUENCIA CON UN GASTO A CADA LADO DEL ANCLA, con LITERALES y sin ningun reloj (C27). El
		// ancla es la lectura del 1 de junio a las nueve en punto; una entrada cae en ese MISMO segundo
		// y la otra un segundo despues.
		private static void TheSequenceThatPutsACostOnEachSideOfTheAnchor(
			AScheduleOnARealJournal journal)
		{
			DateTime theAnchor = new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc);

			journal.Api.RecordReading(20000, "UnitA", theAnchor, "Telemetry");
			journal.Api.RecordReading(
				23200, "UnitA", new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc), "Inspection");

			journal.Api.RecordRecurringCost(theAnchor, TheOilChange, TheColon, "Cambio de aceite");
			journal.Api.RecordRecurringCost(
				theAnchor.AddSeconds(1), TheTyresOfLastYear, TheColon, "Llantas nuevas");
		}

		// LAS DOS DEL CRUCE, CADA UNA CON SU NOMBRE AL LADO (C60), en una sola cadena comparable.
		private static string TheCrossingAnswerOf(AScheduleOnARealJournal journal)
		{
			return "CanCostPerDistanceBeSaid : "
				+ journal.Api.CanCostPerDistanceBeSaid(TheYearFrom, TheYearTo)
				+ "\nDescribeCostPerDistanceBetween : "
				+ journal.Api.DescribeCostPerDistanceBetween(TheYearFrom, TheYearTo, TheLanguage.OfTheRecord);
		}

		// El SEGUNDO PROGRAMA que el journal guarda: cada define, cada invoke y cada blob de
		// argumentos, en orden, leyendo los tramos imprimibles del binario. Copiado letra por letra del
		// que ya existe -- una variante propia compararia otra cosa y se veria igual.
		private static string TheSecondProgramOf(string folder)
		{
			StringBuilder program = new StringBuilder();

			List<string> files = new List<string>(
				Directory.GetFiles(folder, "*", SearchOption.AllDirectories));
			files.Sort(StringComparer.Ordinal);

			foreach (string file in files)
			{
				using (FileStream open = new FileStream(
					file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				{
					byte[] content = new byte[open.Length];
					open.ReadExactly(content, 0, content.Length);

					StringBuilder run = new StringBuilder();

					foreach (byte each in content)
					{
						if (each >= 0x20 && each <= 0x7E) { run.Append((char)each); continue; }

						if (run.Length >= 6) program.AppendLine(run.ToString());

						run.Clear();
					}

					if (run.Length >= 6) program.AppendLine(run.ToString());
				}
			}

			return program.ToString();
		}

		// El arbol entero del journal reducido a una cadena: nombre relativo mas hash del contenido, en
		// orden. Repetido a proposito -- cada fixture de esta suite es dueño de su aparejo.
		private static string TheBytesOf(string folder)
		{
			List<string> files = new List<string>(
				Directory.GetFiles(folder, "*", SearchOption.AllDirectories));
			files.Sort(StringComparer.Ordinal);

			StringBuilder tree = new StringBuilder();

			foreach (string file in files)
			{
				using (FileStream open = new FileStream(
					file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				{
					byte[] content = new byte[open.Length];
					open.ReadExactly(content, 0, content.Length);

					tree.Append(Path.GetRelativePath(folder, file).Replace('\\', '/'));
					tree.Append(' ');
					tree.Append(content.Length);
					tree.Append(' ');
					tree.AppendLine(Convert.ToHexString(MD5.HashData(content)));
				}
			}

			return tree.ToString();
		}

		// EL LIBRO CON LAS DOS DE COLONES: una en el instante del ancla y una despues.
		private static CostLedger ALedgerWithTheAnchorSpendAndTheOilChange()
		{
			CostLedger ledger = new CostLedger(TheVin);

			ledger.RecordRecurringCost(TheAnchor, TheTyresOfLastYear, TheColon, "Llantas nuevas");
			ledger.RecordRecurringCost(
				TheOilChangeIncurred, TheOilChange, TheColon, "Cambio de aceite");

			return ledger;
		}

		// LA SERIE EN DOS UNIDADES Y UN GASTO. Es el escenario de
		// AForeignUnitInsideThePeriodSuspendsItEvenWhenBothEndsShareAUnit: los dos extremos comparten
		// unidad y una lectura de adentro no.
		private static void AMixedUnitSeriesAndOneSpend(AScheduleOnARealJournal journal)
		{
			journal.Api.RecordReading(20000, "UnitA", TheAnchor, "Telemetry");
			journal.Api.RecordReading(
				15000, "UnitB", new DateTime(2026, 5, 15, 8, 0, 0, DateTimeKind.Utc), "HumanEntry");
			journal.Api.RecordReading(
				23200, "UnitA", new DateTime(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc), "Inspection");

			journal.Api.RecordRecurringCost(
				TheOilChangeIncurred, TheOilChange, TheColon, "Cambio de aceite");
		}

		// LA CIFRA DE COLONES POR UNIDAD QUE LA FRASE DICE, sin su signo. Es aritmetica de la PRUEBA y
		// no de una caratula: lo que la regla 3 prohibe es que una caratula divida dos valores del
		// dominio, y lo que L13 pide es exactamente esta comparacion entre dos respuestas.
		private static decimal TheColonesPerUnitInside(string said)
		{
			int opens = said.IndexOf('₡');

			Assert.That(opens, Is.GreaterThanOrEqualTo(0), "La frase no dice ninguna cifra en colones.");

			int ends = opens + 1;

			while (ends < said.Length && (char.IsDigit(said[ends]) || said[ends] == '.')) ends++;

			return decimal.Parse(
				said.Substring(opens + 1, ends - opens - 1), NumberStyles.Number,
				CultureInfo.InvariantCulture);
		}
	}
}
