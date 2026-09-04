using Microsoft.JSInterop;

namespace Cargas.Wasm;

// EL PUENTE HACIA GNOTHI, y la deteccion de en que hospedaje corre la app.
//
// COMO SE DETECTA. Por la PRESENCIA DEL PUENTE, no por el origen. Lo que la app necesita
// saber no es como se llama su origen sino SI TIENE con quien hablar: un origen que diga
// hospedado sin puente no sirve para nada, y un puente presente es suficiente.
//
// QUE VIAJA POR ACA (modulo -> Gnothi):
//     pull(after)                  pedirle los eventos posteriores al cursor DEL ACTOR
//     listar / leer / escribir     el journal, contra los archivos del contenedor de Gnothi
//
// Lo que va al reves -call, tools, deliver- se publica desde C# y lo llama el nativo; ver
// SuperficieDelModulo. Dos direcciones, dos mecanismos.
public sealed class PuenteDeGnothi : IAsyncDisposable
{
	private readonly IJSRuntime js;
	private IJSObjectReference? modulo;

	public PuenteDeGnothi(IJSRuntime js)
	{
		ArgumentNullException.ThrowIfNull(js);
		this.js = js;
	}

	private async ValueTask<IJSObjectReference> Modulo() =>
		modulo ??= await js.InvokeAsync<IJSObjectReference>("import", "./gnothi-interop.js");

	// Si hay Gnothi del otro lado. Se pregunta UNA VEZ al arrancar y decide todo lo demas:
	// que fuente de eventos se arma y a que destino baja el journal.
	public async ValueTask<bool> EstaHospedada() =>
		await (await Modulo()).InvokeAsync<bool>("hospedada");

	// Avisa que el actor ya esta hidratado y el modulo puede recibir. Gnothi no puede
	// adivinarlo mirando la pantalla: el HTML se dibuja antes de que el actor termine.
	public async ValueTask Listo() =>
		await (await Modulo()).InvokeVoidAsync("listo");

	// EL CURSOR LO PONE EL ACTOR. Gnothi valida que sea un entero, hace el HTTP con SU
	// credencial contra SU endpoint -fijo, registrado al instalar- y devuelve el sobre tal
	// cual. No guarda cursor: no hay dos verdades sobre hasta donde leyo (13 §8).
	public async ValueTask<string> Pull(long despuesDe) =>
		await (await Modulo()).InvokeAsync<string>("pull", despuesDe);

	// El journal, contra los archivos de Gnothi. Misma forma que el destino del browser,
	// porque el movimiento MEMFS <-> destino es el mismo (ver IDestinoDelJournal).
	public async ValueTask<ResultadoDeListado> Listar() =>
		await (await Modulo()).InvokeAsync<ResultadoDeListado>("listar");

	public async ValueTask<ResultadoDeLectura> Leer(string ruta) =>
		await (await Modulo()).InvokeAsync<ResultadoDeLectura>("leer", ruta);

	public async ValueTask<ResultadoDeEscritura> Escribir(string ruta, string contenidoBase64) =>
		await (await Modulo()).InvokeAsync<ResultadoDeEscritura>("escribir", ruta, contenidoBase64);

	// El destino del journal armado sobre este puente. Es lo que se le pasa al
	// PuenteDeJournal cuando la app corre hospedada.
	public IDestinoDelJournal ComoDestino() =>
		new DestinoPorPuente(Listar, Leer, Escribir);

	public async ValueTask DisposeAsync()
	{
		if (modulo is not null)
		{
			try { await modulo.DisposeAsync(); } catch (JSDisconnectedException) { }
			modulo = null;
		}
	}
}
