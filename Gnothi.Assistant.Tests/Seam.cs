using Gnothi.Host;

namespace Gnothi.Tests;

// THE SEAM THAT CHOOSES THE SHELL, AND WHY IT NEEDED A TEST AT ALL.
//
// `Module.ShellFactory` is ONE static field. Whoever assembles the platform puts a factory there so
// that a module already installed can run in another host -- inside the box, inside the sala --
// WITHOUT rewriting the person's installation.json, which is a file of hers.
//
// ON 2026-09-23 IT HAD TWO OWNERS AND NOBODY HAD SAID SO. On Android the box registered from
// `OnCreate` and the sala from `Start()`, later, and both ASSIGNED. The last one won and the box was
// switched off -- while `caja: encendida para todos los modulos hibridos` went on printing. Neither
// line was written wrong. What was missing was that the field should say it belongs to one owner.
//
// THE DAMAGE IS NOT THE ONE IT LOOKS LIKE. It is not that a module ran in the wrong host: it is that
// A MEASUREMENT TAKEN LIKE THAT COMPARES NOTHING TO NOTHING, and the log swears it is good. The
// memory ladder, the border counters, the 250-300 ms per screen -- all of it is read off runs where
// one of the two switches may have been off without a word.
//
// WHY IT LIVES HERE AND NOT WHERE IT BROKE. The chaining used to be written TWICE -- MainActivity
// and ViewController -- and neither is compiled by any project that runs on an ordinary machine:
// Android needs a workload and iOS an exact version of Xcode. Two copies of a rule, zero tests. So
// the rule moved into `Module.ChainShellFactory`, which this suite does compile. That is the point
// of the move: it is not that the test needed a place, it is that a rule written twice has no owner.
internal static class Seam
{
	public static void Run(Action<string, bool, string> assert)
	{
		Func<string, Installation, IShell?>? before = Module.ShellFactory;

		try
		{
			// ── 1 · THE FIRST ONE, with nothing to chain onto ───────────────────────────────
			Module.ShellFactory = null;

			bool hubo = Module.ChainShellFactory((_, _) => new ShellThatSays(1));

			assert("la primera fabrica dice que no habia ninguna antes",
				!hubo, $"dijo que si habia: {hubo}");

			assert("y contesta",
				CursorOf("cualquiera", "hybrid") == 1,
				$"contesto {CursorOf("cualquiera", "hybrid")}");

			// ── 2 · THE SECOND ONE DOES NOT ERASE THE FIRST, which is the whole defect ──────
			//
			// This is the assertion that would have gone red in September. With the flat
			// assignment, factory 1 was gone and every module got 2 -- silently.
			Module.ChainShellFactory((name, _) => name == "suyo" ? new ShellThatSays(2) : null);

			assert("la segunda contesta por SU nombre",
				CursorOf("suyo", "hybrid") == 2,
				$"contesto {CursorOf("suyo", "hybrid")}");

			assert("Y LA PRIMERA SIGUE VIVA para los demas -- el defecto del 2026-09-23",
				CursorOf("ajeno", "hybrid") == 1,
				$"contesto {CursorOf("ajeno", "hybrid")}: la segunda se comio a la primera");

			// ── 3 · AND IT SAYS SO, which is what was missing for it not to be silent ─────────
			//
			// Chaining quietly brings the defect back in a calmer shape: two laboratories switched on
			// without either log mentioning it.
			assert("y avisa que habia una antes, para que la plataforma pueda decirlo",
				Module.ChainShellFactory((_, _) => null),
				"dijo que no habia ninguna, y habia dos");

			// ── 4 · EL ORDEN: la ultima registrada pregunta primero ─────────────────────────
			//
			// Not an implementation detail. The box's claims a FORM -- every hybrid -- and the room's
			// claims a LIST OF NAMES. If the wide one answered first, the narrow one would never win
			// a single module.
			Module.ShellFactory = null;
			Module.ChainShellFactory((_, _) => new ShellThatSays(10));   // la ancha, primero
			Module.ChainShellFactory((name, _) => name == "mio" ? new ShellThatSays(20) : null);

			assert("la ultima en registrarse pregunta primero",
				CursorOf("mio", "hybrid") == 20,
				$"contesto {CursorOf("mio", "hybrid")}: la ancha se quedo con el modulo de la angosta");

			// ── 5 · NOBODY CLAIMS IT: it falls through to the switch, the normal path ────────
			//
			// What is asserted is that both were consulted and neither claimed, without looking at
			// which shell comes out: that is the `switch`'s call and belongs to the declared form, not
			// to this seam. An `in-process` with a dll that does not exist is not started on construction.
			int consultas = 0;
			Module.ShellFactory = null;
			Module.ChainShellFactory((_, _) => { consultas++; return null; });
			Module.ChainShellFactory((_, _) => { consultas++; return null; });

			_ = ModuleOf("nadie", "in-process");

			assert("si ninguna opina, las dos igual fueron consultadas y decide el switch",
				consultas == 2, $"se consultaron {consultas} de 2");

			// ── 6 · LA FORMA LLEGA ENTERA a quien decide ────────────────────────────────────
			//
			// The box's gate filters on `installation.Shell.Type`, not on the name. If the seam handed
			// it something else, the gate would be deciding on data that is not its own.
			string vioLaForma = "";
			Module.ShellFactory = null;
			Module.ChainShellFactory((_, installation) => { vioLaForma = installation.Shell.Type; return null; });

			_ = ModuleOf("da-igual", "sala");

			assert("la fabrica ve la forma declarada en el installation.json",
				vioLaForma == "sala", $"vio '{vioLaForma}'");
		}
		finally { Module.ShellFactory = before; }
	}

	// THE OBSERVABLE, and it is the only one there is: `Module.IsRunning` and `Module.Cursor` are
	// what comes out of the shell without passing through anything else. The cursor serves better
	// because it is a number: each fake shell returns its own, so you know WHICH answered, not
	// merely that one did.
	private static long CursorOf(string name, string type) =>
		ModuleOf(name, type).Cursor(CancellationToken.None).GetAwaiter().GetResult();

	private static Module ModuleOf(string name, string type) => new(
		new Manifest { Module = name },
		new Installation
		{
			Shell = new Shell { Type = type, Command = "dotnet", Arguments = ["/no/existe.dll"] },
			GrantedFolder = "/journals/" + name,
			Enabled = true,
		},
		"/Modulos/" + name);

	// A shell that only knows how to say who it is. It opens nothing and starts nothing: what is
	// proved is WHICH one came out of the seam, not that it does its job.
	private sealed class ShellThatSays(long id) : IShell
	{
		public bool IsRunning => true;

		public Task<long> Cursor(CancellationToken ct) => Task.FromResult(id);

		public Task<object> Invoke(string tool, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct)
			=> throw new NotSupportedException("esta cascara solo existe para decir quien es");

		public Task<IReadOnlyList<string>> ActualTools(CancellationToken ct)
			=> Task.FromResult<IReadOnlyList<string>>([]);

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}
}
