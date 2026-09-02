using System.Text.Json;
using Gnothi.Assistant;
using Gnothi.Host;

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
}
