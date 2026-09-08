using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// El limite ya llego: el monto es el EXCESO y nunca es negativo. Cero es el limite alcanzado
	/// exacto y tiene frase propia -- "0 de mas" se leeria como defecto y no como respuesta.
	/// </summary>
	internal sealed class ARemainderAlreadyPassed : ARemainder
	{
		private readonly string _dimensionName;
		private readonly int _overshoot;
		private readonly string _unitName;
		private readonly string _overshootInWords;

		/// <summary>Se llega por <see cref="ARemainder.AlreadyPassed"/>, que la compone la dimension.</summary>
		internal ARemainderAlreadyPassed(
			string dimensionName, int overshoot, string unitName, string overshootInWords)
		{
			if (overshoot < 0)
				throw new DomainException(
					$"Se compuso un exceso de {overshoot}. El exceso es cuanto se paso del limite y "
					+ "eso no puede ser negativo: lo que todavia falta es la otra variante, y el "
					+ "signo de la resta es lo que elige entre las dos.");

			_dimensionName = dimensionName;
			_overshoot = overshoot;
			_unitName = unitName;
			_overshootInWords = overshootInWords;
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

		/// <summary>Si. El umbral es INCLUSIVO: el limite exacto ya llego.</summary>
		internal override bool IsAlreadyPassed()
		{
			return true;
		}

		/// <summary><see cref="RemainderStanding.AlreadyPassed"/>.</summary>
		internal override RemainderStanding Standing()
		{
			return RemainderStanding.AlreadyPassed;
		}

		/// <summary>Cuanto se paso del limite. Nunca negativo, y nunca acotado a cero.</summary>
		internal override int Amount()
		{
			return _overshoot;
		}

		/// <summary>La unidad del exceso, por su nombre.</summary>
		internal override string UnitName()
		{
			return _unitName;
		}

		/// <summary>Cuanto se paso, o que esta justo en el limite, con la dimension al frente.</summary>
		internal override string Describe()
		{
			if (_overshoot == 0) return _dimensionName + ": justo en el límite";

			return _dimensionName + ": lleva " + _overshootInWords + " de más";
		}
	}
}
