using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// Distancia por unidad de volumen en un tramo. Es la magnitud DERIVADA del dominio:
	/// no llega en ningun evento, solo se puede construir desde un tramo (distancia y
	/// volumen consumido) o declararla el fabricante.
	/// </summary>
	// CANONICO EN KM/L. El veredicto de una serie se calcula UNA sola vez en este
	// canonico, donde mas siempre es mejor; la unidad en que Sonia lo mira (mpg,
	// L/100km) es presentacion y vive en PresentacionDeRendimiento. Si el veredicto se
	// calculara en la unidad de presentacion, cambiar a L/100km invertiria "mejoro" y
	// "empeoro" (§5.3 del brief).
	internal sealed class Rendimiento
	{
		internal decimal KilometrosPorLitro { get; private set; }

		private Rendimiento(decimal kilometrosPorLitro)
		{
			KilometrosPorLitro = kilometrosPorLitro;
		}

		/// <summary>La unica construccion desde datos medidos: lo que se recorrio entre
		/// dos tanques llenos, dividido por lo que se consumio en el medio.</summary>
		internal static Rendimiento DeTramo(Distancia recorrida, Volumen consumido)
		{
			ArgumentNullException.ThrowIfNull(recorrida);
			ArgumentNullException.ThrowIfNull(consumido);
			if (recorrida.EnKilometros <= 0m)
				throw new ErrorDeDominio(
					"Un tramo sin distancia recorrida no tiene rendimiento que calcular.");

			return new Rendimiento(recorrida.EnKilometros / consumido.EnLitros);
		}

		/// <summary>Lo que declara el fabricante en el manual. Entra como dato, no como
		/// medicion: es la otra mitad del cruce con el concesionario.</summary>
		internal static Rendimiento DeFabrica(decimal kilometrosPorLitro)
		{
			if (kilometrosPorLitro <= 0m)
				throw new ErrorDeDominio(
					$"Un rendimiento de fabrica debe ser positivo, no {kilometrosPorLitro}.");

			return new Rendimiento(kilometrosPorLitro);
		}

		// Para reconstruir una magnitud ya derivada de mediciones (la mediana de una
		// serie de tramos). No es una puerta de entrada de datos: por eso es interna.
		internal static Rendimiento EnKmPorLitro(decimal kilometrosPorLitro)
		{
			if (kilometrosPorLitro <= 0m)
				throw new ErrorDeDominio(
					$"Un rendimiento debe ser positivo, no {kilometrosPorLitro}.");

			return new Rendimiento(kilometrosPorLitro);
		}

		// El cambio proporcional contra otro rendimiento, en canonico. Positivo = este
		// rinde mas. Es el numero sobre el que se juzga Mejoro/Empeoro/Estable.
		internal decimal CambioProporcionalDesde(Rendimiento referencia)
		{
			ArgumentNullException.ThrowIfNull(referencia);
			return (KilometrosPorLitro - referencia.KilometrosPorLitro)
				/ referencia.KilometrosPorLitro;
		}

		internal bool SuperaPorMultiploA(Rendimiento referencia, decimal multiplo)
		{
			ArgumentNullException.ThrowIfNull(referencia);
			return KilometrosPorLitro > referencia.KilometrosPorLitro * multiplo;
		}

	}
}
