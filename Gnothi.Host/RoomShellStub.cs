namespace Gnothi.Host;

// THE SALA DOES NOT EXIST ON THE DESKTOP, for the same reason the WebView shell does not: there
// the module is loaded in-process, which is what does not pass Google's text on a phone -- the
// foreign dll would have DIRECT access to the system APIs (see B11) -- and on a PC nobody cares.
//
// The type exists only because Module.ChooseShell names "sala" in its switch. Throwing with a
// clear message is better than not compiling: it keeps the Host's code IDENTICAL on both
// platforms.
//
// It is called RoomShellStub.cs so as not to clash with Gnothi.MobileShared's RoomShell.cs,
// which is the real one. Gnothi.Mobile EXCLUDES this file from its glob.
internal sealed class RoomShell : IShell
{
	private const string PorQue =
		"la cascara 'sala' solo existe en movil: es donde el modulo corre adentro del motor web, "
		+ "en el sustrato que Gnothi presta. En escritorio usa \"type\": \"in-process\".";

	public RoomShell(string name, Shell shell, string grantedFolder) { }

	public bool IsRunning => false;

	public Task<object> Invoke(string tool, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct)
		=> throw new PlatformNotSupportedException(PorQue);

	public Task<IReadOnlyList<string>> ActualTools(CancellationToken ct)
		=> throw new PlatformNotSupportedException(PorQue);

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
