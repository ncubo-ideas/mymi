namespace Gnothi.Host;

// THE SHELL, as a shape. It used to be a concrete class -- StdioShell -- named directly by
// Module. Now it is a shape with implementations, and the choice lives in each module's
// installation.json, which belongs to the Host.
//
// Notice what this interface does NOT mention: any MCP type. The response content travels
// as `object` and the front interprets it. That is the property that keeps a new shell from
// inheriting the previous one's protocol -- and it had to be fixed before adding the second
// one, because Module.Invoke used to return a CallToolResult.
internal interface IShell : IAsyncDisposable
{
	bool IsRunning { get; }

	// The content comes back OPAQUE. The stdio shell returns the module's CallToolResult as
	// it is; the in-process shell returns the projection of whatever the method returned.
	// The Host looks at neither: it forwards them.
	//
	// `accepts` is the HINT, not an order: the gate says which formats it can read and the
	// module picks one per answer. It travels here and not in `arguments` because it is not the
	// assistant's to set -- it is the Host's, and mixing the two would let a question choose its
	// own wrapper.
	Task<object> Invoke(string tool, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct);

	// The NAMES of what the module actually exposes, for the declared-versus-actual check.
	// Names and not protocol objects: names are the only thing being compared.
	Task<IReadOnlyList<string>> ActualTools(CancellationToken ct);

	// WHAT THE INSTITUTION'S FACE ASKS FOR, and it is ANOTHER SET than the tools'.
	//
	// Invoke answers what the assistant may ask: what is declared in the manifest, coarse, and
	// switchable off one by one. This answers what a SCREEN needs in order to draw -- series,
	// points, reference lines --, which is neither declared nor of any use to the model. Measured
	// over exámenes: 2 against 21.
	//
	// They are two methods and not one parameter on purpose: with a single path, a screen query
	// could arrive through the assistant's door, and the other way round, with nothing to stop it.
	Task<object> View(string name, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct) =>
		throw new NotSupportedException(
			"this shell has no screen queries: only the hybrid form has a face that asks.");

	// WHAT THE POSTMAN DROPS OFF. Gnothi did the networking -- with ITS credential, against the
	// endpoint IT pinned -- and hands the module an envelope it has already read. The module decides
	// what to do with it.
	//
	// IT IS OPTIONAL, which is why it has a default body: a module that only reads has nowhere to
	// record anything, and that is not a failing of its own. Throwing would tell the caller that
	// something broke; what is happening is that there is nobody to deliver to.
	//
	// The shell that DOES take deliveries is the hybrid one: there the face has no engine and the
	// surface is the only thing that can write the journal.
	Task<string> Deliver(string envelopeJson, CancellationToken ct) =>
		throw new NotSupportedException(
			"this shell does not take deliveries: the module has no surface that writes. "
			+ "In the hosted form the page applies events with the engine it carries.");

	// HOW FAR THE MODULE HAS ALREADY READ, so the postman asks from there onwards.
	//
	// ZERO BY DEFAULT, and zero is not an error: it means «bring me everything», which is what the
	// postman did before this existed. Only the hybrid form answers it for real, because it is the
	// only one where the page has no actor to know it with.
	Task<long> Cursor(CancellationToken ct) => Task.FromResult(0L);
}
