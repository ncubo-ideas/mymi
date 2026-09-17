using Android.App;
using Gnothi.Host;

namespace Gnothi.Mobile;

// WHAT ANDROID PROVIDES SO THE WASM BOX CAN EXIST. See B12 and F8.
//
// Three things, and none of them is an architecture decision: where the .wasm lives, where the
// precompiled artifact lives, and where the log goes. The box itself -- the engine, the border, the
// containment -- lives in Gnothi.MobileShared, shared, because that is the part that does not depend
// on the platform.
//
// THE SWITCH BELONGS TO THIS STAGE, not to the product. It lets an ALREADY INSTALLED hybrid module
// run inside the box without rewriting the person's installation.json, which is a file of theirs. It
// is turned on from outside:
//
//     adb shell am start -n com.ncubo.gnothi.host/... --ez caja true
//
// The day the box is a form in its own right, the shell comes out of the Host's file like the other
// three, the bundle carries its own .wasm, and this file is left with nothing but the paths.
public partial class MainActivity : Activity
{
	// THE MODULE THAT RUNS IN THE BOX IN THIS CUT. One only, and by name, on purpose: what this came
	// to measure is how the WebView sees the box, not how many modules fit.
	private const string TheOneInTheBox = "examenes";

	// THE SUBSTRATE, and the name is the point: it is no longer «examenes in a box». It is the .NET
	// runtime, the BCL, the engine and the border -- ONE for every module -- and each module's DLLs
	// come in from its own bundle through the second mount. See F8 and C10.
	private const string TheWasm = "sustrato.wasm";

	// THE ASSET'S FINGERPRINT, AND NOT ITS LENGTH. The previous version compared the LENGTH, and the
	// length LIED: the build that fixed the border's field names came out at 6 583 761 bytes, exactly
	// the same as the previous one -- metadata changed, size did not -- so the asset was not copied,
	// the old file's fingerprint did not change, the old precompiled artifact kept being used, and
	// the screen came out identical without a single error. It is the SAME defect WasmBox had already
	// fixed by naming its artifact after its source's fingerprint, committed again one link further
	// up the same chain: a length does not identify a content, and the cheap comparison is the one
	// that costs you the afternoon.
	//
	// AND IT COSTS NO MORE: an Android asset does not report its length without reading it whole
	// either, so the version that lied was already paying for the 6 MB read. All that is added is the
	// SHA over what was being read anyway.
	private static string Fingerprint(System.IO.Stream s)
	{
		using var sha = System.Security.Cryptography.SHA256.Create();
		return Convert.ToHexString(sha.ComputeHash(s));
	}

	private void PrepareTheBox()
	{
		bool on = Intent?.GetBooleanExtra("caja", false) ?? false;
		if (!on) return;

		try
		{
			string privateFolder = FilesDir!.AbsolutePath;
			string wasm = System.IO.Path.Combine(privateFolder, TheWasm);

			// THE .wasm IS COPIED OUT OF THE APK INTO THE PRIVATE FOLDER because wasmtime needs a real
			// filesystem path: an asset lives inside the apk and has none.
			//
			// AND IT IS COPIED AGAIN IF THE APK'S IS A DIFFERENT ONE. The first version only copied if
			// the file was missing, so a new version of the module did NOT replace the old one. The
			// second compared the length, and the length lied. The asset's fingerprint is written
			// BESIDE the copy: if it is missing or does not match, it is copied.
			string mark = wasm + ".sha256";
			string fromApk;
			using (System.IO.Stream measuring = Assets!.Open(TheWasm)) fromApk = Fingerprint(measuring);

			bool sameOne = System.IO.File.Exists(wasm)
				&& System.IO.File.Exists(mark)
				&& string.Equals(System.IO.File.ReadAllText(mark).Trim(), fromApk, StringComparison.Ordinal);

			if (!sameOne)
			{
				using (System.IO.Stream incoming = Assets!.Open(TheWasm))
				using (var outgoing = System.IO.File.Create(wasm))
					incoming.CopyTo(outgoing);

				System.IO.File.WriteAllText(mark, fromApk);
				Log($"caja: {TheWasm} copiado, huella {fromApk[..16]}");
			}

			// AND ANY OTHER .wasm IN THE PRIVATE FOLDER GOES. There is exactly one substrate and this
			// class is the only thing that ever puts a .wasm here, so anything else is a leftover --
			// today `examenes-en-caja.wasm`, 6,5 MB of the person's storage holding a file nothing
			// names any more. Renaming the asset is what made it visible; the same would happen on
			// any future rename, so this sweeps by rule rather than by that one name.
			foreach (string leftover in System.IO.Directory.EnumerateFiles(privateFolder, "*.wasm"))
			{
				if (string.Equals(System.IO.Path.GetFileName(leftover), TheWasm, StringComparison.Ordinal)) continue;
				try
				{
					System.IO.File.Delete(leftover);
					try { System.IO.File.Delete(leftover + ".sha256"); } catch { /* no tenia */ }
					Log($"caja: sustrato viejo borrado ({System.IO.Path.GetFileName(leftover)})");
				}
				catch { /* lo tiene alguien; se reintenta al proximo arranque */ }
			}

			WasmBox.Log = Log;
			BoxShell.SubstrateWasm = wasm;
			BoxShell.PrivateFolder = privateFolder;
			BoxShell.Preferred = true;

			// THE SEAM, and it is the only thing this class decides. A hybrid module -- the
			// institution's package, without touching a byte -- runs inside the box. The others carry
			// on as they are, which is what makes the measurement comparable.
			Module.ShellFactory = (name, installation) =>
				name.Equals(TheOneInTheBox, StringComparison.OrdinalIgnoreCase)
				&& installation.Shell.Type.Equals("hybrid", StringComparison.OrdinalIgnoreCase)
					? new BoxShell(name, installation.Shell, installation.GrantedFolder)
					: null;

			// THE CANARY'S OWN TEST, and it exists because a canary that always sings "shut" looks
			// EXACTLY LIKE one that works. It is switched on separately:
			//
			//     adb shell am start -n ... --ez caja true --ez jaulamala true
			//
			// It mounts the private folder at the root -- the real regression, the one that leaves the
			// module working the same while opening everything to it -- and the canary is expected to
			// SEE it and refuse.
			if (Intent?.GetBooleanExtra("jaulamala", false) ?? false)
			{
				WasmBox.Prepare(wasm, privateFolder);
				bool passed = WasmBox.PassesWithBrokenCage(privateFolder);
				Log($"caja: prueba del canario con la jaula mala -- canto {(passed ? "CERRADA (MAL: no la vio)" : "abierta (bien: la vio)")}");
			}

			Log($"caja: encendida para '{TheOneInTheBox}'");
		}
		catch (Exception ex)
		{
			// If the box cannot be prepared, Gnothi carries on as always: modules run in their usual
			// form and nobody loses their records over an experiment.
			Log($"caja: no se pudo preparar ({ex.GetType().Name}: {ex.Message}); todo sigue igual");
			BoxShell.Preferred = false;
			Module.ShellFactory = null;
		}
	}
}
