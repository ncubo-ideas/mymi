using System;
using System.Collections.Generic;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Todo lo que se ha gastado en este vehiculo, entrada por entrada. Es el agregado, uno por
	/// <see cref="Vin"/>, y una raiz global del DSL.
	/// <para>
	/// Guarda las entradas en orden del instante en que se incurrieron y compone en cada lectura todo
	/// lo demas -- las dos vistas de un periodo, las monedas por su lado, la comparacion entre dos
	/// periodos y las filas. Ninguna entrada se reemplaza: una equivocada se corrige registrando lo
	/// que de verdad paso.
	/// </para>
	/// </summary>
	internal sealed class CostLedger
	{
		private readonly Vin _vin;
		private readonly List<ACostEntry> _entries = new List<ACostEntry>();

		/// <summary>
		/// El constructor ES la creacion y toma el vehiculo y nada mas. Cada gasto llega despues, por
		/// comando: es un hecho que la sucursal registra cuando pasa.
		/// </summary>
		internal CostLedger(string vin)
		{
			_vin = new Vin(vin);
		}

		/// <summary>De que vehiculo son estos gastos.</summary>
		internal string ReadVin()
		{
			return _vin.Value;
		}

		/// <summary>
		/// Registra un gasto que se consume en el periodo en que se incurrio: cuanto, en que moneda,
		/// en que se fue y cuando. La clasificacion es este comando: no hay ninguna ranura por donde
		/// decirla ni ninguna vida util que darle.
		/// </summary>
		internal void RecordRecurringCost(
			DateTime incurredAt, decimal amount, string currency, string subject)
		{
			Place(new ARecurringCost(
				TheMoneyOf(amount, currency), incurredAt, new CostSubject(subject)));
		}

		/// <summary>
		/// Registra un gasto que sigue dando valor despues, con su vida util en meses. La vida util es
		/// obligatoria y la clasificacion es este comando.
		/// </summary>
		internal void RecordDurableCost(
			DateTime incurredAt, decimal amount, string currency, string subject,
			int expectedLifeMonths)
		{
			Place(new ADurableCost(
				TheMoneyOf(amount, currency), incurredAt, new CostSubject(subject),
				new AnExpectedLife(expectedLifeMonths)));
		}

		/// <summary>Si el periodo tiene algun gasto, salido o consumido. TOTAL.</summary>
		internal bool HasAnyCostBetween(DateTime from, DateTime to)
		{
			return TheSpendBetween(from, to).HasAnyCost();
		}

		/// <summary>
		/// El periodo en una frase, con las dos vistas juntas, o la constancia con nombre de que no
		/// hay ningun gasto. TOTAL.
		/// </summary>
		internal string DescribeSpendBetween(DateTime from, DateTime to)
		{
			return TheSpendBetween(from, to).Describe();
		}

		/// <summary>Si todo el gasto del periodo esta dicho en una sola moneda. TOTAL.</summary>
		internal bool IsSpendSaidInOneCurrencyBetween(DateTime from, DateTime to)
		{
			return TheSpendBetween(from, to).IsSaidInOneCurrency();
		}

		/// <summary>
		/// El periodo dicho moneda por moneda, o el estado con nombre en que no hay total entre ellas.
		/// TOTAL.
		/// </summary>
		internal string DescribeSpendPerCurrencyBetween(DateTime from, DateTime to)
		{
			return TheSpendBetween(from, to).DescribePerCurrency();
		}

		/// <summary>
		/// Los dos periodos dichos juntos, cada uno con sus limites y sus dos vistas, y sin ninguna
		/// diferencia suelta. TOTAL.
		/// </summary>
		internal string DescribeTwoPeriodsOfSpendCompared(
			DateTime from1, DateTime to1, DateTime from2, DateTime to2)
		{
			return ASpendComparison.Of(
				TheSpendBetween(from1, to1), TheSpendBetween(from2, to2)).Describe();
		}

		/// <summary>
		/// Lo que este periodo costo por unidad de distancia, moneda por moneda, sobre el tramo que
		/// otro agregado midio: el instante de la primera lectura del periodo, la distancia recorrida
		/// desde ella y la unidad de esa distancia. TOTAL.
		/// <para>
		/// Solo cuenta lo incurrido DESPUES de esa primera lectura. La unidad llega como CODIGO porque
		/// el valor viene de otro agregado adentro del mismo script, donde un enum cruza tipado.
		/// </para>
		/// </summary>
		internal string DescribeCostPerDistance(
			DateTime from, DateTime to, DateTime firstReading, int distanceTravelled,
			DistanceUnitCode unitOfTravel)
		{
			return ACostPerDistance.Of(
				from, to, firstReading, distanceTravelled, DistanceUnit.OfCode(unitOfTravel),
				TheSpendBetween(from, to).TheEntriesIncurredInside()).Describe();
		}

		/// <summary>
		/// Lo mismo cuando el periodo no tiene tramo de uso que medir: el motivo llega DICHO por la
		/// serie del odometro y se pasa entero. TOTAL, y es una operacion aparte y no un valor especial
		/// de la de arriba -- una distancia por defecto seria una medicion que nadie tomo.
		/// </summary>
		internal string DescribeCostPerDistanceWithNoSpan(
			DateTime from, DateTime to, string whyTheRateCannotBeSaid)
		{
			return ACostPerDistance.WithNoSpanOfUse(from, to, whyTheRateCannotBeSaid).Describe();
		}

		/// <summary>Cuantas entradas se incurrieron adentro del periodo. TOTAL.</summary>
		internal int CostEntryCountBetween(DateTime from, DateTime to)
		{
			return TheSpendBetween(from, to).TheEntriesIncurredInside().Count;
		}

		/// <summary>
		/// Cuando se incurrio cada entrada del periodo, dicho como INSTANTE con su hora, una por fila
		/// y en orden. TOTAL.
		/// </summary>
		internal string[] CostEntryInstantsBetween(DateTime from, DateTime to)
		{
			List<ACostEntry> rows = TheSpendBetween(from, to).TheEntriesIncurredInside();

			string[] said = new string[rows.Count];

			for (int row = 0; row < rows.Count; row++) said[row] = rows[row].TheInstantItWasIncurred();

			return said;
		}

		/// <summary>
		/// En que se fue cada entrada del periodo, con las palabras que llegaron, una por fila y en el
		/// mismo orden. TOTAL.
		/// </summary>
		internal string[] CostEntrySubjectsBetween(DateTime from, DateTime to)
		{
			List<ACostEntry> rows = TheSpendBetween(from, to).TheEntriesIncurredInside();

			string[] said = new string[rows.Count];

			for (int row = 0; row < rows.Count; row++) said[row] = rows[row].TheSubjectItWasSpentOn();

			return said;
		}

		/// <summary>
		/// El dinero de cada entrada del periodo, dicho como su propia moneda lo dice, una por fila y
		/// en el mismo orden. TOTAL.
		/// </summary>
		internal string[] CostEntryMoniesBetween(DateTime from, DateTime to)
		{
			List<ACostEntry> rows = TheSpendBetween(from, to).TheEntriesIncurredInside();

			string[] said = new string[rows.Count];

			for (int row = 0; row < rows.Count; row++) said[row] = rows[row].TheMoneyItSays();

			return said;
		}

		/// <summary>
		/// De que clase es cada entrada del periodo, por el NOMBRE de su miembro, una por fila y en el
		/// mismo orden. TOTAL.
		/// </summary>
		internal string[] CostEntryClassificationsBetween(DateTime from, DateTime to)
		{
			List<ACostEntry> rows = TheSpendBetween(from, to).TheEntriesIncurredInside();

			string[] said = new string[rows.Count];

			for (int row = 0; row < rows.Count; row++) said[row] = rows[row].Classification().Name;

			return said;
		}

		/// <summary>
		/// La vida util de cada entrada del periodo, o nada en la celda de la que no tiene ninguna, una
		/// por fila y en el mismo orden. TOTAL.
		/// </summary>
		internal string[] CostEntryExpectedLivesBetween(DateTime from, DateTime to)
		{
			List<ACostEntry> rows = TheSpendBetween(from, to).TheEntriesIncurredInside();

			string[] said = new string[rows.Count];

			for (int row = 0; row < rows.Count; row++)
				said[row] = rows[row].HowItsExpectedLifeIsSaid();

			return said;
		}

		/// <summary>
		/// Cada entrada del periodo en su propia frase, dicha entera, una por fila y en el mismo
		/// orden. TOTAL.
		/// </summary>
		internal string[] DescribeEachCostEntryBetween(DateTime from, DateTime to)
		{
			List<ACostEntry> rows = TheSpendBetween(from, to).TheEntriesIncurredInside();

			string[] said = new string[rows.Count];

			for (int row = 0; row < rows.Count; row++) said[row] = rows[row].Describe();

			return said;
		}

		private static Money TheMoneyOf(decimal amount, string currency)
		{
			return new Money(amount, Currency.OfPublished(currency));
		}

		private SpendInAPeriod TheSpendBetween(DateTime from, DateTime to)
		{
			List<ACostEntry> touching = new List<ACostEntry>();

			foreach (ACostEntry entry in _entries)
				if (entry.WasIncurredBetween(from, to) || entry.ConsumesAnythingBetween(from, to))
					touching.Add(entry);

			return SpendInAPeriod.Of(from, to, touching);
		}

		private void Place(ACostEntry arriving)
		{
			_entries.Insert(ItsPlaceOnTheIncurredTimeline(arriving), arriving);
		}

		private int ItsPlaceOnTheIncurredTimeline(ACostEntry arriving)
		{
			int place = _entries.Count;

			while (place > 0 && _entries[place - 1].IncurredAt > arriving.IncurredAt) place--;

			return place;
		}
	}
}
