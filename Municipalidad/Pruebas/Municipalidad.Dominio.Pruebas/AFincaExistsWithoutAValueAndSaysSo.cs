using System;
using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// HUECO 2, CON DUENO: la persona, con su declaracion o su avaluo. La finca existe en el modelo
	// aunque ningun papel la nombre -- el portal consolida por cedula, pero el IBI es de la finca -- y
	// su valor entra por comando porque ningun artefacto lo trae. Cuando no esta, SE DICE.
	//
	// Mudado desde Municipalidad.Consola en M-6. Regla 18 y hueco 12: una declaracion por dia, y en
	// orden.
	[TestFixture]
	public sealed class AFincaExistsWithoutAValueAndSaysSo
	{
		[Test]
		public void TheFincaEntersOnceAndWithoutADeclarationItHasNoValueAtAnyDate()
		{
			Cuenta c = new Cuenta();
			c.IncorporarFinca("F-1");
			Finca f = c.FincaNumero("F-1");

			Assert.Multiple(() =>
			{
				Assert.That(c.TieneFinca("F-1"), Is.True, "La finca esta");
				Assert.That(c.PuedeIncorporarFinca("F-1"), Is.False,
					"La misma finca dos veces no entra");
				Assert.That(f.TieneValorAl(new DateTime(2026, 1, 1)), Is.False,
					"Sin declaracion, no tiene valor a ninguna fecha");
				Assert.Throws<DomainException>(
					() => f.ValorVigenteAl(new DateTime(2026, 1, 1)),
					"Y pedirlo revienta en vez de inventar uno -es la leccion de T1-");
			});
		}

		[Test]
		public void ADeclaredValueIsInForceFromItsDateOnwardAndOnePerDay()
		{
			Cuenta c = new Cuenta();
			c.IncorporarFinca("F-1");
			Finca f = c.FincaNumero("F-1");
			c.DeclararValorDeFinca("F-1", new DateTime(2025, 1, 15), 60000000m);

			Assert.Multiple(() =>
			{
				Assert.That(f.ValorVigenteAl(new DateTime(2026, 1, 1)), Is.EqualTo(60000000m),
					"Declarado, el valor vigente en 2026 es el de 2025");
				Assert.That(
					c.PuedeDeclararValorDeFinca("F-1", new DateTime(2025, 1, 15), 60000000m), Is.False,
					"La misma declaracion otra vez no entra: es el mismo hecho -regla 18, hueco 12-");
				Assert.That(
					c.PuedeDeclararValorDeFinca("F-1", new DateTime(2026, 1, 15), 65000000m), Is.True,
					"Una declaracion posterior, si");
				Assert.That(f.TieneValorAl(new DateTime(2024, 6, 1)), Is.False,
					"Pero en 2024 todavia no habia");
				Assert.That(
					c.PuedeDeclararValorDeFinca("F-1", new DateTime(2024, 6, 1), 50000000m), Is.False,
					"Una declaracion con fecha anterior a la ultima no entra");
			});
		}
	}
}
