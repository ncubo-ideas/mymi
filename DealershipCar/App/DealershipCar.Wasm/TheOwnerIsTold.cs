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
			"Todavía no se ha publicado nada sobre este modelo. Eso no es una falla ni es una "
			+ "pantalla esperando que vos la llenes: tu concesionario manda la ficha del "
			+ "fabricante, y hasta que llegue aquí no hay nada que citar.";

		/// <summary>
		/// Que alguno de los tres hechos con que se mide donde esta parado este vehiculo todavia no
		/// llego al registro, quien los manda, y que un sobre que no esta registrado no es un carro
		/// sin garantia. CUAL falta lo dice la frase del dominio, no esta.
		/// </summary>
		public const string TheDealershipHasNotSentTheseFactsYet =
			"Parte de lo que decide esto todavía no ha llegado a tu registro: el sobre de garantía "
			+ "mismo, el día en que se entregó el carro, o una lectura del odómetro tomada hoy o "
			+ "antes. La frase de arriba nombra cuál, con las palabras de tu propio registro. Tu "
			+ "concesionario manda los tres, por el mismo feed que trae todo lo demás aquí, así que "
			+ "no hay nada que tengas que llenar ni nada que valga la pena adivinar. Un sobre que no "
			+ "está registrado aquí no es lo mismo que un carro sin garantía -- pedile a tu "
			+ "concesionario que lo registre.";

		/// <summary>
		/// Que dos hechos que el concesionario registro no pueden ser ciertos los dos, que ninguno
		/// lo puso la persona, y que solo el concesionario los corrige. Es lo unico que la pantalla
		/// puede decir cuando el registro se contradice consigo mismo.
		/// </summary>
		public const string TheRecordSaysTwoThingsThatCannotBothBeTrue =
			"Dos hechos que tu concesionario registró sobre este carro se contradicen entre sí, así "
			+ "que dónde está parado tu carro no se pudo medir del todo y nada de lo de arriba es "
			+ "una estimación de eso. Ninguno de los dos hechos vino de vos. Solo tu concesionario "
			+ "puede corregirlos: pediles que revisen el día en que entregaron el carro y la lectura "
			+ "del odómetro que tomaron ese día contra la última lectura que han mandado. Lo que el "
			+ "fabricante publicó está abajo y esto no lo afecta.";

		/// <summary>
		/// Que un sistema que la lista no nombra no es un sistema que el alcance deje afuera. Es la
		/// unica forma que tiene una pantalla sin buscador de no convertir un silencio en una
		/// negativa.
		/// </summary>
		public const string ASystemThatIsNotListedWasNotRuledOut =
			"Estos son los sistemas que el fabricante publicó, y nada más. Un sistema que no está en "
			+ "esta lista no quedó descartado aquí -- nunca se mencionó, que es otra cosa y vale más "
			+ "preguntárselo a tu concesionario que darlo por hecho.";

		/// <summary>
		/// Que la regla de servicio en otro taller esta dicha entera y que donde queda este vehiculo
		/// frente a ella no. Los tres conteos que decidirian eso van nombrados, y quien los pone.
		/// </summary>
		public const string TheServicingRuleStandingIsNotInThisRecord =
			"Esa es la regla tal como se publicó, no dónde está parado tu carro frente a ella. "
			+ "Decir eso necesita cuántos servicios ha tenido este carro en otro lado, cuántos de "
			+ "esos tienen comprobante, y cuánto mantenimiento programado se dejó de hacer. Quien "
			+ "los pone aquí es tu concesionario.";

		/// <summary>
		/// Que los dias de una estadia todavia abierta no estan contados arriba, y por que: los manda
		/// el taller cuando entrega el carro. Es lo unico que ninguna frase del dominio dice, porque
		/// cada una contesta su propia pregunta y ninguna habla de la otra.
		/// </summary>
		public const string TheDaysOfAStayArriveWhenTheCarIsCollected =
			"Los días de esta estadía todavía no están contados arriba, y no es un error de la "
			+ "cuenta: los días de una estadía los manda el taller cuando te entrega el carro, así "
			+ "que hasta entonces se sabe que está adentro y desde cuándo, pero no cuántos días "
			+ "habrán sido.";

		/// <summary>
		/// Que un periodo sin ninguna estadia registrada no es un periodo en que el carro no entro al
		/// taller, y quien las registra. Es la frase que mas facil se lee al reves en este bloque.
		/// </summary>
		public const string TheStaysAreRecordedByTheWorkshopThatReceivesTheCar =
			"Que no haya ninguna aquí no quiere decir que tu carro no haya entrado a un taller: "
			+ "quiere decir que ninguna quedó registrada en este periodo. Las estadías las registra "
			+ "el taller que recibe el carro, por el mismo feed que trae todo lo demás aquí, y no hay "
			+ "nada que tengas que llenar vos.";
	}
}
