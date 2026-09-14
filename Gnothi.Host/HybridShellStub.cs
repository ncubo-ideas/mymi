namespace Gnothi.Host;

// LA CASCARA HIBRIDA NO EXISTE EN ESCRITORIO, por la misma razon que no existe la de webview:
// ahi no hace falta. La mitad que el hibrido agrega es la CARA en un WebView, y en escritorio
// el modulo se carga en proceso y el Host renderiza. Lo que en un telefono es una prohibicion
// de tienda, aca no lo prohibe nadie.
//
// El tipo existe solo porque Module.ChooseShell nombra "hybrid" en su switch. Tirar con un
// mensaje claro es mejor que no compilar: mantiene el codigo del Host IDENTICO en las dos
// plataformas, y deja dicha la imposibilidad donde alguien la buscaria.
//
// El archivo se llama HybridShellStub.cs para no chocar con el HybridShell.cs de Gnothi.MobileShared,
// que es el de verdad. Gnothi.Mobile EXCLUYE este archivo de su glob; si algun dia entraran
// los dos, chocarian dos definiciones y esa es la senal correcta.
internal sealed class HybridShell : IShell
{
	private const string PorQue =
		"la cascara 'hybrid' solo existe en movil: es donde el modulo trae su cara en un WebView "
		+ "y su superficie en un dll al lado. En escritorio usa \"type\": \"in-process\".";

	public HybridShell(string name, Shell shell, string grantedFolder) { }

	public bool IsRunning => false;

	public Task<object> Invoke(string tool, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct)
		=> throw new PlatformNotSupportedException(PorQue);

	public Task<IReadOnlyList<string>> ActualTools(CancellationToken ct)
		=> throw new PlatformNotSupportedException(PorQue);

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
