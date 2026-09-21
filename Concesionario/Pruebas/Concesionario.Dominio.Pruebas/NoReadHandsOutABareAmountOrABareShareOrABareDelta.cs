using System;
using System.Collections.Generic;
using System.Reflection;
using Concesionario.App;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LA LEY CENTRAL DEL SLICE, EN LA PUERTA. El dominio compone las DOS vistas de un periodo en una
	// sola respuesta (L6), reparte lo duradero adentro (L7) y dice dos periodos juntos sin ninguna
	// diferencia suelta (L11). El atajo es una lectura que entregue un monto, una parte o una
	// diferencia como numero y deje que la pantalla arme la frase, y esa lectura se ve exactamente
	// igual de razonable que las siete que si estan.
	//
	// POR QUE ESTE FIXTURE EXISTE APARTE DE LOS TRES DEL DOMINIO. CL-3 construyo tres instrumentos
	// sobre ASpendSummary y sobre CostLedger -- que ningun miembro expuesto devuelva un Money, que el
	// resumen conteste exactamente UNA cosa, y que las lecturas del agregado que dicen "Spend"
	// traigan las DOS cifras. LA PUERTA ES OTRA SUPERFICIE: es un ensamblado distinto, con miembros
	// distintos y con un DTO propio, y los tres instrumentos del dominio no la miran.
	//
	// SE AFIRMA POR TRES MITADES Y HACEN FALTA LAS TRES:
	//
	//   POR TIPO DE RETORNO: las SIETE lecturas del libro contestan exactamente lo que §7.2 dice, y
	//   una sola de las siete contesta con un NUMERO -- una cuenta de filas, que es una respuesta
	//   entera. Ninguna contesta un decimal, ninguna una coleccion pelada de numeros.
	//
	//   POR NOMBRE: ningun miembro de la puerta -- ni ninguna columna del registro -- se llama por un
	//   monto suelto, por una parte suelta o por una diferencia suelta. Por ENUMERACION contra la
	//   reflexion, asi que una lectura nueva se pone roja con CUALQUIER nombre de esa familia.
	//
	//   POR VALOR: con tres entradas puestas en montos que no se pueden confundir con nada,
	//   NINGUNA de las preguntas de la puerta que contesta con un numero contesta ninguno de ellos,
	//   ni la parte amortizada, ni la diferencia entre los dos periodos. El censo prueba que nadie
	//   agrego un miembro sin escribirlo; esto prueba que ninguno de los que hay entrega media
	//   respuesta.
	//
	// LOS MONTOS SON CENTINELAS Y ESTAN ELEGIDOS PARA QUE NO SE CRUCEN CON NADA (C60). Un 240000 o un
	// 45000 se podrian cruzar con un odometro o con un limite por casualidad; 777777, 131313 y 909091
	// no se cruzan con nada de lo que esta puerta contesta.
	[TestFixture]
	public sealed class NoReadHandsOutABareAmountOrABareShareOrABareDelta
	{
		private const string TheVin = "5YJ3E1EA7JF000316";

		// LAS SIETE LECTURAS DEL LIBRO Y LO QUE CADA UNA CONTESTA, como mapa exacto. Un monto suelto,
		// una parte suelta o una diferencia suelta tendrian que aparecer aca, y aparecer aca es
		// escribirlo.
		private static readonly Dictionary<string, Type> TheSevenReadsOfTheLedger =
			new Dictionary<string, Type>
			{
				{ "HasAnyCostBetween", typeof(bool) },
				{ "DescribeSpendBetween", typeof(string) },
				{ "IsSpendSaidInOneCurrencyBetween", typeof(bool) },
				{ "DescribeSpendPerCurrencyBetween", typeof(string) },
				{ "DescribeTwoPeriodsOfSpendCompared", typeof(string) },
				{ "CostEntriesBetween", typeof(CostLedgerRecord) },
				{ "CostEntryCountBetween", typeof(int) },
			};

		// COMO SE LLAMARIA UN MONTO SUELTO, UNA PARTE SUELTA O UNA DIFERENCIA SUELTA, EN CUALQUIER
		// GRAFIA. Los primeros son las tres mitades que este slice prohibe; los ultimos son la
		// familia con la que una capa arma una frase que el dominio nunca dijo.
		private static readonly string[] TheNamesTheDoorMayNotCarry =
		{
			"Amount", "CashOut", "Consumed", "TotalSpent", "SpendTotal",
			"Share", "Amortized", "Prorated",
			"Delta", "Difference", "PercentChange", "HowItCompares",
			"Average", "Mean", "Trend", "Slope", "Projection", "Forecast", "Estimate",
		};

		// LOS CENTINELAS. Ninguno se cruza con ningun conteo, ningun odometro ni ningun resto de esta
		// puerta, ni entre si.
		private const decimal TheTyresCost = 777777m;
		private const decimal TheOilChangeCost = 131313m;
		private const decimal TheWashCost = 909091m;

		private const string TheColon = "CRC";
		private const int FourYearsOfLife = 48;

		// LO QUE HAY QUE MONTAR PARA QUE EL BARRIDO POR VALOR PUEDA CORRER, y montarlo es parte de la
		// prueba y no una comodidad. La mitad POR VALOR de este fixture INVOCA cada pregunta de la
		// puerta que contesta con un numero, y tres de ellas son GUARDADAS -- el odometro de la
		// entrega, y las dos restas de una operacion agendada -- asi que sobre un registro sin
		// entrega y sin operacion el barrido se cae con una excepcion en vez de con una afirmacion.
		//
		// SALTARSE LOS MIEMBROS GUARDADOS ERA LA OTRA SALIDA Y ES C61 EXACTAMENTE: tomar la
		// comparacion justo en el estado donde los miembros que podrian entregar una mitad no
		// contestan nada. Es la misma pared que MS-4 encontro y resolvio igual.
		//
		// LOS VALORES ESTAN ELEGIDOS PARA NO CRUZARSE CON NINGUN CENTINELA: los dos odometros son
		// 30000 y 35000, los dos limites de la operacion dan restos de 7345 y 255, y ninguno de esos
		// cinco es 777777, 131313, 909091, ni su suma, ni su diferencia.
		private const int TheOdometerAtDelivery = 30000;
		private const int TheOdometerToday = 35000;
		private const int TheOperationsDayLimit = 500;
		private const int TheOperationsDistanceLimit = 12345;
		private const string TheUnitEverythingIsIn = "UnitA";

		private static readonly DateTime TheDayItWasHandedOver =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItWasObserved =
			new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheYearFrom =
			new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheYearTo =
			new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);

		private static readonly DateTime TheHalfFrom =
			new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheHalfTo =
			new DateTime(2026, 6, 30, 23, 59, 59, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.RecordDelivery(
				TheDayItWasHandedOver, TheOdometerAtDelivery, TheUnitEverythingIsIn);
			journal.Api.RecordReading(
				TheOdometerToday, TheUnitEverythingIsIn, TheDayItWasObserved, "Telemetry");
			journal.Api.SeedPlanOperation(
				"FluidChange", "TimeOrDistance",
				TheOperationsDayLimit, TheOperationsDistanceLimit, TheUnitEverythingIsIn, true, false,
				TheDayItWasHandedOver, TheOdometerAtDelivery, TheUnitEverythingIsIn);

			journal.Api.RecordDurableCost(
				new DateTime(2026, 3, 12, 9, 0, 0, DateTimeKind.Utc), TheTyresCost, TheColon,
				"Llantas nuevas", FourYearsOfLife);

			journal.Api.RecordRecurringCost(
				new DateTime(2026, 5, 20, 10, 30, 0, DateTimeKind.Utc), TheOilChangeCost, TheColon,
				"Cambio de aceite");

			journal.Api.RecordRecurringCost(
				new DateTime(2026, 8, 5, 14, 15, 0, DateTimeKind.Utc), TheWashCost, TheColon,
				"Lavado premium");
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// ===== POR TIPO DE RETORNO =======================================================

		[Test]
		public void TheSevenReadsOfTheLedgerAnswerExactlyTheseTypesAndOnlyOneOfThemAnswersWithANumber()
		{
			foreach (KeyValuePair<string, Type> each in TheSevenReadsOfTheLedger)
				Assert.That(TheDoorsMember(each.Key).ReturnType, Is.EqualTo(each.Value),
					"'" + each.Key + "' dejo de contestar lo que §7.2 dice que contesta.");

			var answeringWithANumber = new List<string>();

			foreach (KeyValuePair<string, Type> each in TheSevenReadsOfTheLedger)
				if (each.Value == typeof(int) || each.Value == typeof(long)
					|| each.Value == typeof(decimal))
					answeringWithANumber.Add(each.Key);

			Assert.That(answeringWithANumber, Is.EqualTo(new[] { "CostEntryCountBetween" }),
				"Una sola de las siete contesta con un NUMERO, y es una CUENTA DE FILAS: una cuenta "
				+ "es una respuesta entera, no le falta ni moneda ni instante ni clase. Cualquier "
				+ "otra de las siete que empezara a contestar con un numero seria un monto, una parte "
				+ "o una diferencia -- las tres son mitades, y una mitad ya le llega a quien pregunta "
				+ "adentro de la frase.");
		}

		// Y NINGUNA CONTESTA UN DECIMAL NI UNA COLECCION PELADA DE NUMEROS. Sin este renglon, el de
		// arriba se cumpliria igual si una de las siete pasara a devolver un decimal[] de montos: no
		// seria un int y el mapa exacto seguiria diciendo lo que dice.
		[Test]
		public void NoneOfTheSevenAnswersWithADecimalOrABareCollectionOfNumbers()
		{
			foreach (KeyValuePair<string, Type> each in TheSevenReadsOfTheLedger)
			{
				Type answered = TheDoorsMember(each.Key).ReturnType;

				foreach (Type banned in new[]
				{
					typeof(decimal), typeof(decimal[]), typeof(int[]), typeof(long[]),
				})
					Assert.That(answered, Is.Not.EqualTo(banned),
						"'" + each.Key + "' contesta un numero pelado o una coleccion de numeros "
						+ "pelados. Los montos del libro salen DICHOS por el dominio, con el signo y "
						+ "los decimales de su moneda adentro.");
			}
		}

		// ===== POR NOMBRE ================================================================

		[Test]
		public void NoMemberOfTheDoorIsNamedForABareAmountABareShareOrABareDelta()
		{
			var offending = new List<string>();

			foreach (MethodInfo each in TheDoorsMembers())
				foreach (string banned in TheNamesTheDoorMayNotCarry)
					if (each.Name.Contains(banned, StringComparison.OrdinalIgnoreCase))
						offending.Add(each.Name + " lleva '" + banned + "'");

			Assert.That(offending, Is.Empty,
				"La puerta gano un miembro llamado por un monto suelto, por una parte suelta o por "
				+ "una diferencia suelta. Lo que sale son LAS DOS VISTAS EN UNA RESPUESTA y las filas "
				+ "aparte: ninguna de las dos es la mitad de la otra.");
		}

		// Y NINGUNA COLUMNA DEL REGISTRO TAMPOCO. Una columna es texto y el censo numerico no la
		// puede ver (R4), asi que un monto suelto se colaria por ahi sin que nada lo notara.
		[Test]
		public void NoColumnOfTheLedgerRecordIsNamedForABareAmountOrABareShareEither()
		{
			foreach (PropertyInfo column in typeof(CostLedgerRecord).GetProperties(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
				foreach (string banned in TheNamesTheDoorMayNotCarry)
					Assert.That(
						column.Name.Contains(banned, StringComparison.OrdinalIgnoreCase), Is.False,
						$"La columna '{column.Name}' del registro del libro lleva un monto suelto, una "
						+ "parte suelta o una diferencia. Una fila es COMPLETA: dinero con su moneda, "
						+ "asunto, instante y clase.");
		}

		// Y NINGUNA COLUMNA ES UN NUMERO. El registro de la serie tiene una columna int[] porque un
		// valor del odometro es un hecho recibido; un monto NO lo es: sin su moneda no se puede leer,
		// y la moneda va adentro de como el dominio lo DICE.
		[Test]
		public void EveryColumnOfTheLedgerRecordIsTextAndNoneIsANumber()
		{
			var columns = new List<string>();

			foreach (PropertyInfo each in typeof(CostLedgerRecord).GetProperties(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
			{
				columns.Add(each.Name);

				Assert.That(each.PropertyType, Is.EqualTo(typeof(string[])),
					"La columna '" + each.Name + "' dejo de ser texto. Un monto sin su moneda no se "
					+ "puede leer, y la moneda va adentro de como el dominio lo dice.");
			}

			Assert.That(
				columns,
				Is.EquivalentTo(new[]
				{
					"IncurredAt", "Subject", "Money", "Classification", "ExpectedLife", "Says",
				}),
				"El registro gano o perdio una columna. NO HAY COLUMNA DE PARTE AMORTIZADA: una parte "
				+ "por fila es la mitad de la respuesta de L6 con una tabla encima, y CL-3 afirma su "
				+ "ausencia del lado del dominio. NO HAY COLUMNA DE ID NI LUGAR DE LA ENTRADA: una "
				+ "entrada del libro no tiene clave, porque no hay comando que la corrija (DEC-94).");
		}

		// ===== POR VALOR =================================================================

		// CON LOS TRES CENTINELAS EN EL LIBRO, NINGUNA PREGUNTA DE LA PUERTA QUE CONTESTE CON UN
		// NUMERO CONTESTA NINGUNO DE ELLOS. Ni el monto entero, ni la caja del periodo, ni la
		// diferencia entre los dos.
		[Test]
		public void NotOneNumberTheDoorHandsOutIsAnAmountASumOrADifference()
		{
			var forbidden = new Dictionary<string, long>
			{
				{ "el monto de las llantas", (long)TheTyresCost },
				{ "el monto del cambio de aceite", (long)TheOilChangeCost },
				{ "el monto del lavado", (long)TheWashCost },
				{ "la caja del semestre", (long)(TheTyresCost + TheOilChangeCost) },
				{ "la caja del anio", (long)(TheTyresCost + TheOilChangeCost + TheWashCost) },
				{
					"la diferencia entre los dos periodos",
					(long)(TheTyresCost + TheOilChangeCost + TheWashCost
						- (TheTyresCost + TheOilChangeCost))
				},
			};

			foreach (KeyValuePair<string, long> answered in EveryNumberAnsweredRightNow())
				foreach (KeyValuePair<string, long> each in forbidden)
					Assert.That(answered.Value, Is.Not.EqualTo(each.Value),
						answered.Key + " contesto " + each.Key + ".");
		}

		// Y LOS MONTOS SI LLEGAN A QUIEN PREGUNTA -- dentro de la frase, que es el otro lado de la
		// misma ley. Sin este renglon, el de arriba se cumpliria tambien si la puerta no dijera los
		// montos en ninguna parte, que seria un defecto distinto y peor.
		[Test]
		public void TheAmountsAndTheShareReachTheCallerInsideTheSentence()
		{
			string said = journal.Api.DescribeSpendBetween(TheHalfFrom, TheHalfTo, TheLanguage.OfTheRecord);

			Assert.That(said, Does.Contain("₡777.777,00"), "El monto entero de la duradera.");
			Assert.That(said, Does.Contain("₡909.090,00"),
				"Y la caja del semestre, sumada por el DOMINIO: 777777 mas 131313.");
			Assert.That(said, Does.Contain("a este período le tocan ₡"),
				"Y la PARTE amortizada, adentro de la clausula que dice la regla con que se saco. "
				+ "Suelta no sale por ninguna lectura; dicha, sale con el entero, la vida util y el "
				+ "instante de anclaje al lado.");
		}

		// Y NINGUNA COLUMNA DEL REGISTRO TRAE UN CENTINELA PELADO. La columna Money los trae DICHOS
		// -- con el signo y los decimales de su moneda -- que es distinto de traerlos como el numero
		// que un caller podria sumar sin volver a parsear.
		[Test]
		public void NoColumnOfTheRecordHandsOutTheAmountAsABareNumber()
		{
			CostLedgerRecord record = journal.Api.CostEntriesBetween(TheYearFrom, TheYearTo, TheLanguage.OfTheRecord);

			foreach (string cell in record.Money)
				Assert.That(cell, Does.StartWith("₡"),
					"La celda '" + cell + "' salio sin el signo de su moneda. Un monto pelado en una "
					+ "columna es la mitad que el censo numerico NO puede ver (R4), y es por eso que "
					+ "esta se afirma aca.");

			foreach (string cell in record.Money)
				Assert.That(cell, Is.Not.EqualTo("777777").And.Not.EqualTo("131313")
					.And.Not.EqualTo("909091"));
		}

		// Cada pregunta de la puerta que contesta con un numero, invocada contra este libro. Los
		// argumentos se arman por TIPO y no por nombre: es lo que hace que una lectura nueva entre al
		// barrido sola en vez de esperar a que alguien la agregue.
		private List<KeyValuePair<string, long>> EveryNumberAnsweredRightNow()
		{
			var answered = new List<KeyValuePair<string, long>>();

			foreach (MethodInfo each in TheDoorsMembers())
			{
				if (each.ReturnType != typeof(int) && each.ReturnType != typeof(long)) continue;

				answered.Add(new KeyValuePair<string, long>(
					each.Name, Convert.ToInt64(Invoke(each))));
			}

			Assert.That(answered, Is.Not.Empty,
				"El barrido dejo de encontrar las preguntas que contestan con un numero.");

			return answered;
		}

		private object Invoke(MethodInfo member)
		{
			var arguments = new List<object>();

			foreach (ParameterInfo each in member.GetParameters())
			{
				if (each.ParameterType == typeof(DateTime))
				{
					arguments.Add(each.Name == "from" || each.Name == "from1" || each.Name == "from2"
						? TheYearFrom
						: TheYearTo);
					continue;
				}

				if (each.Name == "fromDay") { arguments.Add("2026-01-01"); continue; }
				if (each.Name == "toDay") { arguments.Add("2026-12-31"); continue; }

				if (each.ParameterType == typeof(string)) { arguments.Add("FluidChange"); continue; }

				Assert.Fail(
					"La pregunta '" + member.Name + "' toma un " + each.ParameterType.Name
					+ ", que este barrido no sabe armar. Se arma por TIPO a proposito; agregue el "
					+ "tipo nuevo y diga con que valor se pregunta.");
			}

			try
			{
				return member.Invoke(journal.Api, arguments.ToArray());
			}
			catch (TargetInvocationException wrapped)
			{
				throw wrapped.InnerException;
			}
		}

		private static MethodInfo[] TheDoorsMembers()
		{
			return typeof(DealershipCarAPI).GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
		}

		// EL MIEMBRO, Y REVIENTA SI HAY DOS CON ESE NOMBRE. Devolver el primero seria afirmar sobre
		// 'el' miembro eligiendo uno y no mirando el otro.
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
