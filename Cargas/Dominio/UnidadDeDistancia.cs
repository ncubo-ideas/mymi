using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// La unidad en la que se lee el odometro. Cerrada y por referencia, como toda unidad
	/// de este dominio: un tablero marca kilometros o millas, no "un numero".
	/// </summary>
	public sealed class UnidadDeDistancia
	{
		public static readonly UnidadDeDistancia Kilometro = new UnidadDeDistancia("km", 1m);
		public static readonly UnidadDeDistancia Milla = new UnidadDeDistancia("mi", 1.609344m);

		public string Simbolo { get; private set; }

		private readonly decimal kilometrosPorUnidad;

		private UnidadDeDistancia(string simbolo, decimal kilometrosPorUnidad)
		{
			Simbolo = simbolo;
			this.kilometrosPorUnidad = kilometrosPorUnidad;
		}

		public decimal EnKilometros(decimal valor) => valor * kilometrosPorUnidad;

		public static UnidadDeDistancia PorSimbolo(string simbolo)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(simbolo);

			string normalizado = simbolo.Trim();
			if (normalizado == Kilometro.Simbolo) return Kilometro;
			if (normalizado == Milla.Simbolo) return Milla;

			throw new ErrorDeDominio(
				$"'{simbolo}' no es una unidad de distancia conocida. Las validas son: "
				+ $"{Kilometro.Simbolo}, {Milla.Simbolo}.");
		}

		public bool EsLaMisma(UnidadDeDistancia otra)
		{
			ArgumentNullException.ThrowIfNull(otra);
			return ReferenceEquals(this, otra);
		}
	}
}
