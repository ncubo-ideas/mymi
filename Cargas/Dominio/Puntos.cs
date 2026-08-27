using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// Puntos del programa de lealtad. NO SON DINERO: tienen una tasa de canje que el
	/// programa puede cambiar, no se suman con montos y no se convierten sin una regla
	/// del programa. Que sumar Puntos y Dinero no compile es el diseno, no una falta.
	/// </summary>
	public sealed class Puntos
	{
		public int Cantidad { get; private set; }

		public Puntos(int cantidad)
		{
			if (cantidad < 0)
				throw new ErrorDeDominio($"Los puntos no pueden ser negativos: {cantidad}.");

			Cantidad = cantidad;
		}

		public Puntos Mas(Puntos otros)
		{
			ArgumentNullException.ThrowIfNull(otros);
			return new Puntos(Cantidad + otros.Cantidad);
		}

		public bool AlcanzanPara(Puntos umbral)
		{
			ArgumentNullException.ThrowIfNull(umbral);
			return Cantidad >= umbral.Cantidad;
		}

		// Cuantos faltan para un umbral. Total: si ya se alcanzo, faltan cero.
		public Puntos LosQueFaltanPara(Puntos umbral)
		{
			ArgumentNullException.ThrowIfNull(umbral);
			return new Puntos(Math.Max(0, umbral.Cantidad - Cantidad));
		}
	}
}
