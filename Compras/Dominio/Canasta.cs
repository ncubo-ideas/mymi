using System;

namespace Compras.Dominio
{
	/// <summary>
	/// La foto de UN MES de compras, sumada por perfil de consumo: cuanto de lo
	/// comestible fue recomendado, cuanto neutro y cuanto ocasional, con lo no comestible
	/// aparte. Devuelve montos y una proporcion — hechos, no prosa.
	/// </summary>
	public class Canasta
	{
		public int Anio { get; private set; }
		public int Mes { get; private set; }
		public int CantidadDeCompras { get; private set; }

		public decimal GastoRecomendado { get; private set; }
		public decimal GastoNeutro { get; private set; }
		public decimal GastoOcasional { get; private set; }
		public decimal GastoNoComestible { get; private set; }

		public decimal GastoComestible => GastoRecomendado + GastoNeutro + GastoOcasional;

		public decimal GastoTotal => GastoComestible + GastoNoComestible;

		/// <summary>
		/// Sobre lo COMESTIBLE, que es donde significa algo: una olla arrocera carisima no
		/// puede diluir el helado. Total: sin comestibles es 0 y HayComestible lo dice.
		/// </summary>
		public decimal ProporcionOcasional =>
			GastoComestible == 0m ? 0m : Math.Round(GastoOcasional / GastoComestible, 4);

		public bool HayComestible => GastoComestible > 0m;

		public bool HayCompras => CantidadDeCompras > 0;

		internal Canasta(int anio, int mes)
		{
			if (anio < 1900 || anio > 3000)
				throw new ArgumentOutOfRangeException(nameof(anio), anio, "El anio de la canasta no es razonable.");
			if (mes < 1 || mes > 12)
				throw new ArgumentOutOfRangeException(nameof(mes), mes, "El mes tiene que estar entre 1 y 12.");

			Anio = anio;
			Mes = mes;
		}

		internal void Sumar(Compra compra)
		{
			ArgumentNullException.ThrowIfNull(compra);

			foreach (Item item in compra.Items)
			{
				if (!item.Categoria.EsComestible)
				{
					GastoNoComestible = GastoNoComestible + item.Total;
					continue;
				}

				PerfilDeConsumo perfil = item.Categoria.Perfil;
				if (ReferenceEquals(perfil, PerfilDeConsumo.Recomendado)) GastoRecomendado = GastoRecomendado + item.Total;
				else if (ReferenceEquals(perfil, PerfilDeConsumo.Neutro)) GastoNeutro = GastoNeutro + item.Total;
				else GastoOcasional = GastoOcasional + item.Total;
			}

			CantidadDeCompras = CantidadDeCompras + 1;
		}
	}
}
