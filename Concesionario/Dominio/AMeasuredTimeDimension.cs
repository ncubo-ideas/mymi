using System;

namespace Concesionario.Dominio
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

		/// <summary>El tiempo.</summary>
		internal override string TheDimensionItMeasures()
		{
			return IntervalDimension.Time;
		}

		/// <summary>
		/// El dia es la unidad de esta dimension y no tiene nombre propio que proyectar: su
		/// dimension ya dice cual es, y como se dice una cantidad de dias es de quien la muestra.
		/// </summary>
		protected override string UnitName
		{
			get { return ""; }
		}
	}
}
