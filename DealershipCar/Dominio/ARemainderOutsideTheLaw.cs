using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La ley evaluada no mide esa dimension, asi que no hay nada que le quede. Ausente no es
	/// suspendida: lo que no existe no lo cura ningun hecho que llegue despues.
	/// <para>
	/// Compone su propia frase en vez de reusar la de la dimension: la de
	/// <see cref="ADimensionOutsideTheLaw"/> es la cadena VACIA a proposito -- una dimension que la
	/// ley no tiene no se nombra en el motivo -- y una cuenta atras no puede callar una mitad.
	/// </para>
	/// </summary>
	internal sealed class ARemainderOutsideTheLaw : ARemainder
	{
		private readonly string _dimensionName;

		/// <summary>Se llega por <see cref="ARemainder.NotMeasuredByThisLaw"/>.</summary>
		internal ARemainderOutsideTheLaw(string dimensionName)
		{
			_dimensionName = dimensionName;
		}

		/// <summary>No.</summary>
		internal override bool Applies()
		{
			return false;
		}

		/// <summary><see cref="RemainderStanding.OutsideTheLaw"/>.</summary>
		internal override RemainderStanding Standing()
		{
			return RemainderStanding.OutsideTheLaw;
		}

		/// <summary>Dice que la ley no tiene ese limite, con las palabras del dominio.</summary>
		internal override string Describe()
		{
			return _dimensionName + ": esta operación no tiene límite de " + _dimensionName;
		}
	}
}
