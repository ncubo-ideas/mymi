using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// Unos limites de garantia que si existen: al menos una de sus dos mitades se publico, y la
	/// distancia viene siempre con la unidad en que se publico. Solo mide contra si mismo -- que
	/// sean estos los que rigen lo decide <see cref="CoveredSystem"/>.
	/// </summary>
	internal sealed class ASystemLimits : SystemLimits
	{
		private readonly int _months;
		private readonly int _distance;
		private readonly DistanceUnit _unit;

		/// <summary>
		/// Se llega por las dos fabricas de <see cref="SystemLimits"/>. Una mitad negativa o una
		/// distancia sin unidad son defectos de quien publica: en negativo el limite no dice nada
		/// que se pueda cumplir, y sin unidad no se puede comparar con nada.
		/// </summary>
		internal ASystemLimits(int months, int distance, DistanceUnit unit)
		{
			if (months < 0 || distance < 0)
				throw new DomainException(
					$"Se publicaron unos limites de {months} meses y {distance} de distancia. Un "
					+ "limite en negativo no es un limite: ya estaria cumplido antes de empezar.");

			if (distance > 0 && unit == null)
				throw new DomainException(
					"Se publicaron unos limites con distancia y sin decir en que unidad esta. Una "
					+ "distancia sin unidad no se puede comparar con el odometro de nadie, y aca las "
					+ "unidades no se convierten entre si.");

			_months = months;
			_distance = distance;
			_unit = unit;
		}

		/// <summary>Si.</summary>
		internal override bool Exists()
		{
			return true;
		}

		/// <summary>Si se publico una distancia.</summary>
		internal override bool HasADistanceHalf()
		{
			return _distance > 0;
		}

		/// <summary>
		/// Compara por IDENTIDAD contra la unidad en que se publico. Sin mitad de distancia no hay
		/// unidad que comparar, y entonces no coincide con ninguna.
		/// </summary>
		internal override bool MeasuresDistanceIn(DistanceUnit unit)
		{
			return HasADistanceHalf() && _unit == unit;
		}

		/// <summary>
		/// La unidad de la mitad de distancia. Revienta si no hay mitad de distancia: sin ella no
		/// hay unidad, y devolver una haria aparecer una comparacion que nadie publico.
		/// </summary>
		internal override DistanceUnit TheUnitItsDistanceIsMeasuredIn()
		{
			if (!HasADistanceHalf())
				throw new DomainException(
					"Estos limites no publicaron mitad de distancia, asi que no tienen unidad: hay "
					+ "que preguntar primero si la tienen.");

			return _unit;
		}

		/// <summary>
		/// Si el plazo se cumplio. Sin mitad de tiempo publicada no se cumple nunca: un cero leido
		/// como plazo dejaria al sistema afuera el mismo dia de la entrega. Los meses se cuentan
		/// ENTEROS -- el dia anterior al aniversario todavia esta adentro.
		/// </summary>
		internal override bool TheTimeHalfIsPast(DateTime deliveredAt, DateTime asOf)
		{
			if (_months == 0) return false;

			return WholeMonthsBetween(deliveredAt, asOf) >= _months;
		}

		/// <summary>
		/// Si lo recorrido desde la entrega alcanzo la distancia publicada. Sin mitad de distancia
		/// no se alcanza nunca, por la misma razon que el plazo.
		/// </summary>
		internal override bool TheDistanceHalfIsPast(int deliveryReading, int currentReading)
		{
			if (!HasADistanceHalf()) return false;

			return currentReading - deliveryReading >= _distance;
		}

		/// <summary>
		/// Los limites en palabras. La mitad que no se publico no se nombra: un cero ahi se
		/// leeria como un limite cumplido desde siempre.
		/// </summary>
		internal override string Describe()
		{
			if (!HasADistanceHalf()) return _months + " meses";

			string byDistance = _distance + " " + _unit.Name;

			if (_months == 0) return byDistance;

			return _months + " meses o " + byDistance;
		}

		private static int WholeMonthsBetween(DateTime from, DateTime to)
		{
			int months = ((to.Year - from.Year) * 12) + (to.Month - from.Month);

			if (to.Day < from.Day) months--;

			return months;
		}
	}
}
