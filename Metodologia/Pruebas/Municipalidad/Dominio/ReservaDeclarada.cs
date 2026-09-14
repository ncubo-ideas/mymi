using System;

namespace Municipalidad.Dominio
{
	// LO QUE LA PERSONA DECIDIO APARTAR POR MES. Es INTENCION, y es de ella: la app le
	// propuso un numero, ella lo ajusto o no, y lo adopto. Un comando con fecha, que
	// entra al journal como hecho de la persona.
	//
	// Por eso este dominio es hibrido y el journal no es enteramente reconstruible:
	// esto no lo manda la municipalidad, y borrar la carpeta lo pierde.
	internal sealed class ReservaDeclarada
	{
		internal decimal MontoMensual { get; }
		internal DateTime Fecha { get; }

		internal ReservaDeclarada(decimal montoMensual, DateTime fecha)
		{
			if (montoMensual <= 0)
				throw new ArgumentOutOfRangeException(nameof(montoMensual), montoMensual,
					"Una reserva tiene que ser positiva.");
			MontoMensual = montoMensual;
			Fecha = fecha.Date;
		}
	}
}
