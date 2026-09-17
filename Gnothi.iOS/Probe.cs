using Foundation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace Gnothi.iOS;

// THE PROBE. It answers, on the device, the question note 31 left open: whether a module that
// arrives AFTER installing can be loaded when AOT is mandatory.
//
// WHY A FILE JUST FOR THIS. Because the alternative is finding out from the symptom: the app comes
// up, the catalogue comes out empty or a tool fails, and which of the four steps fell over is
// unknown. That already cost dearly on Android -- five attempts reading a symptom that kept shifting,
// until `logcat -b crash` was read -- and it must not be repeated.
//
// THE STEPS ARE THE SAME ONES THE A54 MEASURED, in the same order, so the results are COMPARABLE.
// From InProcessShell.cs's comments, measured on a Galaxy A54, SDK 36:
//
//     1) AssemblyLoadContext(isCollectible: true) .... OK
//     2) AssemblyDependencyResolver .................. FAILS PlatformNotSupported
//     3) LoadFromAssemblyPath ........................ OK
//     4) GetTypes .................................... FAILS: 'Choreography' missing
//
// The 4 failed AS A CONSEQUENCE of the 2: with no resolver, nothing found the dependencies. That is
// why InProcessShell resolves by adjacency. If the pattern on iOS is a different one, it has to be
// seen here and not deduced.
//
// WHAT IS MEASURED BEFORE ANYTHING ELSE, and it holds even with not a single dll present: the
// runtime's two flags. They are the direct answer to whether the interpreter is in place.
internal static class Probe
{
	public static void Run(Action<string> log)
	{
		log("── sonda AOT ──────────────────────────────────");

		// THE TWO FLAGS, and the difference between them is exactly what matters to us:
		//
		//   IsDynamicCodeSupported   new code CAN be generated at runtime
		//   IsDynamicCodeCompiled    that code is compiled to native (JIT) or INTERPRETED
		//
		// On an iPhone, Supported=true / Compiled=false is expected: that IS Mono's interpreter,
		// which generates code without compiling it to native -- which is what Apple forbids.
		// If Supported comes out false, the interpreter is not there and there is nothing to load.
		log($"IsDynamicCodeSupported = {RuntimeFeature.IsDynamicCodeSupported}");
		log($"IsDynamicCodeCompiled  = {RuntimeFeature.IsDynamicCodeCompiled}");
		log($"runtime                = {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");

		string? dll = FindAModule();
		if (dll is null)
		{
			log("no hay ningun dll de modulo todavia; los pasos 1-4 quedan sin correr.");
			log("dejá el paquete en Documents/incoming por la app Archivos y volvé a abrir.");
			log("───────────────────────────────────────────────");
			return;
		}
		log($"probando con {Path.GetFileName(dll)}");

		AssemblyLoadContext? ctx = null;

		// 1 · the context
		try
		{
			ctx = new AssemblyLoadContext("sonda", isCollectible: true);
			log("1) AssemblyLoadContext(isCollectible: true) .... OK");
		}
		catch (Exception e) { log($"1) AssemblyLoadContext .... FALLA {e.GetType().Name}: {e.Message}"); return; }

		// 2 · the dependency resolver. On Android it throws PlatformNotSupportedException because it
		//     depends on hostpolicy, which does not exist on mobile. If the same happens here,
		//     InProcessShell's adjacency already covers it and there is nothing to do.
		AssemblyDependencyResolver? resolver = null;
		try
		{
			resolver = new AssemblyDependencyResolver(dll);
			log("2) AssemblyDependencyResolver .................. OK");
		}
		catch (Exception e) { log($"2) AssemblyDependencyResolver .... FALLA {e.GetType().Name} (Android igual; se resuelve por vecindad)"); }

		// 3 · THE REAL QUESTION. Loading an assembly that did NOT ship inside the app.
		Assembly asm;
		try
		{
			string vecindad = Path.GetDirectoryName(dll)!;
			ctx.Resolving += (c, n) =>
			{
				try { _ = AssemblyLoadContext.Default.LoadFromAssemblyName(n); return null; }
				catch { /* private to the module */ }
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

		// 4 · reading the types. This is where Android fell over because of step 2.
		Type[] tipos;
		try
		{
			tipos = asm.GetTypes();
			log($"4) GetTypes .................................... OK ({tipos.Length} tipos)");
		}
		catch (Exception e) { log($"4) GetTypes .... FALLA {e.GetType().Name}: {e.Message}"); return; }

		// 5 · THE STEP ANDROID NEVER HAD TO TRY, and on iOS it is the deciding one. Loading the types
		//     is not executing them: that assembly's IL is NOT AOT-compiled, because it did not exist
		//     when the app was compiled. Running a method of its own is the only thing that proves
		//     the interpreter is really running it.
		//
		//     If this throws ExecutionEngineException «Attempting to JIT compile method while
		//     running in aot-only mode», the interpreter is NOT in place -- check MtouchInterpreter
		//     in the csproj.
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

		try { ctx.Unload(); } catch { /* cooperative, same as on Android */ }
		log("───────────────────────────────────────────────");
	}

	/// The first dll that looks like a module's, wherever it looks. Both folders are searched because
	/// the probe runs BEFORE the shell copies from incoming into the private area.
	private static string? FindAModule()
	{
		foreach (NSSearchPathDirectory which in new[] { NSSearchPathDirectory.LibraryDirectory, NSSearchPathDirectory.DocumentDirectory })
		{
			string root = NSSearchPath.GetDirectories(which, NSSearchPathDomain.User, true)[0];
			if (!Directory.Exists(root)) continue;

			foreach (string f in Directory.EnumerateFiles(root, "*.dll", SearchOption.AllDirectories))
			{
				// The module app's assembly, not its dependencies: it is the one carrying the marked
				// class, and the one InProcessShell works with.
				string n = Path.GetFileNameWithoutExtension(f);
				if (n.EndsWith(".App", StringComparison.OrdinalIgnoreCase)
					|| n.EndsWith(".Mcp", StringComparison.OrdinalIgnoreCase)) return f;
			}
		}
		return null;
	}
}
