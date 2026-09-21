namespace Concesionario.Dominio.Pruebas
{
	// LOS DOS IDIOMAS EN QUE SE LE PUEDE PEDIR UNA FRASE A LA PUERTA, nombrados donde el modulo los
	// declara y no copiados aqui: dos copias de una subetiqueta dejan de decir lo mismo sin que nada
	// avise, que es el mismo argumento con que Phrases las declara una sola vez.
	internal static class TheLanguage
	{
		// El del registro del concesionario, que es al que cae toda etiqueta desconocida.
		internal const string OfTheRecord = App.Wording.Phrases.LanguageOfTheRecord;

		// El otro que este modulo habla. El alcance son dos y no hay un tercero.
		internal const string OfAnEnglishReader = App.Wording.Phrases.TheOtherLanguage;
	}
}
