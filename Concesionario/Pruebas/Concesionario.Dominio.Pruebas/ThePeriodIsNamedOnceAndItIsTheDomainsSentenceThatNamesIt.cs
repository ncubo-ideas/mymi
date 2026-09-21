using System;
using System.IO;
using Concesionario.App.Views;
using Concesionario.Wasm;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LA DECISION DE RENDERIZADO QUE ESTA REBANADA TENIA QUE TOMAR, Y ESTA PRUEBA ES DONDE SE PAGA.
	//
	// P-11 dice que la ventana de cada bloque es fija y va NOMBRADA EN SU ENCABEZADO, porque la
	// pantalla no ofrece ningun selector de periodo y la persona tiene que saber que esta mirando.
	// Cuando eso se escribio no se sabia que la frase del dominio nombra sus dos limites ELLA MISMA,
	// en los dos estados del registro -- con dias y sin ninguno.
	//
	// ASI QUE LAS DOS COSAS JUNTAS IMPRIMEN EL PERIODO DOS VECES EN LA MISMA COLUMNA, que es lo que un
	// modelo no ve y una persona lee como un defecto. Lo que hace en una pantalla el trabajo que el
	// nombre de un campo hace para un modelo es EL ENCABEZADO, y el trabajo del encabezado es decir de
	// que es el bloque, no repetir una clausula que la respuesta ya trae.
	//
	// DECIDIDO: EL ENCABEZADO NOMBRA EL BLOQUE Y LA FRASE DEL DOMINIO NOMBRA EL PERIODO. El proposito
	// de P-11 se cumple entero -- el periodo esta dicho en la pantalla, con los dos limites exactos con
	// que se le pregunto a la puerta y en las palabras del dominio --, y se dice UNA sola vez.
	//
	// LA MITAD QUE HACE QUE ESTO SEA UNA MEDICION Y NO UNA CONVENCION es la de abajo del todo: que la
	// frase nombre el periodo se lee del registro, no se supone. El dia que deje de nombrarlo, la
	// pantalla se queda sin decir que ventana esta mirando y esta prueba es la que lo dice.
	[TestFixture]
	public sealed class ThePeriodIsNamedOnceAndItIsTheDomainsSentenceThatNamesIt
	{
		private const string TheVin = "JH4KA7561PC000321";

		private const string TheYearFrom = "2026-01-01";
		private const string TheYearTo = "2026-12-31";

		private const string TheBranch = "Taller Santa Ana";
		private const string TheSubject = "Cambio de bujías";

		private static readonly DateTime TheEntry =
			new DateTime(2026, 2, 10, 8, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheCollection =
			new DateTime(2026, 2, 11, 16, 0, 0, DateTimeKind.Utc);

		private static readonly string[] TheDays = { "2026-02-10", "2026-02-11" };

		private static readonly DateTime TheInstantTheScreenAsksAt =
			new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);

		// EL ENCABEZADO DICE DE QUE ES EL BLOQUE Y NO REPITE EL PERIODO.
		[Test]
		public void TheHeadingNamesTheBlockAndTheTwoBoundsAreNotPaintedBesideIt()
		{
			string markup = TheBlockAsItIsWritten();

			Assert.That(markup, Does.Contain("history.staysTitle"),
				"El bloque perdio su encabezado. Es lo que hace para una persona lo que el nombre de "
				+ "un campo hace para un modelo: decir que pregunta contesta lo de abajo.");

			// Y LA CLAVE SIGUE DICIENDO LAS MISMAS PALABRAS en el idioma del registro. El markup nombra
			// la CLAVE desde que la pantalla habla dos idiomas, asi que sin este renglon una clave que no
			// este en el catalogo dejaria el encabezado en blanco y esto verde.
			Assert.That(TheCatalogueOfTheScreen.InSpanish("history.staysTitle"),
				Is.EqualTo("Los días en el taller"));

			foreach (string half in new[] { "@Stays.FromDay", "@Stays.ToDay", "Stays.FromDay", "Stays.ToDay" })
				Assert.That(markup, Does.Not.Contain(half),
					"El bloque pinta '" + half + "'. La frase del dominio ya nombra los dos limites, "
					+ "con esas mismas fechas, en los dos estados del registro -- asi que esto imprime "
					+ "el periodo por segunda vez en la misma columna. Un modelo lee campos con nombre "
					+ "y nunca los ve pegados; una persona lee una sola columna.");
		}

		// Y LA FRASE QUE SI LO NOMBRA SE PINTA ENTERA, sin recortar y sin nada pegado.
		[Test]
		public void AndTheSentenceThatDoesNameItIsPaintedWhole()
		{
			string markup = TheBlockAsItIsWritten();

			Assert.That(markup, Does.Contain("@Stays.Says"),
				"El bloque dejo de pintar la frase del periodo, que es la unica cosa en la pantalla "
				+ "que dice entre cuando y cuando se pregunto.");
		}

		// LA MEDICION: LA FRASE NOMBRA LOS DOS LIMITES, EN LOS DOS ESTADOS DEL REGISTRO. Sin este
		// renglon, lo de arriba seria una convencion de markup y el encabezado quedaria callado el dia
		// que el dominio dejara de nombrar el periodo.
		[Test]
		public void TheSentenceNamesBothBoundsWithStaysOnRecordAndWithNone()
		{
			using (var empty = new AScheduleOnARealJournal(TheVin, AppState.TheProfileAtDelivery))
			{
				TheStaysOfThisVehicle nothing = TheStaysOfThisVehicle.ReadThrough(
					empty.Api, TheYearFrom, TheYearTo, TheInstantTheScreenAsksAt, TheLanguage.OfTheRecord);

				Assert.That(
					nothing.Says, Does.Contain(ADay(TheYearFrom)).And.Contains(ADay(TheYearTo)),
					"Sin ninguna estadia, la frase TAMBIEN nombra el periodo. Es el estado en que un "
					+ "encabezado callado costaria mas, y es justo donde el dominio si lo dice.");

				empty.Api.RecordShopStay(TheBranch, TheSubject, TheEntry, TheCollection, TheDays);

				TheStaysOfThisVehicle something = TheStaysOfThisVehicle.ReadThrough(
					empty.Api, TheYearFrom, TheYearTo, TheInstantTheScreenAsksAt, TheLanguage.OfTheRecord);

				Assert.That(
					something.Says, Does.Contain(ADay(TheYearFrom)).And.Contains(ADay(TheYearTo)),
					"Y con una estadia registrada, tambien. Los dos estados, porque una frase que "
					+ "nombrara el periodo en uno solo dejaria la mitad de las pantallas sin ventana.");

				Assert.That(something.Says, Is.Not.EqualTo(nothing.Says),
					"Y las dos frases son distintas, que es lo que hace que las dos afirmaciones de "
					+ "arriba midan dos estados y no uno repetido.");
			}
		}

		private static string TheBlockAsItIsWritten()
		{
			string folder = Path.Combine(AppContext.BaseDirectory, "SeccionesComoDato");

			Assert.That(Directory.Exists(folder), Is.True,
				"Las secciones no llegaron al output.");

			foreach (string file in Directory.GetFiles(folder, "*.razor"))
				if (Path.GetFileName(file) == "Historial.razor") return File.ReadAllText(file);

			Assert.Fail("'Historial.razor' no llego al output.");
			return "";
		}

		private static string ADay(string yyyyMMdd)
		{
			return TheSentenceTheDoorWouldCompose.ADayAsTheSentenceSaysIt(
				yyyyMMdd, TheLanguage.OfTheRecord);
		}
	}
}
