namespace Gnothi.Desktop;

// THE FACES OF HYBRID MODULES, as a shape the components can use without knowing which web
// engine paints them. By default it is FrameWebModuleHost, a frame inside Gnothi's own window,
// on the three systems; on Windows, `--cara ventana` picks WebView2WebModuleHost instead, one
// window per module. NoFaces is the value HostState starts with, so a state built without a
// host says so instead of failing with a null.
internal interface IFaces
{
	Task<IFace> OpenFace(Gnothi.Host.Module m, CancellationToken ct);

	// Close a module's face if it is open -- what the enrollment does before replacing it.
	void Close(string module);

	// AN ICON ON THE PERSON'S DESKTOP that opens Gnothi straight into this module's face -- the
	// desktop's counterpart of the phone's pinned shortcut (ShortcutManager.requestPinShortcut).
	// Returns the path of what was created. It does not turn the module into an app: the process,
	// the memory and the folder stay Gnothi's, and if Gnothi goes the icon stops working.
	string PinToDesktop(Gnothi.Host.Module m, string modulesRoot, string journalsRoot);
}

internal interface IFace
{
	string Title { set; }

	// What a test needs to see: an expression evaluated in the face, and a PNG of it.
	Task<string> Eval(string js);
	Task Screenshot(string path);
	int Violations { get; }
}

internal sealed class NoFaces : IFaces
{
	public Task<IFace> OpenFace(Gnothi.Host.Module m, CancellationToken ct) =>
		throw new InvalidOperationException(
			"esta ventana arrancó sin un anfitrión de caras: no hay dónde abrir la pantalla de un módulo híbrido");

	public void Close(string module) { }

	public string PinToDesktop(Gnothi.Host.Module m, string modulesRoot, string journalsRoot) =>
		throw new InvalidOperationException("esta ventana arrancó sin un anfitrión de caras: no hay qué fijar en el escritorio");
}
