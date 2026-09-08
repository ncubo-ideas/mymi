using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Un perfil de uso declarado y desde cuando rige. La vigencia va HACIA ADELANTE: antes de su
	/// instante manda el que regia entonces.
	/// </summary>
	internal sealed class UsageProfileDeclaration
	{
		/// <summary>Un perfil ausente no es una declaracion incompleta: no es una declaracion.</summary>
		internal UsageProfileDeclaration(UsageProfile profile, DateTime effectiveFrom)
		{
			if (profile == null)
				throw new DomainException(
					"No se puede declarar la vigencia de ningun perfil: falta el perfil. Que no "
					+ "escale nada es Standard, que existe.");

			Profile = profile;
			EffectiveFrom = effectiveFrom;
		}

		/// <summary>El perfil que se declaro.</summary>
		internal UsageProfile Profile { get; }

		/// <summary>Desde cuando rige.</summary>
		internal DateTime EffectiveFrom { get; }

		/// <summary>Si ya rige en ese instante. Inclusiva, como los demas umbrales.</summary>
		internal bool TakesEffectBy(DateTime asOf)
		{
			return asOf >= EffectiveFrom;
		}
	}
}
