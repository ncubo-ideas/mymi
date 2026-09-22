using System.Runtime.InteropServices;
using System.Text;

namespace Gnothi.Host;

// THE CANARY: Gnothi checks its own cage BEFORE putting anyone inside it. See B12, C10 and F8.
//
// WHAT QUESTION IT ANSWERS, and it is not the obvious one. What D2 asks for is that containment can
// be ATTEMPTED TO BREAK and seen to fail. That is not the same as counting how many times a module
// tried -- that counter was looked for and does not exist: wasmtime publishes no event per denied
// access, because from the inside there is nothing to deny; the syscall DOES NOT EXIST, and what
// never happens cannot be counted.
//
// The canary answers the other half, which is the half that matters anyway: **does the cage Gnothi
// just built actually contain?** Gnothi builds a box with EXACTLY the configuration it is about to
// hand a module -- same function, `Cage.Build` --, puts a probe of ITS OWN inside, and checks that
// the eight escape attempts fail and that the ninth, the one that must work, works.
//
// THE PROBE IS GNOTHI'S, AND THAT IS THE WHOLE POINT. If the verdict were written by the
// institution's code, a module that wants out only has to sing "shut" and it is done. `LosIntentos`
// lives in the half of the artifact Gnothi controls -- the substrate --, never in the package's
// DLLs, and that split has to hold when the substrate is split out.
//
// AND IF IT SINGS WRONG, NOTHING OPENS. An open cage is not a warning you write to the log and move
// past: it is the difference between containing and promising to contain. `WasmBox.Open` does not
// hand out an instance if the canary did not pass.
internal static class Canary
{
	// THE EIGHT THAT MUST FAIL. The two that must WORK are not in this count: they are checked by
	// name, below. If `LosIntentos` ever grows one, this number has to grow with it -- a new attempt
	// nobody counts is a new attempt nobody looks at.
	private const int LeaksThatMustFail = 8;

	// AND THE TWO THAT MUST WORK, because a cage that contains everything contains nothing useful:
	//
	//   read its own records   otherwise the box is a brick
	//   read its own package   otherwise the module cannot even load
	//
	// The second one is new with the two-preopen cage, and it is the half people forget: a door that
	// is checked only for what it must NOT let through can be checked, pass, and be walled shut.
	private static readonly string[] MustWork = ["MI PROPIO journal", "MI PROPIO paquete"];

	// WHAT THE CANARY NEEDS IN ITS FOLDERS, and they are toys on purpose: they touch nobody's
	// records and nobody's package. A file it must be able to read in each, and a "neighbour" it
	// must not reach at all.
	private const string Mark = "canario.marca";

	internal static bool Passes(nint engine, nint component, nint linker, string privateFolder, Action<string> log)
		=> Passes(engine, component, linker, privateFolder, log, brokenCage: false);

	// `brokenCage` mounts too much on purpose. That is not a curiosity: it is the only way to check
	// that the canary SEES a broken cage. A canary that always sings "shut" -- because its probe
	// failed for some other reason, because the notebook was never read, because the count is wrong
	// -- is worse than no canary, and from the outside it looks exactly like one that works.
	internal static bool Passes(
		nint engine, nint component, nint linker, string privateFolder, Action<string> log, bool brokenCage)
	{
		string nest = Path.Combine(privateFolder, "canario");
		string own = Path.Combine(nest, "propia");
		string package = Path.Combine(nest, "paquete");
		string neighbour = Path.Combine(nest, "vecina");

		try
		{
			Directory.CreateDirectory(own);
			Directory.CreateDirectory(package);
			Directory.CreateDirectory(neighbour);
			File.WriteAllText(Path.Combine(own, Mark), "el canario pasó por aquí");
			File.WriteAllText(Path.Combine(package, Mark), "esto haría de paquete");
			File.WriteAllText(Path.Combine(neighbour, "marca.txt"), "esto es de otra institución");
		}
		catch (Exception e)
		{
			log($"caja: el canario no pudo armar su nido ({e.GetType().Name}); no se abre ningun modulo");
			return false;
		}

		// THE SAME CAGE. Not one line of its own: if it were built differently here, the canary would
		// stop saying something about modules and start saying something about itself.
		nint wasi;
		try { wasi = Cage.Build(own, package); }
		catch (Exception e) { log($"caja: el canario no pudo armar la jaula ({e.Message})"); return false; }

		// THE APP FOLDER MOUNTED AT THE ROOT is exactly the regression we are hunting: the module
		// keeps working the same, the screen looks the same, and suddenly it can reach everything.
		if (brokenCage)
		{
			Wasmtime.wasi_config_preopen_dir(wasi, privateFolder, "/", true);
			log("caja: canario con la jaula MAL ARMADA a proposito (la carpeta privada montada en /)");
		}

		var notebook = new StringBuilder();
		Wasmtime.GuestWrites listening = (_, bytes, count) =>
		{
			int n = (int)count;
			byte[] b = new byte[n];
			Marshal.Copy(bytes, b, 0, n);
			lock (notebook) notebook.Append(Encoding.UTF8.GetString(b));
			return n;
		};
		Wasmtime.wasi_config_set_stdout_custom(wasi, listening, 0, 0);

		// THE TARGETS. `neighbour` and the private folder are HOST-side names on purpose: they are
		// not mounted, so for the probe those names do not exist -- which is precisely what is
		// measured. The last two are GUEST-side: what it must be able to read in each of the two
		// mounts.
		nint[] argv = [
			Marshal.StringToHGlobalAnsi("canario"),
			Marshal.StringToHGlobalAnsi(Cage.Granted),
			Marshal.StringToHGlobalAnsi("--intentos"),
			Marshal.StringToHGlobalAnsi(neighbour),
			Marshal.StringToHGlobalAnsi(privateFolder),
			Marshal.StringToHGlobalAnsi(Cage.Granted + "/" + Mark),
			Marshal.StringToHGlobalAnsi(Cage.Package + "/" + Mark)];
		Wasmtime.wasi_config_set_argv(wasi, (nuint)argv.Length, argv);

		nint store = 0;
		try
		{
			store = Cage.Enclose(engine, wasi, out nint ctx);

			string? wrong = Wasmtime.Message(
				Wasmtime.wasmtime_component_linker_instantiate(linker, ctx, component, out Wasmtime.Instance inst));
			if (wrong is not null) { log($"caja: el canario no instancio: {wrong}"); return false; }

			byte[] func = Cage.EntryPoint(ref inst, ctx, "canario");

			// THE PROBE RUNS TO THE END AND RETURNS, unlike a module: it opens no actor, makes its
			// nine attempts and exits. So here `func_call` does return, and no thread is needed.
			nint res = Marshal.AllocHGlobal(256);
			for (int i = 0; i < 256; i++) Marshal.WriteByte(res, i, 0);
			Wasmtime.Message(Wasmtime.wasmtime_component_func_call(func, ctx, 0, 0, res, 1));
			Marshal.FreeHGlobal(res);
		}
		catch (Exception e) { log($"caja: el canario no pudo correr ({e.GetType().Name}: {e.Message})"); return false; }
		finally { if (store != 0) Wasmtime.wasmtime_store_delete(store); }

		return Verdict(notebook.ToString(), log);
	}

	// WHAT IT MANAGED TO DO IS COUNTED, NOT WHAT IT SAID. The probe writes one line per attempt with
	// the real outcome -- `PUDO` with what it got, `NO PUDO` with the exception -- and the verdict
	// comes from counting those lines. A probe that hangs or blows up does not leave eight `NO PUDO`:
	// it leaves fewer, and that is a "did not pass", not a "passed".
	private static bool Verdict(string notebook, Action<string> log)
	{
		var could = new List<string>();
		int couldNot = 0;

		foreach (string line in notebook.Split('\n'))
		{
			string t = line.Trim();
			if (t.StartsWith("PUDO", StringComparison.Ordinal)) could.Add(t);
			else if (t.StartsWith("NO PUDO", StringComparison.Ordinal)) couldNot++;
		}

		// THE ONES THAT MUST WORK, checked BY NAME and not by counting: a cage that shuts everything
		// is not a cage, it is a wall. Each one is looked up on its own so the log can say WHICH is
		// missing -- "it cannot read its records" and "it cannot read its package" are two different
		// broken things and they need two different fixes.
		string[] missing = [.. MustWork.Where(m => !could.Exists(p => p.Contains(m, StringComparison.Ordinal)))];

		log($"caja: canario -- {couldNot} de {LeaksThatMustFail} fugas cerradas; "
			+ $"{MustWork.Length - missing.Length} de {MustWork.Length} puertas abiertas");

		foreach (string p in could)
			if (!MustWork.Any(m => p.Contains(m, StringComparison.Ordinal)))
				log("caja: canario   FUGA ABIERTA -> " + p);
		foreach (string m in missing)
			log("caja: canario   PUERTA TAPIADA -> no pudo leer " + m);

		if (couldNot == LeaksThatMustFail && missing.Length == 0)
		{
			log("caja: canario -- LA JAULA ESTA CERRADA");
			return true;
		}

		log("caja: canario -- LA JAULA NO ESTA CERRADA; no se abre ningun modulo");
		return false;
	}
}
