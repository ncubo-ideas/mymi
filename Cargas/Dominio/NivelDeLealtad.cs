using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// El nivel del programa de lealtad. Cerrado y ordenado. El nivel LLEGA por evento
	/// de la institucion -es su programa y su regla-, nunca se calcula localmente: si la
	/// tasa cambia, el historico no se recalcula, que es lo que significa append-only.
	/// </summary>
	// ⚠️ Los nombres y los umbrales para el siguiente nivel son criterio propio del
	// demo, pendientes de la marca.
	public sealed class NivelDeLealtad
	{
		public static readonly NivelDeLealtad Base = new NivelDeLealtad("base", 0, 200);
		public static readonly NivelDeLealtad Plata = new NivelDeLealtad("plata", 1, 800);
		public static readonly NivelDeLealtad Oro = new NivelDeLealtad("oro", 2, 0);

		public string Nombre { get; private set; }

		private readonly int peso;

		/// <summary>Los puntos a los que la institucion promueve al siguiente. Cero en el
		/// ultimo nivel. ⚠️ Es un dato del programa, no una regla de este modulo: se usa
		/// solo para decirle a Sonia cuanto le falta.</summary>
		public Puntos ParaElSiguiente { get; private set; }

		private NivelDeLealtad(string nombre, int peso, int puntosParaElSiguiente)
		{
			Nombre = nombre;
			this.peso = peso;
			ParaElSiguiente = new Puntos(puntosParaElSiguiente);
		}

		// La query de reconocimiento para la frontera: decide sin reventar.
		public static bool EsConocido(string nombre)
		{
			if (string.IsNullOrWhiteSpace(nombre)) return false;
			string normalizado = nombre.Trim().ToLowerInvariant();
			return normalizado == Base.Nombre || normalizado == Plata.Nombre
				|| normalizado == Oro.Nombre;
		}

		public static NivelDeLealtad PorNombre(string nombre)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);

			string normalizado = nombre.Trim().ToLowerInvariant();
			if (normalizado == Base.Nombre) return Base;
			if (normalizado == Plata.Nombre) return Plata;
			if (normalizado == Oro.Nombre) return Oro;

			throw new ErrorDeDominio(
				$"'{nombre}' no es un nivel de lealtad conocido. Los validos son: "
				+ $"{Base.Nombre}, {Plata.Nombre}, {Oro.Nombre}.");
		}

		public bool EsMasAltoQue(NivelDeLealtad otro)
		{
			ArgumentNullException.ThrowIfNull(otro);
			return peso > otro.peso;
		}

		public bool EsElUltimo => ParaElSiguiente.Cantidad == 0;
	}
}
