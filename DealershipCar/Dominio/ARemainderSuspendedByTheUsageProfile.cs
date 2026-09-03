using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El perfil de uso declarado ignora los limites de esa dimension, asi que no hay nada contra
	/// que medir. Es una decision declarada, no una falta de datos.
	/// </summary>
	internal sealed class ARemainderSuspendedByTheUsageProfile : ARemainder
	{
		private readonly string _said;

		/// <summary>
		/// Se llega por <see cref="ARemainder.IgnoredByTheUsageProfile"/>, con la frase que la
		/// dimension ya compone: es la que nombra el perfil que decidio.
		/// </summary>
		internal ARemainderSuspendedByTheUsageProfile(string said)
		{
			_said = said;
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

		/// <summary>La frase de la dimension, tal como la compuso.</summary>
		internal override string Describe()
		{
			return _said;
		}
	}
}
