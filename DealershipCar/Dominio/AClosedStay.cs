using System;
using System.Collections.Generic;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// Una estadia con sus dos extremos y los dias de calendario que la sucursal dice que cubrio.
	/// Es terminal: una estadia cerrada no se reabre.
	/// </summary>
	internal sealed class AClosedStay : AStay
	{
		private readonly DateTime _collectedAt;
		private readonly List<StayDay> _days;

		/// <summary>
		/// Los dos extremos y los dias recibidos. Una salida anterior a su entrada es un registro
		/// que se contradice; y los dias tienen que ser al menos uno y venir uno detras de otro, sin
		/// saltos ni repetidos, porque una estadia es continua por naturaleza.
		/// </summary>
		internal AClosedStay(
			string branch, StaySubject subject, DateTime enteredAt, DateTime collectedAt,
			StayDay[] days)
			: base(branch, subject, enteredAt)
		{
			if (collectedAt < enteredAt)
				throw new DomainException(
					"La estadia dice que el vehiculo salio el " + TheInstant(collectedAt)
					+ " y que entro el " + TheInstant(enteredAt) + ", asi que salio antes de "
					+ "entrar: es un registro que se contradice a si mismo, no una estadia corta.");

			_days = TheContiguousDays(days);
			_collectedAt = collectedAt;
		}

		/// <summary>No.</summary>
		internal override bool IsOpen()
		{
			return false;
		}

		/// <summary>Si alguno de los dias recibidos es ese dia.</summary>
		internal override bool Covers(StayDay day)
		{
			foreach (StayDay covered in _days)
				if (covered.IsTheSameDayAs(day)) return true;

			return false;
		}

		/// <summary>Si alguno de los dias recibidos cae entre esos dos, ambos inclusive.</summary>
		internal override bool OverlapsDays(StayDay from, StayDay to)
		{
			foreach (StayDay covered in _days)
				if (!covered.IsBefore(from) && !covered.IsAfter(to)) return true;

			return false;
		}

		/// <summary>Adentro entre sus dos instantes, ambos inclusive.</summary>
		internal override bool IsInsideAt(DateTime asOf)
		{
			return EnteredAt <= asOf && asOf <= _collectedAt;
		}

		/// <summary>Cuando salio.</summary>
		internal override DateTime CollectedAt
		{
			get { return _collectedAt; }
		}

		/// <summary>Los dias que cubrio, en el orden en que llegaron.</summary>
		internal override StayDay[] TheDaysItCovered()
		{
			return _days.ToArray();
		}

		/// <summary>Cuantos dias cubrio. Se cuenta sobre los dias en cada lectura, nunca se guarda.</summary>
		internal int HowManyDays()
		{
			return _days.Count;
		}

		/// <summary>
		/// La estadia cerrada en palabras: los dos extremos como instantes, cuantos dias cubrio y
		/// cuales.
		/// </summary>
		internal override string Describe()
		{
			return "Estuvo " + TheEntryClause() + " hasta el " + TheInstant(_collectedAt) + ": "
				+ HowManyDays() + (HowManyDays() == 1 ? " día" : " días") + " en el taller -- "
				+ TheDaysSaid() + ". " + TheSubjectClause();
		}

		private string TheDaysSaid()
		{
			List<string> said = new List<string>();

			foreach (StayDay day in _days) said.Add(day.HowItIsSaid());

			return string.Join(", ", said);
		}

		private static List<StayDay> TheContiguousDays(StayDay[] days)
		{
			if (days == null || days.Length == 0)
				throw new DomainException(
					"Una estadia cerrada cubrio al menos un dia, y con esta no llego ninguno. Los "
					+ "dias los dice la sucursal junto con la salida.");

			for (int day = 0; day < days.Length; day++)
				if (days[day] == null)
					throw new DomainException(
						"Llego un dia ausente entre los dias de una estadia, y un hueco no es un "
						+ "dia menos preciso: no es ninguno.");

			for (int day = 1; day < days.Length; day++)
			{
				StayDay previous = days[day - 1];

				if (!previous.Next().IsTheSameDayAs(days[day]))
					throw new DomainException(
						"Los dias de la estadia van del " + previous.HowItIsSaid() + " al "
						+ days[day].HowItIsSaid() + ", y despues del " + previous.HowItIsSaid()
						+ " viene el " + previous.Next().HowItIsSaid() + ". Una estadia es continua: "
						+ "un salto, un repetido o un desorden es un registro que se contradice a si "
						+ "mismo, y dos entradas separadas son dos estadias.");
			}

			return new List<StayDay>(days);
		}
	}
}
