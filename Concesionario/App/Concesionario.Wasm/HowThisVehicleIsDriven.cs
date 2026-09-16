using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Concesionario.App;

namespace Concesionario.Wasm
{
	/// <summary>
	/// Cuanto se maneja ESTE vehiculo, leido entero en un solo momento y por la unica puerta: el ritmo
	/// del periodo por el que la pantalla pregunto, ese periodo contra el anterior, y las lecturas del
	/// odometro de ese periodo como filas. Ninguna frase se compone aca y ningun numero se opera. Va
	/// aparte de la ficha y de la mitad del vehiculo porque la ficha es de un modelo, la mitad del
	/// vehiculo se mide contra el sobre, y esto es un registro de un periodo.
	/// </summary>
	public sealed class HowThisVehicleIsDriven
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
		/// Si el ritmo del periodo se puede decir. Esta en la superficie siempre, y la pantalla no lo
		/// dibuja: la frase de abajo ya lo dice en palabras.
		/// </summary>
		public bool CanBeSaid { get; init; }

		/// <summary>
		/// En cual de las cinco situaciones esta el ritmo, por el NOMBRE del miembro. Esta en la
		/// superficie siempre, y la pantalla no lo dibuja ni nombra ninguno de los cinco.
		/// </summary>
		public string Standing { get; init; } = "";

		/// <summary>
		/// EL RITMO EN LA FRASE QUE COMPUSO EL DOMINIO: los dos extremos con sus instantes, la
		/// distancia, el tiempo y la distancia por dia; o la razon con nombre que impide decirlo.
		/// </summary>
		public string Says { get; init; } = "";

		/// <summary>
		/// LOS DOS PERIODOS EN UNA SOLA FRASE DEL DOMINIO, cada uno con su ritmo y el cambio del
		/// primero respecto al segundo dicho adentro. El cambio no llega nunca como numero.
		/// </summary>
		public string BothPeriodsSay { get; init; } = "";

		/// <summary>Cuantas lecturas del odometro cayeron en el periodo, contadas por el dominio.</summary>
		public int ReadingCount { get; init; }

		/// <summary>
		/// Las lecturas del periodo, una fila por lectura y en orden de observacion. Cada una se dice
		/// sola, asi que no hay nada que recombinar.
		/// </summary>
		public IReadOnlyList<AReadingOnTheRecord> Rows { get; init; } =
			new List<AReadingOnTheRecord>();

		/// <summary>
		/// Lee las seis respuestas por la puerta con los CUATRO limites ya calculados, para que el
		/// ritmo, la comparacion y las filas hablen del mismo periodo del mismo registro. Los limites
		/// llegan calculados porque el instante del que salen lo lee la caratula, que es la unica que
		/// puede.
		/// </summary>
		public static HowThisVehicleIsDriven ReadThrough(
			DealershipCarAPI api, DateTime from, DateTime to, DateTime earlierFrom,
			DateTime earlierTo, string language)
		{
			ArgumentNullException.ThrowIfNull(api);

			OdometerSeriesRecord record = api.ReadingsBetween(from, to, language);

			var rows = new List<AReadingOnTheRecord>();

			foreach (int row in Enumerable.Range(0, record.Says.Length))
				rows.Add(new AReadingOnTheRecord
				{
					ObservedAt = record.ObservedAt[row],
					Value = record.Value[row],
					Unit = record.Unit[row],
					Source = record.Source[row],
					Says = record.Says[row],
				});

			return new HowThisVehicleIsDriven
			{
				From = TheMoment(from),
				To = TheMoment(to),
				EarlierFrom = TheMoment(earlierFrom),
				EarlierTo = TheMoment(earlierTo),
				CanBeSaid = api.CanARateOfUseBeSaid(from, to),
				Standing = api.ReadRateOfUseStanding(from, to),
				Says = api.DescribeRateOfUse(from, to, language),
				BothPeriodsSay = api.DescribeTwoPeriodsOfUseCompared(
					from, to, earlierFrom, earlierTo, language),
				ReadingCount = api.ReadingCountBetween(from, to),
				Rows = rows,
			};
		}

		private static string TheMoment(DateTime instant)
		{
			return instant.ToString("yyyy-MM-dd HH:mm:ss'Z'", CultureInfo.InvariantCulture);
		}
	}

	/// <summary>
	/// UNA lectura del odometro del periodo, con sus propias columnas y con la frase que el dominio
	/// compuso para ella. La frase nombra la fuente en las palabras del dominio, asi que la pantalla
	/// no lleva ninguna tabla propia para ese conjunto cerrado.
	/// </summary>
	public sealed class AReadingOnTheRecord
	{
		/// <summary>Cuando se observo, como instante con su hora y nunca como un dia a secas.</summary>
		public string ObservedAt { get; init; } = "";

		/// <summary>Lo que marcaba el odometro, en la unidad que dice esta misma fila.</summary>
		public int Value { get; init; }

		/// <summary>La unidad de esa lectura, por su nombre de mundo real.</summary>
		public string Unit { get; init; } = "";

		/// <summary>
		/// De que fuente vino, por el NOMBRE del miembro, que es la mitad direccionable. Nada se pesa
		/// ni se descarta por ella.
		/// </summary>
		public string Source { get; init; } = "";

		/// <summary>La lectura entera en la frase que compuso el dominio, con su fuente adentro.</summary>
		public string Says { get; init; } = "";
	}
}
