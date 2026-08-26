using System;
using System.Collections.Generic;
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
			LaLecturaEsDelAnalitoNoDeLaPantalla();
			ElCuadroPoneLasFechasEnElEje();
			DosBinariosIgualesPidenCosasDistintas();
			LoQueFaltaMedirEsLoQueNingunaPantallaMuestra();

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
			Igual("  cambio con signo", "80", unAno.Cambio.ToString());
			Igual("  dias transcurridos", "365", unAno.DiasTranscurridos.ToString());

			EvaluacionDeTendencia estable = SerieDeAntigeno(
				(Hoy.AddDays(-1095), 3.0m), (Hoy.AddDays(-730), 3.1m));
			Igual("antigeno 3.0->3.1 en 2 anos", "estable", estable.NombreDeLaTendencia);
			Igual("  y el analito no pide nada", "False",
				LeeElAntigeno((Hoy.AddDays(-1095), 3.0m), (Hoy.AddDays(-730), 3.1m))
					.PideAlgo.ToString());

			EvaluacionDeTendencia salto = SerieDeAntigeno(
				(Hoy.AddDays(-365), 3.0m), (Hoy, 5.2m));
			Igual("antigeno 3.0->5.2 en 1 ano", "en ascenso", salto.NombreDeLaTendencia);
			Igual("  y el analito manda a consulta", "consulta medica",
				LeeElAntigeno((Hoy.AddDays(-365), 3.0m), (Hoy, 5.2m)).NombreDeLaSeveridad);

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

			Interpretacion baja = LeeLaHemoglobina((Hoy.AddDays(-365), 15.0m), (Hoy, 11.0m));
			Igual("hemoglobina 15->11 en 1 ano", "en descenso", baja.Hechos.NombreDeLaTendencia);
			Igual("  el analito la lee como empeorando", "empeorando", baja.NombreDelVeredicto);
			Igual("  y dice cuanta anemia hay", "seguimiento", baja.NombreDeLaSeveridad);

			Interpretacion sube = LeeLaHemoglobina((Hoy.AddDays(-365), 11.0m), (Hoy, 15.0m));
			Igual("hemoglobina 11->15 es mejorando", "mejorando", sube.NombreDelVeredicto);
			Igual("  y no pide nada", "False", sube.PideAlgo.ToString());

			// El mismo signo, el otro analito, la conclusion opuesta. Es el criterio 2 del
			// feedback: si estos dos dieran lo mismo, el modelo colapso a "la misma tabla".
			Interpretacion psaSube = LeeElAntigeno((Hoy.AddDays(-365), 3.0m), (Hoy, 5.2m));
			Igual("el mismo ascenso en el antigeno es empeorando", "empeorando",
				psaSube.NombreDelVeredicto);
			Igual("  y en la hemoglobina el ascenso es mejorando", "mejorando",
				sube.NombreDelVeredicto);
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
			Igual("  y se dice completa", "2.20 ng/mL por año", psa.Expresada);

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

		// Las mismas series, pero pidiendo la LECTURA del analito en vez de los hechos.
		private static Interpretacion LeeElColesterol(params (DateTime fecha, decimal valor)[] puntos)
		{
			var expediente = new Expediente(60);
			foreach ((DateTime fecha, decimal valor) in puntos)
				expediente.Agregar(new Colesterol(fecha, valor, "mg/dL"));
			return expediente.InterpretarSerieDe(Analito.ColesterolTotal.Nombre);
		}

		private static Interpretacion LeeElAntigeno(params (DateTime fecha, decimal valor)[] puntos)
		{
			var expediente = new Expediente(60);
			foreach ((DateTime fecha, decimal valor) in puntos)
				expediente.Agregar(new AntigenoProstatico(fecha, valor, "ng/mL"));
			return expediente.InterpretarSerieDe(Analito.AntigenoProstatico.Nombre);
		}

		private static Interpretacion LeeLaHemoglobina(params (DateTime fecha, decimal valor)[] puntos)
		{
			var expediente = new Expediente(60);
			foreach ((DateTime fecha, decimal valor) in puntos)
				expediente.Agregar(new Hemoglobina(fecha, valor, "g/dL"));
			return expediente.InterpretarSerieDe(Analito.Hemoglobina.Nombre);
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

			// MEDIDO contra el emisor real: URL2 serializa `valorSecundario` en TODOS los
			// eventos porque es un decimal no anulable, asi que llega en cero hasta en un
			// antigeno. Preguntar por PRESENCIA hacia que todo pareciera fraccionado y el
			// primer Refresh reventaba. Lo que dice que hay fraccion es que haya un valor.
			Igual("un antigeno con valorSecundario en cero NO es fraccionado",
				"Antigeno prostatico",
				AnalitoRuteado(new SenalDeEvento(912, "ExamenAgregado", @"{
					""id"": 912, ""tipo"": ""ExamenAgregado"",
					""examen"": ""AntigenoProstatico"",
					""fecha"": ""2026-08-21T08:31:00"",
					""valor"": 5.2, ""valorSecundario"": 0, ""unidad"": ""ng/mL""
				}")));
			Igual("  y no dice traer fraccionamiento", "False",
				Ruteado(new SenalDeEvento(913, "ExamenAgregado", @"{
					""id"": 913, ""tipo"": ""ExamenAgregado"",
					""examen"": ""Hemoglobina"",
					""fecha"": ""2026-08-21T08:32:00"",
					""valor"": 14.2, ""valorSecundario"": 0, ""unidad"": ""g/dL""
				}")).TraeFraccionamiento.ToString());

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


		// ---- la lectura la produce el analito, no la pantalla ----
		//
		// Los criterios de aceptacion del feedback de Alvaro del 2026-08-25. La app le
		// ponia "EN ASCENSO" y "Conviene que lo vea tu medico" a cualquier analito que se
		// moviera, con la frase escrita en el .razor. Eso es la misma tabla, y esto es lo
		// que la reemplaza.
		private static void LaLecturaEsDelAnalitoNoDeLaPantalla()
		{
			Titulo("la lectura la produce el analito");

			// EL CASO DE ALVARO, textual: "por 20 anos lo he tenido super bien, un dia me
			// sale altisimo, y lo que deseo ver es como va bajando". 280 sigue fuera de
			// rango y es BUENA NOTICIA.
			Interpretacion bajandoDelPico = LeeElColesterol(
				(Hoy.AddDays(-730), 180m), (Hoy.AddDays(-365), 320m), (Hoy, 280m));
			Igual("colesterol 280 bajando desde 320", "mejorando", bajandoDelPico.NombreDelVeredicto);
			Igual("  fuera de rango y sin embargo no alarma", "informativo",
				bajandoDelPico.NombreDeLaSeveridad);
			Igual("  y no le pide nada a la persona", "False", bajandoDelPico.PideAlgo.ToString());

			// El mismo valor final, la historia al reves.
			Interpretacion subiendo = LeeElColesterol(
				(Hoy.AddDays(-730), 180m), (Hoy.AddDays(-365), 190m), (Hoy, 280m));
			Igual("el MISMO 280 subiendo desde 190", "empeorando", subiendo.NombreDelVeredicto);
			Igual("  ese si va al medico", "consulta medica", subiendo.NombreDeLaSeveridad);

			// El nivel habitual, que es lo que distingue un episodio de un fondo alto.
			var conFondo = new Expediente(60);
			foreach (decimal v in new[] { 180m, 175m, 185m, 320m, 280m })
				conFondo.Agregar(new Colesterol(Hoy.AddDays(-400 + (int)(v % 7) * 10), v, "mg/dL"));
			Igual("el colesterol sabe cual es tu nivel habitual", "True",
				(new Colesterol(Hoy, 280m, "mg/dL")
					.NivelHabitual(conFondo.SerieDe(Analito.ColesterolTotal.Nombre)) != null).ToString());

			// MEDIDO sobre un reporte real: 220 (2022) -> 234 (2026), 14 mg/dL en 1362
			// dias. Parece un ascenso; esta por debajo de la variacion biologica del propio
			// analito. Antes la app decia "en ascenso" y se equivocaba.
			Interpretacion real = LeeElColesterol((Hoy.AddDays(-1362), 220m), (Hoy, 234m));
			Igual("220 -> 234 en 1362 dias no es un ascenso", "estable", real.NombreDelVeredicto);

			// Criterio 4: la accion NO se escribe en la pantalla. Sale del conjunto
			// cerrado, y dos severidades distintas dan acciones distintas.
			Igual("la accion viene del dominio", "mostráselo a tu médico en la próxima cita",
				subiendo.Accion);
			Igual("  y cuando no hay nada que hacer, no se inventa", "",
				bajandoDelPico.Accion);

			// Criterio 5 en su version disponible: dos analitos, dos severidades para el
			// mismo hecho de estar fuera de rango.
			Interpretacion anemia = LeeLaHemoglobina((Hoy.AddDays(-365), 13.5m), (Hoy, 9.5m));
			Igual("una hemoglobina en 9.5 es anemia moderada", "moderada",
				new Hemoglobina(Hoy, 9.5m, "g/dL").GradoDeAnemia().Nombre);
			Igual("  y por eso su severidad NO es la del colesterol", "consulta medica",
				anemia.NombreDeLaSeveridad);
			Igual("  una de 12.0 baja de grado y de severidad", "seguimiento",
				new Hemoglobina(Hoy, 12.0m, "g/dL").GradoDeAnemia().Severidad.Nombre);
			Igual("  y una de 7.5 sube a lo mas urgente", "atencion inmediata",
				new Hemoglobina(Hoy, 7.5m, "g/dL").GradoDeAnemia().Severidad.Nombre);

			// Criterio 6: la velocidad existe SOLO en el antigeno. En los demas la consulta
			// sigue siendo total y contesta "no aplica" en vez de devolver un cero que
			// parezca una medicion.
			var soloColesterol = new Expediente(60);
			soloColesterol.Agregar(new Colesterol(Hoy.AddDays(-365), 200m, "mg/dL"));
			soloColesterol.Agregar(new Colesterol(Hoy, 280m, "mg/dL"));
			Igual("el colesterol no tiene velocidad", "False",
				soloColesterol.VelocidadDe(Analito.ColesterolTotal.Nombre).EsAplicable.ToString());

			var soloAntigeno = new Expediente(60);
			soloAntigeno.Agregar(new AntigenoProstatico(Hoy.AddDays(-365), 3.0m, "ng/mL"));
			soloAntigeno.Agregar(new AntigenoProstatico(Hoy, 5.2m, "ng/mL"));
			Igual("  y el antigeno si", "True",
				soloAntigeno.VelocidadDe(Analito.AntigenoProstatico.Nombre).EsAplicable.ToString());

			// Criterio 7: una sola medicion no inventa tendencia. Estaba bien y sigue.
			var unaSola = new Expediente(60);
			unaSola.Agregar(new Hemoglobina(Hoy, 14.2m, "g/dL"));
			Interpretacion sinSerie = unaSola.InterpretarSerieDe(Analito.Hemoglobina.Nombre);
			Igual("una sola medicion sigue siendo indeterminada", "indeterminado",
				sinSerie.NombreDelVeredicto);

			// Pero el NIVEL ya se puede leer aunque no haya serie, y eso es propio de la
			// hemoglobina: una de 9.0 sola no es "no se sabe nada".
			var unaSolaBaja = new Expediente(60);
			unaSolaBaja.Agregar(new Hemoglobina(Hoy, 9.0m, "g/dL"));
			Igual("  pero una sola de 9.0 ya dice algo", "consulta medica",
				unaSolaBaja.InterpretarSerieDe(Analito.Hemoglobina.Nombre).NombreDeLaSeveridad);

			// Lo mas urgente de todo el expediente, que ninguna pantalla contesta.
			var variosAnalitos = new Expediente(60);
			variosAnalitos.Agregar(new Colesterol(Hoy.AddDays(-365), 190m, "mg/dL"));
			variosAnalitos.Agregar(new Colesterol(Hoy, 280m, "mg/dL"));
			variosAnalitos.Agregar(new Hemoglobina(Hoy.AddDays(-365), 14.0m, "g/dL"));
			variosAnalitos.Agregar(new Hemoglobina(Hoy, 14.2m, "g/dL"));
			Igual("lo mas urgente del expediente entero", "consulta medica",
				variosAnalitos.SeveridadMayor.Nombre);
		}


		// ---- el cuadro historico: las fechas son el eje ----
		//
		// Criterio 1 del feedback. La lista plana repetia el nombre del analito en cada
		// fila; el cuadro pone una fila por analito y una columna por fecha, para que la
		// serie se lea de un barrido horizontal.
		private static void ElCuadroPoneLasFechasEnElEje()
		{
			Titulo("el cuadro historico pone las fechas en el eje");

			var expediente = new Expediente(60);
			// Dos analitos el MISMO dia, a horas distintas: es una visita, una columna.
			expediente.Agregar(new AntigenoProstatico(Fecha(2023, 8, 21, 9, 10), 3.0m, "ng/mL"));
			expediente.Agregar(new AntigenoProstatico(Fecha(2024, 8, 21, 9, 5), 3.1m, "ng/mL"));
			expediente.Agregar(new Colesterol(Fecha(2025, 8, 21, 8, 40), 200m, "mg/dL"));
			expediente.Agregar(new AntigenoProstatico(Fecha(2025, 8, 21, 8, 41), 3.0m, "ng/mL"));
			expediente.Agregar(new Colesterol(Fecha(2026, 8, 21, 8, 30), 280m, "mg/dL"));
			expediente.Agregar(new AntigenoProstatico(Fecha(2026, 8, 21, 8, 31), 5.2m, "ng/mL"));
			expediente.Agregar(new Hemoglobina(Fecha(2026, 8, 21, 8, 32), 14.2m, "g/dL"));

			IReadOnlyList<CeldaDelCuadro> celdas = expediente.CeldasDelCuadro;

			// Siete examenes en cuatro dias: cuatro columnas, no siete.
			Igual("siete examenes en cuatro dias dan cuatro columnas", "4",
				(Maximo(celdas, c => c.Columna) + 1).ToString());
			Igual("  y tres analitos dan tres filas", "3",
				(Maximo(celdas, c => c.Fila) + 1).ToString());

			// NO se interpola: la grilla tiene 3x4 = 12 lugares y solo 7 estan ocupados.
			Igual("las celdas sin medicion NO se rellenan", "7", celdas.Count.ToString());

			// El barrido horizontal del antigeno: cuatro puntos en cuatro columnas.
			Igual("el antigeno ocupa las cuatro columnas", "0,1,2,3",
				Columnas(celdas, Analito.AntigenoProstatico.Nombre));
			// El colesterol entro tarde: sus dos primeras celdas no existen.
			Igual("  el colesterol solo las dos ultimas", "2,3",
				Columnas(celdas, Analito.ColesterolTotal.Nombre));
			Igual("  y la hemoglobina solo la ultima", "3",
				Columnas(celdas, Analito.Hemoglobina.Nombre));

			// La llave de la fila es el nombre CANONICO, que es con el que se pide la
			// lectura de esa fila.
			Igual("la fila se identifica con la llave del analito", "True",
				(expediente.InterpretarSerieDe(Celda(celdas, 0, 0).Analito) != null).ToString());
			Igual("  y muestra el nombre con acento", "Antígeno prostático",
				Celda(celdas, 0, 0).ParaMostrar);
			Igual("  con el resultado ya dicho, con unidad", "3.0 ng/mL",
				Celda(celdas, 0, 0).Expresado);

			// La orina no forma serie pero SI tiene un resultado por fecha, asi que entra
			// al cuadro. Va despues de las que si forman serie, y sin veredicto al final.
			expediente.Agregar(new ExamenDeOrina(Fecha(2026, 8, 21, 8, 33), "ambar"));
			IReadOnlyList<CeldaDelCuadro> conOrina = expediente.CeldasDelCuadro;
			Igual("la orina entra al cuadro", "4",
				(Maximo(conOrina, c => c.Fila) + 1).ToString());
			Igual("  al final, porque no forma serie", "False",
				Celda(conOrina, 3, 3).AportaSerie.ToString());
			Igual("  y su resultado no es un numero", "ambar", Celda(conOrina, 3, 3).Expresado);

			// Una remedicion el mismo dia no abre una columna nueva: gana la ultima.
			var mismoDia = new Expediente(60);
			mismoDia.Agregar(new Colesterol(Fecha(2026, 8, 21, 8, 30), 280m, "mg/dL"));
			mismoDia.Agregar(new Colesterol(Fecha(2026, 8, 21, 15, 0), 265m, "mg/dL"));
			IReadOnlyList<CeldaDelCuadro> repetido = mismoDia.CeldasDelCuadro;
			Igual("dos mediciones el mismo dia son una columna", "1", repetido.Count.ToString());
			Igual("  y gana la ultima por hora", "265 mg/dL", repetido[0].Expresado);
		}

		private static DateTime Fecha(int ano, int mes, int dia, int hora, int minuto) =>
			new DateTime(ano, mes, dia, hora, minuto, 0);

		private static int Maximo(IReadOnlyList<CeldaDelCuadro> celdas, Func<CeldaDelCuadro, int> de)
		{
			int mayor = -1;
			foreach (CeldaDelCuadro celda in celdas) if (de(celda) > mayor) mayor = de(celda);
			return mayor;
		}

		// Las columnas que ocupa un analito, para leer de un vistazo donde tiene huecos.
		private static string Columnas(IReadOnlyList<CeldaDelCuadro> celdas, string analito)
		{
			var indices = new List<string>();
			foreach (CeldaDelCuadro celda in celdas)
				if (celda.Analito == analito) indices.Add(celda.Columna.ToString());
			return string.Join(",", indices);
		}

		private static CeldaDelCuadro Celda(IReadOnlyList<CeldaDelCuadro> celdas, int fila, int columna)
		{
			foreach (CeldaDelCuadro celda in celdas)
				if (celda.Fila == fila && celda.Columna == columna) return celda;
			throw new InvalidOperationException($"no hay celda en ({fila},{columna})");
		}


		// ---- misma forma, distinta conducta ----
		//
		// Criterio 5, y el ejemplo con el que Alvaro explico TODO el feedback:
		//
		//   "Sangre en las heces ⟹ quizas estrenimiento. Sangre en la orina ⟹ quizas CORRA
		//    donde el medico, rapido. No es asunto de pantalla, es que el dominio deberia
		//    tener mas funciones o metodos."
		//
		// Si estas comprobaciones dieran lo mismo para las dos, el modelo colapso a la
		// misma tabla y no hacia falta el dominio.
		private static void DosBinariosIgualesPidenCosasDistintas()
		{
			Titulo("dos binarios de la misma forma piden cosas distintas");

			// Los dos son un binario positivo. Los dos son "fuera de lo normal".
			Igual("sangre en orina y en heces son la misma FORMA", "True",
				(SangreEnOrina.Positivo is MagnitudBinaria
					&& SangreEnHeces.Positivo is MagnitudBinaria).ToString());
			Igual("  las dos son hallazgo", "True",
				(SangreEnOrina.Positivo.EsHallazgo && SangreEnHeces.Positivo.EsHallazgo).ToString());

			// Y piden cosas distintas. ESTA es la comprobacion del criterio.
			Igual("la de orina no espera", "atencion inmediata",
				SangreEnOrina.Positivo.SeveridadDelHallazgo.Nombre);
			Igual("  la de heces se revisa", "seguimiento",
				SangreEnHeces.Positivo.SeveridadDelHallazgo.Nombre);
			Igual("  o sea que NO dan lo mismo", "True",
				SangreEnOrina.Positivo.SeveridadDelHallazgo
					.EsMasUrgenteQue(SangreEnHeces.Positivo.SeveridadDelHallazgo).ToString());

			// Y las acciones tampoco, porque salen de la severidad y no de la pantalla.
			Igual("la accion de la orina", "buscá atención médica pronto",
				SangreEnOrina.Positivo.SeveridadDelHallazgo.QueHacer);
			Igual("  la de heces", "conviene repetir el examen",
				SangreEnHeces.Positivo.SeveridadDelHallazgo.QueHacer);

			// TRES magnitudes en el urianalisis, y DOS de ellas son la misma forma sin
			// leerse igual: los nitritos van a consulta, la sangre no espera.
			Igual("en la MISMA tira, los nitritos piden otra cosa", "consulta medica",
				Nitritos.Positivo.SeveridadDelHallazgo.Nombre);
			Igual("  que la sangre de al lado", "atencion inmediata",
				SangreEnOrina.Positivo.SeveridadDelHallazgo.Nombre);

			// El tercer estado sigue vivo en los dos: no medido no es negativo.
			Igual("no medido no es negativo, tampoco aca", "False",
				SangreEnOrina.NoRealizado.EsHallazgo.ToString());
			Revienta<InvalidOperationException>("  y preguntar el resultado revienta",
				() => { bool _ = SangreEnOrina.NoRealizado.EsPositivo; });

			// Un negativo no pide nada. La severidad no se deduce de la forma.
			Igual("un negativo no pide nada", "informativo",
				SangreEnOrina.Negativo.SeveridadDelHallazgo.Nombre);

			// Y en el expediente entero: lo mas urgente puede ser un examen SIN SERIE.
			// Un modelo que solo mirara tendencias se lo perderia completo.
			var conOrina = new Expediente(60);
			conOrina.Agregar(new Colesterol(Hoy.AddDays(-365), 190m, "mg/dL"));
			conOrina.Agregar(new Colesterol(Hoy, 195m, "mg/dL"));
			conOrina.Agregar(new ExamenDeOrina(Hoy, "ambar", "negativo", "positivo"));
			Igual("lo mas urgente del expediente no tiene serie", "atencion inmediata",
				conOrina.SeveridadMayor.Nombre);

			var conHeces = new Expediente(60);
			conHeces.Agregar(new Colesterol(Hoy.AddDays(-365), 190m, "mg/dL"));
			conHeces.Agregar(new Colesterol(Hoy, 195m, "mg/dL"));
			conHeces.Agregar(new ExamenDeHeces(Hoy, "chocolate", "positivo"));
			Igual("  y el mismo positivo en heces no escala igual", "seguimiento",
				conHeces.SeveridadMayor.Nombre);

			// El examen dice la lectura; la pantalla no la compone.
			Igual("el examen de heces dice lo suyo", "True",
				new ExamenDeHeces(Hoy, "chocolate", "positivo").ParaSonia
					.Contains("conviene revisarlo").ToString());
			Igual("  y el de orina dice otra cosa", "True",
				new ExamenDeOrina(Hoy, "ambar", "negativo", "positivo").ParaSonia
					.Contains("pronto").ToString());

			// Los journals viejos siguen entrando: un examen de orina escrito antes de que
			// existiera la sangre no midio sangre, que es lo que NoRealizado significa.
			Igual("un journal viejo de dos argumentos sigue reproduciendo", "no realizado",
				new ExamenDeOrina(Hoy, "ambar").Sangre.Expresado);
			Igual("  y uno de tres tambien", "no realizado",
				new ExamenDeOrina(Hoy, "ambar", "negativo").Sangre.Expresado);
		}


		// ---- lo que falta medir: la unica pregunta que mira el hueco ----
		//
		// Todo lo demas de este dominio interpreta lo que HAY. Esto mira lo que NO esta, y
		// es lo unico que una pantalla no puede mostrar: solo renderiza datos que tiene.
		//
		// Reemplaza a la tool `revisar_mis_analitos`, que recorria los analitos y devolvia
		// la tendencia de cada uno — o sea lo que la app ya renderiza. Es el mismo reclamo
		// que Alvaro le hizo al `total` de Jonathan.
		private static void LoQueFaltaMedirEsLoQueNingunaPantallaMuestra()
		{
			Titulo("lo que falta medir");

			// LA PERIODICIDAD NO ES FIJA: el mismo analito contesta distinto segun donde
			// quedo su ultimo valor. Es lo que la hace conocimiento y no configuracion.
			Igual("un colesterol EN RANGO se repite cada 5 años", "60",
				new Colesterol(Hoy, 180m, "mg/dL").MesesHastaElProximo(60).ToString());
			Igual("  el mismo analito FUERA de rango, cada año", "12",
				new Colesterol(Hoy, 280m, "mg/dL").MesesHastaElProximo(60).ToString());
			Igual("una hemoglobina normal, cada año", "12",
				new Hemoglobina(Hoy, 14.2m, "g/dL").MesesHastaElProximo(60).ToString());
			Igual("  con anemia, cada seis meses", "6",
				new Hemoglobina(Hoy, 9.5m, "g/dL").MesesHastaElProximo(60).ToString());

			// El caso completo: un colesterol alto de hace cuatro anos.
			var expediente = new Expediente(60);
			expediente.Agregar(new Colesterol(Hoy.AddDays(-1460), 280m, "mg/dL"));
			expediente.Agregar(new AntigenoProstatico(Hoy.AddDays(-30), 3.0m, "ng/mL"));

			IReadOnlyList<ControlPendiente> pendientes = expediente.ControlesPendientes(Hoy);

			Igual("aparecen los dos analitos con historia", "2", pendientes.Count.ToString());
			Igual("  y lo mas vencido va primero", "Colesterol total", pendientes[0].Analito.Nombre);
			Igual("  el colesterol de hace 4 años esta vencido", "True",
				pendientes[0].EstaVencido.ToString());
			Igual("  el antigeno de hace un mes no", "False", pendientes[1].EstaVencido.ToString());

			// Al DOBLE del intervalo escala: ahi ya no es "se me paso", es que el analito
			// dejo de tener serie util.
			Igual("pasado el doble del intervalo, escala", "consulta medica",
				pendientes[0].NombreDeLaSeveridad);

			var apenasVencido = new Expediente(60);
			apenasVencido.Agregar(new Colesterol(Hoy.AddDays(-400), 280m, "mg/dL"));
			Igual("  apenas vencido, solo seguimiento", "seguimiento",
				apenasVencido.ControlesPendientes(Hoy)[0].NombreDeLaSeveridad);

			// Al dia no pide nada, y lo dice.
			var alDia = new Expediente(60);
			alDia.Agregar(new Colesterol(Hoy.AddDays(-100), 180m, "mg/dL"));
			ControlPendiente sano = alDia.ControlesPendientes(Hoy)[0];
			Igual("al dia no pide nada", "informativo", sano.NombreDeLaSeveridad);
			Igual("  y lo dice sin inventar una accion", "", sano.QueHacer);
			Igual("  la lectura la escribe el dominio", "True",
				sano.ParaSonia.Contains("al día").ToString());

			// De un analito que NUNCA se midio no se dice nada. No hay nada que repetir, y
			// el dominio no inventa una recomendacion sobre algo que nadie pidio.
			Igual("de lo que nunca se midio no se opina", "1",
				alDia.ControlesPendientes(Hoy).Count.ToString());

			// Una medicion fechada DESPUES de la referencia cuenta como de hoy. Pasa de
			// verdad -el guion tiene un examen del dia siguiente- y "hace -1 dias" no es
			// una respuesta.
			var futuro = new Expediente(60);
			futuro.Agregar(new Colesterol(Hoy.AddDays(1), 180m, "mg/dL"));
			Igual("una medicion del futuro no da dias negativos", "0",
				futuro.ControlesPendientes(Hoy)[0].DiasDesdeLaUltima.ToString());

			// Y la respuesta depende de la fecha que se le pase, no del reloj: es lo que
			// mantiene reproducible el dominio.
			Igual("mañana el mismo expediente contesta distinto", "True",
				(alDia.ControlesPendientes(Hoy.AddDays(2000))[0].EstaVencido
					&& !alDia.ControlesPendientes(Hoy)[0].EstaVencido).ToString());
		}

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
