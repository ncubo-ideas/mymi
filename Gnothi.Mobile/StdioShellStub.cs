namespace Gnothi.Host;

// THE STDIO SHELL DOES NOT EXIST ON ANDROID, and this is not a shortcut for the spike: it is
// the operating system's restriction. An app with targetSdk >= 29 cannot exec() files from
// its data directory -- that is a W^X violation -- and there is no dotnet CLI on the device.
//
// The type exists only because Module.ChooseShell names it in its default branch. Throwing
// with a clear message beats not compiling: it keeps the Host's code IDENTICAL to desktop,
// and the impossibility is stated where somebody would try to use it.
//
// The FILE is called StdioShellStub.cs so it does not collide with the real StdioShell.cs,
// which is deliberately NOT compiled in: it would drag the MCP SDK into the phone.
internal sealed class StdioShell : IShell
{
	private const string Why =
		"the 'stdio' shell does not exist on Android: exec() from the data directory is blocked " +
		"by W^X (targetSdk >= 29) and there is no dotnet CLI. Use \"type\": \"in-process\".";

	public StdioShell(string name, Shell shell, string grantedFolder) { }

	public bool IsRunning => false;

	public Task<object> Invoke(string tool, IReadOnlyDictionary<string, object?>? arguments, CancellationToken ct)
		=> throw new PlatformNotSupportedException(Why);

	public Task<IReadOnlyList<string>> ActualTools(CancellationToken ct)
		=> throw new PlatformNotSupportedException(Why);

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
