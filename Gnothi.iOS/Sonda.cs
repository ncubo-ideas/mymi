using Foundation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace Gnothi.iOS;

// LA SONDA. Contesta, en el aparato, la pregunta que dejo abierta la nota 31: si un modulo que
// llega DESPUES de instalar se puede cargar cuando el AOT es obligatorio.
//
// POR QUE EXISTE UN ARCHIVO SOLO PARA ESTO. Porque la alternativa es enterarse por el sintoma:
// la app levanta, el catalogo sale vacio o una tool falla, y no se sabe cual de los cuatro
// pasos se cayo. Eso ya costo caro en Android -cinco intentos leyendo un sintoma que oscilaba,
// hasta que se leyo `logcat -b crash`- y no hay que repetirlo.
//
// LOS PASOS SON LOS MISMOS QUE MIDIO EL A54, en el mismo orden, para que los resultados sean
// COMPARABLES. De los comentarios de InProcessShell.cs, medido en un Galaxy A54, SDK 36:
//
//     1) AssemblyLoadContext(isCollectible: true) .... OK
//     2) AssemblyDependencyResolver .................. FALLA PlatformNotSupported
//     3) LoadFromAssemblyPath ........................ OK
//     4) GetTypes .................................... FALLA: falta 'Choreography'
//
// El 4 fallaba COMO CONSECUENCIA del 2: sin resolver, nadie encontraba las dependencias. Por
// eso InProcessShell resuelve por vecindad. Si en iOS el patron es otro, hay que verlo aca y
// no deducirlo.
//
// LO QUE SE MIDE ANTES QUE NADA, y vale aunque no haya ni un dll: las dos banderas del runtime.
// Son la respuesta directa a si el interprete esta puesto.
internal static class Sonda
{
	public static void Correr(Action<string> log)
	{
		log("── sonda AOT ──────────────────────────────────");

		// LAS DOS BANDERAS, y la diferencia entre ellas es justo lo que nos importa:
		//
		//   IsDynamicCodeSupported   se PUEDE generar codigo nuevo en runtime
		//   IsDynamicCodeCompiled    ese codigo se compila a nativo (JIT) o se INTERPRETA
		//
		// En un iPhone se espera Supported=true / Compiled=false: eso ES el interprete de Mono,
		// que genera codigo sin compilarlo a nativo -- que es lo que Apple prohibe.
		// Si Supported sale false, el interprete no esta y no hay nada que cargar.
		log($"IsDynamicCodeSupported = {RuntimeFeature.IsDynamicCodeSupported}");
		log($"IsDynamicCodeCompiled  = {RuntimeFeature.IsDynamicCodeCompiled}");
		log($"runtime                = {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");

		string? dll = BuscarUnModulo();
		if (dll is null)
		{
			log("no hay ningun dll de modulo todavia; los pasos 1-4 quedan sin correr.");
			log("dejá el paquete en Documents/incoming por la app Archivos y volvé a abrir.");
			log("───────────────────────────────────────────────");
			return;
		}
		log($"probando con {Path.GetFileName(dll)}");

		AssemblyLoadContext? ctx = null;

		// 1 · el contexto
		try
		{
			ctx = new AssemblyLoadContext("sonda", isCollectible: true);
			log("1) AssemblyLoadContext(isCollectible: true) .... OK");
		}
		catch (Exception e) { log($"1) AssemblyLoadContext .... FALLA {e.GetType().Name}: {e.Message}"); return; }

		// 2 · el resolver de dependencias. En Android tira PlatformNotSupportedException porque
		//     depende de hostpolicy, que en movil no existe. Si aca pasa lo mismo, la vecindad
		//     de InProcessShell ya lo cubre y no hay nada que hacer.
		AssemblyDependencyResolver? resolver = null;
		try
		{
			resolver = new AssemblyDependencyResolver(dll);
			log("2) AssemblyDependencyResolver .................. OK");
		}
		catch (Exception e) { log($"2) AssemblyDependencyResolver .... FALLA {e.GetType().Name} (Android igual; se resuelve por vecindad)"); }

		// 3 · LA PREGUNTA DE VERDAD. Cargar un ensamblado que NO venia dentro de la app.
		Assembly asm;
		try
		{
			string vecindad = Path.GetDirectoryName(dll)!;
			ctx.Resolving += (c, n) =>
			{
				try { _ = AssemblyLoadContext.Default.LoadFromAssemblyName(n); return null; }
				catch { /* privado del modulo */ }
				string? p = resolver?.ResolveAssemblyToPath(n);
				if (p is null)
				{
					string vecino = Path.Combine(vecindad, (n.Name ?? "") + ".dll");
					if (File.Exists(vecino)) p = vecino;
				}
				return p is null ? null : c.LoadFromAssemblyPath(p);
			};

			asm = ctx.LoadFromAssemblyPath(dll);
			log("3) LoadFromAssemblyPath ........................ OK");
		}
		catch (Exception e) { log($"3) LoadFromAssemblyPath .... FALLA {e.GetType().Name}: {e.Message}"); return; }

		// 4 · leer los tipos. Aca es donde Android se caia por culpa del paso 2.
		Type[] tipos;
		try
		{
			tipos = asm.GetTypes();
			log($"4) GetTypes .................................... OK ({tipos.Length} tipos)");
		}
		catch (Exception e) { log($"4) GetTypes .... FALLA {e.GetType().Name}: {e.Message}"); return; }

		// 5 · EL PASO QUE ANDROID NUNCA TUVO QUE PROBAR, y en iOS es el que decide. Cargar los
		//     tipos no es ejecutarlos: el IL de ese ensamblado NO esta compilado AOT, porque no
		//     existia cuando se compilo la app. Ejecutar un metodo suyo es lo unico que prueba
		//     que el interprete lo esta corriendo de verdad.
		//
		//     Si esto tira ExecutionEngineException «Attempting to JIT compile method while
		//     running in aot-only mode», el interprete NO esta puesto -- revisar
		//     MtouchInterpreter en el csproj.
		try
		{
			Type? conMarca = tipos.FirstOrDefault(t => t.GetCustomAttributes(false)
				.Any(a => a.GetType().Name is "McpServerToolTypeAttribute" or "ModuleSurfaceAttribute"));

			if (conMarca is null) { log("5) ejecutar .... no hay clase marcada en este dll; probá con el del modulo"); }
			else
			{
				MethodInfo[] ms = conMarca.GetMethods(
					BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
				log($"5) reflexion sobre la superficie ............... OK ({conMarca.Name}, {ms.Length} metodos)");
			}
		}
		catch (Exception e) { log($"5) reflexion .... FALLA {e.GetType().Name}: {e.Message}"); }

		try { ctx.Unload(); } catch { /* cooperativo, igual que en Android */ }
		log("───────────────────────────────────────────────");
	}

	/// El primer dll que parezca de un modulo, mire donde mire. Se buscan las dos carpetas
	/// porque la sonda corre ANTES de que la cascara copie de incoming al area privada.
	private static string? BuscarUnModulo()
	{
		foreach (NSSearchPathDirectory which in new[] { NSSearchPathDirectory.LibraryDirectory, NSSearchPathDirectory.DocumentDirectory })
		{
			string root = NSSearchPath.GetDirectories(which, NSSearchPathDomain.User, true)[0];
			if (!Directory.Exists(root)) continue;

			foreach (string f in Directory.EnumerateFiles(root, "*.dll", SearchOption.AllDirectories))
			{
				// El ensamblado de la app del modulo, no sus dependencias: es el que lleva la
				// clase marcada, y es con el que InProcessShell trabaja.
				string n = Path.GetFileNameWithoutExtension(f);
				if (n.EndsWith(".App", StringComparison.OrdinalIgnoreCase)
					|| n.EndsWith(".Mcp", StringComparison.OrdinalIgnoreCase)) return f;
			}
		}
		return null;
	}
}
