using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// §6 DEL SLICE: dos clases de numero, dos casas.
	//
	// Los limites POR OPERACION -- cuantos dias, cuanta distancia -- llegan del plan del
	// fabricante y son DATOS recibidos al sembrar. No estan aca y no deben estar aca.
	//
	// La politica de la agencia son los SIETE umbrales de §6, y son constantes con nombre en
	// SchedulePolicy. Esta prueba fija sus valores por dos razones distintas:
	//
	//   1. que nadie los retoque sin querer. Bajar OverdueGrace de 14 a 7 dias cambia cuando
	//      una persona se entera de que se le paso un servicio, y no es un cambio que deba
	//      poder colarse en un commit de otra cosa.
	//
	//   2. que sigan estando NOMBRADOS. E17: una cuenta correcta sin nombre es un defecto en
	//      este slice. El dia que alguien escriba "30" adentro de una regla en vez de
	//      nombrar DueSoonLeadDays, la constante deja de tener quien la use -- y esta prueba
	//      la mantiene viva y visible mientras eso se corrige.
	//
	// GAP G5, y esta escrito en el /// del tipo: que sean constantes es una DECISION DE ESTE
	// SLICE, no una creencia. En OS5 los topes de dinero son por-otorgamiento y genuinamente
	// no pueden ser constantes. Cuando eso pase, SchedulePolicy cambia de forma -- y nada mas
	// cambia, que es de lo que sirve tener una sola casa para estos numeros.
	[TestFixture]
	public sealed class TheSchedulePoliciesAreNamedThresholds
	{
		[Test]
		public void DueSoonLeadIsThirtyDaysOrOneThousandDistanceUnits()
		{
			Assert.That(SchedulePolicy.DueSoonLeadDays, Is.EqualTo(30));
			Assert.That(SchedulePolicy.DueSoonLeadDistance, Is.EqualTo(1000));
		}

		[Test]
		public void OverdueGraceIsFourteenDaysOrFiveHundredUnitsPastTheDueDate()
		{
			Assert.That(SchedulePolicy.OverdueGraceDays, Is.EqualTo(14));
			Assert.That(SchedulePolicy.OverdueGraceDistance, Is.EqualTo(500));
		}

		[Test]
		public void TheDeferralCapIsTwo()
		{
			Assert.That(SchedulePolicy.MaxDeferralsPerOperation, Is.EqualTo(2));
		}

		[Test]
		public void TheDeferralWindowIsSixtyDays()
		{
			Assert.That(SchedulePolicy.DeferralWindowDays, Is.EqualTo(60));
		}

		[Test]
		public void SevereDutyShortensTheIntervalsToSeventyPercent()
		{
			Assert.That(SchedulePolicy.SevereDutyIntervalFactor, Is.EqualTo(0.70m));
		}

		[Test]
		public void LowDistanceLeavesTheTimeLimitsIntact()
		{
			Assert.That(SchedulePolicy.LowDistanceTimeFactor, Is.EqualTo(1.00m));
		}

		[Test]
		public void TheBundlingWindowIsSixtyDaysOrTwoThousandDistanceUnits()
		{
			Assert.That(SchedulePolicy.BundlingWindowDays, Is.EqualTo(60));
			Assert.That(SchedulePolicy.BundlingWindowDistance, Is.EqualTo(2000));
		}

		// SchedulePolicy es un CATALOGO de umbrales, no un objeto con estado. Que nadie lo
		// pueda instanciar es lo que hace que no haya dos politicas distintas circulando
		// mientras este slice dice que hay una.
		[Test]
		public void SchedulePolicyIsNotInstantiated()
		{
			Assert.That(typeof(SchedulePolicy).IsAbstract && typeof(SchedulePolicy).IsSealed,
				Is.True,
				"SchedulePolicy dejo de ser un catalogo estatico. Si esto cambio a proposito -- "
				+ "porque llego la configuracion de G5 -- el /// del tipo tiene que decirlo y "
				+ "esta prueba tiene que cambiar con la razon escrita.");
		}

		// Cada umbral tiene NOMBRE, y todos los que §6 nombra estan. Que sobre uno es tan
		// interesante como que falte: un numero de politica que nadie declaro en §6 llego
		// por un camino que no es el slice.
		[Test]
		public void TheTenNamesOfTheSevenThresholdsAreThereAndNoMore()
		{
			string[] esperados =
			{
				"BundlingWindowDays",
				"BundlingWindowDistance",
				"DeferralWindowDays",
				"DueSoonLeadDays",
				"DueSoonLeadDistance",
				"LowDistanceTimeFactor",
				"MaxDeferralsPerOperation",
				"OverdueGraceDays",
				"OverdueGraceDistance",
				"SevereDutyIntervalFactor",
			};

			List<string> declarados = typeof(SchedulePolicy)
				.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
					| BindingFlags.DeclaredOnly)
				.Where(campo => campo.IsPublic || campo.IsAssembly)
				.Select(campo => campo.Name)
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToList();

			Assert.That(declarados, Is.EqualTo(esperados),
				"Cambio la lista de umbrales de politica. §6 nombra siete; tres de ellos son "
				+ "compuestos (dias O distancia) y se declaran en dos mitades porque dias y "
				+ "unidades de distancia no se pueden sumar -- eso es OINV-21 otra vez.");
		}
	}
}
