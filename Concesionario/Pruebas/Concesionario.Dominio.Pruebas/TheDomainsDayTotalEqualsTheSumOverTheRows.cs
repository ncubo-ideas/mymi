using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// REGLA 4 DE LA CONVENCION DE REGISTRO CRUDO (§7 del brief, §2.1 del plan) y LEY L9: "el total
	// del dominio y la suma de las filas coinciden, y una prueba lo dice. No porque la caratula
	// pueda sumarlas, sino para que nunca este tentada ni tenga razon."
	//
	// Y ESTA PRUEBA ES EXACTAMENTE LA TRAMPA DE E49, asi que se escribe con las DOS obligaciones y
	// no con una. El total del periodo y las filas del periodo salen del MISMO resolutor privado
	// -- TheStaysBetween -- asi que coinciden POR CONSTRUCCION, tambien si el resolutor estuviera
	// uniformemente equivocado. Borrar el filtro de periodo del resolutor deja las dos mitades en
	// perfecto acuerdo sobre un numero que ninguna de las dos debia usar.
	//
	//   obligacion 1, ESTRUCTURAL: un solo miembro resuelve el periodo y todo lo demas lo envuelve.
	//   obligacion 2, EL ANCLA: algo tiene que estar amarrado a los dias RECIBIDOS, por valor.
	//
	// LA TERCERA PRUEBA ES EL LIMITE DECLARADO (C56). La ley vale para filas cuyos dias caen DENTRO
	// del periodo. Una estadia que cruza el borde aporta al total solo sus dias de adentro mientras
	// la fila sigue nombrando todos los suyos -- y eso no es un defecto, es que la lectura esta
	// acotada. Se afirma en vez de dejarse escrito en prosa.
	[TestFixture]
	public sealed class TheDomainsDayTotalEqualsTheSumOverTheRows
	{
		private const string TheVin = "1FTFW1ET5DFC10312";
		private const string TheBranch = "Taller Curridabat";
		private const string TheOtherBranch = "Taller Escazu";
		private const string TheSubject = "Cambio de aceite";
		private const string TheOtherSubject = "Llanta ponchada";

		private const string TheFirstOfJune = "2026-06-01";
		private const string TheLastOfJune = "2026-06-30";

		private ShopStay stays;

		[SetUp]
		public void Setup()
		{
			stays = new ShopStay(TheVin);

			stays.RecordStay(TheBranch, TheSubject,
				new DateTime(2026, 6, 10, 8, 30, 0), new DateTime(2026, 6, 12, 16, 45, 0),
				new[] { "2026-06-10", "2026-06-11", "2026-06-12" });

			stays.RecordStay(TheOtherBranch, TheOtherSubject,
				new DateTime(2026, 6, 20, 7, 0, 0), new DateTime(2026, 6, 20, 11, 0, 0),
				new[] { "2026-06-20" });
		}

		// REGLA 4. El total compuesto y la suma sobre las filas, sumada AQUI.
		[Test]
		public void TheComposedTotalEqualsTheSumOfTheRowsDayCounts()
		{
			StaysInAPeriod period = stays.TheStaysBetween(TheFirstOfJune, TheLastOfJune);

			int sumOverTheRows = 0;
			foreach (AStay row in period.TheStays())
			{
				Assert.That(row.IsOpen(), Is.False,
					"una fila abierta no tiene dias que sumar (L2), y no deberia estar en un "
					+ "periodo acotado por dias");
				sumOverTheRows += row.TheDaysItCovered().Length;
			}

			Assert.That(period.HowManyDays(), Is.EqualTo(sumOverTheRows));
			Assert.That(period.HowManyStays(), Is.EqualTo(2));
		}

		// OBLIGACION 2 DE E49, EL ANCLA. Sin este renglon la de arriba compara dos consumidores del
		// mismo resolutor y no dice nada sobre el resolutor.
		[Test]
		public void TheTotalIsAnchoredToTheDaysThatWereReceived()
		{
			Assert.That(stays.DaysInTheShopBetween(TheFirstOfJune, TheLastOfJune),
				Is.EqualTo(new[] { "2026-06-10", "2026-06-11", "2026-06-12", "2026-06-20" }),
				"los dias que llegaron, por valor y en orden");
			Assert.That(stays.DaysInTheShopCountBetween(TheFirstOfJune, TheLastOfJune), Is.EqualTo(4));
			Assert.That(stays.StayCountBetween(TheFirstOfJune, TheLastOfJune), Is.EqualTo(2));
		}

		// OBLIGACION 1 DE E49, ESTRUCTURAL: UN solo miembro resuelve el periodo. Si hubiera dos, dos
		// lecturas podrian estar keyadas distinto y coincidir por casualidad en el escenario que
		// alguien eligio.
		[Test]
		public void ExactlyOneMemberOfTheAggregateResolvesAPeriod()
		{
			List<string> resolutores = typeof(ShopStay)
				.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static
					| BindingFlags.Public | BindingFlags.NonPublic)
				.Where(metodo => metodo.ReturnType == typeof(StaysInAPeriod))
				.Select(metodo => metodo.Name)
				.ToList();

			Assert.That(resolutores, Is.EqualTo(new[] { "TheStaysBetween" }),
				"E49 obligacion 1: un solo resolutor y todo lo demas lo envuelve. Aparecieron: "
				+ string.Join(", ", resolutores));
		}

		// EL LIMITE, AFIRMADO (C56). La estadia que cruza el borde de junio aporta al total sus dos
		// dias de junio; su fila sigue nombrando los tres que llegaron.
		[Test]
		public void AStayThatStraddlesTheEdgeContributesOnlyItsDaysInsideThePeriod()
		{
			stays.RecordStay(TheBranch, "Revision de fin de mes",
				new DateTime(2026, 6, 29, 8, 0, 0), new DateTime(2026, 7, 1, 10, 0, 0),
				new[] { "2026-06-29", "2026-06-30", "2026-07-01" });

			Assert.That(stays.DaysInTheShopBetween(TheFirstOfJune, TheLastOfJune),
				Is.EqualTo(new[]
				{
					"2026-06-10", "2026-06-11", "2026-06-12", "2026-06-20", "2026-06-29",
					"2026-06-30"
				}),
				"el 2026-07-01 esta afuera del periodo y no entra al total");

			StaysInAPeriod june = stays.TheStaysBetween(TheFirstOfJune, TheLastOfJune);

			int sumOverTheRows = june.TheStays().Sum(row => row.TheDaysItCovered().Length);

			Assert.That(june.HowManyDays(), Is.EqualTo(6));
			Assert.That(sumOverTheRows, Is.EqualTo(7),
				"y la fila sigue nombrando sus TRES dias. Regla 4 vale sobre filas de adentro; "
				+ "una que cruza el borde es una lectura acotada, no un defecto.");
		}

		// Y LOS DIAS DE UNA FILA SON LOS QUE LLEGARON CON ELLA, no los del periodo. Es lo que hace
		// que la suma de arriba tenga sentido como suma sobre FILAS.
		[Test]
		public void EachRowNamesTheDaysThatArrivedWithIt()
		{
			List<AStay> rows = stays.TheStaysBetween(TheFirstOfJune, TheLastOfJune).TheStays();

			Assert.That(rows[0].TheDaysItCovered().Select(day => day.HowItIsSaid()),
				Is.EqualTo(new[] { "2026-06-10", "2026-06-11", "2026-06-12" }));
			Assert.That(rows[1].TheDaysItCovered().Select(day => day.HowItIsSaid()),
				Is.EqualTo(new[] { "2026-06-20" }));
			Assert.That(rows[0].Branch, Is.EqualTo(TheBranch),
				"C60: la afirmacion nombra de que fila habla");
			Assert.That(rows[1].Branch, Is.EqualTo(TheOtherBranch));
		}
	}
}
