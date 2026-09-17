using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Lo que le queda a UNA dimension de UNA operacion al instante preguntado: el monto con su
	/// unidad, o cual de las seis razones impide decirlo. Se compone en cada lectura y nunca se
	/// guarda -- es correcto para un solo instante y esta viejo desde el siguiente.
	/// <para>
	/// Sale como SITUACION mas valores ya tipados y nunca como una frase: la redaccion vive donde se
	/// elige el idioma, y el dominio no elige uno.
	/// </para>
	/// <para>
	/// La resta vive adentro, una sola vez, en <see cref="AMeasuredDimension"/>: lo recorrido y el
	/// limite ya escalado no llegan nunca los dos a un llamador. Lo que sale es este monto, que
	/// esta completo.
	/// </para>
	/// </summary>
	internal abstract class ARemainder
	{
		/// <summary>
		/// Queda algo por delante: la dimension que lo mide por su CODIGO, el monto y la unidad en
		/// que esta dicho. Ninguna palabra: la dimension aporta los valores y la redaccion es de
		/// quien la muestra.
		/// </summary>
		internal static ARemainder StillAhead(string dimension, int amount, string unitName)
		{
			return new ARemainderStillAhead(dimension, amount, unitName);
		}

		/// <summary>
		/// El limite ya llego. El monto es el EXCESO y nunca es negativo; cero es el limite exacto,
		/// que tiene clausula propia.
		/// </summary>
		internal static ARemainder AlreadyPassed(string dimension, int overshoot, string unitName)
		{
			return new ARemainderAlreadyPassed(dimension, overshoot, unitName);
		}

		/// <summary>La ley evaluada no mide esa dimension, dicha por su codigo.</summary>
		internal static ARemainder NotMeasuredByThisLaw(string dimension)
		{
			return new ARemainderOutsideTheLaw(dimension);
		}

		/// <summary>No hay de donde contar.</summary>
		internal static ARemainder NoReadingToCountFrom()
		{
			return new ARemainderSuspendedByAnAbsentReading();
		}

		/// <summary>
		/// El limite y las mediciones no calzan, y se nombran las dos unidades: la del limite y la
		/// que no coincidio.
		/// </summary>
		internal static ARemainder UnitsThatDoNotMatch(string limitUnit, string measuredUnit)
		{
			return new ARemainderSuspendedByAUnitMismatch(limitUnit, measuredUnit);
		}

		/// <summary>El perfil de uso la ignora, nombrado por su codigo.</summary>
		internal static ARemainder IgnoredByTheUsageProfile(string usageProfile)
		{
			return new ARemainderSuspendedByTheUsageProfile(usageProfile);
		}

		/// <summary>
		/// La agenda no tiene ninguna operacion activa de esa clase, que se nombra por su codigo:
		/// sin el nombre las clases no agendadas dirian todas la misma clausula.
		/// </summary>
		internal static ARemainder WithNoOperationScheduled(string kind)
		{
			return new ARemainderWithNoOperationScheduled(kind);
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
		/// La dimension que esta mitad mide, por su CODIGO. TOTAL, y vacia donde la situacion no
		/// habla de una dimension.
		/// </summary>
		internal virtual string TheDimensionItMeasures()
		{
			return "";
		}

		/// <summary>
		/// El monto que falta o que se paso, en UNA fila, o en CERO filas donde no hay ninguno.
		/// TOTAL, y la ausencia nunca se dice con un cero: un cero se leeria como que no falta nada.
		/// </summary>
		internal virtual int[] TheAmountItCounts()
		{
			return new int[0];
		}

		/// <summary>
		/// La unidad en que esta dicho este monto, por su nombre, o vacia donde la situacion no
		/// nombra ninguna. TOTAL, a diferencia de <see cref="UnitName"/>, que es la lectura guardada
		/// de la misma unidad.
		/// </summary>
		internal virtual string TheUnitItIsExpressedIn()
		{
			return "";
		}

		/// <summary>
		/// La unidad en que estan las mediciones cuando no coincide con la del limite, o vacia donde
		/// coinciden. TOTAL.
		/// </summary>
		internal virtual string TheUnitTheMeasurementsAreIn()
		{
			return "";
		}

		/// <summary>
		/// El perfil de uso que ignora esta dimension, por su codigo, o vacio donde no la ignora
		/// ninguno. TOTAL.
		/// </summary>
		internal virtual string TheUsageProfileThatIgnoresIt()
		{
			return "";
		}

		/// <summary>
		/// La clase que no tiene ninguna operacion agendada, por su codigo, o vacia donde si la
		/// tiene. TOTAL.
		/// </summary>
		internal virtual string TheKindWithNoOperationScheduled()
		{
			return "";
		}

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
