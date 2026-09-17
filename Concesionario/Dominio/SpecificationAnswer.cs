using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Cualquier respuesta que da esta especificacion: el valor publicado JUNTO CON la revision de
	/// la que salio, o la constancia de que no hay nada publicado. Una respuesta sin procedencia
	/// no existe -- es lo que separa este modulo de un PDF.
	/// <para>
	/// Sale como FORMA mas valores ya tipados y nunca como una frase: la redaccion vive donde se
	/// elige el idioma, y el dominio no elige uno.
	/// </para>
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
		/// Si hay algo publicado, por su codigo. Total, y es lo mismo que <see cref="IsPublished"/>
		/// dicho como nombre: el booleano lo pregunta quien decide, el codigo lo lleva quien redacta.
		/// </summary>
		internal abstract string TheShapeItHas();

		/// <summary>
		/// Lo que el item mismo tiene que decir, sin la procedencia. Total: lo no publicado contesta
		/// vacio, porque no hay ningun item que haya dicho nada.
		/// </summary>
		internal abstract string TheClauseItCarries();

		/// <summary>
		/// De que se pregunto, para que la ausencia nombre lo que falta. Total: lo publicado contesta
		/// vacio, porque ahi el sujeto ya esta dentro de la clausula.
		/// </summary>
		internal abstract string TheSubjectItIsAbout();

		/// <summary>
		/// El codigo de la revision de la que salio. Total: lo no publicado contesta vacio, y eso
		/// no se confunde con una revision porque una revision publicada nunca tiene codigo en
		/// blanco. La revision misma no se entrega.
		/// </summary>
		internal abstract string RevisionCode();
	}
}
