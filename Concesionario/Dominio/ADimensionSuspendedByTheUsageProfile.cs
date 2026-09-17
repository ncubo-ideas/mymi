using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La dimension de distancia suspendida porque el perfil de uso declarado ignora los limites
	/// de distancia (LowDistance). Es una decision declarada, no una falta de datos.
	/// </summary>
	internal sealed class ADimensionSuspendedByTheUsageProfile : LimitStanding
	{
		private readonly string _usageProfile;

		/// <summary>El perfil que declaro la decision, por su codigo, para poder decir cual aplico.</summary>
		internal ADimensionSuspendedByTheUsageProfile(string usageProfile)
		{
			_usageProfile = usageProfile;
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

		/// <summary><see cref="LimitClause.UsageProfile"/>, que nombra el perfil que decidio.</summary>
		internal override string TheClauseItAdds()
		{
			return LimitClause.UsageProfile;
		}

		/// <summary>La distancia.</summary>
		internal override string TheDimensionItMeasures()
		{
			return IntervalDimension.Distance;
		}

		/// <summary>El perfil que decidio ignorarla, por su codigo.</summary>
		internal override string TheUsageProfileThatIgnoresIt()
		{
			return _usageProfile;
		}

		/// <summary>
		/// No hay contra que medir cuando el perfil ignora la dimension, y lo que queda nombra el
		/// mismo perfil que la clausula.
		/// </summary>
		internal override ARemainder WhatRemains()
		{
			return ARemainder.IgnoredByTheUsageProfile(_usageProfile);
		}
	}
}
