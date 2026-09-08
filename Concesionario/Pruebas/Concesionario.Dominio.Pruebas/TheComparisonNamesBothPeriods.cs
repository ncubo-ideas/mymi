using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LEY L11 de SLICE-CostLedger.md: "'Este año lleva ₡285.000; el año pasado eran ₡152.000' -- la
	// respuesta es EL PAR, cada periodo nombrado, cada uno con sus dos vistas. Una diferencia suelta o
	// un porcentaje suelto son la mitad de una respuesta compuesta, y el modulo tiene una ley fijada
	// sobre esas."
	//
	// EL EJEMPLO DEL CANON NO DICE NINGUNA DIFERENCIA, Y ESO DECIDE LA FORMA. ATwoSpanComparison --
	// la comparacion de OD-1, entre dos ritmos de uso -- si dice el cambio adentro de la frase. Esta
	// no, y son dos razones distintas y las dos bastan por si solas:
	//
	//   Un porcentaje entre dos periodos necesitaria un TERCER redondeo autorado por el dominio, y
	//   R7 del plan dice que en este plan el dominio autora DOS -- el de OD-1 y el de DEC-87 -- y que
	//   un tercero necesita su propio argumento. No lo tiene.
	//
	//   Y una diferencia entre dos periodos en monedas distintas no es un numero (L3), asi que la
	//   respuesta tendria una forma para un caso y otra para el otro.
	//
	// Asi que la comparacion dice las dos frases enteras, cada una nombrando su periodo, y no dice
	// ninguna resta. Las dos cifras estan al lado y la resta la hace quien lee, que es lo que el
	// ejemplo del canon muestra.
	[TestFixture]
	public sealed class TheComparisonNamesBothPeriods
	{
		private const string TheVin = "1FTFW1ET5DFC10312";

		private static readonly DateTime ThisYearFrom = new DateTime(2026, 1, 1, 0, 0, 0);
		private static readonly DateTime ThisYearTo = new DateTime(2026, 12, 31, 0, 0, 0);
		private static readonly DateTime LastYearFrom = new DateTime(2025, 1, 1, 0, 0, 0);
		private static readonly DateTime LastYearTo = new DateTime(2025, 12, 31, 0, 0, 0);

		private CostLedger ledger;

		[SetUp]
		public void Setup()
		{
			ledger = new CostLedger(TheVin);

			ledger.RecordRecurringCost(
				new DateTime(2026, 5, 10, 9, 0, 0), 285000m, "CRC", "Reparaciones de este año");
			ledger.RecordRecurringCost(
				new DateTime(2025, 5, 10, 9, 0, 0), 152000m, "CRC", "Reparaciones del año pasado");
		}

		[Test]
		public void BothPeriodsAreNamedAndBothCarryTheirOwnFigures()
		{
			string said = ledger.DescribeTwoPeriodsOfSpendCompared(
				ThisYearFrom, ThisYearTo, LastYearFrom, LastYearTo);

			Assert.That(said, Does.Contain("₡285000.00"));
			Assert.That(said, Does.Contain("₡152000.00"));
			Assert.That(said, Does.Contain("2026-01-01").And.Contain("2026-12-31"),
				"Cada periodo nombra sus propios limites: sin ellos la frase dice dos cifras de dos "
				+ "periodos que no se sabe cuales son.");
			Assert.That(said, Does.Contain("2025-01-01").And.Contain("2025-12-31"));
			Assert.That(said, Does.Contain("Primer período").And.Contain("Segundo período"),
				"Y se sabe cual es cual, en el orden en que se preguntaron.");
		}

		// LAS DOS VISTAS EN CADA UNO DE LOS DOS, que es L6 sobreviviendo dentro de L11: la tentacion
		// de una comparacion es decir una cifra por periodo.
		[Test]
		public void AndEachOfTheTwoCarriesBothOfItsViews()
		{
			string said = ledger.DescribeTwoPeriodsOfSpendCompared(
				ThisYearFrom, ThisYearTo, LastYearFrom, LastYearTo);

			Assert.That(TimesItSays(said, "₡285000.00"), Is.EqualTo(2),
				"Con solo gastos recurrentes la caja y lo consumido son la misma cifra, asi que "
				+ "cada periodo la dice DOS veces: una por vista. Una sola vez seria una vista "
				+ "sola.");
			Assert.That(TimesItSays(said, "₡152000.00"), Is.EqualTo(2));
		}

		// NINGUNA RESTA Y NINGUN PORCENTAJE, dicho por valor sobre las tres formas que tomaria.
		[Test]
		public void AndTheAnswerCarriesNoDeltaAndNoPercentage()
		{
			string said = ledger.DescribeTwoPeriodsOfSpendCompared(
				ThisYearFrom, ThisYearTo, LastYearFrom, LastYearTo);

			Assert.That(said, Does.Not.Contain("133000"),
				"285000 menos 152000. Una diferencia suelta es la mitad de una respuesta compuesta.");
			Assert.That(said, Does.Not.Contain("%"));
			Assert.That(said, Does.Not.Contain("87").And.Not.Contain("188"),
				"Ni el porcentaje en ninguna de sus dos direcciones.");
		}

		// Y NINGUNA LECTURA DEVUELVE LA DIFERENCIA SOLA -- el test 11 del brief -- por censo de los
		// nombres Y de los tipos de retorno: un `TheChangeBetween` que devolviera un decimal cae por
		// el segundo aunque se llamara de otra forma.
		[Test]
		public void AndNoReadReturnsTheDeltaAlone()
		{
			List<string> read = new List<string>();

			foreach (MethodInfo member in typeof(CostLedger).GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
				| BindingFlags.DeclaredOnly))
			{
				if (!(member.IsPublic || member.IsAssembly) || member.IsSpecialName) continue;

				read.Add(member.Name);

				foreach (string word in new[] { "Delta", "Change", "Difference", "Percent", "Ratio" })
					Assert.That(member.Name, Does.Not.Contain(word),
						"CostLedger." + member.Name + " nombra una diferencia. L11: el par, cada "
						+ "periodo nombrado.");
			}

			TestContext.Out.WriteLine(
				"LOS MIEMBROS EXPUESTOS DEL LIBRO (" + read.Count + ") -- "
				+ string.Join(", ", read));
		}

		// UN PERIODO SIN GASTO SIGUE COMPARANDOSE, y la frase dice cual de los dos no tiene nada:
		// es la mitad total de E47 dentro de la comparacion.
		[Test]
		public void AndAPeriodWithNothingIsStillComparedAndSaidAsSuch()
		{
			string said = ledger.DescribeTwoPeriodsOfSpendCompared(
				ThisYearFrom, ThisYearTo,
				new DateTime(2024, 1, 1, 0, 0, 0), new DateTime(2024, 12, 31, 0, 0, 0));

			Assert.That(said, Does.Contain("₡285000.00"));
			Assert.That(said, Does.Contain("no hay ningún gasto registrado"));
			Assert.That(said, Does.Contain("2024-01-01"),
				"El periodo vacio tambien se nombra: es lo que lo distingue de otro periodo vacio.");
		}

		// Y LA COMPARACION NECESITA LOS DOS PERIODOS, con guarda propia y con su propio test, porque
		// una guarda que nadie defiende es una afirmacion estructural que nada puede ver.
		[Test]
		public void AndAComparisonWithOnlyOnePeriodIsRefused()
		{
			DomainException refused = Assert.Throws<DomainException>(
				() => ASpendComparison.Of(null, null));

			Assert.That(refused.Message, Does.Contain("dos"),
				"El rechazo dice que hacen falta los dos.");
		}

		private static int TimesItSays(string said, string figure)
		{
			int times = 0;
			int from = said.IndexOf(figure, StringComparison.Ordinal);

			while (from >= 0)
			{
				times++;
				from = said.IndexOf(figure, from + figure.Length, StringComparison.Ordinal);
			}

			return times;
		}
	}
}
