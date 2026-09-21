using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La respuesta de sobre que si se decidio: trae uno de los tres <see cref="EnvelopeStanding"/>
	/// y la clausula que explica contra que se midio. Que la agencia no haya publicado ningun
	/// sobre llega tambien por aqui, con <see cref="EnvelopeStanding.NotPublished"/>: es algo que
	/// el registro sabe con certeza, no algo que no pueda decir.
	/// </summary>
	internal sealed class ASettledEnvelopeAnswer : EnvelopeAnswer
	{
		private readonly EnvelopeStanding _standing;

		/// <summary>
		/// La compone quien conoce el sobre y la entrega. Sin resultado no seria una respuesta
		/// decidida.
		/// </summary>
		internal ASettledEnvelopeAnswer(EnvelopeStanding standing, AnEnvelopeClause clause)
			: base(clause)
		{
			if (standing == null)
				throw new DomainException(
					"Se compuso una respuesta decidida sobre el sobre de garantia sin ninguna de las "
					+ "respuestas posibles. Decidida quiere decir que hay una.");

			_standing = standing;
		}

		/// <summary>Si.</summary>
		internal override bool CanBeEvaluated()
		{
			return true;
		}

		/// <summary>El resultado, que aca si existe.</summary>
		internal override EnvelopeStanding Standing()
		{
			return _standing;
		}

		/// <summary>
		/// La decidida, salvo que lo decidido sea que nadie publico nada: esa se dice sin citar
		/// ningun limite, porque no hay ninguno que citar.
		/// </summary>
		internal override string TheShapeItHas()
		{
			return _standing == EnvelopeStanding.NotPublished ? NothingWasPublished : Settled;
		}

		/// <summary>El resultado por el nombre de su miembro.</summary>
		internal override string TheStandingItReached()
		{
			return _standing.Name;
		}
	}
}
