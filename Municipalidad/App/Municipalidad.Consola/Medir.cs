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

			MunicipalidadApp app = new MunicipalidadApp(MunicipalidadApp.ElActor);
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

			Paso("al cobro basura 2025.10", api.PonerAlCobro("recoleccion-de-basura", "2025.10", 8000m, new DateTime(2025, 10, 1)));
			Paso("al cobro basura 2025.10 otra vez", api.PonerAlCobro("recoleccion-de-basura", "2025.10", 8000m, new DateTime(2025, 10, 1)));
			Paso("al cobro basura 2025.09, que el pendiente ya puso", api.PonerAlCobro("recoleccion-de-basura", "2025.09", 24000m, new DateTime(2025, 10, 1)));
			Paso("al cobro un trimestre que no existe", api.PonerAlCobro("bienes-inmuebles", "2025.05", 36000m, new DateTime(2025, 10, 1)));

			Paso("la reserva que la persona adopto", api.AdoptarReserva(30000m, new DateTime(2025, 8, 1)));

			Paso("el dispositivo", api.RegistrarDispositivo("DISPOSITIVO-DEMO-1"));
			Paso("el dispositivo otra vez", api.RegistrarDispositivo("DISPOSITIVO-DEMO-1"));
			Paso("la credencial", api.GuardarCredencial("token-demo-1", "refresco-demo-1"));
			Paso("la credencial otra vez", api.GuardarCredencial("token-demo-1", "refresco-demo-1"));

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
			RegistroDelHistorial basura = api.HistorialDe("recoleccion-de-basura");
			Console.WriteLine("    estado: " + basura.Estado);
			Console.WriteLine("    dice:   " + basura.EstadoDicho);
			for (int fila = 0; fila < basura.Periodo.Length; fila++)
				Console.WriteLine("    " + basura.Periodo[fila]
					+ "  monto " + basura.Monto[fila]
					+ "  mora " + basura.Mora[fila]
					+ "  cubre " + basura.Cubre[fila]
					+ "  por periodo " + basura.PorPeriodo[fila]
					+ "  inferido " + basura.Inferido[fila]);
			Console.WriteLine();

			Console.WriteLine("  por que pago mas de basura? -umbral 25 %, ruido 2 %, propuestos-:");
			LaVariacionDe(api, "recoleccion-de-basura");
			Console.WriteLine();

			Console.WriteLine("  y de bienes inmuebles?");
			LaVariacionDe(api, "bienes-inmuebles");
			Console.WriteLine();

			Console.WriteLine("  que se me vence, al 25 de setiembre, con 7 dias de aviso -la ausencia-:");
			RegistroDeVencimientos vencimientos = api.VencimientosAl(new DateTime(2025, 9, 25), 7);
			for (int fila = 0; fila < vencimientos.Rubro.Length; fila++)
			{
				Console.WriteLine("    " + vencimientos.Rubro[fila]
					+ "  " + vencimientos.Periodo[fila]
					+ "  vence " + vencimientos.Vence[fila]
					+ "  monto " + vencimientos.Monto[fila]
					+ "  faltan " + vencimientos.Faltan[fila]
					+ "  urgencia " + vencimientos.Urgencia[fila]);
				Console.WriteLine("      dice: " + vencimientos.Dice[fila]);
			}
			Console.WriteLine();

			Console.WriteLine("  los atrasos al 5 de octubre, con el costo proyectado a 6,2 %:");
			RegistroDeAtrasos atrasos = api.AtrasosAl(new DateTime(2025, 10, 5), 0.062m);
			for (int fila = 0; fila < atrasos.Rubro.Length; fila++)
			{
				Console.WriteLine("    " + atrasos.Rubro[fila]
					+ "  " + atrasos.Periodo[fila]
					+ "  vencio " + atrasos.Vencio[fila]
					+ "  monto " + atrasos.Monto[fila]
					+ "  dias " + atrasos.Dias[fila]
					+ "  costo proyectado " + atrasos.CostoProyectado[fila]
					+ "  urgencia " + atrasos.Urgencia[fila]);
				Console.WriteLine("      dice: " + atrasos.Dice[fila]);
			}
			Console.WriteLine();

			Console.WriteLine("  se puede proyectar : " + api.SePuedeProyectar());
			Console.WriteLine("  la reserva que la app propone para 2025:");
			if (api.SePuedeProyectar())
			{
				RegistroDeLaReservaPropuesta propuesta = api.ReservaPropuestaPara(2025);
				Console.WriteLine("    anio " + propuesta.Anio
					+ "  proyeccion " + propuesta.Proyeccion
					+ "  propuesto por mes " + propuesta.PropuestoPorMes);
				Console.WriteLine("      dice: " + propuesta.Dice);
			}
			else
			{
				Console.WriteLine("    sin un pago registrado no hay con que proyectar");
			}
			Console.WriteLine();

			Console.WriteLine("  se puede ver el faltante de reserva : " + api.SePuedeVerElFaltanteDeReserva());
			Console.WriteLine("  el faltante contra lo que la persona adopto, al 15 de setiembre:");
			if (api.SePuedeVerElFaltanteDeReserva())
			{
				RegistroDelFaltanteDeReserva faltante =
					api.FaltanteDeReservaAl(new DateTime(2025, 9, 15), 0.10m);
				Console.WriteLine("    propuesto " + faltante.Propuesto
					+ "  declarado " + faltante.Declarado
					+ "  faltante " + faltante.Faltante
					+ "  proporcion " + faltante.Proporcion
					+ "  vence este mes " + faltante.VenceEsteMes
					+ "  urgencia " + faltante.Urgencia);
				Console.WriteLine("      dice: " + faltante.Dice);
			}
			else
			{
				Console.WriteLine("    hace falta una proyeccion y una reserva adoptada");
			}
			Console.WriteLine();

			Console.WriteLine("  ahorro por pronto pago si pago todo el 3 de setiembre a 6,2 % : " + api.AhorroPorProntoPago(consulta, 0.062m));
			Console.WriteLine();

			Console.WriteLine("  periodos pagados sin comprobante segun la consulta:");
			RegistroDePeriodosSinComprobante sinComprobante = api.PeriodosPagadosSinComprobante(consulta);
			Console.WriteLine("    estado: " + sinComprobante.Estado);
			Console.WriteLine("    dice:   " + sinComprobante.EstadoDicho);
			for (int fila = 0; fila < sinComprobante.Rubro.Length; fila++)
			{
				Console.WriteLine("    " + sinComprobante.Rubro[fila]
					+ "  " + sinComprobante.Periodo[fila]);
				Console.WriteLine("      dice: " + sinComprobante.Dice[fila]);
			}
			Console.WriteLine();

			Console.WriteLine("  y el mismo cotejo un dia sin consulta -1 de enero de 2025-:");
			RegistroDePeriodosSinComprobante sinConsulta =
				api.PeriodosPagadosSinComprobante(new DateTime(2025, 1, 1));
			Console.WriteLine("    estado: " + sinConsulta.Estado);
			Console.WriteLine("    dice:   " + sinConsulta.EstadoDicho);
			Console.WriteLine();

			Console.WriteLine("  las lecturas de descubrimiento -lo que una caratula no puede inventar-:");
			Console.WriteLine("    los rubros que hay      : " + string.Join(", ", api.LosRubrosQueHay()));
			Console.WriteLine("    los que tienen historia : " + string.Join(", ", api.RubrosConHistoria()));
			Console.WriteLine("    hay alguna consulta     : " + api.HayAlgunaConsulta());
			Console.WriteLine("    la mas reciente         : "
				+ (api.HayAlgunaConsulta()
					? api.ConsultaMasReciente().ToString("yyyy-MM-dd")
					: "ninguna todavia"));
			Console.WriteLine("    las fincas              : " + string.Join(", ", api.NumerosDeLasFincas()));
			foreach (string numeroDeFinca in api.NumerosDeLasFincas())
				Console.WriteLine("    el valor de " + numeroDeFinca + " al 30 de junio : "
					+ (api.TieneValorDeclaradoLaFinca(numeroDeFinca, new DateTime(2025, 6, 30))
						? api.ValorDeFincaAl(numeroDeFinca, new DateTime(2025, 6, 30)).ToString()
						: "sin valor declarado a esa fecha"));
			Console.WriteLine();

			Console.WriteLine("  hasta que evento leyo la app : " + api.UltimoEventoAplicado());
			Console.WriteLine("  la clave de esta instalacion : '" + api.ClaveDeDispositivoGuardada() + "'");
			Console.WriteLine("  el token guardado            : '" + api.TokenGuardado() + "'");
			Console.WriteLine("  el token de refresco         : '" + api.TokenDeRefrescoGuardado() + "'");

			Console.WriteLine();
			Console.WriteLine("Corre esto DOS veces sobre la misma carpeta: la segunda rechaza todo");
			Console.WriteLine("y los numeros no se mueven.");
			return 0;
		}

		/// <summary>
		/// Imprime la variacion de un rubro preguntando ANTES si se puede ver, que es lo que una
		/// caratula tiene que hacer con cada rubro del catalogo. Las dos ramas se corren.
		/// </summary>
		private static void LaVariacionDe(MunicipalidadAPI api, string nombreDelRubro)
		{
			Console.WriteLine("    se puede ver: " + api.SePuedeVerLaVariacionDe(nombreDelRubro));
			if (!api.SePuedeVerLaVariacionDe(nombreDelRubro))
			{
				Console.WriteLine("    hacen falta al menos dos pagos de ese rubro");
				return;
			}

			RegistroDeLaVariacion variacion = api.VariacionDe(nombreDelRubro, 0.25m, 0.02m);
			Console.WriteLine("    " + variacion.Rubro
				+ "  de " + variacion.Desde + " a " + variacion.Hasta
				+ "  antes " + variacion.Antes
				+ "  ahora " + variacion.Ahora
				+ "  proporcion " + variacion.Proporcion
				+ "  explicacion " + variacion.Explicacion
				+ "  es salto " + variacion.EsSalto
				+ "  urgencia " + variacion.Urgencia);
			Console.WriteLine("      dice: " + variacion.Dice);
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
