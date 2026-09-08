using System;

namespace Municipalidad.Dominio
{
	// LO QUE LA MUNICIPALIDAD DICE QUE VA A COBRAR: un rubro, un periodo, un monto.
	//
	// Es INTENCION de la institucion, no hecho: la Proposed Action de la pasada 2. El
	// pago es su Implemented Action. Un cobro puesto y no pagado, con el periodo vencido,
	// es un Atraso; con el periodo por vencer, un VencimientoProximo. Ninguno de los dos
	// se guarda: se derivan de comparar cobros contra pagos.
	//
	// HUECO 11: la app nunca ve "el cobro" como evento. Lo que ve es la consulta -la foto-
	// con sus lineas pendientes, y eso es lo que se registra como puesto al cobro, con la
	// fecha de la consulta. Si el portal algun dia manda los cobros de otra forma, cambia.
	internal sealed class Cobro
	{
		internal Rubro Rubro { get; }
		internal Periodo Periodo { get; }
		internal decimal Monto { get; }
		internal DateTime PuestoEl { get; }

		internal Cobro(Rubro rubro, Periodo periodo, decimal monto, DateTime puestoEl)
		{
			ArgumentNullException.ThrowIfNull(rubro);
			ArgumentNullException.ThrowIfNull(periodo);
			if (monto <= 0)
				throw new ArgumentOutOfRangeException(nameof(monto), monto, "Un cobro tiene que ser positivo.");
			Rubro = rubro;
			Periodo = periodo;
			Monto = monto;
			PuestoEl = puestoEl.Date;
		}

		internal bool EsDe(Rubro rubro, Periodo periodo)
		{
			return Rubro.Es(rubro) && Periodo.EsElMismoQue(periodo);
		}
	}
}
