using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L3 del brief, ScopeIsNotAdjudication: "este modulo dice si un sistema esta dentro del
	// alcance publicado, si el vehiculo esta dentro de su sobre, y que exclusiones aplican. NUNCA
	// dice que un reclamo se va a pagar -- adjudicar es del fabricante."
	//
	// ES LA MITAD DE ADJUDICACION DEL TEST 15 DE §11, y es la ley sobre la que descansa la
	// credibilidad del modulo entero. Una app que dice "esto esta cubierto" en el sentido de "te
	// lo van a pagar" esta prometiendo algo que ni el concesionario decide. Lo que este modulo
	// sabe es lo que el fabricante PUBLICO; lo que pasa con un reclamo concreto lo decide otro.
	//
	// SE MECANIZA DE DOS FORMAS, porque una sola no alcanza:
	//
	//   -- NINGUNA FRASE PROMETE PAGO. Se generan las respuestas de las seis situaciones del
	//      procedimiento y se pasan por una lista de frases prohibidas. No se prohibe la palabra
	//      "Covered" -- es un miembro del canon y decir el alcance es justamente el trabajo --
	//      se prohibe el lenguaje de PROMESA: que se va a pagar, que esta garantizado, que el
	//      reclamo esta aprobado.
	//
	//   -- LA CLAUSULA DE ALCANCE VA EN TODA RESPUESTA. §3.4 del plan pide que la ley SE DIGA A
	//      SI MISMA en la respuesta: cada frase de cobertura termina diciendo que es alcance
	//      publicado y no una decision sobre un reclamo. Una ley que solo vive en la cabeza de
	//      quien escribio el codigo se pierde en la primera capa que reformula la frase.
	[TestFixture]
	public sealed class ScopeIsNotAdjudication
	{
		private const string TheModel = "SM-SEDAN-2025";
		private const string TheRevision = "REV-2025-01";
		private const string TheCoveredSystem = "ENG-BLOCK";
		private const string TheExcludedSystem = "PAINT";
		private const string TheSystemWithShortLimits = "HV-BATTERY";
		private const string TheSystemInAnotherUnit = "GEARBOX";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);
		private static readonly DateTime DeliveredAt = new DateTime(2020, 1, 15, 9, 0, 0);
		private static readonly DateTime AsOf = new DateTime(2022, 1, 15, 9, 0, 0);

		// El lenguaje de PROMESA, que es lo unico prohibido. Cada frase esta escrita en la forma
		// en que una app tranquilizadora la escribiria sin darse cuenta.
		private static readonly string[] LoQueNingunaRespuestaPuedeDecir =
		{
			"will be paid",
			"will pay",
			"will be reimbursed",
			"reimburse",
			"guaranteed",
			"we guarantee",
			"claim approved",
			"approved claim",
			"free of charge",
			"at no cost",
			"entitled to",
		};

		private ModelSpecification specification;

		[SetUp]
		public void Setup()
		{
			specification = new ModelSpecification(TheModel);
			specification.OpenRevision(TheRevision, PublishedAt);
			specification.PublishCoveredSystem(
				TheRevision, TheCoveredSystem, 0, 0, DistanceUnitCode.UnitA);
			specification.PublishCoveredSystem(
				TheRevision, TheSystemWithShortLimits, 12, 20000, DistanceUnitCode.UnitA);
			specification.PublishCoveredSystem(
				TheRevision, TheSystemInAnotherUnit, 0, 60000, DistanceUnitCode.UnitB);
			specification.PublishCoverageExclusion(
				TheRevision, TheExcludedSystem,
				"paintwork and exterior trim are outside the warranty",
				"from delivery onwards");
			specification.PublishCoverageExclusion(
				TheRevision, TheCoveredSystem,
				"damage from running the engine without oil",
				"when the oil level was below the minimum");
			specification.SealRevision(TheRevision);
		}

		// TEST 15 DE §11, la mitad de adjudicacion.
		[Test]
		public void NoAnswerAnywhereSaysAClaimWillBePaid()
		{
			List<string> prometen = new List<string>();

			foreach (string sentence in EveryCoverageSentenceThisModuleCanProduce())
				foreach (string promesa in LoQueNingunaRespuestaPuedeDecir)
					if (sentence.IndexOf(promesa, StringComparison.OrdinalIgnoreCase) >= 0)
						prometen.Add(promesa + " -> " + sentence);

			Assert.That(prometen, Is.Empty,
				"Ley L3: este modulo dice el alcance publicado y no adjudica. Frases que prometen: "
				+ string.Join(" | ", prometen));
		}

		// §3.4 del plan: la ley SE DICE A SI MISMA en la respuesta.
		[Test]
		public void EveryCoverageSentenceSaysItIsScopeAndNotADecisionOnAClaim()
		{
			foreach (string sentence in EveryCoverageSentenceThisModuleCanProduce())
				Assert.That(sentence, Does.Contain("Solo alcance"),
					"Falta la clausula de alcance en: " + sentence);
		}

		// Y la clausula dice de QUIEN es la decision. "No adjudicamos" sin decir quien lo hace
		// deja a la persona sin saber a donde ir.
		[Test]
		public void TheScopeClauseSaysWhoseTheDecisionIs()
		{
			Assert.That(specification.DescribeCoverageSummary(),
				Does.Contain("fabricante"));
		}

		// La misma ley alcanza al resumen del alcance, que es la respuesta que la gente lee
		// primero: es una lista de lo publicado, no un ofrecimiento.
		[Test]
		public void TheScopeSummaryDoesNotPromiseAnythingEither()
		{
			string summary = specification.DescribeCoverageSummary();

			foreach (string promesa in LoQueNingunaRespuestaPuedeDecir)
				Assert.That(summary.IndexOf(promesa, StringComparison.OrdinalIgnoreCase), Is.LessThan(0),
					"El resumen promete: " + promesa);
		}

		private IEnumerable<string> EveryCoverageSentenceThisModuleCanProduce()
		{
			yield return TheSentenceFor(TheCoveredSystem, DistanceUnitCode.UnitA, AsOf, 12000);
			yield return TheSentenceFor(TheExcludedSystem, DistanceUnitCode.UnitA, AsOf, 12000);
			yield return TheSentenceFor("NO-SUCH", DistanceUnitCode.UnitA, AsOf, 12000);
			yield return TheSentenceFor(
				TheSystemWithShortLimits, DistanceUnitCode.UnitA, AsOf, 12000);
			yield return TheSentenceFor(
				TheSystemInAnotherUnit, DistanceUnitCode.UnitA, AsOf, 12000);
			yield return specification.DescribeCoverageSummary();

			ModelSpecification nothingSealed = new ModelSpecification(TheModel);
			yield return nothingSealed.DescribeCoverage(
				TheCoveredSystem, 36, 100000, DistanceUnitCode.UnitA,
				DeliveredAt, 10, 12000, DistanceUnitCode.UnitA, AsOf);
			yield return nothingSealed.DescribeCoverageSummary();
		}

		private string TheSentenceFor(
			string systemCode, DistanceUnitCode odometerUnit, DateTime asOf, int currentReading)
		{
			return specification.DescribeCoverage(
				systemCode, 36, 100000, DistanceUnitCode.UnitA,
				DeliveredAt, 10, currentReading, odometerUnit, asOf);
		}
	}
}
