using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LEY L4, NextOccurrenceFromActualCompletion, abstracta sobre IntervalRule (§5, OINV-26):
	// "La proxima ocurrencia se calcula desde el instante y la lectura en que la operacion se
	// completo DE VERDAD, nunca desde la grilla teorica en que estaba agendada."
	//
	// Es la prueba 5 de §9, y el numero que la hace valer la pena es el 200 de diferencia:
	// vencia a 40000, se hizo a 39800, y la siguiente vence a 49800. NO a 50000.
	//
	// POR QUE IMPORTA MAS DE LO QUE PARECE. La grilla teorica es comoda de programar --
	// 30000, 40000, 50000, siempre redondo -- y es MENTIRA. La persona hizo el servicio a
	// 39800; medir el proximo intervalo desde 40000 le regala 200 unidades que nadie le dio, y
	// el error se ACUMULA: doce ocurrencias mas tarde el plan del fabricante y la realidad no
	// se parecen. El fabricante dijo "cada 10000 desde que se hizo", no "en los multiplos de
	// 10000".
	//
	// La discriminacion esta en 49800 exacto: anclado en la completacion real, son 10000
	// recorridas y VENCE; anclado en la grilla teorica, son 9800 y solo avisa. Las dos
	// lecturas posibles dan respuestas distintas en ese punto, que es lo que hace que la
	// prueba pruebe algo.
	[TestFixture]
	public sealed class TheNextBasePointComesFromTheActualCompletion
	{
		private const int DoceMesesEnDias = 365;
		private const int DiezMilUnidades = 10000;

		private static readonly DateTime SeCompletoEl = new DateTime(2026, 2, 1);
		private const int SeCompletoA = 39800;

		private IntervalRule regla;
		private BasePoint proximaOcurrencia;
		private IntervalLengthFactor sinEscalar;

		[SetUp]
		public void Setup()
		{
			regla = IntervalRule.Of(
				IntervalBasisCode.TimeOrDistance, DoceMesesEnDias, DiezMilUnidades,
				DistanceUnitCode.UnitA);

			proximaOcurrencia = regla.NextOccurrenceFromActualCompletion(
				SeCompletoEl, SeCompletoA, DistanceUnit.UnitA);

			sinEscalar = IntervalLengthFactor.For(UsageProfile.Standard);
		}

		[Test]
		public void TheNewBasePointIsTheActualCompletion()
		{
			Assert.That(proximaOcurrencia.Instant, Is.EqualTo(SeCompletoEl));
			Assert.That(proximaOcurrencia.HasReading, Is.True);
			Assert.That(proximaOcurrencia.Reading, Is.EqualTo(SeCompletoA),
				"Vencia a 40000 y se hizo a 39800. El ancla es 39800: la que paso, no la que "
				+ "estaba agendada.");
			Assert.That(proximaOcurrencia.Unit, Is.SameAs(DistanceUnit.UnitA));
		}

		// LA DISCRIMINACION. 49800 = 39800 + 10000 -> vence. Si estuviera anclada en la grilla
		// teorica (40000), 49800 serian 9800 recorridas y esto diria DueSoon.
		[Test]
		public void TheNextOneFallsDueAtTheIntervalCountedFromTheActualCompletion()
		{
			DueAssessment vencimiento = regla.Assess(
				proximaOcurrencia, new DateTime(2026, 6, 1), Lectura(49800), sinEscalar);

			Assert.That(vencimiento.State, Is.SameAs(DueState.Due),
				"49800 son exactamente 10000 unidades desde 39800. Si esto no dice Due, el "
				+ "intervalo se esta midiendo desde 40000 -- la grilla teorica -- y la persona "
				+ "acaba de recibir 200 unidades que nadie le dio.");
		}

		[Test]
		public void OneUnitBeforeThatPointItIsNotDueYet()
		{
			DueAssessment vencimiento = regla.Assess(
				proximaOcurrencia, new DateTime(2026, 6, 1), Lectura(49799), sinEscalar);

			Assert.That(vencimiento.State, Is.Not.SameAs(DueState.Due));
		}

		// La misma ley del lado del tiempo. Anclada en la completacion real vence el
		// 2027-02-01; anclada en la grilla teorica de la ocurrencia anterior (2025-06-01 mas
		// 365) habria vencido el 2026-06-01, que es la fecha que se prueba aca en negativo.
		[Test]
		public void TimeIsAlsoCountedFromTheActualCompletion()
		{
			DueAssessment enLaFechaDeLaGrillaVieja = regla.Assess(
				proximaOcurrencia, new DateTime(2026, 6, 1), Lectura(41000), sinEscalar);

			DueAssessment unAnoDespuesDeLaCompletacion = regla.Assess(
				proximaOcurrencia, new DateTime(2027, 2, 1), Lectura(41000), sinEscalar);

			Assert.That(enLaFechaDeLaGrillaVieja.State, Is.SameAs(DueState.NotDue),
				"Han pasado 120 dias desde que se completo. Si esto vence, el intervalo se esta "
				+ "midiendo desde la ocurrencia anterior y no desde esta.");
			Assert.That(unAnoDespuesDeLaCompletacion.State, Is.SameAs(DueState.Due));
		}

		// TimeOnly no tiene dimension de distancia, asi que su punto base NO lleva lectura.
		// Guardar una lectura que ninguna ley lee es invitar al proximo lector a creer que la
		// distancia cuenta para esta operacion. La mitad opcional del punto base es la
		// lectura, nunca el instante: una operacion siempre sabe CUANDO se hizo.
		[Test]
		public void TimeOnlyAnchorsOnlyTheInstant()
		{
			IntervalRule soloTiempo = IntervalRule.Of(
				IntervalBasisCode.TimeOnly, DoceMesesEnDias, 0, DistanceUnitCode.UnitA);

			BasePoint proxima = soloTiempo.NextOccurrenceFromActualCompletion(
				SeCompletoEl, SeCompletoA, DistanceUnit.UnitA);

			Assert.That(proxima.Instant, Is.EqualTo(SeCompletoEl));
			Assert.That(proxima.HasReading, Is.False,
				"TimeOnly no admite limite de distancia (la guarda de OINV-20 lo rechaza), asi "
				+ "que su ancla tampoco lleva una: seria un dato que ninguna ley lee.");
		}

		[Test]
		public void ConditionOnlyAnchorsOnlyTheInstant()
		{
			IntervalRule porCondicion = IntervalRule.Of(
				IntervalBasisCode.ConditionOnly, 0, 0, DistanceUnitCode.UnitA);

			BasePoint proxima = porCondicion.NextOccurrenceFromActualCompletion(
				SeCompletoEl, SeCompletoA, DistanceUnit.UnitA);

			Assert.That(proxima.Instant, Is.EqualTo(SeCompletoEl));
			Assert.That(proxima.HasReading, Is.False);
		}

		// Leer la lectura de un ancla que no la tiene es defecto de quien llama, no una
		// respuesta vacia: "no hay lectura" y "la lectura es cero" son cosas distintas y
		// confundirlas hace que un vehiculo recien entregado parezca haber recorrido cero
		// unidades desde su ultima completacion inexistente (E41, la mitad guardada).
		[Test]
		public void ReadingTheOdometerOfABasePointThatHasNoneThrows()
		{
			BasePoint soloInstante = BasePoint.AtInstantOnly(SeCompletoEl);

			Assert.That(soloInstante.HasReading, Is.False);
			Assert.Throws<DomainException>(() => { int _ = soloInstante.Reading; });
			Assert.Throws<DomainException>(() => { DistanceUnit _ = soloInstante.Unit; });
		}

		private static UsableReading Lectura(int valor)
		{
			return UsableReading.Of(new OdometerReading(
				valor, DistanceUnit.UnitA, new DateTime(2026, 6, 1), ReadingSource.Telemetry));
		}
	}
}
