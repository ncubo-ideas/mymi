using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LA LEY DE CONTIGUIDAD DE AClosedStay -- L5 puesta donde se puede comprobar, y la DEPARTURE 3
	// del §4.3 del plan.
	//
	// EL BRIEF PEDIA OTRA PRUEBA Y NO SE PUEDE ESCRIBIR (§12 R2 del plan). Su test 6 dice "una
	// estadia cuyos dias recibidos no incluyen su dia de entrada -> rechazada". Comprobar eso es
	// comparar un DIA DE CALENDARIO recibido contra el dia de un INSTANTE, y sacar el dia de un
	// instante necesita la zona que G1 y L4 se niegan a inventar. Lo que si se puede comprobar sin
	// zona ninguna es lo que esta prueba comprueba: que los dias que llegaron son al menos uno,
	// estrictamente ascendentes y CONTIGUOS.
	//
	// UNA SOLA REGLA LAS CUBRE A LAS TRES: cada dia tiene que ser el Next() del anterior. Un salto
	// falla, un repetido falla y un desorden falla, y ninguna de las tres necesita un umbral -- §6
	// del brief sigue en pie.
	//
	// POR QUE ES UNA CONTRADICCION Y NO UNA ESTADIA RARA: una estadia es continua por naturaleza. El
	// carro no sale el 11 y vuelve el 12 dentro de la misma estadia; eso son DOS estadias, y el
	// registro las tiene.
	[TestFixture]
	public sealed class ADayThatIsNotTheNextOfThePreviousIsARecordThatContradictsItself
	{
		private const string TheVin = "1FTFW1ET5DFC10312";
		private const string TheBranch = "Taller Curridabat";
		private const string TheSubject = "Cambio de aceite";

		private static readonly DateTime Entered = new DateTime(2026, 6, 10, 8, 30, 0);
		private static readonly DateTime Collected = new DateTime(2026, 6, 12, 16, 45, 0);

		private ShopStay stays;

		[SetUp]
		public void Setup()
		{
			stays = new ShopStay(TheVin);
		}

		// EL SALTO, que es el caso que la ley nombra: llegaron el 10 y el 12, y entre los dos hay un
		// 11 que nadie mando.
		[Test]
		public void ADayMissingInTheMiddleIsRefusedAndTheMessageNamesTheTwoDays()
		{
			DomainException refusal = Assert.Throws<DomainException>(() =>
				stays.RecordStay(TheBranch, TheSubject, Entered, Collected,
					new[] { "2026-06-10", "2026-06-12" }));

			Assert.That(refusal.Message, Does.Contain("2026-06-10"));
			Assert.That(refusal.Message, Does.Contain("2026-06-12"));
			Assert.That(refusal.Message, Does.Contain("2026-06-11"),
				"y nombra el dia que falta, que es lo que quien registro tiene que arreglar");
		}

		[Test]
		public void ARepeatedDayIsRefused()
		{
			Assert.Throws<DomainException>(() =>
				stays.RecordStay(TheBranch, TheSubject, Entered, Collected,
					new[] { "2026-06-10", "2026-06-10" }));
		}

		[Test]
		public void DaysThatGoBackwardsAreRefused()
		{
			Assert.Throws<DomainException>(() =>
				stays.RecordStay(TheBranch, TheSubject, Entered, Collected,
					new[] { "2026-06-12", "2026-06-11", "2026-06-10" }));
		}

		// UNA ESTADIA CERRADA CUBRIO AL MENOS UN DIA. Cero dias en una estadia cerrada seria una
		// estadia que no paso, y la salida ya dice que paso.
		[Test]
		public void AClosedStayWithNoDaysAtAllIsRefused()
		{
			Assert.Throws<DomainException>(() =>
				stays.RecordStay(TheBranch, TheSubject, Entered, Collected, new string[0]));

			Assert.Throws<DomainException>(() =>
				stays.RecordStay(TheBranch, TheSubject, Entered, Collected, null));
		}

		// LA MISMA LEY POR LA OTRA RUTA. Cerrar una estadia abierta pasa por el mismo constructor,
		// asi que la ley no depende de por cual de las dos rutas llego el hecho.
		[Test]
		public void TheSameLawHoldsWhenAnOpenStayIsClosed()
		{
			stays.RecordEntry(TheBranch, TheSubject, Entered);

			Assert.Throws<DomainException>(() =>
				stays.RecordCollection(Collected, new[] { "2026-06-10", "2026-06-12" }));
		}

		// Y LA ESTADIA ABIERTA QUEDA INTACTA DESPUES DEL RECHAZO (E22): la guarda corre ANTES de que
		// nada se mueva, asi que un cierre rechazado no deja media estadia cerrada.
		[Test]
		public void ARefusedCollectionLeavesTheOpenStayExactlyAsItWas()
		{
			stays.RecordEntry(TheBranch, TheSubject, Entered);

			Assert.Throws<DomainException>(() =>
				stays.RecordCollection(Collected, new[] { "2026-06-10", "2026-06-12" }));

			Assert.That(stays.IsInsideTheShopAt(Collected.AddDays(1)), Is.True,
				"sigue abierta, que es lo que era antes del rechazo");

			stays.RecordCollection(Collected, new[] { "2026-06-10", "2026-06-11", "2026-06-12" });

			Assert.That(stays.DaysInTheShopCountBetween("2026-06-01", "2026-06-30"), Is.EqualTo(3),
				"y se puede cerrar bien despues, que es lo que 'intacta' quiere decir");
		}

		// EL CONTROL. Sin este renglon, una guarda que rechazara TODO pasaria las cinco de arriba.
		[Test]
		public void ThreeContiguousDaysAreAccepted()
		{
			stays.RecordStay(TheBranch, TheSubject, Entered, Collected,
				new[] { "2026-06-10", "2026-06-11", "2026-06-12" });

			Assert.That(stays.DaysInTheShopCountBetween("2026-06-01", "2026-06-30"), Is.EqualTo(3));
		}

		// Y LA CONTIGUIDAD CRUZA EL FIN DE MES, que es donde una comparacion aritmetica sobre el
		// numero del dia se rompe sin que nada mas se vea mal.
		[Test]
		public void ContiguityCrossesTheEndOfAMonth()
		{
			stays.RecordStay(TheBranch, TheSubject,
				new DateTime(2026, 6, 29, 8, 0, 0), new DateTime(2026, 7, 1, 10, 0, 0),
				new[] { "2026-06-29", "2026-06-30", "2026-07-01" });

			Assert.That(stays.DaysInTheShopCountBetween("2026-06-01", "2026-07-31"), Is.EqualTo(3));
		}
	}
}
