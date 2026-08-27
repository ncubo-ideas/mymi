using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El codigo con el que una <see cref="DistanceUnit"/> cruza el wire. Lo que viaja es el
	/// NOMBRE del miembro, asi que el numero no significa nada.
	/// </summary>
	internal enum DistanceUnitCode
	{
		/// <summary>Vease <see cref="DistanceUnit.UnitA"/>.</summary>
		UnitA,

		/// <summary>Vease <see cref="DistanceUnit.UnitB"/>.</summary>
		UnitB,
	}

	/// <summary>
	/// La unidad en que esta expresada una distancia. Conjunto cerrado de dos miembros,
	/// comparados por identidad, que NUNCA se inter-convierten: esa ausencia es OINV-21 / ley L7,
	/// y un metodo de conversion aca es un defecto, no una comodidad.
	/// </summary>
	internal sealed class DistanceUnit
	{
		/// <summary>Una de las dos. Abstracta: el dominio no necesita saber cual es.</summary>
		internal static readonly DistanceUnit UnitA = new DistanceUnit("UnitA");

		/// <summary>La otra. Incomparable con <see cref="UnitA"/> por definicion.</summary>
		internal static readonly DistanceUnit UnitB = new DistanceUnit("UnitB");

		/// <summary>
		/// El nombre, para proyectarlo en el motivo y diagnosticar. NO es clave de comparacion: dos
		/// unidades se comparan por identidad.
		/// </summary>
		internal string Name { get; }

		private DistanceUnit(string name)
		{
			Name = name;
		}

		/// <summary>
		/// Resuelve el codigo del wire al miembro, una sola vez al entrar. Total con excepcion: un
		/// codigo desconocido es un defecto de quien llama y revienta (E32).
		/// </summary>
		internal static DistanceUnit OfCode(DistanceUnitCode code)
		{
			switch (code)
			{
				case DistanceUnitCode.UnitA: return UnitA;
				case DistanceUnitCode.UnitB: return UnitB;
			}

			throw new DomainException(
				$"'{code}' no nombra ninguna unidad de distancia. Hay exactamente dos y no se "
				+ "convierten entre si: un codigo desconocido no es una tercera unidad, es un "
				+ "defecto de quien llama.");
		}
	}
}
