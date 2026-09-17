using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Queda algo por delante del limite ya escalado: el monto, su unidad y la dimension que lo
	/// mide. Es la unica variante junto con <see cref="ARemainderAlreadyPassed"/> que trae numero.
	/// </summary>
	internal sealed class ARemainderStillAhead : ARemainder
	{
		private readonly string _dimension;
		private readonly int _amount;
		private readonly string _unitName;

		/// <summary>Se llega por <see cref="ARemainder.StillAhead"/>, que la compone la dimension.</summary>
		internal ARemainderStillAhead(string dimension, int amount, string unitName)
		{
			_dimension = dimension;
			_amount = amount;
			_unitName = unitName;
		}

		/// <summary>Si.</summary>
		internal override bool Applies()
		{
			return true;
		}

		/// <summary>Si: es una de las dos variantes que se pudieron medir.</summary>
		internal override bool CanBeSaid()
		{
			return true;
		}

		/// <summary><see cref="RemainderStanding.StillAhead"/>.</summary>
		internal override RemainderStanding Standing()
		{
			return RemainderStanding.StillAhead;
		}

		/// <summary>El monto que falta, siempre positivo.</summary>
		internal override int Amount()
		{
			return _amount;
		}

		/// <summary>La unidad del monto, por su nombre.</summary>
		internal override string UnitName()
		{
			return _unitName;
		}

		/// <summary>La dimension que lo mide, por su codigo.</summary>
		internal override string TheDimensionItMeasures()
		{
			return _dimension;
		}

		/// <summary>El monto que falta, en una fila.</summary>
		internal override int[] TheAmountItCounts()
		{
			return new[] { _amount };
		}

		/// <summary>La unidad en que esta dicho el monto, por su nombre.</summary>
		internal override string TheUnitItIsExpressedIn()
		{
			return _unitName;
		}
	}
}
