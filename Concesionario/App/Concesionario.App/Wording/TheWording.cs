using System;
using System.Text;

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
		private const string TheDecidedAnswer = "Settled";
		private const string TheAnswerThatNothingWasPublished = "NotPublished";
		private const string ThePublishedShape = "Published";
		private const string TheMeasuredHalf = "Measured";
		private const string TheLimitsAreNamed = "Named";
		private const string TheSuspensionByAUnitMismatch = "UnitMismatch";
		private const string HalvesThatIncludeThisVehicle = "GeneralAndPersonalTogether";
		private const string NoReasonToGive = "";

		/// <summary>
		/// LO QUE VA ENTRE DOS CLAUSULAS ENTERAS de una lista cuyo largo no se sabe de antemano. No es
		/// una clave del catalogo a proposito: una clave que solo lleva un espacio no es una frase, y
		/// el catalogo se comprueba pidiendole que ninguna este sin palabras. Cada clausula ya termina
		/// en punto, asi que lo unico que falta entre dos es la separacion.
		/// </summary>
		private const string BetweenTwoClauses = " ";

		/// <summary>
		/// LO QUE VA ENTRE DOS GRADOS DE COMBUSTIBLE de una lista cuyo largo no se sabe de antemano, y
		/// no es una clave del catalogo por la misma razon que lo anterior: una clave que solo lleva una
		/// coma no es una frase, y ninguna puede quedarse sin palabras.
		/// </summary>
		private const string BetweenTwoGrades = ", ";

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

		/// <summary>
		/// LO QUE LA AGENCIA PUBLICO, sin ningun hecho de ningun vehiculo adentro. Las dos mitades van
		/// siempre juntas: una sola no le dice nada a quien no sabe cuanto maneja.
		/// </summary>
		public static string ThePublishedWarrantyEnvelope(
			Phrases phrases, string shape, int months, int distance, string unit, string publishedOn)
		{
			if (phrases == null) throw new ArgumentNullException(nameof(phrases));

			if (shape != ThePublishedShape) return phrases.Say("envelope.notPublished");

			return phrases.Say("envelope.published",
				("months", phrases.Months(months)),
				("distance", ADistance(phrases, distance, unit)),
				("day", phrases.ADay(publishedOn)));
		}

		/// <summary>
		/// DONDE ESTA PARADO ESTE VEHICULO contra su sobre: la forma elige el motivo, y la clausula --
		/// lo publicado, lo que le queda a cada mitad y contra que odometro -- va detras, en la
		/// cantidad que la forma de la clausula manda. La ley de que esto no adjudica nada la pone la
		/// frase y no quien compone, para que no dependa de que se acuerde.
		/// </summary>
		public static string TheWarrantyEnvelope(
			Phrases phrases, string shape, string standing, string unitThatDidNotMatch,
			string clauseShape, string publishedShape, int monthsPublished, int distancePublished,
			string unitPublished, string publishedOn, int monthsLeft, int monthsLimit,
			string distanceHalfShape, int[] distanceLeft, int[] distanceLimit, string distanceUnit,
			string otherDistanceUnit, string whoseDistance, string whichFirst,
			string odometerShape, int reading, string readingUnit, string observedOn,
			int deliveryReading, string deliveryUnit, string deliveredOn)
		{
			if (phrases == null) throw new ArgumentNullException(nameof(phrases));

			string published = ThePublishedWarrantyEnvelope(
				phrases, publishedShape, monthsPublished, distancePublished, unitPublished,
				publishedOn);

			return phrases.Say("envelope.said",
				("reason", TheEnvelopeReason(phrases, shape, standing, unitPublished,
					unitThatDidNotMatch)),
				("clause", phrases.Say("envelope.clause." + clauseShape,
					("published", published),
					("remainder", TheRemainingEnvelope(
						phrases, monthsLeft, monthsLimit, distanceHalfShape, distanceLeft,
						distanceLimit, distanceUnit, unitPublished, otherDistanceUnit,
						whoseDistance, whichFirst)),
					("odometer", TheOdometerClause(
						phrases, odometerShape, reading, readingUnit, observedOn,
						deliveryReading, deliveryUnit, deliveredOn)))),
				("law", phrases.Say("envelope.law")));
		}

		private static string TheEnvelopeReason(
			Phrases phrases, string shape, string standing, string unitPublished,
			string unitThatDidNotMatch)
		{
			if (shape == TheDecidedAnswer || shape == TheAnswerThatNothingWasPublished)
				return phrases.Say(
					"envelope.reason.Settled",
					("standing", phrases.HowItIsSaid("canon.standing", standing)));

			return phrases.Say("envelope.reason." + shape,
				("limit", unitPublished), ("other", unitThatDidNotMatch));
		}

		private static string TheRemainingEnvelope(
			Phrases phrases, int monthsLeft, int monthsLimit, string distanceHalfShape,
			int[] distanceLeft, int[] distanceLimit, string distanceUnit, string unitPublished,
			string otherDistanceUnit, string whoseDistance, string whichFirst)
		{
			return phrases.Say("envelope.remainder",
				("time", AHalf(
					phrases, "envelope.timeHalf", monthsLeft, phrases.Months(monthsLeft),
					phrases.Months(-monthsLeft), phrases.Months(monthsLimit))),
				("distance", TheDistanceHalf(
					phrases, distanceHalfShape, distanceLeft, distanceLimit, distanceUnit,
					unitPublished, otherDistanceUnit, whoseDistance)),
				("first", phrases.Say("envelope.first." + whichFirst)));
		}

		private static string TheDistanceHalf(
			Phrases phrases, string shape, int[] left, int[] limit, string unit,
			string unitPublished, string otherUnit, string whoseDistance)
		{
			if (shape != TheMeasuredHalf)
				return phrases.Say("envelope.distanceHalf.NotMeasured",
					("why", phrases.Say("envelope.why." + shape,
						("limit", unitPublished),
						("other", otherUnit),
						("whose", phrases.Say("envelope.whose." + whoseDistance)))));

			int amount = TheOnlyAmountOf(left);

			return AHalf(
				phrases, "envelope.distanceHalf", amount,
				ADistance(phrases, amount, unit), ADistance(phrases, -amount, unit),
				ADistance(phrases, TheOnlyAmountOf(limit), unit));
		}

		/// <summary>
		/// UNA MITAD DICHA: el signo elige el marco y el limite alcanzado exacto tiene el suyo, porque
		/// "0 pasados del limite" se leeria como un defecto y no como una respuesta.
		/// </summary>
		private static string AHalf(
			Phrases phrases, string family, int left, string ahead, string over, string limit)
		{
			if (left == 0) return phrases.Say(family + ".AtTheLimit", ("limit", limit));

			if (left < 0)
				return phrases.Say(
					family + ".PastTheLimit", ("amount", over), ("limit", limit));

			return phrases.Say(
				family + (left == 1 ? ".BeforeTheLimit.one" : ".BeforeTheLimit.many"),
				("amount", ahead), ("limit", limit));
		}

		private static string TheOdometerClause(
			Phrases phrases, string shape, int reading, string readingUnit, string observedOn,
			int deliveryReading, string deliveryUnit, string deliveredOn)
		{
			if (shape != TheMeasuredHalf) return phrases.Say("envelope.odometer.Absent");

			return phrases.Say("envelope.odometer.Measured",
				("reading", ADistance(phrases, reading, readingUnit)),
				("observedOn", phrases.ADay(observedOn)),
				("deliveryReading", ADistance(phrases, deliveryReading, deliveryUnit)),
				("deliveredOn", phrases.ADay(deliveredOn)));
		}

		/// <summary>
		/// CUALQUIER RESPUESTA DE LA HOJA DEL FABRICANTE: lo que el item tiene que decir con la
		/// revision que lo publico en la misma frase, o la constancia de que no hay nada publicado
		/// nombrando lo que se preguntó. Una respuesta sin procedencia no existe -- es lo que separa
		/// este modulo de un PDF --, y por eso las dos mitades se dicen aqui y no en cada item.
		/// </summary>
		public static string TheSpecificationAnswer(
			Phrases phrases, string shape, string revision, string clause, string subject)
		{
			if (phrases == null) throw new ArgumentNullException(nameof(phrases));

			if (shape == ThePublishedShape)
				return phrases.Say(
					"specification.published", ("clause", clause), ("revision", revision));

			return phrases.Say("specification.notPublished", ("subject", subject));
		}

		/// <summary>
		/// QUE FLUIDO DE UNA CLASE LLEVA EL MODELO: la clase, el grado, la capacidad con su unidad y
		/// cada cuanto se cambia. La capacidad lleva los digitos con que se publico y la coma del
		/// idioma.
		/// </summary>
		public static string TheFluid(
			Phrases phrases, string publication, string revision, string fluidKind, string grade,
			string capacity, string capacityUnit, string intervalHalves, int intervalDistance,
			int intervalDays, string intervalUnit)
		{
			if (phrases == null) throw new ArgumentNullException(nameof(phrases));

			string said = phrases.HowItIsSaid("canon.fluid", fluidKind);

			return TheSpecificationAnswer(
				phrases, publication, revision,
				phrases.Say("fluid.said",
					("fluid", said),
					("grade", grade),
					("capacity", phrases.ANumber(capacity)),
					("unit", capacityUnit),
					("interval", TheInterval(
						phrases, intervalHalves, intervalDistance, intervalDays, intervalUnit))),
				phrases.Say("sheet.subject.Fluid", ("fluid", said)));
		}

		/// <summary>
		/// QUE LLANTA LLEVA EL MODELO EN UNA POSICION: la medida, la presion en frio con la unidad en
		/// que se publico y las clasificaciones. Aqui no se convierte ninguna unidad a ninguna otra.
		/// </summary>
		public static string TheTyre(
			Phrases phrases, string publication, string revision, string position, string size,
			string pressure, string pressureUnit, string ratings)
		{
			if (phrases == null) throw new ArgumentNullException(nameof(phrases));

			string said = phrases.HowItIsSaid("canon.tyre", position).ToLowerInvariant();

			return TheSpecificationAnswer(
				phrases, publication, revision,
				phrases.Say("tyre.said",
					("position", said),
					("size", size),
					("pressure", phrases.ANumber(pressure)),
					("unit", pressureUnit),
					("ratings", ratings)),
				phrases.Say("sheet.subject.Tyre", ("position", said)));
		}

		/// <summary>
		/// QUE COMBUSTIBLE ACEPTA EL MODELO: todos los grados publicados, cada uno con su postura, en el
		/// orden en que se publicaron. Ninguna postura se omite y ninguna se resume: un prohibido callado
		/// es un motor danado. El texto de cada grado es del fabricante y no se traduce.
		/// </summary>
		public static string TheFuel(
			Phrases phrases, string publication, string revision, string[] grades,
			string[] standings)
		{
			if (phrases == null) throw new ArgumentNullException(nameof(phrases));

			return TheSpecificationAnswer(
				phrases, publication, revision,
				phrases.Say("fuel.said", ("grades", EachGrade(phrases, grades, standings))),
				phrases.Say("sheet.subject.Fuel"));
		}

		/// <summary>
		/// CADA CUANTO SE HACE UNA OPERACION, y -- cuando se pregunto por un vehiculo -- donde queda la
		/// distancia que le queda frente a ese intervalo. Las dos mitades van juntas o es un folleto, y
		/// lo que las separa es puntuacion, que es del idioma.
		/// </summary>
		public static string TheServiceInterval(
			Phrases phrases, string publication, string revision, string operation,
			string intervalHalves, int intervalDistance, int intervalDays, string intervalUnit,
			string halvesAnswered, string remainderShape, int distanceRemaining,
			string remainingUnit)
		{
			if (phrases == null) throw new ArgumentNullException(nameof(phrases));

			string published = phrases.Say("interval.said",
				("operation", phrases.HowItIsSaid("canon.operation", operation)),
				("interval", TheInterval(
					phrases, intervalHalves, intervalDistance, intervalDays, intervalUnit)));

			return TheSpecificationAnswer(
				phrases, publication, revision,
				BothHalves(phrases, halvesAnswered, published,
					phrases.Say("interval." + remainderShape,
						("amount", ADistance(phrases, distanceRemaining, remainingUnit)),
						("unit", intervalUnit))),
				phrases.Say("sheet.subject.ServiceInterval",
					("operation",
						phrases.HowItIsSaid("canon.operation", operation).ToLowerInvariant())));
		}

		/// <summary>
		/// LA REGLA DEL FABRICANTE SOBRE SERVICIO EN OTRO TALLER y que conserva la cobertura, y --
		/// cuando se pregunto por un vehiculo -- sus conteos, dichos como conteos. Dice el alcance y la
		/// posicion; nunca dice que una reclamacion se vaya a pagar.
		/// </summary>
		public static string TheServicingElsewhereRule(
			Phrases phrases, string publication, string revision, string rule,
			string whatPreservesCover, string halvesAnswered, int externalWithEvidence,
			int externalWithoutEvidence, int missedMaintenance)
		{
			if (phrases == null) throw new ArgumentNullException(nameof(phrases));

			string published = phrases.Say("servicing.said",
				("rule", rule), ("keeps", whatPreservesCover));

			return TheSpecificationAnswer(
				phrases, publication, revision,
				BothHalves(phrases, halvesAnswered, published,
					phrases.Say("servicing.against",
						("withEvidence", phrases.Grouped(externalWithEvidence)),
						("withoutEvidence", phrases.Grouped(externalWithoutEvidence)),
						("missed", phrases.Grouped(missedMaintenance)))),
				phrases.Say("sheet.subject.ServicingPolicy"));
		}

		/// <summary>
		/// CADA CUANTO, con la mitad o las dos mitades que se publicaron. La que no se publico no se
		/// nombra: un cero ahi se leeria como un plazo cumplido desde siempre.
		/// </summary>
		private static string TheInterval(
			Phrases phrases, string shape, int distance, int days, string unit)
		{
			if (shape.Length == 0) return "";

			return phrases.Say("interval." + shape,
				("distance", ADistance(phrases, distance, unit)),
				("days", phrases.Days(days)));
		}

		/// <summary>
		/// LAS MITADES QUE UNA RESPUESTA CONTESTA. Con las dos, lo que va entre ellas es puntuacion, y
		/// la puntuacion la pone el idioma y no quien compone.
		/// </summary>
		private static string BothHalves(
			Phrases phrases, string halvesAnswered, string published, string received)
		{
			if (halvesAnswered != HalvesThatIncludeThisVehicle) return published;

			return phrases.Say(
				"sheet.bothHalves", ("published", published), ("received", received));
		}

		private static string EachGrade(Phrases phrases, string[] grades, string[] standings)
		{
			StringBuilder said = new StringBuilder();

			for (int each = 0; each < grades.Length; each++)
			{
				if (each > 0) said.Append(BetweenTwoGrades);

				said.Append(phrases.Say("fuel.grade",
					("grade", grades[each]),
					("standing", phrases.HowItIsSaid("canon.fuelStanding", standings[each]))));
			}

			return said.ToString();
		}

		/// <summary>
		/// EL ALCANCE DE GARANTIA PUBLICADO ENTERO, sin ningun hecho de ningun vehiculo adentro: los
		/// sistemas que declara adentro con sus limites propios y las exclusiones con su condicion. La
		/// ley de que esto no adjudica nada la pone la frase y no quien compone, para que no dependa
		/// de que se acuerde.
		/// </summary>
		public static string TheCoverageScope(
			Phrases phrases, string publication, string revision, string[] systemCodes,
			string[] systemLimitShapes, int[] systemMonths, int[] systemDistances,
			string[] systemDistanceUnits, string[] exclusionSystems, string[] exclusionTexts,
			string[] exclusionConditions)
		{
			if (phrases == null) throw new ArgumentNullException(nameof(phrases));

			if (publication != ThePublishedShape)
				return NothingWasPublishedAbout(phrases, phrases.Say("coverage.subject.scope"));

			return TheSpecificationAnswer(
				phrases, publication, revision,
				phrases.Say("coverage.scope",
					("systems", TheSystemsInsideTheScope(
						phrases, systemCodes, systemLimitShapes, systemMonths, systemDistances,
						systemDistanceUnits)),
					("exclusions", TheExclusions(
						phrases, exclusionSystems, exclusionTexts, exclusionConditions)),
					("law", phrases.Say("coverage.law"))),
				"");
		}

		/// <summary>
		/// DONDE ESTA PARADO UN SISTEMA contra el alcance publicado: la forma elige el marco, y la
		/// clausula -- que paso de la ley decidio, contra que limites y con que exclusiones -- va
		/// detras. La ley de que esto no adjudica nada la pone la frase, igual que en el resumen.
		/// </summary>
		public static string TheCoverage(
			Phrases phrases, string publication, string revision, string systemCode, string shape,
			string standing, string reason, string limitsShape, int months, int distance,
			string distanceUnit, string limitUnit, string odometerUnit, string[] exclusionSystems,
			string[] exclusionTexts, string[] exclusionConditions)
		{
			if (phrases == null) throw new ArgumentNullException(nameof(phrases));

			string subject = phrases.Say("coverage.subject.system", ("system", systemCode));

			if (publication != ThePublishedShape)
				return NothingWasPublishedAbout(phrases, subject);

			string clause = TheCoverageClause(
				phrases, reason, limitsShape, months, distance, distanceUnit,
				exclusionSystems, exclusionTexts, exclusionConditions);

			string said = shape == TheSuspensionByAUnitMismatch
				? phrases.Say("coverage.unitMismatch",
					("subject", subject), ("limit", limitUnit), ("other", odometerUnit),
					("clause", clause))
				: phrases.Say("coverage.settled",
					("subject", subject),
					("standing", phrases.HowItIsSaid("canon.standing", standing)),
					("clause", clause));

			return TheSpecificationAnswer(phrases, publication, revision, said, "");
		}

		/// <summary>
		/// UNA EXCLUSION PUBLICADA: sobre que sistema, que excluye y bajo que condicion. Las tres,
		/// porque las tres son lo que la persona tiene que leer para saber si le toca. El texto y la
		/// condicion son del registro del fabricante y no se traducen.
		/// </summary>
		public static string AnExclusion(
			Phrases phrases, string system, string text, string condition)
		{
			if (phrases == null) throw new ArgumentNullException(nameof(phrases));

			return phrases.Say("coverage.exclusion",
				("system", system), ("text", text), ("condition", condition));
		}

		private static string NothingWasPublishedAbout(Phrases phrases, string subject)
		{
			return phrases.Say("coverage.notPublished",
				("answer", TheSpecificationAnswer(
					phrases, TheAnswerThatNothingWasPublished, "", "", subject)),
				("law", phrases.Say("coverage.law")));
		}

		/// <summary>
		/// CONTRA QUE SE MIDIO una respuesta de cobertura. El desacuerdo de unidades no da ningun paso
		/// que contar -- su motivo ya esta en el marco de la respuesta --, asi que ahi la clausula es
		/// la letra chica y la ley, y nada mas.
		/// </summary>
		private static string TheCoverageClause(
			Phrases phrases, string reason, string limitsShape, int months, int distance,
			string distanceUnit, string[] systems, string[] texts, string[] conditions)
		{
			string law = phrases.Say("coverage.law");
			bool anyExclusion = texts.Length > 0;

			if (reason == NoReasonToGive)
				return anyExclusion
					? phrases.Say("coverage.clause.reasonless",
						("exclusions", TheExclusions(phrases, systems, texts, conditions)),
						("law", law))
					: law;

			string said = phrases.Say("coverage.reason." + reason,
				("limits", TheLimitsInForce(
					phrases, limitsShape, months, distance, distanceUnit)));

			return anyExclusion
				? phrases.Say("coverage.clause.withExclusions",
					("reason", said),
					("exclusions", TheExclusions(phrases, systems, texts, conditions)),
					("law", law))
				: phrases.Say("coverage.clause", ("reason", said), ("law", law));
		}

		/// <summary>
		/// LOS LIMITES QUE RIGEN, con la mitad o las mitades que se publicaron. La que no se publico no
		/// se nombra: un cero ahi se leeria como un limite cumplido desde siempre.
		/// </summary>
		private static string TheLimitsInForce(
			Phrases phrases, string shape, int months, int distance, string unit)
		{
			if (shape != TheLimitsAreNamed) return "";

			if (unit.Length == 0) return phrases.Months(months);

			string byDistance = ADistance(phrases, distance, unit);

			if (months == 0) return byDistance;

			return phrases.Say("coverage.limits.both",
				("months", phrases.Months(months)), ("distance", byDistance));
		}

		private static string TheSystemsInsideTheScope(
			Phrases phrases, string[] codes, string[] limitShapes, int[] months, int[] distances,
			string[] units)
		{
			if (codes.Length == 0) return phrases.Say("coverage.systems.none");

			StringBuilder said = new StringBuilder();

			for (int each = 0; each < codes.Length; each++)
			{
				if (each > 0) said.Append(BetweenTwoClauses);

				said.Append(limitShapes[each] == TheLimitsAreNamed
					? phrases.Say("coverage.system.withLimits",
						("code", codes[each]),
						("limits", TheLimitsInForce(
							phrases, limitShapes[each], months[each], distances[each], units[each])))
					: phrases.Say("coverage.system", ("code", codes[each])));
			}

			return said.ToString();
		}

		private static string TheExclusions(
			Phrases phrases, string[] systems, string[] texts, string[] conditions)
		{
			if (texts.Length == 0) return phrases.Say("coverage.exclusions.none");

			StringBuilder said = new StringBuilder();

			for (int each = 0; each < texts.Length; each++)
			{
				if (each > 0) said.Append(BetweenTwoClauses);

				said.Append(AnExclusion(phrases, systems[each], texts[each], conditions[each]));
			}

			return said.ToString();
		}

		private static string ADistance(Phrases phrases, int amount, string unit)
		{
			return phrases.Say(
				"quantity.distance", ("amount", phrases.Grouped(amount)), ("unit", unit));
		}

		private static int TheOnlyAmountOf(int[] amount)
		{
			return amount.Length == 1 ? amount[0] : 0;
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
