using Microsoft.JSInterop;

namespace Compras.Wasm;

// LA CREDENCIAL ANTE EL PROVEEDOR, fuera del journal (set de especificacion, 13 §2).
//
// Device key, access token y refresh token son contexto de sesion de ESTA instalacion, no
// hechos de la vida de la persona. Hasta el 2026-09-03 se journalizaban (Bitacora), y eso
// los dejaba en claro en el journal, viajando con cualquier copia o traspaso, legibles por
// todo proceso que lee la carpeta. Ahora viven en el storage del browser del origen
// (credencial-interop.js) y el bucle de refresh los recibe de aca, no del diario.
//
// Se cachea en memoria tras la primera lectura: la credencial se consulta en cada refresh
// y no tiene sentido cruzar al JS cada vez. Toda escritura pasa por aca y actualiza el
// cache, asi que el cache nunca queda atras.
//
// Esta app todavia no corre hospedada dentro de Gnothi. Cuando lo haga, esta clase no se
// usara: la red la hara Gnothi con SU credencial y la PWA no vera ningun token.
public sealed class AlmacenDeCredencial : IAsyncDisposable
{
	private readonly IJSRuntime js;
	private IJSObjectReference? modulo;
	private CredencialGuardada? enMemoria;

	public AlmacenDeCredencial(IJSRuntime js)
	{
		ArgumentNullException.ThrowIfNull(js);
		this.js = js;
	}

	// Si la ultima escritura no pudo respaldarse en el browser (storage bloqueado). La
	// credencial sigue valiendo en memoria durante esta sesion; la pantalla lo dice.
	public bool SinRespaldo { get; private set; }

	// Sincrono a proposito: la pantalla lo pregunta al renderizar, y despues de Arrancar
	// el cache ya esta cargado.
	public bool TieneToken => enMemoria is { Token.Length: > 0 };

	private async ValueTask<IJSObjectReference> Modulo() =>
		modulo ??= await js.InvokeAsync<IJSObjectReference>("import", "./credencial-interop.js");

	public async ValueTask<CredencialGuardada> Leer()
	{
		if (enMemoria is not null) return enMemoria;

		enMemoria = await (await Modulo()).InvokeAsync<CredencialGuardada>("leerCredencial")
		            ?? new CredencialGuardada();
		return enMemoria;
	}

	public async ValueTask Guardar(CredencialGuardada credencial)
	{
		ArgumentNullException.ThrowIfNull(credencial);

		enMemoria = credencial;
		SinRespaldo = !await (await Modulo()).InvokeAsync<bool>("guardarCredencial", credencial);
	}

	public async ValueTask Borrar()
	{
		enMemoria = new CredencialGuardada();
		await (await Modulo()).InvokeAsync<bool>("borrarCredencial");
	}

	public async ValueTask DisposeAsync()
	{
		if (modulo is not null) await modulo.DisposeAsync();
	}
}

// Las tres piezas. Cruza el interop como JSON: Blazor serializa en camelCase, asi que del
// lado JS son deviceKey / token / refresh.
public sealed class CredencialGuardada
{
	public string DeviceKey { get; set; } = "";

	public string Token { get; set; } = "";

	public string Refresh { get; set; } = "";
}
