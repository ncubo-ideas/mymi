namespace Gnothi.Assistant.Tests;

// THE FIXED SET OF QUESTIONS TOOL SELECTION IS MEASURED WITH, IN BOTH LANGUAGES.
//
// Two per module of the local bench -Modulos.local- plus two crossing ones, in Sonia's words
// (docs/persona-sonia.md) and those of the spikes' READMEs.
//
// IT IS AN INSTRUMENT, NOT A TEST OF BEHAVIOUR. It does not claim a model gets things right: it
// fixes WHAT is measured against, so two measurements made months apart can be compared. The
// question it answers is «of the ones it had to call, how many did it call», and that count means
// nothing if the set changes between one run and the next.
//
// WHY EVERY QUESTION COMES IN BOTH LANGUAGES, and it is not decorative symmetry: language is the
// heaviest variable this team has measured over tool calling. On 2026-09-17, with the same binary
// and the same tool, the device's model called 4 out of 4 on iOS 26.6.2 and 0 out of 8 on iOS
// 27.0 -- and two of those eight reversed the verdict. Three hypotheses fell before the cause was
// isolated: the framing went in Spanish and the person was in English. A bench in a single
// language cannot measure the one thing known to decide the result.
//
// AND THE ASYMMETRY IS MEASURED IN BOTH DIRECTIONS. The concesionario manifest is in English:
// asking in Spanish, 'carro' does not match 'vehicle' nor 'aceite' match 'oil'. Asking in English
// that mismatch does not exist -- and the one for examenes, compras and cargas, which declare in
// Spanish, is inverted. That is what `SoloInforma` names by language instead of by question.
//
// It was born on 2026-09-08 measuring whether pruning the catalogue made it fit inside Apple's
// model's 4,096 tokens -it fitted 11/11 and got 6/11 right. That pruning was discarded on
// 2026-09-17, when it was measured that the small model's ceiling is not in understanding but in
// DECIDING, and that the 4,096 wall only pinches on the desktop. The set outlived its first use
// because it serves any selection measurement, with a model or without one.
//
// The only thing looking after it is Conformance.cs's assertion: that the names it mentions are
// still tools some module publishes. They are strings, compiled against nothing.
//
// Esperadas are the tools that HAVE to stay in the catalogue for the question to be answerable. In
// cargas the plate is given by mis_cargas, so that one is tolerable as well as the direct one;
// hence Tolerables separately.
public static class Preguntas
{
	/// <summary>
	/// A question, said in the two languages Gnothi speaks. The tools do NOT change with the
	/// language: a query's public name is protocol, not face.
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
		/// <summary>The text in that language. Any tag not starting with 'es' is English.</summary>
		public string Texto(string idioma) =>
			idioma.StartsWith("es", StringComparison.OrdinalIgnoreCase) ? Es : En;

		/// <summary>
		/// Why that question is measured and printed but not demanded, IN THAT LANGUAGE. The reason
		/// is a finding, not an excuse, and is read in the report beside the result. That it depends
		/// on the language is no detail: a question can be demandable in English and not in Spanish
		/// -- concesionario declares in English -- and the other way round.
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

		// THE LANGUAGE MISMATCH, NOW MEASURED IN BOTH DIRECTIONS. The concesionario manifest
		// is in English. Asking in Spanish the lexical pruning left these two out
		// -measured on 2026-09-08- and that is why in Spanish they are only reported. In English
		// the question and the manifest match, so there they ARE demanded: if they fail, the
		// problem is not the language and has to be looked for elsewhere.
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

		// THE CROSSINGS. No tool names them today -Lab.Cruce says they have to be named- so they
		// are measured and reported, not demanded. That does not depend on the language: the tool
		// is missing.
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

	/// <summary>The tags this bench is written in, so it can be walked in full.</summary>
	public static readonly string[] LosIdiomas = ["es", "en"];
}
