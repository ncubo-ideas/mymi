using System;

namespace Municipalidad.Dominio
{
	/// <summary>
	/// Lo que la municipalidad dice que va a cobrar: un rubro, un periodo y un monto positivo. Es
	/// INTENCION de la institucion y no un hecho; el hecho es el pago.
	/// <para>
	/// Un cobro sin pagar no guarda su estado: el atraso y el vencimiento proximo se derivan de
	/// compararlo contra los pagos.
	/// </para>
	/// <para>
	/// La app nunca ve el cobro como evento. Lo que ve es la consulta con sus lineas pendientes, y eso
	/// es lo que queda puesto al cobro, con la fecha de la consulta.
	/// </para>
	/// </summary>
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
