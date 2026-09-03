using System;
using System.Reflection;
using DealershipCar.App;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LA REGLA 2 DEL §7 DEL BRIEF: CADA FILA SE DICE A SI MISMA. Una fila que no significa nada sin
	// la de al lado es una mitad disfrazada de tabla, y ese es el defecto que separa un REGISTRO --
	// que si puede salir en crudo -- de una MITAD, que no.
	//
	// SE AFIRMA EL TEXTO DEL ASUNTO ADENTRO DE CADA FILA Y NO Is.Not.Empty (C60). Is.Not.Empty prueba
	// que llego una respuesta, nunca que es la de ESA fila: con dos filas, una columna que trajera la
	// misma frase dos veces pasaria igual. El discriminador mas barato es el SUJETO que el dominio ya
	// escribe adentro de su propia frase.
	//
	// LAS DOS FILAS LLEVAN CONTENIDO DISTINTO EN LAS CINCO COLUMNAS, y una tiene TRES dias donde la
	// otra tiene DOS: cualquier cruce entre columnas o entre filas cambia un valor y un largo.
	//
	// Y EL PARALELISMO ES POR CONSTRUCCION, no por costumbre: ShopStayRecord.Of refusa columnas de
	// largos distintos, porque una columna corta por uno emparejaria en silencio cada fila posterior
	// con otra estadia. Aca se afirma la ley y ademas que la construccion la sostiene.
	[TestFixture]
	public sealed class TheRecordsColumnsAreParallelAndEveryRowSaysItself
	{
		private const string TheVin = "JH4KA7532PC000001";

		private const string TheYearFrom = "2026-01-01";
		private const string TheYearTo = "2026-12-31";

		private const string TheMarchBranch = "Taller Escazú";
		private const string TheMarchSubject = "Frenos";

		private static readonly DateTime MarchEntered =
			new DateTime(2026, 3, 2, 7, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime MarchCollected =
			new DateTime(2026, 3, 4, 17, 0, 0, DateTimeKind.Utc);

		private static readonly string[] TheMarchDays =
			{ "2026-03-02", "2026-03-03", "2026-03-04" };

		private const string TheJulyBranch = "Taller Curridabat";
		private const string TheJulySubject = "Cambio de aceite";

		private static readonly DateTime JulyEntered =
			new DateTime(2026, 7, 20, 9, 15, 0, DateTimeKind.Utc);

		private static readonly DateTime JulyCollected =
			new DateTime(2026, 7, 21, 16, 0, 0, DateTimeKind.Utc);

		private static readonly string[] TheJulyDays = { "2026-07-20", "2026-07-21" };

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.RecordShopStay(
				TheMarchBranch, TheMarchSubject, MarchEntered, MarchCollected, TheMarchDays);

			journal.Api.RecordShopStay(
				TheJulyBranch, TheJulySubject, JulyEntered, JulyCollected, TheJulyDays);
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// LAS CINCO COLUMNAS MIDEN LO MISMO, Y LO MISMO ES LA CUENTA QUE EL DOMINIO COMPONE. Sin el
		// segundo renglon, cinco columnas vacias cumplirian el primero.
		[Test]
		public void TheFiveColumnsAreTheSameLengthAndThatLengthIsTheStayCount()
		{
			ShopStayRecord record = journal.Api.StaysBetween(TheYearFrom, TheYearTo);

			Assert.That(record.Subject, Has.Length.EqualTo(record.Branch.Length));
			Assert.That(record.EnteredAt, Has.Length.EqualTo(record.Branch.Length));
			Assert.That(record.CoveredDays, Has.Length.EqualTo(record.Branch.Length));
			Assert.That(record.Says, Has.Length.EqualTo(record.Branch.Length));

			Assert.That(
				record.Branch.Length,
				Is.EqualTo(journal.Api.StayCountBetween(TheYearFrom, TheYearTo)),
				"Y el largo es la cuenta que el dominio compone, no una que la caratula derive. Sin "
				+ "este renglon, cinco columnas VACIAS cumplirian los cuatro de arriba.");

			Assert.That(record.Branch, Has.Length.EqualTo(2));
		}

		// CADA FILA SE DICE A SI MISMA, Y SE COMPRUEBA POR EL SUJETO. La frase de la fila nombra el
		// asunto de ESA estadia; la de al lado nombra el suyo. Una columna que trajera la misma frase
		// dos veces, o que emparejara la frase de marzo con el asunto de julio, cambia esto.
		[Test]
		public void EveryRowNamesItsOwnSubjectInsideItsOwnSentence()
		{
			ShopStayRecord record = journal.Api.StaysBetween(TheYearFrom, TheYearTo);

			for (int row = 0; row < record.Branch.Length; row++)
			{
				// SE COMPARA CONTRA LA CLAUSULA DEL DOMINIO Y NO CONTRA EL TEXTO SUELTO, y esto es
				// lo que la primera version de esta prueba no hacia. MEDIDO: con la columna de asunto
				// trayendo la SUCURSAL, un Does.Contain(record.Subject[row]) pelado seguia verde,
				// porque la frase nombra la sucursal tambien. Cada mitad tiene que caer en SU
				// clausula, que es lo unico que distingue una columna de la de al lado.
				Assert.That(record.Says[row], Does.Contain("Entró por " + record.Subject[row]),
					"La fila " + row + " dice '" + record.Subject[row] + "' en su columna de asunto y "
					+ "su frase no lo dice como el asunto por el que entro. Una fila que no se dice a "
					+ "si misma es una mitad disfrazada de tabla.");

				Assert.That(
					record.Says[row], Does.Contain("en " + record.Branch[row] + " desde el"),
					"Y la misma fila tenia que nombrar SU sucursal, en la clausula de la sucursal.");

				Assert.That(record.Subject[row], Is.Not.EqualTo(record.Branch[row]),
					"Y las dos columnas de esta fila tenian que ser dos hechos distintos.");
			}

			Assert.That(record.Says[0], Does.Not.Contain(TheJulySubject),
				"Y la frase de marzo NO nombra el asunto de julio: sin este renglon, una columna que "
				+ "trajera las dos frases pegadas cumpliria el bucle de arriba.");
			Assert.That(record.Says[1], Does.Not.Contain(TheMarchSubject));
		}

		// LAS CINCO COLUMNAS DE UNA FILA SON DE LA MISMA ESTADIA, afirmadas por valor y una por una.
		// Es lo que un cruce entre columnas rompe, y los dos juegos de dias tienen largos distintos
		// para que hasta el largo lo diga.
		[Test]
		public void TheFiveCellsOfARowAreAllFromTheSameStay()
		{
			ShopStayRecord record = journal.Api.StaysBetween(TheYearFrom, TheYearTo);

			Assert.That(record.Branch[0], Is.EqualTo(TheMarchBranch));
			Assert.That(record.Subject[0], Is.EqualTo(TheMarchSubject));
			Assert.That(record.EnteredAt[0], Is.EqualTo("2026-03-02 07:00:00"));
			Assert.That(
				record.CoveredDays[0], Is.EqualTo("2026-03-02 2026-03-03 2026-03-04"),
				"TRES dias, en la fila de marzo.");

			Assert.That(record.Branch[1], Is.EqualTo(TheJulyBranch));
			Assert.That(record.Subject[1], Is.EqualTo(TheJulySubject));
			Assert.That(record.EnteredAt[1], Is.EqualTo("2026-07-20 09:15:00"));
			Assert.That(
				record.CoveredDays[1], Is.EqualTo("2026-07-20 2026-07-21"),
				"Y DOS en la de julio, asi que un cruce entre las dos filas cambia el largo tambien.");
		}

		// EL PARALELISMO ES POR CONSTRUCCION. Se le pide al DTO que arme una fila con una columna
		// corta por uno y refusa, asi que la ley no depende de que los cinco miembros del dominio
		// caminen el mismo resolvedor por costumbre.
		[Test]
		public void TheRecordRefusesColumnsOfDifferentLengths()
		{
			MethodInfo build = typeof(ShopStayRecord).GetMethod(
				"Of", BindingFlags.Static | BindingFlags.NonPublic);

			Assert.That(build, Is.Not.Null,
				"El DTO se arma por una fabrica interna: sin ella, la puerta podria construir una fila "
				+ "desalineada y nadie lo notaria hasta la pantalla.");

			TargetInvocationException refused = Assert.Throws<TargetInvocationException>(
				() => build.Invoke(null, new object[]
				{
					new[] { "a", "b" },
					new[] { "a", "b" },
					new[] { "a", "b" },
					new[] { "a" },
					new[] { "a", "b" },
				}));

			Assert.That(refused.InnerException, Is.TypeOf<InvalidOperationException>());
			Assert.That(refused.InnerException.Message, Does.Contain("2").And.Contains("1"),
				"Y el rechazo dice los dos largos que no calzan.");
		}

		// Y NINGUNA COLUMNA DEL REGISTRO CONTESTA UN NUMERO. El censo numerico de la puerta no puede
		// ver adentro de un string[] (R4), asi que la mitad que si se puede afirmar se afirma aca: las
		// cinco columnas son de TEXTO, y la unica cuenta del periodo la contesta el dominio.
		[Test]
		public void NoColumnOfTheRecordIsANumber()
		{
			foreach (PropertyInfo each in typeof(ShopStayRecord).GetProperties())
				Assert.That(each.PropertyType, Is.EqualTo(typeof(string[])),
					"La columna '" + each.Name + "' dejo de ser texto. Una columna numerica en el DTO "
					+ "es un numero que el censo de la puerta no alcanza a ver.");

			Assert.That(typeof(ShopStayRecord).GetProperties(), Has.Length.EqualTo(5));
		}
	}
}
