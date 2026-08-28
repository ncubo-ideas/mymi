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
		/// Que esto es el alcance publicado y no donde esta parado este vehiculo frente a el, con
		/// los cuatro hechos que faltan nombrados y con quien los pone.
		/// </summary>
		public const string TheStandingIsNotInThisRecord =
			"This is what the manufacturer published, not where your car stands against it. Saying "
			+ "that needs your warranty envelope, the day the car was handed over, its odometer "
			+ "then and its odometer now, and this record holds none of the four. Your dealership "
			+ "is who puts them here.";

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
