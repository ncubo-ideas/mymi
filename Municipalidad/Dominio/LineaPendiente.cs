using System;

namespace Municipalidad.Dominio
{
	// UNA LINEA DE LA CONSULTA: lo que el portal dice que se debe hoy, por rubro, hasta
	// que periodo, y con cuanta mora acumulada. La mora es recibida, igual que en el
	// recibo: la app no la recalcula.
	internal sealed class LineaPendiente
	{
		internal Rubro Rubro { get; }
		internal Periodo Periodo { get; }
		internal decimal Monto { get; }
		internal decimal Intereses { get; }

		internal LineaPendiente(Rubro rubro, Periodo periodo, decimal monto, decimal intereses)
		{
			ArgumentNullException.ThrowIfNull(rubro);
			ArgumentNullException.ThrowIfNull(periodo);
			if (monto <= 0)
				throw new ArgumentOutOfRangeException(nameof(monto), monto, "Un pendiente tiene que ser positivo.");
			if (intereses < 0)
				throw new ArgumentOutOfRangeException(nameof(intereses), intereses, "La mora no puede ser negativa.");
			Rubro = rubro;
			Periodo = periodo;
			Monto = monto;
			Intereses = intereses;
		}

		internal decimal Total { get { return Monto + Intereses; } }

		internal bool EsDe(Rubro rubro, Periodo periodo)
		{
			return Rubro.Es(rubro) && Periodo.EsElMismoQue(periodo);
		}
	}
}
