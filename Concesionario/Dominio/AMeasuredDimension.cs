using System;

namespace Concesionario.Dominio
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
		/// Cual de las tres clausulas medidas. El limite alcanzado exacto tiene la suya: "0 pasadas
		/// del limite" se leeria como defecto y no como respuesta.
		/// </summary>
		internal override string TheClauseItAdds()
		{
			if (_travelled == _effectiveLimit) return LimitClause.AtTheLimit;
			if (_travelled > _effectiveLimit) return LimitClause.PastTheLimit;

			return LimitClause.BeforeTheLimit;
		}

		/// <summary>
		/// Lo contado desde el ancla, o lo que se paso del limite donde la clausula es la de haberlo
		/// pasado: cada clausula nombra el monto del que habla y nunca los dos.
		/// </summary>
		internal override int TheAmountItReached()
		{
			return _travelled > _effectiveLimit ? _travelled - _effectiveLimit : _travelled;
		}

		/// <summary>El limite ya escalado contra el que se conto.</summary>
		internal override int TheLimitItIsAgainst()
		{
			return _effectiveLimit;
		}

		/// <summary>La unidad del limite, por su nombre.</summary>
		internal override string TheUnitOfTheLimit()
		{
			return UnitName;
		}

		/// <summary>
		/// Lo que queda contra el limite ya escalado. ES LA UNICA RESTA de este dominio en el
		/// sentido "lo que falta", y el signo elige la variante: nunca sale un numero negativo, y
		/// tampoco se acota a cero -- pasado el limite el monto es el exceso, que es verdad.
		/// </summary>
		internal override ARemainder WhatRemains()
		{
			int left = _effectiveLimit - _travelled;

			if (left > 0) return ARemainder.StillAhead(TheDimensionItMeasures(), left, UnitName);

			return ARemainder.AlreadyPassed(TheDimensionItMeasures(), -left, UnitName);
		}

		/// <summary>La unidad en que esta dimension dice sus montos, por su nombre.</summary>
		protected abstract string UnitName { get; }

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
