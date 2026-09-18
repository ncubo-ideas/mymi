using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Concesionario.App.Wording;
using Concesionario.Wasm;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LOS CONJUNTOS CERRADOS SE TRADUCEN, Y ESTA FIXTURE ES LO QUE LO SOSTIENE.
	//
	// Un miembro de un conjunto cerrado -- Overdue, EngineOil, Spare -- PARECE dato del registro y no lo
	// es: no salio del feed del concesionario, lo eligio el dominio de una lista suya. En pantalla es
	// indistinguible a ojo de un nombre de sistema que SI vino del registro, y ahi esta la trampa: lo
	// que el dominio elige se traduce, lo que la institucion asento no.
	//
	// DE LOS DOS MODOS EN QUE ESTO SE PUEDE ROMPER, SOLO UNO SE CUIDA SOLO:
	//
	//   una clave MAL ESCRITA cae al nombre separado en mayusculas, que se ve raro y se nota;
	//   una clave QUE FALTA EN UN SOLO IDIOMA no la nota nadie que lea en el otro -- y quien lea en el
	//   que falta ve el codigo crudo del canon en medio de una pantalla traducida.
	//
	// LO QUE SE AFIRMA VA EN LA DIRECCION QUE IMPORTA: que todo miembro del dominio tenga etiqueta, no
	// que toda etiqueta tenga miembro. El tablero dice un estado que el dominio no tiene -- Missed, que
	// es de la pantalla -- y eso es legitimo.
	[TestFixture]
	public sealed class EveryClosedSetIsSaidInBothLanguages
	{
		private static readonly Regex AHole = new Regex(@"\{(\w+)\}", RegexOptions.Compiled);

		private static Phrases InSpanish => Phrases.Of(Phrases.LanguageOfTheRecord);

		private static Phrases InEnglish => Phrases.Of(Phrases.TheOtherLanguage);

		[Test]
		public void TheTwoCataloguesOfTheAppCarryTheSameKeys()
		{
			string[] spanish = InSpanish.EveryKey().OrderBy(key => key, StringComparer.Ordinal).ToArray();
			string[] english = InEnglish.EveryKey().OrderBy(key => key, StringComparer.Ordinal).ToArray();

			Assert.That(spanish, Is.Not.Empty,
				"El catalogo en espanol vino vacio, asi que esta fixture no esta afirmando nada.");

			Assert.Multiple(() =>
			{
				Assert.That(english.Where(key => !spanish.Contains(key)), Is.Empty,
					"claves que el catalogo en ingles tiene y el espanol no");
				Assert.That(spanish.Where(key => !english.Contains(key)), Is.Empty,
					"claves que el catalogo en espanol tiene y el ingles no: quien lea en ingles ve el "
					+ "nombre del canon crudo en medio de una pantalla traducida");
			});
		}

		[Test]
		public void NoKeyOfEitherCatalogueIsLeftWithoutWords()
		{
			Assert.Multiple(() =>
			{
				foreach (string key in InSpanish.EveryKey())
					Assert.That(InSpanish.Say(key).Trim(), Is.Not.Empty, "es · " + key);

				foreach (string key in InEnglish.EveryKey())
					Assert.That(InEnglish.Say(key).Trim(), Is.Not.Empty, "en · " + key);
			});
		}

		[Test]
		public void EveryKeySaysTheSameHolesInBothLanguages()
		{
			Assert.Multiple(() =>
			{
				foreach (string key in InSpanish.EveryKey())
				{
					if (!InEnglish.Knows(key)) continue;

					Assert.That(TheHolesOf(InEnglish.Say(key)), Is.EqualTo(TheHolesOf(InSpanish.Say(key))),
						key + ": los dos idiomas no nombran los mismos huecos. Uno que falta borra un "
						+ "dato de la frase y se lee bien, que es peor que uno que sobra.");
				}
			});
		}

		// LAS NUEVE OPERACIONES, LOS SEIS FLUIDOS Y LAS TRES POSICIONES, en las DOS familias y en los
		// DOS idiomas. Las tres listas ya estan atadas al dominio miembro por miembro por
		// TheClosedSetsInTheBrowserAreTheDomainsOwn, asi que una clase nueva del dominio llega hasta aca.
		[Test]
		public void EveryMemberTheScreenMirrorsIsSaidInBothFamiliesAndBothLanguages()
		{
			Assert.Multiple(() =>
			{
				foreach (string kind in AppState.TheNineKinds)
				{
					ItIsSaid("canon.operation", kind);
					ItIsSaid("board.operation", kind);
					ItIsSaid("board.operation.short", kind);
				}

				foreach (string kind in AppState.TheSixFluidKinds)
				{
					ItIsSaid("canon.fluid", kind);
					ItIsSaid("board.fluid", kind);
				}

				foreach (string position in AppState.TheThreeTyrePositions)
				{
					ItIsSaid("canon.tyre", position);
					ItIsSaid("board.tyre", position);
				}
			});
		}

		// Y LOS SIETE ESTADOS DEL DOMINIO, leidos del dominio y no de una lista escrita aqui: DueState es
		// una clase con miembros estaticos y no un enum, asi que se enumeran por reflexion sobre sus
		// campos. Un octavo estado que alguien agregue llega a esta prueba sin que nadie lo copie.
		[Test]
		public void EveryDueStateOfTheDomainIsSaidInBothFamiliesAndBothLanguages()
		{
			string[] theDomains = typeof(DueState)
				.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
				.Where(each => each.FieldType == typeof(DueState))
				.Select(each => ((DueState)each.GetValue(null)).Name)
				.ToArray();

			Assert.That(theDomains, Is.Not.Empty,
				"No se leyo ningun DueState del dominio, asi que esta prueba no afirma nada.");

			Assert.Multiple(() =>
			{
				foreach (string state in theDomains)
				{
					ItIsSaid("canon.dueState", state);
					ItIsSaid("board.dueState", state);
				}
			});
		}

		// Y LOS DOCE MESES EN LAS DOS FORMAS, que es lo que hace que una fecha del registro se diga en
		// palabras. Un mes que falte sale a pantalla como {month.9} en medio de una fecha.
		[Test]
		public void EveryMonthIsSaidInBothFormsAndBothLanguages()
		{
			Assert.Multiple(() =>
			{
				foreach (string month in new[]
					{ "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" })
				{
					ItIsSaid("month", month);
					ItIsSaid("month.short", month);
				}
			});
		}

		// LOS DOS CONJUNTOS QUE EL DOMINIO NOMBRA DENTRO DE SUS PROPIAS FRASES, y que la caratula no
		// espeja en ninguna tabla: el perfil de uso que ignora la distancia, y el motivo con que la
		// persona aplazo. Desde que el dominio dejo de redactar los manda por su CODIGO, asi que un
		// miembro nuevo sale a pantalla como {canon.usageProfile.Loquesea} si nadie lo dice.
		[Test]
		public void EveryUsageProfileAndEveryDeferralReasonIsSaidInBothLanguages()
		{
			string[] profiles = TheMembersOf<UsageProfile>(each => each.Name);
			string[] reasons = TheMembersOf<DeferralReason>(each => each.Name);

			Assert.That(profiles, Is.Not.Empty);
			Assert.That(reasons, Is.Not.Empty);

			Assert.Multiple(() =>
			{
				foreach (string profile in profiles) ItIsSaid("canon.usageProfile", profile);
				foreach (string reason in reasons) ItIsSaid("canon.deferralReason", reason);
			});
		}

		// Y LAS CINCO FUENTES DE UNA LECTURA DEL ODOMETRO, que es el conjunto cerrado que la serie
		// nombra en cada fila. Nada se pesa ni se descarta por ella -- decirla es la obligacion
		// entera --, asi que una fuente que el catalogo no diga sale a pantalla como el nombre del
		// miembro separado en mayusculas en medio de una fila traducida.
		[Test]
		public void EveryReadingSourceIsSaidInBothLanguages()
		{
			string[] sources = TheMembersOf<ReadingSource>(each => each.Name);

			Assert.That(sources, Has.Length.EqualTo(5));

			Assert.Multiple(() =>
			{
				foreach (string source in sources) ItIsSaid("canon.readingSource", source);
			});
		}

		// LAS SIETE SITUACIONES DE UN RESTO, PEDIDAS AL RENDERIZADOR Y NO AL CATALOGO. Es la unica
		// forma de ver las dos mitades a la vez: que la clave EXISTA, y que el renderizador sepa a
		// cual de las familias mandar cada situacion -- tres de las siete dicen la clausula del
		// motivo, que vive en otra familia. Una clave que falta sale como {clave} entre llaves y no
		// revienta, asi que lo que esta prueba busca es la llave abierta.
		[Test]
		public void EveryRemainderStandingTheDomainCanChooseIsSaidWholeInBothLanguages()
		{
			string[] standings = TheMembersOf<RemainderStanding>(each => each.Name);

			Assert.That(standings, Has.Length.EqualTo(7));

			Assert.Multiple(() =>
			{
				foreach (string standing in standings)
					foreach (Phrases said in new[] { InSpanish, InEnglish })
						Assert.That(
							TheWording.TheRemainder(
								said, standing, IntervalDimension.Distance, new[] { 5 },
								DistanceUnit.UnitA.Name, DistanceUnit.UnitB.Name,
								UsageProfile.LowDistance.Name,
								MaintenanceOperationKind.FluidChange.Name),
							Does.Not.Contain("{"),
							standing + ": la situacion sale con una llave abierta, asi que falta una "
							+ "clave o falta llenar un hueco.");
			});
		}

		// Y LAS FORMAS DE UN MOTIVO CON SUS CLAUSULAS, por lo mismo y con el mismo instrumento. Las
		// formas se leen del dominio; la unica que se salta es TimeAndDistance, que NO es una forma
		// de motivo sino la tercera respuesta a "cual decidio", y se afirma con las otras dos abajo.
		[Test]
		public void EveryReasonShapeAndEveryClauseTheDomainCanChooseIsSaidWholeInBothLanguages()
		{
			string[] shapes = TheCodesDeclaredBy(typeof(ADueReason))
				.Where(code => code != ADueReason.TimeAndDistance).ToArray();
			string[] clauses = TheCodesDeclaredBy(typeof(LimitClause));

			Assert.That(shapes, Has.Length.EqualTo(8));
			Assert.That(clauses, Has.Length.EqualTo(6));

			Assert.Multiple(() =>
			{
				foreach (string shape in shapes)
					foreach (string clause in clauses)
						foreach (Phrases said in new[] { InSpanish, InEnglish })
							Assert.That(
								AReasonSaid(said, shape, clause), Does.Not.Contain("{"),
								shape + " con " + clause + ": el motivo sale con una llave abierta.");

				foreach (string who in new[]
					{ IntervalDimension.Time, IntervalDimension.Distance, ADueReason.TimeAndDistance })
					foreach (Phrases said in new[] { InSpanish, InEnglish })
						Assert.That(said.Knows("reason.decidedBy." + who), Is.True,
							"reason.decidedBy." + who + " no esta dicho.");
			});
		}

		private static string AReasonSaid(Phrases said, string shape, string clause)
		{
			return TheWording.TheDueReason(
				said, shape, DueState.Overdue.Name, IntervalDimension.Distance,
				MaintenanceOperationKind.FluidChange.Name, "2026-03-21", DeferralReason.Cost.Name,
				1, 2,
				new[] { clause }, new[] { IntervalDimension.Distance }, new[] { 1500 },
				new[] { 8000 }, new[] { DistanceUnit.UnitA.Name },
				new[] { DistanceUnit.UnitB.Name }, new[] { UsageProfile.LowDistance.Name });
		}

		private static string[] TheMembersOf<T>(Func<T, string> named)
		{
			return typeof(T)
				.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
				.Where(each => each.FieldType == typeof(T))
				.Select(each => named((T)each.GetValue(null)))
				.ToArray();
		}

		private static string[] TheCodesDeclaredBy(Type holder)
		{
			return holder
				.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
				.Where(each => each.IsLiteral && each.FieldType == typeof(string))
				.Select(each => (string)each.GetRawConstantValue())
				.ToArray();
		}

		// EL AUTOCONTROL. Sin esto, un catalogo que contestara cualquier cosa y una prueba que no
		// distingue son el mismo verde: un codigo que NINGUNA lista nombra tiene que caer al nombre
		// separado en mayusculas, no a una etiqueta.
		[Test]
		public void ACodeNoListNamesFallsBackToItsOwnNameSpacedOut()
		{
			Assert.That(InSpanish.HowItIsSaid("canon.operation", "SomethingNew"),
				Is.EqualTo("Something new"));
			Assert.That(InEnglish.HowItIsSaid("board.fluid", "SomethingNew"),
				Is.EqualTo("Something new"));
			Assert.That(InSpanish.HowItIsSaid("canon.tyre", ""), Is.Empty);
		}

		// Y LAS DOS FAMILIAS DICEN COSAS DISTINTAS, que es lo que justifica que sean dos. Si alguna vez
		// se unifican, esta prueba es la que lo dice en voz alta antes de que nadie lo descubra leyendo.
		[Test]
		public void TheBoardAndTheSheetStillDisagreeOnPurposeInTheLanguageOfTheRecord()
		{
			Assert.That(InSpanish.HowItIsSaid("board.dueState", "Overdue"),
				Is.Not.EqualTo(InSpanish.HowItIsSaid("canon.dueState", "Overdue")),
				"El tablero y la hoja decian 'Vencido' y 'Atrasada' para el mismo estado. Si ya dicen lo "
				+ "mismo, las dos familias sobran y hay que quitar una a proposito.");

			Assert.That(InSpanish.HowItIsSaid("board.standing", "Inside"),
				Is.Not.EqualTo(InSpanish.HowItIsSaid("canon.standing", "Inside")));
		}

		private static void ItIsSaid(string family, string code)
		{
			Assert.That(InSpanish.Knows(family + "." + code), Is.True,
				"es · " + family + "." + code + ": el dominio nombra ese miembro y el catalogo no lo dice.");
			Assert.That(InEnglish.Knows(family + "." + code), Is.True,
				"en · " + family + "." + code + ": el dominio nombra ese miembro y el catalogo no lo dice.");
		}

		private static string[] TheHolesOf(string said)
		{
			return AHole.Matches(said).Select(one => one.Groups[1].Value)
				.OrderBy(one => one, StringComparer.Ordinal).ToArray();
		}
	}
}
