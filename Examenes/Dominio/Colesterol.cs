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
		private static readonly AnalitoContinuo Suyo = Examenes.Dominio.Analito.ColesterolTotal;

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
		// El colesterol se mueve mucho mas que el antigeno con la dieta y con la vida, asi
		// que el corte va mas arriba: triplicarse si es plausible en anos.
		public override decimal MultiploQueNingunIntervaloJustifica => 4m;

		public override int MesesHastaElProximo(int edad) => EsNormalPara(edad) ? 60 : 12;

		// Lo que se puede contestar con UNA medicion: a donde tiene que llegar desde aca.
		// La partida del episodio y el progreso necesitan la historia y viven en
		// SerieDeColesterol.
		public MetaDeColesterol Meta() => MetaDeColesterol.Para(Valor);

		public override SerieContinua NuevaSerie() => new SerieDeColesterol();
	}
}
