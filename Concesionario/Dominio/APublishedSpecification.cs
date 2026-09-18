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

		/// <summary>Publicada.</summary>
		internal override string TheShapeItHas()
		{
			return SpecificationPublication.Published;
		}

		/// <summary>
		/// La clausula del item, sola. La revision que la publico viaja al lado y no dentro: quien
		/// redacte pone las dos en la misma frase, que es donde la persona las necesita juntas.
		/// </summary>
		internal override string TheClauseItCarries()
		{
			return _clause;
		}

		/// <summary>Vacio: el sujeto ya esta dentro de la clausula del item.</summary>
		internal override string TheSubjectItIsAbout()
		{
			return string.Empty;
		}

		/// <summary>El codigo de la revision que la publico.</summary>
		internal override string RevisionCode()
		{
			return _revision.Code;
		}
	}
}
