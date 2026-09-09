using System;
using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// HUECO 9, CON DUENO: el menu del portal lista otros cinco rubros -- cementerio, patentes, licores,
	// arreglo de pago, permiso -- de los que no se sabe cadencia ni ley. No se inventan.
	//
	// Mudado desde Municipalidad.Consola en M-6. Lo que cada rubro PARAMETRIZA -- su cadencia y su ley
	// de calculo -- es lo que hace del catalogo cinco instancias y no cinco clases.
	[TestFixture]
	public sealed class TheCatalogueHasFiveRubrosAndInventsNoOthers
	{
		[Test]
		public void EachRubroCarriesItsOwnCadenceAndItsOwnLawOfCalculation()
		{
			Assert.Multiple(() =>
			{
				Assert.That(
					Rubro.Basura().Cadencia.EsMensual() && !Rubro.Basura().Ley.RespondeAlAvaluo, Is.True,
					"Basura es mensual y por unidad");
				Assert.That(
					Rubro.LimpiezaDeCalles().Cadencia.EsMensual()
						&& Rubro.LimpiezaDeCalles().Ley.RespondeAlAvaluo, Is.True,
					"Limpieza es mensual pero sobre la base imponible");
				Assert.That(
					Rubro.BienesInmuebles().Cadencia.EsTrimestral()
						&& Rubro.BienesInmuebles().Ley.RespondeAlAvaluo, Is.True,
					"Bienes inmuebles es trimestral y sobre la base imponible");
			});
		}

		[Test]
		public void ARubroNobodyBroughtAReceiptForIsRefusedAndNeverGivenACadence()
		{
			Cuenta c = new Cuenta();
			c.RegistrarPago("R-1", new DateTime(2024, 3, 31), 0m);

			Assert.Multiple(() =>
			{
				Assert.That(Rubro.EsUnNombreConocido("cementerio"), Is.False,
					"Cementerio no se conoce: nadie trajo su recibo");
				Assert.Throws<ArgumentOutOfRangeException>(
					() => Rubro.PorNombre("cementerio"),
					"Y pedirlo revienta en vez de adivinarle una cadencia");
				Assert.That(
					c.PuedeAgregarLineaAlPago("R-1", "cementerio", 2024, 3, 1000m, 0m), Is.False,
					"Una linea de un rubro desconocido no entra al recibo");
			});
		}
	}
}
