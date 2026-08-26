using System;
using System.Collections.Generic;

namespace Examenes.Dominio
{
	// LA SERIE DEL COLESTEROL, que se lee AL REVES que la del antigeno.
	//
	//   antigeno    "usted no quiere perder la historia"  -> contra su linea base
	//   colesterol  "yo no ocupo ver esa historia"        -> contra una meta
	//
	// Por eso son dos tipos y no uno con un parametro: no comparten ni la pregunta.
	public sealed class SerieDeColesterol : SerieDeAnalito
	{
		public SerieDeColesterol() : base(Analito.ColesterolTotal) { }

		private Colesterol Ultima => (Colesterol)Ultimo;

		private static readonly Analito Suyo = Analito.ColesterolTotal;

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
		public MetaDeColesterol Meta() => Ultima.Meta();

		// DONDE EMPEZO EL EPISODIO: el pico desde el que viene bajando. Es el punto de
		// partida contra el que se mide el progreso, y no es la primera medicion de la
		// serie — "la historia no es de todos los examenes que el se ha hecho".
		//
		// Null cuando no hubo episodio, o sea cuando nunca subio.
		public Medicion PartidaDelEpisodio()
		{
			if (Cantidad < 2) return null;

			IReadOnlyList<ExamenDeSangre> mediciones = Mediciones;

			// El maximo entre las previas: desde ahi es de donde se viene bajando.
			decimal pico = 0m;
			for (int i = 0; i < mediciones.Count - 1; i++)
				if (mediciones[i].Valor.Valor > pico) pico = mediciones[i].Valor.Valor;

			return pico > Ultima.Valor.Valor ? new Medicion(pico, Analito.Unidad) : null;
		}

		// CUANTO LLEVA RECORRIDO hacia la meta, de cero a uno. Es lo que una barra de
		// progreso puede pintar sin decidir nada.
		public decimal TrayectoriaHaciaMeta() =>
			Meta().ProgresoDesde(PartidaDelEpisodio());

		// Si el valor viene BAJANDO desde un pico anterior. Es la pregunta que convierte
		// un fuera de rango en buena noticia, y no existe para ningun otro analito del
		// demo porque ninguno tiene un "episodio" del que se vuelve.
		public bool VieneBajandoDesdeSuPico()
		{
			if (Cantidad < 2) return false;

			IReadOnlyList<ExamenDeSangre> mediciones = Mediciones;

			decimal pico = 0m;
			for (int i = 0; i < mediciones.Count - 1; i++)
				if (mediciones[i].Valor.Valor > pico) pico = mediciones[i].Valor.Valor;

			return Ultima.EsCambioReal(Ultima.Valor.Valor - pico) && Ultima.Valor.Valor < pico;
		}

		// ---------------------------------------------------------------- la lectura

		public override Interpretacion Interpretar(int edad)
		{
			EvaluacionDeTendencia hechos = Evaluar();
			if (!hechos.HaySerie) return Interpretacion.SinSerie(hechos);

			if (hechos.Tendencia.SugiereRepetirElExamen)
				return new Interpretacion(Veredicto.ProbableErrorDeMedicion, Severidad.Seguimiento,
					$"pasó de {hechos.ValorInicial} a {hechos.ValorFinal} {hechos.Unidad} en "
					+ $"{hechos.DiasTranscurridos} día{(hechos.DiasTranscurridos == 1 ? "" : "s")}, "
					+ "que no es un cambio que el cuerpo pueda hacer en ese tiempo", hechos);

			// EL CASO DE ALVARO, y va antes que la comparacion contra el rango a
			// proposito: fuera de rango y malo son dos ejes distintos. Si esto estuviera
			// despues, un 280 bajando desde 320 saldria como alarma.
			if (VieneBajandoDesdeSuPico())
			{
				MetaDeColesterol meta = Meta();
				Medicion partida = PartidaDelEpisodio();
				string desde = partida == null ? "" : $"venías de {partida.Expresado} y ";

				return new Interpretacion(Veredicto.Mejorando, Severidad.Informativo,
					$"{desde}vas bien: {meta.Expresada}", hechos);
			}

			bool enRango = Ultima.EsNormalPara(edad);

			if (!Ultima.EsCambioReal(hechos.Cambio))
				return enRango
					? new Interpretacion(Veredicto.Estable, Severidad.Informativo,
						$"se mantiene en {Ultima.Valor.Expresado}, dentro de lo deseable", hechos)
					: new Interpretacion(Veredicto.Estable, Severidad.ConsultaMedica,
						$"lleva tiempo en {Ultima.Valor.Expresado}, por encima de lo deseable", hechos);

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
					$"subió a {Ultima.Valor.Expresado}. A tu edad esto se corrige, y {objetivo.ComoObjetivo}",
					hechos)
				: new Interpretacion(Veredicto.Empeorando, Severidad.ConsultaMedica,
					$"subió de {hechos.ValorInicial} a {hechos.ValorFinal} {hechos.Unidad} en "
					+ $"{hechos.DiasTranscurridos} días. {Mayuscula(objetivo.ComoObjetivo)}", hechos);
		}

		private static string Mayuscula(string frase) =>
			frase.Length == 0 ? frase : char.ToUpperInvariant(frase[0]) + frase.Substring(1);

		// A partir de que edad un colesterol fuera de rango deja de leerse como algo que se
		// corrige con habitos y pasa a ser algo que conviene que vea un medico.
		private const int EdadEnQuePesaMas = 50;
	}
}
