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
