using System.IO.Pipes;
using System.Runtime.InteropServices;

namespace Gnothi.Host;

// LA TUBERIA POR LA QUE GNOTHI LE HABLA AL INVITADO, y el unico lugar donde la caja sabe en que
// sistema operativo esta.
//
// ── POR QUE HACE FALTA UNA TUBERIA, Y NO UN CALLBACK ────────────────────────────────────────────
//
// La C API de wasmtime es asimetrica: OIR es facil -- `set_stdout_custom` entrega cada escritura del
// invitado a codigo nuestro -- pero HABLAR no tiene callback. Lo unico que ofrece es una RUTA
// (`set_stdin_file`) o un buffer fijo. Un archivo comun no sirve: se termina. Hace falta algo que se
// abra por ruta y **no se acabe**, que es exactamente lo que es una tuberia con nombre.
//
// ── Y HASTA EL 2026-09-25 ESO ERA UN FIFO DE POSIX, EN LAS TRES PLATAFORMAS ─────────────────────
//
// `mkfifo(3)` por `DllImport("libc")`. En Android anda; **en Windows no hay ni `libc` ni `mkfifo`**,
// asi que la caja arrancaba entera -- motor, sustrato, canario cantando 8 de 8 -- y moria al primer
// modulo con `DllNotFoundException: Unable to load DLL 'libc'` (0x8007007E, un HRESULT de Windows y
// no un error de wasm, que es lo que delato donde estaba pasando).
//
// Se vio comparando la caja con la sala sobre el MISMO modulo y el MISMO journal. Ninguna de las dos
// corridas por separado lo habria mostrado: la de la sala andaba, y la de la caja arrancaba bien.
//
// EN WINDOWS LA PIEZA EQUIVALENTE ES UN NAMED PIPE, y encaja sin tocar al invitado porque **tambien
// se abre por ruta**: `\\.\pipe\<nombre>`. El invitado no se entera de nada -- sigue leyendo su
// stdin --, y wasmtime lo abre con el mismo `set_stdin_file` de siempre.
//
// ── EL ORDEN IMPORTA, LITERALMENTE, Y EN LAS DOS FORMAS ─────────────────────────────────────────
//
// Abrir un FIFO para leer bloquea hasta que haya alguien escribiendo, y al reves. Con un named pipe
// pasa lo mismo con otros nombres: el servidor espera en `WaitForConnection` hasta que un cliente se
// conecta. En los dos casos **los dos lados tienen que estar en vuelo a la vez**, y por eso esta
// clase parte la apertura en dos: `BeginOpening` antes de darle la ruta a wasmtime, y `FinishOpening`
// despues. Si se hiciera en un solo paso, el hilo se cuelga y no hay error que leer.
internal sealed class BorderPipe : IDisposable
{
	private readonly string guestPath;
	private readonly string? fifoOnDisk;
	private readonly NamedPipeServerStream? server;

	private Thread? opening;
	private StreamWriter? opened;
	private Exception? failed;

	// LO QUE SE LE DA A `set_stdin_file`. En POSIX es el archivo del FIFO; en Windows, la ruta del
	// named pipe, que NO es un archivo del disco y por eso no se borra al cerrar.
	internal string GuestPath => guestPath;

	private BorderPipe(string guestPath, string? fifoOnDisk, NamedPipeServerStream? server)
	{
		this.guestPath = guestPath;
		this.fifoOnDisk = fifoOnDisk;
		this.server = server;
	}

	internal static BorderPipe Create(string privateFolder, string module)
	{
		if (OperatingSystem.IsWindows())
		{
			// EL NOMBRE LLEVA EL PID, y no es adorno: dos Gnothi abiertos a la vez -- una ventana y
			// una prueba, por ejemplo -- pedirian el mismo nombre y el segundo fallaria con
			// «ya existe». Un FIFO no tiene ese problema porque vive en la carpeta privada, que ya
			// es distinta por instalacion; el espacio de nombres de los named pipes es de la MAQUINA.
			string name = $"gnothi-caja-{module}-{Environment.ProcessId}";

			// UNA SOLA INSTANCIA Y DE SALIDA. `PipeDirection.Out` es desde el servidor: Gnothi
			// escribe, el invitado lee. Es el mismo sentido que el FIFO.
			var s = new NamedPipeServerStream(name, PipeDirection.Out, 1);
			return new BorderPipe($@"\\.\pipe\{name}", null, s);
		}

		string path = Path.Combine(privateFolder, "caja-" + module + ".fifo");
		try { File.Delete(path); } catch { /* no estaba */ }

		if (Wasmtime.mkfifo(path, 384) != 0)   // 0600
			throw new InvalidOperationException(
				$"caja: no pude crear la tuberia de {module}, errno={Marshal.GetLastWin32Error()}");

		return new BorderPipe(path, path, null);
	}

	// EL LADO DE GNOTHI, PUESTO EN VUELO. Vuelve enseguida; lo que bloquea queda en el hilo.
	internal void BeginOpening(string module)
	{
		opening = new Thread(() =>
		{
			try
			{
				if (server is not null)
				{
					// Espera a que wasmtime abra el otro extremo. `leaveOpen: false` no hace falta:
					// el StreamWriter es dueno del stream y lo cierra al cerrarse.
					server.WaitForConnection();
					opened = new StreamWriter(server) { AutoFlush = true };
				}
				else
				{
					opened = new StreamWriter(new FileStream(guestPath, FileMode.Open, FileAccess.Write))
					{ AutoFlush = true };
				}
			}
			catch (Exception e) { failed = e; }
		})
		{ IsBackground = true, Name = "caja-abre-" + module };

		opening.Start();
	}

	internal StreamWriter FinishOpening(string module, TimeSpan patience)
	{
		opening?.Join(patience);

		if (failed is not null)
			throw new InvalidOperationException($"caja: la tuberia de {module} no abrio", failed);

		return opened ?? throw new InvalidOperationException($"caja: la tuberia de {module} no abrio a tiempo");
	}

	public void Dispose()
	{
		// EL NAMED PIPE NO DEJA NADA EN EL DISCO -- vive en el espacio de nombres de la maquina y se
		// va con el proceso --, asi que solo hay que soltarlo. El FIFO SI es un archivo, y ese se
		// borra, que es lo que se venia haciendo.
		try { server?.Dispose(); } catch { /* ya se fue */ }
		if (fifoOnDisk is not null)
			try { File.Delete(fifoOnDisk); } catch { /* ya no esta */ }
	}
}
