using Gnothi.Assistant;
using Gnothi.Host;

namespace Gnothi.MobileShared;

// THE SNAPSHOT THE HOST LEAVES FOR THE CARATULA.
//
// THE PROBLEM IT SOLVES, in one line: on a phone the only assistant that can talk to an app is the
// system's, and on iOS that assistant only comes in through App Intents, which only exist in Swift.
// The Host is in .NET. The two cannot touch each other directly without paying for NativeAOT and
// losing dynamic module loading (NotasGnothiHost/33).
//
// So they do not touch: the Host WRITES DOWN what the caratula is going to need, and the caratula
// reads it.
//
//     Gnothi.iOS runs         ->  writes this snapshot into the shared container
//                                    |
//     Siri: "how much did I spend"   |
//                                    v
//              App Intent (Swift)  ->  reads the snapshot  ->  answers
//
// WHY IT IS POSSIBLE. Both apps are OURS, meaning the same Team ID, which is exactly what App Groups
// requires. It is the reason this does NOT work for an institution shipping its own caratula --
// there the Team ID is somebody else's -- and does work between our two.
//
// THE PRICE, said plainly: the answer comes from a snapshot, not live. Siri answers with whatever
// the Host left the last time the app ran.
//
// And that is NOT a new compromise. The instructions the module already gives the assistant say
// verbatim: «if the total did not change, ask them to open their app and hit Refresh». The journal
// only advances when the app runs. The snapshot repeats a limit that already existed; it does not
// add one.
//
// CONSENT IS RESPECTED BY CONSTRUCTION, and this was the hole that had to be closed. The snapshot is
// assembled by walking Dispatcher.Catalog(), which ALREADY comes filtered by what the person
// authorised, and each fact is requested through Dispatcher.Serve, which checks the permission
// again. Which means: nothing the assistant had no right to see can land here. And since it is
// rewritten whole every time consent changes, switching a module off ERASES it from the snapshot --
// no stale snapshot is left answering on its behalf.
public static class Snapshot
{
	/// The name is fixed and both sides know it. If it changes, it changes in Swift too.
	public const string FileName = "gnothi-instantanea.json";

	/// <param name="carpeta">
	/// The shared container. The platform picks it: on iOS it comes from
	/// NSFileManager.GetContainerUrl of the App Group. On Android it is null and this does not run --
	/// over there the caratula talks over binder, live, and needs no snapshot.
	/// </param>
	public static async Task Write(Dispatcher dispatcher, string carpeta, Action<string> log, CancellationToken ct)
	{
		try
		{
			Directory.CreateDirectory(carpeta);

			List<object?> modulos = [];

			foreach (IGrouping<Module, (Module module, DeclaredTool tool)> porModulo in
				dispatcher.Catalog().GroupBy(x => x.module))
			{
				Module m = porModulo.Key;
				List<object?> tools = [];

				foreach ((_, DeclaredTool t) in porModulo)
				{
					Dictionary<string, object?> declarada = new()
					{
						["nombre"] = t.Name,
						["publico"] = Module.PublicName(m.Name, t.Name),
						["titulo"] = t.Title,
						["descripcion"] = t.Description,
					};

					// ONLY THE ONES THAT ASK FOR NOTHING GET PHOTOGRAPHED. A tool with parameters has
					// infinitely many answers and no snapshot can hold them; it is declared anyway so
					// the caratula can say "this one exists but has to be asked from the app", which
					// beats hiding it.
					if (t.Input.Count > 0)
					{
						declarada["hechos"] = null;
						// IN ENGLISH, and it is not an oversight: `porQueNo` is read by a MODEL, not by a
						// person. The whole snapshot is handed to Siri with a framing that asks it to answer
						// «in their language»; the field is input, not screen. It was in Spanish while the
						// other source of the same field -- the reason Dispatcher composes, a few lines below
						// -- came in English, so the file mixed both languages under one key.
						declarada["porQueNo"] = "asks for parameters: it cannot be answered in advance";
						tools.Add(declarada);
						continue;
					}

					Response r = await dispatcher.Serve(new Request(m.Name, t.Name, null), ct);

					// A module that fails does NOT bring the snapshot down: the reason is noted and
					// the rest carry on. Twenty installed modules are twenty ways for something
					// outside our control to fail, and the caratula has to be able to answer for the
					// ones that do work.
					if (!r.Authorized) { declarada["hechos"] = null; declarada["porQueNo"] = r.Reason; }
					else declarada["hechos"] = r.Content;

					tools.Add(declarada);
				}

				modulos.Add(new Dictionary<string, object?>
				{
					["nombre"] = m.Name,
					// The identifying line comes from the manifest and not from here, just as on the
					// screen: one rule for the three faces and for the snapshot.
					["identidad"] = m.Manifest.Identity,
					["titulo"] = m.Manifest.Title,
					["institucion"] = m.Manifest.Institution,
					// The emoji by sector. The snapshot cannot paint a logo -- it is read by a system
					// intent, not a WebView -- so here the Host's icon is still all there is, and now
					// at least it comes from what the institution declared itself to be.
					["icono"] = Shell.Icon(m.Manifest),
					["tools"] = tools,
				});
			}

			string json = Json.Write(new Dictionary<string, object?>
			{
				// So the caratula can say "this is from three days ago" instead of presenting it as
				// if it were from now.
				["escritaEn"] = DateTimeOffset.UtcNow,
				["modulos"] = modulos,
			});

			// ATOMIC. Siri can fire the intent right while this is writing, and reading half a file is
			// worse than reading an old one: an old one is understandable, a torn one blows up the
			// parser and the caratula has no idea why.
			string destino = Path.Combine(carpeta, FileName);
			string temporal = destino + ".tmp";
			File.WriteAllText(temporal, json);
			File.Move(temporal, destino, overwrite: true);

			log($"instantanea: {modulos.Count} modulos, {json.Length} caracteres -> {destino}");
		}
		catch (Exception e)
		{
			// Not being able to write the snapshot must NOT bring the app down: Door 2 inside the app
			// goes on working the same. What is lost is Siri answering, and that has to show up in
			// the log and not as a crash.
			log($"instantanea: FALLO {e.GetType().Name}: {e.Message}");
		}
	}
}
