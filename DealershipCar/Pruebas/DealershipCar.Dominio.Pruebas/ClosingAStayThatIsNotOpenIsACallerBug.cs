using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LAS TRANSICIONES ILEGALES DE §10 DEL BRIEF que no son L6: cerrar una estadia que no esta
	// abierta, y reabrir una cerrada.
	//
	// LA POLARIDAD. Una LECTURA de un periodo sin estadias contesta "no hay ninguna" (L8, E41): la
	// ausencia es una respuesta legitima. Cerrar lo que no esta abierto es lo contrario -- no hay
	// nada que cerrar, asi que no es una respuesta vacia sino un defecto de quien llama, y REVIENTA
	// (E32). Las dos leyes viven en el mismo agregado y apuntan al lado opuesto, que es por lo que
	// esta prueba existe aparte de NoStayIsAnAnswer.
	//
	// REABRIR UNA CERRADA NO NECESITA GUARDA PROPIA, y eso es E29 funcionando: al cerrarse, la
	// identidad de la abierta se DISUELVE y el agregado cambia el objeto por otro tipo. No queda
	// nada abierto que reabrir, asi que el segundo cierre cae en la misma guarda que el primero sin
	// que nadie escriba una segunda.
	//
	// EL MENSAJE NOMBRA LA PREGUNTA QUE HAY QUE HACER PRIMERO (E41): quien llama tiene una lectura
	// TOTAL con que averiguarlo antes, y el mensaje se la dice.
	[TestFixture]
	public sealed class ClosingAStayThatIsNotOpenIsACallerBug
	{
		private const string TheVin = "1FTFW1ET5DFC10312";
		private const string TheBranch = "Taller Curridabat";
		private const string TheSubject = "Cambio de aceite";

		private static readonly DateTime Entered = new DateTime(2026, 6, 10, 8, 30, 0);
		private static readonly DateTime Collected = new DateTime(2026, 6, 12, 16, 45, 0);

		private static readonly string[] TheDaysItCovered =
			{ "2026-06-10", "2026-06-11", "2026-06-12" };

		private ShopStay stays;

		[SetUp]
		public void Setup()
		{
			stays = new ShopStay(TheVin);
		}

		[Test]
		public void ClosingWithNothingRecordedAtAllIsRefused()
		{
			DomainException refusal = Assert.Throws<DomainException>(() =>
				stays.RecordCollection(Collected, TheDaysItCovered));

			Assert.That(refusal.Message, Does.Contain("adentro"),
				"E41: el mensaje nombra la pregunta TOTAL que hay que hacer antes.");
		}

		// REABRIR UNA CERRADA. No hay guarda escrita para esto: la hay porque al cerrar, el objeto
		// abierto dejo de existir (E29).
		[Test]
		public void ClosingAStayThatWasAlreadyClosedIsRefused()
		{
			stays.RecordEntry(TheBranch, TheSubject, Entered);
			stays.RecordCollection(Collected, TheDaysItCovered);

			Assert.Throws<DomainException>(() =>
				stays.RecordCollection(new DateTime(2026, 6, 13, 9, 0, 0), new[] { "2026-06-13" }));
		}

		// Y UNA ESTADIA REGISTRADA DE UN SOLO ACTO TAMPOCO DEJA NADA QUE CERRAR: nace cerrada.
		[Test]
		public void AStayRecordedInOneActLeavesNothingToClose()
		{
			stays.RecordStay(TheBranch, TheSubject, Entered, Collected, TheDaysItCovered);

			Assert.Throws<DomainException>(() =>
				stays.RecordCollection(new DateTime(2026, 6, 13, 9, 0, 0), new[] { "2026-06-13" }));
		}

		// EL SEGUNDO CIERRE RECHAZADO NO TOCA EL PRIMERO. Sin esto, un rechazo que igual hubiera
		// pisado los dias pasaria las de arriba.
		[Test]
		public void ARefusedSecondClosingLeavesTheFirstStayAsItWas()
		{
			stays.RecordEntry(TheBranch, TheSubject, Entered);
			stays.RecordCollection(Collected, TheDaysItCovered);

			Assert.Throws<DomainException>(() =>
				stays.RecordCollection(new DateTime(2026, 6, 13, 9, 0, 0), new[] { "2026-06-13" }));

			Assert.That(stays.DaysInTheShopBetween("2026-06-01", "2026-06-30"),
				Is.EqualTo(TheDaysItCovered));
		}

		// EL CONTROL: con una abierta, cerrar funciona.
		[Test]
		public void ClosingAnOpenStayWorks()
		{
			stays.RecordEntry(TheBranch, TheSubject, Entered);
			stays.RecordCollection(Collected, TheDaysItCovered);

			Assert.That(stays.DaysInTheShopCountBetween("2026-06-01", "2026-06-30"), Is.EqualTo(3));
			Assert.That(stays.IsInsideTheShopAt(new DateTime(2026, 6, 13)), Is.False);
		}
	}
}
