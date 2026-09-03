using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L4 DE SLICE-TheMaintenanceScreen: el historial se produce CAMINANDO la cadena hacia atras
	// en cada lectura. Ni lista guardada, ni contador, ni campo nuevo en ScheduledOperation. Una
	// copia seria un segundo historial que puede discrepar del primero.
	//
	// LA MITAD ESTRUCTURAL VA PRIMERO. Que las filas salgan bien no dice nada sobre si alguien las
	// guardo: una implementacion que cacheara el arreglo en un campo pasaria las lecturas sucesivas
	// igual mientras nadie complete nada nuevo. El censo de campos es lo que ve eso, y _deferrals ya
	// es una List, asi que "ninguna lista guardada" significa NINGUNA NUEVA y el censo tiene que
	// nombrar la que ya estaba.
	//
	// Y EL LIMITE DE L4, que MS-0 §3 encontro caminando la cadena a mano y que esta tarea PINCHA en
	// vez de emitir L7 en silencio: hay TRES formas de que una ocurrencia termine, y la tercera --
	// Supersede() sola -- no enlaza sucesora. RemoveOwnerOperation desinstala la clase entera y
	// OneActiveOperationPerKind instala una genesis cuando el ORIGEN cambia. En los dos casos las
	// completaciones anteriores quedan fuera del alcance del agregado, y eso no es un numero mal
	// contado: es una respuesta que contradice el historial. Se dice con dos pruebas nombradas.
	[TestFixture]
	public sealed class TheChainIsTheHistoryAndItIsNotCopied
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TheTimeLimitInDays = 180;
		private const int TheDistanceLimit = 8000;

		private static readonly DateTime TheAnchor = new DateTime(2025, 1, 10, 7, 30, 0);
		private const int TheAnchorReading = 30000;

		// LOS DOS INSTANTES Y LAS DOS LECTURAS NO SE CRUZAN CON NADA (C60): ninguna coincide con el
		// ancla, con la otra, con los limites ni con los conteos.
		private static readonly DateTime TheFirstCompletion = new DateTime(2025, 4, 15, 9, 20, 0);
		private const int TheFirstCompletionReading = 34100;
		private static readonly DateTime TheSecondCompletion = new DateTime(2025, 8, 22, 14, 5, 0);
		private const int TheSecondCompletionReading = 38700;

		// LOS OCHO CAMPOS DE HOY, leidos uno por uno en ScheduledOperation.cs y afirmados EXACTOS.
		// No se cita ningun conteo: la lista es la afirmacion.
		[Test]
		public void TheseAreTheFieldsOfAScheduledOperationAndNoOthers()
		{
			string[] theEightFields =
			{
				"_definition", "_basePoint", "_requestedByTheOwner", "_precededBy", "_deferrals",
				"_state", "_standing", "_conditionThresholdCrossed",
			};

			List<string> declared = typeof(ScheduledOperation)
				.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
				.Select(field => field.Name)
				.ToList();

			Assert.That(declared, Is.EquivalentTo(theEightFields),
				"El juego de campos de ScheduledOperation cambio. Part B camina la cadena y no guarda "
				+ "nada, asi que un campo nuevo aca es la copia que L4 existe para prohibir. Si se "
				+ "agrego a proposito, se dice por que y se amplia esta lista.");
		}

		// NINGUNA LISTA NUEVA. _deferrals es la unica, y estaba antes de part B: se nombra para que
		// "ninguna lista guardada" no se lea como "ninguna lista".
		[Test]
		public void TheOnlyStoredListIsTheOneThatWasAlreadyThere()
		{
			List<string> lists = typeof(ScheduledOperation)
				.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
				.Where(field => field.FieldType.IsGenericType)
				.Select(field => field.Name)
				.ToList();

			Assert.That(lists, Is.EquivalentTo(new[] { "_deferrals" }),
				"Hay una coleccion guardada que no es la de aplazamientos: " + string.Join(", ", lists)
				+ ". El historial se camina, no se acumula.");
		}

		// DERIVADO EN CADA LECTURA (E35). Dos lecturas seguidas devuelven arreglos DISTINTOS con el
		// mismo contenido -- se compone cada vez -- y una completacion mas lo mueve sin que nadie
		// refresque nada.
		[Test]
		public void TheRowsAreWalkedOnEveryReadAndNothingIsStoredBetweenThem()
		{
			MaintenanceSchedule schedule = APlanOperationCompletedTwice();

			string[] first = schedule.CompletionInstantsOf(OperationKindCode.FluidChange);
			string[] second = schedule.CompletionInstantsOf(OperationKindCode.FluidChange);

			Assert.That(second, Is.EqualTo(first));
			Assert.That(second, Is.Not.SameAs(first),
				"La segunda lectura devolvio el MISMO arreglo. Un arreglo compartido es una copia "
				+ "guardada, y una copia puede discrepar de la cadena.");

			schedule.CompleteOperation(
				OperationKindCode.FluidChange, new DateTime(2026, 1, 30, 11, 45, 0), 43900,
				DistanceUnitCode.UnitA);

			Assert.That(
				schedule.CompletionInstantsOf(OperationKindCode.FluidChange),
				Has.Length.EqualTo(first.Length + 1),
				"Una completacion mas tiene que aparecer sin que nadie invalide nada: el historial ES "
				+ "la cadena y se lee de ella.");
		}

		// EL CONTEO SALE DE LAS FILAS, no de un contador. Se compara contra el largo de la columna,
		// que es la respuesta primaria.
		[Test]
		public void TheCompletionCountIsDerivedFromTheRows()
		{
			MaintenanceSchedule schedule = APlanOperationCompletedTwice();

			Assert.That(
				schedule.CompletionCount(OperationKindCode.FluidChange),
				Is.EqualTo(schedule.CompletionInstantsOf(OperationKindCode.FluidChange).Length));

			Assert.That(
				schedule.CompletionCount(OperationKindCode.FluidChange),
				Is.EqualTo(schedule.DescribeEachCompletionOf(OperationKindCode.FluidChange).Length),
				"El conteo y las frases tienen que contar lo mismo: si un contador aparte se "
				+ "desincroniza, la tarjeta dice un numero y muestra otra cantidad de filas.");
		}

		// LAS COLUMNAS SON PARALELAS. Tres columnas del mismo juego de filas, del mismo largo, y la
		// fila 0 de cada una habla de la misma completacion: la mas reciente.
		[Test]
		public void TheColumnsAreParallelAndEveryRowSaysItself()
		{
			MaintenanceSchedule schedule = APlanOperationCompletedTwice();

			string[] instants = schedule.CompletionInstantsOf(OperationKindCode.FluidChange);
			string[] readings = schedule.CompletionReadingsOf(OperationKindCode.FluidChange);
			string[] said = schedule.DescribeEachCompletionOf(OperationKindCode.FluidChange);

			Assert.That(readings, Has.Length.EqualTo(instants.Length));
			Assert.That(said, Has.Length.EqualTo(instants.Length));

			Assert.That(instants[0], Is.EqualTo("2025-08-22 14:05:00"));
			Assert.That(readings[0], Is.EqualTo("38700 km"));
			Assert.That(said[0], Does.Contain("2025-08-22 14:05:00").And.Contain("38700 km"),
				"Cada fila se describe a si misma: la frase de la fila 0 tiene que nombrar el instante "
				+ "y la lectura de la fila 0, o las columnas estan cruzadas y nadie lo ve.");
		}

		// NINGUN PORTADOR DE PART B CRUZA LA SUPERFICIE DE LA AGENDA. Lo que sale son escalares y
		// columnas de texto (regla de salida de E3). NoOdometerCarrierCrossesTheFacade es especifico
		// de sus dos tipos y no veria estos, asi que esta censura se escribe aca.
		[Test]
		public void NoCompletionCarrierCrossesTheScheduleSurface()
		{
			List<string> exposed = typeof(MaintenanceSchedule)
				.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
				.Where(member => member.IsPublic || member.IsAssembly)
				.Where(member => member.ReturnType == typeof(ACompletionRow)
					|| member.ReturnType == typeof(ACompletionRow[])
					|| member.ReturnType == typeof(CompletionsOfAnOperation)
					|| member.ReturnType == typeof(List<ACompletionRow>))
				.Select(member => member.Name)
				.ToList();

			Assert.That(exposed, Is.Empty,
				"La agenda expone un portador de part B: " + string.Join(", ", exposed)
				+ ". El perform no lo puede llevar de vuelta, y quien lo reciba puede volver a derivar "
				+ "lo que el agregado ya compuso.");
		}

		// Y NINGUNA LECTURA DEL HISTORIAL RECIBE UN INSTANTE. Es la departure 2 de §5.4 hecha
		// estructura: una fila trae SU propio instante y la cadena es la que es. Ademas es lo que
		// deja a part B fuera de la guarda que part A heredo -- un resto preguntado antes del ancla
		// revienta porque llama a Assess; aca no hay instante que preguntar y no se llama a ninguna
		// regla.
		[Test]
		public void NoReadOfTheHistoryTakesAnInstant()
		{
			string[] theTenReads =
			{
				"HasAnyCompletion", "CompletionCount", "DescribeCompletions",
				"CompletionInstantsOf", "CompletionReadingsOf", "DescribeEachCompletionOf",
				"CompletionKindsOnRecord", "CompletionInstantsOnRecord",
				"CompletionReadingsOnRecord", "DescribeEachCompletionOnRecord",
			};

			foreach (string each in theTenReads)
			{
				MethodInfo[] found = typeof(MaintenanceSchedule)
					.GetMethods(BindingFlags.Instance | BindingFlags.Public
						| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
					.Where(member => member.Name == each)
					.ToArray();

				Assert.That(found, Has.Length.EqualTo(1), $"'{each}'");

				Assert.That(
					found[0].GetParameters().Any(p => p.ParameterType == typeof(DateTime)),
					Is.False,
					$"'{each}' recibe un instante. Una fila de completacion trae el suyo y el "
					+ "historial no se pregunta 'a que fecha': si esto se agrega, se decide a "
					+ "proposito y se dice contra que ancla se compara.");
			}
		}

		// EL LIMITE DE L4, RUTA 3a: RemoveOwnerOperation desinstala la clase y la cadena entera queda
		// sin nadie que la alcance. La agenda no puede decir que hubo completaciones porque ya no
		// tiene por donde llegar a ellas -- y lo que SI dice es que no hay operacion agendada, que no
		// es lo mismo que decir que nunca se hizo nada.
		[Test]
		public void TheHistoryReachesBackOnlyAsFarAsTheChainDoes()
		{
			MaintenanceSchedule schedule = AnOwnerOperationCompletedTwice();

			Assert.That(schedule.CompletionCount(OperationKindCode.Diagnostic), Is.EqualTo(2));

			schedule.RemoveOwnerOperation(OperationKindCode.Diagnostic);

			Assert.That(schedule.CompletionCount(OperationKindCode.Diagnostic), Is.EqualTo(0),
				"LIMITE CONOCIDO Y NO CERRADO: quitar la operacion desinstala la clase, y con ella la "
				+ "cabeza de la cadena. Las dos completaciones siguen habiendo ocurrido y el agregado "
				+ "ya no tiene por donde alcanzarlas.");

			Assert.That(
				schedule.DescribeCompletions(OperationKindCode.Diagnostic),
				Does.Contain("no tiene agendada ninguna operación de diagnóstico"),
				"Y esta es la mitad que hace el limite honesto: la frase dice que no hay operacion "
				+ "agendada, no que nunca se completo ninguna. Un unico L7 para las dos situaciones "
				+ "seria la respuesta que contradice el historial.");
		}

		// EL LIMITE DE L4, RUTA 3b: cuando el ORIGEN cambia -- el plan desplazando a un recordatorio
		// de la persona -- OneActiveOperationPerKind sustituye la activa y entra una GENESIS con
		// Predecessor.None. La clase sigue agendada, asi que la respuesta es L7 y no la de arriba, y
		// L7 es FALSO EN LOS HECHOS. Es la peor de las tres rutas y esta prueba es su constancia.
		[Test]
		public void AChainCutByAChangeOfOriginAnswersThatThereIsNoneAndThatIsTheLimit()
		{
			MaintenanceSchedule schedule = AnOwnerOperationCompletedTwice();

			Assert.That(schedule.CompletionCount(OperationKindCode.Diagnostic), Is.EqualTo(2));

			schedule.SeedPlanOperation(
				OperationKindCode.Diagnostic, IntervalBasisCode.TimeAndDistance,
				TheTimeLimitInDays, TheDistanceLimit, DistanceUnitCode.UnitA,
				true, false,
				new DateTime(2026, 2, 14, 8, 0, 0), 44500, DistanceUnitCode.UnitA);

			Assert.That(schedule.HasAnyCompletion(OperationKindCode.Diagnostic), Is.False);

			Assert.That(
				schedule.DescribeCompletions(OperationKindCode.Diagnostic),
				Does.Contain("no hay ninguna completación registrada"),
				"LIMITE CONOCIDO Y NO CERRADO, Y EL PEOR DE LOS TRES: la clase sigue agendada, asi que "
				+ "la respuesta es la de 'nunca se completo' -- y se completo dos veces. El corte de "
				+ "la cadena no deja rastro en el agregado, asi que ninguna frase puede decir que hubo "
				+ "algo antes del corte. Cerrarlo es una decision sobre OneActiveOperationPerKind, que "
				+ "no es de part B.");
		}

		// NO SON LA MISMA RESPUESTA, y esto es lo que separa las dos ausencias (E47). Una clase que
		// nunca se completo y una clase que no esta agendada dicen frases DISTINTAS, cada una
		// nombrando su propia razon. Un solo "no hay ninguna" para las dos juntaria una ausencia
		// curable por una visita con una que necesita que alguien siembre la operacion.
		[Test]
		public void NoOperationScheduledIsNotTheSameAnswerAsNeverCompleted()
		{
			MaintenanceSchedule schedule = APlanOperationCompletedTwice();

			string neverCompleted = schedule.DescribeCompletions(OperationKindCode.Inspection);
			string notScheduled = schedule.DescribeCompletions(OperationKindCode.Repair);

			Assert.That(neverCompleted, Is.Not.EqualTo(notScheduled));

			Assert.That(schedule.HasAnyCompletion(OperationKindCode.Inspection), Is.False);
			Assert.That(
				neverCompleted, Does.Contain("no hay ninguna completación registrada"),
				"Revisión ESTA agendada y no se ha completado nunca: eso es L7, y una visita al taller "
				+ "lo cura.");

			Assert.That(
				notScheduled, Does.Contain("no tiene agendada ninguna operación de reparación"),
				"Reparación no esta agendada: no hay nada que completar, y ninguna visita produce una "
				+ "fila hasta que alguien siembre la operacion. Son dos razones estructuralmente "
				+ "distintas para la misma columna vacia.");
		}

		private static MaintenanceSchedule APlanOperationCompletedTwice()
		{
			MaintenanceSchedule schedule = AScheduleWith(
				OperationKindCode.FluidChange, OperationKindCode.Inspection);

			schedule.CompleteOperation(
				OperationKindCode.FluidChange, TheFirstCompletion, TheFirstCompletionReading,
				DistanceUnitCode.UnitA);
			schedule.CompleteOperation(
				OperationKindCode.FluidChange, TheSecondCompletion, TheSecondCompletionReading,
				DistanceUnitCode.UnitA);

			return schedule;
		}

		private static MaintenanceSchedule AnOwnerOperationCompletedTwice()
		{
			MaintenanceSchedule schedule =
				new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);

			schedule.AddOwnerOperation(
				OperationKindCode.Diagnostic, IntervalBasisCode.TimeAndDistance,
				TheTimeLimitInDays, TheDistanceLimit, DistanceUnitCode.UnitA,
				TheAnchor, TheAnchorReading, DistanceUnitCode.UnitA);

			schedule.CompleteOperation(
				OperationKindCode.Diagnostic, TheFirstCompletion, TheFirstCompletionReading,
				DistanceUnitCode.UnitA);
			schedule.CompleteOperation(
				OperationKindCode.Diagnostic, TheSecondCompletion, TheSecondCompletionReading,
				DistanceUnitCode.UnitA);

			return schedule;
		}

		private static MaintenanceSchedule AScheduleWith(params OperationKindCode[] kinds)
		{
			MaintenanceSchedule schedule =
				new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);

			foreach (OperationKindCode kind in kinds)
				schedule.SeedPlanOperation(
					kind, IntervalBasisCode.TimeAndDistance,
					TheTimeLimitInDays, TheDistanceLimit, DistanceUnitCode.UnitA,
					true, false,
					TheAnchor, TheAnchorReading, DistanceUnitCode.UnitA);

			return schedule;
		}
	}
}
