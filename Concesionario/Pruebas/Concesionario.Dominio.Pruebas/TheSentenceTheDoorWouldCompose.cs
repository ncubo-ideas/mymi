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

		internal static string ThePublishedWarrantyEnvelope(
			VehicleProfile profile, string language)
		{
			return ThePublishedWarrantyEnvelope(profile.ThePublishedWarrantyEnvelope(), language);
		}

		internal static string ThePublishedWarrantyEnvelope(
			WarrantyEnvelope envelope, string language)
		{
			return TheWording.ThePublishedWarrantyEnvelope(
				Phrases.Of(language),
				envelope.TheShapeItHas(), envelope.TheMonthsItPublished(),
				envelope.TheDistanceItPublished(), envelope.TheUnitItsDistanceIsSaidIn(),
				envelope.TheDayItWasPublished());
		}

		internal static string TheWarrantyEnvelope(
			VehicleProfile profile, int reading, DistanceUnitCode readingUnit,
			DateTime observedAt, DateTime asOf, string language)
		{
			return TheWarrantyEnvelope(
				profile.TheStandingAsOf(reading, readingUnit, observedAt, asOf), language);
		}

		internal static string TheWarrantyEnvelopeWithNoOdometer(
			VehicleProfile profile, DateTime asOf, string language)
		{
			return TheWarrantyEnvelope(profile.TheStandingWithNoOdometerAsOf(asOf), language);
		}

		internal static string TheWarrantyEnvelope(EnvelopeAnswer answer, string language)
		{
			return TheWording.TheWarrantyEnvelope(
				Phrases.Of(language),
				answer.TheShapeItHas(), answer.TheStandingItReached(),
				answer.TheUnitThatDidNotMatch(), answer.TheClauseShape(),
				answer.ThePublishedShape(), answer.TheMonthsPublished(),
				answer.TheDistancePublished(), answer.TheUnitTheDistanceIsPublishedIn(),
				answer.TheDayTheEnvelopeWasPublished(), answer.TheMonthsLeft(),
				answer.TheMonthsLimit(), answer.TheDistanceHalfShape(),
				answer.TheDistanceLeftCounted(), answer.TheDistanceLimitCounted(),
				answer.TheUnitTheDistanceIsSaidIn(), answer.TheUnitTheOtherDistanceIsIn(),
				answer.WhoseDistanceDidNotMatch(), answer.WhichHalfArrivedFirst(),
				answer.TheOdometerShape(), answer.TheReading(), answer.TheUnitTheReadingIsIn(),
				answer.TheDayTheReadingWasObserved(), answer.TheDeliveryReading(),
				answer.TheUnitTheDeliveryReadingIsIn(), answer.TheDayItWasDelivered());
		}

		// LA HOJA DEL FABRICANTE. Cada item lleva su propio portador -- forma, revision y sus valores
		// ya tipados -- asi que el cableado de cada uno esta una sola vez, aqui, y el envoltorio que
		// los nueve comparten vive dentro del renderizador y no se repite.
		//
		// EL PRECIO TODAVIA NO. Sus dos lecturas siguen pasando por el envoltorio de abajo con la
		// clausula que el dominio redacta en espanol: la moneda es su propio turno.
		internal static string TheFluid(
			ModelSpecification specification, FluidKindCode kind, string language)
		{
			return TheFluid(specification.TheFluidAnswer(kind), language);
		}

		internal static string TheFluid(AFluidClause answer, string language)
		{
			return TheWording.TheFluid(
				Phrases.Of(language),
				answer.ThePublicationShape(), answer.TheRevisionItCameFrom(),
				answer.TheFluidItIsAbout(), answer.TheGradePublished(),
				answer.TheCapacityAsPublished(), answer.TheUnitTheCapacityIsIn(),
				answer.TheIntervalHalvesPublished(), answer.TheIntervalDistance(),
				answer.TheIntervalDays(), answer.TheUnitTheIntervalIsIn());
		}

		internal static string TheTyre(
			ModelSpecification specification, TyrePositionCode position, string language)
		{
			ATyreClause answer = specification.TheTyreAnswer(position);

			return TheWording.TheTyre(
				Phrases.Of(language),
				answer.ThePublicationShape(), answer.TheRevisionItCameFrom(),
				answer.ThePositionItIsAbout(), answer.TheSizePublished(),
				answer.ThePressureAsPublished(), answer.TheUnitThePressureIsIn(),
				answer.TheRatingsPublished());
		}

		internal static string TheFuel(ModelSpecification specification, string language)
		{
			AFuelClause answer = specification.TheFuelAnswer();

			return TheWording.TheFuel(
				Phrases.Of(language),
				answer.ThePublicationShape(), answer.TheRevisionItCameFrom(),
				answer.TheGradesPublished(), answer.TheStandingsPublished());
		}

		internal static string TheServiceInterval(
			ModelSpecification specification, OperationKindCode kind, int distanceRemaining,
			DistanceUnitCode remainingUnit, string language)
		{
			return TheServiceInterval(
				specification.TheServiceIntervalAnswer(kind, distanceRemaining, remainingUnit),
				language);
		}

		internal static string ThePublishedServiceInterval(
			ModelSpecification specification, OperationKindCode kind, string language)
		{
			return TheServiceInterval(
				specification.ThePublishedServiceIntervalAnswer(kind), language);
		}

		internal static string TheServiceInterval(AServiceIntervalClause answer, string language)
		{
			return TheWording.TheServiceInterval(
				Phrases.Of(language),
				answer.ThePublicationShape(), answer.TheRevisionItCameFrom(),
				answer.TheOperationItIsAbout(), answer.TheIntervalHalvesPublished(),
				answer.TheIntervalDistance(), answer.TheIntervalDays(),
				answer.TheUnitTheIntervalIsIn(), answer.TheHalvesItAnswers(),
				answer.TheRemainderShape(), answer.TheDistanceRemaining(),
				answer.TheUnitTheRemainderIsIn());
		}

		internal static string TheServicePrice(
			ModelSpecification specification, OperationKindCode kind, int milestoneDistance,
			DistanceUnitCode milestoneUnit, DateTime asOf, string language)
		{
			return TheSpecificationAnswer(
				specification.TheServicePriceAnswer(kind, milestoneDistance, milestoneUnit, asOf),
				language);
		}

		internal static string ThePublishedServicePrice(
			ModelSpecification specification, OperationKindCode kind, int milestoneDistance,
			DistanceUnitCode milestoneUnit, string language)
		{
			return TheSpecificationAnswer(
				specification.ThePublishedServicePriceAnswer(
					kind, milestoneDistance, milestoneUnit),
				language);
		}

		internal static string TheServicingElsewhereRule(
			ModelSpecification specification, int withEvidence, int withoutEvidence, int missed,
			string language)
		{
			return TheServicingElsewhereRule(
				specification.TheServicingElsewhereAnswer(withEvidence, withoutEvidence, missed),
				language);
		}

		internal static string ThePublishedServicingElsewhereRule(
			ModelSpecification specification, string language)
		{
			return TheServicingElsewhereRule(
				specification.ThePublishedServicingElsewhereAnswer(), language);
		}

		internal static string TheServicingElsewhereRule(AServicingClause answer, string language)
		{
			return TheWording.TheServicingElsewhereRule(
				Phrases.Of(language),
				answer.ThePublicationShape(), answer.TheRevisionItCameFrom(),
				answer.TheRulePublished(), answer.TheWhatPreservesCoverPublished(),
				answer.TheHalvesItAnswers(), answer.TheExternalServicesWithEvidence(),
				answer.TheExternalServicesWithoutEvidence(), answer.TheMissedMaintenanceCount());
		}

		internal static string TheSpecificationAnswer(SpecificationAnswer answer, string language)
		{
			return TheWording.TheSpecificationAnswer(
				Phrases.Of(language),
				answer.TheShapeItHas(), answer.RevisionCode(),
				answer.TheClauseItCarries(), answer.TheSubjectItIsAbout());
		}

		// Cada precio publicado en su fila, como la puerta las compone: la misma frase que la lectura
		// con clave, porque es el mismo marco y el mismo renderizador.
		internal static string[] EachPublishedPrice(
			ModelSpecification specification, DateTime asOf, string language)
		{
			Phrases phrases = Phrases.Of(language);
			string[] shapes = specification.PublishedPriceShapesAsOf(asOf);
			string[] clauses = specification.PublishedPriceClausesAsOf(asOf);
			string[] revisions = specification.PublishedPriceRevisionsAsOf(asOf);
			string[] said = new string[shapes.Length];

			for (int fila = 0; fila < shapes.Length; fila++)
				said[fila] = TheWording.TheSpecificationAnswer(
					phrases, shapes[fila], revisions[fila], clauses[fila], "");

			return said;
		}

		// LA GARANTIA. El resumen y la posicion de un sistema pasan por su propio marco, que dice la
		// ley de que esto no adjudica nada, y las exclusiones salen una por fila.
		internal static string TheCoverageSummary(
			ModelSpecification specification, string language)
		{
			ACoverageScope scope = specification.TheCoverageScope();

			return TheWording.TheCoverageScope(
				Phrases.Of(language),
				scope.ThePublicationShape(), scope.TheRevisionItCameFrom(),
				scope.TheSystemCodes(), scope.TheSystemLimitShapes(), scope.TheSystemMonths(),
				scope.TheSystemDistances(), scope.TheSystemDistanceUnits(),
				scope.TheExclusionSystems(), scope.TheExclusionTexts(),
				scope.TheExclusionConditions());
		}

		internal static string[] TheCoverageExclusions(
			ModelSpecification specification, string language)
		{
			Phrases phrases = Phrases.Of(language);
			ACoverageScope scope = specification.TheCoverageScope();
			string[] systems = scope.TheExclusionSystems();
			string[] texts = scope.TheExclusionTexts();
			string[] conditions = scope.TheExclusionConditions();
			string[] said = new string[texts.Length];

			for (int fila = 0; fila < texts.Length; fila++)
				said[fila] = TheWording.AnExclusion(
					phrases, systems[fila], texts[fila], conditions[fila]);

			return said;
		}

		internal static string TheCoverage(
			ModelSpecification specification, string systemCode, int envelopeMonths,
			int envelopeDistance, DistanceUnitCode envelopeUnit, DateTime deliveredAt,
			int deliveryReading, int currentReading, DistanceUnitCode currentUnit, DateTime asOf,
			string language)
		{
			return TheCoverage(
				specification.TheCoverageAnswer(
					systemCode, envelopeMonths, envelopeDistance, envelopeUnit, deliveredAt,
					deliveryReading, currentReading, currentUnit, asOf),
				language);
		}

		internal static string TheCoverage(CoverageAnswer answer, string language)
		{
			return TheWording.TheCoverage(
				Phrases.Of(language),
				answer.ThePublicationShape(), answer.TheRevisionItCameFrom(),
				answer.TheSystemItIsAbout(), answer.TheShapeItHas(),
				answer.TheStandingItReached(), answer.TheReasonItGives(),
				answer.TheLimitsShape(), answer.TheMonthsInForce(), answer.TheDistanceInForce(),
				answer.TheUnitTheDistanceIsInForceIn(), answer.TheUnitTheLimitIsIn(),
				answer.TheUnitTheOdometerIsIn(), answer.TheExclusionSystems(),
				answer.TheExclusionTexts(), answer.TheExclusionConditions());
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
