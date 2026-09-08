using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La respuesta que si tiene algo publicado detras: la clausula que el item compuso y la
	/// revision de la que salio. Es una PROYECCION del momento en que se pregunto -- se queda con
	/// su revision y no vuelve a resolver, asi que lo que a alguien le contestaron sigue diciendo
	/// lo mismo despues de que otra revision se selle.
	/// </summary>
	internal sealed class APublishedSpecification : SpecificationAnswer
	{
		private readonly PublishedRevision _revision;
		private readonly string _clause;

		/// <summary>
		/// Se llega por <see cref="SpecificationAnswer.EveryAnswerNamesItsRevision"/>, que es la
		/// que guarda que haya revision y que haya algo que decir.
		/// </summary>
		internal APublishedSpecification(PublishedRevision revision, string clause)
		{
			_revision = revision;
			_clause = clause;
		}

		/// <summary>Si.</summary>
		internal override bool IsPublished()
		{
			return true;
		}

		/// <summary>
		/// La clausula del item y, en la misma frase, la revision que la publico: quien lea la
		/// respuesta y no pregunte aparte por la procedencia igual la tiene delante.
		/// </summary>
		internal override string Describe()
		{
			return _clause + " Publicado en la revisión " + _revision.Code + ".";
		}

		/// <summary>El codigo de la revision que la publico.</summary>
		internal override string RevisionCode()
		{
			return _revision.Code;
		}
	}
}
