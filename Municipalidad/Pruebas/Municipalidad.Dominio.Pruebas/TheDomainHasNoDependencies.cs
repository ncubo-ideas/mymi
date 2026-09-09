using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// LA PRIMERA LEY DEL REPO, y la unica que ARCHITECTURE.md §8 pone primero: el dominio no
	// cambia al cruzar a wasm, y la prueba de eso es que su csproj no declara NI UNA referencia.
	//
	// SE LEE EL CSPROJ, NO EL ENSAMBLADO. Assembly.GetReferencedAssemblies() solo reporta lo que
	// el compilador emitio de verdad, asi que una dependencia declarada y no usada se optimiza y
	// desaparece: con un PackageReference a Newtonsoft.Json metido en el dominio de Concesionario,
	// la prueba por reflexion pasaba igual. Por eso el csproj viaja al output como dato.
	//
	// InternalsVisibleTo es un ATRIBUTO, no una referencia, y por eso no se cuenta aca. Lo que se
	// cuenta de el es otra cosa y esta en la segunda prueba.
	[TestFixture]
	public sealed class TheDomainHasNoDependencies
	{
		private XDocument elCsproj;

		[SetUp]
		public void Leer()
		{
			string ruta = Path.Combine(
				TestContext.CurrentContext.TestDirectory, "Municipalidad.Dominio.csproj.dato");

			Assert.That(File.Exists(ruta), Is.True,
				"El csproj del dominio no llego al output como dato. Sin el, esta prueba no puede "
				+ "fallar, y una prueba que no puede fallar no sirve de nada: ver el ItemGroup del "
				+ "csproj de este proyecto.");

			elCsproj = XDocument.Load(ruta);
		}

		[Test]
		public void TheCsprojDeclaresNoReferenceOfAnyKind()
		{
			List<string> declaradas = new List<string>();

			foreach (XElement cada in elCsproj.Descendants("PackageReference"))
				declaradas.Add("PackageReference " + (string)cada.Attribute("Include"));

			foreach (XElement cada in elCsproj.Descendants("ProjectReference"))
				declaradas.Add("ProjectReference " + (string)cada.Attribute("Include"));

			Assert.That(declaradas, Is.Empty,
				"El dominio declaro una referencia. Esa vacuidad ES la prueba de que el dominio no "
				+ "cambia al cruzar a wasm, asi que si de verdad hace falta una, el hallazgo es la "
				+ "referencia y no esta prueba: pararse y reportarlo. Declaradas: "
				+ string.Join(", ", declaradas));
		}

		// LA CERCA, medida sobre la lista DECLARADA y no sobre la disciplina de nadie.
		//
		// HOY ES UNA SOLA, y hasta M-6 eran dos: Municipalidad.Consola veia el dominio por dentro
		// porque ahi vivian las 91 comprobaciones. Esa era C3, aceptada y anotada, y esta prueba se
		// puso ROJA el dia que el permiso se cayo -- que era el punto de afirmar la lista EXACTA en
		// vez de una de permitidos.
		//
		// AHORA LA LEY ES LA DE CONCESIONARIO: solo las Pruebas entran, y la App y la Consola estan
		// deliberadamente AFUERA, porque una caratula que puede nombrar tipos del dominio puede
		// re-derivar.
		[Test]
		public void OnlyTheseAssembliesSeeInsideTheDomain()
		{
			List<string> concedidos = new List<string>();

			foreach (XElement cada in elCsproj.Descendants("InternalsVisibleTo"))
				concedidos.Add((string)cada.Attribute("Include"));

			concedidos.Sort();

			Assert.That(concedidos, Is.EqualTo(new[]
			{
				"Municipalidad.Dominio.Pruebas",
			}), "La lista de quien ve el dominio por dentro cambio. Nadie entra sin una razon "
				+ "escrita, y la razon se escribe en el csproj del dominio, no solo aca.");
		}

		// Y NINGUNA CARATULA COMPILA CONTRA EL DOMINIO, que es la otra mitad de lo mismo: la cerca de
		// arriba dice quien PUEDE verlo por dentro, y esto dice que ninguna de ellas lo NOMBRA en C#.
		// Sin una referencia de compilacion no hay `using Municipalidad.Dominio` posible.
		//
		// NINGUNA DE LAS TRES LO REFERENCIA: la Consola desde M-6, el Mcp desde C-1, y la Wasm desde
		// que nacio en W-1. La App si lo referencia, y esa mitad la mide la prueba de abajo.
		//
		// ESTE NOMBRE DECIA `Neither` Y C-1 LO VOLVIO FALSO: la caratula del asistente es una tercera,
		// y "ninguna de las dos" nombraba una ley mas chica que la que rige. El nombre se mueve con la
		// ley, que es lo que DEC-33 ya hizo una vez en este modulo con TheOperationsAreThirtyFour.
		// W-1 agrega la cuarta caratula y el nombre ya no se tuvo que mover, que era el punto.
		[Test]
		public void NoShellCompilesAgainstTheDomain()
		{
			Assert.Multiple(() =>
			{
				XElement laConsola = ElCsprojDeDato("Municipalidad.Consola.csproj.dato");

				Assert.That(ReferenciasAlDominioDe(laConsola), Is.Empty,
					"Municipalidad.Consola no referencia al dominio de ninguna manera. Las 91 "
					+ "comprobaciones que la obligaban se mudaron a Pruebas/ en M-6.");

				XElement elMcp = ElCsprojDeDato("Municipalidad.Mcp.csproj.dato");

				Assert.That(ReferenciasAlDominioDe(elMcp), Is.Empty,
					"Municipalidad.Mcp tampoco. Es la caratula que mas facil lo habria referenciado: "
					+ "abre el journal ella misma, y el anfitrion de solo lectura necesita el "
					+ "ensamblado del dominio -- que le llega por ElDominio.");

				XElement laWasm = ElCsprojDeDato("Municipalidad.Wasm.csproj.dato");

				Assert.That(ReferenciasAlDominioDe(laWasm), Is.Empty,
					"Municipalidad.Wasm tampoco, y es la que tuvo el motivo mas fuerte para hacerlo: "
					+ "adentro de un navegador no hay ruta de disco de donde cargar el dominio, asi "
					+ "que una referencia de compilacion suya habria sido el atajo. Lo que W-1 cambio "
					+ "es COMO carga la App, no QUIEN puede nombrar.");
			});
		}

		// LA OTRA MITAD, Y ES LA QUE MIDE LA COSA EN VEZ DE UN SUSTITUTO SUYO.
		//
		// Hasta W-1 esto se afirmaba sobre el csproj de la App: su referencia al dominio declaraba
		// ReferenceOutputAssembly="false", o sea que no era de compilacion. W-1 la volvio de
		// compilacion, porque cargar por ruta no funciona adentro de un navegador -- DEC-55 --, asi
		// que ese atributo ya no dice nada y afirmarlo seria afirmar lo contrario de lo que rige.
		//
		// Lo que la cerca siempre quiso decir es que la Superficie NO PUEDE DECLARAR UNA VARIABLE
		// DEL DOMINIO, y eso nunca dependio del atributo: depende de que todo tipo del dominio sea
		// internal y de que la App no este en la lista de InternalsVisibleTo, que es la prueba de
		// mas arriba. Esto afirma la primera mitad DIRECTAMENTE, que es mas fuerte que el atributo
		// y no menos: con un tipo publico ahi adentro, la referencia de compilacion pasa a ser
		// nombrable y esta prueba se pone roja.
		//
		// Y SE ENTRA POR ElDominio A PROPOSITO: es el mismo camino que usan las cuatro caratulas,
		// asi que si la carga por nombre dejara de resolver, esto se pone rojo antes que un shell.
		[Test]
		public void TheDomainExposesNoTypeAShellCouldName()
		{
			Assembly elDominio = Municipalidad.App.ElDominio.Assembly;

			Type[] todos = elDominio.GetTypes();

			Assert.That(todos, Is.Not.Empty,
				"El ensamblado del dominio se cargo vacio, asi que esta prueba pasaria por vacuidad.");

			List<string> publicos = new List<string>();

			foreach (Type cada in todos)
				if (cada.IsPublic || cada.IsNestedPublic) publicos.Add(cada.FullName);

			Assert.That(publicos, Is.Empty,
				"El dominio expuso un tipo publico. Municipalidad.App lo referencia en compilacion "
				+ "desde W-1, asi que un tipo publico ahi adentro es un tipo que la Superficie puede "
				+ "declarar, y una caratula que puede nombrar el dominio puede re-derivar. Publicos: "
				+ string.Join(", ", publicos));
		}

		private static XElement ElCsprojDeDato(string nombre)
		{
			string ruta = Path.Combine(TestContext.CurrentContext.TestDirectory, nombre);

			Assert.That(File.Exists(ruta), Is.True,
				"No esta " + nombre + " junto a la prueba. Viaja como dato desde el csproj de "
				+ "Pruebas/, y sin el esta prueba no mide nada.");

			return XElement.Load(ruta);
		}

		private static List<XElement> ReferenciasAlDominioDe(XElement elCsprojDeUnaCaratula)
		{
			List<XElement> alDominio = new List<XElement>();

			foreach (XElement cada in elCsprojDeUnaCaratula.Descendants("ProjectReference"))
				if (((string)cada.Attribute("Include")).Contains("Municipalidad.Dominio.csproj"))
					alDominio.Add(cada);

			return alDominio;
		}
	}
}
