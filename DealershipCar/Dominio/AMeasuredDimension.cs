using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// Una dimension que si se pudo medir: cuanto se lleva recorrido, contra que limite ya
	/// escalado, y en que estado la deja esa comparacion.
	/// </summary>
	internal abstract class AMeasuredDimension : LimitStanding
	{
		private readonly DueState _state;
		private readonly int _travelled;
		private readonly int _effectiveLimit;

		/// <summary>El estado, lo recorrido y el limite ya escalado por el perfil.</summary>
		protected AMeasuredDimension(DueState state, int travelled, int effectiveLimit)
		{
			_state = state;
			_travelled = travelled;
			_effectiveLimit = effectiveLimit;
		}

		/// <summary>Si.</summary>
		internal override bool Applies
		{
			get { return true; }
		}

		/// <summary>Si: es la unica variante que se pudo evaluar.</summary>
		internal override bool CanBeEvaluated
		{
			get { return true; }
		}

		/// <summary>El estado que esta sola dimension implica.</summary>
		internal override DueState State
		{
			get { return _state; }
		}

		/// <summary>
		/// La clausula del motivo. El limite alcanzado exacto tiene frase propia: "0 pasadas del
		/// limite" se leeria como defecto y no como respuesta.
		/// </summary>
		internal override string Describe()
		{
			if (_travelled == _effectiveLimit)
				return Name + ": exactly at a limit of " + Amount(_effectiveLimit);

			if (_travelled > _effectiveLimit)
				return Name + ": " + Amount(_travelled - _effectiveLimit)
					+ " past a limit of " + Amount(_effectiveLimit);

			return Name + ": " + _travelled + " of " + Amount(_effectiveLimit)
				+ " since the last one";
		}

		/// <summary>Como se llama esta dimension en el motivo: <c>time</c> o <c>distance</c>.</summary>
		protected abstract string Name { get; }

		/// <summary>Como se dice una cantidad de esta dimension.</summary>
		protected abstract string Amount(int quantity);

		/// <summary>El rango de avance. Solo lo medido entra en esta escalera.</summary>
		protected override int Advancement()
		{
			if (_state == DueState.Overdue) return 3;
			if (_state == DueState.Due) return 2;
			if (_state == DueState.DueSoon) return 1;

			return 0;
		}
	}
}
