using System;
using System.Threading.Tasks;

namespace Municipalidad.Consola
{
	/// <summary>
	/// La caratula de consola. Dos subcomandos: `medir carpeta`, el ciclo completo por la PUERTA contra
	/// un journal real, y `refrescar carpeta`, el ciclo de INGESTA contra el endpoint de eventos. Los dos
	/// se corren DOS veces sobre la misma carpeta -- la segunda tiene que rechazar todo y los numeros no
	/// se pueden mover.
	/// <para>
	/// No nombra ni un tipo del dominio. Eso es lo que la saca de la lista de InternalsVisibleTo del
	/// dominio y lo que le quita su ProjectReference: las 91 comprobaciones del dominio puro que
	/// vivian aca se mudaron a Pruebas/ en M-6, donde SI pueden ponerse rojas en CI.
	/// </para>
	/// <para>
	/// NINGUNO DE LOS DOS BORRA UN JOURNAL. Los dos escriben en la carpeta que se les da, y sobre una
	/// carpeta que ya tiene una cuenta lo que hacen es agregarle hechos.
	/// </para>
	/// </summary>
	internal static class Program
	{
		private static async Task<int> Main(string[] args)
		{
			string comando = args.Length > 0 ? args[0] : "medir";

			switch (comando)
			{
				case "medir":
					return Medir.Correr(LaCarpeta(args, "municipalidad"));

				case "refrescar":
					return await Refrescar.CorrerAsync(
						LaCarpeta(args, "municipalidad-refresh"),
						Argumento(args, "--url", "http://localhost:8121/"),
						Argumento(args, "--recurso", "contribuyente"),
						Argumento(args, "--id", "CEDULA-DEMO-1"));

				default:
					Console.WriteLine("Comandos:  medir <carpeta>");
					Console.WriteLine("           refrescar <carpeta> [--url U] [--recurso R] [--id CEDULA]");
					return 1;
			}
		}

		private static string LaCarpeta(string[] args, string porDefecto)
		{
			return args.Length > 1 && !args[1].StartsWith("--")
				? args[1]
				: System.IO.Path.Combine(System.IO.Path.GetTempPath(), porDefecto);
		}

		private static string Argumento(string[] args, string nombre, string porDefecto)
		{
			for (int cual = 0; cual < args.Length - 1; cual++)
				if (string.Equals(args[cual], nombre, StringComparison.OrdinalIgnoreCase))
					return args[cual + 1];

			return porDefecto;
		}
	}
}
