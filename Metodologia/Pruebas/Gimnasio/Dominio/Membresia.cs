using System;
using System.Collections.Generic;

namespace Gimnasio.Dominio
{
	// LA MEMBRESIA: lo que hace que Sonia sea socia. Es el "en virtud de que" -el
	// relator, glosario nº 20- y es lo que el v1 no tenia.
	//
	// Estaba declarada en la Parte 1 del Preguntas.md -"pagos, proximo cobro, atrasos"-
	// y a la vez en "lo que el gimnasio ya contesta, y por eso se descarta". La regla
	// del descarte del bloque 1, mal escrita, la dejo afuera del modelo entero (hueco
	// 12), y por eso un periodo en mora contaba como incumplimiento del plan (hueco 9).
	// Que el gimnasio ya te diga si estas al dia es razon perfecta para no hacer una
	// pantalla de pagos. No es ninguna razon para que el dominio no sepa que es una
	// membresia.
	//
	// LO PROPIO (N5): sabe cuando el plan estuvo en mora, y por eso cuando un
	// incumplimiento no fue incumplimiento. Nadie mas puede decirlo. Por eso es una
	// clase y no tres listas sueltas en la raiz: JUZGA, no solo parametriza.
	//
	// NADA DE ESTO LO APORTA SONIA. Bandera, pagos y cobros llegan del gimnasio; el
	// journal sigue siendo reconstruible.
	//
	// SIN ESTADO DECLARADO NO HAY "AL DIA" POR DEFECTO. Si el gimnasio nunca mando la
	// bandera, la membresia no esta al dia ni en mora: NO SE SABE, y EstaInactiva lo dice
	// reventando. Arrancar en al-dia seria un juicio inventado con mejores modales
	// (capitulo 3 §3). Lo que SI es total es el descuento: sin mora registrada se
	// descuenta cero, porque solo se descuenta lo que el gimnasio dijo -"si el gimnasio
	// no lo manda, no existe"-.
	internal sealed class Membresia
	{
		private readonly List<CambioDeEstado> cambios = new List<CambioDeEstado>();
		private readonly List<Pago> pagos = new List<Pago>();
		private readonly List<Cobro> cobros = new List<Cobro>();
		private readonly List<Contratacion> contrataciones = new List<Contratacion>();

		internal int CuantosCambiosDeEstado { get { return cambios.Count; } }
		internal int CuantosPagos { get { return pagos.Count; } }
		internal int CuantosCobros { get { return cobros.Count; } }

		// ------------------------------------------------ el producto contratado (N11, v3)

		// "Plan Black" y "Fecha de vencimiento: Dia 10" entran juntos como una contratacion
		// con fecha. Se guarda la historia porque la persona puede cambiar de producto;
		// la vigente es la ultima.
		internal void Contratar(Contratacion contratacion)
		{
			ArgumentNullException.ThrowIfNull(contratacion);
			if (!PuedeContratar(contratacion.Producto, contratacion.DiaDeVencimiento, contratacion.Fecha))
				throw new InvalidOperationException(
					"Esa contratacion no entra: es el mismo producto que ya tiene, o es anterior a la ultima. "
					+ "Preguntar PuedeContratar antes.");
			contrataciones.Add(contratacion);
		}

		// EL MISMO PRODUCTO OTRA VEZ ES EL MISMO HECHO OTRA VEZ (regla 18); y no se
		// contrata hacia atras.
		internal bool PuedeContratar(string producto, int diaDeVencimiento, DateTime fecha)
		{
			if (string.IsNullOrWhiteSpace(producto)) return false;
			if (diaDeVencimiento < 1 || diaDeVencimiento > 31) return false;
			if (contrataciones.Count == 0) return true;
			Contratacion ultima = contrataciones[contrataciones.Count - 1];
			if (ultima.EsElMismoProducto(producto)) return false;
			return fecha.Date > ultima.Fecha;
		}

		internal bool TieneProductoContratado { get { return contrataciones.Count > 0; } }

		internal string Producto
		{
			get { ExigirContratacion(); return contrataciones[contrataciones.Count - 1].Producto; }
		}

		internal int DiaDeVencimiento
		{
			get { ExigirContratacion(); return contrataciones[contrataciones.Count - 1].DiaDeVencimiento; }
		}

		private void ExigirContratacion()
		{
			if (!TieneProductoContratado)
				throw new InvalidOperationException(
					"El gimnasio nunca mando que producto tiene esta membresia. Preguntar TieneProductoContratado antes.");
		}

		// ------------------------------------------------------------------ lo que entra

		internal void CambiarEstado(EstadoDeMembresia estado, DateTime fecha)
		{
			ArgumentNullException.ThrowIfNull(estado);
			if (!PuedeCambiarA(estado, fecha))
				throw new InvalidOperationException(
					"Ese cambio de estado no entra: o es el mismo estado que ya tiene, o es anterior "
					+ "al ultimo cambio. Preguntar PuedeCambiarA antes.");
			cambios.Add(new CambioDeEstado(fecha, estado));
		}

		internal void RegistrarPago(Pago pago)
		{
			ArgumentNullException.ThrowIfNull(pago);
			if (!PuedeRegistrarPago(pago.Fecha, pago.Monto))
				throw new InvalidOperationException(
					"Ese pago ya estaba registrado. Preguntar PuedeRegistrarPago antes.");
			pagos.Add(pago);
		}

		internal void AnunciarCobro(Cobro cobro)
		{
			ArgumentNullException.ThrowIfNull(cobro);
			if (!PuedeAnunciarCobro(cobro.Vence, cobro.Monto, cobro.Etiqueta, cobro.CubreDesde, cobro.CubreHasta,
					cobro.Estado.Etiqueta))
				throw new InvalidOperationException(
					"Ya hay un cobro anunciado para ese vencimiento, o el cobro no es valido. "
					+ "Preguntar PuedeAnunciarCobro antes.");
			cobros.Add(cobro);
		}

		// EL GIMNASIO DICE QUE UN COBRO PASO A PAGADO (hueco 18). Se identifica por su
		// vencimiento; el predicado junta cobro inexistente, estado desconocido y mismo
		// estado otra vez (reglas 17 y 18).
		internal bool PuedeCambiarEstadoDeCobro(DateTime vence, string nombreDelEstado)
		{
			if (!EstadoDeCobro.EsUnNombreConocido(nombreDelEstado)) return false;
			foreach (Cobro c in cobros)
				if (c.EsElMismoQue(vence)) return c.PuedeCambiarA(EstadoDeCobro.PorNombre(nombreDelEstado));
			return false;
		}

		internal void CambiarEstadoDeCobro(DateTime vence, EstadoDeCobro estado)
		{
			ArgumentNullException.ThrowIfNull(estado);
			foreach (Cobro c in cobros)
				if (c.EsElMismoQue(vence)) { c.CambiarEstado(estado); return; }
			throw new KeyNotFoundException(
				"No hay un cobro con ese vencimiento. Preguntar PuedeCambiarEstadoDeCobro antes.");
		}

		// ------------------------------------------------ que cobro cubre un dia (N10, v3)

		// PREDICADO TOTAL. El gimnasio manda cada cobro con el periodo que cubre; la app
		// puede decir que cobro cubre el dia que la persona entreno. A lo sumo uno: dos
		// cobros sobre el mismo dia se rechazan al anunciar (hueco 22, cerrado por Jorge).
		internal bool TieneCobroQueCubre(DateTime dia)
		{
			foreach (Cobro c in cobros)
				if (c.Cubre(dia)) return true;
			return false;
		}

		internal Cobro CobroQueCubre(DateTime dia)
		{
			foreach (Cobro c in cobros)
				if (c.Cubre(dia)) return c;
			throw new KeyNotFoundException(
				"Ningun cobro anunciado cubre ese dia. Preguntar TieneCobroQueCubre antes.");
		}

		// -------------------------------- UN PREDICADO POR COMANDO (reglas 17 y 18)

		// EL MISMO ESTADO OTRA VEZ ES EL MISMO HECHO OTRA VEZ. Un feed que repite "en
		// mora" no esta diciendo nada nuevo, y un replay lo repite siempre. Es la
		// leccion de Asistio: el comando que no podia fallar conto 30 dias dos veces.
		// Y no se cambia hacia atras: los cambios quedan en orden, que es lo que
		// PeriodosInactivos necesita para derivar los tramos.
		internal bool PuedeCambiarA(EstadoDeMembresia estado, DateTime fecha)
		{
			if (estado == null) return false;
			if (cambios.Count == 0) return true;
			CambioDeEstado ultimo = cambios[cambios.Count - 1];
			if (ultimo.Estado.Es(estado)) return false;
			return fecha.Date >= ultimo.Fecha;
		}

		// La sobrecarga por NOMBRE: un Check solo tiene el string que vino como
		// parametro. Sin ella el Check no puede preguntar lo que el comando va a
		// rechazar, y eso envenena el journal (regla 17).
		internal bool PuedeCambiarA(string nombreDelEstado, DateTime fecha)
		{
			return EstadoDeMembresia.EsUnNombreConocido(nombreDelEstado)
				&& PuedeCambiarA(EstadoDeMembresia.PorNombre(nombreDelEstado), fecha);
		}

		internal bool PuedeRegistrarPago(DateTime fecha, decimal monto)
		{
			if (monto <= 0) return false;
			foreach (Pago p in pagos)
				if (p.EsElMismoQue(fecha, monto)) return false;
			return true;
		}

		// Junta todo lo que el constructor de Cobro rechazaria (regla 17) mas la identidad
		// por vencimiento (regla 18) y el estado desconocido (regla 11).
		internal bool PuedeAnunciarCobro(DateTime vence, decimal monto, string etiqueta,
			DateTime cubreDesde, DateTime cubreHasta, string nombreDelEstado)
		{
			if (monto <= 0) return false;
			if (string.IsNullOrWhiteSpace(etiqueta)) return false;
			if (cubreHasta.Date < cubreDesde.Date) return false;
			if (!EstadoDeCobro.EsUnNombreConocido(nombreDelEstado)) return false;
			foreach (Cobro c in cobros)
			{
				if (c.EsElMismoQue(vence)) return false;
				if (c.SeSolapaCon(cubreDesde, cubreHasta)) return false;   // hueco 22: es un error
			}
			return true;
		}

		// ------------------------------------------------------------------ el estado

		internal bool TieneEstadoDeclarado { get { return cambios.Count > 0; } }

		// GUARDADO: sin bandera no hay estado, y no se inventa uno.
		internal bool EstaInactiva()
		{
			ExigirEstado();
			return cambios[cambios.Count - 1].Estado.EsNoActiva();
		}

		// La hoja para el actor: el nombre del estado actual. Guardado igual.
		internal string EstadoActual
		{
			get
			{
				ExigirEstado();
				return cambios[cambios.Count - 1].Estado.Etiqueta;
			}
		}

		// PREDICADO TOTAL: contesta para cualquier fecha. Antes del primer cambio, o sin
		// cambios, no consta mora -que no es lo mismo que "al dia", pero para descontar
		// del plan es lo unico honesto: solo se descuenta lo que el gimnasio dijo-.
		internal bool EstabaInactivaEl(DateTime fecha)
		{
			DateTime f = fecha.Date;
			CambioDeEstado vigente = null;
			foreach (CambioDeEstado c in cambios)
				if (c.Fecha <= f) vigente = c;
			return vigente != null && vigente.Estado.EsNoActiva();
		}

		// ------------------------------------------ los periodos, derivados (fig. 8.4)

		// Cada tramo en mora, del cambio a en-mora al siguiente cambio a activo. El
		// ultimo puede quedar ABIERTO. Se recalcula cada vez: no se guarda.
		internal IReadOnlyList<PeriodoInactivo> PeriodosInactivos()
		{
			List<PeriodoInactivo> periodos = new List<PeriodoInactivo>();
			bool inactiva = false;
			DateTime desde = DateTime.MinValue;
			foreach (CambioDeEstado c in cambios)
			{
				if (c.Estado.EsNoActiva() && !inactiva) { inactiva = true; desde = c.Fecha; }
				else if (c.Estado.EsActivo() && inactiva)
				{
					periodos.Add(PeriodoInactivo.Cerrado(desde, c.Fecha));
					inactiva = false;
				}
			}
			if (inactiva) periodos.Add(PeriodoInactivo.Abierto(desde));
			return periodos;
		}

		// CUANTOS DIAS EN MORA CAEN DENTRO DE [desde, hasta]. Total: cero sin mora. Es
		// el numero que N2 le descuenta al plan: durante la mora las asistencias
		// esperadas no se esperan (Fowler 8.4, la suspension).
		internal int DiasInactivaEntre(DateTime desde, DateTime hasta)
		{
			int dias = 0;
			foreach (PeriodoInactivo p in PeriodosInactivos())
				dias += p.DiasDentroDe(desde, hasta);
			return dias;
		}

		// ------------------------------------------------------------------ internos

		private void ExigirEstado()
		{
			if (!TieneEstadoDeclarado)
				throw new InvalidOperationException(
					"El gimnasio nunca mando la bandera de esta membresia, asi que no se sabe si esta "
					+ "al dia o en mora -y no se inventa-. Preguntar TieneEstadoDeclarado antes.");
		}
	}
}
