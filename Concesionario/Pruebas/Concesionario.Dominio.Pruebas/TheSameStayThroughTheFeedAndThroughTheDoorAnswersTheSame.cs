using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Concesionario.App;
using Concesionario.App.Events;
using NUnit.Framework;
using Puppets.Portador;

namespace Concesionario.Dominio.Pruebas
{
	// §8 DEL BRIEF DICE QUE TODO SE RECIBE POR DOS CAMINOS QUE TIENEN QUE PONERSE DE ACUERDO -- por la
	// puerta, cuando una sucursal registra la estadia, y por el feed, cuando el concesionario la
	// publica -- y que los dos aterrizan en el MISMO comando del dominio. Hasta SH-5 nada lo fijaba
	// para las estadias: SH-3 escribio los comandos, SH-4 las lecturas, y ninguno de los dos tuvo el
	// otro camino con que compararlas.
	//
	// LO QUE SE COMPARA SON LAS OCHO LECTURAS, no un contador. Dos caminos que aplican el comando
	// equivocado dejan el mismo numero de entradas y el mismo punto de control; lo unico que los
	// separa es lo que el registro contesta despues.
	//
	// Y SE COMPARA EN DOS ESTADOS (C61). Con una sola estadia cerrada las ocho contestan, pero cinco de
	// ellas contestan lo mismo que contestarian con cualquier estadia; el estado que hace que la
	// comparacion mida algo es el segundo -- dos cerradas y una abierta -- donde cada lectura contesta
	// algo distinto de la de al lado.
	//
	// LO QUE ESTA COMPARACION NO PUEDE VER ESTA ESCRITO ABAJO COMO PRUEBA y no como comentario: los dos
	// caminos corren los MISMOS scripts y normalizan por el MISMO miembro, asi que un defecto que este
	// en los dos lados se pone de acuerdo consigo mismo. El ancla contra los hechos RECIBIDOS es lo que
	// le falta a una comparacion asi, y esta abajo tambien.
	[TestFixture]
	public sealed class TheSameStayThroughTheFeedAndThroughTheDoorAnswersTheSame
	{
		private const string TheYearFrom = "2026-01-01";
		private const string TheYearTo = "2026-12-31";

		private static readonly DateTime InsideTheOpenStay =
			new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime InsideTheFirstClosedStay =
			new DateTime(2026, 5, 12, 12, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime BetweenTheTwoClosedStays =
			new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		[Test]
		public void TheEightReadsAnswerTheSameWhicheverRouteTheStaysArrivedBy()
		{
			using (AScheduleOnARealJournal byTheFeed = AJournal())
			using (AScheduleOnARealJournal byTheDoor = AJournal())
			{
				TheCompletedStayByTheFeed(byTheFeed);
				TheCompletedStayByTheDoor(byTheDoor);

				TheEightAgree(byTheFeed, byTheDoor, "con una sola estadia cerrada en el registro");

				TheRestByTheFeed(byTheFeed);
				TheRestByTheDoor(byTheDoor);

				TheEightAgree(byTheFeed, byTheDoor, "con dos cerradas y una todavia abierta");
			}
		}

		// Y SIGUEN DE ACUERDO DESPUES DE REABRIR LOS DOS JOURNALS. Es lo que el segundo programa no
		// muestra (C53): el journal normaliza tambien, asi que una normalizacion del borde solo se ve
		// comparando una respuesta VIVA contra una REPRODUCIDA -- y aca, ademas, las dos reproducidas
		// entre si, que es donde una diferencia de camino sobreviviria al replay.
		[Test]
		public void AndTheyStillAgreeAfterBothJournalsAreClosedAndReopened()
		{
			using (AScheduleOnARealJournal byTheFeed = AJournal())
			using (AScheduleOnARealJournal byTheDoor = AJournal())
			{
				TheCompletedStayByTheFeed(byTheFeed);
				TheRestByTheFeed(byTheFeed);

				TheCompletedStayByTheDoor(byTheDoor);
				TheRestByTheDoor(byTheDoor);

				Dictionary<string, string> liveByTheFeed = TheEightAnswersOf(byTheFeed);

				byTheFeed.CloseAndReopenFromTheJournal();
				byTheDoor.CloseAndReopenFromTheJournal();

				foreach (KeyValuePair<string, string> each in TheEightAnswersOf(byTheFeed))
					Assert.That(liveByTheFeed[each.Key], Is.EqualTo(each.Value),
						"'" + each.Key + "' contesto distinto despues de reabrir el journal que "
						+ "vivio por el feed.");

				TheEightAgree(byTheFeed, byTheDoor, "despues de reabrir los dos journals");
			}
		}

		// EL ANCLA, y sin ella todo lo de arriba compara DOS CONSUMIDORES DE UN MISMO RESOLVEDOR: los
		// dos caminos pasan por el mismo script y el mismo agregado, asi que se ponen de acuerdo por
		// construccion, incluido el caso en que los dos estan igual de equivocados. Algo tiene que
		// atar un lado a los hechos que LLEGARON, y son literales escritos aca.
		[Test]
		public void AndBothRoutesAnswerWithTheDaysTheBranchActuallySent()
		{
			using (AScheduleOnARealJournal byTheFeed = AJournal())
			using (AScheduleOnARealJournal byTheDoor = AJournal())
			{
				TheCompletedStayByTheFeed(byTheFeed);
				TheRestByTheFeed(byTheFeed);

				TheCompletedStayByTheDoor(byTheDoor);
				TheRestByTheDoor(byTheDoor);

				string[] theDaysThatWereSent =
				{
					"2026-05-11", "2026-05-12", "2026-05-13", "2026-07-06", "2026-07-07",
				};

				Assert.That(
					byTheFeed.Api.DaysInTheShopBetween(TheYearFrom, TheYearTo),
					Is.EqualTo(theDaysThatWereSent),
					"Por el feed, los cinco dias que las dos sucursales dijeron y no otros cinco.");

				Assert.That(
					byTheDoor.Api.DaysInTheShopBetween(TheYearFrom, TheYearTo),
					Is.EqualTo(theDaysThatWereSent),
					"Y por la puerta, los mismos cinco.");

				Assert.That(
					byTheFeed.Api.StaysBetween(TheYearFrom, TheYearTo).Branch,
					Is.EqualTo(new[]
					{
						ADealershipNarrative.TheFirstShopBranch,
						ADealershipNarrative.TheSecondShopBranch,
					}),
					"Y las dos sucursales, con las palabras que llegaron.");
			}
		}

		// LO QUE ESTA COMPARACION NO PUEDE VER, dicho como prueba (C56). Los dos caminos normalizan el
		// instante por UN SOLO miembro de la puerta, y esa es exactamente la razon por la que un
		// defecto adentro de el deja los dos lados de acuerdo. El dia que la ingesta normalice por su
		// cuenta, esta prueba se pone roja -- y ese es el dia en que la comparacion de arriba empieza a
		// medir la normalizacion en vez de heredarla.
		[Test]
		public void TheComparisonCannotSeeANormalizationTheTwoRoutesShare()
		{
			List<string> normalizers = new List<string>();

			foreach (MethodInfo each in typeof(DealershipCarAPI).GetMethods(
				BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance
				| BindingFlags.DeclaredOnly))
				if (each.Name == "InUtc") normalizers.Add(each.Name);

			Assert.That(normalizers, Has.Count.EqualTo(1),
				"La puerta tiene " + normalizers.Count + " miembros llamados InUtc. Con uno solo, los "
				+ "dos caminos comparten la normalizacion y compararlos entre si no dice nada sobre "
				+ "ella; con dos, cada camino tiene la suya y la comparacion pasa a medirla.");
		}

		// ------------------------------------------------------ el aparejo

		private static AScheduleOnARealJournal AJournal()
		{
			return new AScheduleOnARealJournal(ADealershipNarrative.TheVin, "Standard");
		}

		// LOS MISMOS HECHOS, dichos una sola vez: los dos caminos leen las mismas constantes de la
		// narrativa. Escribirlos dos veces dejaria que una tercera cosa -- un dia mal copiado -- los
		// separara sin que la comparacion supiera de que se trata.
		private static void TheCompletedStayByTheFeed(AScheduleOnARealJournal journal)
		{
			Apply(journal, ADealershipNarrative.CompletedStay(
				30, ADealershipNarrative.TheFirstShopBranch, ADealershipNarrative.TheFirstShopSubject,
				ADealershipNarrative.TheFirstShopEntry, ADealershipNarrative.TheFirstShopCollection,
				ADealershipNarrative.TheFirstShopDays));
		}

		private static void TheRestByTheFeed(AScheduleOnARealJournal journal)
		{
			Apply(journal, ADealershipNarrative.ShopEntry(
				31, ADealershipNarrative.TheSecondShopBranch,
				ADealershipNarrative.TheSecondShopSubject, ADealershipNarrative.TheSecondShopEntry));

			Apply(journal, ADealershipNarrative.ShopCollection(
				32, ADealershipNarrative.TheSecondShopCollection,
				ADealershipNarrative.TheSecondShopDays));

			Apply(journal, ADealershipNarrative.ShopEntry(
				33, ADealershipNarrative.TheOpenShopBranch, ADealershipNarrative.TheOpenShopSubject,
				ADealershipNarrative.TheOpenShopEntry));
		}

		private static void TheCompletedStayByTheDoor(AScheduleOnARealJournal journal)
		{
			journal.Api.RecordShopStay(
				ADealershipNarrative.TheFirstShopBranch, ADealershipNarrative.TheFirstShopSubject,
				TheInstant(ADealershipNarrative.TheFirstShopEntry),
				TheInstant(ADealershipNarrative.TheFirstShopCollection),
				ADealershipNarrative.TheFirstShopDays);
		}

		private static void TheRestByTheDoor(AScheduleOnARealJournal journal)
		{
			journal.Api.RecordShopEntry(
				ADealershipNarrative.TheSecondShopBranch, ADealershipNarrative.TheSecondShopSubject,
				TheInstant(ADealershipNarrative.TheSecondShopEntry));

			journal.Api.RecordShopCollection(
				TheInstant(ADealershipNarrative.TheSecondShopCollection),
				ADealershipNarrative.TheSecondShopDays);

			journal.Api.RecordShopEntry(
				ADealershipNarrative.TheOpenShopBranch, ADealershipNarrative.TheOpenShopSubject,
				TheInstant(ADealershipNarrative.TheOpenShopEntry));
		}

		private static void Apply(AScheduleOnARealJournal journal, SenalDeEvento signal)
		{
			IMensajeDeEvento message = DealershipEventRoute.Route(signal);

			Assert.That(message, Is.Not.Null,
				"La ruta descarto " + signal.Tipo + ", asi que ese hecho no llego por el feed y la "
				+ "comparacion estaria entre un registro y otro que no tiene lo mismo.");

			Assert.That(journal.Api.Apply(message), Is.EqualTo(""),
				"El dominio rechazo " + signal.Tipo + " por el feed.");
		}

		private static DateTime TheInstant(string said)
		{
			return DateTime.Parse(
				said, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
		}

		private static void TheEightAgree(
			AScheduleOnARealJournal byTheFeed, AScheduleOnARealJournal byTheDoor, string state)
		{
			Dictionary<string, string> feed = TheEightAnswersOf(byTheFeed);
			Dictionary<string, string> door = TheEightAnswersOf(byTheDoor);

			Assert.Multiple(() =>
			{
				foreach (KeyValuePair<string, string> each in feed)
					Assert.That(door[each.Key], Is.EqualTo(each.Value),
						"'" + each.Key + "' contesta distinto segun por donde llego la estadia, "
						+ state + ". Los dos caminos aterrizan en el mismo comando del dominio, asi "
						+ "que una diferencia aca es del camino y no del registro.");
			});
		}

		// Las ocho, cada una con el nombre de su miembro al lado (C60), reducidas a texto para poder
		// compararlas de a pares. La fila entera va en la cadena: comparar solo la cuenta dejaria pasar
		// una columna con otro contenido.
		private static Dictionary<string, string> TheEightAnswersOf(AScheduleOnARealJournal journal)
		{
			ShopStayRecord record = journal.Api.StaysBetween(TheYearFrom, TheYearTo);

			return new Dictionary<string, string>
			{
				{
					"IsInsideTheShopAt(dentro de la abierta)",
					journal.Api.IsInsideTheShopAt(InsideTheOpenStay).ToString()
				},
				{
					"IsInsideTheShopAt(entre las dos cerradas)",
					journal.Api.IsInsideTheShopAt(BetweenTheTwoClosedStays).ToString()
				},
				{
					"DescribeTheStayAt(dentro de la abierta)",
					journal.Api.DescribeTheStayAt(InsideTheOpenStay)
				},
				{
					"DescribeTheStayAt(dentro de la primera cerrada)",
					journal.Api.DescribeTheStayAt(InsideTheFirstClosedStay)
				},
				{
					"HasAnyStayBetween",
					journal.Api.HasAnyStayBetween(TheYearFrom, TheYearTo).ToString()
				},
				{
					"DaysInTheShopBetween",
					string.Join("|", journal.Api.DaysInTheShopBetween(TheYearFrom, TheYearTo))
				},
				{
					"DaysInTheShopCountBetween",
					journal.Api.DaysInTheShopCountBetween(TheYearFrom, TheYearTo).ToString()
				},
				{
					"DescribeDaysInTheShopBetween",
					journal.Api.DescribeDaysInTheShopBetween(TheYearFrom, TheYearTo)
				},
				{
					"StayCountBetween",
					journal.Api.StayCountBetween(TheYearFrom, TheYearTo).ToString()
				},
				{
					"StaysBetween",
					string.Join("|", record.Branch) + " / " + string.Join("|", record.Subject)
					+ " / " + string.Join("|", record.EnteredAt)
					+ " / " + string.Join("|", record.CoveredDays)
					+ " / " + string.Join("|", record.Says)
				},
			};
		}
	}
}
