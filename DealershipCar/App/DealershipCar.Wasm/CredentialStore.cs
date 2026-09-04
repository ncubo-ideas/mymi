using Microsoft.JSInterop;

namespace DealershipCar.Wasm
{
	// LA CREDENCIAL ANTE EL CONCESIONARIO, fuera del journal (set de especificacion, 13 §2).
	//
	// Device key, access token y refresh token son contexto de sesion de ESTA instalacion, no
	// hechos de la vida de la persona. Hasta el 2026-09-03 se journalizaban (Logbook), y eso los
	// dejaba en claro en el expediente, viajando con cualquier copia, legibles por todo proceso
	// que lee la carpeta. Ahora viven en el storage del browser del origen
	// (credential-interop.js) y el bucle de refresh los recibe de aca, no del diario.
	//
	// Se cachea en memoria tras la primera lectura: la credencial se consulta en cada refresh y
	// no tiene sentido cruzar al JS cada vez. Toda escritura pasa por aca y actualiza el cache,
	// asi que el cache nunca queda atras.
	public sealed class CredentialStore : IAsyncDisposable
	{
		private readonly IJSRuntime js;
		private IJSObjectReference module;
		private StoredCredential inMemory;

		public CredentialStore(IJSRuntime js)
		{
			ArgumentNullException.ThrowIfNull(js);
			this.js = js;
		}

		// Si la ultima escritura no pudo respaldarse en el browser (storage bloqueado). La
		// credencial sigue valiendo en memoria durante esta sesion; la pantalla lo dice.
		public bool NotBackedUp { get; private set; }

		// Sincrono a proposito: la pantalla lo pregunta al renderizar, y despues de arrancar el
		// cache ya esta cargado.
		public bool HasToken => inMemory is { Token.Length: > 0 };

		private async ValueTask<IJSObjectReference> Module() =>
			module ??= await js.InvokeAsync<IJSObjectReference>("import", "./credential-interop.js");

		public async ValueTask<StoredCredential> ReadAsync()
		{
			if (inMemory is not null) return inMemory;

			inMemory = await (await Module()).InvokeAsync<StoredCredential>("readCredential")
			           ?? new StoredCredential();
			return inMemory;
		}

		public async ValueTask StoreAsync(StoredCredential credential)
		{
			ArgumentNullException.ThrowIfNull(credential);

			inMemory = credential;
			NotBackedUp = !await (await Module()).InvokeAsync<bool>("storeCredential", credential);
		}

		public async ValueTask ClearAsync()
		{
			inMemory = new StoredCredential();
			await (await Module()).InvokeAsync<bool>("clearCredential");
		}

		public async ValueTask DisposeAsync()
		{
			if (module is not null) await module.DisposeAsync();
		}
	}

	// Las tres piezas. Cruza el interop como JSON: Blazor serializa en camelCase, asi que del
	// lado JS son deviceKey / token / refresh.
	public sealed class StoredCredential
	{
		public string DeviceKey { get; set; } = "";

		public string Token { get; set; } = "";

		public string Refresh { get; set; } = "";
	}
}
