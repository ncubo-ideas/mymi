using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// Un monto con su moneda. La aritmetica exige la MISMA moneda: sumar colones con
	/// dolares revienta, porque el dominio no tiene tipo de cambio (ver Moneda). Toda
	/// agregacion de dinero de este dominio agrupa por moneda, nunca suma cruzado.
	/// </summary>
	public sealed class Dinero
	{
		public decimal Monto { get; private set; }
		public Moneda Moneda { get; private set; }

		public Dinero(decimal monto, Moneda moneda)
		{
			ArgumentNullException.ThrowIfNull(moneda);
			if (monto < 0m)
				throw new ErrorDeDominio($"Un monto no puede ser negativo: {monto}.");

			Monto = monto;
			Moneda = moneda;
		}

		public Dinero Mas(Dinero otro)
		{
			ArgumentNullException.ThrowIfNull(otro);
			if (!Moneda.EsLaMisma(otro.Moneda))
				throw new ErrorDeDominio(
					$"No se puede sumar {Expresado} con {otro.Expresado}: el dinero no se "
					+ "convierte, se registra. Agrupar por moneda es del que consulta.");

			return new Dinero(Monto + otro.Monto, Moneda);
		}

		// La diferencia absoluta, para la tolerancia de redondeo del total (ver Carga).
		public decimal DiferenciaAbsolutaCon(Dinero otro)
		{
			ArgumentNullException.ThrowIfNull(otro);
			if (!Moneda.EsLaMisma(otro.Moneda))
				throw new ErrorDeDominio(
					$"No se puede comparar {Expresado} con {otro.Expresado}: son monedas distintas.");

			return Math.Abs(Monto - otro.Monto);
		}

		public string Expresado => $"{Moneda.Simbolo}{Monto:0.##}";
	}
}
