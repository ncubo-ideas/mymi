namespace Gnothi.Host;

// THE HYBRID SHELL: the institution's face in the WebView, its surface loaded IN PROCESS, and the
// engine lent by Gnothi.
//
// WHY IT EXISTS. The other two are half answers for the phone:
//
//   cargo   lends the engine but brings no screen -- the Host renders view.json
//   hosted  brings the screen but loads its own engine: measured over exámenes, running .NET in a
//           browser is 88.8% of the bundle and the engine another 8.6%, against 188 KB for the same
//           module in process
//
// The hybrid is the two halves nobody had let sit together. And what it gains is not weight: it is
// that there is ONE SINGLE ENGINE over the journal, meaning one set of semaphores and one owner.
//
// WHO ANSWERS AND WHO PAINTS, and that is this class's entire decision:
//
//   the assistant is answered by THE SURFACE in process -> Invoke and ActualTools delegate there
//   the person is painted for by THE FACE in the WebView -> it lives, but answers nothing
//
// It is exactly the other way round from the hosted form, where the page is the one that answers.
// Here the page ASKS, through the bridge's `consultar` verb, and the answer comes from the same
// surface that answers through the other doors. One implementation, three doors.
//
// THE ARGUMENTS, AND THEIR ORDER IS NOT THE ONE YOU WOULD WRITE FIRST.
//
//   [0]  the bundle folder            what gets served
//   [1]  the publisher's endpoint     `baseUrl|resource|id`
//   [2]  the entry dll                what gets loaded
//
// The other shells' convention is that Arguments[0] is "the thing", and here the thing ought to be
// the dll because it is what answers. But [0] and [1] are already read by the whole web side -- and
// above all by WebModuleHost.Open, which opens the screen from the catalogue and knows nothing about
// forms. Putting the dll there would make opening the module try to serve a .dll as a folder, and it
// would fail far from here. The new consumer is the one that adapts.
//
// CAREFUL -- THIS IS NOT MEASURED ON ANDROID. InProcessShell is compiled into the APK and Android's
// stub points at it ("Use \"type\": \"in-process\""), but there is no run that shows Mono loading
// somebody else's assembly on the phone. That is precisely what this path exists to find out.
//
// AND THERE IS AN OBJECTION THIS CLASS DOES NOT ANSWER: an institution's dll executing natively is
// code arriving from outside. Against Google Play's text -- read on 2026-09-16, see B11 -- that
// gives DIRECT access to Android's APIs, and the policy's exception is precisely for what gives
// INDIRECT access ("JavaScript in a webview"). The hybrid inherits that objection whole.
//
// What the wasm box (F5) would do differently is exactly this: an intermediary that grants only the
// imports Gnothi decides on. That one DOES fit the text.
internal sealed class HybridShell : IShell
{
	private readonly InProcessShell surface;
	private readonly WebViewShell face;

	public HybridShell(string name, Shell shell, string grantedFolder)
	{
		// The face reads [0] and [1] as they are: to WebViewShell this Shell is indistinguishable
		// from a hosted module's, and that is the idea -- it never finds out it is hybrid.
		face = new WebViewShell(name, shell, grantedFolder);

		// And the surface gets the dll moved into first position, which is the only one
		// InProcessShell reads. Neither of the two knows about the other.
		//
		// keepLoaded: the face asks several times in a row while the person watches, and receives
		// deliveries in between. Unloading the assembly between paints puts two actors over the same
		// journal -- the Unload is cooperative and does not complete if the previous one left live
		// references -- and that shows up as `FileNotFoundException: Temp file not found`. The module
		// stays loaded while the face is open, and is released when the face is evicted.
		surface = new InProcessShell(name, SoloElDll(shell), grantedFolder, keepLoaded: true);
	}

	private static Shell SoloElDll(Shell shell) => new()
	{
		Type = shell.Type,
		Command = shell.Command,
		Arguments = shell.Arguments.Count > 2 ? [shell.Arguments[2]] : [],
	};

	// IT RUNS IF THE SURFACE RUNS. The face may not be alive -- the person did not open the screen --
	// and the module keeps answering: that is the difference from the hosted form, where with no
	// WebView there is nobody to answer.
	public bool IsRunning => surface.IsRunning;

	public Task<object> Invoke(
		string tool, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct)
		=> surface.Invoke(tool, arguments, accepts, ct);

	public Task<IReadOnlyList<string>> ActualTools(CancellationToken ct) => surface.ActualTools(ct);

	// THE DELIVERY GOES TO THE SURFACE, and this is the only one of the three shells where it needs
	// saying.
	//
	// In the hosted form the postman delivers to the PAGE -- `window.gnothi.deliver` -- because the
	// page carries the engine. Here the face has none, so the envelope goes to the only place that
	// can record it. It is the same postman, the same envelope and the same endpoint pinned at
	// install time: the only thing that changes is who receives it.
	public Task<string> Deliver(string envelopeJson, CancellationToken ct) =>
		surface.Deliver(envelopeJson, ct);

	// AND HOW FAR IT HAS ALREADY READ, which comes from the same actor that does the recording. It is
	// what makes the postman ask from there onwards: the face has no way of knowing it, and no way of
	// moving it either.
	public Task<long> Cursor(CancellationToken ct) => surface.Cursor(ct);

	// AND WHAT THE FACE ASKS FOR. It goes to the same surface that answers the assistant, but to
	// ANOTHER set of methods: the ones marked [View]. One implementation of the domain, two publics,
	// and neither can come in through the other's door.
	public Task<object> View(
		string name, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct) =>
		surface.View(name, arguments, accepts);

	// The face, for whoever has to open it. This class does not open it: opening it is an act of the
	// person's, not of the dispatcher's.
	internal WebViewShell Face => face;

	public async ValueTask DisposeAsync()
	{
		// Both of them, and the face first: evicting a WebView with the runtime inside frees ~150 MB
		// of PSS, and there is no point holding it up while the dll's context is unloaded.
		await face.DisposeAsync();
		await surface.DisposeAsync();
	}
}
