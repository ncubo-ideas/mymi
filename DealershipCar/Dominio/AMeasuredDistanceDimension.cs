using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La dimension de distancia medida: lo recorrido desde el ancla contra el limite ya escalado,
	/// en la unidad del limite. Llegar aqui quiere decir que las tres unidades coincidieron.
	/// </summary>
	internal sealed class AMeasuredDistanceDimension : AMeasuredDimension
	{
		private readonly DistanceUnit _limitUnit;

		/// <summary>Se llega por <see cref="LimitStanding.OfDistance"/>.</summary>
		internal AMeasuredDistanceDimension(
			DueState state, int travelled, int effectiveLimit, DistanceUnit limitUnit)
			: base(state, travelled, effectiveLimit)
		{
			_limitUnit = limitUnit;
		}

		/// <summary><c>distancia</c>.</summary>
		protected override string Name
		{
			get { return "distancia"; }
		}

		/// <summary>La unidad del limite, por su NOMBRE y nunca por su codigo.</summary>
		protected override string UnitName
		{
			get { return _limitUnit.Name; }
		}

		/// <summary>La distancia se dice en la unidad de su limite, que no se pluraliza.</summary>
		protected override string Amount(int quantity)
		{
			return quantity + " " + _limitUnit.Name;
		}
	}
}
