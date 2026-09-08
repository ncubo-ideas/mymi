using System;
using Concesionario.App;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LA DECISION DE SH-4, FIJADA POR PRUEBA. Una estadia abierta no tiene ningun dia todavia -- los
	// dias llegan con la salida, que es L4 -- asi que OverlapsDays es FALSO para ella y NINGUN periodo
	// dicho en dias la alcanza. Decidir si su instante de entrada cae adentro de un periodo dicho en
	// dias pediria la zona horaria que este modulo se niega a inventar (G1).
	//
	// DE AHI SALE LA DECISION: §4.2 le daba al registro una columna 'Standing' y una 'CollectedAt', y
	// las dos son constantes por construccion -- Standing solo puede decir 'Closed' y CollectedAt no
	// puede decir nunca 'sigue adentro'. Se quitaron las dos, y el conjunto de filas es honestamente
	// LAS ESTADIAS CERRADAS DE UN PERIODO. Queda registrado en DECISIONS.md como DEC-52.
	//
	// ESTA PRUEBA ES LA QUE HACE QUE ESO NO SE PUEDA DESHACER EN SILENCIO. Fija las dos mitades
	// juntas para que no puedan separarse:
	//
	//   LO QUE SI LA ALCANZA   IsInsideTheShopAt y DescribeTheStayAt, las dos llaveadas por INSTANTE.
	//   LO QUE NO              las seis lecturas de periodo, las seis llaveadas por DIA.
	//
	// SIN LA PRIMERA MITAD, una estadia abierta que no contestara en ninguna parte cumpliria la
	// segunda -- y ese seria un defecto distinto y peor: el vehiculo estaria adentro y el registro no
	// lo diria en ningun lado.
	[TestFixture]
	public sealed class AnOpenStayIsReachableOnlyByTheInstantKeyedReads
	{
		private const string TheVin = "WBA3A5C50DF000001";

		private const string TheYearFrom = "2026-01-01";
		private const string TheYearTo = "2026-12-31";

		private const string TheClosedBranch = "Taller Escazú";
		private const string TheClosedSubject = "Frenos";

		private static readonly DateTime ClosedEntered =
			new DateTime(2026, 3, 2, 7, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime ClosedCollected =
			new DateTime(2026, 3, 4, 17, 0, 0, DateTimeKind.Utc);

		private static readonly string[] TheClosedDays =
			{ "2026-03-02", "2026-03-03", "2026-03-04" };

		private const string TheOpenBranch = "Taller Alajuela";
		private const string TheOpenSubject = "Revisión de garantía";

		private static readonly DateTime OpenEntered =
			new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime WhileItIsStillInside =
			new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

		// EL PERIODO QUE CONTIENE EL DIA DE LA ENTRADA ABIERTA Y NADA MAS. Es el instrumento: si un
		// periodo dicho en dias alcanzara una estadia abierta, este es el que la traeria.
		private const string TheOctoberFrom = "2026-10-01";
		private const string TheOctoberTo = "2026-10-31";

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.RecordShopStay(
				TheClosedBranch, TheClosedSubject, ClosedEntered, ClosedCollected, TheClosedDays);

			journal.Api.RecordShopEntry(TheOpenBranch, TheOpenSubject, OpenEntered);
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// LAS DOS QUE SI LA ALCANZAN, las dos llaveadas por instante. Sin este renglon la mitad de
		// abajo se cumpliria tambien con una estadia abierta invisible en todas partes.
		[Test]
		public void TheTwoInstantKeyedReadsBothSeeIt()
		{
			Assert.That(journal.Api.IsInsideTheShopAt(WhileItIsStillInside), Is.True,
				"El vehiculo esta adentro mientras la estadia siga abierta.");

			string says = journal.Api.DescribeTheStayAt(WhileItIsStillInside);

			Assert.That(says, Does.Contain(TheOpenBranch).And.Contains(TheOpenSubject),
				"Y la frase la dice entera: donde esta y por que entro.");
			Assert.That(says, Does.Contain("todavía no lo han entregado"),
				"Diciendo que SIGUE ABIERTA, sin inventar ninguna salida.");
			Assert.That(says, Does.Not.Contain(TheClosedBranch),
				"Y es la abierta y no la cerrada: sin esto, una lectura que contestara siempre la "
				+ "primera estadia pasaria los dos renglones de arriba.");
		}

		// Y LAS SEIS DE PERIODO NO LA ALCANZAN, con el periodo que contiene su dia de entrada. Cada
		// una se afirma por separado: son seis sitios distintos por donde la columna 'Standing' podria
		// volver a tener sentido, y el rojo tiene que decir cual.
		[Test]
		public void NotOneOfTheSixDayBoundedReadsSeesIt()
		{
			Assert.That(journal.Api.HasAnyStayBetween(TheOctoberFrom, TheOctoberTo), Is.False,
				"HasAnyStayBetween en el mes de la entrada abierta.");

			Assert.That(journal.Api.DaysInTheShopBetween(TheOctoberFrom, TheOctoberTo), Is.Empty,
				"DaysInTheShopBetween: una estadia abierta no tiene ningun dia todavia.");

			Assert.That(
				journal.Api.DaysInTheShopCountBetween(TheOctoberFrom, TheOctoberTo), Is.EqualTo(0),
				"DaysInTheShopCountBetween en ese mes.");

			Assert.That(
				journal.Api.DescribeDaysInTheShopBetween(TheOctoberFrom, TheOctoberTo),
				Does.Contain("con días registrados"),
				"Y la frase dice que no hay dias REGISTRADOS, que no es lo mismo que decir que el "
				+ "vehiculo no estuvo adentro: en octubre lo estuvo y lo sigue estando.");

			Assert.That(journal.Api.StayCountBetween(TheOctoberFrom, TheOctoberTo), Is.EqualTo(0),
				"StayCountBetween en ese mes.");

			Assert.That(
				journal.Api.StaysBetween(TheOctoberFrom, TheOctoberTo).Branch, Is.Empty,
				"Y el registro no trae ninguna fila para ella.");
		}

		// Y EN EL AÑO ENTERO, DONDE LAS DOS EXISTEN, el periodo trae UNA sola: la cerrada. Es el
		// renglon que separa 'no la alcanza' de 'el periodo de octubre estaba vacio por otra razon'.
		[Test]
		public void TheYearThatHoldsBothBringsBackOnlyTheClosedOne()
		{
			Assert.That(journal.Api.StayCountBetween(TheYearFrom, TheYearTo), Is.EqualTo(1),
				"UNA estadia en el año, no dos: la abierta no entra a ningun periodo dicho en dias.");

			ShopStayRecord record = journal.Api.StaysBetween(TheYearFrom, TheYearTo);

			Assert.That(record.Branch, Is.EqualTo(new[] { TheClosedBranch }));
			Assert.That(record.Subject, Is.EqualTo(new[] { TheClosedSubject }));

			Assert.That(journal.Api.IsInsideTheShopAt(WhileItIsStillInside), Is.True,
				"Y la abierta sigue existiendo, que es lo que hace que su ausencia de arriba sea una "
				+ "decision y no un registro vacio.");
		}

		// LAS DOS COLUMNAS QUE §4.2 PEDIA NO ESTAN, y esto es lo que se pone rojo el dia que alguien
		// las vuelva a poner sin leer por que se quitaron. Una columna que solo puede decir un valor
		// es peso muerto en cada pantalla y en cada lector.
		[Test]
		public void TheRecordCarriesNeitherAStandingColumnNorACollectedAtColumn()
		{
			foreach (System.Reflection.PropertyInfo each in typeof(ShopStayRecord).GetProperties())
			{
				Assert.That(each.Name, Is.Not.EqualTo("Standing"),
					"Una estadia abierta no entra a un periodo dicho en dias, asi que esta columna "
					+ "solo podria decir 'Closed'. Si el registro pasa a tomar limites por INSTANTE, "
					+ "la columna vuelve a tener sentido y esta prueba es la que hay que leer primero.");

				Assert.That(each.Name, Is.Not.EqualTo("CollectedAt"),
					"Y esta nunca podria decir 'sigue adentro'.");
			}
		}
	}
}
