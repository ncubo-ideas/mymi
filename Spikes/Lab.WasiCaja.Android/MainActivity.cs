using System;
using System.Collections.Generic;
using SD = System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using Lab.WasiCaja.Embebido;

namespace Lab.WasiCaja.Android
{
	// LA MEDICION QUE DECIDE. Paso 3 de F2 · P19, en el aparato.
	//
	// En la laptop, tres instancias vivas costaron +179 MB la primera y +72 MB cada una de las
	// siguientes. Ese numero NO se puede comparar con el de la forma hospedada: alla se midio PSS
	// en Android y aca memoria privada en Windows, con otro procesador y otro presupuesto.
	//
	// Asi que se repite aca con el MISMO instrumento con el que se midio la hospedada el
	// 2026-09-16 -- el PSS que reporta ActivityManager -- y sobre el mismo journal real.
	//
	// LA CIFRA CONTRA LA QUE SE COMPARA, medida ese dia en este mismo telefono:
	//     base 238,7 MB · primer modulo +58,8 MB · modulos 2 a 5 entre +2,2 y +7,7 MB
	[Activity(Name = "com.ncubo.gnothi.spike.wasicaja.MainActivity", Label = "Caja de wasm", MainLauncher = true)]
	public partial class MainActivity : Activity
	{
		private TextView pantalla;
		private readonly List<string> dicho = [];

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			var scroll = new ScrollView(this);
			pantalla = new TextView(this) { TextSize = 11f };
			pantalla.SetPadding(24, 24, 24, 24);
			pantalla.SetTypeface(global::Android.Graphics.Typeface.Monospace, global::Android.Graphics.TypefaceStyle.Normal);
			scroll.AddView(pantalla);
			SetContentView(scroll);

			// CUANTAS INSTANCIAS, por intent. Un proceso FRESCO por cada N es la unica forma de que
			// el PSS no lo domine el ruido del arranque: se mide el mismo instante en cuatro corridas
			// distintas, no cuatro instantes de una.
			int veces = Intent?.GetIntExtra("veces", 3) ?? 3;
			bool limpiar = (Intent?.GetIntExtra("limpiar", 0) ?? 0) != 0;
			int romper = Intent?.GetIntExtra("romper", 0) ?? 0;
			bool jaulaMala = (Intent?.GetIntExtra("jaulamala", 0) ?? 0) != 0;
			bool bordeVivo = (Intent?.GetIntExtra("borde", 0) ?? 0) != 0;
			new System.Threading.Thread(() => Medir(veces, limpiar, romper, jaulaMala, bordeVivo)) { IsBackground = true }.Start();
		}

		private void Decir(string s)
		{
			global::Android.Util.Log.Info("CAJA", s);
			lock (dicho) dicho.Add(s);
			RunOnUiThread(() => { lock (dicho) pantalla.Text = string.Join("\n", dicho); });
		}

		// EL PSS DEL PROCESO, que es el mismo numero que `dumpsys meminfo` reporta como TOTAL PSS.
		private long PssMb()
		{
			var am = (ActivityManager)GetSystemService(ActivityService);
			global::Android.OS.Debug.MemoryInfo[] info = am.GetProcessMemoryInfo([SD.Process.GetCurrentProcess().Id]);
			return info[0].TotalPss / 1024;
		}

		private void Medir(int veces, bool limpiar, int romper, bool jaulaMala, bool bordeVivo)
		{
			try
			{
				string raiz = FilesDir.AbsolutePath;
				string bundle = Path.Combine(raiz, "bundle");
				string concedida = Path.Combine(raiz, "concedida");

				Decir("caja: copiando el modulo a " + raiz);
				CopiarAssets("bundle", bundle);
				if (Directory.Exists(concedida)) Directory.Delete(concedida, true);
				CopiarAssets("journal", concedida);
				Decir("caja: modulo copiado (" + new FileInfo(Path.Combine(bundle, "Lab.WasiCaja.wasm")).Length / 1024 / 1024 + " MB, un solo archivo)");

				long baseMb = PssMb();
				Decir("caja: PSS base = " + baseMb + " MB");

				// veces < 0: se queda ANTES de crear el motor. Hace falta para saber cuanto de la
				// memoria es del componente COMPILADO -- que se paga una vez para todos los modulos --
				// y cuanto es de cada instancia. Sin separarlos, el numero no dice nada.
				if (veces < 0)
				{
					Decir("caja: LISTO PARA MEDIR · sin motor, sin componente");
					System.Threading.Thread.Sleep(600000);
					return;
				}

				var reloj = SD.Stopwatch.StartNew();
				IntPtr motor = W.wasm_engine_new();
				Decir("caja: motor creado (" + reloj.ElapsedMilliseconds + " ms) PSS=" + PssMb() + " MB");

				// EL COMPONENTE: precompilado si ya existe, compilado si no.
				//
				// Compilar los 12 MB del runtime cuesta 1,2 s y 163 MB de memoria, y hasta acá se
				// pagaba EN CADA ARRANQUE de Gnothi. Precompilar lo mueve al momento de instalar el
				// modulo, que se hace una vez.
				//
				// EL ARTEFACTO NO VIAJA EN EL BUNDLE: es especifico de la version de wasmtime, del
				// procesador y de las opciones del motor. Lo produce el aparato la primera vez.
				string precompilado = Path.Combine(raiz, "dotnet.cwasm");
				if (limpiar && File.Exists(precompilado)) { File.Delete(precompilado); Decir("caja: borrado el precompilado"); }

				// QUE PASA SI EL ARTEFACTO NO COINCIDE. `deserialize_file` esta documentado como NO
				// seguro contra entrada arbitraria, asi que hay que saber si un artefacto roto
				// devuelve error o se lleva el proceso. Se rompe a proposito y se mira.
				if (romper > 0 && File.Exists(precompilado)) Romper(precompilado, romper);

				IntPtr componente = IntPtr.Zero;
				IntPtr err;
				bool sirvio = false;

				if (File.Exists(precompilado))
				{
					// EL RENGLON QUE WASMTIME NO CUBRE. Medido el 2026-09-16: valida la cabecera ELF y
					// la version del motor -- las dos con error limpio-- pero NO el contenido. Cuatro
					// kilobytes cambiados en medio del artefacto pasaron sin una advertencia y el
					// modulo corrio. Su propia cabecera lo dice: «not safe to receive arbitrary user
					// input».
					//
					// Asi que el artefacto es ENTRADA DE CONFIANZA, y quien responde por el es Gnothi:
					// lo produce el, vive en su carpeta privada, y se comprueba antes de abrirlo.
					long tH = reloj.ElapsedMilliseconds;
					string esperado = File.Exists(precompilado + ".sha256")
						? File.ReadAllText(precompilado + ".sha256").Trim() : null;
					string real = Huella(precompilado);

					if (esperado is null)
					{
						Decir("caja: el precompilado no trae huella; no se puede confiar");
						File.Delete(precompilado);
					}
					else if (!string.Equals(esperado, real, StringComparison.Ordinal))
					{
						Decir("caja: LA HUELLA NO COINCIDE (" + (reloj.ElapsedMilliseconds - tH) + " ms)");
						Decir("      esperaba " + esperado[..16] + "…  encontro " + real[..16] + "…");
						File.Delete(precompilado);
						File.Delete(precompilado + ".sha256");
						Decir("caja: descartado ANTES de abrirlo; se recompila");
					}
					else Decir("caja: huella verificada en " + (reloj.ElapsedMilliseconds - tH) + " ms");
				}

				if (File.Exists(precompilado))
				{
					long t0 = reloj.ElapsedMilliseconds;
					err = W.wasmtime_component_deserialize_file(motor, precompilado, out componente);
					string queDijo = W.Mensaje(err);
					if (queDijo is null)
					{
						sirvio = true;
						Decir("caja: DESERIALIZADO (" + (reloj.ElapsedMilliseconds - t0) + " ms, "
							+ new FileInfo(precompilado).Length / 1024 / 1024 + " MB en disco) PSS=" + PssMb() + " MB");
					}
					else
					{
						// EL CAMINO QUE HAY QUE TENER EN PRODUCCION: no sirve, se tira y se recompila.
						// Si esto se alcanza, wasmtime devolvio un error y NO mato el proceso, que era
						// la pregunta.
						Decir("caja: EL PRECOMPILADO NO SIRVIO -> " + queDijo.Replace("\n", " | "));
						File.Delete(precompilado);
						Decir("caja: descartado; se recompila");
					}
				}

				if (!sirvio)
				{
					byte[] bytes = File.ReadAllBytes(Path.Combine(bundle, "Lab.WasiCaja.wasm"));
					long t0 = reloj.ElapsedMilliseconds;
					err = W.wasmtime_component_new(motor, bytes, (UIntPtr)bytes.Length, out componente);
					if (Fallo(err, "compilar")) return;
					Decir("caja: COMPILADO (" + (reloj.ElapsedMilliseconds - t0) + " ms, " + bytes.Length / 1024 / 1024 + " MB) PSS=" + PssMb() + " MB");

					long t1 = reloj.ElapsedMilliseconds;
					err = W.wasmtime_component_serialize(componente, out W.ByteVec blob);
					if (Fallo(err, "serializar")) return;
					byte[] guardar = new byte[(int)blob.size];
					Marshal.Copy(blob.data, guardar, 0, guardar.Length);
					W.wasm_byte_vec_delete(ref blob);
					File.WriteAllBytes(precompilado, guardar);
					File.WriteAllText(precompilado + ".sha256", Huella(precompilado));
					Decir("caja: SERIALIZADO a disco (" + (reloj.ElapsedMilliseconds - t1) + " ms, "
						+ guardar.Length / 1024 / 1024 + " MB) + huella");
				}

				IntPtr linker = W.wasmtime_component_linker_new(motor);
				err = W.wasmtime_component_linker_add_wasip2(linker);
				if (Fallo(err, "wasip2")) return;
				err = W.wasmtime_component_linker_add_wasi_http(linker);
				if (Fallo(err, "wasi_http")) return;

				// EL CANARIO, que es lo que reemplaza al contador de intentos negados.
				//
				// POR QUE NO HAY CONTADOR. `WebModule.Violations` cuenta lo que la CSP RECHAZA, y
				// puede porque el navegador deja pasar la peticion y despues la niega: hay un evento.
				// Con los preopens de wasm no hay evento que contar: al invitado NUNCA se le entrego
				// el descriptor, asi que del otro lado no hay una negacion, hay una AUSENCIA. Y la
				// C API no expone gancho ninguno sobre el sistema de archivos -- `wasi_config` solo
				// tiene preopens y redireccion de stdio, y sombrear la implementacion de WASI
				// necesitaria definirla entera, que es el muro del mundo WIT (ver F8).
				//
				// LO QUE SI SE PUEDE, Y ES LO QUE D2 PEDIA: probar la jaula. Gnothi corre SU PROPIA
				// sonda con EXACTAMENTE la misma configuracion que le va a dar al modulo, y mira que
				// las fugas fallen. No cuenta lo que un modulo malo intenta -- eso es invisible por
				// construccion -- pero detecta lo unico que de verdad puede romperse: que la jaula
				// este mal armada.
				//
				// EN PRODUCCION LA SONDA TIENE QUE SER DE GNOTHI, un wasm chico suyo. Aca se reusa el
				// del modulo por comodidad del spike, y eso NO sirve en serio: un modulo malo mentiria
				// en su propio canario.
				if (!Canario(motor, componente, linker, bundle, concedida, raiz, jaulaMala)) return;

				// LA PRUEBA DEL BORDE VIVO EMBEBIDO, que decide el diseno de CajaShell. Ver Borde.cs.
				if (bordeVivo) { ProbarBordeVivo(motor, componente, linker, concedida, raiz); return; }

				long anterior = PssMb();
				Decir("caja: listo para instanciar. PSS=" + anterior + " MB");

				var vivas = new List<IntPtr>();
				for (int vuelta = 1; vuelta <= veces; vuelta++)
				{
					IntPtr wasi = W.wasi_config_new();
					// LA SALIDA DEL MODULO A UN ARCHIVO, porque en Android el stdout del proceso no va a
					// ningun lado y sin esto no hay forma de saber si el modulo CONTESTO o solo arranco.
					string salida = Path.Combine(raiz, "salida-" + vuelta + ".txt");
					W.wasi_config_set_stdout_file(wasi, salida);
					W.wasi_config_inherit_stderr(wasi);
					// LO UNICO QUE SE MONTA. Con el `.wasm` de un solo archivo el paquete del modulo ya
					// no es un archivo: esta ADENTRO del wasm. Asi que no hay nada que montar salvo la
					// carpeta concedida, y `/` deja de existir para el invitado.
					if (!W.wasi_config_preopen_dir(wasi, concedida, "/datos", true)) { Decir("caja: no monto la concedida"); return; }

					// CON EL .wasm DE UN SOLO ARCHIVO EL ENSAMBLADO YA NO SE NOMBRA. Con el bundle suelto
					// los argumentos eran `dotnet.wasm Lab.WasiCaja /datos`: el runtime necesitaba que le
					// dijeran cual dll cargar. Aca viene adentro, asi que el primer argumento de verdad es
					// la carpeta -- y dejar el nombre puesto hace que el modulo lo tome COMO la carpeta:
					//     Storage configuration key 'path' points to 'Lab.WasiCaja'
					List<IntPtr> argv = [
						Marshal.StringToHGlobalAnsi("Lab.WasiCaja.wasm"),
						Marshal.StringToHGlobalAnsi("/datos")];
					W.wasi_config_set_argv(wasi, (UIntPtr)argv.Count, argv.ToArray());

					IntPtr store = W.wasmtime_store_new(motor, IntPtr.Zero, IntPtr.Zero);
					vivas.Add(store);
					IntPtr ctx = W.wasmtime_store_context(store);
					W.wasmtime_context_set_wasi(ctx, wasi);
					W.wasmtime_context_set_wasi_http(ctx);

					long t0 = reloj.ElapsedMilliseconds;
					err = W.wasmtime_component_linker_instantiate(linker, ctx, componente, out W.ComponentInstance instancia);
					if (Fallo(err, "instanciar")) return;

					IntPtr idxInst = IntPtr.Zero;
					foreach (string c in new[] { "wasi:cli/run@0.2.0", "wasi:cli/run@0.2.1", "wasi:cli/run@0.2.2", "wasi:cli/run@0.2.3", "wasi:cli/run" })
					{
						idxInst = W.wasmtime_component_instance_get_export_index(ref instancia, ctx, IntPtr.Zero, c, (UIntPtr)c.Length);
						if (idxInst != IntPtr.Zero) break;
					}
					if (idxInst == IntPtr.Zero) { Decir("caja: no encontre wasi:cli/run"); return; }

					IntPtr idxRun = W.wasmtime_component_instance_get_export_index(ref instancia, ctx, idxInst, "run", (UIntPtr)3);
					byte[] func = new byte[256];
					if (!W.wasmtime_component_instance_get_func(ref instancia, ctx, idxRun, func)) { Decir("caja: sin funcion"); return; }

					IntPtr res = Marshal.AllocHGlobal(256);
					for (int i = 0; i < 256; i++) Marshal.WriteByte(res, i, 0);

					err = W.wasmtime_component_func_call(func, ctx, IntPtr.Zero, UIntPtr.Zero, res, (UIntPtr)1);
					long ms = reloj.ElapsedMilliseconds - t0;

					// LA SALIDA DEL MODULO SE LEE PASE LO QUE PASE. Si sale con error, lo unico que el
					// anfitrion ve es «Exited with i32 exit status 1» -- que no dice nada. Lo que dice
					// algo es lo que el modulo alcanzo a escribir.
					try
					{
						foreach (string l in File.ReadAllLines(Path.Combine(raiz, "salida-" + vuelta + ".txt")))
							if (l.Contains("Resumen") || l.Contains("ARRANCADO") || l.Contains("FALLO") || l.Contains("causado"))
								Decir("      " + l.Trim());
					}
					catch (Exception e) { Decir("      (sin salida: " + e.GetType().Name + ")"); }

					if (Fallo(err, "correr la vuelta " + vuelta)) return;

					long ahora = PssMb();
					Decir($"caja: ### instancia {vuelta}: {ms} ms · PSS {ahora} MB"
						+ $" (acumulado +{ahora - baseMb} MB, marginal +{ahora - anterior} MB)");
					anterior = ahora;

					// LO QUE CONTESTO EL MODULO, para que la medicion no sea de un arranque vacio.
					try
					{
						foreach (string l in File.ReadAllLines(Path.Combine(raiz, "salida-" + vuelta + ".txt")))
							if (l.Contains("Resumen") || l.Contains("ARRANCADO") || l.Contains("FALLO")) Decir("      " + l.Trim());
					}
					catch (Exception e) { Decir("      (sin salida: " + e.GetType().Name + ")"); }

					// UNA PAUSA PARA MEDIR DE AFUERA. El PSS que reporta ActivityManager desde adentro
					// esta limitado por Android y devuelve el mismo numero siempre; el bueno es el de
					// `dumpsys meminfo`, y hay que darle tiempo a quien lo pide.

				}

				// ASENTAR ANTES DE MEDIR: un GC y una espera, para que lo que se mida sea lo que
				// QUEDA vivo y no el pico del trabajo.
				GC.Collect();
				GC.WaitForPendingFinalizers();
				System.Threading.Thread.Sleep(3000);
				Decir("caja: LISTO PARA MEDIR · " + veces + " instancia(s) viva(s)");
				System.Threading.Thread.Sleep(600000);
			}
			catch (Exception ex)
			{
				Decir("caja: EXCEPCION " + ex.GetType().FullName + ": " + ex.Message);
				Decir(ex.StackTrace ?? "");
			}
		}

		// CUATRO MANERAS DE QUE UN ARTEFACTO NO COINCIDA, de la mas tonta a la mas realista.
		// La 4 es la que de verdad va a pasar: Gnothi se actualiza con otra version de wasmtime y
		// el artefacto de ayer ya no vale. Se simula tocando la version que el propio artefacto
		// lleva adentro.
		private void Romper(string ruta, int como)
		{
			byte[] b = File.ReadAllBytes(ruta);
			Decir("caja: artefacto de " + b.Length + " bytes; primeros 16: " + BitConverter.ToString(b, 0, 16));
			switch (como)
			{
				case 1:
					File.WriteAllBytes(ruta, b[..(b.Length / 2)]);
					Decir("caja: ROTO 1 -- truncado a la mitad");
					return;
				case 2:
					for (int i = b.Length / 2; i < b.Length / 2 + 4096; i++) b[i] ^= 0xFF;
					File.WriteAllBytes(ruta, b);
					Decir("caja: ROTO 2 -- 4 KB volteados en el medio");
					return;
				case 3:
					for (int i = 0; i < 16; i++) b[i] ^= 0xFF;
					File.WriteAllBytes(ruta, b);
					Decir("caja: ROTO 3 -- cabecera volteada");
					return;
				case 4:
					// LA VERSION QUE EL ARTEFACTO DECLARA. wasmtime la escribe como texto; encontrarla
					// y cambiarle un digito simula exactamente «este artefacto es de otra version».
					byte[] aguja = System.Text.Encoding.ASCII.GetBytes("48.0.2");
					int donde = Buscar(b, aguja);
					if (donde < 0) { Decir("caja: ROTO 4 -- no encontre la version adentro; no se rompio"); return; }
					b[donde] = (byte)'9';   // 48.0.2 -> 98.0.2
					File.WriteAllBytes(ruta, b);
					Decir("caja: ROTO 4 -- version cambiada a 98.0.2 en el offset " + donde);
					return;
			}
		}

		// LA HUELLA DEL ARTEFACTO. Leer 11 MB cuesta lo que cuesta leerlos, y cierra el unico caso
		// que wasmtime no detecta. Se guarda al lado, en la misma carpeta privada.
		private static string Huella(string ruta)
		{
			using var sha = System.Security.Cryptography.SHA256.Create();
			using var f = File.OpenRead(ruta);
			return Convert.ToHexString(sha.ComputeHash(f));
		}

		private static int Buscar(byte[] heno, byte[] aguja)
		{
			for (int i = 0; i + aguja.Length <= heno.Length; i++)
			{
				int j = 0;
				while (j < aguja.Length && heno[i + j] == aguja[j]) j++;
				if (j == aguja.Length) return i;
			}
			return -1;
		}

		private bool Fallo(IntPtr error, string que)
		{
			string m = W.Mensaje(error);
			if (m is null) return false;
			Decir("caja: FALLO al " + que + ": " + m);
			return true;
		}

		private void CopiarAssets(string desde, string hacia)
		{
			Directory.CreateDirectory(hacia);
			string[] hijos = Assets.List(desde);
			foreach (string h in hijos)
			{
				string ruta = desde + "/" + h;
				string[] nietos = Assets.List(ruta);
				if (nietos.Length > 0) { CopiarAssets(ruta, Path.Combine(hacia, h)); continue; }

				using Stream entrada = Assets.Open(ruta);
				using var salida = File.Create(Path.Combine(hacia, h));
				entrada.CopyTo(salida);
			}
		}
	}
}
