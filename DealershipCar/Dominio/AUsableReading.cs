using System;

namespace DealershipCar.Dominio
{
	/// <summary>La lectura usable que existe.</summary>
	internal sealed class AUsableReading : UsableReading
	{
		private readonly OdometerReading _reading;

		/// <summary>Se llega por <see cref="UsableReading.Of"/>, que rechaza el null.</summary>
		internal AUsableReading(OdometerReading reading)
		{
			_reading = reading;
		}

		/// <summary>Si.</summary>
		internal override bool Exists()
		{
			return true;
		}

		/// <summary>La lectura.</summary>
		internal override OdometerReading Reading()
		{
			return _reading;
		}
	}
}
