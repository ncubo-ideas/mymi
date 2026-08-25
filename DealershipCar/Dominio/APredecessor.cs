using System;

namespace DealershipCar.Dominio
{
	/// <summary>La ocurrencia anterior que existe: la que termino y dejo lugar a esta.</summary>
	internal sealed class APredecessor : Predecessor
	{
		private readonly ScheduledOperation _occurrence;

		/// <summary>Se llega por <see cref="Predecessor.Of"/>, que rechaza el null.</summary>
		internal APredecessor(ScheduledOperation occurrence)
		{
			_occurrence = occurrence;
		}

		/// <summary>Si.</summary>
		internal override bool Exists()
		{
			return true;
		}

		/// <summary>La ocurrencia anterior, con su estado terminal intacto.</summary>
		internal override ScheduledOperation Occurrence()
		{
			return _occurrence;
		}
	}
}
