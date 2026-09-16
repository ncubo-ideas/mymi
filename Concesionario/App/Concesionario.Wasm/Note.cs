namespace Concesionario.Wasm
{
	/// <summary>
	/// UN APUNTE QUE LA APP TIENE QUE DECIR, sin haberlo redactado: la clave de la frase y los valores
	/// que van en sus huecos. La capa que lo compone no elige un idioma, y la pantalla que lo dice no
	/// vuelve a decidir nada.
	/// <para>
	/// La otra forma es una CITA: las palabras con que el dominio rechazo algo se muestran tal cual,
	/// porque son suyas y no nuestras. Un apunte es una cosa o la otra, nunca las dos.
	/// </para>
	/// </summary>
	public sealed class Note
	{
		/// <summary>Que no hay nada que decir. Es la ausencia, y no una clave vacia.</summary>
		public static readonly Note None = new Note("", "", new (string, string)[0]);

		private Note(string key, string quoted, (string Hole, string Value)[] holes)
		{
			Key = key;
			Quoted = quoted;
			Holes = holes;
		}

		/// <summary>La clave de la frase en el catalogo de la caratula, o vacio si esto es una cita.</summary>
		public string Key { get; }

		/// <summary>Las palabras tal como llegaron, o vacio si esto es una clave.</summary>
		public string Quoted { get; }

		/// <summary>Los valores que van en los huecos de la frase.</summary>
		public (string Hole, string Value)[] Holes { get; }

		/// <summary>Si hay algo que decir.</summary>
		public bool MustBeSaid => Key.Length > 0 || Quoted.Length > 0;

		/// <summary>Un apunte que la pantalla redacta desde su catalogo.</summary>
		public static Note Of(string key, params (string Hole, string Value)[] holes) =>
			new Note(key, "", holes);

		/// <summary>Las palabras de otro, dichas tal como llegaron.</summary>
		public static Note AsItArrived(string words) =>
			string.IsNullOrWhiteSpace(words) ? None : new Note("", words, new (string, string)[0]);
	}
}
