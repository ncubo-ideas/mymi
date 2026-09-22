using System;
using System.Diagnostics;
using System.IO;
#if CON_MODULO
using Choreography.Theater;
using Examenes.App;
using Puppeteer;
#endif

namespace Lab.WasiCaja
{
	// LA CAJA DE WASM, DEL LADO DE ADENTRO. Pasos 0 y 1 de F2 · P19.
	//
	// Es el modulo de la institucion corriendo dentro de wasmtime, con UNA carpeta montada y nada
	// mas, hablando por stdin/stdout. Del otro lado esta Gnothi, que no reflexiona sobre nada.
	//
	// DOS MODOS, y el segundo es el que importa:
	//   (sin argumentos extra)  arranca, mide y sale        -- paso 0
	//   --borde                 arranca y atiende pedidos   -- paso 1
	//
	// QUE EL ACTOR SIGA VIVO ENTRE PEDIDOS es la razon por la que esto es un bucle y no un
	// programa por llamada: rehidratar en cada pregunta es exactamente lo que el modelo de actor
	// existe para no hacer. Es la misma forma que StdioShell ya tiene en el Host, con wasmtime en
	// lugar de un proceso.
	internal static class Program
	{
		private static int Main(string[] args)
		{
			string carpeta = args.Length > 0 ? args[0] : "/datos";
			bool borde = Array.IndexOf(args, "--borde") >= 0;
			int intentos = Array.IndexOf(args, "--intentos");

			// DONDE ESTA EL MODULO, DEL LADO DE ADENTRO, y solo lo usa el build del sustrato solo.
			// Lo dice el anfitrion porque es el que monto la carpeta: el modulo no elige donde
			// esta, igual que no elige su carpeta concedida.
			int delModulo = Array.IndexOf(args, "--modulo");
			string elModulo = delModulo >= 0 ? args[delModulo + 1] : "/modulo";
			string laEntrada = delModulo >= 0 && args.Length > delModulo + 2 ? args[delModulo + 2] : "Examenes.App.dll";

			// PASO 2: el modulo que se porta mal. No arranca el actor: lo que se mide es lo que el
			// ANFITRION le deja hacer, y eso no depende de que haya un journal abierto.
			if (intentos >= 0)
			{
				LosIntentos.Correr(carpeta, args[intentos + 1], args[intentos + 2], args[intentos + 3], args[intentos + 4]);
				return 0;
			}

			if (!borde)
			{
				Decir("arrancando");
				Decir("carpeta concedida = " + carpeta);
				Decir("existe            = " + Directory.Exists(carpeta));
				Decir("procesadores      = " + Environment.ProcessorCount);
			}

			// EL CANAL DEL PROTOCOLO SE APARTA ANTES QUE NADA, y el «antes que nada» es literal: la
			// primera version lo hacia despues de `app.Start()` y ahi el motor ya habia escrito el
			// progreso del replay -- 266 bytes de ruido en el canal. Arranca el actor DESPUES de
			// esto y el canal queda limpio.
			//
			// LOS DOS SON stdout Y NINGUNO ES `Console.Out`:
			//   - `Console.OpenStandardOutput()` da el descriptor 1 CRUDO, y lo sigue dando aunque
			//     despues se cambie `Console.Out`. Ese queda para el protocolo y nadie mas lo tiene.
			//   - `Console.SetOut` manda a un archivo TODO lo que escriba quien use `Console.Write`:
			//     el motor, la app, una libreria cualquiera.
			//
			// Arregla la trampa de E2 -- «stdout es el protocolo, y el motor no lo sabe» --, que
			// tumbo la primera version del Host con «'S' is an invalid start of a value».
			//
			// Lo correcto de verdad seria que el anfitrion defina las funciones que el invitado
			// importa (`instance_add_func`), y eso necesita un mundo WIT propio que el SDK de .NET
			// no expone. Ver F8. Esto resuelve el sintoma entero sin esperar a eso.
			StreamWriter protocolo = null;
			if (borde)
			{
				protocolo = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
				try
				{
					Console.SetOut(new StreamWriter(File.Create(Path.Combine(carpeta, "ruido-del-motor.log")))
						{ AutoFlush = true });
				}
				catch (Exception ex)
				{
					// Si no hay donde apartar el ruido, se avisa -- por el canal del protocolo, que
					// es el unico que el Host esta leyendo -- y no se arranca.
					protocolo.WriteLine("{\"listo\":false,\"dice\":" + Json.Texto(
						"no pude apartar el ruido del motor: " + ex.GetType().Name) + "}");
					return 1;
				}
			}

			try
			{
				var reloj = Stopwatch.StartNew();

#if CON_MODULO
				var app = new ExamenesApp("examenes");
				app.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
				app.Start();
				long arranque = reloj.ElapsedMilliseconds;
				long entradas = app.Actor.CurrentEntryId;

				object superficie = new ExamenesSurface(carpeta);

				if (!borde)
				{
					Decir("ACTOR ARRANCADO             (" + arranque + " ms)");
					Decir("entradas en el journal     = " + entradas);
					Decir("cursor del journal          = " + new ExamenesAPI(app).UltimoEventoProcesado());
					Decir("Resumen() contesto: " + Json.De(((ExamenesSurface)superficie).Resumen()));
					(superficie as IDisposable)?.Dispose();
					Decir("OK");
					return 0;
				}
#else
				// EL SUSTRATO SOLO. El modulo no esta adentro de este wasm: se carga de la carpeta
				// que el anfitrion monto, y quien arranca el actor es la propia superficie al
				// construirse -- igual que en InProcessShell, donde el Host tampoco lo arranca.
				//
				// `entradas` sale en -1 porque eso lo sabia `app.Actor`, que era del modulo. El
				// sustrato no tiene por que conocer el actor de nadie: es justo lo que se separa.
				object superficie = ElSustrato.Cargar(
					elModulo, Path.Combine(elModulo, laEntrada), carpeta,
					borde ? (_ => { }) : Decir);
				long arranque = reloj.ElapsedMilliseconds;
				long entradas = -1;

				if (!borde)
				{
					Decir("SUPERFICIE VIVA             (" + arranque + " ms)");
					Decir("OK");
					(superficie as IDisposable)?.Dispose();
					return 0;
				}
#endif

				// ---- EL BORDE ----------------------------------------------------------
				// Una linea por pedido, una linea por respuesta. El protocolo entero cabe en
				// tres campos porque el contrato ya decidio cuales son los verbos.
				var puerta = new ElBorde(superficie);

				// EL SALUDO: lo primero que el modulo dice es QUE OFRECE. El Host no lo averigua:
				// se lo dicen. Con el tiempo de arranque al lado, que es lo que le cuesta tenerlo.
				protocolo.WriteLine("{\"listo\":true,\"arranqueMs\":" + arranque
					+ ",\"entradas\":" + entradas
					+ ",\"declara\":" + puerta.Declaracion() + "}");

				string linea;
				while ((linea = Console.ReadLine()) is not null)
				{
					if (linea.Length == 0) continue;
					string[] partes = linea.Split('\t');
					string verbo = partes.Length > 0 ? partes[0] : "";
					string nombre = partes.Length > 1 ? partes[1] : "";
					string carga = partes.Length > 2 ? partes[2] : "";

					if (verbo == "chau") break;

					string respuesta;
					try { respuesta = puerta.Atender(verbo, nombre, carga); }
					catch (Exception ex)
					{
						// Lo que el modulo rompe se contesta como respuesta, no como caida: un
						// modulo que se muere se lleva el actor y la carpeta abierta con el.
						Exception real = ex.InnerException ?? ex;
						respuesta = "{\"ok\":false,\"dice\":" + Json.Texto(real.GetType().Name + ": " + real.Message) + "}";
					}

					protocolo.WriteLine(respuesta);
				}

				(superficie as IDisposable)?.Dispose();
				return 0;
			}
			catch (Exception ex)
			{
				Decir("FALLO " + ex.GetType().FullName + ": " + ex.Message);
				Console.WriteLine(ex.StackTrace);
				for (Exception i = ex.InnerException; i is not null; i = i.InnerException)
					Decir("  causado por " + i.GetType().FullName + ": " + i.Message);
				return 1;
			}
		}

		private static void Decir(string s) => Console.WriteLine("caja: " + s);
	}
}
