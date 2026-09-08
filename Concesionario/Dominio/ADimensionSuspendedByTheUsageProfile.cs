using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La dimension de distancia suspendida porque el perfil de uso declarado ignora los limites
	/// de distancia (LowDistance). Es una decision declarada, no una falta de datos.
	/// </summary>
	internal sealed class ADimensionSuspendedByTheUsageProfile : LimitStanding
	{
		private readonly string _profileName;

		/// <summary>El perfil que declaro la decision, para poder mostrar cual aplico.</summary>
		internal ADimensionSuspendedByTheUsageProfile(string profileName)
		{
			_profileName = profileName;
		}

		/// <summary>Si: la ley mide distancia; lo que la ignora es el perfil.</summary>
		internal override bool Applies
		{
			get { return true; }
		}

		/// <summary>Si.</summary>
		internal override bool SuspendedByTheUsageProfile
		{
			get { return true; }
		}

		/// <summary>Nombra el perfil, que es el que decidio.</summary>
		internal override string Describe()
		{
			return "distancia: no se puede evaluar porque el perfil de uso " + _profileName
				+ " ignora los límites de distancia";
		}

		/// <summary>
		/// No hay contra que medir cuando el perfil ignora la dimension. La frase es la del
		/// dominio, que nombra el perfil que decidio.
		/// </summary>
		internal override ARemainder WhatRemains()
		{
			return ARemainder.IgnoredByTheUsageProfile(Describe());
		}
	}
}
