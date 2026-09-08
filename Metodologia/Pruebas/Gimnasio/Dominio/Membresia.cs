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
	// bandera, la membresia no esta al dia ni en mora: NO SE SABE, y EstaEnMora lo dice
	// reventando. Arrancar en al-dia seria un juicio inventado con mejores modales
	// (capitulo 3 §3). Lo que SI es total es el descuento: sin mora registrada se
	// descuenta cero, porque solo se descuenta lo que el gimnasio dijo -"si el gimnasio
	// no lo manda, no existe"-.
	internal sealed class Membresia
	{
		private readonly List<CambioDeEstado> cambios = new List<CambioDeEstado>();
		private readonly List<Pago> pagos = new List<Pago>();
		private readonly List<Cobro> cobros = new List<Cobro>();

		internal int CuantosCambiosDeEstado { get { return cambios.Count; } }
		internal int CuantosPagos { get { return pagos.Count; } }
		internal int CuantosCobros { get { return cobros.Count; } }

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
			if (!PuedeAnunciarCobro(cobro.Vence, cobro.Monto))
				throw new InvalidOperationException(
					"Ya hay un cobro anunciado para ese vencimiento. Preguntar PuedeAnunciarCobro antes.");
			cobros.Add(cobro);
		}

		// -------------------------------- UN PREDICADO POR COMANDO (reglas 17 y 18)

		// EL MISMO ESTADO OTRA VEZ ES EL MISMO HECHO OTRA VEZ. Un feed que repite "en
		// mora" no esta diciendo nada nuevo, y un replay lo repite siempre. Es la
		// leccion de Asistio: el comando que no podia fallar conto 30 dias dos veces.
		// Y no se cambia hacia atras: los cambios quedan en orden, que es lo que
		// PeriodosEnMora necesita para derivar los tramos.
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

		internal bool PuedeAnunciarCobro(DateTime vence, decimal monto)
		{
			if (monto <= 0) return false;
			foreach (Cobro c in cobros)
				if (c.EsElMismoQue(vence)) return false;
			return true;
		}

		// ------------------------------------------------------------------ el estado

		internal bool TieneEstadoDeclarado { get { return cambios.Count > 0; } }

		// GUARDADO: sin bandera no hay estado, y no se inventa uno.
		internal bool EstaEnMora()
		{
			ExigirEstado();
			return cambios[cambios.Count - 1].Estado.EsMora();
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
		internal bool EstabaEnMoraEl(DateTime fecha)
		{
			DateTime f = fecha.Date;
			CambioDeEstado vigente = null;
			foreach (CambioDeEstado c in cambios)
				if (c.Fecha <= f) vigente = c;
			return vigente != null && vigente.Estado.EsMora();
		}

		// ------------------------------------------ los periodos, derivados (fig. 8.4)

		// Cada tramo en mora, del cambio a en-mora al siguiente cambio a al-dia. El
		// ultimo puede quedar ABIERTO. Se recalcula cada vez: no se guarda.
		internal IReadOnlyList<PeriodoEnMora> PeriodosEnMora()
		{
			List<PeriodoEnMora> periodos = new List<PeriodoEnMora>();
			bool enMora = false;
			DateTime desde = DateTime.MinValue;
			foreach (CambioDeEstado c in cambios)
			{
				if (c.Estado.EsMora() && !enMora) { enMora = true; desde = c.Fecha; }
				else if (c.Estado.EsAlDia() && enMora)
				{
					periodos.Add(PeriodoEnMora.Cerrado(desde, c.Fecha));
					enMora = false;
				}
			}
			if (enMora) periodos.Add(PeriodoEnMora.Abierto(desde));
			return periodos;
		}

		// CUANTOS DIAS EN MORA CAEN DENTRO DE [desde, hasta]. Total: cero sin mora. Es
		// el numero que N2 le descuenta al plan: durante la mora las asistencias
		// esperadas no se esperan (Fowler 8.4, la suspension).
		internal int DiasEnMoraEntre(DateTime desde, DateTime hasta)
		{
			int dias = 0;
			foreach (PeriodoEnMora p in PeriodosEnMora())
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
