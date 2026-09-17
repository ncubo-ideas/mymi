using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// COMO se mira un rendimiento: km/L, mpg o L/100km. Cada presentacion sabe convertir
	/// desde el canonico y sabe su DireccionDeseable, porque cambiar la unidad invierte
	/// la flecha y el signo... pero nunca el veredicto, que ya se juzgo en canonico.
	/// </summary>
	// Es la DireccionDeseable de examenes con mas filo: alla dependia del analito, aca
	// depende de como lo estas mirando (§5.3 del brief). El selector de unidades de la
	// UI cambia cual de estas tres se usa, y nada mas.
	internal sealed class PresentacionDeRendimiento
	{
		internal static readonly PresentacionDeRendimiento KilometrosPorLitro =
			new PresentacionDeRendimiento("km/L", DireccionDeseable.MasEsMejor);

		internal static readonly PresentacionDeRendimiento MillasPorGalonUS =
			new PresentacionDeRendimiento("mpg", DireccionDeseable.MasEsMejor);

		// La inversa: volumen por distancia. Es la que invierte la direccion.
		internal static readonly PresentacionDeRendimiento LitrosPorCienKilometros =
			new PresentacionDeRendimiento("L/100km", DireccionDeseable.MenosEsMejor);

		internal string Simbolo { get; private set; }
		internal DireccionDeseable Direccion { get; private set; }

		private PresentacionDeRendimiento(string simbolo, DireccionDeseable direccion)
		{
			Simbolo = simbolo;
			Direccion = direccion;
		}

		// Kilometros por milla y litros por galon US, para la conversion de mpg.
		private const decimal KilometrosPorMilla = 1.609344m;
		private const decimal LitrosPorGalonUS = 3.785412m;

		internal decimal Convertir(Rendimiento rendimiento)
		{
			ArgumentNullException.ThrowIfNull(rendimiento);
			return ConvertirKmPorLitro(rendimiento.KilometrosPorLitro);
		}

		/// <summary>La misma conversion desde el canonico suelto. Existe porque varias
		/// lecturas ya viajan como decimal en km/L y volver a envolverlas en un Rendimiento
		/// solo para convertirlas seria ceremonia.</summary>
		internal decimal ConvertirKmPorLitro(decimal kmPorLitro)
		{
			if (kmPorLitro == 0m) return 0m;

			if (ReferenceEquals(this, KilometrosPorLitro)) return kmPorLitro;
			if (ReferenceEquals(this, MillasPorGalonUS))
				return kmPorLitro / KilometrosPorMilla * LitrosPorGalonUS;

			// L/100km: la inversa escalada. Un rendimiento de tramo siempre es positivo
			// (lo garantiza Rendimiento.DeTramo), asi que la division es total.
			return 100m / kmPorLitro;
		}

		/// <summary>EL CAMBIO TAL COMO SE VE EN ESTA PRESENTACION, y no es el canonico.
		///
		/// La inversa no es lineal: 12.6 -> 9.4 km/L es un 25% MENOS, y esos mismos dos
		/// tramos en L/100km son un 33% MAS -porque 1/(1-0.25) - 1 = 0.33, no 0.25-, asi
		/// que decir "25% menos" al lado de 7.9 y 10.6 es una contradiccion que la
		/// persona ve sola.
		///
		/// El VEREDICTO no sale de aca -ese se juzgo en canonico y no cambia con la
		/// unidad-: de aca salen la palabra y la magnitud, que son presentacion.</summary>
		internal decimal CambioComoSeVe(Rendimiento desde, Rendimiento hasta)
		{
			ArgumentNullException.ThrowIfNull(desde);
			ArgumentNullException.ThrowIfNull(hasta);

			decimal antes = Convertir(desde);
			return antes == 0m ? 0m : (Convertir(hasta) - antes) / antes;
		}

		/// <summary>RECONOCE sin reventar. Ver UnidadDeDistancia.EsConocida: la elegida se
		/// journaliza y la leen dos binarios distintos.</summary>
		internal static bool EsConocida(string simbolo)
		{
			if (string.IsNullOrWhiteSpace(simbolo)) return false;
			string normalizado = simbolo.Trim();
			return normalizado == KilometrosPorLitro.Simbolo
				|| normalizado == MillasPorGalonUS.Simbolo
				|| normalizado == LitrosPorCienKilometros.Simbolo;
		}

		internal static PresentacionDeRendimiento PorSimbolo(string simbolo)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(simbolo);

			string normalizado = simbolo.Trim();
			if (normalizado == KilometrosPorLitro.Simbolo) return KilometrosPorLitro;
			if (normalizado == MillasPorGalonUS.Simbolo) return MillasPorGalonUS;
			if (normalizado == LitrosPorCienKilometros.Simbolo) return LitrosPorCienKilometros;

			throw new ErrorDeDominio(
				$"'{simbolo}' no es una presentacion de rendimiento conocida. Las validas son: "
				+ $"{KilometrosPorLitro.Simbolo}, {MillasPorGalonUS.Simbolo}, {LitrosPorCienKilometros.Simbolo}.");
		}
	}
}
