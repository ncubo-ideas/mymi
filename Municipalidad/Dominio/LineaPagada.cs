using System;

namespace Municipalidad.Dominio
{
	/// <summary>
	/// Una linea de un comprobante: un rubro, hasta que periodo cubre, cuanto, y la mora.
	/// <para>
	/// LA MORA SE RECIBE, NO SE CALCULA. Intereses es lo que la municipalidad cobro, tal cual vino en el
	/// recibo: la evaluacion de la institucion llega como hecho, y la app no la recalcula ni la discute.
	/// </para>
	/// <para>
	/// CUANTOS PERIODOS CUBRE ES INFERIDO. El recibo dice hasta que periodo llega la linea y cuanto,
	/// pero no cuantos periodos son; la cuenta lo deduce de la distancia al ultimo periodo pagado de ese
	/// rubro, y por eso PeriodosQueCubre lo pone ella y no el recibo. Sin historia previa se asume uno, y
	/// EsInferido lo dice.
	/// </para>
	/// </summary>
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

		/// <summary>
		/// El nombre del rubro, aplanado: el print proyecta un solo nivel, asi que un objeto adentro de
		/// un carrier se alcanza por un accesor y no por dos saltos.
		/// </summary>
		internal string NombreDelRubro { get { return Rubro.Nombre; } }

		/// <summary>El periodo, aplanado por la misma razon.</summary>
		internal string EtiquetaDelPeriodo { get { return Periodo.Etiqueta; } }

		/// <summary>
		/// El monto comparable entre lineas: lo que costo UN periodo. Es lo que la variacion compara;
		/// comparar los montos crudos de lineas que cubren distinta cantidad de periodos daria saltos
		/// falsos.
		/// </summary>
		internal decimal MontoPorPeriodo { get { return Monto / PeriodosQueCubre; } }

		internal bool EsDe(Rubro rubro)
		{
			return Rubro.Es(rubro);
		}

		/// <summary>
		/// Si esta linea cubre ese periodo. Una linea que llega a un periodo y cubre n cubre los n que
		/// terminan en el, asi que se camina hacia adelante desde el candidato.
		/// </summary>
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
