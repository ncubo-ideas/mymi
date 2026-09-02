using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// La unidad en la que el surtidor despacho el combustible. Es el "que" de "40 que":
	/// sin ella un 40 no significa nada, y EL GALON NO ES UNO -el estadounidense son
	/// ~3.785 L y el imperial ~4.546 L, un 20% de diferencia-. Un modelo con un decimal
	/// y la etiqueta "gal" produce un error del 20% sin avisar.
	/// </summary>
	// Conjunto CERRADO comparado por referencia, igual que UnidadDeMedida en examenes.
	// PorSimbolo es la frontera: el simbolo llega como texto en el evento y se resuelve
	// UNA vez. "gal" pelado se rechaza a proposito, porque no dice cual de los dos es.
	public sealed class UnidadDeVolumen
	{
		public static readonly UnidadDeVolumen Litro = new UnidadDeVolumen("L", 1m);
		public static readonly UnidadDeVolumen GalonUS = new UnidadDeVolumen("galUS", 3.785412m);
		public static readonly UnidadDeVolumen GalonImperial = new UnidadDeVolumen("galImp", 4.546090m);

		public string Simbolo { get; private set; }

		private readonly decimal litrosPorUnidad;

		private UnidadDeVolumen(string simbolo, decimal litrosPorUnidad)
		{
			Simbolo = simbolo;
			this.litrosPorUnidad = litrosPorUnidad;
		}

		// La conversion a litros es FISICA, no monetaria: un galon US son 3.785412 litros
		// hoy, ayer y en cualquier pais. Por eso existe, a diferencia de Moneda que no
		// convierte nada.
		public decimal EnLitros(decimal valor) => valor * litrosPorUnidad;

		// La query de reconocimiento para la frontera: decide sin reventar. Ojo: "gal"
		// pelado NO es conocido, a proposito.
		public static bool EsConocida(string simbolo)
		{
			if (string.IsNullOrWhiteSpace(simbolo)) return false;
			string normalizado = simbolo.Trim();
			return normalizado == Litro.Simbolo || normalizado == GalonUS.Simbolo
				|| normalizado == GalonImperial.Simbolo;
		}

		public static UnidadDeVolumen PorSimbolo(string simbolo)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(simbolo);

			string normalizado = simbolo.Trim();
			if (normalizado == Litro.Simbolo) return Litro;
			if (normalizado == GalonUS.Simbolo) return GalonUS;
			if (normalizado == GalonImperial.Simbolo) return GalonImperial;

			// "gal" pelado tiene su propio mensaje porque es el error mas probable y el
			// mas caro: cual galon es decide un 20% del rendimiento.
			if (string.Equals(normalizado, "gal", StringComparison.OrdinalIgnoreCase))
				throw new ErrorDeDominio(
					"'gal' no dice cual galon es, y el estadounidense y el imperial difieren un 20%. "
					+ $"El emisor debe decir '{GalonUS.Simbolo}' o '{GalonImperial.Simbolo}'.");

			throw new ErrorDeDominio(
				$"'{simbolo}' no es una unidad de volumen conocida. Las validas son: "
				+ $"{Litro.Simbolo}, {GalonUS.Simbolo}, {GalonImperial.Simbolo}.");
		}

		public bool EsLaMisma(UnidadDeVolumen otra)
		{
			ArgumentNullException.ThrowIfNull(otra);
			return ReferenceEquals(this, otra);
		}
	}
}
