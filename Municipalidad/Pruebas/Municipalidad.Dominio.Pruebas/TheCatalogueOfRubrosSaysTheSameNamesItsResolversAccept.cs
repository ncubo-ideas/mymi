using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// NIVEL A: EL CATALOGO CONTRA SUS PROPIOS RESOLUTORES. `Rubro` dice los cinco nombres en TRES
	// lugares -- las cinco fabricas, el switch de `PorNombre` y el switch de `EsUnNombreConocido` -- y
	// tres listas de lo mismo pueden separarse.
	//
	// `LosQueHay()` NO agrega una cuarta: sale de las fabricas, asi que lo que cruza aca son las dos
	// listas que ya existian contra la unica que las nombra. Un rubro agregado al catalogo y olvidado en
	// cualquiera de los dos switch se ve desde aca; sin esta prueba se veria cuando un Check negara un
	// rubro que existe, o cuando `PorNombre` reventara con uno que el catalogo dice que hay.
	//
	// Y ES LA MITAD QUE G5 PEDIA: Concesionario tambien tiene los nombres de sus conjuntos cerrados
	// escritos a mano en su caratula, y los pinta contra el dominio con una prueba. Municipalidad no
	// podia hacerlo cuando G5 se escribio porque no habia proyecto de pruebas.
	[TestFixture]
	public sealed class TheCatalogueOfRubrosSaysTheSameNamesItsResolversAccept
	{
		[Test]
		public void TheCatalogueSaysFiveNames()
		{
			Assert.That(Rubro.LosQueHay(), Has.Length.EqualTo(5),
				"Los cinco que los recibos mostraron. Los otros cinco del menu del portal no tienen "
				+ "cadencia conocida y no se inventan -- es el hueco 9, con dueno.");
		}

		[Test]
		public void EveryNameTheCatalogueSaysIsANameItsRecogniserAccepts()
		{
			Assert.Multiple(() =>
			{
				foreach (string nombre in Rubro.LosQueHay())
					Assert.That(Rubro.EsUnNombreConocido(nombre), Is.True,
						"Rubro.EsUnNombreConocido, con el nombre '" + nombre + "' que el catalogo dice "
						+ "que existe. Un rubro que el catalogo nombra y el reconocedor niega queda "
						+ "rechazado por el Check de todos los comandos.");
			});
		}

		[Test]
		public void EveryNameTheCatalogueSaysResolvesBackToARubroThatSaysTheSameName()
		{
			Assert.Multiple(() =>
			{
				foreach (string nombre in Rubro.LosQueHay())
					Assert.That(Rubro.PorNombre(nombre).Nombre, Is.EqualTo(nombre),
						"Rubro.PorNombre, con el nombre '" + nombre + "'. Va y vuelve por el mismo "
						+ "nombre, que es lo que hace del nombre una llave y no una etiqueta.");
			});
		}

		// Y LOS CINCO SON DISTINTOS. Una fabrica copiada y mal editada -- dos que devuelvan el mismo
		// nombre -- dejaria las tres pruebas de arriba verdes con un rubro perdido.
		[Test]
		public void TheFiveNamesAreFiveDifferentNames()
		{
			Assert.That(Rubro.LosQueHay(), Is.Unique);
		}

		// CADA UNO TRAE SU CADENCIA Y SU LEY, que es lo que hace del rubro un catalogo de cinco
		// instancias y no cinco clases: un nombre que resolviera a un Rubro sin cadencia no podria leer
		// un periodo, y `PuedeLeerPeriodo` lo negaria para todo.
		[Test]
		public void EveryRubroTheCatalogueSaysCanReadAPeriodOfItsOwnCadence()
		{
			Assert.Multiple(() =>
			{
				foreach (string nombre in Rubro.LosQueHay())
				{
					Rubro r = Rubro.PorNombre(nombre);

					Assert.That(r.PuedeLeerPeriodo(2025, 1), Is.True,
						"Rubro.PuedeLeerPeriodo(2025, 1) para '" + nombre + "': el primer mes y el "
						+ "primer trimestre existen en las dos cadencias.");

					Assert.That(r.Cadencia.PeriodosPorAnio, Is.GreaterThan(0),
						"Rubro.Cadencia.PeriodosPorAnio para '" + nombre + "': sin periodos por anio "
						+ "no hay con que proyectar el anio.");
				}
			});
		}
	}
}
