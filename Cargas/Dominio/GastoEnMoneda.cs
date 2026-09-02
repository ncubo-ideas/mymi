using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// El gasto de un periodo EN UNA MONEDA. Toda agregacion de dinero de este dominio
	/// agrupa por moneda y devuelve una fila por cada una: sumar cruzado exigiria un
	/// tipo de cambio con fecha, que es presentacion, no dominio (ver Moneda).
	/// </summary>
	internal sealed class GastoEnMoneda
	{
		internal Moneda Moneda { get; private set; }
		internal decimal Monto { get; private set; }
		internal int CantidadDeCargas { get; private set; }
		internal decimal Litros { get; private set; }

		internal GastoEnMoneda(Moneda moneda, decimal monto, int cantidadDeCargas, decimal litros)
		{
			ArgumentNullException.ThrowIfNull(moneda);

			Moneda = moneda;
			Monto = monto;
			CantidadDeCargas = cantidadDeCargas;
			Litros = litros;
		}

		internal string CodigoDeMoneda => Moneda.Codigo;
		internal string Expresado => $"{Moneda.Simbolo}{Monto:0.##} en {CantidadDeCargas} cargas";
	}
}
