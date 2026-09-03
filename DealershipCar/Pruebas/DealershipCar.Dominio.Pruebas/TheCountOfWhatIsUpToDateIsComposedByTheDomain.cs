using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LA PANTALLA DE INICIO CONTABA POR SU CUENTA, y era el unico caso vivo de la regla 3 de la
	// convencion de registro crudo en todo el modulo. Inicio.razor tenia:
	//
	//     private int AlDia => Nueve.Count(k => ComoSeLee.Tono(Board[k]) == "ok");
	//     private int Porcentaje => 100.0 * AlDia / Nueve.Count;
	//
	// DOS DEFECTOS EN TRES RENGLONES, y el segundo se veia solo cuando se arreglaba el primero.
	//
	//   UNO. La cuenta salia de los TONOS de la pantalla y no del registro. El tono es un reparto de
	//   la caratula: "NotDue" y "Completed" pintan verde. Contar por ahi es contar por como se ve.
	//
	//   DOS. El denominador eran las NUEVE clases del canon, y la frase al lado dice "de las que tu
	//   concesionario programo". Un vehiculo con cuatro operaciones agendadas mostraba algo sobre
	//   nueve. Y peor: ReadDueState contesta "NotDue" para una clase SIN operacion agendada -- es
	//   total a proposito -- asi que un vehiculo sin nada agendado mostraba NUEVE DE NUEVE AL DIA.
	//
	// LAS DOS MITADES LAS COMPONE EL DOMINIO Y NINGUNA SE DIVIDE. CountUpToDate y CountScheduled son
	// dos conteos que la agenda publica por su cuenta, que es la primera categoria que el censo
	// numerico ya admite; el porcentaje NO existe en ninguna de las dos capas, y la barra de progreso
	// que lo usaba se fue con el (P-9: ninguna barra, ningun grafico, ningun porcentaje).
	//
	// LA CUENTA VIVE DONDE VIVEN LOS OPERANDOS (E24). La ocurrencia sabe donde esta parada; la agenda
	// sabe cuantas tiene. Ninguna de las dos guarda un contador: los dos numeros son DERIVADOS, con
	// la misma forma que MissedCount, para que bajen solos cuando la operacion se hace.
	[TestFixture]
	public sealed class TheCountOfWhatIsUpToDateIsComposedByTheDomain
	{
		private const string TheVin = "1HGCM82633A004352";
		private const string TheUnit = "UnitA";
		private const int TwelveMonthsInDays = 365;
		private const int TenThousandUnits = 10000;

		private static readonly DateTime Delivery =
			new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime AYearLater =
			new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);

		private MaintenanceSchedule schedule;

		[SetUp]
		public void Setup()
		{
			schedule = new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);
		}

		// UNA AGENDA SIN NADA AGENDADO NO TIENE NINGUNA AL DIA. Es el renglon que se ponia mal antes:
		// ReadDueState contesta "NotDue" para una clase sin operacion, asi que contar por el tablero
		// daba nueve de nueve para un vehiculo del que no se sabe nada.
		[Test]
		public void AScheduleWithNothingOnItHasNothingUpToDate()
		{
			Assert.That(schedule.CountScheduled(), Is.EqualTo(0));
			Assert.That(schedule.CountUpToDate(), Is.EqualTo(0));
		}

		[Test]
		public void EverythingSeededAndNothingEvaluatedIsUpToDate()
		{
			Seed(OperationKindCode.FluidChange);
			Seed(OperationKindCode.Inspection);

			Assert.That(schedule.CountScheduled(), Is.EqualTo(2));
			Assert.That(schedule.CountUpToDate(), Is.EqualTo(2));
		}

		// LA QUE SE VENCIO SALE DE LA CUENTA, y las dos mitades dejan de ser el mismo numero. Sin
		// este renglon, CountUpToDate podria estar devolviendo CountScheduled y nadie lo notaria.
		[Test]
		public void AnOperationThatCameDueIsNoLongerUpToDate()
		{
			Seed(OperationKindCode.FluidChange);
			Seed(OperationKindCode.Inspection);

			schedule.RecordReading(
				40000, DistanceUnitCode.UnitA, AYearLater, ReadingSourceCode.Telemetry);
			schedule.Evaluate(AYearLater);

			Assert.That(schedule.ReadDueState(OperationKindCode.FluidChange), Is.EqualTo("Due"));
			Assert.That(schedule.CountScheduled(), Is.EqualTo(2),
				"Vencerse no saca a la operacion de la agenda: sigue programada.");
			Assert.That(schedule.CountUpToDate(), Is.EqualTo(0),
				"Las dos se vencieron con la misma lectura, asi que no queda ninguna al dia.");
			Assert.That(schedule.CountDue(), Is.EqualTo(2));
		}

		// Y LA CUENTA VUELVE A SUBIR CUANDO LA OPERACION SE HACE, porque es DERIVADA de donde esta
		// parada cada una. Un contador aparte se quedaria abajo para siempre.
		[Test]
		public void CompletingTheOperationPutsItBackAmongThoseUpToDate()
		{
			Seed(OperationKindCode.FluidChange);

			schedule.RecordReading(
				40000, DistanceUnitCode.UnitA, AYearLater, ReadingSourceCode.Telemetry);
			schedule.Evaluate(AYearLater);

			Assert.That(schedule.CountUpToDate(), Is.EqualTo(0));

			schedule.CompleteOperation(
				OperationKindCode.FluidChange, AYearLater, 40000, DistanceUnitCode.UnitA);

			Assert.That(schedule.CountUpToDate(), Is.EqualTo(1),
				"La sucesora arranca sin vencer, y la cuenta la sigue en vez de arrastrar la vieja.");
			Assert.That(schedule.CountScheduled(), Is.EqualTo(1),
				"Hacer la operacion no agrega una clase a la agenda: la sucesora ocupa el mismo sitio.");
		}

		// NINGUNO DE LOS DOS SE GUARDA. La misma ley que RemainingIsDerivedNeverStored afirma de lo
		// que queda: un contador en un campo se desfasa la primera vez que algo cambia por otro
		// camino, y aca hay dos caminos -- evaluar y completar.
		[Test]
		public void NeitherCountIsStoredAnywhere()
		{
			var fields = new System.Collections.Generic.List<string>();

			foreach (System.Reflection.FieldInfo each in typeof(MaintenanceSchedule).GetFields(
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
				| System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly))
				fields.Add(each.Name);

			Assert.That(fields, Has.No.Member("_upToDate"));
			Assert.That(fields, Has.No.Member("_scheduled"));
			Assert.That(fields, Has.No.Member("_countUpToDate"));

			// Y EL CONTROL: el censo de campos de verdad enumera algo. Sin este renglon, los tres de
			// arriba pasan igual si GetFields devolviera vacio.
			Assert.That(fields, Contains.Item("_operations"));
			Assert.That(fields, Contains.Item("_order"));
		}

		private void Seed(OperationKindCode kind)
		{
			schedule.SeedPlanOperation(
				kind, IntervalBasisCode.TimeOrDistance, TwelveMonthsInDays, TenThousandUnits,
				DistanceUnitCode.UnitA, true, false,
				Delivery, 30000, DistanceUnitCode.UnitA);
		}
	}
}
