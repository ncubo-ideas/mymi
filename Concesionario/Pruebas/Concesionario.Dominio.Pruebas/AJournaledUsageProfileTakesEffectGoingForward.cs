using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// PRUEBA 14 de §9 EN EL NIVEL B (ley L8, OINV-22, OINV-15): "un perfil de uso escala el LARGO
	// de los intervalos por un factor declarado; nunca cambia la base de una operacion ni la
	// quita", y rige "desde el instante del cambio HACIA ADELANTE".
	//
	// LO QUE HACE FALSIFICABLE "HACIA ADELANTE" es evaluar un instante ANTERIOR a la vigencia
	// DESPUES de haber declarado. Los numeros estan elegidos para que los dos perfiles NO contesten
	// lo mismo: 238 dias son NotDue con Standard y DueSoon con SevereDuty.
	//
	// LO QUE ESTE NIVEL AGREGA: el perfil que rige no es un campo, es una DECLARACION journalizada
	// con su instante. Reproducida desde el journal tiene que seguir rigiendo desde el mismo dia --
	// y el perfil con que se CREO la agenda tiene que salir de la semilla, que es el unico lugar
	// donde ese dato existe.
	//
	// SevereDuty = 70% del largo declarado: 365 dias pasan a 256 y 10000 unidades a 7000.
	[TestFixture]
	public sealed class AJournaledUsageProfileTakesEffectGoingForward
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TwelveMonthsInDays = 365;
		private const int TenThousandUnits = 10000;

		private static readonly DateTime Delivery =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
		private const int OdometerAtDelivery = 30000;

		// 2025-06-01 + 238 dias. Con Standard faltan 127 dias del limite; con SevereDuty ya se paso
		// el anticipo de cortesia de 30 sobre un limite de 256.
		private static readonly DateTime BeforeTheProfileTakesEffect =
			new DateTime(2026, 1, 25, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime TheProfileTakesEffect =
			new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

		// 2025-06-01 + 259 dias: pasado el limite escalado de 256, dentro de la gracia de 14.
		private static readonly DateTime AfterTheProfileTookEffect =
			new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");
			SeedTheFluidChange();

			journal.Api.RecordReading(33000, "UnitA", Delivery.AddDays(30), "Telemetry");
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// PRUEBA 14 de §9, la mitad de OINV-15: declarado y todo, antes de su instante rige el
		// perfil anterior.
		[Test]
		public void BeforeItsInstantTheProfileInForceIsStillTheOldOne()
		{
			journal.Api.DeclareUsageProfile("SevereDuty", TheProfileTakesEffect);

			journal.Api.Evaluate(BeforeTheProfileTakesEffect);

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("NotDue"),
				"238 dias de 365 es NotDue. Con el factor de SevereDuty el limite seria 256 y esto "
				+ "seria DueSoon.");
		}

		// PRUEBA 14 de §9, la mitad del escalamiento: desde su instante, los largos se escalan.
		[Test]
		public void FromItsInstantForwardTheDeclaredFactorScalesTheLength()
		{
			journal.Api.DeclareUsageProfile("SevereDuty", TheProfileTakesEffect);

			journal.Api.Evaluate(AfterTheProfileTookEffect);

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("Due"),
				"259 dias contra el limite escalado de 256. Sin escalar serian 259 de 365 y la "
				+ "operacion no venceria hasta junio.");
		}

		// Y la declaracion sobrevive al journal CON SU INSTANTE. Una vigencia que se perdiera al
		// rehidratar haria que el perfil rigiera desde siempre, y con el la agenda re-fecharia
		// hacia atras todo lo que la ley ya decidio.
		[Test]
		public void TheDeclarationKeepsItsInstantAcrossTheJournal()
		{
			journal.Api.DeclareUsageProfile("SevereDuty", TheProfileTakesEffect);

			journal.CloseAndReopenFromTheJournal();

			journal.Api.Evaluate(BeforeTheProfileTakesEffect);

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("NotDue"),
				"Reproducida, la declaracion sigue rigiendo desde el 2026-02-01 y no desde antes.");

			journal.Api.Evaluate(AfterTheProfileTookEffect);

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("Due"));
		}

		// El umbral es INCLUSIVO, con la misma regla que los otros del slice (C16).
		[Test]
		public void TheProfileIsInForceOnTheVeryInstantItTakesEffect()
		{
			journal.Api.DeclareUsageProfile("SevereDuty", AfterTheProfileTookEffect);

			journal.Api.Evaluate(AfterTheProfileTookEffect);

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("Due"),
				"259 dias contra el limite escalado de 256. Con el umbral exclusivo la vigencia "
				+ "empezaria al dia siguiente y esto seria NotDue.");
		}

		// EL PERFIL DE LA CREACION SALE DE LA SEMILLA, y este nivel es el unico que lo prueba: el
		// dato entra por un parametro del upgrade('semilla') y no vuelve a aparecer nunca. Si la
		// semilla lo perdiera -- o lo quemara en el texto -- un vehiculo comercial se mediria con
		// los intervalos de un uso normal, que son mas largos.
		[Test]
		public void TheProfileTheScheduleWasSeededWithRulesUntilOneIsDeclared()
		{
			using (AScheduleOnARealJournal severeFromTheStart =
				new AScheduleOnARealJournal(TheVin, "SevereDuty"))
			{
				severeFromTheStart.Api.SeedPlanOperation(
					"FluidChange", "TimeOrDistance", TwelveMonthsInDays, TenThousandUnits, "UnitA",
					true, false,
					Delivery, OdometerAtDelivery, "UnitA");

				severeFromTheStart.Api.Evaluate(AfterTheProfileTookEffect);

				Assert.That(
					severeFromTheStart.Api.ReadDueState("FluidChange"), Is.EqualTo("Due"),
					"259 dias contra el limite escalado de 256. Con el perfil de la creacion "
					+ "ignorado serian 259 de 365 y no venceria hasta junio.");

				severeFromTheStart.CloseAndReopenFromTheJournal();

				Assert.That(
					severeFromTheStart.Api.ReadDueState("FluidChange"), Is.EqualTo("Due"),
					"Y sigue siendo el suyo despues del replay: la semilla lo journalizo.");
			}
		}

		// "Nunca cambia la BASE": la operacion sigue siendo de dos dimensiones y el motivo sigue
		// nombrando las dos.
		[Test]
		public void TheProfileScalesTheLengthAndLeavesTheBasisWhereItWas()
		{
			journal.Api.DeclareUsageProfile("SevereDuty", TheProfileTakesEffect);
			journal.Api.Evaluate(AfterTheProfileTookEffect);

			string reason = journal.Api.ReadDueReason("FluidChange");

			Assert.That(reason, Does.Contain("tiempo:"));
			Assert.That(reason, Does.Contain("distancia:"),
				"Las dos dimensiones siguen ahi: SevereDuty no volvio otra operacion a esta.");
			Assert.That(reason, Does.Contain("256"),
				"Y el limite que se compara es el ESCALADO, no el declarado.");
		}

		// LowDistance no escala la distancia: la IGNORA (§6).
		[Test]
		public void LowDistanceIgnoresTheDistanceLimitInsteadOfShrinkingIt()
		{
			journal.Api.DeclareUsageProfile("LowDistance", TheProfileTakesEffect);

			journal.Api.RecordReading(45000, "UnitA", AfterTheProfileTookEffect, "Telemetry");
			journal.Api.Evaluate(AfterTheProfileTookEffect);

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("NotDue"),
				"15000 unidades recorridas de un limite de 10000, y la operacion NO vence: con "
				+ "LowDistance la dimension de distancia no se evalua.");
			Assert.That(journal.Api.ReadDueReason("FluidChange"), Does.Contain("de poca distancia"),
				"Y lo dice: OHS-07 exige poder mostrar cual perfil aplico.");
		}

		private void SeedTheFluidChange()
		{
			journal.Api.SeedPlanOperation(
				"FluidChange", "TimeOrDistance", TwelveMonthsInDays, TenThousandUnits, "UnitA",
				true, false,
				Delivery, OdometerAtDelivery, "UnitA");
		}
	}
}
