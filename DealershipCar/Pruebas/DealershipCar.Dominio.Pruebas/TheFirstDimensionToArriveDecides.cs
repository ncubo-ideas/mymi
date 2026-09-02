using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L1, WhicheverLimitArrivesFirst, sobre TimeOrDistance (§5 del slice, OINV-20):
	// "La operacion vence en el PRIMERO de sus dos limites. Ninguna dimension domina; decide
	// la que llegue primero."
	//
	// Son las pruebas 1, 2, 3, 4 y 6 de §9, con el escenario que el slice fija: intervalo de
	// 12 meses O 10000 unidades, ultima completacion 2025-06-01 a 30000.
	//
	// LO QUE DE VERDAD SE PRUEBA ACA no es la aritmetica -- restar dos fechas lo hace
	// cualquiera -- sino que las DOS dimensiones se miran y que la mas avanzada manda. Un
	// defecto tipico es escribir la ley como "vence por tiempo, y de paso miro la distancia":
	// pasa las pruebas de tiempo y falla la 1, donde la distancia llega primero y el tiempo ni
	// se acerco.
	//
	// PRUEBA 3: EL LIMITE ES INCLUSIVO. Llegar al limite ES vencer, no una unidad despues. Se
	// prueba por los dos lados del borde -- una unidad antes todavia no, exacto ya si --
	// porque un ">" en vez de un ">=" pasa desapercibido para siempre: siempre vence, un dia
	// tarde.
	//
	// PRUEBA 6 (ley L10, DueSoonIsACourtesy): si el vencimiento se alcanza en la misma
	// evaluacion que habria puesto DueSoon, se va DERECHO a Due. El aviso es cortesia, nunca
	// una parada obligatoria. A nivel de regla eso quiere decir: cuando el limite esta
	// alcanzado, Assess NUNCA contesta DueSoon. El nombre de la ley vive en
	// ScheduledOperation (T4); lo que se fija aca es la respuesta sobre la que T4 la nombra.
	[TestFixture]
	public sealed class TheFirstDimensionToArriveDecides
	{
		private const int DoceMesesEnDias = 365;
		private const int DiezMilUnidades = 10000;

		private static readonly DateTime UltimaCompletacion = new DateTime(2025, 6, 1);
		private const int LecturaDeLaUltimaCompletacion = 30000;

		private IntervalRule regla;
		private BasePoint desde;
		private IntervalLengthFactor sinEscalar;

		[SetUp]
		public void Setup()
		{
			regla = IntervalRule.Of(
				IntervalBasisCode.TimeOrDistance, DoceMesesEnDias, DiezMilUnidades,
				DistanceUnitCode.UnitA);

			desde = BasePoint.At(
				UltimaCompletacion, LecturaDeLaUltimaCompletacion, DistanceUnit.UnitA);

			sinEscalar = IntervalLengthFactor.For(UsageProfile.Standard);
		}

		// PRUEBA 1 de §9. Lectura 39000 el 2026-01-15.
		//   distancia recorrida 9000 de 10000, y el anticipo es 1000 -> DueSoon
		//   tiempo transcurrido   228 de 365,   y el anticipo es 30   -> ni cerca
		// Decide la DISTANCIA, que es la que llego primero.
		[Test]
		public void DistanceArrivesFirstAndTheReasonNamesIt()
		{
			DueAssessment vencimiento = regla.Assess(
				desde, new DateTime(2026, 1, 15), Lectura(39000, DistanceUnit.UnitA), sinEscalar);

			Assert.That(vencimiento.State, Is.SameAs(DueState.DueSoon));
			Assert.That(vencimiento.DecidedByDistance, Is.True);
			Assert.That(vencimiento.DecidedByTime, Is.False,
				"El tiempo lleva 228 de 365 dias: no decide nada todavia. Si decide, la ley se "
				+ "escribio como 'vence por tiempo y de paso miro la distancia'.");

			Assert.That(vencimiento.Describe(), Does.Contain("decided by distance"));
			Assert.That(vencimiento.Describe(), Does.Not.Contain("decided by time"));
			Assert.That(vencimiento.Describe(), Does.Contain("9000"),
				"OUS-07: el motivo tiene que decir la DIMENSION y el MONTO, no solo que vence.");
			Assert.That(vencimiento.Describe(), Does.Contain("of 10000 km"));
		}

		// PRUEBA 2 de §9. Lectura 33000 el 2026-05-02.
		//   tiempo    335 de 365 dias, anticipo 30 -> justo en el borde del aviso -> DueSoon
		//   distancia 3000 de 10000                -> ni cerca
		// Decide el TIEMPO. Es la prueba 1 al reves, y las dos juntas son las que obligan a
		// mirar de verdad las dos dimensiones.
		[Test]
		public void TimeArrivesFirstAndTheReasonNamesIt()
		{
			DueAssessment vencimiento = regla.Assess(
				desde, new DateTime(2026, 5, 2), Lectura(33000, DistanceUnit.UnitA), sinEscalar);

			Assert.That(vencimiento.State, Is.SameAs(DueState.DueSoon));
			Assert.That(vencimiento.DecidedByTime, Is.True);
			Assert.That(vencimiento.DecidedByDistance, Is.False);

			Assert.That(vencimiento.Describe(), Does.Contain("decided by time"));
			Assert.That(vencimiento.Describe(), Does.Contain("335"));
			Assert.That(vencimiento.Describe(), Does.Contain("days"));
		}

		// PRUEBA 3 de §9. Lectura 40000 el 2026-01-20 -> Due.
		// EL VALOR DEL LIMITE ES INCLUSIVO, y se prueba por los dos lados del borde.
		[Test]
		public void ReachingTheLimitIsAlreadyFallingDue()
		{
			DueAssessment enElLimite = regla.Assess(
				desde, new DateTime(2026, 1, 20), Lectura(40000, DistanceUnit.UnitA), sinEscalar);

			Assert.That(enElLimite.State, Is.SameAs(DueState.Due),
				"39999+1 = 40000 son exactamente las 10000 unidades del intervalo. Alcanzar el "
				+ "limite ES vencer; exigir una unidad mas es vencer siempre un dia tarde.");
			Assert.That(enElLimite.DecidedByDistance, Is.True);
		}

		[Test]
		public void OneUnitBeforeTheLimitIsNotFallingDueYet()
		{
			DueAssessment justoAntes = regla.Assess(
				desde, new DateTime(2026, 1, 20), Lectura(39999, DistanceUnit.UnitA), sinEscalar);

			Assert.That(justoAntes.State, Is.SameAs(DueState.DueSoon),
				"9999 de 10000 esta dentro del anticipo de cortesia pero no en el limite. Si esto "
				+ "dice Due, el borde se corrio una unidad hacia abajo.");
		}

		// La otra mitad de la inclusividad: el anticipo tambien es inclusivo. 9000 es
		// exactamente limite menos anticipo (10000 - 1000).
		[Test]
		public void TheCourtesyLeadIsInclusiveToo()
		{
			DueAssessment enElBorde = regla.Assess(
				desde, new DateTime(2026, 1, 15), Lectura(39000, DistanceUnit.UnitA), sinEscalar);

			DueAssessment unaUnidadAntes = regla.Assess(
				desde, new DateTime(2026, 1, 15), Lectura(38999, DistanceUnit.UnitA), sinEscalar);

			Assert.That(enElBorde.State, Is.SameAs(DueState.DueSoon));
			Assert.That(unaUnidadAntes.State, Is.SameAs(DueState.NotDue));
		}

		// PRUEBA 4 de §9. Los dos limites alcanzados el mismo dia -> Due, UNA transicion, y el
		// motivo nombra LAS DOS dimensiones.
		//   2026-06-01 son exactamente 365 dias desde 2025-06-01
		//   40000 son exactamente 10000 unidades desde 30000
		[Test]
		public void BothLimitsOnTheSameDayFallDueOnceAndTheReasonNamesBoth()
		{
			DueAssessment vencimiento = regla.Assess(
				desde, new DateTime(2026, 6, 1), Lectura(40000, DistanceUnit.UnitA), sinEscalar);

			Assert.That(vencimiento.State, Is.SameAs(DueState.Due));
			Assert.That(vencimiento.DecidedByTime, Is.True);
			Assert.That(vencimiento.DecidedByDistance, Is.True);
			Assert.That(vencimiento.Describe(), Does.Contain("decided by time and distance"));
		}

		// CADA CLAUSULA SE NOMBRA A SI MISMA, y hasta aqui nadie lo defendia: una etiqueta
		// cambiada dejaba el motivo diciendo "distance" dos veces, con la cuenta de los dias
		// adentro de la clausula equivocada, y toda la suite seguia verde. Se encontro
		// falsificando a proposito el nombre de la dimension de tiempo.
		//
		// "decided by time and distance" NO alcanza para taparlo: esa frase la compone
		// DueAssessment con dos booleanos, y no toca las etiquetas que pone cada dimension.
		[Test]
		public void EachClauseOfTheReasonNamesItsOwnDimension()
		{
			string motivo = regla.Assess(
				desde, new DateTime(2026, 6, 1), Lectura(40000, DistanceUnit.UnitA), sinEscalar)
				.Describe();

			Assert.That(motivo, Does.Contain("time: "),
				"La clausula del tiempo tiene que decir que es del tiempo.");
			Assert.That(motivo, Does.Contain("distance: "),
				"Y la de la distancia, que es de la distancia.");
			Assert.That(motivo, Does.Contain("days"), "Los dias van en la clausula del tiempo.");
			Assert.That(motivo, Does.Contain(" km"),
				"Y la unidad del limite en la de la distancia.");
		}

		// PRUEBA 6 de §9, ley L10. La evaluacion que habria puesto DueSoon encuentra el limite
		// ya alcanzado: se va derecho a Due. A nivel de regla, DueSoon NUNCA sale cuando el
		// limite esta alcanzado.
		//
		// 40200 esta a la vez dentro del anticipo (>= 9000 recorridas) y en el limite (>= 10000).
		// Una implementacion que pregunte por el aviso ANTES que por el vencimiento contesta
		// DueSoon aca, y esa es exactamente la que L10 prohibe.
		[Test]
		public void TheWarningIsSkippedWhenTheDueDateWasAlreadyReached()
		{
			DueAssessment vencimiento = regla.Assess(
				desde, new DateTime(2026, 1, 20), Lectura(40200, DistanceUnit.UnitA), sinEscalar);

			Assert.That(vencimiento.State, Is.SameAs(DueState.Due),
				"L10: DueSoon es una cortesia, nunca una parada obligatoria. Si el vencimiento se "
				+ "alcanzo en esta misma evaluacion, el aviso no se emite: se va derecho a Due.");
			Assert.That(vencimiento.State, Is.Not.SameAs(DueState.DueSoon));
		}

		// Consumida la gracia, Overdue. Es el estado que cuenta como mantenimiento no hecho
		// (OINV-24) y es lo que distingue "vencida" de "vencida hace rato".
		//
		// LOS TRES UMBRALES SON INCLUSIVOS, con la misma regla: alcanzar un umbral es ENTRAR en
		// su estado. El slice fija esa polaridad para el limite (prueba 3) y no da ninguna
		// razon para invertirla en los otros dos, asi que se aplica la misma a los tres. Con
		// una gracia de 500, la unidad 40500 es la que la consume entera.
		[Test]
		public void OnceTheGraceIsSpentTheOperationBecomesOverdue()
		{
			DueAssessment dentroDeLaGracia = regla.Assess(
				desde, new DateTime(2026, 1, 20), Lectura(40499, DistanceUnit.UnitA), sinEscalar);

			DueAssessment consumidaLaGracia = regla.Assess(
				desde, new DateTime(2026, 1, 20), Lectura(40500, DistanceUnit.UnitA), sinEscalar);

			Assert.That(dentroDeLaGracia.State, Is.SameAs(DueState.Due),
				"499 unidades pasadas del limite: la gracia es de 500 y todavia queda una.");
			Assert.That(consumidaLaGracia.State, Is.SameAs(DueState.Overdue),
				"500 pasadas son la gracia entera. Un umbral alcanzado es un umbral cruzado, "
				+ "igual que el limite en la prueba 3.");
		}

		// Assess es una FUNCION PURA de los hechos recibidos: no guarda nada entre llamadas.
		// Es la mitad de la pureza del reloj (§7) que se puede afirmar a nivel de regla; la
		// otra mitad -- que no hay DateTime.Now adentro -- es la prueba 16, en T5.
		[Test]
		public void TwoIdenticalEvaluationsGiveTheSameAnswer()
		{
			DueAssessment primera = regla.Assess(
				desde, new DateTime(2026, 1, 15), Lectura(39000, DistanceUnit.UnitA), sinEscalar);

			DueAssessment segunda = regla.Assess(
				desde, new DateTime(2026, 1, 15), Lectura(39000, DistanceUnit.UnitA), sinEscalar);

			Assert.That(segunda.State, Is.SameAs(primera.State));
			Assert.That(segunda.Describe(), Is.EqualTo(primera.Describe()));
		}

		// Evaluar ANTES del punto base es un defecto de quien llama: no se evalua el pasado de
		// un ancla que todavia no existia (E32).
		[Test]
		public void EvaluatingBeforeTheBasePointThrows()
		{
			Assert.Throws<DomainException>(() => regla.Assess(
				desde, UltimaCompletacion.AddDays(-1), Lectura(30000, DistanceUnit.UnitA),
				sinEscalar));
		}

		private static UsableReading Lectura(int valor, DistanceUnit unidad)
		{
			return UsableReading.Of(new OdometerReading(
				valor, unidad, new DateTime(2026, 1, 1), ReadingSource.Telemetry));
		}
	}
}
