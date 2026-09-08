using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// El sobre que la agencia no publico. No es un sobre de cero meses y cero distancia: es la
	/// constancia de que no hay ninguno, y por eso la respuesta que sale de aqui esta DECIDIDA --
	/// el registro lo sabe con certeza -- en vez de suspendida.
	/// </summary>
	internal sealed class NoWarrantyEnvelope : WarrantyEnvelope
	{
		/// <summary>Se llega por <see cref="WarrantyEnvelope.None"/>.</summary>
		internal NoWarrantyEnvelope()
		{
		}

		/// <summary>No.</summary>
		internal override bool Exists()
		{
			return false;
		}

		/// <summary>Revienta: no hay sobre, asi que tampoco plazo.</summary>
		internal override int Months
		{
			get { throw NothingWasPublished(); }
		}

		/// <summary>Revienta: no hay sobre, asi que tampoco distancia.</summary>
		internal override int Distance
		{
			get { throw NothingWasPublished(); }
		}

		/// <summary>Revienta: sin distancia publicada no hay unidad que nombrar.</summary>
		internal override DistanceUnit TheUnitItsDistanceIsMeasuredIn
		{
			get { throw NothingWasPublished(); }
		}

		/// <summary>Revienta: sin unidad no hay codigo con que cruzarla.</summary>
		internal override DistanceUnitCode TheCodeOfItsDistanceUnit
		{
			get { throw NothingWasPublished(); }
		}

		/// <summary>Revienta: nadie publico nada, asi que no hay instante de publicacion.</summary>
		internal override DateTime PublishedAt
		{
			get { throw NothingWasPublished(); }
		}

		/// <summary>
		/// Revienta, y no contesta que no. Un "no esta en esa unidad" mandaria a nombrar un
		/// desajuste de unidades donde lo cierto es que no hay nada publicado, que es una
		/// respuesta distinta y con su propio nombre.
		/// </summary>
		internal override bool MeasuresDistanceIn(DistanceUnit unit)
		{
			throw NothingWasPublished();
		}

		/// <summary>Revienta: sin plazo publicado no hay meses que queden.</summary>
		internal override int MonthsLeftAsOf(DateTime deliveredAt, DateTime asOf)
		{
			throw NothingWasPublished();
		}

		/// <summary>
		/// Revienta: sin distancia publicada no hay distancia que quede. Un cero aqui dejaria al
		/// vehiculo alcanzado el mismo dia de la entrega, y el limite entero lo dejaria adentro para
		/// siempre.
		/// </summary>
		internal override int DistanceLeftFrom(int deliveryReading, int currentReading)
		{
			throw NothingWasPublished();
		}

		/// <summary>
		/// La UNICA que contesta, y contesta con palabras. Que no hayan publicado sobre es la
		/// respuesta, y es la que puede citar quien no tiene ni un dato de ningun vehiculo: un
		/// throw aqui haria que no tener el odometro de alguien volviera incitable lo que la
		/// agencia publico -- o no publico -- para todos.
		/// </summary>
		internal override string Describe()
		{
			return "El concesionario no ha publicado ningún sobre de garantía para este vehículo, "
				+ "así que aquí no hay contra qué medir -- que no es lo mismo que este vehículo no "
				+ "tenga garantía.";
		}

		private static DomainException NothingWasPublished()
		{
			return new DomainException(
				"La agencia no publico ningun sobre de garantia para este vehiculo: hay que "
				+ "preguntar primero si lo publico. Un limite inventado aqui pondria al vehiculo "
				+ "adentro o afuera de una garantia que nadie escribio.");
		}
	}
}
