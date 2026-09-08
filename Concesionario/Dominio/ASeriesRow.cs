using System;
using System.Globalization;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Una lectura del odometro dicha entera: el valor, su unidad, cuando se observo y de que fuente
	/// vino. Es la unidad del registro de la serie, se compone en cada lectura y no se guarda en
	/// ninguna parte.
	/// <para>
	/// La fila es COMPLETA: nada en ella invita a recombinarla, porque lo que se compondria con ella
	/// -- el ritmo -- ya viene compuesto adentro y llega en la misma respuesta. Nada se pesa, se
	/// promedia ni se descarta por su fuente: decirla es la obligacion entera.
	/// </para>
	/// </summary>
	internal sealed class ASeriesRow
	{
		private readonly OdometerReading _reading;

		private ASeriesRow(OdometerReading reading)
		{
			if (reading == null)
				throw new DomainException(
					"Una fila de la serie sin lectura no dice nada: la fila ES la lectura dicha "
					+ "entera.");

			_reading = reading;
		}

		/// <summary>La fila de esa lectura observada.</summary>
		internal static ASeriesRow Of(OdometerReading reading)
		{
			return new ASeriesRow(reading);
		}

		/// <summary>El valor leido, en la unidad que dice <see cref="TheNameOfItsUnit"/>.</summary>
		internal int TheValueItRead()
		{
			return _reading.Value;
		}

		/// <summary>
		/// La unidad del valor, por su nombre de mundo real. Es el nombre y no el codigo: proyectar
		/// un codigo dejaria el mapeo del conjunto cerrado en manos de quien lo muestra.
		/// </summary>
		internal string TheNameOfItsUnit()
		{
			return _reading.Unit.Name;
		}

		/// <summary>
		/// Cuando se observo, dicho como INSTANTE con su hora y no como un dia de calendario: de que
		/// dia local es un instante necesitaria una zona que este modulo no tiene.
		/// </summary>
		internal string TheInstantItWasObserved()
		{
			return AsAnInstant(_reading.ObservedAt);
		}

		/// <summary>
		/// Esa misma unidad, dicha con su CODIGO, para que un script pueda pasarla como argumento al
		/// metodo de otro agregado. No sale hacia ninguna vista: el valor va directo a otro metodo de
		/// dominio adentro del mismo script.
		/// </summary>
		internal DistanceUnitCode TheCodeOfItsUnit()
		{
			return _reading.TheCodeOfItsUnit;
		}

		/// <summary>
		/// Ese mismo instante, como INSTANTE y no como su forma dicha, por la misma razon que el
		/// codigo de la unidad: otro agregado lo recibe como argumento adentro del mismo script y
		/// filtra con el.
		/// </summary>
		internal DateTime WhenItWasObserved()
		{
			return _reading.ObservedAt;
		}

		/// <summary>
		/// El NOMBRE del miembro de la fuente, que es la mitad direccionable: quien lea la columna
		/// puede volver a preguntar por esa fuente con el.
		/// </summary>
		internal string TheNameOfItsSource()
		{
			return _reading.Source.Name;
		}

		/// <summary>Si esta fila esta expresada en la misma unidad que esa otra. Por IDENTIDAD.</summary>
		internal bool SharesItsUnitWith(ASeriesRow other)
		{
			if (other == null)
				throw new DomainException(
					"No hay con que comparar la unidad de esta fila: llego una fila ausente.");

			return _reading.IsExpressedIn(other._reading.Unit);
		}

		/// <summary>Si esta fila se observo en el mismo segundo entero que esa otra.</summary>
		internal bool WasObservedInTheSameSecondAs(ASeriesRow other)
		{
			if (other == null)
				throw new DomainException(
					"No hay con que comparar el instante de esta fila: llego una fila ausente.");

			return TheWholeSecondOf(_reading.ObservedAt)
				== TheWholeSecondOf(other._reading.ObservedAt);
		}

		/// <summary>
		/// La distancia entre esta fila y esa otra, en la unidad que las dos comparten. Revienta si
		/// no la comparten: restar dos escalas distintas no da una distancia.
		/// </summary>
		internal int DistanceFrom(ASeriesRow earlier)
		{
			if (!SharesItsUnitWith(earlier))
				throw new DomainException(
					"Estas dos lecturas no estan en la misma unidad, asi que la distancia entre "
					+ "ellas no es una distancia. Las unidades nunca se convierten.");

			return _reading.Value - earlier._reading.Value;
		}

		/// <summary>
		/// Los dias transcurridos entre esa otra fila y esta, exactos y sin redondear. Revienta si
		/// las dos caen en el mismo segundo: ahi no hay tiempo transcurrido.
		/// </summary>
		internal decimal DaysFrom(ASeriesRow earlier)
		{
			if (WasObservedInTheSameSecondAs(earlier))
				throw new DomainException(
					"Estas dos lecturas se observaron en el mismo segundo, asi que no hay tiempo "
					+ "transcurrido entre ellas por el que dividir.");

			long ticks = TheWholeSecondOf(_reading.ObservedAt).Ticks
				- TheWholeSecondOf(earlier._reading.ObservedAt).Ticks;

			return (decimal)ticks / TimeSpan.TicksPerDay;
		}

		/// <summary>
		/// Los segundos enteros transcurridos entre esa otra fila y esta. Es la forma en que se dice
		/// un tramo mas corto que un dia, que es la unidad por la que el ritmo se dice.
		/// </summary>
		internal long SecondsFrom(ASeriesRow earlier)
		{
			if (WasObservedInTheSameSecondAs(earlier))
				throw new DomainException(
					"Estas dos lecturas se observaron en el mismo segundo, asi que no hay tiempo "
					+ "transcurrido entre ellas.");

			return (TheWholeSecondOf(_reading.ObservedAt).Ticks
				- TheWholeSecondOf(earlier._reading.ObservedAt).Ticks) / TimeSpan.TicksPerSecond;
		}

		/// <summary>
		/// La lectura en una frase, que es lo que hace la fila autodescriptiva: el valor con su
		/// unidad, cuando se observo, y de donde vino dicho en palabras.
		/// </summary>
		internal string Describe()
		{
			return TheValueItRead() + " " + TheNameOfItsUnit() + " el " + TheInstantItWasObserved()
				+ ", " + _reading.Source.HowItIsSaid;
		}

		/// <summary>El valor con su unidad, que es la mitad que una frase de ritmo reusa.</summary>
		internal string TheReadingItSays()
		{
			return TheValueItRead() + " " + TheNameOfItsUnit();
		}

		private static DateTime TheWholeSecondOf(DateTime instant)
		{
			return new DateTime(instant.Ticks - (instant.Ticks % TimeSpan.TicksPerSecond));
		}

		private static string AsAnInstant(DateTime instant)
		{
			return instant.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		}
	}
}
