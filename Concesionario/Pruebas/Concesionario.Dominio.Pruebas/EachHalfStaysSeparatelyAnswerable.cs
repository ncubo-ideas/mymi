using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LEY L7b del brief, EachHalfStaysSeparatelyAnswerable, agregada por AMEND-ModelSpecification §2
	// y construida por PLAN-ModelSpecification-AmendmentMigrations B1.
	//
	// QUE DEFIENDE. L7 dice que una respuesta COMPLETA empareja lo publicado con donde queda este
	// vehiculo. Implementada al pie de la letra dejo dos de los siete items INCITABLES por cualquier
	// caller que no tenga los hechos del carro: el dominio funde las dos mitades en UNA frase que
	// lleva la revision del fabricante, y sin la mitad personal se pierde tambien la publicada, que
	// el caller tenia todo el derecho de mostrar. C58 lo encontro en el borde del asistente y C63 en
	// el del navegador, y la de C63 es la forma general.
	//
	// POR QUE ES NIVEL B Y NO NIVEL A. La mitad publicada ya existia en el dominio -- las dos
	// sobrecargas sin hechos del vehiculo estan ahi desde T2 -- y lo que faltaba era la PUERTA. L7b
	// dice "y las dos estan en la superficie de lectura, no solo en el dominio", asi que la ley se
	// defiende donde vive el defecto: en DealershipCarAPI.
	[TestFixture]
	public sealed class EachHalfStaysSeparatelyAnswerable
	{
		private const string TheVin = "1HGCM82633A004352";
		private const string TheRevision = "R1";
		private const string TheOperation = "FluidChange";

		private const int TenThousandUnits = 10000;
		private const int TwelveMonthsInDays = 365;

		// Los hechos del vehiculo que la mitad emparejada pide y la publicada no. Estan aca para que
		// el control pueda exigir que NINGUNO aparezca en la frase de la mitad publicada.
		private const int TheDistanceRemaining = 8400;

		// LA MISMA DISTANCIA COMO SE DICE, y escrita a mano a proposito: buscarla con
		// TheDistanceRemaining.ToString() encontraria "8400" en una frase que dice "8.400" nunca, y
		// el control de abajo quedaria verde por no tener nada que encontrar en ninguna de las dos.
		private const string TheDistanceRemainingSaid = "8.400";
		private const string TheUnitItAsksIn = "UnitA";
		private const int TheExternalServicesWithEvidence = 2;
		private const int TheExternalServicesWithoutEvidence = 1;
		private const int TheMissedOperations = 0;

		private static readonly DateTime Published =
			new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// LA MITAD PUBLICADA DEL INTERVALO, contestada por un caller que no tiene ni un hecho del
		// carro. Igualdad exacta y no Does.Contain: C60 -- una asercion que solo dice que llego algo
		// la cumple cualquier otra frase del mismo tipo, y aca lo que importa es que la frase sea la
		// publicada y NADA mas.
		[Test]
		public void ThePublishedServiceIntervalIsAnsweredWithNoVehicleFactAtAll()
		{
			ASealedSheet();

			Assert.That(
				journal.Api.DescribePublishedServiceInterval(TheOperation, TheLanguage.OfTheRecord),
				Is.EqualTo(
					"Cambio de fluido: se hace cada 10.000 km o 365 días. Publicado en la revisión R1."));
		}

		// LA MITAD PUBLICADA DE LA REGLA DE SERVICIO EN OTRO TALLER. Es la que el navegador tenia
		// que decir con palabras propias -- TheOwnerIsTold.NoServicingRuleWasPublished -- mientras
		// sus cinco vecinas las decia el dominio (C63).
		[Test]
		public void ThePublishedServicingRuleIsAnsweredWithNoVehicleFactAtAll()
		{
			ASealedSheet();

			string answer = journal.Api.DescribePublishedServicingElsewhereRule(TheLanguage.OfTheRecord);

			Assert.That(answer, Does.Contain(TheRuleAsPublished));
			Assert.That(answer, Does.Contain(WhatPreservesCover));
			Assert.That(answer, Does.Contain("Publicado en la revisión " + TheRevision));
		}

		// EL CONTROL, y sin el las dos pruebas de arriba pasan por la razon equivocada. Una lectura
		// de la mitad publicada apuntada por error a la sobrecarga EMPAREJADA contesta una frase que
		// tambien es no vacia, tambien nombra el sujeto y tambien lleva la revision: las tres
		// aserciones de arriba la aceptan. Lo unico que la delata es que lleva un hecho del carro
		// adentro.
		[Test]
		public void ThePublishedHalfCarriesNoReceivedFactAndIsNotThePairedSentence()
		{
			ASealedSheet();

			string publishedHalf = journal.Api.DescribePublishedServiceInterval(
				TheOperation, TheLanguage.OfTheRecord);
			string paired = journal.Api.DescribeServiceInterval(
				TheOperation, TheDistanceRemaining, TheUnitItAsksIn, TheLanguage.OfTheRecord);

			Assert.That(publishedHalf, Is.Not.EqualTo(paired),
				"Si las dos lecturas contestan lo mismo, la mitad publicada esta apuntada a la "
				+ "sobrecarga emparejada y L7b no esta defendida por nada.");

			Assert.That(publishedHalf, Does.Not.Contain(TheDistanceRemainingSaid),
				"La distancia que le queda al vehiculo es la mitad RECIBIDA. En la frase de la "
				+ "mitad publicada seria un hecho de un carro que esta lectura no pregunto.");

			Assert.That(paired, Does.Contain(TheDistanceRemainingSaid),
				"Y el control del control: si la emparejada tampoco lleva el numero recibido, la "
				+ "asercion de arriba esta verde porque no hay nada que distinguir.");

			string publishedRule = journal.Api.DescribePublishedServicingElsewhereRule(TheLanguage.OfTheRecord);
			string pairedRule = journal.Api.DescribeServicingElsewhereRule(
				TheExternalServicesWithEvidence, TheExternalServicesWithoutEvidence,
				TheMissedOperations, TheLanguage.OfTheRecord);

			Assert.That(publishedRule, Is.Not.EqualTo(pairedRule));
			Assert.That(publishedRule, Does.Not.Contain("This vehicle"),
				"Los tres conteos entran por la frase emparejada bajo 'Este vehículo'. La mitad "
				+ "publicada habla del MODELO y no de ningun carro.");
			Assert.That(pairedRule, Does.Contain("Este vehículo"));
		}

		// LEY L2 sobre la mitad publicada: lo no publicado se dice, con palabras del DOMINIO y
		// nombrando el sujeto, que es exactamente lo que el fix le devuelve al navegador. Sin
		// revision sellada las dos lecturas contestan la ausencia y ninguna revienta.
		[Test]
		public void AnUnpublishedItemAnswersWithTheDomainsOwnAbsenceSentence()
		{
			Assert.That(journal.Api.HasASealedRevision(), Is.False);

			Assert.That(journal.Api.DescribePublishedServiceInterval(
				TheOperation, TheLanguage.OfTheRecord),
				Is.EqualTo(
					"Intervalo de servicio de cambio de fluido: no se ha publicado nada para este "
					+ "modelo, así que no hay nada que citar ni ninguna revisión que nombrar."));

			Assert.That(journal.Api.DescribePublishedServicingElsewhereRule(TheLanguage.OfTheRecord),
				Is.EqualTo(
					"La regla de servicio en otro taller: no se ha publicado nada para este modelo, así "
					+ "que no hay nada que citar ni ninguna revisión que nombrar."));
		}

		// L12: una revision SIN SELLAR es invisible para toda lectura, y la mitad publicada no es
		// una excepcion. Es la prueba que faltaria si el fix hubiera compuesto la frase en la puerta
		// en vez de pedirsela al dominio: la puerta no tiene el filtro de sellado.
		[Test]
		public void ARevisionThatIsNotSealedYetAnswersNeitherHalf()
		{
			journal.Api.OpenRevision(TheRevision, Published);
			journal.Api.PublishServiceInterval(
				TheRevision, TheOperation, TenThousandUnits, TheUnitItAsksIn, TwelveMonthsInDays);
			journal.Api.PublishServicingPolicy(
				TheRevision, TheRuleAsPublished, WhatPreservesCover);

			Assert.That(journal.Api.DescribePublishedServiceInterval(
				TheOperation, TheLanguage.OfTheRecord),
				Does.Contain("no se ha publicado nada"));
			Assert.That(journal.Api.DescribePublishedServicingElsewhereRule(TheLanguage.OfTheRecord),
				Does.Contain("no se ha publicado nada"));
		}

		private const string TheRuleAsPublished =
			"Servicing at a workshop outside the network is allowed";
		private const string WhatPreservesCover =
			"an itemised invoice naming the parts and the fluids used";

		private void ASealedSheet()
		{
			journal.Api.OpenRevision(TheRevision, Published);

			journal.Api.PublishServiceInterval(
				TheRevision, TheOperation, TenThousandUnits, TheUnitItAsksIn, TwelveMonthsInDays);

			journal.Api.PublishServicingPolicy(
				TheRevision, TheRuleAsPublished, WhatPreservesCover);

			journal.Api.SealRevision(TheRevision);
		}
	}
}
