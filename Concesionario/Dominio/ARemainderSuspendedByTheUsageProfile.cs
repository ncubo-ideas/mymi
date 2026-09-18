using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// El perfil de uso declarado ignora los limites de esa dimension, asi que no hay nada contra
	/// que medir. Es una decision declarada, no una falta de datos.
	/// </summary>
	internal sealed class ARemainderSuspendedByTheUsageProfile : ARemainder
	{
		private readonly string _usageProfile;

		/// <summary>
		/// Se llega por <see cref="ARemainder.IgnoredByTheUsageProfile"/>, con el perfil que
		/// decidio, por su codigo.
		/// </summary>
		internal ARemainderSuspendedByTheUsageProfile(string usageProfile)
		{
			_usageProfile = usageProfile;
		}

		/// <summary>Si: la ley mide la dimension; lo que la ignora es el perfil.</summary>
		internal override bool Applies()
		{
			return true;
		}

		/// <summary>Si.</summary>
		internal override bool SuspendedByTheUsageProfile()
		{
			return true;
		}

		/// <summary><see cref="RemainderStanding.SuspendedByTheUsageProfile"/>.</summary>
		internal override RemainderStanding Standing()
		{
			return RemainderStanding.SuspendedByTheUsageProfile;
		}

		/// <summary>La distancia, que es la dimension que un perfil puede ignorar.</summary>
		internal override string TheDimensionItMeasures()
		{
			return IntervalDimension.Distance;
		}

		/// <summary>El perfil que decidio ignorarla, por su codigo.</summary>
		internal override string TheUsageProfileThatIgnoresIt()
		{
			return _usageProfile;
		}
	}
}
