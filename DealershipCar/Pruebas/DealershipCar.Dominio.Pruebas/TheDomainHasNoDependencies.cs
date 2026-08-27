using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LA INVARIANTE CENTRAL DE ARCHITECTURE 3.1, ejecutable.
	//
	// "Que este csproj no tenga un solo PackageReference ni ProjectReference es la prueba de
	// que el dominio no cambia al cruzar a wasm. Si algun dia necesita una, ESO ES EL
	// HALLAZGO."
	//
	// Hasta ahora eso vivia como una frase en un documento y un comentario en un csproj: se
	// cumple las primeras semanas. Aca queda como prueba, que es como se cumple siempre. El
	// dia que alguien agregue Newtonsoft "solo para serializar", esto se pone rojo y dice
	// exactamente que entro y por donde.
	[TestFixture]
	public sealed class TheDomainHasNoDependencies
	{
		private const string EnsambladoDelDominio = "DealershipCar.Dominio";

		// Lo unico que un dominio puro puede tocar: la biblioteca base. Nada de HTTP, nada
		// de JSON, nada de motor.
		private static readonly string[] PrefijosDeLaBibliotecaBase =
		{
			"System",
			"netstandard",
			"mscorlib",
			"Microsoft.CSharp",
			"Microsoft.VisualBasic",
		};

		// El csproj del dominio, copiado al output como dato por el csproj de pruebas.
		private const string CsprojDelDominio = "DealershipCar.Dominio.csproj.dato";

		private Assembly dominio;

		[SetUp]
		public void Setup() => dominio = Assembly.Load(new AssemblyName(EnsambladoDelDominio));

		// LA PRUEBA QUE DE VERDAD MUERDE, y la razon por la que no alcanza con la reflexion.
		//
		// Assembly.GetReferencedAssemblies() solo ve lo que el compilador EMITIO. Una
		// dependencia declarada y todavia no usada se optimiza y no aparece — MEDIDO: con
		// Newtonsoft.Json declarado en el dominio, la prueba por reflexion pasaba en verde.
		// O sea que habria dado confianza falsa justo el dia que hay que detectar algo.
		//
		// Asi que se cuenta sobre la FUENTE DE LA VERDAD: el propio csproj. Una referencia
		// declarada se ve aunque nadie la use todavia, que es exactamente cuando conviene
		// verla — antes de que alguien escriba el using.
		[Test]
		public void TheDomainCsprojDeclaresNotASingleReference()
		{
			string ruta = Path.Combine(AppContext.BaseDirectory, CsprojDelDominio);
			Assert.That(File.Exists(ruta), Is.True,
				$"No se encontro '{CsprojDelDominio}' en el output. Lo copia el csproj de "
				+ "pruebas; si desaparecio, esta prueba dejo de proteger nada y hay que arreglarla.");

			XDocument csproj = XDocument.Load(ruta);

			List<string> declaradas = csproj.Descendants()
				.Where(elemento => elemento.Name.LocalName == "PackageReference"
					|| elemento.Name.LocalName == "ProjectReference")
				.Select(elemento => elemento.Name.LocalName + " " + (string)elemento.Attribute("Include"))
				.OrderBy(texto => texto, StringComparer.Ordinal)
				.ToList();

			Assert.That(declaradas, Is.Empty,
				"El dominio declaro referencias: " + string.Join(" | ", declaradas) + ". "
				+ "ARCHITECTURE 3.1: que este csproj no tenga ni una es LA PRUEBA de que el "
				+ "dominio no cambia al cruzar a wasm. Si de verdad hace falta, ESO ES EL "
				+ "HALLAZGO — se para y se reporta, no se agrega la referencia ni se afloja "
				+ "esta prueba.");
		}

		// El nombre completo cuenta, no un "empieza con": 'Systematic.Cosas' no es
		// System.* por mas que lo parezca. Se compara el nombre exacto o el primer
		// segmento del nombre punteado.
		private static bool EsDeLaBibliotecaBase(string nombre)
		{
			foreach (string prefijo in PrefijosDeLaBibliotecaBase)
			{
				if (string.Equals(nombre, prefijo, StringComparison.Ordinal)) return true;
				if (nombre.StartsWith(prefijo + ".", StringComparison.Ordinal)) return true;
			}
			return false;
		}

		// La segunda red, por reflexion. NO reemplaza a la de arriba — ve menos, porque solo
		// reporta lo que el compilador emitio. Pero ve algo que la otra no: una dependencia
		// que llegue por un camino que no sea una referencia declarada en ESE csproj (un
		// Directory.Build.props, un import, un paquete transitivo que se termine usando).
		// Las dos juntas cubren declarado Y usado.
		[Test]
		public void TheDomainReferencesOnlyTheBaseLibrary()
		{
			List<string> intrusos = dominio.GetReferencedAssemblies()
				.Select(referencia => referencia.Name)
				.Where(nombre => !EsDeLaBibliotecaBase(nombre))
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToList();

			Assert.That(intrusos, Is.Empty,
				"El dominio dejo de ser puro. Referencias que no son de la biblioteca base: "
				+ string.Join(", ", intrusos) + ". "
				+ "Eso no se arregla agregando la referencia al allowlist: ES EL HALLAZGO. "
				+ "Un dominio con dependencias ya no se puede afirmar que cruza a wasm sin cambiar.");
		}

		// El motor, el mecanismo y el transporte por nombre propio. Es redundante con la
		// prueba de arriba a proposito: cuando esta se pone roja, el mensaje nombra al
		// culpable en vez de obligar a leer una lista.
		[Test]
		public void TheDomainKnowsNeitherTheEngineNorTheTransport()
		{
			string[] prohibidos = { "Ncubo", "Puppeteer", "Choreography", "Puppets.Portador", "ModelContextProtocol" };

			List<string> referencias = dominio.GetReferencedAssemblies()
				.Select(referencia => referencia.Name)
				.ToList();

			foreach (string prohibido in prohibidos)
			{
				bool aparece = referencias.Any(nombre =>
					nombre.IndexOf(prohibido, StringComparison.OrdinalIgnoreCase) >= 0);

				Assert.That(aparece, Is.False,
					$"El dominio referencia '{prohibido}'. El puppet no conoce a su titiritero: "
					+ "el motor entra por DealershipCar.App, y el mecanismo le habla al journal "
					+ "por IDiarioDeLaApp, que implementa la app.");
			}
		}

		// La otra mitad de S1 (ver SPIKES-OS2.md): el dominio queda internal, y solo dos
		// ensamblados pueden verlo por dentro. Cada nombre de mas es una carátula con la
		// posibilidad de hablarle al dominio por la espalda en vez de entrar por
		// DealershipCarAPI, que es la unica puerta.
		[Test]
		public void OnlyTwoAssembliesSeeInsideTheDomain()
		{
			string[] esperados = { "DealershipCar.App", "DealershipCar.Dominio.Pruebas" };

			List<string> declarados = dominio
				.GetCustomAttributes<InternalsVisibleToAttribute>()
				.Select(atributo => atributo.AssemblyName)
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToList();

			Assert.That(declarados, Is.EquivalentTo(esperados),
				"Cambio quien ve el interior del dominio. Declarados: ["
				+ string.Join(", ", declarados) + "]. Si hace falta uno mas, la razon va escrita "
				+ "en AssemblyInfo.cs, no solo aca.");
		}
	}
}
