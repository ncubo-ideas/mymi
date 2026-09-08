using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// E45, regla 2: un singleton opcional es un PAR DE OBJETO NULO, no un campo anulable.
	//
	// LA LECTURA USABLE ES EL CASO OPCIONAL DE ESTE SLICE. Un vehiculo recien entregado no tiene
	// ninguna lectura todavia, y eso es un hecho legitimo del negocio -- no un defecto. Hasta T3
	// ese hecho viajaba hacia adentro de Assess como un null literal.
	//
	// LO QUE CAMBIA ES LA REPRESENTACION Y NADA MAS. La semantica que T3 fijo se queda entera:
	// pregunta de presencia TOTAL, accesor GUARDADO que revienta (E41). Lo que se mueve es quien
	// la sostiene -- el TIPO, en vez de un "!= null" repartido por cada llamador.
	//
	// POR QUE MUERDE AQUI Y NO EN ABSTRACTO. Un null que entra por Assess no se queda en Assess:
	// T4 y T5 traen dos llamadores mas -- ScheduledOperation y MaintenanceSchedule -- y cada uno
	// tendria que acordarse de que ese parametro puede venir vacio. Nadie se acuerda para
	// siempre. Un UsableReading.None no se puede olvidar: sin el no compila.
	[TestFixture]
	public sealed class AnAbsentReadingIsATypeAndNotANull
	{
		private const int TwelveMonthsInDays = 365;
		private const int TenThousandUnits = 10000;

		private BasePoint from;
		private IntervalLengthFactor unscaled;

		[SetUp]
		public void Setup()
		{
			from = BasePoint.At(new DateTime(2025, 6, 1), 30000, DistanceUnit.UnitA);
			unscaled = IntervalLengthFactor.For(UsageProfile.Standard);
		}

		// La pregunta de presencia es TOTAL: contesta siempre, tambien cuando no hay nada.
		[Test]
		public void TheAbsenceAnswersThatThereIsNoReading()
		{
			Assert.That(UsableReading.None.Exists(), Is.False);
		}

		// El accesor es GUARDADO (E41). Devolver una lectura en cero seria el mismo defecto que
		// BasePoint.Reading evita del otro lado: un odometro que nadie leyo apareceria recorrido.
		[Test]
		public void ReadingTheAbsenceThrows()
		{
			Assert.Throws<DomainException>(() => UsableReading.None.Reading());
		}

		[Test]
		public void APresentReadingAnswersAndHandsBackTheSameInstance()
		{
			OdometerReading taken = Reading(45000, DistanceUnit.UnitA);

			UsableReading usable = UsableReading.Of(taken);

			Assert.That(usable.Exists(), Is.True);
			Assert.That(usable.Reading(), Is.SameAs(taken),
				"El par envuelve la lectura, no la copia: la identidad es lo unico con lo que se "
				+ "comparan los objetos de este dominio (E23).");
		}

		// NO SE ESCRIBE AQUI que la ausencia es una sola instancia compartida. Leer dos veces un
		// static readonly da la misma referencia SIEMPRE, asi que la prueba pasaria escrito lo
		// que se escriba en el dominio: es la tautologia que E17 manda no afirmar, no una ley.
		// Que None sea compartida se lee de su declaracion, y ninguna prueba la puede defender.

		// E45, regla 1. Un null NO es como se dice "no hay": para eso esta None, que contesta.
		// Un hueco no contesta nada, y aceptarlo aqui devolveria por la puerta de atras
		// exactamente lo que este tipo existe para sacar del dominio.
		[Test]
		public void TheAbsenceIsNotSpelledWithANull()
		{
			Assert.Throws<DomainException>(() => UsableReading.Of(null));
		}

		// LA PRUEBA QUE SOSTIENE TODO LO DEMAS. Si la regla aceptara un null en el lugar de la
		// ausencia, el par de objeto nulo seria decorativo: el null seguiria viajando hacia
		// adentro y T4 y T5 lo heredarian igual.
		[Test]
		public void ARuleRefusesANullWhereTheAbsenceBelongs()
		{
			Assert.Throws<DomainException>(() => TheRuleOfBoth().Assess(
				from, new DateTime(2026, 1, 20), null, unscaled));
		}

		// LA SEMANTICA NO SE MOVIO, y esta prueba es lo que lo afirma en vez de suponerlo: la
		// ausencia dicha con un tipo suspende la distancia por el mismo motivo, con el mismo
		// estado y con el mismo motivo escrito que la dicha con un null (GAP G2).
		[Test]
		public void TheAbsenceSuspendsTheDistanceExactlyAsTheNullUsedTo()
		{
			DueAssessment assessment = TheRuleOfBoth().Assess(
				from, new DateTime(2026, 1, 20), UsableReading.None, unscaled);

			Assert.That(assessment.DistanceIsSuspended, Is.True);
			Assert.That(assessment.SuspendedByAnAbsentReading, Is.True);
			Assert.That(assessment.SuspendedByUnitMismatch, Is.False);
			Assert.That(assessment.Describe(), Does.Contain("ninguna lectura usable"));
		}

		// ===== LA OTRA MITAD OPCIONAL DEL SLICE: la del ancla =============================
		//
		// BasePoint tiene el instante SIEMPRE y la lectura A VECES -- una operacion que nunca se
		// completo sabe cuando se sembro y no sabe en que odometro. Esa mitad opcional viajaba
		// como un int en cero al lado de un bool, con la unidad en null.
		//
		// La semantica que T3 fijo no se toca: preguntar HasReading antes, y que leer sin
		// preguntar reviente. Lo que cambia es quien la sostiene.
		//
		// ==================================================================================

		[Test]
		public void TheAnchorsOdometerHalfIsATypeToo()
		{
			Assert.That(AnchorReading.None.Exists(), Is.False);
			Assert.Throws<DomainException>(() => { int _ = AnchorReading.None.Value; });
			Assert.Throws<DomainException>(() => { DistanceUnit _ = AnchorReading.None.Unit; });
		}

		// POLARIDAD, y es la que se equivoca sola (E41). El accesor es GUARDADO y revienta; la
		// pregunta "esta medida en esta unidad" es TOTAL y contesta que no. Un ancla sin lectura
		// no esta medida en ninguna unidad -- eso es una respuesta, no una falta de datos.
		[Test]
		public void AnAbsentAnchorIsExpressedInNoUnitAndSaysSoWithoutThrowing()
		{
			Assert.That(AnchorReading.None.IsExpressedIn(DistanceUnit.UnitA), Is.False);
			Assert.That(AnchorReading.None.IsExpressedIn(DistanceUnit.UnitB), Is.False);
		}

		[Test]
		public void AnAnchorReadingWithoutAUnitIsRefused()
		{
			Assert.Throws<DomainException>(() => AnchorReading.Of(30000, null));
		}

		[Test]
		public void AnAnchorReadingCannotBeNegative()
		{
			Assert.Throws<DomainException>(() => AnchorReading.Of(-1, DistanceUnit.UnitA));
		}

		// Las dos guardas se mudaron de BasePoint.At al tipo que sostiene la mitad opcional, que
		// es donde vive lo que restringen. Por la puerta de adelante tienen que seguir sonando
		// igual: mudar una guarda es donde se pierden en silencio.
		[Test]
		public void TheBasePointStillRefusesAReadingWithoutAUnit()
		{
			Assert.Throws<DomainException>(
				() => BasePoint.At(new DateTime(2025, 6, 1), 30000, null));
		}

		// ===== EL TERCER SITIO: el estado que no se puede saber ============================
		//
		// Una dimension que no se puede evaluar no tiene estado, y una que ni siquiera existe en
		// la ley tampoco. Hasta aqui eso viajaba como un DueState en null -- y el conjunto
		// cerrado tiene siete miembros, ninguno de los cuales dice "no lo se".
		//
		// La respuesta NO es un octavo DueState: eso seria vocabulario que el canon no nombra
		// (DistanceOnly.cs ya lo rechazo por escrito). Es que la pregunta este GUARDADA, como la
		// lectura del ancla, y que quien la guarde sea el tipo.
		//
		// ==================================================================================

		[Test]
		public void AStateThatCannotBeKnownIsNotANullState()
		{
			Assert.Throws<DomainException>(
				() => { DueState _ = LimitStanding.DistanceIsNotPartOfThisLaw().State; },
				"Una dimension que no existe en la ley no tiene estado que devolver.");

			Assert.Throws<DomainException>(
				() => { DueState _ = SuspendedByAUnitMismatch().State; },
				"Una dimension suspendida tampoco: no se pudo medir, asi que no hay donde este "
				+ "parada. Devolver NotDue seria la mentira que el GAP G2 existe para impedir.");
		}

		// LOS TRES MOTIVOS SON EXCLUYENTES, y hasta aqui eso lo sostenia cual argumento llego en
		// null. Un cuarto motivo, o dos a la vez, se colaban sin que nada se quejara.
		[Test]
		public void ASuspendedDimensionNamesExactlyOneReason()
		{
			AssertExactlyOneReason(SuspendedByAUnitMismatch(), "unidades distintas");
			AssertExactlyOneReason(SuspendedByAnAbsentReading(), "sin lectura usable");
			AssertExactlyOneReason(SuspendedByTheUsageProfile(), "perfil que ignora la distancia");
		}

		// Una dimension que SI se pudo medir no esta suspendida por ningun motivo. La polaridad
		// importa: "no suspendida" es una respuesta total, no la ausencia de una.
		[Test]
		public void AMeasuredDimensionIsSuspendedByNothing()
		{
			LimitStanding measured = LimitStanding.OfDistance(
				from, UsableReading.Of(Reading(45000, DistanceUnit.UnitA)), DistanceUnit.UnitA,
				TenThousandUnits, unscaled);

			Assert.That(measured.CanBeEvaluated, Is.True);
			Assert.That(measured.SuspendedByUnitMismatch, Is.False);
			Assert.That(measured.SuspendedByAnAbsentReading, Is.False);
			Assert.That(measured.SuspendedByTheUsageProfile, Is.False);
			Assert.That(measured.State, Is.SameAs(DueState.Overdue));
		}

		private static void AssertExactlyOneReason(LimitStanding suspended, string caso)
		{
			int reasons = 0;
			if (suspended.SuspendedByUnitMismatch) reasons++;
			if (suspended.SuspendedByAnAbsentReading) reasons++;
			if (suspended.SuspendedByTheUsageProfile) reasons++;

			Assert.That(suspended.CanBeEvaluated, Is.False, caso);
			Assert.That(reasons, Is.EqualTo(1),
				"Suspendida por " + caso + ", y los motivos que se declaran son " + reasons + ". "
				+ "Tienen que ser exactamente uno: dos a la vez es un motivo inventado, y cero es "
				+ "una suspension que no se puede explicar.");
		}

		private LimitStanding SuspendedByAUnitMismatch()
		{
			return LimitStanding.OfDistance(
				from, UsableReading.Of(Reading(45000, DistanceUnit.UnitB)), DistanceUnit.UnitA,
				TenThousandUnits, unscaled);
		}

		private LimitStanding SuspendedByAnAbsentReading()
		{
			return LimitStanding.OfDistance(
				from, UsableReading.None, DistanceUnit.UnitA, TenThousandUnits, unscaled);
		}

		private LimitStanding SuspendedByTheUsageProfile()
		{
			return LimitStanding.OfDistance(
				from, UsableReading.Of(Reading(45000, DistanceUnit.UnitA)), DistanceUnit.UnitA,
				TenThousandUnits, IntervalLengthFactor.For(UsageProfile.LowDistance));
		}

		private static IntervalRule TheRuleOfBoth()
		{
			return IntervalRule.Of(
				IntervalBasisCode.TimeOrDistance, TwelveMonthsInDays, TenThousandUnits,
				DistanceUnitCode.UnitA);
		}

		private static OdometerReading Reading(int value, DistanceUnit unit)
		{
			return new OdometerReading(
				value, unit, new DateTime(2026, 1, 1), ReadingSource.Telemetry);
		}
	}
}
