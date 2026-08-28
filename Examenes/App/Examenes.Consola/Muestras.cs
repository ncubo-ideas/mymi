using Puppets.Portador;

namespace Examenes.Consola
{
	// EL GUION DE LA DEMO, en la MISMA forma de wire que emite el mock del laboratorio.
	// Se usa para medir el dominio sin depender de que docker este arriba: entran por la
	// misma ruta (SenalDeEvento -> RutaDeExamenes -> comando) que los reales.
	//
	// ES EL EXPEDIENTE DE SONIA, la persona de la demo: 34 anos, y sus resultados del 28
	// de abril de 2026. Eso no es decoracion: la landing de URL1 la saluda por su nombre y
	// muestra dos veredictos —el conteo de lo que esta en rango y el colesterol que subio
	// un poquito—, asi que la app tiene que decir esas dos cosas con los numeros de ella.
	// Un expediente con un antigeno prostatico de un senor de 60 contaba otra historia.
	//
	// Las fechas son LITERALES, no "hoy menos tres anos". Es obligatorio: el dominio no
	// lee el reloj, y si estas fechas se calcularan con DateTime.Now el mismo journal
	// reproducido manana daria otra cantidad de dias transcurridos y por lo tanto otra
	// tendencia. Un guion de demo con fechas moviles no es reproducible.
	//
	// La historia que cuentan estos once eventos:
	//
	//   colesterol total     182 -> 188 -> 212 mg/dL   en dos anos
	//     los dos primeros son la LINEA BASE, y el 188 esta ahi para que se vea lo que el
	//     dominio NO llama cambio: 6 mg/dL estan por debajo del umbral del analito. El 212
	//     si lo supera, y aun asi queda por debajo del techo de 220 que le corresponde a
	//     los 34 — o sea "subio de verdad y todavia esta donde tiene que estar", que es
	//     exactamente el veredicto ambar del landing y no se puede decir con un valor solo.
	//
	//   hemoglobina          13.1 -> 13.4 g/dL         en dos anos
	//     estable, y con el rango que depende del sexo: mientras Sonia no lo declare este
	//     analito NO tiene veredicto, y por eso el bloque que insiste aparece.
	//
	//   LDL/HDL de 2021, y no es relleno: es lo UNICO que sale VENCIDO en "lo que falta
	//     medir". El LDL de Sonia salio en rango, asi que su control es cada 60 meses, y
	//     abril de 2021 los pasa: sin este punto todo el guion cae en abril de 2024-2026 y
	//     nada aparece atrasado nunca, o sea que la consulta que mira el HUECO —la unica
	//     que habla de lo que NO esta— no tendria nada que mostrar. Ademas ejercita
	//     ColesterolDetallado, con su HDL que se lee al reves que el LDL y cuyo piso
	//     tambien depende del sexo.
	//
	//   orina y heces: negativos en las dos y las tres veces. "En las tres veces que te lo
	//     hiciste nunca aparecio" es informacion, y es lo que un examen solo no puede
	//     decir. El expediente de Sonia esta limpio a proposito: la demo se abre con "todo
	//     esta bien" y un solo ambar. Para ver el otro lado de la serie binaria alcanza
	//     cambiar UN negativo por positivo aca; con el positivo de fabrica, el hallazgo
	//     tapaba el veredicto con el que la demo se abre.
	//
	// NO HAY ANTIGENO PROSTATICO, y su ausencia es el dato: a Sonia no se lo hicieron
	// porque no le corresponde. Que el dominio conteste "este analito no te corresponde"
	// no necesita que exista la serie —lo contesta la identidad del analito— asi que la
	// capacidad se sigue viendo sin sembrarle a una mujer un examen que ningun laboratorio
	// le haria.
	internal static class Muestras
	{
		internal const string AnalitoDelColesterol = "Colesterol total";
		internal const string AnalitoDeLaHemoglobina = "Hemoglobina";

		// El que NO esta en el guion, y se nombra igual: es el que la consola consulta para
		// mostrar que sin serie y sin prostata la respuesta no es "no hay datos".
		internal const string AnalitoDelAntigeno = "Antigeno prostatico";

		internal static SenalDeEvento[] LaHistoria() => new[]
		{
			// --- el perfil lipidico de 2021: lo unico que sale VENCIDO en "que falta medir" ---
			Fraccionado(200, "2021-04-22T08:15:00", "118", "58"),

			// --- hace dos anos: la linea base del colesterol y la primera hemoglobina ---
			Sangre(201, "Colesterol", "2024-04-24T08:20:00", "182.0", "mg/dL"),
			Sangre(202, "Hemoglobina", "2024-04-24T08:21:00", "13.1", "g/dL"),

			// --- hace un ano: subio 6 mg/dL, que para este analito NO es un cambio ---
			Sangre(203, "Colesterol", "2025-04-26T08:30:00", "188.0", "mg/dL"),

			// --- el 28 de abril, el dia que la landing muestra ---
			Sangre(204, "Colesterol", "2026-04-28T08:40:00", "212.0", "mg/dL"),
			Sangre(205, "Hemoglobina", "2026-04-28T08:41:00", "13.4", "g/dL"),
			Orina(206, "2026-04-28T08:42:00", "ambar", "negativo", "negativo"),
			Heces(207, "2026-04-28T08:43:00", "marrón", "negativo"),

			// LA HISTORIA DE LOS BINARIOS, con ids posteriores y fechas anteriores: es el
			// caso de la carga atrasada, y el mismo orden con el que URL2 los siembra. El
			// dominio ordena la serie por fecha, no por el orden en que llegaron.
			//
			// Sin esto, el negativo del 207 es un dato suelto y de "sangre en heces" no se
			// puede decir mas que "una sola prueba". Con las tres, la serie contesta que en
			// las tres veces nunca aparecio.
			Heces(208, "2024-04-24T08:22:00", "marrón", "negativo"),
			Heces(209, "2025-04-26T08:31:00", "marrón", "negativo"),
			Orina(210, "2025-04-26T08:32:00", "amarillo palido", "negativo", "negativo"),
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
		//
		// LAS DOS TENDENCIAS SON DE ANALITOS DISTINTOS a proposito: una que se movio y una
		// que no. Medir dos ascensos no habria descubierto un umbral roto, porque un
		// dominio que llamara "cambio" a cualquier diferencia habria pasado las dos.
		internal const int ExamenesEsperados = 11;
		internal const string TendenciaEsperadaDelColesterol = "en ascenso";
		internal const string TendenciaEsperadaDeLaHemoglobina = "estable";

		// Una senal de un tipo que esta app no consume. La ruta debe devolver null y el
		// bucle descartarla, sin inventar un comando para tragarsela.
		internal static SenalDeEvento TipoDesconocido() =>
			new SenalDeEvento(301, "CitaAgendada", @"{ ""id"": 301, ""tipo"": ""CitaAgendada"" }");

		// Un ExamenAgregado de un examen que la app NO modela. Tambien se descarta: el
		// laboratorio puede emitir mas examenes de los que esta app sabe leer.
		internal static SenalDeEvento ExamenNoModelado() =>
			new SenalDeEvento(302, "ExamenAgregado", @"{
				""id"": 302, ""tipo"": ""ExamenAgregado"",
				""examen"": ""Tiroides"",
				""fecha"": ""2026-04-28T08:44:00"",
				""valor"": 2.1, ""unidad"": ""mg/dL""
			}");

		// EL LABORATORIO PUBLICANDO EL SEXO. Es el segundo tipo de evento de esta narrativa,
		// y NO va en LaHistoria() a proposito: el guion arranca con el sexo DESCONOCIDO
		// para que se vea lo que el dominio puede y no puede decir sin ese dato.
		//
		// Sirve para medir la otra mitad: que la procedencia INSTITUCION entra por la ruta
		// como cualquier evento, con su Check de idempotencia y su checkpoint.
		internal static SenalDeEvento SexoPublicado(long id, string sexo) =>
			new SenalDeEvento(id, "DatosDePacienteActualizados", $@"{{
				""id"": {id}, ""tipo"": ""DatosDePacienteActualizados"",
				""fecha"": ""2026-04-28T08:00:00"",
				""sexo"": ""{sexo}""
			}}");

		// Un sexo que este dominio no modela. Se DESCARTA, como cualquier otro evento que
		// la app no sabe aplicar: un laboratorio puede publicar categorias que esta app no
		// tiene, y eso no puede impedirle ponerse al dia con el resto.
		internal static SenalDeEvento SexoNoModelado() =>
			new SenalDeEvento(304, "DatosDePacienteActualizados", @"{
				""id"": 304, ""tipo"": ""DatosDePacienteActualizados"",
				""fecha"": ""2026-04-28T08:00:00"",
				""sexo"": ""no lo dice""
			}");

		// Un colesterol que salta 148 mg/dL en UN DIA respecto del de hoy. Sirve para
		// demostrar que el dominio distingue un error de medicion de un cambio real: es un
		// salto mayor que el de 201->204, y lo que lo delata no es el tamano sino el dia.
		internal static SenalDeEvento SaltoInverosimil() =>
			Sangre(303, "Colesterol", "2026-04-29T08:30:00", "360.0", "mg/dL");
	}
}
