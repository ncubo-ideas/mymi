using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// Una revision del plan del fabricante que esta agenda adopto: su codigo y cuando se adopto.
	/// Es la procedencia del plan que la persona esta corriendo; las operaciones que la
	/// revision trae entran despues, una por una, por la siembra.
	/// </summary>
	internal sealed class PlanRevision
	{
		/// <summary>Una revision sin codigo no se puede nombrar despues.</summary>
		internal PlanRevision(string code, DateTime adoptedAt)
		{
			if (string.IsNullOrWhiteSpace(code))
				throw new DomainException(
					"Una revision del plan sin codigo no se puede adoptar: el codigo es lo que "
					+ "dice CUAL plan quedo rigiendo, y sin el la agenda no puede decir de donde "
					+ "salieron sus operaciones.");

			Code = code;
			AdoptedAt = adoptedAt;
		}

		/// <summary>Como se llama esta revision.</summary>
		internal string Code { get; }

		/// <summary>Cuando se adopto.</summary>
		internal DateTime AdoptedAt { get; }

		/// <summary>Si esta revision es anterior a la otra.</summary>
		internal bool WasAdoptedBefore(PlanRevision other)
		{
			return AdoptedAt < other.AdoptedAt;
		}
	}
}
