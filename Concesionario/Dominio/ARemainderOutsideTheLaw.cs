using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La ley evaluada no mide esa dimension, asi que no hay nada que le quede. Ausente no es
	/// suspendida: lo que no existe no lo cura ningun hecho que llegue despues.
	/// <para>
	/// Nombra su dimension aunque <see cref="ADimensionOutsideTheLaw"/> no aporte clausula al
	/// motivo: una cuenta atras no puede callar una de sus dos mitades.
	/// </para>
	/// </summary>
	internal sealed class ARemainderOutsideTheLaw : ARemainder
	{
		private readonly string _dimension;

		/// <summary>Se llega por <see cref="ARemainder.NotMeasuredByThisLaw"/>.</summary>
		internal ARemainderOutsideTheLaw(string dimension)
		{
			_dimension = dimension;
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

		/// <summary>La dimension que la ley no mide, por su codigo.</summary>
		internal override string TheDimensionItMeasures()
		{
			return _dimension;
		}
	}
}
