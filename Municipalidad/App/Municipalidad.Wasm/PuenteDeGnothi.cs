using Microsoft.JSInterop;

namespace Municipalidad.Wasm
{
	/// <summary>
	/// El puente hacia Gnothi, y la deteccion de en que hospedaje corre esta app.
	/// <para>
	/// SE DETECTA POR LA PRESENCIA DEL PUENTE, no por el origen: lo que la app necesita saber no es como
	/// se llama su origen sino SI TIENE con quien hablar. Un origen que diga hospedado sin puente no
	/// sirve para nada, y un puente presente alcanza.
	/// </para>
	/// <para>
	/// Por aca viaja lo que el modulo le pide a Gnothi: los eventos posteriores al punto de control del
	/// actor, y el journal contra los archivos de Gnothi. Lo que va al reves -- las consultas y la
	/// entrega de un sobre -- se publica desde C# y lo llama el nativo. Son dos direcciones y dos
	/// mecanismos, y juntarlos en un solo objeto se leeria como si el modulo pudiera llamarse a si mismo.
	/// </para>
	/// </summary>
	public sealed class PuenteDeGnothi : IAsyncDisposable
	{
		private readonly IJSRuntime js;
		private IJSObjectReference modulo;

		public PuenteDeGnothi(IJSRuntime js)
		{
			ArgumentNullException.ThrowIfNull(js);

			this.js = js;
		}

		private async ValueTask<IJSObjectReference> ModuloAsync() =>
			modulo ??= await js.InvokeAsync<IJSObjectReference>("import", "./gnothi-interop.js");

		/// <summary>
		/// Si hay Gnothi del otro lado. Se pregunta UNA vez al arrancar y decide todo lo demas: que
		/// fuente de eventos se arma y a que destino baja el journal.
		/// </summary>
		public async ValueTask<bool> EstaHospedadaAsync() =>
			await (await ModuloAsync()).InvokeAsync<bool>("hospedada");

		/// <summary>
		/// Avisa que el actor ya esta hidratado y el modulo puede recibir. Gnothi no puede adivinarlo
		/// mirando la pantalla: el HTML se dibuja antes de que el actor termine.
		/// </summary>
		public async ValueTask ListoAsync() =>
			await (await ModuloAsync()).InvokeVoidAsync("listo");

		/// <summary>
		/// EL PUNTO DE CONTROL LO PONE EL ACTOR: sale del replay de su propio journal, igual que en el
		/// modo suelto. Gnothi hace el pedido con SU credencial contra SU endpoint -- fijos, registrados
		/// al instalar el modulo -- y devuelve el sobre tal cual, sin guardar el punto de control: no hay
		/// dos verdades sobre hasta donde leyo esta app.
		/// </summary>
		public async ValueTask<string> PullAsync(long despuesDe) =>
			await (await ModuloAsync()).InvokeAsync<string>("pull", despuesDe);

		public async ValueTask<ResultadoDeListado> ListarAsync() =>
			await (await ModuloAsync()).InvokeAsync<ResultadoDeListado>("listar");

		public async ValueTask<ResultadoDeLectura> LeerAsync(string ruta) =>
			await (await ModuloAsync()).InvokeAsync<ResultadoDeLectura>("leer", ruta);

		public async ValueTask<ResultadoDeEscritura> EscribirAsync(string ruta, string contenidoBase64) =>
			await (await ModuloAsync()).InvokeAsync<ResultadoDeEscritura>("escribir", ruta, contenidoBase64);

		/// <summary>El destino del journal armado sobre este puente, para cuando la app corre hospedada.</summary>
		public IDestinoDelJournal ComoDestino() =>
			new DestinoPorPuente(ListarAsync, LeerAsync, EscribirAsync);

		public async ValueTask DisposeAsync()
		{
			if (modulo is not null)
			{
				try { await modulo.DisposeAsync(); } catch (JSDisconnectedException) { }
				modulo = null;
			}
		}
	}
}
