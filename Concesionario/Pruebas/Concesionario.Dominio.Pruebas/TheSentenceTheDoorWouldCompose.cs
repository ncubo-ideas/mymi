using System;
using Concesionario.App.Wording;

namespace Concesionario.Dominio.Pruebas
{
	// LA FRASE QUE LA PUERTA COMPONDRIA con lo que este agregado contesta, para las pruebas que
	// montan una agenda PURA y cuya ley es sobre la frase y no sobre la decision.
	//
	// NO ES UNA SEGUNDA REDACCION: el renderizador es el mismo -- TheWording, uno por idioma -- y lo
	// unico que vive aqui es el cableado, que accesor va en que parametro. Ese cableado si esta dos
	// veces, aqui y en la puerta, y por eso NO se cree solo: TheHelperOfThePureSchedulesComposes-
	// ExactlyWhatTheDoorComposes, en ARemainderReachesTheDoorComposedWithItsUnitAndItsStanding, pone
	// las dos salidas una contra la otra sobre el mismo registro.
	//
	// UNA PRUEBA CUYA LEY SEA SOBRE LA DECISION NO PASA POR AQUI: esa pregunta por la situacion, la
	// forma del motivo y sus valores, que es lo que el dominio elige.
	internal static class TheSentenceTheDoorWouldCompose
	{
		internal static string TheDistanceRemainder(
			MaintenanceSchedule schedule, OperationKindCode kind, DateTime asOf, string language)
		{
			return TheRemainder(schedule.TheDistanceRemainderFor(kind, asOf), language);
		}

		internal static string TheTimeRemainder(
			MaintenanceSchedule schedule, OperationKindCode kind, DateTime asOf, string language)
		{
			return TheRemainder(schedule.TheTimeRemainderFor(kind, asOf), language);
		}

		internal static string TheCountdown(
			MaintenanceSchedule schedule, OperationKindCode kind, DateTime asOf, string language)
		{
			return TheWording.TheCountdown(
				Phrases.Of(language),
				schedule.TheCountdownHasTwoHalves(kind, asOf),
				TheDistanceRemainder(schedule, kind, asOf, language),
				TheTimeRemainder(schedule, kind, asOf, language));
		}

		internal static string TheDueReason(
			MaintenanceSchedule schedule, OperationKindCode kind, string language)
		{
			ADueReason reason = schedule.TheDueReasonFor(kind);

			return TheWording.TheDueReason(
				Phrases.Of(language),
				reason.Shape, reason.State, reason.WhoDecided, reason.Kind,
				reason.DeferredUntil, reason.DeferralReason, reason.DeferralsUsed,
				reason.DeferralsAllowed, reason.ClauseCodes, reason.ClauseDimensions,
				reason.ClauseAmounts, reason.ClauseLimits, reason.ClauseUnits,
				reason.ClauseMeasuredUnits, reason.ClauseUsageProfiles);
		}

		private static string TheRemainder(ARemainder remainder, string language)
		{
			return TheWording.TheRemainder(
				Phrases.Of(language),
				remainder.Standing().Name,
				remainder.TheDimensionItMeasures(),
				remainder.TheAmountItCounts(),
				remainder.TheUnitItIsExpressedIn(),
				remainder.TheUnitTheMeasurementsAreIn(),
				remainder.TheUsageProfileThatIgnoresIt(),
				remainder.TheKindWithNoOperationScheduled());
		}
	}
}
