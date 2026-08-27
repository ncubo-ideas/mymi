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
	public sealed class PresentacionDeRendimiento
	{
		public static readonly PresentacionDeRendimiento KilometrosPorLitro =
			new PresentacionDeRendimiento("km/L", DireccionDeseable.MasEsMejor);

		public static readonly PresentacionDeRendimiento MillasPorGalonUS =
			new PresentacionDeRendimiento("mpg", DireccionDeseable.MasEsMejor);

		// La inversa: volumen por distancia. Es la que invierte la direccion.
		public static readonly PresentacionDeRendimiento LitrosPorCienKilometros =
			new PresentacionDeRendimiento("L/100km", DireccionDeseable.MenosEsMejor);

		public string Simbolo { get; private set; }
		public DireccionDeseable Direccion { get; private set; }

		private PresentacionDeRendimiento(string simbolo, DireccionDeseable direccion)
		{
			Simbolo = simbolo;
			Direccion = direccion;
		}

		// Kilometros por milla y litros por galon US, para la conversion de mpg.
		private const decimal KilometrosPorMilla = 1.609344m;
		private const decimal LitrosPorGalonUS = 3.785412m;

		public decimal Convertir(Rendimiento rendimiento)
		{
			ArgumentNullException.ThrowIfNull(rendimiento);

			decimal kmPorLitro = rendimiento.KilometrosPorLitro;
			if (ReferenceEquals(this, KilometrosPorLitro)) return kmPorLitro;
			if (ReferenceEquals(this, MillasPorGalonUS))
				return kmPorLitro / KilometrosPorMilla * LitrosPorGalonUS;

			// L/100km: la inversa escalada. Un rendimiento de tramo siempre es positivo
			// (lo garantiza Rendimiento.DeTramo), asi que la division es total.
			return 100m / kmPorLitro;
		}

		public string Formatear(Rendimiento rendimiento) =>
			$"{Convertir(rendimiento):0.#} {Simbolo}";

		public static PresentacionDeRendimiento PorSimbolo(string simbolo)
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
