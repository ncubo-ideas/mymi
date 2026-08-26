using System;
using DealershipCar.Dominio;

namespace DealershipCar.App
{
	// LA PUERTA DE LOS CONJUNTOS CERRADOS. Un codigo del wire llega como el NOMBRE de un miembro
	// -- "FluidChange", no 2 -- porque asi es como el motor deja pasar un enum: el parametro se
	// declara typeof(Enum) y el motor lo resuelve al enum concreto en el sitio de la llamada
	// (medido, SPIKES-OS2 S2).
	//
	// POR QUE SE VALIDA ACA Y NO ADENTRO. El agregado ya tiene su resolvedor total-con-throw, pero
	// corre DESPUES de que el motor escribio la entrada. Un nombre que no existe llegaria al
	// journal, reventaria al ejecutarse y dejaria una entrada permanente que hace loguear un fallo
	// de rehidratacion en cada arranque futuro (medido, SPIKES-OS2 S5). Rechazarlo en la frontera
	// es lo unico que evita esa cicatriz -- y es donde CONTRADICTIONS C3 pone las excepciones de
	// argumento del BCL: guardan el WIRE, no el dominio.
	//
	// Se compara sin tolerancia de mayusculas a proposito. El motor si tolera, pero el canon manda
	// en los nombres: "fluidchange" no es como se dice, y dejarlo pasar pondria en el journal una
	// grafia que el canon no escribe.
	internal static class WireName
	{
		internal static string OperationKind(string kind)
		{
			return AMemberOf(typeof(OperationKindCode), kind, "kind");
		}

		internal static string IntervalBasis(string basis)
		{
			return AMemberOf(typeof(IntervalBasisCode), basis, "basis");
		}

		internal static string DistanceUnit(string unit, string parameterName)
		{
			return AMemberOf(typeof(DistanceUnitCode), unit, parameterName);
		}

		internal static string ReadingSource(string source)
		{
			return AMemberOf(typeof(ReadingSourceCode), source, "source");
		}

		internal static string UsageProfile(string profile, string parameterName)
		{
			return AMemberOf(typeof(UsageProfileCode), profile, parameterName);
		}

		internal static string DeferralReason(string reason)
		{
			return AMemberOf(typeof(DeferralReasonCode), reason, "reason");
		}

		private static string AMemberOf(Type closedSet, string value, string parameterName)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

			if (!Enum.IsDefined(closedSet, value))
				throw new ArgumentException(
					$"'{value}' no nombra ningun miembro de {closedSet.Name}. Los que hay son: "
					+ string.Join(", ", Enum.GetNames(closedSet)) + ".",
					parameterName);

			return value;
		}
	}
}
