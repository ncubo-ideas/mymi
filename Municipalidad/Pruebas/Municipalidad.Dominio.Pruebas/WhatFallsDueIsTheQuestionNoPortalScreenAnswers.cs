using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// M5, propuesto: avisar SIETE DIAS antes del vencimiento, urgente el dia. Es la pregunta que
	// NINGUNA PANTALLA DEL PORTAL contesta -- el portal muestra lo puesto al cobro, no cuando vence --
	// y la que la persona pidio con la palabra "recordatorios".
	//
	// Mudado desde Municipalidad.Consola en M-6. Los dias de aviso entran por parametro y la fecha de
	// corte tambien: el dominio no lee el reloj.
	[TestFixture]
	public sealed class WhatFallsDueIsTheQuestionNoPortalScreenAnswers
	{
		private static VencimientoProximo ElDeBasuraAl(Cuenta c, DateTime fecha)
		{
			foreach (VencimientoProximo v in c.VencimientosAl(fecha))
				if (v.Rubro.Es(Rubro.Basura())) return v;

			throw new InvalidOperationException(
				"El expediente de demo tiene que dejar un vencimiento de basura al " + fecha + ", y no "
				+ "lo dejo. Sin el, las afirmaciones de abajo no estarian mirando nada.");
		}

		[Test]
		public void WhatIsOnChargeAndNotYetDueIsListedWithItsOwnClosingDay()
		{
			Cuenta c = ElExpedienteDeDemo.UnaCuentaCompleta();
			IReadOnlyList<VencimientoProximo> proximos = c.VencimientosAl(new DateTime(2025, 9, 3));

			VencimientoProximo basura = ElDeBasuraAl(c, new DateTime(2025, 9, 3));

			Assert.Multiple(() =>
			{
				Assert.That(proximos, Has.Count.EqualTo(5),
					"Al 3 de setiembre hay cinco cosas puestas al cobro sin vencer");
				Assert.That(basura.Vence, Is.EqualTo(new DateTime(2025, 9, 30)),
					"La basura vence el 30 de setiembre");
				Assert.That(basura.DiasQueFaltan(new DateTime(2025, 9, 3)), Is.EqualTo(27),
					"Faltan 27 dias");
			});
		}

		[Test]
		public void TheWarningWindowIsAParameterAndTheDayItselfIsUrgent()
		{
			Cuenta c = ElExpedienteDeDemo.UnaCuentaCompleta();
			VencimientoProximo basura = ElDeBasuraAl(c, new DateTime(2025, 9, 3));

			Assert.Multiple(() =>
			{
				Assert.That(basura.Urgencia(new DateTime(2025, 9, 3), 7).Avisa(), Is.False,
					"A 27 dias, con 7 de aviso, no avisa");
				Assert.That(basura.Urgencia(new DateTime(2025, 9, 25), 7).Es(Urgencia.Aviso()), Is.True,
					"A 5 dias avisa");
				Assert.That(basura.Urgencia(new DateTime(2025, 9, 30), 7).EsUrgente(), Is.True,
					"El dia que vence es urgente");
			});
		}

		[Test]
		public void TheListIsTotalAndEmptyWhereNothingIsDue()
		{
			Cuenta c = ElExpedienteDeDemo.UnaCuentaCompleta();

			Assert.Multiple(() =>
			{
				Assert.That(c.VencimientosAl(new DateTime(2025, 10, 5)), Is.Empty,
					"Al 5 de octubre ya no hay vencimientos proximos");
				Assert.That(new Cuenta().VencimientosAl(new DateTime(2025, 9, 3)), Is.Empty,
					"Sin nada al cobro, la lista esta vacia y no revienta");
			});
		}
	}
}
