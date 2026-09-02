using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La regla de servicio en otro taller que el fabricante si publico, con lo que conserva la
	/// cobertura. El texto se muestra entre comillas y como se publico: sin traducir, sin resumir y
	/// sin reescribir, porque lo que dice el fabricante no es lo mismo que lo que compone esta hoja.
	/// </summary>
	internal sealed class AServicingPolicy : ServicingPolicy
	{
		private readonly string _rule;
		private readonly string _whatPreservesCover;

		/// <summary>
		/// Se llega por <see cref="ServicingPolicy.Of"/>. Las dos mitades son obligatorias: media
		/// regla publicada es lo que el sello existe para que nadie pueda citar.
		/// </summary>
		internal AServicingPolicy(string rule, string whatPreservesCover)
		{
			if (string.IsNullOrWhiteSpace(rule))
				throw new DomainException(
					"Se publico una regla de servicio en otro taller sin decir cual es la regla. "
					+ "Publicarla vacia dejaria una hoja que parece haber contestado la pregunta.");

			if (string.IsNullOrWhiteSpace(whatPreservesCover))
				throw new DomainException(
					"Se publico la regla de servicio en otro taller sin decir que conserva la "
					+ "cobertura. Es la mitad accionable: sin ella la persona sabe que tiene un "
					+ "problema y no sabe que hacer con el.");

			_rule = rule;
			_whatPreservesCover = whatPreservesCover;
		}

		/// <summary>Si.</summary>
		internal override bool Exists()
		{
			return true;
		}

		/// <summary>Las dos mitades, cada una entre comillas y tal como se publicaron.</summary>
		internal override string Describe()
		{
			return "Servicio en otro taller: \"" + _rule + "\" Qué conserva la cobertura: \""
				+ _whatPreservesCover + "\"";
		}

		/// <summary>
		/// Los conteos recibidos, dichos como conteos y sin concluir nada de ellos. Decir aca que la
		/// cobertura esta conservada seria adjudicar, y adjudicar es del fabricante.
		/// </summary>
		internal override string DescribeAgainst(
			int externalServicesWithEvidence, int externalServicesWithoutEvidence,
			int missedMaintenanceCount)
		{
			return "Este vehículo: servicios externos con comprobante "
				+ externalServicesWithEvidence + ", servicios externos sin comprobante "
				+ externalServicesWithoutEvidence + ", mantenimientos no hechos "
				+ missedMaintenanceCount + ".";
		}
	}
}
