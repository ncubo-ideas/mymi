using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// Lo que de verdad le importa a la billetera: cuanto cuesta cada kilometro, por
	/// moneda. Sale de la misma ley del tramo aplicada al dinero: lo gastado DESPUES de
	/// la primera lectura valida, entre los kilometros recorridos desde ella -lo pagado
	/// en la primera ancla movio los kilometros del periodo anterior-.
	/// </summary>
	internal sealed class CostoPorDistanciaEnMoneda
	{
		internal Moneda Moneda { get; private set; }
		internal decimal MontoPorKilometro { get; private set; }
		internal decimal Kilometros { get; private set; }

		internal CostoPorDistanciaEnMoneda(Moneda moneda, decimal montoPorKilometro,
			decimal kilometros)
		{
			ArgumentNullException.ThrowIfNull(moneda);

			Moneda = moneda;
			MontoPorKilometro = montoPorKilometro;
			Kilometros = kilometros;
		}

		internal string CodigoDeMoneda => Moneda.Codigo;
		internal string Expresado =>
			$"{Moneda.Simbolo}{MontoPorKilometro:0.##} por km, sobre {Kilometros:0} km";
	}
}
