using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El sistema que no publico limites propios. No es un limite de cero: es la constancia de
	/// que no hay ninguno, y por eso lo que rige en su lugar es el sobre del vehiculo.
	/// </summary>
	internal sealed class NoSystemLimits : SystemLimits
	{
		/// <summary>Se llega por <see cref="SystemLimits.None"/>.</summary>
		internal NoSystemLimits()
		{
		}

		/// <summary>No.</summary>
		internal override bool Exists()
		{
			return false;
		}

		/// <summary>Revienta: no hay limites, asi que tampoco mitades.</summary>
		internal override bool HasADistanceHalf()
		{
			throw NothingToMeasureAgainst();
		}

		/// <summary>Revienta: sin limites no hay unidad en que esten medidos.</summary>
		internal override bool MeasuresDistanceIn(DistanceUnit unit)
		{
			throw NothingToMeasureAgainst();
		}

		/// <summary>Revienta: sin limites no hay unidad que nombrar.</summary>
		internal override DistanceUnit TheUnitItsDistanceIsMeasuredIn()
		{
			throw NothingToMeasureAgainst();
		}

		/// <summary>Revienta: no hay plazo que se pueda haber cumplido.</summary>
		internal override bool TheTimeHalfIsPast(DateTime deliveredAt, DateTime asOf)
		{
			throw NothingToMeasureAgainst();
		}

		/// <summary>Revienta: no hay distancia que se pueda haber alcanzado.</summary>
		internal override bool TheDistanceHalfIsPast(int deliveryReading, int currentReading)
		{
			throw NothingToMeasureAgainst();
		}

		/// <summary>Revienta: no hay limites que decir.</summary>
		internal override string Describe()
		{
			throw NothingToMeasureAgainst();
		}

		private static DomainException NothingToMeasureAgainst()
		{
			return new DomainException(
				"Este sistema no publico limites propios: hay que preguntar primero si los tiene, y "
				+ "si no los tiene lo que rige es el sobre del vehiculo. Un 'no' benigno aca diria "
				+ "que el vehiculo esta dentro de unos limites que nadie publico.");
		}
	}
}
