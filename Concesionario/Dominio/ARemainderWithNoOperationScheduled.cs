using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La agenda no tiene ninguna operacion activa de esa clase, asi que no hay dimension que
	/// medir. NO es una suspension -- nadie la cura trayendo una lectura -- y no lleva dimension,
	/// porque la ausencia es de la operacion entera y no de una de sus dos mitades.
	/// </summary>
	internal sealed class ARemainderWithNoOperationScheduled : ARemainder
	{
		private readonly string _kindName;

		/// <summary>
		/// Se llega por <see cref="ARemainder.WithNoOperationScheduled"/>, con la clase nombrada:
		/// sin el nombre las clases no agendadas dirian todas la misma frase.
		/// </summary>
		internal ARemainderWithNoOperationScheduled(string kindName)
		{
			_kindName = kindName;
		}

		/// <summary>No: no hay ley evaluada de la que esta dimension pueda ser parte.</summary>
		internal override bool Applies()
		{
			return false;
		}

		/// <summary>Si.</summary>
		internal override bool HasNoOperationScheduled()
		{
			return true;
		}

		/// <summary><see cref="RemainderStanding.NoOperationScheduled"/>.</summary>
		internal override RemainderStanding Standing()
		{
			return RemainderStanding.NoOperationScheduled;
		}

		/// <summary>Nombra la clase que no esta agendada.</summary>
		internal override string Describe()
		{
			return "no hay ninguna operación de " + _kindName + " agendada para este vehículo";
		}
	}
}
