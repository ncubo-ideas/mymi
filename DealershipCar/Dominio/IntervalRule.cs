using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El codigo con el que la base de un intervalo cruza el wire hacia adentro. NO es un
	/// septimo conjunto cerrado: la base ES el subtipo de <see cref="IntervalRule"/>, asi que
	/// este codigo lo resuelve una fabrica (<see cref="IntervalRule.Of"/>) y no un catalogo.
	/// </summary>
	internal enum IntervalBasisCode
	{
		/// <summary>Vence con la PRIMERA de las dos dimensiones que llegue (ley L1). Exige los dos limites.</summary>
		TimeOrDistance,

		/// <summary>Vence solo cuando se alcanzan LAS DOS (ley L2). Exige los dos limites.</summary>
		TimeAndDistance,

		/// <summary>Vence por tiempo cumplido. Exige limite de tiempo y NINGUN limite de distancia.</summary>
		TimeOnly,

		/// <summary>Vence por distancia recorrida. Exige limite de distancia y NINGUN limite de tiempo.</summary>
		DistanceOnly,

		/// <summary>
		/// Vence cuando llega un cruce de umbral recibido, y solo puede ADELANTAR un
		/// vencimiento, nunca empujarlo (ley L3). No lleva limite de tiempo ni de distancia.
		/// </summary>
		ConditionOnly,
	}

	/// <summary>
	/// La ley que decide cuando vence una operacion de mantenimiento. La base del intervalo ES el
	/// subtipo, nunca un campo. Cada subtipo contesta dos preguntas: donde esta parada la
	/// operacion (<see cref="Assess"/>, leyes L1 y L2) y donde queda el proximo punto base
	/// (<see cref="NextOccurrenceFromActualCompletion"/>, ley L4).
	/// </summary>
	internal abstract class IntervalRule
	{
		/// <summary>
		/// Donde esta parada la operacion, dados el ancla, el instante evaluado, la ultima lectura
		/// usable y el escalamiento del perfil de uso. El instante SE RECIBE: el dominio no lee un
		/// reloj (§7). Funcion pura, asi que una suspension se cura sola cuando llegan los hechos
		/// (GAP G4).
		/// </summary>
		internal abstract DueAssessment Assess(
			BasePoint from, DateTime asOf, UsableReading latestUsable, IntervalLengthFactor factor);

		/// <summary>
		/// LEY L4 (OINV-26): la proxima ocurrencia se mide desde el instante y la lectura en que la
		/// operacion se completo DE VERDAD, nunca desde la grilla teorica en que estaba agendada.
		/// Abstracta porque cada base ancla dimensiones distintas.
		/// </summary>
		internal abstract BasePoint NextOccurrenceFromActualCompletion(
			DateTime completedAt, int reading, DistanceUnit unit);

		/// <summary>
		/// Lo que toda evaluacion que mida algo exige: un ancla, un escalamiento, una respuesta
		/// sobre la ultima lectura usable, y un instante no anterior al ancla. Los cuatro son
		/// violaciones de contrato y revientan (E32). ConditionOnly no la llama: no mide nada.
		/// </summary>
		protected static void RequireAnAnchorAFactorAndAReading(
			BasePoint from, DateTime asOf, UsableReading latestUsable, IntervalLengthFactor factor)
		{
			if (from == null)
				throw new DomainException(
					"No hay desde donde medir el intervalo: falta el punto base. Una operacion "
					+ "sin ancla no es una operacion que no vencio, es una que no se puede "
					+ "evaluar.");

			if (factor == null)
				throw new DomainException(
					"Falta el escalamiento del perfil de uso. Que no escale nada es el factor de "
					+ "Standard, que existe; no es la ausencia del factor.");

			if (latestUsable == null)
				throw new DomainException(
					"Falta la respuesta sobre la ultima lectura usable. Que no haya ninguna se "
					+ "dice con UsableReading.None, que contesta que no hay; un null no contesta "
					+ "nada y obliga a cada llamador a acordarse solo (E45).");

			if (asOf < from.Instant)
				throw new DomainException(
					$"Se pidio evaluar al {asOf} un intervalo anclado en {from.Instant}: el "
					+ "instante que se evalua es anterior al ancla. No se evalua el pasado de un "
					+ "ancla que todavia no existia.");
		}

		/// <summary>
		/// OINV-20: LA BASE NUNCA SE INFIERE. Rechaza los limites que no concuerdan con la base
		/// declarada -- las dos de dos dimensiones exigen los dos limites, TimeOnly solo el de
		/// tiempo, DistanceOnly solo el de distancia, ConditionOnly ninguno. Un limite ausente se
		/// expresa con cero; uno negativo es un defecto. Revienta (E32).
		/// </summary>
		internal static void BasisIsNeverInferred(
			IntervalBasisCode basis, int timeLimitDays, int distanceLimit)
		{
			if (timeLimitDays < 0)
				throw new DomainException(
					$"El limite de tiempo no puede ser negativo, y llego {timeLimitDays}. La "
					+ "ausencia de un limite se expresa con cero.");

			if (distanceLimit < 0)
				throw new DomainException(
					$"El limite de distancia no puede ser negativo, y llego {distanceLimit}. La "
					+ "ausencia de un limite se expresa con cero.");

			bool hayTiempo = timeLimitDays > 0;
			bool hayDistancia = distanceLimit > 0;

			switch (basis)
			{
				case IntervalBasisCode.TimeOrDistance:
					if (hayTiempo && hayDistancia) return;
					throw new DomainException(
						"TimeOrDistance exige los DOS limites: vence con la primera dimension que "
						+ "llegue, y con una sola no hay dos que comparar. Si de verdad hay una "
						+ "sola dimension, la base declarada es TimeOnly o DistanceOnly.");

				case IntervalBasisCode.TimeAndDistance:
					if (hayTiempo && hayDistancia) return;
					throw new DomainException(
						"TimeAndDistance exige los DOS limites: vence solo cuando se alcanzan los "
						+ "dos, y con uno faltando nunca podria vencer.");

				case IntervalBasisCode.TimeOnly:
					if (!hayTiempo)
						throw new DomainException(
							"TimeOnly exige un limite de tiempo: sin el no vence nunca, y OINV-19 "
							+ "dice que no hay operacion sin regla de cuando vence.");
					if (hayDistancia)
						throw new DomainException(
							$"TimeOnly no admite limite de distancia, y llego {distanceLimit}. O la "
							+ "base esta mal declarada -- seria TimeOrDistance o TimeAndDistance -- "
							+ "o el limite sobra. La base no se infiere: se rechaza.");
					return;

				case IntervalBasisCode.DistanceOnly:
					if (!hayDistancia)
						throw new DomainException(
							"DistanceOnly exige un limite de distancia: sin el no vence nunca "
							+ "(OINV-19).");
					if (hayTiempo)
						throw new DomainException(
							$"DistanceOnly no admite limite de tiempo, y llego {timeLimitDays}. O la "
							+ "base esta mal declarada o el limite sobra.");
					return;

				case IntervalBasisCode.ConditionOnly:
					if (hayTiempo || hayDistancia)
						throw new DomainException(
							"ConditionOnly no lleva limite de tiempo ni de distancia, y llegaron "
							+ $"tiempo={timeLimitDays} distancia={distanceLimit}. Vence cuando se "
							+ "recibe un cruce de umbral, y solo puede adelantar un vencimiento "
							+ "(ley L3).");
					return;
			}

			throw new DomainException(
				$"'{basis}' no nombra ninguna base de intervalo. Hay exactamente cinco, y la base "
				+ "llega declarada porque no se infiere de los limites.");
		}

		/// <summary>
		/// La unica fabrica de reglas, en la frontera: corre la guarda de OINV-20, resuelve la
		/// unidad, y devuelve la regla de la base pedida. La guarda va primero, siempre. La unidad
		/// se resuelve aunque la base no mida distancia, porque la firma del contrato es una sola.
		/// </summary>
		internal static IntervalRule Of(
			IntervalBasisCode basis, int timeLimitDays, int distanceLimit, DistanceUnitCode unitCode)
		{
			BasisIsNeverInferred(basis, timeLimitDays, distanceLimit);

			DistanceUnit unit = DistanceUnit.OfCode(unitCode);

			switch (basis)
			{
				case IntervalBasisCode.TimeOrDistance:
					return new TimeOrDistance(timeLimitDays, distanceLimit, unit);

				case IntervalBasisCode.TimeAndDistance:
					return new TimeAndDistance(timeLimitDays, distanceLimit, unit);

				case IntervalBasisCode.TimeOnly:
					return new TimeOnly(timeLimitDays);

				case IntervalBasisCode.DistanceOnly:
					return new DistanceOnly(distanceLimit, unit);

				case IntervalBasisCode.ConditionOnly:
					return new ConditionOnly();
			}

			throw new DomainException(
				$"'{basis}' paso la guarda y no tiene regla que construir. Si entro una base "
				+ "nueva, BasisIsNeverInferred y esta fabrica se actualizan juntas.");
		}
	}
}
