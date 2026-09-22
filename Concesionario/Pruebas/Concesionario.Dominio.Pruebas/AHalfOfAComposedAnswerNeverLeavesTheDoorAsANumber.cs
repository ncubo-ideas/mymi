using System;
using System.Collections.Generic;
using System.Reflection;
using Concesionario.App;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LA REGLA QUE T5 MIDIO QUE NO TENIA NINGUNA PRUEBA DETRAS. Los dos limites del sobre, y las dos
	// mitades de lo que queda, llegan a un llamador SOLO dentro de la frase que el dominio compone.
	// Quien arme "36 meses o 60000" con escalares se llevo la composicion fuera del dominio, y desde
	// ahi cada capa que la reformule puede perder una mitad, la unidad o la procedencia.
	//
	// MEDIDO EN T5: agregarle a la puerta un int ReadEnvelopeMonths() ponia rojo EXACTAMENTE UNA cosa
	// -- el censo de operaciones corridas -- y se ponia roja porque el miembro estaba SIN CORRER, no
	// porque entregara un escalar. Con el nombre en la lista, la suite quedaba verde. La regla la
	// sostenia la revision y nada mas, y T6 es donde eso se pone peor: las lecturas compuestas son
	// justo donde una caratula querria los numeros sueltos.
	//
	// EL INSTRUMENTO SE ARMA POR ELIMINACION Y TIENE DOS MITADES, y hace falta que sean dos.
	//
	//   POR CENSO: toda pregunta de la puerta que conteste con un NUMERO, como conjunto exacto y con
	//   su aridad. Una lectura escalar nueva se pone roja con CUALQUIER nombre, que es lo que un
	//   vocabulario de nombres no daria -- quien agregue la de comodidad elige como llamarla.
	//
	//   POR VALOR: con un sobre publicado, NINGUNA de esas preguntas contesta ninguno de sus dos
	//   limites. El censo solo prueba que nadie agrego un miembro sin escribirlo; esto prueba que
	//   ninguno de los que hay entrega media respuesta.
	//
	// POR QUE LOS DIECISIETE QUE HAY SI PUEDEN, Y SON TRES CATEGORIAS. Ninguno es la mitad de una
	// respuesta compuesta:
	//
	//   TRECE son CONTEOS QUE EL DOMINIO COMPONE y publica por su cuenta -- vencidas, atrasadas, al
	//   dia, agendadas, aplazos usados, mantenimientos perdidos, ultimo evento aplicado, dias en el
	//   taller, estadias en un periodo, completaciones de una clase, precios publicados, lecturas
	//   del odometro en un periodo y entradas del libro de costos en un periodo. Un conteo es una
	//   respuesta entera: no le falta ni unidad ni limite ni instante.
	//
	//   DOS son HECHOS RECIBIDOS que el registro guarda tal como llegaron: el odometro del dia de la
	//   entrega y el de hoy. Un hecho recibido se puede dar suelto porque nadie lo derivo de otro.
	//
	//   Y DOS son RESTOS COMPUESTOS ADENTRO, CON SU UNIDAD Y SU MEDIBILIDAD AL LADO EN LA MISMA
	//   SUPERFICIE: la distancia y los dias que le quedan a una operacion. Esta tercera categoria la
	//   abrio BuildOrder/MS-4 y es una AMPLIACION DELIBERADA de la regla de admision, escrita en
	//   DECISIONS.md el mismo dia y en el mismo commit que las lecturas que admite.
	//
	// POR QUE UN RESTO SI Y UNA MITAD NO, que es toda la ampliacion. La ley no habla de numeros: habla
	// de MITADES. Una mitad es peligrosa porque COMPLETA UNA AFIRMACION QUE EL DOMINIO NO HIZO --
	// quien tenga lo recorrido Y el limite arma la resta y ademas un porcentaje del intervalo, y esas
	// dos frases el dominio no las dijo nunca. Un resto no tiene con que combinarse: ES la respuesta,
	// llega con su unidad al lado (ReadDistanceRemainingUnit, o el nombre del miembro donde la unidad
	// es una sola) y con su medibilidad al lado (CanDistanceRemainingBeSaid), que es exactamente la
	// forma que WhereThisVehicleStands ya tiene para el odometro. Los DOS OPERANDOS siguen prohibidos
	// en cualquier grafia, y de eso se ocupa
	// TravelledAndEffectiveLimitNeverLeaveTheDoorInAnySpelling, por nombre y por valor.
	//
	// LA UNICA FRASE RIVAL QUE ESTO HABILITA YA TIENE DUENO: quien tenga los dos restos los divide y
	// pinta un ritmo de uso. Esa respuesta la compone SLICE-OdometerSeries.md adentro del dominio,
	// sobre la serie de verdad y con sus unidades comprobadas, y que ninguna caratula la escriba lo
	// barre TheShellPerformsNoArithmeticOnADomainValue.
	//
	// Y ESA SERIE YA LLEGO A LA PUERTA -- BuildOrder/OD-2 -- CON UN SOLO NUMERO: ReadingCountBetween,
	// que es un CONTEO QUE EL DOMINIO COMPONE, la primera categoria, la que ya estaba. Las otras
	// cinco lecturas de la serie contestan un si o no, un nombre, dos frases y el registro. El ritmo
	// NO sale como numero por ninguna: sale adentro de la frase, con su tramo dicho.
	//
	// LO QUE ESTE INSTRUMENTO NO VE, SEGUNDA PARTE, dicho como LIMITE y no tapado con una prueba que
	// se le parezca: un numero adentro de una COLUMNA de un registro. El discriminador de abajo es
	// ReturnType == int o long, exactamente, asi que el int[] de valores del odometro que
	// OdometerSeriesRecord lleva no entra en este censo y no tiene por que -- el sujeto de esta ley
	// es una MITAD, y una fila de la serie es COMPLETA: valor, unidad, instante y fuente, con el
	// ritmo ya compuesto llegando en la misma respuesta. Que ninguna columna se llame por una mitad
	// lo afirman TravelledAndEffectiveLimitNeverLeaveTheDoorInAnySpelling y
	// NoReadReturnsTheEndsWithoutTheRateAndNoReadReturnsTheChangeAlone, cada uno sobre sus nombres.
	//
	// LAS DOS QUE ENTRARON EN MS-4 Y NO NECESITARON NINGUNA AMPLIACION: CompletionCount y
	// PublishedServicePriceCount son CONTEOS QUE EL DOMINIO COMPONE, la primera categoria, que ya
	// estaba. Dejar que la ampliacion las cubriera en silencio seria justamente la regresion que la
	// ampliacion escrita existe para poder distinguir.
	//
	// AL DIA Y AGENDADAS ENTRARON JUNTAS Y POR ESO, no por comodidad. Inicio.razor contaba las al dia
	// por el TONO de la pantalla y las dividia entre las NUEVE clases del canon; las dos mitades ya
	// las compone la agenda y ninguna capa las divide. El PORCENTAJE no esta aca ni en la caratula:
	// un porcentaje no es un conteo, ni un hecho recibido, ni un resto -- es una razon entre dos
	// numeros, que es una mitad con otro nombre.
	[TestFixture]
	public sealed class AHalfOfAComposedAnswerNeverLeavesTheDoorAsANumber
	{
		private const string TheVin = "2HGFC2F59KH123456";
		private const string TheUnitEverythingIsIn = "UnitB";

		// NUMEROS QUE NO SE PUEDEN CONFUNDIR CON NINGUN OTRO DE ESTE FIXTURE (C60). Un plazo de 36 o
		// un limite de 60000 se podrian cruzar con un conteo o con un odometro por casualidad; 37 y
		// 61111 no se cruzan con nada de lo que hay abajo.
		private const int TheMonthsNothingElseCouldAnswer = 37;
		private const int TheDistanceNothingElseCouldAnswer = 61111;

		private const int TheOdometerAtDelivery = 30000;
		private const int TheOdometerToday = 35000;

		// Los dos limites con que el barrido pregunta las dos cuentas de periodo. Sin ninguna estadia
		// registrada las dos contestan cero, que es lo que hace falta aca: lo que este fixture mide
		// es que ninguna de las diecisiete conteste 37 ni 61111.
		private const string TheFirstDayOfThePeriod = "2026-01-01";
		private const string TheLastDayOfThePeriod = "2026-12-31";

		private static readonly DateTime ThePublication =
			new DateTime(2023, 11, 15, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItWasHandedOver =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItWasObserved =
			new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItIsAsked =
			new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

		// LA OPERACION QUE MS-4 TUVO QUE INSTALAR, y por que instalarla es parte de la ampliacion y no
		// una comodidad. La mitad POR VALOR de este fixture INVOCA cada pregunta que contesta con un
		// numero, y las dos restas nuevas son lecturas GUARDADAS: sobre un registro sin ninguna
		// operacion agendada contestan que no se puede decir y rechazan, asi que el barrido se caia
		// con una excepcion en vez de con una afirmacion. La alternativa era saltarse los miembros
		// guardados, que es tomar la comparacion justo en el momento en que los miembros nuevos no
		// contestan nada -- una comparacion vale lo que vale el estado en que se toma.
		//
		// LO QUE CAMBIA DE LO QUE ESTE FIXTURE MIDE, dicho y no disimulado: ahora hay una agenda con
		// una operacion, asi que los conteos de la agenda contestan uno en vez de cero. LOS CUATRO
		// CENTINELAS SIGUEN SIENDO UNICOS -- 37, 61111, 29 y 56111 -- y los dos limites de la
		// operacion se eligieron para que sus dos restas den 7345 y 255, que no se cruzan con ninguno
		// de los cuatro ni con ningun odometro ni conteo de este fixture.
		private const int TheOperationsDistanceLimit = 12345;
		private const int TheOperationsDayLimit = 500;

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.PublishWarrantyEnvelope(
				TheMonthsNothingElseCouldAnswer, TheDistanceNothingElseCouldAnswer,
				TheUnitEverythingIsIn, ThePublication);
			journal.Api.RecordDelivery(
				TheDayItWasHandedOver, TheOdometerAtDelivery, TheUnitEverythingIsIn);
			journal.Api.RecordReading(
				TheOdometerToday, TheUnitEverythingIsIn, TheDayItWasObserved, "Telemetry");

			journal.Api.SeedPlanOperation(
				"FluidChange", "TimeOrDistance",
				TheOperationsDayLimit, TheOperationsDistanceLimit, TheUnitEverythingIsIn, true, false,
				TheDayItWasHandedOver, TheOdometerAtDelivery, TheUnitEverythingIsIn);
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// LAS DIECISIETE PREGUNTAS DE LA PUERTA QUE CONTESTAN CON UN NUMERO. Conjunto exacto, con la
		// aridad en la clave: una sobrecarga es su propio sitio y el rojo la nombra.
		//
		// LAS DOS DE ESTADIAS ENTRARON EN BuildOrder/SH-4 Y BAJO LA REGLA DE ADMISION QUE YA HABIA:
		// las dos son CONTEOS QUE EL DOMINIO COMPONE -- la primera categoria -- sobre las estadias de
		// un periodo. Los dias salen de las estadias que se cruzan con el periodo y la cuenta sale de
		// los dias; ninguna de las dos es la mitad de una frase, y las dos tienen su hermana en
		// palabras al lado (DescribeDaysInTheShopBetween). No hizo falta cambiar la regla.
		//
		// Y LAS CUATRO DE BuildOrder/MS-4 ENTRARON POR DOS PUERTAS DISTINTAS, que es lo que hay que
		// leer aca y no en la suma:
		//
		//   CompletionCount y PublishedServicePriceCount son CONTEOS QUE EL DOMINIO COMPONE -- la
		//   PRIMERA categoria, la que ya estaba -- y las dos tienen su hermana en palabras al lado
		//   (DescribeCompletions, DescribeEachPublishedPriceAsOf). No pidieron ninguna ampliacion.
		//
		//   ReadDistanceRemaining y ReadDaysRemaining son la TERCERA categoria, la que MS-4 abrio: un
		//   resto compuesto adentro, con su unidad y su medibilidad al lado. Es la ampliacion, y esta
		//   escrita arriba y en DECISIONS.md.
		//
		// Y LA DE BuildOrder/OD-2 ENTRA POR LA PRIMERA, LA QUE YA ESTABA. ReadingCountBetween cuenta
		// las FILAS del registro de la serie en un periodo -- de las lecturas sale la cuenta y de la
		// cuenta no salen las lecturas -- y tiene su hermana en filas al lado (ReadingsBetween) y su
		// hermana en palabras al lado (DescribeRateOfUse). No pidio ninguna ampliacion, y decirlo es
		// lo que impide que la ampliacion de MS-4 la cubra en silencio.
		//
		// Y LA DE BuildOrder/CL-5 ENTRA POR LA MISMA PRIMERA, Y SE DICE POR CUAL. CostEntryCountBetween
		// cuenta las FILAS del registro del libro de costos en un periodo -- de las entradas sale la
		// cuenta y de la cuenta no salen las entradas -- y tiene su hermana en filas al lado
		// (CostEntriesBetween) y su hermana en palabras al lado (DescribeSpendBetween). NO PIDIO
		// NINGUNA AMPLIACION.
		//
		// Y ES LA UNICA DEL LIBRO QUE ENTRA, que es la mitad que hay que leer aca. Las otras seis
		// lecturas del libro contestan un si o no, dos frases, una comparacion y el registro: ningun
		// MONTO, ninguna PARTE amortizada y ninguna DIFERENCIA entre dos periodos salen como numero
		// por ninguna. Las tres son mitades -- un monto sin su moneda no se puede leer, una parte es
		// media respuesta de L6, y una diferencia entre dos monedas no es un numero -- y las tres
		// llegan a quien pregunta adentro de la frase que el dominio compuso. De eso se ocupa
		// NoReadHandsOutABareAmountOrABareShareOrABareDelta, por tipo de retorno, por nombre y por
		// valor con montos centinela.
		// Y LA DE CountUpToDatePerHundred ENTRA PORQUE ES UNA RESPUESTA COMPUESTA Y NO UNA MITAD, que
		// es justo la distincion que esta lista cuida. Las dos de las que sale -- CountUpToDate y
		// CountScheduled -- ya salen enteras y estan arriba; la division de las dos la hace la
		// AGENDA, que es la unica que puede dividir sus propias cuentas, y sale ya dividida. De ella
		// no se puede rearmar ninguna frase que el dominio no haya dicho: es la misma lectura que
		// las dos, en otra unidad.
		//
		// EXISTE PORQUE UNA CARATULA QUISO DIBUJARLA. El aro de "Estado de tu vehiculo" dividia las
		// dos cuentas en el razor, que es lo que CountScheduled prohibe por escrito -- "va al lado de
		// ella para que nadie divida una cuenta del dominio entre una lista de la caratula" --. Que
		// la proporcion exista una sola vez, y en el dominio, es lo que impide que cada pantalla
		// invente la suya.
		private static readonly string[] EveryNumberTheDoorHandsOut =
		{
			"CountDue/0",
			"CountOverdue/0",
			"CountUpToDate/0",
			"CountScheduled/0",
			"CountUpToDatePerHundred/0",
			"DeferralsUsed/1",
			"MissedCount/0",
			"LastEventApplied/0",
			"ReadDeliveryReading/0",
			"ReadOdometer/1",
			"DaysInTheShopCountBetween/2",
			"StayCountBetween/2",
			"CompletionCount/1",
			"PublishedServicePriceCount/0",
			"ReadDistanceRemaining/2",
			"ReadDaysRemaining/2",
			"ReadingCountBetween/2",
			"CostEntryCountBetween/2",
		};

		[Test]
		public void TheDoorHandsOutTheseNumbersAndNoOthers()
		{
			Assert.That(EveryQuestionAnsweringWithANumber(), Is.EquivalentTo(EveryNumberTheDoorHandsOut),
				"La puerta gano o perdio una pregunta que contesta con un NUMERO. ESTA ES LA PRUEBA "
				+ "QUE SE PONE ROJA EL DIA QUE ALGUIEN SACA UNA MITAD DE LA FRASE: un limite del "
				+ "sobre, lo recorrido, el limite ya escalado, o cualquier operando del que otra capa "
				+ "pueda armar una frase que el dominio no dijo. Lo nuevo entra por UNA de las tres "
				+ "categorias del encabezado -- un CONTEO que el dominio compone, un hecho RECIBIDO "
				+ "que el registro guarda tal como llego, o un RESTO compuesto adentro con su unidad y "
				+ "su medibilidad al lado en la misma superficie -- y se dice POR CUAL. Si no es "
				+ "ninguna de las tres, es una mitad, y una mitad ya llega a quien pregunta dentro de "
				+ "la frase.");
		}

		// Y LA MITAD POR VALOR, que es la que el censo no da: con un sobre publicado, ninguna de las
		// DIECISIETE contesta ninguno de sus dos limites. El censo prueba que nadie agrego un miembro sin
		// escribirlo; esto prueba que ninguno de los que hay entrega media respuesta.
		[Test]
		public void NotOneOfThemAnswersWithEitherLimitOfThePublishedEnvelope()
		{
			foreach (KeyValuePair<string, long> answered in EveryNumberAnsweredRightNow())
			{
				Assert.That(answered.Value, Is.Not.EqualTo(TheMonthsNothingElseCouldAnswer),
					answered.Key + " contesto el PLAZO del sobre.");
				Assert.That(answered.Value, Is.Not.EqualTo(TheDistanceNothingElseCouldAnswer),
					answered.Key + " contesto la DISTANCIA del sobre.");
			}
		}

		// Y LOS DOS LIMITES SI LLEGAN A QUIEN PREGUNTA -- dentro de la frase, que es el otro lado de
		// la misma ley. Sin este renglon, la de arriba se cumpliria tambien si la puerta no dijera
		// los limites en ninguna parte, que seria un defecto distinto y peor.
		[Test]
		public void BothLimitsReachTheCallerInsideTheSentence()
		{
			Assert.That(
				journal.Api.DescribePublishedWarrantyEnvelope(TheLanguage.OfTheRecord),
				Does.Contain("37 meses o 61.111 mi"));

			Assert.That(
				journal.Api.DescribeWarrantyEnvelope(TheDayItIsAsked, TheLanguage.OfTheRecord),
				Does.Contain("37 meses o 61.111 mi"));
		}

		// Y LO QUE LE QUEDA A CADA MITAD DEL SOBRE, TAMBIEN DENTRO DE LA FRASE Y EN NINGUN OTRO LADO.
		// Es la misma ley aplicada a lo DERIVADO en vez de a lo publicado, y es la mitad que mas
		// tentacion da: una pantalla que quiera pintar una barra querria los dos numeros sueltos.
		//
		// EL NOMBRE SE ESTRECHO EN BuildOrder/MS-4 Y ESO ES LO QUE HAY QUE LEER AQUI. Decia
		// "WhatIsLeftOfEachHalf...", sin nombrar de que; desde MS-4 la puerta SI entrega un resto como
		// numero -- ReadDistanceRemaining y ReadDaysRemaining -- y el nombre viejo se volvio falso. El
		// resto que ahora cruza es de OTRO AGREGADO y contra OTRO LIMITE: la agenda contra el limite
		// del plan escalado por el perfil de uso, no el perfil del vehiculo contra el sobre publicado.
		// Asi que lo que sigue siendo cierto, y lo que este renglon afirma, es de LAS DOS MITADES DEL
		// SOBRE: 29 meses y 56111 de distancia no salen por ningun miembro, y salen dentro de la
		// frase. Estrechar el nombre es la unica forma de que la prueba siga diciendo lo que hace; sin
		// estrecharlo, un lector dentro de seis meses no puede distinguir una ampliacion deliberada de
		// una regresion que dejo de notarse.
		[Test]
		public void WhatIsLeftOfTheEnvelopesTwoHalvesReachesTheCallerInsideTheSentenceAndNowhereElse()
		{
			string said = journal.Api.DescribeWarrantyEnvelope(
				TheDayItIsAsked, TheLanguage.OfTheRecord);

			Assert.That(said, Does.Contain("Tiempo: quedan 29 meses de 37 meses."));
			Assert.That(said, Does.Contain("Distancia: quedan 56.111 mi de 61.111 mi."));

			foreach (KeyValuePair<string, long> answered in EveryNumberAnsweredRightNow())
			{
				Assert.That(answered.Value, Is.Not.EqualTo(29),
					answered.Key + " contesto los meses que quedan.");
				Assert.That(answered.Value, Is.Not.EqualTo(56111),
					answered.Key + " contesto la distancia que queda.");
			}
		}

		// LA AMPLIACION, HECHA VISIBLE. El brief pide que el censo quede ROJO contra la enumeracion
		// ANTERIOR y verde contra la nueva, en un solo commit y con la decision escrita. Esto lo dice
		// literalmente: las ONCE de antes se vuelven a enumerar aqui y se afirma que YA NO son lo que
		// la puerta contesta.
		//
		// SIN ESTE RENGLON, LA AMPLIACION SE VE IGUAL QUE UNA REGRESION QUE DEJO DE NOTARSE: la lista
		// de arriba habria crecido cuatro nombres y nada diria que crecio a proposito. Con el, quien
		// vuelva a estrechar la puerta a las once tiene que borrar esta prueba, y borrarla es un acto.
		private static readonly string[] TheEnumerationBeforeTheAmendment =
		{
			"CountDue/0",
			"CountOverdue/0",
			"CountUpToDate/0",
			"CountScheduled/0",
			"DeferralsUsed/1",
			"MissedCount/0",
			"LastEventApplied/0",
			"ReadDeliveryReading/0",
			"ReadOdometer/1",
			"DaysInTheShopCountBetween/2",
			"StayCountBetween/2",
		};

		[Test]
		public void ThePreAmendmentEnumerationIsRedAgainstThisDoor()
		{
			Assert.That(TheEnumerationBeforeTheAmendment, Has.Length.EqualTo(11));

			Assert.That(
				EveryQuestionAnsweringWithANumber(),
				Is.Not.EquivalentTo(TheEnumerationBeforeTheAmendment),
				"La puerta volvio a contestar exactamente las once de antes de la ampliacion. Si eso "
				+ "es a proposito, la ampliacion se retira entera -- encabezado, mensaje, conjunto y "
				+ "el nombre estrechado de la ley del sobre -- y no solo esta prueba.");

			foreach (string admitted in new[]
			{
				"CompletionCount/1", "PublishedServicePriceCount/0",
				"ReadDistanceRemaining/2", "ReadDaysRemaining/2",
			})
			{
				Assert.That(TheEnumerationBeforeTheAmendment, Does.Not.Contain(admitted));
				Assert.That(EveryQuestionAnsweringWithANumber(), Does.Contain(admitted));
			}
		}

		private static List<string> EveryQuestionAnsweringWithANumber()
		{
			var found = new List<string>();

			foreach (MethodInfo each in TheDoorsMembers())
				if (AnswersWithANumber(each))
					found.Add(each.Name + "/" + each.GetParameters().Length);

			return found;
		}

		// Cada una de las diecisiete, invocada contra este registro. Los argumentos se arman por TIPO y no
		// por nombre: es lo que hace que una lectura nueva entre al barrido sola en vez de esperar a
		// que alguien la agregue.
		private List<KeyValuePair<string, long>> EveryNumberAnsweredRightNow()
		{
			var answered = new List<KeyValuePair<string, long>>();

			foreach (MethodInfo each in TheDoorsMembers())
			{
				if (!AnswersWithANumber(each)) continue;

				answered.Add(new KeyValuePair<string, long>(
					each.Name, Convert.ToInt64(Invoke(each))));
			}

			Assert.That(answered, Has.Count.EqualTo(EveryNumberTheDoorHandsOut.Length),
				"El barrido dejo de encontrar alguna de las que cuenta.");

			return answered;
		}

		private object Invoke(MethodInfo member)
		{
			var arguments = new List<object>();

			foreach (ParameterInfo each in member.GetParameters())
			{
				if (each.ParameterType == typeof(DateTime)) { arguments.Add(TheDayItIsAsked); continue; }

				// EL BARRIDO SE ARMA POR TIPO Y AQUI TIENE QUE MIRAR EL NOMBRE, y esa es la excepcion:
				// un limite de periodo es una cadena que la puerta exige que DIGA UN DIA antes de
				// performar, asi que "FluidChange" no la pasa. Los dos nombres se escriben porque la
				// alternativa -- pasar un dia a toda cadena -- rompe las claves de catalogo, que son
				// cadenas de otra clase.
				if (each.Name == "fromDay") { arguments.Add(TheFirstDayOfThePeriod); continue; }
				if (each.Name == "toDay") { arguments.Add(TheLastDayOfThePeriod); continue; }

				if (each.ParameterType == typeof(string)) { arguments.Add("FluidChange"); continue; }

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

		private static bool AnswersWithANumber(MethodInfo member)
		{
			return member.ReturnType == typeof(int) || member.ReturnType == typeof(long);
		}

		private static MethodInfo[] TheDoorsMembers()
		{
			return typeof(DealershipCarAPI).GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
		}
	}
}
