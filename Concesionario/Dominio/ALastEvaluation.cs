using System;

namespace Concesionario.Dominio
{
	/// <summary>La evaluacion que si ocurrio, con el instante que se recibio para ella.</summary>
	internal sealed class ALastEvaluation : LastEvaluation
	{
		private readonly DateTime _instant;

		/// <summary>Se llega por <see cref="LastEvaluation.At"/>.</summary>
		internal ALastEvaluation(DateTime instant)
		{
			_instant = instant;
		}

		/// <summary>Si.</summary>
		internal override bool Exists()
		{
			return true;
		}

		/// <summary>El instante que se evaluo.</summary>
		internal override DateTime Instant()
		{
			return _instant;
		}

		/// <summary>Si lo evaluado quedo despues de ese instante.</summary>
		internal override bool IsAfter(DateTime instant)
		{
			return _instant > instant;
		}
	}
}
