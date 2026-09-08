using System;

namespace Gimnasio.Dominio
{
	// LO QUE EL GIMNASIO DICE QUE VA A COBRAR: una fecha de vencimiento y un monto.
	//
	// Es INTENCION, no hecho: la Proposed Action de Fowler (fig. 8.2). Todavia no paso;
	// el gimnasio anuncia que va a pasar. El Pago es su Implemented Action. Mismo par
	// que ReservaPropuesta / ReservaDeclarada en la municipalidad, con la institucion
	// proponiendo en vez de la app.
	//
	// La app NO cruza cobros contra pagos para deducir un atraso: quien esta en mora lo
	// decide y lo manda el gimnasio (N1). El cobro esta aca porque el gimnasio lo manda
	// y porque "cuando es el proximo cobro" se declaro en la Parte 1 -no porque la app
	// vaya a hacerle una pantalla: eso el gimnasio ya lo contesta-.
	internal sealed class Cobro
	{
		internal DateTime Vence { get; }
		internal decimal Monto { get; }

		internal Cobro(DateTime vence, decimal monto)
		{
			if (monto <= 0)
				throw new ArgumentOutOfRangeException(nameof(monto), monto,
					"Un cobro tiene que ser positivo.");
			Vence = vence.Date;
			Monto = monto;
		}

		// Identidad para la regla 18: el gimnasio no numera sus cobros, asi que el
		// mismo vencimiento es el mismo cobro.
		internal bool EsElMismoQue(DateTime vence)
		{
			return Vence == vence.Date;
		}
	}
}
