using System;
using System.IO;
using System.Text.Json;
using HolaMundo.App;
using HolaMundo.Dominio;
using Puppeteer;

namespace HolaMundo.Consola
{
	// EL ARNÉS. Dos comandos, y conviene correr los dos en ese orden:
	//
	//   comprobar          el dominio PURO: sin actor, sin journal, sin disco
	//   correr <carpeta>   el ciclo completo contra un journal de verdad
	//
	// La diferencia entre los dos es la lección: `comprobar` valida que el modelo
	// PIENSA bien; `correr` valida que el motor lo puede MOVER. Un modelo puede pasar
	// todas las comprobaciones y no sobrevivir un perform — por eso hacen falta las dos.
	internal static class Program
	{
		private static int fallas;

		private static int Main(string[] args)
		{
			string comando = args.Length > 0 ? args[0] : "comprobar";

			switch (comando)
			{
				case "comprobar":
					return Comprobar();
				case "correr":
					return Correr(args.Length > 1
						? args[1]
						: Path.Combine(Path.GetTempPath(), "holamundo"));
				default:
					Console.WriteLine("Comandos:  comprobar  |  correr <carpeta>");
					return 1;
			}
		}

		// ------------------------------------------------------------ el dominio puro

		private static int Comprobar()
		{
			Console.WriteLine("El dominio, sin actor y sin disco:");
			Console.WriteLine();

			Linea linea = new Linea("Café", 3, 2500m);
			Ok("La línea deriva su total, no lo recibe", linea.Total == 7500m);
			Revienta("Una línea con cantidad cero no se construye",
				() => new Linea("Café", 0, 2500m));
			Revienta("Ni una con precio negativo",
				() => new Linea("Café", 1, -1m));

			Factura factura = Muestra("F-001");
			Ok("La factura suma sus líneas", factura.Total == 9900m);
			Ok("Y sabe cuántas tiene", factura.CantidadDeLineas == 2);
			Revienta("Tres listas de largos distintos no arman una factura",
				() => new Factura("F-002", DateTime.Today,
					new[] { "Café", "Azúcar" }, new[] { 1 }, new[] { 100m, 100m }));
			Revienta("Y una factura sin líneas tampoco",
				() => new Factura("F-003", DateTime.Today,
					new string[0], new int[0], new decimal[0]));

			Libro libro = new Libro();
			Ok("Un libro vacío puede recibir cualquier número", libro.PuedeAgregar("F-001"));
			libro.Agregar(factura);
			Ok("Agregada, la encuentra por número", libro["F-001"].Total == 9900m);
			Ok("Y el total del libro es la suma", libro.Total == 9900m);
			Ok("Ese número ya no se puede volver a agregar", !libro.PuedeAgregar("F-001"));
			Revienta("Y si igual se intenta, revienta",
				() => libro.Agregar(Muestra("F-001")));

			Console.WriteLine();
			Console.WriteLine(fallas == 0
				? "12 comprobaciones, todas bien."
				: fallas + " FALLARON.");
			return fallas == 0 ? 0 : 1;
		}

		private static Factura Muestra(string numero) => new Factura(
			numero, new DateTime(2026, 3, 1),
			new[] { "Café", "Azúcar" }, new[] { 3, 2 }, new[] { 2500m, 1200m });

		private static void Ok(string dice, bool cierto)
		{
			Console.WriteLine((cierto ? "  ok    " : "  FALLA ") + dice);
			if (!cierto) fallas++;
		}

		private static void Revienta(string dice, Action accion)
		{
			try { accion(); Ok(dice, false); }
			catch (Exception) { Ok(dice, true); }
		}

		// -------------------------------------------------------- el ciclo con journal

		private static int Correr(string carpeta)
		{
			Directory.CreateDirectory(carpeta);
			Console.WriteLine("Journal: " + carpeta);
			Console.WriteLine();

			// El MISMO par que hace la carátula del browser; allá el path va a MEMFS.
			HolaMundoApp app = new HolaMundoApp("holamundo");
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
			app.Start();

			HolaMundoAPI api = new HolaMundoAPI(app);

			Console.WriteLine("  F-001           -> " + Veredicto(api.AgregarFactura(
				"F-001", new DateTime(2026, 3, 1),
				new[] { "Café", "Azúcar" }, new[] { 3, 2 }, new[] { 2500m, 1200m })));

			// LA MISMA otra vez. El Check la rechaza y NO deja entrada en el journal.
			// Eso es la idempotencia, y es lo que hace seguro reintentar.
			Console.WriteLine("  F-001 de nuevo  -> " + Veredicto(api.AgregarFactura(
				"F-001", new DateTime(2026, 3, 1),
				new[] { "Pan" }, new[] { 1 }, new[] { 900m })));

			Console.WriteLine("  F-002           -> " + Veredicto(api.AgregarFactura(
				"F-002", new DateTime(2026, 3, 15),
				new[] { "Pan" }, new[] { 4 }, new[] { 900m })));

			Console.WriteLine();
			Console.WriteLine("  Facturas en el libro : " + api.CantidadDeFacturas());
			Console.WriteLine("  Total                : " + api.TotalDeFacturas());
			Console.WriteLine();
			Console.WriteLine("  La lista, tal como sale del print:");
			Console.WriteLine("  " + api.ListaDeFacturas());
			Console.WriteLine();
			Console.WriteLine("Corré esto DOS veces sobre la misma carpeta. La segunda no");
			Console.WriteLine("duplica nada: el journal ya las tiene y el Check las rechaza.");
			return 0;
		}

		// CÓMO SE SABE SI ENTRÓ O SE RECHAZÓ.
		//
		// Con el formatter JSON, un rechazo llega así:
		//     {"EWI":[{"Error":"esa factura ya estaba en el libro"}]}
		// así que el marcador es la PROPIEDAD `EWI` en la raíz — NO que el texto empiece
		// con esas tres letras, porque empieza con `{`. Y no alcanza con "devolvió algo":
		// un comando aceptado que imprime también devuelve texto.
		private static string Veredicto(string salida)
		{
			if (string.IsNullOrWhiteSpace(salida)) return "entró";

			using (JsonDocument documento = JsonDocument.Parse(salida))
			{
				if (!documento.RootElement.TryGetProperty("EWI", out JsonElement ewi))
					return "entró";

				foreach (JsonElement aviso in ewi.EnumerateArray())
					if (aviso.TryGetProperty("Error", out JsonElement error))
						return "RECHAZADA — " + error.GetString();

				return "RECHAZADA";
			}
		}
	}
}
