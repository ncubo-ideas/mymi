using System;
using System.Collections.Generic;

namespace Municipalidad.Dominio
{
	// LA RAIZ. Se llama Cuenta y no Municipalidad porque la raiz nunca se llama como el
	// negocio -el namespace y el assembly ya se llaman asi-: nombra lo que contiene. Es la
	// cuenta de un contribuyente con la municipalidad: sus fincas, lo que le pusieron al
	// cobro, lo que pago, lo que consulto, y lo que decidio apartar.
	//
	// Guarda lo que la municipalidad manda -cobros, pagos, la mora recibida- y lo que la
	// persona aporta -la reserva, el valor de su finca-. Por eso este dominio es hibrido
	// y su journal no es enteramente reconstruible.
	//
	// EL RELOJ NO ENTRA. Ninguna operacion mira el dia de hoy; toda fecha de corte llega
	// por parametro.
	//
	// UN PREDICADO TOTAL POR COMANDO (reglas 17 y 18): cada Puede... junta TODAS las
	// razones por las que ese comando fallaria, para que el Check pregunte exactamente lo
	// que el dominio rechaza y un rechazo no envenene el journal.
	internal sealed class Cuenta
	{
		private readonly List<Finca> fincas = new List<Finca>();
		private readonly List<Cobro> cobros = new List<Cobro>();
		private readonly List<Pago> pagos = new List<Pago>();
		private readonly List<Consulta> consultas = new List<Consulta>();
		private readonly List<ReservaDeclarada> reservas = new List<ReservaDeclarada>();

		internal int CuantasFincas { get { return fincas.Count; } }
		internal int CuantosCobros { get { return cobros.Count; } }
		internal int CuantosPagos { get { return pagos.Count; } }
		internal int CuantasConsultas { get { return consultas.Count; } }

		// ================================================================ las fincas

		internal bool TieneFinca(string numero)
		{
			foreach (Finca f in fincas)
				if (f.EsLaMismaQue(numero)) return true;
			return false;
		}

		internal bool PuedeIncorporarFinca(string numero)
		{
			return !string.IsNullOrWhiteSpace(numero) && !TieneFinca(numero);
		}

		internal void IncorporarFinca(string numero)
		{
			if (!PuedeIncorporarFinca(numero))
				throw new InvalidOperationException("Esa finca ya esta, o el numero no sirve. Preguntar PuedeIncorporarFinca antes.");
			fincas.Add(new Finca(numero));
		}

		internal Finca FincaNumero(string numero)
		{
			foreach (Finca f in fincas)
				if (f.EsLaMismaQue(numero)) return f;
			throw new KeyNotFoundException("No hay una finca con ese numero. Preguntar TieneFinca antes.");
		}

		internal bool PuedeDeclararValorDeFinca(string numero, DateTime fecha, decimal monto)
		{
			return TieneFinca(numero) && monto > 0 && FincaNumero(numero).PuedeDeclararValor(fecha);
		}

		internal void DeclararValorDeFinca(string numero, DateTime fecha, decimal monto)
		{
			if (!PuedeDeclararValorDeFinca(numero, fecha, monto))
				throw new InvalidOperationException(
					"No se puede declarar ese valor: la finca no esta, el monto no sirve, o hay una declaracion posterior. "
					+ "Preguntar PuedeDeclararValorDeFinca antes.");
			FincaNumero(numero).DeclararValor(new ValorDeFinca(fecha, monto));
		}

		// ========================================================= lo puesto al cobro

		private bool EstaPuestoAlCobro(Rubro rubro, Periodo periodo)
		{
			foreach (Cobro c in cobros)
				if (c.EsDe(rubro, periodo)) return true;
			return false;
		}

		internal bool PuedePonerAlCobro(string nombreDelRubro, int anio, int numero, decimal monto)
		{
			if (!Rubro.EsUnNombreConocido(nombreDelRubro)) return false;
			Rubro r = Rubro.PorNombre(nombreDelRubro);
			if (!r.PuedeLeerPeriodo(anio, numero)) return false;
			if (monto <= 0) return false;
			return !EstaPuestoAlCobro(r, r.LeerPeriodo(anio, numero));
		}

		internal void PonerAlCobro(string nombreDelRubro, int anio, int numero, decimal monto, DateTime puestoEl)
		{
			if (!PuedePonerAlCobro(nombreDelRubro, anio, numero, monto))
				throw new InvalidOperationException(
					"No se puede poner al cobro: rubro desconocido, periodo ilegible, monto no valido, o ya estaba puesto. "
					+ "Preguntar PuedePonerAlCobro antes.");
			Rubro r = Rubro.PorNombre(nombreDelRubro);
			cobros.Add(new Cobro(r, r.LeerPeriodo(anio, numero), monto, puestoEl));
		}

		// ================================================================== los pagos

		internal bool TienePago(string numeroDeRecibo)
		{
			foreach (Pago p in pagos)
				if (p.EsElMismoQue(numeroDeRecibo)) return true;
			return false;
		}

		// EL MISMO RECIBO DOS VECES ES EL MISMO HECHO OTRA VEZ (regla 18): un replay lo
		// repite siempre, y un feed tambien puede. Es la leccion de Asistio en el gimnasio.
		internal bool PuedeRegistrarPago(string numeroDeRecibo, DateTime fecha, decimal descuento)
		{
			return !string.IsNullOrWhiteSpace(numeroDeRecibo) && !TienePago(numeroDeRecibo) && descuento >= 0;
		}

		internal void RegistrarPago(string numeroDeRecibo, DateTime fecha, decimal descuento)
		{
			if (!PuedeRegistrarPago(numeroDeRecibo, fecha, descuento))
				throw new InvalidOperationException(
					"Ese recibo ya estaba registrado, o el descuento no sirve. Preguntar PuedeRegistrarPago antes.");
			pagos.Add(new Pago(numeroDeRecibo, fecha, descuento));
		}

		internal Pago PagoNumero(string numeroDeRecibo)
		{
			foreach (Pago p in pagos)
				if (p.EsElMismoQue(numeroDeRecibo)) return p;
			throw new KeyNotFoundException("No hay un recibo con ese numero. Preguntar TienePago antes.");
		}

		internal bool PuedeAgregarLineaAlPago(string numeroDeRecibo, string nombreDelRubro, int anio, int numero,
			decimal monto, decimal intereses)
		{
			if (!TienePago(numeroDeRecibo)) return false;
			if (!Rubro.EsUnNombreConocido(nombreDelRubro)) return false;
			Rubro r = Rubro.PorNombre(nombreDelRubro);
			if (!r.PuedeLeerPeriodo(anio, numero)) return false;
			if (monto <= 0 || intereses < 0) return false;
			Periodo periodo = r.LeerPeriodo(anio, numero);
			if (PagoNumero(numeroDeRecibo).Trae(r, periodo)) return false;
			return !EstaPagado(r, periodo);
		}

		internal void AgregarLineaAlPago(string numeroDeRecibo, string nombreDelRubro, int anio, int numero,
			decimal monto, decimal intereses)
		{
			if (!PuedeAgregarLineaAlPago(numeroDeRecibo, nombreDelRubro, anio, numero, monto, intereses))
				throw new InvalidOperationException(
					"No se puede agregar esa linea: el recibo no esta, el rubro no existe, el periodo no se lee, "
					+ "los montos no sirven, o ese periodo ya esta pagado. Preguntar PuedeAgregarLineaAlPago antes.");
			Rubro r = Rubro.PorNombre(nombreDelRubro);
			Periodo periodo = r.LeerPeriodo(anio, numero);
			int cubre = PeriodosQueCubriria(r, periodo, out bool inferido);
			PagoNumero(numeroDeRecibo).AgregarLinea(new LineaPagada(r, periodo, monto, intereses, cubre, inferido));
		}

		// LA INFERENCIA DEL HUECO 6. El recibo dice hasta que periodo llega la linea, no
		// cuantos periodos cubre. Se deduce de la distancia al ultimo periodo pagado de ese
		// rubro: si lo ultimo pagado fue marzo y esta linea llega a junio, cubre tres. Sin
		// historia previa se asume UNO y se marca inferido, porque no hay con que saber.
		private int PeriodosQueCubriria(Rubro rubro, Periodo periodo, out bool inferido)
		{
			Periodo ultimo = UltimoPeriodoPagadoDe(rubro, periodo);
			if (ultimo == null) { inferido = true; return 1; }
			int pasos = 0;
			Periodo p = ultimo;
			while (p.EsAnteriorA(periodo) && pasos < 240) { p = p.Siguiente(); pasos++; }
			inferido = false;
			return pasos == 0 ? 1 : pasos;
		}

		private Periodo UltimoPeriodoPagadoDe(Rubro rubro, Periodo antesDe)
		{
			Periodo ultimo = null;
			foreach (Pago pago in pagos)
				foreach (LineaPagada l in pago.Lineas)
					if (l.EsDe(rubro) && l.Periodo.EsAnteriorA(antesDe) && (ultimo == null || ultimo.EsAnteriorA(l.Periodo)))
						ultimo = l.Periodo;
			return ultimo;
		}

		// PREDICADO TOTAL: hay alguna linea pagada que cubra ese periodo?
		internal bool EstaPagado(Rubro rubro, Periodo periodo)
		{
			foreach (Pago pago in pagos)
				foreach (LineaPagada l in pago.Lineas)
					if (l.EsDe(rubro) && l.Cubre(periodo)) return true;
			return false;
		}

		// ============================================================== las consultas

		internal bool TieneConsultaDel(DateTime fecha)
		{
			foreach (Consulta c in consultas)
				if (c.Fecha == fecha.Date) return true;
			return false;
		}

		internal bool PuedeRegistrarConsulta(DateTime fecha)
		{
			return !TieneConsultaDel(fecha);
		}

		internal void RegistrarConsulta(DateTime fecha)
		{
			if (!PuedeRegistrarConsulta(fecha))
				throw new InvalidOperationException("Ya hay una consulta de ese dia. Preguntar PuedeRegistrarConsulta antes.");
			consultas.Add(new Consulta(fecha));
		}

		internal Consulta ConsultaDel(DateTime fecha)
		{
			foreach (Consulta c in consultas)
				if (c.Fecha == fecha.Date) return c;
			throw new KeyNotFoundException("No hay una consulta de ese dia. Preguntar TieneConsultaDel antes.");
		}

		internal bool PuedeAgregarPendiente(DateTime fechaDeConsulta, string nombreDelRubro, int anio, int numero,
			decimal monto, decimal intereses)
		{
			if (!TieneConsultaDel(fechaDeConsulta)) return false;
			if (!Rubro.EsUnNombreConocido(nombreDelRubro)) return false;
			Rubro r = Rubro.PorNombre(nombreDelRubro);
			if (!r.PuedeLeerPeriodo(anio, numero)) return false;
			if (monto <= 0 || intereses < 0) return false;
			return !ConsultaDel(fechaDeConsulta).Trae(r, r.LeerPeriodo(anio, numero));
		}

		// HUECO 11: lo pendiente de una consulta queda PUESTO AL COBRO con la fecha de la
		// consulta, si no lo estaba. Es la lectura mas simple de "el portal dice que debo
		// esto".
		internal void AgregarPendiente(DateTime fechaDeConsulta, string nombreDelRubro, int anio, int numero,
			decimal monto, decimal intereses)
		{
			if (!PuedeAgregarPendiente(fechaDeConsulta, nombreDelRubro, anio, numero, monto, intereses))
				throw new InvalidOperationException(
					"No se puede agregar ese pendiente: la consulta no esta, el rubro no existe, el periodo no se lee, "
					+ "los montos no sirven, o ya estaba. Preguntar PuedeAgregarPendiente antes.");
			Rubro r = Rubro.PorNombre(nombreDelRubro);
			Periodo periodo = r.LeerPeriodo(anio, numero);
			ConsultaDel(fechaDeConsulta).Agregar(new LineaPendiente(r, periodo, monto, intereses));
			if (!EstaPuestoAlCobro(r, periodo))
				cobros.Add(new Cobro(r, periodo, monto, fechaDeConsulta));
		}

		// ================================================= la reserva, que es de ella

		// UNA ADOPCION POR DIA, Y EN ORDEN. La misma reserva el mismo dia es el mismo hecho
		// otra vez -regla 18-. La primera version de este predicado solo miraba que el
		// monto fuera positivo, y el ciclo con journal corrido dos veces dejo entrar la
		// misma adopcion en la segunda corrida: es el comando que "no podia fallar", igual
		// que Asistio en el gimnasio (hueco 13). Los numeros no se movieron porque la
		// vigente es la ultima, pero el journal tenia un acto de mas.
		internal bool PuedeAdoptarReserva(decimal montoMensual, DateTime fecha)
		{
			if (montoMensual <= 0) return false;
			if (reservas.Count == 0) return true;
			return fecha.Date > reservas[reservas.Count - 1].Fecha;
		}

		internal void AdoptarReserva(decimal montoMensual, DateTime fecha)
		{
			if (!PuedeAdoptarReserva(montoMensual, fecha))
				throw new InvalidOperationException(
					"Una reserva tiene que ser positiva y posterior a la ultima adoptada. Preguntar PuedeAdoptarReserva antes.");
			reservas.Add(new ReservaDeclarada(montoMensual, fecha));
		}

		internal bool TieneReservaDeclarada { get { return reservas.Count > 0; } }

		internal ReservaDeclarada ReservaVigente
		{
			get
			{
				if (!TieneReservaDeclarada)
					throw new InvalidOperationException(
						"La persona no adopto ninguna reserva todavia -y la app no decide por ella-. "
						+ "Preguntar TieneReservaDeclarada antes.");
				return reservas[reservas.Count - 1];
			}
		}

		// =================================================================== preguntas

		// "Cuanto he pagado este anio?" Total: cero si nada.
		internal decimal LoPagadoEn(int anio)
		{
			decimal suma = 0m;
			foreach (Pago p in pagos)
				if (p.Fecha.Year == anio) suma += p.Total;
			return suma;
		}

		// La serie de un rubro, en orden de periodo. Total: vacia para un rubro sin
		// historia o desconocido -es una lectura keyed sobre un espacio, E41-.
		internal IReadOnlyList<LineaPagada> HistorialDe(string nombreDelRubro)
		{
			List<LineaPagada> serie = new List<LineaPagada>();
			if (!Rubro.EsUnNombreConocido(nombreDelRubro)) return serie;
			Rubro r = Rubro.PorNombre(nombreDelRubro);
			foreach (Pago pago in pagos)
				foreach (LineaPagada l in pago.Lineas)
					if (l.EsDe(r)) serie.Add(l);
			serie.Sort((a, b) => a.Periodo.Clave.CompareTo(b.Periodo.Clave));
			return serie;
		}

		internal bool SePuedeVerLaVariacionDe(string nombreDelRubro)
		{
			return HistorialDe(nombreDelRubro).Count >= 2;
		}

		// "Por que este trimestre pago mas?" Las dos ultimas lineas del rubro, comparadas
		// por monto por periodo. Umbral y ruido entran por parametro (M1-M3).
		internal Variacion VariacionDe(string nombreDelRubro, decimal umbralDeSalto, decimal ruido)
		{
			if (!SePuedeVerLaVariacionDe(nombreDelRubro))
				throw new InvalidOperationException(
					"Hacen falta al menos dos pagos de ese rubro para ver una variacion. Preguntar SePuedeVerLaVariacionDe antes.");
			IReadOnlyList<LineaPagada> serie = HistorialDe(nombreDelRubro);
			return Variacion.Entre(serie[serie.Count - 2], serie[serie.Count - 1], umbralDeSalto, ruido);
		}

		// "Que se me vence?" Lo puesto al cobro, no pagado, que todavia no vencio a la
		// fecha. Total: vacia si nada.
		internal IReadOnlyList<VencimientoProximo> VencimientosAl(DateTime fecha)
		{
			List<VencimientoProximo> proximos = new List<VencimientoProximo>();
			foreach (Cobro c in cobros)
				if (!c.Periodo.VencioAl(fecha) && !EstaPagado(c.Rubro, c.Periodo))
					proximos.Add(new VencimientoProximo(c));
			proximos.Sort((a, b) => a.Vence.CompareTo(b.Vence));
			return proximos;
		}

		// Lo puesto al cobro, no pagado, ya vencido a la fecha. Total: vacia si nada.
		internal IReadOnlyList<Atraso> AtrasosAl(DateTime fecha)
		{
			List<Atraso> atrasos = new List<Atraso>();
			foreach (Cobro c in cobros)
				if (c.Periodo.VencioAl(fecha) && !EstaPagado(c.Rubro, c.Periodo))
					atrasos.Add(new Atraso(c));
			atrasos.Sort((a, b) => a.Vencio.CompareTo(b.Vencio));
			return atrasos;
		}

		// ---------------------------------------------- la proyeccion y la reserva

		private IReadOnlyList<Rubro> RubrosConHistoria()
		{
			List<Rubro> rubros = new List<Rubro>();
			foreach (Pago pago in pagos)
				foreach (LineaPagada l in pago.Lineas)
				{
					bool ya = false;
					foreach (Rubro r in rubros) if (r.Es(l.Rubro)) { ya = true; break; }
					if (!ya) rubros.Add(l.Rubro);
				}
			return rubros;
		}

		private decimal UltimoMontoPorPeriodoDe(Rubro rubro)
		{
			IReadOnlyList<LineaPagada> serie = HistorialDe(rubro.Nombre);
			return serie[serie.Count - 1].MontoPorPeriodo;
		}

		internal bool SePuedeProyectar { get { return RubrosConHistoria().Count > 0; } }

		// "Cuanto probablemente tendre que pagar en el anio?" El ultimo monto por periodo
		// de cada rubro con historia, por sus periodos por anio. Es una proyeccion y se
		// muestra como tal. Total: cero sin historia.
		internal decimal ProyeccionDelAnio()
		{
			decimal suma = 0m;
			foreach (Rubro r in RubrosConHistoria())
				suma += UltimoMontoPorPeriodoDe(r) * r.Cadencia.PeriodosPorAnio;
			return suma;
		}

		internal ReservaPropuesta ReservaPropuestaPara(int anio)
		{
			if (!SePuedeProyectar)
				throw new InvalidOperationException(
					"Sin un pago registrado no hay con que proyectar, y no se inventa. Preguntar SePuedeProyectar antes.");
			return new ReservaPropuesta(anio, ProyeccionDelAnio());
		}

		internal bool SePuedeVerElFaltanteDeReserva { get { return SePuedeProyectar && TieneReservaDeclarada; } }

		internal FaltanteDeReserva FaltanteDeReservaAl(DateTime fecha)
		{
			if (!SePuedeVerElFaltanteDeReserva)
				throw new InvalidOperationException(
					"Hace falta una proyeccion y una reserva adoptada. Preguntar SePuedeVerElFaltanteDeReserva antes.");
			decimal venceEsteMes = 0m;
			foreach (Cobro c in cobros)
				if (!EstaPagado(c.Rubro, c.Periodo)
					&& c.Periodo.UltimoDia.Year == fecha.Year && c.Periodo.UltimoDia.Month == fecha.Month)
					venceEsteMes += c.Monto;
			return new FaltanteDeReserva(ReservaPropuestaPara(fecha.Year).MontoMensual, ReservaVigente.MontoMensual, venceEsteMes);
		}

		// M6, propuesto: "cuanto me ahorro si pago hoy todo el anio?" El descuento por
		// pronto pago es la mora al reves -misma tasa, signo contrario-: por cada periodo
		// del anio no pagado, monto x tasa x dias adelantados / 365. La tasa entra por
		// parametro. Total: cero sin historia o sin nada por adelantar.
		internal decimal AhorroPorProntoPago(DateTime fechaDePago, decimal tasaAnual)
		{
			if (tasaAnual < 0)
				throw new ArgumentOutOfRangeException(nameof(tasaAnual), tasaAnual, "La tasa no puede ser negativa.");
			decimal ahorro = 0m;
			foreach (Rubro r in RubrosConHistoria())
			{
				decimal monto = UltimoMontoPorPeriodoDe(r);
				for (int n = 1; n <= r.Cadencia.PeriodosPorAnio; n++)
				{
					Periodo p = r.LeerPeriodo(fechaDePago.Year, n);
					if (EstaPagado(r, p)) continue;
					int dias = (p.UltimoDia - fechaDePago.Date).Days;
					if (dias <= 0) continue;
					ahorro += monto * tasaAnual * dias / 365m;
				}
			}
			return Math.Round(ahorro, 2);
		}

		// M8, propuesto: lo que la consulta dice pagado y la app no vio pagar. Para cada
		// rubro de la consulta, todo periodo desde lo mas viejo que la app conoce hasta el
		// PRIMERO que el pendiente cubre, que ninguna linea pagada cubra. Total: vacia si
		// nada.
		//
		// CUANTOS PERIODOS CUBRE EL PENDIENTE TAMBIEN SE INFIERE (hueco 6), y aca del monto:
		// "basura hasta 2025.09 por 24.000" con un ultimo monto por periodo de 8.000 son
		// tres meses, asi que cubre desde julio, y julio y agosto estan PENDIENTES, no
		// pagados sin comprobante. Sin esta inferencia se marcaria mal. Asume que la tarifa
		// no cambio entre el ultimo pago y el pendiente; si cambio, la cuenta sale distinta
		// y por eso el resultado entero se lee como inferido.
		internal IReadOnlyList<PeriodoSinComprobante> PeriodosPagadosSinComprobante(DateTime fechaDeConsulta)
		{
			List<PeriodoSinComprobante> sinComprobante = new List<PeriodoSinComprobante>();
			if (!TieneConsultaDel(fechaDeConsulta)) return sinComprobante;
			Consulta consulta = ConsultaDel(fechaDeConsulta);
			foreach (Rubro r in RubrosConHistoria())
			{
				if (!consulta.TraeAlgoDe(r)) continue;
				Periodo tope = PrimerPeriodoQueCubreElPendiente(consulta, r);
				Periodo p = PrimerPeriodoConocidoDe(r);
				int pasos = 0;
				while (p != null && p.EsAnteriorA(tope) && pasos < 240)
				{
					if (!EstaPagado(r, p)) sinComprobante.Add(new PeriodoSinComprobante(r, p));
					p = p.Siguiente();
					pasos++;
				}
			}
			return sinComprobante;
		}

		private Periodo PrimerPeriodoQueCubreElPendiente(Consulta consulta, Rubro rubro)
		{
			Periodo masViejo = consulta.PendienteMasViejoDe(rubro);
			decimal montoPendiente = 0m;
			foreach (LineaPendiente lp in consulta.Pendientes)
				if (lp.Rubro.Es(rubro) && lp.Periodo.EsElMismoQue(masViejo)) montoPendiente = lp.Monto;
			decimal porPeriodo = UltimoMontoPorPeriodoDe(rubro);
			int cubre = porPeriodo > 0 ? (int)Math.Round(montoPendiente / porPeriodo, MidpointRounding.AwayFromZero) : 1;
			if (cubre < 1) cubre = 1;
			Periodo primero = masViejo;
			for (int paso = 1; paso < cubre && paso < 240; paso++) primero = primero.Anterior();
			return primero;
		}

		private Periodo PrimerPeriodoConocidoDe(Rubro rubro)
		{
			Periodo primero = null;
			foreach (Pago pago in pagos)
				foreach (LineaPagada l in pago.Lineas)
					if (l.EsDe(rubro) && (primero == null || l.Periodo.EsAnteriorA(primero)))
						primero = l.Periodo;
			foreach (Cobro c in cobros)
				if (c.Rubro.Es(rubro) && (primero == null || c.Periodo.EsAnteriorA(primero)))
					primero = c.Periodo;
			return primero;
		}
	}
}
