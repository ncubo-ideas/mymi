using System;
using System.Collections.Generic;
using System.Reflection;
using Concesionario.App;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LA LEY L1 DE LA PARTE A, Y ES LA MITAD QUE EL CENSO NUMERICO NO DA. Ese censo dice que la puerta
	// no gano ni perdio una pregunta que contesta con un numero; este dice que ninguno de los numeros
	// que si contesta es LO RECORRIDO ni EL LIMITE YA ESCALADO -- los dos operandos de la resta.
	//
	// JUNTOS SON UNA MITAD-PAR: quien tenga los dos arma la resta y ademas un porcentaje del
	// intervalo, y esas dos frases el dominio no las dijo nunca. Lo que si sale es el RESTO, que esta
	// completo y con el que no hay nada que combinar.
	//
	// SE AFIRMA POR DOS MITADES Y HACEN FALTA LAS DOS:
	//
	//   POR NOMBRE: ningun miembro de la puerta se llama por ninguno de los siete nombres prohibidos,
	//   en ninguna grafia. Por ENUMERACION contra la reflexion y no por un grep, que no sabe si el
	//   nombre que encontro es de la puerta.
	//
	//   POR VALOR: con los dos operandos siendo numeros que nada mas de este fixture puede contestar,
	//   ninguna de las preguntas de la puerta que contesta con un numero contesta ninguno de los dos.
	//   El nombre solo prueba que nadie los llamo asi; el valor prueba que nadie los entrega.
	//
	// Y LA TERCERA PRUEBA ES LA QUE IMPIDE QUE LAS DOS PASEN POR LA RAZON EQUIVOCADA: los dos
	// operandos EXISTEN en este momento y llegan a quien pregunta DENTRO DE LA FRASE del dominio. Sin
	// ese renglon, las dos de arriba se cumplirian igual con una agenda que no midiera nada.
	[TestFixture]
	public sealed class TravelledAndEffectiveLimitNeverLeaveTheDoorInAnySpelling
	{
		private const string TheVin = "KMHD35LH5EU000004";
		private const string TheKind = "FluidChange";
		private const string TheUnitEverythingIsIn = "UnitA";

		// LOS DOS OPERANDOS, elegidos para que nada mas de este fixture pueda contestarlos (C60).
		private const int TheDistanceTravelledNothingElseCouldAnswer = 7353;
		private const int TheScaledDistanceLimitNothingElseCouldAnswer = 41999;
		private const int TheDaysElapsedNothingElseCouldAnswer = 245;
		private const int TheScaledDayLimitNothingElseCouldAnswer = 909;

		private const int TheOdometerAtTheAnchor = 20222;
		private const int TheOdometerObservedLater = 27575;

		private static readonly DateTime TheAnchor =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItWasObserved =
			new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItIsAsked =
			new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

		// LOS SIETE NOMBRES QUE EL CANON PROHIBE EN LA PUERTA, EN CUALQUIER GRAFIA. Los cuatro
		// primeros son los operandos; los tres ultimos son lo que quien los tuviera compondria.
		private static readonly string[] TheNamesTheDoorMayNotCarry =
		{
			"Travelled", "ElapsedDays", "EffectiveLimit", "EffectiveLimitDays",
			"Progress", "PercentComplete", "Advancement",
		};

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.SeedPlanOperation(
				TheKind, "TimeOrDistance",
				TheScaledDayLimitNothingElseCouldAnswer,
				TheScaledDistanceLimitNothingElseCouldAnswer,
				TheUnitEverythingIsIn, true, false,
				TheAnchor, TheOdometerAtTheAnchor, TheUnitEverythingIsIn);

			journal.Api.RecordDelivery(TheAnchor, TheOdometerAtTheAnchor, TheUnitEverythingIsIn);

			journal.Api.RecordReading(
				TheOdometerObservedLater, TheUnitEverythingIsIn, TheDayItWasObserved, "Telemetry");

			journal.Api.Evaluate(TheDayItIsAsked);
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// ===== POR NOMBRE ================================================================

		[Test]
		public void NoMemberOfTheDoorIsNamedForEitherOperand()
		{
			var offending = new List<string>();

			foreach (MethodInfo each in TheDoorsMembers())
				foreach (string banned in TheNamesTheDoorMayNotCarry)
					if (each.Name.Contains(banned, StringComparison.OrdinalIgnoreCase))
						offending.Add(each.Name);

			Assert.That(offending, Is.Empty,
				"La puerta gano un miembro llamado por uno de los operandos de la resta, o por lo que "
				+ "quien los tuviera compondria. Lo que sale es el RESTO, compuesto adentro y con su "
				+ "unidad al lado; los dos operandos llegan a quien pregunta dentro de la frase.");
		}

		// Y NINGUNA COLUMNA DE NINGUN REGISTRO TAMPOCO: un registro es donde un operando se colaria sin
		// que el censo numerico lo viera, porque una columna es texto.
		[Test]
		public void NoColumnOfAnyRecordIsNamedForEitherOperandEither()
		{
			foreach (Type record in new[]
			{
				typeof(CompletionRecord), typeof(EveryKindCompletionRecord),
				typeof(PublishedPriceRecord), typeof(ShopStayRecord),
				typeof(OdometerSeriesRecord),
			})
				foreach (PropertyInfo column in record.GetProperties(
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
					foreach (string banned in TheNamesTheDoorMayNotCarry)
						Assert.That(
							column.Name.Contains(banned, StringComparison.OrdinalIgnoreCase), Is.False,
							$"La columna '{column.Name}' de {record.Name} lleva un operando. Un registro "
							+ "es texto y el censo numerico no lo puede ver.");
		}

		// ===== POR VALOR =================================================================

		[Test]
		public void NoNumberTheDoorHandsOutIsTheTravelledDistanceOrTheScaledLimit()
		{
			foreach (KeyValuePair<string, long> answered in EveryNumberAnsweredRightNow())
			{
				Assert.That(answered.Value, Is.Not.EqualTo(TheDistanceTravelledNothingElseCouldAnswer),
					answered.Key + " contesto LO RECORRIDO.");
				Assert.That(
					answered.Value, Is.Not.EqualTo(TheScaledDistanceLimitNothingElseCouldAnswer),
					answered.Key + " contesto EL LIMITE de distancia ya escalado.");
				Assert.That(answered.Value, Is.Not.EqualTo(TheDaysElapsedNothingElseCouldAnswer),
					answered.Key + " contesto LOS DIAS TRANSCURRIDOS.");
				Assert.That(answered.Value, Is.Not.EqualTo(TheScaledDayLimitNothingElseCouldAnswer),
					answered.Key + " contesto EL PLAZO ya escalado.");
			}
		}

		// ===== Y LOS DOS OPERANDOS EXISTEN AHORA MISMO ===================================

		// Sin esto, las dos de arriba se cumplirian tambien con una agenda que no midiera nada -- que
		// seria un defecto distinto y peor. Los dos llegan a quien pregunta DENTRO de la frase, que es
		// el otro lado de la misma ley.
		[Test]
		public void BothOperandsAreRealRightNowAndReachTheCallerInsideTheSentence()
		{
			Assert.That(
				journal.Api.ReadDueReason(TheKind, TheLanguage.OfTheRecord),
				Does.Contain("7.353 de 41.999 km"),
				"Los dos operandos tienen que estar VIVOS para que no entregarlos signifique algo.");

			// Y LO QUE SI SALE COMO NUMERO ES EL RESTO, que es la diferencia de esos dos y esta
			// completo: 41999 menos 7353, con su unidad al lado y su situacion al lado.
			Assert.That(
				journal.Api.ReadDistanceRemaining(TheKind, TheDayItIsAsked), Is.EqualTo(34646));
			Assert.That(
				journal.Api.ReadDistanceRemainingUnit(TheKind, TheDayItIsAsked), Is.EqualTo("km"));
			Assert.That(
				journal.Api.ReadDistanceRemainderStanding(TheKind, TheDayItIsAsked),
				Is.EqualTo("StillAhead"));

			// Lo mismo del lado del tiempo: 909 menos 245.
			Assert.That(journal.Api.ReadDaysRemaining(TheKind, TheDayItIsAsked), Is.EqualTo(664));
		}

		// ===== EL BARRIDO ================================================================

		// Cada pregunta de la puerta que contesta con un numero, invocada contra este registro. Los
		// argumentos se arman por TIPO, con las mismas dos excepciones de nombre que el censo numerico
		// escribe: un limite de periodo es una cadena que la puerta exige que DIGA UN DIA.
		private List<KeyValuePair<string, long>> EveryNumberAnsweredRightNow()
		{
			var answered = new List<KeyValuePair<string, long>>();

			foreach (MethodInfo each in TheDoorsMembers())
			{
				if (each.ReturnType != typeof(int) && each.ReturnType != typeof(long)) continue;

				answered.Add(new KeyValuePair<string, long>(
					each.Name, Convert.ToInt64(Invoke(each))));
			}

			Assert.That(answered, Is.Not.Empty, "El barrido dejo de encontrar la puerta.");

			return answered;
		}

		private object Invoke(MethodInfo member)
		{
			var arguments = new List<object>();

			foreach (ParameterInfo each in member.GetParameters())
			{
				if (each.ParameterType == typeof(DateTime)) { arguments.Add(TheDayItIsAsked); continue; }

				if (each.Name == "fromDay") { arguments.Add("2026-01-01"); continue; }
				if (each.Name == "toDay") { arguments.Add("2026-12-31"); continue; }

				if (each.ParameterType == typeof(string)) { arguments.Add(TheKind); continue; }

				Assert.Fail(
					"La pregunta '" + member.Name + "' toma un " + each.ParameterType.Name
					+ ", que este barrido no sabe armar. Se arma por TIPO a proposito; agregue el tipo "
					+ "nuevo y diga con que valor se pregunta.");
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
	}
}
