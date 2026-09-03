using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La ley mide la dimension y no hay de donde contar. Es la UNICA de las tres suspensiones que
	/// se cura trayendo un hecho, y por eso tiene su propio tipo en vez de un motivo en texto.
	/// </summary>
	internal sealed class ARemainderSuspendedByAnAbsentReading : ARemainder
	{
		private readonly string _said;

		/// <summary>
		/// Se llega por <see cref="ARemainder.NoReadingToCountFrom"/>, con la frase que la
		/// dimension ya compone: es la del dominio y no una paralela.
		/// </summary>
		internal ARemainderSuspendedByAnAbsentReading(string said)
		{
			_said = said;
		}

		/// <summary>Si: la ley mide la dimension; lo que faltan son los hechos.</summary>
		internal override bool Applies()
		{
			return true;
		}

		/// <summary>Si.</summary>
		internal override bool SuspendedByAnAbsentReading()
		{
			return true;
		}

		/// <summary><see cref="RemainderStanding.SuspendedByAnAbsentReading"/>.</summary>
		internal override RemainderStanding Standing()
		{
			return RemainderStanding.SuspendedByAnAbsentReading;
		}

		/// <summary>La frase de la dimension, tal como la compuso.</summary>
		internal override string Describe()
		{
			return _said;
		}
	}
}
