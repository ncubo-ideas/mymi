using System;

namespace Gimnasio.Dominio
{
	// UN PAGO QUE EL GIMNASIO REGISTRO. Hecho: fecha y monto, y nada mas.
	//
	// No dice que periodos cubre ni si llego a tiempo: eso lo dice el gimnasio con la
	// bandera (N1). La app no deduce mora de los pagos, porque la politica de cuando una
	// cuenta esta en mora es del gimnasio y la app la estaria inventando.
	//
	// Es la Implemented Action de Fowler (fig. 8.2); el Cobro es la Proposed Action.
	internal sealed class Pago
	{
		internal DateTime Fecha { get; }
		internal decimal Monto { get; }

		internal Pago(DateTime fecha, decimal monto)
		{
			if (monto <= 0)
				throw new ArgumentOutOfRangeException(nameof(monto), monto,
					"Un pago tiene que ser positivo.");
			Fecha = fecha.Date;
			Monto = monto;
		}

		// IDEMPOTENCIA POR IDENTIDAD (regla 18): el mismo pago puede volver a llegar -un
		// replay es exactamente eso- y no puede contarse dos veces. Sin identificador
		// del gimnasio, la identidad es fecha mas monto.
		internal bool EsElMismoQue(DateTime fecha, decimal monto)
		{
			return Fecha == fecha.Date && Monto == monto;
		}
	}
}
