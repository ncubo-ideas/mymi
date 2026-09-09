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

			// LAS DOS FORMAS DEL PAPEL (v3): "3x 12 a 15 · 00:30" y "15 minutos". Sin peso.
			Paso("plan 1", api.RegistrarPlan(plan1, 3));
			Paso("plan 1 otra vez", api.RegistrarPlan(plan1, 3));
			Paso("remo en el plan 1: 3x 12 a 15", api.PrescribirFuerza(plan1, "remo", 3, 12, 15, 30));
			Paso("remo otra vez", api.PrescribirFuerza(plan1, "remo", 3, 12, 15, 30));
			Paso("caminadora en el plan 1: 15 minutos", api.PrescribirCardio(plan1, "caminadora", 15));
			Paso("un rango invertido", api.PrescribirFuerza(plan1, "curl", 3, 15, 12, 30));
			Paso("terminar el plan 1", api.TerminarPlan(plan1, new DateTime(2026, 4, 6)));
			Paso("terminarlo dos veces", api.TerminarPlan(plan1, new DateTime(2026, 4, 6)));

			Paso("plan 2", api.RegistrarPlan(plan2, 3));
			Paso("remo en el plan 2: 4x 12 a 15", api.PrescribirFuerza(plan2, "remo", 4, 12, 15, 30));
			Paso("caminadora en el plan 2: 20 minutos", api.PrescribirCardio(plan2, "caminadora", 20));
			Paso("un peso para el remo -su maquina lo requiere-", api.IndicarPeso(plan2, "remo", 40m));
			Paso("el mismo peso otra vez", api.IndicarPeso(plan2, "remo", 45m));
			Paso("terminar el plan 2", api.TerminarPlan(plan2, new DateTime(2026, 7, 6)));

			for (int i = 0; i < 30; i++) api.Asistio(plan1.AddDays(i * 3));

			Paso("cintura de enero", api.RegistrarMedida(new DateTime(2026, 1, 10), "cintura", 92m));
			Paso("grasa de enero", api.RegistrarMedida(new DateTime(2026, 1, 10), "grasa", 38m));
			Paso("cintura de abril", api.RegistrarMedida(new DateTime(2026, 4, 9), "cintura", 88m));
			Paso("grasa de abril", api.RegistrarMedida(new DateTime(2026, 4, 9), "grasa", 35m));
			Paso("una medida inventada", api.RegistrarMedida(new DateTime(2026, 4, 9), "estatura", 165m));

			// LO QUE EL APARATO DIJO (v3, N7): la palabra al lado del valor, como hecho.
			Paso("meta de grasa visceral, solo direccion", api.AgregarMetaSoloDireccion("grasa-visceral", "bajar"));
			Paso("grasa visceral de enero, HIGH", api.RegistrarMedidaEvaluada(new DateTime(2026, 1, 10), "grasa-visceral", 14m, "high"));
			Paso("grasa visceral de abril, HIGH", api.RegistrarMedidaEvaluada(new DateTime(2026, 4, 9), "grasa-visceral", 12m, "high"));
			Paso("la misma otra vez", api.RegistrarMedidaEvaluada(new DateTime(2026, 4, 9), "grasa-visceral", 12m, "high"));
			Paso("agua de abril, INSUFFICIENT", api.RegistrarMedidaEvaluada(new DateTime(2026, 4, 9), "agua", 49m, "insufficient"));
			Paso("una palabra que el aparato nunca dijo", api.RegistrarMedidaEvaluada(new DateTime(2026, 4, 9), "masa-osea", 2.5m, "normal"));

			// -------------------------------------------------- la membresia (v2, N1 y N2)
			//
			// El gimnasio manda la bandera; el dominio deriva el periodo. Dos semanas en
			// mora dentro del plan 1 -del 2 al 16 de febrero- tienen que descontarle dos
			// semanas al incumplimiento de abajo: 33 esperados, no 39. Y la misma bandera
			// repetida tiene que RECHAZARSE, que es la leccion de Asistio.

			Paso("el producto: Black, vence el dia 10", api.Contratar("Black", 10, new DateTime(2026, 1, 5)));
			Paso("el mismo producto otra vez", api.Contratar("Black", 10, new DateTime(2026, 2, 1)));
			Paso("la bandera: activo en enero", api.CambiarEstadoDeMembresia("activo", new DateTime(2026, 1, 5)));
			Paso("la palabra del v2, al-dia", api.CambiarEstadoDeMembresia("al-dia", new DateTime(2026, 1, 6)));
			Paso("la bandera: no activa en febrero", api.CambiarEstadoDeMembresia("no-activa", new DateTime(2026, 2, 2)));
			Paso("la misma bandera otra vez", api.CambiarEstadoDeMembresia("no-activa", new DateTime(2026, 2, 9)));
			Paso("la bandera: activo otra vez", api.CambiarEstadoDeMembresia("activo", new DateTime(2026, 2, 16)));
			Paso("un estado que no existe", api.CambiarEstadoDeMembresia("suspendida", new DateTime(2026, 3, 1)));
			Paso("un pago", api.RegistrarPago(new DateTime(2026, 1, 5), 25000m));
			Paso("el mismo pago otra vez", api.RegistrarPago(new DateTime(2026, 1, 5), 25000m));
			// El cobro como en el papel: cubre del 24 al 23 y vence el 10, adentro del periodo.
			Paso("el cobro de enero: cubre 24/01-23/02, vence 10/02", api.AnunciarCobro(new DateTime(2026, 2, 10), 25000m, "Enero",
				new DateTime(2026, 1, 24), new DateTime(2026, 2, 23), "abierto"));
			Paso("el mismo vencimiento otra vez", api.AnunciarCobro(new DateTime(2026, 2, 10), 25000m, "Enero",
				new DateTime(2026, 1, 24), new DateTime(2026, 2, 23), "abierto"));
			Paso("un cobro que se solapa con el de enero", api.AnunciarCobro(new DateTime(2026, 2, 20), 25000m, "Solapado",
				new DateTime(2026, 2, 1), new DateTime(2026, 2, 28), "abierto"));
			Paso("un cobro con un estado que nadie dijo", api.AnunciarCobro(new DateTime(2026, 3, 10), 25000m, "Febrero",
				new DateTime(2026, 2, 24), new DateTime(2026, 3, 23), "vencido"));
			Paso("el cobro de enero paso a pagado", api.CambiarEstadoDeCobro(new DateTime(2026, 2, 10), "pagado"));
			Paso("pagado otra vez", api.CambiarEstadoDeCobro(new DateTime(2026, 2, 10), "pagado"));

			// ------------------------------------------------------------ las preguntas

			Console.WriteLine();
			Console.WriteLine("Las preguntas del Preguntas.md, cruzando el perform:");
			Console.WriteLine();

			Console.WriteLine("  veces que midieron la cintura : " + api.CuantasVecesSeMidio("cintura"));
			Console.WriteLine("  se puede ver el peso del remo : " + api.SePuedeVerLaProgresionDelPeso("remo") + "  (el plan 1 no lo trae)");
			Console.WriteLine();

			Console.WriteLine("  como cambio lo prescrito en remo -N8, volumen-:");
			Console.WriteLine("  " + api.ComoCambioLoPrescrito("remo"));
			Console.WriteLine("  y en caminadora -minutos-:");
			Console.WriteLine("  " + api.ComoCambioLoPrescrito("caminadora"));
			Console.WriteLine();

			Console.WriteLine("  lo que dijo el aparato de la grasa visceral, y como viene -N7-:");
			Console.WriteLine("  " + api.LoQueDijoElAparato("grasa-visceral", 1m, 6m));
			Console.WriteLine();

			Console.WriteLine("  el producto -N11-:");
			Console.WriteLine("  " + api.ElProducto());
			Console.WriteLine();

			Console.WriteLine("  el cobro que cubre el 1 de febrero -N10-:");
			Console.WriteLine("  " + api.CobroQueCubre(new DateTime(2026, 2, 1)));
			Console.WriteLine();

			Console.WriteLine("  como voy en cintura -los dos juicios a la vez-:");
			Console.WriteLine("  " + api.ComoVoy("cintura", 0m, 88m, 1m, 5m));
			Console.WriteLine();

			Console.WriteLine("  como voy en grasa:");
			Console.WriteLine("  " + api.ComoVoy("grasa", 21m, 33m, 1m, 5m));
			Console.WriteLine();

			Console.WriteLine("  el incumplimiento del plan 1 -descontando la inactividad-:");
			Console.WriteLine("  " + api.IncumplimientoDelPlan(plan1));
			Console.WriteLine();

			Console.WriteLine("  el estado de la membresia:");
			Console.WriteLine("  " + api.EstadoDeLaMembresia());
			Console.WriteLine();

			Console.WriteLine("  los periodos no activos dentro del plan 1:");
			Console.WriteLine("  " + api.PeriodosInactivos(plan1, new DateTime(2026, 4, 6)));
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
