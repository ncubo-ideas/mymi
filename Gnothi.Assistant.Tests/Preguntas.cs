namespace Gnothi.Assistant.Tests;

// EL SET FIJO DE PREGUNTAS CON EL QUE SE MIDE LA SELECCION DE TOOLS, EN LOS DOS IDIOMAS.
//
// Dos por modulo del banco local -Modulos.local- mas dos de cruce, en las palabras de Sonia
// (docs/persona-sonia.md) y de los README de los spikes.
//
// ES UN INSTRUMENTO, NO UNA PRUEBA DE COMPORTAMIENTO. No afirma que un modelo acierte: fija
// CONTRA QUE se mide, para que dos mediciones hechas con meses de diferencia se puedan comparar.
// La pregunta que contesta es «de las que tenia que llamar, cuantas llamo», y esa cuenta no
// significa nada si el set cambia entre una corrida y otra.
//
// POR QUE CADA PREGUNTA VIENE EN LOS DOS IDIOMAS, y no es simetria decorativa: el idioma es la
// variable mas pesada que este equipo midio sobre el tool calling. El 2026-09-17, con el mismo
// binario y la misma tool, el modelo del aparato llamaba 4 de 4 en iOS 26.6.2 y 0 de 8 en iOS
// 27.0 -- y dos de esas ocho invirtieron el veredicto --. Tres hipotesis cayeron antes de aislar
// la causa: el encuadre iba en castellano y la persona estaba en ingles. Un banco en un solo
// idioma no puede medir lo unico que se sabe que decide el resultado.
//
// Y LA ASIMETRIA SE MIDE EN LAS DOS DIRECCIONES. El manifest de concesionario esta en ingles:
// preguntando en espanol, 'carro' no coincide con 'vehicle' ni 'aceite' con 'oil'. Preguntando
// en ingles ese desencuentro no existe -- y el de examenes, compras y cargas, que declaran en
// espanol, se invierte. Eso es lo que `SoloInforma` nombra por idioma en vez de por pregunta.
//
// Nacio el 2026-09-08 midiendo si podar el catalogo lo hacia entrar en los 4 096 tokens del
// modelo de Apple -entraba 11/11 y acertaba 6/11-. Esa poda se descarto el 2026-09-17, cuando
// se midio que el techo del modelo chico no esta en entender sino en DECIDIR, y que la pared de
// los 4 096 solo aprieta en el escritorio. El set sobrevivio a su primer uso porque sirve para
// cualquier medicion de seleccion, con modelo o sin el.
//
// Lo unico que lo cuida es la asercion de Conformance.cs: que los nombres que menciona sigan
// siendo tools que algun modulo publica. Son strings, no se compilan contra nada.
//
// Esperadas son las tools que TIENEN que quedar en el catalogo para que la pregunta pueda
// contestarse. En cargas la placa la da mis_cargas, asi que esa es tolerable ademas de la
// directa; por eso Tolerables aparte.
public static class Preguntas
{
	/// <summary>
	/// Una pregunta, dicha en los dos idiomas que Gnothi habla. Las tools NO cambian con el
	/// idioma: el nombre publico de una consulta es protocolo, no cara.
	/// </summary>
	public sealed record Pregunta(
		string Es,
		string En,
		string Modulo,
		string[] Esperadas,
		string[] Tolerables,
		string? SoloInformaEs = null,
		string? SoloInformaEn = null)
	{
		/// <summary>El texto en ese idioma. Cualquier etiqueta que no empiece por 'es' es ingles.</summary>
		public string Texto(string idioma) =>
			idioma.StartsWith("es", StringComparison.OrdinalIgnoreCase) ? Es : En;

		/// <summary>
		/// Por que esa pregunta se mide y se imprime pero no se exige, EN ESE IDIOMA. La razon es
		/// un hallazgo, no una excusa, y se lee en el reporte al lado del resultado. Que dependa
		/// del idioma no es un detalle: una pregunta puede ser exigible en ingles y no en espanol
		/// -- concesionario declara en ingles -- y al reves.
		/// </summary>
		public string? SoloInforma(string idioma) =>
			idioma.StartsWith("es", StringComparison.OrdinalIgnoreCase) ? SoloInformaEs : SoloInformaEn;
	}

	public static readonly IReadOnlyList<Pregunta> DeSonia =
	[
		new("¿Qué exámenes me falta repetir?",
			"Which lab tests am I due to repeat?",
			"examenes", ["examenes_que_me_falta_medir"], []),
		new("¿Cómo viene mi colesterol?",
			"How is my cholesterol doing?",
			"examenes", ["examenes_evaluar_tendencia"], ["examenes_velocidad_de"]),

		new("¿Cómo vengo con las compras del súper?",
			"How am I doing with my grocery shopping?",
			"compras", ["compras_como_viene_mi_canasta"], []),
		new("¿Hasta cuándo tiene garantía la olla arrocera?",
			"How long is the rice cooker under warranty?",
			"compras", ["compras_garantia_de_producto"], ["compras_garantias_por_vencer"]),

		new("¿Cuántos kilómetros manejo por día?",
			"How many kilometres do I drive a day?",
			"cargas", ["cargas_mi_ritmo_de_manejo"], ["cargas_mis_cargas"]),
		new("¿En cuántos días voy a tener que cargar gasolina?",
			"In how many days will I need to fill up?",
			"cargas", ["cargas_dias_hasta_la_proxima_carga"], ["cargas_mis_cargas"]),

		// EL DESENCUENTRO DE IDIOMA, QUE AHORA SE MIDE EN LAS DOS DIRECCIONES. El manifest de
		// concesionario esta en ingles. Preguntando en espanol la poda lexica dejaba estas dos
		// afuera -medido el 2026-09-08-, y por eso en espanol solo se informan. En ingles la
		// pregunta y el manifest coinciden, asi que ahi SI se exigen: si fallan, el problema no
		// es el idioma y hay que buscarlo en otra parte.
		new("¿Mi carro necesita algo de mantenimiento?",
			"Does my car need any maintenance?",
			"concesionario", ["concesionario_review_my_maintenance"], [],
			SoloInformaEs: "manifest en ingles, pregunta en espanol"),
		new("¿Ya toca el cambio de aceite?",
			"Is the oil change due yet?",
			"concesionario", ["concesionario_check_one_operation"], ["concesionario_review_my_maintenance"],
			SoloInformaEs: "manifest en ingles, pregunta en espanol"),

		new("¿Cuánto he gastado en total en facturas?",
			"How much have I spent on bills in total?",
			"facturas", ["facturas_total_de_facturas"], []),

		// LOS CRUCES. Ninguna tool los nombra hoy -Lab.Cruce dice que hay que nombrarlos-, asi
		// que se miden y se informan, no se exigen. Eso no depende del idioma: falta la tool.
		new("¿Por qué me subió el colesterol? ¿Tiene que ver con lo que compro en el súper?",
			"Why did my cholesterol go up? Does it have to do with what I buy at the supermarket?",
			"cruce", ["examenes_evaluar_tendencia", "compras_consumo_de"], ["compras_como_viene_mi_canasta"],
			SoloInformaEs: "cruce sin tool que lo nombre",
			SoloInformaEn: "cruce sin tool que lo nombre"),
		new("Con lo que manejo por día, ¿cuánto le falta al carro para el próximo mantenimiento?",
			"With how much I drive a day, how far is the car from its next service?",
			"cruce", ["cargas_mi_ritmo_de_manejo", "concesionario_review_my_maintenance"], ["concesionario_check_one_operation"],
			SoloInformaEs: "cruce sin tool que lo nombre, y concesionario en ingles",
			SoloInformaEn: "cruce sin tool que lo nombre"),
	];

	/// <summary>Las etiquetas en las que este banco esta escrito, para recorrerlo entero.</summary>
	public static readonly string[] LosIdiomas = ["es", "en"];
}
