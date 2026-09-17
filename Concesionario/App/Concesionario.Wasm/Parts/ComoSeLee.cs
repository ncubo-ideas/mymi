using Concesionario.App.Wording;

namespace Concesionario.Wasm.Parts
{
	/// <summary>
	/// Como se le dice a la duena lo que el registro nombra en el vocabulario del dominio, en el
	/// tablero.
	///
	/// Aca SOLO se nombran CONJUNTOS CERRADOS: las nueve operaciones, los estados del calendario, las
	/// posturas del sobre, los fluidos y las posiciones de llanta. Son conjuntos que el dominio fija y
	/// que no crecen sin que alguien recompile, y por eso mismo NO son dato del registro: se traducen,
	/// y su etiqueta sale del catalogo, uno por idioma.
	///
	/// LO QUE NO SE TRADUCE AQUI son las FRASES del dominio -- "Overdue, decided by distance..." -- y
	/// no es un olvido. Esas frases las compone el dominio con los numeros adentro; reescribirlas en
	/// esta capa seria volver a decidir, en la pantalla, algo que ya se decidio en el registro, y la
	/// primera vez que las dos versiones no coincidieran ganaria la equivocada. Van citadas, literales,
	/// bajo "lo que dice tu registro".
	///
	/// EL TABLERO DICE ESTOS MISMOS CONJUNTOS CON OTRAS PALABRAS QUE LA HOJA, y la diferencia esta en
	/// el catalogo y no aqui: la familia es "board" y la de <see cref="HowACanonNameReads"/> es "canon".
	/// </summary>
	public static class ComoSeLee
	{
		/// <summary>El nombre largo de una de las nueve.</summary>
		public static string Operacion(Phrases phrases, string kind) =>
			phrases.HowItIsSaid("board.operation", kind);

		/// <summary>El nombre corto, para debajo de un punto de color.</summary>
		public static string OperacionCorta(Phrases phrases, string kind) =>
			phrases.HowItIsSaid("board.operation.short", kind);

		/// <summary>El estado del calendario dicho como lo lee una persona.</summary>
		public static string Estado(Phrases phrases, string dueState) =>
			phrases.HowItIsSaid("board.dueState", dueState);

		/// <summary>La postura del sobre, o la de un sistema del alcance, dicha como la lee una persona.</summary>
		public static string Postura(Phrases phrases, string standing) =>
			phrases.HowItIsSaid("board.standing", standing);

		/// <summary>Una de las seis clases de fluido, en el vocabulario del tablero.</summary>
		public static string Fluido(Phrases phrases, string kind) =>
			phrases.HowItIsSaid("board.fluid", kind);

		/// <summary>Una de las tres posiciones de llanta, en el vocabulario del tablero.</summary>
		public static string Llanta(Phrases phrases, string position) =>
			phrases.HowItIsSaid("board.tyre", position);

		/// <summary>
		/// El tono con que se pinta una postura del sobre. Los nombres son los del conjunto cerrado del
		/// dominio: adentro es la buena noticia, alcanzado es lo que hay que plantear, y que nadie haya
		/// publicado un sobre no es ninguna de las dos. NO depende del idioma: es un color.
		/// </summary>
		public static string TonoDeLaPostura(string standing) => standing switch
		{
			"Inside" => "ok",
			"Covered" => "ok",
			"Reached" => "warn",
			"CoveredWithExclusion" => "warn",
			"NotCovered" => "danger",
			"OutsideEnvelope" => "danger",
			_ => "mute",
		};

		/// <summary>
		/// El tono con que se pinta un estado, que es el sufijo de la clase de css que lo pinta. Aca
		/// "NotDue" es VERDE y no neutro: para quien maneja el carro, "no toca" es la buena noticia, y
		/// pintarla gris al lado de dos rojos hace que la pantalla se vea peor de lo que el registro
		/// dice. Un estado que la lista no nombre se pinta neutro en vez de desaparecer.
		/// </summary>
		public static string Tono(string dueState) => dueState switch
		{
			"Overdue" => "danger",
			"Missed" => "danger",
			"Due" => "warn",
			"DueSoon" => "warn",
			"Deferred" => "warn",
			"NotDue" => "ok",
			"Completed" => "ok",
			_ => "mute",
		};

		/// <summary>El dibujo que acompana a un estado en el semaforo.</summary>
		public static string Dibujo(string dueState) => dueState switch
		{
			"Overdue" => "alert",
			"Missed" => "alert",
			"Due" => "clock",
			"DueSoon" => "clock",
			"Deferred" => "calendar",
			_ => "check",
		};

		/// <summary>Un numero entero con el separador de miles del idioma. Es formato, no una cuenta.</summary>
		public static string Miles(Phrases phrases, int value) => phrases.Grouped(value);

		/// <summary>Una fecha del registro dicha en palabras, en el orden que pide el idioma.</summary>
		public static string Dia(Phrases phrases, string yyyymmdd) => phrases.ADay(yyyymmdd);

		/// <summary>La misma fecha, corta, para una linea de tiempo.</summary>
		public static string DiaCorto(Phrases phrases, string yyyymmdd) => phrases.AShortDay(yyyymmdd);
	}
}
