using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// DECISION D2 DEL PLAN (§4.3), tomada corriendo la prueba del Curador de E39 -- "quitele los
	// nombres a las subclases y muevalos a dato: se pierde alguna regla o algun comportamiento?"
	//
	// VEREDICTO: EnvelopeDimension NO SOBREVIVE. El brief §5 lo hace conjunto cerrado de dos,
	// Time y Distance, y no se pierde ninguna regla al sacarlo. Tres razones, y la tercera es la
	// que decide:
	//
	//   1. EL MODULO YA LO DICE POR SUBTIPO. AMeasuredTimeDimension y AMeasuredDistanceDimension
	//      se nombran solas -- "time" y "distance" -- por un miembro PROTEGIDO que nunca sale del
	//      tipo. Y las dos difieren en COMPORTAMIENTO, no en dato: los dias se pluralizan y la
	//      distancia se dice en la unidad de su limite. Eso es territorio de subtipo (E11, E39),
	//      no de vocabulario.
	//
	//   2. NADA EN LA SUPERFICIE DEVUELVE UNA DIMENSION. La tabla de lecturas §4.2 del plan no
	//      tiene ninguna que conteste una dimension: cual llega primero se DICE adentro de la
	//      frase que el dominio compone (E24). Un conjunto cerrado que nunca cruza ninguna
	//      frontera es vocabulario sin lector.
	//
	//   3. Y ESTA ES LA QUE DECIDE: "cual decidio" TIENE TRES RESPUESTAS HONESTAS y un conjunto de
	//      dos miembros tiene dos. DueAssessment ya lo descubrio y lo resolvio con DOS PREDICADOS
	//      TOTALES -- DecidedByTime y DecidedByDistance -- que pueden ser los dos verdaderos a la
	//      vez, y su frase dice "decidido por tiempo y distancia". Un EnvelopeDimension obligaria a
	//      elegir una de las dos en el empate, o a agregarle un tercer miembro que ya no seria una
	//      dimension. Es exactamente el argumento de L9 -- que EnvelopeStanding no sea un booleano
	//      -- aplicado a la otra pregunta del slice.
	//
	// LO QUE ESTA PRUEBA DEFIENDE, entonces, son las dos mitades del veredicto: que el tipo no
	// esta, y que el precedente sobre el que se retiro SIGUE EN PIE. Si manana alguien borra el
	// auto-nombrado de las dos dimensiones medidas, o colapsa los dos predicados de DueAssessment
	// en uno, el argumento de D2 se cayo y hay que rehacerlo -- y esto es lo que lo pone rojo, en
	// vez de dejarlo escrito en un plan que nadie relee.
	[TestFixture]
	public sealed class TheDimensionIsNamedByWhoeverSaysIt
	{
		private const int TwelveMonthsInDays = 365;
		private const int TenThousandUnits = 10000;
		private const int TheAnchorReading = 30000;

		private static readonly DateTime TheAnchorInstant = new DateTime(2025, 6, 1);

		private BasePoint from;
		private IntervalLengthFactor unscaled;

		[SetUp]
		public void Setup()
		{
			from = BasePoint.At(TheAnchorInstant, TheAnchorReading, DistanceUnit.UnitA);
			unscaled = IntervalLengthFactor.For(UsageProfile.Standard);
		}

		// ===== LA MITAD QUE AFIRMA LA AUSENCIA =============================================

		[Test]
		public void ThereIsNoEnvelopeDimensionTypeAndNoBoundaryEnumForOne()
		{
			IEnumerable<string> tipos = typeof(EnvelopeStanding).Assembly.GetTypes()
				.Select(tipo => tipo.Name);

			Assert.That(tipos, Does.Not.Contain("EnvelopeDimension"));
			Assert.That(tipos, Does.Not.Contain("EnvelopeDimensionCode"));
		}

		// Y NO ALCANZA CON PROHIBIR EL NOMBRE: el mismo vocabulario con otro nombre es el mismo
		// error. Lo que se prohibe es la FORMA -- un conjunto cerrado cuyos miembros sean
		// exactamente Time y Distance -- se llame como se llame.
		[Test]
		public void NoClosedSetAnywhereDeclaresTimeAndDistanceAsItsMembers()
		{
			List<string> culpables = new List<string>();

			foreach (Type tipo in typeof(EnvelopeStanding).Assembly.GetTypes())
			{
				List<string> miembros = tipo
					.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
						| BindingFlags.DeclaredOnly)
					.Where(campo => campo.IsInitOnly && campo.FieldType == tipo)
					.Select(campo => campo.Name)
					.OrderBy(nombre => nombre, StringComparer.Ordinal)
					.ToList();

				if (miembros.Count == 2 && miembros[0] == "Distance" && miembros[1] == "Time")
					culpables.Add(tipo.Name);
			}

			Assert.That(culpables, Is.Empty,
				"D2: aparecio un vocabulario de dos miembros para tiempo y distancia: "
				+ string.Join(", ", culpables) + ". La distincion ya la lleva el SUBTIPO que la "
				+ "dice, y 'cual decidio' tiene tres respuestas honestas -- tiempo, distancia, y "
				+ "las dos -- que un conjunto de dos miembros no puede dar.");
		}

		// ===== LA MITAD QUE AFIRMA EL PRECEDENTE ===========================================

		// RAZON 1. Cada dimension medida se nombra sola dentro de su propia clausula.
		[Test]
		public void EachMeasuredDimensionNamesItselfInItsOwnClause()
		{
			Assert.That(LaDimensionDeTiempo().Describe(), Does.Contain("tiempo"));
			Assert.That(LaDimensionDeDistancia().Describe(), Does.Contain("distancia"));
		}

		// Y el nombre NO SALE como valor: el miembro que lo lleva no es alcanzable desde afuera
		// del tipo. Es la mitad que vuelve innecesario el vocabulario -- si el nombre saliera,
		// alguien de afuera tendria que tener con que compararlo.
		[Test]
		public void TheDimensionsNameNeverLeavesTheTypeAsAValue()
		{
			foreach (Type tipo in new[]
				{ typeof(AMeasuredTimeDimension), typeof(AMeasuredDistanceDimension) })
			{
				PropertyInfo nombre = tipo.GetProperty("Name",
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
					| BindingFlags.DeclaredOnly);

				Assert.That(nombre, Is.Not.Null, tipo.Name + " dejo de nombrarse solo.");

				MethodInfo lector = nombre.GetGetMethod(true);

				Assert.That(lector.IsPublic || lector.IsAssembly, Is.False,
					tipo.Name + ".Name se volvio alcanzable desde afuera del tipo. Un nombre de "
					+ "dimension que sale como valor es un vocabulario, y entonces D2 hay que "
					+ "volver a decidirlo.");
			}
		}

		// RAZON 3, Y LA QUE DECIDE. Las dos dimensiones pueden decidir A LA VEZ, y la respuesta lo
		// dice con DOS predicados totales en vez de con un miembro. Un conjunto de dos no puede.
		//
		// EL ESCENARIO ESTA ELEGIDO PARA QUE EL EMPATE SEA REAL Y NO UN DEFAULT (C60): un dia y
		// una unidad recorridos, las dos dimensiones lejisimos de su limite, las dos NotDue.
		[Test]
		public void WhenBothDimensionsArriveTogetherTheAnswerSaysBothAndNotOne()
		{
			DueAssessment tie = LaReglaDeLasDos().Assess(
				from, TheAnchorInstant.AddDays(1),
				LaLectura(TheAnchorReading + 1, DistanceUnit.UnitA), unscaled);

			Assert.That(tie.DecidedByTime, Is.True);
			Assert.That(tie.DecidedByDistance, Is.True,
				"Las dos llegaron igual de lejos, asi que deciden las dos. Con un solo miembro de "
				+ "un conjunto de dos habria que elegir una, y elegir seria inventar.");
			Assert.That(tie.Describe(), Does.Contain("tiempo y distancia"));
		}

		// Y cuando decide UNA sola, los dos predicados tambien lo dicen -- asi que la forma de dos
		// predicados cubre los tres casos y el vocabulario no aporta ninguno.
		[Test]
		public void WhenOneDimensionArrivesFirstTheSameTwoPredicatesSayWhich()
		{
			DueAssessment porDistancia = LaReglaDeLasDos().Assess(
				from, new DateTime(2026, 1, 15),
				LaLectura(39000, DistanceUnit.UnitA), unscaled);

			Assert.That(porDistancia.DecidedByDistance, Is.True);
			Assert.That(porDistancia.DecidedByTime, Is.False);
			Assert.That(porDistancia.Describe(), Does.Contain("decidido por distancia"));
		}

		private LimitStanding LaDimensionDeTiempo()
		{
			return LimitStanding.OfTime(
				TheAnchorInstant, new DateTime(2026, 1, 15), TwelveMonthsInDays, unscaled);
		}

		private LimitStanding LaDimensionDeDistancia()
		{
			return LimitStanding.OfDistance(
				from, LaLectura(39000, DistanceUnit.UnitA), DistanceUnit.UnitA,
				TenThousandUnits, unscaled);
		}

		private static IntervalRule LaReglaDeLasDos()
		{
			return IntervalRule.Of(
				IntervalBasisCode.TimeOrDistance, TwelveMonthsInDays, TenThousandUnits,
				DistanceUnitCode.UnitA);
		}

		private static UsableReading LaLectura(int value, DistanceUnit unit)
		{
			return UsableReading.Of(new OdometerReading(
				value, unit, new DateTime(2026, 1, 1), ReadingSource.Telemetry));
		}
	}
}
