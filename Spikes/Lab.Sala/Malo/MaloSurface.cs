using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Malo.App
{
	// LOS MARCADORES, DECLARADOS ACA Y NO IMPORTADOS.
	//
	// El sustrato descubre la superficie por NOMBRE DE TIPO del atributo, igual que
	// InProcessShell. Asi que un ensamblado que no referencia nada de Gnothi entra a la sala con
	// solo llamar a sus atributos como corresponde. No es una trampa de esta prueba: es como
	// funciona el descubrimiento hoy, y vale la pena verlo escrito.
	[AttributeUsage(AttributeTargets.Class)]
	internal sealed class ModuleSurfaceAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Method)]
	internal sealed class QueryAttribute : Attribute { }

	// EL MODULO QUE SE PORTA MAL, ADENTRO DE LA SALA.
	//
	// Corre los mismos intentos que `Spikes/Lab.WasiCaja/LosIntentos.cs` corre adentro de la caja
	// de wasmtime, mas los de red -- que en la caja no hacen falta porque la syscall no existe, y
	// aca son la pregunta principal: el motor web SI tiene fetch.
	//
	// COMO SE CORRE, y esto es lo que Jonathan dejo escrito y hay que respetar: NO con un solo
	// modulo cargado. Primero se carga un modulo de verdad, se lo desaloja, y RECIEN AHI entra
	// este. Con la sala vacia los intentos dan «no pudo» porque no hay nada, no porque este
	// protegido -- el mismo error que el propio LosIntentos.cs documenta en su intento 6.
	[ModuleSurface]
	public sealed class MaloSurface
	{
		private readonly string concedida;

		// LA MISMA CONVENCION QUE EL RESTO: el string del constructor recibe la carpeta concedida.
		public MaloSurface(string carpetaConcedida) => concedida = carpetaConcedida ?? "";

		[Query]
		// ASINCRONA A PROPOSITO: el runtime del motor web es de un solo hilo, asi que esperar aca
		// adentro con GetResult() no se cuelga, tira PlatformNotSupportedException. El unico
		// camino es devolver la tarea y que la espere el sustrato.
		public async Task<object> resumen()
		{
			var r = new List<object>();

			// 0 · QUE VE DE SU PROPIA RAIZ. En la caja `/` no existe: no es un preopen. Aca el
			//     sistema de archivos es MEMFS y la raiz es de todos.
			Probar(r, "listar la raiz que ve el modulo", () =>
			{
				string[] e = Directory.GetFileSystemEntries("/");
				return e.Length + " entradas: " + string.Join(", ", e);
			});

			Probar(r, "listar /journal (donde monta la sala)", () =>
			{
				string[] e = Directory.GetFileSystemEntries("/journal");
				return e.Length + " entradas: " + string.Join(", ", e);
			});

			// 1 · LO QUE SI TIENE QUE PODER. Si esto falla, la sala no contiene: es un ladrillo.
			Probar(r, "leer MI PROPIA carpeta concedida", () =>
				Directory.GetFileSystemEntries(concedida).Length + " entradas");

			// 2 · LO DEL VECINO. Lo que quedo de la carga anterior: en la sala el vecino no es una
			//     carpeta con su nombre -- eso se cerro --, es una carpeta `cN` de una carga
			//     previa. Se busca cualquiera que no sea la propia y se intenta leerla entera.
			Probar(r, "listar y LEER lo de cargas anteriores", () =>
			{
				var dicho = new StringBuilder();
				long bytes = 0;
				int archivos = 0;

				foreach (string carpeta in Directory.GetDirectories("/journal"))
				{
					if (string.Equals(Path.GetFullPath(carpeta), Path.GetFullPath(concedida), StringComparison.Ordinal))
						continue;

					dicho.Append(Path.GetFileName(carpeta)).Append(' ');
					foreach (string a in Directory.EnumerateFiles(carpeta, "*", SearchOption.AllDirectories))
					{
						archivos++;
						bytes += File.ReadAllBytes(a).Length;
					}
				}

				return archivos == 0
					? "no habia ninguna otra carpeta"
					: $"{dicho.ToString().Trim()} -> {archivos} archivos, {bytes} BYTES LEIDOS";
			});

			// LOS NOMBRES, APARTE DE LOS BYTES. El desmontaje vacia los archivos pero NO los borra,
			// asi que el nombre sobrevive. Un nombre es un canal: un modulo puede escribir un
			// archivo llamado como el dato que quiere contarle al que venga despues, y el vaciado
			// no lo toca. Esta sonda lo lista para ver si eso es cierto.
			Probar(r, "leer los NOMBRES de lo que quedo de antes", () =>
			{
				var nombres = new List<string>();
				foreach (string carpeta in Directory.GetDirectories("/journal"))
				{
					if (string.Equals(Path.GetFullPath(carpeta), Path.GetFullPath(concedida), StringComparison.Ordinal))
						continue;

					foreach (string a in Directory.EnumerateFiles(carpeta, "*", SearchOption.AllDirectories))
						nombres.Add(a.Substring("/journal/".Length));
				}

				return nombres.Count == 0 ? "nada" : string.Join(", ", nombres);
			});

			Probar(r, "ESCRIBIR en la carpeta de una carga anterior", () =>
			{
				foreach (string carpeta in Directory.GetDirectories("/journal"))
				{
					if (string.Equals(Path.GetFullPath(carpeta), Path.GetFullPath(concedida), StringComparison.Ordinal))
						continue;

					File.WriteAllText(Path.Combine(carpeta, "estuve-aqui.txt"), "un modulo paso por aqui");
					return "escrito en " + Path.GetFileName(carpeta);
				}
				return "no habia donde escribir";
			});

			// 3 · AFUERA DE TODO. En un telefono, esto es el disco del aparato. Adentro del motor
			//     web no hay disco -- la pregunta es si el runtime lo simula o no.
			foreach (string afuera in new[] { "/sdcard", "/data", "/data/data", "/storage/emulated/0", "/system", "/etc" })
			{
				string cual = afuera;
				Probar(r, "listar AFUERA " + cual, () =>
					Directory.GetFileSystemEntries(cual).Length + " entradas");
			}

			Probar(r, "leer /etc/hosts", () => File.ReadAllBytes("/etc/hosts").Length + " bytes");

			// 4 · EL CAMINO DE VUELTA. `..` desde la concedida.
			Probar(r, "salir con .. desde la concedida", () =>
				Directory.GetFileSystemEntries(Path.Combine(concedida, "..")).Length + " entradas");

			// 5 · LA RED, Y ESTA ES LA PREGUNTA DE LA SALA.
			//
			// En la caja no hay que preguntarla: sin `wasi-http` la syscall no existe. Aca el
			// modulo corre sobre el motor web, donde HttpClient se implementa sobre fetch. Lo
			// unico que lo limita es el ORIGEN de la pagina y la CSP -- las dos las pone Gnothi.
			// Nunca lo habiamos medido.
			// CONTROL: sin los de Internet, que en el emulador tardan mas que el tope de la sala.
			
			await Red(r, "pedir por el origen de la sala: su propio indice", "modulo/malow/indice.json");

			// Y LO QUE MAS IMPORTA DE TODO: el origen de la sala sirve journals. Si desde adentro
			// se puede pedir el de OTRO modulo por HTTP, el desmontaje entero es decorativo, que es
			// exactamente lo que Jonathan razono sin ver el codigo.
			await Red(r, "pedir el journal de comprasw por HTTP", "modulo/comprasw/journal/indice.json");
			await Red(r, "pedir un .dll de comprasw por HTTP", "modulo/comprasw/dominio/Compras.Dominio.dll");
			await Red(r, "pedir el journal de examenesw por HTTP", "modulo/examenesw/journal/indice.json");

			// INTERNET VA AL FINAL, Y NO ES ORDEN CAPRICHOSO. Con la CSP puesta esto falla en el
			// acto; con la CSP aflojada el pedido SALE y se queda esperando la red del emulador,
			// que tarda mas que el tope de la sala. Poniendolo ultimo, todo lo anterior ya quedo
			// medido aunque la consulta termine cortada, y el corte mismo es el dato: cambiar
			// `connect-src` cambia «rechazo instantaneo» por «esperando la red».
			//
			// Y NO SE PONE EL LOOPBACK DEL HOST, que parecia el destino obvio: el servicio del
			// arnes atiende UNA conexion por vez, asi que el pedido del modulo queda encolado
			// detras de la consulta que lo esta esperando. Se traba mi banco de prueba, no la sala.
			// UN ORIGEN DE AFUERA QUE EL EMULADOR SI ALCANZA. Sin esto no se puede distinguir «la
			// CSP lo bloquea» de «el emulador no tiene salida», y ese es justo el tipo de
			// ambiguedad que hace inutil una medicion de contencion. Lo sirve un node en la
			// maquina, publicado adentro con `adb reverse tcp:8123 tcp:8123`.
			await Red(r, "salir a un origen de AFUERA alcanzable", "http://127.0.0.1:8123/salio");

			await Red(r, "salir a INTERNET (example.com)", "https://example.com/");

			return new { concedida, intentos = r };
		}

		private static void Probar(List<object> r, string que, Func<string> intento)
		{
			try { r.Add(new { que, veredicto = "PUDO", detalle = intento() }); }
			catch (Exception ex)
			{
				Exception real = ex.InnerException ?? ex;
				string m = real.Message;
				if (m.Length > 90) m = m.Substring(0, 90) + "...";
				r.Add(new { que, veredicto = "NO PUDO", detalle = real.GetType().Name + ": " + m });
			}
		}

		// El HttpClient del motor web resuelve las rutas relativas contra el origen del documento,
		// que es el de la sala. Una absoluta sale a donde diga.
		private static async Task Red(List<object> r, string que, string url)
		{
			try
			{
				// SOLO `GetByteArrayAsync`, Y ESO YA ES UN DATO.
				//
				// Dos intentos anteriores dieron «NO PUDO» por la razon equivocada: el trimmer del
				// sustrato se llevo el setter de `Timeout` primero y `GetAsync` despues, asi que
				// los ocho intentos de red fallaban con MissingMethodException antes de tocar la
				// red. Es el error que este laboratorio existe para no cometer, y lo cometimos dos
				// veces seguidas.
				//
				// Lo que el sustrato SI conserva es lo que el sustrato usa: GetStringAsync y
				// GetByteArrayAsync, con las que se baja los .dll. Por ahi se mide de verdad.
				//
				// Y ojo con leer el recorte como contencion: que falte un metodo no es una pared,
				// es una superficie de API mas chica. Lo que decide si el modulo sale o no es la
				// CSP del documento, no el trimmer.
				using (var http = new HttpClient())
				{
					Uri destino = url.StartsWith("http", StringComparison.Ordinal)
						? new Uri(url)
						: new Uri(new Uri("https://sala.gnothi.local/"), url);

					byte[] cuerpo = await http.GetByteArrayAsync(destino);
					r.Add(new { que, veredicto = "PUDO", detalle = "TRAJO " + cuerpo.Length + " bytes" });
				}
			}
			catch (Exception ex)
			{
				Exception real = ex.InnerException ?? ex;
				string m = real.Message;
				if (m.Length > 110) m = m.Substring(0, 110) + "...";
				r.Add(new { que, veredicto = "NO PUDO", detalle = real.GetType().Name + ": " + m });
			}
		}
	}
}
