using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.JSInterop;
using Municipalidad.Wasm;
using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// LAS DOS MITADES DEL PUENTE CON GNOTHI SE NOMBRAN POR CADENA, y nada las obliga a coincidir.
	//
	// Cuando el modulo corre hospedado en el telefono hay dos direcciones y cada una cruza por un
	// nombre escrito a mano en los DOS lados:
	//
	//     Gnothi -> modulo    el JS llama a C# por DotNet.invokeMethodAsync(ensamblado, 'Metodo')
	//     modulo -> Gnothi    el C# importa el modulo JS y llama a sus funciones por nombre
	//
	// Un nombre que se cambia en un lado y no en el otro NO ROMPE LA COMPILACION: falla en el
	// aparato, en silencio, y lo unico que se ve es que el Host no recibe respuesta o que el modulo
	// no trae eventos. En la rama de donde sale esta forma ya paso una vez, con el verbo que avisa que
	// el actor esta hidratado.
	//
	// POR QUE ASI Y NO EN UN NAVEGADOR. Manejar la superficie de verdad necesita un WebView con Gnothi
	// del otro lado inyectando su puente; esto afirma lo que se puede afirmar sin aparato: que los dos
	// lados escriban los mismos nombres. Lo que NO afirma es que el WebView los invoque bien -- eso se
	// mide en el telefono, y hasta que se mida esta fixture es lo unico que hay.
	[TestFixture]
	public sealed class TheBridgeAndTheWebViewSurfaceNameTheSameVerbs
	{
		// EL ENSAMBLADO SE NOMBRA POR CADENA EN EL JS y por reflexion aca. Si el csproj cambia su
		// AssemblyName, el JS deja de encontrar el C# y nada mas se rompe.
		[Test]
		public void TheBridgeNamesTheAssemblyTheShellActuallyBuilds()
		{
			string enElJs = UnicaCoincidencia(ElPuenteEnJs(), @"const ENSAMBLADO = '([^']+)'");

			Assert.That(enElJs, Is.EqualTo(typeof(SuperficieDelModulo).Assembly.GetName().Name),
				"El nombre del ensamblado que el JS le pasa a DotNet tiene que ser el que el proyecto "
				+ "construye de verdad.");
		}

		// LO QUE EL JS INVOCA EN C# TIENE QUE SER EXACTAMENTE LO QUE LLEVA [JSInvokable], en los dos
		// sentidos: un metodo invocado que no existe falla en el aparato, y un metodo marcado que
		// nadie invoca es una superficie que el Host no puede alcanzar.
		[Test]
		public void EveryVerbTheBridgeInvokesIsAMethodTheSurfaceExposesAndTheOtherWayRound()
		{
			List<string> invocadosPorElJs = TodasLasCoincidencias(
				ElPuenteEnJs(), @"DotNet\.invokeMethodAsync\(\s*ENSAMBLADO\s*,\s*'([^']+)'");

			List<string> marcadosEnLaSuperficie = new List<string>();

			foreach (MethodInfo cada in typeof(SuperficieDelModulo).GetMethods(
				BindingFlags.Public | BindingFlags.Static))
				if (cada.GetCustomAttribute<JSInvokableAttribute>() != null)
					marcadosEnLaSuperficie.Add(cada.Name);

			invocadosPorElJs.Sort(StringComparer.Ordinal);
			marcadosEnLaSuperficie.Sort(StringComparer.Ordinal);

			Assert.Multiple(() =>
			{
				Assert.That(invocadosPorElJs, Is.EqualTo(marcadosEnLaSuperficie),
					"Los verbos que el puente invoca y los metodos que la superficie expone son el mismo "
					+ "juego, o el Host llama a algo que no existe.");

				Assert.That(marcadosEnLaSuperficie, Has.Count.EqualTo(3),
					"Son tres verbos: que sabe contestar el modulo, una consulta, y el sobre que trae el "
					+ "cartero. Un cuarto entra por aca o el Host no lo ve.");
			});
		}

		// Y LA OTRA DIRECCION: cada funcion que el C# le pide al modulo JS tiene que estar exportada.
		// Las dos listas se leen del codigo y ninguna esta escrita a mano en esta prueba.
		[Test]
		public void EveryFunctionTheShellAsksTheBridgeForIsExportedByIt()
		{
			string enJs = ElPuenteEnJs();

			List<string> exportadas = TodasLasCoincidencias(
				enJs, @"export\s+(?:async\s+)?function\s+(\w+)");

			// SOLO LAS QUE VAN CONTRA EL MODULO IMPORTADO. El propio "import" es una llamada al
			// runtime del navegador y no una funcion del puente, asi que el patron se ancla en la
			// referencia al modulo en vez de excluir un nombre a mano.
			List<string> pedidasPorElC = TodasLasCoincidencias(
				ElPuenteEnCSharp(), @"ModuloAsync\(\)\)\.Invoke(?:Void)?Async(?:<[^>]+>)?\(""(\w+)""");

			exportadas.Sort(StringComparer.Ordinal);
			pedidasPorElC.Sort(StringComparer.Ordinal);

			Assert.That(pedidasPorElC, Is.SubsetOf(exportadas),
				"El C# le pide al puente una funcion que el JS no exporta: "
				+ string.Join(", ", pedidasPorElC) + " contra " + string.Join(", ", exportadas));

			Assert.That(pedidasPorElC, Has.Count.GreaterThanOrEqualTo(6),
				"Y son al menos seis: si hay, avisar que el actor esta listo, pedir eventos, y las tres "
				+ "del journal. Menos que eso quiere decir que la regex dejo de ver las llamadas y esta "
				+ "prueba se volvio verde por no mirar nada.");
		}

		// LOS TRES VERBOS QUE EL NATIVO LLAMA viven en window.gnothi, y ese objeto lo publica el JS.
		// El nativo los invoca por su nombre en minusculas, que es la unica cara que ve.
		[Test]
		public void TheBridgePublishesTheThreeVerbsTheNativeSideCallsByName()
		{
			List<string> publicados = TodasLasCoincidencias(
				ElPuenteEnJs(), @"window\.gnothi\.(\w+)\s*=\s*function");

			Assert.That(publicados, Is.SupersetOf(new List<string> { "tools", "call", "deliver" }),
				"Gnothi llama window.gnothi.tools(), .call(tool, args) y .deliver(sobre). Un nombre "
				+ "distinto aca no lo ve nadie hasta el aparato.");
		}

		// ------------------------------------------------------ el aparejo

		// LOS DOS ARCHIVOS VIAJAN AL OUTPUT COMO DATO, por lo mismo que el manifiesto y los csproj:
		// --artifacts-path reubica el output entero, asi que ninguna cantidad fija de '..' llega de
		// vuelta al modulo. Y como DATO y no por reflexion porque lo que se afirma es lo que dice el
		// TEXTO: los nombres viven en cadenas, que es precisamente el problema.
		private static string ElPuenteEnJs() => ElDato("gnothi-interop.js.dato");

		private static string ElPuenteEnCSharp() => ElDato("PuenteDeGnothi.cs.dato");

		private static string ElDato(string nombre)
		{
			string ruta = Path.Combine(TestContext.CurrentContext.TestDirectory, nombre);

			Assert.That(File.Exists(ruta), Is.True,
				"No llego " + nombre + " al output como dato. Sin el, esta prueba no puede fallar, y una "
				+ "prueba que no puede fallar no sirve de nada: ver el ItemGroup del csproj.");

			return File.ReadAllText(ruta);
		}

		private static string UnicaCoincidencia(string texto, string patron)
		{
			Match encontrada = Regex.Match(texto, patron);

			Assert.That(encontrada.Success, Is.True, "el patron no encontro nada: " + patron);

			return encontrada.Groups[1].Value;
		}

		private static List<string> TodasLasCoincidencias(string texto, string patron)
		{
			List<string> todas = new List<string>();

			foreach (Match cada in Regex.Matches(texto, patron)) todas.Add(cada.Groups[1].Value);

			Assert.That(todas, Is.Not.Empty, "el patron no encontro nada: " + patron);

			return todas;
		}
	}
}
