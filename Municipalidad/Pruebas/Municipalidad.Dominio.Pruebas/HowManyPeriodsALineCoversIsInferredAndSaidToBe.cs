using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// HUECO 6: el recibo dice hasta que periodo llega la linea y cuanto, pero no cuantos periodos son.
	// La Cuenta lo deduce de la distancia al ultimo periodo pagado de ese rubro, y `EsInferido` lo
	// dice cuando no tuvo con que deducir.
	//
	// Mudado desde Municipalidad.Consola en M-6.
	[TestFixture]
	public sealed class HowManyPeriodsALineCoversIsInferredAndSaidToBe
	{
		[Test]
		public void TheFirstLineOfARubroHasNothingToInferFromAndSaysSo()
		{
			IReadOnlyList<LineaPagada> basura =
				ElExpedienteDeDemo.UnaCuentaCompleta().HistorialDe("recoleccion-de-basura");

			Assert.Multiple(() =>
			{
				Assert.That(basura[0].PeriodosQueCubre == 1 && basura[0].EsInferido, Is.True,
					"La primera linea no tiene con que inferir: cubre uno, y lo dice");
				Assert.That(basura[1].PeriodosQueCubre == 3 && !basura[1].EsInferido, Is.True,
					"La segunda llega a junio despues de marzo: cubre tres");
				Assert.That(basura[1].MontoPorPeriodo, Is.EqualTo(5000m),
					"Y su monto por periodo es 15.000 / 3 = 5.000");
				Assert.That(basura[2].PeriodosQueCubre, Is.EqualTo(12),
					"La tercera cubre doce meses");
				Assert.That(basura[2].MontoPorPeriodo, Is.EqualTo(8000m),
					"A 8.000 por mes");
			});
		}

		[Test]
		public void AQuarterlyRubroInfersItsQuartersTheSameWay()
		{
			IReadOnlyList<LineaPagada> ibi =
				ElExpedienteDeDemo.UnaCuentaCompleta().HistorialDe("bienes-inmuebles");

			Assert.Multiple(() =>
			{
				Assert.That(ibi[2].PeriodosQueCubre, Is.EqualTo(4),
					"Bienes inmuebles: la tercera linea cubre cuatro trimestres");
				Assert.That(ibi[2].MontoPorPeriodo, Is.EqualTo(36000m),
					"A 36.000 por trimestre");
			});
		}

		[Test]
		public void APeriodNoLineNamesIsStillPaidWhenALineCoversIt()
		{
			Cuenta c = ElExpedienteDeDemo.UnaCuentaCompleta();

			Assert.Multiple(() =>
			{
				Assert.That(
					c.EstaPagado(Rubro.Basura(), Rubro.Basura().LeerPeriodo(2024, 5)), Is.True,
					"Mayo de 2024 esta pagado aunque ninguna linea diga 'mayo': lo cubre la de junio");
				Assert.That(
					c.EstaPagado(Rubro.Basura(), Rubro.Basura().LeerPeriodo(2025, 7)), Is.False,
					"Julio de 2025 no esta pagado");
				Assert.That(
					c.PuedeAgregarLineaAlPago("R-3", "recoleccion-de-basura", 2024, 5, 5000m, 0m),
					Is.False,
					"Y una linea para un periodo ya cubierto no entra");
				Assert.That(c.HistorialDe("cementerio"), Is.Empty,
					"Un rubro sin historia da una serie vacia, no revienta");
			});
		}
	}
}
