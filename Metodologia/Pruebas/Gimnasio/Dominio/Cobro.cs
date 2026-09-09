using System;

namespace Gimnasio.Dominio
{
	// LO QUE EL GIMNASIO PUSO AL COBRO. Desde el v3, tal como el papel B lo trae: una
	// etiqueta -"Agosto"-, un monto, cuando vence -el 10/09-, QUE PERIODO CUBRE -del
	// 24/08 al 23/09- y su estado -"Abierto"-. El v2 solo tenia vencimiento y monto,
	// porque salio de una entrevista sin papel.
	//
	// Es INTENCION, no hecho: la Proposed Action de Fowler (fig. 8.2). El Pago es su
	// Implemented Action. Y el periodo es lo que le da sentido al cobro: la cuota de
	// "Agosto" cubre un tramo que empieza el 24 de agosto y VENCE ADENTRO de ese tramo,
	// no antes. La etiqueta es el mes en que el periodo empieza.
	//
	// La app NO cruza cobros contra pagos ni contra vencimientos para deducir mora: quien
	// esta en mora lo decide y lo manda el gimnasio (N1). Un cobro abierto y vencido es
	// un cobro abierto y vencido, y nada mas.
	//
	// Identidad para la regla 18: el gimnasio no numera sus cobros; el mismo vencimiento
	// es el mismo cobro -y el papel lo confirma: cada cobro vence un dia distinto-.
	internal sealed class Cobro
	{
		internal DateTime Vence { get; }
		internal decimal Monto { get; }
		internal string Etiqueta { get; }
		internal DateTime CubreDesde { get; }
		internal DateTime CubreHasta { get; }
		internal EstadoDeCobro Estado { get; private set; }

		internal Cobro(DateTime vence, decimal monto, string etiqueta, DateTime cubreDesde, DateTime cubreHasta,
			EstadoDeCobro estado)
		{
			if (monto <= 0)
				throw new ArgumentOutOfRangeException(nameof(monto), monto,
					"Un cobro tiene que ser positivo.");
			ArgumentException.ThrowIfNullOrWhiteSpace(etiqueta);
			ArgumentNullException.ThrowIfNull(estado);
			if (cubreHasta.Date < cubreDesde.Date)
				throw new ArgumentOutOfRangeException(nameof(cubreHasta), cubreHasta,
					"El periodo que cubre el cobro esta invertido.");

			Vence = vence.Date;
			Monto = monto;
			Etiqueta = etiqueta.Trim();
			CubreDesde = cubreDesde.Date;
			CubreHasta = cubreHasta.Date;
			Estado = estado;
		}

		internal bool EsElMismoQue(DateTime vence)
		{
			return Vence == vence.Date;
		}

		// LA TRANSICION DE LA FASE: de abierto a pagado, cuando el gimnasio lo diga. El
		// mismo estado otra vez es el mismo hecho otra vez (regla 18), y se rechaza.
		internal bool PuedeCambiarA(EstadoDeCobro estado)
		{
			return estado != null && !Estado.Es(estado);
		}

		internal void CambiarEstado(EstadoDeCobro estado)
		{
			ArgumentNullException.ThrowIfNull(estado);
			if (!PuedeCambiarA(estado))
				throw new InvalidOperationException(
					$"El cobro '{Etiqueta}' ya esta {Estado.Etiqueta}. Preguntar PuedeCambiarA antes.");
			Estado = estado;
		}

		// PREDICADO TOTAL: contesta para cualquier dia. Es lo que N10 pregunta -que cobro
		// cubre el dia que entrene- y el gimnasio no dice.
		internal bool Cubre(DateTime dia)
		{
			DateTime d = dia.Date;
			return d >= CubreDesde && d <= CubreHasta;
		}

		internal int DiasQueCubre { get { return (CubreHasta - CubreDesde).Days + 1; } }

		// DOS COBROS SOBRE EL MISMO DIA SON UN ERROR -Jorge, 2026-09-08, hueco 22-. Es el
		// predicado que PuedeAnunciarCobro usa para rechazar el segundo.
		internal bool SeSolapaCon(DateTime desde, DateTime hasta)
		{
			return desde.Date <= CubreHasta && hasta.Date >= CubreDesde;
		}

		// El vencimiento cae adentro del periodo que cubre, como en el papel. Se expone
		// como hoja porque es lo que sorprende al leerlo: se paga despues de empezar.
		internal bool VenceDentroDelPeriodo { get { return Cubre(Vence); } }
	}
}
