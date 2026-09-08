using System;

namespace Concesionario.Dominio
{
	/// <summary>La agenda que todavia no se evaluo ninguna vez.</summary>
	internal sealed class NoLastEvaluation : LastEvaluation
	{
		/// <summary>Se llega por <see cref="LastEvaluation.None"/>.</summary>
		internal NoLastEvaluation()
		{
		}

		/// <summary>No.</summary>
		internal override bool Exists()
		{
			return false;
		}

		/// <summary>Revienta: no hay instante que devolver.</summary>
		internal override DateTime Instant()
		{
			throw new DomainException(
				"Esta agenda no se evaluo nunca: pregunte HasBeenEvaluated antes. Un instante "
				+ "minimo devuelto aqui seria un centinela que parece una fecha, y la primera "
				+ "comparacion que lo tratara como tal saldria bien y estaria mal.");
		}

		/// <summary>No: no hay nada evaluado que pueda quedar despues de nada.</summary>
		internal override bool IsAfter(DateTime instant)
		{
			return false;
		}
	}
}
