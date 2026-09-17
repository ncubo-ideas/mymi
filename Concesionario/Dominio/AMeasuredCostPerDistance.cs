using System;
using System.Collections.Generic;

namespace Concesionario.Dominio
{
	/// <summary>
	/// El costo por distancia de un periodo cuyo tramo de uso SI se pudo medir: el ancla con su
	/// instante, la distancia recorrida desde ella con su unidad, y lo gastado despues de ella
	/// dicho moneda por moneda.
	/// <para>
	/// Dos cosas que este tipo dice con un NOMBRE y nunca con una cifra: que despues del ancla no
	/// se gasto nada, y que el odometro no se movio. Un cero por unidad en cualquiera de las dos se
	/// leeria como un vehiculo que anduvo gratis.
	/// </para>
	/// </summary>
	internal sealed class AMeasuredCostPerDistance : ACostPerDistance
	{
		private readonly DateTime _firstReading;
		private readonly int _distanceTravelled;
		private readonly DistanceUnit _unitOfTravel;
		private readonly List<ACostEntry> _incurredAfterTheAnchor;

		/// <summary>Se llega por <see cref="ACostPerDistance.Of"/>, que filtra por el
		/// ancla.</summary>
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
		/// El periodo, el ancla con su instante, la distancia con su unidad, y una cifra por moneda
		/// -- o cual de las dos situaciones sin cifra le toca a este periodo. TOTAL.
		/// </summary>
		internal override ACostPerDistanceClause TheClauseItComposes()
		{
			if (_incurredAfterTheAnchor.Count == 0)
				return ACostPerDistanceClause.NothingSpentAfterTheAnchor(
					TheInstantItStartsAt(), TheInstantItEndsAt(), TheInstant(_firstReading),
					_distanceTravelled, _unitOfTravel.Name);

			if (_distanceTravelled == 0)
				return ACostPerDistanceClause.NoDistanceTravelled(
					TheInstantItStartsAt(), TheInstantItEndsAt(), TheInstant(_firstReading),
					_unitOfTravel.Name);

			return ACostPerDistanceClause.Measured(
				TheInstantItStartsAt(), TheInstantItEndsAt(), TheInstant(_firstReading),
				_distanceTravelled, _unitOfTravel.Name, TheFigurePerCurrency());
		}

		private List<Money> TheFigurePerCurrency()
		{
			List<Money> said = new List<Money>();

			foreach (List<ACostEntry> ofOneCurrency in ByCurrency())
			{
				Money whole = ofOneCurrency[0].TheMoneyItCost();

				for (int entry = 1; entry < ofOneCurrency.Count; entry++)
					whole = whole.Plus(ofOneCurrency[entry].TheMoneyItCost());

				said.Add(whole.Prorated(1, _distanceTravelled));
			}

			return said;
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
