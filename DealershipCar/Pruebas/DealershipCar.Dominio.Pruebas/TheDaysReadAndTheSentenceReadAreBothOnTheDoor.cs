using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// E48 EN LA PUERTA: LOS DOS HERMANOS DE LA RESPUESTA DEL PERIODO, Y LOS DOS SON LECTURAS. Una
	// respuesta compuesta tiene que exponer tambien cada mitad que compone, o tener una se vuelve
	// precondicion de tener la otra. Aca las dos mitades son formas distintas de la MISMA respuesta:
	//
	//   DaysInTheShopBetween        los dias como DATOS -- un string[] de yyyy-MM-dd que un llamador
	//                               puede cruzar con el calendario de la escuela sin partir ninguna
	//                               frase.
	//   DescribeDaysInTheShopBetween los dias como PALABRAS -- una sola frase que se imprime entera y
	//                               que, cuando no hay ninguna estadia, NOMBRA EL PERIODO en vez de
	//                               contestar cero o vacio.
	//
	// SIN LA PRIMERA, el valor entre instituciones de esta rebanada se pierde: nadie puede cruzar una
	// frase con un registro de asistencia. SIN LA SEGUNDA, un periodo sin estadias contesta la lista
	// vacia y ninguna capa sabe decir por que -- y componer esa frase afuera seria la caratula
	// diciendo algo que el dominio no dijo.
	//
	// LA MITAD QUE HACE QUE ESTO NO SEA SIMETRIA POR GUSTO: la frase contesta SIN NINGUN HECHO DEL
	// VEHICULO. Un registro al que nadie le dijo nada la contesta igual, nombrando los dos dias por
	// los que se pregunto, asi que no hay ningun estado en que tener la lista sea condicion para
	// tener la frase.
	[TestFixture]
	public sealed class TheDaysReadAndTheSentenceReadAreBothOnTheDoor
	{
		private const string TheVin = "3VWFE21C04M000001";

		private const string TheYearFrom = "2026-01-01";
		private const string TheYearTo = "2026-12-31";

		private const string TheBranch = "Taller Escazú";
		private const string TheSubject = "Frenos";

		private static readonly DateTime Entered =
			new DateTime(2026, 3, 2, 7, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime Collected =
			new DateTime(2026, 3, 4, 17, 0, 0, DateTimeKind.Utc);

		private static readonly string[] TheDaysTheBranchSaid =
			{ "2026-03-02", "2026-03-03", "2026-03-04" };

		// EL CALENDARIO DE LA ESCUELA: dias que no salieron de este modulo. Es con lo que se cruza la
		// lista, y es lo que una frase no permite hacer.
		private static readonly string[] TheDaysSchoolWasInSession =
			{ "2026-03-03", "2026-03-04", "2026-03-05", "2026-03-06" };

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

		// EL HERMANO DE DATOS: cada dia se parsea como fecha exacta y se cruza con un calendario
		// ajeno. Es la operacion entera que esta rebanada existe para hacer posible.
		[Test]
		public void TheDaysComeBackAsDatesACallerCanCrossWithASchoolRegister()
		{
			TheStayInMarch();

			string[] days = journal.Api.DaysInTheShopBetween(TheYearFrom, TheYearTo);

			List<string> bothDays = new List<string>();

			foreach (string each in days)
			{
				DateOnly addressable;

				Assert.That(
					DateOnly.TryParseExact(
						each, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None,
						out addressable),
					Is.True,
					"'" + each + "' tenia que ser una fecha direccionable y exacta. Un llamador que "
					+ "tenga que adivinar la forma no puede cruzarla con nada.");

				foreach (string session in TheDaysSchoolWasInSession)
					if (session == each) bothDays.Add(each);
			}

			Assert.That(bothDays, Is.EqualTo(new[] { "2026-03-03", "2026-03-04" }),
				"El cruce da DOS dias -- el 3 y el 4 -- y no los tres ni los cuatro. Se afirma por "
				+ "valor: una interseccion vacia y una que trajera todo pasarian igual un Is.Not.Null.");
		}

		// EL HERMANO EN PALABRAS: la misma respuesta dicha entera, con los dias adentro. Se compara
		// contra la lista para que no puedan separarse: si una dijera tres dias y la otra dos, este
		// renglon lo dice.
		[Test]
		public void TheSentenceSaysTheSameDaysTheListDoes()
		{
			TheStayInMarch();

			string said = journal.Api.DescribeDaysInTheShopBetween(TheYearFrom, TheYearTo);

			foreach (string each in journal.Api.DaysInTheShopBetween(TheYearFrom, TheYearTo))
				Assert.That(said, Does.Contain(each),
					"La frase tenia que nombrar el dia '" + each + "' que la lista trae.");

			Assert.That(said, Does.Contain("3 días").And.Contains("1 estadía"),
				"Y los dos totales, compuestos adentro: la caratula no suma ni cuenta nada.");
		}

		// LA MITAD QUE HACE QUE E48 SEA UNA LEY Y NO UNA SIMETRIA: la frase contesta CON EL REGISTRO
		// VACIO. Sin este renglon, la puerta podria tener las dos y la frase seguir necesitando que
		// hubiera algo -- que es exactamente la precondicion que E48 prohibe.
		[Test]
		public void TheSentenceAnswersWithNoVehicleFactAtAllAndNamesThePeriod()
		{
			Assert.That(journal.Api.DaysInTheShopBetween(TheYearFrom, TheYearTo), Is.Empty,
				"El registro no tiene ni un hecho: la lista esta vacia.");

			string said = journal.Api.DescribeDaysInTheShopBetween(TheYearFrom, TheYearTo);

			Assert.That(said, Does.Contain(TheYearFrom).And.Contains(TheYearTo),
				"Y la frase contesta igual, NOMBRANDO los dos dias por los que se pregunto. Un cero no "
				+ "diria entre cuando y cuando, y una lista vacia tampoco.");
			Assert.That(said, Does.Not.Contain("0 días"),
				"Y no lo dice como un cero disfrazado de frase.");
		}

		// Y LOS DOS ESTAN DECLARADOS EN LA PUERTA, con la forma que cada uno promete. Sin esto, mover
		// uno de los dos a un ayudante de pruebas dejaria los renglones de arriba verdes.
		[Test]
		public void BothSiblingsAreDeclaredOnTheDoorAndNeitherIsTheOthersDerivative()
		{
			Assert.That(
				typeof(DealershipCar.App.DealershipCarAPI).GetMethod("DaysInTheShopBetween").ReturnType,
				Is.EqualTo(typeof(string[])),
				"El hermano de datos contesta una LISTA. Una frase de la que haya que sacar los dias "
				+ "es la mitad que no esta.");

			Assert.That(
				typeof(DealershipCar.App.DealershipCarAPI)
					.GetMethod("DescribeDaysInTheShopBetween").ReturnType,
				Is.EqualTo(typeof(string)),
				"Y el hermano en palabras contesta UNA frase, compuesta adentro.");
		}

		private void TheStayInMarch()
		{
			journal.Api.RecordShopStay(
				TheBranch, TheSubject, Entered, Collected, TheDaysTheBranchSaid);
		}
	}
}
