using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// PRUEBA 13 de §9 EN EL NIVEL B (ley L9, OINV-31): "la persona agrega y quita SUS operaciones;
	// una del plan del fabricante se puede aplazar o declinar, pero nunca agregar, quitar ni
	// re-fechar".
	//
	// LA POLARIDAD ES LA DE E32 y llega intacta hasta la puerta: quitar una del plan es un HECHO
	// del negocio -- la app le muestra un boton a la persona y la ley contesta que no -- asi que
	// DealershipCarAPI no revienta. Reventar obligaria a la caratula a usar excepciones para el
	// flujo normal de alguien que toco lo que no era.
	//
	// COMO SE VE QUE SE QUITO, sin que cruce ninguna coleccion (§8): por el MOTIVO. Es la unica
	// lectura que distingue "no hay nada de esa clase" de "hay una y esto es lo que la ley dijo".
	[TestFixture]
	public sealed class AJournaledOwnerEditsOnlyTheirOwnOperations
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TwelveMonthsInDays = 365;
		private const int TenThousandUnits = 10000;

		private static readonly DateTime Delivery =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
		private const int OdometerAtDelivery = 30000;

		private static readonly DateTime TheLimitIsReached =
			new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.SeedPlanOperation(
				"FluidChange", "TimeOrDistance", TwelveMonthsInDays, TenThousandUnits, "UnitA",
				true, false,
				Delivery, OdometerAtDelivery, "UnitA");
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// PRUEBA 13 de §9, la mitad que se puede: sobre una clase que agrego la persona.
		//
		// LAS CUENTAS DESPUES DE LA REMOCION NO SON DECORACION: en el nivel A la falsificacion
		// encontro que nadie contaba nada despues de quitar, y una clase quitada a medias dejaba a
		// la siguiente lectura reventando con una clave que ya no existia.
		[Test]
		public void AnOwnerAddedOperationIsTheOwnersToRemove()
		{
			AddTheOwnersDiagnostic();
			journal.Api.Evaluate(Delivery.AddDays(TwelveMonthsInDays));

			Assert.That(journal.Api.ReadDueReason("Diagnostic",
				TheLanguage.OfTheRecord), Does.Not.Contain("no tiene agendada"),
				"Primero tiene que estar: si no, la remocion no prueba nada.");
			Assert.That(journal.Api.CountDue(), Is.EqualTo(2));

			journal.Api.RemoveOwnerOperation("Diagnostic");

			Assert.That(journal.Api.ReadDueReason("Diagnostic",
				TheLanguage.OfTheRecord), Does.Contain("no tiene agendada"));
			Assert.That(journal.Api.CountDue(), Is.EqualTo(1),
				"Y la agenda sigue contestando: quitar una operacion la saca del conteo, no deja a "
				+ "la agenda apuntando a una clase que ya no tiene.");
			Assert.That(journal.Api.HasAnythingDue(), Is.True);
			Assert.That(journal.Api.MissedCount(), Is.EqualTo(0));
			Assert.That(journal.Api.ReadNextDueKind(), Is.EqualTo("FluidChange"));
		}

		// Y la remocion sobrevive al journal: reproducida, la clase sigue sin estar. Un
		// RemoveOwnerOperation que no se journalizara devolveria la operacion al proximo arranque y
		// la persona veria reaparecer un recordatorio que ya habia quitado.
		[Test]
		public void TheRemovalSurvivesTheJournal()
		{
			AddTheOwnersDiagnostic();
			journal.Api.RemoveOwnerOperation("Diagnostic");

			journal.CloseAndReopenFromTheJournal();

			Assert.That(journal.Api.ReadDueReason("Diagnostic",
				TheLanguage.OfTheRecord), Does.Contain("no tiene agendada"));
			Assert.That(journal.Api.ReadDueState("Diagnostic"), Is.EqualTo("NotDue"));
		}

		// PRUEBA 13 de §9, la otra mitad: sobre una clase del plan, se rechaza.
		[Test]
		public void APlanOperationIsNotTheOwnersToRemove()
		{
			ReachTheLimit();

			journal.Api.RemoveOwnerOperation("FluidChange");

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("Due"),
				"OINV-31: la operacion del fabricante sigue ahi, y sigue vencida. Si se hubiera "
				+ "ido, nadie volveria a avisarle a la persona.");
			Assert.That(journal.Api.CountDue(), Is.EqualTo(1));
		}

		[Test]
		public void ARefusedRemovalIsAStateAndNotAnException()
		{
			Assert.DoesNotThrow(() => journal.Api.RemoveOwnerOperation("FluidChange"),
				"La columna de rechazo de OINV-31 es un hecho del negocio (RemovalRefused / "
				+ "NotOwnerAdded), no un defecto de quien llama.");
		}

		// La otra prohibicion literal de OINV-31: la persona nunca AGREGA una operacion del plan.
		[Test]
		public void TheOwnerCannotDisplaceAPlanOperationWithTheirOwn()
		{
			ReachTheLimit();

			journal.Api.AddOwnerOperation(
				"FluidChange", "TimeOnly", 30, 0, "UnitA",
				TheLimitIsReached, 40000, "UnitA");

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("Due"),
				"La operacion del plan quedo intacta. Si la de la persona la hubiera sustituido, un "
				+ "limite de 30 dias puesto por ella habria reemplazado al del fabricante -- "
				+ "exactamente 're-fechar una operacion del plan'.");
		}

		// Lo suyo si lo puede reemplazar por lo suyo: es SU lista, y la ley L5 vale igual.
		[Test]
		public void TheOwnerCanReplaceTheirOwnOperationWithAnother()
		{
			AddTheOwnersDiagnostic();

			journal.Api.AddOwnerOperation(
				"Diagnostic", "TimeOnly", 30, 0, "UnitA",
				TheLimitIsReached, 40000, "UnitA");

			journal.Api.Evaluate(TheLimitIsReached.AddDays(31));

			Assert.That(journal.Api.ReadDueState("Diagnostic"), Is.EqualTo("Due"),
				"El limite que rige es el de la SEGUNDA: 30 dias desde 2026-01-20.");
			Assert.That(journal.Api.CountDue(), Is.EqualTo(1),
				"Y hay UNA sola, no dos.");
		}

		// Una operacion que la persona pidio no viene clasificada de seguridad, y la firma de la
		// puerta ni siquiera lleva esa bandera: no hay por donde marcarla.
		[Test]
		public void AnOwnerAddedOperationCanAlwaysBeDeferred()
		{
			AddTheOwnersDiagnostic();
			journal.Api.Evaluate(Delivery.AddDays(TwelveMonthsInDays));

			Assert.That(journal.Api.ReadDueState("Diagnostic"), Is.EqualTo("Due"));

			journal.Api.DeferOperation(
				"Diagnostic", "Timing", Delivery.AddDays(TwelveMonthsInDays));

			Assert.That(journal.Api.ReadDueState("Diagnostic"), Is.EqualTo("Deferred"),
				"Si la puerta de la persona pudiera marcar seguridad, esto seria un rechazo "
				+ "(OINV-52) y la persona se habria quitado a si misma el derecho a aplazar.");
		}

		[Test]
		public void RemovingAKindThatIsNotScheduledDoesNothing()
		{
			Assert.DoesNotThrow(() => journal.Api.RemoveOwnerOperation("RecallRemedy"));
		}

		private void AddTheOwnersDiagnostic()
		{
			journal.Api.AddOwnerOperation(
				"Diagnostic", "TimeOnly", TwelveMonthsInDays, 0, "UnitA",
				Delivery, OdometerAtDelivery, "UnitA");
		}

		private void ReachTheLimit()
		{
			journal.Api.RecordReading(40000, "UnitA", TheLimitIsReached, "Telemetry");
			journal.Api.Evaluate(TheLimitIsReached);
		}
	}
}
