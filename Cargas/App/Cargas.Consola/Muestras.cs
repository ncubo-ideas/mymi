using Puppets.Portador;

namespace Cargas.Consola
{
	// EL GUION DE LA DEMO, en la MISMA forma de wire que emite el mock del laboratorio.
	// Se usa para medir el dominio sin depender de que docker este arriba: entran por la
	// misma ruta (SenalDeEvento -> RutaDeCargas -> comando) que los reales.
	//
	// Las fechas son LITERALES, no "hoy menos tres anos". Es obligatorio: el dominio no
	// lee el reloj, y si estas fechas se calcularan con DateTime.Now el mismo journal
	// reproducido manana daria otra cantidad de dias transcurridos y por lo tanto otra
	// tendencia. Un guion de demo con fechas moviles no es reproducible.
	//
	// La historia que cuentan estos ocho eventos:
	//
	//   antigeno prostatico  3.0 -> 3.1 -> 3.0 -> 5.2   en tres anos
	//     los tres primeros son la LINEA BASE: sin ellos el 5.2 es un numero cualquiera
	//     un poco arriba del techo. Con ellos es un salto de 2.2 sobre un paciente que
	//     venia planchado, y eso es lo que exige atencion.
	//
	//   colesterol total     200 -> 280                 en un ano
	//     el mismo salto en un dia seria un error de laboratorio. En un ano no.
	//
	//   hemoglobina y orina: para que la lista tenga cuerpo, y para que se vea que un
	//     examen sin serie (la orina) convive con los que si la tienen.
	internal static class Muestras
	{
		internal const string AnalitoDelAntigeno = "Antigeno prostatico";
		internal const string AnalitoDelColesterol = "Colesterol total";

		internal static SenalDeEvento[] LaHistoria() => new[]
		{
			// --- un perfil lipidico viejo: lo que hace visible "que falta medir" ---
			Fraccionado(200, "2022-11-01T08:30:00", "150", "38"),

			// --- linea base del antigeno: tres anos de estabilidad ---
			Sangre(201, "AntigenoProstatico", "2023-08-21T09:10:00", "3.0", "ng/mL"),
			Sangre(202, "AntigenoProstatico", "2024-08-21T09:05:00", "3.1", "ng/mL"),

			// --- hace un ano: primer colesterol y el antigeno todavia estable ---
			Sangre(203, "Colesterol", "2025-08-21T08:40:00", "200.0", "mg/dL"),
			Sangre(204, "AntigenoProstatico", "2025-08-21T08:41:00", "3.0", "ng/mL"),

			// --- hoy: los dos saltos que cuentan la historia ---
			Sangre(205, "Colesterol", "2026-08-21T08:30:00", "280.0", "mg/dL"),
			Sangre(206, "AntigenoProstatico", "2026-08-21T08:31:00", "5.2", "ng/mL"),

			// --- relleno con modelo real, no adorno ---
			Sangre(207, "Hemoglobina", "2026-08-21T08:32:00", "14.2", "g/dL"),
			Orina(208, "2026-08-21T08:33:00", "ambar", "negativo", "negativo"),

			// EL CONTRASTE DE ALVARO. Una sangre oculta positiva en heces se lee
			// "conviene revisarlo"; el MISMO positivo en la orina no espera a la proxima
			// cita. Misma forma de magnitud, distinta conducta, y la diferencia vive en el
			// analito.
			Heces(209, "2026-08-21T08:34:00", "marrón", "positivo"),

			// LA HISTORIA DE LOS BINARIOS, con ids posteriores y fechas anteriores: es el
			// caso de la carga atrasada, y el mismo orden con el que URL2 los siembra.
			//
			// Sin esto, el positivo del 209 es un dato suelto y lo unico que se puede
			// decir es "conviene repetir el examen". Con negativo en 2024 y positivo en
			// 2025, ya se repitio y volvio a salir — y eso pide otra cosa.
			Heces(210, "2024-08-21T09:15:00", "marrón", "negativo"),
			Heces(211, "2025-08-21T08:45:00", "marrón", "positivo"),
			Orina(212, "2025-08-21T08:44:00", "amarillo palido", "negativo", "negativo"),
		};

		private static SenalDeEvento Sangre(long id, string examen, string fecha, string valor, string unidad) =>
			new SenalDeEvento(id, "ExamenAgregado", $@"{{
				""id"": {id}, ""tipo"": ""ExamenAgregado"",
				""examen"": ""{examen}"",
				""fecha"": ""{fecha}"",
				""valor"": {valor},
				""unidad"": ""{unidad}""
			}}");

		private static SenalDeEvento Fraccionado(long id, string fecha, string ldl, string hdl) =>
			new SenalDeEvento(id, "ExamenAgregado", $@"{{
				""id"": {id}, ""tipo"": ""ExamenAgregado"",
				""examen"": ""ColesterolDetallado"",
				""fecha"": ""{fecha}"",
				""valor"": {ldl}, ""valorSecundario"": {hdl},
				""unidad"": ""mg/dL""
			}}");

		private static SenalDeEvento Heces(long id, string fecha, string color, string sangre) =>
			new SenalDeEvento(id, "ExamenAgregado", $@"{{
				""id"": {id}, ""tipo"": ""ExamenAgregado"",
				""examen"": ""Heces"",
				""fecha"": ""{fecha}"",
				""color"": ""{color}"",
				""sangre"": ""{sangre}""
			}}");

		// La tira ENTERA, igual que la emite URL2. Este arnes traia solo el color, y esa
		// diferencia con el mock ya escondio un defecto una vez: lo que la consola no
		// manda, la consola no puede medir.
		private static SenalDeEvento Orina(long id, string fecha, string color,
			string nitritos, string sangre) =>
			new SenalDeEvento(id, "ExamenAgregado", $@"{{
				""id"": {id}, ""tipo"": ""ExamenAgregado"",
				""examen"": ""Orina"",
				""fecha"": ""{fecha}"",
				""color"": ""{color}"",
				""nitritos"": ""{nitritos}"",
				""sangre"": ""{sangre}""
			}}");

		// Lo que la historia tiene que dar. Son los criterios de la fase 1: si esto no
		// sale asi, el dominio no esta contando la historia que la demo promete.
		internal const int CargasEsperados = 13;
		internal const string TendenciaEsperadaDelAntigeno = "en ascenso";
		internal const string TendenciaEsperadaDelColesterol = "en ascenso";

		// Una senal de un tipo que esta app no consume. La ruta debe devolver null y el
		// bucle descartarla, sin inventar un comando para tragarsela.
		internal static SenalDeEvento TipoDesconocido() =>
			new SenalDeEvento(301, "CitaAgendada", @"{ ""id"": 301, ""tipo"": ""CitaAgendada"" }");

		// Un ExamenAgregado de un examen que la app NO modela. Tambien se descarta: el
		// laboratorio puede emitir mas cargas de los que esta app sabe leer.
		internal static SenalDeEvento ExamenNoModelado() =>
			new SenalDeEvento(302, "ExamenAgregado", @"{
				""id"": 302, ""tipo"": ""ExamenAgregado"",
				""examen"": ""Tiroides"",
				""fecha"": ""2026-08-21T08:34:00"",
				""valor"": 2.1, ""unidad"": ""mg/dL""
			}");

		// Un colesterol que salta 80 mg/dL en UN DIA respecto del de hoy. Sirve para
		// demostrar que el dominio distingue un error de medicion de un cambio real:
		// mismo salto que 203->205, pero en un dia.
		internal static SenalDeEvento SaltoInverosimil() =>
			Sangre(303, "Colesterol", "2026-08-22T08:30:00", "360.0", "mg/dL");
	}
}
