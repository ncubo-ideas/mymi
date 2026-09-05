using System;
using System.Collections.Generic;
using System.Globalization;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El costo por distancia de un periodo cuyo tramo de uso SI se pudo medir: el ancla con su
	/// instante, la distancia recorrida desde ella con su unidad, y lo gastado despues de ella dicho
	/// moneda por moneda.
	/// <para>
	/// Dos cosas que este tipo dice con palabras y nunca con una cifra: que despues del ancla no se
	/// gasto nada, y que el odometro no se movio. Un cero por unidad en cualquiera de las dos se
	/// leeria como un vehiculo que anduvo gratis.
	/// </para>
	/// </summary>
	internal sealed class AMeasuredCostPerDistance : ACostPerDistance
	{
		private readonly DateTime _firstReading;
		private readonly int _distanceTravelled;
		private readonly DistanceUnit _unitOfTravel;
		private readonly List<ACostEntry> _incurredAfterTheAnchor;

		/// <summary>Se llega por <see cref="ACostPerDistance.Of"/>, que filtra por el ancla.</summary>
		internal AMeasuredCostPerDistance(
			DateTime from, DateTime to, DateTime firstReading, int distanceTravelled,
			DistanceUnit unitOfTravel, List<ACostEntry> incurredAfterTheAnchor)
			: base(from, to)
		{
			_firstReading = firstReading;
			_distanceTravelled = distanceTravelled;
			_unitOfTravel = unitOfTravel;
			_incurredAfterTheAnchor = new List<ACostEntry>(incurredAfterTheAnchor);
		}

		/// <summary>
		/// El periodo, el ancla con su instante, la distancia con su unidad, y una cifra por moneda con
		/// las dos unidades dichas -- el signo de la moneda y la unidad de la distancia. TOTAL.
		/// </summary>
		internal override string Describe()
		{
			return ThePeriodClause() + ", desde la primera lectura del odómetro del período -- la del "
				+ TheInstant(_firstReading) + " -- el vehículo recorrió " + TheDistanceSaid() + ": "
				+ TheFiguresOrWhyThereAreNone() + " " + TheAnchorRule();
		}

		private string TheDistanceSaid()
		{
			return _distanceTravelled.ToString(CultureInfo.InvariantCulture) + " "
				+ _unitOfTravel.Name;
		}

		private string TheFiguresOrWhyThereAreNone()
		{
			if (_incurredAfterTheAnchor.Count == 0)
				return "después de esa lectura no se gastó nada.";

			if (_distanceTravelled == 0)
				return "no hay distancia recorrida por la que dividir lo que se gastó después de esa "
					+ "lectura, y eso no es un costo de cero por " + _unitOfTravel.Name + ".";

			return "lo que se gastó después de ella sale a " + TheFigurePerCurrency() + ".";
		}

		private string TheFigurePerCurrency()
		{
			string said = "";

			foreach (List<ACostEntry> ofOneCurrency in ByCurrency())
			{
				Money whole = ofOneCurrency[0].TheMoneyItCost();

				for (int entry = 1; entry < ofOneCurrency.Count; entry++)
					whole = whole.Plus(ofOneCurrency[entry].TheMoneyItCost());

				string figure = whole.Prorated(1, _distanceTravelled).Expressed()
					+ " por " + _unitOfTravel.Name;

				said = said.Length == 0 ? figure : said + " y " + figure;
			}

			return said;
		}

		private string TheAnchorRule()
		{
			return "Lo que se pagó en esa lectura o antes pagó el recorrido anterior y no cuenta acá.";
		}

		private List<List<ACostEntry>> ByCurrency()
		{
			List<List<ACostEntry>> groups = new List<List<ACostEntry>>();

			foreach (ACostEntry entry in _incurredAfterTheAnchor)
			{
				bool placed = false;

				foreach (List<ACostEntry> group in groups)
				{
					if (!group[0].IsInTheSameCurrencyAs(entry)) continue;

					group.Add(entry);
					placed = true;
					break;
				}

				if (!placed) groups.Add(new List<ACostEntry> { entry });
			}

			return groups;
		}
	}
}
