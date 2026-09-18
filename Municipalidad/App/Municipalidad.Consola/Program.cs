using System;
using System.Threading.Tasks;

namespace Municipalidad.Consola
{
	/// <summary>
	/// La caratula de consola. Tres subcomandos: `medir carpeta`, el ciclo completo por la PUERTA contra
	/// un journal real; `refrescar carpeta`, el ciclo de INGESTA contra el endpoint de eventos; y
	/// `aportar carpeta`, lo que solo la persona puede poner y la institucion no publica. Correr
	/// cualquiera DOS veces sobre la misma carpeta tiene que rechazar todo la segunda vez, y los numeros
	/// no se pueden mover.
	/// <para>
	/// SEMBRAR EL JOURNAL DEL DEMO ES `refrescar` Y DESPUES `aportar`, no `medir`: solo la via del feed
	/// mueve el punto de control, que es lo unico con que el asistente sabe si entro algo nuevo. `medir`
	/// entra por la puerta y lo deja en cero.
	/// </para>
	/// <para>
	/// No nombra ni un tipo del dominio. Eso es lo que la saca de la lista de InternalsVisibleTo del
	/// dominio y lo que le quita su ProjectReference: las 91 comprobaciones del dominio puro que
	/// vivian aca se mudaron a Pruebas/, donde SI pueden ponerse rojas en CI.
	/// </para>
	/// <para>
	/// NINGUNO DE LOS TRES BORRA UN JOURNAL. Los tres escriben en la carpeta que se les da, y sobre una
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

				case "aportar":
					return Aportar.Correr(LaCarpeta(args, "municipalidad-aporte"));

				case "comprobar":
					return ComprobacionesDeLosCatalogos.Correr();

				default:
					Console.WriteLine("Comandos:  medir <carpeta>");
					Console.WriteLine("           refrescar <carpeta> [--url U] [--recurso R] [--id CEDULA]");
					Console.WriteLine("           aportar <carpeta>");
					Console.WriteLine("           comprobar");
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
