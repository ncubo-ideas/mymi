using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// PRUEBA 7 de §9, y con ella los DOS GAPs que aterrizan en T3.
	//
	// OINV-21 / ley L7: "Los limites de distancia se evaluan solo contra lecturas en la MISMA
	// unidad del plan. Una discrepancia SUSPENDE la evaluacion de distancia para esa operacion
	// y la deja cayendo a solo-tiempo. Nunca convierte." El nombre de la ley vive en
	// ScheduledOperation (T4); LA CUENTA es de la regla, y es lo que se fija aca.
	//
	// GAP G2 -- DOS LIMITES INCONMENSURABLES, CUALQUIERA DE LOS DOS PUEDE FALTAR.
	// La respuesta que este slice adopta: una dimension que NO SE PUEDE EVALUAR es
	// exactamente eso, y NO "todavia no llego". Confundirlas es el defecto que el GAP existe
	// para nombrar:
	//
	//   con L1 (la primera que llegue) tratar lo inevaluable como "no llego" es inofensivo por
	//   casualidad -- la otra dimension igual decide;
	//
	//   con L2 (los dos limites) es MORTAL: si la distancia inevaluable cuenta como "no
	//   llego", el minimo de las dos es NotDue PARA SIEMPRE. La operacion se ve al dia el
	//   resto de la vida del vehiculo, y nadie la mira dos veces porque el sistema dice que
	//   esta bien. Por eso la operacion DEGRADA a la dimension que todavia puede evaluar.
	//
	// GAP G4 -- DONDE VIVE LA DIMENSION SUSPENDIDA. Es una RESPUESTA CALCULADA de la regla
	// dadas las lecturas, no un estado guardado. La diferencia se ve en una sola prueba: llega
	// una lectura en la unidad correcta y la suspension DESAPARECE SOLA, sin que nadie la
	// levante. Guardada, alguien tendria que acordarse de apagarla -- y el dia que se olvide,
	// la distancia queda muerta para siempre sin que nada lo diga.
	[TestFixture]
	public sealed class AUnitMismatchSuspendsTheDistance
	{
		private const int DoceMesesEnDias = 365;
		private const int DiezMilUnidades = 10000;

		private static readonly DateTime UltimaCompletacion = new DateTime(2025, 6, 1);
		private const int LecturaDeLaUltimaCompletacion = 30000;

		private BasePoint desde;
		private IntervalLengthFactor sinEscalar;

		[SetUp]
		public void Setup()
		{
			desde = BasePoint.At(
				UltimaCompletacion, LecturaDeLaUltimaCompletacion, DistanceUnit.UnitA);

			sinEscalar = IntervalLengthFactor.For(UsageProfile.Standard);
		}

		// PRUEBA 7 de §9. Limites en UnitA, lectura en UnitB.
		//
		// 45000 menos 30000 son 15000, que pasarian el limite de 10000 con holgura. Si la
		// operacion vence, alguien resto dos numeros en unidades distintas -- que es
		// exactamente lo que OINV-21 prohibe, y no hace falta que exista un Convert para
		// cometerlo: basta con no mirar la unidad.
		[Test]
		public void AReadingInTheOtherUnitIsNotCompared()
		{
			DueAssessment vencimiento = ReglaDeLasDos().Assess(
				desde, new DateTime(2026, 1, 20), Lectura(45000, DistanceUnit.UnitB), sinEscalar);

			Assert.That(vencimiento.State, Is.SameAs(DueState.NotDue),
				"La lectura viene en UnitB y el limite esta en UnitA. Si esto vence, se restaron "
				+ "dos numeros en unidades distintas. No hace falta un Convert para hacerlo: "
				+ "basta con no mirar la unidad.");

			Assert.That(vencimiento.DistanceIsSuspended, Is.True,
				"No es 'todavia no llego': es 'no se puede evaluar'. Esa es la distincion "
				+ "entera del GAP G2.");
			Assert.That(vencimiento.SuspendedByUnitMismatch, Is.True);
		}

		// La discrepancia es VISIBLE. La ley existe para que el sistema DIGA "no puedo comparar
		// esto" en vez de inventar un numero, y un numero inventado en una agenda de
		// mantenimiento es una operacion que parece al dia y no lo esta.
		[Test]
		public void TheMismatchShowsInTheReason()
		{
			DueAssessment vencimiento = ReglaDeLasDos().Assess(
				desde, new DateTime(2026, 1, 20), Lectura(45000, DistanceUnit.UnitB), sinEscalar);

			string motivo = vencimiento.Describe();

			Assert.That(motivo, Does.Contain("cannot be evaluated"));
			Assert.That(motivo, Does.Contain("UnitA"));
			Assert.That(motivo, Does.Contain("UnitB"));
			Assert.That(motivo, Does.Contain("never converted"),
				"OUS-07: la persona tiene que poder saber POR QUE el sistema no esta mirando la "
				+ "distancia. 'No vencio' a secas es indistinguible de 'no lo se'.");
		}

		// GAP G4. La suspension es CALCULADA: llega una lectura en la unidad del plan y
		// desaparece sola, sin que nadie la levante.
		[Test]
		public void TheSuspensionHealsItselfWhenAReadingInThePlanUnitArrives()
		{
			IntervalRule regla = ReglaDeLasDos();

			DueAssessment conLaUnidadEquivocada = regla.Assess(
				desde, new DateTime(2026, 1, 20), Lectura(45000, DistanceUnit.UnitB), sinEscalar);

			DueAssessment conLaUnidadDelPlan = regla.Assess(
				desde, new DateTime(2026, 1, 20), Lectura(45000, DistanceUnit.UnitA), sinEscalar);

			Assert.That(conLaUnidadEquivocada.DistanceIsSuspended, Is.True);
			Assert.That(conLaUnidadDelPlan.DistanceIsSuspended, Is.False,
				"Si la suspension fuera ESTADO GUARDADO en vez de respuesta calculada, la misma "
				+ "regla seguiria suspendida despues de recibir una lectura buena, y alguien "
				+ "tendria que acordarse de levantarla.");
			Assert.That(conLaUnidadDelPlan.State, Is.SameAs(DueState.Overdue));
		}

		// GAP G2, EL CASO QUE MUERDE. Con L2 (los dos limites), tratar la distancia
		// inevaluable como "todavia no llego" deja la operacion NotDue para siempre. La
		// respuesta adoptada es DEGRADAR a la dimension que si se puede evaluar.
		[Test]
		public void WithBothLimitsAnUnevaluableDistanceDoesNotFreezeTheOperation()
		{
			IntervalRule losDos = IntervalRule.Of(
				IntervalBasisCode.TimeAndDistance, DoceMesesEnDias, DiezMilUnidades,
				DistanceUnitCode.UnitA);

			DueAssessment vencimiento = losDos.Assess(
				desde, new DateTime(2026, 6, 2), Lectura(45000, DistanceUnit.UnitB), sinEscalar);

			Assert.That(vencimiento.State, Is.SameAs(DueState.Due),
				"366 dias de un limite de 365, y la distancia no se puede evaluar. Si lo "
				+ "inevaluable contara como 'no llego', el minimo de las dos seria NotDue para "
				+ "SIEMPRE: la operacion se veria al dia el resto de la vida del vehiculo.");
			Assert.That(vencimiento.DistanceIsSuspended, Is.True);
			Assert.That(vencimiento.DecidedByTime, Is.True);
		}

		// Sin lectura ninguna -- un vehiculo recien entregado, o una operacion cuyo ancla nunca
		// se completo -- es el mismo caso: no se puede evaluar, no es que no llego.
		[Test]
		public void WithoutAReadingTheDistanceCannotBeEvaluatedEither()
		{
			DueAssessment vencimiento = ReglaDeLasDos().Assess(
				desde, new DateTime(2026, 1, 20), null, sinEscalar);

			Assert.That(vencimiento.DistanceIsSuspended, Is.True);
			Assert.That(vencimiento.SuspendedByAnAbsentReading, Is.True);
			Assert.That(vencimiento.SuspendedByUnitMismatch, Is.False);
			Assert.That(vencimiento.Describe(), Does.Contain("no usable reading"));
		}

		// Un ancla sin lectura -- la operacion nunca se completo -- suspende la distancia por
		// el mismo motivo: no hay desde donde contar.
		[Test]
		public void ABasePointWithoutAReadingGivesNothingToCountFromEither()
		{
			DueAssessment vencimiento = ReglaDeLasDos().Assess(
				BasePoint.AtInstantOnly(UltimaCompletacion), new DateTime(2026, 1, 20),
				Lectura(45000, DistanceUnit.UnitA), sinEscalar);

			Assert.That(vencimiento.SuspendedByAnAbsentReading, Is.True);
			Assert.That(vencimiento.State, Is.SameAs(DueState.NotDue),
				"233 dias de 365: decide el tiempo, que es lo unico evaluable.");
		}

		// DistanceOnly no tiene a donde degradar: su unica dimension es la que no se puede
		// evaluar. La respuesta NO es un DueState nuevo -- el conjunto es cerrado y tiene
		// siete -- sino NotDue CON LA INEVALUABILIDAD DICHA EN VOZ ALTA. Callarla seria
		// exactamente el "parece al dia y no lo esta" que G2 existe para impedir.
		[Test]
		public void DistanceOnlyWithoutAnEvaluableDistanceSaysSoOutLoud()
		{
			IntervalRule soloDistancia = IntervalRule.Of(
				IntervalBasisCode.DistanceOnly, 0, DiezMilUnidades, DistanceUnitCode.UnitA);

			DueAssessment vencimiento = soloDistancia.Assess(
				desde, new DateTime(2026, 1, 20), Lectura(45000, DistanceUnit.UnitB), sinEscalar);

			Assert.That(vencimiento.CanBeEvaluated, Is.False,
				"Es la unica dimension que esta regla tiene, asi que no hay a donde degradar. "
				+ "Devolver NotDue a secas seria mentir con la verdad.");
			Assert.That(vencimiento.State, Is.SameAs(DueState.NotDue));
			Assert.That(vencimiento.Describe(), Does.Contain("not evidence"),
				"El motivo tiene que decir que este NotDue no es evidencia de nada.");
		}

		[Test]
		public void DistanceOnlyHealsItselfLikeTheOthers()
		{
			IntervalRule soloDistancia = IntervalRule.Of(
				IntervalBasisCode.DistanceOnly, 0, DiezMilUnidades, DistanceUnitCode.UnitA);

			DueAssessment conLaUnidadDelPlan = soloDistancia.Assess(
				desde, new DateTime(2026, 1, 20), Lectura(40000, DistanceUnit.UnitA), sinEscalar);

			Assert.That(conLaUnidadDelPlan.CanBeEvaluated, Is.True);
			Assert.That(conLaUnidadDelPlan.State, Is.SameAs(DueState.Due));
		}

		// Un ancla tomada en una unidad y un limite en la otra es la misma discrepancia por el
		// otro lado. Convertir el ancla seria la conversion entrando por la puerta de atras.
		[Test]
		public void ABasePointInTheOtherUnitAlsoSuspends()
		{
			DueAssessment vencimiento = ReglaDeLasDos().Assess(
				BasePoint.At(UltimaCompletacion, LecturaDeLaUltimaCompletacion, DistanceUnit.UnitB),
				new DateTime(2026, 1, 20), Lectura(45000, DistanceUnit.UnitA), sinEscalar);

			Assert.That(vencimiento.SuspendedByUnitMismatch, Is.True);
		}

		// Una lectura por debajo del ancla, en la misma unidad, es un odometro que fue para
		// atras. No es una dimension suspendida: es un hecho imposible, y por lo tanto un
		// defecto de quien llama (E32). Quien filtra las lecturas usables es el agregado (T5);
		// la regla no adivina cual de las dos esta mal.
		[Test]
		public void AReadingBelowTheBasePointThrows()
		{
			Assert.Throws<DomainException>(() => ReglaDeLasDos().Assess(
				desde, new DateTime(2026, 1, 20), Lectura(29999, DistanceUnit.UnitA), sinEscalar));
		}

		private static IntervalRule ReglaDeLasDos()
		{
			return IntervalRule.Of(
				IntervalBasisCode.TimeOrDistance, DoceMesesEnDias, DiezMilUnidades,
				DistanceUnitCode.UnitA);
		}

		private static OdometerReading Lectura(int valor, DistanceUnit unidad)
		{
			return new OdometerReading(
				valor, unidad, new DateTime(2026, 1, 1), ReadingSource.Telemetry);
		}
	}
}
