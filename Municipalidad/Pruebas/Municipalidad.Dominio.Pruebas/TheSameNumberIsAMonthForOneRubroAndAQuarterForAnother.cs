using System;
using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// JUICIO 1, FIRMADO: la cadencia lee el periodo, y el vencimiento es el ultimo dia.
	//
	// Mudado desde Municipalidad.Consola en M-6. Cada afirmacion nombra el juicio o el hueco del
	// Preguntas.md que fija, para que cuando se rompa se sepa que decision se violo y no solo que
	// fallo un assert. Y donde la consola decia `Revienta` -- que atrapaba cualquier excepcion -- aca
	// va el TIPO exacto, que es lo que M-1 y DEC-08 hacen distinguible.
	[TestFixture]
	public sealed class TheSameNumberIsAMonthForOneRubroAndAQuarterForAnother
	{
		[Test]
		public void TheCadenceOfTheRubroDecidesWhatTheNumberMeans()
		{
			Periodo abril = Rubro.Basura().LeerPeriodo(2026, 4);
			Periodo cuarto = Rubro.BienesInmuebles().LeerPeriodo(2026, 4);

			Assert.Multiple(() =>
			{
				Assert.That(abril.UltimoDia, Is.EqualTo(new DateTime(2026, 4, 30)),
					"Para la basura, 2026.04 es abril y vence el 30 de abril");
				Assert.That(cuarto.UltimoDia, Is.EqualTo(new DateTime(2026, 12, 31)),
					"Para bienes inmuebles, 2026.04 es el cuarto trimestre y vence el 31 de diciembre");
				Assert.That(abril.EsElMismoQue(cuarto), Is.False,
					"Un mes y un trimestre con el mismo numero NO son el mismo periodo");
			});
		}

		[Test]
		public void ANumberOutsideItsCadenceIsRefusedAndCanBeAskedAboutFirst()
		{
			Assert.Multiple(() =>
			{
				Assert.Throws<ArgumentOutOfRangeException>(
					() => Rubro.Basura().LeerPeriodo(2026, 13),
					"Un servicio no tiene mes 13");
				Assert.Throws<ArgumentOutOfRangeException>(
					() => Rubro.BienesInmuebles().LeerPeriodo(2026, 5),
					"Bienes inmuebles no tiene quinto trimestre");
				Assert.That(Rubro.BienesInmuebles().PuedeLeerPeriodo(2026, 5), Is.False,
					"Y el Check lo puede preguntar antes");
			});
		}

		[Test]
		public void EachCadenceKnowsWhichPeriodFollowsAndWhichPrecedes()
		{
			Assert.Multiple(() =>
			{
				Assert.That(
					Rubro.Basura().LeerPeriodo(2025, 12).Siguiente()
						.EsElMismoQue(Rubro.Basura().LeerPeriodo(2026, 1)), Is.True,
					"Diciembre sigue en enero del anio siguiente");
				Assert.That(
					Rubro.Parques().LeerPeriodo(2025, 4).Siguiente()
						.EsElMismoQue(Rubro.Parques().LeerPeriodo(2026, 1)), Is.True,
					"Y el cuarto trimestre sigue en el primero");
				Assert.That(
					Rubro.Basura().LeerPeriodo(2026, 1).Anterior()
						.EsElMismoQue(Rubro.Basura().LeerPeriodo(2025, 12)), Is.True,
					"Enero retrocede a diciembre");
			});
		}
	}
}
