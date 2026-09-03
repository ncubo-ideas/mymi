using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// Lo que le queda a UNA dimension de UNA operacion al instante preguntado: el monto con su
	/// unidad, o cual de las seis razones impide decirlo. Se compone en cada lectura y nunca se
	/// guarda -- es correcto para un solo instante y esta viejo desde el siguiente.
	/// <para>
	/// La resta vive adentro, una sola vez, en <see cref="AMeasuredDimension"/>: lo recorrido y el
	/// limite ya escalado no llegan nunca los dos a un llamador. Lo que sale es este monto, que
	/// esta completo.
	/// </para>
	/// </summary>
	internal abstract class ARemainder
	{
		/// <summary>
		/// Queda algo por delante: la dimension que lo mide, el monto, su unidad y como se dice esa
		/// cantidad. Las palabras las trae la dimension, que es la que sabe si su unidad se
		/// pluraliza.
		/// </summary>
		internal static ARemainder StillAhead(
			string dimensionName, int amount, string unitName, string amountInWords)
		{
			return new ARemainderStillAhead(dimensionName, amount, unitName, amountInWords);
		}

		/// <summary>
		/// El limite ya llego. El monto es el EXCESO y nunca es negativo; cero es el limite exacto,
		/// que tiene frase propia.
		/// </summary>
		internal static ARemainder AlreadyPassed(
			string dimensionName, int overshoot, string unitName, string overshootInWords)
		{
			return new ARemainderAlreadyPassed(
				dimensionName, overshoot, unitName, overshootInWords);
		}

		/// <summary>La ley evaluada no mide esa dimension, dicha por su nombre.</summary>
		internal static ARemainder NotMeasuredByThisLaw(string dimensionName)
		{
			return new ARemainderOutsideTheLaw(dimensionName);
		}

		/// <summary>No hay de donde contar. La frase es la que la dimension ya compone.</summary>
		internal static ARemainder NoReadingToCountFrom(string said)
		{
			return new ARemainderSuspendedByAnAbsentReading(said);
		}

		/// <summary>El limite y las mediciones no calzan. La frase es la de la dimension.</summary>
		internal static ARemainder UnitsThatDoNotMatch(string said)
		{
			return new ARemainderSuspendedByAUnitMismatch(said);
		}

		/// <summary>El perfil de uso la ignora. La frase es la de la dimension.</summary>
		internal static ARemainder IgnoredByTheUsageProfile(string said)
		{
			return new ARemainderSuspendedByTheUsageProfile(said);
		}

		/// <summary>
		/// La agenda no tiene ninguna operacion activa de esa clase, que se nombra: sin el nombre
		/// las clases no agendadas dirian todas la misma frase.
		/// </summary>
		internal static ARemainder WithNoOperationScheduled(string kindName)
		{
			return new ARemainderWithNoOperationScheduled(kindName);
		}

		/// <summary>
		/// Si esta dimension existe en la ley evaluada. Ausente y suspendida no son lo mismo: lo
		/// que no existe no se puede curar.
		/// </summary>
		internal abstract bool Applies();

		/// <summary>
		/// Si se puede decir lo que queda con los hechos que hay. TOTAL, y es la pregunta que hay
		/// que hacer antes de las dos guardadas.
		/// </summary>
		internal virtual bool CanBeSaid()
		{
			return false;
		}

		/// <summary>
		/// Si el limite ya llego. TOTAL: una dimension que no se pudo medir contesta que no, y esa
		/// respuesta no es una medicion porque <see cref="CanBeSaid"/> ya dijo que no hay ninguna.
		/// </summary>
		internal virtual bool IsAlreadyPassed()
		{
			return false;
		}

		/// <summary>Suspendida porque no hay de donde contar.</summary>
		internal virtual bool SuspendedByAnAbsentReading()
		{
			return false;
		}

		/// <summary>Suspendida porque el limite y las mediciones estan en unidades distintas.</summary>
		internal virtual bool SuspendedByAUnitMismatch()
		{
			return false;
		}

		/// <summary>Suspendida porque el perfil de uso ignora los limites de esta dimension.</summary>
		internal virtual bool SuspendedByTheUsageProfile()
		{
			return false;
		}

		/// <summary>Si no hay ninguna operacion agendada de esa clase.</summary>
		internal virtual bool HasNoOperationScheduled()
		{
			return false;
		}

		/// <summary>En cual de las siete situaciones esta. TOTAL.</summary>
		internal abstract RemainderStanding Standing();

		/// <summary>
		/// Lo que queda, en palabras, con la dimension al frente. TOTAL: las siete situaciones
		/// dicen algo, y ninguna dice la frase vacia.
		/// </summary>
		internal abstract string Describe();

		/// <summary>
		/// El monto. Accesor GUARDADO: revienta si no se pudo medir, porque un cero aqui se leeria
		/// como que no falta nada.
		/// </summary>
		internal virtual int Amount()
		{
			throw new DomainException(
				"Esto no se pudo medir, asi que no tiene monto: pregunte CanBeSaid antes. Un cero "
				+ "devuelto aqui se leeria como que no falta nada, que es exactamente lo que no se "
				+ "puede decir.");
		}

		/// <summary>
		/// La unidad del monto, por su nombre. Accesor GUARDADO por lo mismo: una unidad para una
		/// mitad que nadie midio se lee como que hubo medicion.
		/// </summary>
		internal virtual string UnitName()
		{
			throw new DomainException(
				"Esto no se pudo medir, asi que no tiene unidad: pregunte CanBeSaid antes.");
		}
	}
}
