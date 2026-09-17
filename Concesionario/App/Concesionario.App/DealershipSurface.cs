using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Choreography.Theater;
using Concesionario.App.Events;
using Concesionario.App.Views;
using Gnothi.Contract;
using Puppeteer;
using Puppets.Portador;

namespace Concesionario.App
{
	// THE SURFACE OF THE DEALERSHIP MODULE, for the cargo and hybrid forms. The same domain,
	// exposed to the Host WITHOUT the MCP SDK: it wears [ModuleSurface]/[Query] from
	// Gnothi.Contract -a tiny, zero-dependency label the Host lends- instead of [McpServerTool]
	// (~1.3 MB). That is what lets the module ship as two small DLLs and borrow Puppeteer,
	// Choreography and this contract from the Host.
	//
	// THREE AUDIENCES, THREE MARKERS, and none of them enters through another's door:
	//
	//   [Query]    what the ASSISTANT may ask -- declared in the manifest, and the person
	//              switches them off one by one
	//   [View]     what the SCREEN needs in order to paint -- not in the manifest, never seen
	//              by the model
	//   [Deliver]  what the COURIER drops -- not in the manifest either, and the only thing
	//              that writes
	//
	// ONE LIVE ACTOR for all three. See `app` at the bottom.
	//
	// ------------------------------------------------------------------------------------------
	//
	// THIS MODULE IS THE ODD ONE OUT, and it is worth saying why.
	//
	// Every other module keeps one actor named after itself -"examenes", "cargas"- so its surface
	// knows the name at compile time. This one keeps ONE ACTOR PER VEHICLE, named
	// `maintenance-<vin>`, because the thing the domain is about is a car and not an account.
	//
	// So this surface has to find out WHICH vehicle it is looking at, and the only thing the Host
	// hands it is a folder. It reads the answer off the journal itself: the actor directories are
	// right under the granted folder, and DealershipCarApp.TheVinOf already knows how to take the
	// VIN back out of an actor name.
	//
	// When the folder is empty there is no journal to ask, so the VIN falls back to the one the
	// module was delivered with -- the same constant the standalone app carries. One vehicle per
	// install, which is what the demo has always been.
	[ModuleSurface]
	public sealed class DealershipSurface : IDisposable
	{
		// What the vehicle was delivered as. Only used to CREATE the actor the first time: an
		// upgrade does not run twice, so a different profile or model on a later open is ignored
		// in silence (see DealershipCarApp).
		private const string TheProfileAtDelivery = "Standard";
		private const string TheModelAtDelivery = "YARIS-2019";
		private const string TheVinAtDelivery = "1HGCM82633A004352";

		// THE LANGUAGE THIS SURFACE ASKS THE DOOR IN, and it is the RECORD'S, not the person's.
		//
		// The standalone screen has a choice to read -navigator.languages, read once at boot- and
		// passes what it read. This form has nothing to read: the Host hands a surface a granted
		// folder and nothing else, and Gnothi.Contract carries no language. So the sentences the
		// domain composes come back in the language the dealership keeps its record in, which is
		// exactly what these views said before the door learned a second language.
		//
		// A LANGUAGE CHANNEL FOR THE HOSTED FORM IS A DECISION WITH AN OWNER, not something a merge
		// settles: it is the contract that would have to carry it.
		private const string TheLanguageOfTheRecord = Wording.Phrases.LanguageOfTheRecord;

		private readonly string folder;

		public DealershipSurface(string grantedFolder)
		{
			if (string.IsNullOrWhiteSpace(grantedFolder))
				throw new ArgumentException("the granted folder is required", nameof(grantedFolder));
			folder = Path.GetFullPath(grantedFolder);
			app = new Lazy<DealershipCarApp>(Start);
		}

		// WHICH VEHICLE THIS FOLDER HOLDS. The actor directory is the record: `maintenance-<vin>`
		// with a journal inside. Empty when nothing has been delivered yet.
		private string TheVinInTheFolder()
		{
			try
			{
				foreach (string each in Directory.EnumerateDirectories(folder))
				{
					if (!Directory.Exists(Path.Combine(each, "journal"))) continue;

					string vin = DealershipCarApp.TheVinOf(Path.GetFileName(each));
					if (vin.Length > 0) return vin;
				}
			}
			catch (IOException) { /* an unreadable folder reads as an empty one */ }
			catch (UnauthorizedAccessException) { }

			return "";
		}

		private bool HasJournal => TheVinInTheFolder().Length > 0;

		// ------------------------------------------------------------- what answers the assistant

		[Query(ModuleQueries.Summary)]
		public object Summary()
		{
			if (!HasJournal) return NoData();
			return Read(ModuleQueries.MakeSummary);
		}

		[Query(ModuleQueries.Freshness)]
		public object Freshness()
		{
			if (!HasJournal) return NoData();
			return Read(ModuleQueries.MakeFreshness);
		}

		// ------------------------------------------------------------- what the face paints

		// THE HEADER: whose car this is, what the manufacturer published for it, and the four
		// counts the screen leads with.
		[View("header")]
		public object Header() => !HasJournal ? NoData() : Read(api => (object)new
		{
			hasData = true,
			vin = api.ReadVin(),
			plate = ThePlateOfThisVehicle.ReadThrough(api, Now(), TheLanguageOfTheRecord),
			specification = WhatTheManufacturerPublished.ReadThrough(api, TheLanguageOfTheRecord),
			due = api.CountDue(),
			overdue = api.CountOverdue(),
			scheduled = api.CountScheduled(),
			upToDate = api.CountUpToDate(),
			missed = api.MissedCount(),
			nextDueKind = api.ReadNextDueKind(),
			lastEventApplied = api.LastEventApplied(),
		});

		// WHERE THE VEHICLE STANDS, and where each of the nine operations stands. They travel
		// together because the screen shows them together: the verdict on top and the table that
		// backs it underneath.
		//
		// `asOf` is set HERE and not by the face, and that is not a detail: everything in this
		// view is computed AGAINST AN INSTANT, and letting the page send it would let the screen
		// move the answer.
		[View("standing")]
		public object Standing() => !HasJournal ? NoData() : Read(api =>
		{
			DateTime asOf = Now();

			// THE DUE STATE PER KIND, and it has to be asked for separately. `HowOneOperationStands`
			// carries a DistanceStanding and a TimeStanding -- two countdowns, because an operation
			// can be due by either -- and no single verdict. The one verdict is `ReadDueState`, and
			// the screen paints its semaphore off that: the standalone app does exactly the same.
			//
			// Guessing a field name here is not a small mistake: it paints the WRONG COLOUR, which
			// is the one failure a semaphore cannot have. Measured on the phone -- "lo que sigue"
			// came up with a green check next to a pill that said "1 te toca ahora".
			var dueStates = new Dictionary<string, string>();
			foreach (string kind in TheClosedSets.TheNineKinds)
				dueStates[kind] = api.ReadDueState(kind);

			return (object)new
			{
				vehicle = WhereThisVehicleStands.ReadThrough(api, asOf, TheLanguageOfTheRecord),
				operations =
					WhereEachOperationStands.ReadThrough(api, asOf, TheLanguageOfTheRecord),
				dueStates,
			};
		});

		// THE STAYS AT THE SHOP, this year. The window is the same one the standalone app uses:
		// from the first day of the current year to today.
		[View("history")]
		public object History() => !HasJournal ? NoData() : Read(api =>
		{
			DateTime asOf = Now();
			return (object)new
			{
				stays = TheStaysOfThisVehicle.ReadThrough(
					api, TheFirstDayOfTheYearOf(asOf), TheDay(asOf), asOf, TheLanguageOfTheRecord),
			};
		});

		// WHAT IT COSTS AND HOW IT IS DRIVEN, both over the last twelve months against the twelve
		// before that. Two windows, because both readings are comparisons and a single window
		// would only give half of each.
		[View("costs")]
		public object Costs() => !HasJournal ? NoData() : Read(api =>
		{
			DateTime asOf = Now();
			DateTime aYearBack = asOf.AddYears(-1), twoYearsBack = asOf.AddYears(-2);

			return (object)new
			{
				spent = WhatThisVehicleHasCost.ReadThrough(
					api, aYearBack, asOf, twoYearsBack, aYearBack, TheLanguageOfTheRecord),
				driven = HowThisVehicleIsDriven.ReadThrough(
					api, aYearBack, asOf, twoYearsBack, aYearBack, TheLanguageOfTheRecord),
				services = WhatTheServicesCost.ReadThrough(api, asOf, TheLanguageOfTheRecord),
			};
		});

		// ------------------------------------------------------------- what the courier drops

		// Gnothi did the network -with ITS credential, against the endpoint IT fixed at install-
		// and hands over an envelope already read. This surface books it.
		//
		// IT IS THE SAME ASSEMBLY THE STANDALONE APP DOES: the same route and the same Check
		// guard, so delivering the same envelope twice duplicating nothing comes for free. The
		// two differences with AppState are that the envelope has ALREADY arrived -there is
		// nobody to ask- and that there is no flush: the journal of this form lives in the folder
		// Gnothi granted, not in a MEMFS that has to be written down to disk.
		//
		// THE CURSOR DOES NOT TRAVEL IN THE ENVELOPE and does not need to: the actor sets it,
		// coming out of the journal.
		[Deliver]
		public string Deliver(string envelopeJson)
		{
			ArgumentNullException.ThrowIfNull(envelopeJson);

			var once = new BucleDeRefresh(
				new DealershipCarAPI(app.Value),
				new FuenteDeEventosPorGnothi((_, _) => Task.FromResult(envelopeJson), "gnothi.deliver"),
				DealershipEventRoute.Route);

			ResultadoDeRefresh outcome = once.Ejecutar().GetAwaiter().GetResult();
			return outcome.ToString();
		}

		// HOW FAR IT HAS ALREADY READ, so the courier asks from there on and not for the whole
		// history again. It comes off the SAME actor that books, which is what makes it
		// trustworthy: it is the journal's real cursor, not a count the screen keeps.
		//
		// Zero when there is no journal yet, and zero means "bring me everything": the first time
		// round that is exactly right.
		[Cursor]
		public long HowFarItHasRead() => !HasJournal ? 0L : Read(api => api.LastEventApplied());

		private object NoData() => ModuleQueries.NoData(folder);

		// ------------------------------------------------------------- time

		// THE WHOLE SECOND IT IS NOW, in UTC. Not DateTime.UtcNow: sub-second precision makes two
		// readings taken in the same breath disagree, and every one of these views is computed
		// against an instant. Same rule as the standalone app.
		private static DateTime Now()
		{
			DateTime now = DateTime.UtcNow;
			return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);
		}

		private static string TheDay(DateTime instant) =>
			instant.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

		private static string TheFirstDayOfTheYearOf(DateTime instant) =>
			TheDay(new DateTime(instant.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc));

		// ------------------------------------------------------------- the actor

		// ONE LIVE ACTOR, and it is the change that matters. Measured on Examenes on 2026-09-11
		// and brought over as is, because the problem was not the laboratory's: it was the form's.
		//
		// Before that there were two -a read-only one per query, a writable one for delivery- and
		// the read-only one REHYDRATED on every question. That decision came with its reason
		// written down: the institution's standalone app writes this folder while Gnothi reads it.
		// True in A2/A3; in the HYBRID form there is no external app -- Gnothi owns the journal,
		// which was the whole point of the form.
		//
		// AND TWO ACTORS OVER ONE JOURNAL WAS THE PROBLEM, not the solution: the reader opened
		// while the writer was still closing its atomic operation, and that showed up as
		// `FileNotFoundException: Temp file not found`.
		//
		// THE SEED IS REQUIRED. An actor with an empty OnHydrated -put there so delivery would not
		// seed on every envelope- leaves the DSL without its globals, and the loop dies asking for
		// the cursor. The upgrade guard is once-per-actor, so there was nothing to avoid.
		//
		// AND THE WRITES ARE NOT REACHABLE FROM OUTSIDE, which is what really closes them:
		// [Deliver] is found by the shell through its own marker, is not in the manifest, the
		// assistant never sees it, and `consultar` only reaches [View].
		//
		// AN ORDINARY FIELD: the shell keeps the instance, so the actor's life is this object's
		// life. This used to be a `static` with a lock -- the actor had to outlive a class the
		// shell rebuilt on every call -- and all five institutions copied the same patch, which
		// is how it became clear the gap was the shell's and not theirs.
		//
		// Lazy because `ActualTools` -- the module check, on install and on startup -- builds this
		// class without asking it anything, and listing two names must not rehydrate the journal.
		// `Lazy<T>` publishes once by default, so this is one line where there were four fields.
		//
		// And no lock, because none is needed: two simultaneous questions on the same actor are
		// ordered by the engine, with ActorHandler's ReaderWriterLockSlim.
		private readonly Lazy<DealershipCarApp> app;

		private DealershipCarApp Start()
		{
			string vin = TheVinInTheFolder();
			if (vin.Length == 0) vin = TheVinAtDelivery;

			var starting = new DealershipCarApp(vin, TheProfileAtDelivery, TheModelAtDelivery);
			starting.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			starting.Start();
			return starting;
		}

		private T Read<T>(Func<DealershipCarAPI, T> question) => question(new DealershipCarAPI(app.Value));

		// AND IT CLOSES WHEN THE SHELL EVICTS: the context's `Unload` is cooperative and does not
		// complete if the actor left live references. Through IDisposable, which is what
		// `InProcessShell.Evict` asks for, and not through a new Gnothi marker.
		public void Dispose()
		{
			if (!app.IsValueCreated) return;

			try { app.Value.Actor.GracefulExit(); } catch { /* already going away */ }
		}
	}
}
