using System;
using System.Collections.Generic;

namespace Concesionario.Dominio
{
	/// <summary>
	/// LO QUE EL HISTORIAL DE UNA CLASE TIENE QUE DECIR: en cual de las tres situaciones esta, de que
	/// clase se pregunto, cuantas completaciones hay y como se hizo la mas reciente.
	/// <para>
	/// Sale como FORMA mas valores ya tipados y nunca como una frase. La clase viaja por su NOMBRE de
	/// miembro y no por como se dice: el nombre es identidad y quien redacta elige la etiqueta.
	/// </para>
	/// <para>
	/// La mas reciente es la fila que el juego nombra, y nombra UNA: las demas se dicen por su propia
	/// fila, que la puerta compone con las mismas columnas. La lectura de esa fila es la mitad
	/// OPCIONAL y su ausencia es una situacion con nombre, nunca un cero.
	/// </para>
	/// </summary>
	internal sealed class ACompletionsClause
	{
		private const string NothingToSay = "";
		private const int NoNumberToSay = 0;

		private readonly string _shape;
		private readonly string _kind;
		private readonly int _count;
		private readonly string _newestAt;
		private readonly string _newestReadingShape;
		private readonly int _newestReadingValue;
		private readonly string _newestReadingUnit;

		private ACompletionsClause(
			string shape, string kind, int count, string newestAt, string newestReadingShape,
			int newestReadingValue, string newestReadingUnit)
		{
			_shape = shape;
			_kind = kind;
			_count = count;
			_newestAt = newestAt;
			_newestReadingShape = newestReadingShape;
			_newestReadingValue = newestReadingValue;
			_newestReadingUnit = newestReadingUnit;
		}

		/// <summary>
		/// El historial de una clase agendada cuya cadena dejo filas, de la mas reciente a la mas
		/// vieja. Al menos una: sin ninguna la respuesta es la ausencia con nombre.
		/// </summary>
		internal static ACompletionsClause OnRecord(string kind, List<ACompletionRow> rows)
		{
			if (rows == null || rows.Count == 0)
				throw new DomainException(
					"Se compuso un historial con completaciones y no llego ninguna fila. Que no haya "
					+ "ninguna es su propia situacion y se dice con palabras.");

			ACompletionRow newest = rows[0];

			return new ACompletionsClause(
				CompletionsShape.OnRecord, TheKind(kind), rows.Count, newest.TheInstantItWasDone(),
				newest.TheShapeOfItsReading(), newest.TheValueOfItsReading(),
				newest.TheUnitOfItsReading());
		}

		/// <summary>
		/// La clase esta agendada y nunca se completo. Es la ausencia que una visita al taller cura.
		/// </summary>
		internal static ACompletionsClause WithNoneOnRecord(string kind)
		{
			return new ACompletionsClause(
				CompletionsShape.NoneOnRecord, TheKind(kind), 0, NothingToSay,
				CompletionReadingShape.WithNoReading, NoNumberToSay, NothingToSay);
		}

		/// <summary>
		/// La clase no esta agendada. Es la ausencia que ninguna visita cura hasta que alguien siembre
		/// la operacion, y decir la otra por esta afirmaria que nunca se hizo.
		/// </summary>
		internal static ACompletionsClause OfAKindThatIsNotOnTheSchedule(string kind)
		{
			return new ACompletionsClause(
				CompletionsShape.NotOnTheSchedule, TheKind(kind), 0, NothingToSay,
				CompletionReadingShape.WithNoReading, NoNumberToSay, NothingToSay);
		}

		/// <summary>En cual de las tres situaciones esta, por su codigo. TOTAL.</summary>
		internal string TheShapeItHas()
		{
			return _shape;
		}

		/// <summary>El NOMBRE del miembro de la clase por la que se pregunto. TOTAL.</summary>
		internal string TheKindItIsAbout()
		{
			return _kind;
		}

		/// <summary>
		/// Cuantas completaciones hay, y cero en las dos ausencias. TOTAL, y cual de las tres es lo
		/// dice la forma y nunca ese cero.
		/// </summary>
		internal int HowManyCompletions()
		{
			return _count;
		}

		/// <summary>
		/// Cuando se hizo la mas reciente, como instante con su hora, o vacio donde no hay ninguna.
		/// TOTAL.
		/// </summary>
		internal string TheInstantOfTheNewest()
		{
			return _newestAt;
		}

		/// <summary>Si la mas reciente tiene lectura del odometro, por su codigo. TOTAL.</summary>
		internal string TheShapeOfTheNewestReading()
		{
			return _newestReadingShape;
		}

		/// <summary>
		/// Que leyo el odometro en la mas reciente, o cero donde no hay lectura que decir. TOTAL, y
		/// cual de los dos es lo dice la forma de la lectura.
		/// </summary>
		internal int TheValueOfTheNewestReading()
		{
			return _newestReadingValue;
		}

		/// <summary>La unidad de esa lectura, o vacia por lo mismo. TOTAL.</summary>
		internal string TheUnitOfTheNewestReading()
		{
			return _newestReadingUnit;
		}

		private static string TheKind(string kind)
		{
			if (string.IsNullOrWhiteSpace(kind))
				throw new DomainException(
					"Se compuso un historial sin decir de que clase de operacion habla. Sin la clase, "
					+ "todas las clases dirian la misma frase.");

			return kind;
		}
	}
}
