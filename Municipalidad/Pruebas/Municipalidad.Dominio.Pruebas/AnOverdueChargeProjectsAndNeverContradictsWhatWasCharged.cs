using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// M7, propuesto: la tasa de mora, SOLO PARA PROYECTAR. La mora real la recibe la app en el recibo o
	// en la consulta, tal cual la municipalidad la cobro; lo unico que el Atraso calcula es una
	// PROYECCION, y se muestra como proyeccion. Decision de producto del 3 de setiembre: recibir y
	// alarmar, no verificar.
	//
	// Mudado desde Municipalidad.Consola en M-6.
	[TestFixture]
	public sealed class AnOverdueChargeProjectsAndNeverContradictsWhatWasCharged
	{
		private static Atraso ElDeBasuraAl(Cuenta c, DateTime fecha)
		{
			foreach (Atraso a in c.AtrasosAl(fecha))
				if (a.Rubro.Es(Rubro.Basura())) return a;

			throw new InvalidOperationException(
				"El expediente de demo tiene que dejar un atraso de basura al " + fecha + ", y no lo "
				+ "dejo. Sin el, las afirmaciones de abajo no estarian mirando nada.");
		}

		[Test]
		public void NothingIsOverdueBeforeItsClosingDayAndEverythingIsAfterIt()
		{
			Cuenta c = ElExpedienteDeDemo.UnaCuentaCompleta();

			Assert.Multiple(() =>
			{
				Assert.That(c.AtrasosAl(new DateTime(2025, 9, 3)), Is.Empty,
					"Al 3 de setiembre no hay atrasos");
				Assert.That(c.AtrasosAl(new DateTime(2025, 10, 5)), Has.Count.EqualTo(5),
					"Al 5 de octubre hay cinco");
			});
		}

		[Test]
		public void WhatIsOverdueIsUrgentByClassAndItsLateCostIsAProjection()
		{
			Cuenta c = ElExpedienteDeDemo.UnaCuentaCompleta();
			Atraso basura = ElDeBasuraAl(c, new DateTime(2025, 10, 5));

			Assert.Multiple(() =>
			{
				Assert.That(basura.DiasDeAtraso(new DateTime(2025, 10, 5)), Is.EqualTo(5),
					"La basura lleva 5 dias vencida");
				Assert.That(basura.Urgencia().EsUrgente(), Is.True,
					"Y es urgente por CLASE, no por monto");
				Assert.That(
					basura.CostoDePagarTarde(new DateTime(2025, 10, 5), 0.062m), Is.EqualTo(20.38m),
					"Pagarla el 5 a 6,2 % anual costaria 20,38 -proyectado, no cobrado-");
				Assert.That(
					basura.CostoDePagarTarde(new DateTime(2025, 10, 5), 0m), Is.EqualTo(0m),
					"A tasa cero, cero");
			});
		}
	}
}
