using System;
using System.IO;
using Choreography.Theater;
using Compras.Dominio;
using Gnothi.Contract;
using Puppeteer;

namespace Compras.App
{
	// THE CARGO SURFACE OF COMPRAS. The same domain, exposed to the Host WITHOUT the MCP SDK:
	// it wears [ModuleSurface]/[Query] from Gnothi.Contract -a tiny, zero-dependency label the
	// Host lends- instead of [McpServerTool]. That is what lets the module ship as two small
	// DLLs (this + Compras.Dominio) and borrow Puppeteer, Choreography and this contract from
	// the Host.
	//
	// READ ONLY, two disciplines, like Compras.Mcp/LectorDelJournal:
	//   1. never a PerformCommand -every ComprasAPI query is a PerformQuery;
	//   2. NEVER the seed -ComprasApp seeds on OnHydrated, which would journal an entry on
	//      every start. So a read-only actor: same name, same domain, OnHydrated empty.
	//
	// The Host builds this by passing the granted folder to the string constructor (the
	// InProcessShell convention). It does NOT touch the app's own branding: this is a separate
	// cargo artifact for the assistant path.
	[ModuleSurface]
	public sealed class ComprasSurface
	{
		private const string ActorName = "compras";

		private readonly string folder;

		public ComprasSurface(string grantedFolder)
		{
			if (string.IsNullOrWhiteSpace(grantedFolder))
				throw new ArgumentException("the granted folder is required", nameof(grantedFolder));
			folder = Path.GetFullPath(grantedFolder);
		}

		private bool HasJournal => Directory.Exists(Path.Combine(folder, ActorName, "journal"));

		[Query("resumen")]
		public object Resumen()
		{
			if (!HasJournal) return NoData();
			return Read(api => new
			{
				hayDatos = true,
				totalGastado = api.TotalDeCompras(),
				cantidadDeCompras = api.CantidadDeCompras(),
				ultimoEventoLeido = api.UltimoEventoProcesado(),
			});
		}

		[Query("lista_de_compras")]
		public object ListaDeCompras()
		{
			if (!HasJournal) return NoData();
			return Read(api => new { hayDatos = true, compras = api.Compras() });
		}

		private object NoData() => new
		{
			hayDatos = false,
			carpeta = folder,
			detalle = "The app has not recorded anything in this folder yet. Ask the person to open "
				+ "their supermarket app and press Update.",
		};

		private T Read<T>(Func<ComprasAPI, T> question)
		{
			var reader = new ReadOnly(ActorName);
			try
			{
				reader.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
				reader.Start();
				return question(new ComprasAPI(reader));
			}
			finally
			{
				try { reader.Actor.GracefulExit(); } catch { /* already going away */ }
			}
		}

		private sealed class ReadOnly : PerformanceV2
		{
			internal ReadOnly(string actorName) : base(actorName, typeof(Historial).Assembly) { }

			protected override void OnHydrated()
			{
				// Deliberately empty. See the read-only note above.
			}
		}
	}
}
