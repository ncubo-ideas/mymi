using System;
using System.IO;
using Concesionario.App.Views;
using Concesionario.Wasm;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// EL BLOQUE "LO QUE LE HAN HECHO", Y LA PREGUNTA QUE ESTA TAREA TUVO QUE CONTESTAR POR TERCERA VEZ:
	// CUANTAS FORMAS DE UN MISMO HECHO OFRECE ESTA SUPERFICIE.
	//
	// La parte B llega a la puerta como DOS registros: el de una clase trae tres columnas y el de TODAS
	// las clases trae cuatro, con la clase adentro. Cada fila del primero ES una fila del segundo
	// filtrada por esa columna, que es direccionable por nombre.
	//
	//   LA CONSOLA imprime los dos, porque es el instrumento de medicion y su oficio es enumerar la
	//   puerta.
	//   LA CARATULA MCP contesta desde el de TODAS y deja el de una clase sin llamar.
	//   ESTA PANTALLA hace lo mismo, y no por herencia: es UNA superficie alimentada por UN Reread, y
	//   las filas por clase en una pestana mas el historial entero en otra son las mismas filas dos
	//   veces, con las primeras un subconjunto de las segundas.
	//
	// LO QUE LA PANTALLA SI NECESITA DE LAS LECTURAS POR CLASE NO SON LAS FILAS: es la FRASE. El juego
	// de todas las clases no tiene ninguna frase de ausencia -- contesta la lista vacia y nada mas -- y
	// las dos ausencias de L7 son dos cosas distintas, de las cuales solo una la cura una visita al
	// taller. Asi que la frase por clase se pinta EXACTAMENTE donde el juego entero se queda callado, y
	// no al lado de una fila: MEDIDO abajo, para una clase que si tiene filas la frase por clase repite
	// la clausula de la fila mas reciente palabra por palabra.
	[TestFixture]
	public sealed class TheCompletionsBlockPaintsTheRowsAndTheAbsencesAreThePerKindSentences
	{
		private const string TheVin = "JTDKB20U0930000014";
		private const string TheUnitEverythingIsIn = "UnitA";
		private const string TheBlock = "Historial.razor";

		private const string TheKindThatWasCompleted = "Inspection";
		private const string TheKindScheduledAndNeverDone = "FluidChange";
		private const string TheKindThatIsNotOnTheSchedule = "Repair";

		private static readonly DateTime TheAnchor =
			new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheFirstCompletion =
			new DateTime(2025, 4, 11, 12, 0, 0, DateTimeKind.Utc);
		private const int TheOdometerAtTheFirst = 22500;

		private static readonly DateTime TheSecondCompletion =
			new DateTime(2025, 8, 22, 9, 30, 0, DateTimeKind.Utc);
		private const int TheOdometerAtTheSecond = 27300;

		private static readonly DateTime TheInstantTheScreenAsksAt =
			new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

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

		// ===== LA MEDICION QUE DECIDIO DONDE VA LA FRASE POR CLASE ========================

		// PARA UNA CLASE QUE TIENE FILAS, LA FRASE POR CLASE REPITE LA CLAUSULA DE LA FILA MAS RECIENTE.
		// No es una opinion sobre el diseno: es lo que el dominio compone, y pintar las dos juntas
		// imprime la misma clausula dos veces en una sola columna.
		[Test]
		public void ForAKindWithRowsThePerKindSentenceRestatesTheNewestRowsOwnClause()
		{
			TwoCompletionsOfOneKind();

			WhereEachOperationStands handed = WhatTheScreenIsHanded();

			string perKind = handed.Of(TheKindThatWasCompleted).CompletionsSays;
			AThingThatWasDone newest = handed.WhatHasBeenDone[0];

			Assert.That(newest.Kind, Is.EqualTo(TheKindThatWasCompleted));

			Assert.That(perKind, Does.Contain("2 completaciones registradas"));

			Assert.That(
				perKind,
				Does.Contain("se hizo el 2025-08-22 09:30:00 con el odómetro en 27300 km"),
				"LA MEDICION: la frase por clase lleva ADENTRO la clausula de la fila mas reciente. Es "
				+ "por esto que no se pinta al lado de las filas -- pintar las dos imprime la misma "
				+ "clausula dos veces en una sola columna.");

			Assert.That(
				newest.Says,
				Does.Contain("se hizo el 2025-08-22 09:30:00 con el odómetro en 27300 km"),
				"Y es la MISMA clausula, que es lo que hace que la de arriba sea una duplicacion y no un "
				+ "parecido.");
		}

		// Y PARA UNA CLASE SIN FILAS ES LO UNICO QUE DICE ALGO, y dice DOS cosas distintas segun por
		// que no hay ninguna. El juego de todas las clases no tiene ninguna frase de ausencia por
		// decision de alcance, asi que sin esto la pantalla se queda callada donde hay algo que decir.
		[Test]
		public void ForAKindWithNoRowsThePerKindSentenceIsTheOnlyThingThatSaysAnything()
		{
			TwoCompletionsOfOneKind();

			WhereEachOperationStands handed = WhatTheScreenIsHanded();

			foreach (AThingThatWasDone each in handed.WhatHasBeenDone)
				Assert.That(each.Kind, Is.EqualTo(TheKindThatWasCompleted),
					"El juego entero solo tiene filas de la clase completada, asi que las otras ocho no "
					+ "aparecen en el de ninguna forma.");

			HowOneOperationStands scheduled = handed.Of(TheKindScheduledAndNeverDone);
			HowOneOperationStands notScheduled = handed.Of(TheKindThatIsNotOnTheSchedule);

			Assert.That(scheduled.HasBeenCompleted, Is.False);
			Assert.That(notScheduled.HasBeenCompleted, Is.False);

			Assert.That(
				scheduled.CompletionsSays,
				Is.EqualTo("Cambio de fluido: no hay ninguna completación registrada para este vehículo."),
				"Agendada y nunca hecha: la cura una visita al taller.");

			Assert.That(notScheduled.CompletionsSays, Does.Contain("no tiene agendada ninguna operación"),
				"Y no agendada no la cura ninguna. Un booleano juntaria las dos.");

			Assert.That(notScheduled.CompletionsSays, Is.Not.EqualTo(scheduled.CompletionsSays));
		}

		// ===== EL MARKUP DEL BLOQUE =======================================================

		// LA FRASE POR CLASE SE PINTA ADENTRO DE LA GUARDA DE LA AUSENCIA Y EN NINGUN OTRO SITIO, que es
		// la medicion de arriba hecha estructura.
		[Test]
		public void ThePerKindSentenceIsPaintedOnlyInsideTheGuardOfTheAbsence()
		{
			string markup = TheBlockAsItIsWritten();

			Assert.That(markup, Does.Contain("@if (!parada.HasBeenCompleted)"),
				"La frase por clase se pinta sin la guarda de la ausencia. Para una clase que tiene "
				+ "filas, esa frase repite la clausula de la fila mas reciente.");

			Assert.That(TimesItSays(markup, "@parada.CompletionsSays"), Is.EqualTo(1),
				"La frase por clase se pinta en mas de un sitio. Dos sitios son dos ramas, y una de las "
				+ "dos se va a quedar sin la guarda que la protege.");
		}

		// EL BLOQUE PINTA LA FRASE DE CADA FILA ENTERA Y NO ARMA NINGUNA PROPIA.
		[Test]
		public void TheBlockPaintsEveryRowsOwnSentenceWholeAndComposesNoneOfItsOwn()
		{
			string markup = TheBlockAsItIsWritten();

			Assert.That(markup, Does.Contain("@hecho.Says"),
				"El bloque dejo de pintar la frase de cada fila.");

			Assert.That(markup, Does.Contain("@hecho.Reading"),
				"El bloque dejo de pintar con que lectura del odometro se hizo cada una, que es la "
				+ "columna que dice con palabras que no hay ninguna en vez de decir cero.");

			Assert.That(markup, Does.Contain("@hecho.CompletedAt"));

			foreach (string forbidden in new[]
			{
				"hecho.Says +", "+ hecho.Says", "Reading +", "+ Reading",
				"CompletionsSays +", "+ CompletionsSays",
			})
				Assert.That(markup, Does.Not.Contain(forbidden),
					"El bloque concatena algo con una frase del dominio ('" + forbidden + "').");
		}

		// LA FILA NO NOMBRA SU CLASE DOS VECES EN DOS PALABRAS DISTINTAS. La frase de la fila abre con la
		// clase en la palabra del DOMINIO, y la tabla de la caratula deletrea DOS de las nueve con otra
		// palabra: un titulo traducido encima de esa frase pondria dos nombres de una misma operacion en
		// una sola fila. La columna Kind viaja en el portador y no va a la pantalla.
		[Test]
		public void TheRowDoesNotNameItsKindTwiceInTwoDifferentWords()
		{
			string markup = TheBlockAsItIsWritten();

			foreach (string translating in new[]
			{
				"ComoSeLee.Operacion(hecho.Kind)", "ComoSeLee.OperacionCorta(hecho.Kind)",
			})
				Assert.That(markup, Does.Not.Contain(translating),
					"La fila del historial gano un titulo traducido ('" + translating + "'). Su frase ya "
					+ "abre con la clase en la palabra del dominio, y la tabla de la caratula no dice la "
					+ "misma palabra para dos de las nueve clases.");

			// Y LA CLASE SIGUE LLEGANDO EN EL PORTADOR, que es lo que hace que la de arriba sea una
			// decision de pintado y no una columna perdida.
			TwoCompletionsOfOneKind();

			Assert.That(
				WhatTheScreenIsHanded().WhatHasBeenDone[0].Kind, Is.EqualTo(TheKindThatWasCompleted));
		}

		// EL ENCABEZADO NO NOMBRA NINGUNA VENTANA, y no es una omision: la lectura del historial no toma
		// ningun instante y no tiene periodo. Cada fila trae su propio instante y la cadena es la que es,
		// asi que no hay ninguna ventana que el encabezado pudiera duplicar.
		//
		// EL SUJETO SE ESTRECHO AL ENCABEZADO DE ESTE BLOQUE, y hay que decir por que. Esta afirmacion
		// se escribio cuando Historial.razor tenia DOS bloques y buscaba las palabras de periodo en el
		// ARCHIVO ENTERO; hoy tiene CUATRO, y el del ritmo de uso nombra su ventana en el encabezado
		// porque su ley se lo exige -- la frase del dominio no la nombra cuando hay lecturas. Un
		// archivo compartido convierte una afirmacion sobre el propio bloque en una afirmacion sobre
		// los vecinos, y esa segunda es falsa. No se aflojo: se apunto a lo que siempre fue su sujeto,
		// y el renglon de abajo prueba que sigue atrapando lo que existe para atrapar.
		[Test]
		public void TheBlockNamesNoWindowBecauseTheReadIsUnbounded()
		{
			string markup = TheBlockAsItIsWritten();

			Assert.That(markup, Does.Contain("Lo que le han hecho"),
				"El encabezado del bloque cambio de nombre.");

			string heading = TheHeadingOfThisBlockIn(markup);

			foreach (string bounded in new[]
			{
				"Lo que le han hecho en", "Lo que le han hecho desde", "últimos", "este año",
			})
			{
				Assert.That(heading, Does.Not.Contain(bounded),
					"El encabezado del bloque del historial nombra un periodo ('" + bounded + "'). La "
					+ "lectura no toma ningun instante: acotarla en el encabezado seria una ventana que "
					+ "nadie aplico.");

				// EL CONTROL DEL RECORTE: la misma palabra, metida en este encabezado, SI se ve. Sin
				// este renglon, recortar el sujeto y recortarlo hasta que no mire nada son el mismo
				// verde.
				Assert.That(
					TheHeadingOfThisBlockIn(markup.Replace(
						"Lo que le han hecho", "Lo que le han hecho, " + bounded)),
					Does.Contain(bounded),
					"El recorte del sujeto dejo de ver una palabra de periodo puesta en ESTE "
					+ "encabezado, asi que la afirmacion de arriba no mide nada.");
			}

			// Y EL PORTADOR TAMPOCO LE PONE UNO: el instante que si lleva es el de la cuenta atras, cuyas
			// frases no nombran su propio momento.
			foreach (System.Reflection.PropertyInfo each in
				typeof(AThingThatWasDone).GetProperties())
				Assert.That(each.Name, Does.Not.Contain("AskedAt"),
					"Una fila del historial gano el instante en que se pregunto. No depende de ninguno.");
		}

		// Y LA AUSENCIA LLEVA UNA LINEA DE LA CARATULA QUE NOMBRA UN ROL. Ninguna frase del dominio dice
		// quien registra lo que se le hace al carro: cada una contesta por su propia clase. Es la frase
		// que mas facil se lee al reves en este bloque -- "no aparece" no es "nunca se le hizo".
		[Test]
		public void TheScreenSaysWhoRecordsTheWorkAndNamesNoFactAboutTheCar()
		{
			string markup = TheBlockAsItIsWritten();

			Assert.That(
				markup,
				Does.Contain("TheOwnerIsTold.TheCompletionsAreRecordedByTheWorkshopThatDoesTheWork"),
				"El bloque no dice quien registra lo que se le hace al carro.");

			// Y VA DEBAJO DE LA LISTA DE AUSENCIAS, no de la rama del bloque vacio. MEDIDO EN EL
			// NAVEGADOR: con una completacion en el registro la rama vacia no se dibuja, y quien lee
			// ocho frases seguidas que dicen "no hay ninguna completacion registrada" es exactamente el
			// publico de esta frase. La rama vacia y la lista de ausencias tampoco son excluyentes -- sin
			// ninguna completacion se dibujan las dos -- asi que la frase va en UNA sola de las dos.
			Assert.That(
				markup.IndexOf(
					"TheOwnerIsTold.TheCompletionsAreRecordedByTheWorkshopThatDoesTheWork",
					StringComparison.Ordinal),
				Is.GreaterThan(markup.IndexOf("id=\"operaciones-sin-registro\"", StringComparison.Ordinal)),
				"La frase quedo antes de la lista de ausencias. Va debajo de ella: es la lista la que se "
				+ "lee al reves, y es la que se dibuja tambien cuando SI hay filas.");

			Assert.That(
				TimesItSays(
					markup, "TheOwnerIsTold.TheCompletionsAreRecordedByTheWorkshopThatDoesTheWork"),
				Is.EqualTo(1),
				"La frase se pinta en mas de un sitio, y las dos ramas se dibujan juntas cuando no hay "
				+ "ninguna completacion.");

			string said = TheOwnerIsTold.TheCompletionsAreRecordedByTheWorkshopThatDoesTheWork;

			Assert.That(said, Does.Contain("no quiere decir que nunca se le haya hecho al carro"),
				"La frase no dice al reves lo que se lee al reves.");

			Assert.That(said, Does.Contain("no hay nada que tengas que llenar vos"),
				"Y no dice que la persona no es la que aporta esto.");
		}

		// ===== NINGUN CONTROL, Y NINGUNA FORMA DE MOVER NADA ==============================

		// LA TERCERA INVENCION PROHIBIDA, EN EL BLOQUE NUEVO. El barrido del modulo ya cubre el archivo
		// -- MS-7 no creo ninguno -- y estas cinco formas se vuelven a mirar aqui porque son las
		// estructurales. Y el bloque no puede ofrecer nada aunque quisiera: el historial no tiene ningun
		// periodo que mover.
		[Test]
		public void NoControlOfTheCompletionsBlockCollectsAFactAboutThisVehicle()
		{
			string markup = TheBlockAsItIsWritten();

			foreach (string shape in new[]
			{
				"<input", "<select", "<textarea", "<form", "@bind-Value", "@onclick", "<button",
			})
				Assert.That(markup, Does.Not.Contain(shape),
					"El bloque del historial gano un control ('" + shape + "'). La app del dueno es de "
					+ "SOLO LECTURA: lo que se le hizo al carro lo registra el taller, y si falta algo el "
					+ "arreglo es que lo manden, no que la persona lo escriba.");

			// EL CONTROL DEL DETECTOR. Sin este renglon, un comparador roto pasaria diciendo lo mismo.
			int caught = 0;

			foreach (string shape in new[]
			{
				"<input", "<select", "<textarea", "<form", "@bind-Value", "@onclick", "<button",
			})
				if (("<div>" + shape + " Label=\"¿Qué le hicieron?\" /></div>").Contains(shape)) caught++;

			Assert.That(caught, Is.EqualTo(7));
		}

		// ===== EL APAREJO =================================================================

		private WhereEachOperationStands WhatTheScreenIsHanded()
		{
			return WhereEachOperationStands.ReadThrough(journal.Api, TheInstantTheScreenAsksAt);
		}

		private void TwoCompletionsOfOneKind()
		{
			journal.Api.SeedPlanOperation(
				TheKindThatWasCompleted, "TimeOrDistance", 400, 9000, TheUnitEverythingIsIn,
				true, false, TheAnchor, 20000, TheUnitEverythingIsIn);

			journal.Api.SeedPlanOperation(
				TheKindScheduledAndNeverDone, "TimeOrDistance", 620, 21345, TheUnitEverythingIsIn,
				true, false, new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc), 21000,
				TheUnitEverythingIsIn);

			journal.Api.CompleteOperation(
				TheKindThatWasCompleted, TheFirstCompletion, TheOdometerAtTheFirst,
				TheUnitEverythingIsIn);

			journal.Api.CompleteOperation(
				TheKindThatWasCompleted, TheSecondCompletion, TheOdometerAtTheSecond,
				TheUnitEverythingIsIn);
		}

		private static int TimesItSays(string markup, string what)
		{
			int found = 0;
			int at = markup.IndexOf(what, StringComparison.Ordinal);

			while (at >= 0)
			{
				found++;
				at = markup.IndexOf(what, at + what.Length, StringComparison.Ordinal);
			}

			return found;
		}

		/// <summary>
		/// El encabezado de ESTE bloque y nada mas: el div de seccion que lleva su titulo, desde su
		/// apertura hasta su cierre. Hace falta porque el archivo tiene mas de un bloque y cada uno
		/// decide por su cuenta si nombra una ventana.
		/// </summary>
		private static string TheHeadingOfThisBlockIn(string markup)
		{
			int title = markup.IndexOf("Lo que le han hecho", StringComparison.Ordinal);

			Assert.That(title, Is.GreaterThanOrEqualTo(0),
				"El titulo del bloque no esta en el markup, asi que no hay encabezado que recortar.");

			int opens = markup.LastIndexOf("<div class=\"d-sec\"", title, StringComparison.Ordinal);

			Assert.That(opens, Is.GreaterThanOrEqualTo(0),
				"El titulo del bloque dejo de estar dentro de un div de seccion.");

			int closes = markup.IndexOf("</div>", title, StringComparison.Ordinal);

			Assert.That(closes, Is.GreaterThan(opens),
				"El div de seccion del encabezado no cierra.");

			return markup.Substring(opens, closes - opens);
		}

		private static string TheBlockAsItIsWritten()
		{
			string folder = Path.Combine(AppContext.BaseDirectory, "SeccionesComoDato");

			Assert.That(Directory.Exists(folder), Is.True, "Las secciones no llegaron al output.");

			foreach (string file in Directory.GetFiles(folder, "*.razor"))
				if (Path.GetFileName(file) == TheBlock) return File.ReadAllText(file);

			Assert.Fail("'" + TheBlock + "' no llego al output.");
			return "";
		}
	}
}
