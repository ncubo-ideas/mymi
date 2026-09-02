using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L9, PlanIsNotTheOwnersToEdit, sobre MaintenanceSchedule (§5 del slice, OINV-31):
	// "La persona agrega y quita SUS operaciones. Una del plan del fabricante se puede aplazar o
	// declinar, pero nunca agregar, quitar ni re-fechar."
	//
	// Es la prueba 13 de §9.
	//
	// LA POLARIDAD ES LA DE E32, y OINV-31 la nombra: su columna de rechazo dice
	// "RemovalRefused, reason NotOwnerAdded" -- un HECHO del negocio, no una excepcion. Quitar
	// una operacion del plan es la persona pidiendo algo que no es suyo de hacer; la app le
	// muestra un boton y la ley contesta que no. Reventar obligaria a la app a usar excepciones
	// para el flujo normal de alguien que toco lo que no era.
	//
	// COMO SE VE QUE SE QUITO, sin que cruce ninguna coleccion (§8): por el MOTIVO. Una clase que
	// no esta en la agenda contesta que no hay nada de esa clase agendado; una que si esta
	// contesta lo que la ley dijo de ella. Es la unica lectura que distingue las dos, y por eso
	// ReadDueReason es total con una frase de verdad y no con una cadena vacia.
	[TestFixture]
	public sealed class TheOwnerEditsOnlyTheirOwnOperations
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TwelveMonthsInDays = 365;
		private const int TenThousandUnits = 10000;

		private static readonly DateTime Delivery = new DateTime(2025, 6, 1);
		private const int OdometerAtDelivery = 30000;

		private static readonly DateTime TheLimitIsReached = new DateTime(2026, 1, 20);

		private MaintenanceSchedule schedule;

		[SetUp]
		public void Setup()
		{
			schedule = new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);

			schedule.SeedPlanOperation(
				OperationKindCode.FluidChange, IntervalBasisCode.TimeOrDistance,
				TwelveMonthsInDays, TenThousandUnits, DistanceUnitCode.UnitA,
				true, false,
				Delivery, OdometerAtDelivery, DistanceUnitCode.UnitA);
		}

		// PRUEBA 13 de §9, la mitad que se puede: sobre una clase que agrego la persona.
		//
		// LAS CUENTAS DESPUES DE LA REMOCION NO SON DECORACION. La falsificacion encontro que
		// nadie contaba nada despues de quitar: con la clase quitada del diccionario pero no del
		// orden en que entraron, la siguiente lectura de CountDue reventaba con una clave que ya
		// no existia. Verde en 266 pruebas, y una excepcion esperando a la primera persona que
		// quitara un recordatorio suyo y mirara su agenda.
		[Test]
		public void AnOwnerAddedOperationIsTheOwnersToRemove()
		{
			AddTheOwnersDiagnostic();
			schedule.Evaluate(Delivery.AddDays(TwelveMonthsInDays));

			Assert.That(
				schedule.ReadDueReason(OperationKindCode.Diagnostic),
				Does.Not.Contain("no tiene agendada"),
				"Primero tiene que estar: si no, la remocion no prueba nada.");
			Assert.That(schedule.CountDue(), Is.EqualTo(2));

			schedule.RemoveOwnerOperation(OperationKindCode.Diagnostic);

			Assert.That(
				schedule.ReadDueReason(OperationKindCode.Diagnostic),
				Does.Contain("no tiene agendada"));
			Assert.That(schedule.CountDue(), Is.EqualTo(1),
				"Y la agenda sigue contestando: quitar una operacion la saca del conteo, no deja "
				+ "a la agenda apuntando a una clase que ya no tiene.");
			Assert.That(schedule.HasAnythingDue(), Is.True);
			Assert.That(schedule.MissedCount(), Is.EqualTo(0));
			Assert.That(schedule.ReadNextDueKind(), Is.EqualTo("FluidChange"));
		}

		// Quitar lo suyo TERMINA esa ocurrencia -- no la suelta. Es la otra mitad que ninguna de
		// las ocho lecturas de §8 ve (la instancia quitada deja de ser alcanzable), asi que se
		// mira donde si se ve: sobre la ocurrencia que la ley L5 devolvio al instalarla.
		[Test]
		public void RemovingTheOwnersOperationEndsThatOccurrence()
		{
			MaintenanceOperationDefinition definition = new MaintenanceOperationDefinition(
				MaintenanceOperationKind.Diagnostic,
				IntervalRule.Of(
					IntervalBasisCode.DistanceOnly, 0, TenThousandUnits, DistanceUnitCode.UnitA),
				false,
				false);

			ScheduledOperation theOwners = schedule.OneActiveOperationPerKind(
				OperationKindCode.Diagnostic,
				definition,
				BasePoint.At(Delivery, OdometerAtDelivery, DistanceUnit.UnitA),
				true);

			schedule.RemoveOwnerOperation(OperationKindCode.Diagnostic);

			Assert.That(theOwners.HasEnded(), Is.True);
			Assert.That(theOwners.State, Is.SameAs(DueState.Superseded),
				"OINV-31 la journaliza como OwnerOperationRemoved -> Superseded, terminal. Si se "
				+ "soltara sin terminarla, quedaria una ocurrencia viva fuera de toda agenda.");
		}

		// PRUEBA 13 de §9, la otra mitad: sobre una clase del plan, se rechaza.
		[Test]
		public void APlanOperationIsNotTheOwnersToRemove()
		{
			ReachTheLimit();

			schedule.RemoveOwnerOperation(OperationKindCode.FluidChange);

			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("Due"),
				"OINV-31: la operacion del fabricante sigue ahi, y sigue vencida. Si se hubiera "
				+ "ido, la persona habria borrado de su agenda algo que el plan manda y nadie "
				+ "volveria a avisarle.");
			Assert.That(schedule.CountDue(), Is.EqualTo(1));
		}

		[Test]
		public void ARefusedRemovalIsAStateAndNotAnException()
		{
			Assert.DoesNotThrow(
				() => schedule.RemoveOwnerOperation(OperationKindCode.FluidChange),
				"E32: la columna de rechazo de OINV-31 es un hecho del negocio "
				+ "(RemovalRefused / NotOwnerAdded), no un defecto de quien llama.");
		}

		// La otra prohibicion literal de OINV-31: la persona nunca AGREGA una operacion del plan.
		// Como L5 deja una sola activa por clase, agregar la suya encima de una del plan seria
		// re-fechar la del fabricante, que es la tercera cosa que la ley le prohibe.
		[Test]
		public void TheOwnerCannotDisplaceAPlanOperationWithTheirOwn()
		{
			ReachTheLimit();

			schedule.AddOwnerOperation(
				OperationKindCode.FluidChange, IntervalBasisCode.TimeOnly,
				30, 0, DistanceUnitCode.UnitA,
				TheLimitIsReached, 40000, DistanceUnitCode.UnitA);

			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("Due"),
				"La operacion del plan quedo intacta. Si la de la persona la hubiera "
				+ "sustituido, un limite de 30 dias puesto por ella habria reemplazado al del "
				+ "fabricante -- exactamente 're-fechar una operacion del plan'.");

			schedule.RemoveOwnerOperation(OperationKindCode.FluidChange);

			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("Due"),
				"Y sigue siendo del plan, asi que tampoco se puede quitar. Si el agregado la "
				+ "hubiera dejado entrar, la persona se habria quedado con el derecho de "
				+ "borrarla.");
		}

		// Lo suyo si lo puede reemplazar por lo suyo: es SU lista.
		[Test]
		public void TheOwnerCanReplaceTheirOwnOperationWithAnother()
		{
			AddTheOwnersDiagnostic();

			schedule.AddOwnerOperation(
				OperationKindCode.Diagnostic, IntervalBasisCode.TimeOnly,
				30, 0, DistanceUnitCode.UnitA,
				TheLimitIsReached, 40000, DistanceUnitCode.UnitA);

			schedule.Evaluate(TheLimitIsReached.AddDays(31));

			Assert.That(schedule.ReadDueState(OperationKindCode.Diagnostic), Is.EqualTo("Due"),
				"El limite que rige es el de la SEGUNDA: 30 dias desde 2026-01-20. Con el de la "
				+ "primera -- 365 dias desde la entrega -- todavia no venceria.");
			Assert.That(schedule.CountDue(), Is.EqualTo(1),
				"Y hay UNA sola, no dos: la ley L5 vale igual para las operaciones de la "
				+ "persona.");
		}

		// Una operacion que la persona pidio no viene clasificada de seguridad: la clasificacion
		// la pone el fabricante, y su firma ni siquiera la lleva.
		[Test]
		public void AnOwnerAddedOperationCanAlwaysBeDeferred()
		{
			AddTheOwnersDiagnostic();
			schedule.Evaluate(Delivery.AddDays(TwelveMonthsInDays));

			Assert.That(schedule.ReadDueState(OperationKindCode.Diagnostic), Is.EqualTo("Due"));

			schedule.DeferOperation(
				OperationKindCode.Diagnostic, DeferralReasonCode.Timing,
				Delivery.AddDays(TwelveMonthsInDays));

			Assert.That(
				schedule.ReadDueState(OperationKindCode.Diagnostic), Is.EqualTo("Deferred"),
				"Si la fachada de la persona pudiera marcar seguridad, esto seria un rechazo "
				+ "(OINV-52) y la persona se habria quitado a si misma el derecho a aplazar.");
		}

		[Test]
		public void RemovingAKindThatIsNotScheduledDoesNothing()
		{
			Assert.DoesNotThrow(
				() => schedule.RemoveOwnerOperation(OperationKindCode.RecallRemedy));

			Assert.That(
				schedule.ReadDueReason(OperationKindCode.RecallRemedy),
				Does.Contain("no tiene agendada"));
		}

		[Test]
		public void AnUnknownKindIsACallerBugOnBothOwnerSideCommands()
		{
			Assert.Throws<DomainException>(
				() => schedule.RemoveOwnerOperation((OperationKindCode)99));

			Assert.Throws<DomainException>(() => schedule.AddOwnerOperation(
				(OperationKindCode)99, IntervalBasisCode.TimeOnly,
				30, 0, DistanceUnitCode.UnitA,
				Delivery, OdometerAtDelivery, DistanceUnitCode.UnitA));
		}

		private void AddTheOwnersDiagnostic()
		{
			schedule.AddOwnerOperation(
				OperationKindCode.Diagnostic, IntervalBasisCode.TimeOnly,
				TwelveMonthsInDays, 0, DistanceUnitCode.UnitA,
				Delivery, OdometerAtDelivery, DistanceUnitCode.UnitA);
		}

		private void ReachTheLimit()
		{
			schedule.RecordReading(
				40000, DistanceUnitCode.UnitA, TheLimitIsReached, ReadingSourceCode.Telemetry);
			schedule.Evaluate(TheLimitIsReached);
		}
	}
}
