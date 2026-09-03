using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// La unidad en la que se lee el odometro. Cerrada y por referencia, como toda unidad
	/// de este dominio: un tablero marca kilometros o millas, no "un numero".
	/// </summary>
	internal sealed class UnidadDeDistancia
	{
		internal static readonly UnidadDeDistancia Kilometro = new UnidadDeDistancia("km", 1m);
		internal static readonly UnidadDeDistancia Milla = new UnidadDeDistancia("mi", 1.609344m);

		internal string Simbolo { get; private set; }

		private readonly decimal kilometrosPorUnidad;

		private UnidadDeDistancia(string simbolo, decimal kilometrosPorUnidad)
		{
			Simbolo = simbolo;
			this.kilometrosPorUnidad = kilometrosPorUnidad;
		}

		internal decimal EnKilometros(decimal valor) => valor * kilometrosPorUnidad;

		/// <summary>De canonico a esta unidad: la vuelta de EnKilometros, para MOSTRAR.</summary>
		internal decimal DesdeKilometros(decimal kilometros) => kilometros / kilometrosPorUnidad;

		/// <summary>RECONOCE sin reventar, para la frontera. Hace falta porque la unidad
		/// elegida se journaliza y la leen DOS binarios: si una WASM nueva guarda un
		/// simbolo que un MCP viejo no conoce, PorSimbolo le revienta la hidratacion
		/// entera y deja de contestar CUALQUIER pregunta, no solo esta.</summary>
		internal static bool EsConocida(string simbolo)
		{
			if (string.IsNullOrWhiteSpace(simbolo)) return false;
			string normalizado = simbolo.Trim();
			return normalizado == Kilometro.Simbolo || normalizado == Milla.Simbolo;
		}

		internal static UnidadDeDistancia PorSimbolo(string simbolo)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(simbolo);

			string normalizado = simbolo.Trim();
			if (normalizado == Kilometro.Simbolo) return Kilometro;
			if (normalizado == Milla.Simbolo) return Milla;

			throw new ErrorDeDominio(
				$"'{simbolo}' no es una unidad de distancia conocida. Las validas son: "
				+ $"{Kilometro.Simbolo}, {Milla.Simbolo}.");
		}

		internal bool EsLaMisma(UnidadDeDistancia otra)
		{
			ArgumentNullException.ThrowIfNull(otra);
			return ReferenceEquals(this, otra);
		}
	}
}
