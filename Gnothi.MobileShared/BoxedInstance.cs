using System.Runtime.InteropServices;
using System.Text;

namespace Gnothi.Host;

// A LIVE MODULE INSIDE A BOX, with the border Gnothi talks to it through. See B12 and F8.
//
// THE ACTOR LIVES BETWEEN QUESTIONS, and that is why this is a dialogue and not a call per question:
// rehydrating on every query is exactly what the actor model exists in order not to do. Measured on
// the phone: 288 ms for the first, 1 ms for the rest. The exámenes face makes 21 reads to paint one
// screen; with a start-up per read it would be unusable.
//
// HOW YOU TALK TO SOMETHING INSIDE A WASM. wasmtime's C API is asymmetric:
//   - HEARING is easy: `set_stdout_custom` hands every guest write to code of ours.
//   - SPEAKING has no callback: only a PATH or a fixed buffer.
// So the path is a FIFO. The guest reads it and blocks; Gnothi writes to it whenever it wants.
//
// AND THE ORDER MATTERS, literally: opening a FIFO for reading blocks until there is a writer, and
// the other way round. `set_stdin_file` opens it for reading right then and there, so with no writer
// in flight the thread hangs and there is no error to read. Both sides have to be waiting for each
// other.
internal sealed class BoxedInstance : IDisposable
{
	// The guest answers one line per request. More than this means something broke on the other side.
	private static readonly TimeSpan Patience = TimeSpan.FromSeconds(30);

	private readonly Action<string> log;
	private readonly string module;
	private readonly string pipe;
	private readonly nint store;
	private readonly StreamWriter toTheBox;
	private readonly Thread theGuest;

	// The callback is kept in a field ON PURPOSE: it is a delegate that crosses into native code, and
	// if it only lived on the stack the collector could take it away with the box still running.
	private readonly Wasmtime.GuestWrites listening;

	private readonly StringBuilder halfLine = new();
	private readonly Queue<string> said = new();
	private readonly SemaphoreSlim aLineIsReady = new(0);
	private readonly SemaphoreSlim oneAtATime = new(1, 1);
	private bool closed;

	// WHAT THE MODULE SAYS IT OFFERS. Gnothi does not find it out by looking inside its code -- it
	// cannot --: it is told, and it contrasts that against the manifest as it already does today.
	internal string Declaration { get; }

	// `packageFolder` and `entry` are where the module's own DLLs live and which one holds its
	// surface -- the same two facts `InProcessShell` reads off `Shell.Arguments`. They are passed in
	// and not derived here because WHERE a module's package sits is the Host's business: the module
	// does not choose it, exactly as it does not choose its granted folder.
	internal BoxedInstance(nint engine, nint component, nint linker,
		string module, string grantedFolder, string privateFolder,
		string packageFolder, string entry, Action<string> log)
	{
		this.module = module;
		this.log = log;

		Directory.CreateDirectory(grantedFolder);
		pipe = Path.Combine(privateFolder, "caja-" + module + ".fifo");
		try { File.Delete(pipe); } catch { /* it was not there */ }
		if (Wasmtime.mkfifo(pipe, 384) != 0)   // 0600
			throw new InvalidOperationException(
				$"caja: no pude crear la tuberia de {module}, errno={Marshal.GetLastWin32Error()}");

		// THE CAGE IS BUILT BY `Cage`, not by this file: what the guest can reach has to be the SAME
		// thing the canary checks, and the only way to guarantee that is for it to be the same code.
		nint wasi = Cage.Build(grantedFolder, packageFolder);

		listening = (_, bytes, count) =>
		{
			int n = (int)count;
			byte[] b = new byte[n];
			Marshal.Copy(bytes, b, 0, n);
			Receive(Encoding.UTF8.GetString(b));
			return n;
		};
		Wasmtime.wasi_config_set_stdout_custom(wasi, listening, 0, 0);

		// BOTH ENDS OF THE FIFO, IN FLIGHT AT ONCE. See the note above.
		StreamWriter? opened = null;
		Exception? failedToOpen = null;
		var opening = new Thread(() =>
		{
			try { opened = new StreamWriter(new FileStream(pipe, FileMode.Open, FileAccess.Write)) { AutoFlush = true }; }
			catch (Exception e) { failedToOpen = e; }
		}) { IsBackground = true, Name = "caja-abre-" + module };
		opening.Start();

		if (!Wasmtime.wasi_config_set_stdin_file(wasi, pipe))
			throw new InvalidOperationException($"caja: no pude darle la tuberia a {module}");

		opening.Join(TimeSpan.FromSeconds(10));
		if (failedToOpen is not null)
			throw new InvalidOperationException($"caja: la tuberia de {module} no abrio", failedToOpen);
		toTheBox = opened ?? throw new InvalidOperationException($"caja: la tuberia de {module} no abrio a tiempo");

		// WHERE ITS PACKAGE IS, SAID BY THE HOST. The guest does not go looking: it is told, with the
		// guest-side name of the mount and which assembly holds the surface. Same reason the granted
		// folder travels this way -- what the module can reach is not the module's to decide.
		nint[] argv = [
			Marshal.StringToHGlobalAnsi(module),
			Marshal.StringToHGlobalAnsi(Cage.Granted),
			Marshal.StringToHGlobalAnsi("--borde"),
			Marshal.StringToHGlobalAnsi("--modulo"),
			Marshal.StringToHGlobalAnsi(Cage.Package),
			Marshal.StringToHGlobalAnsi(entry)];
		Wasmtime.wasi_config_set_argv(wasi, (nuint)argv.Length, argv);

		store = Cage.Enclose(engine, wasi, out nint ctx);

		string? wrong = Wasmtime.Message(
			Wasmtime.wasmtime_component_linker_instantiate(linker, ctx, component, out Wasmtime.Instance inst));
		if (wrong is not null) throw new InvalidOperationException($"caja: {module} no instancio: {wrong}");

		byte[] func = Cage.EntryPoint(ref inst, ctx, module);

		// THE GUEST RUNS ON ITS OWN THREAD because `func_call` does not return until the module ends,
		// and in border mode that is when Gnothi says goodbye to it.
		theGuest = new Thread(() =>
		{
			nint res = Marshal.AllocHGlobal(256);
			for (int i = 0; i < 256; i++) Marshal.WriteByte(res, i, 0);
			string? itSaid = Wasmtime.Message(
				Wasmtime.wasmtime_component_func_call(func, ctx, 0, 0, res, 1));
			if (itSaid is not null && !closed) log($"caja: {module} se salio: {itSaid.Replace("\n", " | ")}");
			Marshal.FreeHGlobal(res);
		}) { IsBackground = true, Name = "caja-" + module };
		theGuest.Start();

		Declaration = WaitForLine() ?? throw new InvalidOperationException(
			$"caja: {module} arranco y no dijo que ofrece");
		log($"caja: {module} vivo -- {Declaration}");
	}

	// ONE REQUEST, ONE ANSWER. Serialised: the guest is a single thread and the protocol is one line
	// per request, so two questions at once would mix the answers up.
	internal async Task<string> Ask(string verb, string name, string payload, CancellationToken ct)
	{
		if (closed) throw new InvalidOperationException($"caja: {module} ya no esta viva");

		await oneAtATime.WaitAsync(ct).ConfigureAwait(false);
		try
		{
			// The protocol separates with tabs because the payload is json and carries spaces and braces.
			toTheBox.WriteLine(verb + "\t" + name + "\t" + payload.Replace("\t", " ").Replace("\n", " "));
			string? itSaid = await Task.Run(WaitForLine, ct).ConfigureAwait(false);
			return itSaid ?? throw new TimeoutException(
				$"caja: {module} no contesto a {verb} {name} en {Patience.TotalSeconds:0} s");
		}
		finally { oneAtATime.Release(); }
	}

	private void Receive(string chunk)
	{
		lock (halfLine)
		{
			halfLine.Append(chunk);
			int cut;
			while ((cut = halfLine.ToString().IndexOf('\n')) >= 0)
			{
				string line = halfLine.ToString(0, cut).TrimEnd('\r');
				halfLine.Remove(0, cut + 1);
				if (line.Length == 0) continue;
				lock (said) said.Enqueue(line);
				aLineIsReady.Release();
			}
		}
	}

	private string? WaitForLine()
	{
		if (!aLineIsReady.Wait(Patience)) return null;
		lock (said) return said.Count > 0 ? said.Dequeue() : null;
	}

	public void Dispose()
	{
		if (closed) return;
		closed = true;

		try { toTheBox.WriteLine("chau\t\t"); } catch { /* already gone */ }
		try { theGuest.Join(TimeSpan.FromSeconds(5)); } catch { /* already gone */ }
		try { toTheBox.Dispose(); } catch { /* already gone */ }
		if (store != 0) Wasmtime.wasmtime_store_delete(store);
		try { File.Delete(pipe); } catch { /* no longer there */ }
		log($"caja: {module} cerrada");
	}
}
