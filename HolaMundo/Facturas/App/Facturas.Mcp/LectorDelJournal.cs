using Choreography.Theater;
using Facturas.App;
using Puppeteer;

namespace Facturas.Mcp;

// Lee la app instalada rehidratandola desde SU carpeta de journal.
//
// La IA no consulta "la app corriendo": consulta la criatura, reconstruida desde su
// narrativa. No hay protocolo entre este proceso y el browser - los dos comparten la
// carpeta, y el journal YA es el protocolo.
//
// SOLO LECTURA, y eso son dos disciplinas distintas:
//
//  1. Nunca un PerformCommand. Las consultas de FacturasAPI son todas PerformQuery.
//  2. NUNCA LA SEMILLA. FacturasApp emite upgrade('semilla') en OnHydrated, y aunque el
//     upgrade sea idempotente, el PerformCommand que lo envuelve JOURNALIZA UNA ENTRADA
//     en cada arranque - medido. Si este proceso usara FacturasApp, cada pregunta de la
//     IA escribiria en el journal del usuario y romperia la invariante de 1-writer.
//     De ahi SoloLectura: mismo actor, mismo dominio, OnHydrated vacio.
internal sealed class LectorDelJournal
{
	private const string NombreDelActor = "facturas";

	private readonly string carpeta;

	internal LectorDelJournal(string carpeta)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(carpeta);
		this.carpeta = Path.GetFullPath(carpeta);
	}

	internal string Carpeta => carpeta;

	// La app todavia no escribio nada: no es un error, es que la persona no la abrio.
	internal bool HayJournal =>
		Directory.Exists(Path.Combine(carpeta, NombreDelActor, "journal"));

	// Se rehidrata en CADA consulta a proposito. La app instalada le escribe a esta
	// carpeta mientras este proceso vive, asi que un actor cacheado contestaria con el
	// estado de la primera pregunta: la IA preguntaria despues de un Refresh y veria el
	// total viejo. Medido en 651 bytes para semilla + una factura, asi que releer sale
	// barato; si el journal creciera, ACA va un cache invalidado por fecha de la carpeta.
	internal T Consultar<T>(Func<FacturasAPI, T> pregunta)
	{
		ArgumentNullException.ThrowIfNull(pregunta);

		var lector = new SoloLectura(NombreDelActor);
		try
		{
			lector.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
			lector.Start();

			return pregunta(new FacturasAPI(lector));
		}
		finally
		{
			try { lector.Actor.GracefulExit(); } catch { }
		}
	}

	// El mismo actor y el mismo dominio que la app, sin la semilla. Ver el comentario
	// de arriba: la semilla es lo unico que escribiria.
	private sealed class SoloLectura : PerformanceV2
	{
		internal SoloLectura(string nombreDelActor)
			: base(nombreDelActor, ElDominio.Assembly)
		{
		}

		protected override void OnHydrated()
		{
			// A proposito vacio.
		}
	}
}
