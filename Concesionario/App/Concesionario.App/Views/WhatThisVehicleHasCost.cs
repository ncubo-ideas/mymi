using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Concesionario.App.Views
{
	/// <summary>
	/// En que se le fue la plata a ESTE vehiculo, leido entero en un solo momento y por la unica
	/// puerta: el gasto del periodo por el que la pantalla pregunto con sus dos vistas, ese periodo
	/// contra el anterior, lo que costo la distancia, y los gastos de ese periodo como filas. Ninguna
	/// frase se compone aca, ningun numero se opera y ninguna cifra se vuelve a decir con un formato
	/// propio: el dinero llega dicho por su moneda.
	/// <para>
	/// Va aparte de la ficha, de la mitad del vehiculo y del ritmo de uso porque la ficha es de un
	/// modelo, la mitad del vehiculo se mide contra el sobre, y esto es un registro de un periodo con
	/// su propia clave.
	/// </para>
	/// </summary>
	public sealed class WhatThisVehicleHasCost
	{
		/// <summary>El comienzo del periodo por el que se pregunto, como instante con su hora.</summary>
		public string From { get; init; } = "";
		/// <summary>El final de ese periodo, como instante con su hora.</summary>
		public string To { get; init; } = "";
		/// <summary>El comienzo del periodo anterior, con el que este se compara.</summary>
		public string EarlierFrom { get; init; } = "";
		/// <summary>
		/// El final del periodo anterior, que es el mismo instante en que empieza el de arriba: las
		/// dos ventanas son contiguas y comparten esa bisagra.
		/// </summary>
		public string EarlierTo { get; init; } = "";
		/// <summary>
		/// Si el periodo tiene algun gasto, salido del banco adentro o consumido adentro. Esta en la
		/// superficie siempre y decide si la pantalla dibuja las filas o la constancia de que no hay.
		/// </summary>
		public bool AnyCost { get; init; }
		/// <summary>
		/// Si todo el gasto del periodo esta dicho en una sola moneda. Esta en la superficie siempre,
		/// y la pantalla no lo dibuja: la frase de abajo ya lo dice en palabras.
		/// </summary>
		public bool SaidInOneCurrency { get; init; }
		/// <summary>
		/// EL GASTO DEL PERIODO EN LA FRASE QUE COMPUSO EL DOMINIO, con las dos vistas adentro. Esta
		/// en la superficie siempre, y la pantalla dibuja la de abajo: esta dice todo lo que dice
		/// aquella menos la clausula de la moneda, asi que pintar las dos diria dos veces lo mismo.
		/// </summary>
		public string Says { get; init; } = "";
		/// <summary>
		/// EL MISMO GASTO DICHO MONEDA POR MONEDA, con las dos vistas adentro tambien y ademas en
		/// cuantas monedas esta dicho. Es la que la pantalla pinta.
		/// </summary>
		public string PerCurrencySays { get; init; } = "";
		/// <summary>
		/// LOS DOS PERIODOS EN UNA SOLA FRASE DEL DOMINIO, cada uno con sus limites y sus dos vistas.
		/// Ninguna diferencia entre ellos llega nunca como numero.
		/// </summary>
		public string BothPeriodsSay { get; init; } = "";
		/// <summary>
		/// Si se puede decir cuanto costo la distancia del periodo: hace falta un tramo de uso que
		/// medir y algun gasto. Esta en la superficie siempre, y la pantalla no lo dibuja.
		/// </summary>
		public bool CostPerDistanceCanBeSaid { get; init; }
		/// <summary>
		/// LO QUE COSTO LA DISTANCIA DEL PERIODO, moneda por moneda y con las dos unidades dichas, o
		/// la razon con nombre que impide decirlo. Cruza dos globales de raiz adentro del dominio.
		/// </summary>
		public string CostPerDistanceSays { get; init; } = "";
		/// <summary>Cuantos gastos se incurrieron adentro del periodo, contados por el dominio.</summary>
		public int EntryCount { get; init; }
		/// <summary>
		/// Los gastos incurridos adentro del periodo, una fila por gasto y en orden del instante en
		/// que se incurrieron. Cada una se dice sola, asi que no hay nada que recombinar.
		/// </summary>
		public IReadOnlyList<ACostOnTheRecord> Rows { get; init; } = new List<ACostOnTheRecord>();
		/// <summary>
		/// Lee las nueve respuestas por la puerta con los CUATRO limites ya calculados, para que el
		/// gasto, la comparacion, el costo por distancia y las filas hablen del mismo periodo del
		/// mismo registro. Los limites llegan calculados porque el instante del que salen lo lee la
		/// caratula, que es la unica que puede.
		/// </summary>
		public static WhatThisVehicleHasCost ReadThrough(
			DealershipCarAPI api, DateTime from, DateTime to, DateTime from2, DateTime to2)
		{
			ArgumentNullException.ThrowIfNull(api);
			CostLedgerRecord record = api.CostEntriesBetween(from, to);
			var rows = new List<ACostOnTheRecord>();
			foreach (int row in Enumerable.Range(0, record.Says.Length))
				rows.Add(new ACostOnTheRecord
				{
					IncurredAt = record.IncurredAt[row],
					Subject = record.Subject[row],
					Money = record.Money[row],
					Classification = record.Classification[row],
					ExpectedLife = record.ExpectedLife[row],
					Says = record.Says[row],
				});
			return new WhatThisVehicleHasCost
			{
				From = TheMoment(from),
				To = TheMoment(to),
				EarlierFrom = TheMoment(from2),
				EarlierTo = TheMoment(to2),
				AnyCost = api.HasAnyCostBetween(from, to),
				SaidInOneCurrency = api.IsSpendSaidInOneCurrencyBetween(from, to),
				Says = api.DescribeSpendBetween(from, to),
				PerCurrencySays = api.DescribeSpendPerCurrencyBetween(from, to),
				BothPeriodsSay = api.DescribeTwoPeriodsOfSpendCompared(from, to, from2, to2),
				CostPerDistanceCanBeSaid = api.CanCostPerDistanceBeSaid(from, to),
				CostPerDistanceSays = api.DescribeCostPerDistanceBetween(from, to),
				EntryCount = api.CostEntryCountBetween(from, to),
				Rows = rows,
			};
		}
		private static string TheMoment(DateTime instant)
		{
			return instant.ToString("yyyy-MM-dd HH:mm:ss'Z'", CultureInfo.InvariantCulture);
		}
	}
	/// <summary>
	/// UN gasto incurrido adentro del periodo, con sus propias columnas y con la frase que el dominio
	/// compuso para el. El dinero viene dicho por su moneda -- con su signo y sus decimales -- asi que
	/// la pantalla no lleva ningun formateador propio para el.
	/// </summary>
	public sealed class ACostOnTheRecord
	{
		/// <summary>Cuando se incurrio, como instante con su hora y nunca como un dia a secas.</summary>
		public string IncurredAt { get; init; } = "";
		/// <summary>En que se fue, con las palabras que llegaron.</summary>
		public string Subject { get; init; } = "";
		/// <summary>
		/// Lo que costo, dicho como su propia moneda lo dice. Nunca el numero pelado, que sin su
		/// moneda no se puede leer.
		/// </summary>
		public string Money { get; init; } = "";
		/// <summary>
		/// De que clase es, por el NOMBRE del miembro. La puso el comando que lo registro.
		/// </summary>
		public string Classification { get; init; } = "";
		/// <summary>
		/// La vida util de este gasto, y nada en la celda del que no tiene ninguna. Una celda vacia
		/// dice que no tiene; un cero se leeria como una vida de cero meses.
		/// </summary>
		public string ExpectedLife { get; init; } = "";
		/// <summary>El gasto entero en la frase que compuso el dominio, con su clase adentro.</summary>
		public string Says { get; init; } = "";
	}
}
