using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Gnothi.Host;

// EL VIGILANTE: hace que mover el interruptor tenga efecto AHORA, sin reiniciar nada.
//
// Sin esto el consentimiento se leia una sola vez, al arrancar. La persona apagaba una
// consulta en medio de una conversacion y el asistente seguia viendo la lista vieja hasta
// que alguien cerraba el Host. Un interruptor que hay que reiniciar para que sirva no es
// un interruptor: es un archivo de configuracion.
//
// La forma es la del protocolo. El servidor NO le manda el catalogo nuevo a nadie: manda
// 'notifications/tools/list_changed', que es un golpecito en el hombro, y el cliente
// vuelve a pedir tools/list cuando quiere. Por eso el catalogo se recarga acá y el que lo
// contesta sigue siendo PuenteDeTools, leyendo el mismo Despacho. Una sola fuente.
//
// Lo que se vigila es el archivo de LA PERSONA -instalacion.json- y no el manifiesto. El
// manifiesto es de la institucion y no cambia mientras el Host corre; si un dia cambiara,
// cambiaria lo que el modulo OFRECE, y eso es una reinstalacion, no un cambio de opinion.
internal sealed class VigilanteDeConsentimiento : BackgroundService
{
	// Un editor de texto -y cualquier UI que escriba por archivo temporal y rename- dispara
	// varios eventos por un solo guardado. Se junta la rafaga en una sola recarga.
	private static readonly TimeSpan Rafaga = TimeSpan.FromMilliseconds(300);

	private readonly Despacho despacho;
	private readonly IServiceProvider servicios;
	private readonly SemaphoreSlim señal = new(0, 1);

	public VigilanteDeConsentimiento(Despacho despacho, IServiceProvider servicios)
	{
		this.despacho = despacho;
		this.servicios = servicios;
	}

	protected override async Task ExecuteAsync(CancellationToken ct)
	{
		string raiz = despacho.Modulos.Raiz;
		if (!Directory.Exists(raiz)) return;

		using var mirilla = new FileSystemWatcher(raiz, "instalacion.json")
		{
			IncludeSubdirectories = true,
			NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
			EnableRaisingEvents = true,
		};

		mirilla.Changed += Aviso;
		mirilla.Created += Aviso;
		mirilla.Deleted += Aviso;
		mirilla.Renamed += Aviso;

		Console.Error.WriteLine($"[gnothi] vigilando el consentimiento en {raiz}");

		while (!ct.IsCancellationRequested)
		{
			try
			{
				await señal.WaitAsync(ct);
				await Task.Delay(Rafaga, ct);   // se junta la rafaga
				await Reaccionar(ct);
			}
			catch (OperationCanceledException) { break; }
			catch (Exception e)
			{
				// El vigilante no puede tumbar el Host. Si falla una recarga, el catalogo
				// que ya estaba sirviendo sigue sirviendo.
				Console.Error.WriteLine($"[gnothi] el vigilante falló: {e.GetType().Name}: {e.Message}");
			}
		}
	}

	private void Aviso(object remitente, FileSystemEventArgs e)
	{
		try { señal.Release(); } catch (SemaphoreFullException) { /* ya hay una recarga pendiente */ }
	}

	private async Task Reaccionar(CancellationToken ct)
	{
		IReadOnlyList<string> cambios = despacho.Modulos.RecargarConsentimiento();
		if (cambios.Count == 0) return;   // el archivo se toco pero decia lo mismo

		foreach (string c in cambios) Console.Error.WriteLine($"[gnothi] consentimiento: {c}");

		// El servidor se resuelve ACA y no en el constructor: en el arranque todavia no
		// existe, y pedirlo antes de tiempo ordena la construccion al reves.
		McpServer servidor = servicios.GetRequiredService<McpServer>();
		await servidor.SendNotificationAsync(NotificationMethods.ToolListChangedNotification, ct);

		int publicadas = despacho.Catalogo().Count();
		Console.Error.WriteLine($"[gnothi] avisado tools/list_changed; el catalogo queda en {publicadas} tools de modulos");
	}
}
