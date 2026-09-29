using System.IO.Pipes;
using System.Runtime.InteropServices;

namespace Gnothi.Host;

// THE PIPE GNOTHI TALKS TO THE GUEST THROUGH, and the only place where the box knows which
// operating system it is on.
//
// ── WHY A PIPE IS NEEDED, AND NOT A CALLBACK ───────────────────────────────────────────────────
//
// wasmtime's C API is asymmetric: HEARING is easy -- `set_stdout_custom` hands every guest write to
// code of ours -- but SPEAKING has no callback. All it offers is a PATH (`set_stdin_file`) or a
// fixed buffer. An ordinary file will not do: it ends. What is needed is something that opens BY
// PATH and **does not run out**, which is exactly what a named pipe is.
//
// ── AND UNTIL 2026-09-25 THAT WAS A POSIX FIFO, ON ALL THREE PLATFORMS ─────────────────────────
//
// `mkfifo(3)` through `DllImport("libc")`. On Android it works; **on Windows there is neither
// `libc` nor `mkfifo`**, so the box came up whole -- engine, substrate, canary singing 8 of 8 --
// and died on the first module with `DllNotFoundException: Unable to load DLL 'libc'` (0x8007007E,
// a WINDOWS HRESULT and not a wasm error, which is what gave away where it was happening).
//
// It was seen comparing the box against the room over the SAME module and the SAME journal. Neither
// run on its own would have shown it: the room's worked, and the box's started up fine.
//
// ON WINDOWS THE EQUIVALENT PIECE IS A NAMED PIPE, and it fits without touching the guest because
// it **also opens by path**: `\\.\pipe\<name>`. The guest notices nothing -- it goes on reading its
// stdin -- and wasmtime opens it with the same old `set_stdin_file`.
//
// ── THE ORDER MATTERS, LITERALLY, AND IN BOTH FORMS ────────────────────────────────────────────
//
// Opening a FIFO for reading blocks until there is a writer, and the other way round. With a named
// pipe the same happens under other names: the server waits in `WaitForConnection` until a client
// connects. In both cases **both ends have to be in flight at once**, and that is why this class
// splits the opening in two: `BeginOpening` before handing the path to wasmtime, and
// `FinishOpening` after. Done in one step, the thread hangs and there is no error to read.
internal sealed class BorderPipe : IDisposable
{
	private readonly string guestPath;
	private readonly string? fifoOnDisk;
	private readonly NamedPipeServerStream? server;

	private Thread? opening;
	private StreamWriter? opened;
	private Exception? failed;

	// WHAT IS HANDED TO `set_stdin_file`. On POSIX it is the FIFO's file; on Windows, the named
	// pipe's path, which is NOT a file on disk and therefore is not deleted on close.
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
			// THE NAME CARRIES THE PID, and that is not decoration: two Gnothis open at once -- a
			// window and a test, say -- would ask for the same name and the second would fail with
			// «already exists». A FIFO does not have that problem because it lives in the private
			// folder, already per-installation; the named-pipe namespace belongs to the MACHINE.
			string name = $"gnothi-caja-{module}-{Environment.ProcessId}";

			// ONE INSTANCE, AND OUTBOUND. `PipeDirection.Out` is from the server: Gnothi writes, the
			// guest reads. Same direction as the FIFO.
			var s = new NamedPipeServerStream(name, PipeDirection.Out, 1);
			return new BorderPipe($@"\\.\pipe\{name}", null, s);
		}

		string path = Path.Combine(privateFolder, "caja-" + module + ".fifo");
		try { File.Delete(path); } catch { /* it was not there */ }

		if (Wasmtime.mkfifo(path, 384) != 0)   // 0600
			throw new InvalidOperationException(
				$"caja: no pude crear la tuberia de {module}, errno={Marshal.GetLastWin32Error()}");

		return new BorderPipe(path, path, null);
	}

	// GNOTHI'S SIDE, PUT IN FLIGHT. It returns at once; what blocks stays on the thread.
	internal void BeginOpening(string module)
	{
		opening = new Thread(() =>
		{
			try
			{
				if (server is not null)
				{
					// Waits for wasmtime to open the other end. `leaveOpen: false` is not needed: the
					// StreamWriter owns the stream and closes it when it closes.
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
		// THE NAMED PIPE LEAVES NOTHING ON DISK -- it lives in the machine's namespace and goes
		// with the process -- so it only has to be released. The FIFO IS a file, and that one is
		// deleted, which is what was already being done.
		try { server?.Dispose(); } catch { /* already gone */ }
		if (fifoOnDisk is not null)
			try { File.Delete(fifoOnDisk); } catch { /* no longer there */ }
	}
}
