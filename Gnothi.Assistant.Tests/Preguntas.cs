namespace Gnothi.Assistant.Tests;

// EL SET FIJO DE PREGUNTAS CON EL QUE SE MIDE LA SELECCION DE TOOLS.
//
// Dos por modulo del banco local -Modulos.local- mas dos de cruce, en las palabras de Sonia
// (docs/persona-sonia.md) y de los README de los spikes.
//
// ES UN INSTRUMENTO, NO UNA PRUEBA DE COMPORTAMIENTO. No afirma que un modelo acierte: fija
// CONTRA QUE se mide, para que dos mediciones hechas con meses de diferencia se puedan comparar.
// La pregunta que contesta es «de las que tenia que llamar, cuantas llamo», y esa cuenta no
// significa nada si el set cambia entre una corrida y otra.
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
// Esperadas son las tools que TIENEN que quedar en el catalogo podado para que la pregunta
// pueda contestarse. En cargas la placa la da mis_cargas, asi que esa es tolerable ademas de
// la directa; por eso Tolerables aparte.
//
// SoloInforma, cuando no es null, dice por que esa pregunta se mide y se imprime pero no se
// exige: la razon es un hallazgo, no una excusa, y se lee en el reporte al lado del resultado.
public static class Preguntas
{
	public sealed record Pregunta(string Texto, string Modulo, string[] Esperadas, string[] Tolerables, string? SoloInforma = null);

	public static readonly IReadOnlyList<Pregunta> DeSonia =
	[
		new("¿Qué exámenes me falta repetir?", "examenes", ["examenes_que_me_falta_medir"], []),
		new("¿Cómo viene mi colesterol?", "examenes", ["examenes_evaluar_tendencia"], ["examenes_velocidad_de"]),

		new("¿Cómo vengo con las compras del súper?", "compras", ["compras_como_viene_mi_canasta"], []),
		new("¿Hasta cuándo tiene garantía la olla arrocera?", "compras", ["compras_garantia_de_producto"], ["compras_garantias_por_vencer"]),

		new("¿Cuántos kilómetros manejo por día?", "cargas", ["cargas_mi_ritmo_de_manejo"], ["cargas_mis_cargas"]),
		new("¿En cuántos días voy a tener que cargar gasolina?", "cargas", ["cargas_dias_hasta_la_proxima_carga"], ["cargas_mis_cargas"]),

		// EL MANIFEST DE CONCESIONARIO ESTA EN INGLES y Sonia pregunta en espanol: 'carro' no
		// coincide con 'vehicle' ni 'aceite' con 'oil' por mas raices que se comparen. Medido el
		// 2026-09-08: la poda lexica deja estas dos afuera. Es el limite de B -es por idioma- y
		// la razon de que exista la alternativa C (embeddings) o de pedirle a la institucion el
		// manifest en el idioma de la persona.
		new("¿Mi carro necesita algo de mantenimiento?", "concesionario", ["concesionario_review_my_maintenance"], [],
			SoloInforma: "manifest en ingles, pregunta en espanol"),
		new("¿Ya toca el cambio de aceite?", "concesionario", ["concesionario_check_one_operation"], ["concesionario_review_my_maintenance"],
			SoloInforma: "manifest en ingles, pregunta en espanol"),

		new("¿Cuánto he gastado en total en facturas?", "facturas", ["facturas_total_de_facturas"], []),

		// LOS CRUCES. Ninguna tool los nombra hoy -Lab.Cruce dice que hay que nombrarlos-, asi
		// que se miden y se informan, no se exigen.
		new("¿Por qué me subió el colesterol? ¿Tiene que ver con lo que compro en el súper?", "cruce",
			["examenes_evaluar_tendencia", "compras_consumo_de"], ["compras_como_viene_mi_canasta"],
			SoloInforma: "cruce sin tool que lo nombre"),
		new("Con lo que manejo por día, ¿cuánto le falta al carro para el próximo mantenimiento?", "cruce",
			["cargas_mi_ritmo_de_manejo", "concesionario_review_my_maintenance"], ["concesionario_check_one_operation"],
			SoloInforma: "cruce sin tool que lo nombre, y concesionario en ingles"),
	];
}
