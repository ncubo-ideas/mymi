using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El codigo con el que una <see cref="TyrePosition"/> cruza el wire hacia adentro. Lo que
	/// viaja es el NOMBRE del miembro; se resuelve con <see cref="TyrePosition.OfCode"/>.
	/// </summary>
	internal enum TyrePositionCode
	{
		/// <summary>Vease <see cref="TyrePosition.Front"/>.</summary>
		Front,

		/// <summary>Vease <see cref="TyrePosition.Rear"/>.</summary>
		Rear,

		/// <summary>Vease <see cref="TyrePosition.Spare"/>.</summary>
		Spare,
	}

	/// <summary>
	/// Para que posicion del vehiculo vale una especificacion de llanta. Conjunto cerrado de
	/// tres miembros, comparados por identidad: el fabricante publica medida, presion en frio y
	/// clasificaciones por posicion, y las de adelante y atras casi nunca coinciden.
	/// </summary>
	internal sealed class TyrePosition
	{
		/// <summary>Las de adelante.</summary>
		internal static readonly TyrePosition Front = new TyrePosition("Front");

		/// <summary>Las de atras.</summary>
		internal static readonly TyrePosition Rear = new TyrePosition("Rear");

		/// <summary>La de repuesto, que suele llevar su propia presion.</summary>
		internal static readonly TyrePosition Spare = new TyrePosition("Spare");

		/// <summary>
		/// El nombre. Es lo unico que sale del conjunto hacia afuera; el objeto no se entrega.
		/// </summary>
		internal string Name { get; }

		private TyrePosition(string name)
		{
			Name = name;
		}

		/// <summary>
		/// Resuelve el codigo del wire al miembro, una sola vez al entrar. Total con excepcion: un
		/// codigo desconocido es un defecto de quien llama y revienta.
		/// </summary>
		internal static TyrePosition OfCode(TyrePositionCode code)
		{
			switch (code)
			{
				case TyrePositionCode.Front: return Front;
				case TyrePositionCode.Rear: return Rear;
				case TyrePositionCode.Spare: return Spare;
			}

			throw new DomainException(
				$"'{code}' no nombra ninguna posicion de llanta. Hay exactamente tres; una "
				+ "desconocida no es una cuarta posicion, es un defecto de quien llama.");
		}
	}
}
