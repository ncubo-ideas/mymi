using System;
using System.IO;
using Choreography.Theater;
using Examenes.Dominio;
using Gnothi.Contract;
using Puppeteer;

namespace Examenes.App
{
	// THE CARGO SURFACE OF EXAMENES. The same domain, exposed to the Host WITHOUT the MCP SDK.
	//
	// It wears [ModuleSurface]/[Query] from Gnothi.Contract -a tiny, zero-dependency label the
	// Host lends- instead of [McpServerTool] from the MCP SDK (~1.3 MB). That is what lets the
	// module ship as two small DLLs (this + Examenes.Dominio) and borrow Puppeteer, Choreography
	// and this contract from the Host. It is the reference model's SuperficieDeFacturas, here.
	//
	// It is READ ONLY, and that is two disciplines, exactly like Examenes.Mcp/LectorDelJournal:
	//   1. never a PerformCommand -every ExamenesAPI query is a PerformQuery;
	//   2. NEVER the seed -ExamenesApp emits upgrade('semilla') in OnHydrated, which journals an
	//      entry on every start. So this uses a read-only actor: same actor name, same domain,
	//      OnHydrated empty. Otherwise every question would write to the person's journal.
	//
	// The Host builds this by passing the granted folder to the string constructor -the
	// InProcessShell convention. The module never opens the journal on its own nor chooses the
	// folder.
	[ModuleSurface]
	public sealed class ExamenesSurface
	{
		private const string ActorName = "examenes";

		private readonly string folder;

		public ExamenesSurface(string grantedFolder)
		{
			if (string.IsNullOrWhiteSpace(grantedFolder))
				throw new ArgumentException("the granted folder is required", nameof(grantedFolder));
			folder = Path.GetFullPath(grantedFolder);
		}

		private bool HasJournal => Directory.Exists(Path.Combine(folder, ActorName, "journal"));

		// A SUMMARY LINE. The smallest useful block, and the one a screen shows at the top.
		[Query(ConsultasDelModulo.Resumen)]
		public object Resumen()
		{
			if (!HasJournal) return NoData();
			return Read(ConsultasDelModulo.HacerResumen);
		}

		// THE LIST OF EXAMS. An array of flat rows -exactly what the window renders as a table.
		[Query(ConsultasDelModulo.ListaDeExamenes)]
		public object ListaDeExamenes()
		{
			if (!HasJournal) return NoData();
			return Read(ConsultasDelModulo.HacerListaDeExamenes);
		}

		private object NoData() => ConsultasDelModulo.SinDatos(folder);

		// Re-hydrated on EVERY query on purpose: the installed app writes to this folder while
		// this process lives, so a cached actor would answer with the state of the first
		// question. Measured cheap for the whole history.
		private T Read<T>(Func<ExamenesAPI, T> question)
		{
			var reader = new ReadOnly(ActorName);
			try
			{
				reader.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
				reader.Start();
				return question(new ExamenesAPI(reader));
			}
			finally
			{
				try { reader.Actor.GracefulExit(); } catch { /* already going away */ }
			}
		}

		// Same actor and same domain as the app, without the seed. The domain assembly is found
		// by reflection from any of its types -Expediente here- exactly as ExamenesApp does.
		private sealed class ReadOnly : PerformanceV2
		{
			internal ReadOnly(string actorName) : base(actorName, typeof(Expediente).Assembly) { }

			protected override void OnHydrated()
			{
				// Deliberately empty. See the read-only note above.
			}
		}
	}
}
