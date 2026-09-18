using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Concesionario.App;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LAS ONCE LECTURAS SIN COMPONER DE T5, MIEMBRO POR MIEMBRO Y ESTADO POR ESTADO. El hermano de
	// ASpecificationIsReadBackThroughTheDoor del lado del perfil y del odometro: siete sobre el
	// perfil -- las dos de presencia, la mitad publicada y las cuatro guardadas -- y cuatro sobre la
	// agenda, todas llaveadas por el instante preguntado.
	//
	// POR QUE SE BARREN LOS ESTADOS Y NO SOLO LOS MIEMBROS (C61). Un perfil al que no le dijeron
	// nada contesta "no publicado" a casi todo, asi que un barrido tomado ahi compara once ausencias
	// que se ponen de acuerdo correctamente sobre nada. Los estados son CINCO -- sin nada, con sobre,
	// con entrega, con los dos, y con los dos mas una lectura -- y el renglon que hace que el
	// barrido mida algo es que en el ultimo contestan LAS ONCE, sin una sola guardada rechazando.
	//
	// Y CADA RESPUESTA VA CON EL NOMBRE DE SU MIEMBRO AL LADO (C60). Is.Not.Empty prueba que llego
	// una respuesta, nunca que es la de ESE miembro: con el nombre escrito en la cadena, una lectura
	// que contestara siempre por el sobre cambia la comparacion en vez de pasar once veces.
	//
	// TODO EN UnitB Y CON NUMEROS QUE NO SON CERO, por lo mismo. UnitA es el PRIMER miembro del
	// enum, asi que una ranura que el motor hubiera dejado por defecto contestaria 'km' y la
	// afirmacion pasaria sin haber medido que el codigo cruzo. El caso UnitA vive como espejo.
	//
	// LO QUE ESTE FIXTURE NO PUEDE VER: que aparezca una DOCEAVA lectura del perfil sin barrer. La
	// lista de once es de nombres y no sale de ninguna reflexion -- no hay predicado que separe "las
	// lecturas del perfil" del resto de la puerta -- asi que quien la agregue tiene que escribirla
	// aca. Lo que si se pone rojo con cualquier miembro nuevo de la puerta es el censo de
	// EveryScriptOfTheDoorHasBeenRun, y de ahi sale el aviso para venir a este barrido.
	[TestFixture]
	public sealed class AProfileIsReadBackThroughTheDoor
	{
		private const string TheVin = "1HGCM82633A004352";
		private const string TheUnitEverythingIsIn = "UnitB";

		private const int ThirtySixMonths = 36;
		private const int SixtyThousand = 60000;
		private const int TheOdometerAtDelivery = 1200;
		private const int TheOdometerToday = 18450;

		// El relleno fechado HACIA ADELANTE: llego, esta en el journal, y al instante preguntado
		// todavia no se habia observado. Ninguna de las cuatro lecturas del odometro puede darlo.
		private const int TheOdometerNotYetObserved = 21990;

		private static readonly DateTime TheDayItWasPublished =
			new DateTime(2023, 11, 15, 9, 30, 45, DateTimeKind.Utc);

		private static readonly DateTime TheDayItWasHandedOver =
			new DateTime(2024, 1, 1, 8, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItWasObserved =
			new DateTime(2026, 3, 10, 7, 15, 20, DateTimeKind.Utc);

		private static readonly DateTime TheInstantAsked =
			new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayAfterTheQuestion =
			new DateTime(2026, 9, 20, 6, 40, 10, DateTimeKind.Utc);

		// Lo que se escribe en la cadena de estado cuando una lectura guardada rechaza. Es una
		// respuesta del barrido y no una falla: la mitad guardada de E41 tiene que rechazar donde no
		// hay hecho, y verlo escrito es lo que hace comparables los cinco estados.
		private const string ItRefused = "<refused>";

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

		// ===== LAS ONCE SON ONCE, Y CADA UNA TIENE LA FORMA QUE DICE ======================

		// La aridad va en la afirmacion porque es la mitad que separa una lectura de una MEDICION
		// pedida a quien pregunta: las siete del perfil no toman nada y las cuatro de la agenda toman
		// exactamente el instante. Un argumento de mas en cualquiera de las once se ve aca antes que
		// en el censo de L1.
		[Test]
		public void TheElevenReadsAreDeclaredOnTheDoorWithTheShapeThisSweepAsks()
		{
			foreach (KeyValuePair<string, string> each in TheShapeOfEachOfTheEleven())
			{
				MethodInfo declared = TheDoorsOnlyMemberCalled(each.Key);

				Assert.That(
					declared.ReturnType.Name + "/" + declared.GetParameters().Length,
					Is.EqualTo(each.Value),
					$"'{each.Key}' cambio de forma. Una lectura que empieza a tomar un argumento de "
					+ "mas es por donde entra un hecho del vehiculo pedido a quien pregunta.");
			}
		}

		// ===== EL ESTADO EN QUE CONTESTAN LAS ONCE ========================================

		// EL RENGLON QUE HACE QUE EL BARRIDO MIDA ALGO. Con el sobre publicado, la entrega registrada
		// y una lectura observada antes del instante preguntado, ninguna de las once rechaza -- y
		// cada una se afirma por su valor, no por no venir vacia.
		[Test]
		public void EveryOneOfTheElevenAnswersWhenTheRecordHoldsEveryFact()
		{
			TheDealershipPublishedTheEnvelope(journal);
			TheDealershipRecordedTheDelivery(journal);
			TheFeedBroughtAReading(journal);

			Assert.That(journal.Api.IsWarrantyEnvelopePublished(), Is.True);

			Assert.That(
				journal.Api.DescribePublishedWarrantyEnvelope(TheLanguage.OfTheRecord),
				Does.Contain("36 meses o 60.000 mi"),
				"La frase publicada trae las DOS mitades con su unidad. Es la unica manera de "
				+ "llegar a los dos limites: la puerta no los da sueltos, porque una caratula que "
				+ "arme '36 meses o 60000' con escalares se llevo la composicion fuera del dominio.");

			Assert.That(
				journal.Api.ReadEnvelopePublishedAt(), Is.EqualTo(TheDayItWasPublished));

			Assert.That(journal.Api.HasDeliveryBeenRecorded(), Is.True);
			Assert.That(journal.Api.ReadDeliveredAt(), Is.EqualTo(TheDayItWasHandedOver));
			Assert.That(journal.Api.ReadDeliveryReading(), Is.EqualTo(TheOdometerAtDelivery));
			Assert.That(journal.Api.ReadDeliveryUnit(), Is.EqualTo("mi"));

			Assert.That(journal.Api.HasAnOdometerReading(TheInstantAsked), Is.True);
			Assert.That(journal.Api.ReadOdometer(TheInstantAsked), Is.EqualTo(TheOdometerToday));
			Assert.That(
				journal.Api.ReadOdometer(TheInstantAsked),
				Is.Not.EqualTo(TheOdometerNotYetObserved),
				"Y NO es el relleno fechado hacia adelante. El feed lo trajo y esta en el journal; al "
				+ "instante preguntado todavia no se habia observado, asi que ni Evaluate ni esta "
				+ "lectura lo pueden usar.");
			Assert.That(journal.Api.ReadOdometerUnit(TheInstantAsked), Is.EqualTo("mi"));
			Assert.That(
				journal.Api.ReadOdometerObservedAt(TheInstantAsked),
				Is.EqualTo(TheDayItWasObserved),
				"El instante en que se OBSERVO la lectura, no el instante en que se pregunto. Sin "
				+ "este numero, una frase compuesta con una lectura de marzo se lee como tomada hoy.");

			Assert.That(EveryAnswerOf(journal), Does.Not.Contain(ItRefused),
				"Y ninguna guardada rechazo: este es el estado en que las once contestan.");
		}

		// LOS DOS NUMEROS DEL VEHICULO NO SON EL MISMO, dicho aparte porque es la forma en que un
		// barrido queda verde midiendo una sola lectura. El odometro de la entrega y el de hoy son
		// dos hechos distintos y la puerta los saca por dos miembros distintos.
		[Test]
		public void TheDeliveryOdometerAndTodaysOdometerAreTwoDifferentFacts()
		{
			TheDealershipRecordedTheDelivery(journal);
			TheFeedBroughtAReading(journal);

			Assert.That(
				journal.Api.ReadDeliveryReading(),
				Is.Not.EqualTo(journal.Api.ReadOdometer(TheInstantAsked)));

			Assert.That(journal.Api.ReadDeliveryReading(), Is.EqualTo(TheOdometerAtDelivery));
			Assert.That(journal.Api.ReadOdometer(TheInstantAsked), Is.EqualTo(TheOdometerToday));
		}

		// ===== LOS CINCO ESTADOS, Y QUE NO CONTESTAN LO MISMO =============================

		// C61 AL PIE DE LA LETRA: se enumeran los ESTADOS y no solo los miembros. Los cinco dejan
		// cinco cadenas distintas dos a dos, y el numero de guardadas que rechazan BAJA de siete a
		// cero segun lo que el registro va llevando. Un barrido tomado solo en el primero compararia
		// once ausencias que se ponen de acuerdo sobre nada.
		[Test]
		public void TheFiveStatesTheRecordCanBeInDoNotAnswerTheSame()
		{
			List<KeyValuePair<string, string>> said = EveryStateAnswered();

			for (int one = 0; one < said.Count; one++)
				for (int other = one + 1; other < said.Count; other++)
					Assert.That(said[one].Value, Is.Not.EqualTo(said[other].Value),
						$"'{said[one].Key}' y '{said[other].Key}' contestan lo MISMO. Dos estados del "
						+ "registro que dejan la misma respuesta quieren decir que el barrido no esta "
						+ "leyendo lo que cambio entre ellos.");
		}

		[Test]
		public void TheGuardedReadsStopRefusingAsTheRecordFillsUp()
		{
			var refusals = new List<KeyValuePair<string, int>>();

			foreach (KeyValuePair<string, string> each in EveryStateAnswered())
				refusals.Add(new KeyValuePair<string, int>(each.Key, HowManyRefused(each.Value)));

			Assert.That(refusals, Is.EqualTo(new[]
			{
				new KeyValuePair<string, int>("told nothing", 7),
				new KeyValuePair<string, int>("the envelope only", 6),
				new KeyValuePair<string, int>("the delivery only", 4),
				new KeyValuePair<string, int>("both", 3),
				new KeyValuePair<string, int>("both and a reading", 0),
			}),
				"Las cuatro totales contestan siempre y las siete guardadas dejan de rechazar segun "
				+ "el hecho que las paga entra al registro. Una guardada que contestara sin el hecho "
				+ "-- un cero, un instante en blanco -- bajaria esta cuenta sin que nada mas lo diga.");
		}

		// ===== LA UNIDAD, QUE ES SU PROPIO DISCRIMINADOR ==================================

		// LAS DOS LECTURAS DE UNIDAD DICEN LA UNIDAD, y por eso se prueban con UnitB. 'km' es lo que
		// contestaria una ranura que el motor dejo por defecto -- UnitA es el primer miembro -- asi
		// que afirmar 'mi' es lo unico que prueba que el codigo cruzo.
		[Test]
		public void BothUnitReadsSayTheUnitTheFactWasRecordedInAndNotTheFirstMember()
		{
			TheDealershipRecordedTheDelivery(journal);
			TheFeedBroughtAReading(journal);

			Assert.That(journal.Api.ReadDeliveryUnit(), Is.EqualTo("mi"));
			Assert.That(journal.Api.ReadDeliveryUnit(), Is.Not.EqualTo("km"));

			Assert.That(journal.Api.ReadOdometerUnit(TheInstantAsked), Is.EqualTo("mi"));
			Assert.That(journal.Api.ReadOdometerUnit(TheInstantAsked), Is.Not.EqualTo("km"));
		}

		// EL ESPEJO, que es lo que hace que las dos afirmaciones de arriba discriminen: en la otra
		// unidad las dos contestan la otra cosa.
		[Test]
		public void TheOtherUnitComesBackAsItselfOnBothReads()
		{
			journal.Api.RecordDelivery(TheDayItWasHandedOver, TheOdometerAtDelivery, "UnitA");
			journal.Api.RecordReading(
				TheOdometerToday, "UnitA", TheDayItWasObserved, "Telemetry");

			Assert.That(journal.Api.ReadDeliveryUnit(), Is.EqualTo("km"));
			Assert.That(journal.Api.ReadOdometerUnit(TheInstantAsked), Is.EqualTo("km"));
		}

		// ===== Y NINGUNA DE LAS ONCE ESCRIBE ==============================================

		// El error que esta prueba existe para atrapar es de una sola letra: PerformCommand donde iba
		// PerformQuery. Una lectura que escribe le agrega una entrada al journal de la persona cada
		// vez que alguien pregunta, para siempre, y el estado sigue siendo correcto -- asi que
		// ninguna otra prueba de este fixture se pondria roja.
		[Test]
		public void NoneOfTheElevenWritesAnEntry()
		{
			TheDealershipPublishedTheEnvelope(journal);
			TheDealershipRecordedTheDelivery(journal);
			TheFeedBroughtAReading(journal);

			string before = TheBytesOf(journal.Folder);

			Assert.That(EveryAnswerOf(journal), Does.Not.Contain(ItRefused));

			Assert.That(TheBytesOf(journal.Folder), Is.EqualTo(before),
				"El journal quedo byte a byte como estaba.");
		}

		// Los cinco estados, cada uno sobre su propio journal: no hay forma de DESPUBLICAR un sobre,
		// asi que "solo la entrega" no se puede alcanzar desde "con sobre" y los cinco tienen que
		// nacer aparte.
		private static List<KeyValuePair<string, string>> EveryStateAnswered()
		{
			var said = new List<KeyValuePair<string, string>>();

			foreach (KeyValuePair<string, Action<AScheduleOnARealJournal>> each in TheFiveStates())
				using (AScheduleOnARealJournal one =
					new AScheduleOnARealJournal(TheVin, "Standard"))
				{
					each.Value(one);

					said.Add(new KeyValuePair<string, string>(each.Key, EveryAnswerOf(one)));
				}

			return said;
		}

		private static List<KeyValuePair<string, Action<AScheduleOnARealJournal>>> TheFiveStates()
		{
			return new List<KeyValuePair<string, Action<AScheduleOnARealJournal>>>
			{
				new KeyValuePair<string, Action<AScheduleOnARealJournal>>(
					"told nothing", one => { }),

				new KeyValuePair<string, Action<AScheduleOnARealJournal>>(
					"the envelope only", TheDealershipPublishedTheEnvelope),

				new KeyValuePair<string, Action<AScheduleOnARealJournal>>(
					"the delivery only", TheDealershipRecordedTheDelivery),

				new KeyValuePair<string, Action<AScheduleOnARealJournal>>(
					"both",
					one => {
						TheDealershipPublishedTheEnvelope(one);
						TheDealershipRecordedTheDelivery(one);
					}),

				new KeyValuePair<string, Action<AScheduleOnARealJournal>>(
					"both and a reading",
					one => {
						TheDealershipPublishedTheEnvelope(one);
						TheDealershipRecordedTheDelivery(one);
						TheFeedBroughtAReading(one);
					}),
			};
		}

		// LAS ONCE EN UNA SOLA CADENA COMPARABLE, cada respuesta con el NOMBRE de su miembro al lado.
		// Es lo que compara los cinco estados entre si, y el nombre escrito es lo que impide que una
		// lectura que contestara siempre lo mismo pase el barrido once veces.
		private static string EveryAnswerOf(AScheduleOnARealJournal one)
		{
			StringBuilder said = new StringBuilder();

			said.Append("IsWarrantyEnvelopePublished=")
				.Append(one.Api.IsWarrantyEnvelopePublished());

			said.Append("\nDescribePublishedWarrantyEnvelope=")
				.Append(one.Api.DescribePublishedWarrantyEnvelope(TheLanguage.OfTheRecord));

			said.Append("\nReadEnvelopePublishedAt=")
				.Append(OrRefused(() => one.Api.ReadEnvelopePublishedAt().ToString("O")));

			said.Append("\nHasDeliveryBeenRecorded=").Append(one.Api.HasDeliveryBeenRecorded());

			said.Append("\nReadDeliveredAt=")
				.Append(OrRefused(() => one.Api.ReadDeliveredAt().ToString("O")));

			said.Append("\nReadDeliveryReading=")
				.Append(OrRefused(() => one.Api.ReadDeliveryReading().ToString()));

			said.Append("\nReadDeliveryUnit=").Append(OrRefused(() => one.Api.ReadDeliveryUnit()));

			said.Append("\nHasAnOdometerReading=")
				.Append(one.Api.HasAnOdometerReading(TheInstantAsked));

			said.Append("\nReadOdometer=")
				.Append(OrRefused(() => one.Api.ReadOdometer(TheInstantAsked).ToString()));

			said.Append("\nReadOdometerUnit=")
				.Append(OrRefused(() => one.Api.ReadOdometerUnit(TheInstantAsked)));

			said.Append("\nReadOdometerObservedAt=")
				.Append(OrRefused(
					() => one.Api.ReadOdometerObservedAt(TheInstantAsked).ToString("O")));

			return said.ToString();
		}

		// Solo se atrapa el rechazo de la PUERTA. Cualquier otra excepcion sale, porque una lectura
		// que revienta por otra razon es un defecto y no un estado del registro.
		private static string OrRefused(Func<string> guarded)
		{
			try
			{
				return guarded();
			}
			catch (InvalidOperationException)
			{
				return ItRefused;
			}
		}

		private static int HowManyRefused(string said)
		{
			int refused = 0;

			for (int at = said.IndexOf(ItRefused, StringComparison.Ordinal); at >= 0;
				at = said.IndexOf(ItRefused, at + 1, StringComparison.Ordinal))
				refused++;

			return refused;
		}

		private static Dictionary<string, string> TheShapeOfEachOfTheEleven()
		{
			return new Dictionary<string, string>
			{
				{ "IsWarrantyEnvelopePublished",       "Boolean/0" },
				{ "DescribePublishedWarrantyEnvelope", "String/1" },
				{ "ReadEnvelopePublishedAt",           "DateTime/0" },
				{ "HasDeliveryBeenRecorded",           "Boolean/0" },
				{ "ReadDeliveredAt",                   "DateTime/0" },
				{ "ReadDeliveryReading",               "Int32/0" },
				{ "ReadDeliveryUnit",                  "String/0" },
				{ "HasAnOdometerReading",              "Boolean/1" },
				{ "ReadOdometer",                      "Int32/1" },
				{ "ReadOdometerUnit",                  "String/1" },
				{ "ReadOdometerObservedAt",            "DateTime/1" },
			};
		}

		// Y REVIENTA SI HAY DOS CON ESE NOMBRE: devolver el primero afirmaria sobre una sobrecarga y
		// dejaria la otra sin mirar, que es la cobertura que un barrido cree tener y no tiene.
		private static MethodInfo TheDoorsOnlyMemberCalled(string name)
		{
			var found = new List<MethodInfo>();

			foreach (MethodInfo each in typeof(DealershipCarAPI).GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
				if (each.Name == name) found.Add(each);

			Assert.That(found, Has.Count.EqualTo(1),
				$"La puerta tiene {found.Count} miembros llamados '{name}'.");

			return found[0];
		}

		private static void TheDealershipPublishedTheEnvelope(AScheduleOnARealJournal one)
		{
			one.Api.PublishWarrantyEnvelope(
				ThirtySixMonths, SixtyThousand, TheUnitEverythingIsIn, TheDayItWasPublished);
		}

		private static void TheDealershipRecordedTheDelivery(AScheduleOnARealJournal one)
		{
			one.Api.RecordDelivery(
				TheDayItWasHandedOver, TheOdometerAtDelivery, TheUnitEverythingIsIn);
		}

		// DOS LECTURAS Y NO UNA, Y LA SEGUNDA FECHADA DESPUES DEL INSTANTE PREGUNTADO. MEDIDO en la
		// falsificacion de T5: con una sola lectura, quitarle el instante a la lectura del odometro
		// -- resolver "la ultima registrada, y punto" -- dejaba este barrido entero VERDE, porque con
		// una sola lectura las dos respuestas coinciden. Con el relleno fechado hacia adelante, el
		// barrido tambien se pone rojo, y la lectura de aca tiene que ser la primera.
		private static void TheFeedBroughtAReading(AScheduleOnARealJournal one)
		{
			one.Api.RecordReading(
				TheOdometerToday, TheUnitEverythingIsIn, TheDayItWasObserved, "Telemetry");

			one.Api.RecordReading(
				TheOdometerNotYetObserved, TheUnitEverythingIsIn, TheDayAfterTheQuestion,
				"Telemetry");
		}

		// El arbol entero del journal reducido a una cadena. Repetido a proposito -- cada fixture de
		// esta suite es dueño de su aparejo.
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
