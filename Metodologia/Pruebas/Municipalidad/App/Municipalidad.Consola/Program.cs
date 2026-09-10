using System;
using System.Collections.Generic;
using Municipalidad.Dominio;

namespace Municipalidad.Consola
{
	// EL ARNES DEL DOMINIO PURO. Sin actor, sin journal, sin disco.
	//
	// Cada comprobacion nombra el juicio o el hueco del Preguntas.md que fija, para que
	// cuando se rompa se sepa que decision se violo y no solo que fallo un assert.
	//
	// LOS DATOS SON INVENTADOS. Reproducen los fenomenos que los papeles reales mostraron
	// -los dos alfabetos de Periodo, mensual contra trimestral, el salto del 2025, la
	// mora recibida- sin ningun monto ni fecha de la cuenta real. Capitulo 4 §6: los
	// datos del demo se orquestan como casos de prueba.
	internal static class Program
	{
		private static int fallas;

		private static int Main(string[] args)
		{
			string comando = args.Length > 0 ? args[0] : "comprobar";
			if (comando == "medir")
				return Medir.Correr(args.Length > 1
					? args[1]
					: System.IO.Path.Combine(System.IO.Path.GetTempPath(), "municipalidad"));
			if (comando != "comprobar")
			{
				Console.WriteLine("Comandos:  comprobar  |  medir <carpeta>");
				return 1;
			}

			Console.WriteLine("El dominio de la municipalidad, sin actor y sin disco:");
			Console.WriteLine();

			LosDosAlfabetosDelPeriodo();
			ElCatalogoNoInventaRubros();
			CuantosPeriodosCubreUnaLineaSeInfiere();
			ElMismoSaltoSeExplicaDistinto();
			ElReciboEsLaIdentidad();
			LaAusencia();
			ElAtrasoProyectaPeroNoVerifica();
			LaReservaPropuestaYLaAdoptada();
			ElProntoPagoEsLaMoraAlReves();
			LoPagadoSinComprobanteSeDice();
			LaFincaSinValorLoDice();

			Console.WriteLine();
			Console.WriteLine(fallas == 0
				? "Todas las comprobaciones, bien."
				: fallas + " FALLARON.");
			return fallas == 0 ? 0 : 1;
		}

		// ------------------------------------------ juicio 1: la cadencia lee el periodo

		private static void LosDosAlfabetosDelPeriodo()
		{
			Titulo("El mismo '4' es abril para un servicio y el cuarto trimestre para bienes inmuebles");

			Periodo abril = Rubro.Basura().LeerPeriodo(2026, 4);
			Periodo cuarto = Rubro.BienesInmuebles().LeerPeriodo(2026, 4);

			Ok("Para la basura, 2026.04 es abril y vence el 30 de abril", abril.UltimoDia == new DateTime(2026, 4, 30));
			Ok("Para bienes inmuebles, 2026.04 es el cuarto trimestre y vence el 31 de diciembre",
				cuarto.UltimoDia == new DateTime(2026, 12, 31));
			Ok("Un mes y un trimestre con el mismo numero NO son el mismo periodo", !abril.EsElMismoQue(cuarto));

			Revienta("Un servicio no tiene mes 13", () => Rubro.Basura().LeerPeriodo(2026, 13));
			Revienta("Bienes inmuebles no tiene quinto trimestre", () => Rubro.BienesInmuebles().LeerPeriodo(2026, 5));
			Ok("Y el Check lo puede preguntar antes", !Rubro.BienesInmuebles().PuedeLeerPeriodo(2026, 5));

			Ok("Diciembre sigue en enero del anio siguiente",
				Rubro.Basura().LeerPeriodo(2025, 12).Siguiente().EsElMismoQue(Rubro.Basura().LeerPeriodo(2026, 1)));
			Ok("Y el cuarto trimestre sigue en el primero",
				Rubro.Parques().LeerPeriodo(2025, 4).Siguiente().EsElMismoQue(Rubro.Parques().LeerPeriodo(2026, 1)));
			Ok("Enero retrocede a diciembre",
				Rubro.Basura().LeerPeriodo(2026, 1).Anterior().EsElMismoQue(Rubro.Basura().LeerPeriodo(2025, 12)));
		}

		// -------------------------------------------------------------------- hueco 9

		private static void ElCatalogoNoInventaRubros()
		{
			Titulo("El catalogo tiene los cinco rubros que los recibos mostraron, y no inventa los otros");

			Ok("Basura es mensual y por unidad", Rubro.Basura().Cadencia.EsMensual() && !Rubro.Basura().Ley.RespondeAlAvaluo);
			Ok("Limpieza es mensual pero sobre la base imponible",
				Rubro.LimpiezaDeCalles().Cadencia.EsMensual() && Rubro.LimpiezaDeCalles().Ley.RespondeAlAvaluo);
			Ok("Bienes inmuebles es trimestral y sobre la base imponible",
				Rubro.BienesInmuebles().Cadencia.EsTrimestral() && Rubro.BienesInmuebles().Ley.RespondeAlAvaluo);

			Ok("Cementerio no se conoce: nadie trajo su recibo", !Rubro.EsUnNombreConocido("cementerio"));
			Revienta("Y pedirlo revienta en vez de adivinarle una cadencia", () => Rubro.PorNombre("cementerio"));

			Cuenta c = new Cuenta();
			c.RegistrarPago("R-1", new DateTime(2024, 3, 31), 0m);
			Ok("Una linea de un rubro desconocido no entra al recibo",
				!c.PuedeAgregarLineaAlPago("R-1", "cementerio", 2024, 3, 1000m, 0m));
		}

		// -------------------------------------------------------------------- hueco 6

		private static void CuantosPeriodosCubreUnaLineaSeInfiere()
		{
			Titulo("Cuantos periodos cubre una linea se infiere del ultimo pagado, y se marca");

			Cuenta c = CuentaDeDemo();
			IReadOnlyList<LineaPagada> basura = c.HistorialDe("recoleccion-de-basura");

			Ok("La primera linea no tiene con que inferir: cubre uno, y lo dice", basura[0].PeriodosQueCubre == 1 && basura[0].EsInferido);
			Ok("La segunda llega a junio despues de marzo: cubre tres", basura[1].PeriodosQueCubre == 3 && !basura[1].EsInferido);
			Ok("Y su monto por periodo es 15.000 / 3 = 5.000", basura[1].MontoPorPeriodo == 5000m);
			Ok("La tercera cubre doce meses", basura[2].PeriodosQueCubre == 12);
			Ok("A 8.000 por mes", basura[2].MontoPorPeriodo == 8000m);

			IReadOnlyList<LineaPagada> ibi = c.HistorialDe("bienes-inmuebles");
			Ok("Bienes inmuebles: la tercera linea cubre cuatro trimestres", ibi[2].PeriodosQueCubre == 4);
			Ok("A 36.000 por trimestre", ibi[2].MontoPorPeriodo == 36000m);

			Ok("Mayo de 2024 esta pagado aunque ninguna linea diga 'mayo': lo cubre la de junio",
				c.EstaPagado(Rubro.Basura(), Rubro.Basura().LeerPeriodo(2024, 5)));
			Ok("Julio de 2025 no esta pagado", !c.EstaPagado(Rubro.Basura(), Rubro.Basura().LeerPeriodo(2025, 7)));
			Ok("Y una linea para un periodo ya cubierto no entra",
				!c.PuedeAgregarLineaAlPago("R-3", "recoleccion-de-basura", 2024, 5, 5000m, 0m));

			Ok("Un rubro sin historia da una serie vacia, no revienta", c.HistorialDe("cementerio").Count == 0);
		}

		// -------------------------------------------------- M1, M2, M3, M4: la variacion

		private static void ElMismoSaltoSeExplicaDistinto()
		{
			Titulo("El mismo salto se explica distinto segun la ley del rubro");

			Cuenta c = CuentaDeDemo();

			Variacion basura = c.VariacionDe("recoleccion-de-basura", 0.25m, 0.02m);
			Ok("Basura subio 60 %", Math.Round(basura.Proporcion, 2) == 0.60m);
			Ok("Y se explica como CAMBIO DE TARIFA: se cobra por unidad", basura is CambioDeTarifa && basura.Etiqueta == "cambio-de-tarifa");

			Variacion ibi = c.VariacionDe("bienes-inmuebles", 0.25m, 0.02m);
			Ok("Bienes inmuebles subio 80 %", Math.Round(ibi.Proporcion, 2) == 0.80m);
			Ok("Y se explica como CAMBIO DE AVALUO: se cobra sobre la base imponible", ibi is CambioDeAvaluo && ibi.Etiqueta == "cambio-de-avaluo");

			Variacion policia = c.VariacionDe("servicio-policia", 0.25m, 0.02m);
			Ok("Policia subio 1,7 %: por debajo del ruido, sin cambio", policia is SinSalto && policia.Etiqueta == "sin-cambio");
			Variacion policiaFina = c.VariacionDe("servicio-policia", 0.25m, 0.01m);
			Ok("Con un ruido del 1 %, es un cambio menor: supera el ruido, no llega al umbral",
				policiaFina is SinSalto && policiaFina.Etiqueta == "cambio-menor" && !policiaFina.EsSalto);

			Ok("Un aumento NO es urgente -M4-: ya paso", !basura.Urgencia().Avisa() && !ibi.Urgencia().Avisa());
			Ok("Compara monto POR PERIODO, no monto crudo: 8.000 contra 5.000, no 96.000 contra 15.000",
				basura.MontoAnteriorPorPeriodo == 5000m && basura.MontoActualPorPeriodo == 8000m);

			Revienta("Un umbral menor que el ruido no tiene sentido", () => c.VariacionDe("recoleccion-de-basura", 0.01m, 0.02m));
			Cuenta vacia = new Cuenta();
			Ok("Con menos de dos pagos no hay variacion que ver", !vacia.SePuedeVerLaVariacionDe("recoleccion-de-basura"));
			Revienta("Y pedirla revienta", () => vacia.VariacionDe("recoleccion-de-basura", 0.25m, 0.02m));
		}

		// ------------------------------------------------------------------- regla 18

		private static void ElReciboEsLaIdentidad()
		{
			Titulo("El numero de recibo es la identidad del pago: el mismo dos veces se rechaza");

			Cuenta c = new Cuenta();
			Ok("Un recibo nuevo entra", c.PuedeRegistrarPago("R-100", new DateTime(2026, 1, 5), 0m));
			c.RegistrarPago("R-100", new DateTime(2026, 1, 5), 0m);
			Ok("El mismo recibo otra vez no entra", !c.PuedeRegistrarPago("R-100", new DateTime(2026, 1, 5), 0m));
			Revienta("Y forzarlo revienta", () => c.RegistrarPago("R-100", new DateTime(2026, 1, 5), 0m));

			c.AgregarLineaAlPago("R-100", "recoleccion-de-basura", 2026, 1, 8000m, 0m);
			Ok("La misma linea otra vez en el mismo recibo no entra",
				!c.PuedeAgregarLineaAlPago("R-100", "recoleccion-de-basura", 2026, 1, 8000m, 0m));
			Ok("Una linea con mora negativa no entra", !c.PuedeAgregarLineaAlPago("R-100", "servicio-policia", 2026, 1, 6000m, -1m));

			Pago adelantado = new Pago("R-200", new DateTime(2026, 1, 5), 1500m);
			adelantado.AgregarLinea(new LineaPagada(Rubro.Basura(), Rubro.Basura().LeerPeriodo(2026, 12), 96000m, 0m, 12, false));
			Ok("Pagar en enero hasta diciembre es pagar por adelantado, y se lee de las fechas", adelantado.PagoPorAdelantado());
			Ok("El total descuenta el pronto pago", adelantado.Total == 94500m);
		}

		// ------------------------------------------------------------ M5: la ausencia

		private static void LaAusencia()
		{
			Titulo("La ausencia: que se me vence, y con que urgencia");

			Cuenta c = CuentaDeDemo();

			IReadOnlyList<VencimientoProximo> proximos = c.VencimientosAl(new DateTime(2025, 9, 3));
			Ok("Al 3 de setiembre hay cinco cosas puestas al cobro sin vencer", proximos.Count == 5);

			VencimientoProximo basura = null;
			foreach (VencimientoProximo v in proximos) if (v.Rubro.Es(Rubro.Basura())) basura = v;
			Ok("La basura vence el 30 de setiembre", basura != null && basura.Vence == new DateTime(2025, 9, 30));
			Ok("Faltan 27 dias", basura.DiasQueFaltan(new DateTime(2025, 9, 3)) == 27);
			Ok("A 27 dias, con 7 de aviso, no avisa", !basura.Urgencia(new DateTime(2025, 9, 3), 7).Avisa());
			Ok("A 5 dias avisa", basura.Urgencia(new DateTime(2025, 9, 25), 7).Es(Urgencia.Aviso()));
			Ok("El dia que vence es urgente", basura.Urgencia(new DateTime(2025, 9, 30), 7).EsUrgente());

			Ok("Al 5 de octubre ya no hay vencimientos proximos", c.VencimientosAl(new DateTime(2025, 10, 5)).Count == 0);
			Ok("Sin nada al cobro, la lista esta vacia y no revienta", new Cuenta().VencimientosAl(new DateTime(2025, 9, 3)).Count == 0);
		}

		// ------------------------------------------------------------- M7: el atraso

		private static void ElAtrasoProyectaPeroNoVerifica()
		{
			Titulo("El atraso: lo vencido es urgente, y el costo de pagar tarde es una proyeccion");

			Cuenta c = CuentaDeDemo();
			Ok("Al 3 de setiembre no hay atrasos", c.AtrasosAl(new DateTime(2025, 9, 3)).Count == 0);

			IReadOnlyList<Atraso> atrasos = c.AtrasosAl(new DateTime(2025, 10, 5));
			Ok("Al 5 de octubre hay cinco", atrasos.Count == 5);

			Atraso basura = null;
			foreach (Atraso a in atrasos) if (a.Rubro.Es(Rubro.Basura())) basura = a;
			Ok("La basura lleva 5 dias vencida", basura != null && basura.DiasDeAtraso(new DateTime(2025, 10, 5)) == 5);
			Ok("Y es urgente por CLASE, no por monto", basura.Urgencia().EsUrgente());
			Ok("Pagarla el 5 a 6,2 % anual costaria 20,38 -proyectado, no cobrado-",
				basura.CostoDePagarTarde(new DateTime(2025, 10, 5), 0.062m) == 20.38m);
			Ok("A tasa cero, cero", basura.CostoDePagarTarde(new DateTime(2025, 10, 5), 0m) == 0m);
		}

		// ---------------------------------------------------- juicios 5 y 6: la reserva

		private static void LaReservaPropuestaYLaAdoptada()
		{
			Titulo("La app propone la reserva; la persona la adopta; y son dos cosas");

			Cuenta vacia = new Cuenta();
			Ok("Sin un pago no hay con que proyectar", !vacia.SePuedeProyectar);
			Revienta("Y pedir una propuesta revienta en vez de inventar un numero", () => vacia.ReservaPropuestaPara(2025));
			Revienta("Sin reserva adoptada no hay reserva vigente, y no se decide por la persona",
				() => { ReservaDeclarada vigente = vacia.ReservaVigente; });

			Cuenta c = CuentaDeDemo();
			Ok("La proyeccion del anio es 454.800", c.ProyeccionDelAnio() == 454800m);
			ReservaPropuesta propuesta = c.ReservaPropuestaPara(2025);
			Ok("Y la propuesta mensual es 37.900", propuesta.MontoMensual == 37900m);

			Ok("Sin reserva adoptada no se puede ver el faltante", !c.SePuedeVerElFaltanteDeReserva);
			c.AdoptarReserva(30000m, new DateTime(2025, 8, 1));
			Ok("Adoptada, si", c.SePuedeVerElFaltanteDeReserva);
			Ok("La misma adopcion el mismo dia no entra otra vez: es el mismo hecho -regla 18, hueco 13-",
				!c.PuedeAdoptarReserva(30000m, new DateTime(2025, 8, 1)));
			Ok("Una adopcion nueva un dia despues, si", c.PuedeAdoptarReserva(31000m, new DateTime(2025, 8, 2)));

			FaltanteDeReserva enAgosto = c.FaltanteDeReservaAl(new DateTime(2025, 8, 15));
			Ok("Le faltan 7.900 por mes", enAgosto.Faltante == 7900m);
			Ok("Es mas del 10 %: avisa", enAgosto.Avisa(0.10m));
			Ok("Pero en agosto no vence nada: no es urgente", enAgosto.VenceEsteMes == 0m && !enAgosto.EsUrgente);

			FaltanteDeReserva enSetiembre = c.FaltanteDeReservaAl(new DateTime(2025, 9, 15));
			Ok("En setiembre vencen 113.700", enSetiembre.VenceEsteMes == 113700m);
			Ok("Y lo apartado no alcanza: urgente", enSetiembre.Urgencia(0.10m).EsUrgente());

			Revienta("Una reserva de cero no se adopta", () => c.AdoptarReserva(0m, new DateTime(2025, 8, 1)));
		}

		// ------------------------------------------------------------ M6: pronto pago

		private static void ElProntoPagoEsLaMoraAlReves()
		{
			Titulo("El pronto pago: cuanto me ahorro si pago hoy todo el anio");

			Cuenta c = CuentaDeDemo();
			decimal enSetiembre = c.AhorroPorProntoPago(new DateTime(2025, 9, 3), 0.062m);
			decimal enNoviembre = c.AhorroPorProntoPago(new DateTime(2025, 11, 1), 0.062m);

			Ok("En setiembre todavia hay algo que adelantar", enSetiembre > 0m);
			Ok("Cuanto mas tarde, menos se ahorra", enNoviembre < enSetiembre);
			Ok("A tasa cero no hay ahorro", c.AhorroPorProntoPago(new DateTime(2025, 9, 3), 0m) == 0m);
			Ok("El 31 de diciembre ya no queda nada por adelantar", c.AhorroPorProntoPago(new DateTime(2025, 12, 31), 0.062m) == 0m);
			Ok("Sin historia, cero", new Cuenta().AhorroPorProntoPago(new DateTime(2025, 1, 3), 0.062m) == 0m);
		}

		// --------------------------------------------------------------- M8, hueco 3

		private static void LoPagadoSinComprobanteSeDice()
		{
			Titulo("Lo que la consulta dice pagado y la app no vio pagar, se dice");

			Cuenta completa = CuentaDeDemo();
			Ok("Con los tres recibos, la consulta no destapa nada sin comprobante",
				completa.PeriodosPagadosSinComprobante(new DateTime(2025, 9, 3)).Count == 0);

			// La misma historia SIN el tercer recibo: la app conoce basura hasta junio de
			// 2024; la consulta de setiembre de 2025 debe 24.000 de basura, que a 5.000 por
			// mes -lo ultimo conocido- son ~5 meses: mayo a setiembre de 2025. Julio de 2024
			// a abril de 2025 se pagaron en algun lado que la app no vio.
			Cuenta incompleta = new Cuenta();
			PrimerosDosRecibos(incompleta);
			ConsultaDeSetiembre(incompleta);
			IReadOnlyList<PeriodoSinComprobante> sinComprobante = incompleta.PeriodosPagadosSinComprobante(new DateTime(2025, 9, 3));

			int basura = 0;
			foreach (PeriodoSinComprobante p in sinComprobante) if (p.Rubro.Es(Rubro.Basura())) basura++;
			Ok("Diez meses de basura -julio 2024 a abril 2025- figuran pagados sin comprobante", basura == 10);
			Ok("Y se nombran, no se tapan: el primero es julio de 2024",
				sinComprobante.Count > 0 && sinComprobante[0].EtiquetaDelPeriodo == "2024-07");

			Ok("Sin consulta, la lista es vacia y no revienta", completa.PeriodosPagadosSinComprobante(new DateTime(2025, 1, 1)).Count == 0);
		}

		// -------------------------------------------------------------------- hueco 2

		private static void LaFincaSinValorLoDice()
		{
			Titulo("La finca existe aunque nada la llene, y sin valor lo dice");

			Cuenta c = new Cuenta();
			c.IncorporarFinca("F-1");
			Ok("La finca esta", c.TieneFinca("F-1"));
			Ok("La misma finca dos veces no entra", !c.PuedeIncorporarFinca("F-1"));

			Finca f = c.FincaNumero("F-1");
			Ok("Sin declaracion, no tiene valor a ninguna fecha", !f.TieneValorAl(new DateTime(2026, 1, 1)));
			Revienta("Y pedirlo revienta en vez de inventar uno -es la leccion de T1-", () => f.ValorVigenteAl(new DateTime(2026, 1, 1)));

			c.DeclararValorDeFinca("F-1", new DateTime(2025, 1, 15), 60000000m);
			Ok("Declarado, el valor vigente en 2026 es el de 2025", f.ValorVigenteAl(new DateTime(2026, 1, 1)) == 60000000m);
			Ok("La misma declaracion otra vez no entra: es el mismo hecho -regla 18, hueco 12-",
				!c.PuedeDeclararValorDeFinca("F-1", new DateTime(2025, 1, 15), 60000000m));
			Ok("Una declaracion posterior, si", c.PuedeDeclararValorDeFinca("F-1", new DateTime(2026, 1, 15), 65000000m));
			Ok("Pero en 2024 todavia no habia", !f.TieneValorAl(new DateTime(2024, 6, 1)));
			Ok("Una declaracion con fecha anterior a la ultima no entra", !c.PuedeDeclararValorDeFinca("F-1", new DateTime(2024, 6, 1), 50000000m));
		}

		// ------------------------------------------------------------------ los datos

		// LA HISTORIA INVENTADA. Tres recibos y una consulta que reproducen: la primera
		// linea sin historia, lineas que cubren varios periodos, el salto del 2025 por
		// tarifa en basura y por avaluo en bienes inmuebles, y policia casi quieta.
		private static Cuenta CuentaDeDemo()
		{
			Cuenta c = new Cuenta();
			c.IncorporarFinca("F-1");
			PrimerosDosRecibos(c);

			c.RegistrarPago("R-3", new DateTime(2025, 6, 30), 0m);
			c.AgregarLineaAlPago("R-3", "recoleccion-de-basura", 2025, 6, 96000m, 310m);   // 12 meses a 8.000
			c.AgregarLineaAlPago("R-3", "servicio-policia", 2025, 6, 73200m, 240m);         // 12 meses a 6.100
			c.AgregarLineaAlPago("R-3", "limpieza-de-calles", 2025, 6, 120000m, 390m);     // 12 meses a 10.000
			c.AgregarLineaAlPago("R-3", "bienes-inmuebles", 2025, 2, 144000m, 470m);       // 4 trimestres a 36.000
			c.AgregarLineaAlPago("R-3", "parques-y-zonas-verdes", 2025, 2, 21600m, 70m);   // 4 trimestres a 5.400

			ConsultaDeSetiembre(c);
			return c;
		}

		private static void PrimerosDosRecibos(Cuenta c)
		{
			c.RegistrarPago("R-1", new DateTime(2024, 3, 31), 0m);
			c.AgregarLineaAlPago("R-1", "recoleccion-de-basura", 2024, 3, 5000m, 0m);
			c.AgregarLineaAlPago("R-1", "servicio-policia", 2024, 3, 6000m, 0m);
			c.AgregarLineaAlPago("R-1", "limpieza-de-calles", 2024, 3, 6000m, 0m);
			c.AgregarLineaAlPago("R-1", "bienes-inmuebles", 2024, 1, 20000m, 0m);
			c.AgregarLineaAlPago("R-1", "parques-y-zonas-verdes", 2024, 1, 3000m, 0m);

			c.RegistrarPago("R-2", new DateTime(2024, 6, 30), 0m);
			c.AgregarLineaAlPago("R-2", "recoleccion-de-basura", 2024, 6, 15000m, 0m);     // 3 meses a 5.000
			c.AgregarLineaAlPago("R-2", "servicio-policia", 2024, 6, 18000m, 0m);
			c.AgregarLineaAlPago("R-2", "limpieza-de-calles", 2024, 6, 18000m, 0m);
			c.AgregarLineaAlPago("R-2", "bienes-inmuebles", 2024, 2, 20000m, 0m);
			c.AgregarLineaAlPago("R-2", "parques-y-zonas-verdes", 2024, 2, 3000m, 0m);
		}

		private static void ConsultaDeSetiembre(Cuenta c)
		{
			DateTime hoy = new DateTime(2025, 9, 3);
			c.RegistrarConsulta(hoy);
			c.AgregarPendiente(hoy, "recoleccion-de-basura", 2025, 9, 24000m, 45m);       // 3 meses a 8.000
			c.AgregarPendiente(hoy, "servicio-policia", 2025, 9, 18300m, 34m);
			c.AgregarPendiente(hoy, "limpieza-de-calles", 2025, 9, 30000m, 56m);
			c.AgregarPendiente(hoy, "bienes-inmuebles", 2025, 3, 36000m, 0m);
			c.AgregarPendiente(hoy, "parques-y-zonas-verdes", 2025, 3, 5400m, 0m);
		}

		// ---------------------------------------------------------------- utilitarios

		private static void Titulo(string texto)
		{
			Console.WriteLine();
			Console.WriteLine("  " + texto);
		}

		private static void Ok(string dice, bool cierto)
		{
			Console.WriteLine((cierto ? "    ok    " : "    FALLA ") + dice);
			if (!cierto) fallas++;
		}

		private static void Revienta(string dice, Action accion)
		{
			try { accion(); Ok(dice, false); }
			catch (Exception) { Ok(dice, true); }
		}
	}
}
