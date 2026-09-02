namespace DealershipCar.Wasm
{
	/// <summary>
	/// Lo que la pantalla le dice a la persona cuando la respuesta no puede ser la que esperaba.
	/// Vive en un solo sitio porque las dos secciones contestan lo mismo ante lo mismo.
	/// </summary>
	public static class TheOwnerIsTold
	{
		/// <summary>
		/// Que todavia no hay ninguna revision sellada, que es un estado normal y no una pantalla
		/// vacia esperando que la llenen.
		/// </summary>
		public const string NothingHasBeenPublishedYet =
			"Nothing has been published about this model yet. That is not a fault and it is not a "
			+ "screen waiting for you to fill it in: your dealership sends the manufacturer's sheet, "
			+ "and until it arrives there is nothing here to quote.";

		/// <summary>
		/// Que alguno de los tres hechos con que se mide donde esta parado este vehiculo todavia no
		/// llego al registro, quien los manda, y que un sobre que no esta registrado no es un carro
		/// sin garantia. CUAL falta lo dice la frase del dominio, no esta.
		/// </summary>
		public const string TheDealershipHasNotSentTheseFactsYet =
			"Part of what decides this has not reached your record yet: the warranty envelope itself, "
			+ "the day the car was handed over, or an odometer reading taken on or before today. The "
			+ "sentence above names which one, in your record's own words. Your dealership sends all "
			+ "three, by the same feed that brings everything else here, so there is nothing here for "
			+ "you to fill in and nothing worth guessing. An envelope that is not recorded here is "
			+ "not the same as a car with no warranty -- ask your dealership to record it.";

		/// <summary>
		/// Que dos hechos que la agencia registro no pueden ser ciertos los dos, que ninguno lo puso
		/// la persona, y que solo la agencia los corrige. Es lo unico que la pantalla puede decir
		/// cuando el registro se contradice consigo mismo.
		/// </summary>
		public const string TheRecordSaysTwoThingsThatCannotBothBeTrue =
			"Two facts your dealership recorded about this car contradict each other, so where your "
			+ "car stands could not be measured at all and nothing above is an estimate of it. "
			+ "Neither fact came from you. Only your dealership can correct them: ask them to check "
			+ "the day they handed the car over and the odometer reading they took that day against "
			+ "the latest reading they have sent. What the manufacturer published is below and is "
			+ "unaffected by this.";

		/// <summary>
		/// Que un sistema que la lista no nombra no es un sistema que el alcance deje afuera. Es la
		/// unica forma que tiene una pantalla sin buscador de no convertir un silencio en una
		/// negativa.
		/// </summary>
		public const string ASystemThatIsNotListedWasNotRuledOut =
			"These are the systems the manufacturer published, and nothing else. A system that is "
			+ "not on this list was not ruled out here -- it was never mentioned, which is a "
			+ "different thing and worth asking your dealership about rather than assuming.";

		/// <summary>
		/// Que la regla de servicio en otro taller esta dicha entera y que donde queda este vehiculo
		/// frente a ella no. Los tres conteos que decidirian eso van nombrados, y quien los pone.
		/// </summary>
		public const string TheServicingRuleStandingIsNotInThisRecord =
			"That is the rule as published, not where your car stands against it. Saying that needs "
			+ "how many services this car has had elsewhere, how many of those have paperwork, and "
			+ "how much scheduled maintenance was missed. Your dealership is who puts them here.";
	}
}
