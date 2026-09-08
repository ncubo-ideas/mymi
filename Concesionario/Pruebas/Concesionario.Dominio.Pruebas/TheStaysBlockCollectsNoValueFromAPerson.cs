using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LA TERCERA DE LAS TRES INVENCIONES PROHIBIDAS, EN EL BLOQUE NUEVO: nada en la pantalla recoge un
	// valor de una persona. La app del dueno es de SOLO LECTURA, y esa regla trata de quien DICE un
	// hecho y no de quien escribe un journal: un formulario que guardara su valor en el estado de la
	// pagina la violaria igual.
	//
	// EL BARRIDO DEL MODULO YA LO CUBRE, Y ESA ES JUSTAMENTE LA COSA QUE HAY QUE MEDIR.
	// TheStandingReachesTheOwnersScreenAndNoControlAsksForIt barre TODO el markup por glob recursivo,
	// asi que un archivo nuevo entra solo -- si el csproj lo copio. LA LECCION DE SH-0 ES QUE UN
	// BARRIDO QUE LEYO CERO ARCHIVOS ESTA VERDE: un glob que no trajo este archivo deja el bloque nuevo
	// sin mirar y no se anuncia. Lo primero que esta prueba afirma es que el archivo LLEGO al barrido.
	//
	// LAS CINCO FORMAS QUE SE VUELVEN A MIRAR AQUI son las estructurales -- los cuatro controles de
	// HTML y el enlace de dos vias. La familia de componentes de MudBlazor no se repite: la mira el
	// barrido del modulo, y lo que hace falta para que lo mire es que este archivo este entre los que
	// lee, que es la primera afirmacion. Repetir las veinte aqui seria una segunda lista que se queda
	// vieja sin que nada lo diga.
	//
	// Y EL BLOQUE NO PODRIA PEDIR NADA AUNQUE QUISIERA: el periodo lo fija la pantalla con su propio
	// reloj (P-11), asi que no hay ningun selector de fechas que ofrecer. Esta prueba es lo que se pone
	// rojo el dia que alguien decida ofrecerlo.
	[TestFixture]
	public sealed class TheStaysBlockCollectsNoValueFromAPerson
	{
		private const string TheBlock = "Historial.razor";

		private static readonly string[] EveryShapeThatCollectsAValueFromAPerson =
		{
			"<input", "<select", "<textarea", "<form", "@bind-Value",
		};

		// PRIMERO: EL ARCHIVO LLEGO AL BARRIDO DEL MODULO. Sin este renglon, todo lo de abajo se puede
		// satisfacer leyendo un archivo que no es este, o ninguno.
		[Test]
		public void TheModuleWideMarkupSweepIsReadingTheNewBlock()
		{
			var swept = new List<string>();

			foreach (string file in EveryMarkupFileTheShellHas())
				swept.Add(Path.GetFileName(file));

			Assert.That(swept, Contains.Item(TheBlock),
				"'" + TheBlock + "' no esta entre el markup que el barrido del modulo lee. El grupo "
				+ "<None> del csproj lo trae por glob recursivo para que un archivo nuevo entre solo; "
				+ "si no llego, el bloque nuevo no lo mira nadie y NADA se pone rojo -- que es la "
				+ "forma exacta en que un barrido esta verde por no haber leido.");

			Assert.That(swept, Has.Count.GreaterThanOrEqualTo(8),
				"El barrido trajo menos markup que las secciones que hay. Lo que fallo es la copia.");
		}

		// Y EL BLOQUE NO LLEVA NINGUNO DE LOS CINCO CONTROLES ESTRUCTURALES.
		[Test]
		public void NoControlOfTheStaysBlockCollectsAFactAboutThisVehicle()
		{
			string markup = TheBlockAsItIsWritten();

			foreach (string shape in EveryShapeThatCollectsAValueFromAPerson)
				Assert.That(markup, Does.Not.Contain(shape),
					"El bloque de los dias en el taller lleva un control que recoge un valor ('"
					+ shape + "'). La app del dueno es de SOLO LECTURA. Las estadias las registra el "
					+ "taller, por comando o por feed; si falta una, el arreglo es que la manden, no "
					+ "que la persona la escriba. Y el periodo no se pide: lo fija la pantalla con su "
					+ "propio reloj.");

			// EL CONTROL DEL DETECTOR. Sin este renglon, un barrido con la lista vacia o con un
			// comparador roto pasaria diciendo exactamente lo mismo.
			int caught = 0;

			foreach (string shape in EveryShapeThatCollectsAValueFromAPerson)
				if (("<div>" + shape + " Label=\"¿Desde cuándo?\" /></div>").Contains(shape)) caught++;

			Assert.That(caught, Is.EqualTo(EveryShapeThatCollectsAValueFromAPerson.Length));
		}

		// Y NO OFRECE NINGUN BOTON QUE MUEVA EL PERIODO. Un selector de periodo no tiene por que ser un
		// <input>: dos botones "año pasado" / "este año" recogen un valor de una persona igual, y
		// ninguna de las cinco formas de arriba lo ve. El discriminador es que la ventana sale del
		// reloj y de ningun otro lado, asi que la seccion no puede tener estado propio que la mueva.
		[Test]
		public void TheBlockOffersNoWayToMoveTheWindowItWasHanded()
		{
			string markup = TheBlockAsItIsWritten();

			foreach (string shape in new[] { "@onclick", "<button" })
				Assert.That(markup, Does.Not.Contain(shape),
					"El bloque de los dias en el taller gano un control ('" + shape + "'). Dos botones "
					+ "que cambien el periodo recogen un valor de una persona igual que un campo, y "
					+ "ninguna de las cinco formas estructurales los ve. La ventana la fija la pantalla "
					+ "con su propio reloj, y esa es la unica razon por la que no hay nada que pedir.");
		}

		private static string TheBlockAsItIsWritten()
		{
			foreach (string file in EveryMarkupFileTheShellHas())
				if (Path.GetFileName(file) == TheBlock) return File.ReadAllText(file);

			Assert.Fail("'" + TheBlock + "' no llego al output.");
			return "";
		}

		private static string[] EveryMarkupFileTheShellHas()
		{
			string folder = Path.Combine(AppContext.BaseDirectory, "MarcadoComoDato");

			Assert.That(Directory.Exists(folder), Is.True,
				"El markup de la caratula no llego al output.");

			return Directory.GetFiles(folder, "*.razor", SearchOption.AllDirectories);
		}
	}
}
