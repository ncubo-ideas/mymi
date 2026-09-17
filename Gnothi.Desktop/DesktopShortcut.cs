namespace Gnothi.Desktop;

// THE ICON ON THE DESKTOP: a .lnk with the institution's logo that launches THIS executable with
// `--modulo <nombre>`, which opens the module's face as soon as the window is up. The logo comes
// from the bundle -brand/logo.png, the same file the card shows- wrapped as an .ico next to it;
// Windows accepts a PNG inside an .ico since Vista, so no re-encoding and no quality loss.
//
// WHAT THE ICON SAYS, and it is the decision Jonathan left open for the phone: the label is the
// institution's name, so on the desktop nothing distinguishes it from a native app of that
// institution. The shortcut's description and its Properties do say it is Gnothi; the label does
// not. Windows only for now (a .lnk through WScript.Shell); macOS and Linux would be an alias
// and a .desktop file.
internal static class DesktopShortcut
{
	internal static string Pin(Gnothi.Host.Module m, string modulesRoot, string journalsRoot, Action<string> log)
	{
		if (!OperatingSystem.IsWindows())
			throw new PlatformNotSupportedException("el icono en el escritorio todavía solo se crea en Windows");

		string exe = Environment.ProcessPath ?? throw new InvalidOperationException("no sé qué ejecutable soy");
		string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
		string label = Sanitize(m.Manifest.Identity.Length > 0 ? m.Manifest.Identity : m.Name);
		string link = Path.Combine(desktop, label + ".lnk");
		string? icon = IconFor(m, log);

		Type shellType = Type.GetTypeFromProgID("WScript.Shell")
			?? throw new PlatformNotSupportedException("WScript.Shell no está disponible");
		dynamic shell = Activator.CreateInstance(shellType)!;
		dynamic shortcut = shell.CreateShortcut(link);
		shortcut.TargetPath = exe;
		shortcut.Arguments = $"\"{modulesRoot}\" --journals \"{journalsRoot}\" --modulo {m.Name}";
		shortcut.WorkingDirectory = Path.GetDirectoryName(exe);
		shortcut.Description = $"La pantalla de {m.Manifest.Identity}, dentro de Gnothi";
		if (icon is not null) shortcut.IconLocation = icon + ",0";
		shortcut.Save();

		log($"icono: '{m.Name}' fijado en el escritorio como {link}" + (icon is null ? " (con el icono de Gnothi: el módulo no trae logo png)" : ""));
		return link;
	}

	// brand/logo.png -> brand/icono.ico, the PNG embedded as the one entry. Only png: an .ico
	// can carry PNG bytes as they are; anything else would need re-encoding and is left to the
	// executable's own icon.
	private static string? IconFor(Gnothi.Host.Module m, Action<string> log)
	{
		string logo = m.Manifest.Brand?.Logo ?? "";
		if (logo.Length == 0 || !logo.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return null;
		string path = Path.Combine(m.Directory, logo.Replace('/', Path.DirectorySeparatorChar));
		if (!File.Exists(path)) return null;

		byte[] png = File.ReadAllBytes(path);
		if (png.Length < 24 || png[0] != 0x89 || png[1] != (byte)'P') return null;
		int width = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
		int height = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
		if (width > 256 || height > 256) { log($"icono: el logo de '{m.Name}' mide {width}x{height} y un icono admite hasta 256: se usa el de Gnothi"); return null; }

		string ico = Path.Combine(Path.GetDirectoryName(path)!, "icono.ico");
		using var f = File.Create(ico);
		using var w = new BinaryWriter(f);
		w.Write((ushort)0); w.Write((ushort)1); w.Write((ushort)1);            // ICONDIR: reserved, type, count
		w.Write((byte)(width == 256 ? 0 : width)); w.Write((byte)(height == 256 ? 0 : height));
		w.Write((byte)0); w.Write((byte)0);                                     // colors, reserved
		w.Write((ushort)1); w.Write((ushort)32);                                // planes, bit count
		w.Write(png.Length); w.Write(6 + 16);                                   // size, offset
		w.Write(png);
		return ico;
	}

	private static string Sanitize(string name)
	{
		foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, ' ');
		return name.Trim();
	}
}
