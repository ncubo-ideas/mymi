using System.Globalization;
using DealershipCar.App;

namespace DealershipCar.Wasm
{
	/// <summary>
	/// Las estadias de ESTE vehiculo en el taller, leidas enteras en un solo momento y por la unica
	/// puerta: el periodo por el que la pantalla pregunto y la estadia que cubre ese instante. Ninguna
	/// frase se compone aca. Va aparte de la ficha y de la mitad del vehiculo porque la ficha es de un
	/// modelo, la mitad del vehiculo se mide contra el sobre, y esto es un registro de un periodo.
	/// </summary>
	public sealed class TheStaysOfThisVehicle
	{
		/// <summary>El primer dia del periodo por el que se pregunto, en yyyy-MM-dd.</summary>
		public string FromDay { get; init; } = "";

		/// <summary>El ultimo dia del periodo por el que se pregunto, en yyyy-MM-dd.</summary>
		public string ToDay { get; init; } = "";

		/// <summary>El instante en que la pantalla pregunto. La mitad por instante es a ese momento.</summary>
		public string AskedAt { get; init; } = "";

		/// <summary>
		/// Si alguna estadia con dias registrados cruza el periodo. Es lo que decide si hay filas que
		/// pintar; una estadia todavia abierta no cuenta aqui, porque sus dias no han llegado.
		/// </summary>
		public bool AnyStayInThePeriod { get; init; }

		/// <summary>
		/// EL PERIODO EN LA FRASE QUE COMPUSO EL DOMINIO, con sus dos limites, sus dias y en cuantas
		/// estadias. Contesta tambien cuando no hay ninguna, nombrando el periodo igual.
		/// </summary>
		public string Says { get; init; } = "";

		/// <summary>Los dias del periodo en que estuvo adentro, ascendentes y sin repetidos.</summary>
		public string[] Days { get; init; } = new string[0];

		/// <summary>Cuantos dias del periodo estuvo adentro, contados por el dominio.</summary>
		public int DayCount { get; init; }

		/// <summary>Cuantas estadias del periodo, contadas por el dominio.</summary>
		public int StayCount { get; init; }

		/// <summary>
		/// Las estadias CERRADAS del periodo, una fila por estadia y en orden de entrada. Una estadia
		/// todavia abierta no esta aqui: se dice por instante, abajo.
		/// </summary>
		public IReadOnlyList<AStayOnTheRecord> Rows { get; init; } = new List<AStayOnTheRecord>();

		/// <summary>Si el vehiculo estaba adentro de algun taller al instante en que se pregunto.</summary>
		public bool InsideRightNow { get; init; }

		/// <summary>
		/// La estadia que cubre ese instante, en la frase del dominio, o la constancia de que ninguna
		/// lo cubre. Es una de las dos unicas respuestas que alcanzan una estadia todavia abierta.
		/// </summary>
		public string TheStayRightNowSays { get; init; } = "";

		/// <summary>
		/// Lee las dos mitades por la puerta con UN solo instante y UN solo periodo, para que lo que
		/// dice el periodo y lo que dice la estadia de ahora mismo sean del mismo momento.
		/// </summary>
		public static TheStaysOfThisVehicle ReadThrough(
			DealershipCarAPI api, string fromDay, string toDay, DateTime asOf)
		{
			ArgumentNullException.ThrowIfNull(api);

			ShopStayRecord record = api.StaysBetween(fromDay, toDay);

			var rows = new List<AStayOnTheRecord>();

			foreach (int row in Enumerable.Range(0, record.Branch.Length))
				rows.Add(new AStayOnTheRecord
				{
					Branch = record.Branch[row],
					Subject = record.Subject[row],
					EnteredAt = record.EnteredAt[row],
					CoveredDays = TheDaysOf(record.CoveredDays[row]),
					Says = record.Says[row],
				});

			return new TheStaysOfThisVehicle
			{
				FromDay = fromDay,
				ToDay = toDay,
				AskedAt = TheMoment(asOf),
				AnyStayInThePeriod = api.HasAnyStayBetween(fromDay, toDay),
				Says = api.DescribeDaysInTheShopBetween(fromDay, toDay),
				Days = api.DaysInTheShopBetween(fromDay, toDay),
				DayCount = api.DaysInTheShopCountBetween(fromDay, toDay),
				StayCount = api.StayCountBetween(fromDay, toDay),
				Rows = rows,
				InsideRightNow = api.IsInsideTheShopAt(asOf),
				TheStayRightNowSays = api.DescribeTheStayAt(asOf),
			};
		}

		/// <summary>
		/// Los dias de una celda del registro, uno por elemento. La puerta los manda pegados con
		/// espacios porque un registro de columnas paralelas es rectangular y una estadia de tres dias
		/// no tiene donde poner tres celdas; una pantalla no es un cable, y los devuelve a la forma en
		/// que la propia puerta contesta los dias de un periodo.
		/// </summary>
		private static string[] TheDaysOf(string cell)
		{
			return cell.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		}

		private static string TheMoment(DateTime instant)
		{
			return instant.ToString("yyyy-MM-dd HH:mm:ss'Z'", CultureInfo.InvariantCulture);
		}
	}

	/// <summary>
	/// UNA estadia cerrada del periodo, con sus propias columnas y con la frase que el dominio compuso
	/// para ella. La fila trae la estadia entera y nunca recortada al periodo: su propia frase nombra
	/// todos sus dias, y una celda recortada contradiria a su vecina.
	/// </summary>
	public sealed class AStayOnTheRecord
	{
		/// <summary>La sucursal en que la paso, en las palabras que mando el taller.</summary>
		public string Branch { get; init; } = "";

		/// <summary>Por que entro, en las palabras que mando el taller.</summary>
		public string Subject { get; init; } = "";

		/// <summary>Cuando entro, como un instante con su hora y nunca como un dia a secas.</summary>
		public string EnteredAt { get; init; } = "";

		/// <summary>Los dias de calendario que cubrio, uno por elemento.</summary>
		public string[] CoveredDays { get; init; } = new string[0];

		/// <summary>La estadia entera en la frase que compuso el dominio.</summary>
		public string Says { get; init; } = "";
	}
}
