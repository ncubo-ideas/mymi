using System;
using System.Collections.Generic;
using System.Globalization;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La respuesta de un periodo cuyo gasto esta en mas de una moneda: cada una dicha por su lado,
	/// con sus dos vistas, y ningun total entre ellas. Es un estado CON NOMBRE y no una ausencia: el
	/// gasto esta dicho entero, y lo unico que no existe es un total que cruce monedas.
	/// <para>
	/// El dinero no se convierte, se registra; agrupar por moneda es de quien consulta. Un cero, la
	/// mayor de las dos, o una excepcion que la caratula tenga que interpretar serian las tres formas
	/// de contestar algo que este modulo no sabe.
	/// </para>
	/// </summary>
	internal sealed class ASpendNotSaidAcrossCurrencies : SpendInAPeriod
	{
		private readonly List<ASpendSummary> _eachOnItsOwn;

		/// <summary>
		/// Se arma con el periodo, las entradas que lo tocan y un resumen por moneda, en el orden en
		/// que cada moneda aparecio.
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

		/// <summary>
		/// El periodo, cuantas monedas hay, la constancia de que no hay total entre ellas, y cada una
		/// dicha por su lado con sus dos vistas.
		/// </summary>
		internal override string Describe()
		{
			return ThePeriodClause() + " el gasto está en "
				+ _eachOnItsOwn.Count.ToString(CultureInfo.InvariantCulture)
				+ " monedas distintas, así que no hay un total entre ellas y cada una se dice por su "
				+ "lado." + EachOnItsOwn();
		}

		/// <summary>
		/// Lo mismo. Con mas de una moneda no existe una respuesta "entera" distinta de la lista por
		/// moneda: el total que la separaria es justo el que no hay.
		/// </summary>
		internal override string DescribePerCurrency()
		{
			return Describe();
		}

		private string EachOnItsOwn()
		{
			string said = "";

			foreach (ASpendSummary summary in _eachOnItsOwn) said = said + " " + summary.Describe();

			return said;
		}
	}
}
