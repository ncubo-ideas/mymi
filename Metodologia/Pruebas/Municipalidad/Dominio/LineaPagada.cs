using System;

namespace Municipalidad.Dominio
{
	// UNA LINEA DE UN COMPROBANTE: un rubro, hasta que periodo cubre, cuanto, y la mora.
	//
	// LA MORA SE RECIBE, NO SE CALCULA. `Intereses` es lo que la municipalidad cobro, tal
	// cual vino en el recibo. Es la decision de producto del 3 de setiembre -"recibir y
	// alarmar, no verificar"- y la regla de la guia: la evaluacion de la institucion llega
	// como hecho. La app no la recalcula ni la discute.
	//
	// CUANTOS PERIODOS CUBRE ES INFERIDO -hueco 6-. El recibo dice hasta que periodo
	// llega la linea y cuanto, pero no cuantos periodos son: 22.080 de basura en 2026.06
	// eran tres meses a 7.360. La Cuenta lo deduce de la distancia al ultimo periodo
	// pagado de ese rubro, y por eso `PeriodosQueCubre` lo pone ella, no el recibo. Sin
	// historia previa se asume 1 y `EsInferido` lo dice.
	internal sealed class LineaPagada
	{
		internal Rubro Rubro { get; }
		internal Periodo Periodo { get; }
		internal decimal Monto { get; }
		internal decimal Intereses { get; }
		internal int PeriodosQueCubre { get; }
		internal bool EsInferido { get; }

		internal LineaPagada(Rubro rubro, Periodo periodo, decimal monto, decimal intereses,
			int periodosQueCubre, bool esInferido)
		{
			ArgumentNullException.ThrowIfNull(rubro);
			ArgumentNullException.ThrowIfNull(periodo);
			if (monto <= 0)
				throw new ArgumentOutOfRangeException(nameof(monto), monto, "Una linea pagada tiene que ser positiva.");
			if (intereses < 0)
				throw new ArgumentOutOfRangeException(nameof(intereses), intereses, "La mora no puede ser negativa.");
			if (periodosQueCubre < 1)
				throw new ArgumentOutOfRangeException(nameof(periodosQueCubre), periodosQueCubre,
					"Una linea cubre al menos un periodo.");
			Rubro = rubro;
			Periodo = periodo;
			Monto = monto;
			Intereses = intereses;
			PeriodosQueCubre = periodosQueCubre;
			EsInferido = esInferido;
		}

		internal decimal Total { get { return Monto + Intereses; } }

		// HOJAS PLANAS para el print (regla 13): un objeto adentro de un carrier se
		// aplana con un accesor. `linea.Periodo.Etiqueta` son dos niveles y no cruzan.
		internal string NombreDelRubro { get { return Rubro.Nombre; } }
		internal string EtiquetaDelPeriodo { get { return Periodo.Etiqueta; } }

		// EL MONTO COMPARABLE entre lineas: lo que costo UN periodo. Es lo que la
		// Variacion compara; comparar los montos crudos de lineas que cubren distinta
		// cantidad de periodos daria saltos falsos.
		internal decimal MontoPorPeriodo { get { return Monto / PeriodosQueCubre; } }

		internal bool EsDe(Rubro rubro)
		{
			return Rubro.Es(rubro);
		}

		// Cubre este periodo? Una linea que llega a P y cubre n periodos cubre los n que
		// terminan en P. Se camina hacia adelante desde el candidato porque Periodo no
		// sabe retroceder, y con eso alcanza.
		internal bool Cubre(Periodo periodo)
		{
			ArgumentNullException.ThrowIfNull(periodo);
			if (periodo.GetType() != Periodo.GetType()) return false;
			Periodo p = periodo;
			for (int paso = 0; paso < PeriodosQueCubre; paso++)
			{
				if (p.EsElMismoQue(Periodo)) return true;
				if (!p.EsAnteriorA(Periodo)) return false;
				p = p.Siguiente();
			}
			return false;
		}
	}
}
