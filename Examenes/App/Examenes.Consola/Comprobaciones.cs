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

		// EL PACIENTE DE LAS COMPROBACIONES QUE YA ESTABAN. Todas se escribieron cuando el
		// dominio no conocia el sexo, y por eso comparaban contra los rangos que hoy son
		// los masculinos: declararlo aca es hacer explicito lo que ya estaba supuesto, no
		// cambiarles el sentido.
		//
		// Las comprobaciones NUEVAS —las de ElSexoEsUnaVariableDelDominio— arman sus
		// propios pacientes, que es donde el sexo importa.
		private static readonly Paciente Hombre60 = new Paciente(60, Sexo.Hombre);
		private static readonly Paciente Mujer60 = new Paciente(60, Sexo.Mujer);
		private static readonly Paciente SinSexo60 = Paciente.DeEdad(60);

		private static Paciente Hombre(int edad) => new Paciente(edad, Sexo.Hombre);

		// Un expediente con el sexo ya declarado. Es la fabrica que usan las comprobaciones
		// que existian antes de este cambio.
		private static Expediente DeHombre(int edad = 60)
		{
			var expediente = new Expediente(edad);
			expediente.DeclararSexo(Sexo.Hombre.Nombre);
			return expediente;
		}

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
			ElAntigenoSeLeeContraLoTuyo();
			ElColesterolSeLeeContraUnaMeta();
			CadaSerieEsSuPropioTipo();
			LoQueNingunaPersonaVivaPodriaTener();
			ComputosPropiosDelAntigeno();
			TodoLoQueSeMideEsUnAnalito();
			UnBinarioTambienTieneSerie();
			NingunNumeroSinSuReferencia();
			TodoConsejoConPlazo();
			ElRojoEstaReservado();
			ElSexoEsUnaVariableDelDominio();

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
			Igual("  y el analito manda a consulta", "consulte pronto",
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
			Igual("PSA 4.0 a los 45", "alto", psa.ClasificarPara(Hombre(45)).Nombre);
			Igual("PSA 4.0 a los 65", "normal", psa.ClasificarPara(Hombre(65)).Nombre);

			var col = new Colesterol(Hoy, 210m, "mg/dL");
			Igual("colesterol 210 a los 30", "normal", col.ClasificarPara(Hombre(30)).Nombre);
			Igual("colesterol 210 a los 60", "alto", col.ClasificarPara(Hombre(60)).Nombre);
		}

		// ---- subir es malo en unos analitos y bajar en otros ----
		private static void LaDireccionPreocupanteEsPorAnalito()
		{
			Titulo("la direccion que preocupa es por analito");

			Interpretacion baja = LeeLaHemoglobina((Hoy.AddDays(-365), 15.0m), (Hoy, 11.0m));
			Igual("hemoglobina 15->11 en 1 ano", "en descenso", baja.NombreDeLaLectura);
			Igual("  el analito la lee como empeorando", "empeorando", baja.NombreDelVeredicto);
			Igual("  y dice cuanta anemia hay", "vigilar", baja.NombreDeLaSeveridad);

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
			Igual("la orina informa un color", "ámbar", orina.ResultadoExpresado);
			Igual("el colesterol si aporta serie", "True",
				new Colesterol(Hoy, 200m, "mg/dL").AportaSerie.ToString());
			Revienta<ArgumentException>("color que el laboratorio no reporta",
				() => new ExamenDeOrina(Hoy, "fucsia"));

			// Una orina VIEJA -sin tira- no midio ni nitritos ni sangre, asi que no crea
			// ninguna serie. Lo que el laboratorio no corrio no es un punto negativo.
			var expediente = DeHombre(60);
			expediente.Agregar(orina);
			Igual("una orina sin tira no crea ninguna serie", "0",
				expediente.CantidadDeAnalitosConSerie.ToString());
			Igual("  pero si entra a la lista", "1", expediente.Cantidad.ToString());
		}

		// ---- con un punto no hay tendencia, y no se inventa ----
		private static void SinSerieNoSeInventaTendencia()
		{
			Titulo("sin serie no se inventa una tendencia");

			var expediente = DeHombre(60);
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

			var expediente = DeHombre(60);
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
			var expediente = DeHombre(60);
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
			var bajando = DeHombre(60);
			bajando.Agregar(new AntigenoProstatico(Hoy.AddDays(-365), 5.2m, "ng/mL"));
			bajando.Agregar(new AntigenoProstatico(Hoy, 3.0m, "ng/mL"));
			Igual("un PSA que BAJA rapido no exige atencion", "False",
				bajando.VelocidadDe(Analito.AntigenoProstatico.Nombre).ExigeAtencion.ToString());

			// El colesterol no se lee por velocidad, y esa es una respuesta del analito.
			var col = DeHombre(60);
			col.Agregar(new Colesterol(Hoy.AddDays(-365), 200m, "mg/dL"));
			col.Agregar(new Colesterol(Hoy, 280m, "mg/dL"));
			Velocidad velColesterol = col.VelocidadDe(Analito.ColesterolTotal.Nombre);
			Igual("el colesterol NO se interpreta por velocidad", "False",
				velColesterol.EsAplicable.ToString());
			Igual("  aunque cambio 80 en un ano", "False", velColesterol.ExigeAtencion.ToString());

			// Una sola medicion no da un ritmo, y preguntarlo es legitimo.
			var unSolo = DeHombre(60);
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
			Igual("dice las dos cosas", "ámbar, nitritos negativo", completo.ResultadoExpresado);
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
			Igual("  y la lista no menciona nitritos", "ámbar", sinTira.ResultadoExpresado);

			Revienta<ArgumentException>("un resultado de nitritos que no existe",
				() => new ExamenDeOrina(Hoy, "ambar", "mas o menos"));

			// EL COLOR no entra a ninguna serie y LOS NITRITOS si, y eso es lo corregido:
			// un urianalisis trae tres cosas medidas y no todas se leen igual. Preguntarle
			// a un color si viene subiendo no es una pregunta; preguntarle a unos nitritos
			// si volvieron a salir positivos, si.
			var expediente = DeHombre(60);
			expediente.Agregar(conHallazgo);
			Igual("la orina crea serie de los nitritos, no del color", "1",
				expediente.CantidadDeAnalitosConSerie.ToString());
			Igual("  y la serie es la de los nitritos", Analito.NitritosEnOrina.Nombre,
				expediente.Analitos[0]);
			Igual("  pero si entra a la lista", "1", expediente.Cantidad.ToString());
		}

		private static EvaluacionDeTendencia SerieDeColesterol(params (DateTime fecha, decimal valor)[] puntos)
		{
			var expediente = DeHombre(60);
			foreach ((DateTime fecha, decimal valor) in puntos)
				expediente.Agregar(new Colesterol(fecha, valor, "mg/dL"));
			return expediente.EvaluarTendenciaDe(Analito.ColesterolTotal.Nombre);
		}

		private static EvaluacionDeTendencia SerieDeAntigeno(params (DateTime fecha, decimal valor)[] puntos)
		{
			var expediente = DeHombre(60);
			foreach ((DateTime fecha, decimal valor) in puntos)
				expediente.Agregar(new AntigenoProstatico(fecha, valor, "ng/mL"));
			return expediente.EvaluarTendenciaDe(Analito.AntigenoProstatico.Nombre);
		}

		// Las mismas series, pero pidiendo la LECTURA del analito en vez de los hechos.
		private static Interpretacion LeeElColesterol(params (DateTime fecha, decimal valor)[] puntos)
		{
			var expediente = DeHombre(60);
			foreach ((DateTime fecha, decimal valor) in puntos)
				expediente.Agregar(new Colesterol(fecha, valor, "mg/dL"));
			return expediente.InterpretarSerieDe(Analito.ColesterolTotal.Nombre);
		}

		private static Interpretacion LeeElAntigeno(params (DateTime fecha, decimal valor)[] puntos)
		{
			var expediente = DeHombre(60);
			foreach ((DateTime fecha, decimal valor) in puntos)
				expediente.Agregar(new AntigenoProstatico(fecha, valor, "ng/mL"));
			return expediente.InterpretarSerieDe(Analito.AntigenoProstatico.Nombre);
		}

		private static Interpretacion LeeLaHemoglobina(params (DateTime fecha, decimal valor)[] puntos)
		{
			var expediente = DeHombre(60);
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
			Expediente expediente = DeHombre(60);
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
			Igual("  ese si va al medico", "consulte pronto", subiendo.NombreDeLaSeveridad);

			// El colesterol sabe A DONDE tiene que llegar. Antes esto comprobaba un
			// NivelHabitual() que era el metodo del antigeno puesto en este analito; la
			// reunion del 2026-08-26 lo corrigio: aca la historia larga no interesa.
			Igual("el colesterol sabe a donde tiene que llegar", "240 mg/dL",
				new Colesterol(Hoy, 280m, "mg/dL").Meta().Objetivo.Expresado);

			// MEDIDO sobre un reporte real: 220 (2022) -> 234 (2026), 14 mg/dL en 1362
			// dias. Parece un ascenso; esta por debajo de la variacion biologica del propio
			// analito. Antes la app decia "en ascenso" y se equivocaba.
			Interpretacion real = LeeElColesterol((Hoy.AddDays(-1362), 220m), (Hoy, 234m));
			Igual("220 -> 234 en 1362 dias no es un ascenso", "estable", real.NombreDelVeredicto);

			// Criterio 4: la accion NO se escribe en la pantalla. Sale del conjunto
			// cerrado, viene CON PLAZO, y dos severidades distintas dan acciones distintas.
			Igual("la accion viene del dominio, con plazo", "mostrále esto a tu médico este mes",
				subiendo.AccionLegible);
			Igual("  y cuando no hay nada que hacer, no se inventa", "",
				bajandoDelPico.AccionLegible);

			// Criterio 5 en su version disponible: dos analitos, dos severidades para el
			// mismo hecho de estar fuera de rango.
			Interpretacion anemia = LeeLaHemoglobina((Hoy.AddDays(-365), 13.5m), (Hoy, 9.5m));
			Igual("una hemoglobina en 9.5 es anemia moderada", "moderada",
				new Hemoglobina(Hoy, 9.5m, "g/dL").GradoDeAnemia(Hombre60).Nombre);
			Igual("  y por eso su severidad NO es la del colesterol", "consulte pronto",
				anemia.NombreDeLaSeveridad);
			Igual("  una de 12.0 baja de grado y de severidad", "vigilar",
				new Hemoglobina(Hoy, 12.0m, "g/dL").GradoDeAnemia(Hombre60).Severidad.Nombre);
			Igual("  y una de 7.5 sube a lo mas urgente", "urgente",
				new Hemoglobina(Hoy, 7.5m, "g/dL").GradoDeAnemia(Hombre60).Severidad.Nombre);

			// Criterio 6: la velocidad existe SOLO en el antigeno. En los demas la consulta
			// sigue siendo total y contesta "no aplica" en vez de devolver un cero que
			// parezca una medicion.
			var soloColesterol = DeHombre(60);
			soloColesterol.Agregar(new Colesterol(Hoy.AddDays(-365), 200m, "mg/dL"));
			soloColesterol.Agregar(new Colesterol(Hoy, 280m, "mg/dL"));
			Igual("el colesterol no tiene velocidad", "False",
				soloColesterol.VelocidadDe(Analito.ColesterolTotal.Nombre).EsAplicable.ToString());

			var soloAntigeno = DeHombre(60);
			soloAntigeno.Agregar(new AntigenoProstatico(Hoy.AddDays(-365), 3.0m, "ng/mL"));
			soloAntigeno.Agregar(new AntigenoProstatico(Hoy, 5.2m, "ng/mL"));
			Igual("  y el antigeno si", "True",
				soloAntigeno.VelocidadDe(Analito.AntigenoProstatico.Nombre).EsAplicable.ToString());

			// Criterio 7: una sola medicion no inventa tendencia. Estaba bien y sigue.
			var unaSola = DeHombre(60);
			unaSola.Agregar(new Hemoglobina(Hoy, 14.2m, "g/dL"));
			Interpretacion sinSerie = unaSola.InterpretarSerieDe(Analito.Hemoglobina.Nombre);
			Igual("una sola medicion sigue siendo indeterminada", "indeterminado",
				sinSerie.NombreDelVeredicto);

			// Pero el NIVEL ya se puede leer aunque no haya serie, y eso es propio de la
			// hemoglobina: una de 9.0 sola no es "no se sabe nada".
			var unaSolaBaja = DeHombre(60);
			unaSolaBaja.Agregar(new Hemoglobina(Hoy, 9.0m, "g/dL"));
			Igual("  pero una sola de 9.0 ya dice algo", "consulte pronto",
				unaSolaBaja.InterpretarSerieDe(Analito.Hemoglobina.Nombre).NombreDeLaSeveridad);

			// Lo mas urgente de todo el expediente, que ninguna pantalla contesta.
			var variosAnalitos = DeHombre(60);
			variosAnalitos.Agregar(new Colesterol(Hoy.AddDays(-365), 190m, "mg/dL"));
			variosAnalitos.Agregar(new Colesterol(Hoy, 280m, "mg/dL"));
			variosAnalitos.Agregar(new Hemoglobina(Hoy.AddDays(-365), 14.0m, "g/dL"));
			variosAnalitos.Agregar(new Hemoglobina(Hoy, 14.2m, "g/dL"));
			Igual("lo mas urgente del expediente entero", "consulte pronto",
				variosAnalitos.SeveridadMayor.Nombre);
			Igual("  y viene con SU accion y SU plazo", "mostrále esto a tu médico este mes",
				variosAnalitos.LoMasUrgente.AccionLegible);
		}


		// ---- el cuadro historico: las fechas son el eje ----
		//
		// Criterio 1 del feedback. La lista plana repetia el nombre del analito en cada
		// fila; el cuadro pone una fila por analito y una columna por fecha, para que la
		// serie se lea de un barrido horizontal.
		private static void ElCuadroPoneLasFechasEnElEje()
		{
			Titulo("el cuadro historico pone las fechas en el eje");

			var expediente = DeHombre(60);
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
			Igual("  y su resultado no es un numero", "ámbar", Celda(conOrina, 3, 3).Expresado);

			// Una remedicion el mismo dia no abre una columna nueva: gana la ultima.
			var mismoDia = DeHombre(60);
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
			Igual("la de orina no espera", "urgente",
				SangreEnOrina.Positivo.SeveridadDelHallazgo.Nombre);
			Igual("  la de heces se revisa", "vigilar",
				SangreEnHeces.Positivo.SeveridadDelHallazgo.Nombre);
			Igual("  o sea que NO dan lo mismo", "True",
				SangreEnOrina.Positivo.SeveridadDelHallazgo
					.EsMasUrgenteQue(SangreEnHeces.Positivo.SeveridadDelHallazgo).ToString());

			// Y las acciones tampoco, porque las elige el analito CON su plazo, no la
			// pantalla ni la severidad sola.
			Igual("la accion de la orina", "buscá atención médica pronto",
				SangreEnOrina.Positivo.AccionDelHallazgo.Legible);
			Igual("  la de heces", "repetí el examen este mes",
				SangreEnHeces.Positivo.AccionDelHallazgo.Legible);

			// TRES magnitudes en el urianalisis, y DOS de ellas son la misma forma sin
			// leerse igual: los nitritos van a consulta, la sangre no espera.
			Igual("en la MISMA tira, los nitritos piden otra cosa", "consulte pronto",
				Nitritos.Positivo.SeveridadDelHallazgo.Nombre);
			Igual("  que la sangre de al lado", "urgente",
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
			var conOrina = DeHombre(60);
			conOrina.Agregar(new Colesterol(Hoy.AddDays(-365), 190m, "mg/dL"));
			conOrina.Agregar(new Colesterol(Hoy, 195m, "mg/dL"));
			conOrina.Agregar(new ExamenDeOrina(Hoy, "ambar", "negativo", "positivo"));
			Igual("lo mas urgente del expediente no tiene serie", "urgente",
				conOrina.SeveridadMayor.Nombre);

			var conHeces = DeHombre(60);
			conHeces.Agregar(new Colesterol(Hoy.AddDays(-365), 190m, "mg/dL"));
			conHeces.Agregar(new Colesterol(Hoy, 195m, "mg/dL"));
			conHeces.Agregar(new ExamenDeHeces(Hoy, "chocolate", "positivo"));
			Igual("  y el mismo positivo en heces no escala igual", "vigilar",
				conHeces.SeveridadMayor.Nombre);

			// El examen dice la lectura; la pantalla no la compone.
			// El color llega como lo escriba el laboratorio y se muestra con acento.
			Igual("marron, MARRÓN y Marrón son el mismo color", "True",
				(ColorDeHeces.PorNombre("marron") == ColorDeHeces.PorNombre("MARRÓN")
					&& ColorDeHeces.PorNombre("Marrón") == ColorDeHeces.PorNombre("marron")).ToString());
			Igual("  y se muestra con acento", "marrón",
				ColorDeHeces.PorNombre("marron").Expresado);
			Igual("  el de la orina tambien", "ámbar",
				ColorDeOrina.PorNombre("ambar").Expresado);

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
				new Colesterol(Hoy, 180m, "mg/dL").MesesHastaElProximo(Hombre(60)).ToString());
			Igual("  el mismo analito FUERA de rango, cada año", "12",
				new Colesterol(Hoy, 280m, "mg/dL").MesesHastaElProximo(Hombre(60)).ToString());
			Igual("una hemoglobina normal, cada año", "12",
				new Hemoglobina(Hoy, 14.2m, "g/dL").MesesHastaElProximo(Hombre(60)).ToString());
			Igual("  con anemia, cada seis meses", "6",
				new Hemoglobina(Hoy, 9.5m, "g/dL").MesesHastaElProximo(Hombre(60)).ToString());

			// El caso completo: un colesterol alto de hace cuatro anos.
			var expediente = DeHombre(60);
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
			Igual("pasado el doble del intervalo, escala", "consulte pronto",
				pendientes[0].NombreDeLaSeveridad);
			Igual("  y su accion trae el plazo", "repetí el examen esta semana",
				pendientes[0].AccionLegible);

			var apenasVencido = DeHombre(60);
			apenasVencido.Agregar(new Colesterol(Hoy.AddDays(-400), 280m, "mg/dL"));
			Igual("  apenas vencido, solo vigilar", "vigilar",
				apenasVencido.ControlesPendientes(Hoy)[0].NombreDeLaSeveridad);
			Igual("  con un plazo mas holgado", "repetí el examen este mes",
				apenasVencido.ControlesPendientes(Hoy)[0].AccionLegible);

			// Al dia no pide nada, y lo dice.
			var alDia = DeHombre(60);
			alDia.Agregar(new Colesterol(Hoy.AddDays(-100), 180m, "mg/dL"));
			ControlPendiente sano = alDia.ControlesPendientes(Hoy)[0];
			Igual("al dia no pide nada", "informativo", sano.NombreDeLaSeveridad);
			Igual("  y lo dice sin inventar una accion", "", sano.AccionLegible);
			Igual("  la lectura la escribe el dominio", "True",
				sano.ParaSonia.Contains("al día").ToString());

			// De un analito que NUNCA se midio no se dice nada. No hay nada que repetir, y
			// el dominio no inventa una recomendacion sobre algo que nadie pidio.
			Igual("de lo que nunca se midio no se opina", "1",
				alDia.ControlesPendientes(Hoy).Count.ToString());

			// Una medicion fechada DESPUES de la referencia cuenta como de hoy. Pasa de
			// verdad -el guion tiene un examen del dia siguiente- y "hace -1 dias" no es
			// una respuesta.
			var futuro = DeHombre(60);
			futuro.Agregar(new Colesterol(Hoy.AddDays(1), 180m, "mg/dL"));
			Igual("una medicion del futuro no da dias negativos", "0",
				futuro.ControlesPendientes(Hoy)[0].DiasDesdeLaUltima.ToString());

			// Y la respuesta depende de la fecha que se le pase, no del reloj: es lo que
			// mantiene reproducible el dominio.
			Igual("mañana el mismo expediente contesta distinto", "True",
				(alDia.ControlesPendientes(Hoy.AddDays(2000))[0].EstaVencido
					&& !alDia.ControlesPendientes(Hoy)[0].EstaVencido).ToString());
		}


		// ---- el antigeno se lee contra LO TUYO, no contra la poblacion ----
		//
		// Reunion del 2026-08-26. Las dos lecturas que este analito exige y que un rango de
		// referencia no puede dar.
		private static void ElAntigenoSeLeeContraLoTuyo()
		{
			Titulo("el antigeno se lee contra lo tuyo");

			// EL PICO QUE NO SE CONFIRMA. Alvaro: "paso de 3 a casi 4, y cuando lo vuelve a
			// medir pasa otra vez a 3. Ahi al medico esa segunda lectura le sirve."
			//
			// ANTES esto daba "probable error de medicion": el dominio leia la vuelta a la
			// normalidad COMO el error, que es la lectura exactamente opuesta.
			Interpretacion pico = LeeElAntigeno(
				(Hoy.AddDays(-730), 3.0m), (Hoy.AddDays(-365), 3.1m),
				(Hoy.AddDays(-7), 3.9m), (Hoy, 3.0m));
			Igual("el pico se repitio y no se confirmo", "pico no confirmado",
				pico.NombreDelVeredicto);
			Igual("  y por lo tanto no pide nada", "informativo", pico.NombreDeLaSeveridad);
			Igual("  se lo dice como buena noticia", "True",
				pico.ParaSonia.Contains("no se confirmó").ToString());

			// La MISMA forma pero con un ano de por medio NO es una confirmacion: es el
			// control siguiente, y ahi los dos valores cuentan.
			Interpretacion controlSiguiente = LeeElAntigeno(
				(Hoy.AddDays(-1095), 3.0m), (Hoy.AddDays(-730), 3.1m),
				(Hoy.AddDays(-365), 3.9m), (Hoy, 3.0m));
			Igual("a un ano ya no es repeticion del examen", "False",
				controlSiguiente.NombreDelVeredicto.Equals("pico no confirmado").ToString());

			// SALIRSE DE LO SUYO. Los dos terminan en 3.0 y estan bajo el techo de su edad
			// (4.5 a los 60): el rango de referencia no los distingue, la linea base si.
			Interpretacion vivioEnDos = LeeElAntigeno(
				(Hoy.AddDays(-1825), 2.0m), (Hoy.AddDays(-1460), 2.1m),
				(Hoy.AddDays(-1095), 1.8m), (Hoy, 3.0m));
			Interpretacion vivioEnTres = LeeElAntigeno(
				(Hoy.AddDays(-1825), 3.0m), (Hoy.AddDays(-1460), 3.1m),
				(Hoy.AddDays(-1095), 2.9m), (Hoy, 3.0m));

			Igual("el que vive en 2.0 y da 3.0 se salio de lo suyo", "empeorando",
				vivioEnDos.NombreDelVeredicto);
			Igual("  y el que SIEMPRE vivio en 3.0, no", "estable",
				vivioEnTres.NombreDelVeredicto);
			Igual("  mismo valor final, misma edad, mismo techo", "True",
				(new AntigenoProstatico(Hoy, 3.0m, "ng/mL").EsNormalPara(Hombre(60))).ToString());
			Igual("  y la frase habla de SU nivel, no del rango", "True",
				vivioEnDos.ParaSonia.Contains("se salió de lo tuyo").ToString());

			// La linea base es la MEDIANA de lo previo, no el promedio: un pico no la
			// arrastra, que es justo lo que se quiere medir contra ella.
			var expediente = DeHombre(60);
			foreach ((int dias, decimal v) in new[] {
				(-1460, 2.0m), (-1095, 2.1m), (-730, 1.9m), (-365, 8.0m), (0, 2.0m) })
				expediente.Agregar(new AntigenoProstatico(Hoy.AddDays(dias), v, "ng/mL"));

			// Previos: 2.0, 2.1, 1.9 y un pico de 8.0. La MEDIANA da 2.05; el promedio
			// habria dado 3.5, arrastrado por el unico valor que no es de esta persona.
			// Ahora se le pregunta A LA SERIE, que es de quien es: el metodo dejo de recibir
			// la serie por parametro porque ES la serie.
			var laSuya = (SerieDeAntigenoProstatico)
				expediente.SerieDe(Analito.AntigenoProstatico.Nombre);
			Igual("un pico no arrastra la linea base", "2.05 ng/mL",
				laSuya.NivelHabitual().Expresado);

			// Sin fondo suficiente no se inventa una linea base.
			var corta = DeHombre(60);
			corta.Agregar(new AntigenoProstatico(Hoy.AddDays(-365), 3.0m, "ng/mL"));
			corta.Agregar(new AntigenoProstatico(Hoy, 3.2m, "ng/mL"));
			Igual("con dos mediciones todavia no hay linea base", "True",
				(((SerieDeAntigenoProstatico)corta.SerieDe(Analito.AntigenoProstatico.Nombre))
					.NivelHabitual() == null).ToString());
		}


		// ---- el colesterol se lee contra una META, no contra su historia ----
		//
		// Es la diferencia entera con el antigeno, y sale de la misma reunion:
		//
		//   antigeno    "usted no quiere perder la historia"
		//   colesterol  "yo no ocupo ver esa historia"
		private static void ElColesterolSeLeeContraUnaMeta()
		{
			Titulo("el colesterol se lee contra una meta");

			// LA META ES EL ESCALON SIGUIENTE, no el techo de lo deseable. A alguien en 280
			// no se le dice "llega a 200": no se ve alcanzable y deja de servir de meta.
			Igual("280 apunta a 240", "240 mg/dL",
				new Colesterol(Hoy, 280m, "mg/dL").Meta().Objetivo.Expresado);
			Igual("  y cuando cruza, la meta se mueve sola", "200 mg/dL",
				new Colesterol(Hoy, 230m, "mg/dL").Meta().Objetivo.Expresado);
			Igual("  en 190 ya llego", "True",
				new Colesterol(Hoy, 190m, "mg/dL").Meta().YaLlego.ToString());
			Igual("  y dice cuanto falta", "40",
				new Colesterol(Hoy, 280m, "mg/dL").Meta().CuantoFalta.ToString());

			// EL PROGRESO se cuenta desde donde arranco el EPISODIO, no desde la primera
			// medicion de la vida de la persona.
			var bajando = DeHombre(60);
			foreach ((int dias, decimal v) in new[] {
				(-1825, 180m), (-1460, 175m), (-730, 320m), (-365, 300m), (0, 280m) })
				bajando.Agregar(new Colesterol(Hoy.AddDays(dias), v, "mg/dL"));

			var laDelColesterol = (SerieDeColesterol)
				bajando.SerieDe(Analito.ColesterolTotal.Nombre);

			Igual("el episodio arranca en el pico, no en la primera medicion", "320 mg/dL",
				laDelColesterol.PartidaDelEpisodio().Expresado);
			Igual("  y el progreso se mide contra eso", "0.5",
				laDelColesterol.TrayectoriaHaciaMeta().ToString());

			// Fuera de rango y BAJANDO sigue siendo buena noticia, y ahora lo dice con la
			// meta en vez de con el nivel habitual.
			Interpretacion mejora = bajando.InterpretarSerieDe(Analito.ColesterolTotal.Nombre);
			Igual("bajando desde el pico", "mejorando", mejora.NombreDelVeredicto);
			Igual("  y le dice a donde va", "True",
				mejora.ParaSonia.Contains("te faltan 40 mg/dL").ToString());

			// LA EDAD MODULA. Mismo valor, misma serie, dos lecturas: "a tu edad esto se
			// corrige" contra "que lo vea tu medico".
			var joven = DeHombre(35);
			joven.Agregar(new Colesterol(Hoy.AddDays(-365), 190m, "mg/dL"));
			joven.Agregar(new Colesterol(Hoy, 280m, "mg/dL"));
			var mayor = DeHombre(60);
			mayor.Agregar(new Colesterol(Hoy.AddDays(-365), 190m, "mg/dL"));
			mayor.Agregar(new Colesterol(Hoy, 280m, "mg/dL"));

			Igual("a los 35, fuera de rango se corrige", "vigilar",
				joven.InterpretarSerieDe(Analito.ColesterolTotal.Nombre).NombreDeLaSeveridad);
			Igual("  a los 60, el mismo valor va al medico", "consulte pronto",
				mayor.InterpretarSerieDe(Analito.ColesterolTotal.Nombre).NombreDeLaSeveridad);

			// Y donde vive ahora el metodo prestado: la linea base es del ANTIGENO, que es
			// el que se lee contra su propia historia.
			var suyaDelAntigeno = DeHombre(60);
			foreach ((int dias, decimal v) in new[] {
				(-1095, 2.0m), (-730, 2.1m), (-365, 1.9m), (0, 2.0m) })
				suyaDelAntigeno.Agregar(new AntigenoProstatico(Hoy.AddDays(dias), v, "ng/mL"));
			Igual("la linea base vive en la serie del antigeno", "2.0 ng/mL",
				((SerieDeAntigenoProstatico)
					suyaDelAntigeno.SerieDe(Analito.AntigenoProstatico.Nombre))
					.NivelHabitual().Expresado);
		}


		// ---- cada serie es su propio tipo ----
		//
		// §4 de la reunion del 2026-08-26. Alvaro: "eso no es lo mismo que el conjunto de
		// examenes de antigeno prostatico evaluando su tendencia. Ahi usted quizas tiene
		// que hacer EL PLURAL antigeno prostatico, examenes plural, y a ese hacerle metodos
		// especificos. […] si usted generaliza y lo pone en examenes, ya perdio la mitad
		// del valor."
		private static void CadaSerieEsSuPropioTipo()
		{
			Titulo("cada serie es su propio tipo");

			var expediente = DeHombre(60);
			expediente.Agregar(new AntigenoProstatico(Hoy.AddDays(-365), 3.0m, "ng/mL"));
			expediente.Agregar(new AntigenoProstatico(Hoy, 5.2m, "ng/mL"));
			expediente.Agregar(new Colesterol(Hoy.AddDays(-365), 200m, "mg/dL"));
			expediente.Agregar(new Colesterol(Hoy, 280m, "mg/dL"));
			expediente.Agregar(new Hemoglobina(Hoy.AddDays(-365), 9.0m, "g/dL"));
			expediente.Agregar(new Hemoglobina(Hoy, 14.0m, "g/dL"));

			// El analito decide cual es SU plural. El expediente no tiene ningun switch.
			Igual("el antigeno arma la suya", "SerieDeAntigenoProstatico",
				expediente.SerieDe(Analito.AntigenoProstatico.Nombre).GetType().Name);
			Igual("  el colesterol la suya", "SerieDeColesterol",
				expediente.SerieDe(Analito.ColesterolTotal.Nombre).GetType().Name);
			Igual("  la hemoglobina la suya", "SerieDeHemoglobina",
				expediente.SerieDe(Analito.Hemoglobina.Nombre).GetType().Name);

			// Y NO COMPARTEN SUPERFICIE. Esto es lo que §4 compra: preguntarle la velocidad
			// a la serie del colesterol no devuelve cero, NO COMPILA.
			var delAntigeno = (SerieDeAntigenoProstatico)
				expediente.SerieDe(Analito.AntigenoProstatico.Nombre);
			var delColesterol = (SerieDeColesterol)
				expediente.SerieDe(Analito.ColesterolTotal.Nombre);
			var deLaHemoglobina = (SerieDeHemoglobina)
				expediente.SerieDe(Analito.Hemoglobina.Nombre);

			Igual("la velocidad es de la serie del antigeno", "True",
				delAntigeno.VelocidadDelTramo().EsAplicable.ToString());
			Igual("  la meta es de la del colesterol", "240 mg/dL",
				delColesterol.Meta().Objetivo.Expresado);
			Igual("  el peor grado de anemia es de la de hemoglobina", "moderada",
				deLaHemoglobina.PeorGrado(Hombre60).Nombre);
			Igual("  y esa serie sabe que se recupero", "True",
				deLaHemoglobina.VieneRecuperandose(Hombre60).ToString());

			// Ninguna de las tres tiene los metodos de las otras. Se comprueba contando la
			// superficie propia, porque lo que NO compila no se puede escribir en un test.
			Igual("la del antigeno no tiene Meta ni PeorGrado", "False",
				(delAntigeno.GetType().GetMethod("Meta") != null
					|| delAntigeno.GetType().GetMethod("PeorGrado") != null).ToString());
			Igual("  la del colesterol no tiene VelocidadDelTramo", "False",
				(delColesterol.GetType().GetMethod("VelocidadDelTramo") != null).ToString());
			Igual("  la de hemoglobina no tiene ninguna de las dos", "False",
				(deLaHemoglobina.GetType().GetMethod("Meta") != null
					|| deLaHemoglobina.GetType().GetMethod("VelocidadDelTramo") != null).ToString());

			// Lo comun sigue siendo comun, y es lo que de verdad vale para todas: los
			// HECHOS de la serie y la unica operacion que todas contestan.
			Igual("las tres saben ordenar y evaluar los hechos", "True",
				(delAntigeno.Evaluar() != null && delColesterol.Evaluar() != null
					&& deLaHemoglobina.Evaluar() != null).ToString());
			Igual("  y las tres se interpretan, cada una a su modo", "True",
				(delAntigeno.Interpretar(Hombre(60)).NombreDelVeredicto
					!= deLaHemoglobina.Interpretar(Hombre(60)).NombreDelVeredicto).ToString());
		}


		// ---- lo que ninguna persona viva podria tener ----
		//
		// §5 de la reunion. Alvaro: "si yo ahi pase de 1 a 10 — no senor, ya se esta
		// muerto. Aqui me estan mandando una vaina de un cadaver. […] el sistema suyo
		// deberia conocer esos: no solo valorar los examenes normales, sino que ese brinco
		// no es racional en este examen."
		//
		// Son DOS guardas distintas y hacen falta las dos.
		private static void LoQueNingunaPersonaVivaPodriaTener()
		{
			Titulo("lo que ninguna persona viva podria tener");

			// UNO: el valor imposible. No es un hallazgo alto, es un dato malformado.
			Revienta<ArgumentOutOfRangeException>("una hemoglobina de 40 no entra",
				() => new Hemoglobina(Hoy, 40m, "g/dL"));
			Revienta<ArgumentOutOfRangeException>("  ni un colesterol de 3",
				() => new Colesterol(Hoy, 3m, "mg/dL"));

			// Y lo que SI tiene que pasar: un hallazgo real, por feo que sea. Apretar la
			// guarda de mas descartaria justamente lo que hay que detectar.
			Igual("una hemoglobina de 6.0 SI entra, es anemia severa", "severa",
				new Hemoglobina(Hoy, 6.0m, "g/dL").GradoDeAnemia(Hombre60).Nombre);
			Igual("  y un antigeno de 80 tambien", "80 ng/mL",
				new AntigenoProstatico(Hoy, 80m, "ng/mL").Valor.Expresado);

			// La ruta lo DESCARTA en vez de reventar: un laboratorio que manda basura no
			// puede impedir que la app se ponga al dia con el resto.
			Igual("la ruta descarta el evento con valor imposible", "descartado",
				AnalitoRuteado(new SenalDeEvento(920, "ExamenAgregado", @"{
					""id"": 920, ""tipo"": ""ExamenAgregado"",
					""examen"": ""Hemoglobina"",
					""fecha"": ""2026-08-21T08:32:00"",
					""valor"": 40, ""unidad"": ""g/dL""
				}")));
			Igual("  y el siguiente evento sigue entrando", "Hemoglobina",
				AnalitoRuteado(new SenalDeEvento(921, "ExamenAgregado", @"{
					""id"": 921, ""tipo"": ""ExamenAgregado"",
					""examen"": ""Hemoglobina"",
					""fecha"": ""2026-08-21T08:32:00"",
					""valor"": 14.2, ""unidad"": ""g/dL""
				}")));

			// DOS: el salto que ningun intervalo justifica. Los dos valores son posibles y
			// aun asi uno de los dos esta mal — y esto la regla de la TASA no lo puede
			// decir, porque con suficientes dias cualquier salto pasa.
			Interpretacion deUnoADiez = LeeElAntigeno(
				(Hoy.AddDays(-3650), 1.0m), (Hoy, 10.0m));
			Igual("de 1 a 10, aunque pasen diez años", "probable error de medicion",
				deUnoADiez.NombreDelVeredicto);

			// La tasa sola lo dejaba pasar: 9 ng/mL en 3650 dias son 0.0025 por dia, muy
			// por debajo del 0.05 que el antigeno admite.
			Igual("  la tasa sola no lo habria visto", "True",
				(9m / 3650m < 0.05m).ToString());

			// Y lo que NO se puede romper: duplicarse es exactamente el caso que este
			// dominio existe para detectar, y tiene que llegar como hallazgo.
			Interpretacion duplicarse = LeeElAntigeno(
				(Hoy.AddDays(-1095), 2.5m), (Hoy.AddDays(-730), 2.6m),
				(Hoy.AddDays(-365), 2.5m), (Hoy, 5.0m));
			Igual("duplicarse SI es hallazgo, no error", "empeorando",
				duplicarse.NombreDelVeredicto);
		}


		// ---- computos propios del antigeno: histograma y comparativo ----
		//
		// §6. Alvaro: "usted a eso le tiene que hacer un histograma, que tiene que hacer
		// tal vez un comparativo con el ano pasado, deme la tendencia". Y que son: "NO SON
		// PRINTS DE LA TABLA, SON COMPUTOS".
		private static void ComputosPropiosDelAntigeno()
		{
			Titulo("computos propios del antigeno");

			var expediente = DeHombre(60);
			// Dos mediciones en 2024 y ningun examen en 2025: los dos casos que separan un
			// comparativo interanual de la lista agrupada.
			foreach ((DateTime f, decimal v) in new[] {
				(new DateTime(2023, 3, 10), 2.0m),
				(new DateTime(2024, 2, 5),  2.4m),
				(new DateTime(2024, 11, 20), 2.2m),
				(new DateTime(2026, 8, 21), 3.0m) })
				expediente.Agregar(new AntigenoProstatico(f, v, "ng/mL"));

			var serie = (SerieDeAntigenoProstatico)
				expediente.SerieDe(Analito.AntigenoProstatico.Nombre);
			IReadOnlyList<MedicionDelAno> porAno = serie.PorAno();

			Igual("cuatro mediciones dan TRES anos", "3", porAno.Count.ToString());
			Igual("  2024 tuvo dos mediciones", "2", porAno[1].Cuantas.ToString());
			Igual("  y se toma la ULTIMA del ano, no el promedio", "2.2 ng/mL",
				porAno[1].Valor.Expresado);
			Igual("el primer ano no tiene con que comparar", "False",
				porAno[0].HayConQueComparar.ToString());
			Igual("  el segundo compara contra el primero", "0.2",
				porAno[1].CambioContraElAnterior.Value.ToString());

			// 2025 no existe y NO se rellena: interpolar un ano que nadie midio es inventar
			// un dato, igual que una celda del cuadro.
			Igual("el ano sin mediciones no aparece", "2026", porAno[2].Ano.ToString());
			Igual("  y compara contra el ultimo ano CON mediciones", "0.8",
				porAno[2].CambioContraElAnterior.Value.ToString());

			// EL HISTOGRAMA: la linea base vista en vez de resumida.
			IReadOnlyList<BandaDelHistograma> bandas = serie.Histograma(4);
			Igual("el histograma devuelve las bandas pedidas", "4", bandas.Count.ToString());

			int total = 0;
			foreach (BandaDelHistograma banda in bandas) total += banda.Cuantas;
			Igual("  y no pierde ni duplica mediciones", "4", total.ToString());

			// Las bandas se arman sobre el rango REAL de la persona -2.0 a 3.0-, no sobre
			// el techo de su edad.
			// Todos los bordes con la misma precision, incluidos los extremos: dentro de un
			// histograma no puede haber dos escalas.
			Igual("arranca en su minimo", "2.00 ng/mL", bandas[0].Desde.Expresado);
			Igual("  y termina en su maximo", "3.00 ng/mL", bandas[3].Hasta.Expresado);
			Igual("  con los bordes pegados, sin huecos", "True",
				(bandas[0].Hasta.Valor == bandas[1].Desde.Valor
					&& bandas[1].Hasta.Valor == bandas[2].Desde.Valor).ToString());
			Igual("  y senala donde cae la ultima", "True", bandas[3].CaeLaUltima.ToString());

			// Sin dispersion no se inventa ancho.
			var planas = DeHombre(60);
			foreach (int dias in new[] { -730, -365, 0 })
				planas.Agregar(new AntigenoProstatico(Hoy.AddDays(dias), 2.0m, "ng/mL"));
			IReadOnlyList<BandaDelHistograma> unaSola =
				((SerieDeAntigenoProstatico)planas.SerieDe(Analito.AntigenoProstatico.Nombre))
					.Histograma(4);
			Igual("tres mediciones iguales dan UNA banda", "1", unaSola.Count.ToString());

			Revienta<ArgumentOutOfRangeException>("un histograma de cero bandas no existe",
				() => serie.Histograma(0));

			// Y lo que §4 garantiza: estos computos son del antigeno y de nadie mas.
			var delColesterol = DeHombre(60);
			delColesterol.Agregar(new Colesterol(Hoy, 200m, "mg/dL"));
			Igual("la serie del colesterol no tiene histograma", "False",
				(delColesterol.SerieDe(Analito.ColesterolTotal.Nombre)
					.GetType().GetMethod("Histograma") != null).ToString());
		}


		// ---- todo lo que se mide es un analito ----
		//
		// §7. Lo levanto Renan preguntando: "el aspecto y la presencia de sangre no son
		// analitos distintos?". Lo eran por la definicion del propio dominio -"Analito: QUE
		// se midio"- y no podian serlo porque Analito EXIGIA una UnidadDeMedida. O sea que
		// era "analito cuantitativo de sangre" con un nombre general puesto encima.
		private static void TodoLoQueSeMideEsUnAnalito()
		{
			Titulo("todo lo que se mide es un analito");

			// LA UNIDAD BAJO A LA FORMA CONTINUA, que es de quien es.
			Igual("un analito de sangre es continuo y tiene unidad", "ng/mL",
				Analito.AntigenoProstatico.Unidad.Simbolo);
			Igual("  un color es un analito y NO tiene unidad", "True",
				(Analito.ColorDeLaOrina is AnalitoNominal).ToString());
			Igual("  y un binario tampoco", "True",
				(Analito.SangreEnLaOrina is AnalitoBinario).ToString());
			Igual("los tres son analitos igual", "True",
				(Analito.ColorDeLaOrina is Analito && Analito.SangreEnLaOrina is Analito
					&& Analito.AntigenoProstatico is Analito).ToString());

			// Y resuelven por nombre como cualquier otro, con sus sinonimos.
			Igual("el color de la orina resuelve por nombre", "True",
				Analito.PorNombre("color de orina").EsElMismo(Analito.ColorDeLaOrina).ToString());
			Igual("  la sangre en orina por su sinonimo clinico", "True",
				Analito.PorNombre("hematuria").EsElMismo(Analito.SangreEnLaOrina).ToString());
			Igual("  y sigue siendo distinta de la de heces", "False",
				Analito.SangreEnLaOrina.EsElMismo(Analito.SangreEnLasHeces).ToString());

			// EL SINTOMA QUE ESTO ARREGLA. Un urianalisis mide TRES cosas y antes las tres
			// caian en una fila, aplanadas en un string:
			//     "ambar, nitritos negativo, sangre negativo"
			var expediente = DeHombre(60);
			expediente.Agregar(new ExamenDeOrina(Hoy, "ambar", "negativo", "positivo"));

			IReadOnlyList<CeldaDelCuadro> celdas = expediente.CeldasDelCuadro;
			Igual("un urianalisis da TRES filas, no una", "3", celdas.Count.ToString());

			var nombres = new List<string>();
			foreach (CeldaDelCuadro celda in celdas) nombres.Add(celda.ParaMostrar);
			nombres.Sort(StringComparer.Ordinal);
			Igual("  y cada cosa medida tiene la suya", "Color de la orina|Nitritos|Sangre en orina",
				string.Join("|", nombres));

			// Una prueba NO REALIZADA no es una lectura vacia: es una lectura que no existe.
			var sinNitritos = DeHombre(60);
			sinNitritos.Agregar(new ExamenDeOrina(Hoy, "ambar"));
			Igual("lo que el laboratorio no corrio no aparece", "1",
				sinNitritos.CeldasDelCuadro.Count.ToString());

			// Y el colesterol fraccionado, que es el unico examen de SANGRE que mide dos
			// cosas: el HDL desaparecia heredando la lectura singular.
			var fraccionado = DeHombre(60);
			fraccionado.Agregar(new ColesterolDetallado(Hoy, 150m, 38m, "mg/dL"));
			Igual("el fraccionado da dos filas: LDL y HDL", "2",
				fraccionado.CeldasDelCuadro.Count.ToString());

			// La ruta ahora puede descartar un evento de sangre que nombre un analito que
			// no es continuo. Esa rama no podia existir antes.
			Igual("un evento de sangre con un analito nominal se descarta", "descartado",
				AnalitoRuteado(new SenalDeEvento(930, "ExamenAgregado", @"{
					""id"": 930, ""tipo"": ""ExamenAgregado"",
					""examen"": ""Color de la orina"",
					""fecha"": ""2026-08-21T08:33:00"",
					""valor"": 3.0, ""unidad"": ""ng/mL""
				}")));
		}

		// ---- un binario tambien tiene serie ----
		//
		// La otra mitad de §7. `SerieDeAnalito` estaba tipada a `ExamenDeSangre`, asi que
		// una serie de nitritos -negativo, negativo, positivo- no se podia tener, aunque
		// §6.5 del brief la anticipa desde el principio.
		//
		// LO QUE LA SERIE APORTA, que es de lo unico que se trata: una magnitud binaria
		// solo se ve a si misma, y por eso contesta lo mismo siempre. La serie mira la
		// historia, y con la historia el MISMO positivo se lee distinto.
		private static void UnBinarioTambienTieneSerie()
		{
			Titulo("un binario tambien tiene serie");

			// LA FORMA SE ORDENA O NO SE ORDENA, y lo dice el analito. Es la tabla de
			// §6.5 del brief: EvaluarTendencia vale en todas menos en la nominal.
			Igual("un binario forma serie", "True", Analito.NitritosEnOrina.FormaSerie.ToString());
			Igual("  un continuo tambien", "True", Analito.Hemoglobina.FormaSerie.ToString());
			Igual("  un color NO", "False", Analito.ColorDeLaOrina.FormaSerie.ToString());

			// EL CASO DEL BRIEF, textual: "negativo -> negativo -> 2+ es un empeoramiento
			// perfectamente interpretable, y exige la serie completa igual que el PSA".
			Expediente nitritos = ConOrinas(("negativo", -730), ("negativo", -365), ("positivo", 0));
			Interpretacion lectura = nitritos.InterpretarSerieDe(Analito.NitritosEnOrina.Nombre);
			Igual("negativo, negativo, positivo es una serie", "apareció", lectura.NombreDeLaLectura);
			Igual("  y el analito la lee como empeorando", "empeorando", lectura.NombreDelVeredicto);
			Igual("  con la severidad de SU hallazgo", "consulte pronto", lectura.NombreDeLaSeveridad);

			// EL CORAZON DE TODO ESTO. El mismo positivo, leido con y sin historia.
			//
			// Una sangre oculta en heces positiva UNA vez dice "conviene repetir el
			// examen": la causa mas frecuente es local y banal. DOS VECES SEGUIDAS ya no,
			// y esa distincion no la puede hacer ninguna medicion sola.
			Expediente unaVez = ConHeces(("negativo", -365), ("positivo", 0));
			Expediente dosVeces = ConHeces(("negativo", -730), ("positivo", -365), ("positivo", 0));

			Igual("una sangre en heces positiva: repetir", "vigilar",
				unaVez.InterpretarSerieDe(Analito.SangreEnLasHeces.Nombre).NombreDeLaSeveridad);
			Igual("  DOS SEGUIDAS: ya no alcanza con repetir", "consulte pronto",
				dosVeces.InterpretarSerieDe(Analito.SangreEnLasHeces.Nombre).NombreDeLaSeveridad);
			Igual("  y la magnitud sola sigue diciendo lo mismo en los dos casos", "vigilar",
				SangreEnHeces.Positivo.SeveridadDelHallazgo.Nombre);

			// LA SIMETRICA, que es la buena noticia: §2 de la reunion en version binaria.
			// Sin serie, una sangre en la orina que aparecio una vez se lee para siempre
			// como "buscá atención médica pronto".
			Expediente pasajera = ConOrinasConSangre(("positivo", -365), ("negativo", 0));
			Interpretacion descargo = pasajera.InterpretarSerieDe(Analito.SangreEnLaOrina.Nombre);
			Igual("un positivo que no se repite no queda confirmado", "hallazgo no confirmado",
				descargo.NombreDelVeredicto);
			Igual("  y deja de pedir algo", "False", descargo.PideAlgo.ToString());
			Igual("  cuando el mismo positivo solo pedia lo maximo", "urgente",
				SangreEnOrina.Positivo.SeveridadDelHallazgo.Nombre);

			// Y LA SEVERIDAD DEL EXPEDIENTE SE ENTERA. Antes lo mas urgente salia de sumar
			// la severidad de cada EXAMEN, o sea del positivo suelto, y el descargo de la
			// serie quedaba tapado: el expediente seguia gritando para siempre.
			Igual("el descargo baja lo mas urgente del expediente", "informativo",
				pasajera.SeveridadMayor.Nombre);

			// Y la celda del color ya no arrastra la severidad de los binarios que van al
			// lado. Un ámbar es informativo aunque en la misma tira haya un positivo.
			Expediente conColor = ConOrinasConSangre(("positivo", 0));
			foreach (CeldaDelCuadro celda in conColor.CeldasDelCuadro)
				if (celda.Analito == Analito.ColorDeLaOrina.Nombre)
					Igual("el color lleva SU lectura, no la del examen", "informativo",
						celda.Severidad);

			// CEDIO NO ES LO MISMO QUE NO CONFIRMADO. Ahi nunca se confirmo nada; aca
			// estuvo de verdad -dos veces- y despues se apago.
			Expediente cedio = ConOrinas(("positivo", -730), ("positivo", -365), ("negativo", 0));
			Igual("dos positivos que se apagan cedieron", "mejorando",
				cedio.InterpretarSerieDe(Analito.NitritosEnOrina.Nombre).NombreDelVeredicto);

			// SEGUIDAS AL FINAL, no en total. Negativo, positivo, negativo, positivo son
			// dos positivos y NO son una persistencia: nunca se repitio.
			Expediente alternado = ConHeces(("negativo", -1095), ("positivo", -730),
				("negativo", -365), ("positivo", 0));
			Igual("dos positivos alternados no son persistencia", "vigilar",
				alternado.InterpretarSerieDe(Analito.SangreEnLasHeces.Nombre).NombreDeLaSeveridad);

			// CON UNA SOLA PRUEBA no hay historia, y sin embargo el hallazgo esta. Las dos
			// cosas son ciertas a la vez, y por eso el veredicto queda Indeterminado
			// mientras la severidad la pone la magnitud.
			Expediente sola = ConHeces(("positivo", 0));
			Interpretacion unica = sola.InterpretarSerieDe(Analito.SangreEnLasHeces.Nombre);
			Igual("una sola prueba no da veredicto", "indeterminado", unica.NombreDelVeredicto);
			Igual("  pero el hallazgo sigue estando", "vigilar", unica.NombreDeLaSeveridad);

			// LO QUE NO SE LE PUEDE PREGUNTAR A UN BINARIO. La consulta es total, y la
			// respuesta honesta no es "indeterminada" -eso significaria que le faltan
			// examenes- sino que la pregunta no le corresponde.
			EvaluacionDeTendencia sinTendencia =
				dosVeces.EvaluarTendenciaDe(Analito.SangreEnLasHeces.Nombre);
			Igual("un binario no tiene tendencia", "no aplica", sinTendencia.NombreDeLaTendencia);
			Igual("  y no es que le falten mediciones", "3",
				sinTendencia.CantidadDeMediciones.ToString());
			Igual("  tampoco tiene velocidad", "False",
				dosVeces.VelocidadDe(Analito.SangreEnLasHeces.Nombre).EsAplicable.ToString());
			Igual("  ni lineas de referencia que dibujar", "0",
				dosVeces.ReferenciasDe(Analito.SangreEnLasHeces.Nombre).Count.ToString());
			Igual("  y su forma es la suya", "presencia en el tiempo",
				dosVeces.FormaDeLecturaDe(Analito.SangreEnLasHeces.Nombre));

			// QUE FALTA MEDIR ahora los cubre. Era el limite anotado en §7 bis: la
			// consulta que mira el hueco solo llegaba a los analitos de sangre.
			var pendientes = new List<string>();
			foreach (ControlPendiente control in dosVeces.ControlesPendientes(Hoy))
				pendientes.Add(control.Nombre);
			Igual("la sangre en heces ya entra en lo que falta medir", "True",
				pendientes.Contains(Analito.SangreEnLasHeces.Nombre).ToString());

			// Y la periodicidad la resuelve el analito contra su ultimo resultado: una
			// sangre oculta positiva no espera al ano que viene.
			Igual("positiva se repite a los 3 meses", "3", MesesDe(dosVeces).ToString());
			Igual("  y negativa, al ano", "12",
				MesesDe(ConHeces(("negativo", -365), ("negativo", 0))).ToString());

			// EL COLOR SIGUE SIN SERIE, y eso no es un pendiente: preguntarle a un color
			// si viene subiendo no es una pregunta.
			Igual("el color no crea serie", "1",
				ConHeces(("negativo", -365), ("negativo", 0)).CantidadDeAnalitosConSerie.ToString());

			// Una prueba que el laboratorio no corrio no es un punto negativo.
			Revienta<ArgumentException>("una prueba no realizada no es un punto",
				() => new PruebaBinaria(Hoy, Nitritos.NoRealizado));
		}

		private static int MesesDe(Expediente expediente)
		{
			foreach (ControlPendiente control in expediente.ControlesPendientes(Hoy))
				if (control.Nombre == Analito.SangreEnLasHeces.Nombre) return control.MesesRecomendados;
			return 0;
		}

		// Tres armadores, y son tres porque los examenes son tres muestras distintas. El
		// dia se corre para que la serie tenga fechas separadas, que es lo que la ordena.
		private static Expediente ConOrinas(params (string nitritos, int dias)[] pruebas)
		{
			var expediente = DeHombre(60);
			foreach ((string nitritos, int dias) in pruebas)
				expediente.Agregar(new ExamenDeOrina(Hoy.AddDays(dias), "ambar", nitritos));
			return expediente;
		}

		private static Expediente ConOrinasConSangre(params (string sangre, int dias)[] pruebas)
		{
			var expediente = DeHombre(60);
			foreach ((string sangre, int dias) in pruebas)
				expediente.Agregar(new ExamenDeOrina(Hoy.AddDays(dias), "ambar", null, sangre));
			return expediente;
		}

		private static Expediente ConHeces(params (string sangre, int dias)[] pruebas)
		{
			var expediente = DeHombre(60);
			foreach ((string sangre, int dias) in pruebas)
				expediente.Agregar(new ExamenDeHeces(Hoy.AddDays(dias), "marrón", sangre));
			return expediente;
		}

		// ---- ningun numero sin su referencia ----
		//
		// Criterio 7 de la adopcion de marca Delfos: la tarjeta mostraba "5.2 ng/mL" y
		// nada mas, y Sonia no tenia forma de saber si 5.2 es mucho. La referencia la
		// dice el ANALITO —depende de la edad, y en la hemoglobina tambien tiene piso— y
		// viaja con la interpretacion para que la UI no tenga que escribirla.
		private static void NingunNumeroSinSuReferencia()
		{
			Titulo("ningun numero sin su referencia");

			Interpretacion psa = LeeElAntigeno((Hoy.AddDays(-365), 3.0m), (Hoy, 5.2m));
			Igual("el antigeno dice su techo por edad", "hasta 4.5 ng/mL", psa.ReferenciaLegible);

			Interpretacion hemoglobina = LeeLaHemoglobina((Hoy.AddDays(-365), 15.0m), (Hoy, 14.2m));
			Igual("  la hemoglobina es una banda, no un techo", "entre 12.5 g/dL y 16.5 g/dL",
				hemoglobina.ReferenciaLegible);

			Interpretacion colesterol = LeeElColesterol((Hoy.AddDays(-365), 190m), (Hoy, 280m));
			Igual("  el colesterol dice lo deseable a esa edad", "hasta 200 mg/dL",
				colesterol.ReferenciaLegible);

			// La referencia depende de la EDAD: el mismo antigeno con otro expediente.
			var joven = DeHombre(45);
			joven.Agregar(new AntigenoProstatico(Hoy.AddDays(-365), 3.0m, "ng/mL"));
			joven.Agregar(new AntigenoProstatico(Hoy, 3.1m, "ng/mL"));
			Igual("la referencia depende de la edad", "hasta 2.5 ng/mL",
				joven.InterpretarSerieDe(Analito.AntigenoProstatico.Nombre).ReferenciaLegible);

			// Un binario no tiene rango: su referencia es la polaridad.
			Expediente conNitritos = ConOrinas(("negativo", -365), ("positivo", 0));
			Igual("un binario dice su polaridad", "lo esperable es negativo",
				conNitritos.InterpretarSerieDe(Analito.NitritosEnOrina.Nombre).ReferenciaLegible);

			// Sin mediciones no hay valor que acompanar, y no se inventa una referencia.
			var vacio = DeHombre(60);
			Igual("sin mediciones no hay referencia", "",
				vacio.InterpretarSerieDe(Analito.Hemoglobina.Nombre).ReferenciaLegible);
		}

		// ---- todo consejo con plazo ----
		//
		// Criterio 8. "Conviene que lo vea tu medico" sin un cuando era el mismo texto
		// para el PSA y para el colesterol. El plazo es un dato del dominio: depende del
		// analito, de la magnitud del cambio y de la severidad — y la guarda del
		// constructor lo hace ley, no disciplina.
		private static void TodoConsejoConPlazo()
		{
			Titulo("todo consejo con plazo");

			// La tabla del documento de adopcion, fila por fila.
			Interpretacion psa = LeeElAntigeno(
				(Hoy.AddDays(-1096), 3.0m), (Hoy.AddDays(-731), 3.1m),
				(Hoy.AddDays(-365), 3.0m), (Hoy, 5.2m));
			Igual("PSA 3 -> 5.2 en 3 anos: al medico este mes",
				"mostrále esto a tu médico este mes", psa.AccionLegible);

			Interpretacion colesterolJoven = LeeElColesterolA(35,
				(Hoy.AddDays(-365), 190m), (Hoy, 280m));
			Igual("colesterol 280 subiendo, joven: en la proxima cita",
				"conversalo con tu médico en la próxima cita", colesterolJoven.AccionLegible);

			Interpretacion bajando = LeeElColesterol(
				(Hoy.AddDays(-730), 180m), (Hoy.AddDays(-365), 320m), (Hoy, 280m));
			Igual("colesterol 280 bajando: ninguna accion", "", bajando.AccionLegible);

			Interpretacion unaSola = LeeLaHemoglobina((Hoy, 14.2m));
			Igual("hemoglobina de una medicion: repetir cuando el medico lo indique",
				"repetí el examen cuando el médico lo indique", unaSola.AccionLegible);

			Expediente conSangre = ConOrinasConSangre(("negativo", -365), ("positivo", 0));
			Igual("sangre en orina: atencion pronto", "buscá atención médica pronto",
				conSangre.InterpretarSerieDe(Analito.SangreEnLaOrina.Nombre).AccionLegible);

			// DOS ANALITOS CON LA MISMA SEVERIDAD Y DISTINTO PLAZO: es lo que demuestra
			// que el plazo es del analito y no de la severidad. Los dos ameritan cita;
			// la infeccion urinaria no espera un mes.
			Expediente conNitritos = ConOrinas(("negativo", -365), ("positivo", 0));
			Interpretacion nitritos = conNitritos.InterpretarSerieDe(Analito.NitritosEnOrina.Nombre);
			Igual("nitritos y PSA piden lo mismo", "consulte pronto", nitritos.NombreDeLaSeveridad);
			Igual("  con el mismo pedido que el PSA", "consulte pronto", psa.NombreDeLaSeveridad);
			Igual("  y NO con el mismo plazo", "mostrále esto a tu médico esta semana",
				nitritos.AccionLegible);

			// La guarda: una severidad que pide algo no puede viajar sin accion.
			Revienta<ArgumentNullException>("un consejo sin plazo no se puede construir",
				() => new Interpretacion(Veredicto.Empeorando, Severidad.Vigilar,
					"sube", new HechosDePrueba()));
		}

		// Hechos minimos para comprobar la guarda del constructor sin armar una serie.
		private sealed class HechosDePrueba : HechosDeLaSerie
		{
			internal HechosDePrueba() : base(Examenes.Dominio.Analito.ColesterolTotal, 2) { }
			public override string NombreDeLaLectura => "en ascenso";
			public override bool SugiereRepetirElExamen => false;
		}

		// ---- el rojo esta reservado ----
		//
		// Criterios 5, 6 y 10. "Solo para lo que de verdad es urgente. Si se usa para el
		// colesterol, deja de significar algo." La app pintaba el PSA y el colesterol con
		// el mismo tono y el mismo texto; el color lo decide el dominio, y el dominio no
		// produce Urgente para ningun continuo del demo.
		private static void ElRojoEstaReservado()
		{
			Titulo("el rojo esta reservado");

			// Criterio 5: dos analitos con la MISMA direccion de cambio, severidades
			// distintas. PSA subiendo vs colesterol subiendo, verificable.
			Interpretacion psaSubiendo = LeeElAntigeno(
				(Hoy.AddDays(-365), 3.0m), (Hoy, 5.2m));
			Interpretacion colesterolSubiendo = LeeElColesterolA(35,
				(Hoy.AddDays(-365), 190m), (Hoy, 280m));
			Igual("PSA subiendo pide consulta", "consulte pronto", psaSubiendo.NombreDeLaSeveridad);
			Igual("  el colesterol subiendo, solo vigilar", "vigilar",
				colesterolSubiendo.NombreDeLaSeveridad);

			// Criterio 6: el colesterol no llega a Urgente en NINGUN escenario del guion.
			// Se recorren los que el guion produce: subiendo, estable alto, bajando, salto.
			bool algunoUrgente =
				LeeElColesterol((Hoy.AddDays(-365), 190m), (Hoy, 280m)).NombreDeLaSeveridad == "urgente"
				|| LeeElColesterol((Hoy.AddDays(-730), 280m), (Hoy, 281m)).NombreDeLaSeveridad == "urgente"
				|| LeeElColesterol((Hoy.AddDays(-730), 320m), (Hoy, 280m)).NombreDeLaSeveridad == "urgente"
				|| LeeElColesterol((Hoy.AddDays(-1), 200m), (Hoy, 280m)).NombreDeLaSeveridad == "urgente";
			Igual("el colesterol nunca es urgente", "False", algunoUrgente.ToString());

			// Lo que SI es urgente: la sangre en la orina. El rojo tiene un dueno.
			Expediente conSangre = ConOrinasConSangre(("positivo", -30), ("positivo", 0));
			Igual("  la sangre en orina persistente si lo es", "urgente",
				conSangre.InterpretarSerieDe(Analito.SangreEnLaOrina.Nombre).NombreDeLaSeveridad);

			// Criterio 10: fuera de rango con tendencia favorable NO se pinta como alarma.
			Interpretacion favorable = LeeElColesterol(
				(Hoy.AddDays(-730), 180m), (Hoy.AddDays(-365), 320m), (Hoy, 280m));
			Igual("fuera de rango y mejorando no alarma", "False", favorable.PideAlgo.ToString());
		}

		private static Interpretacion LeeElColesterolA(int edad,
			params (DateTime fecha, decimal valor)[] puntos)
		{
			var expediente = new Expediente(edad);
			foreach ((DateTime fecha, decimal valor) in puntos)
				expediente.Agregar(new Colesterol(fecha, valor, "mg/dL"));
			return expediente.InterpretarSerieDe(Analito.ColesterolTotal.Nombre);
		}


		// ---- el sexo es una variable del dominio, no un filtro de la pantalla ----
		//
		// Alvaro: "El App de laboratorio debe ser diferente segun el sexo. Para empezar
		// solo decir antigeno prostatico cuando Sonia es ELLA y no el, ya no tiene sentido.
		// Y el dominio de Renan DEBE depender ahi de la variable sexo."
		//
		// Lo que estas comprobaciones defienden, y que un filtro de vista NO daria: que el
		// dominio conteste "no corresponde" SIN devolver un valor, aunque le pregunten
		// directamente. Si el antigeno sigue saliendo por alguna puerta —la interpretacion,
		// los puntos, el historial, el titular— el dominio no cambio.
		private static void ElSexoEsUnaVariableDelDominio()
		{
			Titulo("el sexo es una variable del dominio");

			// TRES EFECTOS DISTINTOS, y separarlos es el punto entero (§2 de la spec).
			Igual("el antigeno exige prostata", "prostata",
				Analito.AntigenoProstatico.Requisito.Nombre);
			Igual("  la hemoglobina no exige nada", "ninguno",
				Analito.Hemoglobina.Requisito.Nombre);
			Igual("  y sin embargo su rango si depende del sexo", "True",
				(new Hemoglobina(Hoy, 14m, "g/dL").RangoPara(Hombre60).Minimo.Valor
					!= new Hemoglobina(Hoy, 14m, "g/dL").RangoPara(Mujer60).Minimo.Valor).ToString());

			// EL PREDICADO NO ES EL SEXO, ES LA ANATOMIA (§3.3).
			Igual("un hombre tiene prostata", "aplica", Hombre60.TieneProstata.Nombre);
			Igual("  una mujer no", "no aplica", Mujer60.TieneProstata.Nombre);
			Igual("  y sin el dato no es 'no', es indeterminado", "indeterminada",
				SinSexo60.TieneProstata.Nombre);

			// ---- criterio 1: con MUJER el antigeno no aparece EN NINGUNA PARTE ----
			Expediente deElla = ConLaHistoria(Sexo.Mujer);

			Igual("con mujer el antigeno no esta entre los analitos", "False",
				Nombra(deElla.Analitos, Analito.AntigenoProstatico.Nombre).ToString());
			Igual("  ni tiene serie que consultar", "False",
				deElla.TieneSerieDe(Analito.AntigenoProstatico.Nombre).ToString());
			Igual("  ni sale en el historial", "False",
				EstaEnElCuadro(deElla, Analito.AntigenoProstatico.Nombre).ToString());
			Igual("  ni en lo que falta medir", "False",
				EstaEnLosControles(deElla, Analito.AntigenoProstatico.Nombre).ToString());

			// LA QUE DISTINGUE ESTO DE UN FILTRO DE UI (criterio 7): se le pregunta
			// DIRECTO al dominio, como hace la tool MCP, y NO devuelve un valor.
			Interpretacion suyo = deElla.InterpretarSerieDe(Analito.AntigenoProstatico.Nombre);
			Igual("preguntado directo, contesta que no aplica", "no aplica",
				suyo.NombreDelVeredicto);
			Igual("  y no devuelve ningun valor", "0",
				deElla.PuntosDe(Analito.AntigenoProstatico.Nombre).Count.ToString());
			Igual("  ni su velocidad", "False",
				deElla.VelocidadDe(Analito.AntigenoProstatico.Nombre).EsAplicable.ToString());
			Igual("  ni una tendencia que parezca 'todavia no hay datos'", "no aplica",
				deElla.EvaluarTendenciaDe(Analito.AntigenoProstatico.Nombre).NombreDeLaTendencia);
			Igual("  y no es un hallazgo: es una pregunta sin sentido", "informativo",
				suyo.NombreDeLaSeveridad);

			// Y NO SE COLA EN EL TITULAR. Con hombre ese mismo antigeno pide consulta.
			Expediente deEl = ConLaHistoria(Sexo.Hombre);
			Igual("con hombre el antigeno pide consulta", "consulte pronto",
				deEl.InterpretarSerieDe(Analito.AntigenoProstatico.Nombre).NombreDeLaSeveridad);
			// Y NO SE COLA EN EL TITULAR. Con un expediente que SOLO tiene el antigeno, la
			// diferencia se ve limpia: el mismo journal, dos titulares distintos.
			var soloAntigenoDeEl = new Expediente(60);
			soloAntigenoDeEl.DeclararSexo("hombre");
			soloAntigenoDeEl.Agregar(new AntigenoProstatico(Hoy.AddDays(-365), 3.0m, "ng/mL"));
			soloAntigenoDeEl.Agregar(new AntigenoProstatico(Hoy, 5.2m, "ng/mL"));

			var soloAntigenoDeElla = new Expediente(60);
			soloAntigenoDeElla.DeclararSexo("mujer");
			soloAntigenoDeElla.Agregar(new AntigenoProstatico(Hoy.AddDays(-365), 3.0m, "ng/mL"));
			soloAntigenoDeElla.Agregar(new AntigenoProstatico(Hoy, 5.2m, "ng/mL"));

			Igual("  y el titular lo nombra", "mostrále esto a tu médico este mes",
				soloAntigenoDeEl.LoMasUrgente.AccionLegible);
			Igual("  con mujer, el mismo journal no pide nada", "",
				soloAntigenoDeElla.LoMasUrgente.AccionLegible);

			// ---- criterio 2: con DESCONOCIDO hay valor y NO hay veredicto ----
			Expediente sinSaber = ConLaHistoria(Sexo.Desconocido);

			Igual("sin sexo el antigeno sigue estando", "True",
				Nombra(sinSaber.Analitos, Analito.AntigenoProstatico.Nombre).ToString());
			Igual("  pero sin veredicto", "requiere sexo",
				sinSaber.InterpretarSerieDe(Analito.AntigenoProstatico.Nombre).NombreDelVeredicto);
			Igual("  y su valor SI se muestra", "4",
				sinSaber.PuntosDe(Analito.AntigenoProstatico.Nombre).Count.ToString());
			Igual("la hemoglobina tampoco se puede leer", "requiere sexo",
				sinSaber.InterpretarSerieDe(Analito.Hemoglobina.Nombre).NombreDelVeredicto);
			Igual("  y su referencia lo dice en vez de inventar un rango", "depende de tu sexo",
				sinSaber.InterpretarSerieDe(Analito.Hemoglobina.Nombre).ReferenciaLegible);
			Igual("  sin banda que dibujar", "0",
				sinSaber.ReferenciasDe(Analito.Hemoglobina.Nombre).Count.ToString());

			// UN VALOR QUE NO SE PUEDE JUZGAR NO ES UN HALLAZGO. Contarlo como fuera de
			// rango seria inventar exactamente lo que falta saber. Se comprueba sumando una
			// hemoglobina claramente baja y viendo que el conteo NO se mueve.
			Igual("sin sexo, una hemoglobina de 9.0 no suma un hallazgo", "True",
				(ConUnaHemoglobinaDe(Sexo.Desconocido, 9.0m)
					== ConUnaHemoglobinaDe(Sexo.Desconocido, 14.2m)).ToString());
			Igual("  y con el sexo sabido, si", "True",
				(ConUnaHemoglobinaDe(Sexo.Hombre, 9.0m)
					> ConUnaHemoglobinaDe(Sexo.Hombre, 14.2m)).ToString());
			Igual("  el antigeno tampoco cuenta mientras no se sepa si corresponde", "True",
				(ConUnaHemoglobinaDe(Sexo.Hombre, 14.2m)
					> ConUnaHemoglobinaDe(Sexo.Desconocido, 14.2m)).ToString());

			// ---- criterio 6: la regla NO es plana ----
			Igual("el colesterol total se lee igual sin sexo", "estable",
				sinSaber.InterpretarSerieDe(Analito.ColesterolTotal.Nombre).NombreDelVeredicto);
			Igual("  el LDL tambien", "True",
				sinSaber.InterpretarSerieDe(Analito.ColesterolLdl.Nombre).HayLectura.ToString());
			Igual("  y la sangre en heces tampoco pide el sexo", "False",
				sinSaber.InterpretarSerieDe(Analito.SangreEnLasHeces.Nombre).RequiereElSexo.ToString());
			Igual("solo DOS de los siete quedan sin leer", "2",
				sinSaber.AnalitosNoInterpretables.ToString());

			// PERO EL HDL, QUE VIAJA ADENTRO DEL LDL, SI depende del sexo: lo que se calla
			// es su coletilla, no la lectura del LDL.
			var conHdl = new ColesterolDetallado(Hoy, 150m, 45m, "mg/dL");
			Igual("un HDL de 45 protege a un hombre", "True",
				conHdl.HdlEsProtectorPara(Hombre60).ToString());
			Igual("  y a una mujer no le alcanza", "False",
				conHdl.HdlEsProtectorPara(Mujer60).ToString());
			Igual("  sin sexo no se puede decir ninguna de las dos", "False",
				conHdl.SeSabeSiElHdlAcompana(SinSexo60).ToString());
			Igual("  y por eso la lectura del LDL no lo nombra", "False",
				sinSaber.InterpretarSerieDe(Analito.ColesterolLdl.Nombre).ParaSonia
					.Contains("HDL").ToString());

			// ---- criterio 3: el bloque que insiste cuantifica, y lo dice el dominio ----
			PedidoDeSexo pedido = sinSaber.FaltaElSexo;
			Igual("con sexo desconocido hace falta insistir", "True", pedido.HaceFalta.ToString());
			Igual("  y dice cuantos de cuantos", "2 de 7",
				$"{pedido.SinInterpretar} de {pedido.Analitos}");
			Igual("  con severidad informativo: el semaforo no se toca", "informativo",
				pedido.NombreDeLaSeveridad);
			Igual("  la frase la escribe el dominio, no la pantalla", "True",
				pedido.ParaSonia.Contains("no puedo interpretar 2 de tus 7").ToString());
			Igual("  y distingue los dos motivos", "True",
				(pedido.ParaSonia.Contains("el rango de hemoglobina es distinto según el sexo")
					&& pedido.ParaSonia.Contains("quizás no te")).ToString());
			Igual("con el sexo sabido no hace falta", "False",
				deEl.FaltaElSexo.HaceFalta.ToString());

			// ---- criterio 4: declarar el sexo reinterpreta el historial ENTERO ----
			//
			// Ninguna medicion se vuelve a cargar y ningun evento se migra: el rango nunca
			// se guardo con el valor, se calcula al consultar. Es la consecuencia elegante
			// del event sourcing, y se ve en que el MISMO expediente cambia de respuesta.
			Igual("antes de declarar, sin veredicto", "requiere sexo",
				sinSaber.InterpretarSerieDe(Analito.Hemoglobina.Nombre).NombreDelVeredicto);
			sinSaber.DeclararSexo("hombre");
			Igual("  y despues del comando, con veredicto real", "True",
				sinSaber.InterpretarSerieDe(Analito.Hemoglobina.Nombre).HayLectura.ToString());
			Igual("  el antigeno tambien vuelve a existir", "True",
				Nombra(sinSaber.Analitos, Analito.AntigenoProstatico.Nombre).ToString());
			Igual("  y ya no falta nada", "0", sinSaber.AnalitosNoInterpretables.ToString());

			// ---- criterio 8: el orden en que llega el sexo no cambia el resultado ----
			//
			// Es el replay: declararlo ANTES de los examenes o DESPUES tiene que dar lo
			// mismo, porque el sexo no se guarda con la medicion.
			var primeroElSexo = new Expediente(60);
			primeroElSexo.DeclararSexo("mujer");
			Sembrar(primeroElSexo);

			var primeroLosExamenes = new Expediente(60);
			Sembrar(primeroLosExamenes);
			primeroLosExamenes.DeclararSexo("mujer");

			Igual("el evento de sexo en cualquier posicion da lo mismo", "True",
				(primeroElSexo.InterpretarSerieDe(Analito.Hemoglobina.Nombre).ParaSonia
					== primeroLosExamenes.InterpretarSerieDe(Analito.Hemoglobina.Nombre).ParaSonia)
					.ToString());
			Igual("  y el antigeno no existe en ninguno de los dos", "False",
				(Nombra(primeroElSexo.Analitos, Analito.AntigenoProstatico.Nombre)
					|| Nombra(primeroLosExamenes.Analitos, Analito.AntigenoProstatico.Nombre))
					.ToString());

			// ---- criterio 5: los rangos cambian, y la referencia lo muestra ----
			Expediente hombre = ConLaHistoria(Sexo.Hombre);
			Expediente mujer = ConLaHistoria(Sexo.Mujer);

			Igual("la referencia de la hemoglobina cambia con el sexo", "True",
				(hombre.InterpretarSerieDe(Analito.Hemoglobina.Nombre).ReferenciaLegible
					!= mujer.InterpretarSerieDe(Analito.Hemoglobina.Nombre).ReferenciaLegible)
					.ToString());
			Igual("  la del colesterol NO", "True",
				(hombre.InterpretarSerieDe(Analito.ColesterolTotal.Nombre).ReferenciaLegible
					== mujer.InterpretarSerieDe(Analito.ColesterolTotal.Nombre).ReferenciaLegible)
					.ToString());

			// EL MISMO NUMERO, DOS LECTURAS. Es el caso que justifica el rango por sexo.
			var doceDos = new Hemoglobina(Hoy, 12.2m, "g/dL");
			Igual("12.2 g/dL es anemia leve en un hombre", "leve",
				doceDos.GradoDeAnemia(Hombre60).Nombre);
			Igual("  y es normal en una mujer", "ninguna",
				doceDos.GradoDeAnemia(Mujer60).Nombre);

			// ---- de donde viene el sexo: gana lo que dijo la persona (§4, opcion C) ----
			var conConflicto = new Expediente(60);
			conConflicto.PublicarSexo("hombre");
			Igual("si solo publica la institucion, ese vale", "hombre",
				conConflicto.Sexo.Nombre);

			conConflicto.DeclararSexo("mujer");
			Igual("  si la persona dice otra cosa, gana ella", "mujer",
				conConflicto.Sexo.Nombre);
			Igual("  y el journal conserva las dos", "hombre",
				conConflicto.SexoPublicadoPorLaInstitucion.Nombre);
			Igual("  el conflicto es un hecho, no un error", "True",
				conConflicto.HayConflictoDeSexo.ToString());

			// La frontera resuelve como escribe un laboratorio real, no como decidio esta app.
			Igual("un laboratorio que escribe 'M'", "hombre", Sexo.PorNombre("M").Nombre);
			Igual("  otro que escribe 'Femenino'", "mujer", Sexo.PorNombre("Femenino").Nombre);
			Igual("  un campo vacio es Desconocido, no un error", "desconocido",
				Sexo.PorNombre("").Nombre);
			Revienta<ArgumentException>("  y un valor que no se modela revienta al PREGUNTAR",
				() => Sexo.PorNombre("no binario"));
			Igual("  pero se DESCARTA al recibirlo de afuera", "False",
				Sexo.EsConocidoElNombre("no binario").ToString());
		}

		// La historia del guion, con el sexo que se le indique. Siete analitos con serie:
		// antigeno, colesterol total, LDL, hemoglobina, nitritos y las dos sangres ocultas.
		private static Expediente ConLaHistoria(Sexo sexo)
		{
			var expediente = new Expediente(60);
			if (sexo.EsConocido) expediente.DeclararSexo(sexo.Nombre);
			Sembrar(expediente);
			return expediente;
		}

		private static void Sembrar(Expediente expediente)
		{
			expediente.Agregar(new AntigenoProstatico(Hoy.AddDays(-1096), 3.0m, "ng/mL"));
			expediente.Agregar(new AntigenoProstatico(Hoy.AddDays(-731), 3.1m, "ng/mL"));
			expediente.Agregar(new AntigenoProstatico(Hoy.AddDays(-365), 3.0m, "ng/mL"));
			expediente.Agregar(new AntigenoProstatico(Hoy, 5.2m, "ng/mL"));
			expediente.Agregar(new Colesterol(Hoy.AddDays(-365), 200m, "mg/dL"));
			expediente.Agregar(new Colesterol(Hoy, 205m, "mg/dL"));
			expediente.Agregar(new ColesterolDetallado(Hoy.AddDays(-365), 150m, 45m, "mg/dL"));
			expediente.Agregar(new ColesterolDetallado(Hoy, 148m, 45m, "mg/dL"));
			expediente.Agregar(new Hemoglobina(Hoy.AddDays(-365), 14.0m, "g/dL"));
			expediente.Agregar(new Hemoglobina(Hoy, 14.2m, "g/dL"));
			expediente.Agregar(new ExamenDeOrina(Hoy, "ambar", "negativo", "negativo"));
			expediente.Agregar(new ExamenDeHeces(Hoy, "marron", "negativo"));
		}

		// IReadOnlyList no trae Contains, y agregar Linq a estas comprobaciones por una
		// busqueda de tres lineas no compra nada.
		private static bool Nombra(IReadOnlyList<string> nombres, string cual)
		{
			foreach (string nombre in nombres)
				if (nombre == cual) return true;
			return false;
		}

		private static bool EstaEnElCuadro(Expediente expediente, string analito)
		{
			foreach (CeldaDelCuadro celda in expediente.CeldasDelCuadro)
				if (celda.Analito == analito) return true;
			return false;
		}

		private static bool EstaEnLosControles(Expediente expediente, string analito)
		{
			foreach (ControlPendiente control in expediente.ControlesPendientes(Hoy))
				if (control.Nombre == analito) return true;
			return false;
		}

		// Cuantos hallazgos tiene la historia del guion con una hemoglobina mas al final.
		// Es para comprobar que un valor sin rango —o de un analito que quizas no
		// corresponda— no se cuenta como fuera de rango.
		private static int ConUnaHemoglobinaDe(Sexo sexo, decimal ultima)
		{
			Expediente expediente = ConLaHistoria(sexo);
			expediente.Agregar(new Hemoglobina(Hoy.AddDays(1), ultima, "g/dL"));
			return expediente.AnalitosFueraDeRango;
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
