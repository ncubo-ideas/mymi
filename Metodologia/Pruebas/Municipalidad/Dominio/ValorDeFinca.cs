using System;

namespace Municipalidad.Dominio
{
	// UN VALOR DE LA FINCA EN UNA FECHA. Hecho: la persona lo declaro, o la municipalidad
	// la avaluo -y cual de las dos es el hueco 10, que el v1 no distingue-.
	//
	// Es la base imponible de los rubros que responden al avaluo. Cuando cambia, el IBI
	// salta, y eso es lo que CambioDeAvaluo explica.
	internal sealed class ValorDeFinca
	{
		internal DateTime Fecha { get; }
		internal decimal Monto { get; }

		internal ValorDeFinca(DateTime fecha, decimal monto)
		{
			if (monto <= 0)
				throw new ArgumentOutOfRangeException(nameof(monto), monto,
					"El valor de una finca tiene que ser positivo.");
			Fecha = fecha.Date;
			Monto = monto;
		}
	}
}
