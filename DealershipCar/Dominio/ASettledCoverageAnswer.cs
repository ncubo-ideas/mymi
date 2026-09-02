using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La respuesta de cobertura que si se decidio: trae uno de los cinco
	/// <see cref="CoverageStanding"/> y la clausula que explica contra que se midio, con las
	/// exclusiones publicadas que haya y el recordatorio de que esto es alcance y no la
	/// adjudicacion de un reclamo.
	/// </summary>
	internal sealed class ASettledCoverageAnswer : CoverageAnswer
	{
		private readonly string _subject;
		private readonly CoverageStanding _standing;
		private readonly string _clause;

		/// <summary>
		/// La compone <see cref="WarrantyCoverage"/>, que es quien conoce el alcance publicado.
		/// Sin sujeto la frase no dice de que habla, y sin resultado no seria una respuesta
		/// decidida.
		/// </summary>
		internal ASettledCoverageAnswer(string subject, CoverageStanding standing, string clause)
		{
			if (string.IsNullOrWhiteSpace(subject))
				throw new DomainException(
					"Se compuso una respuesta de cobertura sin decir de que sistema habla. La misma "
					+ "frase serviria para todos, que es como una respuesta termina leyendose como si "
					+ "fuera de otra cosa.");

			if (standing == null)
				throw new DomainException(
					$"Se compuso una respuesta decidida sobre {subject} sin ninguna de las respuestas "
					+ "posibles. Decidida quiere decir que hay una.");

			_subject = subject;
			_standing = standing;
			_clause = clause;
		}

		/// <summary>Si.</summary>
		internal override bool CanBeEvaluated()
		{
			return true;
		}

		/// <summary>El resultado, que aca si existe.</summary>
		internal override CoverageStanding Standing()
		{
			return _standing;
		}

		/// <summary>
		/// El sujeto, el resultado por su nombre, y la clausula que dice contra que se midio.
		/// </summary>
		internal override string Describe()
		{
			return _subject + ": " + _standing.Name + ". " + _clause;
		}
	}
}
