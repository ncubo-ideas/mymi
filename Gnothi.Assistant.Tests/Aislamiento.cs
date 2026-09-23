using System.Reflection;
using System.Text;
using System.Text.Json;
using Gnothi.MobileShared;

namespace Gnothi.Tests;

// EL AISLAMIENTO ENTRE MODULOS, COMO GUARDIAN Y NO COMO INTENCION.
//
// La separacion de los journals NO la da el origen del navegador: la da el PUENTE, que hoy se ata
// a un WebView y por lo tanto a un modulo. `WebModule.Serve` lo dice sobre `consultar`:
//
//     "IT DOES NOT NAME A MODULE, on purpose. [...] A `module` parameter here would let a face
//      ask about somebody else's journal, and no rule downstream would stop it."
//
// Mientras haya un puente por WebView, esa regla se cumple sola: el llamante no tiene donde
// escribir la mentira. La forma que se esta evaluando -un solo documento del motor web con varios
// modulos adentro (nota 49)- le quita esa muleta: un puente, cinco modulos. Ahi la propiedad pasa
// de ser un hecho de la arquitectura a ser un invariante que hay que sostener con codigo.
//
// Por eso este guardian no espera a esa forma. Afirma HOY las dos mitades que esa forma no puede
// perder:
//
//   1 · que una escritura de un modulo no puede aterrizar en la carpeta de otro, y
//   2 · que ningun verbo del puente acepta el nombre de un modulo como dato del llamante.
//
// La segunda es la que se rompe sin que nadie lo note: agregar un parametro `module` compila,
// queda verde, y recien se ve el dia que un modulo lee el journal del vecino.
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

			// El vecino tiene algo que perder: sin esto, "no lo leyo" no prueba nada.
			Directory.CreateDirectory(Path.Combine(deCompras, "compras"));
			File.WriteAllText(Path.Combine(deCompras, "compras", "meta.bin"), "del vecino");

			// ── 1 · lo legitimo funciona ────────────────────────────────────────────────────
			string suyo = "examenes/journal/journal_000001.bin";
			examenes.Write(suyo, Base64("lo mio"));
			assert("un modulo escribe en SU journal",
				File.Exists(Path.Combine(deExamenes, "examenes", "journal", "journal_000001.bin")),
				$"no aparecio bajo {deExamenes}");

			// ── 2 · subir por la carpeta del vecino ─────────────────────────────────────────
			assert("una ruta que SUBE fuera del journal se rechaza",
				Rechaza(() => examenes.Write("../compras/compras/meta.bin", Base64("pisado"))),
				"Write acepto una ruta con ..");

			assert("y no toco el archivo del vecino",
				File.ReadAllText(Path.Combine(deCompras, "compras", "meta.bin")) == "del vecino",
				"el meta.bin del vecino cambio");

			// La barra invertida es separador en Windows y no la normaliza el reemplazo de '/'.
			assert("una ruta que sube con barra invertida tambien se rechaza",
				Rechaza(() => examenes.Write(@"..\compras\robado.bin", Base64("x"))),
				"Write acepto una ruta con ..\\");

			// ── 3 · nombrar la carpeta del vecino de frente ─────────────────────────────────
			assert("una ruta ABSOLUTA a otro journal se rechaza",
				Rechaza(() => examenes.Write(Path.Combine(deCompras, "robado.bin"), Base64("x"))),
				"Write acepto una ruta absoluta ajena");

			assert("leer del vecino tambien se rechaza",
				Rechaza(() => examenes.Read("../compras/compras/meta.bin")),
				"Read acepto una ruta con ..");

			// ── 4 · lo que un modulo VE al listar es solo lo suyo ───────────────────────────
			string listado = examenes.List();
			assert("al listar, un modulo no ve los archivos del vecino",
				!listado.Contains("compras", StringComparison.OrdinalIgnoreCase),
				listado);

			// ── 5 · LA FORMA DEL PUENTE: ningun verbo recibe el nombre de un modulo ─────────
			//
			// Esta es la que protege la nota 49. Con un puente por WebView el nombre sobra; con
			// un solo documento y varios modulos adentro, agregarlo es la salida obvia -- y es
			// justamente la que el comentario de `Serve` prohibe. Si alguien lo agrega, esto
			// falla el mismo dia.
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

			// Y lo mismo para todo lo que el puente atiende: si una de estas creciera un
			// parametro `module`, el Host veria una pregunta legitima de un modulo legitimo.
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

			// CONTROL POSITIVO. Sin esto, las dos afirmaciones de arriba pasarian igual aunque el
			// detector estuviera roto, y una prueba que no puede fallar no prueba nada. El
			// desalojo SI nombra un modulo -- y esta bien que lo haga, porque lo llama el Host y
			// no el modulo. Eso es exactamente la distincion que el puente sostiene.
			MethodInfo? desalojar = typeof(WebModule).Assembly
				.GetTypes()
				.Where(t => t.Name.StartsWith("WebModuleHost", StringComparison.Ordinal))
				.SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				.FirstOrDefault(m => m.Name == "Evict" && m.GetParameters().Length == 1);

			assert("el detector no es vacio: reconoce el parametro de Evict, que si nombra un modulo",
				desalojar is not null && desalojar.GetParameters().Any(p => EsNombreDeModulo(p.Name ?? "")),
				desalojar is null ? "no se encontro Evict(string)" : desalojar.GetParameters()[0].Name ?? "");

			// ── 6 · dos modulos vivos NO comparten carpeta ─────────────────────────────────
			//
			// Trivial hoy, porque hay un WebModule por modulo. Deja de serlo el dia que un solo
			// documento hospede a varios: ahi esta afirmacion es la que dice si el enrutado del
			// puente se hizo bien.
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

	// `modulo`, `module`, `moduleName`, `nombreDelModulo`: cualquiera de esas deja que el
	// llamante elija de quien habla, que es lo unico que el puente no puede permitir.
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

	// EL DOBLE, y es lo minimo: las cinco cosas que la plataforma llena. Nada de esto se ejecuta
	// en la prueba -- no se abre ningun WebView--, porque lo que se afirma es la parte NEUTRAL,
	// que es donde vive el cerco del journal.
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
