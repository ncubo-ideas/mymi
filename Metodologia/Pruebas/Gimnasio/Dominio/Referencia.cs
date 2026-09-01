using System;

namespace Gimnasio.Dominio
{
	// LOS UMBRALES, QUE ENTRAN Y NO VIVEN ACA.
	//
	// El Preguntas.md lo deja escrito y es la linea que sostiene todo el diseno:
	// "la IA da los valores; el dominio se queda con la forma y hace la comparacion".
	//
	// Si la IA hiciera la comparacion, el juicio no quedaria en el journal y la app
	// contestaria distinto manana sobre los mismos datos. Aca los valores son un
	// PARAMETRO -journalizado, replayable- y la comparacion es del dominio.
	//
	// Por eso ninguna constante numerica de las tablas del juicio 2 y 4 aparece en
	// este archivo ni en ningun otro del dominio.
	internal sealed class Referencia
	{
		internal decimal BajoHasta { get; }
		internal decimal AltoDesde { get; }
		internal decimal Ruido { get; }
		internal decimal CambioNoCreible { get; }

		internal Referencia(decimal bajoHasta, decimal altoDesde, decimal ruido, decimal cambioNoCreible)
		{
			if (altoDesde < bajoHasta)
				throw new ArgumentException(
					$"El rango esta invertido: bajo hasta {bajoHasta} y alto desde {altoDesde}.",
					nameof(altoDesde));
			if (ruido < 0)
				throw new ArgumentOutOfRangeException(nameof(ruido), ruido, "El ruido no puede ser negativo.");
			if (cambioNoCreible <= ruido)
				throw new ArgumentException(
					$"Un cambio no creible ({cambioNoCreible}) tiene que ser mayor que el ruido ({ruido}); "
					+ "si no, todo cambio con noticia seria a la vez increible.",
					nameof(cambioNoCreible));

			BajoHasta = bajoHasta;
			AltoDesde = altoDesde;
			Ruido = ruido;
			CambioNoCreible = cambioNoCreible;
		}

		// PREDICADOS TOTALES: contestan para cualquier entrada, sin reventar. Es lo que
		// permite preguntarles desde un Check antes de escribir.
		internal bool EsBajo(decimal valor) { return valor < BajoHasta; }
		internal bool EsAlto(decimal valor) { return valor > AltoDesde; }
		internal bool EsRuido(decimal cambio) { return Math.Abs(cambio) < Ruido; }
		internal bool NoEsCreible(decimal cambio) { return Math.Abs(cambio) > CambioNoCreible; }
	}
}
