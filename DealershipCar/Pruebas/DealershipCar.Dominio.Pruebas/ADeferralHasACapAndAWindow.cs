using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L6, DeferralCapAndWindow, sobre ScheduledOperation (§5 del slice, OINV-27, OINV-52):
	// "Una operacion se puede aplazar como maximo MaxDeferralsPerOperation veces, cada una por
	// como maximo DeferralWindow. Una operacion clasificada de seguridad no se aplaza nunca."
	//
	// Son las pruebas 8, 9 y 10 de §9.
	//
	// LO QUE DE VERDAD SE PRUEBA ACA es la POLARIDAD de E32, y es la que se equivoca sola. Un
	// aplazamiento rechazado NO es una excepcion: la persona pidio algo que la politica no da,
	// y eso es un RESULTADO ESPERADO del negocio. Se dice con el estado y con la cuenta, no
	// reventando. Lo unico que revienta en este archivo es un motivo en null, que si es un
	// defecto de quien llama.
	//
	// La otra mitad es E35: DeferralsUsed es DERIVADO de los aplazamientos que de verdad se
	// admitieron. Un contador aparte se desincroniza el dia que alguien sume antes de mirar el
	// tope, y nadie lo nota, porque un numero equivocado sigue pareciendo un numero.
	//
	// EL ESCENARIO ES DistanceOnly A PROPOSITO. Una ley que mide tiempo no puede quedarse
	// quieta mientras corre una ventana de 60 dias: al vencerla ya consumio la gracia de 14 y
	// contesta Overdue. Con distancia y sin lecturas nuevas, la ley contesta lo mismo antes y
	// despues de la ventana, que es lo que la prueba 8 quiere mirar -- que la operacion SALE
	// del aplazamiento -- sin mezclarlo con el paso del tiempo.
	[TestFixture]
	public sealed class ADeferralHasACapAndAWindow
	{
		private const int TenThousandUnits = 10000;

		private static readonly DateTime LastCompletion = new DateTime(2025, 6, 1);
		private const int ReadingAtTheLastCompletion = 30000;

		// 2026-01-20 + 60 dias de ventana.
		private static readonly DateTime FellDue = new DateTime(2026, 1, 20);
		private static readonly DateTime TheWindowEnds = new DateTime(2026, 3, 21);

		private ScheduledOperation operation;
		private IntervalLengthFactor noScaling;

		[SetUp]
		public void Setup()
		{
			noScaling = IntervalLengthFactor.For(UsageProfile.Standard);
			operation = AnOperationOf(MaintenanceOperationKind.WearPartReplacement, false);
		}

		// PRUEBA 8 de §9. Aplazar una vez -> Deferred, DeferralsUsed = 1. Cumplida la ventana
		// -> Due otra vez, y la cuenta se conserva.
		[Test]
		public void DeferringOnceHoldsTheOperationUntilTheWindowEnds()
		{
			EvaluateAt(FellDue);

			Assert.That(operation.State, Is.SameAs(DueState.Due),
				"El escenario tiene que llegar vencido al aplazamiento: si no, no hay nada que "
				+ "aplazar y la prueba estaria mirando el no-op de E36 sin darse cuenta.");

			operation.Defer(DeferralReason.Cost, FellDue);

			Assert.That(operation.State, Is.SameAs(DueState.Deferred));
			Assert.That(operation.DeferralsUsed, Is.EqualTo(1));
		}

		[Test]
		public void InsideTheWindowTheOperationStaysDeferred()
		{
			EvaluateAt(FellDue);
			operation.Defer(DeferralReason.Cost, FellDue);

			DueStateTransition transition = EvaluateAt(TheWindowEnds.AddDays(-1));

			Assert.That(operation.State, Is.SameAs(DueState.Deferred),
				"Un dia antes del fin de la ventana la operacion sigue aplazada. Si vuelve a "
				+ "Due aca, la ventana no se esta mirando.");
			Assert.That(transition.Happened, Is.False,
				"Evaluar adentro de la ventana no mueve nada, asi que no hay transicion que "
				+ "contar ni entrada que journalizar.");
		}

		// La ventana es INCLUSIVA, con la misma regla que los otros tres umbrales del slice:
		// alcanzar un umbral es entrar en su estado (C16). El dia 60 es el que la cumple.
		[Test]
		public void OnceTheWindowIsSpentTheOperationIsDueAgainAndTheCountIsRetained()
		{
			EvaluateAt(FellDue);
			operation.Defer(DeferralReason.Cost, FellDue);

			DueStateTransition transition = EvaluateAt(TheWindowEnds);

			Assert.That(operation.State, Is.SameAs(DueState.Due),
				"Cumplida la ventana la ley vuelve a decidir, y esta ley sigue diciendo Due: "
				+ "10000 unidades recorridas de un limite de 10000, sin lecturas nuevas.");
			Assert.That(operation.DeferralsUsed, Is.EqualTo(1),
				"LA CUENTA SE CONSERVA. Si se reseteara al salir de la ventana, el tope de dos "
				+ "no seria un tope: serian dos por ventana, para siempre.");
			Assert.That(transition.Happened, Is.True);
			Assert.That(transition.BecameDue, Is.True);
		}

		// PRUEBA 9 de §9. Dos aplazamientos, y el tercero se rechaza.
		[Test]
		public void TheThirdDeferralIsRefusedAndTheCountStaysAtTheCap()
		{
			EvaluateAt(FellDue);
			operation.Defer(DeferralReason.Cost, FellDue);

			EvaluateAt(TheWindowEnds);
			operation.Defer(DeferralReason.Timing, TheWindowEnds);

			Assert.That(operation.DeferralsUsed, Is.EqualTo(2));
			Assert.That(operation.State, Is.SameAs(DueState.Deferred));

			DateTime theSecondWindowEnds = TheWindowEnds.AddDays(
				SchedulePolicy.DeferralWindowDays);

			EvaluateAt(theSecondWindowEnds);

			Assert.That(operation.State, Is.SameAs(DueState.Due));

			operation.Defer(DeferralReason.SecondOpinion, theSecondWindowEnds);

			Assert.That(operation.State, Is.SameAs(DueState.Due),
				"En el tope el aplazamiento se RECHAZA, y rechazado quiere decir que la "
				+ "operacion se queda donde estaba: vencida. Si quedara Deferred, el tercer "
				+ "aplazamiento habria pasado sin contarse.");
			Assert.That(operation.DeferralsUsed, Is.EqualTo(2),
				"Ni sube a 3 ni se resetea. DeferralsUsed cuenta los admitidos (E35).");
		}

		[Test]
		public void ADeferralRefusedAtTheCapIsAStateAndNotAnException()
		{
			EvaluateAt(FellDue);
			operation.Defer(DeferralReason.Cost, FellDue);
			EvaluateAt(TheWindowEnds);
			operation.Defer(DeferralReason.Timing, TheWindowEnds);

			DateTime theSecondWindowEnds = TheWindowEnds.AddDays(
				SchedulePolicy.DeferralWindowDays);
			EvaluateAt(theSecondWindowEnds);

			Assert.DoesNotThrow(
				() => operation.Defer(DeferralReason.Cost, theSecondWindowEnds),
				"E32: llegar al tope es un RESULTADO ESPERADO del negocio -- la politica da dos "
				+ "y la persona pidio tres. Un throw obligaria a la app a usar excepciones para "
				+ "el flujo normal de alguien que simplemente no puede ahora.");
		}

		// PRUEBA 10 de §9. Una operacion de seguridad se rechaza en el PRIMER intento, y con
		// el tope todavia entero: no es que se le gasten los dos, es que no tiene ninguno.
		[Test]
		public void ASafetyClassifiedOperationRefusesTheFirstDeferral()
		{
			ScheduledOperation safety = AnOperationOf(MaintenanceOperationKind.RecallRemedy, true);

			safety.Evaluate(FellDue, AReadingOf(40000, DistanceUnit.UnitA), noScaling);

			Assert.That(safety.State, Is.SameAs(DueState.Due));

			safety.Defer(DeferralReason.Cost, FellDue);

			Assert.That(safety.State, Is.SameAs(DueState.Due),
				"OINV-52: una operacion clasificada de seguridad no se aplaza NUNCA. No es un "
				+ "tope mas bajo: es que la puerta no existe.");
			Assert.That(safety.DeferralsUsed, Is.EqualTo(0),
				"Y el intento rechazado no gasta un aplazamiento. Si contara, la clasificacion "
				+ "de seguridad se leeria como 'cero de dos usados' en vez de 'ninguno'.");
		}

		// E36: aplazar fuera de un estado vencido es un NO-OP, no un error. La persona toca
		// 'mas tarde' en una operacion que todavia no vence y no pasa nada; nada que reportar,
		// nada que journalizar.
		[Test]
		public void DeferringAnOperationThatIsNotDueDoesNothing()
		{
			EvaluateAt(new DateTime(2025, 12, 1), 31000);

			Assert.That(operation.State, Is.SameAs(DueState.NotDue));

			Assert.DoesNotThrow(() => operation.Defer(DeferralReason.Cost, new DateTime(2025, 12, 1)));

			Assert.That(operation.State, Is.SameAs(DueState.NotDue));
			Assert.That(operation.DeferralsUsed, Is.EqualTo(0),
				"Un aplazamiento que no se admitio no se cuenta. Si contara, la persona "
				+ "gastaria sus dos oportunidades en operaciones que ni siquiera vencian.");
		}

		// Aplazar una operacion YA aplazada tampoco gasta nada: Deferred no es un estado
		// vencido, es la respuesta a haber aplazado. Sin esto, tocar dos veces el mismo boton
		// consumiria el tope entero.
		[Test]
		public void DeferringAnAlreadyDeferredOperationDoesNotSpendAnother()
		{
			EvaluateAt(FellDue);
			operation.Defer(DeferralReason.Cost, FellDue);

			operation.Defer(DeferralReason.Timing, FellDue.AddDays(1));

			Assert.That(operation.DeferralsUsed, Is.EqualTo(1));
			Assert.That(operation.State, Is.SameAs(DueState.Deferred));
		}

		// OINV-28: aplazar es la decision de la persona y NO saca la operacion de la cuenta de
		// mantenimiento no hecho. Aca es donde se ve que el aplazamiento no para el reloj: la
		// ley la sigue evaluando por debajo, y cuando dice Overdue la operacion cuenta como
		// perdida aunque la persona la haya aplazado en regla.
		[Test]
		public void ADeferredOperationKeepsCountingAsMissedWhenTheLawSaysItIsOverdue()
		{
			EvaluateAt(FellDue);
			operation.Defer(DeferralReason.Cost, FellDue);

			Assert.That(operation.CountsAsMissedMaintenance(), Is.False,
				"Vencida y aplazada dentro de la gracia todavia no es mantenimiento perdido.");

			EvaluateAt(FellDue.AddDays(10), 40500);

			Assert.That(operation.State, Is.SameAs(DueState.Deferred),
				"Sigue aplazada: la ventana no se cumplio.");
			Assert.That(operation.CountsAsMissedMaintenance(), Is.True,
				"OINV-28: el aplazamiento no la saca de la cuenta. 500 unidades pasadas del "
				+ "limite son la gracia entera, y que la persona lo haya pospuesto en regla no "
				+ "cambia que el mantenimiento no se hizo.");
		}

		// La ventana se mide desde el instante de LA DECISION recibida, no desde ningun reloj
		// que el dominio lea (§7).
		[Test]
		public void TheWindowIsMeasuredFromTheReceivedDecisionInstant()
		{
			EvaluateAt(FellDue);
			operation.Defer(DeferralReason.Cost, FellDue.AddDays(30));

			EvaluateAt(TheWindowEnds);

			Assert.That(operation.State, Is.SameAs(DueState.Deferred),
				"La decision se tomo 30 dias despues, asi que su ventana termina 30 dias "
				+ "despues. Si la operacion sale aca, la ventana se esta contando desde el "
				+ "vencimiento y no desde la decision.");

			EvaluateAt(TheWindowEnds.AddDays(30));

			Assert.That(operation.State, Is.SameAs(DueState.Due));
		}

		// Un motivo en null si es un defecto de quien llama: el aplazamiento se journaliza CON
		// su motivo (OINV-28), asi que uno sin motivo no es un aplazamiento incompleto, es un
		// hecho que no se puede registrar.
		[Test]
		public void ADeferralWithoutAReasonThrows()
		{
			EvaluateAt(FellDue);

			Assert.Throws<DomainException>(() => operation.Defer(null, FellDue));
		}

		private ScheduledOperation AnOperationOf(
			MaintenanceOperationKind kind, bool isSafetyClassified)
		{
			MaintenanceOperationDefinition definition = new MaintenanceOperationDefinition(
				kind,
				IntervalRule.Of(
					IntervalBasisCode.DistanceOnly, 0, TenThousandUnits, DistanceUnitCode.UnitA),
				true,
				isSafetyClassified);

			return ScheduledOperation.FromTheManufacturerPlan(
				definition,
				BasePoint.At(LastCompletion, ReadingAtTheLastCompletion, DistanceUnit.UnitA));
		}

		private DueStateTransition EvaluateAt(DateTime asOf)
		{
			return EvaluateAt(asOf, 40000);
		}

		private DueStateTransition EvaluateAt(DateTime asOf, int reading)
		{
			return operation.Evaluate(asOf, AReadingOf(reading, DistanceUnit.UnitA), noScaling);
		}

		private static UsableReading AReadingOf(int value, DistanceUnit unit)
		{
			return UsableReading.Of(new OdometerReading(
				value, unit, new DateTime(2026, 1, 1), ReadingSource.Telemetry));
		}
	}
}
