using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L6 DEL SLICE, test 8 de §11 del brief. "Rechazada, y el rechazo dice CUALES DOS INSTANTES
	// se contradicen."
	//
	// POR QUE ESTE RECHAZO SI ES LEGITIMO Y EL DE §9 R6 DE PLAN-WarrantyEnvelope NO. El brief lo
	// dice en la propia ley: aca los DOS extremos los suministra el mismo hecho registrado, asi que
	// rechazar no es rechazarle a quien llama algo que no aporto. En R6 el vehiculo tiene un
	// odometro de hoy que la persona no puso y una entrega que tampoco, y por eso ese defecto sigue
	// abierto a proposito.
	//
	// EL MENSAJE NOMBRA LOS DOS INSTANTES, y los nombra CON SU HORA. Un mensaje que dijera solo el
	// dia no distinguiria el caso que de verdad pasa -- salir a las 08:29 y entrar a las 08:30 del
	// mismo dia -- y quien lo lea no sabria que arreglar.
	//
	// LA IGUALDAD SE ACEPTA, y no es un descuido. C13: el journal trunca los instantes al segundo
	// entero, asi que dos instantes dentro del mismo segundo llegan IGUALES despues de una
	// reapertura. Rechazar la igualdad haria que un registro legitimo se aceptara vivo y se negara
	// en el replay, que es exactamente lo que L7 dice que la granularidad de segundo no debe
	// alcanzar.
	[TestFixture]
	public sealed class AnExitBeforeItsEntryIsARecordThatContradictsItself
	{
		private const string TheVin = "1FTFW1ET5DFC10312";
		private const string TheBranch = "Taller Curridabat";
		private const string TheSubject = "Cambio de aceite";

		private static readonly DateTime Entered = new DateTime(2026, 6, 10, 8, 30, 0);
		private static readonly DateTime OneSecondBeforeEntering = new DateTime(2026, 6, 10, 8, 29, 59);

		private static readonly string[] OneDay = { "2026-06-10" };

		private ShopStay stays;

		[SetUp]
		public void Setup()
		{
			stays = new ShopStay(TheVin);
		}

		// LA RUTA DE UN SOLO ACTO, y el mensaje con los DOS instantes adentro.
		[Test]
		public void AStayRecordedInOneActWithItsExitFirstIsRefusedNamingBothInstants()
		{
			DomainException refusal = Assert.Throws<DomainException>(() =>
				stays.RecordStay(TheBranch, TheSubject, Entered, OneSecondBeforeEntering, OneDay));

			Assert.That(refusal.Message, Does.Contain("2026-06-10 08:30:00"),
				"L6: el mensaje nombra el instante de ENTRADA.");
			Assert.That(refusal.Message, Does.Contain("2026-06-10 08:29:59"),
				"L6: y el de SALIDA. Con un dia suelto, los dos serian el mismo texto y el mensaje "
				+ "no diria nada.");
		}

		// LA RUTA DE DOS ACTOS. La misma ley, en el mismo constructor.
		[Test]
		public void ACollectionBeforeItsEntryIsRefusedNamingBothInstants()
		{
			stays.RecordEntry(TheBranch, TheSubject, Entered);

			DomainException refusal = Assert.Throws<DomainException>(() =>
				stays.RecordCollection(OneSecondBeforeEntering, OneDay));

			Assert.That(refusal.Message, Does.Contain("2026-06-10 08:30:00"));
			Assert.That(refusal.Message, Does.Contain("2026-06-10 08:29:59"));
		}

		// Y NADA QUEDA A MEDIAS (E22, E12). El rechazo corre antes de que la estadia abierta se
		// reemplace, asi que sigue abierta y se puede cerrar bien.
		[Test]
		public void ARefusedCollectionLeavesTheStayOpen()
		{
			stays.RecordEntry(TheBranch, TheSubject, Entered);

			Assert.Throws<DomainException>(() =>
				stays.RecordCollection(OneSecondBeforeEntering, OneDay));

			Assert.That(stays.IsInsideTheShopAt(Entered.AddDays(3)), Is.True);

			stays.RecordCollection(new DateTime(2026, 6, 10, 17, 0, 0), OneDay);

			Assert.That(stays.IsInsideTheShopAt(Entered.AddDays(3)), Is.False);
		}

		// LA IGUALDAD SE ACEPTA (L7, C13). Un instante de salida IGUAL al de entrada es una estadia
		// de un segundo mal medido, no una contradiccion -- y despues de una reapertura es lo unico
		// que queda de dos instantes dentro del mismo segundo.
		[Test]
		public void AnExitAtTheVeryInstantOfItsEntryIsNotAContradiction()
		{
			stays.RecordStay(TheBranch, TheSubject, Entered, Entered, OneDay);

			Assert.That(stays.DaysInTheShopCountBetween("2026-06-01", "2026-06-30"), Is.EqualTo(1),
				"L7: la granularidad de segundo no alcanza a esta respuesta. Rechazar la igualdad "
				+ "aceptaria vivo lo que el replay negaria (C13).");
		}

		// EL CONTROL: una salida posterior se acepta. Sin el, una guarda que rechazara todo pasaria
		// las de arriba.
		[Test]
		public void AnExitAfterItsEntryIsAccepted()
		{
			stays.RecordStay(TheBranch, TheSubject, Entered, new DateTime(2026, 6, 10, 17, 0, 0), OneDay);

			Assert.That(stays.DaysInTheShopCountBetween("2026-06-01", "2026-06-30"), Is.EqualTo(1));
		}
	}
}
