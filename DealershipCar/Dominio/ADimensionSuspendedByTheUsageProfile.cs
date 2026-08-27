using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La dimension de distancia suspendida porque el perfil de uso declarado ignora los limites
	/// de distancia (§6, LowDistance). Es una decision declarada, no una falta de datos.
	/// </summary>
	internal sealed class ADimensionSuspendedByTheUsageProfile : LimitStanding
	{
		private readonly string _profileName;

		/// <summary>El perfil que declaro la decision. OHS-07 exige poder mostrar cual aplico.</summary>
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
			return "distance: cannot be evaluated because the " + _profileName
				+ " usage profile ignores distance limits";
		}
	}
}
