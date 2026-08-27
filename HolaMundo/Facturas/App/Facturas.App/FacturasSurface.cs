using System;
using System.IO;
using Choreography.Theater;
using Facturas.Dominio;
using Gnothi.Contract;
using Puppeteer;

namespace Facturas.App
{
	// THE CARGO SURFACE OF FACTURAS (Súper La Guaria). The same domain, exposed to the Host
	// WITHOUT the MCP SDK: it wears [ModuleSurface]/[Query] from Gnothi.Contract -a tiny,
	// zero-dependency label the Host lends- instead of [McpServerTool]. That is what lets the
	// module ship as two small DLLs (this + Facturas.Dominio) and borrow Puppeteer,
	// Choreography and this contract from the Host.
	//
	// READ ONLY, two disciplines, exactly like Facturas.Mcp/LectorDelJournal:
	//   1. never a PerformCommand -every FacturasAPI query is a PerformQuery;
	//   2. NEVER the seed -the app seeds on OnHydrated, which would journal an entry on every
	//      start. So a read-only actor: same name, same domain, OnHydrated empty.
	//
	// The Host builds this by passing the granted folder to the string constructor (the
	// InProcessShell convention). The module never opens the journal nor chooses the folder.
	[ModuleSurface]
	public sealed class FacturasSurface
	{
		private const string ActorName = "facturas";

		private readonly string folder;

		public FacturasSurface(string grantedFolder)
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
				total = api.TotalDeFacturas(),
				cantidadDeFacturas = api.CantidadDeFacturas(),
				ultimoEventoLeido = api.UltimoEventoProcesado(),
			});
		}

		[Query("lista_de_facturas")]
		public object ListaDeFacturas()
		{
			if (!HasJournal) return NoData();
			return Read(api => new { hayDatos = true, facturas = api.Facturas() });
		}

		private object NoData() => new
		{
			hayDatos = false,
			carpeta = folder,
			detalle = "The app has not recorded anything in this folder yet. Ask the person to open "
				+ "their Súper La Guaria app and press Update.",
		};

		private T Read<T>(Func<FacturasAPI, T> question)
		{
			var reader = new ReadOnly(ActorName);
			try
			{
				reader.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
				reader.Start();
				return question(new FacturasAPI(reader));
			}
			finally
			{
				try { reader.Actor.GracefulExit(); } catch { /* already going away */ }
			}
		}

		private sealed class ReadOnly : PerformanceV2
		{
			internal ReadOnly(string actorName) : base(actorName, typeof(Facturario).Assembly) { }

			protected override void OnHydrated()
			{
				// Deliberately empty. See the read-only note above.
			}
		}
	}
}
