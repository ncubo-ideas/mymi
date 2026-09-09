using System;

namespace Municipalidad.Dominio
{
	/// <summary>
	/// Un valor de la finca en una fecha, sin distinguir si lo fijo un avaluo de la municipalidad o una
	/// declaracion de la persona. Es la base imponible de los rubros que se cobran sobre ella, asi que
	/// un valor nuevo es lo que explica un salto en esos rubros.
	/// </summary>
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
