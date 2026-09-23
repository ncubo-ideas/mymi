using System.Reflection;
using System.Text;
using System.Text.Json;
using Gnothi.MobileShared;

namespace Gnothi.Tests;

// THE ISOLATION BETWEEN MODULES, AS A GUARD AND NOT AS AN INTENTION.
//
// The journals are NOT separated by the browser origin: they are separated by the BRIDGE, which
// today is tied to one WebView and therefore to one module. `WebModule.Serve` says so of `consultar`:
//
//     "IT DOES NOT NAME A MODULE, on purpose. [...] A `module` parameter here would let a face
//      ask about somebody else's journal, and no rule downstream would stop it."
//
// While there is one bridge per WebView, that rule holds by itself: the caller has nowhere to
// write the lie. The form under evaluation -a single web-engine document with several modules
// inside (note 49)- takes that crutch away: one bridge, five modules. There the property stops
// being a fact of the architecture and becomes an invariant that has to be held up with code.
//
// That is why this guard does not wait for that form. It asserts TODAY the two halves that form
// cannot lose:
//
//   1 · that a write by one module cannot land in another's folder, and
//   2 · that no verb of the bridge accepts a module's name as data from the caller.
//
// The second is the one that breaks without anybody noticing: adding a `module` parameter compiles,
// stays green, and is only seen the day a module reads the neighbour's journal.
internal static class Aislamiento
{
	public static void Run(Action<string, bool, string> assert)
	{
		string raiz = Path.Combine(Path.GetTempPath(), "gnothi-aislamiento-" + Guid.NewGuid().ToString("N"));
		string deExamenes = Path.Combine(raiz, "examenes");
		string deCompras = Path.Combine(raiz, "compras");

		try
		{
			var examenes = new ModuloDePrueba("examenes", deExamenes);
			var compras = new ModuloDePrueba("compras", deCompras);

			// The neighbour has something to lose: without this, "it did not read it" proves nothing.
			Directory.CreateDirectory(Path.Combine(deCompras, "compras"));
			File.WriteAllText(Path.Combine(deCompras, "compras", "meta.bin"), "del vecino");

			// ── 1 · what is legitimate works ───────────────────────────────────────────────
			string suyo = "examenes/journal/journal_000001.bin";
			examenes.Write(suyo, Base64("lo mio"));
			assert("un modulo escribe en SU journal",
				File.Exists(Path.Combine(deExamenes, "examenes", "journal", "journal_000001.bin")),
				$"no aparecio bajo {deExamenes}");

			// ── 2 · climbing up into the neighbour folder ──────────────────────────────────
			assert("una ruta que SUBE fuera del journal se rechaza",
				Rechaza(() => examenes.Write("../compras/compras/meta.bin", Base64("pisado"))),
				"Write acepto una ruta con ..");

			assert("y no toco el archivo del vecino",
				File.ReadAllText(Path.Combine(deCompras, "compras", "meta.bin")) == "del vecino",
				"el meta.bin del vecino cambio");

			// The backslash is a separator on Windows and the replacement of '/' does not normalise it.
			assert("una ruta que sube con barra invertida tambien se rechaza",
				Rechaza(() => examenes.Write(@"..\compras\robado.bin", Base64("x"))),
				"Write acepto una ruta con ..\\");

			// ── 3 · naming the neighbour folder head on ────────────────────────────────────
			assert("una ruta ABSOLUTA a otro journal se rechaza",
				Rechaza(() => examenes.Write(Path.Combine(deCompras, "robado.bin"), Base64("x"))),
				"Write acepto una ruta absoluta ajena");

			assert("leer del vecino tambien se rechaza",
				Rechaza(() => examenes.Read("../compras/compras/meta.bin")),
				"Read acepto una ruta con ..");

			// ── 4 · what a module SEES when listing is only its own ────────────────────────
			string listado = examenes.List();
			assert("al listar, un modulo no ve los archivos del vecino",
				!listado.Contains("compras", StringComparison.OrdinalIgnoreCase),
				listado);

			// ── 5 · THE BRIDGE SHAPE: no verb receives a module name ───────────────────────
			//
			// This is the one that protects note 49. With one bridge per WebView the name is
			// redundant; with a single document and several modules inside, adding it is the
			// obvious way out -- and precisely the one `Serve` comment forbids. If somebody adds
			// it, this fails the same day.
			MethodInfo? serve = typeof(WebModule).GetMethod("Serve",
				BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

			assert("el puente tiene un unico punto de entrada, WebModule.Serve",
				serve is not null, "no se encontro Serve");

			if (serve is not null)
			{
				string[] parametros = [.. serve.GetParameters().Select(p => p.Name ?? "")];
				assert("Serve no recibe el nombre de un modulo",
					!parametros.Any(EsNombreDeModulo),
					string.Join(", ", parametros));
			}

			// And the same for everything the bridge serves: if one of these grew a `module`
			// parameter, the Host would see a legitimate question from a legitimate module.
			string[] verbos = ["List", "Read", "Write", "Pull", "Consultar", "Answer", "Ready", "Violation"];
			var culpables = new List<string>();
			foreach (string verbo in verbos)
			{
				foreach (MethodInfo m in typeof(WebModule)
					.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
					.Where(m => m.Name == verbo))
				{
					foreach (ParameterInfo p in m.GetParameters())
						if (EsNombreDeModulo(p.Name ?? "")) culpables.Add($"{m.Name}({p.Name})");
				}
			}

			assert("ningun verbo del puente acepta el nombre de un modulo como dato del llamante",
				culpables.Count == 0,
				string.Join(", ", culpables));

			// POSITIVE CONTROL. Without this, the two assertions above would pass even with the
			// detector broken, and a test that cannot fail proves nothing. The eviction DOES name
			// a module -- and it is right that it does, because it is called by the Host and not
			// by the module. That is exactly the distinction the bridge sustains.
			MethodInfo? desalojar = typeof(WebModule).Assembly
				.GetTypes()
				.Where(t => t.Name.StartsWith("WebModuleHost", StringComparison.Ordinal))
				.SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				.FirstOrDefault(m => m.Name == "Evict" && m.GetParameters().Length == 1);

			assert("el detector no es vacio: reconoce el parametro de Evict, que si nombra un modulo",
				desalojar is not null && desalojar.GetParameters().Any(p => EsNombreDeModulo(p.Name ?? "")),
				desalojar is null ? "no se encontro Evict(string)" : desalojar.GetParameters()[0].Name ?? "");

			// ── 6 · two live modules do NOT share a folder ─────────────────────────────────
			//
			// Trivial today, because there is one WebModule per module. It stops being trivial
			// the day a single document hosts several: there this assertion is the one that says
			// whether the bridge's routing was done right.
			string rutaDeExamenes = Raiz(examenes);
			string rutaDeCompras = Raiz(compras);
			assert("dos modulos vivos tienen raices de journal distintas",
				!string.Equals(rutaDeExamenes, rutaDeCompras, StringComparison.OrdinalIgnoreCase),
				$"{rutaDeExamenes} == {rutaDeCompras}");

			assert("y ninguna raiz esta adentro de la otra",
				!rutaDeExamenes.StartsWith(rutaDeCompras + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
				&& !rutaDeCompras.StartsWith(rutaDeExamenes + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
				$"{rutaDeExamenes} / {rutaDeCompras}");
		}
		finally
		{
			try { if (Directory.Exists(raiz)) Directory.Delete(raiz, true); } catch { /* temporal */ }
		}
	}

	// `modulo`, `module`, `moduleName`, `nombreDelModulo`: any of those lets the caller choose who
	// is being talked about, which is the one thing the bridge cannot allow.
	private static bool EsNombreDeModulo(string nombre) =>
		nombre.Contains("module", StringComparison.OrdinalIgnoreCase)
		|| nombre.Contains("modulo", StringComparison.OrdinalIgnoreCase);

	private static string Raiz(WebModule m) =>
		(string)typeof(WebModule)
			.GetField("journal", BindingFlags.Instance | BindingFlags.NonPublic)!
			.GetValue(m)!;

	private static bool Rechaza(Action intento)
	{
		try { intento(); return false; }
		catch (UnauthorizedAccessException) { return true; }
		catch (Exception) { return true; }
	}

	private static string Base64(string texto) => Convert.ToBase64String(Encoding.UTF8.GetBytes(texto));

	// THE DOUBLE, and it is the minimum: the five things the platform fills in. None of this runs
	// in the test -- no WebView is opened -- because what is asserted is the NEUTRAL part, which is
	// where the journal's fence lives.
	private sealed class ModuloDePrueba : WebModule
	{
		public ModuloDePrueba(string nombre, string journal)
			: base(nombre, bundle: journal, journal: journal, publisher: "", new SinSecretos(), _ => { })
		{
		}

		protected override Task Create(BundleServer server, string url, CancellationToken ct) => Task.CompletedTask;

		protected override void RunJs(string js) { }

		protected override void Teardown() { }

		protected override bool Alive => true;

		protected override string OriginFor(string basePath) => "https://prueba.local";
	}

	private sealed class SinSecretos : ISecretStore
	{
		public string? Get(string alias) => null;

		public void Save(string alias, string json) { }

		public void Forget(string alias) { }
	}
}
