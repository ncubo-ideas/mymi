using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// El gasto de un periodo EN UNA MONEDA. Toda agregacion de dinero de este dominio
	/// agrupa por moneda y devuelve una fila por cada una: sumar cruzado exigiria un
	/// tipo de cambio con fecha, que es presentacion, no dominio (ver Moneda).
	/// </summary>
	public sealed class GastoEnMoneda
	{
		public Moneda Moneda { get; private set; }
		public decimal Monto { get; private set; }
		public int CantidadDeCargas { get; private set; }
		public decimal Litros { get; private set; }

		internal GastoEnMoneda(Moneda moneda, decimal monto, int cantidadDeCargas, decimal litros)
		{
			ArgumentNullException.ThrowIfNull(moneda);

			Moneda = moneda;
			Monto = monto;
			CantidadDeCargas = cantidadDeCargas;
			Litros = litros;
		}

		public string CodigoDeMoneda => Moneda.Codigo;
		public string Expresado => $"{Moneda.Simbolo}{Monto:0.##} en {CantidadDeCargas} cargas";
	}
}
