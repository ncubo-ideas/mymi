using System;

namespace Concesionario.Dominio
{
	/// <summary>El ancla con lectura de odometro y la unidad en que se leyo.</summary>
	internal sealed class AnAnchorReading : AnchorReading
	{
		private readonly int _value;
		private readonly DistanceUnit _unit;

		/// <summary>Se llega por <see cref="AnchorReading.Of"/>, que lleva las guardas.</summary>
		internal AnAnchorReading(int value, DistanceUnit unit)
		{
			_value = value;
			_unit = unit;
		}

		/// <summary>Si.</summary>
		internal override bool Exists()
		{
			return true;
		}

		/// <summary>La lectura.</summary>
		internal override int Value
		{
			get { return _value; }
		}

		/// <summary>La unidad en que se tomo.</summary>
		internal override DistanceUnit Unit
		{
			get { return _unit; }
		}

		/// <summary>Compara por IDENTIDAD, nunca por nombre.</summary>
		internal override bool IsExpressedIn(DistanceUnit unit)
		{
			return _unit == unit;
		}
	}
}
