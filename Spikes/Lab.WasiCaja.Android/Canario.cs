using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Android.App;
using Lab.WasiCaja.Embebido;

namespace Lab.WasiCaja.Android
{
	// EL CANARIO. Lo que reemplaza al contador de intentos negados, y no es un sustituto pobre:
	// contesta la pregunta que D2 hace -- «¿se puede intentar romper la contencion y verla fallar?»
	// -- que es distinta de «¿cuantas veces lo intento este modulo?».
	//
	// Gnothi arma una caja con EXACTAMENTE la configuracion que le va a dar al modulo, mete adentro
	// una sonda suya, y mira que las ocho fugas fallen. Si alguna pasa, la jaula esta mal armada y
	// Gnothi lo sabe ANTES de abrir un modulo de verdad.
	public partial class MainActivity
	{
		private const int FugasQueDebenFallar = 8;

		private bool Canario(IntPtr motor, IntPtr componente, IntPtr linker,
			string bundle, string concedida, string raiz, bool jaulaMala)
		{
			string cuaderno = Path.Combine(raiz, "canario.txt");
			if (File.Exists(cuaderno)) File.Delete(cuaderno);

			// LA MISMA CONFIGURACION, y esa es toda la gracia. Si esto se arma distinto que lo de
			// abajo, el canario deja de decir algo del modulo y pasa a decir algo de si mismo.
			IntPtr wasi = W.wasi_config_new();
			W.wasi_config_set_stdout_file(wasi, cuaderno);
			W.wasi_config_inherit_stderr(wasi);
			if (!W.wasi_config_preopen_dir(wasi, concedida, "/datos", true))
			{
				Decir("canario: no se pudo montar la concedida");
				return false;
			}

			// UNA JAULA MAL ARMADA, a proposito, para comprobar que el canario la ve. Montar la
			// carpeta de la app en / es exactamente lo que hacia falta con el bundle suelto, y es la
			// clase de regresion que nadie nota: el modulo sigue andando igual.
			if (jaulaMala)
			{
				W.wasi_config_preopen_dir(wasi, raiz, "/", true);
				Decir("canario: (jaula mal armada a proposito: la carpeta de la app montada en /)");
			}

			// LOS DOS BLANCOS: la carpeta de OTRO modulo, y algo de afuera del contenedor.
			foreach (string a in new[] { "Lab.WasiCaja.wasm", "/datos", "--intentos", "/vecino/municipalidad", "/Users" })
				argvTemporal.Add(Marshal.StringToHGlobalAnsi(a));
			W.wasi_config_set_argv(wasi, (UIntPtr)argvTemporal.Count, argvTemporal.ToArray());
			argvTemporal.Clear();

			IntPtr store = W.wasmtime_store_new(motor, IntPtr.Zero, IntPtr.Zero);
			IntPtr ctx = W.wasmtime_store_context(store);
			W.wasmtime_context_set_wasi(ctx, wasi);
			W.wasmtime_context_set_wasi_http(ctx);

			IntPtr err = W.wasmtime_component_linker_instantiate(linker, ctx, componente, out W.ComponentInstance inst);
			if (Fallo(err, "instanciar el canario")) return false;

			IntPtr idxInst = IntPtr.Zero;
			foreach (string c in new[] { "wasi:cli/run@0.2.0", "wasi:cli/run@0.2.1", "wasi:cli/run@0.2.2", "wasi:cli/run@0.2.3", "wasi:cli/run" })
			{
				idxInst = W.wasmtime_component_instance_get_export_index(ref inst, ctx, IntPtr.Zero, c, (UIntPtr)c.Length);
				if (idxInst != IntPtr.Zero) break;
			}
			IntPtr idxRun = W.wasmtime_component_instance_get_export_index(ref inst, ctx, idxInst, "run", (UIntPtr)3);
			byte[] func = new byte[256];
			if (!W.wasmtime_component_instance_get_func(ref inst, ctx, idxRun, func)) { Decir("canario: sin funcion"); return false; }

			IntPtr res = Marshal.AllocHGlobal(256);
			for (int i = 0; i < 256; i++) Marshal.WriteByte(res, i, 0);
			W.Mensaje(W.wasmtime_component_func_call(func, ctx, IntPtr.Zero, UIntPtr.Zero, res, (UIntPtr)1));
			W.wasmtime_store_delete(store);

			// LO QUE LA SONDA ALCANZO A HACER. Se cuenta lo que PUDO, no lo que dijo.
			var pudo = new List<string>();
			int noPudo = 0;
			try
			{
				foreach (string l in File.ReadAllLines(cuaderno))
				{
					string t = l.Trim();
					if (t.StartsWith("PUDO", StringComparison.Ordinal)) pudo.Add(t);
					else if (t.StartsWith("NO PUDO", StringComparison.Ordinal)) noPudo++;
				}
			}
			catch (Exception e) { Decir("canario: no pude leer el cuaderno: " + e.GetType().Name); return false; }

			// LA UNICA QUE TIENE QUE PODER es leer su propio journal. Si no puede, la caja no sirve;
			// si puede alguna otra, la caja no contiene.
			bool leeLoSuyo = pudo.Exists(p => p.Contains("MI PROPIO journal"));

			Decir($"canario: {noPudo} fugas cerradas de {FugasQueDebenFallar} · lee lo suyo: {leeLoSuyo}");
			foreach (string p in pudo) if (!p.Contains("MI PROPIO journal")) Decir("canario:   ABIERTA -> " + p);

			if (noPudo == FugasQueDebenFallar && leeLoSuyo)
			{
				Decir("canario: LA JAULA ESTA CERRADA");
				return true;
			}

			// UNA JAULA ABIERTA NO ES UNA ADVERTENCIA. Si el canario pasa, no se abre ningun modulo.
			Decir("canario: LA JAULA NO ESTA CERRADA -- no se abre ningun modulo");
			return false;
		}

		private readonly List<IntPtr> argvTemporal = [];
	}
}
