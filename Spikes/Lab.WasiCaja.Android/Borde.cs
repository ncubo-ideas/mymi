using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Lab.WasiCaja.Embebido;

namespace Lab.WasiCaja.Android
{
	// EL BORDE VIVO, CON WASMTIME EMBEBIDO. La pregunta que decide el diseno de CajaShell.
	//
	// En el spike de escritorio el borde funciona porque el Host lanza `wasmtime.exe` como PROCESO
	// y se queda con sus tuberias. Embebido no hay tuberias, y la C API es asimetrica:
	//
	//   stdout  ->  `set_stdout_custom`, un CALLBACK. El Host OYE al modulo sin archivos.
	//   stdin   ->  solo `set_stdin_file` (una ruta) o `set_stdin_bytes` (un bufer fijo).
	//               NO hay callback: el Host no puede HABLARLE mientras corre.
	//
	// Sin resolver eso quedan dos salidas malas: un `run` por pregunta -- que rearranca el actor,
	// ~300 ms medidos, y la cara de examenes hace 21 lecturas para pintar una pantalla -- o un
	// proceso por modulo, que B1 documenta como imposible en movil.
	//
	// LA TERCERA ES ESTA: `set_stdin_file` apuntando a un FIFO. Si wasmtime BLOQUEA leyendolo en
	// vez de ver EOF, el Host puede escribirle cuando quiera y el actor se queda vivo.
	public partial class MainActivity
	{
		// mkfifo(3) de bionic. No hay API administrada para esto y no hace falta mas que esta.
		[DllImport("libc", SetLastError = true)]
		private static extern int mkfifo([MarshalAs(UnmanagedType.LPUTF8Str)] string ruta, uint modo);

		private readonly StringBuilder loQueDijo = new();
		private readonly AutoResetEvent hayLinea = new(false);
		private readonly Queue<string> lineas = new();
		private W.EscribeElInvitado retenerElCallback;   // que el GC no se lo lleve

		private void ProbarBordeVivo(IntPtr motor, IntPtr componente, IntPtr linker,
			string concedida, string raiz)
		{
			string tuberia = Path.Combine(raiz, "pedidos.fifo");
			if (File.Exists(tuberia)) File.Delete(tuberia);

			if (mkfifo(tuberia, 384) != 0)   // 0600
			{
				Decir("borde: mkfifo FALLO, errno=" + Marshal.GetLastWin32Error());
				return;
			}
			Decir("borde: FIFO creado en " + tuberia);

			IntPtr wasi = W.wasi_config_new();

			// LO QUE EL MODULO DICE, directo a codigo nuestro.
			retenerElCallback = (datos, bytes, cuantos) =>
			{
				int n = (int)cuantos;
				byte[] b = new byte[n];
				Marshal.Copy(bytes, b, 0, n);
				lock (loQueDijo)
				{
					loQueDijo.Append(Encoding.UTF8.GetString(b));
					int corte;
					while ((corte = loQueDijo.ToString().IndexOf('\n')) >= 0)
					{
						string linea = loQueDijo.ToString(0, corte).TrimEnd('\r');
						loQueDijo.Remove(0, corte + 1);
						if (linea.Length > 0) { lineas.Enqueue(linea); hayLinea.Set(); }
					}
				}
				return (IntPtr)n;   // bytes aceptados
			};
			W.wasi_config_set_stdout_custom(wasi, retenerElCallback, IntPtr.Zero, IntPtr.Zero);

			// Y LO QUE EL HOST LE DICE, por el FIFO.
			//
			// EL ORDEN ES LO QUE COSTO LA PRIMERA CORRIDA. Abrir un FIFO para LEER bloquea hasta que
			// haya escritor, y para ESCRIBIR bloquea hasta que haya lector. `set_stdin_file` abre
			// para leer ahi mismo, asi que llamarlo sin escritor en vuelo cuelga el hilo y no se
			// imprime una linea mas. Los dos tienen que estar esperandose.
			StreamWriter aLaTuberia = null;
			Exception falloAlAbrir = null;
			var abriendo = new Thread(() =>
			{
				try { aLaTuberia = new StreamWriter(new FileStream(tuberia, FileMode.Open, FileAccess.Write)) { AutoFlush = true }; }
				catch (Exception e) { falloAlAbrir = e; }
			}) { IsBackground = true };
			abriendo.Start();

			Decir("borde: abriendo el FIFO por los dos lados...");
			if (!W.wasi_config_set_stdin_file(wasi, tuberia)) { Decir("borde: no pude poner el FIFO como stdin"); return; }
			abriendo.Join(5000);
			if (falloAlAbrir is not null) { Decir("borde: no pude escribir el FIFO: " + falloAlAbrir.GetType().Name + ": " + falloAlAbrir.Message); return; }
			if (aLaTuberia is null) { Decir("borde: el FIFO no abrio por el lado del Host"); return; }
			Decir("borde: FIFO abierto por los dos lados");
			W.wasi_config_inherit_stderr(wasi);
			if (!W.wasi_config_preopen_dir(wasi, concedida, "/datos", true)) { Decir("borde: no monto la concedida"); return; }

			var argv = new List<IntPtr> {
				Marshal.StringToHGlobalAnsi("Lab.WasiCaja.wasm"),
				Marshal.StringToHGlobalAnsi("/datos"),
				Marshal.StringToHGlobalAnsi("--borde") };
			W.wasi_config_set_argv(wasi, (UIntPtr)argv.Count, argv.ToArray());

			IntPtr store = W.wasmtime_store_new(motor, IntPtr.Zero, IntPtr.Zero);
			IntPtr ctx = W.wasmtime_store_context(store);
			W.wasmtime_context_set_wasi(ctx, wasi);
			W.wasmtime_context_set_wasi_http(ctx);

			IntPtr err = W.wasmtime_component_linker_instantiate(linker, ctx, componente, out W.ComponentInstance inst);
			if (Fallo(err, "instanciar el borde")) return;

			IntPtr idxInst = IntPtr.Zero;
			foreach (string c in new[] { "wasi:cli/run@0.2.0", "wasi:cli/run@0.2.1", "wasi:cli/run@0.2.2", "wasi:cli/run@0.2.3", "wasi:cli/run" })
			{
				idxInst = W.wasmtime_component_instance_get_export_index(ref inst, ctx, IntPtr.Zero, c, (UIntPtr)c.Length);
				if (idxInst != IntPtr.Zero) break;
			}
			IntPtr idxRun = W.wasmtime_component_instance_get_export_index(ref inst, ctx, idxInst, "run", (UIntPtr)3);
			byte[] func = new byte[256];
			if (!W.wasmtime_component_instance_get_func(ref inst, ctx, idxRun, func)) { Decir("borde: sin funcion"); return; }

			// EL INVITADO CORRE EN OTRO HILO, porque `run` no vuelve hasta que el modulo termina:
			// en modo borde eso es cuando el Host le dice chau. Mientras tanto contesta.
			var reloj = System.Diagnostics.Stopwatch.StartNew();
			var elInvitado = new Thread(() =>
			{
				IntPtr res = Marshal.AllocHGlobal(256);
				for (int i = 0; i < 256; i++) Marshal.WriteByte(res, i, 0);
				string dijo = W.Mensaje(W.wasmtime_component_func_call(func, ctx, IntPtr.Zero, UIntPtr.Zero, res, (UIntPtr)1));
				if (dijo is not null) Decir("borde: el invitado salio -> " + dijo.Replace("\n", " | "));
			}) { IsBackground = true };
			elInvitado.Start();

			string saludo = Esperar(8000);
			if (saludo is null) { Decir("borde: NO HUBO SALUDO -- el FIFO no sirve como stdin vivo"); return; }
			Decir("borde: saludo en " + reloj.ElapsedMilliseconds + " ms");
			Decir("borde:   " + (saludo.Length > 160 ? saludo[..160] + "..." : saludo));

			// LA PRUEBA DE VERDAD: preguntar DESPUES del saludo. Si esto contesta, el Host puede
			// hablarle al modulo mientras corre y el actor se queda vivo entre preguntas.
			foreach (var (verbo, nombre, que) in new[] {
				("preguntar", "resumen", "1a consulta"),
				("cursor", "", "el cursor"),
				("preguntar", "resumen", "2a consulta (actor ya vivo)"),
				("consultar", "cabecera", "una vista") })
			{
				long t0 = reloj.ElapsedMilliseconds;
				aLaTuberia.WriteLine(verbo + "\t" + nombre + "\t");
				string r = Esperar(8000);
				long ms = reloj.ElapsedMilliseconds - t0;
				Decir("borde: " + que.PadRight(30) + ms.ToString().PadLeft(4) + " ms  "
					+ (r is null ? "(nada)" : r.Length > 110 ? r[..110] + "..." : r));
				if (r is null) { Decir("borde: se corto el dialogo"); return; }
			}

			aLaTuberia.WriteLine("chau\t\t");
			elInvitado.Join(4000);
			W.wasmtime_store_delete(store);
			Decir("borde: EL BORDE VIVO FUNCIONA EMBEBIDO");
		}

		private string Esperar(int ms)
		{
			var hasta = DateTime.UtcNow.AddMilliseconds(ms);
			while (DateTime.UtcNow < hasta)
			{
				lock (loQueDijo) if (lineas.Count > 0) return lineas.Dequeue();
				hayLinea.WaitOne(100);
			}
			return null;
		}
	}
}
