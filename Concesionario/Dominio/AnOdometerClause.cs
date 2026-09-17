using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// CONTRA QUE ODOMETRO se midio la mitad de distancia de un sobre: la lectura con su unidad y
	/// cuando se observo, y desde que lectura de entrega se conto -- o la constancia de que no hay
	/// ninguna lectura contra la cual medir.
	/// <para>
	/// Sale como FORMA mas valores ya tipados y nunca como una frase. La ausencia no da ningun
	/// numero en su lugar: un cero aqui se leeria como un odometro que alguien leyo.
	/// </para>
	/// </summary>
	internal sealed class AnOdometerClause
	{
		/// <summary>Hay lectura, y la clausula nombra las dos y sus dos instantes.</summary>
		internal const string Measured = "Measured";

		/// <summary>No hay ninguna lectura contra la cual medir, y quien trae una es el feed.</summary>
		internal const string Absent = "Absent";

		private readonly string _shape;
		private readonly int _reading;
		private readonly string _readingUnit;
		private readonly string _observedOn;
		private readonly int _deliveryReading;
		private readonly string _deliveryUnit;
		private readonly string _deliveredOn;

		private AnOdometerClause(
			string shape, int reading, string readingUnit, string observedOn,
			int deliveryReading, string deliveryUnit, string deliveredOn)
		{
			_shape = shape;
			_reading = reading;
			_readingUnit = readingUnit;
			_observedOn = observedOn;
			_deliveryReading = deliveryReading;
			_deliveryUnit = deliveryUnit;
			_deliveredOn = deliveredOn;
		}

		/// <summary>
		/// La lectura recibida y la de la entrega desde la que se cuenta, cada una con su unidad y su
		/// dia. Las dos van juntas: una sola no dice desde donde se conto.
		/// </summary>
		internal static AnOdometerClause Measuring(
			int reading, DistanceUnit unit, DateTime observedAt, Delivery delivery)
		{
			if (unit == null)
				throw new DomainException(
					"Se compuso la clausula del odometro sin decir en que unidad esta la lectura. Una "
					+ "distancia sin unidad no se puede decir ni comparar.");

			if (delivery == null || !delivery.Exists())
				throw new DomainException(
					"Se compuso la clausula del odometro sin la entrega desde la que se cuenta. Lo "
					+ "recorrido bajo garantia se mide desde la lectura de la entrega y no desde cero.");

			return new AnOdometerClause(
				Measured, reading, unit.Name, TheDay(observedAt),
				delivery.Reading, delivery.Unit.Name, TheDay(delivery.DeliveredAt));
		}

		/// <summary>No hay ninguna lectura contra la cual medir, dicho y sin numero en su lugar.</summary>
		internal static AnOdometerClause NoneToMeasureAgainst()
		{
			return new AnOdometerClause(Absent, 0, "", "", 0, "", "");
		}

		/// <summary>Que forma tiene esta clausula, por su codigo. TOTAL.</summary>
		internal string TheShapeItHas()
		{
			return _shape;
		}

		/// <summary>La lectura contra la que se midio, o cero donde no hay ninguna. TOTAL.</summary>
		internal int TheReading()
		{
			return _reading;
		}

		/// <summary>La unidad de esa lectura, por su nombre, o vacia donde no hay ninguna. TOTAL.</summary>
		internal string TheUnitTheReadingIsIn()
		{
			return _readingUnit;
		}

		/// <summary>El dia en que se observo, o vacio donde no hay lectura. TOTAL.</summary>
		internal string TheDayItWasObserved()
		{
			return _observedOn;
		}

		/// <summary>La lectura registrada en la entrega, o cero donde no hay lectura. TOTAL.</summary>
		internal int TheDeliveryReading()
		{
			return _deliveryReading;
		}

		/// <summary>La unidad de la lectura de la entrega, por su nombre. TOTAL.</summary>
		internal string TheUnitTheDeliveryReadingIsIn()
		{
			return _deliveryUnit;
		}

		/// <summary>El dia en que el vehiculo se entrego, o vacio donde no hay lectura. TOTAL.</summary>
		internal string TheDayItWasDelivered()
		{
			return _deliveredOn;
		}

		private static string TheDay(DateTime instant)
		{
			return instant.ToString(
				"yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
		}
	}
}
