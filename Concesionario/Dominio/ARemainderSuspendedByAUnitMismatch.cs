using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// El limite y las mediciones estan en unidades distintas, asi que no hay resta que hacer. No
	/// se convierte: se deja de medir y se nombran las dos, cual contra cual.
	/// </summary>
	internal sealed class ARemainderSuspendedByAUnitMismatch : ARemainder
	{
		private readonly string _limitUnit;
		private readonly string _measuredUnit;

		/// <summary>
		/// Se llega por <see cref="ARemainder.UnitsThatDoNotMatch"/>, con la unidad del limite y la
		/// que no coincidio, sea la del ancla o la de la lectura.
		/// </summary>
		internal ARemainderSuspendedByAUnitMismatch(string limitUnit, string measuredUnit)
		{
			_limitUnit = limitUnit;
			_measuredUnit = measuredUnit;
		}

		/// <summary>Si: la ley mide la dimension; lo que no se puede es comparar.</summary>
		internal override bool Applies()
		{
			return true;
		}

		/// <summary>Si.</summary>
		internal override bool SuspendedByAUnitMismatch()
		{
			return true;
		}

		/// <summary><see cref="RemainderStanding.SuspendedByAUnitMismatch"/>.</summary>
		internal override RemainderStanding Standing()
		{
			return RemainderStanding.SuspendedByAUnitMismatch;
		}

		/// <summary>La distancia, que es la unica dimension con mas de una unidad.</summary>
		internal override string TheDimensionItMeasures()
		{
			return IntervalDimension.Distance;
		}

		/// <summary>La unidad contra la que se habria comparado, por su nombre.</summary>
		internal override string TheUnitItIsExpressedIn()
		{
			return _limitUnit;
		}

		/// <summary>La unidad en que estan las mediciones, por su nombre.</summary>
		internal override string TheUnitTheMeasurementsAreIn()
		{
			return _measuredUnit;
		}
	}
}
