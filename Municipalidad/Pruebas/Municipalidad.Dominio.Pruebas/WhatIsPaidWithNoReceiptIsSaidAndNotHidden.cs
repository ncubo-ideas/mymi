using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// M8, propuesto, y HUECO 3 con dueno: la persona sabe como paga. Si la consulta de hoy no debe nada
	// anterior a X, lo anterior a X se pago -- en ventanilla, en el banco, donde sea -- pero la app no
	// vio el recibo. Contarlo como historia seria inventarla; ignorarlo seria un hueco invisible.
	//
	// Mudado desde Municipalidad.Consola en M-6. El ESTADO de esta lectura -- las dos razones por las
	// que sale vacia -- lo mira AnAbsenceWithKindsIsOneNamedTypePerReason, que es la otra mitad de M-6.
	[TestFixture]
	public sealed class WhatIsPaidWithNoReceiptIsSaidAndNotHidden
	{
		[Test]
		public void WithEveryReceiptInHandTheConsultaUncoversNothing()
		{
			Cuenta completa = ElExpedienteDeDemo.UnaCuentaCompleta();

			Assert.Multiple(() =>
			{
				Assert.That(
					completa.PeriodosPagadosSinComprobante(ElExpedienteDeDemo.LaConsulta), Is.Empty,
					"Con los tres recibos, la consulta no destapa nada sin comprobante");
				Assert.That(
					completa.PeriodosPagadosSinComprobante(new DateTime(2025, 1, 1)), Is.Empty,
					"Sin consulta, la lista es vacia y no revienta");
			});
		}

		[Test]
		public void WithAReceiptMissingTheUncoveredPeriodsAreNamedOneByOne()
		{
			Cuenta incompleta = ElExpedienteDeDemo.UnaCuentaSinElTercerRecibo();
			IReadOnlyList<PeriodoSinComprobante> sinComprobante =
				incompleta.PeriodosPagadosSinComprobante(ElExpedienteDeDemo.LaConsulta);

			int basura = 0;
			foreach (PeriodoSinComprobante p in sinComprobante)
				if (p.Rubro.Es(Rubro.Basura())) basura++;

			Assert.Multiple(() =>
			{
				Assert.That(basura, Is.EqualTo(10),
					"Diez meses de basura -julio 2024 a abril 2025- figuran pagados sin comprobante");
				Assert.That(sinComprobante, Is.Not.Empty);
				Assert.That(sinComprobante[0].EtiquetaDelPeriodo, Is.EqualTo("2024-07"),
					"Y se nombran, no se tapan: el primero es julio de 2024");
			});
		}
	}
}
