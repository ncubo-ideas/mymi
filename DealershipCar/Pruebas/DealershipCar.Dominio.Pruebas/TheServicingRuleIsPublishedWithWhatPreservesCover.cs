using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LA REGLA DEL FABRICANTE SOBRE SERVICIO EN OTRO TALLER, y lo que hay que hacer para conservar
	// la cobertura. Son las DOS mitades del canon §4 del brief y ninguna sirve sola: una regla sin
	// que hacer al respecto deja a la persona sabiendo que tiene un problema y no que hacer con el.
	//
	// LA HOJA DICE LA REGLA; NO DICTAMINA. Este modulo publica lo que el fabricante publico y la
	// posicion de este vehiculo frente a ello. No dice que la cobertura este conservada, ni que se
	// vaya a pagar nada: eso es adjudicacion, y la adjudicacion es del fabricante. La frase se
	// escribe para que no se pueda leer como una promesa.
	//
	// EL TEXTO PUBLICADO SE MUESTRA COMO SE PUBLICO -- entre comillas, sin traducir y sin reescribir
	// (§3 del brief deja traduccion y locale afuera). Las comillas no son adorno: separan lo que
	// dijo el fabricante de lo que compone esta hoja.
	[TestFixture]
	public sealed class TheServicingRuleIsPublishedWithWhatPreservesCover
	{
		private const string TheModel = "YARIS-2019";
		private const string TheRevision = "REV-2025-01";
		private const string TheRule = "Work may be done at any workshop.";
		private const string WhatPreservesCover = "Keep the invoice and the parts numbers.";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);

		private ModelSpecification specification;

		[SetUp]
		public void Setup()
		{
			specification = new ModelSpecification(TheModel);
			specification.OpenRevision(TheRevision, PublishedAt);
		}

		[Test]
		public void BothHalvesOfTheRuleAreSaidAndTheTextIsShownAsPublished()
		{
			ThePolicyEnters();
			specification.SealRevision(TheRevision);

			string answer = specification.DescribeServicingElsewhereRule(0, 0, 0);

			Assert.That(answer, Does.Contain("\"" + TheRule + "\""));
			Assert.That(answer, Does.Contain("\"" + WhatPreservesCover + "\""));
			Assert.That(specification.ReadServicingPolicyRevision(), Is.EqualTo(TheRevision));
		}

		// NO DICTAMINA. Ninguna palabra de la respuesta promete un resultado: dice la regla y dice
		// donde esta parado el vehiculo, y quien decide si una reclamacion se paga es el fabricante.
		[Test]
		public void TheAnswerNeverPromisesThatCoverIsPreservedOrThatAnythingWillBePaid()
		{
			ThePolicyEnters();
			specification.SealRevision(TheRevision);

			string answer = specification.DescribeServicingElsewhereRule(2, 1, 0);

			foreach (string promesa in new[]
				{ "will be paid", "is covered", "guarantee", "entitled", "approved" })
				Assert.That(answer, Does.Not.Contain(promesa),
					$"La respuesta dice '{promesa}'. Esta hoja publica el alcance; adjudicar es del "
					+ "fabricante, y una frase que suena a promesa se repite como si lo fuera.");
		}

		[Test]
		public void APolicyWithoutARuleIsNotAPublishedPolicy()
		{
			Assert.Throws<DomainException>(
				() => specification.PublishServicingPolicy(TheRevision, "   ", WhatPreservesCover));
		}

		[Test]
		public void APolicyThatDoesNotSayWhatPreservesCoverIsNotAPublishedPolicy()
		{
			DomainException error = Assert.Throws<DomainException>(
				() => specification.PublishServicingPolicy(TheRevision, TheRule, ""));

			Assert.That(error.Message, Does.Not.Contain("§"),
				"C45: el mensaje dice la regla en palabras, no cita un documento.");
		}

		[Test]
		public void APolicyPublishedOnlyIntoADraftIsNotAnswered()
		{
			ThePolicyEnters();

			Assert.That(specification.IsServicingPolicyPublished(), Is.False);
			Assert.That(specification.DescribeServicingElsewhereRule(0, 0, 0),
				Does.Not.Contain(TheRule));
		}

		[Test]
		public void WithinOneDraftTheLastPublishedPolicyIsTheOneThatStands()
		{
			ThePolicyEnters();
			specification.PublishServicingPolicy(
				TheRevision, "Work must be done at an authorised workshop.", WhatPreservesCover);
			specification.SealRevision(TheRevision);

			string answer = specification.DescribeServicingElsewhereRule(0, 0, 0);

			Assert.That(answer, Does.Contain("authorised workshop"));
			Assert.That(answer, Does.Not.Contain("at any workshop"));
		}

		private void ThePolicyEnters()
		{
			specification.PublishServicingPolicy(TheRevision, TheRule, WhatPreservesCover);
		}
	}
}
