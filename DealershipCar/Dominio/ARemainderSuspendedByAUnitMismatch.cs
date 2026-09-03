using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El limite y las mediciones estan en unidades distintas, asi que no hay resta que hacer. No
	/// se convierte: se deja de medir y se dice cual contra cual.
	/// </summary>
	internal sealed class ARemainderSuspendedByAUnitMismatch : ARemainder
	{
		private readonly string _said;

		/// <summary>
		/// Se llega por <see cref="ARemainder.UnitsThatDoNotMatch"/>, con la frase que la dimension
		/// ya compone: es la que nombra las dos unidades.
		/// </summary>
		internal ARemainderSuspendedByAUnitMismatch(string said)
		{
			_said = said;
		}

		/// <summary>Si: la ley mide la dimension; lo que no se puede es comparar.</summary>
		internal override bool Applies()
		{
			return true;
		}

		/// <summary>Si.</summary>
		internal override bool SuspendedByAUnitMismatch()
		{
			return true;
		}

		/// <summary><see cref="RemainderStanding.SuspendedByAUnitMismatch"/>.</summary>
		internal override RemainderStanding Standing()
		{
			return RemainderStanding.SuspendedByAUnitMismatch;
		}

		/// <summary>La frase de la dimension, tal como la compuso.</summary>
		internal override string Describe()
		{
			return _said;
		}
	}
}
