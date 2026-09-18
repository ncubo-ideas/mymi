namespace Concesionario.Wasm
{
	/// <summary>
	/// Lo que la pantalla le dice a la persona cuando la respuesta no puede ser la que esperaba. Vive
	/// en un solo sitio porque las dos secciones contestan lo mismo ante lo mismo.
	/// <para>
	/// SON CLAVES Y NO FRASES. La redaccion esta en el catalogo de la caratula, uno por idioma, y esta
	/// clase es el nombre estable con que se la pide: una clave se puede leer en las dos pantallas sin
	/// que ninguna de las dos elija un idioma.
	/// </para>
	/// </summary>
	public static class TheOwnerIsTold
	{
		/// <summary>
		/// Que todavia no hay ninguna revision sellada, que es un estado normal y no una pantalla vacia
		/// esperando que la llenen.
		/// </summary>
		public const string NothingHasBeenPublishedYet = "ownerIsTold.nothingHasBeenPublishedYet";

		/// <summary>
		/// Que alguno de los tres hechos con que se mide donde esta parado este vehiculo todavia no
		/// llego al registro, quien los manda, y que un sobre que no esta registrado no es un carro sin
		/// garantia. CUAL falta lo dice la frase del dominio, no esta.
		/// </summary>
		public const string TheDealershipHasNotSentTheseFactsYet =
			"ownerIsTold.theDealershipHasNotSentTheseFactsYet";

		/// <summary>
		/// Que dos hechos que el concesionario registro no pueden ser ciertos los dos, que ninguno lo
		/// puso la persona, y que solo el concesionario los corrige.
		/// </summary>
		public const string TheRecordSaysTwoThingsThatCannotBothBeTrue =
			"ownerIsTold.theRecordSaysTwoThingsThatCannotBothBeTrue";

		/// <summary>
		/// Que un sistema que la lista no nombra no es un sistema que el alcance deje afuera. Es la
		/// unica forma que tiene una pantalla sin buscador de no convertir un silencio en una negativa.
		/// </summary>
		public const string ASystemThatIsNotListedWasNotRuledOut =
			"ownerIsTold.aSystemThatIsNotListedWasNotRuledOut";

		/// <summary>
		/// Que la regla de servicio en otro taller esta dicha entera y que donde queda este vehiculo
		/// frente a ella no. Los tres conteos que decidirian eso van nombrados, y quien los pone.
		/// </summary>
		public const string TheServicingRuleStandingIsNotInThisRecord =
			"ownerIsTold.theServicingRuleStandingIsNotInThisRecord";

		/// <summary>
		/// Que los dias de una estadia todavia abierta no estan contados arriba, y por que: los manda el
		/// taller cuando entrega el carro.
		/// </summary>
		public const string TheDaysOfAStayArriveWhenTheCarIsCollected =
			"ownerIsTold.theDaysOfAStayArriveWhenTheCarIsCollected";

		/// <summary>
		/// Que un periodo sin ninguna estadia registrada no es un periodo en que el carro no entro al
		/// taller, y quien las registra.
		/// </summary>
		public const string TheStaysAreRecordedByTheWorkshopThatReceivesTheCar =
			"ownerIsTold.theStaysAreRecordedByTheWorkshopThatReceivesTheCar";

		/// <summary>
		/// Que la agenda rechazo la pregunta de esa operacion, que ninguno de los hechos que la deciden
		/// vino de la persona, y quien los corrige. NO dice CUAL hecho, porque el rechazo se atrapa por
		/// un tipo mas ancho que la condicion que lo causa.
		/// </summary>
		public const string TheDealershipSetTheDateThisOperationCountsFrom =
			"ownerIsTold.theDealershipSetTheDateThisOperationCountsFrom";

		/// <summary>
		/// Que una operacion sin ninguna completacion registrada no es una operacion que nunca se le
		/// hizo al carro, y quien las registra.
		/// </summary>
		public const string TheCompletionsAreRecordedByTheWorkshopThatDoesTheWork =
			"ownerIsTold.theCompletionsAreRecordedByTheWorkshopThatDoesTheWork";

		/// <summary>
		/// Que un periodo sin ninguna lectura del odometro no es un periodo en que el carro no se movio,
		/// y quien manda las lecturas.
		/// </summary>
		public const string TheOdometerReadingsAreSentByTheDealershipAndTheVehicle =
			"ownerIsTold.theOdometerReadingsAreSentByTheDealershipAndTheVehicle";

		/// <summary>
		/// Que un periodo sin ningun gasto registrado no es un periodo en que el carro no costo nada, y
		/// quien registra un gasto.
		/// </summary>
		public const string TheCostsAreRecordedByTheBranchThatDoesTheWork =
			"ownerIsTold.theCostsAreRecordedByTheBranchThatDoesTheWork";
	}
}
