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
		// EL PRECIO YA ENTRO, con la moneda: su portador lleva el hito con su unidad, el monto con la
		// forma y el codigo de su moneda, la vigencia y donde cae el instante preguntado.
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
			return ThePrice(
				specification.TheServicePriceAnswer(kind, milestoneDistance, milestoneUnit, asOf),
				language);
		}

		internal static string ThePublishedServicePrice(
			ModelSpecification specification, OperationKindCode kind, int milestoneDistance,
			DistanceUnitCode milestoneUnit, string language)
		{
			return ThePrice(
				specification.ThePublishedServicePriceAnswer(
					kind, milestoneDistance, milestoneUnit),
				language);
		}

		internal static string ThePrice(APriceClause answer, string language)
		{
			return TheWording.ThePrice(
				Phrases.Of(language),
				answer.ThePublicationShape(), answer.TheRevisionItCameFrom(),
				answer.TheOperationItIsAbout(), answer.TheMilestoneDistance(),
				answer.TheUnitTheMilestoneIsIn(), answer.TheShapeOfItsCurrency(),
				answer.TheAmountAsSaid(), answer.TheCodeOfItsCurrency(),
				answer.TheDayItsValidityStarts(), answer.TheDayItsValidityEnds(),
				answer.WhereTheInstantAskedFalls(), answer.TheDayItWasAskedOn());
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
			string[] operations = specification.PublishedPriceOperationsAsOf(asOf);
			int[] milestones = specification.PublishedPriceMilestoneDistancesAsOf(asOf);
			string[] milestoneUnits = specification.PublishedPriceMilestoneUnitsAsOf(asOf);
			string[] shapes = specification.PublishedPriceShapesAsOf(asOf);
			string[] currencyShapes = specification.PublishedPriceCurrencyShapesAsOf(asOf);
			string[] amounts = specification.PublishedPriceAmountsAsOf(asOf);
			string[] currencyCodes = specification.PublishedPriceCurrencyCodesAsOf(asOf);
			string[] validFrom = specification.PublishedPriceValidFromAsOf(asOf);
			string[] validUntil = specification.PublishedPriceValidUntilAsOf(asOf);
			string[] validity = specification.PublishedPriceValiditiesAsOf(asOf);
			string[] askedOn = specification.PublishedPriceDaysAskedOnAsOf(asOf);
			string[] revisions = specification.PublishedPriceRevisionsAsOf(asOf);
			string[] said = new string[shapes.Length];

			for (int fila = 0; fila < shapes.Length; fila++)
				said[fila] = TheWording.ThePrice(
					phrases, shapes[fila], revisions[fila], operations[fila], milestones[fila],
					milestoneUnits[fila], currencyShapes[fila], amounts[fila], currencyCodes[fila],
					validFrom[fila], validUntil[fila], validity[fila], askedOn[fila]);

			return said;
		}

		// Cada hito publicado dicho con su unidad, que es la columna que el DTO de la puerta lleva.
		internal static string[] EachPublishedMilestone(
			ModelSpecification specification, DateTime asOf, string language)
		{
			Phrases phrases = Phrases.Of(language);
			int[] milestones = specification.PublishedPriceMilestoneDistancesAsOf(asOf);
			string[] units = specification.PublishedPriceMilestoneUnitsAsOf(asOf);
			string[] said = new string[milestones.Length];

			for (int fila = 0; fila < milestones.Length; fila++)
				said[fila] = TheWording.TheMilestone(phrases, milestones[fila], units[fila]);

			return said;
		}

		// EL LIBRO DE COSTOS. Las tres lecturas del gasto pasan por el mismo portador y por el mismo
		// marco, y lo unico que cambia entre ellas es COMO se pregunto.
		internal static string TheSpend(CostLedger ledger, DateTime from, DateTime to, string language)
		{
			return TheSpend(ledger.TheSpendClauseBetween(from, to), language);
		}

		internal static string TheSpendPerCurrency(
			CostLedger ledger, DateTime from, DateTime to, string language)
		{
			return TheSpend(ledger.TheSpendPerCurrencyClauseBetween(from, to), language);
		}

		internal static string TheTwoPeriodsOfSpendCompared(
			CostLedger ledger, DateTime from1, DateTime to1, DateTime from2, DateTime to2,
			string language)
		{
			return TheSpend(
				ledger.TheTwoPeriodsOfSpendComparedClause(from1, to1, from2, to2), language);
		}

		internal static string TheSpend(ASpendClause clause, string language)
		{
			return TheWording.TheSpend(
				Phrases.Of(language),
				clause.HowItWasAsked(), clause.TheShapesOfItsPeriods(),
				clause.TheInstantsEachPeriodStartsAt(), clause.TheInstantsEachPeriodEndsAt(),
				clause.TheCurrencyCountOfEachPeriod(), clause.ThePeriodOfEachCurrency(),
				clause.TheViewsOfEachCurrency(),
				clause.TheShapesOfEachCashOut(), clause.TheCashOutsAsSaid(),
				clause.TheCodesOfEachCashOut(), clause.TheShapesOfEachConsumed(),
				clause.TheConsumedAsSaid(), clause.TheCodesOfEachConsumed(),
				clause.TheCurrencyOfEachShare(), clause.TheShapesOfEachWhole(),
				clause.TheWholesAsSaid(), clause.TheCodesOfEachWhole(),
				clause.TheSubjectsOfEachShare(), clause.TheMonthsEachShareIsSpreadOver(),
				clause.TheInstantsEachShareWasIncurred(), clause.TheShapesOfEachShare(),
				clause.TheSharesAsSaid(), clause.TheCodesOfEachShare());
		}

		// LAS TRES SITUACIONES DEL COSTO POR DISTANCIA QUE SI TIENEN TRAMO no nombran ningun motivo:
		// su clave no lleva ese hueco, asi que no hay razon que decir y no se inventa una.
		internal static string TheCostPerDistance(ACostPerDistanceClause clause, string language)
		{
			return TheCostPerDistanceSaid(clause, "", language);
		}

		// LA QUE NO TIENE TRAMO SI LO NOMBRA, Y EL MOTIVO ES DE LA SERIE DEL ODOMETRO: llega como su
		// propia clausula, se dice con el MISMO renderizador y en el MISMO idioma, y esta funcion la
		// pone en su hueco. Es el cableado que la puerta hace, y es la costura que CL-9 dejo abierta:
		// hasta este turno el motivo cruzaba ya redactado en espanol y se citaba igual en los dos.
		internal static string TheCostPerDistance(
			ACostPerDistanceClause clause, ARateClause why, string language)
		{
			return TheCostPerDistanceSaid(clause, TheRateOfUse(why, language), language);
		}

		private static string TheCostPerDistanceSaid(
			ACostPerDistanceClause clause, string why, string language)
		{
			return TheWording.TheCostPerDistance(
				Phrases.Of(language),
				clause.TheShapeItHas(), clause.TheInstantItStartsAt(), clause.TheInstantItEndsAt(),
				clause.TheInstantOfTheAnchor(), clause.TheDistanceTravelled(),
				clause.TheUnitTheDistanceIsIn(), why,
				clause.TheShapesOfEachFigure(), clause.TheFiguresAsSaid(),
				clause.TheCodesOfEachFigure());
		}

		// LA SERIE DEL ODOMETRO. El ritmo de un periodo y los dos periodos comparados pasan por el
		// mismo portador y por el mismo marco, y lo unico que cambia entre ellos es COMO se pregunto.
		internal static string TheRateOfUse(
			MaintenanceSchedule schedule, DateTime from, DateTime to, string language)
		{
			return TheRateOfUse(schedule.TheRateOfUseClause(from, to), language);
		}

		internal static string TheTwoPeriodsOfUseCompared(
			MaintenanceSchedule schedule, DateTime from1, DateTime to1, DateTime from2, DateTime to2,
			string language)
		{
			return TheRateOfUse(
				schedule.TheTwoPeriodsOfUseComparedClause(from1, to1, from2, to2), language);
		}

		internal static string TheRateOfUse(ARateClause clause, string language)
		{
			return TheWording.TheRateOfUse(
				Phrases.Of(language),
				clause.HowItWasAsked(), clause.TheShapesOfItsPeriods(),
				clause.TheInstantsEachPeriodStartsAt(), clause.TheInstantsEachPeriodEndsAt(),
				clause.TheUnitsEachPeriodIsIn(), clause.TheSecondsBothEndsFellIn(),
				clause.TheDistancesEachPeriodTravelled(), clause.TheUnitsEachDistanceIsIn(),
				clause.TheDistancesPerDayAsSaid(), clause.TheShapesOfEachSpan(),
				clause.TheSpansAsSaid(), clause.ThePeriodOfEachReading(),
				clause.TheValuesOfEachReading(), clause.TheUnitsOfEachReading(),
				clause.TheInstantsOfEachReading(), clause.TheShapeOfItsChange(),
				clause.TheChangeAsSaid());
		}

		// EL HISTORIAL. El juego de una clase pasa por su portador -- forma, clase, conteo y la mas
		// reciente -- y cada fila se dice con las MISMAS columnas que la puerta lee, que es lo que hace
		// que la celda de la lectura y la frase de la fila no se puedan separar.
		internal static string TheCompletions(
			MaintenanceSchedule schedule, OperationKindCode kind, string language)
		{
			return TheCompletions(schedule.TheCompletionsClauseFor(kind), language);
		}

		internal static string TheCompletions(ACompletionsClause clause, string language)
		{
			return TheWording.TheCompletions(
				Phrases.Of(language),
				clause.TheShapeItHas(), clause.TheKindItIsAbout(), clause.HowManyCompletions(),
				clause.TheInstantOfTheNewest(), clause.TheShapeOfTheNewestReading(),
				clause.TheValueOfTheNewestReading(), clause.TheUnitOfTheNewestReading());
		}

		internal static string[] EachCompletionOf(
			MaintenanceSchedule schedule, OperationKindCode kind, string language)
		{
			return EachCompletion(
				language, TheKindOfEachRow(schedule, kind), schedule.CompletionInstantsOf(kind),
				schedule.CompletionReadingShapesOf(kind), schedule.CompletionReadingValuesOf(kind),
				schedule.CompletionReadingUnitsOf(kind));
		}

		internal static string[] EachCompletionReadingOf(
			MaintenanceSchedule schedule, OperationKindCode kind, string language)
		{
			return EachCompletionReading(
				language, schedule.CompletionReadingShapesOf(kind),
				schedule.CompletionReadingValuesOf(kind), schedule.CompletionReadingUnitsOf(kind));
		}

		internal static string[] EachCompletionOnRecord(
			MaintenanceSchedule schedule, string language)
		{
			return EachCompletion(
				language, schedule.CompletionKindsOnRecord(),
				schedule.CompletionInstantsOnRecord(), schedule.CompletionReadingShapesOnRecord(),
				schedule.CompletionReadingValuesOnRecord(),
				schedule.CompletionReadingUnitsOnRecord());
		}

		internal static string[] EachCompletionReadingOnRecord(
			MaintenanceSchedule schedule, string language)
		{
			return EachCompletionReading(
				language, schedule.CompletionReadingShapesOnRecord(),
				schedule.CompletionReadingValuesOnRecord(),
				schedule.CompletionReadingUnitsOnRecord());
		}

		private static string[] EachCompletion(
			string language, string[] kinds, string[] instants, string[] shapes, int[] readings,
			string[] units)
		{
			Phrases phrases = Phrases.Of(language);
			string[] said = new string[kinds.Length];

			for (int fila = 0; fila < said.Length; fila++)
				said[fila] = TheWording.TheCompletionOnTheRecord(
					phrases, kinds[fila], instants[fila], shapes[fila], readings[fila], units[fila]);

			return said;
		}

		private static string[] EachCompletionReading(
			string language, string[] shapes, int[] readings, string[] units)
		{
			Phrases phrases = Phrases.Of(language);
			string[] said = new string[shapes.Length];

			for (int fila = 0; fila < said.Length; fila++)
				said[fila] = TheWording.TheReadingOfACompletion(
					phrases, shapes[fila], readings[fila], units[fila]);

			return said;
		}

		// La clase de cada fila del juego de UNA clase, que es la misma en todas: el portador la dice
		// una vez porque la puso quien pregunto, y la frase de cada fila la nombra igual.
		private static string[] TheKindOfEachRow(
			MaintenanceSchedule schedule, OperationKindCode kind)
		{
			string said = schedule.TheCompletionsClauseFor(kind).TheKindItIsAbout();
			string[] kinds = new string[schedule.CompletionInstantsOf(kind).Length];

			for (int fila = 0; fila < kinds.Length; fila++) kinds[fila] = said;

			return kinds;
		}

		// UN NUMERO DE UNA COLUMNA, DICHO COMO LA FRASE LO DICE. Una prueba que arma el pedazo que
		// espera a partir de una COLUMNA del registro tiene que agrupar los miles igual que el
		// renderizador: la columna cruza el NUMERO y la frase lo dice con los separadores del idioma.
		// Sin esto la afirmacion compara un 20000 contra un 20.000 y se pone roja por el formato,
		// que no es la ley que esta defendiendo.
		internal static string ANumberAsTheSentenceSaysIt(int value, string language)
		{
			return Phrases.Of(language).Grouped(value);
		}

		// Cada lectura del periodo en su frase, como la puerta compone esa columna: de las cuatro que
		// el registro ya lleva, y con la fuente dicha por el catalogo.
		internal static string[] EachReadingOnTheRecord(
			MaintenanceSchedule schedule, DateTime from, DateTime to, string language)
		{
			Phrases phrases = Phrases.Of(language);
			string[] instants = schedule.ReadingInstantsBetween(from, to);
			int[] values = schedule.ReadingValuesBetween(from, to);
			string[] units = schedule.ReadingUnitsBetween(from, to);
			string[] sources = schedule.ReadingSourcesBetween(from, to);
			string[] said = new string[instants.Length];

			for (int fila = 0; fila < instants.Length; fila++)
				said[fila] = TheWording.TheReadingOnTheRecord(
					phrases, values[fila], units[fila], instants[fila], sources[fila]);

			return said;
		}

		// Las filas del libro: el dinero de cada una, su vida util y su frase entera, como la puerta
		// las compone.
		internal static string[] EachCostEntryMoney(
			CostLedger ledger, DateTime from, DateTime to, string language)
		{
			Phrases phrases = Phrases.Of(language);
			string[] shapes = ledger.CostEntryCurrencyShapesBetween(from, to);
			string[] amounts = ledger.CostEntryAmountsBetween(from, to);
			string[] codes = ledger.CostEntryCurrencyCodesBetween(from, to);
			string[] said = new string[shapes.Length];

			for (int fila = 0; fila < shapes.Length; fila++)
				said[fila] = TheWording.TheMoney(phrases, shapes[fila], amounts[fila], codes[fila]);

			return said;
		}

		internal static string[] EachCostEntryExpectedLife(
			CostLedger ledger, DateTime from, DateTime to, string language)
		{
			Phrases phrases = Phrases.Of(language);
			string[] classifications = ledger.CostEntryClassificationsBetween(from, to);
			int[] lives = ledger.CostEntryExpectedLivesBetween(from, to);
			string[] said = new string[lives.Length];

			for (int fila = 0; fila < lives.Length; fila++)
				said[fila] = TheWording.TheExpectedLife(
					phrases, classifications[fila], lives[fila]);

			return said;
		}

		internal static string[] EachCostEntry(
			CostLedger ledger, DateTime from, DateTime to, string language)
		{
			Phrases phrases = Phrases.Of(language);
			string[] instants = ledger.CostEntryInstantsBetween(from, to);
			string[] subjects = ledger.CostEntrySubjectsBetween(from, to);
			string[] shapes = ledger.CostEntryCurrencyShapesBetween(from, to);
			string[] amounts = ledger.CostEntryAmountsBetween(from, to);
			string[] codes = ledger.CostEntryCurrencyCodesBetween(from, to);
			string[] classifications = ledger.CostEntryClassificationsBetween(from, to);
			int[] lives = ledger.CostEntryExpectedLivesBetween(from, to);
			string[] said = new string[instants.Length];

			for (int fila = 0; fila < instants.Length; fila++)
				said[fila] = TheWording.TheCostEntry(
					phrases, classifications[fila], shapes[fila], amounts[fila], codes[fila],
					subjects[fila], instants[fila], lives[fila]);

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
