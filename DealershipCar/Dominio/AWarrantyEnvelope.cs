using System;
using System.Globalization;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El sobre de garantia que si se publico: sus dos mitades, la unidad de la de distancia y el
	/// instante en que la agencia lo publico. Lo que queda de cada mitad no vive aqui.
	/// </summary>
	internal sealed class AWarrantyEnvelope : WarrantyEnvelope
	{
		private readonly int _months;
		private readonly int _distance;
		private readonly DistanceUnit _unit;
		private readonly DateTime _publishedAt;

		/// <summary>Se llega por <see cref="WarrantyEnvelope.PublishedAs"/>, que lleva las guardas.</summary>
		internal AWarrantyEnvelope(
			int months, int distance, DistanceUnit unit, DateTime publishedAt)
		{
			_months = months;
			_distance = distance;
			_unit = unit;
			_publishedAt = publishedAt;
		}

		/// <summary>Si.</summary>
		internal override bool Exists()
		{
			return true;
		}

		/// <summary>El plazo publicado.</summary>
		internal override int Months
		{
			get { return _months; }
		}

		/// <summary>La distancia publicada.</summary>
		internal override int Distance
		{
			get { return _distance; }
		}

		/// <summary>La unidad en que se publico la distancia.</summary>
		internal override DistanceUnit TheUnitItsDistanceIsMeasuredIn
		{
			get { return _unit; }
		}

		/// <summary>
		/// Esa misma unidad, dicha con su codigo. Total con excepcion: una unidad que este mapeo
		/// no nombre revienta en vez de caer en la primera, porque caer en la primera mediria
		/// contra una unidad distinta de la que la frase nombra y nada se veria mal.
		/// </summary>
		internal override DistanceUnitCode TheCodeOfItsDistanceUnit
		{
			get
			{
				if (_unit == DistanceUnit.UnitA) return DistanceUnitCode.UnitA;
				if (_unit == DistanceUnit.UnitB) return DistanceUnitCode.UnitB;

				throw new DomainException(
					$"La unidad '{_unit.Name}' de este sobre no tiene codigo con que cruzar hacia "
					+ "otro agregado. Entro una unidad de distancia que este mapeo no nombra.");
			}
		}

		/// <summary>Cuando la agencia lo publico.</summary>
		internal override DateTime PublishedAt
		{
			get { return _publishedAt; }
		}

		/// <summary>Compara por IDENTIDAD, nunca por nombre.</summary>
		internal override bool MeasuresDistanceIn(DistanceUnit unit)
		{
			return _unit == unit;
		}

		/// <summary>
		/// Las dos mitades y la fecha en que se publicaron. Las dos van siempre: "36 meses" sin la
		/// distancia, o al reves, no le dice nada a quien no sabe cuanto maneja.
		/// </summary>
		internal override string Describe()
		{
			return _months + " months or " + _distance + " " + _unit.Name + ", published on "
				+ TheDay(_publishedAt) + ".";
		}

		private static string TheDay(DateTime instant)
		{
			return instant.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}
	}
}
