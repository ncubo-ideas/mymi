using System;
using Examenes.App.Eventos;
using Examenes.Dominio;
using Puppets.Portador;

namespace Examenes.Consola
{
	// Las comprobaciones del DOMINIO PURO, sin actor y sin journal.
	//
	// Van en la consola y no en un proyecto de test porque este repo no tiene framework de
	// pruebas y `Facturas` usa la misma idea: la caratula de consola ES el arnes, y `medir`
	// es su criterio de aceptacion. Esto es la version del dominio, un nivel mas abajo:
	// corre en microsegundos, no toca disco, y cuando algo se rompe dice exactamente que
	// regla se rompio en vez de dejar un total distinto al esperado.
	//
	// Lo que se comprueba aca es lo que NO se puede ver en la salida de `medir`: que la
	// unidad sea parte del dato, que los rangos dependan de la edad, que la direccion
	// preocupante sea por analito, y que un examen sin serie conviva con los que si la
	// tienen.
	internal static class Comprobaciones
	{
		private static int fallas;
		private static int corridas;

		// Fecha de referencia fija. Igual que el guion: si esto usara DateTime.Now, las
		// comprobaciones de "en un ano" cambiarian de resultado segun el dia.
		private static readonly DateTime Hoy = new DateTime(2026, 8, 21);

		internal static int Correr()
		{
			fallas = 0;
			corridas = 0;

			LasCuatroLecturasDelGuion();
			LaUnidadEsParteDelDato();
			LosRangosDependenDeLaEdad();
			LaDireccionPreocupanteEsPorAnalito();
			LaOrinaNoTieneNumero();
			SinSerieNoSeInventaTendencia();
			ElOrdenDeLlegadaNoImportaS();
			LaVelocidadEsLaLecturaDelAntigeno();
			ElUrianalisisInformaDosFormas();
			ElAnalitoEsUnaIdentidadNoUnNombre();
			LaRutaResuelveElNombreDelLaboratorio();

			Console.WriteLine();
			Console.WriteLine(fallas == 0
				? $"TODO OK  ({corridas} comprobaciones)"
				: $"{fallas} FALLAS de {corridas} comprobaciones");
			return fallas == 0 ? 0 : 1;
		}

		// ---- el corazon: la misma diferencia se lee distinto segun el tiempo ----
		private static void LasCuatroLecturasDelGuion()
		{
			Titulo("las cuatro lecturas del guion");

			EvaluacionDeTendencia unDia = SerieDeColesterol(
				(Hoy.AddDays(-1), 200m), (Hoy, 280m));
			Igual("colesterol 200->280 en 1 dia", "salto inverosimil", unDia.NombreDeLaTendencia);
			Igual("  sugiere repetir el examen", "True", unDia.SugiereRepetirElExamen.ToString());

			EvaluacionDeTendencia unAno = SerieDeColesterol(
				(Hoy.AddDays(-365), 200m), (Hoy, 280m));
			Igual("colesterol 200->280 en 1 ano", "en ascenso", unAno.NombreDeLaTendencia);
			Igual("  exige atencion", "True", unAno.ExigeAtencion.ToString());
			Igual("  cambio con signo", "80", unAno.Cambio.ToString());
			Igual("  dias transcurridos", "365", unAno.DiasTranscurridos.ToString());

			EvaluacionDeTendencia estable = SerieDeAntigeno(
				(Hoy.AddDays(-1095), 3.0m), (Hoy.AddDays(-730), 3.1m));
			Igual("antigeno 3.0->3.1 en 2 anos", "estable", estable.NombreDeLaTendencia);
			Igual("  no exige atencion", "False", estable.ExigeAtencion.ToString());

			EvaluacionDeTendencia salto = SerieDeAntigeno(
				(Hoy.AddDays(-365), 3.0m), (Hoy, 5.2m));
			Igual("antigeno 3.0->5.2 en 1 ano", "en ascenso", salto.NombreDeLaTendencia);
			Igual("  exige atencion", "True", salto.ExigeAtencion.ToString());

			// Un valor raro EN EL MEDIO de la serie tambien se detecta: si solo se
			// compararan los extremos, el tiempo total lo diluiria y quedaria invisible.
			EvaluacionDeTendencia enElMedio = SerieDeColesterol(
				(Hoy.AddDays(-365), 200m), (Hoy.AddDays(-364), 280m), (Hoy, 210m));
			Igual("un salto de un dia EN EL MEDIO", "salto inverosimil", enElMedio.NombreDeLaTendencia);
			Igual("  reporta el paso sospechoso, no los extremos", "200 -> 280",
				$"{enElMedio.ValorInicial} -> {enElMedio.ValorFinal}");
		}

		// ---- "200 que": el que es una clase ----
		private static void LaUnidadEsParteDelDato()
		{
			Titulo("la unidad es parte del dato");

			var mg = new Medicion(200m, UnidadDeMedida.MiligramosPorDecilitro);
			var ng = new Medicion(200m, UnidadDeMedida.NanogramosPorMililitro);

			Revienta<InvalidOperationException>("restar mg/dL menos ng/mL", () => mg.DiferenciaDesde(ng));
			Revienta<InvalidOperationException>("ordenar mg/dL contra ng/mL", () => mg.EsMayorQue(ng));
			Revienta<ArgumentException>("antigeno medido en mg/dL",
				() => new AntigenoProstatico(Hoy, 3m, "mg/dL"));
			Revienta<ArgumentException>("unidad que no existe",
				() => UnidadDeMedida.PorSimbolo("miligramos"));
			Revienta<ArgumentOutOfRangeException>("valor negativo",
				() => new Medicion(-1m, UnidadDeMedida.GramosPorDecilitro));
			Igual("la medicion se dice con unidad", "200 mg/dL", mg.Expresado);
		}

		// ---- los rangos viven en el codigo, y dependen de la edad ----
		private static void LosRangosDependenDeLaEdad()
		{
			Titulo("los rangos de referencia dependen de la edad");

			var psa = new AntigenoProstatico(Hoy, 4.0m, "ng/mL");
			Igual("PSA 4.0 a los 45", "alto", psa.ClasificarPara(45).Nombre);
			Igual("PSA 4.0 a los 65", "normal", psa.ClasificarPara(65).Nombre);

			var col = new Colesterol(Hoy, 210m, "mg/dL");
			Igual("colesterol 210 a los 30", "normal", col.ClasificarPara(30).Nombre);
			Igual("colesterol 210 a los 60", "alto", col.ClasificarPara(60).Nombre);
		}

		// ---- subir es malo en unos analitos y bajar en otros ----
		private static void LaDireccionPreocupanteEsPorAnalito()
		{
			Titulo("la direccion que preocupa es por analito");

			var bajando = new Expediente(60);
			bajando.Agregar(new Hemoglobina(Hoy.AddDays(-365), 15.0m, "g/dL"));
			bajando.Agregar(new Hemoglobina(Hoy, 11.0m, "g/dL"));
			EvaluacionDeTendencia baja = bajando.EvaluarTendenciaDe(Analito.Hemoglobina.Nombre);
			Igual("hemoglobina 15->11 en 1 ano", "en descenso", baja.NombreDeLaTendencia);
			Igual("  exige atencion (bajar es lo malo)", "True", baja.ExigeAtencion.ToString());

			var subiendo = new Expediente(60);
			subiendo.Agregar(new Hemoglobina(Hoy.AddDays(-365), 11.0m, "g/dL"));
			subiendo.Agregar(new Hemoglobina(Hoy, 15.0m, "g/dL"));
			Igual("hemoglobina 11->15 NO exige atencion", "False",
				subiendo.EvaluarTendenciaDe(Analito.Hemoglobina.Nombre).ExigeAtencion.ToString());
		}

		// ---- la orina no tiene numero; la sangre no tiene color ----
		private static void LaOrinaNoTieneNumero()
		{
			Titulo("la orina no tiene numero, la sangre no tiene color");

			var orina = new ExamenDeOrina(Hoy, "ambar");
			Igual("la orina no aporta serie", "False", orina.AportaSerie.ToString());
			Igual("la orina informa un color", "ambar", orina.ResultadoExpresado);
			Igual("el colesterol si aporta serie", "True",
				new Colesterol(Hoy, 200m, "mg/dL").AportaSerie.ToString());
			Revienta<ArgumentException>("color que el laboratorio no reporta",
				() => new ExamenDeOrina(Hoy, "fucsia"));

			var expediente = new Expediente(60);
			expediente.Agregar(orina);
			Igual("la orina no crea ninguna serie", "0", expediente.CantidadDeAnalitosConSerie.ToString());
			Igual("  pero si entra a la lista", "1", expediente.Cantidad.ToString());
		}

		// ---- con un punto no hay tendencia, y no se inventa ----
		private static void SinSerieNoSeInventaTendencia()
		{
			Titulo("sin serie no se inventa una tendencia");

			var expediente = new Expediente(60);
			expediente.Agregar(new AntigenoProstatico(Hoy, 3.0m, "ng/mL"));
			Igual("una sola medicion", "indeterminada",
				expediente.EvaluarTendenciaDe(Analito.AntigenoProstatico.Nombre).NombreDeLaTendencia);
			Igual("analito sin ninguna medicion", "indeterminada",
				expediente.EvaluarTendenciaDe(Analito.Hemoglobina.Nombre).NombreDeLaTendencia);
			Igual("  y se puede saber que no hay serie", "False",
				expediente.EvaluarTendenciaDe(Analito.Hemoglobina.Nombre).HaySerie.ToString());
		}

		// ---- un evento atrasado no da vuelta la tendencia ----
		private static void ElOrdenDeLlegadaNoImportaS()
		{
			Titulo("un examen que llega atrasado se ordena por fecha");

			var expediente = new Expediente(60);
			expediente.Agregar(new Colesterol(Hoy, 280m, "mg/dL"));
			expediente.Agregar(new Colesterol(Hoy.AddDays(-365), 200m, "mg/dL"));
			EvaluacionDeTendencia t = expediente.EvaluarTendenciaDe(Analito.ColesterolTotal.Nombre);
			Igual("insertado al reves: sigue siendo ascenso", "en ascenso", t.NombreDeLaTendencia);
			Igual("  cambio +80, no -80", "80", t.Cambio.ToString());
		}

		// ---------------------------------------------------------------- andamio

		// ---- la velocidad: la lectura que el antigeno prostatico exige ----
		private static void LaVelocidadEsLaLecturaDelAntigeno()
		{
			Titulo("la velocidad se mide sobre el tramo reciente, no sobre la serie entera");

			// La serie del guion completa. El tramo reciente son los ultimos 365 dias.
			var expediente = new Expediente(60);
			expediente.Agregar(new AntigenoProstatico(Hoy.AddDays(-1096), 3.0m, "ng/mL"));
			expediente.Agregar(new AntigenoProstatico(Hoy.AddDays(-731), 3.1m, "ng/mL"));
			expediente.Agregar(new AntigenoProstatico(Hoy.AddDays(-365), 3.0m, "ng/mL"));
			expediente.Agregar(new AntigenoProstatico(Hoy, 5.2m, "ng/mL"));

			Velocidad psa = expediente.VelocidadDe(Analito.AntigenoProstatico.Nombre);
			Igual("PSA: el tramo son los ultimos 365 dias", "365", psa.DiasDelTramo.ToString());
			Igual("  2.2 ng/mL en un ano = 2.20 por ano", "2.20", psa.PorAno.ToString("0.00"));
			Igual("  supera el umbral de 0.75", "True", psa.ExigeAtencion.ToString());
			Igual("  y se dice completa", "2.20 ng/mL por ano", psa.Expresada);

			// EL PUNTO DE TODA LA REGLA. Promediando sobre la serie entera los mismos datos
			// dan 2.2 ng/mL en 1096 dias = 0.73 por ano, que NO llega al umbral: los tres
			// anos de estabilidad previos taparian el salto de este ano. Se comprueba a
			// mano para dejar el contraste medido y no como afirmacion de un comentario.
			decimal sobreLaSerieEntera = 2.2m * 365m / 1096m;
			Igual("la serie entera daria 0.73 y NO alarmaria", "0.73",
				sobreLaSerieEntera.ToString("0.00"));
			Igual("  por eso el tramo reciente es la regla", "True",
				(sobreLaSerieEntera < 0.75m && psa.PorAno >= 0.75m).ToString());

			// Bajar rapido no es una alarma de PSA. Sin comparar CON SIGNO, el valor
			// absoluto reportaria esto como si fuera lo mismo que subir.
			var bajando = new Expediente(60);
			bajando.Agregar(new AntigenoProstatico(Hoy.AddDays(-365), 5.2m, "ng/mL"));
			bajando.Agregar(new AntigenoProstatico(Hoy, 3.0m, "ng/mL"));
			Igual("un PSA que BAJA rapido no exige atencion", "False",
				bajando.VelocidadDe(Analito.AntigenoProstatico.Nombre).ExigeAtencion.ToString());

			// El colesterol no se lee por velocidad, y esa es una respuesta del analito.
			var col = new Expediente(60);
			col.Agregar(new Colesterol(Hoy.AddDays(-365), 200m, "mg/dL"));
			col.Agregar(new Colesterol(Hoy, 280m, "mg/dL"));
			Velocidad velColesterol = col.VelocidadDe(Analito.ColesterolTotal.Nombre);
			Igual("el colesterol NO se interpreta por velocidad", "False",
				velColesterol.EsAplicable.ToString());
			Igual("  aunque cambio 80 en un ano", "False", velColesterol.ExigeAtencion.ToString());

			// Una sola medicion no da un ritmo, y preguntarlo es legitimo.
			var unSolo = new Expediente(60);
			unSolo.Agregar(new AntigenoProstatico(Hoy, 5.2m, "ng/mL"));
			Igual("una sola medicion no da velocidad", "False",
				unSolo.VelocidadDe(Analito.AntigenoProstatico.Nombre).EsAplicable.ToString());
			Igual("un analito del que no hay nada tampoco revienta", "False",
				unSolo.VelocidadDe(Analito.Hemoglobina.Nombre).EsAplicable.ToString());
		}

		// ---- un examen, dos formas de magnitud, y ninguna es un numero ----
		private static void ElUrianalisisInformaDosFormas()
		{
			Titulo("el urianalisis informa dos formas de magnitud distintas");

			var completo = new ExamenDeOrina(Hoy, "ambar", "negativo");
			Igual("dice las dos cosas", "ambar, nitritos negativo", completo.ResultadoExpresado);
			Igual("  el color es nominal", "True", (completo.Color is MagnitudNominal).ToString());
			Igual("  los nitritos son binarios", "True", (completo.Nitritos is MagnitudBinaria).ToString());
			Igual("  sin hallazgo", "False", completo.TieneHallazgo.ToString());

			var conHallazgo = new ExamenDeOrina(Hoy, "ambar", "positivo");
			Igual("nitritos positivos SI son hallazgo", "True", conHallazgo.TieneHallazgo.ToString());

			// El tercer estado, que es la razon de que esto no sea un bool.
			var sinTira = new ExamenDeOrina(Hoy, "ambar");
			Igual("un examen viejo no midio nitritos", "no realizado", sinTira.Nitritos.Expresado);
			Igual("  y eso NO es un negativo", "False", sinTira.Nitritos.FueRealizada.ToString());
			Igual("  no lo reporta como hallazgo", "False", sinTira.Nitritos.EsHallazgo.ToString());
			Revienta<InvalidOperationException>("  preguntar el resultado de lo que no se midio",
				() => { bool _ = sinTira.Nitritos.EsPositivo; });
			Igual("  y la lista no menciona nitritos", "ambar", sinTira.ResultadoExpresado);

			Revienta<ArgumentException>("un resultado de nitritos que no existe",
				() => new ExamenDeOrina(Hoy, "ambar", "mas o menos"));

			// Ni el color ni los nitritos entran a ninguna serie: la pregunta no existe.
			var expediente = new Expediente(60);
			expediente.Agregar(conHallazgo);
			Igual("la orina no crea series", "0", expediente.CantidadDeAnalitosConSerie.ToString());
			Igual("  pero si entra a la lista", "1", expediente.Cantidad.ToString());
		}

		private static EvaluacionDeTendencia SerieDeColesterol(params (DateTime fecha, decimal valor)[] puntos)
		{
			var expediente = new Expediente(60);
			foreach ((DateTime fecha, decimal valor) in puntos)
				expediente.Agregar(new Colesterol(fecha, valor, "mg/dL"));
			return expediente.EvaluarTendenciaDe(Analito.ColesterolTotal.Nombre);
		}

		private static EvaluacionDeTendencia SerieDeAntigeno(params (DateTime fecha, decimal valor)[] puntos)
		{
			var expediente = new Expediente(60);
			foreach ((DateTime fecha, decimal valor) in puntos)
				expediente.Agregar(new AntigenoProstatico(fecha, valor, "ng/mL"));
			return expediente.EvaluarTendenciaDe(Analito.AntigenoProstatico.Nombre);
		}


		// ---- el analito es una identidad, no la cadena con que lo escribieron ----
		//
		// MEDIDO sobre tres reportes reales del mismo paciente: el mismo analito llega como
		// "HEMOGLOBINA", como "HGB" y como "Hemoglobina" segun el laboratorio. Con el
		// analito como cadena eso daba tres series de un punto y la app contestaba
		// "indeterminada" tres veces, que es perder la unica capacidad que justifica el
		// dominio justo en el caso para el que existe.
		private static void ElAnalitoEsUnaIdentidadNoUnNombre()
		{
			Titulo("el analito es una identidad, no un nombre");

			Igual("una abreviatura de laboratorio resuelve al analito", "True",
				Analito.PorNombre("HGB").EsElMismo(Analito.Hemoglobina).ToString());
			Igual("  el acento no lo convierte en otro", "True",
				Analito.PorNombre("Antígeno prostático")
					.EsElMismo(Analito.AntigenoProstatico).ToString());
			Igual("  ni las mayusculas ni los espacios sobrantes", "True",
				Analito.PorNombre("  COLESTEROL  ").EsElMismo(Analito.ColesterolTotal).ToString());
			Igual("  ni los guiones en el medio", "True",
				Analito.PorNombre("LDL-C").EsElMismo(Analito.ColesterolLdl).ToString());

			// LA comprobacion de toda la seccion: la serie NO se parte por el nombre.
			Expediente expediente = new Expediente(60);
			expediente.Agregar(new Hemoglobina(Hoy.AddDays(-730), 15.8m, "g/dL"));
			expediente.Agregar(new Hemoglobina(Hoy.AddDays(-52), 15.3m, "g/dL"));
			expediente.Agregar(new Hemoglobina(Hoy, 16.2m, "g/dL"));

			Igual("tres hojas de tres laboratorios son UNA serie", "1",
				expediente.CantidadDeAnalitosConSerie.ToString());
			Igual("  y se alcanza preguntando como la nombra cada uno", "3",
				expediente.EvaluarTendenciaDe("HGB").CantidadDeMediciones.ToString());
			Igual("  da lo mismo por cual de sus nombres se pregunte", "3",
				expediente.EvaluarTendenciaDe("Hemoglobina").CantidadDeMediciones.ToString());
			Igual("  y por el nombre con acento tambien", "3",
				expediente.SerieDe("HB").Cantidad.ToString());

			// Las dos respuestas que antes eran indistinguibles.
			Revienta<ArgumentException>("un nombre que no es ningun analito revienta",
				() => expediente.EvaluarTendenciaDe("colesterol malo"));
			Igual("un analito real sin mediciones NO revienta", "indeterminada",
				expediente.EvaluarTendenciaDe("PSA").NombreDeLaTendencia);

			Igual("el nombre para mostrar lleva el acento", "Antígeno prostático",
				Analito.AntigenoProstatico.ParaMostrar);
			Igual("  y el canonico es el que sigue siendo la llave", "Antigeno prostatico",
				Analito.AntigenoProstatico.Nombre);

			// La unidad ahora la dice el analito, no la subclase del examen.
			Igual("la unidad es del analito", "g/dL", Analito.Hemoglobina.Unidad.Simbolo);
			Revienta<ArgumentException>("  y un evento en otra unidad no entra",
				() => new Hemoglobina(Hoy, 15.3m, "mg/dL"));

			// Propiedad, no ejemplo: TODOS los nombres de cada analito vuelven a el. Es lo
			// que protege la tabla de sinonimos el dia que alguien agregue uno de mas.
			foreach (Analito analito in Analito.Todos)
			{
				bool todosVuelven = analito.RespondeA(analito.Nombre)
					&& analito.RespondeA(analito.ParaMostrar);
				foreach (string sinonimo in analito.Sinonimos)
					todosVuelven = todosVuelven && analito.RespondeA(sinonimo);

				Igual($"  todos los nombres de {analito.Nombre} vuelven a el", "True",
					todosVuelven.ToString());
			}
		}


		// ---- la ruta acepta el vocabulario del laboratorio que emitio ----
		//
		// La otra punta de la identidad. Resolver bien al CONSULTAR no sirve de nada si al
		// INGERIR se compara el nombre con == contra el token que esta app eligio: el
		// evento se descarta y no hay serie que consultar.
		//
		// Y descartar sigue siendo lo correcto para lo que esta app no modela. Las dos
		// cosas conviven porque son preguntas distintas: "no se como se llama" es un
		// problema, "no modelo eso" no lo es.
		private static void LaRutaResuelveElNombreDelLaboratorio()
		{
			Titulo("la ruta acepta el vocabulario del laboratorio");

			Igual("un laboratorio que escribe 'HGB'", "Hemoglobina",
				AnalitoRuteado(Sangre("HGB", "15.3", "g/dL")));
			Igual("  otro que escribe 'HEMOGLOBINA'", "Hemoglobina",
				AnalitoRuteado(Sangre("HEMOGLOBINA", "15.8", "g/dL")));
			Igual("  y el token que emite URL2 hoy", "Hemoglobina",
				AnalitoRuteado(Sangre("Hemoglobina", "16.2", "g/dL")));
			Igual("el antigeno con acentos y todo", "Antigeno prostatico",
				AnalitoRuteado(Sangre("Antígeno prostático", "5.2", "ng/mL")));
			Igual("  y por su sigla", "Antigeno prostatico",
				AnalitoRuteado(Sangre("PSA", "5.2", "ng/mL")));

			// Lo que NO cambia: lo que esta app no modela se sigue descartando.
			Igual("un examen que esta app no modela se descarta", "descartado",
				AnalitoRuteado(Sangre("Tiroides", "2.1", "mg/dL")));
			Igual("  y un evento de otro tipo tampoco entra", "descartado",
				AnalitoRuteado(new SenalDeEvento(900, "CitaAgendada", @"{ ""id"": 900 }")));
			Igual("  ni uno sin fecha", "descartado",
				AnalitoRuteado(new SenalDeEvento(901, "ExamenAgregado", @"{
					""id"": 901, ""tipo"": ""ExamenAgregado"",
					""examen"": ""Hemoglobina"", ""valor"": 15.0, ""unidad"": ""g/dL"" }")));

			// La forma del payload dejo de estar codificada en el nombre: el token viejo y
			// el nombre del analito con su fraccion producen el MISMO mensaje.
			ExamenAgregado porToken = Ruteado(Fraccionado("ColesterolDetallado"));
			ExamenAgregado porNombre = Ruteado(Fraccionado("LDL"));
			Igual("el token de forma que emite URL2 sigue entrando", "Colesterol LDL",
				porToken.Analito.Nombre);
			Igual("  y trae su fraccionamiento", "True", porToken.TraeFraccionamiento.ToString());
			Igual("un emisor que manda 'LDL' con su HDL da lo mismo", "Colesterol LDL",
				porNombre.Analito.Nombre);
			Igual("  y tambien trae fraccionamiento", "True", porNombre.TraeFraccionamiento.ToString());

			// Los dos casos que la ruta filtra para que el comando no arme un examen a
			// medias. El dominio modela el LDL junto con su HDL, no sueltos.
			Igual("un LDL sin su HDL no se aplica a medias", "descartado",
				AnalitoRuteado(Sangre("LDL", "177", "mg/dL")));
			Igual("  ni un HDL solo, que no forma serie", "descartado",
				AnalitoRuteado(Sangre("HDL", "32", "mg/dL")));

			// La orina no es un analito: es otro tipo de muestra, y por eso sigue siendo
			// el unico token propio que queda.
			ExamenAgregado orina = Ruteado(new SenalDeEvento(905, "ExamenAgregado", @"{
				""id"": 905, ""tipo"": ""ExamenAgregado"", ""examen"": ""Orina"",
				""fecha"": ""2026-08-21T08:33:00"", ""color"": ""ambar"" }"));
			Igual("la orina entra sin ser un analito", "True", orina.EsDeOrina.ToString());
			Igual("  y una orina sin color se descarta", "descartado",
				AnalitoRuteado(new SenalDeEvento(906, "ExamenAgregado", @"{
					""id"": 906, ""tipo"": ""ExamenAgregado"", ""examen"": ""Orina"",
					""fecha"": ""2026-08-21T08:33:00"" }")));
		}

		private static ExamenAgregado Ruteado(SenalDeEvento senal) =>
			RutaDeExamenes.Rutear(senal) as ExamenAgregado;

		// El nombre canonico del analito que la ruta resolvio, o "descartado" si devolvio
		// null. Un solo texto para comparar hace que la comprobacion diga que paso.
		private static string AnalitoRuteado(SenalDeEvento senal)
		{
			ExamenAgregado mensaje = Ruteado(senal);
			if (mensaje == null) return "descartado";
			return mensaje.EsDeOrina ? "orina" : mensaje.Analito.Nombre;
		}

		private static SenalDeEvento Sangre(string examen, string valor, string unidad) =>
			new SenalDeEvento(910, "ExamenAgregado", $@"{{
				""id"": 910, ""tipo"": ""ExamenAgregado"",
				""examen"": ""{examen}"",
				""fecha"": ""2026-08-21T08:30:00"",
				""valor"": {valor}, ""unidad"": ""{unidad}""
			}}");

		private static SenalDeEvento Fraccionado(string examen) =>
			new SenalDeEvento(911, "ExamenAgregado", $@"{{
				""id"": 911, ""tipo"": ""ExamenAgregado"",
				""examen"": ""{examen}"",
				""fecha"": ""2026-08-21T08:30:00"",
				""valor"": 177, ""valorSecundario"": 32, ""unidad"": ""mg/dL""
			}}");

		private static void Titulo(string texto)
		{
			Console.WriteLine();
			Console.WriteLine($"-- {texto} --");
		}

		private static void Igual(string titulo, string esperado, string obtenido)
		{
			corridas++;
			bool ok = esperado == obtenido;
			if (!ok) fallas++;

			Console.WriteLine($"  [{(ok ? "OK  " : "FALLA")}] {titulo}");
			if (!ok) Console.WriteLine($"           esperado=[{esperado}] obtenido=[{obtenido}]");
		}

		// Que una operacion invalida REVIENTE es parte del contrato, no un detalle: es la
		// unica forma de que un evento mal formado no entre al expediente.
		private static void Revienta<T>(string titulo, Action accion) where T : Exception
		{
			corridas++;
			try
			{
				accion();
				fallas++;
				Console.WriteLine($"  [FALLA] {titulo}");
				Console.WriteLine($"           esperado=[{typeof(T).Name}] obtenido=[no revento]");
			}
			catch (T)
			{
				Console.WriteLine($"  [OK  ] {titulo}");
			}
			catch (Exception ex)
			{
				fallas++;
				Console.WriteLine($"  [FALLA] {titulo}");
				Console.WriteLine($"           esperado=[{typeof(T).Name}] obtenido=[{ex.GetType().Name}]");
			}
		}
	}
}
