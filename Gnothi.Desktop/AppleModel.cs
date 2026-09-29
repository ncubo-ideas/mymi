using System.Runtime.InteropServices;
using Gnothi.Host;
using Gnothi.iOS;

namespace Gnothi.Desktop;

// APPLE INTELLIGENCE ON THE DESKTOP, behind the same door as on the phone.
//
// ModelFoundation.cs is THE SAME FILE Gnothi.iOS compiles -linked, not copied- and it talks to
// the Swift through DllImport("__Internal"). On iOS that name exists because the library goes
// statically into the executable. In CoreCLR it means nothing, so here it is resolved by hand:
// the resolver below maps "__Internal" to the dylib armar-modelo.sh leaves beside the
// executable. One ModelFoundation, two ways of linking it.
//
// IT FAILS CLOSED AND IN SILENCE. With no dylib (Windows, Linux, a Mac without Xcode), without
// macOS 26, or with a model that claims to be there and does not generate, the preset does not appear. None of this brings the window down.
internal static class AppleModel
{
	private const string Dylib = "libGnothiModelo.dylib";

	private static bool wired;
	private static bool? works;

	public static string Path => System.IO.Path.Combine(AppContext.BaseDirectory, Dylib);

	// Called ONCE from Program: it registers the resolver on the assembly that declares the
	// DllImports (this one, because ModelFoundation.cs is compiled here).
	public static void Wire()
	{
		if (wired) return;
		wired = true;
		if (!OperatingSystem.IsMacOS()) return;
		NativeLibrary.SetDllImportResolver(typeof(AppleModel).Assembly, (name, _, _) =>
			name == "__Internal" && File.Exists(Path) ? NativeLibrary.Load(Path) : IntPtr.Zero);
	}

	// Expensive the first time -it really generates, to tell "claims to be there" from "answers"-
	// and that is why it is cached. On a Mac with the model measured: ~1 s.
	public static bool Works()
	{
		if (works is bool w) return w;
		if (!OperatingSystem.IsMacOS() || !File.Exists(Path)) return (works = false).Value;
		try { works = ModelFoundation.Works(); }
		catch (Exception e)
		{
			// DllNotFound, EntryPointNotFound, a dylib of another architecture: they all mean
			// "there is none", and it is said on stderr for whoever is debugging.
			Console.Error.WriteLine($"[gnothi.desktop] apple intelligence no disponible: {e.GetType().Name}: {e.Message}");
			works = false;
		}
		return works.Value;
	}

	/// <summary>Why there is no Apple model here, said in the person language.</summary>
	///
	/// THE STATE IS A CLOSED SET and the sentence is said by the catalogue: ModelFoundation.Diagnosis()
	/// writes for a phone, and its text cannot be adapted to a Mac by replacing words.
	public static string Diagnosis()
	{
		if (!OperatingSystem.IsMacOS()) return Texts.T("ajustes.apple.solo-mac");
		if (!File.Exists(Path)) return Texts.T("ajustes.apple.sin-puente");
		try
		{
			return ModelFoundation.State() switch
			{
				2 => Texts.T("ajustes.apple.contesta"),
				1 => Texts.T("ajustes.apple.no-genera"),
				_ => Texts.T("ajustes.apple.sin-modelo"),
			};
		}
		catch (Exception e)
		{
			return Texts.T("ajustes.apple.fallo", ("detalle", $"{e.GetType().Name}: {e.Message}"));
		}
	}

	public static Gnothi.Assistant.IModel Create(string instructions) => new ModelFoundation(instructions);
}
