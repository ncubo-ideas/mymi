using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LA PARTE A DEL SLICE, DEL LADO DEL WIRE. Las diez lecturas de la cuenta atras cruzan la puerta,
	// y lo que cruza es el RESTO ya compuesto: un monto con su unidad y su situacion al lado. Lo
	// recorrido y el limite ya escalado no cruzan nunca, ni juntos ni sueltos -- de eso se ocupa
	// TravelledAndEffectiveLimitNeverLeaveTheDoorInAnySpelling, que es la otra mitad de la ley L1.
	//
	// TODO SE AFIRMA CONTRA LO QUE SE RECIBIO, POR LITERAL (E49 obligacion 2). Comparar la respuesta
	// de la puerta contra la del dominio seria comparar DOS CONSUMIDORES DE UN MISMO RESOLVEDOR: se
	// ponen de acuerdo por construccion, incluso cuando el resolvedor esta uniformemente equivocado.
	// MS-3 lo midio: borrar la comparacion de vigencia adentro del unico resolvedor de precios dejo la
	// enumeracion y las lecturas por clave en acuerdo perfecto sobre una frase que ninguna de las dos
	// debia estar diciendo, y solo los literales lo vieron. Asi que aca los numeros salen de
	// SeedPlanOperation y de RecordReading y de ningun otro lado:
	//
	//   ancla 2025-06-01 con el odometro en 30000, lectura 35000 observada el 2026-01-20,
	//   preguntado el 2026-02-01: lo recorrido es 5000 y el plazo transcurrido 245 dias.
	//   Contra un limite de 12345 y uno de 500 dias, lo que queda es 7345 y 255.
	//
	// LOS DOS LIMITES SE ELIGIERON PARA QUE NADA MAS DE ESTE FIXTURE PUEDA CONTESTARLOS (C60), y las
	// dos restas dan numeros que tampoco se cruzan con ningun conteo, ningun odometro ni ninguna de
	// las dos mitades del sobre de garantia.
	[TestFixture]
	public sealed class ARemainderReachesTheDoorComposedWithItsUnitAndItsStanding
	{
		private const string TheVin = "3VWFE21C04M000001";
		private const string TheKind = "FluidChange";
		private const string TheUnitEverythingIsIn = "UnitA";
		private const string TheUnitAsAPersonReadsIt = "km";

		private const int TheDistanceLimitNothingElseCouldAnswer = 12345;
		private const int TheDayLimitNothingElseCouldAnswer = 500;

		private const int TheOdometerAtTheAnchor = 30000;
		private const int TheOdometerObservedLater = 35000;

		// Lo que las dos restas tienen que dar, escrito aqui y no calculado: una prueba que repitiera
		// la resta afirmaria su propia aritmetica y no la del dominio.
		private const int TheDistanceThatIsLeft = 7345;
		private const int TheDaysThatAreLeft = 255;

		private static readonly DateTime TheAnchor =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItWasObserved =
			new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItIsAsked =
			new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// ===== LO QUE QUEDA, COMPUESTO Y CON SU UNIDAD ====================================

		[Test]
		public void TheDistanceRemainderComesBackComposedWithItsUnitAndItsStanding()
		{
			AnOperationWithAReadingAfterIt();

			Assert.That(journal.Api.CanDistanceRemainingBeSaid(TheKind, TheDayItIsAsked), Is.True);

			Assert.That(
				journal.Api.ReadDistanceRemaining(TheKind, TheDayItIsAsked),
				Is.EqualTo(TheDistanceThatIsLeft));

			Assert.That(
				journal.Api.ReadDistanceRemainingUnit(TheKind, TheDayItIsAsked),
				Is.EqualTo(TheUnitAsAPersonReadsIt),
				"La unidad cruza por su NOMBRE y nunca por su codigo: proyectar el codigo en la puerta "
				+ "pondria el mapeo del conjunto cerrado en una caratula.");

			Assert.That(
				journal.Api.ReadDistanceRemainderStanding(TheKind, TheDayItIsAsked),
				Is.EqualTo("StillAhead"));

			// LA FRASE ENTERA Y NO DOS PEDAZOS SUELTOS: dos valores sueltos que se contienen no
			// distinguen una frase cruzada de una correcta.
			Assert.That(
				journal.Api.DescribeDistanceRemaining(TheKind, TheDayItIsAsked, TheLanguage.OfTheRecord),
				Is.EqualTo("distancia: te faltan 7.345 km"));
		}

		[Test]
		public void TheTimeRemainderComesBackComposedWithItsStanding()
		{
			AnOperationWithAReadingAfterIt();

			Assert.That(journal.Api.CanDaysRemainingBeSaid(TheKind, TheDayItIsAsked), Is.True);

			Assert.That(
				journal.Api.ReadDaysRemaining(TheKind, TheDayItIsAsked),
				Is.EqualTo(TheDaysThatAreLeft));

			Assert.That(
				journal.Api.ReadTimeRemainderStanding(TheKind, TheDayItIsAsked),
				Is.EqualTo("StillAhead"));

			Assert.That(
				journal.Api.DescribeDaysRemaining(TheKind, TheDayItIsAsked, TheLanguage.OfTheRecord),
				Is.EqualTo("tiempo: te faltan 255 días"));
		}

		// LA UNIDAD DEL PLAZO ESTA EN EL NOMBRE DEL MIEMBRO Y LA DE LA DISTANCIA NO PUEDE ESTARLO, y
		// esa asimetria es la razon por la que hay ReadDistanceRemainingUnit y no hay su gemela de
		// tiempo. La distancia tiene un conjunto cerrado de DOS unidades, asi que un monto sin su
		// unidad es ambiguo; el tiempo tiene UNA, y ReadDaysRemaining ya la dice.
		[Test]
		public void TheDaysAreSaidWithTheirUnitInTheMembersOwnNameAndInTheSentence()
		{
			AnOperationWithAReadingAfterIt();

			Assert.That(
				journal.Api.DescribeDaysRemaining(TheKind, TheDayItIsAsked, TheLanguage.OfTheRecord),
				Does.Contain("255 días"));

			Assert.That(TheDoorsMembers("ReadDaysRemainingUnit"), Is.Empty,
				"La puerta gano una unidad de tiempo. El plazo tiene una sola unidad y el nombre del "
				+ "miembro ya la dice; agregarla seria una columna constante.");
		}

		// LA MISMA RESPUESTA EN EL OTRO IDIOMA, Y LOS VALORES DEL REGISTRO INTACTOS. El marco cambia
		// entero; la unidad no, porque «km» es lo que el concesionario asento y no una palabra
		// nuestra. El separador de miles SI cambia: es formato y viaja con el renderizador.
		[Test]
		public void TheSameRemainderSaidToSomebodyWhoReadsEnglishKeepsTheRecordsOwnValues()
		{
			AnOperationWithAReadingAfterIt();

			string inEnglish = journal.Api.DescribeCountdown(
				TheKind, TheDayItIsAsked, TheLanguage.OfAnEnglishReader);

			Assert.That(inEnglish, Is.EqualTo("distance: 7,345 km to go; time: 255 days to go"));

			Assert.That(
				journal.Api.ReadDistanceRemainingUnit(TheKind, TheDayItIsAsked),
				Is.EqualTo(TheUnitAsAPersonReadsIt),
				"Y la unidad que la puerta contesta como valor es la misma en los dos idiomas: no es "
				+ "texto nuestro.");

			Assert.That(
				journal.Api.DescribeCountdown(TheKind, TheDayItIsAsked, TheLanguage.OfTheRecord),
				Is.Not.EqualTo(inEnglish),
				"Sin esto, un catalogo que contestara lo mismo en los dos idiomas pasaria igual.");
		}

		// Y UNA ETIQUETA QUE ESTE MODULO NO HABLA CAE AL IDIOMA DEL REGISTRO. Una consulta no se
		// refuta por el nombre de un idioma: la puerta contesta, en el idioma en que lleva el
		// registro el concesionario.
		[Test]
		public void ALanguageThisModuleDoesNotSpeakFallsBackToTheLanguageOfTheRecord()
		{
			AnOperationWithAReadingAfterIt();

			Assert.That(
				journal.Api.DescribeCountdown(TheKind, TheDayItIsAsked, "jp"),
				Is.EqualTo(journal.Api.DescribeCountdown(
					TheKind, TheDayItIsAsked, TheLanguage.OfTheRecord)));
		}

		// EL CABLEADO DEL AYUDANTE DE LAS AGENDAS PURAS ES EL MISMO QUE EL DE LA PUERTA, medido y no
		// supuesto. TheSentenceTheDoorWouldCompose existe porque una agenda pura no tiene puerta, y
		// su unico contenido es que accesor va en que parametro -- cableado que si esta escrito dos
		// veces. Esto pone las dos salidas una contra la otra sobre EL MISMO registro, que es lo que
		// impide que una de las dos se quede atras sin que nada avise.
		[Test]
		public void TheHelperOfThePureSchedulesComposesExactlyWhatTheDoorComposes()
		{
			AnOperationWithAReadingAfterIt();

			MaintenanceSchedule pure = TheSameStoryOnAPureSchedule();

			foreach (string language in new[]
				{ TheLanguage.OfTheRecord, TheLanguage.OfAnEnglishReader })
			{
				Assert.That(
					TheSentenceTheDoorWouldCompose.TheCountdown(
						pure, OperationKindCode.FluidChange, TheDayItIsAsked, language),
					Is.EqualTo(journal.Api.DescribeCountdown(TheKind, TheDayItIsAsked, language)),
					"El cableado del ayudante y el de la puerta dejaron de decir lo mismo en "
					+ language + ".");

				Assert.That(
					TheSentenceTheDoorWouldCompose.TheDueReason(
						pure, OperationKindCode.FluidChange, language),
					Is.EqualTo(journal.Api.ReadDueReason(TheKind, language)));
			}
		}

		// LA MISMA HISTORIA SIN JOURNAL: los mismos literales, sembrados y observados en el mismo
		// orden. No se evalua, porque ninguna de las dos frases que se comparan mira el estado.
		private static MaintenanceSchedule TheSameStoryOnAPureSchedule()
		{
			MaintenanceSchedule pure =
				new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);

			pure.SeedPlanOperation(
				OperationKindCode.FluidChange, IntervalBasisCode.TimeOrDistance,
				TheDayLimitNothingElseCouldAnswer, TheDistanceLimitNothingElseCouldAnswer,
				DistanceUnitCode.UnitA, true, false,
				TheAnchor, TheOdometerAtTheAnchor, DistanceUnitCode.UnitA);

			pure.RecordReading(
				TheOdometerObservedLater, DistanceUnitCode.UnitA, TheDayItWasObserved,
				ReadingSourceCode.Telemetry);

			return pure;
		}

		// ===== G1 AL LLEGAR A LA PUERTA: DICE LAS DOS Y NO ELIGE ==========================

		[Test]
		public void TheCountdownSaysBothDimensionsAndTheDoorNeverPicks()
		{
			AnOperationWithAReadingAfterIt();

			string said = journal.Api.DescribeCountdown(TheKind, TheDayItIsAsked, TheLanguage.OfTheRecord);

			Assert.That(said, Does.Contain("distancia: te faltan 7.345 km"));
			Assert.That(said, Does.Contain("tiempo: te faltan 255 días"));

			// Y LA PUERTA NO TIENE NINGUN MIEMBRO QUE ELIJA. Los dos que ordenan dimensiones existen
			// en el dominio y ordenan para el ESTADO, no para la frase; reusarlos para mostrar es la
			// tentacion que G1 nombra.
			foreach (string tempting in new[] { "Nearer", "Nearest", "MoreAdvanced", "WhichDecides" })
				Assert.That(TheDoorsMembersContaining(tempting), Is.Empty,
					$"La puerta gano un miembro que ELIGE una dimension: '{tempting}'. La cuenta atras "
					+ "dice las dos siempre; elegir dejaria el numero de la pantalla discrepando del "
					+ "motivo del estado para quien lea los dos juntos.");
		}

		// ===== LAS SIETE SITUACIONES, Y LA QUE NO ES NINGUNA DE LAS SEIS DIMENSIONALES =====

		// UNA CLASE SIN OPERACION AGENDADA CONTESTA, no revienta -- y contesta una situacion PROPIA,
		// que no es ninguna de las suspensiones. Un cero aqui diria "te faltan 0 km" a un vehiculo que
		// no tiene ese servicio agendado.
		[Test]
		public void AKindWithNoOperationScheduledIsAnAnswerAndNotAThrow()
		{
			Assert.That(journal.Api.CanDistanceRemainingBeSaid("Inspection", TheDayItIsAsked), Is.False);
			Assert.That(journal.Api.CanDaysRemainingBeSaid("Inspection", TheDayItIsAsked), Is.False);

			Assert.That(
				journal.Api.ReadDistanceRemainderStanding("Inspection", TheDayItIsAsked),
				Is.EqualTo("NoOperationScheduled"));
			Assert.That(
				journal.Api.ReadTimeRemainderStanding("Inspection", TheDayItIsAsked),
				Is.EqualTo("NoOperationScheduled"));

			Assert.That(
				journal.Api.DescribeCountdown("Inspection", TheDayItIsAsked, TheLanguage.OfTheRecord),
				Is.EqualTo("no hay ninguna operación de Revisión agendada para este vehículo"),
				"Sin operacion agendada no hay dos mitades que decir, asi que la cuenta atras lo dice "
				+ "UNA vez y no dos.");
		}

		// ===== LAS TRES GUARDADAS, Y EL TIPO QUE UNA CARATULA PUEDE NOMBRAR ===============

		// Assert.Throws Y NO Assert.Catch: Catch pasaria justo para el defecto que esto existe para
		// evitar -- que la excepcion INTERNA del dominio salga por la puerta y una caratula solo la
		// pueda atrapar como Exception, junto con un fallo de disco y un defecto de programacion.
		[Test]
		public void TheGuardedRemainderReadsThrowATypeAShellCanNameAndNeverTheDomains()
		{
			Assert.That(journal.Api.CanDistanceRemainingBeSaid("Inspection", TheDayItIsAsked), Is.False);

			Assert.Throws<InvalidOperationException>(
				() => journal.Api.ReadDistanceRemaining("Inspection", TheDayItIsAsked));
			Assert.Throws<InvalidOperationException>(
				() => journal.Api.ReadDistanceRemainingUnit("Inspection", TheDayItIsAsked));
			Assert.Throws<InvalidOperationException>(
				() => journal.Api.ReadDaysRemaining("Inspection", TheDayItIsAsked));
		}

		// Y EL TIPO NO ES LO QUE DISCRIMINA AL PRE-CHEQUEO, QUE ES LO QUE ESTA PRUEBA EXISTE PARA
		// DECIR. MEDIDO: borrar el pre-chequeo de ReadDistanceRemaining no ponia NADA rojo, porque la
		// traduccion del rechazo del dominio devuelve el MISMO tipo. Los dos caminos son
		// indistinguibles por tipo y no lo son por MENSAJE, y el mensaje es lo unico que una persona
		// ve: el del pre-chequeo NOMBRA LA PREGUNTA QUE HAY QUE HACER ANTES, y el de la traduccion
		// habla del ancla de la ocurrencia -- que aqui seria FALSO, porque el ancla no tiene nada que
		// ver con que falte la lectura.
		//
		// Es la forma de MS-2 patch 6 otra vez: el instrumento que nombra la conducta no puede ver el
		// defecto que su nombre describe, y hace falta uno que discrimine.
		[Test]
		public void TheGuardsRefusalNamesTheQuestionToAskFirstAndNotTheAnchor()
		{
			AnOperationWithAReadingAfterIt();

			DateTime beforeTheReadingArrived = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);

			InvalidOperationException refused = Assert.Throws<InvalidOperationException>(
				() => journal.Api.ReadDistanceRemaining(TheKind, beforeTheReadingArrived));

			Assert.That(refused.Message, Does.Contain("CanDistanceRemainingBeSaid"));
			Assert.That(refused.Message, Does.Not.Contain("ancla"));
			Assert.That(refused.InnerException, Is.Null,
				"El pre-chequeo rechaza ANTES de que el dominio reviente, asi que no envuelve nada.");

			InvalidOperationException days = Assert.Throws<InvalidOperationException>(
				() => journal.Api.ReadDaysRemaining("Inspection", TheDayItIsAsked));

			Assert.That(days.Message, Does.Contain("CanDaysRemainingBeSaid"));
			Assert.That(days.Message, Does.Not.Contain("ancla"));
		}

		// Y LA GUARDA SE PREGUNTA CON EL MISMO INSTANTE QUE LA LECTURA VA A USAR, que es lo que hace
		// que las dos hablen de un solo momento. Una operacion instalada y una lectura observada
		// DESPUES del instante preguntado: la mitad de distancia no se puede decir a ese instante y la
		// guarda tiene que verlo con ese mismo instante.
		[Test]
		public void TheGuardIsAskedAtTheSameInstantTheReadWillUse()
		{
			AnOperationWithAReadingAfterIt();

			DateTime beforeTheReadingArrived = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);

			Assert.That(
				journal.Api.CanDistanceRemainingBeSaid(TheKind, beforeTheReadingArrived), Is.False);
			Assert.That(
				journal.Api.ReadDistanceRemainderStanding(TheKind, beforeTheReadingArrived),
				Is.EqualTo("SuspendedByAnAbsentReading"));

			Assert.Throws<InvalidOperationException>(
				() => journal.Api.ReadDistanceRemaining(TheKind, beforeTheReadingArrived));

			// La mitad de TIEMPO si se mide sin ninguna lectura: son dos mitades y no una.
			Assert.That(journal.Api.CanDaysRemainingBeSaid(TheKind, beforeTheReadingArrived), Is.True);
		}

		// EL RECHAZO QUE LA PUERTA NO PUEDE PRE-CHEQUEAR, y por eso lo TRADUCE. Un instante anterior
		// al ancla de la ocurrencia es un rechazo del dominio, y la puerta no tiene ninguna lectura
		// que conteste cual es esa ancla: no hay nada que preguntar antes. Lo que si hay es el
		// requisito de que quien lee pueda atrapar el rechazo POR TIPO.
		[Test]
		public void AnInstantEarlierThanTheAnchorIsRefusedInATypeAShellCanName()
		{
			AnOperationWithAReadingAfterIt();

			DateTime beforeTheOperationExisted = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

			foreach (string member in new[] { "can", "standing", "describe", "countdown" })
				Assert.Throws<InvalidOperationException>(
					() => AskTheCountdown(member, beforeTheOperationExisted),
					$"'{member}' dejo salir la excepcion del dominio. Es INTERNA: ninguna caratula la "
					+ "puede nombrar, y un catch de Exception a secas confundiria un fallo de disco con "
					+ "el rechazo del dominio.");
		}

		// Y LLEGA POR UNA ACCION ORDINARIA DE LA AGENCIA, no solo por un llamador que monta un
		// escenario: un ancla en el FUTURO deja el AHORA de cualquier pantalla antes del ancla.
		[Test]
		public void AnAnchorInTheFutureIsWhatMakesThatRefusalReachableWithoutAnybodyStagingAnything()
		{
			journal.Api.SeedPlanOperation(
				"Inspection", "TimeOrDistance",
				TheDayLimitNothingElseCouldAnswer, TheDistanceLimitNothingElseCouldAnswer,
				TheUnitEverythingIsIn, true, false,
				new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc),
				TheOdometerAtTheAnchor, TheUnitEverythingIsIn);

			Assert.Throws<InvalidOperationException>(
				() => journal.Api.DescribeCountdown("Inspection", TheDayItIsAsked, TheLanguage.OfTheRecord),
				"La agencia puede sembrar un plan anclado adelante, y entonces el instante en que la "
				+ "pantalla pregunta es anterior al ancla sin que nadie haya montado nada.");
		}

		// ===== E49 OBLIGACION 2: ATADO A LO RECIBIDO, NO AL TABLERO =======================

		// DOS LECTURAS RECIBIDAS, Y LA PREGUNTA EN MEDIO: lo que queda tiene que salir de la PRIMERA,
		// que es la ultima usable a ese instante. Una prueba que comparara la puerta contra el
		// dominio, o contra lo que Evaluate decidio, se quedaria verde con el resolvedor corrido.
		[Test]
		public void TheRemainderIsAnchoredToTheReceivedReadingsAndNotToTheBoard()
		{
			AnOperationWithAReadingAfterIt();

			journal.Api.RecordReading(
				39000, TheUnitEverythingIsIn,
				new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc), "Telemetry");

			// Preguntado ENTRE las dos: la de marzo todavia no se observo, asi que lo recorrido es
			// 35000 - 30000 y lo que queda es 12345 - 5000.
			Assert.That(
				journal.Api.ReadDistanceRemaining(TheKind, TheDayItIsAsked),
				Is.EqualTo(TheDistanceThatIsLeft));

			// Y despues de la segunda, lo recorrido es 39000 - 30000 y lo que queda es 12345 - 9000.
			Assert.That(
				journal.Api.ReadDistanceRemaining(
					TheKind, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)),
				Is.EqualTo(3345));
		}

		// ===== EL NOMBRE DEL WIRE SE EXIGE ANTES DE PERFORMAR =============================

		// Un nombre que no es de ningun miembro se rechaza ANTES de que haya consulta, que es la unica
		// forma de rechazarlo sin dejar nada escrito en el journal de la persona.
		[Test]
		public void AKindTheWireDoesNotNameIsRefusedBeforeAnyPerform()
		{
			AnOperationWithAReadingAfterIt();

			string before = TheBytesOf(journal.Folder);

			Assert.Throws<ArgumentException>(
				() => journal.Api.DescribeCountdown("CambioDeFluido", TheDayItIsAsked,
					TheLanguage.OfTheRecord));

			Assert.That(TheBytesOf(journal.Folder), Is.EqualTo(before),
				"El journal quedo byte a byte como estaba.");
		}

		// ===== EL APAREJO =================================================================

		private void AnOperationWithAReadingAfterIt()
		{
			journal.Api.SeedPlanOperation(
				TheKind, "TimeOrDistance",
				TheDayLimitNothingElseCouldAnswer, TheDistanceLimitNothingElseCouldAnswer,
				TheUnitEverythingIsIn, true, false,
				TheAnchor, TheOdometerAtTheAnchor, TheUnitEverythingIsIn);

			journal.Api.RecordReading(
				TheOdometerObservedLater, TheUnitEverythingIsIn, TheDayItWasObserved, "Telemetry");
		}

		private void AskTheCountdown(string member, DateTime asOf)
		{
			if (member == "can") journal.Api.CanDistanceRemainingBeSaid(TheKind, asOf);
			if (member == "standing") journal.Api.ReadDistanceRemainderStanding(TheKind, asOf);
			if (member == "describe") journal.Api.DescribeDistanceRemaining(TheKind, asOf,
				TheLanguage.OfTheRecord);
			if (member == "countdown") journal.Api.DescribeCountdown(TheKind, asOf, TheLanguage.OfTheRecord);
		}

		private static List<string> TheDoorsMembers(string named)
		{
			var found = new List<string>();

			foreach (MethodInfo each in typeof(Concesionario.App.DealershipCarAPI).GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
				if (each.Name == named) found.Add(each.Name);

			return found;
		}

		private static List<string> TheDoorsMembersContaining(string fragment)
		{
			var found = new List<string>();

			foreach (MethodInfo each in typeof(Concesionario.App.DealershipCarAPI).GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
				if (each.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase))
					found.Add(each.Name);

			return found;
		}

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
	}
}
