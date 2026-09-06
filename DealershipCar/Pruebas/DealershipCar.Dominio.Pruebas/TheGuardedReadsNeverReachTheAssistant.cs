using System;
using DealershipCar.Mcp;
using DealershipCar.Mcp.Tools;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LAS LECTURAS GUARDADAS NO PUEDEN SALIR COMO ERROR DEL PROTOCOLO.
	//
	// ReadNextDueKind y LastEvaluatedAt tiran InvalidOperationException cuando no hay nada vencido /
	// cuando nunca se evaluo. Eso es E41 y es correcto: su contrato ES reventar, y la que avisa es
	// su guarda de presencia (decision 6 de T6). Pero una excepcion que escapa de una tool MCP le
	// llega al asistente como "la herramienta fallo", y "no hay nada vencido" es la mejor noticia
	// que esta app puede dar, no una falla.
	//
	// EN T8 PASARON A SER CUATRO. La ficha del fabricante trae el mismo par: ReadCurrentRevisionCode
	// y ReadCurrentRevisionPublishedAt revientan sin ninguna revision sellada, que es el estado de
	// toda especificacion recien creada -- o sea el estado NORMAL, no el raro. El nombre de esta
	// fixture decia "las dos" y ya no era cierto; la ley no cambio.
	//
	// (La quinta guardada del modulo, ReadCoverageStanding, no llega a esta frontera porque ninguna
	// tool la llama: T8 decidio que la posicion de este vehiculo frente al alcance no se contesta
	// aca. Eso lo defiende ThePublishedScopeReachesTheAssistantAndTheStandingDoesNot.)
	//
	// Asi que la caratula pregunta la guarda y contesta el lado VACIO. Estas pruebas fijan las dos
	// mitades: que la tool no revienta, y -- el control -- que la puerta SIGUE reventando. Sin el
	// control, ablandar la puerta dejaria estas pruebas verdes y habria roto E41 sin que nadie lo
	// notara.
	[TestFixture]
	public sealed class TheGuardedReadsNeverReachTheAssistant
	{
		private const string TheVin = "1HGCM82633A004352";

		private static readonly DateTime ThePublication =
			new DateTime(2025, 8, 15, 0, 0, 0, DateTimeKind.Utc);

		[Test]
		public void NothingDueAnswersWithAnEmptyNextToDoInsteadOfThrowing()
		{
			using (AScheduleOnARealJournal journal = AVehicleWithNothingDue())
			{
				var tools = new DealershipCarTools(new JournalReader(journal.Folder, ""));

				MaintenanceReview review = tools.Review();

				Assert.That(review.HasData, Is.True);
				Assert.That(review.AnythingDue, Is.False);
				Assert.That(review.NextToDo, Is.EqualTo(""),
					"Vacio es la respuesta a 'que hago primero' cuando no hay nada que hacer. Un "
					+ "nombre inventado seria peor que la excepcion.");

				// EL CONTROL: la puerta no se ablando. Si esto dejara de tirar, la prueba de arriba
				// pasaria por la razon equivocada.
				Assert.Throws<InvalidOperationException>(() => journal.Api.ReadNextDueKind());
			}
		}

		[Test]
		public void AScheduleNeverEvaluatedAnswersWithAnEmptyInstantInsteadOfThrowing()
		{
			using (AScheduleOnARealJournal journal = AVehicleWithNothingDue())
			{
				var tools = new DealershipCarTools(new JournalReader(journal.Folder, ""));

				MaintenanceReview review = tools.Review();
				ReadingFreshness fresh = tools.HowUpToDate();
				OperationStanding one = tools.CheckOne(MaintenanceOperation.FluidChange);

				Assert.That(review.Evaluated, Is.False);
				Assert.That(review.EvaluatedUpTo, Is.EqualTo(""));
				Assert.That(fresh.EvaluatedUpTo, Is.EqualTo(""));
				Assert.That(one.EvaluatedUpTo, Is.EqualTo(""),
					"Vacio y no DateTime.MinValue: un centinela magico es un instante que parece un "
					+ "instante y no lo es (E45).");

				Assert.Throws<InvalidOperationException>(() => journal.Api.LastEvaluatedAt());
			}
		}

		// Y CUANDO SI HAY, LOS DOS CAMPOS TRAEN EL DATO. Sin esto, una caratula que contestara ""
		// siempre pasaria las dos pruebas de arriba.
		[Test]
		public void AndWhenThereIsSomethingDueBothFieldsCarryIt()
		{
			using (AScheduleOnARealJournal journal = AVehicleWithNothingDue())
			{
				journal.Api.Evaluate(new DateTime(2027, 10, 1, 0, 0, 0, DateTimeKind.Utc));

				var tools = new DealershipCarTools(new JournalReader(journal.Folder, ""));
				MaintenanceReview review = tools.Review();

				Assert.That(review.AnythingDue, Is.True);
				Assert.That(review.NextToDo, Is.EqualTo("FluidChange"));
				Assert.That(review.Evaluated, Is.True);
				Assert.That(review.EvaluatedUpTo, Is.EqualTo("2027-10-01T00:00:00.0000000Z"),
					"Con la Z: la puerta vuelve a estampar Utc al salir porque el journal borra el "
					+ "Kind (C24), y perderla aca haria que el asistente leyera un instante sin huso.");
			}
		}

		// EL PAR DE T8, Y ES EL ESTADO NORMAL Y NO EL RARO: una especificacion sin ninguna revision
		// sellada es toda especificacion recien creada, y lo es tambien mientras el fabricante arma
		// una revision que todavia no cerro. La caratula tiene que contestar en las TRES tools de la
		// ficha, porque las tres traen la revision vigente.
		[Test]
		public void ASpecificationWithNothingSealedAnswersWithAnEmptyRevisionInsteadOfThrowing()
		{
			using (AScheduleOnARealJournal journal = AVehicleWithNothingDue())
			{
				var tools = new ModelSpecificationTools(new JournalReader(journal.Folder, ""));

				SpecificationSheet sheet = tools.WhatMyCarTakes();
				WarrantyScope scope = tools.DoesTheWarrantyCover("");
				ServicePricing pricing =
					tools.WhatTheServiceCosts(MaintenanceOperation.FluidChange);

				Assert.That(sheet.SpecificationKnown, Is.True,
					"La especificacion SI esta -- la app de hoy la siembra --; lo que no hay es nada "
					+ "sellado adentro. Confundir las dos ausencias es exactamente lo que este par "
					+ "de campos existe para no hacer.");
				Assert.That(sheet.CurrentRevision, Is.EqualTo(""));
				Assert.That(sheet.CurrentRevisionPublishedAt, Is.EqualTo(""),
					"Vacio y no DateTime.MinValue, por lo mismo que arriba.");
				Assert.That(scope.CurrentRevision, Is.EqualTo(""));
				Assert.That(pricing.CurrentRevision, Is.EqualTo(""));

				// EL CONTROL, las dos mitades del par: la puerta SIGUE reventando.
				Assert.Throws<InvalidOperationException>(
					() => journal.Api.ReadCurrentRevisionCode());
				Assert.Throws<InvalidOperationException>(
					() => journal.Api.ReadCurrentRevisionPublishedAt());
			}
		}

		// Y CUANDO SI HAY REVISION SELLADA, LOS DOS CAMPOS TRAEN EL DATO. Sin esto, una caratula que
		// contestara "" siempre pasaria la prueba de arriba.
		[Test]
		public void AndWhenARevisionIsSealedTheCodeAndTheInstantBothCarryIt()
		{
			using (AScheduleOnARealJournal journal = AVehicleWithNothingDue())
			{
				journal.Api.OpenRevision("MY2019-R1", ThePublication);
				journal.Api.PublishFuelGrade("MY2019-R1", "91 RON regular", "Minimum");
				journal.Api.SealRevision("MY2019-R1");

				var tools = new ModelSpecificationTools(new JournalReader(journal.Folder, ""));
				SpecificationSheet sheet = tools.WhatMyCarTakes();

				Assert.That(sheet.CurrentRevision, Is.EqualTo("MY2019-R1"));
				Assert.That(sheet.CurrentRevisionPublishedAt,
					Is.EqualTo("2025-08-15T00:00:00.0000000Z"),
					"Con la Z, por la misma razon que el instante de la evaluacion (C24).");

				Assert.That(
					tools.DoesTheWarrantyCover("").CurrentRevision, Is.EqualTo("MY2019-R1"));
				Assert.That(
					tools.WhatTheServiceCosts(MaintenanceOperation.FluidChange)
						.CurrentRevision,
					Is.EqualTo("MY2019-R1"));
			}
		}

		private static AScheduleOnARealJournal AVehicleWithNothingDue()
		{
			var journal = new AScheduleOnARealJournal(TheVin, "Standard");

			// Un plan sembrado y NADA evaluado: los dos lados vacios a la vez.
			journal.Api.SeedPlanOperation(
				"FluidChange", "TimeOrDistance", 365, 10000, "UnitA", true, false,
				new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), 12000, "UnitA");

			return journal;
		}
	}
}
