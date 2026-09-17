using System;
using System.IO;
using System.Net.Http;

namespace Lab.WasiCaja
{
	// PASO 2 · LA CONTENCION, FALSABLE. (F2 · P19)
	//
	// D2 pide que la contencion se pueda INTENTAR ROMPER y verla fallar. Hoy no se puede: el dll
	// de la institucion corre en el proceso de Gnothi y no hay cerca que intentar. Lo unico que
	// impide que un modulo abra el journal de otro es que no lo hace.
	//
	// ESTE ARCHIVO ES UN MODULO QUE SE PORTA MAL, a proposito. Corre EL MISMO codigo en los dos
	// anfitriones -- dentro de wasmtime y como .NET nativo -- y lo que se compara es el resultado.
	// Si los dos contestan lo mismo, la caja no aporta nada. Si difieren, la diferencia ES la
	// contencion, medida en vez de prometida.
	//
	// NINGUNO DE ESTOS INTENTOS ES UN ATAQUE: son las cuatro cosas que un modulo cualquiera puede
	// hacer con la biblioteca base, sin trucos y sin codigo nativo. Esa es justamente la
	// incomodidad.
	internal static class LosIntentos
	{
		// EL ARCHIVO QUE SI TIENE QUE PODER LEER LO DICE EL ANFITRION, y eso dejo de ser un detalle
		// cuando el canario se mudo a Gnothi. La primera version lo tenia escrito adentro --
		// `<concedida>/examenes/journal/journal_000001.bin` -- y eso ataba la sonda A UN MODULO: el
		// canario de Gnothi corre sobre una carpeta suya, de juguete, donde no hay ningun examenes.
		// Con el nombre adentro, el intento 1 fallaba y el veredicto salia «la caja no sirve».
		internal static void Correr(string concedida, string vecino, string afuera, string propio,
			string propioPaquete)
		{
			Console.WriteLine("intentos: concedida = " + concedida);
			Console.WriteLine("intentos: vecino    = " + vecino);
			Console.WriteLine("intentos: afuera    = " + afuera);
			Console.WriteLine("intentos: propio    = " + propio);
			Console.WriteLine("intentos: paquete   = " + propioPaquete);
			Console.WriteLine();

			// 1. LO QUE SI TIENE QUE PODER. Si esto falla, la caja no sirve: un modulo que no
			//    alcanza su propio journal no es contencion, es un ladrillo.
			Probar("leer MI PROPIO journal", () => File.ReadAllBytes(propio).Length + " bytes");

			// 2. EL JOURNAL DEL VECINO. Otra institucion, la misma persona, el mismo aparato.
			Probar("listar la carpeta de OTRO modulo", () =>
				Directory.GetFileSystemEntries(vecino).Length + " entradas");

			Probar("leer un archivo de OTRO modulo", () =>
				File.ReadAllBytes(Path.Combine(vecino, "marca.txt")).Length + " bytes");

			Probar("ESCRIBIR en la carpeta de otro modulo", () =>
			{
				File.WriteAllText(Path.Combine(vecino, "estuve-aqui.txt"), "un modulo pasó por aquí");
				return "escrito";
			});

			// 3. AFUERA DE TODO. La carpeta del usuario, los archivos del Host.
			Probar("listar una carpeta de AFUERA", () =>
				Directory.GetFileSystemEntries(afuera).Length + " entradas");

			// 4. EL CAMINO DE VUELTA. `..` es la forma clasica de salir de una carpeta concedida.
			Probar("salir con .. desde la concedida", () =>
				Directory.GetFileSystemEntries(Path.Combine(concedida, "..")).Length + " entradas");

			// 5. LA RED. Un modulo no tiene por que salir: el cartero es de Gnothi. En proceso
			//    NADA se lo impide.
			Probar("salir a la RED por su cuenta", () =>
			{
				using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
				return "HTTP " + (int)http.GetAsync("http://127.0.0.1:8122/.well-known/gnothi/modules.json")
					.GetAwaiter().GetResult().StatusCode;
			});

			// 6. SU PROPIO PAQUETE, Y AHORA SON DOS INTENTOS PORQUE ES UNA PUERTA DE VERDAD.
			//
			//    Hasta que el sustrato se separo del modulo, el paquete viajaba ADENTRO del .wasm y
			//    no habia nada montado: este intento apuntaba a `/managed`, que no existia, y pasaba
			//    por la razon equivocada -- fallaba porque no habia nada, no porque estuviera
			//    protegido. Ahora el paquete se monta de verdad, asi que hay algo que comprobar.
			//
			//    LEERLO TIENE QUE PODER: es de donde el sustrato carga los dll del modulo. Si esto
			//    falla, la caja no es una caja, es una pared.
			Probar("leer MI PROPIO paquete", () => File.ReadAllBytes(propioPaquete).Length + " bytes");

			//    ESCRIBIRLO NO. Un modulo que puede escribir su propio dll reescribe el codigo que
			//    corre la proxima vez, y ninguna firma lo detectaria: el paquete se verifico al dar
			//    el alta, no en cada arranque. Lo que lo impide es `fsMutable: false` en el preopen,
			//    no la buena educacion del modulo.
			Probar("ESCRIBIR en su propio paquete", () =>
			{
				File.WriteAllText(Path.Combine(Path.GetDirectoryName(propioPaquete), "estuve-aqui.txt"),
					"un modulo se reescribio");
				return "escrito";
			});

			// 7. Y LA RAIZ, que dice que MAS ve el modulo de lo que le concedieron. Con dos preopens
			//    montados, `/` sigue sin ser uno: no existe.
			Probar("listar la raiz que ve el modulo", () =>
			{
				string[] e = Directory.GetFileSystemEntries("/");
				return e.Length + " entradas: " + string.Join(", ", e);
			});
		}

		private static void Probar(string que, Func<string> intento)
		{
			try
			{
				string r = intento();
				Console.WriteLine($"  PUDO      {que,-42} -> {r}");
			}
			catch (Exception ex)
			{
				Exception real = ex.InnerException ?? ex;
				string m = real.Message;
				if (m.Length > 70) m = m.Substring(0, 70) + "…";
				Console.WriteLine($"  NO PUDO   {que,-42} -> {real.GetType().Name}: {m}");
			}
		}
	}
}
