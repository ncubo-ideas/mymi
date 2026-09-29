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

	// AND IT RETURNS SOMETHING THAT CAN BE WARMED UP. `Func<CancellationToken, Task>` and not the
	// room itself: that is the only thing the caller needs from it.
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

		// THE ROOM LIVES IN GNOTHI'S PAGE, AND SO THERE IS NO ROOM WITHOUT IT. It is a hidden iframe
		// of that page, served over loopback, and it speaks through the frames' bridge. With
		// `--cara ventana` -- the old Windows-only shape, where a face is a separate WebView2 window
		// -- that page is not there, so there is nowhere to put the room.
		//
		// THERE WAS A SECOND ROOM FOR THAT MODE AND IT WAS DELETED · 2026-09-25. 407 lines over
		// WebView2 with its own document and a virtual host. **Nobody passes `--cara ventana`**: not
		// an installer, not a script, not a doc -- it only shows up if somebody types it by hand. It
		// was a room for a dead mode, and with a real module it did not even load: it was found by
		// asking "is this finished?", not by running it.
		//
		// The lesson, which is the only reason this stays written: **building for a flag without
		// checking who passes it.** The flag existed and compiled, so every cheap signal said yes.

		if (ventana)
		{
			log("sala: con `--cara ventana` la cara es una ventana de WebView2 y no hay pagina de "
				+ "Gnothi donde vivir; los modulos corren como siempre");
			return null;
		}

		// THE SAME WIRE AS `HybridShell.Log` AND `WasmBox.Log`, and it goes here for the same reason:
		// the shell does not know how to write to anybody's log, it is handed one at start-up.
		// Without this line `RoomShell`'s counter counts and nobody says it -- as it was until 2026-09-25.
		RoomShell.Log = log;

		var theRoom = new PhotinoRoom(substrate, log);
		RoomShell.Host = theRoom;
		RoomBridge.Current = theRoom;
		Func<CancellationToken, Task> warm = theRoom.WarmUp;

		// THE SEAM, AND IT CLAIMS HYBRIDS BY A DECISION OF CODE -- LIKE THE BOX.
		//
		// THIS WAS WRITTEN BACK TO FRONT AND LASTED A DAY. The first version claimed the modules that
		// **declare** `"sala"` in their installation.json, and for that to be worth anything the
		// enrolment had to be taught to write that type. Both things were wrong:
		//
		//   1 · The factory contributed NOTHING. `ChooseShell`'s `switch` already maps `"sala"` to
		//       `RoomShell`, so registering a factory that claims the same thing is a layer that
		//       decides nothing. The decision stayed entirely in the stored type.
		//
		//   2 · And storing the host in the person's file is what the box does NOT do, for a measured
		//       reason: with `"sala"` written there and the room unavailable, the `switch` builds the
		//       shell anyway and it blows up on first use. **The module does not degrade: it
		//       breaks.** A module IN A BOX, by contrast, goes on saying `"hybrid"`; if tomorrow the
		//       native library is missing, the factory is never registered and everything runs as
		//       before.
		//
		// The host is a property of THIS Gnothi on THIS machine, not of the person's installation. So
		// it is decided in code, at every start-up, and their file is not touched.
		//
		// ── AND WHY BY LIST AND NOT ALL, which is the only place it departs from the box ─────────
		//
		// The desktop box claims ALL the hybrids, because its job is to contain them and there is
		// nothing to choose. If the room did the same it would **take them all**: it registers
		// AFTER, and `ChainShellFactory` asks the last one first. The box would be on in the log and
		// off in fact -- exactly the defect of 2026-09-23, reintroduced by misread symmetry.
		//
		// With an empty list it claims nothing, which is right: on the desktop the containment is
		// already the box's, and the room exists here so the two can be compared on one machine.
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
