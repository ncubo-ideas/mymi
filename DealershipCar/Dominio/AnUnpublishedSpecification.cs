using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La respuesta cuando no hay nada publicado sobre lo que se pregunto. Dice que no lo hay, con
	/// palabras y nombrando lo que se pregunto: nunca un cero, nunca una cadena vacia, nunca el
	/// valor de otra clase. La ausencia tiene una sola clase en este modulo, a proposito -- lo que
	/// nadie publico y lo que solo esta dentro de una revision sin sellar contestan lo mismo,
	/// porque distinguirlos contaria que hay una revision en preparacion.
	/// </summary>
	internal sealed class AnUnpublishedSpecification : SpecificationAnswer
	{
		private readonly string _subject;

		/// <summary>
		/// Se llega por <see cref="ModelSpecification.UnpublishedIsAnAnswer"/>, el unico sitio
		/// donde esta especificacion compone una ausencia. El sujeto es lo que se pregunto: sin el,
		/// la misma frase serviria para todo y no diria de que habla.
		/// </summary>
		internal AnUnpublishedSpecification(string subject)
		{
			if (string.IsNullOrWhiteSpace(subject))
				throw new DomainException(
					"Se compuso una respuesta de 'no publicado' sin decir de que. Una ausencia que "
					+ "no nombra lo que falta no se distingue de cualquier otra.");

			_subject = subject;
		}

		/// <summary>No.</summary>
		internal override bool IsPublished()
		{
			return false;
		}

		/// <summary>
		/// Lo dice con todas las letras y sin ningun numero: un cero adentro de la frase se leeria
		/// como un dato publicado, que es la forma en que un default entra sin que nadie lo
		/// escriba.
		/// </summary>
		internal override string Describe()
		{
			return _subject
				+ ": not published for this model, so there is nothing to quote and no revision to "
				+ "name.";
		}

		/// <summary>
		/// Vacio: no hay revision que nombrar. Ninguna revision publicada tiene el codigo en
		/// blanco, asi que esto no se puede confundir con una.
		/// </summary>
		internal override string RevisionCode()
		{
			return string.Empty;
		}
	}
}
