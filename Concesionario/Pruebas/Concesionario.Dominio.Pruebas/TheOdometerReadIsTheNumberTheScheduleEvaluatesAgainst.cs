using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LEY L6 DEL SLICE DEL SOBRE, TheOdometerReadIsTheNumberTheScheduleEvaluatesAgainst, y el
	// test 13 de §11 del brief: "una lectura observada DESPUES del instante preguntado no se
	// devuelve, y el numero que la lectura da es el mismo que Evaluate usa a ese instante --
	// comparense los dos caminos directamente".
	//
	// EL DEFECTO QUE ESTO EVITA ES C63 · 2 REPRODUCIDO ADENTRO DE NUESTRA PROPIA SUPERFICIE: dos
	// numeros para un hecho en una pagina. Un feed puede traer una lectura fechada hacia adelante;
	// Evaluate(asOf) la ignora, y una lectura del odometro que devolviera "la ultima registrada, y
	// punto" la devolveria. La seccion de garantia y el tablero de mantenimiento quedarian
	// discrepando sobre UN hecho sin que ninguna de las dos pantallas lo dijera.
	//
	// NO HAY MIEMBRO QUE REPORTE EL NUMERO QUE Evaluate USO, asi que la comparacion se hace
	// OBSERVABLE por dos vias, y las dos hacen falta:
	//
	//   POR CONSECUENCIA   dos agendas con las MISMAS operaciones y las MISMAS dos lecturas, que
	//                      solo se diferencian en CUANDO se observo la segunda. La lectura y el
	//                      estado del tablero se dan vuelta JUNTOS: 35000 -> NotDue y 42000 -> Due.
	//                      Si la lectura devolviera la ultima registrada sin mirar observedAt, la
	//                      primera agenda diria 42000 con el tablero en NotDue -- los dos numeros.
	//
	//   POR ESTRUCTURA     un solo miembro resuelve una lectura de la linea de tiempo, y es
	//                      privado. Con eso L6 no es una promesa que la prueba verifica: es una
	//                      propiedad del tipo, y la prueba la confirma.
	//
	// LO QUE ESTO **NO** PRUEBA, y el riesgo R2 del plan lo dice antes de que nadie lo descubra:
	// L6 cierra la brecha entre DOS LECTURAS, no entre una lectura y el TABLERO. El tablero muestra
	// estados del ultimo Evaluate, que uso un instante posiblemente ANTERIOR, asi que una lectura
	// que llegue entre los dos momentos puede dejar la seccion de garantia mas nueva que el
	// tablero. Un hecho, un numero, dos momentos. La mitigacion es que la lectura devuelve su
	// instante de observacion (test 14), no que la brecha no exista.
	//
	// LOS NUMEROS ESTAN ELEGIDOS PARA QUE LA RESPUESTA EQUIVOCADA SE VEA DISTINTA (C60, y el
	// hallazgo 3 de T2 con una FECHA en lugar de un numero): ninguna lectura es 0, las dos estan a
	// lados opuestos del limite, y el instante preguntado no cae el mismo dia que ninguna
	// observacion.
	[TestFixture]
	public sealed class TheOdometerReadIsTheNumberTheScheduleEvaluatesAgainst
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TenThousandUnits = 10000;
		private const int OdometerAtDelivery = 30000;

		// 35000 - 30000 = 5000 recorridos contra un limite de 10000: NotDue con holgura, lejos del
		// anticipo de cortesia de 1000.
		private const int TheReadingBeforeTheLimit = 35000;

		// 40200 - 30000 = 10200 recorridos: Due, y no Overdue -- 10200 < 10000 + 500 de gracia.
		private const int TheReadingPastTheLimit = 40200;

		private static readonly DateTime Delivery = new DateTime(2025, 6, 1);
		private static readonly DateTime TheEarlierObservation = new DateTime(2026, 3, 10);
		private static readonly DateTime TheInstantAsked = new DateTime(2026, 6, 15);
		private static readonly DateTime AfterTheInstantAsked = new DateTime(2026, 9, 20);
		private static readonly DateTime BeforeTheInstantAsked = new DateTime(2026, 5, 20);

		// EL CASO QUE IMPORTA: el relleno esta fechado HACIA ADELANTE. Evaluate(asOf) no lo mira, y
		// la lectura tampoco puede mirarlo.
		[Test]
		public void AReadingObservedAfterTheInstantAskedIsNotTheOneReturned()
		{
			MaintenanceSchedule schedule = AScheduleWhoseSecondReadingWasObservedOn(
				AfterTheInstantAsked);

			Assert.That(schedule.ReadOdometer(TheInstantAsked),
				Is.EqualTo(TheReadingBeforeTheLimit),
				"La lectura tiene que dar la que Evaluate usaria al mismo instante. Devolver la "
				+ "ultima registrada dejaria la seccion de garantia diciendo 40200 mientras el "
				+ "tablero decide con 35000, y ninguna de las dos pantallas lo diria.");

			Assert.That(schedule.ReadOdometer(TheInstantAsked),
				Is.Not.EqualTo(TheReadingPastTheLimit));
		}

		// LA COMPARACION DE LOS DOS CAMINOS, POR CONSECUENCIA. Las dos agendas son identicas salvo
		// en CUANDO se observo la segunda lectura, y las dos respuestas se dan vuelta juntas.
		[Test]
		public void TheBoardAndTheReadMoveOnTheSameReading()
		{
			MaintenanceSchedule theBackfillIsDatedAhead =
				AScheduleWhoseSecondReadingWasObservedOn(AfterTheInstantAsked);
			MaintenanceSchedule theBackfillIsAlreadyObserved =
				AScheduleWhoseSecondReadingWasObservedOn(BeforeTheInstantAsked);

			theBackfillIsDatedAhead.Evaluate(TheInstantAsked);
			theBackfillIsAlreadyObserved.Evaluate(TheInstantAsked);

			Assert.That(theBackfillIsDatedAhead.ReadOdometer(TheInstantAsked),
				Is.EqualTo(TheReadingBeforeTheLimit));
			Assert.That(
				theBackfillIsDatedAhead.ReadDueState(OperationKindCode.FilterChange),
				Is.EqualTo("NotDue"),
				"El tablero decidio con 35000. La lectura tiene que decir 35000.");

			Assert.That(theBackfillIsAlreadyObserved.ReadOdometer(TheInstantAsked),
				Is.EqualTo(TheReadingPastTheLimit));
			Assert.That(
				theBackfillIsAlreadyObserved.ReadDueState(OperationKindCode.FilterChange),
				Is.EqualTo("Due"),
				"Y con 40200 el tablero decide distinto. Las dos respuestas se dan vuelta JUNTAS, "
				+ "que es la unica forma de ver que las dos leen el mismo numero.");
		}

		// La misma comparacion, con el instante preguntado corrido en vez de la observacion: la
		// MISMA agenda contesta las dos cosas segun a que instante se le pregunte. Aca la clave es
		// visiblemente el instante y no el orden en que llegaron las lecturas.
		[Test]
		public void TheSameScheduleAnswersDifferentlyAtTwoInstantsBecauseTheKeyIsTheInstant()
		{
			MaintenanceSchedule schedule = AScheduleWhoseSecondReadingWasObservedOn(
				AfterTheInstantAsked);

			Assert.That(schedule.ReadOdometer(TheInstantAsked),
				Is.EqualTo(TheReadingBeforeTheLimit));
			Assert.That(schedule.ReadOdometer(AfterTheInstantAsked.AddDays(1)),
				Is.EqualTo(TheReadingPastTheLimit),
				"Pasada la observacion, la misma agenda contesta la otra lectura. La respuesta es "
				+ "del INSTANTE preguntado y no de la agenda.");
		}

		// LA MITAD ESTRUCTURAL. Un solo miembro resuelve una lectura de la linea de observacion, y
		// es PRIVADO: Evaluate y las cinco lecturas del odometro pasan todas por el. Con eso hay
		// literalmente un numero, y no dos que se prometen estar de acuerdo.
		[Test]
		public void OnlyOneMemberResolvesAReadingFromTheTimeline()
		{
			List<string> resolutores = MiembrosDeLaAgendaQueDevuelven(typeof(UsableReading))
				.Select(miembro => miembro.Name)
				.ToList();

			Assert.That(resolutores, Is.EqualTo(new[] { "TheLatestUsableReadingAsOf" }),
				"MaintenanceSchedule resuelve una lectura usable en mas de un lugar: "
				+ string.Join(", ", resolutores) + ". Dos resolutores es la forma en que dos "
				+ "numeros para un hecho entran sin que nadie lo decida.");

			MethodInfo resolutor = typeof(MaintenanceSchedule).GetMethod(
				"TheLatestUsableReadingAsOf",
				BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
					| BindingFlags.DeclaredOnly);

			Assert.That(resolutor, Is.Not.Null);
			Assert.That(resolutor.IsPrivate, Is.True,
				"El resolutor tiene que quedarse privado. Ensancharlo permitiria comparar los dos "
				+ "caminos llamando al MISMO miembro -- que solo prueba que un miembro coincide "
				+ "consigo mismo -- y sacaria el portador UsableReading del agregado.");
		}

		// Y NINGUN PORTADOR CRUZA LA FACHADA (regla de salida de E3, y la nota de §9 del brief).
		// Lo que sale son escalares: el numero, el nombre de la unidad, su codigo y el instante.
		[Test]
		public void NoOdometerCarrierCrossesTheFacade()
		{
			List<string> portadores = MiembrosExpuestosDeLaAgendaQueDevuelven(typeof(UsableReading))
				.Concat(MiembrosExpuestosDeLaAgendaQueDevuelven(typeof(OdometerReading)))
				.Select(miembro => miembro.Name)
				.ToList();

			Assert.That(portadores, Is.Empty,
				"Miembros expuestos que entregan un portador del dominio: "
				+ string.Join(", ", portadores) + ". Quien lo reciba puede volver a derivar lo que "
				+ "el agregado ya decidio, y el perform no lo puede llevar de vuelta igual.");
		}

		private static IEnumerable<MethodInfo> MiembrosDeLaAgendaQueDevuelven(Type devuelto)
		{
			return typeof(MaintenanceSchedule)
				.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
				.Where(miembro => miembro.ReturnType == devuelto)
				.OrderBy(miembro => miembro.Name, StringComparer.Ordinal);
		}

		private static IEnumerable<MethodInfo> MiembrosExpuestosDeLaAgendaQueDevuelven(Type devuelto)
		{
			return MiembrosDeLaAgendaQueDevuelven(devuelto)
				.Where(miembro => miembro.IsPublic || miembro.IsAssembly);
		}

		private static MaintenanceSchedule AScheduleWhoseSecondReadingWasObservedOn(
			DateTime theSecondObservation)
		{
			MaintenanceSchedule schedule =
				new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);

			schedule.SeedPlanOperation(
				OperationKindCode.FilterChange, IntervalBasisCode.DistanceOnly,
				0, TenThousandUnits, DistanceUnitCode.UnitA,
				true, false,
				Delivery, OdometerAtDelivery, DistanceUnitCode.UnitA);

			schedule.RecordReading(
				TheReadingBeforeTheLimit, DistanceUnitCode.UnitA, TheEarlierObservation,
				ReadingSourceCode.Telemetry);
			schedule.RecordReading(
				TheReadingPastTheLimit, DistanceUnitCode.UnitA, theSecondObservation,
				ReadingSourceCode.Telemetry);

			return schedule;
		}
	}
}
