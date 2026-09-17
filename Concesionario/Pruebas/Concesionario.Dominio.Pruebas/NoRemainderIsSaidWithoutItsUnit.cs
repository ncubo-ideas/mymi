using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LEY L3 DE SLICE-TheMaintenanceScreen: la unidad viaja CON el valor, como en toda lectura de
	// este modulo, y se dice por NOMBRE y nunca como codigo -- la proyeccion del codigo se queda
	// afuera de la puerta (DEC-24).
	//
	// C67 ES POR QUE LA SEGUNDA MITAD NO ES DECORATIVA. Las tres unidades del modulo se dicen DOS
	// VECES: UnitA a la maquina y km a la persona. El wire y el journal siguen llevando el nombre
	// del MIEMBRO, y moverlos de ahi deja de rehidratar todo journal ya escrito. Asi que un resto
	// que dijera "1200 UnitA" no estaria mal escrito: estaria proyectando el vocabulario equivocado
	// de los dos que existen, y es el unico lugar del modulo donde el nombre proyectado no es el
	// del miembro.
	//
	// G1 ENTRA EN ESTE FIXTURE porque es la misma pregunta una vez mas: la cuenta atras dice LAS
	// DOS dimensiones y no elige. IsMoreAdvancedThan e IsAsAdvancedAs ya existen y rankean para el
	// ESTADO, no para la frase; §13 G1 del brief dice que la tentacion de reusarlas para mostrar es
	// real y que no.
	[TestFixture]
	public sealed class NoRemainderIsSaidWithoutItsUnit
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TenThousandUnits = 10000;
		private const int TheOdometerAtDelivery = 30000;
		private const int TheReadingWellInsideTheLimit = 38800;
		private const int TheAmountLeft = 1200;

		// El mismo monto tal como lo escribe el renderizador del idioma del registro: el separador
		// de miles se fue con la redaccion y una cifra se escribe en un solo lugar.
		private const string TheAmountLeftAsTheScreenWritesIt = "1.200";
		private const int TheTimeLimitInDays = 400;
		private const int TheDaysElapsedWhenAsked = 334;
		private const int TheDaysLeft = 66;

		private static readonly DateTime Delivery = new DateTime(2025, 6, 1);
		private static readonly DateTime TheObservation = new DateTime(2025, 9, 9);

		private static DateTime TheInstantAsked
		{
			get { return Delivery.AddDays(TheDaysElapsedWhenAsked); }
		}

		// EL MONTO Y SU UNIDAD, LOS DOS EN LA FRASE. Sin la unidad, 1200 no dice nada: el mismo
		// numero en la otra unidad es otra distancia, y este modulo no las convierte.
		[Test]
		public void EverySaidRemainderNamesItsUnit()
		{
			MaintenanceSchedule schedule = AScheduleMeasuredInBothDimensions();

			Assert.That(
				schedule.CanDistanceRemainingBeSaid(
					OperationKindCode.FluidChange, TheInstantAsked),
				Is.True);

			Assert.That(
				schedule.ReadDistanceRemainingUnit(
					OperationKindCode.FluidChange, TheInstantAsked),
				Is.EqualTo(DistanceUnit.UnitA.Name));

			string said = TheSentenceTheDoorWouldCompose.TheDistanceRemainder(
					schedule, OperationKindCode.FluidChange, TheInstantAsked, TheLanguage.OfTheRecord);

			Assert.That(said, Does.Contain(TheAmountLeftAsTheScreenWritesIt));
			Assert.That(said, Does.Contain(DistanceUnit.UnitA.Name),
				"El monto sin su unidad es un numero suelto, y el mismo numero en la otra unidad es "
				+ "otra distancia.");

			// LA MITAD DE TIEMPO TAMBIEN, y su unidad son los dias -- que si se pluralizan.
			string saidInDays = TheSentenceTheDoorWouldCompose.TheTimeRemainder(
					schedule, OperationKindCode.FluidChange, TheInstantAsked, TheLanguage.OfTheRecord);

			Assert.That(saidInDays, Does.Contain(TheDaysLeft.ToString()));
			Assert.That(saidInDays, Does.Contain("días"));
		}

		// Y SE DICE POR NOMBRE: ni la frase ni el accesor sacan el nombre del MIEMBRO, que es lo
		// que el wire lleva y lo que el journal guarda.
		[Test]
		public void TheUnitIsSaidByNameAndNeverAsACode()
		{
			MaintenanceSchedule schedule = AScheduleMeasuredInBothDimensions();

			foreach (string said in new[]
			{
				schedule.ReadDistanceRemainingUnit(
					OperationKindCode.FluidChange, TheInstantAsked),
				TheSentenceTheDoorWouldCompose.TheDistanceRemainder(
					schedule, OperationKindCode.FluidChange, TheInstantAsked, TheLanguage.OfTheRecord),
				TheSentenceTheDoorWouldCompose.TheTimeRemainder(
					schedule, OperationKindCode.FluidChange, TheInstantAsked, TheLanguage.OfTheRecord),
				TheSentenceTheDoorWouldCompose.TheCountdown(
					schedule, OperationKindCode.FluidChange, TheInstantAsked, TheLanguage.OfTheRecord),
			})
			{
				Assert.That(said, Does.Not.Contain("UnitA"),
					"Se proyecto el nombre del miembro. La persona lee esta palabra en su pantalla "
					+ "y no lee 'UnitA'.");
				Assert.That(said, Does.Not.Contain("UnitB"));
			}
		}

		// LA UNIDAD ESTA GUARDADA CUANDO NO HAY MONTO. Devolver una unidad para una mitad que nadie
		// midio se leeria como que hubo medicion.
		[Test]
		public void TheUnitIsGuardedWhereNothingCouldBeMeasured()
		{
			MaintenanceSchedule schedule = AScheduleWithNoReadingAtAll();

			Assert.That(
				schedule.CanDistanceRemainingBeSaid(
					OperationKindCode.FluidChange, TheInstantAsked),
				Is.False);

			Assert.Throws<DomainException>(
				() => schedule.ReadDistanceRemainingUnit(
					OperationKindCode.FluidChange, TheInstantAsked));
		}

		// G1: LAS DOS, SIEMPRE, Y NUNCA UNA ELEGIDA. El "o" de la peticion original -- "1.200 km o
		// en 35 dias" -- ES la respuesta.
		[Test]
		public void TheCountdownSaysBothDimensionsAndNeverPicks()
		{
			MaintenanceSchedule schedule = AScheduleMeasuredInBothDimensions();

			string countdown = TheSentenceTheDoorWouldCompose.TheCountdown(
					schedule, OperationKindCode.FluidChange, TheInstantAsked, TheLanguage.OfTheRecord);

			Assert.That(countdown, Does.Contain(TheAmountLeftAsTheScreenWritesIt));
			Assert.That(countdown, Does.Contain(DistanceUnit.UnitA.Name));
			Assert.That(countdown, Does.Contain(TheDaysLeft.ToString()));
			Assert.That(countdown, Does.Contain("días"),
				"La cuenta atras dice las dos mitades. Elegir la mas cercana dejaria el numero de la "
				+ "pantalla discrepando del motivo del estado para quien lea los dos juntos.");
		}

		// Y SIGUE DICIENDO LAS DOS CUANDO UNA NO SE PUEDE DECIR: es donde una implementacion que
		// "elige la que hay" se vuelve invisible, porque la frase se lee bien.
		[Test]
		public void TheCountdownStillSaysBothWhenOneHalfCannotBeSaid()
		{
			MaintenanceSchedule schedule = AScheduleWithNoReadingAtAll();

			string countdown = TheSentenceTheDoorWouldCompose.TheCountdown(
					schedule, OperationKindCode.FluidChange, TheInstantAsked, TheLanguage.OfTheRecord);

			Assert.That(countdown, Does.Contain("lectura"),
				"La mitad que no se pudo medir dice POR QUE, dentro de la misma frase.");
			Assert.That(countdown, Does.Contain(TheDaysLeft.ToString()),
				"Y la que si se pudo medir sigue estando. Callarla porque la otra falto seria hacer "
				+ "de una mitad la precondicion de la otra (E48).");
		}

		// LA CUENTA ATRAS DE UNA CLASE NO AGENDADA LO DICE UNA SOLA VEZ. Sin esto, las dos mitades
		// repetirian la misma frase en una linea.
		[Test]
		public void TheCountdownOfAKindWithNoOperationSaysItOnce()
		{
			MaintenanceSchedule schedule = AScheduleMeasuredInBothDimensions();

			string countdown = TheSentenceTheDoorWouldCompose.TheCountdown(
					schedule, OperationKindCode.RecallRemedy, TheInstantAsked, TheLanguage.OfTheRecord);

			Assert.That(countdown,
				Is.EqualTo(TheSentenceTheDoorWouldCompose.TheDistanceRemainder(
					schedule, OperationKindCode.RecallRemedy, TheInstantAsked, TheLanguage.OfTheRecord)),
				"No hay dos mitades que decir cuando no hay operacion: decirlo dos veces es la "
				+ "duplicacion de §6 de E48 en una sola linea.");
		}

		private static MaintenanceSchedule AScheduleMeasuredInBothDimensions()
		{
			MaintenanceSchedule schedule = ASeededSchedule();

			schedule.RecordReading(
				TheReadingWellInsideTheLimit, DistanceUnitCode.UnitA, TheObservation,
				ReadingSourceCode.Telemetry);

			return schedule;
		}

		private static MaintenanceSchedule AScheduleWithNoReadingAtAll()
		{
			return ASeededSchedule();
		}

		private static MaintenanceSchedule ASeededSchedule()
		{
			MaintenanceSchedule schedule =
				new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);

			schedule.SeedPlanOperation(
				OperationKindCode.FluidChange, IntervalBasisCode.TimeAndDistance,
				TheTimeLimitInDays, TenThousandUnits, DistanceUnitCode.UnitA,
				true, false,
				Delivery, TheOdometerAtDelivery, DistanceUnitCode.UnitA);

			return schedule;
		}
	}
}
