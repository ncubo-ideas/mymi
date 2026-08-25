using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// IntervalLengthFactor: el escalamiento declarado que un UsageProfile aplica al LARGO de
	// un intervalo (§3.3 del plan, §6 del slice). Soporta la ley L8,
	// UsageAdjustsLengthNotBasis, cuyo nombre vive en MaintenanceSchedule (T5).
	//
	// LO QUE ESTE TIPO TIENE QUE HACER IMPOSIBLE, no solo evitar: OINV-22 y OHS-07 dicen que
	// un perfil de uso escala el LARGO y NADA MAS. No cambia la base de una operacion -- una
	// TimeOrDistance no se vuelve TimeOnly porque el vehiculo trabaje duro -- y no quita
	// operaciones del plan.
	//
	// Esa mitad se prueba con una LISTA BLANCA, igual que la ausencia de conversion en
	// DistanceUnit: el factor expone exactamente cinco miembros, ninguno de los cuales
	// devuelve una regla ni un codigo de base. Una ausencia no se puede afirmar llamando a
	// algo. La tension que OHS-07 nombra es real -- "una sucursal que acorta intervalos para
	// vender servicios destruye la confianza" -- y el unico ajuste permitido son estos
	// factores.
	//
	// LowDistance es el miembro raro y por eso lleva pruebas propias: no escala la distancia,
	// LA IGNORA. Un factor de cero no serviria -- un limite de cero unidades vence en el acto
	// -- asi que ignorar es una tercera cosa, y cae en el mismo cajon que la discrepancia de
	// unidad: la dimension NO SE PUEDE EVALUAR (G2).
	[TestFixture]
	public sealed class TheLengthFactorScalesTheLengthNotTheBasis
	{
		private static readonly string[] LoQueElFactorPuedeExponer =
		{
			"For",
			"Profile",
			"DistanceIsIgnored",
			"ApplyToTimeLimitDays",
			"ApplyToDistanceLimit",
		};

		[Test]
		public void TheFactorExposesNothingOutsideTheAllowlist()
		{
			List<string> intrusos = typeof(IntervalLengthFactor)
				.GetMembers(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
				.Where(EsExpuesto)
				.Select(miembro => miembro.Name)
				.Distinct()
				.Where(nombre => !LoQueElFactorPuedeExponer.Contains(nombre))
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToList();

			Assert.That(intrusos, Is.Empty,
				"IntervalLengthFactor gano miembros: " + string.Join(", ", intrusos) + ". "
				+ "OINV-22 / ley L8: un perfil de uso escala el LARGO y nada mas. Un miembro que "
				+ "devuelva una regla, un codigo de base o una operacion le abre al perfil una "
				+ "puerta para cambiar la base -- que es justo el ajuste que OHS-07 cerro.");
		}

		[Test]
		public void StandardLeavesTheLimitsAsTheManufacturerGaveThem()
		{
			IntervalLengthFactor factor = IntervalLengthFactor.For(UsageProfile.Standard);

			Assert.That(factor.ApplyToTimeLimitDays(365), Is.EqualTo(365));
			Assert.That(factor.ApplyToDistanceLimit(10000), Is.EqualTo(10000));
			Assert.That(factor.DistanceIsIgnored, Is.False);
		}

		// 365 por 0.70 son 255.5, y se redondea al dia entero mas cercano. El redondeo es una
		// decision con nombre, no un truncamiento accidental: truncar acortaria el intervalo
		// una segunda vez, ademas del factor.
		[Test]
		public void SevereDutyLeavesSeventyPercentOfTheDeclaredLength()
		{
			IntervalLengthFactor factor = IntervalLengthFactor.For(UsageProfile.SevereDuty);

			Assert.That(factor.ApplyToTimeLimitDays(365), Is.EqualTo(256));
			Assert.That(factor.ApplyToDistanceLimit(10000), Is.EqualTo(7000));
			Assert.That(factor.DistanceIsIgnored, Is.False);
		}

		[Test]
		public void LowDistanceLeavesTimeIntactAndIgnoresDistance()
		{
			IntervalLengthFactor factor = IntervalLengthFactor.For(UsageProfile.LowDistance);

			Assert.That(factor.ApplyToTimeLimitDays(365), Is.EqualTo(365),
				"§6: 'los limites de tiempo quedan sin cambio'. Un vehiculo que casi no se mueve "
				+ "igual necesita sus operaciones por tiempo cumplido.");
			Assert.That(factor.DistanceIsIgnored, Is.True,
				"IGNORA la distancia, no la escala. Un factor de cero no sirve: un limite de cero "
				+ "unidades venceria en el acto.");
		}

		// §6 no declara factor para Commercial. Inventarle uno seria exactamente el camino que
		// OHS-07 cerro: acortar intervalos por una via que nadie declaro.
		[Test]
		public void CommercialHasNoDeclaredFactorAndSoScalesNothing()
		{
			IntervalLengthFactor factor = IntervalLengthFactor.For(UsageProfile.Commercial);

			Assert.That(factor.ApplyToTimeLimitDays(365), Is.EqualTo(365));
			Assert.That(factor.ApplyToDistanceLimit(10000), Is.EqualTo(10000));
		}

		[Test]
		public void AnAbsentLimitStaysAbsentAfterScaling()
		{
			IntervalLengthFactor factor = IntervalLengthFactor.For(UsageProfile.SevereDuty);

			Assert.That(factor.ApplyToTimeLimitDays(0), Is.EqualTo(0),
				"La ausencia de un limite se expresa con cero, y escalarla no la convierte en un "
				+ "limite.");
			Assert.That(factor.ApplyToDistanceLimit(0), Is.EqualTo(0));
		}

		// El factor escala el LARGO: la regla sigue siendo la misma regla y la misma base.
		// Con SevereDuty el limite de 365 dias queda en 256, asi que a los 256 dias vence algo
		// que con Standard todavia no vencia -- y sigue siendo una TimeOrDistance.
		[Test]
		public void TheSameIntervalFallsDueSoonerWithSevereDutyAndKeepsTheSameBasis()
		{
			IntervalRule regla = IntervalRule.Of(
				IntervalBasisCode.TimeOrDistance, 365, 10000, DistanceUnitCode.UnitA);

			BasePoint desde = BasePoint.At(new DateTime(2025, 6, 1), 30000, DistanceUnit.UnitA);
			DateTime alDiaDoscientosCincuentaYSeis = new DateTime(2026, 2, 12);
			OdometerReading lectura = new OdometerReading(
				30000, DistanceUnit.UnitA, alDiaDoscientosCincuentaYSeis, ReadingSource.Telemetry);

			DueAssessment conStandard = regla.Assess(
				desde, alDiaDoscientosCincuentaYSeis, lectura,
				IntervalLengthFactor.For(UsageProfile.Standard));

			DueAssessment conSevereDuty = regla.Assess(
				desde, alDiaDoscientosCincuentaYSeis, lectura,
				IntervalLengthFactor.For(UsageProfile.SevereDuty));

			Assert.That(conStandard.State, Is.SameAs(DueState.NotDue));
			Assert.That(conSevereDuty.State, Is.SameAs(DueState.Due));
			Assert.That(regla, Is.InstanceOf<TimeOrDistance>(),
				"El perfil escalo el largo. La regla es el mismo objeto y de la misma base: un "
				+ "perfil no cambia una TimeOrDistance en otra cosa (OINV-22).");
		}

		// LowDistance ignora la distancia, y eso cae en el mismo cajon que la discrepancia de
		// unidad: la dimension NO SE PUEDE EVALUAR, no es que no llego (G2).
		[Test]
		public void WithLowDistanceTheDistanceIsSuspendedAndSaidSo()
		{
			IntervalRule regla = IntervalRule.Of(
				IntervalBasisCode.TimeOrDistance, 365, 10000, DistanceUnitCode.UnitA);

			DueAssessment vencimiento = regla.Assess(
				BasePoint.At(new DateTime(2025, 6, 1), 30000, DistanceUnit.UnitA),
				new DateTime(2026, 1, 20),
				new OdometerReading(45000, DistanceUnit.UnitA, new DateTime(2026, 1, 20),
					ReadingSource.Telemetry),
				IntervalLengthFactor.For(UsageProfile.LowDistance));

			Assert.That(vencimiento.DistanceIsSuspended, Is.True);
			Assert.That(vencimiento.SuspendedByTheUsageProfile, Is.True);
			Assert.That(vencimiento.State, Is.SameAs(DueState.NotDue),
				"15000 unidades recorridas de un limite de 10000, y aun asi no vence: con "
				+ "LowDistance los limites de distancia se ignoran y decide el tiempo, que va en "
				+ "233 de 365.");
			Assert.That(vencimiento.Describe(), Does.Contain("LowDistance"));
		}

		private static bool EsExpuesto(MemberInfo miembro)
		{
			if (miembro is FieldInfo campo) return campo.IsPublic || campo.IsAssembly;
			if (miembro is PropertyInfo propiedad)
			{
				MethodInfo lector = propiedad.GetGetMethod(true);
				return lector != null && (lector.IsPublic || lector.IsAssembly);
			}
			if (miembro is MethodInfo metodo)
				return !metodo.IsSpecialName && (metodo.IsPublic || metodo.IsAssembly);
			if (miembro is ConstructorInfo ctor) return ctor.IsPublic || ctor.IsAssembly;
			return false;
		}
	}
}
