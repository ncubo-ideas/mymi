using System;
using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// M6, propuesto: avisar en enero que pagar todo el anio en el primer trimestre tiene descuento, con
	// el ahorro calculado. Es la mora al reves -- misma tasa, signo contrario -- y la tasa entra por
	// parametro. Juicio 4, firmado: por todo el monto, prorrateado por el tiempo que se adelanta.
	//
	// Mudado desde Municipalidad.Consola en M-6.
	[TestFixture]
	public sealed class TheEarlyPaymentDiscountIsTheLateFeeInReverse
	{
		[Test]
		public void TheEarlierItIsPaidTheMoreIsSavedAndAtZeroRateNothingIs()
		{
			Cuenta c = ElExpedienteDeDemo.UnaCuentaCompleta();

			decimal enSetiembre = c.AhorroPorProntoPago(new DateTime(2025, 9, 3), 0.062m);
			decimal enNoviembre = c.AhorroPorProntoPago(new DateTime(2025, 11, 1), 0.062m);

			Assert.Multiple(() =>
			{
				Assert.That(enSetiembre, Is.GreaterThan(0m),
					"En setiembre todavia hay algo que adelantar");
				Assert.That(enNoviembre, Is.LessThan(enSetiembre),
					"Cuanto mas tarde, menos se ahorra");
				Assert.That(c.AhorroPorProntoPago(new DateTime(2025, 9, 3), 0m), Is.EqualTo(0m),
					"A tasa cero no hay ahorro");
			});
		}

		[Test]
		public void TheReadIsTotalAndAnswersZeroWhereThereIsNothingToBringForward()
		{
			Cuenta c = ElExpedienteDeDemo.UnaCuentaCompleta();

			Assert.Multiple(() =>
			{
				Assert.That(
					c.AhorroPorProntoPago(new DateTime(2025, 12, 31), 0.062m), Is.EqualTo(0m),
					"El 31 de diciembre ya no queda nada por adelantar");
				Assert.That(
					new Cuenta().AhorroPorProntoPago(new DateTime(2025, 1, 3), 0.062m), Is.EqualTo(0m),
					"Sin historia, cero");
			});
		}
	}
}
