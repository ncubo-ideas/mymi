using System;
using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// M1, M2, M3, M4 -- propuestos, sin firmar, y entran POR PARAMETRO. El mismo +80 % es tarifa nueva
	// en basura y avaluo nuevo en bienes inmuebles, porque lo decide la LEY DE CALCULO del rubro.
	//
	// Mudado desde Municipalidad.Consola en M-6.
	[TestFixture]
	public sealed class TheSameJumpIsExplainedByTheRubrosLawOfCalculation
	{
		[Test]
		public void AJumpInAPerUnitRubroIsANewTariffAndInAValueBasedOneIsANewAssessment()
		{
			Cuenta c = ElExpedienteDeDemo.UnaCuentaCompleta();

			Variacion basura = c.VariacionDe("recoleccion-de-basura", 0.25m, 0.02m);
			Variacion ibi = c.VariacionDe("bienes-inmuebles", 0.25m, 0.02m);

			Assert.Multiple(() =>
			{
				Assert.That(Math.Round(basura.Proporcion, 2), Is.EqualTo(0.60m), "Basura subio 60 %");
				Assert.That(basura is CambioDeTarifa && basura.Etiqueta == "cambio-de-tarifa", Is.True,
					"Y se explica como CAMBIO DE TARIFA: se cobra por unidad");

				Assert.That(Math.Round(ibi.Proporcion, 2), Is.EqualTo(0.80m),
					"Bienes inmuebles subio 80 %");
				Assert.That(ibi is CambioDeAvaluo && ibi.Etiqueta == "cambio-de-avaluo", Is.True,
					"Y se explica como CAMBIO DE AVALUO: se cobra sobre la base imponible");
			});
		}

		[Test]
		public void AChangeUnderTheNoiseIsNotAChangeAndOneOverItButUnderTheThresholdIsAMinorOne()
		{
			Cuenta c = ElExpedienteDeDemo.UnaCuentaCompleta();

			Variacion policia = c.VariacionDe("servicio-policia", 0.25m, 0.02m);
			Variacion policiaFina = c.VariacionDe("servicio-policia", 0.25m, 0.01m);

			Assert.Multiple(() =>
			{
				Assert.That(policia is SinSalto && policia.Etiqueta == "sin-cambio", Is.True,
					"Policia subio 1,7 %: por debajo del ruido, sin cambio");
				Assert.That(
					policiaFina is SinSalto && policiaFina.Etiqueta == "cambio-menor"
						&& !policiaFina.EsSalto, Is.True,
					"Con un ruido del 1 %, es un cambio menor: supera el ruido, no llega al umbral");
			});
		}

		[Test]
		public void AnIncreaseIsNeverUrgentAndTheComparisonIsPerPeriod()
		{
			Cuenta c = ElExpedienteDeDemo.UnaCuentaCompleta();

			Variacion basura = c.VariacionDe("recoleccion-de-basura", 0.25m, 0.02m);
			Variacion ibi = c.VariacionDe("bienes-inmuebles", 0.25m, 0.02m);

			Assert.Multiple(() =>
			{
				Assert.That(!basura.Urgencia().Avisa() && !ibi.Urgencia().Avisa(), Is.True,
					"Un aumento NO es urgente -M4-: ya paso");
				Assert.That(
					basura.MontoAnteriorPorPeriodo == 5000m && basura.MontoActualPorPeriodo == 8000m,
					Is.True,
					"Compara monto POR PERIODO, no monto crudo: 8.000 contra 5.000, no 96.000 contra 15.000");
			});
		}

		[Test]
		public void AThresholdUnderTheNoiseIsRefusedAndSoIsAVariationWithNothingToCompare()
		{
			Cuenta c = ElExpedienteDeDemo.UnaCuentaCompleta();
			Cuenta vacia = new Cuenta();

			Assert.Multiple(() =>
			{
				Assert.Throws<ArgumentException>(
					() => c.VariacionDe("recoleccion-de-basura", 0.01m, 0.02m),
					"Un umbral menor que el ruido no tiene sentido");
				Assert.That(vacia.SePuedeVerLaVariacionDe("recoleccion-de-basura"), Is.False,
					"Con menos de dos pagos no hay variacion que ver");
				Assert.Throws<DomainException>(
					() => vacia.VariacionDe("recoleccion-de-basura", 0.25m, 0.02m),
					"Y pedirla revienta");
			});
		}
	}
}
