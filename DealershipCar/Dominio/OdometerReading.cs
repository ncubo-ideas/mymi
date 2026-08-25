using System;

namespace DealershipCar.Dominio
{
	// POR QUE EL VALOR NO LLEVA APELLIDO. El canon (§2) prohibe "mileage", "km" y "miles" como
	// nombres de dominio, y no es purismo: en el momento en que un tipo dice "km", cualquiera
	// sabe cuanto vale en millas y la conversion se escribe sola. Un entero sin apellido, con
	// su unidad al lado, no le sugiere el factor a nadie.
	//
	// MEDIDO (T0, spike S4): el journal guarda los DateTime al SEGUNDO ENTERO. Ordenar lecturas
	// por ObservedAt -- que es lo que pide OINV-54 -- necesita entonces un desempate
	// journalizado para las que caen en el mismo segundo. Eso es de T5; queda anotado aca
	// porque este es el tipo que lleva el instante.

	/// <summary>
	/// Una lectura del odometro: el valor, la unidad en que esta expresado, el instante en que
	/// se observo y de donde vino. La unidad viaja siempre con el valor, porque es lo que
	/// permite rechazar una comparacion en vez de inventar una conversion (OINV-21, ley L7).
	/// </summary>
	internal sealed class OdometerReading
	{
		/// <summary>
		/// El valor leido, en <see cref="Unit"/>. No lleva apellido a proposito: el canon
		/// prohibe "mileage", "km" y "miles" como nombres de dominio (§2).
		/// </summary>
		internal int Value { get; }

		/// <summary>
		/// La unidad en que esta expresado el valor. Se compara por IDENTIDAD contra la unidad
		/// del limite, nunca por nombre y nunca convirtiendo.
		/// </summary>
		internal DistanceUnit Unit { get; }

		/// <summary>
		/// Cuando se OBSERVO la lectura, que no es cuando llego (OINV-53). Una lectura atrasada
		/// que encaja en la linea de tiempo es un relleno valido, no una lectura posterior.
		/// </summary>
		internal DateTime ObservedAt { get; }

		/// <summary>
		/// De donde vino la lectura, que es lo que determina cuanto se le cree (§2). Cual
		/// lectura es usable lo decide el agregado; la regla del intervalo recibe la que ya
		/// paso ese filtro.
		/// </summary>
		internal ReadingSource Source { get; }

		/// <summary>
		/// Levanta la lectura con su unidad, su instante de observacion y su fuente. Un valor
		/// negativo, una unidad ausente o una fuente ausente son defectos de quien llama: un
		/// odometro no marca menos que cero, y una lectura sin unidad es un numero que
		/// cualquiera puede comparar contra cualquier cosa.
		/// </summary>
		internal OdometerReading(int value, DistanceUnit unit, DateTime observedAt, ReadingSource source)
		{
			if (value < 0)
				throw new DomainException(
					$"Una lectura del odometro no puede ser negativa, y llego {value}.");

			if (unit == null)
				throw new DomainException(
					"Una lectura sin unidad es un numero suelto, y un numero suelto se puede "
					+ "comparar contra cualquier cosa. La unidad viaja con el valor (OINV-21).");

			if (source == null)
				throw new DomainException(
					"Una lectura sin fuente no se puede pesar: la fuente es lo que determina "
					+ "cuanta confianza merece.");

			Value = value;
			Unit = unit;
			ObservedAt = observedAt;
			Source = source;
		}

		/// <summary>
		/// Si esta lectura esta expresada en esa unidad. Comparacion por IDENTIDAD, que es la
		/// unica que un conjunto cerrado tiene (E16/E23): comparar el nombre seria igualdad de
		/// valor disfrazada de string, y es el reflejo que termina en "si son distintas,
		/// convierto".
		/// </summary>
		internal bool IsExpressedIn(DistanceUnit unit)
		{
			return Unit == unit;
		}
	}
}
