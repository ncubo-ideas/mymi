using System;
using System.Globalization;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// Una cosa que se le hizo al vehiculo: que clase de operacion fue, cuando, y con que lectura
	/// del odometro si el ancla de la que sale tiene una. Es la unidad del historial, se compone
	/// caminando la cadena en cada lectura y no se guarda en ninguna parte.
	/// <para>
	/// La lectura es la mitad OPCIONAL y la trae el par de <see cref="AnchorReading"/>: una
	/// completacion de una ley que no mide distancia dice que no hay lectura, nunca cero.
	/// </para>
	/// </summary>
	internal sealed class ACompletionRow
	{
		private readonly MaintenanceOperationKind _kind;
		private readonly DateTime _completedAt;
		private readonly AnchorReading _reading;

		private ACompletionRow(
			MaintenanceOperationKind kind, DateTime completedAt, AnchorReading reading)
		{
			if (kind == null)
				throw new DomainException(
					"Una fila del historial sin clase de operacion no dice que se hizo, y una fila "
					+ "que no dice que se hizo no es un registro menos preciso: no es ninguno.");

			_kind = kind;
			_completedAt = completedAt;
			_reading = reading;
		}

		/// <summary>
		/// La completacion de una operacion cuya ley mide distancia: el ancla de su sucesora trae la
		/// lectura y su unidad. Las guardas de la lectura viven en <see cref="AnchorReading.Of"/>.
		/// </summary>
		internal static ACompletionRow WithAReading(
			MaintenanceOperationKind kind, DateTime completedAt, int reading, DistanceUnit unit)
		{
			return new ACompletionRow(kind, completedAt, AnchorReading.Of(reading, unit));
		}

		/// <summary>
		/// La completacion cuya ancla no tiene lectura, porque la ley no mide distancia y descarta la
		/// que se recibio. Son DOS fabricas y no una con un booleano: cual se llama es lo que dice si
		/// hay lectura, asi que la ausencia no puede quedar en desacuerdo con el numero.
		/// </summary>
		internal static ACompletionRow WithNoReading(
			MaintenanceOperationKind kind, DateTime completedAt)
		{
			return new ACompletionRow(kind, completedAt, AnchorReading.None);
		}

		/// <summary>
		/// Si esta completacion es mas reciente que esa otra. Es una operacion CON NOMBRE y no la
		/// entrega del instante: quien ordena las filas las compara sin recibir el numero.
		/// </summary>
		internal bool IsNewerThan(ACompletionRow other)
		{
			if (other == null)
				throw new DomainException(
					"No hay con que comparar esta completacion: llego una fila ausente. Comparar "
					+ "contra nada dejaria el orden decidido por el hueco.");

			return _completedAt > other._completedAt;
		}

		/// <summary>
		/// El NOMBRE del miembro de la clase que se hizo, que es la mitad direccionable: quien lea la
		/// columna puede volver a preguntar por esa clase con el.
		/// </summary>
		internal string TheNameOfItsKind()
		{
			return _kind.Name;
		}

		/// <summary>
		/// Cuando se hizo, dicho como INSTANTE con su hora y no como un dia de calendario: de que dia
		/// local es un instante necesitaria una zona que este modulo no tiene.
		/// </summary>
		internal string TheInstantItWasDone()
		{
			return _completedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// La lectura del odometro con su unidad, o la constancia en palabras de que el ancla no
		/// tiene ninguna. TOTAL: nunca cero y nunca la celda vacia, que se leerian las dos como que
		/// hubo medicion.
		/// </summary>
		internal string TheReadingItWasDoneAt()
		{
			if (!_reading.Exists()) return "sin lectura registrada";

			return _reading.Value + " " + _reading.Unit.Name;
		}

		/// <summary>
		/// Como se hizo, sin nombrar la clase: el instante y la lectura o su ausencia. Es la mitad
		/// que el juego entero reusa para hablar de la mas reciente sin repetir la clase.
		/// </summary>
		internal string HowItWasDone()
		{
			if (!_reading.Exists())
				return "se hizo el " + TheInstantItWasDone()
					+ ", sin lectura del odómetro registrada";

			return "se hizo el " + TheInstantItWasDone() + " con el odómetro en "
				+ TheReadingItWasDoneAt();
		}

		/// <summary>
		/// La completacion en una frase, que es lo que hace la fila autodescriptiva: la clase como se
		/// dice, cuando, y la lectura o su ausencia.
		/// </summary>
		internal string Describe()
		{
			return _kind.HowItIsSaid + ": " + HowItWasDone() + ".";
		}
	}
}
