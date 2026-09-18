using System.Text.Json;
using Gnothi.Assistant;
using Gnothi.Host;
using Gnothi.MobileShared;   // Snapshot: la foto que lee la App Intent de Siri

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
	// EL CONSENTIMIENTO SE RESPETA, Y NINGUNA PUERTA LO ESQUIVA.
	//
	// La compuerta vive en UN solo lugar -- Dispatcher.Serve -- y el catalogo sale ya filtrado de
	// Module.EnabledTools(). Eso esta bien construido, y el comentario del Dispatcher lo dice:
	// «the permission is checked HERE, not in the front end. A new front end cannot forget to
	// check it because it is not the one doing the invoking».
	//
	// LO QUE NO EXISTIA ERA LA PRUEBA. Hasta hoy ningun test del repo apagaba una tool: la suite
	// recorria ToolIndex.All, que YA viene filtrado, asi que solo ejercitaba tools ENCENDIDAS. La
	// propiedad se sostenia por argumento, no por medicion, y un front end nuevo que decidiera
	// invocar por su cuenta habria pasado el CI en verde.
	//
	// POR QUE ESTO CUBRE TAMBIEN LA FOTO QUE LEE SIRI, que es el camino mas riesgoso porque su
	// respuesta sale de un archivo y no de una consulta en vivo: Snapshot.cs arma la foto
	// recorriendo dispatcher.Catalog() y pidiendo cada hecho por dispatcher.Serve. Si una tool
	// apagada no aparece en el catalogo NI la sirve el dispatcher, no hay por donde entrar en la
	// foto. Lo que esta prueba NO puede cubrir es que la foto EN DISCO se reescriba cuando el
	// consentimiento cambia -- eso es ConsentWatch en la app viva -- y por eso se mide en el aparato.
	//
	// NO TOCA EL DISCO: Module.Reconsent solo cambia el objeto en memoria. El installation.json de
	// la persona lo escribe Shell.SetConsent, que no participa aca. El working tree queda igual.
	//
	// NO NECESITA QUE LOS MODULOS ARRANQUEN, y es a proposito: el permiso se chequea ANTES de
	// invocar, asi que esto corre igual en una maquina donde installation.json trae rutas de otra
	// -- la limitacion abierta de la seccion 8 --.
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

		// El estado original, para devolverlo al final aunque una afirmacion falle.
		bool enabled0 = m.Installation.Enabled;
		string[] disabled0 = [.. m.Installation.DisabledTools];

		// Las tres listas donde una tool apagada NO debe aparecer. Son las que alimentan a los
		// tres consumidores: el catalogo crudo va a la foto de Siri, ToolIndex es lo que ve la
		// Puerta 1, y CatalogFor es lo que ve el modelo en la Puerta 2.
		bool EnCatalogo() => d.Catalog().Any(x => x.module.Name == m.Name && x.tool.Name == tool);
		bool EnPuerta1() => ToolIndex.Of(d).All.Any(e => e.PublicName == publico);
		bool EnPuerta2() => AgentLoop.CatalogFor(d).Any(t => t.Name == publico);

		try
		{
			// ── 1 · EL PISO: encendida, aparece en las tres ────────────────────────────────
			m.Reconsent(true, []);
			assert($"{publico}: encendida, la ven las tres puertas",
				EnCatalogo() && EnPuerta1() && EnPuerta2(),
				$"puerta1={EnPuerta1()} puerta2={EnPuerta2()} foto-de-Siri={EnCatalogo()}");

			// ── 2 · APAGAR SOLO ESA TOOL ───────────────────────────────────────────────────
			m.Reconsent(true, [tool]);

			Response apagada = await d.Serve(new Request(m.Name, tool, null), CancellationToken.None);

			// LA INVOCACION EN VIVO NO SE SEPARA POR PUERTA, y es a proposito: Puerta 1, Puerta 2
			// y la caratula de Android convergen las tres en Dispatcher.Serve. Inventar tres
			// llamadas identicas no probaria nada mas -- es lo que ya hace la conformance de
			// arriba con r2 y r3 -- y daria la ilusion de tres mediciones donde hay una.
			assert($"{publico}: apagada · cualquier puerta que invoque: el Dispatcher la RECHAZA",
				!apagada.Authorized,
				"el dispatcher la sirvio igual: la compuerta no se aplico");

			// Y la rechaza POR CONSENTIMIENTO. Sin esto la afirmacion de arriba tambien pasaria
			// si el modulo simplemente no arranca -- Serve devuelve Denied en los dos casos --,
			// y estariamos festejando una compuerta que nunca se ejecuto.
			assert($"{publico}: y el motivo es el consentimiento, no un fallo del modulo",
				(apagada.Reason ?? "").Contains("switched off", StringComparison.OrdinalIgnoreCase),
				$"motivo: {apagada.Reason}");

			// UNA AFIRMACION POR PUERTA, y conviene decir con precision QUE compra eso, porque
			// las tres listas NO son independientes: son una cadena.
			//
			//     Dispatcher.Catalog()        <- la raiz, m.EnabledTools()
			//            v
			//     ToolIndex.Of(dispatcher)    <- Puerta 1, construido desde Catalog()
			//            v
			//     AgentLoop.CatalogFor(host)  <- Puerta 2, construido desde ToolIndex
			//
			// O sea que romper la raiz enrojece las tres, y separarlas NO da tres pruebas
			// independientes. Lo que si da, y por eso se separan: si manana una capa agrega su
			// propio filtro o transformacion y se equivoca -- ToolIndex incluyendo algo que
			// Catalog excluyo, o CatalogFor volviendo a sumar tools -- la linea roja nombra LA
			// CAPA. Y de paso deja escrito que lista alimenta a que puerta, que es justo lo que
			// nadie tenia claro.
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

			// ── 3 · VOLVER A ENCENDERLA ────────────────────────────────────────────────────
			// Un apagado que no se puede deshacer no es consentimiento, es una baja.
			m.Reconsent(true, []);
			assert($"{publico}: al reencenderla vuelve a las tres puertas",
				EnCatalogo() && EnPuerta1() && EnPuerta2(),
				$"puerta1={EnPuerta1()} puerta2={EnPuerta2()} foto-de-Siri={EnCatalogo()}");

			// ── 4 · APAGAR EL MODULO ENTERO ────────────────────────────────────────────────
			// Es el otro switch de la UI, y no comparte codigo con el de arriba: Enabled y
			// DisabledTools son campos distintos, asi que se afirma aparte.
			m.Reconsent(false, []);

			string[] asomadas = [.. d.Catalog().Where(x => x.module.Name == m.Name).Select(x => x.tool.Name)];
			assert($"{m.Name}: apagado el modulo entero, NINGUNA de sus tools queda en el catalogo",
				asomadas.Length == 0,
				$"quedaron asomadas: [{string.Join(", ", asomadas)}]");

			Response moduloOff = await d.Serve(new Request(m.Name, tool, null), CancellationToken.None);
			assert($"{m.Name}: apagado el modulo entero, el Dispatcher rechaza sus tools",
				!moduloOff.Authorized && (moduloOff.Reason ?? "").Contains("switched off", StringComparison.OrdinalIgnoreCase),
				$"autorizado={moduloOff.Authorized} motivo: {moduloOff.Reason}");
			// ── 5 · LA FOTO DE VERDAD, el archivo que Siri lee ─────────────────────────────
			// Hasta aca se probo que la tool apagada no esta en el catalogo que la foto recorre.
			// Esto prueba el ARCHIVO: se escribe con Snapshot.Escribir -- el mismo codigo que
			// compilan iOS y Android, no una reimplementacion -- y se busca el nombre adentro.
			//
			// Importa que sea el archivo y no el catalogo porque en iOS la App Intent NO llama al
			// Dispatcher: abre este json y contesta con lo que diga. Es el unico artefacto que
			// Siri realmente consume.
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

				// El modulo entero apagado no puede dejar NI SU NOMBRE: si la foto lo nombrara,
				// Siri podria decir "tenes un modulo de compras" de uno que la persona revoco.
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
			// Devolver el estado, pase lo que pase. Es en memoria, pero dejar el objeto mentido
			// haria que cualquier afirmacion posterior midiera otra cosa.
			m.Reconsent(enabled0, disabled0);
			await catalog.DisposeAsync();
		}
	}
}
