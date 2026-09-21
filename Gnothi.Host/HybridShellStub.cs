namespace Gnothi.Host;

// THE HYBRID SHELL DOES NOT EXIST ON THE DESKTOP, for the same reason the webview one does not: it
// is not needed there. The half the hybrid adds is the FACE in a WebView, and on the desktop the
// module is loaded in process and the Host renders. What is a store ban on a phone is banned by
// nobody here.
//
// The type exists only because Module.ChooseShell names "hybrid" in its switch. Throwing with a
// clear message beats not compiling: it keeps the Host's code IDENTICAL on both platforms, and
// leaves the impossibility stated where somebody would go looking for it.
//
// El archivo se llama HybridShellStub.cs para no chocar con el HybridShell.cs de Gnothi.MobileShared,
// que es el de verdad. Gnothi.Mobile EXCLUYE este archivo de su glob; si algun dia entraran
// los dos, chocarian dos definiciones y esa es la senal correcta.
//
// LA VENTANA DE ESCRITORIO ES LA EXCEPCION: Gnothi.Desktop compila el HybridShell real contra
// este ensamblado y lo registra por Module.ShellFactory antes de cargar el catalogo, con la cara
// como marco dentro de su ventana. Este stub queda para el Host sin ventana -el frente MCP-.
internal sealed class HybridShell : IShell
{
	private const string PorQue =
		"la cascara 'hybrid' solo existe en movil: es donde el modulo trae su cara en un WebView "
		+ "y su superficie en un dll al lado. En escritorio usa \"type\": \"in-process\".";

	public HybridShell(string name, Shell shell, string grantedFolder) { }

	public bool IsRunning => false;

	public Task<object> Invoke(string tool, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct)
		=> throw new PlatformNotSupportedException(PorQue);

	public Task<IReadOnlyList<string>> ActualTools(CancellationToken ct)
		=> throw new PlatformNotSupportedException(PorQue);

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
