using System.Reflection;
using Choreography.Theater;
using Presupuesto.App;
using Puppeteer;

namespace Presupuesto.Mcp;

// Lee la app instalada rehidratándola desde SU carpeta de journal.
//
// La IA no consulta "la app corriendo": consulta al puppet, reconstruido desde su
// narrativa. No hay protocolo entre este proceso y el browser — los dos comparten la
// carpeta, y el journal ya es el protocolo.
//
// SOLO LECTURA, y son dos disciplinas distintas:
//
//  1. Nunca un PerformCommand. Todo lo que este proceso llama son PerformQuery.
//
//  2. NUNCA LA SEMILLA. PresupuestoApp emite upgrade('semilla') en OnHydrated, y aunque
//     el upgrade sea idempotente, el PerformCommand que lo envuelve JOURNALIZA UNA
//     ENTRADA en cada arranque. Si este proceso usara PresupuestoApp, cada pregunta de la
//     IA escribiría en el journal de la persona y rompería la invariante de un solo
//     escritor. De ahí SoloLectura: mismo actor, mismo dominio, OnHydrated vacío.
internal sealed class LectorDelJournal
{
	private const string NombreDelActor = "presupuesto-de-prueba";

	private readonly string carpeta;

	internal LectorDelJournal(string carpeta)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(carpeta);
		this.carpeta = Path.GetFullPath(carpeta);
	}

	internal string Carpeta => carpeta;

	// Que la app todavía no haya escrito nada NO es un error: es que la persona no la
	// abrió. Distinguirlo importa, porque la respuesta a la IA es otra.
	internal bool HayJournal =>
		Directory.Exists(Path.Combine(carpeta, NombreDelActor, "journal"));

	// Se rehidrata en CADA consulta, a propósito. La app instalada le escribe a esta
	// carpeta mientras este proceso vive, así que un actor cacheado contestaría con el
	// estado de la primera pregunta: la persona registraría un gasto y la IA seguiría
	// viendo el total viejo. Si el journal creciera al punto de que releer duela, acá va
	// un caché invalidado por la fecha de la carpeta — no antes.
	internal T Consultar<T>(Func<PresupuestoAPI, T> pregunta)
	{
		ArgumentNullException.ThrowIfNull(pregunta);

		var lector = new SoloLectura(NombreDelActor);
		try
		{
			lector.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
			lector.Start();

			return pregunta(new PresupuestoAPI(lector));
		}
		finally
		{
			try { lector.Actor.GracefulExit(); } catch { }
		}
	}

	// El mismo actor y el mismo dominio que la app, sin la semilla.
	//
	// El dominio se carga POR ARCHIVO, igual que en PresupuestoApp: este proyecto tampoco
	// nombra un tipo del dominio en C#, y por eso el modelo entero sigue siendo internal.
	private sealed class SoloLectura : PerformanceV2
	{
		internal SoloLectura(string nombreDelActor)
			: base(nombreDelActor, ElDominio.Assembly)
		{
		}

		protected override void OnHydrated()
		{
			// A propósito vacío. Ver el comentario de la clase.
		}
	}
}
