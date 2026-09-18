using System;
using Concesionario.App;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LAS PRUEBAS 1, 2, 3, 4, 6 y 7 de §9 EN EL NIVEL B: las mismas afirmaciones de negocio, de
	// punta a punta contra un actor journalizado, entrando por DealershipCarAPI.
	//
	// NO ES REPETICION DEL NIVEL A. Lo que se prueba aca no existe una capa mas abajo: que
	// SeedPlanOperation ate diez parametros al metodo correcto, que tres codigos crucen como NOMBRE
	// y lleguen al miembro que nombran, que el instante se normalice sin cambiar de dia, y que
	// ReadDueState y ReadDueReason vuelvan tipados por la puerta. Cualquiera de esas cosas se puede
	// romper con el dominio intacto -- y ninguna se descubre al compilar (SPIKES-OS2 S5).
	//
	// Intervalo: 12 meses o 10000 unidades, anclado el 2025-06-01 en 30000 UnitA. Vence el
	// 2026-06-01 o en 40000, lo que llegue primero (ley L1).
	[TestFixture]
	public sealed class AJournaledScheduleDecidesByTheFirstLimitToArrive
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TwelveMonthsInDays = 365;
		private const int TenThousandUnits = 10000;

		private static readonly DateTime Delivery =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
		private const int OdometerAtDelivery = 30000;

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");
			SeedTheFluidChange("UnitA", false);
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// PRUEBA 1 de §9: 39000 el 2026-01-15 -> DueSoon, y el motivo nombra la DISTANCIA.
		[Test]
		public void TheCourtesyWarningArrivesByDistanceWhenDistanceIsAhead()
		{
			ReadingOf(39000, TheDay(2026, 1, 15));
			journal.Api.Evaluate(TheDay(2026, 1, 15));

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("DueSoon"));
			Assert.That(
				journal.Api.ReadDueReason("FluidChange",
					TheLanguage.OfTheRecord), Does.Contain("decidido por distancia"),
				"9000 de 10000 esta dentro del anticipo de 1000; 228 dias de 365 no.");
		}

		// PRUEBA 2 de §9: 33000 el 2026-05-02 -> DueSoon, y el motivo nombra el TIEMPO.
		[Test]
		public void TheCourtesyWarningArrivesByTimeWhenTimeIsAhead()
		{
			ReadingOf(33000, TheDay(2026, 5, 2));
			journal.Api.Evaluate(TheDay(2026, 5, 2));

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("DueSoon"));
			Assert.That(
				journal.Api.ReadDueReason("FluidChange",
					TheLanguage.OfTheRecord), Does.Contain("decidido por tiempo"),
				"335 dias de 365 esta dentro del anticipo de 30; 3000 de 10000 no. Ninguna de las "
				+ "dos dimensiones domina (OINV-20).");
		}

		// PRUEBA 3 de §9: el limite es INCLUSIVO, y se mira desde los dos lados del borde.
		[Test]
		public void TheLimitValueIsInclusiveSeenFromBothSidesOfTheEdge()
		{
			ReadingOf(39999, TheDay(2026, 1, 19));
			journal.Api.Evaluate(TheDay(2026, 1, 19));

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("DueSoon"),
				"Una unidad antes del limite todavia es aviso.");

			ReadingOf(40000, TheDay(2026, 1, 20));
			journal.Api.Evaluate(TheDay(2026, 1, 20));

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("Due"),
				"Y alcanzar el limite es entrar en su estado (C16): 40000 vence, no 40001.");
		}

		// PRUEBA 4 de §9: las dos dimensiones llegan el mismo dia -> Due, UNA transicion, y el
		// motivo nombra las DOS.
		[Test]
		public void WhenBothLimitsArriveTogetherTheReasonNamesThemBoth()
		{
			DateTime bothArrive = Delivery.AddDays(TwelveMonthsInDays);

			ReadingOf(40000, bothArrive);
			EvaluationOutcome moved = journal.Api.Evaluate(bothArrive);

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("Due"));
			Assert.That(moved.TransitionCount, Is.EqualTo(1),
				"UNA transicion, no dos. Y llega hasta aca por un Out tipado: si el conteo se "
				+ "perdiera en la puerta, la caratula no tendria como saber que aviso dar.");
			Assert.That(
				journal.Api.ReadDueReason("FluidChange", TheLanguage.OfTheRecord),
				Does.Contain("decidido por tiempo y distancia"),
				"Empatadas deciden las dos, y decirlo importa.");
		}

		// PRUEBA 6 de §9, la ley L10: el aviso de cortesia NUNCA es una parada obligatoria.
		[Test]
		public void TheOperationGoesStraightToDueWithoutStoppingAtTheCourtesyWarning()
		{
			ReadingOf(40000, TheDay(2026, 1, 20));
			EvaluationOutcome moved = journal.Api.Evaluate(TheDay(2026, 1, 20));

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("Due"),
				"Nunca paso por DueSoon: la primera evaluacion la encuentra ya vencida.");
			Assert.That(moved.TransitionCount, Is.EqualTo(1),
				"Y es UNA transicion: si caminara el diagrama, el host recibiria dos avisos por un "
				+ "solo hecho (OEX-06).");
			Assert.That(moved.AnythingBecameDue, Is.True);
			Assert.That(moved.AnythingBecameOverdue, Is.False);
		}

		// PRUEBA 7 de §9, la ley L7: el limite esta en UnitA y llega una lectura en UnitB. NO se
		// hace ninguna comparacion de distancia, la operacion cae a solo-tiempo, y se DICE.
		//
		// EN ESTE NIVEL LA PRUEBA GANA UNA MITAD: la unidad de la lectura cruza como nombre por un
		// parametro propio. Si la puerta la fijara -- o mandara la del limite -- 40000 en UnitB se
		// leerian como UnitA, la operacion venceria, y el dominio no tendria como notarlo porque le
		// habrian dicho la unidad equivocada con todas las letras.
		[Test]
		public void AReadingInTheOtherUnitSuspendsTheDistanceInsteadOfBeingConverted()
		{
			journal.Api.RecordReading(40000, "UnitB", TheDay(2026, 1, 20), "Telemetry");
			journal.Api.Evaluate(TheDay(2026, 1, 20));

			Assert.That(journal.Api.ReadDueState("FluidChange"), Is.EqualTo("NotDue"),
				"OINV-21: si alguien hubiera convertido, 40000 seria el limite y esto seria Due.");
			Assert.That(
				journal.Api.ReadDueReason("FluidChange", TheLanguage.OfTheRecord),
				Does.Contain("las unidades nunca se convierten"),
				"Y el motivo lo dice con todas las letras: ese NotDue no es evidencia de nada "
				+ "(GAP G2).");
		}

		// EL ANCLA LLEVA SU PROPIA UNIDAD (C18), y la siembra manda DOS codigos de unidad por el
		// mismo comando: el del LIMITE y el de la LECTURA del ancla. Nada mas en la agenda los
		// distingue, asi que si la puerta prestara el del limite para los dos, todas las demas
		// pruebas seguirian verdes -- porque en todas los dos coinciden.
		//
		// Aca no coinciden: limite en UnitA, ancla en UnitB. La distancia queda suspendida desde
		// que la operacion nace, y eso es la ley L7 funcionando y no un defecto.
		[Test]
		public void TheAnchorCarriesItsOwnUnitAndNotTheLimits()
		{
			journal.Api.SeedPlanOperation(
				"FilterChange", "TimeOrDistance", TwelveMonthsInDays, TenThousandUnits, "UnitA",
				true, false,
				Delivery, OdometerAtDelivery, "UnitB");

			ReadingOf(39000, TheDay(2026, 1, 15));
			journal.Api.Evaluate(TheDay(2026, 1, 15));

			Assert.That(
				journal.Api.ReadDueReason("FilterChange", TheLanguage.OfTheRecord),
				Does.Contain("las unidades nunca se convierten"),
				"El ancla se tomo en UnitB y el limite esta en UnitA. Si la puerta hubiera prestado "
				+ "la unidad del limite, la agenda estaria midiendo 9000 unidades desde una lectura "
				+ "que nadie tomo en esa escala.");
			Assert.That(
				journal.Api.ReadDueReason("FluidChange",
					TheLanguage.OfTheRecord), Does.Contain("decidido por distancia"),
				"Y la MISMA siembra con las dos unidades iguales si mide: es lo que hace que esta "
				+ "prueba este mirando la unidad del ancla y no otra cosa.");
		}

		// La otra mitad de lo que solo se ve en este nivel: un nombre que no nombra ningun miembro
		// se rechaza EN LA PUERTA, antes de que el motor escriba nada. Si llegara al script,
		// reventaria al ejecutarse y dejaria una entrada permanente que hace loguear un fallo de
		// rehidratacion en cada arranque futuro (SPIKES-OS2 S5).
		[Test]
		public void ACodeThatNamesNoMemberIsRefusedAtTheDoorAndNotInTheJournal()
		{
			Assert.Throws<ArgumentException>(
				() => journal.Api.ReadDueState("CambioDeAceite"));
			Assert.Throws<ArgumentException>(
				() => journal.Api.RecordReading(1, "Millas", TheDay(2026, 1, 20), "Telemetry"));
			Assert.Throws<ArgumentException>(
				() => journal.Api.DeferOperation("FluidChange", "PorqueSi", TheDay(2026, 1, 20)));
			Assert.Throws<ArgumentException>(
				() => journal.Api.ReadDueState("fluidchange"),
				"Ni siquiera con otra caja: el canon manda en los nombres, y el journal guarda la "
				+ "grafia que se le mande.");

			journal.CloseAndReopenFromTheJournal();

			Assert.That(journal.Api.ReadVin(), Is.EqualTo(TheVin),
				"Y el journal sigue rehidratando limpio, que es lo que la entrada envenenada "
				+ "habria costado.");
		}

		// UN CODIGO AUSENTE O EN BLANCO ES OTRA COSA que un codigo que no existe, y la puerta tiene
		// que decir CUAL parametro fue. Una caratula con un desplegable sin elegir manda cadena
		// vacia, no "Chorizo".
		//
		// ESTA PRUEBA EXISTE POR LA FALSIFICACION: quitar la guarda de nulo-o-blanco de WireName
		// dejaba las 360 pruebas en verde. Nada mandaba nulo ni blanco, asi que nadie veia que sin
		// ella un nulo sale como una ArgumentNullException del BCL que culpa a 'value' -- un
		// parametro de un ayudante privado que quien llama no escribio nunca y no puede encontrar.
		[Test]
		public void ACodeThatIsMissingOrBlankIsRefusedAtTheDoorAndSaysWhichOne()
		{
			ArgumentException missing = Assert.Throws<ArgumentNullException>(
				() => journal.Api.ReadDueState(null));

			Assert.That(missing.ParamName, Is.EqualTo("kind"),
				"Culpar al parametro correcto es toda la diferencia entre un mensaje que se puede "
				+ "arreglar y uno que hay que depurar.");

			ArgumentException blank = Assert.Throws<ArgumentException>(
				() => journal.Api.RecordReading(1, "   ", TheDay(2026, 1, 20), "Telemetry"));

			Assert.That(blank.ParamName, Is.EqualTo("unit"));

			ArgumentException noSource = Assert.Throws<ArgumentNullException>(
				() => journal.Api.RecordReading(1, "UnitA", TheDay(2026, 1, 20), null));

			Assert.That(noSource.ParamName, Is.EqualTo("source"));
		}

		private static DateTime TheDay(int year, int month, int day)
		{
			return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
		}

		private void SeedTheFluidChange(string limitUnit, bool isSafety)
		{
			journal.Api.SeedPlanOperation(
				"FluidChange", "TimeOrDistance", TwelveMonthsInDays, TenThousandUnits, limitUnit,
				true, isSafety,
				Delivery, OdometerAtDelivery, "UnitA");
		}

		private void ReadingOf(int value, DateTime observedAt)
		{
			journal.Api.RecordReading(value, "UnitA", observedAt, "Telemetry");
		}
	}
}
