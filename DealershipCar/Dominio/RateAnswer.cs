using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El ritmo de uso de UN periodo: la distancia entre sus dos extremos por unidad de tiempo, con
	/// el periodo dicho, o cual de las cuatro razones impide decirlo. Se compone en cada lectura y
	/// nunca se guarda -- una lectura que llega tarde redefine periodos ya vistos, y un ritmo
	/// guardado seria una segunda respuesta que puede no coincidir con la primera.
	/// <para>
	/// Las dos lecturas de los extremos no llegan nunca las dos sueltas a un llamador: lo que sale
	/// es esta respuesta, que ya trae los extremos Y el ritmo. Quien tuviera los dos numeros
	/// restaria y dividiria y diria un ritmo que el dominio nunca dijo.
	/// </para>
	/// </summary>
	internal abstract class RateAnswer
	{
		/// <summary>
		/// El ritmo medido entre los dos extremos del periodo. La division vive adentro, una sola
		/// vez, y sale dicha.
		/// </summary>
		internal static RateAnswer Measured(ASeriesRow from, ASeriesRow to)
		{
			return new AMeasuredRate(from, to);
		}

		/// <summary>
		/// El periodo mezcla dos unidades. Se nombran las dos y la lectura que introdujo la
		/// segunda, que es la que alguien puede ir a arreglar.
		/// </summary>
		internal static RateAnswer UnitsThatDoNotMatch(
			string unitOfThePeriod, ASeriesRow introducedTheSecond)
		{
			return new ARateSuspendedByAUnitMismatch(unitOfThePeriod, introducedTheSecond);
		}

		/// <summary>
		/// El periodo tiene una sola lectura, que se nombra con su fecha. Un punto no es un ritmo
		/// de cero.
		/// </summary>
		internal static RateAnswer OnlyOneReading(ASeriesRow theOnlyOne)
		{
			return new ARateSuspendedByASingleReading(theOnlyOne);
		}

		/// <summary>
		/// El periodo no tiene ninguna lectura. Se nombra el periodo, porque sin el todos los
		/// periodos vacios dirian la misma frase.
		/// </summary>
		internal static RateAnswer NoReadingInThePeriod(string from, string to)
		{
			return new ARateSuspendedByAnAbsentReading(from, to);
		}

		/// <summary>
		/// Los dos extremos caen en el mismo segundo, que se nombra. No hay tiempo transcurrido por
		/// el que dividir, y eso es un estado y no una division por cero.
		/// </summary>
		internal static RateAnswer AZeroLengthSpan(string theSecond)
		{
			return new ARateSuspendedByAZeroLengthSpan(theSecond);
		}

		/// <summary>
		/// Si se puede decir el ritmo con los hechos que hay. TOTAL, y es la pregunta que hay que
		/// hacer antes de las guardadas.
		/// </summary>
		internal virtual bool CanBeSaid()
		{
			return false;
		}

		/// <summary>Suspendido porque el periodo mezcla dos unidades de distancia.</summary>
		internal virtual bool SuspendedByAUnitMismatch()
		{
			return false;
		}

		/// <summary>Suspendido porque el periodo tiene una sola lectura.</summary>
		internal virtual bool SuspendedByASingleReading()
		{
			return false;
		}

		/// <summary>Suspendido porque el periodo no tiene ninguna lectura.</summary>
		internal virtual bool SuspendedByAnAbsentReading()
		{
			return false;
		}

		/// <summary>Suspendido porque los dos extremos caen en el mismo segundo.</summary>
		internal virtual bool SuspendedByAZeroLengthSpan()
		{
			return false;
		}

		/// <summary>En cual de las cinco situaciones esta. TOTAL.</summary>
		internal abstract RateStanding Standing();

		/// <summary>
		/// El ritmo en palabras, con los dos extremos y el periodo adentro. TOTAL: las cinco
		/// situaciones dicen algo, y ninguna dice la frase vacia.
		/// </summary>
		internal abstract string Describe();

		/// <summary>
		/// La distancia recorrida por dia, redondeada al entero. Accesor GUARDADO: revienta si el
		/// ritmo no se pudo decir, porque un cero aqui se leeria como que el vehiculo no se movio.
		/// Existe para que una comparacion entre dos periodos pueda preguntarlo sin volver a
		/// derivarlo, y no cruza ninguna puerta.
		/// </summary>
		internal virtual long DistancePerDay()
		{
			throw new DomainException(
				"Este periodo no tiene ritmo que decir, asi que no tiene distancia por dia: "
				+ "pregunte CanBeSaid antes. Un cero devuelto aqui se leeria como que el vehiculo "
				+ "no se movio, que es otro hecho.");
		}

		/// <summary>
		/// Como se dice el cambio de este ritmo respecto a ese otro, en una clausula. Accesor
		/// GUARDADO: los dos ritmos tienen que poder decirse. El redondeo del porcentaje vive aqui,
		/// una sola vez.
		/// </summary>
		internal virtual string HowItComparesTo(RateAnswer other)
		{
			throw new DomainException(
				"Este periodo no tiene ritmo que decir, asi que no se puede comparar: pregunte "
				+ "CanBeSaid en los dos antes.");
		}
	}
}
