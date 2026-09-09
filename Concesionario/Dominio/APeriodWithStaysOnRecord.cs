using System;
using System.Collections.Generic;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La respuesta de un periodo que si tiene estadias: las que se cruzan con el, en orden de
	/// entrada, y los dias del periodo en que el vehiculo estuvo adentro, ascendentes y sin
	/// repetidos.
	/// </summary>
	internal sealed class APeriodWithStaysOnRecord : StaysInAPeriod
	{
		private readonly StayDay _from;
		private readonly StayDay _to;
		private readonly List<AStay> _stays;
		private readonly List<StayDay> _days;

		/// <summary>
		/// Se arma con el periodo y las estadias que se cruzan con el, y compone ahi mismo los dias
		/// que quedan adentro: un dia que dos estadias cubren es un dia en el taller.
		/// </summary>
		internal APeriodWithStaysOnRecord(StayDay from, StayDay to, List<AStay> stays)
		{
			_from = from;
			_to = to;
			_stays = new List<AStay>(stays);
			_days = TheDaysInsideThePeriod(stays, from, to);
		}

		/// <summary>Si.</summary>
		internal override bool HasAnyStay()
		{
			return true;
		}

		/// <summary>Las estadias que se cruzan con el periodo, en orden de entrada.</summary>
		internal override List<AStay> TheStays()
		{
			return new List<AStay>(_stays);
		}

		/// <summary>Los dias del periodo en que estuvo adentro, ascendentes y sin repetidos.</summary>
		internal override List<StayDay> TheDays()
		{
			return new List<StayDay>(_days);
		}

		/// <summary>
		/// El periodo en palabras: los dos dias por los que se pregunto, cuantos dias estuvo
		/// adentro, en cuantas estadias, y cuales son esos dias.
		/// </summary>
		internal override string Describe()
		{
			return "Entre el " + _from.HowItIsSaid() + " y el " + _to.HowItIsSaid()
				+ " estuvo en el taller " + HowManyDays() + (HowManyDays() == 1 ? " día" : " días")
				+ ", en " + HowManyStays() + (HowManyStays() == 1 ? " estadía" : " estadías") + ": "
				+ TheDaysSaid() + ".";
		}

		private string TheDaysSaid()
		{
			List<string> said = new List<string>();

			foreach (StayDay day in _days) said.Add(day.HowItIsSaid());

			return string.Join(", ", said);
		}

		private static List<StayDay> TheDaysInsideThePeriod(
			List<AStay> stays, StayDay from, StayDay to)
		{
			List<StayDay> days = new List<StayDay>();

			foreach (AStay stay in stays)
			{
				if (stay.IsOpen()) continue;

				foreach (StayDay day in stay.TheDaysItCovered())
				{
					if (day.IsBefore(from) || day.IsAfter(to)) continue;

					Place(days, day);
				}
			}

			return days;
		}

		private static void Place(List<StayDay> days, StayDay arriving)
		{
			int place = days.Count;

			while (place > 0 && days[place - 1].IsAfter(arriving)) place--;

			if (place > 0 && days[place - 1].IsTheSameDayAs(arriving)) return;

			days.Insert(place, arriving);
		}
	}
}
