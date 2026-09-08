using System;
using Concesionario.App;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LA REGLA 4 DEL §7 DEL BRIEF: EL TOTAL DEL DOMINIO Y LA SUMA DE LAS FILAS SE PONEN DE ACUERDO.
	// No porque la caratula pueda sumarlas -- tiene prohibido hacerlo -- sino para que nunca este
	// tentada y nunca tenga razon.
	//
	// E49: EL ACUERDO ENTRE DOS CONSUMIDORES DE UN RESOLVEDOR NO ES EVIDENCIA SOBRE EL RESOLVEDOR.
	// DaysInTheShopCountBetween y StayCoveredDaysBetween salen los dos de TheStaysBetween, asi que un
	// resolvedor que acotara mal el periodo los movería a los DOS y el acuerdo seguiria verde. Por eso
	// esta prueba tiene DOS obligaciones y no una:
	//
	//   EL ACUERDO      el total del dominio es la suma de los dias de las filas.
	//   EL ANCLA        y los dos son los dias que se RECIBIERON, escritos aca por valor. Es lo que
	//                   ata la respuesta a lo que la sucursal dijo, y no a otra cosa derivada de lo
	//                   mismo.
	//
	// Y EL RENGLON QUE DICE POR QUE EL ACUERDO NO ES UNA IDENTIDAD: una estadia que se sale del
	// periodo por un lado sigue diciendo SUS dias enteros en su fila -- la regla 2 -- mientras el
	// total del dominio cuenta solo los que caen adentro. Los dos numeros difieren ahi a proposito, y
	// esta escrito para que nadie 'arregle' la fila para que calce.
	[TestFixture]
	public sealed class TheDomainsDayTotalEqualsTheSumOverTheRowsThroughTheDoor
	{
		private const string TheVin = "5YJ3E1EA7JF000001";

		private const string TheYearFrom = "2026-01-01";
		private const string TheYearTo = "2026-12-31";

		private const string TheMarchBranch = "Taller Escazú";
		private const string TheMarchSubject = "Frenos";

		private static readonly DateTime MarchEntered =
			new DateTime(2026, 3, 2, 7, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime MarchCollected =
			new DateTime(2026, 3, 4, 17, 0, 0, DateTimeKind.Utc);

		// LOS DIAS QUE LA SUCURSAL DIJO, escritos aca por valor. Son el ancla: cada afirmacion de
		// abajo se compara contra ESTO y no contra otra respuesta de la puerta.
		private static readonly string[] TheMarchDaysTheBranchSaid =
			{ "2026-03-02", "2026-03-03", "2026-03-04" };

		private const string TheJulyBranch = "Taller Curridabat";
		private const string TheJulySubject = "Cambio de aceite";

		private static readonly DateTime JulyEntered =
			new DateTime(2026, 7, 20, 9, 15, 0, DateTimeKind.Utc);

		private static readonly DateTime JulyCollected =
			new DateTime(2026, 7, 21, 16, 0, 0, DateTimeKind.Utc);

		private static readonly string[] TheJulyDaysTheBranchSaid = { "2026-07-20", "2026-07-21" };

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.RecordShopStay(
				TheMarchBranch, TheMarchSubject, MarchEntered, MarchCollected,
				TheMarchDaysTheBranchSaid);

			journal.Api.RecordShopStay(
				TheJulyBranch, TheJulySubject, JulyEntered, JulyCollected, TheJulyDaysTheBranchSaid);
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// EL ACUERDO. Con las dos estadias enteras adentro del periodo y sin ningun dia compartido, el
		// total que el dominio compone es la suma de los dias de las filas.
		[Test]
		public void TheDomainsTotalIsTheSumOfTheDaysOfTheRows()
		{
			ShopStayRecord record = journal.Api.StaysBetween(TheYearFrom, TheYearTo);

			int overTheRows = 0;

			foreach (string cell in record.CoveredDays)
				overTheRows += cell.Split(' ').Length;

			Assert.That(
				journal.Api.DaysInTheShopCountBetween(TheYearFrom, TheYearTo),
				Is.EqualTo(overTheRows),
				"El total que el dominio compone y la suma de las filas tienen que decir lo mismo. "
				+ "Que no lo digan es lo que hace que una pantalla muestre un total que sus propias "
				+ "filas desmienten.");

			Assert.That(overTheRows, Is.EqualTo(5),
				"Y son CINCO y no cualquier numero: dos totales que se pusieran de acuerdo en cero se "
				+ "pondrian de acuerdo igual.");
		}

		// EL ANCLA (E49). El total y las filas son dos consumidores del MISMO resolvedor, asi que su
		// acuerdo no dice nada de si el resolvedor acoto bien. Lo que si lo dice es que los dos
		// contesten LOS DIAS QUE LA SUCURSAL MANDO, escritos aca a mano y comparados por valor.
		[Test]
		public void AndBothAreTheDaysThatWereReceived()
		{
			ShopStayRecord record = journal.Api.StaysBetween(TheYearFrom, TheYearTo);

			Assert.That(
				record.CoveredDays[0], Is.EqualTo(string.Join(" ", TheMarchDaysTheBranchSaid)),
				"La fila de marzo dice los TRES dias que la sucursal mando, en ese orden.");
			Assert.That(
				record.CoveredDays[1], Is.EqualTo(string.Join(" ", TheJulyDaysTheBranchSaid)),
				"Y la de julio los DOS suyos.");

			Assert.That(
				journal.Api.DaysInTheShopBetween(TheYearFrom, TheYearTo),
				Is.EqualTo(new[]
				{
					"2026-03-02", "2026-03-03", "2026-03-04", "2026-07-20", "2026-07-21",
				}),
				"Y la lista del periodo son esos mismos cinco, ascendentes. Un resolvedor que acotara "
				+ "mal moveria el total Y las filas a la vez; esto es lo unico que lo ve.");
		}

		// Y LA CUENTA DE ESTADIAS ES LA OTRA, y no la misma. Dos estadias y cinco dias: sin este
		// renglon, un miembro que contestara dias donde dice estadias pasaria los dos de arriba.
		[Test]
		public void TheStayCountIsNotTheDayCount()
		{
			Assert.That(journal.Api.StayCountBetween(TheYearFrom, TheYearTo), Is.EqualTo(2));
			Assert.That(journal.Api.DaysInTheShopCountBetween(TheYearFrom, TheYearTo), Is.EqualTo(5));
		}

		// POR QUE EL ACUERDO NO ES UNA IDENTIDAD, Y ESTA ESCRITO PARA QUE NADIE LO 'ARREGLE'. Un
		// periodo que corta la estadia de julio por la mitad deja la fila diciendo SUS dos dias --
		// regla 2, la fila es el hecho completo de esa estadia y su frase dice lo mismo -- mientras el
		// total del dominio cuenta UNO. Los dos son correctos y dicen cosas distintas.
		[Test]
		public void APeriodThatCutsAStayLeavesTheRowWholeAndTheDomainsTotalClipped()
		{
			const string cutAtTheTwentieth = "2026-07-20";

			ShopStayRecord record = journal.Api.StaysBetween(TheYearFrom, cutAtTheTwentieth);

			Assert.That(record.Branch, Has.Length.EqualTo(2),
				"La estadia de julio entra al periodo: uno de sus dias cae adentro.");

			Assert.That(
				record.CoveredDays[1], Is.EqualTo(string.Join(" ", TheJulyDaysTheBranchSaid)),
				"Y su fila sigue diciendo SUS DOS dias, porque la fila es el hecho completo de esa "
				+ "estadia y su propia frase nombra los dos. Recortarla aca la haria mentir contra su "
				+ "vecina de columna.");

			Assert.That(record.Says[1], Does.Contain("2026-07-21"),
				"La frase de la fila tambien nombra el dia que quedo fuera del periodo, que es por lo "
				+ "que la celda no se puede recortar sin desmentirla.");

			Assert.That(
				journal.Api.DaysInTheShopCountBetween(TheYearFrom, cutAtTheTwentieth), Is.EqualTo(4),
				"Y el total del dominio cuenta CUATRO: los tres de marzo y el unico de julio que cae "
				+ "adentro. Es el numero que la pantalla imprime, y es el del periodo, no el de las "
				+ "filas.");

			Assert.That(
				journal.Api.DaysInTheShopBetween(TheYearFrom, cutAtTheTwentieth),
				Is.EqualTo(new[] { "2026-03-02", "2026-03-03", "2026-03-04", "2026-07-20" }));
		}

		// Y UN DIA QUE DOS ESTADIAS CUBREN ES UN DIA EN EL TALLER, no dos. Es la otra razon por la que
		// el acuerdo de arriba se mide en vez de darse por sabido.
		[Test]
		public void ADayTwoStaysCoverCountsOnceInThePeriodAndTwiceOverTheRows()
		{
			journal.Api.RecordShopStay(
				"Taller Heredia", "Alineamiento",
				new DateTime(2026, 3, 4, 18, 0, 0, DateTimeKind.Utc),
				new DateTime(2026, 3, 5, 11, 0, 0, DateTimeKind.Utc),
				new[] { "2026-03-04", "2026-03-05" });

			Assert.That(
				journal.Api.DaysInTheShopBetween("2026-03-01", "2026-03-31"),
				Is.EqualTo(new[] { "2026-03-02", "2026-03-03", "2026-03-04", "2026-03-05" }),
				"El 4 aparece UNA vez: dos estadias que cubren el mismo dia son un dia en el taller.");

			Assert.That(
				journal.Api.DaysInTheShopCountBetween("2026-03-01", "2026-03-31"), Is.EqualTo(4));

			ShopStayRecord record = journal.Api.StaysBetween("2026-03-01", "2026-03-31");

			int overTheRows = 0;

			foreach (string cell in record.CoveredDays) overTheRows += cell.Split(' ').Length;

			Assert.That(overTheRows, Is.EqualTo(5),
				"Y sobre las filas se cuenta CINCO, porque cada fila dice sus propios dias. El total "
				+ "del periodo es el del periodo; nadie lo saca sumando filas.");
		}
	}
}
