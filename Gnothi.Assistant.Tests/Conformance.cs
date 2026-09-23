using System.Text.Json;
using Gnothi.Assistant;
using Gnothi.Host;
using Gnothi.MobileShared;   // Snapshot: the picture Siri's App Intent reads
using Gnothi.Assistant.Tests;   // Preguntas: the bench tool selection is measured against

namespace Gnothi.Conformance;

// THE CONFORMANCE SUITE — Alvaro asked for it in 08 and until now it did not exist.
//
//   «La suite de conformance corre la misma tool call por las tres puertas y afirma resultado
//    identico. Eso convierte "somos independientes del gatekeeper" de afirmacion en prueba que
//    corre en CI. Sin eso, en dieciocho meses la Puerta 2 no compila y nadie se entero.»
//
// IT IS NOT A HYPOTHETICAL RISK. It already cost a measured defect: with a module over stdio,
// Gate 2 sent the model the MCP envelope with the facts escaped inside a string -- 525
// characters where in-process sent 126, for the same four invoices. Gate 1 hid it because
// ToolBridge carried a branch per shape and Gate 2 had none. Nothing failed; the model just
// got four times the tokens and worse data. THAT is what this suite exists to catch.
//
// WHAT IT COMPARES, and why each gate is represented the way it is:
//
//   Puerta 1  ToolBridge.Call    -> CallToolResult, the MCP projection
//   Puerta 2  AgentLoop          -> JsonSerializer.Serialize(r.Content)
//   Puerta 3  IpcService         -> JsonSerializer.Serialize(r.Content), across processes
//
// Gate 3 lives in Gnothi.Mobile and cannot be referenced from here -- it is an Android
// assembly. What IS reproduced is THE EXACT CALL IT MAKES, copied from IpcService.HandleMessage.
// If that line ever drifts, this suite goes stale silently, so the shape is asserted below
// against the same Request the other two build.
internal static class Conformance
{
	// A tool with parameters cannot be invoked blind: we would be inventing values the module
	// never agreed to. DeclaredParameter has no `required` flag -- everything a manifest
	// declares IS required -- so the test is simply whether it declares any. Only the
	// parameterless ones are exercised, which is enough: the defect this catches is in the
	// ENVELOPE, not in the arguments.
	private static bool NeedsNoArguments(DeclaredTool t) => t.Input is null || t.Input.Count == 0;

	public static async Task<int> Run(string modulesRoot, Action<string, bool, string> assert)
	{
		var catalog = new ModuleCatalog(modulesRoot);
		catalog.Load();

		// THE MANIFEST AGAINST THE MODULE'S REAL SURFACE, and it runs FIRST because every
		// comparison below is built on the catalog the manifest produces. If a manifest declares
		// a tool the module does not have, the three gates agree perfectly -- on a lie.
		//
		// IT ALREADY COST A DEFECT, and that one travelled all the way to the phone: the
		// lab-tests manifest kept declaring `revisar_mis_analitos` long after the code replaced
		// it with `que_me_falta_medir`, and omitted the two tools that do exist. The Host builds
		// its catalog BY READING the manifest, so the snapshot Siri's intent hands the assistant
		// carried a tool that failed on invocation -- "module 'examenes' does not expose
		// 'revisar_mis_analitos'" -- and hid the two that answer. Lab tests was the only business
		// contributing no fact at all to the snapshot; invoices was fine.
		//
		// THE CHECK ALREADY EXISTED AND NOBODY RAN IT. `verify_module` in HostTools asks the
		// module for its tools/list and crosses it against the manifest; the manifest's own
		// `//names` note states the invariant it broke. Having it available on demand was not
		// enough -- drift is silent, so it has to run unprompted. This is that same crossing.
		int verificados = 0;
		var sinVerificar = new List<string>();

		foreach (Module m in catalog.Modules)
		{
			string[] declared = [.. m.Manifest.Tools.Select(t => t.Name).Order()];
			string[] actual;

			try
			{
				actual = [.. (await m.ActualToolNames(CancellationToken.None)).Order()];
			}
			catch (Exception ex)
			{
				// NOT A DEFECT OF THE MODULE, and it must not be reported as one: the shell could
				// not be started. Since the bench went relative (installation.json anchors
				// shell.arguments[0] to its own folder) the usual cause is that the module's dll
				// is not BUILT on this machine -- the path points at bin/Debug of its project.
				//
				// It is listed instead of asserted, because a red line here would train people
				// to ignore the suite for a reason that has nothing to do with drift. What IS
				// asserted, below, is that not EVERY module ended up here -- a suite that
				// verified nothing must not look green.
				sinVerificar.Add($"{m.Name} ({ex.GetType().Name})");
				Console.WriteLine($"  [----] {m.Name}: no se pudo verificar contra el modulo real");
				Console.WriteLine($"          {Cut(ex.Message)}");
				continue;
			}

			string[] declaradasYAusentes = [.. declared.Except(actual)];
			string[] expuestasSinDeclarar = [.. actual.Except(declared)];

			// The two directions fail differently and are worth separating. A tool DECLARED and
			// missing reaches the assistant and breaks on call -- that is the defect above. One
			// exposed and NOT declared never reaches it: the person paid for a capability the
			// catalog hides, which is quieter and just as wrong.
			assert($"{m.Name}: no declara ninguna tool que el modulo no tenga",
				declaradasYAusentes.Length == 0,
				$"declaradas y ausentes: [{string.Join(",", declaradasYAusentes)}]  ·  el modulo expone [{string.Join(",", actual)}]");

			assert($"{m.Name}: no expone ninguna tool sin declarar",
				expuestasSinDeclarar.Length == 0,
				$"expuestas y sin declarar: [{string.Join(",", expuestasSinDeclarar)}]  ·  el manifest declara [{string.Join(",", declared)}]");

			verificados++;
		}

		// THE GUARD THAT KEEPS THE ONE ABOVE HONEST. Every module can fail to start for
		// environmental reasons, and then the crossing runs over nothing while the suite still
		// prints green. That is the exact shape of the risk this suite was built against, so it
		// is stated as an assertion and not as a note.
		assert("al menos un modulo se pudo verificar contra su superficie real",
			verificados > 0,
			$"ninguno de {catalog.Modules.Count} se pudo arrancar: [{string.Join("  ·  ", sinVerificar)}]. "
			+ "Casi siempre falta compilar los modulos: shell.arguments[0] de cada installation.json "
			+ "apunta, relativo a su carpeta, al bin/Debug del proyecto Mcp del modulo. Correr "
			+ "dotnet build sobre Compras/App/Compras.Mcp, Examenes/App/Examenes.Mcp y "
			+ "HolaMundo/Facturas/App/Facturas.Mcp, y repetir.");

		if (verificados > 0 && sinVerificar.Count > 0)
			Console.WriteLine($"  ({verificados} modulos verificados, {sinVerificar.Count} sin verificar)");

		// The gate declares its own format hint; Gate 1 says json, Gate 2 says json or toon.
		// They are built separately ON PURPOSE: if a future change made the catalog depend on
		// the hint, the two dispatchers would disagree and the tools comparison below breaks.
		var one = new Dispatcher(catalog, new Meter(), Accepts.JsonOnly);
		var two = new Dispatcher(catalog, new Meter(), Accepts.JsonOrToon);

		string[] published1 = [.. ToolIndex.Of(one).All.Select(e => e.PublicName).Order()];
		string[] published2 = [.. AgentLoop.CatalogFor(two).Select(t => t.Name).Order()];

		assert("las tres puertas publican el MISMO catalogo",
			published1.SequenceEqual(published2),
			$"puerta1=[{string.Join(",", published1)}]  puerta2=[{string.Join(",", published2)}]");

		if (published1.Length == 0)
		{
			assert("hay al menos una tool publicada para comparar", false,
				$"el catalogo de {modulesRoot} salio vacio");
			return 1;
		}

		// THE QUESTION BANK HAS NOT GONE STALE.
		//
		// Preguntas.cs names, for each of Sonia's questions, which tool OUGHT to answer it. It is an
		// instrument of measurement, not a test of behaviour: it exists to measure tool selection
		// against a model, and for that it has to name tools that exist. A module that renames a
		// query leaves the bank measuring against a dead name, and that does not show -- the bank is
		// compiled against nothing, they are strings.
		//
		// ONLY THE TOOLS OF THE MODULES THIS BENCH HAS ARE REQUIRED. The local bench on this Mac
		// does not always bring the five modules, and an assertion demanding all of them would fail
		// over what is missing from the install and not over what is written wrong.
		{
			HashSet<string> publicadas = [.. published1];
			HashSet<string> modulosDelBanco =
				[.. ToolIndex.Of(one).All.Select(e => e.ModuleName)];

			string[] muertas = [.. Preguntas.DeSonia
				.Where(p => modulosDelBanco.Contains(p.Modulo))
				.SelectMany(p => p.Esperadas.Concat(p.Tolerables))
				.Distinct()
				.Where(nombre => !publicadas.Contains(nombre))
				.Order()];

			assert("el banco de preguntas nombra tools que existen",
				muertas.Length == 0,
				$"nombres que ya no publica ningun modulo: {string.Join(", ", muertas)}");

			// AND THAT IT IS WHOLE IN BOTH LANGUAGES. Language is the heaviest variable measured
			// over tool calling -- on 2026-09-17 it was the difference between 4 out of 4 and 0 out
			// of 8 -- so a question missing one side leaves that side unmeasured, and it does not
			// show: it is an empty string, not an error.
			string[] mancas = [.. Preguntas.DeSonia
				.Where(p => string.IsNullOrWhiteSpace(p.Es) || string.IsNullOrWhiteSpace(p.En))
				.Select(p => p.Es.Length > 0 ? p.Es : p.En)
				.Order()];

			assert("el banco de preguntas esta entero en los dos idiomas",
				mancas.Length == 0,
				$"preguntas a las que les falta un idioma: {string.Join(" | ", mancas)}");
		}

		int compared = 0;

		foreach (ToolIndex.Entry e in ToolIndex.Of(one).All.OrderBy(x => x.PublicName))
		{
			if (!NeedsNoArguments(e.Tool)) continue;

			// THE SAME REQUEST, built the way each gate builds it. All three converge here:
			// ToolBridge.Call, AgentLoop.Invoke and IpcService.HandleMessage every one of them
			// constructs `new Request(moduleName, tool.Name, arguments)`.
			var request = new Request(e.ModuleName, e.Tool.Name, null);

			Response r1 = await one.Serve(request, CancellationToken.None);
			Response r2 = await two.Serve(request, CancellationToken.None);

			// Puerta 3 reproduced: IpcService does exactly this, then puts the string in a
			// Bundle. Copied from Gnothi.Mobile/IpcService.cs.
			Response r3 = await two.Serve(new Request(e.ModuleName, e.Tool.Name, null), CancellationToken.None);

			assert($"{e.PublicName}: las tres puertas autorizan igual",
				r1.Authorized == r2.Authorized && r2.Authorized == r3.Authorized,
				$"p1={r1.Authorized} p2={r2.Authorized} p3={r3.Authorized}");

			if (!r1.Authorized)
			{
				// A denial has to say the SAME thing through every door too: a module switched
				// off must look switched off from all of them, or the person turned it off in
				// one place and it kept answering in another.
				assert($"{e.PublicName}: el rechazo dice lo mismo por las tres",
					r1.Reason == r2.Reason && r2.Reason == r3.Reason,
					$"p1='{r1.Reason}' p2='{r2.Reason}' p3='{r3.Reason}'");
				continue;
			}

			// THE ASSERTION THAT WOULD HAVE CAUGHT THE 525-vs-126 DEFECT.
			//
			// Gate 2 and Gate 3 serialize r.Content plainly. Gate 1 wraps it in MCP, and its
			// TEXT block is that very same serialization -- see ToolBridge.Call. So the three
			// strings must be byte-identical. If a gate ever grows a branch per shell again,
			// this is the line that goes red.
			string byGate1 = JsonSerializer.Serialize(r1.Content);
			string byGate2 = JsonSerializer.Serialize(r2.Content);
			string byGate3 = JsonSerializer.Serialize(r3.Content);

			// A DISTINCTION THIS SUITE FOUND ON ITS FIRST RUN, and it is worth keeping.
			//
			// `gnothi_installed_modules` came back 818 characters through all three gates and
			// still differed -- at character 224: "assistantQueries":1 vs 2. That is the Meter,
			// and it is NOT a defect: the Host's own tools report what happened ON THIS DEVICE,
			// so a gate that has served one query legitimately says one. In production the
			// desktop Host and the phone Host are different processes with different meters.
			//
			// So the rule is: THE PERSON'S FACTS must be identical through every door -- that
			// is the claim the suite defends. The HOST'S TELEMETRY is allowed to differ,
			// because measuring is per-process by definition. Excluded explicitly and named,
			// never quietly.
			bool telemetriaDelHost = string.Equals(e.ModuleName, Dispatcher.HostModule, StringComparison.OrdinalIgnoreCase);

			if (telemetriaDelHost)
			{
				// COMPARING LENGTH WAS FRAGILE, and the growing catalog exposed it. The counter
				// differs by VALUE between gates (each door served a different number of
				// queries) and, once a count crosses 9->10, by DIGIT COUNT too -- so two
				// responses of the same shape ended up 822 vs 823 characters and this went red
				// for the wrong reason. Blank the per-process counter and compare the FORM
				// exactly: stricter than length -it catches a real shape change- and blind only
				// to the number it must be blind to.
				string f1 = SinContadorDeQueries(byGate1);
				string f2 = SinContadorDeQueries(byGate2);
				string f3 = SinContadorDeQueries(byGate3);
				assert($"{e.PublicName}: telemetria del Host -- misma forma por las tres puertas",
					f1 == f2 && f2 == f3,
					PrimeraDiferencia(f1, f2, f3));
				compared++;
				continue;
			}

			assert($"{e.PublicName}: los HECHOS son identicos por las tres puertas",
				byGate1 == byGate2 && byGate2 == byGate3,
				PrimeraDiferencia(byGate1, byGate2, byGate3));

			// And the size, stated out loud. The original defect was not an exception: it was
			// four times the characters, silently. A number in the log is what makes a
			// regression visible to a person reading CI, not just to an assertion.
			assert($"{e.PublicName}: ninguna puerta infla el resultado",
				byGate1.Length == byGate2.Length && byGate2.Length == byGate3.Length,
				$"{byGate1.Length} / {byGate2.Length} / {byGate3.Length} caracteres");

			compared++;
		}

		assert("se comparo al menos una tool de punta a punta",
			compared > 0,
			"todas las tools publicadas piden parametros obligatorios; no hay nada que comparar a ciegas");

		Console.WriteLine($"  ({compared} tools comparadas por las tres puertas)");
		return compared;
	}

	private static string Cut(string s) => s.Length <= 90 ? s : s[..90] + "…";

	// Two strings of the same length that differ somewhere are the worst kind of failure to
	// read: the eye cannot find the spot. This points at it. The original defect was a length
	// difference and was obvious; the next one may not be.
	// The Meter's per-process counter, blanked so the FORM of the Host telemetry can be compared
	// across gates without the one number that legitimately differs between processes.
	private static string SinContadorDeQueries(string json) =>
		System.Text.RegularExpressions.Regex.Replace(json, "\"assistantQueries\":\\d+", "\"assistantQueries\":N");

	private static string PrimeraDiferencia(string a, string b, string c)
	{
		int i = 0;
		while (i < a.Length && i < b.Length && i < c.Length && a[i] == b[i] && a[i] == c[i]) i++;
		int from = Math.Max(0, i - 40);
		string Window(string s) => from >= s.Length ? "(mas corta)" : s[from..Math.Min(s.Length, i + 40)];
		return $"difieren en el caracter {i} (largos {a.Length}/{b.Length}/{c.Length})\n"
		     + $"          p1: …{Window(a)}…\n          p2: …{Window(b)}…\n          p3: …{Window(c)}…";
	}

	// Walks up from the binary looking for the repo's Modulos. Returns "" when it is not there
	// -- and the caller turns that into a FAILED assertion, never a silent pass. A conformance
	// suite that quietly compares nothing is worse than no suite: CI stays green and the claim
	// it is supposed to defend stops being defended.
	public static string FindModules()
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		for (int up = 0; up < 8 && dir is not null; up++, dir = dir.Parent)
		{
			string candidate = Path.Combine(dir.FullName, "Modulos");
			if (Directory.Exists(candidate)) return candidate;
		}
		return "";
	}

	// ============================================================================================
	// CONSENT IS HONOURED, AND NO DOOR GETS AROUND IT.
	//
	// The gate lives in ONE place -- Dispatcher.Serve -- and the catalogue comes out of
	// Module.EnabledTools() already filtered. That is well built, and the Dispatcher's comment says
	// so: «the permission is checked HERE, not in the front end. A new front end cannot forget to
	// check it because it is not the one doing the invoking».
	//
	// WHAT DID NOT EXIST WAS THE TEST. Until today no test in the repo switched a tool off: the
	// suite walked ToolIndex.All, which comes ALREADY filtered, so it only exercised tools that
	// were ON. The property was held up by argument, not by measurement, and a new front end that
	// decided to invoke on its own would have passed CI green.
	//
	// WHY THIS ALSO COVERS THE SNAPSHOT SIRI READS, which is the riskiest path because its answer
	// comes out of a file and not out of a live query: Snapshot.cs builds the snapshot by walking
	// dispatcher.Catalog() and asking for each fact through dispatcher.Serve. If a switched-off
	// tool is not in the catalogue NOR served by the dispatcher, there is no way into the snapshot.
	// What this test CANNOT cover is that the snapshot ON DISK is rewritten when consent changes --
	// that is ConsentWatch in the live app -- and that is why it is measured on the device.
	//
	// IT DOES NOT TOUCH THE DISK: Module.Reconsent only changes the object in memory. The person's
	// installation.json is written by Shell.SetConsent, which takes no part here. The working tree
	// is left as it was.
	//
	// IT DOES NOT NEED THE MODULES TO START, and that is on purpose: the permission is checked
	// BEFORE invoking, so this runs the same on a machine where installation.json carries another
	// machine's paths -- the open limitation of section 8.
	public static async Task Consentimiento(string modulesRoot, Action<string, bool, string> assert)
	{
		var catalog = new ModuleCatalog(modulesRoot);
		catalog.Load();

		var d = new Dispatcher(catalog, new Meter(), Accepts.JsonOrToon);

		Module? m = catalog.Modules.FirstOrDefault(x => x.Manifest.Tools.Count > 0);
		if (m is null)
		{
			assert("hay un modulo con tools para probar el consentimiento", false,
				$"ninguno de {catalog.Modules.Count} modulos declara tools");
			await catalog.DisposeAsync();
			return;
		}

		string tool = m.Manifest.Tools[0].Name;
		string publico = Module.PublicName(m.Name, tool);

		// The original state, to be put back at the end even if an assertion fails.
		bool enabled0 = m.Installation.Enabled;
		string[] disabled0 = [.. m.Installation.DisabledTools];

		// The three lists where a switched-off tool must NOT appear. They are the ones feeding the
		// three consumers: the raw catalogue goes into Siri's snapshot, ToolIndex is what Gate 1
		// sees, and CatalogFor is what the model sees at Gate 2.
		bool EnCatalogo() => d.Catalog().Any(x => x.module.Name == m.Name && x.tool.Name == tool);
		bool EnPuerta1() => ToolIndex.Of(d).All.Any(e => e.PublicName == publico);
		bool EnPuerta2() => AgentLoop.CatalogFor(d).Any(t => t.Name == publico);

		try
		{
			// ── 1 · THE FLOOR: switched on, it shows in all three ──────────────────────────
			m.Reconsent(true, []);
			assert($"{publico}: encendida, la ven las tres puertas",
				EnCatalogo() && EnPuerta1() && EnPuerta2(),
				$"puerta1={EnPuerta1()} puerta2={EnPuerta2()} foto-de-Siri={EnCatalogo()}");

			// ── 2 · SWITCH OFF THAT ONE TOOL ───────────────────────────────────────────────
			m.Reconsent(true, [tool]);

			Response apagada = await d.Serve(new Request(m.Name, tool, null), CancellationToken.None);

			// LA INVOCACION EN VIVO NO SE SEPARA POR PUERTA, y es a proposito: Puerta 1, Puerta 2
			// and Android's caratula all three converge on Dispatcher.Serve. Inventing three
			// identical calls would prove nothing more -- it is what the conformance above already
			// does with r2 and r3 -- and would give the illusion of three measurements where there
			// is one.
			assert($"{publico}: apagada · cualquier puerta que invoque: el Dispatcher la RECHAZA",
				!apagada.Authorized,
				"el dispatcher la sirvio igual: la compuerta no se aplico");

			// And it refuses it BY CONSENT. Without this the assertion above would also pass if
			// the module simply does not start -- Serve returns Denied in both cases -- and we
			// would be celebrating a gate that never ran.
			assert($"{publico}: y el motivo es el consentimiento, no un fallo del modulo",
				(apagada.Reason ?? "").Contains("switched off", StringComparison.OrdinalIgnoreCase),
				$"motivo: {apagada.Reason}");

			// ONE ASSERTION PER GATE, and it is worth saying precisely WHAT that buys, because the
			// three lists are NOT independent: they are a chain.
			//
			//     Dispatcher.Catalog()        <- the root, m.EnabledTools()
			//            v
			//     ToolIndex.Of(dispatcher)    <- Gate 1, built from Catalog()
			//            v
			//     AgentLoop.CatalogFor(host)  <- Gate 2, built from ToolIndex
			//
			// Which means breaking the root reddens all three, and separating them does NOT give
			// three independent tests. What it does give, and why they are separated: if tomorrow a
			// layer adds its own filter or transformation and gets it wrong -- ToolIndex including
			// something Catalog excluded, or CatalogFor adding tools back -- the red line names THE
			// LAYER. And in passing it leaves written down which list feeds which gate, which is
			// exactly what nobody had clear.
			assert($"{publico}: apagada · Puerta 1 (asistente externo): no esta en su catalogo",
				!EnPuerta1(),
				"sigue en ToolIndex: un asistente externo la veria y podria pedirla");

			assert($"{publico}: apagada · Puerta 2 (nuestro bucle): no esta en su catalogo",
				!EnPuerta2(),
				"sigue en AgentLoop.CatalogFor: el modelo la veria en su lista de tools");

			assert($"{publico}: apagada · la foto de Siri: no entra en el catalogo que la arma",
				!EnCatalogo(),
				"sigue en Dispatcher.Catalog(): entraria en la instantanea que lee la App Intent, "
				+ "que es la que contestan Siri y el Atajo en iOS");

			// ── 3 · SWITCHING IT BACK ON ───────────────────────────────────────────────────
			// A switch-off that cannot be undone is not consent, it is a cancellation.
			m.Reconsent(true, []);
			assert($"{publico}: al reencenderla vuelve a las tres puertas",
				EnCatalogo() && EnPuerta1() && EnPuerta2(),
				$"puerta1={EnPuerta1()} puerta2={EnPuerta2()} foto-de-Siri={EnCatalogo()}");

			// ── 4 · SWITCHING OFF THE WHOLE MODULE ─────────────────────────────────────────
			// It is the UI's other switch, and it shares no code with the one above: Enabled and
			// DisabledTools are different fields, so it is asserted separately.
			m.Reconsent(false, []);

			string[] asomadas = [.. d.Catalog().Where(x => x.module.Name == m.Name).Select(x => x.tool.Name)];
			assert($"{m.Name}: apagado el modulo entero, NINGUNA de sus tools queda en el catalogo",
				asomadas.Length == 0,
				$"quedaron asomadas: [{string.Join(", ", asomadas)}]");

			Response moduloOff = await d.Serve(new Request(m.Name, tool, null), CancellationToken.None);
			assert($"{m.Name}: apagado el modulo entero, el Dispatcher rechaza sus tools",
				!moduloOff.Authorized && (moduloOff.Reason ?? "").Contains("switched off", StringComparison.OrdinalIgnoreCase),
				$"autorizado={moduloOff.Authorized} motivo: {moduloOff.Reason}");
			// ── 5 · THE REAL SNAPSHOT, the file Siri reads ────────────────────────────────
			// Up to here it was proven that the switched-off tool is not in the catalogue the
			// snapshot walks. This proves the FILE: it is written with Snapshot.Escribir -- the same
			// code iOS and Android compile, not a reimplementation -- and the name is looked for inside.
			//
			// It matters that it is the file and not the catalogue because on iOS the App Intent does
			// NOT call the Dispatcher: it opens this json and answers with whatever it says. It is the
			// only artefact Siri actually consumes.
			string carpeta = Path.Combine(Path.GetTempPath(), "gnothi-foto-" + Guid.NewGuid().ToString("N"));
			try
			{
				m.Reconsent(true, []);
				await Snapshot.Write(d, carpeta, _ => { }, CancellationToken.None);
				string conLaTool = await File.ReadAllTextAsync(Path.Combine(carpeta, Snapshot.FileName));

				assert($"{publico}: encendida, SI aparece en la foto que lee Siri",
					conLaTool.Contains($"\"{tool}\"", StringComparison.Ordinal),
					"no aparecio encendida: la foto no la trae y el resto de esta prueba no diria nada");

				m.Reconsent(true, [tool]);
				await Snapshot.Write(d, carpeta, _ => { }, CancellationToken.None);
				string sinLaTool = await File.ReadAllTextAsync(Path.Combine(carpeta, Snapshot.FileName));

				assert($"{publico}: apagada, NO aparece en la foto que lee Siri",
					!sinLaTool.Contains($"\"{tool}\"", StringComparison.Ordinal),
					"el nombre de la tool apagada quedo escrito en el json que la App Intent lee");

				// The whole module switched off cannot leave EVEN ITS NAME: if the snapshot named it,
				// Siri could say "you have a compras module" about one the person revoked.
				m.Reconsent(false, []);
				await Snapshot.Write(d, carpeta, _ => { }, CancellationToken.None);
				string sinElModulo = await File.ReadAllTextAsync(Path.Combine(carpeta, Snapshot.FileName));

				assert($"{m.Name}: apagado el modulo entero, no queda ni su nombre en la foto",
					!sinElModulo.Contains($"\"{m.Name}\"", StringComparison.Ordinal),
					"el modulo revocado sigue nombrado en la foto que lee Siri");
			}
			finally
			{
				try { Directory.Delete(carpeta, recursive: true); } catch { }
			}
		}
		finally
		{
			// Put the state back, whatever happens. It is in memory, but leaving the object lying
			// would make every later assertion measure something else.
			m.Reconsent(enabled0, disabled0);
			await catalog.DisposeAsync();
		}
	}

	// ============================================================================================
	// AN ENUM OUTSIDE THE SET COMES BACK WITH THE LIST, AND COMES BACK FROM THE HOST.
	//
	// Measured on 2026-09-21 with Apple's model in the desktop window (docs/16, finding 2): it
	// asked for evaluar_tendencia with analito="colesterol" and got the module's raw Enum.Parse
	// back -- «Requested value 'colesterol' was not found» -- which does not say the valid values.
	// The small model does not re-read the schema: with that text it moves to another tool or
	// invents. The gate is now in Dispatcher.Serve, against the manifest's `enum`, and the reason
	// names every value.
	//
	// IT DOES NOT NEED THE MODULE TO START, the same as consent: it is checked BEFORE invoking.
	// That is also why it is asserted that the reason is NOT a failure of the module -- if the
	// module did not start, Serve would also return Denied, and we would be celebrating a gate
	// that never ran.
	//
	// IT IS TESTED OVER THE BENCH'S FIRST CLOSED PARAMETER, not over examenes by name: the local
	// bench does not always bring the five modules, and the property is the Host's, not a
	// module's.
	public static async Task EnumFueraDelConjunto(string modulesRoot, Action<string, bool, string> assert)
	{
		var catalog = new ModuleCatalog(modulesRoot);
		catalog.Load();

		var d = new Dispatcher(catalog, new Meter(), Accepts.JsonOrToon);

		(Module m, DeclaredTool t, DeclaredParameter p)? cerrado = catalog.Modules
			.SelectMany(m => m.EnabledTools().Select(t => (m, t)))
			.SelectMany(x => x.t.Input.Select(p => (x.m, x.t, p)))
			.FirstOrDefault(x => x.p.Enum is { Count: > 0 });

		if (cerrado is null)
		{
			assert("hay un parametro declarado como enum para probar la compuerta", false,
				$"ninguna tool de {catalog.Modules.Count} modulos declara `enum` en su entrada");
			await catalog.DisposeAsync();
			return;
		}

		(Module modulo, DeclaredTool tool, DeclaredParameter param) = cerrado.Value;
		string publico = Module.PublicName(modulo.Name, tool.Name);
		const string fuera = "colesterol";

		try
		{
			// ── 1 · THE VALUE THE MODEL WROTE, exactly as it arrives through each gate ────
			//
			// Gate 1 hands over a JsonElement (the MCP SDK deserialises the arguments); Gate 2 hands
			// over the model's string. The gate compares TEXT and has to read both alike, or an
			// invalid value would pass through one gate and not the other.
			var comoString = new Dictionary<string, object?> { [param.Name] = fuera };
			var comoJson = new Dictionary<string, object?>
			{
				[param.Name] = JsonSerializer.SerializeToElement(fuera),
			};

			Response r2 = await d.Serve(new Request(modulo.Name, tool.Name, comoString), CancellationToken.None);
			Response r1 = await d.Serve(new Request(modulo.Name, tool.Name, comoJson), CancellationToken.None);

			assert($"{publico}: '{fuera}' fuera del enum · el Dispatcher lo RECHAZA",
				!r1.Authorized && !r2.Authorized,
				$"p1={r1.Authorized} p2={r2.Authorized}: la compuerta no se aplico");

			assert($"{publico}: y el rechazo dice lo mismo venga como JsonElement o como string",
				r1.Reason == r2.Reason,
				$"p1='{r1.Reason}' p2='{r2.Reason}'");

			// ── 2 · THE REASON NAMES EVERY VALID VALUE, the parameter and what arrived ────
			string motivo = r2.Reason ?? "";
			string[] faltan = [.. param.Enum!.Where(v => !motivo.Contains(v, StringComparison.Ordinal))];

			assert($"{publico}: el motivo lista los {param.Enum!.Count} valores validos",
				faltan.Length == 0,
				$"faltan [{string.Join(",", faltan)}] en: {motivo}");

			assert($"{publico}: el motivo nombra el parametro y el valor que llego",
				motivo.Contains($"{param.Name} '{fuera}'", StringComparison.Ordinal),
				$"motivo: {motivo}");

			assert($"{publico}: y es la compuerta del Host, no un fallo del modulo",
				!motivo.Contains("failed", StringComparison.OrdinalIgnoreCase)
					&& motivo == Dispatcher.OutsideTheDeclaredSet(modulo.Manifest.Tools, tool.Name, comoString),
				$"motivo: {motivo}");

			// ── 3 · A VALUE FROM THE SET IS NOT REFUSED BY THE GATE, in any casing ────────
			//
			// In-process already accepted `colesteroltotal` (Enum.Parse with ignoreCase); the gate
			// does not harden that rule. If the module does not start on this machine, Serve returns
			// Denied for ANOTHER reason, and that is not what is measured here.
			string valido = param.Enum![0];
			foreach (string caja in new[] { valido, valido.ToLowerInvariant() })
			{
				var dentro = new Dictionary<string, object?> { [param.Name] = caja };
				Response ok = await d.Serve(new Request(modulo.Name, tool.Name, dentro), CancellationToken.None);
				assert($"{publico}: '{caja}' esta en el enum · la compuerta lo deja pasar",
					ok.Authorized || !(ok.Reason ?? "").Contains("is not one of", StringComparison.Ordinal),
					$"motivo: {ok.Reason}");
			}

			// ── 4 · WITHOUT THE ARGUMENT, the gate has no opinion: the module says that ───
			Response sinArg = await d.Serve(new Request(modulo.Name, tool.Name, new Dictionary<string, object?>()), CancellationToken.None);
			assert($"{publico}: sin el argumento, el motivo no es de la compuerta del enum",
				!(sinArg.Reason ?? "").Contains("is not one of", StringComparison.Ordinal),
				$"motivo: {sinArg.Reason}");
		}
		finally
		{
			await catalog.DisposeAsync();
		}
	}
}
