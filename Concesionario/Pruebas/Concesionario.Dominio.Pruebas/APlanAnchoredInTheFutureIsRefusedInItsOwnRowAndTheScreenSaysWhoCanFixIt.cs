using System;
using System.Collections.Generic;
using System.IO;
using Concesionario.App.Views;
using Concesionario.Wasm;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// EL RECHAZO QUE NINGUNA CARATULA PUEDE PRE-CHEQUEAR, EN UNA PANTALLA, Y ES LA TERCERA VEZ QUE HAY
	// QUE DECIDIR QUE HACER CON EL. Las diez lecturas de la cuenta atras heredan una guarda del dominio
	// -- no se evalua un instante anterior al ancla de la ocurrencia -- que la puerta TRADUCE a un tipo
	// que una caratula puede nombrar y NO puede pre-chequear, porque ninguna lectura de la puerta
	// contesta cual es esa ancla.
	//
	// Y LLEGA SIN QUE NADIE MONTE NADA: SeedPlanOperation toma el ancla de la agencia y ninguna caratula
	// lee un reloj para eso, asi que un plan anclado ADELANTE de hoy deja el AHORA de cualquier pantalla
	// antes del ancla. Es una accion ordinaria de una agencia, no un defecto de quien llama.
	//
	// LAS DOS RESPUESTAS QUE YA EXISTEN NO SIRVEN TAL CUAL, y por eso esta es una decision y no una
	// copia:
	//
	//   LA CONSOLA pierde las lineas de esa clase y quien lee el tablero VE el hueco, porque el tablero
	//   es un censo de nueve impreso a la vez. Una pantalla no tiene ese censo al lado: las filas SON
	//   las nueve de la agenda, que ya estaban ahi por otro motivo, y una fila sin su linea de cuenta
	//   atras se lee igual que una que no tiene ninguna que mostrar.
	//
	//   LA CARATULA MCP nombra el rechazo en un CAMPO, porque una fila de JSON con cadenas vacias se le
	//   lee a un modelo como "no hay datos". Un campo es la mitad correcta; la otra mitad no transfiere,
	//   porque una persona no lee campos con nombre. Y el mensaje de la puerta NO ES PROSA PARA UNA
	//   PERSONA: nombra la clase por el nombre del canon y el instante con siete decimales.
	//
	// LO QUE ESTA PANTALLA HACE: atrapa POR CLASE -- que es posible porque el portador es por clase --,
	// lleva el mensaje de la puerta como CONSTANCIA sin pintarlo, y pinta en su lugar UNA linea propia
	// que nombra un ROL y ningun hecho del carro. Un rechazo sin atrapar aqui es el banner global de
	// Blazor encima de una pagina que por lo demas esta completa y correcta.
	[TestFixture]
	public sealed class APlanAnchoredInTheFutureIsRefusedInItsOwnRowAndTheScreenSaysWhoCanFixIt
	{
		private const string TheVin = "1G1YY22G5X5000011";
		private const string TheUnitEverythingIsIn = "UnitA";

		private const string TheKindAnchoredAhead = "Inspection";
		private const string TheKindThatAnswers = "FluidChange";

		private static readonly DateTime AnAnchorEveryFutureRunIsStillBefore =
			new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheAnchorThatAnswers =
			new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayTheReadingWasObserved =
			new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheInstantTheScreenAsksAt =
			new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

		private const int TheOdometerAtTheAnchor = 21000;
		private const int TheOdometerObserved = 35000;

		private const int TheDistanceLimitNothingElseCouldAnswer = 21345;
		private const int TheDayLimitNothingElseCouldAnswer = 620;

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, AppState.TheProfileAtDelivery);
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// ===== POR CLASE, Y LAS OTRAS OCHO CONTESTAN ======================================

		[Test]
		public void ThePlanAnchoredAheadIsRefusedInItsOwnRowAndTheOtherEightStillAnswer()
		{
			APlanAnchoredAheadBesideOneThatAnswers();

			WhereEachOperationStands handed = WhatTheScreenIsHanded();

			Assert.That(handed.SomeCountdownWasRefused, Is.True,
				"El portador no dice que hubo un rechazo, asi que la pantalla no tiene con que decir "
				+ "quien puede corregir el hecho de atras.");

			HowOneOperationStands refused = handed.Of(TheKindAnchoredAhead);

			Assert.That(refused.CountdownRefused, Is.True);
			Assert.That(refused.Says, Is.Empty,
				"Cuando la agenda rechaza, la frase llega vacia -- por eso la bandera se lee ANTES que "
				+ "la frase.");
			Assert.That(refused.DistanceStanding, Is.Empty);
			Assert.That(refused.TimeStanding, Is.Empty);

			// Y LAS OTRAS OCHO NO PIERDEN NADA. Un catch alrededor de la composicion entera perderia
			// ocho por una, que es lo que esta afirmacion existe para prohibir.
			foreach (HowOneOperationStands each in handed.Operations)
				if (each.Kind != TheKindAnchoredAhead)
					Assert.That(each.CountdownRefused, Is.False,
						"'" + each.Kind + "' quedo marcada como rechazada. El rechazo es de UNA clase.");

			HowOneOperationStands answers = handed.Of(TheKindThatAnswers);

			Assert.That(answers.Says, Does.Contain("distancia: te faltan 7345 km"));
			Assert.That(answers.DistanceRemaining, Is.EqualTo(7345));
			Assert.That(answers.DaysRemaining, Is.EqualTo(255));
		}

		// EL CONTROL, y sin el lo de arriba pasaria con un portador que marcara todo como rechazado.
		[Test]
		public void AndWithNoAnchorAheadNoRowIsRefusedAndTheScreenSaysNothingAboutIt()
		{
			AnOperationWithAReadingAfterIt();

			WhereEachOperationStands handed = WhatTheScreenIsHanded();

			Assert.That(handed.SomeCountdownWasRefused, Is.False,
				"El portador dice que hubo un rechazo donde no hubo ninguno, y la pantalla diria que hay "
				+ "una fecha que revisar cuando no hay ninguna.");

			foreach (HowOneOperationStands each in handed.Operations)
			{
				Assert.That(each.CountdownRefused, Is.False);
				Assert.That(each.Says, Is.Not.Empty,
					"'" + each.Kind + "' llego sin frase y sin estar rechazada. Las siete situaciones "
					+ "dicen algo, incluida la de una clase que no esta agendada.");
			}
		}

		// LA MITAD DEL HISTORIAL SE PIDE AFUERA DEL try, Y ESO ES UNA DECISION. Lo que ya se le hizo a
		// esa operacion el registro lo contesta perfectamente: no depende de ningun instante y no hereda
		// ninguna guarda. Con las dos mitades adentro, una fecha de arranque puesta adelante se llevaria
		// tambien el historial de esa clase, que no tiene nada que ver.
		[Test]
		public void AndTheRefusedRowStillSaysWhatWasDoneToThatOperation()
		{
			APlanAnchoredAheadBesideOneThatAnswers();

			HowOneOperationStands refused = WhatTheScreenIsHanded().Of(TheKindAnchoredAhead);

			Assert.That(refused.CountdownRefused, Is.True);

			Assert.That(refused.CompletionsSays, Is.Not.Empty,
				"La fila rechazada perdio el historial de su clase. La parte B no toma ningun instante y "
				+ "no hereda la guarda: pedirla adentro del try la castiga por algo que no la afecta.");

			Assert.That(
				refused.CompletionsSays,
				Is.EqualTo(journal.Api.DescribeCompletions(TheKindAnchoredAhead)),
				"Y es la frase de la puerta letra por letra, de ESA clase.");

			Assert.That(refused.HasBeenCompleted, Is.False);
		}

		// EL DISCRIMINADOR DEL PRE-CHEQUEO QUE FALTA, y hace falta escrito porque el catch lo esconde.
		// En este registro la mitad de distancia NO se puede decir -- no hay ninguna lectura -- y la de
		// tiempo SI. Si alguien pide el monto de distancia afuera de la rama que su pre-chequeo abre, el
		// catch por clase convierte la fila entera en un rechazo: el plazo que si se podia decir se
		// pierde, y la fila queda marcada como rechazada cuando no lo esta.
		[Test]
		public void AndTheHalfThatCanBeSaidStillCarriesItsAmountWhenTheOtherHalfCannot()
		{
			journal.Api.SeedPlanOperation(
				TheKindThatAnswers, "TimeOrDistance",
				TheDayLimitNothingElseCouldAnswer, TheDistanceLimitNothingElseCouldAnswer,
				TheUnitEverythingIsIn, true, false,
				TheAnchorThatAnswers, TheOdometerAtTheAnchor, TheUnitEverythingIsIn);

			HowOneOperationStands row = WhatTheScreenIsHanded().Of(TheKindThatAnswers);

			Assert.That(row.CountdownRefused, Is.False,
				"La fila quedo marcada como RECHAZADA y no lo esta: falta una lectura, que es una de las "
				+ "siete situaciones y el dominio la dice con palabras. Este es el renglon que ve un "
				+ "pre-chequeo que alguien saco de su rama: el catch por clase lo esconde.");

			Assert.That(row.DistanceCanBeSaid, Is.False);
			Assert.That(row.DistanceStanding, Is.EqualTo("SuspendedByAnAbsentReading"));
			Assert.That(row.DistanceRemaining, Is.EqualTo(0));

			Assert.That(row.DaysCanBeSaid, Is.True,
				"Y la mitad de TIEMPO se mide sin ninguna lectura: son dos mitades y no una.");
			Assert.That(row.DaysRemaining, Is.EqualTo(255),
				"El plazo que si se podia decir sigue en la respuesta. Con el monto de distancia pedido "
				+ "afuera de su rama, esta afirmacion se pierde entera.");
			Assert.That(row.DaysSays, Does.Contain("tiempo: te faltan 255 días"));
		}

		// ===== EL MENSAJE DE LA PUERTA ES CONSTANCIA Y NO PROSA ===========================

		// LA CONSTANCIA DE QUE LA FILA ES UN RECHAZO Y NO UNA AUSENCIA VIAJA, Y NO SE PINTA. Es donde la
		// forma de la caratula MCP no transfiere: alla el campo se le manda a un modelo, que lee campos
		// con nombre; aca el mensaje nombra la clase por el nombre del canon y el instante con siete
		// decimales, y eso no es una frase para quien maneja el carro.
		[Test]
		public void TheDoorsRefusalIsCarriedAsEvidenceAndIsNeverPaintedOnTheScreen()
		{
			APlanAnchoredAheadBesideOneThatAnswers();

			HowOneOperationStands refused = WhatTheScreenIsHanded().Of(TheKindAnchoredAhead);

			Assert.That(refused.RefusalSays, Is.Not.Empty,
				"La fila rechazada no lleva la constancia de la puerta, asi que nada distingue un "
				+ "RECHAZO de una respuesta vacia por otra razon.");

			Assert.That(refused.RefusalSays, Does.Contain("ancla"),
				"Y la constancia es la del rechazo del ancla, en las palabras de la puerta.");

			Assert.That(refused.RefusalSays, Does.Contain(TheKindAnchoredAhead),
				"Nombra la clase por el nombre del CANON -- que es exactamente por lo que no va a la "
				+ "pantalla.");

			foreach (string file in EveryMarkupFileTheShellHas())
				Assert.That(File.ReadAllText(file), Does.Not.Contain("RefusalSays"),
					"'" + Path.GetFileName(file) + "' pinta el mensaje de la puerta. Lleva un nombre del "
					+ "canon y un instante con siete decimales: es una respuesta para quien programa.");
		}

		// Y LO QUE LA PANTALLA DICE EN SU LUGAR NOMBRA UN ROL Y NINGUN HECHO DEL CARRO. El catch es de
		// un tipo MAS ANCHO que la condicion que lo causa, asi que la caratula no puede afirmar CUAL
		// hecho esta mal: decirlo seria inventar algo del carro.
		[Test]
		public void TheScreenSaysWhoSetTheDateItCountsFromAndNamesNoFactAboutTheCar()
		{
			string markup = TheSectionAsItIsWritten("Servicios.razor");

			Assert.That(
				markup,
				Does.Contain("TheOwnerIsTold.TheDealershipSetTheDateThisOperationCountsFrom"),
				"La fila rechazada no dice nada. Un hueco donde iba la cuenta atras se lee igual que una "
				+ "operacion que no tiene ninguna que mostrar.");

			string said = TheOwnerIsTold.TheDealershipSetTheDateThisOperationCountsFrom;

			Assert.That(said, Does.Contain("concesionario"),
				"La frase no dice quien pone la fecha. En una pantalla la persona es la que iria a "
				+ "preguntar, asi que nombrar el rol es lo unico que sirve.");

			Assert.That(said, Does.Contain("Ninguno de los datos que la deciden vino de vos"),
				"Y no dice que ninguno de los datos los puso la persona. Se afirma la clausula entera: "
				+ "nombrar el sustantivo no defiende nada, porque una frase que dijera lo contrario lo "
				+ "contendria igual.");

			foreach (string inventing in new[]
			{
				"adelante de hoy", "en el futuro", "posterior a hoy",
			})
				Assert.That(said, Does.Not.Contain(inventing),
					"La frase afirma CUAL hecho esta mal ('" + inventing + "'). El rechazo se atrapa por "
					+ "un tipo mas ancho que la condicion que lo causa, asi que la pantalla no lo sabe.");
		}

		// ===== EL APAREJO =================================================================

		private WhereEachOperationStands WhatTheScreenIsHanded()
		{
			return WhereEachOperationStands.ReadThrough(journal.Api, TheInstantTheScreenAsksAt);
		}

		private void AnOperationWithAReadingAfterIt()
		{
			journal.Api.SeedPlanOperation(
				TheKindThatAnswers, "TimeOrDistance",
				TheDayLimitNothingElseCouldAnswer, TheDistanceLimitNothingElseCouldAnswer,
				TheUnitEverythingIsIn, true, false,
				TheAnchorThatAnswers, TheOdometerAtTheAnchor, TheUnitEverythingIsIn);

			journal.Api.RecordReading(
				TheOdometerObserved, TheUnitEverythingIsIn, TheDayTheReadingWasObserved, "Telemetry");
		}

		private void APlanAnchoredAheadBesideOneThatAnswers()
		{
			AnOperationWithAReadingAfterIt();

			journal.Api.SeedPlanOperation(
				TheKindAnchoredAhead, "TimeOrDistance", 400, 9000, TheUnitEverythingIsIn,
				true, false, AnAnchorEveryFutureRunIsStillBefore, TheOdometerObserved,
				TheUnitEverythingIsIn);
		}

		private static string[] EveryMarkupFileTheShellHas()
		{
			string folder = Path.Combine(AppContext.BaseDirectory, "MarcadoComoDato");

			Assert.That(Directory.Exists(folder), Is.True,
				"El markup de la caratula no llego al output.");

			return Directory.GetFiles(folder, "*.razor", SearchOption.AllDirectories);
		}

		private static string TheSectionAsItIsWritten(string name)
		{
			foreach (string file in EveryMarkupFileTheShellHas())
				if (Path.GetFileName(file) == name) return File.ReadAllText(file);

			Assert.Fail("'" + name + "' no llego al output.");
			return "";
		}
	}
}
