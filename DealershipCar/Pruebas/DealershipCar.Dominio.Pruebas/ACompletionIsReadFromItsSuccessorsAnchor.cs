using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L5 DE SLICE-TheMaintenanceScreen: una ocurrencia completada guarda SOLO que termino. El
	// cuando y con que lectura son el punto base de la SIGUIENTE, asi que la completacion de la
	// ocurrencia n se lee del ancla de la n+1.
	//
	// MS-0 §3 contesto la pregunta del §10.3 del brief con un NO: una ocurrencia completada no
	// expone nada de su propia completacion. Re-derivado en ScheduledOperation.cs: Complete pone
	// _state = DueState.Completed y nada mas, el instante y la lectura entran a
	// NextOccurrenceFromActualCompletion, no hay accesor de _basePoint de ninguna visibilidad, y
	// Describe() de una completada devuelve el literal "Hecha.". Por eso el camino vive EN
	// ScheduledOperation: desde afuera de la clase el ancla es inalcanzable.
	//
	// Y POR ESO EL BRIEF SE EQUIVOCA EN UNA FRASE. §1.3 dice que "el ancla de la ocurrencia activa es
	// la completacion mas reciente". Es FALSO despues de una revision del plan: la activa esta
	// anclada en la semilla del que llamo, y la fila mas reciente sale de la SUSTITUIDA.
	// AfterAPlanRevisionTheNewestRowIsNotTheActiveOccurrencesAnchor es esa medicion.
	[TestFixture]
	public sealed class ACompletionIsReadFromItsSuccessorsAnchor
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TheTimeLimitInDays = 180;
		private const int TheDistanceLimit = 8000;

		// EL ANCLA SEMBRADA. Ni su instante ni su lectura son una completacion: nadie completo nada
		// el 10 de enero con 30000, y ninguno de los dos numeros coincide con los de abajo.
		private static readonly DateTime TheSeededAnchor = new DateTime(2025, 1, 10, 7, 30, 0);
		private const int TheSeededAnchorReading = 30000;

		private static readonly DateTime TheFirstCompletion = new DateTime(2025, 4, 15, 9, 20, 0);
		private const int TheFirstCompletionReading = 34100;
		private static readonly DateTime TheSecondCompletion = new DateTime(2025, 8, 22, 14, 5, 0);
		private const int TheSecondCompletionReading = 38700;
		private static readonly DateTime TheThirdCompletion = new DateTime(2026, 1, 30, 11, 45, 0);
		private const int TheThirdCompletionReading = 43900;

		// LA SEMILLA DE LA REVISION. Es lo que el brief llamaria "la completacion mas reciente" y no
		// lo es: nadie completo nada el 14 de febrero con 44500.
		private static readonly DateTime TheRevisionAnchor = new DateTime(2026, 2, 14, 8, 0, 0);
		private const int TheRevisionAnchorReading = 44500;

		[Test]
		public void ThreeCompletionsAnswerThreeRowsNewestFirst()
		{
			MaintenanceSchedule schedule = AFluidChangeCompletedThreeTimes();

			Assert.That(schedule.HasAnyCompletion(OperationKindCode.FluidChange), Is.True);
			Assert.That(schedule.CompletionCount(OperationKindCode.FluidChange), Is.EqualTo(3));

			Assert.That(
				schedule.CompletionInstantsOf(OperationKindCode.FluidChange),
				Is.EqualTo(new[]
				{
					"2026-01-30 11:45:00", "2025-08-22 14:05:00", "2025-04-15 09:20:00",
				}));

			Assert.That(
				schedule.CompletionReadingsOf(OperationKindCode.FluidChange),
				Is.EqualTo(new[] { "43900 km", "38700 km", "34100 km" }),
				"Cada fila trae la lectura de SU completacion, no la del ancla sembrada ni la de la "
				+ "vecina: si el camino emitiera desde el predecesor las tres saldrian corridas.");
		}

		// LA FILA MAS RECIENTE SALE DEL ANCLA DE LA ACTIVA. Es la mitad de L5 que decide el sentido
		// del camino: emitir desde el predecesor perderia la ultima completacion y regalaria el ancla
		// sembrada como si fuera una.
		[Test]
		public void TheNewestRowComesFromTheActiveOccurrencesOwnAnchor()
		{
			MaintenanceSchedule schedule = AFluidChangeCompletedThreeTimes();

			Assert.That(
				schedule.CompletionInstantsOf(OperationKindCode.FluidChange)[0],
				Is.EqualTo("2026-01-30 11:45:00"));

			Assert.That(
				schedule.CompletionReadingsOf(OperationKindCode.FluidChange)[0],
				Is.EqualTo("43900 km"));

			Assert.That(
				schedule.DescribeCompletions(OperationKindCode.FluidChange),
				Does.Contain(
					"La más reciente se hizo el 2026-01-30 11:45:00 con el odómetro en 43900 km"),
				"Y la frase del juego nombra esa misma: si el camino se corriera un eslabon, la frase "
				+ "y la columna se corren juntas y ninguna de las dos lo delata sola.");
		}

		// UNA ANTERIOR QUE TERMINO SUSTITUIDA NO ES UNA COMPLETACION. Es la clausula que §5.1 le
		// agrego a L5, y sin ella el camino emite el ancla que el que llamo sembro: una fecha y un
		// odometro donde nadie completo nada, por una accion ordinaria del concesionario.
		[Test]
		public void ASupersededPredecessorIsNotACompletion()
		{
			MaintenanceSchedule schedule = AFluidChangeCompletedThreeTimesThenRevised();

			Assert.That(schedule.CompletionCount(OperationKindCode.FluidChange), Is.EqualTo(3),
				"Sustituir no completa: la revision del plan no agrega una cuarta fila.");

			Assert.That(
				schedule.CompletionInstantsOf(OperationKindCode.FluidChange),
				Does.Not.Contain("2026-02-14 08:00:00"),
				"El ancla de la sustituta es la SEMILLA del que llamo. Emitirla diria que el vehiculo "
				+ "estuvo en el taller el 14 de febrero.");

			Assert.That(
				schedule.CompletionReadingsOf(OperationKindCode.FluidChange),
				Does.Not.Contain("44500 km"),
				"Y con ella iria un odometro que nadie leyo al completar nada.");
		}

		// EL ANCLA DE LA GENESIS TAMPOCO. La primera ocurrencia no salio de ninguna, asi que su ancla
		// es donde se la sembro y no una completacion. El camino se detiene ahi porque no hay
		// anterior, no porque alguien lo filtre.
		[Test]
		public void TheGenesisAnchorIsNotACompletionRow()
		{
			MaintenanceSchedule schedule = AFluidChangeCompletedThreeTimes();

			Assert.That(
				schedule.CompletionInstantsOf(OperationKindCode.FluidChange),
				Does.Not.Contain("2025-01-10 07:30:00"));

			Assert.That(
				schedule.CompletionReadingsOf(OperationKindCode.FluidChange),
				Does.Not.Contain("30000 km"),
				"El ancla sembrada es de donde se empieza a medir, no algo que se hizo. Con tres "
				+ "completaciones son tres filas y no cuatro.");

			MaintenanceSchedule untouched = AFluidChangeSeededAndNeverCompleted();

			Assert.That(untouched.CompletionCount(OperationKindCode.FluidChange), Is.EqualTo(0),
				"Y sin ninguna completacion la genesis sola no produce ninguna fila.");
		}

		// LA FRASE FALSA DEL §1.3, MEDIDA. Despues de una revision del plan la fila mas reciente sale
		// de la SUSTITUIDA y no de la activa. El algoritmo de §5.2 lo hace bien -- emite en el ancla
		// de X saltando los predecesores que no terminaron completados -- pero la oracion del brief
		// es un segundo defecto en el mismo parrafo que L5.
		[Test]
		public void AfterAPlanRevisionTheNewestRowIsNotTheActiveOccurrencesAnchor()
		{
			MaintenanceSchedule schedule = AFluidChangeCompletedThreeTimesThenRevised();

			Assert.That(
				schedule.CompletionInstantsOf(OperationKindCode.FluidChange)[0],
				Is.EqualTo("2026-01-30 11:45:00"),
				"La activa esta anclada el 2026-02-14 y la fila mas reciente es del 2026-01-30: el "
				+ "ancla de la activa NO es la completacion mas reciente.");

			Assert.That(
				schedule.CompletionInstantsOf(OperationKindCode.FluidChange)[0],
				Is.Not.EqualTo("2026-02-14 08:00:00"));
		}

		// EL JUEGO DE TODAS LAS CLASES: mas reciente primero ATRAVESANDO clases, y la columna de la
		// clase alineada fila por fila. Es donde el orden importa de verdad: dentro de una clase la
		// cadena ya viene ordenada, y entre clases hay que intercalar.
		[Test]
		public void TheEveryKindReadPutsTheNewestFirstAcrossKinds()
		{
			MaintenanceSchedule schedule = TwoKindsCompletedAlternately();

			Assert.That(
				schedule.CompletionInstantsOnRecord(),
				Is.EqualTo(new[]
				{
					"2026-01-30 11:45:00", "2025-08-22 14:05:00", "2025-04-15 09:20:00",
				}));

			Assert.That(
				schedule.CompletionKindsOnRecord(),
				Is.EqualTo(new[] { "FilterChange", "FluidChange", "FilterChange" }),
				"Intercaladas: la clase de cada fila tiene que seguir a SU instante. Ordenar por clase "
				+ "y despues por instante daria las mismas tres filas en el orden equivocado.");

			Assert.That(
				schedule.CompletionReadingsOnRecord(),
				Is.EqualTo(new[] { "43900 km", "38700 km", "34100 km" }));

			Assert.That(
				schedule.DescribeEachCompletionOnRecord()[0],
				Does.Contain("Cambio de filtro").And.Contain("2026-01-30 11:45:00"),
				"Y la frase de la fila 0 nombra la clase de la fila 0: con las columnas cruzadas el "
				+ "instante calza y la clase no.");
		}

		// LA CLASE VIAJA DOS VECES, cada vez para quien la lee. La columna dice el NOMBRE DEL MIEMBRO
		// -- direccionable, es lo que un llamador puede volver a pasar -- y la frase dice como se
		// dice en español. Es el mismo reparto que ReadNextDueKind y las frases del dominio ya hacen.
		[Test]
		public void TheKindColumnNamesTheMemberAndTheSentenceSaysItInWords()
		{
			MaintenanceSchedule schedule = TwoKindsCompletedAlternately();

			Assert.That(schedule.CompletionKindsOnRecord(), Does.Contain("FluidChange"));
			Assert.That(schedule.CompletionKindsOnRecord(), Does.Not.Contain("Cambio de fluido"));

			Assert.That(
				schedule.DescribeEachCompletionOnRecord(),
				Has.Some.Contains("Cambio de fluido"));

			Assert.That(
				schedule.DescribeEachCompletionOnRecord(),
				Has.None.Contains("FluidChange"),
				"La frase que una persona lee no dice el nombre del miembro, y la columna que una "
				+ "maquina direcciona no dice la frase. Los dos son el mismo hecho dicho dos veces.");
		}

		private static MaintenanceSchedule AFluidChangeSeededAndNeverCompleted()
		{
			MaintenanceSchedule schedule =
				new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);

			Seed(schedule, OperationKindCode.FluidChange, TheSeededAnchor, TheSeededAnchorReading);

			return schedule;
		}

		private static MaintenanceSchedule AFluidChangeCompletedThreeTimes()
		{
			MaintenanceSchedule schedule = AFluidChangeSeededAndNeverCompleted();

			Complete(schedule, OperationKindCode.FluidChange, TheFirstCompletion,
				TheFirstCompletionReading);
			Complete(schedule, OperationKindCode.FluidChange, TheSecondCompletion,
				TheSecondCompletionReading);
			Complete(schedule, OperationKindCode.FluidChange, TheThirdCompletion,
				TheThirdCompletionReading);

			return schedule;
		}

		// LA RUTA 2 DE MS-0 §3: AdoptPlanRevision no termina nada -- solo deja el codigo y su
		// instante -- y la sustitucion pasa cuando SeedPlanOperation vuelve a entrar por la misma
		// clase con el MISMO origen. Es la unica que enlaza sucesora sin ser una completacion.
		private static MaintenanceSchedule AFluidChangeCompletedThreeTimesThenRevised()
		{
			MaintenanceSchedule schedule = AFluidChangeCompletedThreeTimes();

			schedule.AdoptPlanRevision("R2", TheRevisionAnchor);

			Seed(schedule, OperationKindCode.FluidChange, TheRevisionAnchor,
				TheRevisionAnchorReading);

			return schedule;
		}

		private static MaintenanceSchedule TwoKindsCompletedAlternately()
		{
			MaintenanceSchedule schedule =
				new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);

			Seed(schedule, OperationKindCode.FluidChange, TheSeededAnchor, TheSeededAnchorReading);
			Seed(schedule, OperationKindCode.FilterChange, TheSeededAnchor, TheSeededAnchorReading);

			Complete(schedule, OperationKindCode.FilterChange, TheFirstCompletion,
				TheFirstCompletionReading);
			Complete(schedule, OperationKindCode.FluidChange, TheSecondCompletion,
				TheSecondCompletionReading);
			Complete(schedule, OperationKindCode.FilterChange, TheThirdCompletion,
				TheThirdCompletionReading);

			return schedule;
		}

		private static void Seed(
			MaintenanceSchedule schedule, OperationKindCode kind, DateTime anchoredAt, int reading)
		{
			schedule.SeedPlanOperation(
				kind, IntervalBasisCode.TimeAndDistance,
				TheTimeLimitInDays, TheDistanceLimit, DistanceUnitCode.UnitA,
				true, false,
				anchoredAt, reading, DistanceUnitCode.UnitA);
		}

		private static void Complete(
			MaintenanceSchedule schedule, OperationKindCode kind, DateTime completedAt, int reading)
		{
			schedule.CompleteOperation(kind, completedAt, reading, DistanceUnitCode.UnitA);
		}
	}
}
