using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// Cualquier respuesta que da esta especificacion: el valor publicado JUNTO CON la revision de
	/// la que salio, o la constancia de que no hay nada publicado. Una respuesta sin procedencia
	/// no existe -- es lo que separa este modulo de un PDF.
	/// </summary>
	internal abstract class SpecificationAnswer
	{
		/// <summary>
		/// La unica forma de fabricar una respuesta publicada, y esta GUARDADA a proposito: no hay
		/// camino a un valor que no pase por nombrar la revision que lo publico. Que esa revision
		/// este sellada lo decide el filtro del agregado, no esta fabrica: aca se exige la
		/// procedencia, alla se exige que la revision conteste.
		/// </summary>
		internal static SpecificationAnswer EveryAnswerNamesItsRevision(
			PublishedRevision revision, string clause)
		{
			if (revision == null)
				throw new DomainException(
					"Se quiso componer una respuesta publicada sin nombrar la revision de la que "
					+ "sale. Una respuesta sin procedencia es un rumor: quien la reciba no puede "
					+ "saber si sigue vigente ni contra que compararla cuando cambie.");

			if (string.IsNullOrWhiteSpace(clause))
				throw new DomainException(
					$"Se quiso componer una respuesta de la revision '{revision.Code}' sin nada que "
					+ "decir. Una respuesta vacia pero firmada es peor que no tener respuesta: "
					+ "parece publicada.");

			return new APublishedSpecification(revision, clause);
		}

		/// <summary>Si hay algo publicado que contestar. Total: contesta siempre.</summary>
		internal abstract bool IsPublished();

		/// <summary>
		/// La respuesta en una frase, con su procedencia adentro. Total: la ausencia tambien se
		/// dice con palabras, y nunca con un cero ni con una cadena vacia.
		/// </summary>
		internal abstract string Describe();

		/// <summary>
		/// El codigo de la revision de la que salio. Total: lo no publicado contesta vacio, y eso
		/// no se confunde con una revision porque una revision publicada nunca tiene codigo en
		/// blanco. La revision misma no se entrega.
		/// </summary>
		internal abstract string RevisionCode();
	}
}
