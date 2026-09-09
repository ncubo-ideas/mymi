using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La dimension de distancia que la ley tiene pero no hay de donde contar: el ancla no tiene
	/// lectura, o no llego ninguna lectura usable.
	/// </summary>
	internal sealed class ADimensionSuspendedByAnAbsentReading : LimitStanding
	{
		/// <summary>Se llega por <see cref="LimitStanding.OfDistance"/>.</summary>
		internal ADimensionSuspendedByAnAbsentReading()
		{
		}

		/// <summary>Si: la ley mide distancia; lo que faltan son los hechos.</summary>
		internal override bool Applies
		{
			get { return true; }
		}

		/// <summary>Si.</summary>
		internal override bool SuspendedByAnAbsentReading
		{
			get { return true; }
		}

		/// <summary>Dice que falta el hecho, no el limite.</summary>
		internal override string Describe()
		{
			return "distancia: no se puede evaluar porque no hay ninguna lectura usable";
		}

		/// <summary>
		/// No se puede decir lo que queda, y la razon es la unica de las tres que se cura trayendo
		/// un hecho. La frase es la del dominio, no una paralela.
		/// </summary>
		internal override ARemainder WhatRemains()
		{
			return ARemainder.NoReadingToCountFrom(Describe());
		}
	}
}
