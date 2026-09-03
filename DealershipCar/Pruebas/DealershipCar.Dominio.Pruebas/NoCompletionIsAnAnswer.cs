using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L7 DE SLICE-TheMaintenanceScreen: una clase que nunca se completo contesta "no hay ninguna
	// completacion registrada", NOMBRANDO la clase. No una lista vacia sin explicacion, y no cero.
	//
	// LA AUSENCIA TIENE DOS RAZONES Y CADA UNA ES SU PROPIO TIPO (E47). "Esta agendada y nunca se
	// completo" es curable por una visita al taller; "no esta agendada" necesita que alguien siembre
	// la operacion, y ninguna visita produce una fila hasta entonces. Un solo "no hay ninguna" para
	// las dos juntaria una ausencia curable con una que no lo es -- y en la ruta 3 de MS-0 §3 la
	// primera de las dos frases es directamente falsa, que es el limite que
	// TheChainIsTheHistoryAndItIsNotCopied pincha.
	[TestFixture]
	public sealed class NoCompletionIsAnAnswer
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TheTimeLimitInDays = 180;
		private const int TheDistanceLimit = 8000;

		private static readonly DateTime TheSeededAnchor = new DateTime(2025, 1, 10, 7, 30, 0);
		private const int TheSeededAnchorReading = 30000;

		[Test]
		public void AKindNeverCompletedNamesItselfAndSaysThereIsNone()
		{
			MaintenanceSchedule schedule = AnInspectionSeededAndNeverCompleted();

			Assert.That(schedule.HasAnyCompletion(OperationKindCode.Inspection), Is.False);

			Assert.That(
				schedule.DescribeCompletions(OperationKindCode.Inspection),
				Is.EqualTo(
					"Revisión: no hay ninguna completación registrada para este vehículo."),
				"La frase nombra la clase. Sin el nombre, las nueve clases sin completar dirian todas "
				+ "la misma frase y la tarjeta no diria de que esta hablando.");
		}

		// Y NOMBRA LA CLASE POR LA QUE SE PREGUNTO, no una cualquiera. Dos clases sin completar dan
		// dos frases distintas: sin este par, una frase que nombrara siempre la primera clase de la
		// agenda pasaria la prueba de arriba.
		[Test]
		public void TwoKindsNeverCompletedAnswerTwoDifferentSentences()
		{
			MaintenanceSchedule schedule = AnInspectionSeededAndNeverCompleted();

			schedule.SeedPlanOperation(
				OperationKindCode.SoftwareUpdate, IntervalBasisCode.TimeAndDistance,
				TheTimeLimitInDays, TheDistanceLimit, DistanceUnitCode.UnitA,
				true, false,
				TheSeededAnchor, TheSeededAnchorReading, DistanceUnitCode.UnitA);

			Assert.That(
				schedule.DescribeCompletions(OperationKindCode.SoftwareUpdate),
				Does.StartWith("Actualización de software:"));

			Assert.That(
				schedule.DescribeCompletions(OperationKindCode.Inspection),
				Does.StartWith("Revisión:"));
		}

		// NI UNA LISTA VACIA SIN EXPLICACION NI UN CERO. Las columnas si vienen vacias -- son
		// paralelas y no tienen filas que decir -- pero la RESPUESTA de la clase es la frase, y el
		// conteo es cero PORQUE no hay filas, no en vez de ellas.
		[Test]
		public void ItIsNeitherAnEmptyListNorAZero()
		{
			MaintenanceSchedule schedule = AnInspectionSeededAndNeverCompleted();

			Assert.That(schedule.CompletionCount(OperationKindCode.Inspection), Is.EqualTo(0));
			Assert.That(schedule.CompletionInstantsOf(OperationKindCode.Inspection), Is.Empty);
			Assert.That(schedule.CompletionReadingsOf(OperationKindCode.Inspection), Is.Empty);
			Assert.That(schedule.DescribeEachCompletionOf(OperationKindCode.Inspection), Is.Empty);

			Assert.That(
				schedule.DescribeCompletions(OperationKindCode.Inspection), Is.Not.Empty,
				"El conteo en cero y las columnas vacias son ciertos; lo que no puede faltar es la "
				+ "frase. Un cero solo es lo que L7 llama no ser una respuesta.");
		}

		// UNA CLASE QUE NO ESTA AGENDADA ES LA OTRA AUSENCIA. Contesta totalmente -- no revienta -- y
		// dice SU razon: no hay operacion que completar. Es la polaridad de ReadDueState leida al
		// derecho, y no su degradacion: ahi NotDue es cierto porque no vencido es no vencido, y aca
		// "nunca se completo" no seria cierto sino incontestable.
		[Test]
		public void AKindWithNoOperationScheduledSaysThatInstead()
		{
			MaintenanceSchedule schedule = AnInspectionSeededAndNeverCompleted();

			Assert.That(schedule.HasAnyCompletion(OperationKindCode.Repair), Is.False);
			Assert.That(schedule.CompletionCount(OperationKindCode.Repair), Is.EqualTo(0));

			Assert.That(
				schedule.DescribeCompletions(OperationKindCode.Repair),
				Is.EqualTo(
					"Este vehículo no tiene agendada ninguna operación de reparación, así que no hay "
					+ "ninguna completación que registrar."));

			Assert.That(
				schedule.DescribeCompletions(OperationKindCode.Repair),
				Is.Not.EqualTo(schedule.DescribeCompletions(OperationKindCode.Inspection)));
		}

		// UN CODIGO QUE NO NOMBRA NINGUN MIEMBRO SIGUE SIENDO UN DEFECTO DE QUIEN LLAMA. La polaridad
		// de ReadDueState entera: clase desconocida revienta, clase conocida sin operacion contesta.
		[Test]
		public void AKindThatNamesNoMemberIsStillACallerBug()
		{
			MaintenanceSchedule schedule = AnInspectionSeededAndNeverCompleted();

			Assert.Throws<DomainException>(
				() => schedule.DescribeCompletions((OperationKindCode)99));
			Assert.Throws<DomainException>(
				() => schedule.HasAnyCompletion((OperationKindCode)99));
			Assert.Throws<DomainException>(
				() => schedule.CompletionInstantsOf((OperationKindCode)99));
		}

		// LO QUE EL JUEGO DE TODAS LAS CLASES NO DICE, dicho aca en vez de omitido en silencio (C56).
		// §5.3 no le declara ninguna frase: sin completaciones sus cuatro columnas vienen vacias y
		// paralelas, y no hay ningun miembro que describa el juego entero. La ausencia de la frase es
		// una DECISION de alcance -- la que nombra una clase es DescribeCompletions -- y quien agregue
		// una tiene que ponerse rojo aca primero.
		[Test]
		public void TheEveryKindReadHasNoSentenceOfItsOwnAndThatIsSaidRatherThanOmitted()
		{
			MaintenanceSchedule schedule = AnInspectionSeededAndNeverCompleted();

			Assert.That(schedule.CompletionKindsOnRecord(), Is.Empty);
			Assert.That(schedule.CompletionInstantsOnRecord(), Is.Empty);
			Assert.That(schedule.CompletionReadingsOnRecord(), Is.Empty);
			Assert.That(schedule.DescribeEachCompletionOnRecord(), Is.Empty);

			List<string> describers = typeof(MaintenanceSchedule)
				.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
					| BindingFlags.DeclaredOnly)
				.Where(member => member.ReturnType == typeof(string))
				.Where(member => member.GetParameters().Length == 0)
				.Where(member => member.Name.IndexOf(
					"Completion", StringComparison.Ordinal) >= 0)
				.Select(member => member.Name)
				.ToList();

			Assert.That(describers, Is.Empty,
				"Apareció una frase del juego de todas las clases: " + string.Join(", ", describers)
				+ ". Si se decidio agregarla, se dice a que ausencia contesta -- ninguna clase que "
				+ "nombrar -- y esta prueba se reescribe con la frase adentro.");
		}

		private static MaintenanceSchedule AnInspectionSeededAndNeverCompleted()
		{
			MaintenanceSchedule schedule =
				new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);

			schedule.SeedPlanOperation(
				OperationKindCode.Inspection, IntervalBasisCode.TimeAndDistance,
				TheTimeLimitInDays, TheDistanceLimit, DistanceUnitCode.UnitA,
				true, false,
				TheSeededAnchor, TheSeededAnchorReading, DistanceUnitCode.UnitA);

			return schedule;
		}
	}
}
