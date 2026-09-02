using System;
using System.IO;
using Gimnasio.App;
using Puppeteer;

namespace Gimnasio.Consola
{
	// EL CICLO COMPLETO CONTRA UN JOURNAL REAL.
	//
	// Es lo que `comprobar` NO puede ver: el binding de parametros, la resolucion de los
	// tipos por reflexion, los conjuntos cerrados alcanzables desde un comando, y la
	// forma de los carriers al pasar por el print.
	//
	// El capitulo 3 §8 lo dice sin vueltas: un dominio no esta terminado hasta que un
	// perform lo toco.
	internal static class Medir
	{
		internal static int Correr(string carpeta)
		{
			Directory.CreateDirectory(carpeta);
			Console.WriteLine("Journal: " + carpeta);
			Console.WriteLine();

			GimnasioApp app = new GimnasioApp("gimnasio");
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
			app.Start();

			GimnasioAPI api = new GimnasioAPI(app);

			// ---------------------------------------------- lo que manda el gimnasio

			Paso("el perfil", api.DeclararPerfil("femenino", 42, 165m));
			Paso("el perfil otra vez", api.DeclararPerfil("femenino", 42, 165m));
			Paso("un genero que no existe", api.DeclararPerfil("otro", 42, 165m));

			Paso("el objetivo", api.DeclararObjetivo());
			Paso("meta de cintura, con valor", api.AgregarMetaConValor("cintura", "bajar", 80m));
			Paso("meta de grasa, solo direccion", api.AgregarMetaSoloDireccion("grasa", "bajar"));

			DateTime plan1 = new DateTime(2026, 1, 5);
			DateTime plan2 = new DateTime(2026, 4, 7);

			Paso("plan 1", api.RegistrarPlan(plan1, 3));
			Paso("plan 1 otra vez", api.RegistrarPlan(plan1, 3));
			Paso("prensa en el plan 1", api.PrescribirEjercicio(plan1, "prensa", 40m));
			Paso("terminar el plan 1", api.TerminarPlan(plan1, new DateTime(2026, 4, 6)));
			Paso("terminarlo dos veces", api.TerminarPlan(plan1, new DateTime(2026, 4, 6)));

			Paso("plan 2", api.RegistrarPlan(plan2, 3));
			Paso("prensa en el plan 2", api.PrescribirEjercicio(plan2, "prensa", 55m));
			Paso("terminar el plan 2", api.TerminarPlan(plan2, new DateTime(2026, 7, 6)));

			for (int i = 0; i < 30; i++) api.Asistio(plan1.AddDays(i * 3));

			Paso("cintura de enero", api.RegistrarMedida(new DateTime(2026, 1, 10), "cintura", 92m));
			Paso("grasa de enero", api.RegistrarMedida(new DateTime(2026, 1, 10), "grasa", 38m));
			Paso("cintura de abril", api.RegistrarMedida(new DateTime(2026, 4, 9), "cintura", 88m));
			Paso("grasa de abril", api.RegistrarMedida(new DateTime(2026, 4, 9), "grasa", 35m));
			Paso("una medida inventada", api.RegistrarMedida(new DateTime(2026, 4, 9), "estatura", 165m));

			// ------------------------------------------------------------ las preguntas

			Console.WriteLine();
			Console.WriteLine("Las preguntas del Preguntas.md, cruzando el perform:");
			Console.WriteLine();

			Console.WriteLine("  veces que midieron la cintura : " + api.CuantasVecesSeMidio("cintura"));
			Console.WriteLine("  subio lo indicado en prensa   : " + api.CuantoSubioLoIndicado("prensa"));
			Console.WriteLine();

			Console.WriteLine("  como voy en cintura -los dos juicios a la vez-:");
			Console.WriteLine("  " + api.ComoVoy("cintura", 0m, 88m, 1m, 5m));
			Console.WriteLine();

			Console.WriteLine("  como voy en grasa:");
			Console.WriteLine("  " + api.ComoVoy("grasa", 21m, 33m, 1m, 5m));
			Console.WriteLine();

			Console.WriteLine("  el incumplimiento del plan 1:");
			Console.WriteLine("  " + api.IncumplimientoDelPlan(plan1));
			Console.WriteLine();

			Console.WriteLine("  planes que terminaron sin medicion -la ausencia-:");
			Console.WriteLine("  " + api.PlanesSinMedicion(14));

			Console.WriteLine();
			Console.WriteLine("Corre esto DOS veces sobre la misma carpeta: la segunda rechaza todo");
			Console.WriteLine("y los numeros no se mueven.");
			return 0;
		}

		// El marcador de rechazo es la propiedad EWI en la raiz del documento -regla 2-,
		// no que el texto empiece con esas letras: con el formatter JSON empieza con
		// una llave.
		private static void Paso(string que, string salida)
		{
			string s = (salida ?? "").Trim();
			bool rechazado = s.Contains("\"EWI\"");
			Console.WriteLine("  " + (rechazado ? "rechazado " : "entro     ") + que
				+ (rechazado ? "  <- " + Motivo(s) : ""));
		}

		private static string Motivo(string documento)
		{
			int i = documento.IndexOf("\"Error\"", StringComparison.Ordinal);
			if (i < 0) return documento;
			int abre = documento.IndexOf('"', i + 8);
			int cierra = abre < 0 ? -1 : documento.IndexOf('"', abre + 1);
			return abre < 0 || cierra < 0 ? documento : documento.Substring(abre + 1, cierra - abre - 1);
		}
	}
}
