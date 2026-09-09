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
				throw new DomainException("Esa finca ya esta, o el numero no sirve. Preguntar PuedeIncorporarFinca antes.");
			fincas.Add(new Finca(numero));
		}

		internal Finca FincaNumero(string numero)
		{
			foreach (Finca f in fincas)
				if (f.EsLaMismaQue(numero)) return f;
			throw new DomainException("No hay una finca con ese numero. Preguntar TieneFinca antes.");
		}

		internal bool PuedeDeclararValorDeFinca(string numero, DateTime fecha, decimal monto)
		{
			return TieneFinca(numero) && monto > 0 && FincaNumero(numero).PuedeDeclararValor(fecha);
		}

		internal void DeclararValorDeFinca(string numero, DateTime fecha, decimal monto)
		{
			if (!PuedeDeclararValorDeFinca(numero, fecha, monto))
				throw new DomainException(
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
				throw new DomainException(
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
				throw new DomainException(
					"Ese recibo ya estaba registrado, o el descuento no sirve. Preguntar PuedeRegistrarPago antes.");
			pagos.Add(new Pago(numeroDeRecibo, fecha, descuento));
		}

		internal Pago PagoNumero(string numeroDeRecibo)
		{
			foreach (Pago p in pagos)
				if (p.EsElMismoQue(numeroDeRecibo)) return p;
			throw new DomainException("No hay un recibo con ese numero. Preguntar TienePago antes.");
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
				throw new DomainException(
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
				throw new DomainException("Ya hay una consulta de ese dia. Preguntar PuedeRegistrarConsulta antes.");
			consultas.Add(new Consulta(fecha));
		}

		internal Consulta ConsultaDel(DateTime fecha)
		{
			foreach (Consulta c in consultas)
				if (c.Fecha == fecha.Date) return c;
			throw new DomainException("No hay una consulta de ese dia. Preguntar TieneConsultaDel antes.");
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
				throw new DomainException(
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
				throw new DomainException(
					"Una reserva tiene que ser positiva y posterior a la ultima adoptada. Preguntar PuedeAdoptarReserva antes.");
			reservas.Add(new ReservaDeclarada(montoMensual, fecha));
		}

		internal bool TieneReservaDeclarada { get { return reservas.Count > 0; } }

		internal ReservaDeclarada ReservaVigente
		{
			get
			{
				if (!TieneReservaDeclarada)
					throw new DomainException(
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
				throw new DomainException(
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

		/// <summary>
		/// Los rubros de los que la app vio pagar algo, en el orden en que aparecieron. TOTAL: vacia
		/// donde no hay ningun pago. Queda PRIVADA: una coleccion del dominio no cruza el perform, asi
		/// que lo que sale por la puerta es su proyeccion a nombres.
		/// </summary>
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
				throw new DomainException(
					"Sin un pago registrado no hay con que proyectar, y no se inventa. Preguntar SePuedeProyectar antes.");
			return new ReservaPropuesta(anio, ProyeccionDelAnio());
		}

		internal bool SePuedeVerElFaltanteDeReserva { get { return SePuedeProyectar && TieneReservaDeclarada; } }

		internal FaltanteDeReserva FaltanteDeReservaAl(DateTime fecha)
		{
			if (!SePuedeVerElFaltanteDeReserva)
				throw new DomainException(
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

		/// <summary>
		/// El periodo de cada linea de la serie de ese rubro, una por fila y en el mismo orden que la
		/// serie. TOTAL: cero filas donde no hay ninguna.
		/// </summary>
		internal string[] PeriodosDelHistorialDe(string nombreDelRubro)
		{
			IReadOnlyList<LineaPagada> serie = HistorialDe(nombreDelRubro);
			string[] dicho = new string[serie.Count];
			for (int fila = 0; fila < serie.Count; fila++) dicho[fila] = serie[fila].EtiquetaDelPeriodo;
			return dicho;
		}

		/// <summary>
		/// Lo que cobro cada linea de la serie de ese rubro, una por fila y en el mismo orden. TOTAL:
		/// cero filas donde no hay ninguna.
		/// </summary>
		internal decimal[] MontosDelHistorialDe(string nombreDelRubro)
		{
			IReadOnlyList<LineaPagada> serie = HistorialDe(nombreDelRubro);
			decimal[] dicho = new decimal[serie.Count];
			for (int fila = 0; fila < serie.Count; fila++) dicho[fila] = serie[fila].Monto;
			return dicho;
		}

		/// <summary>
		/// La mora que la municipalidad cobro en cada linea de la serie de ese rubro, una por fila y
		/// en el mismo orden. TOTAL: cero filas donde no hay ninguna.
		/// </summary>
		internal decimal[] MorasDelHistorialDe(string nombreDelRubro)
		{
			IReadOnlyList<LineaPagada> serie = HistorialDe(nombreDelRubro);
			decimal[] dicho = new decimal[serie.Count];
			for (int fila = 0; fila < serie.Count; fila++) dicho[fila] = serie[fila].Intereses;
			return dicho;
		}

		/// <summary>
		/// Cuantos periodos cubre cada linea de la serie de ese rubro, una por fila y en el mismo
		/// orden. Nunca menos de uno. TOTAL: cero filas donde no hay ninguna.
		/// </summary>
		internal int[] PeriodosCubiertosDelHistorialDe(string nombreDelRubro)
		{
			IReadOnlyList<LineaPagada> serie = HistorialDe(nombreDelRubro);
			int[] dicho = new int[serie.Count];
			for (int fila = 0; fila < serie.Count; fila++) dicho[fila] = serie[fila].PeriodosQueCubre;
			return dicho;
		}

		/// <summary>
		/// Lo que costo UN periodo en cada linea de la serie de ese rubro, una por fila y en el mismo
		/// orden. Es el monto comparable entre lineas. TOTAL: cero filas donde no hay ninguna.
		/// </summary>
		internal decimal[] MontosPorPeriodoDelHistorialDe(string nombreDelRubro)
		{
			IReadOnlyList<LineaPagada> serie = HistorialDe(nombreDelRubro);
			decimal[] dicho = new decimal[serie.Count];
			for (int fila = 0; fila < serie.Count; fila++) dicho[fila] = serie[fila].MontoPorPeriodo;
			return dicho;
		}

		/// <summary>
		/// Si cuantos periodos cubre cada linea de la serie de ese rubro se dedujo en vez de venir en
		/// el recibo, una por fila y en el mismo orden. TOTAL: cero filas donde no hay ninguna.
		/// </summary>
		internal bool[] InferenciasDelHistorialDe(string nombreDelRubro)
		{
			IReadOnlyList<LineaPagada> serie = HistorialDe(nombreDelRubro);
			bool[] dicho = new bool[serie.Count];
			for (int fila = 0; fila < serie.Count; fila++) dicho[fila] = serie[fila].EsInferido;
			return dicho;
		}

		/// <summary>
		/// El rubro de cada cosa que a esa fecha todavia no vence, una por fila y en el mismo orden
		/// que la lista. TOTAL: cero filas donde no hay ninguna.
		/// </summary>
		internal string[] RubrosDeLosVencimientosAl(DateTime fecha)
		{
			IReadOnlyList<VencimientoProximo> proximos = VencimientosAl(fecha);
			string[] dicho = new string[proximos.Count];
			for (int fila = 0; fila < proximos.Count; fila++) dicho[fila] = proximos[fila].NombreDelRubro;
			return dicho;
		}

		/// <summary>
		/// El periodo de cada cosa que a esa fecha todavia no vence, una por fila y en el mismo orden.
		/// TOTAL: cero filas donde no hay ninguna.
		/// </summary>
		internal string[] PeriodosDeLosVencimientosAl(DateTime fecha)
		{
			IReadOnlyList<VencimientoProximo> proximos = VencimientosAl(fecha);
			string[] dicho = new string[proximos.Count];
			for (int fila = 0; fila < proximos.Count; fila++) dicho[fila] = proximos[fila].EtiquetaDelPeriodo;
			return dicho;
		}

		/// <summary>
		/// El dia en que vence cada cosa que a esa fecha todavia no vence, dicho como dia de
		/// calendario, una por fila y en el mismo orden. TOTAL: cero filas donde no hay ninguna.
		/// </summary>
		internal string[] UltimosDiasDeLosVencimientosAl(DateTime fecha)
		{
			IReadOnlyList<VencimientoProximo> proximos = VencimientosAl(fecha);
			string[] dicho = new string[proximos.Count];
			for (int fila = 0; fila < proximos.Count; fila++) dicho[fila] = proximos[fila].Periodo.UltimoDiaDicho;
			return dicho;
		}

		/// <summary>
		/// Lo que se cobra por cada cosa que a esa fecha todavia no vence, una por fila y en el mismo
		/// orden. TOTAL: cero filas donde no hay ninguna.
		/// </summary>
		internal decimal[] MontosDeLosVencimientosAl(DateTime fecha)
		{
			IReadOnlyList<VencimientoProximo> proximos = VencimientosAl(fecha);
			decimal[] dicho = new decimal[proximos.Count];
			for (int fila = 0; fila < proximos.Count; fila++) dicho[fila] = proximos[fila].Monto;
			return dicho;
		}

		/// <summary>
		/// Cuantos dias faltan a esa fecha para cada cosa que todavia no vence, una por fila y en el
		/// mismo orden. Nunca negativo. TOTAL: cero filas donde no hay ninguna.
		/// </summary>
		internal int[] DiasQueFaltanDeLosVencimientosAl(DateTime fecha)
		{
			IReadOnlyList<VencimientoProximo> proximos = VencimientosAl(fecha);
			int[] dicho = new int[proximos.Count];
			for (int fila = 0; fila < proximos.Count; fila++) dicho[fila] = proximos[fila].DiasQueFaltan(fecha);
			return dicho;
		}

		/// <summary>
		/// Cuanto apura cada cosa que a esa fecha todavia no vence, por el nombre del nivel, una por
		/// fila y en el mismo orden. Los dias de aviso los pone quien pregunta. TOTAL: cero filas
		/// donde no hay ninguna.
		/// </summary>
		internal string[] UrgenciasDeLosVencimientosAl(DateTime fecha, int diasDeAviso)
		{
			IReadOnlyList<VencimientoProximo> proximos = VencimientosAl(fecha);
			string[] dicho = new string[proximos.Count];
			for (int fila = 0; fila < proximos.Count; fila++)
				dicho[fila] = proximos[fila].Urgencia(fecha, diasDeAviso).Etiqueta;
			return dicho;
		}

		/// <summary>
		/// La frase con que el dominio dice cada cosa que a esa fecha todavia no vence, una por fila y
		/// en el mismo orden. TOTAL: cero filas donde no hay ninguna.
		/// </summary>
		internal string[] FrasesDeLosVencimientosAl(DateTime fecha, int diasDeAviso)
		{
			IReadOnlyList<VencimientoProximo> proximos = VencimientosAl(fecha);
			string[] dicho = new string[proximos.Count];
			for (int fila = 0; fila < proximos.Count; fila++)
				dicho[fila] = proximos[fila].Dice(fecha, diasDeAviso);
			return dicho;
		}

		/// <summary>
		/// El rubro de cada cosa que a esa fecha ya vencio y nadie pago, una por fila y en el mismo
		/// orden que la lista. TOTAL: cero filas donde no hay ninguna.
		/// </summary>
		internal string[] RubrosDeLosAtrasosAl(DateTime fecha)
		{
			IReadOnlyList<Atraso> atrasos = AtrasosAl(fecha);
			string[] dicho = new string[atrasos.Count];
			for (int fila = 0; fila < atrasos.Count; fila++) dicho[fila] = atrasos[fila].NombreDelRubro;
			return dicho;
		}

		/// <summary>
		/// El periodo de cada cosa que a esa fecha ya vencio y nadie pago, una por fila y en el mismo
		/// orden. TOTAL: cero filas donde no hay ninguna.
		/// </summary>
		internal string[] PeriodosDeLosAtrasosAl(DateTime fecha)
		{
			IReadOnlyList<Atraso> atrasos = AtrasosAl(fecha);
			string[] dicho = new string[atrasos.Count];
			for (int fila = 0; fila < atrasos.Count; fila++) dicho[fila] = atrasos[fila].EtiquetaDelPeriodo;
			return dicho;
		}

		/// <summary>
		/// El dia en que vencio cada cosa que a esa fecha ya vencio, dicho como dia de calendario, una
		/// por fila y en el mismo orden. TOTAL: cero filas donde no hay ninguna.
		/// </summary>
		internal string[] UltimosDiasDeLosAtrasosAl(DateTime fecha)
		{
			IReadOnlyList<Atraso> atrasos = AtrasosAl(fecha);
			string[] dicho = new string[atrasos.Count];
			for (int fila = 0; fila < atrasos.Count; fila++) dicho[fila] = atrasos[fila].Periodo.UltimoDiaDicho;
			return dicho;
		}

		/// <summary>
		/// Lo que se cobra por cada cosa que a esa fecha ya vencio, una por fila y en el mismo orden.
		/// No incluye mora: la mora real llega en el recibo. TOTAL: cero filas donde no hay ninguna.
		/// </summary>
		internal decimal[] MontosDeLosAtrasosAl(DateTime fecha)
		{
			IReadOnlyList<Atraso> atrasos = AtrasosAl(fecha);
			decimal[] dicho = new decimal[atrasos.Count];
			for (int fila = 0; fila < atrasos.Count; fila++) dicho[fila] = atrasos[fila].Monto;
			return dicho;
		}

		/// <summary>
		/// Cuantos dias de atraso lleva a esa fecha cada cosa ya vencida, una por fila y en el mismo
		/// orden. Nunca negativo. TOTAL: cero filas donde no hay ninguna.
		/// </summary>
		internal int[] DiasDeAtrasoDeLosAtrasosAl(DateTime fecha)
		{
			IReadOnlyList<Atraso> atrasos = AtrasosAl(fecha);
			int[] dicho = new int[atrasos.Count];
			for (int fila = 0; fila < atrasos.Count; fila++) dicho[fila] = atrasos[fila].DiasDeAtraso(fecha);
			return dicho;
		}

		/// <summary>
		/// Lo que PROYECTA costar pagar tarde cada cosa ya vencida a esa fecha y a esa tasa, una por
		/// fila y en el mismo orden. Es una proyeccion y nunca contradice lo que la municipalidad
		/// cobro. La tasa la pone quien pregunta. TOTAL: cero filas donde no hay ninguna.
		/// </summary>
		internal decimal[] CostosDePagarTardeDeLosAtrasosAl(DateTime fecha, decimal tasaAnual)
		{
			IReadOnlyList<Atraso> atrasos = AtrasosAl(fecha);
			decimal[] dicho = new decimal[atrasos.Count];
			for (int fila = 0; fila < atrasos.Count; fila++)
				dicho[fila] = atrasos[fila].CostoDePagarTarde(fecha, tasaAnual);
			return dicho;
		}

		/// <summary>
		/// Cuanto apura cada cosa que a esa fecha ya vencio, por el nombre del nivel, una por fila y
		/// en el mismo orden. TOTAL: cero filas donde no hay ninguna.
		/// </summary>
		internal string[] UrgenciasDeLosAtrasosAl(DateTime fecha)
		{
			IReadOnlyList<Atraso> atrasos = AtrasosAl(fecha);
			string[] dicho = new string[atrasos.Count];
			for (int fila = 0; fila < atrasos.Count; fila++) dicho[fila] = atrasos[fila].Urgencia().Etiqueta;
			return dicho;
		}

		/// <summary>
		/// La frase con que el dominio dice cada cosa que a esa fecha ya vencio, con su proyeccion a esa
		/// tasa dicha COMO proyeccion, una por fila y en el mismo orden. TOTAL: cero filas donde no hay
		/// ninguna.
		/// </summary>
		internal string[] FrasesDeLosAtrasosAl(DateTime fecha, decimal tasaAnual)
		{
			IReadOnlyList<Atraso> atrasos = AtrasosAl(fecha);
			string[] dicho = new string[atrasos.Count];
			for (int fila = 0; fila < atrasos.Count; fila++) dicho[fila] = atrasos[fila].Dice(fecha, tasaAnual);
			return dicho;
		}

		/// <summary>
		/// El rubro de cada periodo que la consulta de ese dia da por pagado y la app no vio pagar,
		/// una por fila y en el mismo orden que la lista. TOTAL: cero filas donde no hay ninguna, y
		/// tambien donde no hay consulta de ese dia.
		/// </summary>
		internal string[] RubrosDeLoPagadoSinComprobante(DateTime fechaDeConsulta)
		{
			IReadOnlyList<PeriodoSinComprobante> sinComprobante = PeriodosPagadosSinComprobante(fechaDeConsulta);
			string[] dicho = new string[sinComprobante.Count];
			for (int fila = 0; fila < sinComprobante.Count; fila++) dicho[fila] = sinComprobante[fila].NombreDelRubro;
			return dicho;
		}

		/// <summary>
		/// El periodo de cada uno que la consulta de ese dia da por pagado y la app no vio pagar, una
		/// por fila y en el mismo orden. TOTAL: cero filas donde no hay ninguna, y tambien donde no
		/// hay consulta de ese dia.
		/// </summary>
		internal string[] PeriodosDeLoPagadoSinComprobante(DateTime fechaDeConsulta)
		{
			IReadOnlyList<PeriodoSinComprobante> sinComprobante = PeriodosPagadosSinComprobante(fechaDeConsulta);
			string[] dicho = new string[sinComprobante.Count];
			for (int fila = 0; fila < sinComprobante.Count; fila++) dicho[fila] = sinComprobante[fila].EtiquetaDelPeriodo;
			return dicho;
		}

		/// <summary>
		/// La frase con que el dominio dice cada periodo que la consulta de ese dia da por pagado y la
		/// app no vio pagar, una por fila y en el mismo orden. TOTAL: cero filas donde no hay ninguna.
		/// </summary>
		internal string[] FrasesDeLoPagadoSinComprobante(DateTime fechaDeConsulta)
		{
			IReadOnlyList<PeriodoSinComprobante> sinComprobante = PeriodosPagadosSinComprobante(fechaDeConsulta);
			string[] dicho = new string[sinComprobante.Count];
			for (int fila = 0; fila < sinComprobante.Count; fila++) dicho[fila] = sinComprobante[fila].Dice();
			return dicho;
		}

		/// <summary>
		/// Por que la serie de ese rubro trae lo que trae, por el nombre del estado. TOTAL, y separa
		/// las DOS razones por las que la serie sale vacia: el rubro no existe, o no tiene historia.
		/// </summary>
		internal string EtiquetaDelEstadoDeLaSerieDe(string nombreDelRubro)
		{
			return EstadoDeLaSerie.De(nombreDelRubro, HistorialDe(nombreDelRubro)).Etiqueta;
		}

		/// <summary>
		/// La frase con que el dominio dice por que la serie de ese rubro trae lo que trae. TOTAL, y es
		/// la del REGISTRO entero: describe la respuesta y no una fila.
		/// </summary>
		internal string FraseDelEstadoDeLaSerieDe(string nombreDelRubro)
		{
			return EstadoDeLaSerie.De(nombreDelRubro, HistorialDe(nombreDelRubro)).Dice();
		}

		/// <summary>
		/// Por que el cotejo de la consulta de ese dia trae lo que trae, por el nombre del estado.
		/// TOTAL, y separa las DOS razones por las que sale vacio: no hay consulta de ese dia, o la
		/// consulta no destapa nada.
		/// </summary>
		internal string EtiquetaDelEstadoDeLoSinComprobante(DateTime fechaDeConsulta)
		{
			return EstadoDeLosPeriodosSinComprobante.De(
				fechaDeConsulta,
				TieneConsultaDel(fechaDeConsulta),
				PeriodosPagadosSinComprobante(fechaDeConsulta)).Etiqueta;
		}

		/// <summary>
		/// La frase con que el dominio dice por que el cotejo de ese dia trae lo que trae. TOTAL, y es
		/// la del REGISTRO entero: describe la respuesta y no una fila.
		/// </summary>
		internal string FraseDelEstadoDeLoSinComprobante(DateTime fechaDeConsulta)
		{
			return EstadoDeLosPeriodosSinComprobante.De(
				fechaDeConsulta,
				TieneConsultaDel(fechaDeConsulta),
				PeriodosPagadosSinComprobante(fechaDeConsulta)).Dice();
		}

		/// <summary>
		/// El nombre de cada rubro del que la app vio pagar algo, uno por fila y en el orden en que
		/// aparecieron. TOTAL: cero filas donde no hay ningun pago.
		/// </summary>
		internal string[] NombresDeLosRubrosConHistoria()
		{
			IReadOnlyList<Rubro> conHistoria = RubrosConHistoria();
			string[] dicho = new string[conHistoria.Count];
			for (int fila = 0; fila < conHistoria.Count; fila++) dicho[fila] = conHistoria[fila].Nombre;
			return dicho;
		}

		/// <summary>PREDICADO TOTAL: hay al menos una consulta registrada?</summary>
		internal bool HayAlgunaConsulta { get { return consultas.Count > 0; } }

		/// <summary>
		/// El dia de la consulta mas reciente. GUARDADO: sin ninguna consulta no hay un dia que
		/// contestar y no se inventa uno. Preguntar HayAlgunaConsulta antes.
		/// </summary>
		internal DateTime ConsultaMasReciente
		{
			get
			{
				if (!HayAlgunaConsulta)
					throw new DomainException(
						"No hay ninguna consulta registrada, asi que no hay una mas reciente. "
						+ "Preguntar HayAlgunaConsulta antes.");
				DateTime masReciente = consultas[0].Fecha;
				foreach (Consulta c in consultas)
					if (c.Fecha > masReciente) masReciente = c.Fecha;
				return masReciente;
			}
		}

		/// <summary>
		/// El numero de cada finca incorporada, uno por fila y en el orden en que entraron. TOTAL: cero
		/// filas donde no hay ninguna.
		/// </summary>
		internal string[] NumerosDeLasFincas()
		{
			string[] dicho = new string[fincas.Count];
			for (int fila = 0; fila < fincas.Count; fila++) dicho[fila] = fincas[fila].Numero;
			return dicho;
		}

		/// <summary>
		/// PREDICADO TOTAL: esa finca esta Y tenia un valor declarado vigente a esa fecha? Junta las
		/// DOS razones por las que el valor no se puede leer, para que quien pregunta no tenga que
		/// distinguirlas antes de saber si hay respuesta.
		/// </summary>
		internal bool TieneValorDeFincaAl(string numero, DateTime fecha)
		{
			return TieneFinca(numero) && FincaNumero(numero).TieneValorAl(fecha);
		}

		/// <summary>
		/// El valor que esa finca tenia declarado a esa fecha. GUARDADO: no se inventa un valor que la
		/// persona no declaro. Preguntar TieneValorDeFincaAl antes.
		/// </summary>
		internal decimal ValorDeFincaAl(string numero, DateTime fecha)
		{
			if (!TieneValorDeFincaAl(numero, fecha))
				throw new DomainException(
					"Esa finca no esta, o no tenia valor declarado a esa fecha. "
					+ "Preguntar TieneValorDeFincaAl antes.");
			return FincaNumero(numero).ValorVigenteAl(fecha);
		}
	}
}
