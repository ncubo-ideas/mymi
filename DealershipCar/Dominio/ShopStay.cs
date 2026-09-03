using System;
using System.Collections.Generic;
using System.Globalization;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El registro de cada estadia que este vehiculo paso adentro de una sucursal. Es el agregado,
	/// uno por <see cref="Vin"/>, y una raiz global del DSL.
	/// <para>
	/// No tiene maquina de estados: la tiene cada estadia. Guarda las estadias en orden de entrada y
	/// compone en cada lectura lo demas -- si esta adentro, que dias cubrio un periodo y cuantos
	/// son.
	/// </para>
	/// </summary>
	internal sealed class ShopStay
	{
		private readonly Vin _vin;
		private readonly List<AStay> _stays = new List<AStay>();

		/// <summary>
		/// El constructor ES la creacion y toma el vehiculo y nada mas. Cada estadia llega despues,
		/// por comando: es un hecho que la sucursal registra cuando pasa.
		/// </summary>
		internal ShopStay(string vin)
		{
			_vin = new Vin(vin);
		}

		/// <summary>De que vehiculo son estas estadias.</summary>
		internal string ReadVin()
		{
			return _vin.Value;
		}

		/// <summary>
		/// Registra la entrada al taller: donde, por que y cuando entro. Se rechaza mientras haya
		/// otra estadia abierta, porque un carro no puede estar en dos sucursales a la vez, y el
		/// rechazo nombra la que ya esta abierta.
		/// </summary>
		internal void RecordEntry(string branch, string subject, DateTime enteredAt)
		{
			if (IsAnyStayOpen())
			{
				AStay open = TheOpenStay();

				throw new DomainException(
					"El vehiculo ya tiene una estadia abierta en " + open.Branch + " desde el "
					+ TheInstant(open.EnteredAt) + ", asi que no puede entrar a otra: un carro no "
					+ "esta en dos sucursales a la vez. Primero hay que registrar de donde salio.");
			}

			Place(new AnOpenStay(branch, new StaySubject(subject), enteredAt));
		}

		/// <summary>
		/// Registra que entregaron el vehiculo: cuando salio y que dias de calendario dice la
		/// sucursal que cubrio la estadia. Cerrar lo que no esta abierto es un defecto de quien
		/// llama.
		/// </summary>
		internal void RecordCollection(DateTime collectedAt, string[] coveredDays)
		{
			int place = ThePlaceOfTheOpenStay();

			AnOpenStay open = (AnOpenStay)_stays[place];

			_stays[place] = open.CloseAt(collectedAt, TheReceivedDays(coveredDays));
		}

		/// <summary>
		/// Registra de un solo acto una estadia que ya termino: donde, por que, sus dos instantes y
		/// los dias que cubrio. Es la forma correctiva, para lo que llega despues de haber pasado.
		/// </summary>
		internal void RecordStay(
			string branch, string subject, DateTime enteredAt, DateTime collectedAt,
			string[] coveredDays)
		{
			Place(new AClosedStay(
				branch, new StaySubject(subject), enteredAt, collectedAt,
				TheReceivedDays(coveredDays)));
		}

		/// <summary>Si el vehiculo estaba adentro de alguna sucursal en ese instante. TOTAL.</summary>
		internal bool IsInsideTheShopAt(DateTime asOf)
		{
			foreach (AStay stay in _stays)
				if (stay.IsInsideAt(asOf)) return true;

			return false;
		}

		/// <summary>
		/// La estadia que cubre ese instante, dicha entera, o la constancia de que ninguna lo cubre.
		/// TOTAL.
		/// </summary>
		internal string DescribeTheStayAt(DateTime asOf)
		{
			foreach (AStay stay in _stays)
				if (stay.IsInsideAt(asOf)) return stay.Describe();

			return "No hay ninguna estadía en el taller que cubra el " + TheInstant(asOf) + ".";
		}

		/// <summary>Si alguna estadia con dias registrados se cruza con ese periodo. TOTAL.</summary>
		internal bool HasAnyStayBetween(string fromDay, string toDay)
		{
			return TheStaysBetween(fromDay, toDay).HasAnyStay();
		}

		/// <summary>
		/// Los dias del periodo en que el vehiculo estuvo en el taller, dichos en yyyy-MM-dd,
		/// ascendentes y sin repetidos. TOTAL: un periodo sin ninguno contesta la lista vacia, y su
		/// hermana en palabras nombra el periodo.
		/// </summary>
		internal string[] DaysInTheShopBetween(string fromDay, string toDay)
		{
			List<StayDay> days = TheStaysBetween(fromDay, toDay).TheDays();

			string[] said = new string[days.Count];

			for (int day = 0; day < days.Count; day++) said[day] = days[day].HowItIsSaid();

			return said;
		}

		/// <summary>Cuantos dias estuvo en el taller en ese periodo. TOTAL.</summary>
		internal int DaysInTheShopCountBetween(string fromDay, string toDay)
		{
			return TheStaysBetween(fromDay, toDay).HowManyDays();
		}

		/// <summary>
		/// El periodo en una frase: los dias en que estuvo adentro y en cuantas estadias, o la
		/// constancia de que no hay ninguna. TOTAL.
		/// </summary>
		internal string DescribeDaysInTheShopBetween(string fromDay, string toDay)
		{
			return TheStaysBetween(fromDay, toDay).Describe();
		}

		/// <summary>Cuantas estadias se cruzan con ese periodo. TOTAL.</summary>
		internal int StayCountBetween(string fromDay, string toDay)
		{
			return TheStaysBetween(fromDay, toDay).HowManyStays();
		}

		/// <summary>
		/// Compone la respuesta de un periodo. Es el UNICO sitio donde un periodo se resuelve: toda
		/// lectura de periodo lo envuelve, asi que ninguna puede estar acotada distinto que otra.
		/// </summary>
		internal StaysInAPeriod TheStaysBetween(string fromDay, string toDay)
		{
			StayDay from = new StayDay(fromDay);
			StayDay to = new StayDay(toDay);

			List<AStay> overlapping = new List<AStay>();

			foreach (AStay stay in _stays)
				if (stay.OverlapsDays(from, to)) overlapping.Add(stay);

			return StaysInAPeriod.Of(from, to, overlapping);
		}

		private static string TheInstant(DateTime instant)
		{
			return instant.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		}

		private static StayDay[] TheReceivedDays(string[] coveredDays)
		{
			if (coveredDays == null)
				throw new DomainException(
					"Una estadia cerrada llega con los dias que cubrio, dichos por la sucursal, y "
					+ "aca no llego ninguno.");

			StayDay[] days = new StayDay[coveredDays.Length];

			for (int day = 0; day < coveredDays.Length; day++)
				days[day] = new StayDay(coveredDays[day]);

			return days;
		}

		private void Place(AStay arriving)
		{
			_stays.Insert(ItsPlaceOnTheEntryTimeline(arriving), arriving);
		}

		private int ItsPlaceOnTheEntryTimeline(AStay arriving)
		{
			int place = _stays.Count;

			while (place > 0 && _stays[place - 1].EnteredAt > arriving.EnteredAt) place--;

			return place;
		}

		private bool IsAnyStayOpen()
		{
			foreach (AStay stay in _stays)
				if (stay.IsOpen()) return true;

			return false;
		}

		private AStay TheOpenStay()
		{
			return _stays[ThePlaceOfTheOpenStay()];
		}

		private int ThePlaceOfTheOpenStay()
		{
			for (int stay = 0; stay < _stays.Count; stay++)
				if (_stays[stay].IsOpen()) return stay;

			throw new DomainException(
				"El vehiculo no tiene ninguna estadia abierta que cerrar: hay que preguntar primero "
				+ "si esta adentro. Una estadia cerrada es terminal y no se reabre.");
		}
	}
}
