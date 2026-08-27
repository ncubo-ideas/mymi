using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L5, OneActiveOperationPerKind, sobre MaintenanceSchedule (§5 del slice, OINV-25):
	// "A lo sumo existe UNA operacion no terminal por clase. Una revision del plan SUSTITUYE;
	// nunca duplica."
	//
	// Son las pruebas 11 y 12 de §9, y son DOS LLAMADAS y no una -- esa es la resolucion de C5
	// confirmada con el usuario. AdoptPlanRevision ABRE la revision (deja su codigo y su
	// instante); la operacion entra despues, por SeedPlanOperation, que es el unico camino por
	// el que una operacion entra a la agenda. Ensanchar AdoptPlanRevision para que cargara la
	// operacion habria creado un SEGUNDO camino de siembra, que es lo que la regla de fuente
	// unica de E32 desaconseja por si sola.
	//
	// LO QUE SE PUEDE OBSERVAR Y LO QUE NO. §8 prohibe que cruce cualquier coleccion, asi que la
	// instancia sustituida no es alcanzable desde la fachada: la cadena PrecededBy vive adentro.
	// Que la vieja quede Superseded ya lo prueba T4 sobre la ocurrencia; aca se prueba la
	// CONSECUENCIA que si se ve -- que la clase tiene una sola operacion activa y que CountDue no
	// la cuenta dos veces.
	[TestFixture]
	public sealed class ARevisionSupersedesInsteadOfDuplicating
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TwelveMonthsInDays = 365;
		private const int TenThousandUnits = 10000;

		private static readonly DateTime Delivery = new DateTime(2025, 6, 1);
		private const int OdometerAtDelivery = 30000;

		private static readonly DateTime TheLimitIsReached = new DateTime(2026, 1, 20);
		private static readonly DateTime TheRevisionIsAdopted = new DateTime(2026, 3, 1);
		private const int OdometerAtTheRevision = 41000;

		private MaintenanceSchedule schedule;

		[SetUp]
		public void Setup()
		{
			schedule = new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);
			SeedAFluidChangeOf(TenThousandUnits, Delivery, OdometerAtDelivery);
		}

		// PRUEBA 11 de §9. La revision reemplaza una operacion de la misma clase: la nueva queda
		// NotDue y CountDue no cuenta dos veces.
		[Test]
		public void ARevisionsReplacementTakesTheKindsSingleActiveSlot()
		{
			ReachTheLimit();

			Assert.That(schedule.CountDue(), Is.EqualTo(1));
			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("Due"));

			schedule.AdoptPlanRevision("R2", TheRevisionIsAdopted);
			schedule.RecordReading(
				OdometerAtTheRevision, DistanceUnitCode.UnitA, TheRevisionIsAdopted,
				ReadingSourceCode.Inspection);
			SeedAFluidChangeOf(8000, TheRevisionIsAdopted, OdometerAtTheRevision);

			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("NotDue"),
				"El reemplazo es una ocurrencia NUEVA: nace NotDue, no hereda el vencimiento de "
				+ "la que sustituyo.");
			Assert.That(schedule.CountDue(), Is.EqualTo(0),
				"Y la sustituida ya no cuenta. Si la revision DUPLICARA en vez de sustituir, "
				+ "esto seria 1 -- la vieja seguiria vencida al lado de la nueva -- y la persona "
				+ "veria dos cambios de fluido donde hay uno.");

			schedule.Evaluate(TheRevisionIsAdopted.AddDays(1));

			Assert.That(schedule.CountDue(), Is.EqualTo(0));
			Assert.That(schedule.MissedCount(), Is.EqualTo(0));
		}

		// PRUEBA 12 de §9 / OEX-10. La sustituida estaba APLAZADA: el reemplazo empieza NotDue y
		// con la cuenta de aplazamientos en cero.
		[Test]
		public void AReplacementOfADeferredOperationStartsWithItsOwnEmptyCount()
		{
			ReachTheLimit();
			schedule.DeferOperation(
				OperationKindCode.FluidChange, DeferralReasonCode.Cost, TheLimitIsReached);

			Assert.That(
				schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("Deferred"));
			Assert.That(schedule.DeferralsUsed(OperationKindCode.FluidChange), Is.EqualTo(1));

			schedule.AdoptPlanRevision("R2", TheRevisionIsAdopted);
			schedule.RecordReading(
				OdometerAtTheRevision, DistanceUnitCode.UnitA, TheRevisionIsAdopted,
				ReadingSourceCode.Inspection);
			SeedAFluidChangeOf(8000, TheRevisionIsAdopted, OdometerAtTheRevision);

			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("NotDue"));
			Assert.That(schedule.DeferralsUsed(OperationKindCode.FluidChange), Is.EqualTo(0),
				"Y no porque alguien la haya reseteado: porque es otra ocurrencia, con su propia "
				+ "lista, que nace vacia (T4). Un reset que hay que acordarse de hacer es el que "
				+ "un dia no se hace.");
		}

		// El reemplazo se mide desde SU ancla. Es lo que obliga a que el ancla viaje con la
		// siembra y no con la creacion (C18): esta operacion se sembro nueve meses despues de la
		// entrega y no vence donde la entrega decia.
		[Test]
		public void TheReplacementIsMeasuredFromItsOwnAnchorAndNotFromTheOldOne()
		{
			ReachTheLimit();

			schedule.AdoptPlanRevision("R2", TheRevisionIsAdopted);
			schedule.RecordReading(
				OdometerAtTheRevision, DistanceUnitCode.UnitA, TheRevisionIsAdopted,
				ReadingSourceCode.Inspection);
			SeedAFluidChangeOf(TenThousandUnits, TheRevisionIsAdopted, OdometerAtTheRevision);

			schedule.RecordReading(
				51000, DistanceUnitCode.UnitA, TheRevisionIsAdopted.AddDays(30),
				ReadingSourceCode.Telemetry);
			schedule.Evaluate(TheRevisionIsAdopted.AddDays(30));

			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("Due"),
				"41000 del ancla mas 10000 son 51000. Si el reemplazo se hubiera quedado con el "
				+ "ancla vieja -- 30000 -- ya habria vencido en 40000 y naceria vencido.");
		}

		// L5 no depende de que haya una revision abierta: la ley es sobre la AGENDA, no sobre el
		// tramite. Sembrar dos veces la misma clase sustituye igual.
		[Test]
		public void ReSeedingAKindSupersedesEvenWithNoRevisionOpen()
		{
			ReachTheLimit();

			SeedAFluidChangeOf(8000, TheRevisionIsAdopted, OdometerAtTheRevision);

			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("NotDue"));
			Assert.That(schedule.CountDue(), Is.EqualTo(0));
		}

		[Test]
		public void APlanRevisionWithoutACodeCannotBeAdopted()
		{
			Assert.Throws<DomainException>(
				() => schedule.AdoptPlanRevision(null, TheRevisionIsAdopted));
			Assert.Throws<DomainException>(
				() => schedule.AdoptPlanRevision("  ", TheRevisionIsAdopted),
				"OINV-19: la revision es la procedencia del plan que la persona esta corriendo. "
				+ "Una sin codigo no se puede nombrar despues.");
		}

		// Las revisiones van hacia adelante, igual que las declaraciones de perfil (OINV-15). Una
		// revision fechada antes de la que rige seria re-fechar el pasado.
		[Test]
		public void ARevisionAdoptedBeforeTheOneInForceIsRefused()
		{
			schedule.AdoptPlanRevision("R2", TheRevisionIsAdopted);

			Assert.Throws<DomainException>(
				() => schedule.AdoptPlanRevision("R1", TheRevisionIsAdopted.AddDays(-1)));
		}

		// EL CRUCE DE ORIGENES, que ni §9 ni las diez leyes adjudican (se registra en
		// CONTRADICTIONS C20). Lectura adoptada: la cadena continua solo entre operaciones del
		// MISMO origen. El plan del fabricante desplaza al recordatorio de la persona -- termina
		// esa ocurrencia y entra como GENESIS suya -- porque el reemplazo hereda el origen de la
		// que sustituye (T4), y heredarlo cruzado dejaria a la persona quitando una operacion del
		// plan, que es justo lo que la ley L9 prohibe.
		[Test]
		public void ThePlanTakesOverTheKindWithoutBecomingTheOwners()
		{
			schedule.AddOwnerOperation(
				OperationKindCode.Diagnostic, IntervalBasisCode.TimeOnly,
				TwelveMonthsInDays, 0, DistanceUnitCode.UnitA,
				Delivery, OdometerAtDelivery, DistanceUnitCode.UnitA);

			schedule.SeedPlanOperation(
				OperationKindCode.Diagnostic, IntervalBasisCode.TimeOnly,
				180, 0, DistanceUnitCode.UnitA,
				true, false,
				TheRevisionIsAdopted, OdometerAtTheRevision, DistanceUnitCode.UnitA);

			schedule.RemoveOwnerOperation(OperationKindCode.Diagnostic);

			Assert.That(
				schedule.ReadDueReason(OperationKindCode.Diagnostic),
				Does.Not.Contain("no scheduled"),
				"La clase quedo en manos del plan, asi que la ley L9 la protege: la persona ya no "
				+ "la puede quitar. Si el reemplazo hubiera heredado el origen de la persona, "
				+ "esta remocion habria borrado una operacion del fabricante.");
		}

		// LA LEY L5 MIRADA DE CERCA, y esta prueba existe por lo que encontro la falsificacion: la
		// mitad OBSERVABLE de L5 -- una sola operacion activa por clase -- la da la ESTRUCTURA,
		// porque la agenda guarda las operaciones con la clase de clave. Cambiar la ley para que
		// sembrara una genesis nueva cada vez, sin terminar ni enlazar nada, dejo las 266 pruebas
		// en verde.
		//
		// Lo que la ley hace de verdad es TERMINAR la anterior y ENLAZAR la nueva, y eso no lo ve
		// ninguna de las ocho lecturas de §8: la cadena PrecededBy es el historial (GAP G3) y este
		// slice no proyecta historial. Asi que se prueba donde si se ve -- llamando a la ley por
		// su nombre, igual que T3 probo IntervalRule y T4 probo ScheduledOperation.
		[Test]
		public void TheSupersededInstanceIsEndedAndTheReplacementIsLinkedBackToIt()
		{
			ScheduledOperation first = InstallAFilterChangeOf(
				TenThousandUnits, Delivery, OdometerAtDelivery, false);

			ScheduledOperation replacement = InstallAFilterChangeOf(
				8000, TheRevisionIsAdopted, OdometerAtTheRevision, false);

			Assert.That(replacement, Is.Not.SameAs(first),
				"La reemplazante es OTRA ocurrencia: la misma re-anclada seria un campo que se "
				+ "resetea, y con el se iria el historial.");
			Assert.That(first.HasEnded(), Is.True);
			Assert.That(first.State, Is.SameAs(DueState.Superseded),
				"OINV-25: la revision SUSTITUYE. Si la vieja se soltara sin terminarla, quedaria "
				+ "una ocurrencia viva que ya no ocupa lugar en ninguna agenda.");
			Assert.That(replacement.HasAPredecessor(), Is.True);
			Assert.That(replacement.PrecededBy(), Is.SameAs(first),
				"Y queda enlazada hacia atras: esa cadena es lo unico que dice que esta "
				+ "operacion viene de otra.");
		}

		// El cruce de origenes, visto por dentro (C20): la ocurrencia de la persona TERMINA y la
		// del plan entra como genesis suya, sin enlazarse a algo que no es su historia.
		[Test]
		public void ThePlansTakeoverEndsTheOwnersOccurrenceAndStartsItsOwnChain()
		{
			ScheduledOperation theOwners = InstallAFilterChangeOf(
				TenThousandUnits, Delivery, OdometerAtDelivery, true);

			ScheduledOperation thePlans = InstallAFilterChangeOf(
				8000, TheRevisionIsAdopted, OdometerAtTheRevision, false);

			Assert.That(theOwners.State, Is.SameAs(DueState.Superseded));
			Assert.That(thePlans.WasRequestedByTheOwner(), Is.False,
				"Si heredara el origen de la que sustituyo, la persona se quedaria con el "
				+ "derecho de borrar una operacion del fabricante (ley L9).");
			Assert.That(thePlans.HasAPredecessor(), Is.False,
				"Y no se enlaza: la operacion del fabricante no es la continuacion del "
				+ "recordatorio de la persona, es otra cosa que llego a ocupar la clase.");
		}

		private ScheduledOperation InstallAFilterChangeOf(
			int distanceLimit, DateTime anchoredAt, int anchorReading, bool requestedByTheOwner)
		{
			MaintenanceOperationDefinition definition = new MaintenanceOperationDefinition(
				MaintenanceOperationKind.FilterChange,
				IntervalRule.Of(
					IntervalBasisCode.DistanceOnly, 0, distanceLimit, DistanceUnitCode.UnitA),
				true,
				false);

			return schedule.OneActiveOperationPerKind(
				OperationKindCode.FilterChange,
				definition,
				BasePoint.At(anchoredAt, anchorReading, DistanceUnit.UnitA),
				requestedByTheOwner);
		}

		private void SeedAFluidChangeOf(int distanceLimit, DateTime anchoredAt, int anchorReading)
		{
			schedule.SeedPlanOperation(
				OperationKindCode.FluidChange, IntervalBasisCode.TimeOrDistance,
				TwelveMonthsInDays, distanceLimit, DistanceUnitCode.UnitA,
				true, false,
				anchoredAt, anchorReading, DistanceUnitCode.UnitA);
		}

		private void ReachTheLimit()
		{
			schedule.RecordReading(
				40000, DistanceUnitCode.UnitA, TheLimitIsReached, ReadingSourceCode.Telemetry);
			schedule.Evaluate(TheLimitIsReached);
		}
	}
}
