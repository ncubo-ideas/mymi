using System.Text.Json;
using Gnothi.Host;

namespace Gnothi.Tests;

// THE SALA, AND THE TWO THINGS IT CANNOT LOSE.
//
// The sala runs ALL the modules in a single document of the web engine. That gives it what the
// hosted form does not have -- a shared substrate -- and takes away the crutch that made the
// separation free: there, there is one bridge per WebView, tied to one module, and the caller has
// nowhere to write a lie. Here there is one bridge and several modules.
//
// So the separation becomes an INVARIANT this class sustains, and there are two:
//
//   1 · ONE CALL IN FLIGHT, and the module's identity comes out of the FRAME of the call the Host
//       opened, never out of whatever arrives in the message. If an argument could choose who is
//       being talked about, the Host would see a legitimate question from a legitimate module --
//       which is exactly what the comment on `WebModule.Serve` forbids on the other side.
//
//   2 · ONE MODULE LOADED AT A TIME, and the previous one is EVICTED BEFORE the next one mounts
//       its folder. Measured on an A54: with two folders mounted at once, any code inside the
//       sandbox reads the neighbour's; evicting first, there is nothing left to read.
//
// WHY IT CAN BE TESTED HERE, with no phone and no web engine: `IRoomHost` is the only seam with
// the platform, and everything that decides lives on this side. The double below records the
// SEQUENCE of verbs, which is exactly where the two invariants live.
internal static class Room
{
	public static void Run(Action<string, bool, string> assert)
	{
		var host = new RoomDouble();
		RoomShell.Host = host;

		try
		{
			var examenes = new RoomShell("examenes", InstallationOf("examenes"), "/journals/examenes");
			var compras = new RoomShell("compras", InstallationOf("compras"), "/journals/compras");

			// ── 1 · the first one is prepared and loaded before answering ───────────────────
			examenes.Invoke("resumen", null, Accepts.JsonOnly, CancellationToken.None).GetAwaiter().GetResult();

			assert("el primer modulo se prepara y se carga antes de contestar",
				host.Sequence is ["preparar:examenes", "cargar:examenes", "consultar:examenes"],
				host.How());

			// ── 2 · the same module again is NOT reloaded ───────────────────────────────────
			host.Forget();
			examenes.Invoke("lista_de_examenes", null, Accepts.JsonOnly, CancellationToken.None).GetAwaiter().GetResult();

			assert("preguntarle otra vez al mismo modulo no lo vuelve a cargar",
				host.Sequence is ["consultar:examenes"],
				host.How());

			// ── 3 · THE INVARIANT: evict the previous one BEFORE mounting the next ──────────
			host.Forget();
			compras.Invoke("resumen", null, Accepts.JsonOnly, CancellationToken.None).GetAwaiter().GetResult();

			assert("cambiar de modulo desaloja, GUARDA lo que escribio, lo SUELTA, y recien despues monta al siguiente",
				host.Sequence is ["desalojar:examenes", "guardar:examenes", "soltar:examenes", "preparar:compras", "cargar:compras", "consultar:compras"],
				host.How());

			// RELEASE BEFORE PREPARE, AND IT IS NOT A DETAIL OF ORDERING. The host serves the bytes
			// of ONE module through the sala's origin; if the previous one were still servable, a
			// domain inside could ask for its journal over HTTP and skip the whole unmounting.
			// Between one module and the next there is none servable.
			assert("y suelta al anterior ANTES de preparar al siguiente",
				PositionOf(host.Sequence, "soltar:examenes") < PositionOf(host.Sequence, "preparar:compras"),
				host.How());

			assert("y lo guardado es del modulo que se fue, no del que entra",
				host.Saved is ["examenes/examenes/meta.bin"],
				string.Join(", ", host.Saved));

			assert("y nunca hay dos modulos cargados a la vez",
				host.MostLoadedAtOnce == 1,
				$"llego a tener {host.MostLoadedAtOnce} cargados");

			// ── 4 · THE INVARIANT: the caller does not choose who is talked about ───────────
			//
			// This is the one that breaks without anybody noticing. An argument called `module` or
			// `module` compiles, stays green, and is only seen the day a module reads the
			// neighbour's journal.
			host.Forget();
			var lie = new Dictionary<string, object?>
			{
				["modulo"] = "examenes",
				["module"] = "examenes",
				["grantedFolder"] = "/journals/examenes",
			};
			compras.Invoke("resumen", lie, Accepts.JsonOnly, CancellationToken.None).GetAwaiter().GetResult();

			assert("un argumento que nombra otro modulo NO cambia de quien se habla",
				host.Sequence is ["consultar:compras"],
				host.How());

			assert("y ese argumento igual llega al modulo, que es su dueño",
				host.LastArguments.Contains("\"module\"", StringComparison.Ordinal),
				host.LastArguments);

			// ── 5 · the three doors go through their own verb ───────────────────────────────
			host.Forget();
			compras.View("serie", null, Accepts.JsonOnly, CancellationToken.None).GetAwaiter().GetResult();
			compras.Deliver("{}", CancellationToken.None).GetAwaiter().GetResult();
			compras.Cursor(CancellationToken.None).GetAwaiter().GetResult();

			assert("la pantalla, la entrega y el cursor no entran por la puerta del asistente",
				host.Sequence is ["vista:compras", "entregar:compras", "guardar:compras", "cursor:compras"],
				host.How());

			// THE REFRESH HAS TO STAY, and this is the claim that says so: delivering an envelope
			// does not end when the module applies it in memory, it ends when what it wrote has
			// landed in the person's folder. Without this the refresh looked applied and vanished
			// on switching modules.
			assert("lo que el modulo asienta al entregar un sobre se guarda en el acto",
				host.Saved is [_, "compras/compras/journal/journal_000001.bin"],
				string.Join(", ", host.Saved));

			// ── 6 · releasing the shell does NOT throw the document away ────────────────────
			//
			// It is the sala's whole gain: the Dispatcher creates and releases one shell per
			// query, and if that threw the substrate away the start-up would be paid every time.
			// Measured: 3 seconds against 14 milliseconds.
			host.Forget();
			compras.DisposeAsync().AsTask().GetAwaiter().GetResult();

			assert("soltar la cascara no desaloja ni tira el sustrato",
				host.Sequence.Count == 0,
				host.How());

			// ── 7 · FAIL CLOSED: if the journal was not emptied, NO other is loaded on top ──
			//
			// This is the failure mode that was alive and that nobody saw. The sala's border IS
			// the emptying; an eviction that does not empty leaves it open. Before, the Host went
			// on and mounted the next one all the same. Now the query fails, which is the right
			// thing for a containment property: answering correctly with the border broken is
			// worse than not answering.
			host.Forget();
			host.DoesNotEmpty = true;

			bool failed = false;
			try { examenes.Invoke("resumen", null, Accepts.JsonOnly, CancellationToken.None).GetAwaiter().GetResult(); }
			catch (InvalidOperationException) { failed = true; }

			assert("un desalojo que no vacio el journal FALLA la consulta en vez de seguir",
				failed, host.How());

			assert("y no llega a preparar ni a cargar al siguiente",
				!host.Sequence.Any(v => v.StartsWith("preparar:", StringComparison.Ordinal)
					|| v.StartsWith("cargar:", StringComparison.Ordinal)),
				host.How());

			// ── 8 · THE MODULE THAT DOES NOT COME BACK FROM ITS OWN EVICTION ────────────────
			//
			// Number 7 covers the eviction that THROWS. This covers the one that DOES NOT COME
			// BACK, which is another failure mode and is not fixed the same way: inside the
			// eviction runs the module's Dispose() -- code the module chose -- and an infinite
			// loop there is not interrupted from the inside, because the web engine is
			// single-threaded. Without a ceiling, that module keeps the semaphore taken and NO
			// module answers again until somebody kills the app: any one module switching off the
			// whole Host.
			//
			// The way out is throwing the document away. And it is not only recovery: the module's
			// memory lives inside the document, so throwing it away CLOSES the border instead of
			// leaving it depending on the module having cooperated.
			host.Forget();
			host.DoesNotEmpty = false;
			host.Jams = true;

			TimeSpan ceiling = RoomShell.Ceiling;
			RoomShell.Ceiling = TimeSpan.FromMilliseconds(200);

			bool jammed = false;
			try { examenes.Invoke("resumen", null, Accepts.JsonOnly, CancellationToken.None).GetAwaiter().GetResult(); }
			catch (InvalidOperationException) { jammed = true; }

			assert("un modulo que no vuelve de su desalojo no cuelga la sala: la consulta falla",
				jammed, host.How());

			assert("y se tira el documento, nombrando al modulo que no contesto",
				host.Discarded.Contains("compras", StringComparison.Ordinal),
				host.Discarded.Length == 0 ? "(no se descarto nada)" : host.Discarded);

			assert("y no se monta al siguiente encima del que quedo trabado",
				!host.Sequence.Any(v => v.StartsWith("preparar:", StringComparison.Ordinal)
					|| v.StartsWith("cargar:", StringComparison.Ordinal)),
				host.How());

			// THE CLAIM THAT MAKES THIS A RECOVERY AND NOT JUST A TIDIER ERROR: the next query has
			// to work, in a new sala. If it stayed stuck, the ceiling would have been worth
			// nothing.
			host.Forget();
			host.Jams = false;
			examenes.Invoke("resumen", null, Accepts.JsonOnly, CancellationToken.None).GetAwaiter().GetResult();

			assert("y la consulta siguiente contesta, en una sala nueva y sin rastro del anterior",
				host.Sequence is ["preparar:examenes", "cargar:examenes", "consultar:examenes"],
				host.How());

			RoomShell.Ceiling = ceiling;

			// ── 9 · WHAT IS LEFT OF THE PREVIOUS LOAD IS NOT A LOG LINE ─────────────────────
			//
			// The substrate reports on loading how much was left of the previous loads, because
			// unmounting EMPTIES instead of deleting -- deleting kills the runtime from a
			// finaliser. That was written to the log and the module went in all the same.
			//
			// Zero bytes IS the sala's border. A border that is not checked is an expectation, so
			// now the load fails.
			host.Forget();
			host.BytesFromBefore = 48;

			bool somethingLeft = false;
			try { compras.Invoke("resumen", null, Accepts.JsonOnly, CancellationToken.None).GetAwaiter().GetResult(); }
			catch (InvalidOperationException) { somethingLeft = true; }

			assert("si quedaron bytes de una carga anterior, NO se carga el siguiente encima",
				somethingLeft, host.How());

			// AND THE DOCUMENT GOES, WHICH IS WHAT TURNS THE FAILURE INTO A RECOVERY.
			//
			// Failing and leaving everything standing left the sala broken FOR GOOD: `loaded` is
			// never set, so the next request believes nobody is loaded, does not evict, mounts on
			// top and finds the same bytes. Every attempt failed alike and added one more mount.
			//
			// Throwing the document away is the only thing that closes the border, because what is
			// mounted lives in ITS memory -- the same argument as number 8, reached from the other
			// failure mode. The assertion right below, that the next query answers in a clean sala,
			// is the other half and passed with or without this: it is THIS line that tells the
			// difference.
			assert("y se tira el documento, porque lo que quedo montado vive adentro de el",
				host.Sequence.Contains("descartar"), host.How());

			// AND THE FILES MAY INDEED REMAIN: they are zero-byte folders, with neutral names
			// (`c1`, `c2`) since the folder stopped being named after the module. They say how
			// many loads there were, not whose. That is the known residue, and it must fail
			// nothing.
			host.Forget();
			host.BytesFromBefore = 0;
			compras.Invoke("resumen", null, Accepts.JsonOnly, CancellationToken.None).GetAwaiter().GetResult();

			assert("pero carpetas vacias de cargas anteriores no impiden cargar",
				host.Sequence is ["preparar:compras", "cargar:compras", "consultar:compras"],
				host.How());
		}
		finally { RoomShell.Host = null; }
	}

	// Where in the sequence a verb appeared. -1 if it did not appear, which in a comparison of
	// order already says something is missing.
	private static int PositionOf(IReadOnlyList<string> sequence, string verb)
	{
		for (int i = 0; i < sequence.Count; i++)
			if (string.Equals(sequence[i], verb, StringComparison.Ordinal)) return i;

		return -1;
	}

	private static Shell InstallationOf(string module) => new()
	{
		Type = "sala",
		Command = "dotnet",
		Arguments = [$"/Modulos/{module}/web", "", $"/Modulos/{module}/dominio/{module}.App.dll"],
	};

	// THE SALA'S DOUBLE. There is no web engine and no WebView: what is tested is the SEQUENCE of
	// verbs, which is where the two invariants live.
	private sealed class RoomDouble : IRoomHost
	{
		private readonly List<string> sequence = [];
		private readonly HashSet<string> loadedSet = [];

		public IReadOnlyList<string> Sequence => sequence;

		public int MostLoadedAtOnce { get; private set; }

		public string LastArguments { get; private set; } = "";

		public void Forget() => sequence.Clear();

		public string How() => sequence.Count == 0 ? "(no pidio nada)" : string.Join(" -> ", sequence);

		public IReadOnlyList<string> Saved => saved;

		private readonly List<string> saved = [];

		public Task Prepare(string module, string entry, string grantedFolder, CancellationToken ct)
		{
			sequence.Add($"preparar:{module}");
			return Task.CompletedTask;
		}

		public bool DoesNotEmpty { get; set; }

		// THE MODULE THAT DOES NOT COME BACK. The double does not simulate an infinite loop with
		// an infinite loop: it returns a task that never completes, which is what the shell sees
		// from this side.
		public bool Jams { get; set; }

		public string Discarded { get; private set; } = "";

		// HOW MUCH WHAT IS LEFT OF THE PREVIOUS LOADS WEIGHS. Zero is normal: emptied folders.
		// More than zero is the border broken.
		public long BytesFromBefore { get; set; }

		public Task Discard(string reason, CancellationToken ct)
		{
			sequence.Add("descartar");
			Discarded = reason;

			// Throwing the document away takes with it whatever had loaded inside: nothing stays
			// mounted for the next one to read, which is precisely why it is thrown away.
			loadedSet.Clear();
			return Task.CompletedTask;
		}

		public Task Release(string module, CancellationToken ct)
		{
			sequence.Add($"soltar:{module}");
			return Task.CompletedTask;
		}

		public Task Save(string module, string path, string contentBase64, CancellationToken ct)
		{
			sequence.Add($"guardar:{module}");
			saved.Add($"{module}/{path}");
			return Task.CompletedTask;
		}

		public Task<string> Ask(string verb, string module, string a, string b, CancellationToken ct)
		{
			sequence.Add($"{verb}:{module}");

			// A task that never completes: to the shell it is indistinguishable from a Dispose()
			// that hung inside the web engine, which is what is meant to be tested.
			if (Jams && verb == "desalojar") return new TaskCompletionSource<string>().Task;

			switch (verb)
			{
				case "cargar":
					loadedSet.Add(module);
					MostLoadedAtOnce = Math.Max(MostLoadedAtOnce, loadedSet.Count);

					// `neighbours` is what was left of the previous loads. The names are neutral --
					// `c1`, `c2` -- because the folder is no longer named after the module, so the
					// only thing that can be claimed is the weight.
					return Task.FromResult(
						$$"""{"ok":true,"consultas":["resumen"],"vecinos":[{"carga":"c1","files":3,"bytes":{{BytesFromBefore}}}]}""");

				case "desalojar":
					loadedSet.Remove(module);

					// THE SALA THAT COULD NOT EMPTY. It is the case that used to pass in silence:
					// the Host went on and mounted the next one on top of what was left.
					if (DoesNotEmpty)
						return Task.FromResult("""{"ok":true,"vaciada":false,"desmontada":"QUEDARON 2 files sin vaciar de 3"}""");

					// The eviction brings back what the module wrote while it was inside.
					return Task.FromResult(
						$$"""{"ok":true,"vaciada":true,"escrituras":[{"ruta":"{{module}}/meta.bin","contenido":"AAEC"}]}""");

				case "consultar":
				case "vista":
					LastArguments = b;
					return Task.FromResult("""{"ok":true,"contenido":{"hayDatos":true}}""");

				case "entregar":
					// With one write inside: it is what the module recorded on applying the envelope.
					return Task.FromResult(
						"""{"ok":true,"contenido":"","escrituras":[{"ruta":"compras/journal/journal_000001.bin","contenido":"AAEC"}]}""");

				case "cursor":
					return Task.FromResult("""{"ok":true,"contenido":0}""");

				default:
					return Task.FromResult("""{"ok":true,"contenido":null}""");
			}
		}
	}
}
