using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LAS PRUEBAS 8, 9 y 10 de §9 EN EL NIVEL B (ley L6, OINV-27, OINV-52).
	//
	// EL ESCENARIO ES DistanceOnly A PROPOSITO, por la misma razon que en el nivel A: una ley que
	// mide tiempo no puede quedarse quieta mientras corre una ventana de 60 dias, porque al
	// vencerla ya consumio la gracia de 14 y contesta Overdue. Con distancia y sin lecturas nuevas
	// la ley contesta lo mismo antes y despues de la ventana (C19).
	//
	// LO QUE ESTE NIVEL AGREGA: que el MOTIVO que la persona eligio llegue entero hasta la
	// operacion. OINV-28 journaliza el aplazamiento con su motivo, y en la puerta el motivo es un
	// nombre en el wire que hay que resolver -- exactamente donde se puede sustituir en silencio.
	[TestFixture]
	public sealed class AJournaledScheduleHoldsTheOwnersDeferrals
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TenThousandUnits = 10000;

		private static readonly DateTime Delivery =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
		private const int OdometerAtDelivery = 30000;

		private static readonly DateTime FellDue =
			new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime TheWindowEnds =
			new DateTime(2026, 3, 21, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime TheSecondWindowEnds =
			new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc);

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

		// PRUEBA 8 de §9: aplazar una vez -> Deferred, DeferralsUsed = 1. Cumplida la ventana, la
		// ley vuelve a decidir y la cuenta se conserva.
		[Test]
		public void DeferringOnceHoldsTheOperationUntilTheWindowIsSpent()
		{
			SeedAWearPartOf(false);
			ReachTheLimit();

			Assert.That(journal.Api.ReadDueState("WearPartReplacement"), Is.EqualTo("Due"),
				"Tiene que llegar vencida al aplazamiento, o esta prueba estaria mirando el no-op "
				+ "de E36 sin darse cuenta.");

			journal.Api.DeferOperation("WearPartReplacement", "Cost", FellDue);

			Assert.That(journal.Api.ReadDueState("WearPartReplacement"), Is.EqualTo("Deferred"));
			Assert.That(journal.Api.DeferralsUsed("WearPartReplacement"), Is.EqualTo(1));

			journal.Api.Evaluate(TheWindowEnds.AddDays(-1));

			Assert.That(journal.Api.ReadDueState("WearPartReplacement"), Is.EqualTo("Deferred"),
				"Un dia antes sigue aplazada.");

			journal.Api.Evaluate(TheWindowEnds);

			Assert.That(journal.Api.ReadDueState("WearPartReplacement"), Is.EqualTo("Due"),
				"Cumplida la ventana la ley vuelve a decidir, y esta ley sigue diciendo Due (C19).");
			Assert.That(journal.Api.DeferralsUsed("WearPartReplacement"), Is.EqualTo(1),
				"LA CUENTA SE CONSERVA. Reseteada al salir de la ventana, el tope de dos no seria "
				+ "un tope: serian dos por ventana, para siempre.");
		}

		// PRUEBA 9 de §9: dos aplazamientos, y el tercero se rechaza sin subir la cuenta.
		[Test]
		public void TheThirdDeferralIsRefusedAndTheCountStaysAtTheCap()
		{
			SeedAWearPartOf(false);
			ReachTheLimit();

			journal.Api.DeferOperation("WearPartReplacement", "Cost", FellDue);

			journal.Api.Evaluate(TheWindowEnds);
			journal.Api.DeferOperation("WearPartReplacement", "Timing", TheWindowEnds);

			Assert.That(journal.Api.DeferralsUsed("WearPartReplacement"), Is.EqualTo(2));

			journal.Api.Evaluate(TheSecondWindowEnds);

			Assert.That(journal.Api.ReadDueState("WearPartReplacement"), Is.EqualTo("Due"));

			journal.Api.DeferOperation(
				"WearPartReplacement", "SecondOpinion", TheSecondWindowEnds);

			Assert.That(journal.Api.ReadDueState("WearPartReplacement"), Is.EqualTo("Due"),
				"Rechazado quiere decir que la operacion se queda donde estaba: vencida.");
			Assert.That(journal.Api.DeferralsUsed("WearPartReplacement"), Is.EqualTo(2),
				"Ni sube a 3 ni se resetea: DeferralsUsed cuenta los ADMITIDOS (E35).");
		}

		// Llegar al tope es un RESULTADO ESPERADO y no un defecto de quien llama: la puerta no
		// revienta (E32).
		[Test]
		public void ADeferralRefusedAtTheCapIsAStateAndNotAnException()
		{
			SeedAWearPartOf(false);
			ReachTheLimit();

			journal.Api.DeferOperation("WearPartReplacement", "Cost", FellDue);
			journal.Api.Evaluate(TheWindowEnds);
			journal.Api.DeferOperation("WearPartReplacement", "Timing", TheWindowEnds);
			journal.Api.Evaluate(TheSecondWindowEnds);

			Assert.DoesNotThrow(
				() => journal.Api.DeferOperation("WearPartReplacement", "Cost", TheSecondWindowEnds),
				"La politica da dos y la persona pidio tres: un throw obligaria a la app a usar "
				+ "excepciones para el flujo normal de alguien que simplemente no puede ahora.");
		}

		// OINV-28 journaliza el aplazamiento CON SU MOTIVO. En la puerta el motivo es un nombre del
		// wire, asi que hay un lugar mas donde se puede sustituir en silencio -- y despues del
		// replay el registro diria que la persona aplazo por costo cuando queria otra opinion.
		[Test]
		public void TheReasonThePersonGaveIsTheReasonThatSurvivesTheJournal()
		{
			SeedAWearPartOf(false);
			ReachTheLimit();

			journal.Api.DeferOperation("WearPartReplacement", "SecondOpinion", FellDue);

			Assert.That(
				journal.Api.ReadDueReason("WearPartReplacement"), Does.Contain("una segunda opinión"));

			journal.CloseAndReopenFromTheJournal();

			Assert.That(
				journal.Api.ReadDueReason("WearPartReplacement"), Does.Contain("una segunda opinión"),
				"Y sigue siendo el suyo despues de reproducir el journal.");
			Assert.That(journal.Api.DeferralsUsed("WearPartReplacement"), Is.EqualTo(1));
		}

		// PRUEBA 10 de §9: una operacion clasificada de seguridad rechaza el PRIMER intento, y con
		// el tope entero. No es un tope mas bajo: es que la puerta no existe (OINV-52).
		[Test]
		public void ASafetyClassifiedOperationRefusesTheFirstDeferral()
		{
			SeedAWearPartOf(true);
			ReachTheLimit();

			Assert.That(journal.Api.ReadDueState("WearPartReplacement"), Is.EqualTo("Due"));

			journal.Api.DeferOperation("WearPartReplacement", "Cost", FellDue);

			Assert.That(journal.Api.ReadDueState("WearPartReplacement"), Is.EqualTo("Due"));
			Assert.That(journal.Api.DeferralsUsed("WearPartReplacement"), Is.EqualTo(0),
				"Y el intento rechazado no gasta un aplazamiento: si contara, la clasificacion de "
				+ "seguridad se leeria como 'cero de dos usados' en vez de 'ninguno'.");
		}

		// La bandera de seguridad es un bool que cruza por parametro, asi que puede llegar
		// invertida sin que nada mas cambie -- y entonces la operacion mas peligrosa de la agenda
		// seria la unica que se puede aplazar.
		[Test]
		public void TheSafetyClassificationCrossesTheWireWithoutFlipping()
		{
			SeedAWearPartOf(false);
			ReachTheLimit();

			journal.Api.DeferOperation("WearPartReplacement", "Cost", FellDue);

			Assert.That(journal.Api.ReadDueState("WearPartReplacement"), Is.EqualTo("Deferred"),
				"La MISMA operacion sin clasificar si se aplaza. Es lo que hace que la prueba de "
				+ "arriba este midiendo la clasificacion y no otra cosa.");
		}

		// EL CASO QUE LA PANTALLA NO PUEDE ALCANZAR HOY, y por eso vive aca.
		//
		// La agenda que sirve el concesionario tiene UNA operacion clasificada de seguridad --
		// TimedComponent -- y hoy esta en NotDue, asi que el control sale apagado por el ESTADO y no
		// por la clasificacion. Una pantalla que decidiera solo con el estado se veria identica de
		// correcta, y encenderia el control el dia que esa operacion pase a DueSoon: la unica
		// operacion que el dominio no aplaza NUNCA seria la que ofrece el boton.
		//
		// Aca esa operacion SI llega vencida, y la pregunta cruza la puerta de verdad -- el script
		// del DSL, el nombre por el wire, el journal -- que es lo que S5 exige antes de que una linea
		// de DSL toque el journal de una persona.
		[Test]
		public void TheDoorSaysADueSafetyOperationCannotBeDeferred()
		{
			SeedAWearPartOf(true);
			ReachTheLimit();

			Assert.That(journal.Api.ReadDueState("WearPartReplacement"), Is.EqualTo("Due"),
				"Vencida: el estado por si solo diria que se puede aplazar.");

			Assert.That(journal.Api.CanBeDeferred("WearPartReplacement"), Is.False,
				"OINV-52: no se aplaza nunca, y la pantalla tiene que poder saberlo ANTES de ofrecer "
				+ "el control. Deducirlo del estado da true justo aca.");

			// El bool de seguridad viaja por parametro y el replay lo reusa. Si se perdiera al
			// rehidratar, la operacion volveria aplazable al reabrir la app -- la peor forma de
			// fallar, porque nadie mira dos veces un control que ya vio funcionar.
			journal.CloseAndReopenFromTheJournal();

			Assert.That(journal.Api.CanBeDeferred("WearPartReplacement"), Is.False,
				"Y sigue diciendo lo mismo despues de reproducir el journal: la clasificacion es un "
				+ "hecho journalizado, no un dato de la sesion.");
		}

		[Test]
		public void TheDoorSaysTheSameOperationUnclassifiedCanBeDeferred()
		{
			SeedAWearPartOf(false);
			ReachTheLimit();

			Assert.That(journal.Api.CanBeDeferred("WearPartReplacement"), Is.True,
				"La MISMA operacion sin clasificar si se puede. Es lo que hace que la prueba de "
				+ "arriba este midiendo la clasificacion y no otra cosa.");
		}

		// Y EN EL TOPE, tambien por la puerta: es la tercera condicion de L6, y la unica que la
		// pantalla podria haber intentado deducir -- tiene DeferralsUsed -- salvo que el TOPE no se
		// proyecta, asi que 2 no se distingue de 1 desde afuera.
		[Test]
		public void TheDoorSaysAnOperationAtTheCapCannotBeDeferred()
		{
			SeedAWearPartOf(false);
			ReachTheLimit();

			journal.Api.DeferOperation("WearPartReplacement", "Cost", FellDue);
			journal.Api.Evaluate(TheWindowEnds);
			journal.Api.DeferOperation("WearPartReplacement", "Timing", TheWindowEnds);
			journal.Api.Evaluate(TheSecondWindowEnds);

			Assert.That(journal.Api.ReadDueState("WearPartReplacement"), Is.EqualTo("Due"));
			Assert.That(journal.Api.DeferralsUsed("WearPartReplacement"), Is.EqualTo(2));

			Assert.That(journal.Api.CanBeDeferred("WearPartReplacement"), Is.False);
		}

		private void SeedAWearPartOf(bool isSafety)
		{
			journal.Api.SeedPlanOperation(
				"WearPartReplacement", "DistanceOnly", 0, TenThousandUnits, "UnitA",
				true, isSafety,
				Delivery, OdometerAtDelivery, "UnitA");
		}

		private void ReachTheLimit()
		{
			journal.Api.RecordReading(40000, "UnitA", FellDue, "Telemetry");
			journal.Api.Evaluate(FellDue);
		}
	}
}
