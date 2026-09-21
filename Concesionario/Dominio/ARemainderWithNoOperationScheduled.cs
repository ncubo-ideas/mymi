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
		private readonly string _kind;

		/// <summary>
		/// Se llega por <see cref="ARemainder.WithNoOperationScheduled"/>, con la clase nombrada por
		/// su codigo: sin el nombre las clases no agendadas dirian todas la misma clausula.
		/// </summary>
		internal ARemainderWithNoOperationScheduled(string kind)
		{
			_kind = kind;
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

		/// <summary>La clase que no esta agendada, por su codigo.</summary>
		internal override string TheKindWithNoOperationScheduled()
		{
			return _kind;
		}
	}
}
