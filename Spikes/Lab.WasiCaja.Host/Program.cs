using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Lab.WasiCaja.Host
{
	// GNOTHI HABLANDOLE A UN MODULO QUE NO CARGO. Paso 1 de la caja de wasm (F2 · P19).
	//
	// LA PRUEBA ES LO QUE ESTE ARCHIVO NO HACE: no hay Assembly.Load, no hay Type, no hay
	// MethodInfo, no hay una referencia al ensamblado de la institucion ni al contrato. Lo unico
	// que cruza son lineas de texto.
	//
	// Y LA CARPETA ES EL PUNTO. `--dir <concedida>::/datos` es TODO lo que el modulo puede ver
	// del sistema de archivos. Hoy eso es disciplina -- el dll corre en el proceso del Host y
	// podria abrir cualquier cosa -- y aca es una linea de comando.
	internal static class Program
	{
		private static int Main(string[] args)
		{
			if (args.Length < 3)
			{
				Console.Error.WriteLine("uso: Lab.WasiCaja.Host <wasmtime> <AppBundle> <carpeta concedida>");
				return 2;
			}

			string wasmtime = args[0];
			string bundle = Path.GetFullPath(args[1]);
			string concedida = Path.GetFullPath(args[2]);

			Decir("modulo    = " + bundle);
			Decir("concedida = " + concedida);

			var arranque = new ProcessStartInfo(wasmtime)
			{
				WorkingDirectory = bundle,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
			};
			foreach (string a in new[]
			{
				"run", "-S", "http",
				// LO QUE EL MODULO VE, y no hay nada mas. El bundle de solo lectura para poder
				// leerse a si mismo, y la carpeta concedida montada donde el modulo la espera.
				// LO UNICO QUE SE MONTA. Con el `.wasm` de un solo archivo el paquete del modulo va
				// adentro del wasm, asi que no hay nada mas que montar y `/` no existe para el
				// invitado. Ver F8, paso 2.
				"--dir", concedida + "::/datos",
				"Lab.WasiCaja.wasm", "/datos", "--borde",
			})
				arranque.ArgumentList.Add(a);

			var reloj = Stopwatch.StartNew();
			using Process caja = Process.Start(arranque);
			caja.ErrorDataReceived += (_, e) => { if (e.Data is { Length: > 0 }) Decir("  [caja] " + e.Data); };
			caja.BeginErrorReadLine();

			// 1. EL SALUDO. El modulo dice que ofrece; el Host no lo averigua.
			string saludo = Leer(caja);
			Decir("saludo en " + reloj.ElapsedMilliseconds + " ms:");
			Decir("  " + saludo);

			using JsonDocument doc = JsonDocument.Parse(saludo);
			JsonElement declara = doc.RootElement.GetProperty("declara");
			Decir("  arranque del actor: " + doc.RootElement.GetProperty("arranqueMs").GetInt64() + " ms");
			Decir("  entradas: " + doc.RootElement.GetProperty("entradas").GetInt64());

			// 2. LAS PUERTAS, POR SEPARADO. El Host sabe que hay dos conjuntos porque el modulo
			//    los declara por separado; no porque los haya encontrado mirando adentro.
			Decir("  declara consultas (al asistente): " + declara.GetProperty("consultas").GetArrayLength());
			Decir("  declara vistas (a la pantalla)  : " + declara.GetProperty("vistas").GetArrayLength());

			string primeraConsulta = declara.GetProperty("consultas")[0].GetString();
			string primeraVista = declara.GetProperty("vistas")[0].GetString();

			// 3. PREGUNTAR POR NOMBRE, por cada puerta, y medir el viaje redondo.
			Medir(caja, "preguntar", primeraConsulta, "la consulta del asistente");
			Medir(caja, "consultar", primeraVista, "la vista de la pantalla");
			Medir(caja, "cursor", "", "hasta donde leyo");

			// 4. Y LA PRUEBA DE QUE LAS PUERTAS AISLAN: pedir una vista por la puerta del
			//    asistente tiene que fallar, aunque el metodo exista del otro lado. Ver B8.
			Medir(caja, "preguntar", primeraVista, "una VISTA pedida por la puerta del asistente");

			// 5. Repetir la primera, para separar el costo del borde del costo de la primera vez.
			Medir(caja, "preguntar", primeraConsulta, "la misma consulta, segunda vez");
			Medir(caja, "preguntar", primeraConsulta, "la misma consulta, tercera vez");

			// 6. EL CARTERO, que es el unico verbo que ESCRIBE.
			//
			// Gnothi trajo el sobre del publicador -- el modulo no tiene red, y en la caja eso no
			// es disciplina: no se le dio wasi-http -- y se lo entrega. El sobre es el mismo venga
			// por donde venga, y el modulo no sabe ni le importa quien lo trajo.
			//
			// LOS IDS VAN DESPUES DEL CURSOR (303), que es lo que el propio modulo contesto dos
			// lineas arriba. El cartero pide de ahi en adelante; no se inventa desde donde.
			const string sobre =
				"{\"account\":{\"state\":\"active\"},\"events\":[" +
				"{\"id\":400,\"tipo\":\"ExamenAgregado\",\"examen\":\"Hemoglobina\"," +
				"\"fecha\":\"2026-09-16T08:30:00\",\"valor\":13.8,\"unidad\":\"g/dL\"}," +
				"{\"id\":401,\"tipo\":\"ExamenAgregado\",\"examen\":\"Colesterol\"," +
				"\"fecha\":\"2026-09-16T08:30:00\",\"valor\":195.0,\"unidad\":\"mg/dL\"}" +
				"],\"last\":401}";

			Decir("--- el cartero entrega dos examenes nuevos ---");
			MedirConCarga(caja, "entregar", "", sobre, "entregar el sobre del publicador");
			Medir(caja, "cursor", "", "hasta donde leyo DESPUES de entregar");
			Medir(caja, "preguntar", primeraConsulta, "la consulta DESPUES de entregar");

			// Y LA PRUEBA DE QUE NO SE ASIENTA DOS VECES: el mismo sobre otra vez. El cursor no se
			// mueve y la cuenta tampoco -- la idempotencia es del modulo, no del cartero.
			Decir("--- el mismo sobre, otra vez ---");
			MedirConCarga(caja, "entregar", "", sobre, "entregar el MISMO sobre");
			Medir(caja, "cursor", "", "hasta donde leyo despues del repetido");
			Medir(caja, "preguntar", primeraConsulta, "la consulta despues del repetido");

			caja.StandardInput.WriteLine("chau");
			caja.StandardInput.Flush();
			caja.WaitForExit(5000);

			Decir("ruido del motor en el canal del protocolo: " + ruido + " bytes");
			Decir("total " + reloj.ElapsedMilliseconds + " ms");
			return 0;
		}

		// Igual que Medir pero con carga, que es lo que `entregar` necesita: el sobre entero va en
		// el tercer campo. Por eso el protocolo separa con tabuladores y no con espacios.
		private static void MedirConCarga(Process caja, string verbo, string nombre, string carga, string que)
		{
			var reloj = Stopwatch.StartNew();
			caja.StandardInput.WriteLine(verbo + "\t" + nombre + "\t" + carga);
			caja.StandardInput.Flush();
			string respuesta = Leer(caja);
			long ms = reloj.ElapsedMilliseconds;
			string recorte = respuesta is null ? "(nada)"
				: respuesta.Length <= 150 ? respuesta : respuesta.Substring(0, 150) + "…";
			Decir($"{que,-48} {ms,5} ms  {recorte}");
		}

		private static void Medir(Process caja, string verbo, string nombre, string que)
		{
			var reloj = Stopwatch.StartNew();
			caja.StandardInput.WriteLine(verbo + "\t" + nombre + "\t");
			caja.StandardInput.Flush();
			string respuesta = Leer(caja);
			long ms = reloj.ElapsedMilliseconds;

			string recorte = respuesta is null ? "(nada)"
				: respuesta.Length <= 150 ? respuesta
				: respuesta.Substring(0, 150) + "…";

			Decir($"{que,-48} {ms,5} ms  {recorte}");
		}

		// STDOUT ES EL PROTOCOLO, Y EL MOTOR NO LO SABE. `Starting ... Actor` y el progreso de
		// replay (`1%2%3%`) salen por la MISMA salida por la que viaja la respuesta. Es la trampa
		// de E2 otra vez, en un sitio nuevo: alla rompia MCP sobre stdio, aca rompe el borde.
		//
		// SE SALTA LO QUE NO EMPIEZA CON LLAVE, y se dice. Es un parche de esta prueba, no la
		// solucion: en serio, el canal del borde no puede ser el mismo que el que cualquier
		// libreria usa para escribir. Ver F8.
		private static string Leer(Process caja)
		{
			string linea;
			while ((linea = caja.StandardOutput.ReadLine()) is not null)
			{
				string limpia = linea.TrimStart();
				int llave = limpia.IndexOf('{');
				if (llave == 0) return limpia;
				if (llave > 0) { ruido += llave; return limpia.Substring(llave); }
				ruido += linea.Length;
			}
			return null;
		}

		private static int ruido;

		private static void Decir(string s) => Console.WriteLine("gnothi: " + s);
	}
}
