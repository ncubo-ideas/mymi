using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Un grado de combustible tal como el fabricante lo publico, con la postura que le puso:
	/// minimo, permitido o prohibido. El texto del grado no se traduce ni se reescribe -- se
	/// muestra como se publico -- y la postura viaja pegada, porque un grado sin postura es solo un
	/// combustible que aparece en la hoja.
	/// </summary>
	internal sealed class FuelGrade
	{
		private readonly string _grade;
		private readonly FuelGradeStanding _standing;

		/// <summary>
		/// El texto publicado y su postura. Sin texto no hay grado que nombrar, y sin postura la
		/// unica lectura posible es la peligrosa.
		/// </summary>
		internal FuelGrade(string grade, FuelGradeStanding standing)
		{
			if (string.IsNullOrWhiteSpace(grade))
				throw new DomainException(
					"Se publico un grado de combustible sin decir cual. Un grado en blanco no se "
					+ "puede pedir en una bomba.");

			if (standing == null)
				throw new DomainException(
					$"Se publico el grado '{grade}' sin decir si es el minimo, si esta permitido o si "
					+ "esta prohibido. La postura es la mitad que importa: un prohibido callado es un "
					+ "motor danado.");

			_grade = grade;
			_standing = standing;
		}

		/// <summary>
		/// El texto del grado tal como se publico, sin la postura. Es lo que una persona pide en
		/// una bomba, y por eso sale suelto ademas de dentro de la frase.
		/// </summary>
		internal string Text
		{
			get { return _grade; }
		}

		/// <summary>
		/// Si el fabricante le puso ESA postura a este grado. Compara por identidad, que es la
		/// unica comparacion que admite un conjunto cerrado.
		/// </summary>
		internal bool ItStands(FuelGradeStanding standing)
		{
			if (standing == null)
				throw new DomainException(
					"Se pregunto por la postura de un grado de combustible sin decir cual. Sin la "
					+ "postura no hay nada contra que compararlo.");

			return ReferenceEquals(_standing, standing);
		}

		/// <summary>
		/// Si los dos hablan del mismo grado publicado, comparando el texto tal cual. Es la
		/// comparacion con nombre que deja corregir la postura de un grado antes de sellar, sin
		/// escribir ninguna igualdad.
		/// </summary>
		internal bool IsTheSameGradeAs(FuelGrade other)
		{
			if (other == null)
				throw new DomainException(
					"Se pregunto si un grado de combustible es el mismo que ninguno. Sin el otro "
					+ "grado no hay comparacion que hacer.");

			return string.Equals(_grade, other._grade, StringComparison.Ordinal);
		}

		/// <summary>
		/// La postura con que se publico, por el NOMBRE del miembro. Es lo que viaja en la clausula al
		/// lado del texto del grado: la postura es un conjunto cerrado y como se dice es de quien la
		/// muestra.
		/// </summary>
		internal string TheStandingItWasPublishedWith()
		{
			return _standing.Name;
		}
	}
}
