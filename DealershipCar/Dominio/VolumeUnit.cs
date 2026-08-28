using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El codigo con el que una <see cref="VolumeUnit"/> cruza el wire. Lo que viaja es el
	/// NOMBRE del miembro, asi que el numero no significa nada.
	/// </summary>
	internal enum VolumeUnitCode
	{
		/// <summary>Vease <see cref="VolumeUnit.UnitA"/>.</summary>
		UnitA,

		/// <summary>Vease <see cref="VolumeUnit.UnitB"/>.</summary>
		UnitB,
	}

	/// <summary>
	/// La unidad en que esta expresada una capacidad. Conjunto cerrado de dos miembros,
	/// comparados por identidad, que NUNCA se inter-convierten: esa ausencia es la invariante,
	/// y un metodo de conversion aca es un defecto, no una comodidad.
	/// </summary>
	internal sealed class VolumeUnit
	{
		/// <summary>Una de las dos, que se dice «L». Incomparable con <see cref="UnitB"/>.</summary>
		internal static readonly VolumeUnit UnitA = new VolumeUnit("L");

		/// <summary>La otra, que se dice «gal». Incomparable con <see cref="UnitA"/>.</summary>
		internal static readonly VolumeUnit UnitB = new VolumeUnit("gal");

		/// <summary>
		/// Como se dice esta unidad en la frase que alguien lee. NO es clave de comparacion: dos
		/// unidades se comparan por identidad.
		/// </summary>
		internal string Name { get; }

		private VolumeUnit(string name)
		{
			Name = name;
		}

		/// <summary>
		/// Resuelve el codigo del wire al miembro, una sola vez al entrar. Total con excepcion: un
		/// codigo desconocido es un defecto de quien llama y revienta.
		/// </summary>
		internal static VolumeUnit OfCode(VolumeUnitCode code)
		{
			switch (code)
			{
				case VolumeUnitCode.UnitA: return UnitA;
				case VolumeUnitCode.UnitB: return UnitB;
			}

			throw new DomainException(
				$"'{code}' no nombra ninguna unidad de volumen. Hay exactamente dos y no se "
				+ "convierten entre si: un codigo desconocido no es una tercera unidad, es un "
				+ "defecto de quien llama.");
		}
	}
}
