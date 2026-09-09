using System;

namespace Municipalidad.Dominio
{
	/// <summary>
	/// Lo que la persona decidio apartar por mes: una INTENCION suya, con la fecha en que la adopto. Es
	/// la mitad de esta cuenta que la municipalidad no manda, asi que borrar la carpeta la pierde.
	/// </summary>
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
