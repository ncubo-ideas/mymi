using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Lab.WasiCaja.Embebido
{
	// GNOTHI CON WASMTIME ADENTRO, NO AL LADO. Paso 3 de F2 · P19.
	//
	// Tres pendientes distintos pedian esto, y por eso vale mas que un ahorro de proceso:
	//   - el modulo podia REESCRIBIR SU PROPIO PAQUETE (paso 2) -> aca el paquete va de solo lectura
	//   - la red fallaba por falta de hilos, no por politica     -> aca NUNCA se enciende wasi-http
	//   - Gnothi no VEIA los intentos negados                    -> aca el anfitrion es codigo nuestro
	//
	// Lo que este programa hace es lo minimo que prueba el camino: instanciar el componente que
	// .NET produce, con la caja armada DESDE CODIGO, y correrlo.
	internal static class Program
	{
		private static int Main(string[] args)
		{
			if (args.Length < 2)
			{
				Console.Error.WriteLine("uso: Lab.WasiCaja.Embebido <AppBundle> <carpeta concedida> [argumentos del modulo...]");
				return 2;
			}

			string bundle = Path.GetFullPath(args[0]);
			string concedida = Path.GetFullPath(args[1]);
			// Los flags de ESTE programa no viajan al modulo: el modulo recibe sus argumentos, no los
			// nuestros. Costo una corrida: «Could not load assembly --con-red».
			// Se corta en el PRIMER flag: lo que viene despues es nuestro, incluido su valor.
			// Sin el corte, `--veces 3` le mandaba «3» al modulo como nombre de ensamblado.
			//
			// Y ESO DEJABA AL MODULO SIN PODER RECIBIR FLAGS, que es justo lo que hace falta para
			// correrle los intentos: `--intentos` empieza con dos guiones y el corte se lo comia.
			// `--al-modulo` invierte la regla: TODO lo que sigue es suyo, tal cual.
			var propios = new List<string>();
			int alModulo = Array.IndexOf(args, "--al-modulo");
			if (alModulo >= 0)
				for (int i = alModulo + 1; i < args.Length; i++) propios.Add(args[i]);
			else
				for (int i = 2; i < args.Length && !args[i].StartsWith("--"); i++) propios.Add(args[i]);
			string[] delModulo = propios.Count > 0 ? propios.ToArray() : ["Lab.WasiCaja", "/datos"];

			Decir("bundle    = " + bundle);
			Decir("concedida = " + concedida);

			var reloj = Stopwatch.StartNew();
			long memAntes = GC.GetTotalMemory(false);
			long procAntes = Process.GetCurrentProcess().PrivateMemorySize64;
			long anterior = procAntes;

			IntPtr motor = W.wasm_engine_new();
			Decir("motor creado                (" + reloj.ElapsedMilliseconds + " ms)");

			// EL PRECOMPILADO, y la pregunta que estos dos modos contestan: QUE PASA SI NO COINCIDE.
			//   --precompilar  <archivo>   compila y guarda el artefacto
			//   --precompilado <archivo>   intenta abrirlo y dice que paso
			// Corriendo los dos con versiones DISTINTAS de wasmtime.dll se ve el caso real: Gnothi se
			// actualiza y el artefacto de ayer ya no vale.
			int ip = Array.IndexOf(args, "--precompilado");
			if (ip >= 0)
			{
				IntPtr e2 = W.wasmtime_component_deserialize_file(motor, args[ip + 1], out IntPtr _);
				string dijo = W.Mensaje(e2);
				Decir(dijo is null ? "DESERIALIZO BIEN" : "NO SIRVIO -> " + dijo.Replace("\n", " | "));
				return dijo is null ? 0 : 1;
			}

			// 1. COMPILAR EL COMPONENTE. Es lo que las bindings de C# no pueden hacer.
			byte[] bytes = File.ReadAllBytes(Path.Combine(bundle, "dotnet.wasm"));
			IntPtr err = W.wasmtime_component_new(motor, bytes, (UIntPtr)bytes.Length, out IntPtr componente);
			if (Fallo(err, "compilar el componente")) return 1;

			int ig = Array.IndexOf(args, "--precompilar");
			if (ig >= 0)
			{
				err = W.wasmtime_component_serialize(componente, out W.ByteVec blob);
				if (Fallo(err, "serializar")) return 1;
				byte[] g = new byte[(int)blob.size];
				Marshal.Copy(blob.data, g, 0, g.Length);
				W.wasm_byte_vec_delete(ref blob);
				File.WriteAllBytes(args[ig + 1], g);
				Decir("precompilado guardado en " + args[ig + 1] + " (" + g.Length + " bytes)");
				return 0;
			}
			Decir("componente compilado        (" + reloj.ElapsedMilliseconds + " ms, " + bytes.Length + " bytes)");

			// 2. EL LINKER, y lo que se le pone ES la politica.
			IntPtr linker = W.wasmtime_component_linker_new(motor);
			Decir("  linker creado");
			err = W.wasmtime_component_linker_add_wasip2(linker);
			if (Fallo(err, "add_wasip2")) return 1;
			Decir("  wasip2 agregado");

			// LO QUE NO SE LLAMA: `add_wasi_http`. El componente de .NET IMPORTA wasi:http, asi que
			// sin esto no instanciaria -- y por eso se declaran trampas: el modulo arranca, y si
			// alguna vez toca la red, TRAPEA. Es contencion por construccion y no por accidente,
			// que era el hueco 2 del paso 2.
			// `define_unknown_imports_as_traps` PANICA en wasmtime 48.0.2 -- unreachable!() en
			// component.rs:512 al recorrer los imports de ESTE componente. Queda reportado; mientras
			// tanto la red se controla con la otra palanca, que es dar o no dar wasi-http.
			bool conRed = Array.IndexOf(args, "--con-red") >= 0;
			if (conRed)
			{
				err = W.wasmtime_component_linker_add_wasi_http(linker);
				if (Fallo(err, "add_wasi_http")) return 1;
				Decir("linker: wasip2 SI, wasi-http SI  (--con-red)");
			}
			else Decir("linker: wasip2 SI, wasi-http NO");

			// CUANTAS VECES. El numero que decide todo no es el de UNA instancia: es el MARGINAL de
			// la segunda. Si cada instancia trae su propia memoria lineal con su propio runtime de
			// .NET adentro, N modulos cuestan N veces lo mismo -- igual que N WebViews -- y la caja
			// arregla el disco y NO la memoria. Eso hay que saberlo antes de construir nada mas.
			//
			// EL MOTOR Y EL COMPONENTE COMPILADO SE COMPARTEN entre las vueltas, que es justamente
			// lo que un Host haria: compilar una vez, instanciar por modulo.
			int veces = 1;
			int iv = Array.IndexOf(args, "--veces");
			if (iv >= 0 && iv + 1 < args.Length) veces = int.Parse(args[iv + 1]);

			var vivas = new List<IntPtr>();

			for (int vuelta = 1; vuelta <= veces; vuelta++)
			{
			// 3. LA CAJA, ARMADA DESDE CODIGO. Una por instancia: el wasi_config se consume.
			IntPtr wasi = W.wasi_config_new();
			W.wasi_config_inherit_stdout(wasi);
			W.wasi_config_inherit_stderr(wasi);

			// EL PAQUETE, DE SOLO LECTURA. El paso 2 midio que el modulo podia escribir adentro de
			// su propio `managed/` y reescribir sus dll. `fs_mutable: false` lo cierra sin
			// necesitar el .wasm de un solo archivo.
			//
			// Y MONTAR `/` TAPA A LOS DEMAS, medido el 2026-09-16: con `/` puesto, `/datos` y
			// `/modulo` dejan de resolver -- WASI busca dentro del preopen mas general y ahi no
			// estan, asi que TODO da IOException, incluido lo que tiene que poder. La jaula de
			// Gnothi (Cage.Build) NO monta `/` justamente por esto; el bundle en `/` es de la epoca
			// en que los dll del modulo vivian ahi. `--sin-raiz` lo apaga para igualar las dos.
			bool sinRaiz = Array.IndexOf(args, "--sin-raiz") >= 0;
			if (!sinRaiz && !W.wasi_config_preopen_dir(wasi, bundle, "/", false)) { Decir("no se pudo montar el paquete"); return 1; }
			// LA CARPETA CONCEDIDA, la unica que se puede escribir.
			if (!W.wasi_config_preopen_dir(wasi, concedida, "/datos", true)) { Decir("no se pudo montar la concedida"); return 1; }

			// LA JAULA DE DOS PUERTAS, que es la del sustrato separado: los dll del modulo entran
			// desde su bundle, montados en /modulo y de SOLO LECTURA. El CLI de wasmtime no sabe
			// expresar eso -- `--dir` no tiene modo -- asi que esta es la unica forma de comprobar
			// que `fs_mutable: false` cierra de verdad, y es la MISMA llamada que hace Cage.Build.
			int ipq = Array.IndexOf(args, "--paquete");
			if (ipq >= 0 && ipq + 1 < args.Length)
			{
				if (!W.wasi_config_preopen_dir(wasi, args[ipq + 1], "/modulo", false))
				{ Decir("no se pudo montar /modulo"); return 1; }
				if (vuelta == 1) Decir("montado: /modulo (SOLO LECTURA) desde " + args[ipq + 1]);
			}

			if (vuelta == 1) Decir("montado: paquete en / (solo lectura), concedida en /datos (escribible)");

			List<IntPtr> argv = [];
			foreach (string a in new[] { "dotnet.wasm" }.Concat2(delModulo))
				argv.Add(Marshal.StringToHGlobalAnsi(a));
			W.wasi_config_set_argv(wasi, (UIntPtr)argv.Count, argv.ToArray());

			IntPtr store = W.wasmtime_store_new(motor, IntPtr.Zero, IntPtr.Zero);
			vivas.Add(store);   // NO se borra: se quiere ver lo que N instancias VIVAS ocupan.
			IntPtr ctx = W.wasmtime_store_context(store);
			W.wasmtime_context_set_wasi(ctx, wasi);
			if (conRed) W.wasmtime_context_set_wasi_http(ctx);

			// 4. INSTANCIAR.
			err = W.wasmtime_component_linker_instantiate(linker, ctx, componente, out W.ComponentInstance instancia);
			if (Fallo(err, "instanciar")) return 1;
			Decir("INSTANCIADO                 (" + reloj.ElapsedMilliseconds + " ms)");

			// 5. CORRER. El modulo de hoy es un `command`: su entrada es wasi:cli/run#run.
			string[] candidatos = ["wasi:cli/run@0.2.0", "wasi:cli/run@0.2.1", "wasi:cli/run@0.2.2", "wasi:cli/run@0.2.3", "wasi:cli/run"];
			IntPtr idxInstancia = IntPtr.Zero;
			string usado = null;
			foreach (string c in candidatos)
			{
				idxInstancia = W.wasmtime_component_instance_get_export_index(ref instancia, ctx, IntPtr.Zero, c, (UIntPtr)c.Length);
				if (idxInstancia != IntPtr.Zero) { usado = c; break; }
			}
			if (idxInstancia == IntPtr.Zero) { Decir("no encontre wasi:cli/run en el componente"); return 1; }
			Decir("entrada encontrada: " + usado);

			IntPtr idxRun = W.wasmtime_component_instance_get_export_index(ref instancia, ctx, idxInstancia, "run", (UIntPtr)3);
			if (idxRun == IntPtr.Zero) { Decir("no encontre 'run' adentro de " + usado); return 1; }

			byte[] func = new byte[256];
			if (!W.wasmtime_component_instance_get_func(ref instancia, ctx, idxRun, func)) { Decir("no pude tomar la funcion"); return 1; }

			IntPtr resultados = Marshal.AllocHGlobal(256);
			for (int i = 0; i < 256; i++) Marshal.WriteByte(resultados, i, 0);

			Decir("--- adentro del modulo ---");
			long antesDeCorrer = reloj.ElapsedMilliseconds;
			err = W.wasmtime_component_func_call(func, ctx, IntPtr.Zero, UIntPtr.Zero, resultados, (UIntPtr)1);
			Decir("--- fin del modulo ---");
			if (Fallo(err, "correr")) return 1;

			Decir("CORRIO                      (" + (reloj.ElapsedMilliseconds - antesDeCorrer) + " ms adentro, " + reloj.ElapsedMilliseconds + " ms total)");

			// 6. LO QUE COSTO EN MEMORIA, que es el numero que P20 pide y no estaba medido.
			//    Acumulado y marginal: el primero paga la compilacion del componente, los
			//    siguientes ya no. La diferencia entre los dos ES la respuesta.
			var proc = Process.GetCurrentProcess();
			proc.Refresh();
			long ahora = proc.PrivateMemorySize64;
			Decir($"### instancia {vuelta}/{veces}: proceso {ahora / 1024 / 1024} MB"
				+ $"  (acumulado +{(ahora - procAntes) / 1024 / 1024} MB"
				+ $", marginal +{(ahora - anterior) / 1024 / 1024} MB)");
			anterior = ahora;
			}

			Decir("instancias vivas al terminar: " + vivas.Count);
			Decir("  monticulo administrado del Host: " + ((GC.GetTotalMemory(false) - memAntes) / 1024 / 1024) + " MB"
				+ "  <- todo lo demas es de wasmtime");

			foreach (IntPtr s in vivas) W.wasmtime_store_delete(s);
			W.wasmtime_component_linker_delete(linker);
			W.wasmtime_component_delete(componente);
			W.wasm_engine_delete(motor);
			return 0;
		}

		private static bool Fallo(IntPtr error, string que)
		{
			string m = W.Mensaje(error);
			if (m is null) return false;
			Decir("FALLO al " + que + ": " + m);
			return true;
		}

		private static void Decir(string s) => Console.WriteLine("gnothi: " + s);

		private static string[] Concat2(this string[] a, string[] b)
		{
			var r = new string[a.Length + b.Length];
			a.CopyTo(r, 0); b.CopyTo(r, a.Length);
			return r;
		}
	}
}
