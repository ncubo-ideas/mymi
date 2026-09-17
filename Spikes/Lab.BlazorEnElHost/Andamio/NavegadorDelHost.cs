using Microsoft.AspNetCore.Components;

namespace Lab.BlazorEnElHost;

/// EL NAVEGADOR, y es la tercera pieza de andamio que la pantalla real pidio.
///
/// `MisCompras.razor` lo inyecta y lo usa siete veces -- `navegacion.NavigateTo("recibos")`,
/// `"producto/..."`, `"infaltables"` --. `NavigationManager` es ABSTRACTA y no trae
/// implementacion: en Wasm la pone el runtime del navegador, en Blazor Server el circuito. Aca no
/// hay ninguno de los dos, asi que la pone el Host.
///
/// ESTA VERSION NO NAVEGA, y eso es honesto para un spike: anota a donde le pidieron ir y lo dice.
/// Una cuarta forma de verdad tendria que decidir que significa una ruta cuando el modulo es una
/// pantalla adentro de Gnothi -- si son vistas del mismo componente raiz, o si Gnothi le presta
/// una pila de navegacion. Es una decision de producto, no un detalle de implementacion, y el
/// spike la deja a la vista en vez de resolverla a escondidas.
internal sealed class NavegadorDelHost : NavigationManager
{
	private readonly Action<string> anotar;

	public NavegadorDelHost(Action<string> anotar)
	{
		this.anotar = anotar;

		// La base tiene que terminar en barra o la clase base tira al construir.
		Initialize("https://modulo.gnothi.local/", "https://modulo.gnothi.local/");
	}

	protected override void NavigateToCore(string uri, bool forceLoad) =>
		anotar($"la pantalla pidio navegar a '{uri}' (forceLoad={forceLoad})");
}
