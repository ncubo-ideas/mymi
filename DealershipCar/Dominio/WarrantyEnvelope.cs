using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El sobre de garantia que la agencia publico para un vehiculo -- un plazo en meses, una
	/// distancia con su unidad, y cuando se publico -- o la constancia de que no publico ninguno.
	/// Guarda LIMITES y nunca lo que queda: lo que queda es correcto para un solo instante y esta
	/// viejo desde el siguiente, asi que se calcula cada vez que alguien pregunta.
	/// <para>
	/// No es <see cref="WarrantyCoverage"/>, que es el alcance que el FABRICANTE publico sobre un
	/// modelo. Este es de este carro y lo dice la agencia.
	/// </para>
	/// </summary>
	internal abstract class WarrantyEnvelope
	{
		/// <summary>La agencia no publico sobre. Instancia unica, compartida.</summary>
		internal static readonly WarrantyEnvelope None = new NoWarrantyEnvelope();

		/// <summary>
		/// El sobre publicado, con sus DOS mitades y la unidad de la de distancia. Que las dos
		/// sean obligatorias es la ley: un sobre es "36 meses o 60000", y una mitad en cero se
		/// diria en la frase como un limite alcanzado el mismo dia de la entrega. Una mitad
		/// ausente o negativa, o una distancia sin unidad, son defectos de quien publica.
		/// </summary>
		internal static WarrantyEnvelope PublishedAs(
			int months, int distance, DistanceUnit unit, DateTime publishedAt)
		{
			if (months <= 0 || distance <= 0)
				throw new DomainException(
					$"Se publico un sobre de {months} meses y {distance} de distancia. El sobre de un "
					+ "vehiculo lleva sus dos mitades siempre: una en cero se leeria como un limite "
					+ "ya cumplido, y una en negativo como uno que no se puede cumplir nunca.");

			if (unit == null)
				throw new DomainException(
					"Se publico un sobre con distancia y sin decir en que unidad esta. Una distancia "
					+ "sin unidad no se puede comparar con el odometro de nadie, y aca las unidades "
					+ "no se convierten entre si.");

			return new AWarrantyEnvelope(months, distance, unit, publishedAt);
		}

		/// <summary>Si hay sobre publicado. Es la pregunta TOTAL del par: nunca revienta.</summary>
		internal abstract bool Exists();

		/// <summary>El plazo en meses. Revienta si no hay sobre publicado.</summary>
		internal abstract int Months { get; }

		/// <summary>
		/// La distancia del sobre, en <see cref="TheUnitItsDistanceIsMeasuredIn"/>. Revienta si no
		/// hay sobre publicado.
		/// </summary>
		internal abstract int Distance { get; }

		/// <summary>
		/// La unidad en que se publico la distancia, para poder decir cual contra cual no calzo.
		/// Revienta si no hay sobre publicado.
		/// </summary>
		internal abstract DistanceUnit TheUnitItsDistanceIsMeasuredIn { get; }

		/// <summary>
		/// La misma unidad dicha con su codigo, para que un script pueda pasarla como argumento al
		/// metodo de otro agregado sin que nadie de afuera la vea. Revienta si no hay sobre
		/// publicado.
		/// </summary>
		internal abstract DistanceUnitCode TheCodeOfItsDistanceUnit { get; }

		/// <summary>
		/// Cuando la agencia publico este sobre. Revienta si no hay sobre publicado. Todo sobre
		/// viejo tiene que poder verse viejo, y sin este instante no se puede.
		/// </summary>
		internal abstract DateTime PublishedAt { get; }

		/// <summary>
		/// Si la distancia del sobre esta medida en esa unidad, POR IDENTIDAD. Revienta si no hay
		/// sobre publicado: un "no" benigno mandaria a nombrar un desajuste de unidades donde lo
		/// cierto es que no hay nada publicado.
		/// </summary>
		internal abstract bool MeasuresDistanceIn(DistanceUnit unit);

		/// <summary>
		/// El sobre en palabras: las dos mitades, su unidad y cuando se publico, sin ningun hecho
		/// del vehiculo adentro. TOTAL, tambien cuando no hay sobre -- que no hayan publicado
		/// ninguno tambien es una respuesta, y es la que puede citar quien no tiene ni un dato de
		/// ningun carro.
		/// </summary>
		internal abstract string Describe();
	}
}
