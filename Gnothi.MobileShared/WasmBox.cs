using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Gnothi.Host;

// THE HOST'S WASM ENGINE, one for every module. See B12.
//
// What Gnothi lends is this: wasmtime, the .NET runtime compiled to wasm, the Puppeteer engine and
// the BCL. The module brings 227 KB. That is why this class is STATIC: one per module would make
// each one pay the 163 MB of compiling the component all over again, which is exactly what the
// hosted form does and what this architecture exists in order not to do.
//
// THE COMPONENT IS COMPILED ONCE AND KEPT. Compiling costs 516 ms and ~163 MB; opening the
// precompiled artifact costs 4 ms and ~0. The artifact is produced by THIS device and lives in
// Gnothi's private folder: it depends on the wasmtime version, on the processor and on the engine
// options, so it travels in no bundle and is accepted from nobody.
internal static class WasmBox
{
	private static readonly object padlock = new();
	private static nint engine;
	private static nint component;
	private static nint linker;
	private static string? fromWhichWasm;

	internal static Action<string> Log { get; set; } = _ => { };

	// START THE SUBSTRATE, or hand back the one already running. The `wasm` is the same for every
	// module -- it is the runtime, not the module -- and the module comes in through the DLLs it
	// carries inside.
	internal static void Prepare(string wasm, string privateFolder)
	{
		lock (padlock)
		{
			if (component != 0 && fromWhichWasm == wasm) return;
			if (component != 0) Release();

			// NEW ENGINE, NEW CAGE: an old verdict does not hold for a substrate that is not the same.
			sang = null;

			engine = Wasmtime.wasm_engine_new();

			// THE ARTIFACT IS NAMED AFTER ITS SOURCE'S FINGERPRINT, and that is not tidiness: without
			// it, a new .wasm with an old, healthy precompiled artifact beside it makes Gnothi keep
			// running THE PREVIOUS CODE, without a single error. It cost a whole run: the border was
			// changed, the app reinstalled, and the screen came out identical because the old one was
			// running inside.
			//
			// They are two different checks and both are needed:
			//   - the ARTIFACT's fingerprint answers «it is not corrupt»  -> the .sha256 beside it
			//   - the SOURCE's fingerprint answers «it belongs to THIS wasm» -> the file name
			string precompiled = Path.Combine(
				privateFolder, "sustrato-" + Fingerprint(wasm)[..16] + ".cwasm");
			if (!OpenPrecompiled(precompiled)) Compile(wasm, precompiled);

			// AND THE ARTIFACTS OF THE PREVIOUS SUBSTRATES GO. Naming them after their source's
			// fingerprint is what makes a stale one impossible to run -- but it also means every
			// substrate that ever shipped leaves 9 MB behind forever. Nobody notices on the first
			// update; on the fifth it is 45 MB of the person's storage spent on artifacts for wasm
			// files that are no longer on the device.
			//
			// It sweeps AFTER opening the current one, so a sweep that fails costs nothing: the box
			// is already up.
			SweepOldArtifacts(privateFolder, Path.GetFileName(precompiled));

			linker = Wasmtime.wasmtime_component_linker_new(engine);
			Fail(Wasmtime.wasmtime_component_linker_add_wasip2(linker), "add_wasip2");
			Fail(Wasmtime.wasmtime_component_linker_add_wasi_http(linker), "add_wasi_http");

			fromWhichWasm = wasm;
		}
	}

	// THE PRECOMPILED ARTIFACT, WITH ITS FINGERPRINT. wasmtime validates the ELF header and the
	// engine version -- both with a clean error, measured -- but NOT the contents: four kilobytes
	// changed in the middle went through without a warning and the module ran. Its own header says
	// so: «not safe to receive arbitrary user input».
	//
	// So the artifact is TRUSTED INPUT and the one who answers for it is Gnothi: it produces it, it
	// lives in its private folder, and it is checked BEFORE being opened. Costs 22 ms.
	private static bool OpenPrecompiled(string path)
	{
		if (!File.Exists(path)) return false;

		string fingerprint = path + ".sha256";
		if (!File.Exists(fingerprint))
		{
			Log("caja: el precompilado no trae huella; se descarta");
			Erase(path, fingerprint);
			return false;
		}

		if (!string.Equals(File.ReadAllText(fingerprint).Trim(), Fingerprint(path), StringComparison.Ordinal))
		{
			Log("caja: la huella del precompilado NO coincide; se descarta antes de abrirlo");
			Erase(path, fingerprint);
			return false;
		}

		string? wrong = Wasmtime.Message(
			Wasmtime.wasmtime_component_deserialize_file(engine, path, out component));
		if (wrong is null) { Log("caja: sustrato abierto del precompilado"); return true; }

		// Different version, truncated file, broken header: all three give a clean error -- measured
		// -- and the cost of not matching is one slow start, once.
		Log("caja: el precompilado no sirvio (" + wrong.Replace("\n", " | ") + "); se recompila");
		Erase(path, fingerprint);
		component = 0;
		return false;
	}

	private static void Compile(string wasm, string precompiled)
	{
		byte[] bytes = File.ReadAllBytes(wasm);
		Fail(Wasmtime.wasmtime_component_new(engine, bytes, (nuint)bytes.Length, out component),
			"compilar el sustrato");

		Fail(Wasmtime.wasmtime_component_serialize(component, out Wasmtime.ByteVec blob), "serializar");
		byte[] toKeep = new byte[(int)blob.size];
		Marshal.Copy(blob.data, toKeep, 0, toKeep.Length);
		Wasmtime.wasm_byte_vec_delete(ref blob);

		File.WriteAllBytes(precompiled, toKeep);
		File.WriteAllText(precompiled + ".sha256", Fingerprint(precompiled));
		Log($"caja: sustrato compilado y guardado ({toKeep.Length / 1024 / 1024} MB)");
	}

	// ONE BOX FOR ONE MODULE. All it sees of the disk is its granted folder; the package is not
	// mounted because it travels inside the .wasm. Measured: of nine escape attempts, eight fail.
	//
	// AND BEFORE THE FIRST ONE, THE CANARY SINGS. No instance is handed out over a cage that was not
	// checked: see Canary. It is done ONCE per substrate -- the cage depends on the engine and on how
	// `Cage` builds it, not on the module -- and with the padlock held, so no module slips through
	// while the check runs.
	internal static BoxedInstance Open(
		string module, string grantedFolder, string privateFolder, string packageFolder, string entry)
	{
		lock (padlock)
		{
			if (component == 0) throw new InvalidOperationException("la caja no esta preparada");

			sang ??= Canary.Passes(engine, component, linker, privateFolder, Log);
			if (sang != true)
				throw new InvalidOperationException(
					$"caja: el canario no paso, asi que '{module}' no se abre en caja");

			return new BoxedInstance(
				engine, component, linker, module, grantedFolder, privateFolder, packageFolder, entry, Log);
		}
	}

	// THE CANARY'S VERDICT, and it is nullable on purpose: `null` is «it has not sung yet», which is
	// not the same as «it sang wrong». It is cleared along with the substrate -- a new engine is a
	// new cage.
	private static bool? sang;

	// THE CANARY, ASKED BEFORE ANYBODY NEEDS AN ANSWER — and it changes WHEN a broken cage is found,
	// not whether.
	//
	// `Open` already refuses to hand out an instance over a cage that did not pass, so the property
	// was never at risk. What was bad is the MOMENT: the canary ran on the first `Open`, which is a
	// person opening a module. A cage that does not pass turned into a module that fails in her
	// hands, on a path where nothing else was wrong.
	//
	// Calling this at start-up moves that to a line in the log and a box that simply is not used.
	// Whoever assembles the platform can then decide not to register its factory at all, and every
	// module runs the way it ran before — which is the honest outcome when the containment cannot be
	// demonstrated.
	//
	// It shares `sang` with `Open` on purpose: it is the SAME verdict, cached once per substrate, not
	// a second opinion. Two canaries could disagree, and then nobody would know which one to believe.
	internal static bool Sings(string privateFolder)
	{
		lock (padlock)
		{
			if (component == 0) throw new InvalidOperationException("la caja no esta preparada");
			return sang ??= Canary.Passes(engine, component, linker, privateFolder, Log);
		}
	}

	// TO CHECK THAT THE CANARY SEES A BROKEN CAGE. Without this the canary is a boolean that always
	// says true and nobody can tell it apart from one that works. It is switched on by whoever
	// assembles the platform; on Android, by the same extra that switches the box on.
	internal static bool PassesWithBrokenCage(string privateFolder)
	{
		lock (padlock)
		{
			if (component == 0) throw new InvalidOperationException("la caja no esta preparada");
			return Canary.Passes(engine, component, linker, privateFolder, Log, brokenCage: true);
		}
	}

	private static void Release()
	{
		if (linker != 0) Wasmtime.wasmtime_component_linker_delete(linker);
		if (component != 0) Wasmtime.wasmtime_component_delete(component);
		if (engine != 0) Wasmtime.wasm_engine_delete(engine);
		linker = component = engine = 0;
	}

	// EVERY `sustrato*.cwasm` THAT IS NOT THE CURRENT ONE, with its fingerprint file. Matching by
	// prefix and not by a remembered list on purpose: the artifacts of a Gnothi that was uninstalled
	// and reinstalled, or of a substrate that never got recorded anywhere, are swept just the same.
	//
	// AND THE PATTERN HAS NO DASH, which it used to. `sustrato-*.cwasm` only swept artifacts of the
	// CURRENT naming scheme, so the two files from the scheme BEFORE it -- `sustrato.cwasm` and its
	// .sha256, from when the artifact was not named after its source -- were never going to be
	// deleted by anybody, ever. They were found on the phone on 2026-09-17, not by looking for them
	// but in the root listing the canary prints when it catches a broken cage: a leak test that
	// doubled as an audit of the private folder. The lesson is the pattern's, not that one name's --
	// a sweep written against the scheme in force when it was written stops sweeping the day the
	// scheme changes, which is exactly the day it is needed.
	//
	// It never throws: this is housekeeping. A file that cannot be deleted -- because something else
	// still holds it -- is left alone and tried again next start-up.
	private static void SweepOldArtifacts(string privateFolder, string keep)
	{
		try
		{
			foreach (string old in Directory.EnumerateFiles(privateFolder, "sustrato*.cwasm"))
			{
				if (string.Equals(Path.GetFileName(old), keep, StringComparison.Ordinal)) continue;
				Erase(old, old + ".sha256");
				Log($"caja: artefacto viejo borrado ({Path.GetFileName(old)})");
			}
		}
		catch (Exception e) { Log($"caja: no se pudo barrer artefactos viejos ({e.GetType().Name})"); }
	}

	private static void Erase(params string[] paths)
	{
		foreach (string p in paths) { try { File.Delete(p); } catch { /* already gone */ } }
	}

	internal static string Fingerprint(string path)
	{
		using var sha = SHA256.Create();
		using FileStream f = File.OpenRead(path);
		return Convert.ToHexString(sha.ComputeHash(f));
	}

	private static void Fail(nint error, string what)
	{
		string? m = Wasmtime.Message(error);
		if (m is not null) throw new InvalidOperationException($"caja: fallo al {what}: {m}");
	}
}
