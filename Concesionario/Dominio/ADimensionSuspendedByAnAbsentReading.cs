using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La dimension de distancia que la ley tiene pero no hay de donde contar: el ancla no tiene
	/// lectura, o no llego ninguna lectura usable.
	/// </summary>
	internal sealed class ADimensionSuspendedByAnAbsentReading : LimitStanding
	{
		/// <summary>Se llega por <see cref="LimitStanding.OfDistance"/>.</summary>
		internal ADimensionSuspendedByAnAbsentReading()
		{
		}

		/// <summary>Si: la ley mide distancia; lo que faltan son los hechos.</summary>
		internal override bool Applies
		{
			get { return true; }
		}

		/// <summary>Si.</summary>
		internal override bool SuspendedByAnAbsentReading
		{
			get { return true; }
		}

		/// <summary><see cref="LimitClause.AbsentReading"/>: falta el hecho, no el limite.</summary>
		internal override string TheClauseItAdds()
		{
			return LimitClause.AbsentReading;
		}

		/// <summary>La distancia.</summary>
		internal override string TheDimensionItMeasures()
		{
			return IntervalDimension.Distance;
		}

		/// <summary>
		/// No se puede decir lo que queda, y la razon es la unica de las tres que se cura trayendo
		/// un hecho.
		/// </summary>
		internal override ARemainder WhatRemains()
		{
			return ARemainder.NoReadingToCountFrom();
		}
	}
}
