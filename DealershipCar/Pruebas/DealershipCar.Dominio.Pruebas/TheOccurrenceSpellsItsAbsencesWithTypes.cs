using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// E45 aplicado a lo que T4 trae. La ocurrencia tiene DOS mitades opcionales, y las dos son
	// tipos y no huecos:
	//
	//   Predecessor        de que ocurrencia terminal salio esta, o que es la primera.
	//   RecordedAssessment que dijo la ley la ultima vez, o que todavia no se evaluo.
	//
	// LA SEGUNDA ES LA QUE SE ESCRIBE MAL SOLA. "Todavia no se evaluo" se parece tanto a
	// "NotDue" que la tentacion es no distinguirlas -- y son cosas distintas: una es una
	// respuesta que la ley calculo, la otra es que nadie pregunto. Es la misma confusion del
	// GAP G2 corrida un piso: un NotDue que no es evidencia de nada tiene que DECIR que no lo
	// es.
	//
	// Y las dos preguntas totales de RecordedAssessment existen para que la ocurrencia NUNCA
	// tenga que preguntar si hay evaluacion antes de contestar. La ausencia contesta por si
	// misma; eso es lo que E45 llama darle comportamiento seguro a la ausencia, en vez de un
	// "!= null" repartido por cada llamador.
	[TestFixture]
	public sealed class TheOccurrenceSpellsItsAbsencesWithTypes
	{
		private const int TwelveMonthsInDays = 365;
		private const int TenThousandUnits = 10000;

		private static readonly DateTime LastCompletion = new DateTime(2025, 6, 1);

		// ===== LA CADENA HACIA ATRAS ======================================================

		[Test]
		public void TheGenesisAnswersThatThereIsNoEarlierOccurrence()
		{
			Assert.That(Predecessor.None.Exists(), Is.False);
		}

		[Test]
		public void ReadingTheGenesisPredecessorThrows()
		{
			Assert.Throws<DomainException>(() => Predecessor.None.Occurrence());
		}

		[Test]
		public void TheGenesisIsNotSpelledWithANull()
		{
			Assert.Throws<DomainException>(() => Predecessor.Of(null));
		}

		[Test]
		public void APresentPredecessorHandsBackTheSameOccurrence()
		{
			ScheduledOperation earlier = AnOperation();

			Predecessor link = Predecessor.Of(earlier);

			Assert.That(link.Exists(), Is.True);
			Assert.That(link.Occurrence(), Is.SameAs(earlier),
				"El par envuelve la ocurrencia, no la copia: la identidad es lo unico con lo que "
				+ "se comparan los objetos de este dominio (E23).");
		}

		// ===== LA ULTIMA RESPUESTA DE LA LEY ==============================================

		[Test]
		public void AnOperationNeverEvaluatedSaysSoInsteadOfClaimingAnAnswer()
		{
			string reason = RecordedAssessment.None.Describe();

			Assert.That(reason, Does.Contain("not been evaluated"));
			Assert.That(reason, Does.Contain("not evidence"),
				"Es el mismo cuidado del GAP G2 un piso mas arriba: un NotDue que nadie calculo "
				+ "no puede sonar igual que uno que la ley decidio.");
		}

		// LAS DOS PREGUNTAS SON TOTALES. Si revientaran, la ocurrencia tendria que preguntar
		// primero si hay evaluacion, y ese "if" es el != null que E45 saca del dominio.
		[Test]
		public void TheAbsentAssessmentAnswersBothQuestionsWithoutThrowing()
		{
			Assert.That(RecordedAssessment.None.SuspendsDistanceByAUnitMismatch(), Is.False);
			Assert.That(RecordedAssessment.None.SaysTheOperationIsOverdue(), Is.False);
		}

		[Test]
		public void TheAbsenceOfAnEvaluationIsNotSpelledWithANull()
		{
			Assert.Throws<DomainException>(() => RecordedAssessment.Of(null));
		}

		// ===== E45 REGLA 1: los colaboradores obligatorios se guardan ======================

		[Test]
		public void ADefinitionWithoutAKindIsRefused()
		{
			Assert.Throws<DomainException>(() => new MaintenanceOperationDefinition(
				null, ARule(), true, false));
		}

		// OINV-19: no hay operacion sin regla de cuando vence. La guarda es esa invariante, no
		// una comodidad defensiva.
		[Test]
		public void ADefinitionWithoutAnIntervalRuleIsRefused()
		{
			Assert.Throws<DomainException>(() => new MaintenanceOperationDefinition(
				MaintenanceOperationKind.FluidChange, null, true, false));
		}

		[Test]
		public void AnOccurrenceWithoutADefinitionIsRefused()
		{
			Assert.Throws<DomainException>(
				() => ScheduledOperation.FromTheManufacturerPlan(null, AnAnchor()));

			Assert.Throws<DomainException>(
				() => ScheduledOperation.RequestedByTheOwner(null, AnAnchor()));
		}

		// Un ancla AUSENTE no es lo mismo que un ancla SIN LECTURA. Lo primero es una operacion
		// que no tiene desde donde medir, y eso no existe. Lo segundo es legitimo y lo dice
		// AnchorReading.None: es la forma que deja una ley que descarta la lectura de
		// completacion -- TimeOnly, ConditionOnly (L4) -- porque guardarla haria creer que la
		// distancia cuenta para esa operacion.
		//
		// La siembra ya NO es un caso de esto: resuelto C18, SeedPlanOperation recibe el ancla
		// entera -- instante, lectura y unidad -- asi que una operacion recien sembrada si sabe
		// en que odometro empezo.
		[Test]
		public void AnOccurrenceWithoutABasePointIsRefused()
		{
			Assert.Throws<DomainException>(
				() => ScheduledOperation.FromTheManufacturerPlan(ADefinition(), null));

			Assert.DoesNotThrow(() => ScheduledOperation.FromTheManufacturerPlan(
				ADefinition(), BasePoint.AtInstantOnly(LastCompletion)),
				"Sin lectura si: hay leyes que no anclan ninguna, y su ancla es igual de valida.");
		}

		[Test]
		public void ADeferralWithoutAReasonIsRefused()
		{
			Assert.Throws<DomainException>(() => new Deferral(null, LastCompletion));
		}

		[Test]
		public void AReplacementWithoutADefinitionOrAnAnchorIsRefused()
		{
			Assert.Throws<DomainException>(
				() => AnOperation().SupersededBy(null, AnAnchor()));

			Assert.Throws<DomainException>(
				() => AnOperation().SupersededBy(ADefinition(), null));
		}

		// Y la sustitucion rechazada deja la operacion intacta: las guardas van ANTES de mover
		// nada (E12). Si no, una llamada mal hecha dejaria la agenda sin la operacion y sin
		// reemplazo -- desaparecida sin que nadie la haya quitado.
		[Test]
		public void ARefusedSupersessionLeavesTheOperationActive()
		{
			ScheduledOperation operation = AnOperation();

			Assert.Throws<DomainException>(() => operation.SupersededBy(null, AnAnchor()));

			Assert.That(operation.HasEnded(), Is.False);
			Assert.That(operation.State, Is.SameAs(DueState.NotDue));
		}

		private static ScheduledOperation AnOperation()
		{
			return ScheduledOperation.FromTheManufacturerPlan(ADefinition(), AnAnchor());
		}

		private static MaintenanceOperationDefinition ADefinition()
		{
			return new MaintenanceOperationDefinition(
				MaintenanceOperationKind.FluidChange, ARule(), true, false);
		}

		private static IntervalRule ARule()
		{
			return IntervalRule.Of(
				IntervalBasisCode.TimeOrDistance, TwelveMonthsInDays, TenThousandUnits,
				DistanceUnitCode.UnitA);
		}

		private static BasePoint AnAnchor()
		{
			return BasePoint.At(LastCompletion, 30000, DistanceUnit.UnitA);
		}
	}
}
