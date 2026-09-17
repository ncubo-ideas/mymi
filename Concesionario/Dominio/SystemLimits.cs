using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Los limites de garantia que rigen sobre algo: un plazo en meses, una distancia con su
	/// unidad, o las dos. Son los propios de un <see cref="CoveredSystem"/> -- que puede no tener
	/// -- o el sobre del vehiculo que la lectura recibe, y son la ausencia o la presencia dichas
	/// con un tipo: el cero con que el wire dice "este sistema no publico limites propios" se
	/// traduce al entrar y no viaja mas adentro.
	/// </summary>
	internal abstract class SystemLimits
	{
		/// <summary>El sistema no publico limites propios. Instancia unica, compartida.</summary>
		internal static readonly SystemLimits None = new NoSystemLimits();

		/// <summary>
		/// El sobre del vehiculo, tal como la lectura lo recibe. Tiene que traer al menos una de
		/// sus dos mitades: un sobre sin ninguna no es un sobre, y es una llamada mal hecha.
		/// </summary>
		internal static SystemLimits OfTheVehicleEnvelope(int months, int distance, DistanceUnit unit)
		{
			if (months == 0 && distance == 0)
				throw new DomainException(
					"Se pregunto por la cobertura de un vehiculo sin decir su sobre: ni plazo ni "
					+ "distancia. Sin ninguna de las dos mitades no hay contra que medir, y contestar "
					+ "igual seria decir que todo esta adentro de unos limites que nadie dijo.");

			return TheseLimits(months, distance, unit);
		}

		/// <summary>
		/// Los limites propios de un sistema, tal como el fabricante los publico. Cero en las dos
		/// mitades es como se dice que el sistema no publico limites propios, y entonces sale la
		/// ausencia: tomarlo como un limite de cero dejaria al sistema afuera el dia de la entrega.
		/// </summary>
		internal static SystemLimits OfTheSystemsOwn(int months, int distance, DistanceUnit unit)
		{
			if (months == 0 && distance == 0) return None;

			return TheseLimits(months, distance, unit);
		}

		/// <summary>Si estos limites existen. Es la pregunta TOTAL del par: nunca revienta.</summary>
		internal abstract bool Exists();

		/// <summary>
		/// Si se publico la mitad de distancia. Accesor GUARDADO: preguntarselo a unos limites que
		/// no existen es un defecto de quien llama.
		/// </summary>
		internal abstract bool HasADistanceHalf();

		/// <summary>
		/// Si la mitad de distancia esta medida en esa unidad, POR IDENTIDAD. Accesor GUARDADO, y
		/// aca no se convierte nada: dos unidades distintas no se comparan, se dejan de comparar.
		/// </summary>
		internal abstract bool MeasuresDistanceIn(DistanceUnit unit);

		/// <summary>
		/// La unidad en que se publico la mitad de distancia, para poder decir cual contra cual no
		/// calzo. Accesor GUARDADO.
		/// </summary>
		internal abstract DistanceUnit TheUnitItsDistanceIsMeasuredIn();

		/// <summary>
		/// Si el plazo ya se cumplio entre los dos instantes recibidos. Los meses se cuentan
		/// enteros: un mes empezado no es un mes cumplido. Accesor GUARDADO.
		/// </summary>
		internal abstract bool TheTimeHalfIsPast(DateTime deliveredAt, DateTime asOf);

		/// <summary>
		/// Si lo recorrido desde la lectura de entrega hasta la actual ya alcanzo la distancia
		/// publicada. Se mide desde la entrega y no desde cero. Accesor GUARDADO.
		/// </summary>
		internal abstract bool TheDistanceHalfIsPast(int deliveryReading, int currentReading);

		/// <summary>
		/// El plazo publicado, o cero donde solo se publico la mitad de distancia. Accesor GUARDADO:
		/// unos limites que no existen no tienen ninguna mitad que nombrar.
		/// </summary>
		internal abstract int TheMonthsItLimits();

		/// <summary>
		/// La distancia publicada, o cero donde solo se publico el plazo. Accesor GUARDADO por lo
		/// mismo, y el cero de aqui es el de una mitad que no se publico, nunca el de unos limites
		/// que no existen: esa ausencia es un tipo.
		/// </summary>
		internal abstract int TheDistanceItLimits();

		/// <summary>
		/// La unidad de esa distancia, por su NOMBRE, o vacia donde no hay mitad de distancia. Accesor
		/// GUARDADO por la existencia y TOTAL sobre la mitad, a diferencia de
		/// <see cref="TheUnitItsDistanceIsMeasuredIn"/>, que entrega el miembro y exige que la haya.
		/// </summary>
		internal abstract string TheNameOfTheUnitItsDistanceIsIn();

		private static SystemLimits TheseLimits(int months, int distance, DistanceUnit unit)
		{
			return new ASystemLimits(months, distance, unit);
		}
	}
}
