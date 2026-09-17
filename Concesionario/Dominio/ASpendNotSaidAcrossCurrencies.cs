using System;
using System.Collections.Generic;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La respuesta de un periodo cuyo gasto esta en mas de una moneda: cada una dicha por su lado,
	/// con sus dos vistas, y ningun total entre ellas. Es un estado CON NOMBRE y no una ausencia:
	/// el gasto esta dicho entero, y lo unico que no existe es un total que cruce monedas.
	/// <para>
	/// El dinero no se convierte, se registra; agrupar por moneda es de quien consulta. Un cero, la
	/// mayor de las dos, o una excepcion que la caratula tenga que interpretar serian las tres
	/// formas de contestar algo que este modulo no sabe.
	/// </para>
	/// </summary>
	internal sealed class ASpendNotSaidAcrossCurrencies : SpendInAPeriod
	{
		private readonly List<ASpendSummary> _eachOnItsOwn;

		/// <summary>
		/// Se arma con el periodo, las entradas que lo tocan y un resumen por moneda, en el orden
		/// en que cada moneda aparecio.
		/// </summary>
		internal ASpendNotSaidAcrossCurrencies(
			DateTime from, DateTime to, List<ACostEntry> touching, List<ASpendSummary> eachOnItsOwn)
			: base(from, to, touching)
		{
			if (eachOnItsOwn == null || eachOnItsOwn.Count < 2)
				throw new DomainException(
					"Este estado es el de un periodo con mas de una moneda, y aca no llegaron dos "
					+ "resumenes. Con una sola moneda el periodo se dice entero.");

			_eachOnItsOwn = new List<ASpendSummary>(eachOnItsOwn);
		}

		/// <summary>Si: hay gasto. Lo que no hay es un total entre monedas.</summary>
		internal override bool HasAnyCost()
		{
			return true;
		}

		/// <summary>No.</summary>
		internal override bool IsSaidInOneCurrency()
		{
			return false;
		}

		/// <summary>Mas de una moneda, y ningun total entre ellas.</summary>
		internal override string TheShapeItHas()
		{
			return SpendShape.NotSaidAcrossCurrencies;
		}

		/// <summary>
		/// Cada moneda por su lado, con sus dos vistas. Cuantas son se cuenta de esta lista: un
		/// conteo aparte seria una segunda version del mismo hecho.
		/// </summary>
		internal override List<ASummaryClause> TheClausesOfItsCurrencies()
		{
			List<ASummaryClause> said = new List<ASummaryClause>();

			foreach (ASpendSummary summary in _eachOnItsOwn) said.Add(summary.TheClauseItComposes());

			return said;
		}
	}
}
