using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La dimension de distancia suspendida porque el limite y las mediciones estan en unidades
	/// distintas. No se convierte: se deja de comparar y se nombran las dos, cual contra
	/// cual.
	/// </summary>
	internal sealed class ADimensionSuspendedByAUnitMismatch : LimitStanding
	{
		private readonly DistanceUnit _limitUnit;
		private readonly DistanceUnit _mismatchedUnit;

		/// <summary>
		/// La unidad del limite y la que no coincidio, sea la del ancla o la de la lectura.
		/// </summary>
		internal ADimensionSuspendedByAUnitMismatch(
			DistanceUnit limitUnit, DistanceUnit mismatchedUnit)
		{
			_limitUnit = limitUnit;
			_mismatchedUnit = mismatchedUnit;
		}

		/// <summary>Si: la ley mide distancia; lo que no se puede es comparar.</summary>
		internal override bool Applies
		{
			get { return true; }
		}

		/// <summary>Si.</summary>
		internal override bool SuspendedByUnitMismatch
		{
			get { return true; }
		}

		/// <summary><see cref="LimitClause.UnitMismatch"/>.</summary>
		internal override string TheClauseItAdds()
		{
			return LimitClause.UnitMismatch;
		}

		/// <summary>La distancia.</summary>
		internal override string TheDimensionItMeasures()
		{
			return IntervalDimension.Distance;
		}

		/// <summary>La unidad en que esta el limite, por su nombre.</summary>
		internal override string TheUnitOfTheLimit()
		{
			return _limitUnit.Name;
		}

		/// <summary>La unidad en que estan las mediciones, por su nombre.</summary>
		internal override string TheUnitTheMeasurementsAreIn()
		{
			return _mismatchedUnit.Name;
		}

		/// <summary>
		/// No hay resta que hacer entre dos unidades distintas, y lo que queda nombra las mismas dos
		/// unidades que la clausula.
		/// </summary>
		internal override ARemainder WhatRemains()
		{
			return ARemainder.UnitsThatDoNotMatch(_limitUnit.Name, _mismatchedUnit.Name);
		}
	}
}
