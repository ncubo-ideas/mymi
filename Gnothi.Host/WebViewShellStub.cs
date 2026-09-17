namespace Gnothi.Host;

// THE WEBVIEW SHELL DOES NOT EXIST ON THE DESKTOP, and this is not a shortcut: it is that it is not
// needed there. On the desktop the module is loaded in process or as a child process, which is what
// the stores forbid on a phone and what nobody forbids there.
//
// The type exists only because Module.ChooseShell names "webview" in its switch. Throwing with a
// clear message beats not compiling: it keeps the Host's code IDENTICAL on both platforms, and
// leaves the impossibility stated exactly where somebody would go looking for it.
//
// The file is called WebViewShellStub.cs so as not to clash with Gnothi.Mobile's WebViewShell.cs,
// which is the real one and compiles the same class in its place. Gnothi.Mobile EXCLUDES this file
// from its glob; if both ever came in, two definitions would collide and that is the right signal.
internal sealed class WebViewShell : IShell
{
	private const string PorQue =
		"la cascara 'webview' solo existe en movil: es donde el modulo es la PWA de la "
		+ "institucion dentro de un WebView. En escritorio usa \"type\": \"in-process\".";

	public WebViewShell(string name, Shell shell, string grantedFolder) { }

	public bool IsRunning => false;

	public Task<object> Invoke(string tool, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct)
		=> throw new PlatformNotSupportedException(PorQue);

	public Task<IReadOnlyList<string>> ActualTools(CancellationToken ct)
		=> throw new PlatformNotSupportedException(PorQue);

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
