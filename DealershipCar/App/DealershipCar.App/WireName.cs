using System;
using DealershipCar.Dominio;

namespace DealershipCar.App
{
	/// <summary>
	/// Traduce el NOMBRE de un miembro de un conjunto cerrado, tal como llega por el wire, al
	/// nombre que el script del DSL le pasa al dominio. Es el unico sitio donde ese ascenso ocurre
	/// y es total: un nombre que no es de ningun miembro revienta aca, antes de performar, que es
	/// la unica forma de rechazarlo sin dejar una entrada en el journal de la persona.
	/// </summary>
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

		/// <summary>La clase de fluido de la que habla una especificacion publicada.</summary>
		internal static string FluidKind(string kind)
		{
			return AMemberOf(typeof(FluidKindCode), kind, "kind");
		}

		/// <summary>La posicion de la llanta de la que habla una especificacion publicada.</summary>
		internal static string TyrePosition(string position)
		{
			return AMemberOf(typeof(TyrePositionCode), position, "position");
		}

		/// <summary>La postura del fabricante sobre un grado de combustible publicado.</summary>
		internal static string FuelGradeStanding(string standing)
		{
			return AMemberOf(typeof(FuelGradeStandingCode), standing, "standing");
		}

		/// <summary>
		/// La unidad en que se publico una presion. No se convierte a ninguna otra en ningun punto:
		/// aca solo se comprueba que nombre una de las que hay.
		/// </summary>
		internal static string PressureUnit(string unit, string parameterName)
		{
			return AMemberOf(typeof(PressureUnitCode), unit, parameterName);
		}

		/// <summary>
		/// La unidad en que se publico una capacidad. No se convierte a ninguna otra en ningun
		/// punto: aca solo se comprueba que nombre una de las que hay.
		/// </summary>
		internal static string VolumeUnit(string unit, string parameterName)
		{
			return AMemberOf(typeof(VolumeUnitCode), unit, parameterName);
		}

		internal static bool NamesAnOperationKind(string kind)
		{
			return IsAMemberOf(typeof(OperationKindCode), kind);
		}

		internal static bool NamesAnIntervalBasis(string basis)
		{
			return IsAMemberOf(typeof(IntervalBasisCode), basis);
		}

		internal static bool NamesADistanceUnit(string unit)
		{
			return IsAMemberOf(typeof(DistanceUnitCode), unit);
		}

		internal static bool NamesAReadingSource(string source)
		{
			return IsAMemberOf(typeof(ReadingSourceCode), source);
		}

		/// <summary>Si ese texto nombra una clase de fluido. Contesta, no revienta.</summary>
		internal static bool NamesAFluidKind(string kind)
		{
			return IsAMemberOf(typeof(FluidKindCode), kind);
		}

		/// <summary>Si ese texto nombra una posicion de llanta. Contesta, no revienta.</summary>
		internal static bool NamesATyrePosition(string position)
		{
			return IsAMemberOf(typeof(TyrePositionCode), position);
		}

		/// <summary>Si ese texto nombra una postura de combustible. Contesta, no revienta.</summary>
		internal static bool NamesAFuelGradeStanding(string standing)
		{
			return IsAMemberOf(typeof(FuelGradeStandingCode), standing);
		}

		/// <summary>Si ese texto nombra una unidad de presion. Contesta, no revienta.</summary>
		internal static bool NamesAPressureUnit(string unit)
		{
			return IsAMemberOf(typeof(PressureUnitCode), unit);
		}

		/// <summary>Si ese texto nombra una unidad de volumen. Contesta, no revienta.</summary>
		internal static bool NamesAVolumeUnit(string unit)
		{
			return IsAMemberOf(typeof(VolumeUnitCode), unit);
		}

		private static bool IsAMemberOf(Type closedSet, string value)
		{
			return !string.IsNullOrWhiteSpace(value) && Enum.IsDefined(closedSet, value);
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
