using System;

namespace Compras.Dominio
{
	/// <summary>
	/// Cuanto entro de algo en UN mes: unidades, gasto y en cuantas compras. Es el punto
	/// de la serie que hace posible decir CUANDO cambio un consumo, no solo cuanto.
	/// </summary>
	public class MesDeConsumo
	{
		public int Anio { get; private set; }
		public int Mes { get; private set; }
		public int Unidades { get; private set; }
		public decimal Gasto { get; private set; }
		public int Compras { get; private set; }

		internal MesDeConsumo(int anio, int mes)
		{
			if (mes < 1 || mes > 12)
				throw new ArgumentOutOfRangeException(nameof(mes), mes, "El mes tiene que estar entre 1 y 12.");

			Anio = anio;
			Mes = mes;
		}

		/// <summary>Como aaaa-mm, que es la forma en que se le dice a quien pregunta.</summary>
		public string MesExpresado => $"{Anio:0000}-{Mes:00}";

		internal void Sumar(int unidades, decimal gasto)
		{
			Unidades = Unidades + unidades;
			Gasto = Gasto + gasto;
		}

		internal void ContarCompra() => Compras = Compras + 1;
	}
}
