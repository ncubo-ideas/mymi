using Microsoft.JSInterop;

namespace DealershipCar.Wasm
{
	// EN QUE HOSPEDAJE CORRE LA APP, decidido UNA VEZ al arrancar y no vuelto a preguntar.
	//
	// Es el interruptor del que cuelgan las DOS unicas diferencias entre los dos modos
	// (especificacion 11 §2): de donde salen los eventos y a donde bajan los bytes. El
	// dominio, el actor y el journal son identicos.
	//
	// Se decide en Program.cs ANTES de que arranque el render, porque el contenedor construye
	// el JournalBridge con el destino ya elegido y despues seria tarde.
	public sealed class HostingMode
	{
		public bool Hosted { get; private set; }

		// El puente hacia Gnothi. Null cuando corre suelta.
		public GnothiBridge? Bridge { get; private set; }

		// El destino del journal cuando esta hospedada. Null cuando corre suelta, y ahi el
		// JournalBridge se ocupa el mismo del storage del browser.
		public IJournalDestination? Destination { get; private set; }

		public void HostIn(GnothiBridge bridge)
		{
			ArgumentNullException.ThrowIfNull(bridge);

			Hosted = true;
			Bridge = bridge;
			Destination = bridge.AsDestination();
		}
	}

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
	// ModuleSurface. Dos direcciones, dos mecanismos.
	public sealed class GnothiBridge : IAsyncDisposable
	{
		private readonly IJSRuntime js;
		private IJSObjectReference? module;

		public GnothiBridge(IJSRuntime js)
		{
			ArgumentNullException.ThrowIfNull(js);
			this.js = js;
		}

		private async ValueTask<IJSObjectReference> ModuleAsync() =>
			module ??= await js.InvokeAsync<IJSObjectReference>("import", "./gnothi-interop.js");

		// Si hay Gnothi del otro lado. Se pregunta UNA VEZ al arrancar y decide todo lo demas:
		// que fuente de eventos se arma y a que destino baja el journal.
		public async ValueTask<bool> IsHostedAsync() =>
			await (await ModuleAsync()).InvokeAsync<bool>("hospedada");

		// Avisa que el actor ya esta hidratado y el modulo puede recibir. Gnothi no puede
		// adivinarlo mirando la pantalla: el HTML se dibuja antes de que el actor termine.
		public async ValueTask ReadyAsync() =>
			await (await ModuleAsync()).InvokeVoidAsync("listo");

		// EL CURSOR LO PONE EL ACTOR. Gnothi valida que sea un entero, hace el HTTP con SU
		// credencial contra SU endpoint -fijo, registrado al instalar- y devuelve el sobre tal
		// cual. No guarda cursor: no hay dos verdades sobre hasta donde leyo (13 §8).
		public async ValueTask<string> PullAsync(long after) =>
			await (await ModuleAsync()).InvokeAsync<string>("pull", after);

		// El journal, contra los archivos de Gnothi. Misma forma que el destino del browser,
		// porque el movimiento MEMFS <-> destino es el mismo (ver IJournalDestination).
		public async ValueTask<ListResult> ListAsync() =>
			await (await ModuleAsync()).InvokeAsync<ListResult>("listar");

		public async ValueTask<ReadResult> ReadAsync(string path) =>
			await (await ModuleAsync()).InvokeAsync<ReadResult>("leer", path);

		public async ValueTask<WriteResult> WriteAsync(string path, string contentBase64) =>
			await (await ModuleAsync()).InvokeAsync<WriteResult>("escribir", path, contentBase64);

		// El destino del journal armado sobre este puente. Es lo que se le pasa al
		// JournalBridge cuando la app corre hospedada.
		public IJournalDestination AsDestination() =>
			new DestinationOverBridge(ListAsync, ReadAsync, WriteAsync);

		public async ValueTask DisposeAsync()
		{
			if (module is not null)
			{
				try { await module.DisposeAsync(); } catch (JSDisconnectedException) { }
				module = null;
			}
		}
	}
}
