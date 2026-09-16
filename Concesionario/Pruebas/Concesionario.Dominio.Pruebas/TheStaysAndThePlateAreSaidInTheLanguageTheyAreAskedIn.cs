using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LAS RESPUESTAS DE LAS ESTADIAS Y DE LA PLACA, DICHAS POR VALOR EN LOS DOS IDIOMAS: la estadia en
	// sus tres situaciones, el periodo en sus dos, cada fila del periodo, y la placa en sus dos.
	//
	// POR QUE NO ALCANZA CON NINGUNA PRUEBA DE ACUERDO. Comparar dos rutas -- el ayudante y la
	// puerta -- no dice nada sobre el catalogo: las dos pasan por el mismo renderizador, asi que una
	// clave que falta se dice "{clave}" por las dos y un hueco con el nombre cambiado se queda
	// "{hueco}" por las dos. Las dos coinciden y las dos estan mal, que es E49 dicho sobre un
	// catalogo. Lo unico que lo ve es afirmar la frase.
	//
	// Y TAMPOCO ALCANZA CON LA PARIDAD DE CLAVES. TheTwoCataloguesOfTheAppCarryTheSameKeys ve una
	// clave que falta en un catalogo; no ve una clave que sobra en los dos, ni dos huecos cruzados
	// adentro de una sola clave, porque un hueco no es una clave.
	//
	// UN DIA NO ES UN INSTANTE, Y ESA ES LA DECISION QUE ESTE TURNO TOMO. Los dos EXTREMOS de una
	// estadia son instantes -- se registraron con su hora -- y se citan tal cual en los dos idiomas.
	// Los DIAS que cubrio, y los dos limites de un periodo, son dias de calendario que la sucursal
	// dijo aparte: el orden en que se leen es del idioma, asi que cambian. Que caigan en la misma
	// frase no los vuelve la misma clase de cosa, y aqui se afirman uno al lado del otro.
	//
	// Y UNA LISTA DE DIAS SE DICE EN LA FORMA CORTA. La larga repetida cuatro veces deja de leerse
	// como una lista; los dos limites del periodo, que se nombran una vez cada uno, van en la larga.
	[TestFixture]
	public sealed class TheStaysAndThePlateAreSaidInTheLanguageTheyAreAskedIn
	{
		private const string TheVin = "1FTFW1ET5DFC10312";

		private const string TheFirstBranch = "Taller Curridabat";
		private const string TheFirstSubject = "Cambio de aceite";
		private const string TheSecondBranch = "Taller Escazu";
		private const string TheSecondSubject = "Llanta ponchada";
		private const string TheOpenBranch = "Taller Heredia";
		private const string TheOpenSubject = "Ruido en la suspension";

		private const string TheFirstOfJune = "2026-06-01";
		private const string TheLastOfJune = "2026-06-30";
		private const string TheFirstOfJuly = "2026-07-01";
		private const string TheLastOfJuly = "2026-07-31";

		// LA DE TRES DIAS Y LA DE UNO, en el mismo periodo: la de uno es la que hace que el singular
		// se vea, y el singular es del IDIOMA y no del numero.
		private static readonly DateTime TheFirstEntry = new DateTime(2026, 6, 10, 8, 30, 0);
		private static readonly DateTime TheFirstCollection = new DateTime(2026, 6, 12, 16, 45, 0);
		private static readonly string[] TheThreeDays =
			{ "2026-06-10", "2026-06-11", "2026-06-12" };

		private static readonly DateTime TheSecondEntry = new DateTime(2026, 6, 20, 7, 0, 0);
		private static readonly DateTime TheSecondCollection = new DateTime(2026, 6, 20, 11, 0, 0);
		private static readonly string[] TheOneDay = { "2026-06-20" };

		private static readonly DateTime TheOpenEntry = new DateTime(2026, 7, 14, 9, 5, 0);

		private static readonly DateTime InsideTheFirstStay = new DateTime(2026, 6, 11, 12, 0, 0);
		private static readonly DateTime InsideTheSecondStay = new DateTime(2026, 6, 20, 9, 0, 0);
		private static readonly DateTime InsideTheOpenStay = new DateTime(2026, 7, 20, 9, 0, 0);
		private static readonly DateTime NoStayCoversIt = new DateTime(2026, 5, 1, 6, 0, 0);

		private const string ThePlateAsPublished = "BQR-482";
		private static readonly DateTime TheDayThePlateWasAssigned =
			new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime AfterThePlate =
			new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		// LA ESTADIA YA ENTREGADA, que es donde las dos clases de fecha aparecen en una sola frase: los
		// dos extremos como instantes, y los tres dias dichos como dias.
		[Test]
		public void ACollectedStaySaysItsTwoInstantsVerbatimAndItsDaysInWordsInBothLanguages()
		{
			ShopStay stays = ARecordWithTwoClosedStaysAndOneOpen();

			Assert.That(
				TheStayAt(stays, InsideTheFirstStay, TheLanguage.OfTheRecord),
				Is.EqualTo(
					"Estuvo en Taller Curridabat desde el 2026-06-10 08:30:00 hasta el "
					+ "2026-06-12 16:45:00: 3 días en el taller -- 10 jun 2026, 11 jun 2026, "
					+ "12 jun 2026. Entró por Cambio de aceite."));

			Assert.That(
				TheStayAt(stays, InsideTheFirstStay, TheLanguage.OfAnEnglishReader),
				Is.EqualTo(
					"It was at Taller Curridabat from 2026-06-10 08:30:00 until "
					+ "2026-06-12 16:45:00: 3 days in the shop -- 10 Jun 2026, 11 Jun 2026, "
					+ "12 Jun 2026. It came in for Cambio de aceite."));
		}

		// EL SINGULAR ES DEL IDIOMA Y NO DEL NUMERO. Una estadia de un solo dia se dice entera en los
		// dos, y la sucursal y el asunto siguen intactos: son texto del registro.
		[Test]
		public void AStayOfOneDayIsSaidInTheSingularInBothLanguages()
		{
			ShopStay stays = ARecordWithTwoClosedStaysAndOneOpen();

			Assert.That(
				TheStayAt(stays, InsideTheSecondStay, TheLanguage.OfTheRecord),
				Is.EqualTo(
					"Estuvo en Taller Escazu desde el 2026-06-20 07:00:00 hasta el "
					+ "2026-06-20 11:00:00: 1 día en el taller -- 20 jun 2026. Entró por Llanta "
					+ "ponchada."));

			Assert.That(
				TheStayAt(stays, InsideTheSecondStay, TheLanguage.OfAnEnglishReader),
				Is.EqualTo(
					"It was at Taller Escazu from 2026-06-20 07:00:00 until 2026-06-20 11:00:00: "
					+ "1 day in the shop -- 20 Jun 2026. It came in for Llanta ponchada."));
		}

		// LA QUE SIGUE ABIERTA, que no nombra ninguna salida ni ningun dia y lo dice con palabras.
		[Test]
		public void AnOpenStaySaysItIsStillOpenInBothLanguages()
		{
			ShopStay stays = ARecordWithTwoClosedStaysAndOneOpen();

			Assert.That(
				TheStayAt(stays, InsideTheOpenStay, TheLanguage.OfTheRecord),
				Is.EqualTo(
					"Está en Taller Heredia desde el 2026-07-14 09:05:00, y todavía no lo han "
					+ "entregado: la estadía sigue abierta, así que aún no hay días que decir. Entró "
					+ "por Ruido en la suspension."));

			Assert.That(
				TheStayAt(stays, InsideTheOpenStay, TheLanguage.OfAnEnglishReader),
				Is.EqualTo(
					"It is at Taller Heredia since 2026-07-14 09:05:00, and it has not been collected "
					+ "yet: the stay is still open, so there are no days to give yet. It came in for "
					+ "Ruido en la suspension."));
		}

		// LA TERCERA SITUACION, que nombra el INSTANTE por el que se pregunto y lo cita tal cual: es
		// el unico valor que esta frase lleva, y un dia en su lugar pediria una zona que este registro
		// no tiene.
		[Test]
		public void AnInstantNoStayCoversIsSaidWithThatInstantInBothLanguages()
		{
			ShopStay stays = ARecordWithTwoClosedStaysAndOneOpen();

			Assert.That(
				TheStayAt(stays, NoStayCoversIt, TheLanguage.OfTheRecord),
				Is.EqualTo(
					"No hay ninguna estadía en el taller que cubra el 2026-05-01 06:00:00."));

			Assert.That(
				TheStayAt(stays, NoStayCoversIt, TheLanguage.OfAnEnglishReader),
				Is.EqualTo("No shop stay covers 2026-05-01 06:00:00."));
		}

		// EL PERIODO CON ESTADIAS: sus dos limites dichos como dias, los dos conteos enteros, y la
		// lista de dias en la forma corta. Los cuatro dias son de DOS estadias, asi que un plural y el
		// otro se mueven por su cuenta.
		[Test]
		public void APeriodWithStaysSaysItsBoundsItsTwoCountsAndItsDaysInBothLanguages()
		{
			ShopStay stays = ARecordWithTwoClosedStaysAndOneOpen();

			Assert.That(
				ThePeriod(stays, TheFirstOfJune, TheLastOfJune, TheLanguage.OfTheRecord),
				Is.EqualTo(
					"Entre el 1 de junio de 2026 y el 30 de junio de 2026 estuvo en el taller 4 días, "
					+ "en 2 estadías: 10 jun 2026, 11 jun 2026, 12 jun 2026, 20 jun 2026."));

			Assert.That(
				ThePeriod(stays, TheFirstOfJune, TheLastOfJune, TheLanguage.OfAnEnglishReader),
				Is.EqualTo(
					"Between 1 June 2026 and 30 June 2026 it was in the shop 4 days, over 2 stays: "
					+ "10 Jun 2026, 11 Jun 2026, 12 Jun 2026, 20 Jun 2026."));
		}

		// Y LOS DOS SINGULARES DEL PERIODO A LA VEZ: un dia y una estadia. Son dos plurales distintos
		// y una frase que los eligiera juntos pasaria la prueba de arriba sin decir nada.
		[Test]
		public void APeriodOfOneDayInOneStaySaysBothSingularsInBothLanguages()
		{
			ShopStay stays = ARecordWithTwoClosedStaysAndOneOpen();

			Assert.That(
				ThePeriod(stays, "2026-06-20", "2026-06-21", TheLanguage.OfTheRecord),
				Is.EqualTo(
					"Entre el 20 de junio de 2026 y el 21 de junio de 2026 estuvo en el taller 1 día, "
					+ "en 1 estadía: 20 jun 2026."));

			Assert.That(
				ThePeriod(stays, "2026-06-20", "2026-06-21", TheLanguage.OfAnEnglishReader),
				Is.EqualTo(
					"Between 20 June 2026 and 21 June 2026 it was in the shop 1 day, over 1 stay: "
					+ "20 Jun 2026."));
		}

		// EL PERIODO SIN NINGUNA ESTADIA CON DIAS REGISTRADOS, que nombra sus dos limites igual: un
		// cero no diria entre cuando y cuando se pregunto. Julio TIENE la estadia abierta, asi que
		// esta frase tambien dice que lo que falta son los DIAS y no la estadia.
		[Test]
		public void APeriodWithNoRecordedDaysNamesItsBoundsInBothLanguages()
		{
			ShopStay stays = ARecordWithTwoClosedStaysAndOneOpen();

			Assert.That(
				ThePeriod(stays, TheFirstOfJuly, TheLastOfJuly, TheLanguage.OfTheRecord),
				Is.EqualTo(
					"Entre el 1 de julio de 2026 y el 31 de julio de 2026 no hay ninguna estadía en el "
					+ "taller con días registrados."));

			Assert.That(
				ThePeriod(stays, TheFirstOfJuly, TheLastOfJuly, TheLanguage.OfAnEnglishReader),
				Is.EqualTo(
					"Between 1 July 2026 and 31 July 2026 there is no shop stay with recorded days."));
		}

		// CADA FILA DEL PERIODO EN SU PROPIA FRASE, que es la MISMA frase que la estadia por instante:
		// es la misma cosa dicha, y componerla dos veces las dejaria divergir.
		[Test]
		public void EveryRowOfThePeriodSaysItselfInBothLanguages()
		{
			ShopStay stays = ARecordWithTwoClosedStaysAndOneOpen();

			Assert.That(
				TheRows(stays, TheFirstOfJune, TheLastOfJune, TheLanguage.OfTheRecord),
				Is.EqualTo(new[]
				{
					"Estuvo en Taller Curridabat desde el 2026-06-10 08:30:00 hasta el "
					+ "2026-06-12 16:45:00: 3 días en el taller -- 10 jun 2026, 11 jun 2026, "
					+ "12 jun 2026. Entró por Cambio de aceite.",
					"Estuvo en Taller Escazu desde el 2026-06-20 07:00:00 hasta el "
					+ "2026-06-20 11:00:00: 1 día en el taller -- 20 jun 2026. Entró por Llanta "
					+ "ponchada.",
				}));

			Assert.That(
				TheRows(stays, TheFirstOfJune, TheLastOfJune, TheLanguage.OfAnEnglishReader),
				Is.EqualTo(new[]
				{
					"It was at Taller Curridabat from 2026-06-10 08:30:00 until "
					+ "2026-06-12 16:45:00: 3 days in the shop -- 10 Jun 2026, 11 Jun 2026, "
					+ "12 Jun 2026. It came in for Cambio de aceite.",
					"It was at Taller Escazu from 2026-06-20 07:00:00 until 2026-06-20 11:00:00: "
					+ "1 day in the shop -- 20 Jun 2026. It came in for Llanta ponchada.",
				}));
		}

		// LA PLACA PUBLICADA: el texto entra INTACTO -- es dato del registro y lleva el guion que el
		// concesionario escribio -- y el dia desde el que rige se dice en palabras.
		[Test]
		public void AnAssignedPlateKeepsItsTextAndSaysItsDayInBothLanguages()
		{
			VehicleProfile profile = AProfileWithAPlate();

			Assert.That(
				ThePlate(profile, AfterThePlate, TheLanguage.OfTheRecord),
				Is.EqualTo(
					"La placa de este vehículo es BQR-482, asignada desde el 15 de enero de 2026."));

			Assert.That(
				ThePlate(profile, AfterThePlate, TheLanguage.OfAnEnglishReader),
				Is.EqualTo("This vehicle's plate is BQR-482, assigned since 15 January 2026."));
		}

		// Y LA AUSENCIA DE PLACA, que nombra a quien publica una y no lleva ningun texto: una cadena
		// vacia parece una respuesta y el Vin puesto en su lugar no coincidiria con nada.
		[Test]
		public void NoPlateOnRecordSaysWhoPublishesOneInBothLanguages()
		{
			VehicleProfile profile = new VehicleProfile(TheVin);

			Assert.That(
				ThePlate(profile, AfterThePlate, TheLanguage.OfTheRecord),
				Is.EqualTo(
					"No hay ninguna placa registrada para este vehículo al instante preguntado. La "
					+ "placa de un vehículo la publica el concesionario."));

			Assert.That(
				ThePlate(profile, AfterThePlate, TheLanguage.OfAnEnglishReader),
				Is.EqualTo(
					"There is no plate on record for this vehicle at the instant asked. A vehicle's "
					+ "plate is published by the dealership."));
		}

		// NINGUNA RESPUESTA DE LAS ESTADIAS NI DE LA PLACA SE QUEDA CON UN HUECO SIN LLENAR NI CON UNA
		// CLAVE CRUDA. Es el barrido que hace que las pruebas de arriba no dependan de que a alguien se
		// le ocurriera el caso: Say() devuelve "{clave}" cuando no la tiene y deja "{hueco}" cuando el
		// nombre no calza, y las dos cosas se ven con una llave en la frase final.
		[Test]
		public void NoAnswerOfTheStaysOrThePlateComesBackWithAHoleOrARawKeyInIt()
		{
			foreach (string language in TheTwoLanguages())
				foreach (string said in EverySentenceOfTheStaysAndThePlate(language))
					Assert.That(said, Does.Not.Contain("{").And.Not.Contain("}"),
						"Una llave en la frase final es una clave que el catalogo no tiene o un hueco "
						+ "cuyo nombre no calza con el que quien compone le pasa. En " + language
						+ ": " + said);
		}

		private IEnumerable<string> EverySentenceOfTheStaysAndThePlate(string language)
		{
			ShopStay stays = ARecordWithTwoClosedStaysAndOneOpen();
			VehicleProfile withAPlate = AProfileWithAPlate();

			List<string> said = new List<string>
			{
				TheStayAt(stays, InsideTheFirstStay, language),
				TheStayAt(stays, InsideTheSecondStay, language),
				TheStayAt(stays, InsideTheOpenStay, language),
				TheStayAt(stays, NoStayCoversIt, language),
				ThePeriod(stays, TheFirstOfJune, TheLastOfJune, language),
				ThePeriod(stays, "2026-06-20", "2026-06-21", language),
				ThePeriod(stays, TheFirstOfJuly, TheLastOfJuly, language),
				ThePlate(withAPlate, AfterThePlate, language),
				ThePlate(new VehicleProfile(TheVin), AfterThePlate, language),
			};

			said.AddRange(TheRows(stays, TheFirstOfJune, TheLastOfJune, language));
			said.AddRange(TheRows(stays, TheFirstOfJuly, TheLastOfJuly, language));

			return said;
		}

		private static string TheStayAt(ShopStay stays, DateTime asOf, string language)
		{
			return TheSentenceTheDoorWouldCompose.TheStay(stays, asOf, language);
		}

		private static string ThePeriod(
			ShopStay stays, string fromDay, string toDay, string language)
		{
			return TheSentenceTheDoorWouldCompose.TheStayPeriod(stays, fromDay, toDay, language);
		}

		private static string[] TheRows(
			ShopStay stays, string fromDay, string toDay, string language)
		{
			return TheSentenceTheDoorWouldCompose.EachStayBetween(stays, fromDay, toDay, language);
		}

		private static string ThePlate(VehicleProfile profile, DateTime asOf, string language)
		{
			return TheSentenceTheDoorWouldCompose.ThePlate(profile, asOf, language);
		}

		// DOS ESTADIAS CERRADAS EN JUNIO -- una de tres dias y una de uno -- y una ABIERTA en julio.
		// La abierta es lo que hace que el periodo de julio diga que faltan los DIAS y no la estadia.
		private static ShopStay ARecordWithTwoClosedStaysAndOneOpen()
		{
			ShopStay stays = new ShopStay(TheVin);

			stays.RecordStay(
				TheFirstBranch, TheFirstSubject, TheFirstEntry, TheFirstCollection, TheThreeDays);
			stays.RecordStay(
				TheSecondBranch, TheSecondSubject, TheSecondEntry, TheSecondCollection, TheOneDay);
			stays.RecordEntry(TheOpenBranch, TheOpenSubject, TheOpenEntry);

			return stays;
		}

		private static VehicleProfile AProfileWithAPlate()
		{
			VehicleProfile profile = new VehicleProfile(TheVin);

			profile.RecordPlateAssignment(ThePlateAsPublished, TheDayThePlateWasAssigned);

			return profile;
		}

		private static string[] TheTwoLanguages()
		{
			return new[] { TheLanguage.OfTheRecord, TheLanguage.OfAnEnglishReader };
		}
	}
}
