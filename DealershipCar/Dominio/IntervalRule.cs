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
	// DOS PREGUNTAS, Y NADA MAS. Ni cuando evaluar, ni cuantos aplazamientos van, ni que hacer
	// con el resultado: eso es del agregado y de la operacion (T4, T5). Lo que esta lista deja
	// afuera es tan parte del diseno como lo que deja adentro.
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
	/// Cada subtipo contesta exactamente dos preguntas y nada mas: donde esta parada la
	/// operacion (<see cref="Assess"/>, donde viven las leyes L1 y L2), y donde queda el
	/// proximo punto base tras una completacion real
	/// (<see cref="NextOccurrenceFromActualCompletion"/>, la ley L4).
	/// </para>
	/// </summary>
	internal abstract class IntervalRule
	{
		/// <summary>
		/// PRIMERA PREGUNTA: donde esta parada la operacion. Dado el ancla desde la que se mide
		/// el intervalo, el instante que se evalua, la ultima lectura usable -- que puede no
		/// existir, y eso es un hecho legitimo y no un defecto -- y el escalamiento que el
		/// perfil de uso aplica al largo, devuelve el estado, cual dimension lo decidio y si
		/// alguna no se pudo evaluar.
		/// <para>
		/// El instante SE RECIBE, siempre (§7, E30/E31/E44). El dominio no lee un reloj: es lo
		/// que hace que el mismo journal de hechos recibidos vuelva a dar los mismos estados
		/// para siempre, y lo que deja adelantar la demo seis meses con un parametro en vez de
		/// esperando seis meses.
		/// </para>
		/// <para>
		/// Es una FUNCION PURA de lo que recibe: no guarda nada entre llamadas. Por eso la
		/// suspension de una dimension se cura sola cuando llegan los hechos que faltaban
		/// (GAP G4) -- no hay estado que alguien tenga que acordarse de levantar.
		/// </para>
		/// </summary>
		internal abstract DueAssessment Assess(
			BasePoint from, DateTime asOf, OdometerReading latestUsable, IntervalLengthFactor factor);

		/// <summary>
		/// SEGUNDA PREGUNTA, y LEY L4 (§5 del slice, OINV-26): "La proxima ocurrencia se calcula
		/// desde el instante y la lectura en que la operacion se completo DE VERDAD, nunca desde
		/// la grilla teorica en que estaba agendada."
		/// <para>
		/// Vencia a 40000 y se hizo a 39800: la proxima se mide desde 39800. La grilla teorica
		/// -- 30000, 40000, 50000, siempre redondo -- es comoda de programar y es mentira: le
		/// regala a la persona 200 unidades que nadie le dio, y el error SE ACUMULA hasta que el
		/// plan del fabricante y la realidad no se parecen.
		/// </para>
		/// <para>
		/// Es ABSTRACTA porque lo que cambia entre las bases es QUE DIMENSIONES ANCLA: las que
		/// no miden distancia devuelven un ancla sin lectura, ya que guardar una que ninguna ley
		/// lee haria creer que la distancia cuenta para esa operacion.
		/// </para>
		/// </summary>
		internal abstract BasePoint NextOccurrenceFromActualCompletion(
			DateTime completedAt, int reading, DistanceUnit unit);

		/// <summary>
		/// Lo que toda evaluacion que MIDA algo necesita para estar bien planteada: un ancla
		/// desde donde medir, un escalamiento que aplicar, y un instante que no sea anterior al
		/// ancla. Los tres son violaciones de contrato de quien llama y por eso revientan (E32);
		/// evaluar antes del ancla no es un resultado del negocio, es preguntar por el pasado de
		/// algo que todavia no existia.
		/// <para>
		/// ConditionOnly no la llama, y no es un olvido: no mide desde ningun lado, asi que
		/// contesta sin ancla y sin lecturas.
		/// </para>
		/// </summary>
		protected static void RequireAnAnchorAndAFactor(
			BasePoint from, DateTime asOf, IntervalLengthFactor factor)
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

			if (asOf < from.Instant)
				throw new DomainException(
					$"Se pidio evaluar al {asOf} un intervalo anclado en {from.Instant}: el "
					+ "instante que se evalua es anterior al ancla. No se evalua el pasado de un "
					+ "ancla que todavia no existia.");
		}

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
		/// quien llama, cualquiera sea la base. Las dos bases que no miden distancia la reciben
		/// y no se la quedan.
		/// </para>
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

			// Inalcanzable: la guarda ya rechazo cualquier base que no sea una de las cinco.
			// Queda escrito porque el dia que entre una sexta base y alguien se olvide de este
			// switch, lo que tiene que pasar es esto y no un null viajando hacia adentro.
			throw new DomainException(
				$"'{basis}' paso la guarda y no tiene regla que construir. Si entro una base "
				+ "nueva, BasisIsNeverInferred y esta fabrica se actualizan juntas.");
		}
	}
}
