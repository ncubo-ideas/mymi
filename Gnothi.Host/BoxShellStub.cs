namespace Gnothi.Host;

// THE WASM BOX DOES NOT EXIST ON THE DESKTOP, for the same reason the webview and hybrid ones do
// not: it is not needed there. The box solves two things -- that the module weighs what it really
// weighs, and that nobody has to trust it to behave -- and on the desktop the second one is solved
// differently, with the child process and the operating system's confinement. What is a store ban on
// a phone is banned by nobody here.
//
// The type exists only because Module.ChooseShell names "caja" in its switch. Throwing with a clear
// message beats not compiling: it keeps the Host's code IDENTICAL on both platforms.
//
// It is called BoxShellStub.cs so as not to clash with Gnothi.MobileShared's BoxShell.cs, which is
// the real one. Gnothi.Mobile EXCLUDES this file from its glob.
internal sealed class BoxShell : IShell
{
	private const string Why =
		"la cascara 'caja' solo existe en movil: es donde el modulo trae su cara en un WebView y su "
		+ "superficie corre adentro de una caja de wasm. En escritorio usa \"type\": \"in-process\".";

	public BoxShell(string name, Shell shell, string grantedFolder) { }

	public bool IsRunning => false;

	public Task<object> Invoke(string tool, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct)
		=> throw new PlatformNotSupportedException(Why);

	public Task<IReadOnlyList<string>> ActualTools(CancellationToken ct)
		=> throw new PlatformNotSupportedException(Why);

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
