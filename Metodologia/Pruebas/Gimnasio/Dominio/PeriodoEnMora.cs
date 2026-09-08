using System;

namespace Gimnasio.Dominio
{
	// UN TRAMO EN QUE LA MEMBRESIA ESTUVO EN MORA.
	//
	// DERIVADO, NUNCA GUARDADO: nace de un cambio a en-mora y termina en el siguiente
	// cambio a al-dia -o queda ABIERTO si ese cambio todavia no llego-. Es la Suspension
	// de Fowler (fig. 8.4): un Time Period sobre la accion, calculado a partir de los
	// cambios de estado.
	//
	// Es un carrier PLANO -dos fechas y un bool- porque el print proyecta un solo nivel
	// (regla 13), y el fin, que puede no existir, viaja como bool mas valor y no como
	// hueco. El Hasta nace GUARDADO por TieneFin: es la leccion de T1 -el Fin del Plan
	// se leia sin guarda y devolvia 01/01/0001 en silencio- aplicada al codigo nuevo.
	internal sealed class PeriodoEnMora
	{
		private readonly DateTime hasta;

		internal DateTime Desde { get; }
		internal bool TieneFin { get; }

		private PeriodoEnMora(DateTime desde, bool tieneFin, DateTime hasta)
		{
			Desde = desde.Date;
			TieneFin = tieneFin;
			this.hasta = hasta.Date;
		}

		internal static PeriodoEnMora Abierto(DateTime desde)
		{
			return new PeriodoEnMora(desde, false, DateTime.MaxValue);
		}

		internal static PeriodoEnMora Cerrado(DateTime desde, DateTime hasta)
		{
			if (hasta.Date < desde.Date)
				throw new ArgumentOutOfRangeException(nameof(hasta), hasta,
					"Un periodo en mora no puede terminar antes de empezar.");
			return new PeriodoEnMora(desde, true, hasta);
		}

		internal DateTime Hasta
		{
			get
			{
				if (!TieneFin)
					throw new InvalidOperationException(
						"Este periodo en mora sigue abierto: el gimnasio no mando todavia el cambio a al-dia. "
						+ "Preguntar TieneFin antes.");
				return hasta;
			}
		}

		// PREDICADO TOTAL: contesta para cualquier fecha. Un periodo abierto contiene
		// todo lo que venga despues de su inicio.
		internal bool Contiene(DateTime fecha)
		{
			DateTime f = fecha.Date;
			if (f < Desde) return false;
			return !TieneFin || f <= hasta;
		}

		// CUANTOS DIAS DE ESTE PERIODO CAEN DENTRO DE [desde, hasta], ambos inclusive.
		// Total: cero si no se tocan. Un periodo abierto se corta en `hasta`: no se sabe
		// cuando va a terminar, pero dentro del tramo preguntado se sabe que todos esos
		// dias estuvieron en mora. Es lo que N2 descuenta del plan.
		//
		// Un dia en mora es un dia entero, por eso el +1: "del 3 al 17" son 15 dias.
		internal int DiasDentroDe(DateTime desde, DateTime hasta)
		{
			DateTime lo = Desde > desde.Date ? Desde : desde.Date;
			DateTime hi = TieneFin && this.hasta < hasta.Date ? this.hasta : hasta.Date;
			if (hi < lo) return 0;
			return (hi - lo).Days + 1;
		}
	}
}
