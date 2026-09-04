using System;
using System.Collections.Generic;
using System.Globalization;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// Las lecturas observadas DENTRO de un periodo, en orden de observacion, y sus dos extremos.
	/// Es una proyeccion que se recalcula entera en cada lectura y no se guarda: una lectura que
	/// llega tarde redefine periodos ya vistos, y en un registro que solo crece un tramo guardado no
	/// es un cache sino una segunda respuesta que puede no coincidir con la primera.
	/// <para>
	/// Los dos limites del periodo son INCLUSIVOS, y los extremos salen de adentro de ellos: una
	/// lectura fuera del periodo no es un extremo de este periodo aunque este en la serie.
	/// </para>
	/// </summary>
	internal sealed class ASpanOfUse
	{
		private readonly List<ASeriesRow> _rows;
		private readonly DateTime _from;
		private readonly DateTime _to;

		private ASpanOfUse(List<ASeriesRow> rows, DateTime from, DateTime to)
		{
			_rows = rows;
			_from = from;
			_to = to;
		}

		/// <summary>
		/// El tramo de ese periodo, armado con las lecturas que caen adentro. Las lecturas llegan ya
		/// en orden de observacion, que es el orden en que la agenda las guarda.
		/// </summary>
		internal static ASpanOfUse Of(
			List<OdometerReading> readingsInObservationOrder, DateTime from, DateTime to)
		{
			if (readingsInObservationOrder == null)
				throw new DomainException(
					"Un tramo se arma sobre las lecturas de un periodo: sin la lista no hay tramo, "
					+ "y una lista ausente no es un periodo vacio.");

			List<ASeriesRow> rows = new List<ASeriesRow>();

			foreach (OdometerReading reading in readingsInObservationOrder)
				rows.Add(ASeriesRow.Of(reading));

			return new ASpanOfUse(rows, from, to);
		}

		/// <summary>Cuantas lecturas cayeron dentro del periodo. TOTAL.</summary>
		internal int HowManyReadings()
		{
			return _rows.Count;
		}

		/// <summary>Las filas del periodo, en orden de observacion. TOTAL: puede venir vacia.</summary>
		internal List<ASeriesRow> Rows()
		{
			return new List<ASeriesRow>(_rows);
		}

		/// <summary>
		/// El ritmo de este tramo, o la razon con nombre que impide decirlo. TOTAL: las cinco
		/// situaciones contestan y ninguna devuelve cero por no saber.
		/// <para>
		/// El orden en que se preguntan las razones no es arbitrario. Las unidades van primero
		/// porque una mezcla de unidades hace que ni la distancia ni el tramo signifiquen nada, asi
		/// que decir "mismo segundo" sobre lecturas incomparables nombraria el problema menos
		/// grave.
		/// </para>
		/// </summary>
		internal RateAnswer TheRate()
		{
			if (_rows.Count == 0)
				return RateAnswer.NoReadingInThePeriod(AsAnInstant(_from), AsAnInstant(_to));

			if (_rows.Count == 1) return RateAnswer.OnlyOneReading(_rows[0]);

			if (ThePeriodMixesUnits())
				return RateAnswer.UnitsThatDoNotMatch(
					_rows[0].TheNameOfItsUnit(), TheRowThatIntroducedTheSecondUnit());

			ASeriesRow from = _rows[0];
			ASeriesRow to = _rows[_rows.Count - 1];

			if (to.WasObservedInTheSameSecondAs(from))
				return RateAnswer.AZeroLengthSpan(from.TheInstantItWasObserved());

			return RateAnswer.Measured(from, to);
		}

		// PREGUNTA TOTAL Y ACCESOR GUARDADO, la misma pareja que el resto del modulo usa para una
		// ausencia: preguntar primero y leer despues. Un miembro que devolviera la fila o nada
		// tendria que devolver null para decir "ninguna", y en este dominio la ausencia se dice con
		// un tipo o con un estado, nunca con null.
		private bool ThePeriodMixesUnits()
		{
			for (int row = 1; row < _rows.Count; row++)
				if (!_rows[row].SharesItsUnitWith(_rows[0])) return true;

			return false;
		}

		// LA LECTURA QUE INTRODUJO LA SEGUNDA UNIDAD, que es lo que L11 pide nombrar. La unidad del
		// periodo es la de su PRIMERA lectura: cual de las dos es "la otra" lo decide el orden de
		// observacion y no un juicio de este metodo.
		private ASeriesRow TheRowThatIntroducedTheSecondUnit()
		{
			for (int row = 1; row < _rows.Count; row++)
				if (!_rows[row].SharesItsUnitWith(_rows[0])) return _rows[row];

			throw new DomainException(
				"Este periodo no mezcla unidades, asi que ninguna lectura introdujo una segunda: "
				+ "pregunte ThePeriodMixesUnits antes.");
		}

		private static string AsAnInstant(DateTime instant)
		{
			return instant.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		}
	}
}
