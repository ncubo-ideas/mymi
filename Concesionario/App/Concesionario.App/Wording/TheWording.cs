using System;

namespace Concesionario.App.Wording
{
	/// <summary>
	/// COMO SE ARMA CADA RESPUESTA a partir de lo que el dominio mando: su forma, sus claves de
	/// clausula y sus valores. Una funcion por respuesta, y el anidado -- que clausula va en que
	/// hueco -- esta escrito una sola vez, aqui.
	/// <para>
	/// Ningun valor se traduce al entrar en un hueco cuando salio del registro. Lo que si se traduce
	/// es la etiqueta de un conjunto cerrado, que el dominio eligio de una lista y manda por su
	/// codigo.
	/// </para>
	/// </summary>
	public static class TheWording
	{
		private const string TheTimeDimension = "Time";
		private const string TheClauseThatCountsTowardTheLimit = "BeforeTheLimit";

		/// <summary>
		/// POR QUE UNA OPERACION ESTA DONDE ESTA. La forma elige el marco; el estado, la dimension
		/// que decidio y las clausulas entran en sus huecos. Las clausulas llegan en columnas
		/// paralelas y en el orden en que la ley las quiere.
		/// </summary>
		public static string TheDueReason(
			Phrases phrases, string shape, string state, string whoDecided, string kind,
			string deferredUntil, string deferralReason, int deferralsUsed, int deferralsAllowed,
			string[] clauseCodes, string[] clauseDimensions, int[] clauseAmounts,
			int[] clauseLimits, string[] clauseUnits, string[] clauseMeasuredUnits,
			string[] clauseUsageProfiles)
		{
			if (phrases == null) throw new ArgumentNullException(nameof(phrases));

			if (shape == "NoOperationScheduled")
				return phrases.Say("reason.NoOperationScheduled",
					("kind", phrases.HowItIsSaid("canon.operation", kind).ToLowerInvariant()));

			if (shape == "Deferred")
				return phrases.Say("reason.Deferred",
					("state", phrases.HowItIsSaid("canon.dueState", state)),
					("until", phrases.ADay(deferredUntil)),
					("reason", phrases.HowItIsSaid("canon.deferralReason", deferralReason)),
					("used", phrases.Grouped(deferralsUsed)),
					("allowed", phrases.Grouped(deferralsAllowed)));

			if (shape == "Decided")
				return phrases.Say("reason.Decided",
					("state", phrases.HowItIsSaid("canon.dueState", state)),
					("who", phrases.Say("reason.decidedBy." + whoDecided)),
					("clauses", TheClauses(phrases, clauseCodes, clauseDimensions, clauseAmounts,
						clauseLimits, clauseUnits, clauseMeasuredUnits, clauseUsageProfiles)));

			if (shape == "NothingCouldBeEvaluated")
				return phrases.Say("reason.NothingCouldBeEvaluated",
					("state", phrases.HowItIsSaid("canon.dueState", state)),
					("clauses", TheClauses(phrases, clauseCodes, clauseDimensions, clauseAmounts,
						clauseLimits, clauseUnits, clauseMeasuredUnits, clauseUsageProfiles)));

			return phrases.Say(
				"reason." + shape, ("state", phrases.HowItIsSaid("canon.dueState", state)));
		}

		/// <summary>
		/// LO QUE LE QUEDA A UNA MITAD: la situacion elige el marco, y la dimension, el monto y la
		/// unidad entran en sus huecos. Las tres suspensiones dicen la misma clausula que el motivo,
		/// con la misma clave.
		/// </summary>
		public static string TheRemainder(
			Phrases phrases, string standing, string dimension, int[] amount, string unit,
			string measuredUnit, string usageProfile, string kind)
		{
			if (phrases == null) throw new ArgumentNullException(nameof(phrases));

			string said = TheDimension(phrases, dimension);

			if (standing == "StillAhead")
				return phrases.Say(
					AmountIs(amount, 1) ? "remainder.StillAhead.one" : "remainder.StillAhead.many",
					("dimension", said),
					("amount", AQuantity(phrases, dimension, TheAmountOf(amount), unit)));

			if (standing == "AlreadyPassed")
				return AmountIs(amount, 0)
					? phrases.Say("remainder.AtTheLimit", ("dimension", said))
					: phrases.Say("remainder.AlreadyPassed",
						("dimension", said),
						("amount", AQuantity(phrases, dimension, TheAmountOf(amount), unit)));

			if (standing == "OutsideTheLaw")
				return phrases.Say("remainder.OutsideTheLaw", ("dimension", said));

			if (standing == "NoOperationScheduled")
				return phrases.Say("remainder.NoOperationScheduled",
					("kind", phrases.HowItIsSaid("canon.operation", kind)));

			return AClause(
				phrases, TheClauseBehind(standing), dimension, 0, 0, unit, measuredUnit,
				usageProfile);
		}

		/// <summary>
		/// LA CUENTA ATRAS ENTERA: las dos mitades en una linea y sin elegir ninguna. Sin operacion
		/// agendada hay una sola mitad que decir, y la separacion es puntuacion, que es del idioma.
		/// </summary>
		public static string TheCountdown(
			Phrases phrases, bool hasTwoHalves, string distanceSaid, string timeSaid)
		{
			if (phrases == null) throw new ArgumentNullException(nameof(phrases));

			if (!hasTwoHalves) return distanceSaid;

			return phrases.Say("countdown.both", ("distance", distanceSaid), ("time", timeSaid));
		}

		private static string TheClauses(
			Phrases phrases, string[] codes, string[] dimensions, int[] amounts, int[] limits,
			string[] units, string[] measuredUnits, string[] usageProfiles)
		{
			if (codes.Length == 0) return "";

			string first = AClause(phrases, codes[0], dimensions[0], amounts[0], limits[0],
				units[0], measuredUnits[0], usageProfiles[0]);

			if (codes.Length == 1) return first;

			string second = AClause(phrases, codes[1], dimensions[1], amounts[1], limits[1],
				units[1], measuredUnits[1], usageProfiles[1]);

			return phrases.Say("clauses.two", ("first", first), ("second", second));
		}

		/// <summary>
		/// UNA CLAUSULA. El monto va a secas mientras se cuenta hacia el limite y con su unidad
		/// cuando ya se paso: es la misma distincion que el dominio hace al elegir la clave.
		/// </summary>
		private static string AClause(
			Phrases phrases, string code, string dimension, int amount, int limit, string unit,
			string measuredUnit, string usageProfile)
		{
			string counted = code == TheClauseThatCountsTowardTheLimit
				? phrases.Grouped(amount)
				: AQuantity(phrases, dimension, amount, unit);

			return phrases.Say("clause." + code,
				("dimension", TheDimension(phrases, dimension)),
				("amount", counted),
				("limit", AQuantity(phrases, dimension, limit, unit)),
				("unit", unit),
				("measured", measuredUnit),
				("profile", phrases.HowItIsSaid("canon.usageProfile", usageProfile)));
		}

		private static string AQuantity(Phrases phrases, string dimension, int amount, string unit)
		{
			if (dimension == TheTimeDimension) return phrases.Days(amount);

			return phrases.Say(
				"quantity.distance", ("amount", phrases.Grouped(amount)), ("unit", unit));
		}

		private static string TheDimension(Phrases phrases, string dimension)
		{
			return dimension.Length == 0 ? "" : phrases.Say("dimension." + dimension);
		}

		/// <summary>
		/// La clausula que dice una suspension: las tres situaciones suspendidas del resto son las
		/// tres clausulas suspendidas del motivo.
		/// </summary>
		private static string TheClauseBehind(string standing)
		{
			if (standing == "SuspendedByAUnitMismatch") return "UnitMismatch";
			if (standing == "SuspendedByTheUsageProfile") return "UsageProfile";

			return "AbsentReading";
		}

		private static bool AmountIs(int[] amount, int value)
		{
			return amount.Length == 1 && amount[0] == value;
		}

		private static int TheAmountOf(int[] amount)
		{
			return amount.Length == 1 ? amount[0] : 0;
		}
	}
}
