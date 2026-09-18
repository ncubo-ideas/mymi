namespace Gnothi.Host;

// LA CASCARA DE WEBVIEW NO EXISTE EN ESCRITORIO, y esto no es un atajo: es que ahi no hace
// falta. En escritorio el modulo se carga en proceso o como proceso hijo, que es lo que las
// tiendas prohiben en un telefono y ahi nadie prohibe.
//
// El tipo existe solo porque Module.ChooseShell nombra "webview" en su switch. Tirar con un
// mensaje claro es mejor que no compilar: mantiene el codigo del Host IDENTICO en las dos
// plataformas, y deja dicha la imposibilidad justo donde alguien la buscaria.
//
// El archivo se llama WebViewShellStub.cs para no chocar con el WebViewShell.cs de
// Gnothi.MobileShared, que es el de verdad y compila la misma clase en su lugar. Gnothi.Mobile
// EXCLUYE este archivo de su glob; si algun dia entraran los dos, chocarian dos definiciones
// y esa es la senal correcta.
//
// LA VENTANA DE ESCRITORIO ES LA EXCEPCION: Gnothi.Desktop compila el WebViewShell real contra
// este ensamblado y lo registra por Module.ShellFactory antes de cargar el catalogo, asi que
// ahi "webview" e "hybrid" si corren. Este stub queda para el Host sin ventana -el frente MCP-,
// donde sigue sin haber donde pintar una cara.
internal sealed class WebViewShell : IShell
{
	private const string PorQue =
		"la cascara 'webview' solo existe en movil: es donde el modulo es la PWA de la "
		+ "institucion dentro de un WebView. En escritorio usa \"type\": \"in-process\".";

	public WebViewShell(string name, Shell shell, string grantedFolder) { }

	public bool IsRunning => false;

	public Task<object> Invoke(string tool, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct)
		=> throw new PlatformNotSupportedException(PorQue);

	public Task<IReadOnlyList<string>> ActualTools(CancellationToken ct)
		=> throw new PlatformNotSupportedException(PorQue);

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
