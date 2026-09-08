using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// E29 (una transicion terminal SUELTA la identidad del objeto para que no siga actuando) y
	// el GAP G3 (una ocurrencia terminal engendrando a su sucesora), sobre ScheduledOperation.
	//
	// Completed y Superseded terminan la instancia. La siguiente ocurrencia es OTRO objeto,
	// enlazado hacia atras por PrecededBy, y esa cadena ES el historial.
	//
	// POR QUE LA CADENA Y NO UN CAMPO QUE SE RESETEA (respuesta adoptada del GAP G3). La
	// alternativa obvia es dejar la misma instancia y limpiarle el estado -- re-anclarla,
	// poner NotDue, vaciar los aplazamientos. Funciona, y borra el historial mientras
	// funciona: no queda nada que diga que esta operacion se aplazo dos veces antes de
	// hacerse. Con la cadena, lo viejo sigue existiendo con su estado terminal intacto y lo
	// nuevo nace limpio POR SER NUEVO, no porque alguien lo limpio.
	//
	// OEX-10 sale gratis de esa forma: "el reemplazo empieza en NotDue con la cuenta de
	// aplazamientos RESETEADA". No hay reset -- hay una ocurrencia nueva con su propia lista
	// vacia. Un reset que hay que acordarse de hacer es el que un dia no se hace.
	[TestFixture]
	public sealed class AnEndedOccurrenceDoesNotKeepActing
	{
		private const int TwelveMonthsInDays = 365;
		private const int TenThousandUnits = 10000;

		private static readonly DateTime LastCompletion = new DateTime(2025, 6, 1);
		private const int ReadingAtTheLastCompletion = 30000;

		// Prueba 5 de §9: vencida en 40000, completada el 2026-02-01 a 39800.
		private static readonly DateTime CompletedOn = new DateTime(2026, 2, 1);
		private const int ReadingAtTheActualCompletion = 39800;

		private ScheduledOperation operation;
		private IntervalLengthFactor noScaling;

		[SetUp]
		public void Setup()
		{
			noScaling = IntervalLengthFactor.For(UsageProfile.Standard);
			operation = APlanOperation();
		}

		[Test]
		public void AGenesisOccurrenceHasNoPredecessor()
		{
			Assert.That(operation.HasAPredecessor(), Is.False);
			Assert.Throws<DomainException>(() => operation.PrecededBy(),
				"E41: leer una opcional ausente es un defecto de quien llama, no una respuesta "
				+ "vacia. Devolver null aca haria que la cadena termine en un hueco en vez de "
				+ "en un hecho.");
		}

		[Test]
		public void CompletingEndsTheInstanceAndYieldsItsSuccessor()
		{
			ScheduledOperation next = operation.Complete(
				CompletedOn, ReadingAtTheActualCompletion, DistanceUnit.UnitA);

			Assert.That(operation.State, Is.SameAs(DueState.Completed));
			Assert.That(operation.HasEnded(), Is.True);

			Assert.That(next, Is.Not.SameAs(operation),
				"La sucesora es OTRA ocurrencia. La misma instancia re-anclada seria un campo "
				+ "que se resetea, y con el se iria el historial.");
			Assert.That(next.State, Is.SameAs(DueState.NotDue));
			Assert.That(next.HasAPredecessor(), Is.True);
			Assert.That(next.PrecededBy(), Is.SameAs(operation));
		}

		// LEY L4 vista desde la cadena: la sucesora se mide desde la completacion REAL. Es la
		// prueba 5 de §9, y el numero que la hace falsificable es 49800 y no 50000.
		[Test]
		public void TheSuccessorIsMeasuredFromTheActualCompletionAndNotFromTheGrid()
		{
			ScheduledOperation next = operation.Complete(
				CompletedOn, ReadingAtTheActualCompletion, DistanceUnit.UnitA);

			next.Evaluate(
				CompletedOn.AddDays(60), AReadingOf(49800, DistanceUnit.UnitA), noScaling);

			Assert.That(next.State, Is.SameAs(DueState.Due),
				"49800 son exactamente 10000 desde la completacion real de 39800. Si la "
				+ "sucesora se hubiera anclado en la grilla teorica -- 40000 -- esto seria "
				+ "DueSoon y la operacion venceria 200 unidades tarde, para siempre.");
		}

		// La identidad SOLTADA: la instancia terminal ya no actua. No revienta -- no es un
		// defecto preguntarle -- simplemente no se mueve.
		[Test]
		public void ACompletedInstanceNoLongerTransitions()
		{
			operation.Complete(CompletedOn, ReadingAtTheActualCompletion, DistanceUnit.UnitA);

			DueStateTransition transition = operation.Evaluate(
				new DateTime(2030, 1, 1), AReadingOf(90000, DistanceUnit.UnitA), noScaling);

			Assert.That(transition.Happened, Is.False);
			Assert.That(operation.State, Is.SameAs(DueState.Completed),
				"Cinco anos despues sigue Completed. Si volviera a vencer, la operacion hecha "
				+ "estaria compitiendo con su propia sucesora por el mismo lugar.");
		}

		[Test]
		public void ACompletedInstanceCannotBeDeferred()
		{
			operation.Complete(CompletedOn, ReadingAtTheActualCompletion, DistanceUnit.UnitA);

			Assert.DoesNotThrow(() => operation.Defer(DeferralReason.Cost, CompletedOn));

			Assert.That(operation.State, Is.SameAs(DueState.Completed));
			Assert.That(operation.DeferralsUsed, Is.EqualTo(0));
		}

		// Completar dos veces SI revienta, y la asimetria con el aplazamiento es deliberada
		// (E32). Aplazar de mas es la persona pidiendo algo que la politica no da: un
		// resultado esperado. Completar una operacion que ya termino no es que nadie pueda
		// pedirlo -- es que produciria una SEGUNDA sucesora y bifurcaria la cadena, que es el
		// unico artefacto donde vive el historial.
		[Test]
		public void CompletingAnEndedInstanceThrows()
		{
			operation.Complete(CompletedOn, ReadingAtTheActualCompletion, DistanceUnit.UnitA);

			Assert.Throws<DomainException>(() => operation.Complete(
				CompletedOn.AddDays(1), 39900, DistanceUnit.UnitA));
		}

		[Test]
		public void SupersedingAnEndedInstanceThrows()
		{
			operation.Complete(CompletedOn, ReadingAtTheActualCompletion, DistanceUnit.UnitA);

			Assert.Throws<DomainException>(() => operation.Supersede());
		}

		[Test]
		public void SupersedingEndsTheInstanceWithoutASuccessor()
		{
			operation.Supersede();

			Assert.That(operation.State, Is.SameAs(DueState.Superseded));
			Assert.That(operation.HasEnded(), Is.True);
		}

		// PRUEBA 12 de §9 / OEX-10, la mitad de la ocurrencia: una revision del plan sustituye
		// a una operacion APLAZADA, y el reemplazo empieza NotDue con la cuenta en cero.
		[Test]
		public void AReplacementStartsCleanBecauseItIsNewAndNotBecauseSomebodyResetIt()
		{
			operation.Evaluate(
				new DateTime(2026, 1, 20), AReadingOf(40000, DistanceUnit.UnitA), noScaling);
			operation.Defer(DeferralReason.Cost, new DateTime(2026, 1, 20));

			Assert.That(operation.State, Is.SameAs(DueState.Deferred));
			Assert.That(operation.DeferralsUsed, Is.EqualTo(1));

			ScheduledOperation replacement = operation.SupersededBy(
				ADefinitionOf(TwelveMonthsInDays, 8000, false),
				BasePoint.At(new DateTime(2026, 3, 1), 41000, DistanceUnit.UnitA));

			Assert.That(operation.State, Is.SameAs(DueState.Superseded));
			Assert.That(replacement.State, Is.SameAs(DueState.NotDue));
			Assert.That(replacement.DeferralsUsed, Is.EqualTo(0),
				"OEX-10. Y no porque alguien lo haya reseteado: porque es una ocurrencia nueva "
				+ "con su propia lista, que nace vacia.");
			Assert.That(replacement.PrecededBy(), Is.SameAs(operation));
		}

		// LA CADENA ES EL HISTORIAL. Lo viejo conserva SU cuenta: si el reemplazo la reseteara
		// mutando la misma instancia, este numero seria 0 y no habria donde leer que la
		// persona ya habia aplazado esta operacion una vez.
		[Test]
		public void TheSupersededInstanceKeepsItsOwnHistory()
		{
			operation.Evaluate(
				new DateTime(2026, 1, 20), AReadingOf(40000, DistanceUnit.UnitA), noScaling);
			operation.Defer(DeferralReason.Cost, new DateTime(2026, 1, 20));

			ScheduledOperation replacement = operation.SupersededBy(
				ADefinitionOf(TwelveMonthsInDays, 8000, false),
				BasePoint.At(new DateTime(2026, 3, 1), 41000, DistanceUnit.UnitA));

			Assert.That(replacement.PrecededBy().DeferralsUsed, Is.EqualTo(1));
			Assert.That(replacement.PrecededBy().State, Is.SameAs(DueState.Superseded));
		}

		// El origen viaja por la cadena. Sin esto, una operacion que la persona pidio se
		// volveria del plan del fabricante en cuanto se hace una vez -- y a partir de ahi la
		// ley L9 no la dejaria quitarla nunca mas.
		[Test]
		public void TheSuccessorKeepsTheOriginOfTheOccurrenceItCameFrom()
		{
			ScheduledOperation owners = ScheduledOperation.RequestedByTheOwner(
				ADefinitionOf(TwelveMonthsInDays, TenThousandUnits, false),
				BasePoint.At(LastCompletion, ReadingAtTheLastCompletion, DistanceUnit.UnitA));

			Assert.That(owners.WasRequestedByTheOwner(), Is.True);
			Assert.That(operation.WasRequestedByTheOwner(), Is.False);

			ScheduledOperation next = owners.Complete(
				CompletedOn, ReadingAtTheActualCompletion, DistanceUnit.UnitA);

			Assert.That(next.WasRequestedByTheOwner(), Is.True);
		}

		// Los hechos de la completacion se validan ANTES de mover nada: una completacion
		// rechazada deja la operacion exactamente como estaba (E12, mutar al final).
		[Test]
		public void ARefusedCompletionLeavesTheOperationUntouched()
		{
			Assert.Throws<DomainException>(
				() => operation.Complete(CompletedOn, -1, DistanceUnit.UnitA));

			Assert.That(operation.State, Is.SameAs(DueState.NotDue),
				"Si quedara Completed despues de un rechazo, la operacion estaria terminada sin "
				+ "sucesora: desaparecida de la agenda sin que nadie la haya hecho.");
			Assert.That(operation.HasEnded(), Is.False);
		}

		// LA GUARDA DE LA LECTURA ES DE LA OCURRENCIA, y esta prueba es lo unico que lo
		// distingue. Con TimeOrDistance el defecto se tapa solo: la lectura llega hasta
		// AnchorReading, que rechaza los negativos, asi que quitarle la guarda a Complete no
		// rompe nada visible. Con una ley que NO ancla lectura -- TimeOnly, ConditionOnly --
		// la completacion la descarta, y un numero imposible entraria al journal sin que nadie
		// lo mirara nunca.
		//
		// Se encontro falsificando: quitar la guarda dejo las 196 pruebas en verde.
		[Test]
		public void ANegativeCompletionReadingIsRefusedEvenWhenTheLawWouldDiscardIt()
		{
			ScheduledOperation measuredOnlyByTime = ScheduledOperation.FromTheManufacturerPlan(
				new MaintenanceOperationDefinition(
					MaintenanceOperationKind.TimedComponent,
					IntervalRule.Of(
						IntervalBasisCode.TimeOnly, TwelveMonthsInDays, 0, DistanceUnitCode.UnitA),
					true,
					false),
				BasePoint.AtInstantOnly(LastCompletion));

			Assert.Throws<DomainException>(
				() => measuredOnlyByTime.Complete(CompletedOn, -1, DistanceUnit.UnitA));
		}

		[Test]
		public void ACompletionBeforeTheBasePointThrows()
		{
			Assert.Throws<DomainException>(() => operation.Complete(
				LastCompletion.AddDays(-1), 29000, DistanceUnit.UnitA));
		}

		private ScheduledOperation APlanOperation()
		{
			return ScheduledOperation.FromTheManufacturerPlan(
				ADefinitionOf(TwelveMonthsInDays, TenThousandUnits, false),
				BasePoint.At(LastCompletion, ReadingAtTheLastCompletion, DistanceUnit.UnitA));
		}

		private static MaintenanceOperationDefinition ADefinitionOf(
			int timeLimitDays, int distanceLimit, bool isSafetyClassified)
		{
			return new MaintenanceOperationDefinition(
				MaintenanceOperationKind.FluidChange,
				IntervalRule.Of(
					IntervalBasisCode.TimeOrDistance, timeLimitDays, distanceLimit,
					DistanceUnitCode.UnitA),
				true,
				isSafetyClassified);
		}

		private static UsableReading AReadingOf(int value, DistanceUnit unit)
		{
			return UsableReading.Of(new OdometerReading(
				value, unit, new DateTime(2026, 1, 1), ReadingSource.Telemetry));
		}
	}
}
