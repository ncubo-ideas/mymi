using Gnothi.Host;

namespace Gnothi.Desktop;

// WHO PUTS THE ROOM IN PLACE ON THE DESKTOP. The twin of MainActivity's three lines and of
// ViewController's: name the host, and register the shell.
//
// It is its own file and not four lines inside Program.cs for the same reason `TheBox.cs` is: what
// decides whether a form is available, and under what conditions, is worth being findable by name.
internal static class TheDesktopRoom
{
	// WHERE THE SUBSTRATE LANDS IN THE OUTPUT. The csproj copies the published wwwroot under
	// `sala/`; on the phones the same files go into the apk's assets and into the .app under the
	// same name. If those three stop matching, the three rooms stop being the same room.
	private const string Folder = "sala";

	// Y DEVUELVE ALGO QUE SE PUEDE CALENTAR. `Func<CancellationToken, Task>` y no el cuarto entero:
	// lo unico que el llamante necesita de el es esto.
	private static string[] theOnesInTheRoom = [];

	internal static Func<CancellationToken, Task>? Prepare(Action<string> log, bool ventana, string? only)
	{
		theOnesInTheRoom = string.IsNullOrWhiteSpace(only)
			? []
			: [.. only.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

		string substrate = Path.Combine(AppContext.BaseDirectory, Folder);

		// THE DOCUMENT IS THE SUBSTRATE'S, so its index is what says whether there is a room at all.
		// Checking the folder alone would let a half-copied publish through, and the symptom of that
		// is a blank document and no error -- which is exactly what Android paid for once.
		if (!File.Exists(Path.Combine(substrate, "index.html")))
		{
			log($"sala: no esta {Folder}/index.html al lado del ejecutable; los modulos corren como siempre");
			log("sala: se regenera con  dotnet publish Spikes/Lab.Sala/Sustrato -c Release -o Spikes/Lab.Sala/Sustrato/publicado");
			return null;
		}

		// LA SALA VIVE EN LA PAGINA DE GNOTHI, Y POR ESO NO HAY SALA SIN ELLA. Es un iframe oculto de
		// esa pagina, servido por loopback, y habla por el puente de los marcos. Con `--cara ventana`
		// —la forma vieja, de Windows, donde la cara es una ventana de WebView2 aparte— esa pagina no
		// esta, asi que no hay donde poner la sala.
		//
		// HUBO UNA SEGUNDA SALA PARA ESE MODO Y SE BORRO · 2026-09-25. 407 lineas sobre WebView2 con
		// documento propio y host virtual. **Nadie pasa `--cara ventana`**: no la pasa ningun
		// instalador, ni un script, ni un doc — solo aparece si alguien la escribe a mano. Era una
		// sala para un modo muerto, y con un modulo de verdad ni siquiera cargaba: se descubrio al
		// preguntar «esto ya esta terminado?», no corriendola.
		//
		// La leccion, que es la unica razon de que esto quede escrito: **construir para una bandera
		// sin comprobar quien la pasa.** La bandera existia y compilaba, o sea que todas las senales
		// baratas decian que si.
		if (ventana)
		{
			log("sala: con `--cara ventana` la cara es una ventana de WebView2 y no hay pagina de "
				+ "Gnothi donde vivir; los modulos corren como siempre");
			return null;
		}

		// EL MISMO CABLE QUE `HybridShell.Log` Y `WasmBox.Log`, y va aca por el mismo motivo: la
		// cascara no sabe escribir en el log de nadie, se lo dan al arrancar. Sin esta linea el
		// contador de `RoomShell` cuenta y no lo dice nadie, que es como estuvo hasta el 2026-09-25.
		RoomShell.Log = log;

		var theRoom = new PhotinoRoom(substrate, log);
		RoomShell.Host = theRoom;
		SalaBridge.Current = theRoom;
		Func<CancellationToken, Task> warm = theRoom.WarmUp;

		// LA COSTURA, Y RECLAMA HIBRIDAS POR DECISION DE CODIGO — COMO LA CAJA.
		//
		// ESTO ESTUVO ESCRITO AL REVES Y DURO UN DIA. La primera version reclamaba los modulos que
		// **declaran** `"sala"` en su installation.json, y para que eso sirviera de algo hubo que
		// ensenarle al alta a escribir ese tipo. Las dos cosas estaban mal:
		//
		//   1 · La fabrica no aportaba NADA. El `switch` de `ChooseShell` ya mapea `"sala"` a
		//       `RoomShell`, asi que registrar una fabrica que reclama lo mismo es una capa que no
		//       decide nada. La decision seguia entera en el tipo guardado.
		//
		//   2 · Y guardar el anfitrion en el archivo de la persona es lo que la caja NO hace, por un
		//       motivo que se midio: con `"sala"` escrito ahi y la sala no disponible, el `switch`
		//       construye igual la cascara y al primer uso revienta. **El modulo no se degrada: se
		//       rompe.** Un modulo EN CAJA, en cambio, sigue diciendo `"hybrid"`; si manana falta la
		//       libreria nativa, la fabrica no se registra y todo corre como antes.
		//
		// El anfitrion es una propiedad de ESTE Gnothi en ESTA maquina, no de la instalacion de la
		// persona. Asi que se decide en codigo, en cada arranque, y su archivo no se toca.
		//
		// ── Y POR QUE POR LISTA Y NO TODAS, que es lo unico en que se aparta de la caja ──────────
		//
		// La caja de escritorio reclama TODAS las hibridas, porque su trabajo es contenerlas y no hay
		// nada que elegir. Si la sala hiciera lo mismo **se las quedaria todas**: se registra DESPUES,
		// y `ChainShellFactory` le pregunta primero a la ultima. La caja quedaria encendida en el log
		// y apagada de hecho -- exactamente el defecto del 2026-09-23, reintroducido por simetria mal
		// entendida.
		//
		// Con la lista vacia no reclama nada, que es lo correcto: en escritorio la contencion ya la
		// da la caja, y la sala existe aca para poder compararlas en una maquina.
		if (theOnesInTheRoom.Length == 0)
		{
			log("sala: lista sobre Photino, y sin modulos asignados "
				+ "(--sala-solo <modulos>); la caja sigue corriendo todas las hibridas");
			return warm;
		}

		if (Module.ChainShellFactory((name, installation) =>
				installation.Shell.Type.Equals("hybrid", StringComparison.OrdinalIgnoreCase)
				&& Array.Exists(theOnesInTheRoom, each => each.Equals(name, StringComparison.OrdinalIgnoreCase))
					? new RoomShell(name, installation.Shell, installation.GrantedFolder)
					: null))
			log("sala: ya habia una fabrica de cascaras (la caja); la sala se encadena y solo contesta por sus nombres");

		log($"sala: lista sobre Photino; corren en ella {string.Join(", ", theOnesInTheRoom)}");
		return warm;
	}
}
