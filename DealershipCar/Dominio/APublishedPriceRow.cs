using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// Un precio publicado: la operacion, el hito, la frase que el dominio dice y la revision de la
	/// que salio. Es la unidad de la enumeracion de precios, se compone en cada lectura y no se
	/// guarda en ninguna parte.
	/// <para>
	/// Nace de la RESPUESTA del resolvedor y no de la revision que se estaba recorriendo, asi que la
	/// procedencia que nombra es la de la revision que rige. Una respuesta sin nada publicado no da
	/// una fila.
	/// </para>
	/// </summary>
	internal sealed class APublishedPriceRow
	{
		private readonly APublishedPriceKey _key;
		private readonly string _says;
		private readonly string _revision;

		private APublishedPriceRow(APublishedPriceKey key, string says, string revision)
		{
			_key = key;
			_says = says;
			_revision = revision;
		}

		/// <summary>
		/// La fila del precio que esa clave nombra, compuesta de la respuesta del resolvedor. Se
		/// niega a nacer de una ausencia: una fila con la procedencia en blanco no se distingue de
		/// un precio que nadie publico, y es lo que haria falsa la ley de que toda fila nombra su
		/// revision.
		/// </summary>
		internal static APublishedPriceRow Of(APublishedPriceKey key, SpecificationAnswer answer)
		{
			if (key == null)
				throw new DomainException(
					"Se compuso una fila de precio sin la clave que nombra. Una fila que no dice de "
					+ "que operacion y de que hito es no es un registro menos preciso: no es ninguno.");

			if (answer == null)
				throw new DomainException(
					$"Se compuso la fila del precio de {key.TheNameOfItsOperation()} sin la respuesta "
					+ "de la que sale. Sin ella no hay ni frase ni procedencia que poner.");

			if (!answer.IsPublished())
				throw new DomainException(
					$"Se compuso la fila del precio de {key.TheNameOfItsOperation()} en el hito "
					+ $"{key.TheMilestoneAsPublished()} desde una respuesta sin nada publicado, que no "
					+ "nombra ninguna revision. Una fila con la procedencia en blanco parece un precio "
					+ "publicado y no lo es.");

			return new APublishedPriceRow(key, answer.Describe(), answer.RevisionCode());
		}

		/// <summary>
		/// El NOMBRE del miembro de la operacion, que es la mitad direccionable de la fila.
		/// </summary>
		internal string TheNameOfItsOperation()
		{
			return _key.TheNameOfItsOperation();
		}

		/// <summary>El hito al que se publico, dicho con su unidad.</summary>
		internal string TheMilestoneItWasPublishedAt()
		{
			return _key.TheMilestoneAsPublished();
		}

		/// <summary>El codigo de la revision que rige para esta clave.</summary>
		internal string TheRevisionItCameFrom()
		{
			return _revision;
		}

		/// <summary>
		/// El precio en una frase, que es lo que hace la fila autodescriptiva: la operacion como se
		/// dice, el hito, el monto con su moneda, la vigencia, si rige en el instante preguntado, y
		/// la revision.
		/// </summary>
		internal string Describe()
		{
			return _says;
		}
	}
}
