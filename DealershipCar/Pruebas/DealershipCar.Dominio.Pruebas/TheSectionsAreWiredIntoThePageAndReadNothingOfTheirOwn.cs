using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LAS DOS COSAS QUE NINGUNA OTRA PRUEBA DE T10 PUEDE VER, y por que se miran sobre el MARKPUP.
	//
	// Todo lo demas de T10 se defiende sobre la hoja que WhatTheManufacturerPublished compone, que
	// es lo unico que las secciones tienen para pintar. Pero dos regresiones viven fuera de esa
	// hoja: que la pagina de verdad renderice las secciones -- borrar la etiqueta deja el resto del
	// modulo verde y la pantalla sin la mitad de T10 -- y que las secciones no lean nada por su
	// cuenta, que es la unica garantia estructural de que no puedan componer una respuesta.
	//
	// UN COMPONENTE COMPILADO NO SE PUEDE INSPECCIONAR sin un banco de pruebas de componentes, y en
	// este repo no hay ninguno (C40 es la version de esto medida en T10 de OS2: lo que el aparejo no
	// alcanza, no se falsifica). Lo que si se puede leer es la fuente, y el csproj la copia al
	// output COMO DATO -- el mismo mecanismo con que se lee el csproj del dominio para contar sus
	// referencias declaradas, y por la misma razon.
	//
	// LO QUE ESTO NO CUBRE, DICHO: el markup RENDERIZADO. Que las etiquetas esten escritas no prueba
	// que la pantalla se vea bien ni que una rama de error diga algo. Eso lo mide una persona en un
	// browser de verdad, y el informe de T10 nombra cuales ramas son suyas.
	[TestFixture]
	public sealed class TheSectionsAreWiredIntoThePageAndReadNothingOfTheirOwn
	{
		// LA PAGINA RENDERIZA LAS DOS SECCIONES Y LES PASA LA MISMA HOJA. La misma a proposito: si
		// cada una leyera la suya, la revision que una nombra podria no ser la de la otra, y en una
		// pantalla que se refresca contra el taller eso pasa de verdad.
		[Test]
		public void ThePageRendersBothSectionsAndHandsThemOneSheetReadAtOneMoment()
		{
			string page = ThePageAsItIsWritten();

			Assert.That(page, Does.Contain("<MyVehicle Sheet=\"@sheet\" />"),
				"La pagina dejo de renderizar la seccion Mi vehiculo. Nada mas en la suite lo ve: "
				+ "la hoja se sigue componiendo bien y la mitad de T10 no aparece en la pantalla.");

			Assert.That(page, Does.Contain("<Warranty Sheet=\"@sheet\" />"),
				"La pagina dejo de renderizar la seccion Garantia.");

			Assert.That(page, Does.Contain("state.ReadTheSpecification()"),
				"La pagina dejo de leer la ficha, asi que las dos secciones reciben null y no pintan "
				+ "nada -- sin ningun error y sin ningun aviso.");
		}

		// Y LAS SECCIONES NO LEEN NADA POR SU CUENTA. Un componente sin @inject no tiene de donde
		// sacar una respuesta: solo puede pintar lo que le pasaron. Es la garantia estructural detras
		// de "la caratula formatea, no vuelve a derivar", y es mas barata de sostener que revisarlo
		// a ojo cada vez que alguien toca el markup.
		[Test]
		public void NoSectionInjectsAnythingOrNamesTheDoor()
		{
			List<string> swept = new List<string>();

			foreach (string file in TheSectionFiles())
			{
				string markup = File.ReadAllText(file);
				string name = Path.GetFileName(file);

				swept.Add(name);

				Assert.That(markup, Does.Not.Contain("@inject"),
					$"'{name}' inyecta algo. Una seccion que puede leer puede componer, y entonces "
					+ "lo que la pantalla dice deja de ser lo que el dominio dijo.");

				Assert.That(markup, Does.Not.Contain("DealershipCarAPI"),
					$"'{name}' nombra la puerta.");

				Assert.That(markup, Does.Not.Contain("@* "),
					$"'{name}' lleva un comentario de razor, y en este arbol el codigo no lleva "
					+ "prosa fuera de las pruebas.");
			}

			Assert.That(swept, Has.Count.GreaterThanOrEqualTo(2),
				"El barrido no encontro las secciones. Se copian por glob justamente para que una "
				+ "tercera entre sola; si no hay ninguna, lo que fallo es la copia y no el markup.");

			Assert.That(swept, Contains.Item("MyVehicle.razor"));
			Assert.That(swept, Contains.Item("Warranty.razor"));
		}

		private static string ThePageAsItIsWritten()
		{
			string path = Path.Combine(AppContext.BaseDirectory, "MyCar.razor.dato");

			Assert.That(File.Exists(path), Is.True,
				"La pagina no llego al output. La prueba no puede pasar por no haber leido nada.");

			return File.ReadAllText(path);
		}

		private static string[] TheSectionFiles()
		{
			string folder = Path.Combine(AppContext.BaseDirectory, "SeccionesComoDato");

			Assert.That(Directory.Exists(folder), Is.True,
				"Las secciones no llegaron al output.");

			return Directory.GetFiles(folder, "*.razor");
		}
	}
}
