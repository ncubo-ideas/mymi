using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L2 DE SLICE-TheMaintenanceScreen: los cinco estados de §1.2 se quedan cinco. "No es parte
	// de esta ley" no es "no se pudo medir", y las tres suspensiones nombran su propia causa,
	// porque solo UNA de ellas la arregla alguien. Un booleano aca es el defecto que la ley nombra.
	//
	// SON SIETE Y NO CINCO, y las dos que sobran las midio MS-0:
	//
	//   LA MEDIDA SE PARTE POR EL SIGNO (G2) -- StillAhead y AlreadyPassed son dos respuestas
	//   distinguibles, no una con un numero que cambia de signo. §5.4 del plan lo declara.
	//
	//   Y FALTABA UNA SEPTIMA que ni el brief ni §5.2 del plan tienen: una clase SIN OPERACION
	//   AGENDADA. ReadDueState contesta NotDue ahi -- y puede, porque no agendada de verdad no esta
	//   vencida -- pero un resto no tiene esa salida: un 0 diria "te faltan 0" a un vehiculo que no
	//   tiene ese servicio en el plan, que es justo el defecto que el mensaje de LimitStanding.State
	//   y el /// de BasePoint.Reading ("ausente no es cero") existen para negar. Las seis variantes
	//   de §5.2 mapean los seis tipos instanciables de LimitStanding, y una clase no agendada no
	//   produce NINGUN LimitStanding: no hay ocurrencia sobre la que llamar AssessedAt.
	//
	// E47: un tipo por razon, la razon EN EL NOMBRE DEL TIPO, y la base contesta TOTALMENTE, asi
	// que ningun llamador ramifica sobre el tipo concreto.
	[TestFixture]
	public sealed class AbsentIsNotSuspendedAndSuspendedSaysWhy
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TenThousandUnits = 10000;
		private const int TheOdometerAtDelivery = 30000;
		private const int TheReadingWellInsideTheLimit = 34200;
		private const int TheTimeLimitInDays = 400;

		private static readonly DateTime Delivery = new DateTime(2025, 6, 1);
		private static readonly DateTime TheObservation = new DateTime(2025, 9, 9);
		private static readonly DateTime TheInstantAsked = new DateTime(2026, 5, 1);

		// LAS SIETE, ALCANZADAS UNA POR UNA, y cada una contesta algo DISTINTO de las otras seis.
		// Que sean siete frases distintas es la mitad que un conjunto de cinco no puede dar; que
		// sean siete NOMBRES distintos es la que un booleano no puede dar.
		[Test]
		public void TheSevenOutcomesAreReachedInTurnAndEachSaysSomethingDifferent()
		{
			var reached = new Dictionary<string, string>();

			foreach (KeyValuePair<string, MaintenanceSchedule> each in AScheduleForEachOutcome())
			{
				string standing = each.Value.ReadDistanceRemainderStanding(
					OperationKindCode.FluidChange, TheInstantAsked);

				string said = each.Value.DescribeDistanceRemaining(
					OperationKindCode.FluidChange, TheInstantAsked);

				Assert.That(standing, Is.EqualTo(each.Key),
					"El estado alcanzado no es el que la escena monta.");

				reached[standing] = said;
			}

			Assert.That(reached.Keys.OrderBy(name => name, StringComparer.Ordinal), Is.EqualTo(
				new[]
				{
					"AlreadyPassed", "NoOperationScheduled", "OutsideTheLaw", "StillAhead",
					"SuspendedByAUnitMismatch", "SuspendedByAnAbsentReading",
					"SuspendedByTheUsageProfile",
				}),
				"Faltan estados alcanzables o sobran. Una respuesta de cuatro estados es la falla "
				+ "silenciosa mas probable de esta rebanada (§14 item 4 del brief).");

			Assert.That(reached.Values.Distinct().Count(), Is.EqualTo(7),
				"Dos de los siete dicen la MISMA frase, asi que quien lea la pantalla no los puede "
				+ "distinguir. Los nombres serian una diferencia que nadie ve.");
		}

		// LA DISTINCION QUE UN BOOLEANO DESTRUYE, afirmada sobre las frases y no sobre los nombres:
		// lo que no existe no se puede curar, y lo que falta si.
		[Test]
		public void OutsideTheLawAndCannotSayAreNotTheSameAnswer()
		{
			MaintenanceSchedule outsideTheLaw = AScheduleWhoseLawDoesNotMeasureDistance();
			MaintenanceSchedule cannotSay = AScheduleWithNoReadingAtAll();

			Assert.That(
				outsideTheLaw.CanDistanceRemainingBeSaid(
					OperationKindCode.FluidChange, TheInstantAsked),
				Is.False);
			Assert.That(
				cannotSay.CanDistanceRemainingBeSaid(
					OperationKindCode.FluidChange, TheInstantAsked),
				Is.False,
				"Las dos contestan que no se puede decir, y por eso un booleano no alcanza: es lo "
				+ "unico que las dos comparten.");

			Assert.That(
				outsideTheLaw.DescribeDistanceRemaining(
					OperationKindCode.FluidChange, TheInstantAsked),
				Is.Not.EqualTo(cannotSay.DescribeDistanceRemaining(
					OperationKindCode.FluidChange, TheInstantAsked)));

			Assert.That(
				outsideTheLaw.ReadDistanceRemainderStanding(
					OperationKindCode.FluidChange, TheInstantAsked),
				Is.EqualTo("OutsideTheLaw"));
			Assert.That(
				cannotSay.ReadDistanceRemainderStanding(
					OperationKindCode.FluidChange, TheInstantAsked),
				Is.EqualTo("SuspendedByAnAbsentReading"));
		}

		// LA UNICA DE LAS TRES SUSPENSIONES QUE UNA VISITA ARREGLA, y su frase tiene que decir que
		// lo que falta es la LECTURA -- no el limite, no el perfil. Es la mitad accionable de L2.
		[Test]
		public void TheAbsentReadingIsTheOnlySuspensionAVisitFixes()
		{
			MaintenanceSchedule schedule = AScheduleWithNoReadingAtAll();

			string said = schedule.DescribeDistanceRemaining(
				OperationKindCode.FluidChange, TheInstantAsked);

			Assert.That(said, Does.Contain("lectura"),
				"La frase tiene que nombrar la LECTURA que falta: es lo que la persona puede ir a "
				+ "resolver. Sin eso queda sabiendo que algo fallo y sin saber que arreglar.");

			Assert.That(
				schedule.ReadDistanceRemainderStanding(
					OperationKindCode.FluidChange, TheInstantAsked),
				Is.EqualTo("SuspendedByAnAbsentReading"));

			// Y LAS OTRAS DOS NO SON ESA. Un desajuste de unidades no lo cura una visita al taller,
			// y un perfil declarado tampoco: los tres nombres tienen que separarlas.
			Assert.That(
				AScheduleWhoseReadingIsInTheOtherUnit().ReadDistanceRemainderStanding(
					OperationKindCode.FluidChange, TheInstantAsked),
				Is.EqualTo("SuspendedByAUnitMismatch"));

			Assert.That(
				AScheduleWhoseProfileIgnoresDistance().ReadDistanceRemainderStanding(
					OperationKindCode.FluidChange, TheInstantAsked),
				Is.EqualTo("SuspendedByTheUsageProfile"));
		}

		// LA SEPTIMA, Y POR QUE NO ES UNA SUSPENSION. Una clase sin operacion agendada no tiene
		// dimension suspendida: no tiene dimension. La frase nombra la CLASE, que es lo que la
		// distingue -- y lo que la arregla es la agencia, no una visita ni una lectura.
		[Test]
		public void NoOperationScheduledIsItsOwnAnswerAndNotASuspension()
		{
			MaintenanceSchedule schedule = AScheduleWithNothingSeededForThatKind();

			Assert.That(
				schedule.CanDistanceRemainingBeSaid(
					OperationKindCode.RecallRemedy, TheInstantAsked),
				Is.False);

			Assert.That(
				schedule.ReadDistanceRemainderStanding(
					OperationKindCode.RecallRemedy, TheInstantAsked),
				Is.EqualTo("NoOperationScheduled"));

			string said = schedule.DescribeDistanceRemaining(
				OperationKindCode.RecallRemedy, TheInstantAsked);

			Assert.That(said,
				Does.Contain(MaintenanceOperationKind.RecallRemedy.HowItIsSaid),
				"La frase tiene que nombrar la clase. Sin el nombre, las nueve clases no agendadas "
				+ "dicen la misma frase y nadie sabe de cual habla (C60).");

			// Y NO CONTESTA CERO, que es la salida que ReadDueState si puede tomar y esta no.
			Assert.Throws<DomainException>(
				() => schedule.ReadDistanceRemaining(
					OperationKindCode.RecallRemedy, TheInstantAsked),
				"Un 0 aqui diria 'te faltan 0' a un vehiculo que no tiene ese servicio agendado.");

			Assert.That(
				schedule.ReadDistanceRemainderStanding(
					OperationKindCode.RecallRemedy, TheInstantAsked),
				Is.Not.EqualTo("SuspendedByAnAbsentReading"),
				"No agendada no es una suspension: nadie la cura trayendo una lectura.");
		}

		// LAS DOS RESPUESTAS "NO SE MIDE ASI" NOMBRAN CADA UNA SU DIMENSION, y esto es lo que MS-0
		// encontro: hasta MS-1 ADimensionOutsideTheLaw era UNA instancia compartida por las dos
		// fabricas -- TimeIsNotPartOfThisLaw y DistanceIsNotPartOfThisLaw devolvian la misma -- y su
		// Describe() es la cadena VACIA a proposito, asi que el tipo no tenia con que saber cual de
		// las dos era. Las siete escenas de arriba preguntan todas por la DISTANCIA, asi que un
		// singleton compartido las pasaria todas y mentiria del lado del tiempo. Esta es la unica
		// que lo ve.
		[Test]
		public void EachOutsideTheLawAnswerNamesItsOwnDimension()
		{
			MaintenanceSchedule timeIsNotMeasured = AScheduleWhoseLawDoesNotMeasureTime();
			MaintenanceSchedule distanceIsNotMeasured = AScheduleWhoseLawDoesNotMeasureDistance();

			string aboutTime = timeIsNotMeasured.DescribeDaysRemaining(
				OperationKindCode.FluidChange, TheInstantAsked);
			string aboutDistance = distanceIsNotMeasured.DescribeDistanceRemaining(
				OperationKindCode.FluidChange, TheInstantAsked);

			Assert.That(
				timeIsNotMeasured.ReadTimeRemainderStanding(
					OperationKindCode.FluidChange, TheInstantAsked),
				Is.EqualTo("OutsideTheLaw"));
			Assert.That(
				distanceIsNotMeasured.ReadDistanceRemainderStanding(
					OperationKindCode.FluidChange, TheInstantAsked),
				Is.EqualTo("OutsideTheLaw"));

			Assert.That(aboutTime, Does.StartWith("tiempo:"),
				"La mitad de tiempo que la ley no tiene dice que es del TIEMPO. Con una sola "
				+ "instancia compartida entre las dos fabricas, esta frase habla de la otra "
				+ "dimension y ninguna prueba del lado de la distancia lo ve.");
			Assert.That(aboutDistance, Does.StartWith("distancia:"));

			Assert.That(aboutTime, Is.Not.EqualTo(aboutDistance),
				"Las dos dicen lo mismo, asi que la instancia se volvio a compartir.");
		}

		// LA BASE CONTESTA TOTALMENTE, asi que ningun llamador ramifica sobre el tipo concreto
		// (E47). Las siete escenas responden las cinco preguntas totales sin reventar; solo los
		// dos accesores del monto estan guardados.
		[Test]
		public void EveryVariantAnswersEveryTotalQuery()
		{
			foreach (KeyValuePair<string, MaintenanceSchedule> each in AScheduleForEachOutcome())
			{
				MaintenanceSchedule schedule = each.Value;

				Assert.DoesNotThrow(() =>
				{
					schedule.CanDistanceRemainingBeSaid(
						OperationKindCode.FluidChange, TheInstantAsked);
					schedule.ReadDistanceRemainderStanding(
						OperationKindCode.FluidChange, TheInstantAsked);
					schedule.DescribeDistanceRemaining(
						OperationKindCode.FluidChange, TheInstantAsked);
					schedule.CanDaysRemainingBeSaid(
						OperationKindCode.FluidChange, TheInstantAsked);
					schedule.ReadTimeRemainderStanding(
						OperationKindCode.FluidChange, TheInstantAsked);
					schedule.DescribeDaysRemaining(
						OperationKindCode.FluidChange, TheInstantAsked);
					schedule.DescribeCountdown(
						OperationKindCode.FluidChange, TheInstantAsked);
				}, each.Key + " revento una pregunta TOTAL.");

				Assert.That(
					schedule.DescribeDistanceRemaining(
						OperationKindCode.FluidChange, TheInstantAsked),
					Is.Not.Empty,
					each.Key + " contesta la frase vacia. ADimensionOutsideTheLaw.Describe() SI "
					+ "devuelve la cadena vacia a proposito -- una dimension que la ley no tiene no "
					+ "se nombra en el motivo -- y por eso el resto compone la suya en vez de "
					+ "reusarla ahi.");
			}
		}

		// LAS SIETE ESCENAS. Una agenda por estado alcanzable, todas preguntadas por FluidChange
		// salvo la septima, que es la clase que nadie sembro.
		private static Dictionary<string, MaintenanceSchedule> AScheduleForEachOutcome()
		{
			return new Dictionary<string, MaintenanceSchedule>
			{
				{ "StillAhead", AScheduleWellInsideItsLimit() },
				{ "AlreadyPassed", AScheduleAlreadyPastItsLimit() },
				{ "OutsideTheLaw", AScheduleWhoseLawDoesNotMeasureDistance() },
				{ "SuspendedByAnAbsentReading", AScheduleWithNoReadingAtAll() },
				{ "SuspendedByAUnitMismatch", AScheduleWhoseReadingIsInTheOtherUnit() },
				{ "SuspendedByTheUsageProfile", AScheduleWhoseProfileIgnoresDistance() },
				{ "NoOperationScheduled", AScheduleThatNeverSeededFluidChange() },
			};
		}

		private static MaintenanceSchedule AScheduleWellInsideItsLimit()
		{
			MaintenanceSchedule schedule = ASchedule(UsageProfileCode.Standard);

			SeedFluidChange(schedule, IntervalBasisCode.TimeAndDistance);
			schedule.RecordReading(
				TheReadingWellInsideTheLimit, DistanceUnitCode.UnitA, TheObservation,
				ReadingSourceCode.Telemetry);

			return schedule;
		}

		private static MaintenanceSchedule AScheduleAlreadyPastItsLimit()
		{
			MaintenanceSchedule schedule = ASchedule(UsageProfileCode.Standard);

			SeedFluidChange(schedule, IntervalBasisCode.TimeAndDistance);
			schedule.RecordReading(
				TheOdometerAtDelivery + TenThousandUnits + 300, DistanceUnitCode.UnitA,
				TheObservation, ReadingSourceCode.Telemetry);

			return schedule;
		}

		private static MaintenanceSchedule AScheduleWhoseLawDoesNotMeasureDistance()
		{
			MaintenanceSchedule schedule = ASchedule(UsageProfileCode.Standard);

			SeedFluidChange(schedule, IntervalBasisCode.TimeOnly);
			schedule.RecordReading(
				TheReadingWellInsideTheLimit, DistanceUnitCode.UnitA, TheObservation,
				ReadingSourceCode.Telemetry);

			return schedule;
		}

		private static MaintenanceSchedule AScheduleWhoseLawDoesNotMeasureTime()
		{
			MaintenanceSchedule schedule = ASchedule(UsageProfileCode.Standard);

			SeedFluidChange(schedule, IntervalBasisCode.DistanceOnly);
			schedule.RecordReading(
				TheReadingWellInsideTheLimit, DistanceUnitCode.UnitA, TheObservation,
				ReadingSourceCode.Telemetry);

			return schedule;
		}

		private static MaintenanceSchedule AScheduleWithNoReadingAtAll()
		{
			MaintenanceSchedule schedule = ASchedule(UsageProfileCode.Standard);

			SeedFluidChange(schedule, IntervalBasisCode.TimeAndDistance);

			return schedule;
		}

		private static MaintenanceSchedule AScheduleWhoseReadingIsInTheOtherUnit()
		{
			MaintenanceSchedule schedule = ASchedule(UsageProfileCode.Standard);

			SeedFluidChange(schedule, IntervalBasisCode.TimeAndDistance);
			schedule.RecordReading(
				TheReadingWellInsideTheLimit, DistanceUnitCode.UnitB, TheObservation,
				ReadingSourceCode.Telemetry);

			return schedule;
		}

		private static MaintenanceSchedule AScheduleWhoseProfileIgnoresDistance()
		{
			MaintenanceSchedule schedule = ASchedule(UsageProfileCode.LowDistance);

			SeedFluidChange(schedule, IntervalBasisCode.TimeAndDistance);
			schedule.RecordReading(
				TheReadingWellInsideTheLimit, DistanceUnitCode.UnitA, TheObservation,
				ReadingSourceCode.Telemetry);

			return schedule;
		}

		private static MaintenanceSchedule AScheduleThatNeverSeededFluidChange()
		{
			return ASchedule(UsageProfileCode.Standard);
		}

		private static MaintenanceSchedule AScheduleWithNothingSeededForThatKind()
		{
			MaintenanceSchedule schedule = ASchedule(UsageProfileCode.Standard);

			SeedFluidChange(schedule, IntervalBasisCode.TimeAndDistance);
			schedule.RecordReading(
				TheReadingWellInsideTheLimit, DistanceUnitCode.UnitA, TheObservation,
				ReadingSourceCode.Telemetry);

			return schedule;
		}

		private static MaintenanceSchedule ASchedule(UsageProfileCode profile)
		{
			return new MaintenanceSchedule(TheVin, profile);
		}

		// LA BASE NO SE INFIERE y el dominio lo rechaza: una TimeOnly con limite de distancia es un
		// defecto de quien declara (IntervalRule.BasisIsNeverInferred). Asi que el limite que sobra
		// se manda en cero, que es como esa ley dice "esta mitad no existe".
		private static void SeedFluidChange(
			MaintenanceSchedule schedule, IntervalBasisCode basis)
		{
			schedule.SeedPlanOperation(
				OperationKindCode.FluidChange, basis,
				basis == IntervalBasisCode.DistanceOnly ? 0 : TheTimeLimitInDays,
				basis == IntervalBasisCode.TimeOnly ? 0 : TenThousandUnits,
				DistanceUnitCode.UnitA,
				true, false,
				Delivery, TheOdometerAtDelivery, DistanceUnitCode.UnitA);
		}
	}
}
