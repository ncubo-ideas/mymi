using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// Queda algo por delante del limite ya escalado: el monto, su unidad y la dimension que lo
	/// mide. Es la unica variante junto con <see cref="ARemainderAlreadyPassed"/> que trae numero.
	/// </summary>
	internal sealed class ARemainderStillAhead : ARemainder
	{
		private readonly string _dimensionName;
		private readonly int _amount;
		private readonly string _unitName;
		private readonly string _amountInWords;

		/// <summary>Se llega por <see cref="ARemainder.StillAhead"/>, que la compone la dimension.</summary>
		internal ARemainderStillAhead(
			string dimensionName, int amount, string unitName, string amountInWords)
		{
			_dimensionName = dimensionName;
			_amount = amount;
			_unitName = unitName;
			_amountInWords = amountInWords;
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

		/// <summary>
		/// Lo que falta, con la dimension al frente. Uno solo se dice en singular: "te faltan 1 km"
		/// se leeria mal escrito y no como una respuesta.
		/// </summary>
		internal override string Describe()
		{
			return _dimensionName + ": " + (_amount == 1 ? "te falta " : "te faltan ")
				+ _amountInWords;
		}
	}
}
