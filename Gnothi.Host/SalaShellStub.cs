namespace Gnothi.Host;

// LA SALA NO EXISTE EN ESCRITORIO, por la misma razon que no existe la cascara de WebView: ahi
// el modulo se carga en proceso, que es lo que en un telefono no pasa el texto de Google -- el
// dll ajeno tendria acceso DIRECTO a las APIs del sistema (ver B11) -- y en una PC no le importa
// a nadie.
//
// El tipo existe solo porque Module.ChooseShell nombra "sala" en su switch. Tirar con un mensaje
// claro es mejor que no compilar: mantiene el codigo del Host IDENTICO en las dos plataformas.
//
// Se llama SalaShellStub.cs para no chocar con el SalaShell.cs de Gnothi.MobileShared, que es el
// de verdad. Gnothi.Mobile EXCLUYE este archivo de su glob.
internal sealed class SalaShell : IShell
{
	private const string PorQue =
		"la cascara 'sala' solo existe en movil: es donde el modulo corre adentro del motor web, "
		+ "en el sustrato que Gnothi presta. En escritorio usa \"type\": \"in-process\".";

	public SalaShell(string name, Shell shell, string grantedFolder) { }

	public bool IsRunning => false;

	public Task<object> Invoke(string tool, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct)
		=> throw new PlatformNotSupportedException(PorQue);

	public Task<IReadOnlyList<string>> ActualTools(CancellationToken ct)
		=> throw new PlatformNotSupportedException(PorQue);

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
