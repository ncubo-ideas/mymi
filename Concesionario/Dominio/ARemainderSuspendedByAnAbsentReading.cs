using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La ley mide la dimension y no hay de donde contar. Es la UNICA de las tres suspensiones que
	/// se cura trayendo un hecho, y por eso tiene su propio tipo en vez de un motivo en texto.
	/// </summary>
	internal sealed class ARemainderSuspendedByAnAbsentReading : ARemainder
	{
		/// <summary>Se llega por <see cref="ARemainder.NoReadingToCountFrom"/>.</summary>
		internal ARemainderSuspendedByAnAbsentReading()
		{
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

		/// <summary>La distancia, que es la unica dimension que se cuenta desde una lectura.</summary>
		internal override string TheDimensionItMeasures()
		{
			return IntervalDimension.Distance;
		}
	}
}
