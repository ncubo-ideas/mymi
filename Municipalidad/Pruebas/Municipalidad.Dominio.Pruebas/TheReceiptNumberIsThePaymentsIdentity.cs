using System;
using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// REGLA 18: el mismo recibo dos veces es el mismo hecho otra vez. Un replay lo repite siempre, y un
	// feed tambien puede.
	//
	// Mudado desde Municipalidad.Consola en M-6.
	[TestFixture]
	public sealed class TheReceiptNumberIsThePaymentsIdentity
	{
		[Test]
		public void TheSameReceiptTwiceIsRefusedAndForcingItRaisesTheDomainsRefusal()
		{
			Cuenta c = new Cuenta();

			Assert.That(c.PuedeRegistrarPago("R-100", new DateTime(2026, 1, 5), 0m), Is.True,
				"Un recibo nuevo entra");

			c.RegistrarPago("R-100", new DateTime(2026, 1, 5), 0m);

			Assert.Multiple(() =>
			{
				Assert.That(c.PuedeRegistrarPago("R-100", new DateTime(2026, 1, 5), 0m), Is.False,
					"El mismo recibo otra vez no entra");
				Assert.Throws<DomainException>(
					() => c.RegistrarPago("R-100", new DateTime(2026, 1, 5), 0m),
					"Y forzarlo revienta");
			});
		}

		[Test]
		public void TheSameLineTwiceInOneReceiptIsRefusedAndSoIsANegativeLateFee()
		{
			Cuenta c = new Cuenta();
			c.RegistrarPago("R-100", new DateTime(2026, 1, 5), 0m);
			c.AgregarLineaAlPago("R-100", "recoleccion-de-basura", 2026, 1, 8000m, 0m);

			Assert.Multiple(() =>
			{
				Assert.That(
					c.PuedeAgregarLineaAlPago("R-100", "recoleccion-de-basura", 2026, 1, 8000m, 0m),
					Is.False,
					"La misma linea otra vez en el mismo recibo no entra");
				Assert.That(
					c.PuedeAgregarLineaAlPago("R-100", "servicio-policia", 2026, 1, 6000m, -1m),
					Is.False,
					"Una linea con mora negativa no entra");
			});
		}

		[Test]
		public void PayingInJanuaryUpToDecemberIsPayingAheadAndItIsReadFromTheDates()
		{
			Pago adelantado = new Pago("R-200", new DateTime(2026, 1, 5), 1500m);
			adelantado.AgregarLinea(new LineaPagada(
				Rubro.Basura(), Rubro.Basura().LeerPeriodo(2026, 12), 96000m, 0m, 12, false));

			Assert.Multiple(() =>
			{
				Assert.That(adelantado.PagoPorAdelantado(), Is.True,
					"Pagar en enero hasta diciembre es pagar por adelantado, y se lee de las fechas");
				Assert.That(adelantado.Total, Is.EqualTo(94500m),
					"El total descuenta el pronto pago");
			});
		}
	}
}
