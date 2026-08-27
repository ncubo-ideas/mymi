using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La dimension que la ley evaluada no tiene: el tiempo de una DistanceOnly, la distancia de
	/// una TimeOnly, las dos de una ConditionOnly. Ausente no es suspendida.
	/// </summary>
	internal sealed class ADimensionOutsideTheLaw : LimitStanding
	{
		/// <summary>Se llega por las dos fabricas <c>...IsNotPartOfThisLaw</c>.</summary>
		internal ADimensionOutsideTheLaw()
		{
		}

		/// <summary>No.</summary>
		internal override bool Applies
		{
			get { return false; }
		}

		/// <summary>Nada: una dimension que la ley no tiene no se nombra en el motivo.</summary>
		internal override string Describe()
		{
			return "";
		}
	}
}
