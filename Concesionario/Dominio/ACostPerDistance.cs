using System;
using System.Collections.Generic;
using System.Globalization;

namespace Concesionario.Dominio
{
	/// <summary>
	/// El dinero de un periodo sobre la distancia que el vehiculo recorrio en el, moneda por moneda, o
	/// la constancia con nombre de que no hay tramo de uso por el que dividir. Se compone en cada
	/// lectura y no se guarda.
	/// <para>
	/// Solo cuenta lo que se incurrio DESPUES de la primera lectura del odometro del periodo: lo que se
	/// pago en esa lectura o antes pago el recorrido anterior. La frase nombra esa lectura y su
	/// instante para que la regla se vea.
	/// </para>
	/// </summary>
	internal abstract class ACostPerDistance
	{
		private readonly DateTime _from;
		private readonly DateTime _to;

		/// <summary>El periodo por el que se pregunto, que es lo que las dos situaciones comparten.</summary>
		internal ACostPerDistance(DateTime from, DateTime to)
		{
			_from = from;
			_to = to;
		}

		/// <summary>
		/// La respuesta cuando el tramo de uso SI se pudo medir: el ancla, la distancia recorrida desde
		/// ella y su unidad llegan del otro agregado, y las entradas del periodo llegan del libro.
		/// </summary>
		internal static ACostPerDistance Of(
			DateTime from, DateTime to, DateTime firstReading, int distanceTravelled,
			DistanceUnit unitOfTravel, List<ACostEntry> incurredInside)
		{
			if (unitOfTravel == null)
				throw new DomainException(
					"Llego una distancia recorrida sin decir en que unidad. Una distancia sin unidad no "
					+ "se puede dividir por nada, y aca las unidades no se convierten.");

			if (incurredInside == null)
				throw new DomainException(
					"Un costo por distancia se arma con las entradas del periodo, y aca no llego "
					+ "ninguna lista: ninguna entrada y una lista ausente no son lo mismo.");

			if (distanceTravelled < 0)
				throw new DomainException(
					"Llego una distancia recorrida de "
					+ distanceTravelled.ToString(CultureInfo.InvariantCulture) + " "
					+ unitOfTravel.Name + ". El odometro no va para atras: una distancia negativa no es "
					+ "un tramo corto, es un hecho imposible.");

			return new AMeasuredCostPerDistance(
				from, to, firstReading, distanceTravelled, unitOfTravel,
				TheOnesIncurredAfter(firstReading, incurredInside));
		}

		/// <summary>
		/// La respuesta cuando el periodo no tiene tramo de uso que medir. El motivo lo compone el
		/// agregado que lo sabe -- la serie del odometro -- en su propia clausula, y este libro
		/// no lo escribe ni lo transporta: una segunda version de una ley que no es suya podria
		/// discrepar sin que nada lo vea.
		/// </summary>
		internal static ACostPerDistance WithNoSpanOfUse(DateTime from, DateTime to)
		{
			return new NoSpanOfUseToDivideBy(from, to);
		}

		/// <summary>
		/// El costo por distancia del periodo, como forma y valores. TOTAL: las dos situaciones
		/// dicen algo, y ninguna contesta un cero por no poder dividir.
		/// </summary>
		internal abstract ACostPerDistanceClause TheClauseItComposes();

		/// <summary>Donde empieza el periodo, como instante con su hora.</summary>
		protected string TheInstantItStartsAt()
		{
			return TheInstant(_from);
		}

		/// <summary>Donde termina el periodo, como instante con su hora.</summary>
		protected string TheInstantItEndsAt()
		{
			return TheInstant(_to);
		}

		/// <summary>Un instante dicho como instante, con su hora.</summary>
		protected static string TheInstant(DateTime instant)
		{
			return instant.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		}

		private static List<ACostEntry> TheOnesIncurredAfter(
			DateTime firstReading, List<ACostEntry> incurredInside)
		{
			List<ACostEntry> after = new List<ACostEntry>();

			foreach (ACostEntry entry in incurredInside)
				if (entry.IncurredAt > firstReading) after.Add(entry);

			return after;
		}
	}
}
