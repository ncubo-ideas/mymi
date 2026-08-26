using System;
using System.Collections.Generic;

namespace Examenes.Dominio
{
	// Colesterol total. El analito del guion: 200 y despues 280.
	//
	// Los dos umbrales son lo que hace que la misma diferencia de 80 mg/dL se lea de
	// dos maneras distintas segun cuanto tiempo paso:
	//
	//   CambioMaximoVerosimilPorDia = 1.0  -> 80 mg/dL en 1 dia son 80/dia, muy por
	//                                         encima de 1.0: el laboratorio se equivoco.
	//                                         80 mg/dL en 365 dias son 0.22/dia: pasa.
	//   CambioMinimoSignificativo   = 15   -> por debajo de eso la diferencia no se
	//                                         distingue de la variacion normal del propio
	//                                         analito. Ver la nota del umbral, abajo.
	//
	// LO QUE ESTE ANALITO SABE Y NINGUN OTRO: su NIVEL HABITUAL. Es el pedido textual de
	// Alvaro, y es lo unico que permite leer bien el caso que el planteo:
	//
	//   "Por 20 anos he tenido el colesterol super bien. Un dia me sale altisimo... y lo
	//    que deseo ver es como va bajando."
	//
	// Un 280 que viene bajando desde 320 es BUENA NOTICIA aunque siga fuera de rango. La
	// app decia "EN ASCENSO" y "conviene que lo vea tu medico" a cualquier colesterol que
	// se moviera, sin mirar hacia donde. Ese es el caso que este analito ahora distingue.
	public sealed class Colesterol : ExamenDeSangre
	{
		private static readonly Analito Suyo = Examenes.Dominio.Analito.ColesterolTotal;

		public Colesterol(DateTime fecha, decimal valor, string unidad)
			: base(fecha, Suyo, Medida(valor, unidad, Suyo))
		{
		}

		public override string Nombre => Suyo.ParaMostrar;

		// El techo de lo deseable baja con la edad: a los 60 el mismo 210 pesa mas que
		// a los 30.
		public override RangoDeReferencia RangoPara(int edad) =>
			RangoDeReferencia.HastaUnMaximoDe(edad >= 50 ? 200m : 220m, Suyo.Unidad);

		public override decimal CambioMaximoVerosimilPorDia => 1.0m;

		// 15 mg/dL, y no es un numero de demo. La variacion biologica intraindividual del
		// colesterol ronda 6%, que sobre un valor tipico de 200-250 son 12-15 mg/dL: por
		// debajo de eso una diferencia no es interpretable como cambio real.
		//
		// MEDIDO sobre un caso real: 220 (2022) -> 234 (2026) son 14 mg/dL en 1362 dias.
		// Parece un ascenso y no lo es; con este umbral el veredicto es Estable, que es
		// lo correcto. Antes el umbral era 50 y tapaba el caso por el otro lado.
		public override decimal CambioMinimoSignificativo => 15m;

		public override DireccionDeseable Direccion => DireccionDeseable.Bajar;

		// EN RANGO se revisa cada cinco anos; FUERA, todos los anos. Es el ejemplo mas
		// claro de por que esto no puede ser una constante: el mismo analito, la misma
		// persona, y la respuesta cambia segun donde quedo el ultimo valor.
		public override int MesesHastaElProximo(int edad) => EsNormalPara(edad) ? 60 : 12;

		// ---------------------------------------------------------------- lo suyo

		// LA META, y de donde arranco el episodio.
		//
		// Alvaro, 2026-08-26: "¿Como esta el colesterol de Pleitez por sus primeros 30 anos
		// de su vida? Siempre esta bien. YO NO OCUPO VER ESA HISTORIA." Y en cambio: "usted
		// tiene una especie como de meta... tenes el colesterol en 280 y tenes que bajarlo
		// a 240."
		//
		// Es la diferencia entera con el antigeno prostatico, que se lee al reves: alla la
		// historia larga ES el dato, aca estorba. Este analito tenia NivelHabitual() sobre
		// la mediana de toda la serie y se saco: era el metodo del otro analito puesto en
		// este.
		public MetaDeColesterol Meta() => MetaDeColesterol.Para(Valor);

		// DONDE EMPEZO EL EPISODIO: el pico desde el que viene bajando. Es el punto de
		// partida contra el que se mide el progreso, y no es la primera medicion de la
		// serie — "la historia no es de todos los examenes que el se ha hecho".
		//
		// Null cuando no hubo episodio, o sea cuando nunca subio.
		public Medicion PartidaDelEpisodio(SerieDeAnalito serie)
		{
			ArgumentNullException.ThrowIfNull(serie);

			IReadOnlyList<ExamenDeSangre> mediciones = serie.Mediciones;
			if (mediciones.Count < 2) return null;

			// El maximo entre las previas: desde ahi es de donde se viene bajando.
			decimal pico = 0m;
			for (int i = 0; i < mediciones.Count - 1; i++)
				if (mediciones[i].Valor.Valor > pico) pico = mediciones[i].Valor.Valor;

			return pico > Valor.Valor ? new Medicion(pico, Suyo.Unidad) : null;
		}

		// CUANTO LLEVA RECORRIDO hacia la meta, de cero a uno. Es lo que una barra de
		// progreso puede pintar sin decidir nada.
		public decimal TrayectoriaHaciaMeta(SerieDeAnalito serie) =>
			Meta().ProgresoDesde(PartidaDelEpisodio(serie));

		// Si el valor viene BAJANDO desde un pico anterior. Es la pregunta que convierte
		// un fuera de rango en buena noticia, y no existe para ningun otro analito del
		// demo porque ninguno tiene un "episodio" del que se vuelve.
		public bool VieneBajandoDesdeSuPico(SerieDeAnalito serie)
		{
			ArgumentNullException.ThrowIfNull(serie);

			IReadOnlyList<ExamenDeSangre> mediciones = serie.Mediciones;
			if (mediciones.Count < 2) return false;

			decimal pico = 0m;
			for (int i = 0; i < mediciones.Count - 1; i++)
				if (mediciones[i].Valor.Valor > pico) pico = mediciones[i].Valor.Valor;

			return EsCambioReal(Valor.Valor - pico) && Valor.Valor < pico;
		}

		// ---------------------------------------------------------------- la lectura

		public override Interpretacion Interpretar(SerieDeAnalito serie, int edad)
		{
			ArgumentNullException.ThrowIfNull(serie);

			EvaluacionDeTendencia hechos = serie.Evaluar();
			if (!hechos.HaySerie) return Interpretacion.SinSerie(hechos);

			if (hechos.Tendencia.SugiereRepetirElExamen)
				return new Interpretacion(Veredicto.ProbableErrorDeMedicion, Severidad.Seguimiento,
					$"pasó de {hechos.ValorInicial} a {hechos.ValorFinal} {hechos.Unidad} en "
					+ $"{hechos.DiasTranscurridos} día{(hechos.DiasTranscurridos == 1 ? "" : "s")}, "
					+ "que no es un cambio que el cuerpo pueda hacer en ese tiempo", hechos);

			// EL CASO DE ALVARO, y va antes que la comparacion contra el rango a
			// proposito: fuera de rango y malo son dos ejes distintos. Si esto estuviera
			// despues, un 280 bajando desde 320 saldria como alarma.
			if (VieneBajandoDesdeSuPico(serie))
			{
				MetaDeColesterol meta = Meta();
				Medicion partida = PartidaDelEpisodio(serie);
				string desde = partida == null ? "" : $"venías de {partida.Expresado} y ";

				return new Interpretacion(Veredicto.Mejorando, Severidad.Informativo,
					$"{desde}vas bien: {meta.Expresada}", hechos);
			}

			bool enRango = EsNormalPara(edad);

			if (!EsCambioReal(hechos.Cambio))
				return enRango
					? new Interpretacion(Veredicto.Estable, Severidad.Informativo,
						$"se mantiene en {Valor.Expresado}, dentro de lo deseable", hechos)
					: new Interpretacion(Veredicto.Estable, Severidad.ConsultaMedica,
						$"lleva tiempo en {Valor.Expresado}, por encima de lo deseable", hechos);

			if (enRango)
				return new Interpretacion(Veredicto.Empeorando, Severidad.Seguimiento,
					$"subió de {hechos.ValorInicial} a {hechos.ValorFinal} {hechos.Unidad} en "
					+ $"{hechos.DiasTranscurridos} días, todavía dentro de lo deseable", hechos);

			// LA EDAD MODULA, y es otra cosa que el rango solo no dice. Alvaro: "quizas a
			// Pleitez, por la edad, hoy estas mal. Bueno, si, pero se le cura haciendo
			// ejercicio. En cambio ya a cierta edad: usted salio mal en el colesterol, NO
			// ME IMPORTA QUE TUVO 30 ANOS BIEN, hoy salio mal."
			//
			// El mismo valor fuera de rango pide seguimiento en alguien joven y consulta en
			// alguien mayor. La historia previa buena no cambia ninguno de los dos.
			MetaDeColesterol objetivo = Meta();

			return edad < EdadEnQuePesaMas
				? new Interpretacion(Veredicto.Empeorando, Severidad.Seguimiento,
					$"subió a {Valor.Expresado}. A tu edad esto se corrige, y {objetivo.ComoObjetivo}",
					hechos)
				: new Interpretacion(Veredicto.Empeorando, Severidad.ConsultaMedica,
					$"subió de {hechos.ValorInicial} a {hechos.ValorFinal} {hechos.Unidad} en "
					+ $"{hechos.DiasTranscurridos} días. {Mayuscula(objetivo.ComoObjetivo)}", hechos);
		}

		// A partir de que edad un colesterol fuera de rango deja de leerse como algo que se
		// corrige con habitos y pasa a ser algo que conviene que vea un medico.
		private const int EdadEnQuePesaMas = 50;

		private static string Mayuscula(string frase) =>
			frase.Length == 0 ? frase : char.ToUpperInvariant(frase[0]) + frase.Substring(1);
	}
}
