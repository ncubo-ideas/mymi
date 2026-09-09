using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// El codigo con el que una <see cref="PressureUnit"/> cruza el wire. Lo que viaja es el
	/// NOMBRE del miembro, asi que el numero no significa nada.
	/// </summary>
	internal enum PressureUnitCode
	{
		/// <summary>Vease <see cref="PressureUnit.UnitA"/>.</summary>
		UnitA,

		/// <summary>Vease <see cref="PressureUnit.UnitB"/>.</summary>
		UnitB,
	}

	/// <summary>
	/// La unidad en que esta expresada una presion. Conjunto cerrado de dos miembros,
	/// comparados por identidad, que NUNCA se inter-convierten: esa ausencia es la invariante,
	/// y un metodo de conversion aca es un defecto, no una comodidad.
	/// </summary>
	internal sealed class PressureUnit
	{
		/// <summary>Una de las dos, que se dice «bar». Incomparable con <see cref="UnitB"/>.</summary>
		internal static readonly PressureUnit UnitA = new PressureUnit("bar");

		/// <summary>La otra, que se dice «psi». Incomparable con <see cref="UnitA"/>.</summary>
		internal static readonly PressureUnit UnitB = new PressureUnit("psi");

		/// <summary>
		/// Como se dice esta unidad en la frase que alguien lee. NO es clave de comparacion: dos
		/// unidades se comparan por identidad.
		/// </summary>
		internal string Name { get; }

		private PressureUnit(string name)
		{
			Name = name;
		}

		/// <summary>
		/// Resuelve el codigo del wire al miembro, una sola vez al entrar. Total con excepcion: un
		/// codigo desconocido es un defecto de quien llama y revienta.
		/// </summary>
		internal static PressureUnit OfCode(PressureUnitCode code)
		{
			switch (code)
			{
				case PressureUnitCode.UnitA: return UnitA;
				case PressureUnitCode.UnitB: return UnitB;
			}

			throw new DomainException(
				$"'{code}' no nombra ninguna unidad de presion. Hay exactamente dos y no se "
				+ "convierten entre si: un codigo desconocido no es una tercera unidad, es un "
				+ "defecto de quien llama.");
		}
	}
}
