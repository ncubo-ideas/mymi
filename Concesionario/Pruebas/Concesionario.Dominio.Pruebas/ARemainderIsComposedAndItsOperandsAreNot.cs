using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LEY L1 DE SLICE-TheMaintenanceScreen: el resto se resta ADENTRO y sale como un numero con su
	// unidad. travelled y effectiveLimit no llegan nunca los dos a un llamador, en ninguna grafia,
	// en ninguna superficie.
	//
	// LA RESTA SE ESCRIBE UNA SOLA VEZ, en AMeasuredDimension, y el signo elige la variante. Antes
	// de MS-1 no existia: MS-0 barrio los 110 archivos de Dominio y las unicas dos restas del tipo
	// "lo que queda" estaban en AWarrantyEnvelope (87 y 104), del cuarto agregado, y ninguna de las
	// dos llega a App/ como numero.
	//
	// POR QUE LA MITAD ESTRUCTURAL HACE FALTA ADEMAS DE LA NUMERICA. Que el monto sea correcto no
	// dice nada sobre si los dos operandos siguen adentro: una variante que guardara el limite y
	// lo proyectara pasaria las tres lecturas sucesivas igual. El censo de miembros de ARemainder
	// es lo que ve eso.
	//
	// Y LA OBLIGACION 2 DE E49, que es la que se salta sola: comparar el resto contra lo que
	// Evaluate decidio compara DOS CONSUMIDORES DE UN MISMO RESOLUTOR -- se ponen de acuerdo por
	// construccion, tambien cuando el resolutor esta uniformemente mal. Algo tiene que anclar un
	// lado a las lecturas RECIBIDAS, y eso es
	// TheRemainderIsAnchoredToTheReceivedReadingsAndNotToTheBoard, con literales.
	[TestFixture]
	public sealed class ARemainderIsComposedAndItsOperandsAreNot
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TenThousandUnits = 10000;
		private const int TheOdometerAtDelivery = 30000;

		// LOS TRES MONTOS NO SE CRUZAN CON NADA (C60): 5800, 1200 y 300 no coinciden entre si, ni
		// con el limite, ni con los 66 dias que quedan del plazo, ni con ninguna lectura.
		private const int TheFirstReading = 34200;
		private const int TheFirstAmountLeft = 5800;
		private const int TheSecondReading = 38800;
		private const int TheSecondAmountLeft = 1200;
		private const int TheThirdReading = 40300;
		private const int TheThirdOvershoot = 300;

		private const int TheTimeLimitInDays = 400;
		private const int TheDaysElapsedWhenAsked = 334;
		private const int TheDaysLeft = 66;

		private static readonly DateTime Delivery = new DateTime(2025, 6, 1);

		private static DateTime TheInstantAsked
		{
			get { return Delivery.AddDays(TheDaysElapsedWhenAsked); }
		}

		// LAS TRES LECTURAS SUCESIVAS, sobre UNA agenda: el resto baja con cada una y nada se
		// guarda entre ellas. Es E35 -- derivado en cada lectura -- puesto a prueba por consecuencia.
		[Test]
		public void TheAmountIsRightAtThreeSuccessiveReadings()
		{
			MaintenanceSchedule schedule = AScheduleMeasuredInBothDimensions();

			schedule.RecordReading(
				TheFirstReading, DistanceUnitCode.UnitA, Delivery.AddDays(100),
				ReadingSourceCode.Telemetry);

			Assert.That(
				schedule.ReadDistanceRemaining(OperationKindCode.FluidChange, TheInstantAsked),
				Is.EqualTo(TheFirstAmountLeft));

			schedule.RecordReading(
				TheSecondReading, DistanceUnitCode.UnitA, Delivery.AddDays(200),
				ReadingSourceCode.Telemetry);

			Assert.That(
				schedule.ReadDistanceRemaining(OperationKindCode.FluidChange, TheInstantAsked),
				Is.EqualTo(TheSecondAmountLeft));

			schedule.RecordReading(
				TheThirdReading, DistanceUnitCode.UnitA, Delivery.AddDays(300),
				ReadingSourceCode.Telemetry);

			Assert.That(
				schedule.ReadDistanceRemaining(OperationKindCode.FluidChange, TheInstantAsked),
				Is.EqualTo(TheThirdOvershoot),
				"Pasado el limite el monto es el EXCESO, y sigue siendo positivo. El signo elige la "
				+ "variante; no hay resto negativo.");
		}

		// EL EVALUATE NO ES LO QUE MUEVE EL RESTO. Sin llamarlo nunca, las tres lecturas dan los
		// tres montos: el resto se compone en cada lectura y no sale de ningun estado registrado.
		[Test]
		public void NothingIsStoredBetweenThem()
		{
			MaintenanceSchedule schedule = AScheduleMeasuredInBothDimensions();

			schedule.RecordReading(
				TheSecondReading, DistanceUnitCode.UnitA, Delivery.AddDays(200),
				ReadingSourceCode.Telemetry);

			int beforeEvaluating =
				schedule.ReadDistanceRemaining(OperationKindCode.FluidChange, TheInstantAsked);

			schedule.Evaluate(TheInstantAsked);

			Assert.That(
				schedule.ReadDistanceRemaining(OperationKindCode.FluidChange, TheInstantAsked),
				Is.EqualTo(beforeEvaluating),
				"Evaluate registra un estado; el resto no lo lee. Si lo leyera, la pantalla diria "
				+ "un numero de la ultima evaluacion en vez del de ahora.");
		}

		// E49, OBLIGACION 2. Dos lecturas recibidas y el resto preguntado a un instante ENTRE las
		// dos: tiene que dar el que implica la PRIMERA. Es lo que se pone rojo si el resolutor
		// compartido pierde su filtro "<= asOf" -- el caso en que las dos vias siguen de acuerdo
		// sobre un numero que ninguna de las dos deberia haber usado.
		[Test]
		public void TheRemainderIsAnchoredToTheReceivedReadingsAndNotToTheBoard()
		{
			MaintenanceSchedule schedule = AScheduleMeasuredInBothDimensions();

			schedule.RecordReading(
				TheFirstReading, DistanceUnitCode.UnitA, Delivery.AddDays(100),
				ReadingSourceCode.Telemetry);
			schedule.RecordReading(
				TheThirdReading, DistanceUnitCode.UnitA, Delivery.AddDays(300),
				ReadingSourceCode.Telemetry);

			Assert.That(
				schedule.ReadDistanceRemaining(
					OperationKindCode.FluidChange, Delivery.AddDays(200)),
				Is.EqualTo(TheFirstAmountLeft),
				"A un instante entre las dos observaciones manda la PRIMERA lectura. Anclado a lo "
				+ "recibido y no a lo que otra via haya decidido con lo mismo.");

			Assert.That(
				schedule.ReadDistanceRemaining(OperationKindCode.FluidChange, TheInstantAsked),
				Is.EqualTo(TheThirdOvershoot),
				"Y pasada la segunda observacion, la MISMA agenda contesta con la segunda lectura: "
				+ "la clave es el instante preguntado.");
		}

		// EL AVANCE SIGUE PROTEGIDO. Su propio /// dice para que -- que nadie ordene DueStates por
		// el -- y part A no le da ninguna razon nueva para salir: el resto no rankea nada.
		[Test]
		public void AdvancementStaysProtected()
		{
			MethodInfo advancement = typeof(LimitStanding).GetMethod(
				"Advancement",
				BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
					| BindingFlags.DeclaredOnly);

			Assert.That(advancement, Is.Not.Null);
			Assert.That(advancement.IsFamily, Is.True,
				"Advancement dejo de ser protected. Part A no lo necesita y §1.1 del brief lo dice: "
				+ "no es la cuenta atras y no se reusa como una.");
		}

		// LA MITAD ESTRUCTURAL DE L1: ningun miembro de ARemainder entrega un operando. Ni por
		// nombre -- la lista prohibida del §4 del brief -- ni por tipo, devolviendo la dimension
		// que los tiene.
		[Test]
		public void NoMemberOfTheRemainderHandsOutAnOperand()
		{
			string[] theBannedNames =
			{
				"Travelled", "ElapsedDays", "EffectiveLimit", "EffectiveLimitDays",
				"Progress", "PercentComplete", "Advancement", "Limit",
			};

			List<string> found = EveryMemberOfTheRemainder()
				.Where(member => theBannedNames.Any(banned =>
					member.Name.IndexOf(banned, StringComparison.OrdinalIgnoreCase) >= 0))
				.Select(member => member.Name)
				.ToList();

			Assert.That(found, Is.Empty,
				"ARemainder entrega un operando: " + string.Join(", ", found)
				+ ". Los dos juntos son el par que L1 existe para mantener separado.");

			List<string> carriers = EveryMemberOfTheRemainder()
				.Where(member => member.ReturnType == typeof(LimitStanding)
					|| member.ReturnType == typeof(AMeasuredDimension))
				.Select(member => member.Name)
				.ToList();

			Assert.That(carriers, Is.Empty,
				"ARemainder devuelve la dimension que tiene los operandos: "
				+ string.Join(", ", carriers) + ". Quien la reciba los resta el mismo.");
		}

		// Y LA MISMA CENSURA SOBRE LA AGENDA, que es la que las diez lecturas de part A cuelgan:
		// ninguna de ellas entrega un ARemainder. Lo que sale son escalares (regla de salida de E3).
		[Test]
		public void NoRemainderCarrierCrossesTheScheduleSurface()
		{
			List<string> exposed = typeof(MaintenanceSchedule)
				.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
				.Where(member => member.ReturnType == typeof(ARemainder))
				.Where(member => member.IsPublic || member.IsAssembly)
				.Select(member => member.Name)
				.ToList();

			Assert.That(exposed, Is.Empty,
				"La agenda expone un portador ARemainder: " + string.Join(", ", exposed)
				+ ". El perform no lo puede llevar de vuelta, y quien lo reciba puede volver a "
				+ "derivar lo que el agregado ya compuso.");
		}

		private static IEnumerable<MethodInfo> EveryMemberOfTheRemainder()
		{
			return typeof(ARemainder).GetMethods(
				BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
		}

		private static MaintenanceSchedule AScheduleMeasuredInBothDimensions()
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

		// EL PLAZO ENTRA EN ESTE FIXTURE por una razon de C60: sin el, los 66 dias que quedan no
		// existirian y un cruce entre las dos dimensiones no se veria. Se afirma aca para que el
		// numero exista y sea distinto de los tres de distancia.
		[Test]
		public void TheTimeHalfIsComposedTheSameWayAndItsAmountIsItsOwn()
		{
			MaintenanceSchedule schedule = AScheduleMeasuredInBothDimensions();

			Assert.That(
				schedule.ReadDaysRemaining(OperationKindCode.FluidChange, TheInstantAsked),
				Is.EqualTo(TheDaysLeft));

			Assert.That(
				schedule.ReadDaysRemaining(OperationKindCode.FluidChange, TheInstantAsked),
				Is.Not.EqualTo(TheSecondAmountLeft),
				"Las dos dimensiones tienen que poder contestar numeros distintos al mismo "
				+ "instante: son incomparables y cada una se mide contra SU limite.");
		}
	}
}
