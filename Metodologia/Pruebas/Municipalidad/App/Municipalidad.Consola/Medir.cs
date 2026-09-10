using System;
using System.IO;
using Municipalidad.App;
using Puppeteer;

namespace Municipalidad.Consola
{
	// EL CICLO COMPLETO CONTRA UN JOURNAL REAL.
	//
	// Es lo que `comprobar` NO puede ver: el binding de parametros, la resolucion de los
	// tipos por reflexion, los conjuntos cerrados alcanzables desde un comando, la forma
	// de los carriers al pasar por el print, y el adaptador de los dos alfabetos.
	//
	// Se corre DOS veces sobre la misma carpeta: la segunda tiene que rechazar todo y los
	// numeros no se pueden mover. Capitulo 3 §8: un dominio no esta terminado hasta que un
	// perform lo toco.
	internal static class Medir
	{
		internal static int Correr(string carpeta)
		{
			Directory.CreateDirectory(carpeta);
			Console.WriteLine("Journal: " + carpeta);
			Console.WriteLine();

			MunicipalidadApp app = new MunicipalidadApp("municipalidad");
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
			app.Start();

			MunicipalidadAPI api = new MunicipalidadAPI(app);

			// ------------------------------------------ lo que aporta la persona

			Paso("la finca", api.IncorporarFinca("F-1"));
			Paso("la finca otra vez", api.IncorporarFinca("F-1"));
			Paso("el valor de la finca", api.DeclararValorDeFinca("F-1", new DateTime(2025, 1, 15), 60000000m));

			// ------------------------------------------- lo que manda la municipalidad
			//
			// Los periodos van COMO LOS ESCRIBE EL RECIBO -"2024.03", "2024.01"- y la API los
			// parte; el dominio los lee segun la cadencia del rubro.

			Paso("recibo R-1", api.RegistrarPago("R-1", new DateTime(2024, 3, 31), 0m));
			Paso("  basura 2024.03", api.AgregarLineaAlPago("R-1", "recoleccion-de-basura", "2024.03", 5000m, 0m));
			Paso("  policia 2024.03", api.AgregarLineaAlPago("R-1", "servicio-policia", "2024.03", 6000m, 0m));
			Paso("  bienes inmuebles 2024.01 -primer trimestre-", api.AgregarLineaAlPago("R-1", "bienes-inmuebles", "2024.01", 20000m, 0m));
			Paso("  un rubro que no existe", api.AgregarLineaAlPago("R-1", "cementerio", "2024.03", 1000m, 0m));
			Paso("  un trimestre que no existe", api.AgregarLineaAlPago("R-1", "bienes-inmuebles", "2024.05", 20000m, 0m));

			Paso("recibo R-2", api.RegistrarPago("R-2", new DateTime(2024, 6, 30), 0m));
			Paso("  basura 2024.06 -tres meses-", api.AgregarLineaAlPago("R-2", "recoleccion-de-basura", "2024.06", 15000m, 0m));
			Paso("  policia 2024.06", api.AgregarLineaAlPago("R-2", "servicio-policia", "2024.06", 18000m, 0m));
			Paso("  bienes inmuebles 2024.02", api.AgregarLineaAlPago("R-2", "bienes-inmuebles", "2024.02", 20000m, 0m));
			Paso("  basura 2024.05, que ya esta cubierta", api.AgregarLineaAlPago("R-2", "recoleccion-de-basura", "2024.05", 5000m, 0m));

			Paso("recibo R-3", api.RegistrarPago("R-3", new DateTime(2025, 6, 30), 0m));
			Paso("recibo R-3 otra vez", api.RegistrarPago("R-3", new DateTime(2025, 6, 30), 0m));
			Paso("  basura 2025.06 -doce meses, tarifa nueva-", api.AgregarLineaAlPago("R-3", "recoleccion-de-basura", "2025.06", 96000m, 310m));
			Paso("  policia 2025.06", api.AgregarLineaAlPago("R-3", "servicio-policia", "2025.06", 73200m, 240m));
			Paso("  bienes inmuebles 2025.02 -cuatro trimestres, avaluo nuevo-", api.AgregarLineaAlPago("R-3", "bienes-inmuebles", "2025.02", 144000m, 470m));

			DateTime consulta = new DateTime(2025, 9, 3);
			Paso("la consulta del 3 de setiembre", api.RegistrarConsulta(consulta));
			Paso("  pendiente basura 2025.09", api.AgregarPendiente(consulta, "recoleccion-de-basura", "2025.09", 24000m, 45m));
			Paso("  pendiente policia 2025.09", api.AgregarPendiente(consulta, "servicio-policia", "2025.09", 18300m, 34m));
			Paso("  pendiente bienes inmuebles 2025.03", api.AgregarPendiente(consulta, "bienes-inmuebles", "2025.03", 36000m, 0m));

			Paso("la reserva que la persona adopto", api.AdoptarReserva(30000m, new DateTime(2025, 8, 1)));

			// ------------------------------------------------------------ las preguntas

			Console.WriteLine();
			Console.WriteLine("Las preguntas del Preguntas.md, cruzando el perform:");
			Console.WriteLine();

			Console.WriteLine("  el estado de la cuenta:");
			Console.WriteLine("  " + api.EstadoDeLaCuenta());
			Console.WriteLine();

			Console.WriteLine("  lo pagado en 2025 : " + api.LoPagadoEn(2025));
			Console.WriteLine();

			Console.WriteLine("  la serie de basura -con cuantos periodos cubre cada linea, inferido-:");
			Console.WriteLine("  " + api.HistorialDe("recoleccion-de-basura"));
			Console.WriteLine();

			Console.WriteLine("  por que pago mas de basura? -umbral 25 %, ruido 2 %, propuestos-:");
			Console.WriteLine("  " + api.VariacionDe("recoleccion-de-basura", 0.25m, 0.02m));
			Console.WriteLine();

			Console.WriteLine("  y de bienes inmuebles?");
			Console.WriteLine("  " + api.VariacionDe("bienes-inmuebles", 0.25m, 0.02m));
			Console.WriteLine();

			Console.WriteLine("  que se me vence, al 25 de setiembre, con 7 dias de aviso -la ausencia-:");
			Console.WriteLine("  " + api.VencimientosAl(new DateTime(2025, 9, 25), 7));
			Console.WriteLine();

			Console.WriteLine("  los atrasos al 5 de octubre, con el costo proyectado a 6,2 %:");
			Console.WriteLine("  " + api.AtrasosAl(new DateTime(2025, 10, 5), 0.062m));
			Console.WriteLine();

			Console.WriteLine("  la reserva que la app propone para 2025:");
			Console.WriteLine("  " + api.ReservaPropuestaPara(2025));
			Console.WriteLine();

			Console.WriteLine("  el faltante contra lo que la persona adopto, al 15 de setiembre:");
			Console.WriteLine("  " + api.FaltanteDeReservaAl(new DateTime(2025, 9, 15), 0.10m));
			Console.WriteLine();

			Console.WriteLine("  ahorro por pronto pago si pago todo el 3 de setiembre a 6,2 % : " + api.AhorroPorProntoPago(consulta, 0.062m));
			Console.WriteLine();

			Console.WriteLine("  periodos pagados sin comprobante segun la consulta:");
			Console.WriteLine("  " + api.PeriodosPagadosSinComprobante(consulta));

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
