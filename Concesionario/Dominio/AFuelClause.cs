using System;
using System.Collections.Generic;

namespace Concesionario.Dominio
{
	/// <summary>
	/// LO QUE EL COMBUSTIBLE PUBLICADO TIENE QUE DECIR: todos los grados que la revision publico, cada
	/// uno con su postura, en el orden en que se publicaron, y de que revision salen.
	/// <para>
	/// Sale como FORMA mas valores ya tipados y nunca como una frase. Los grados y sus posturas viajan
	/// en columnas paralelas: el texto del grado es del fabricante y no se traduce, la postura es un
	/// conjunto cerrado y viaja por el NOMBRE de su miembro.
	/// </para>
	/// </summary>
	internal sealed class AFuelClause
	{
		private static readonly string[] Nothing = new string[0];

		private readonly string _publication;
		private readonly string _revisionCode;
		private readonly string[] _grades;
		private readonly string[] _standings;

		private AFuelClause(
			string publication, string revisionCode, string[] grades, string[] standings)
		{
			_publication = publication;
			_revisionCode = revisionCode;
			_grades = grades;
			_standings = standings;
		}

		/// <summary>
		/// La clausula cuando ninguna revision sellada publico ningun grado. No lleva nada que nombrar:
		/// el combustible no tiene clave, asi que lo que falta es el item entero.
		/// </summary>
		internal static AFuelClause OfNothingPublished()
		{
			return new AFuelClause(
				SpecificationPublication.NotPublished, "", Nothing, Nothing);
		}

		/// <summary>
		/// La clausula de los grados que si se publicaron, con la revision de la que salen. Se niega a
		/// nacer sin ninguno: una lista vacia firmada se leeria como que el fabricante dijo que no hay
		/// ningun combustible prohibido, que es la lectura opuesta a la verdadera.
		/// </summary>
		internal static AFuelClause Published(string revisionCode, List<FuelGrade> grades)
		{
			if (string.IsNullOrWhiteSpace(revisionCode))
				throw new DomainException(
					"Se compuso la clausula del combustible sin nombrar la revision de la que sale. Una "
					+ "respuesta sin procedencia es un rumor: quien la reciba no puede saber si sigue "
					+ "vigente ni contra que compararla cuando cambie.");

			if (grades == null || grades.Count == 0)
				throw new DomainException(
					$"Se compuso la clausula del combustible de la revision '{revisionCode}' sin ningun "
					+ "grado adentro. Hay que preguntar primero si entro alguno: una lista vacia "
					+ "firmada parece publicada y diria que no hay ningun grado prohibido.");

			string[] texts = new string[grades.Count];
			string[] standings = new string[grades.Count];

			for (int each = 0; each < grades.Count; each++)
			{
				texts[each] = grades[each].Text;
				standings[each] = grades[each].TheStandingItWasPublishedWith();
			}

			return new AFuelClause(
				SpecificationPublication.Published, revisionCode, texts, standings);
		}

		/// <summary>Si hay algo publicado detras de esta respuesta, por su codigo. TOTAL.</summary>
		internal string ThePublicationShape()
		{
			return _publication;
		}

		/// <summary>Si hay algo publicado que contestar. TOTAL.</summary>
		internal bool IsPublished()
		{
			return _publication == SpecificationPublication.Published;
		}

		/// <summary>El codigo de la revision que la publico, o vacio donde no hay ninguna. TOTAL.</summary>
		internal string TheRevisionItCameFrom()
		{
			return _revisionCode;
		}

		/// <summary>
		/// El texto de cada grado publicado, tal como se publico, una por fila. TOTAL.
		/// </summary>
		internal string[] TheGradesPublished()
		{
			return _grades;
		}

		/// <summary>
		/// La postura de cada grado, por el NOMBRE del miembro, en el mismo orden. TOTAL, y ninguna se
		/// omite: un prohibido callado es un motor danado.
		/// </summary>
		internal string[] TheStandingsPublished()
		{
			return _standings;
		}
	}
}
