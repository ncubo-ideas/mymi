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

	// LO QUE EL CARTERO DEJA. Gnothi hizo la red -- con SU credencial, contra el endpoint que
	// EL fijo -- y le entrega al modulo un sobre ya leido. El modulo decide que hace con el.
	//
	// ES OPCIONAL, y por eso tiene cuerpo por omision: un modulo que solo lee no tiene donde
	// asentar nada, y eso no es una falla suya. Tirar seria decirle a quien lo llama que algo
	// se rompio; lo que pasa es que no hay a quien entregarle.
	//
	// La cascara que SI entrega es la hibrida: ahi la cara no tiene motor y la superficie es la
	// unica que puede escribir el journal.
	// LO QUE PIDE LA CARA DE LA INSTITUCION, y es OTRO CONJUNTO que el de las tools.
	//
	// Invoke contesta lo que el asistente puede preguntar: lo declarado en el manifiesto, grueso,
	// y apagable una por una. Esto contesta lo que una PANTALLA necesita para dibujar -- series,
	// puntos, lineas de referencia --, que ni se declara ni le sirve al modelo. Medido sobre
	// examenes: 2 contra 21.
	//
	// Son dos metodos y no un parametro a proposito: con un solo camino, una consulta de pantalla
	// podria llegar por la puerta del asistente, y al reves, sin que nada lo impidiera.
	Task<object> View(string name, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct) =>
		throw new NotSupportedException(
			"this shell has no screen queries: only the hybrid form has a face that asks.");

	Task<string> Deliver(string envelopeJson, CancellationToken ct) =>
		throw new NotSupportedException(
			"this shell does not take deliveries: the module has no surface that writes. "
			+ "In the hosted form the page applies events with the engine it carries.");

	// HASTA DONDE YA LEYO EL MODULO, para que el cartero pida de ahi en adelante.
	//
	// CERO POR OMISION, y cero no es un error: significa «traeme todo», que es lo que el cartero
	// hacia antes de que esto existiera. Solo la forma hibrida lo contesta de verdad, porque es la
	// unica donde la pagina no tiene actor con que saberlo.
	Task<long> Cursor(CancellationToken ct) => Task.FromResult(0L);
}
