using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La dimension de distancia suspendida porque el limite y las mediciones estan en unidades
	/// distintas. No se convierte: se deja de comparar y se dice cual contra
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

		/// <summary>Nombra las dos unidades y dice que no se convierten.</summary>
		internal override string Describe()
		{
			return "distancia: no se puede evaluar porque el límite está en " + _limitUnit.Name
				+ " y las mediciones están en " + _mismatchedUnit.Name
				+ ", y las unidades nunca se convierten";
		}

		/// <summary>
		/// No hay resta que hacer entre dos unidades distintas. La frase es la del dominio, que
		/// nombra cual contra cual.
		/// </summary>
		internal override ARemainder WhatRemains()
		{
			return ARemainder.UnitsThatDoNotMatch(Describe());
		}
	}
}
