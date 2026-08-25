using System;

namespace DealershipCar.Dominio
{
	// LA LEY QUE DECIDE CUANDO VENCE UNA OPERACION. Comportamiento, no dato (§4 del slice).
	//
	// Las cinco bases DECIDEN distinto, no solo guardan numeros distintos: una vence con la
	// PRIMERA de las dos dimensiones, otra con LAS DOS, dos con UNA SOLA, y la ultima con
	// ninguna -- espera un cruce de umbral recibido. Ese es el "tell" de E39: una rama por base
	// que corre LOGICA distinta es comportamiento, y el comportamiento subtipa. Se corrio el
	// test del Curador: quitando los nombres de los subtipos y metiendo los limites en datos, lo
	// que se pierde es el PROCEDIMIENTO DE DECISION either / both / single / none. Se pierde una
	// regla, asi que los subtipos se ganan el lugar.
	//
	// La base es EL SUBTIPO. No hay un campo "basis" que alguien pueda cambiar despues; el tipo
	// de la regla ES su base, y por eso no hay como desincronizarlos.
	//
	// E43: esta base se queda ABIERTA. Sellarla es el reflejo que despues obliga a des-sellarla.
	//
	// ===== QUE CONSTRUYE T2 Y QUE NO ==================================================
	//
	// T2 construye LA GUARDA: BasisIsNeverInferred. Los cinco subtipos y sus dos preguntas
	// -- Assess (leyes L1 y L2) y NextOccurrenceFromActualCompletion (ley L4) -- son T3. Of()
	// ya valida, y despues avisa que la construccion todavia no existe; el aviso desaparece
	// cuando T3 ponga los cinco constructores en su lugar.
	//
	// El orden importa y hay una prueba que lo fija: la guarda corre ANTES de intentar
	// construir. Una combinacion invalida no debe llegar nunca a un constructor.
	//
	// ==================================================================================
	//
	// POR QUE LA GUARDA VALE TANTO COMO LA CUENTA. OINV-20 termina con cuatro palabras que se
	// leen como relleno: "LA BASE NUNCA SE INFIERE". No lo son. TimeOrDistance y
	// TimeAndDistance reciben EXACTAMENTE los mismos dos numeros y deciden al reves; ningun
	// dato los distingue. Si la fabrica dedujera la base de los limites -- "vinieron los dos,
	// entonces sera TimeOrDistance" -- una operacion sembrada con un limite de mas cambiaria
	// de LEY en silencio. La unica forma de que la base sea la que el plan del fabricante dice
	// es que llegue DECLARADA y que lo que no concuerda se RECHACE.
	//
	// La ausencia de un limite se expresa con CERO, porque lo que cruza el wire son primitivos
	// y no hay "int ausente". Negativo no es ausencia: es un defecto.

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
	/// La ley que decide cuando vence una operacion de mantenimiento. Base polimorfica: la base
	/// del intervalo ES el subtipo, nunca un campo, asi que no hay forma de que la base y los
	/// limites se desincronicen. Queda ABIERTA a proposito (E43).
	/// <para>
	/// Los cinco subtipos y las dos preguntas que cada uno contesta -- el estado de vencimiento
	/// dado un punto base, y donde queda el proximo punto base tras una completacion real --
	/// llegan en T3. Lo que esta construido aca es la guarda de OINV-20.
	/// </para>
	/// </summary>
	internal abstract class IntervalRule
	{
		/// <summary>
		/// OINV-20, ultima frase: LA BASE NUNCA SE INFIERE. Quien siembra una operacion declara
		/// la base, y esta guarda rechaza los limites que no concuerdan con la base declarada.
		/// <para>
		/// TimeOrDistance y TimeAndDistance exigen los dos limites; TimeOnly exige el de tiempo
		/// y ninguno de distancia; DistanceOnly al reves; ConditionOnly no admite ninguno. Un
		/// limite ausente se expresa con cero -- lo que cruza el wire son primitivos -- y un
		/// limite negativo es siempre un defecto.
		/// </para>
		/// <para>
		/// Polaridad (E32): esto es una violacion de contrato de quien llama, asi que REVIENTA.
		/// Aceptar callado una combinacion que no concuerda seria inferir la base, y una
		/// operacion cambiaria de ley sin que nadie lo vea.
		/// </para>
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
		/// La UNICA fabrica de reglas de intervalo, en la frontera: recibe los primitivos del
		/// wire, corre la guarda de OINV-20 y resuelve la unidad, y de ahi sale la regla de la
		/// base pedida.
		/// <para>
		/// El orden no es casual y hay una prueba que lo fija: primero la guarda, despues
		/// cualquier construccion. Una combinacion que no concuerda no debe llegar nunca a un
		/// constructor.
		/// </para>
		/// <para>
		/// La unidad solo tiene sentido donde hay limite de distancia; en TimeOnly y
		/// ConditionOnly el wire igual la trae porque la firma del contrato es una sola, y se
		/// resuelve de todas formas -- un codigo que no nombra ninguna unidad es un defecto de
		/// quien llama, cualquiera sea la base.
		/// </para>
		/// <para>
		/// PENDIENTE T3: los cinco subtipos. La guarda ya es definitiva; lo que falta es a quien
		/// construir despues de ella.
		/// </para>
		/// </summary>
		internal static IntervalRule Of(
			IntervalBasisCode basis, int timeLimitDays, int distanceLimit, DistanceUnitCode unitCode)
		{
			BasisIsNeverInferred(basis, timeLimitDays, distanceLimit);

			DistanceUnit.OfCode(unitCode);

			throw new DomainException(
				$"La base '{basis}' y sus limites concuerdan, pero los cinco subtipos de "
				+ "IntervalRule llegan en T3: lo que esta construido es la guarda de OINV-20, no "
				+ "la jerarquia. Ninguna operacion se puede sembrar todavia.");
		}
	}
}
