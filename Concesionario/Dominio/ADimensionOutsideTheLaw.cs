using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La dimension que la ley evaluada no tiene: el tiempo de una DistanceOnly, la distancia de
	/// una TimeOnly, las dos de una ConditionOnly. Ausente no es suspendida.
	/// </summary>
	internal sealed class ADimensionOutsideTheLaw : LimitStanding
	{
		private readonly string _dimension;

		/// <summary>
		/// Se llega por las dos fabricas <c>...IsNotPartOfThisLaw</c>, cada una con la dimension
		/// que nombra: el motivo no la menciona, pero lo que le queda tiene que decir cual es.
		/// </summary>
		internal ADimensionOutsideTheLaw(string dimension)
		{
			_dimension = dimension;
		}

		/// <summary>No.</summary>
		internal override bool Applies
		{
			get { return false; }
		}

		/// <summary>Ninguna: una dimension que la ley no tiene no se nombra en el motivo.</summary>
		internal override string TheClauseItAdds()
		{
			return "";
		}

		/// <summary>La dimension que la ley no tiene, por su codigo.</summary>
		internal override string TheDimensionItMeasures()
		{
			return _dimension;
		}

		/// <summary>
		/// Nada le queda, y lo dice nombrando la dimension. No reusa <see cref="TheClauseItAdds"/>:
		/// esa es la cadena vacia a proposito, y una cuenta atras no puede callar una de sus dos
		/// mitades.
		/// </summary>
		internal override ARemainder WhatRemains()
		{
			return ARemainder.NotMeasuredByThisLaw(_dimension);
		}
	}
}
