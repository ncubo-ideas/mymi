using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// Puntos del programa de lealtad. NO SON DINERO: tienen una tasa de canje que el
	/// programa puede cambiar, no se suman con montos y no se convierten sin una regla
	/// del programa. Que sumar Puntos y Dinero no compile es el diseno, no una falta.
	/// </summary>
	internal sealed class Puntos
	{
		internal int Cantidad { get; private set; }

		internal Puntos(int cantidad)
		{
			if (cantidad < 0)
				throw new ErrorDeDominio($"Los puntos no pueden ser negativos: {cantidad}.");

			Cantidad = cantidad;
		}

		internal Puntos Mas(Puntos otros)
		{
			ArgumentNullException.ThrowIfNull(otros);
			return new Puntos(Cantidad + otros.Cantidad);
		}

		internal bool AlcanzanPara(Puntos umbral)
		{
			ArgumentNullException.ThrowIfNull(umbral);
			return Cantidad >= umbral.Cantidad;
		}

		// Cuantos faltan para un umbral. Total: si ya se alcanzo, faltan cero.
		internal Puntos LosQueFaltanPara(Puntos umbral)
		{
			ArgumentNullException.ThrowIfNull(umbral);
			return new Puntos(Math.Max(0, umbral.Cantidad - Cantidad));
		}
	}
}
