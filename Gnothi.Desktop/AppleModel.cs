using System.Runtime.InteropServices;
using Gnothi.Host;
using Gnothi.iOS;

namespace Gnothi.Desktop;

// APPLE INTELLIGENCE EN EL ESCRITORIO, detras de la misma puerta que en el telefono.
//
// ModelFoundation.cs es EL MISMO ARCHIVO que compila Gnothi.iOS -enlazado, no copiado- y habla
// con el Swift por DllImport("__Internal"). En iOS ese nombre existe porque la libreria entra
// estatica al ejecutable. En CoreCLR no significa nada, asi que aca se resuelve a mano: el
// resolver de abajo mapea "__Internal" a la dylib que armar-modelo.sh deja al lado del
// ejecutable. Un solo ModelFoundation, dos formas de enlazarlo.
//
// FALLA CERRADO Y EN SILENCIO. Sin dylib (Windows, Linux, una Mac sin Xcode), sin macOS 26, o
// con un modelo que dice estar y no genera, el preset no aparece. Nada de esto tira la ventana.
internal static class AppleModel
{
	private const string Dylib = "libGnothiModelo.dylib";

	private static bool wired;
	private static bool? works;

	public static string Path => System.IO.Path.Combine(AppContext.BaseDirectory, Dylib);

	// Se llama UNA vez desde Program: registra el resolver en el ensamblado que declara los
	// DllImport (este, porque ModelFoundation.cs se compila aca).
	public static void Wire()
	{
		if (wired) return;
		wired = true;
		if (!OperatingSystem.IsMacOS()) return;
		NativeLibrary.SetDllImportResolver(typeof(AppleModel).Assembly, (name, _, _) =>
			name == "__Internal" && File.Exists(Path) ? NativeLibrary.Load(Path) : IntPtr.Zero);
	}

	// Caro la primera vez -genera de verdad para distinguir "dice estar" de "contesta"- y por
	// eso se cachea. En una Mac con el modelo medido: ~1 s.
	public static bool Works()
	{
		if (works is bool w) return w;
		if (!OperatingSystem.IsMacOS() || !File.Exists(Path)) return (works = false).Value;
		try { works = ModelFoundation.Works(); }
		catch (Exception e)
		{
			// DllNotFound, EntryPointNotFound, una dylib de otra arquitectura: todos significan
			// "no hay", y se dice por stderr para quien este depurando.
			Console.Error.WriteLine($"[gnothi.desktop] apple intelligence no disponible: {e.GetType().Name}: {e.Message}");
			works = false;
		}
		return works.Value;
	}

	/// <summary>Por que no hay modelo de Apple aca, dicho en el idioma de la persona.</summary>
	///
	/// EL ESTADO ES UN CONJUNTO CERRADO y la frase la dice el catalogo: ModelFoundation.Diagnosis()
	/// redacta para un telefono, y su texto no se puede adaptar a una Mac reemplazando palabras.
	public static string Diagnosis()
	{
		if (!OperatingSystem.IsMacOS()) return Textos.T("ajustes.apple.solo-mac");
		if (!File.Exists(Path)) return Textos.T("ajustes.apple.sin-puente");
		try
		{
			return ModelFoundation.State() switch
			{
				2 => Textos.T("ajustes.apple.contesta"),
				1 => Textos.T("ajustes.apple.no-genera"),
				_ => Textos.T("ajustes.apple.sin-modelo"),
			};
		}
		catch (Exception e)
		{
			return Textos.T("ajustes.apple.fallo", ("detalle", $"{e.GetType().Name}: {e.Message}"));
		}
	}

	public static Gnothi.Assistant.IModel Create(string instructions) => new ModelFoundation(instructions);
}
