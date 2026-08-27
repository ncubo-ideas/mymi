using System;
using DealershipCar.Dominio;

namespace DealershipCar.App
{
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
