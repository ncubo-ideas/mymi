using Microsoft.JSInterop;

namespace Examenes.Wasm;

// EL PUENTE HACIA GNOTHI, y la deteccion de en que hospedaje corre la app.
//
// COMO SE DETECTA. Por la PRESENCIA DEL PUENTE, no por el origen. La rev. 2 del documento
// 11 decia "hospedada si el origen es gnothi-mod://", pero el esquema propio se cambio por
// un host virtual sobre https (11 §3, corregido por el spike), y sobre todo: lo que la app
// necesita saber no es como se llama su origen sino SI TIENE con quien hablar. Un origen
// que diga hospedado sin puente no sirve para nada, y un puente presente es suficiente.
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
	// Android decide la FORMA del enlace del handoff. Es lo unico para lo que la app mira el
	// user agent, y vive aca porque el archivo de interop ya esta importado.
	public async ValueTask<bool> EsAndroid() =>
		await (await Modulo()).InvokeAsync<bool>("esAndroid");

	public async ValueTask<bool> EstaHospedada() =>
		await (await Modulo()).InvokeAsync<bool>("hospedada");

	// EL CURSOR LO PONE EL ACTOR. Gnothi valida que sea un entero, hace el HTTP con SU
	// credencial contra SU endpoint -fijo, registrado al instalar- y devuelve el sobre tal
	// cual. No guarda cursor: no hay dos verdades sobre hasta donde leyo (13 §8).
	// Avisa que el actor ya esta hidratado y el modulo puede recibir. Gnothi espera esto para
	// entregarle lo que trajo el handoff.
	public async ValueTask Listo() =>
		await (await Modulo()).InvokeVoidAsync("listo");

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
