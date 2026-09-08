using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
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
		//
		// Y DESDE T10 GARANTIA RECIBE ADEMAS LA MITAD DEL VEHICULO, que es una hoja aparte porque la
		// ficha es de un MODELO y la garantia de este carro es de un VIN. Las dos se leen en el mismo
		// Reread, una detras de la otra, asi que siguen siendo un solo momento.
		//
		// LA PIEL DEL DUENO METIO UN PISO EN EL MEDIO y la ley no cambio: la pagina sigue haciendo
		// las dos lecturas UNA VEZ y ahora se las pasa a Vehiculo, que es quien reusa MyVehicle y
		// Warranty enteras. Lo que se defiende es la cadena, no en que archivo esta escrita la
		// etiqueta: dos lecturas, un momento, y las dos secciones alimentadas de ahi.
		[Test]
		public void ThePageRendersBothSectionsAndHandsThemOneSheetReadAtOneMoment()
		{
			string page = ThePageAsItIsWritten();

			Assert.That(page, Does.Contain("state.ReadTheSpecification()"),
				"La pagina dejo de leer la ficha, asi que las dos secciones reciben null y no pintan "
				+ "nada -- sin ningun error y sin ningun aviso.");

			Assert.That(page, Does.Contain("state.ReadWhereThisVehicleStands()"),
				"La pagina dejo de leer la mitad del vehiculo. Es la lectura que toma el instante en "
				+ "que la pantalla pregunta, y sin ella la seccion Garantia vuelve a ser el alcance "
				+ "publicado y nada mas.");

			Assert.That(page, Does.Contain("<Vehiculo Sheet=\"@sheet\""),
				"La pagina dejo de pasarle la ficha a la seccion del vehiculo, que es quien reusa "
				+ "MyVehicle y Warranty. Sin ella las dos reciben null y no pintan nada.");

			Assert.That(page, Does.Contain("Stands=\"@stands\""),
				"La pagina dejo de pasar la mitad del vehiculo hacia abajo. Sin Stands la seccion "
				+ "Garantia no tiene con que decir donde esta parado el carro.");

			string vehicle = TheSectionAsItIsWritten("Vehiculo.razor");

			Assert.That(vehicle, Does.Contain("<MyVehicle Sheet=\"@Sheet\" />"),
				"La seccion del vehiculo dejo de renderizar Mi vehiculo. Nada mas en la suite lo ve: "
				+ "la hoja se sigue componiendo bien y esa mitad no aparece en la pantalla.");

			Assert.That(vehicle, Does.Contain("<Warranty Sheet=\"@Sheet\" Stands=\"@Stands\" />"),
				"La seccion del vehiculo dejo de renderizar Garantia, o dejo de pasarle la mitad del "
				+ "vehiculo, y entonces la prosa del dominio sobre el sobre no llega a la pantalla.");
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

				// SE BUSCA "@*" Y NO "@* ". La version con espacio dejaba pasar el encabezado que
				// abre con "@*" y sigue en la linea de abajo, que es justo la forma en que se
				// escribe un bloque largo: cuatro secciones nuevas entraron con uno y el barrido
				// las dio por limpias. El agujero era del barrido, no del markup.
				Assert.That(markup, Does.Not.Contain("@*"),
					$"'{name}' lleva un comentario de razor, y en este arbol el codigo no lleva "
					+ "prosa fuera de las pruebas.");
			}

			Assert.That(swept, Has.Count.GreaterThanOrEqualTo(2),
				"El barrido no encontro las secciones. Se copian por glob justamente para que una "
				+ "tercera entre sola; si no hay ninguna, lo que fallo es la copia y no el markup.");

			Assert.That(swept, Contains.Item("MyVehicle.razor"));
			Assert.That(swept, Contains.Item("Warranty.razor"));
		}

		// Y LA SECCION PINTA LAS FRASES DEL DOMINIO Y NO UNAS PROPIAS. Es la tercera cosa que ninguna
		// otra prueba ve: todo lo demas se defiende sobre las dos hojas, y una seccion que armara su
		// propia frase con pedazos de ellas dejaria las hojas intactas y la suite entera verde.
		//
		// SE AFIRMA SOBRE EL MARKUP porque es donde vive la composicion: los campos que llevan una
		// frase del dominio tienen que aparecer pintados, y aparecer SOLOS -- no adentro de una
		// concatenacion. Un componente Blazor compilado no se puede inspeccionar sin un banco de
		// pruebas de componentes, y en este repo no hay ninguno.
		[Test]
		public void TheWarrantySectionPaintsTheDomainsSentencesAndComposesNoneOfItsOwn()
		{
			string markup = TheSectionAsItIsWritten("Warranty.razor");

			foreach (string sentence in TheDomainsSentencesTheSectionMustQuote)
				Assert.That(markup, Does.Contain(sentence),
					$"La seccion Garantia dejo de pintar '{sentence}'. La frase la compone el dominio: "
					+ "una que la caratula arme con pedazos de las otras sale con la procedencia del "
					+ "fabricante encima de algo que el fabricante no dijo.");

			foreach (string forbidden in new[] { "Stands.Says +", "+ Stands.Says", "Says +", "+ Says" })
				Assert.That(markup, Does.Not.Contain(forbidden),
					$"La seccion concatena algo con una frase del dominio ('{forbidden}'). Lo que sale "
					+ "de la puerta se cita letra por letra o no se cita.");
		}

		// Las frases del dominio que la seccion Garantia tiene que pintar, cada una de su propia
		// mitad: lo que el fabricante publico del alcance y de la regla, el sobre publicado solo, la
		// respuesta compuesta del vehiculo, y la de cada sistema.
		private static readonly string[] TheDomainsSentencesTheSectionMustQuote =
		{
			"@Sheet.Coverage.Says",
			"@Sheet.ServicingRule.Says",
			"@Stands.PublishedSays",
			"@Stands.Says",
			"@system.Says",
		};

		private static string TheSectionAsItIsWritten(string name)
		{
			foreach (string file in TheSectionFiles())
				if (Path.GetFileName(file) == name) return File.ReadAllText(file);

			Assert.Fail($"'{name}' no llego al output.");
			return "";
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
