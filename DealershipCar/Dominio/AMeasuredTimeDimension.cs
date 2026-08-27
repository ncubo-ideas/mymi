using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La dimension de tiempo medida: dias transcurridos desde el ancla contra el limite de dias
	/// ya escalado. No lleva unidad de distancia.
	/// </summary>
	internal sealed class AMeasuredTimeDimension : AMeasuredDimension
	{
		/// <summary>Se llega por <see cref="LimitStanding.OfTime"/>.</summary>
		internal AMeasuredTimeDimension(DueState state, int elapsedDays, int effectiveLimitDays)
			: base(state, elapsedDays, effectiveLimitDays)
		{
		}

		/// <summary><c>time</c>.</summary>
		protected override string Name
		{
			get { return "time"; }
		}

		/// <summary>Los dias se pluralizan.</summary>
		protected override string Amount(int quantity)
		{
			return quantity + (quantity == 1 ? " day" : " days");
		}
	}
}
