using System;
using System.Collections.Generic;

namespace Examenes.Dominio
{
	// LA SERIE DE UNA MAGNITUD CONTINUA: mediciones con numero y unidad, ordenadas.
	//
	// Es lo que antes era SerieDeAnalito entera. Se quedo con TODO lo que exige que los
	// puntos sean numeros -restar, dividir por dias, comparar contra un umbral- y de ahi
	// para arriba solo subio lo que de verdad tiene cualquier serie.
	//
	// Aca esta la capacidad que justifica todo el dominio - evaluar la SERIE en vez del
	// ultimo valor - y la regla vive en codigo, no en los datos:
	//
	//   1. Menos de dos mediciones  -> Indeterminada. No se inventa una tendencia.
	//   2. El cambio dividido por los dias transcurridos supera lo que el analito
	//      considera biologicamente verosimil -> SaltoInverosimil: alguien midio mal, y
	//      lo que corresponde es repetir el examen, no asustar al paciente.
	//   3. El cambio no llega al umbral significativo del analito -> Estable.
	//   4. Si llega -> EnAscenso o EnDescenso. Si esa direccion es buena o mala NO se
	//      decide aca: lo contesta el analito en Interpretar().
	//
	// Los umbrales los aporta el analito (CambioMaximoVerosimilPorDia,
	// CambioMinimoSignificativo, Direccion). El algoritmo es uno; el conocimiento clinico
	// es de cada subclase.
	//
	// ESTO ES LA CAPA DE LOS HECHOS, no la de la lectura. Evaluar() dice que paso -para
	// donde, cuanto, en cuantos dias- con reglas que valen para cualquier magnitud
	// continua. Que SIGNIFICA lo dice Interpretar(), que delega en el analito, porque un
	// colesterol bajando y una hemoglobina bajando son el mismo hecho y conclusiones
	// opuestas.
	// ES ABSTRACTA, y esa es la correccion de la reunion del 2026-08-26.
	//
	// Alvaro, textual: "eso no es lo mismo que el conjunto de examenes de antigeno
	// prostatico evaluando su tendencia. Ahi usted quizas tiene que hacer EL PLURAL
	// antigeno prostatico, examenes plural, y a ese hacerle METODOS ESPECIFICOS de evaluar
	// su tendencia. […] si usted generaliza 'voy a evaluar tendencias' y lo pone en
	// examenes, ahi usted hace tendencias de orinas que no tienen ningun sentido."
	//
	// LA SENAL QUE LO DELATABA: los metodos propios que fuimos agregando —VelocidadDe(serie),
	// NivelHabitual(serie), PicoNoConfirmado(serie), PartidaDelEpisodio(serie)— recibian
	// todos LA SERIE como parametro. Un metodo que necesita la serie entera para contestar
	// no es de la medicion: es de la serie, disfrazada de metodo de la medicion. Ahora
	// viven donde son, y las firmas quedan sin parametro.
	public abstract class SerieContinua : SerieDeAnalito
	{
		private readonly List<ExamenDeSangre> mediciones = new List<ExamenDeSangre>();

		protected SerieContinua(Analito analito) : base(analito)
		{
		}

		public IReadOnlyList<ExamenDeSangre> Mediciones => mediciones;

		public override int Cantidad => mediciones.Count;

		// Se inserta manteniendo el orden por fecha. El proveedor podria emitir un
		// examen viejo despues de uno nuevo (una carga atrasada), y una serie
		// desordenada daria una tendencia al reves.
		public void Agregar(ExamenDeSangre examen)
		{
			ArgumentNullException.ThrowIfNull(examen);
			if (!examen.Analito.EsElMismo(Analito))
				throw new ArgumentException(
					$"Un examen de '{examen.Analito.Nombre}' no pertenece a la serie de '{Analito.Nombre}'.",
					nameof(examen));

			int posicion = mediciones.Count;
			while (posicion > 0 && mediciones[posicion - 1].Fecha > examen.Fecha) posicion--;

			mediciones.Insert(posicion, examen);
		}

		public ExamenDeSangre Primero => mediciones.Count == 0 ? null : mediciones[0];

		public ExamenDeSangre Ultimo => mediciones.Count == 0 ? null : mediciones[mediciones.Count - 1];

		public override DateTime FechaDeLaUltima => Ultimo == null ? default : Ultimo.Fecha;

		// La periodicidad la sabe el ANALITO, no la serie: es conocimiento clinico y ya
		// vivia en la medicion, que la resuelve contra su propio valor y la edad.
		public override int MesesHastaElProximo(Paciente paciente) =>
			Ultimo.MesesHastaElProximo(paciente);

		// Para una continua, "hay algo que no deberia estar" es salirse del rango.
		//
		// SIN RANGO NO HAY HALLAZGO, y eso no es "esta bien": es que no se puede afirmar
		// que este mal. Contar una hemoglobina de sexo desconocido como fuera de rango
		// seria inventar un hallazgo; no contarla es lo unico honesto, y lo que la persona
		// ve en su lugar es el bloque que pide el dato.
		public override bool UltimaEsHallazgo(Paciente paciente) =>
			Ultimo != null && Ultimo.HayRangoPara(paciente) && !Ultimo.EsNormalPara(paciente);

		// La referencia contra la que se lee el ultimo valor, ya dicha. Sale del rango
		// del propio examen —que es donde vive el conocimiento por edad— y se dice como
		// se lee: un rango que arranca en cero es un techo ("hasta 4.5 ng/mL"), uno con
		// piso real es una banda ("entre 13 y 17 g/dL").
		public override string ReferenciaLegible(Paciente paciente)
		{
			if (Ultimo == null) return "";

			RangoDeReferencia rango = Ultimo.RangoPara(paciente);

			// Sin rango la referencia NO es vacia: es "depende de tu sexo". Es la unica
			// frase honesta al lado de un numero que no se puede juzgar, y la escribe el
			// dominio —igual que todas las demas— para que la pantalla no la componga.
			if (rango == null) return "depende de tu sexo";

			return rango.Minimo.Valor == 0m
				? $"hasta {rango.Maximo.Expresado}"
				: $"entre {rango.Minimo.Expresado} y {rango.Maximo.Expresado}";
		}

		// LA operacion del dominio. Evalua la serie completa, no el ultimo valor.
		//
		// Son dos preguntas distintas sobre la misma serie, y se contestan mirando cosas
		// distintas:
		//
		//   "hay una medicion que no puede ser?" -> se mira PASO A PASO, entre mediciones
		//     consecutivas. Un error de laboratorio es UN valor suelto raro; comparando
		//     solo los extremos se vuelve invisible, porque el tiempo total lo diluye.
		//     Medido: 200 (hace un ano) -> 280 (hoy) -> 360 (manana) da 160 mg/dL en 369
		//     dias, que es perfectamente verosimil, y sin embargo el ultimo paso son 80
		//     mg/dL en UN DIA.
		//
		//   "para donde viene?" -> se miran los EXTREMOS. La direccion de la serie es de
		//     la serie entera, no de su ultimo tramo.
		//
		// Cuando hay un paso inverosimil, lo que se reporta como Primero/Ultimo es ESE
		// PASO y no los extremos: el hecho accionable es "de 280 a 360 en un dia", que es
		// lo que le permite a la superficie decir "conviene repetir el examen". Para
		// cualquier otra tendencia, Primero/Ultimo son los extremos de la serie.
		public EvaluacionDeTendencia Evaluar()
		{
			if (mediciones.Count < 2)
				return EvaluacionDeTendencia.SinSerie(Analito, mediciones.Count,
					mediciones.Count == 1 ? mediciones[0].Valor : null);

			// Regla 1: un paso entre mediciones consecutivas que no puede ser.
			for (int i = 1; i < mediciones.Count; i++)
			{
				ExamenDeSangre anterior = mediciones[i - 1];
				ExamenDeSangre actual = mediciones[i];

				decimal cambioDelPaso = actual.Valor.DiferenciaDesde(anterior.Valor);
				int diasDelPaso = Dias(anterior, actual);

				// DOS formas de que un paso sea inverosimil, y hacen falta las dos:
				//
				//   por TASA      cambio demasiado para los dias que pasaron
				//   por MULTIPLO  un salto que ningun intervalo justifica en ESTE analito
				//
				// La tasa sola no alcanza: con suficientes dias entre mediciones, cualquier
				// salto pasa. El multiplo no depende del tiempo, y es el que atrapa el "de
				// 1 a 10" del que hablaba Alvaro.
				bool porTasa = Math.Abs(cambioDelPaso) / DiasParaLaTasa(diasDelPaso)
					> actual.CambioMaximoVerosimilPorDia;
				bool porMultiplo = EsSaltoDeMultiplo(anterior, actual);

				if (porTasa || porMultiplo)
					return new EvaluacionDeTendencia(Analito, Tendencia.SaltoInverosimil, mediciones.Count,
						anterior.Valor, actual.Valor, diasDelPaso, cambioDelPaso);
			}

			ExamenDeSangre primero = mediciones[0];
			ExamenDeSangre ultimo = mediciones[mediciones.Count - 1];

			decimal cambio = ultimo.Valor.DiferenciaDesde(primero.Valor);
			decimal magnitud = Math.Abs(cambio);
			int dias = Dias(primero, ultimo);

			// Regla 2: cambio real pero por debajo del umbral significativo del analito.
			if (!ultimo.EsCambioReal(cambio))
				return new EvaluacionDeTendencia(Analito, Tendencia.Estable, mediciones.Count,
					primero.Valor, ultimo.Valor, dias, cambio);

			// Regla 3: cambio significativo. La direccion es un hecho; que preocupe depende
			// del analito.
			// Solo la DIRECCION, que es un hecho. Si esa direccion es buena o mala lo dice
			// Interpretar(), porque depende del analito.
			Tendencia tendencia = cambio > 0 ? Tendencia.EnAscenso : Tendencia.EnDescenso;

			return new EvaluacionDeTendencia(Analito, tendencia, mediciones.Count,
				primero.Valor, ultimo.Valor, dias, cambio);
		}

		// LA OTRA operacion de la serie, y la que el antigeno prostatico exige: cuanto se
		// movio POR ANO.
		//
		// Es una pregunta distinta de Evaluar(), no un campo mas de la misma respuesta.
		// Evaluar() contesta "para donde viene y es creible"; esto contesta "a que ritmo",
		// que es lo unico que un PSA de 5.2 permite decidir. Por eso son dos operaciones.
		//
		// EL TRAMO ES EL MAS RECIENTE, no la serie entera, y esa es la regla nombrada de
		// esta clase: los anos estables de antes no diluyen un salto de este ano, lo hacen
		// mas notable. El razonamiento con los numeros del guion esta en Velocidad.
		//
		// Dos mediciones el MISMO dia no dan una velocidad: un cociente sobre cero dias no
		// es un ritmo anual, es una division por cero disfrazada. Ese caso ya lo atrapa
		// Evaluar() como salto inverosimil, que es la lectura correcta.

		// El tramo mas reciente, en dias. Lo necesita el analito que calcula velocidad y
		// no tiene por que rehacer la aritmetica de fechas de la serie.
		public int DiasDelUltimoTramo =>
			mediciones.Count < 2 ? 0 : Dias(mediciones[mediciones.Count - 2], Ultimo);

		// El cambio con signo del tramo mas reciente.
		public decimal CambioDelUltimoTramo =>
			mediciones.Count < 2
				? 0m
				: Ultimo.Valor.DiferenciaDesde(mediciones[mediciones.Count - 2].Valor);

		// El ano que usa el dominio para anualizar, expuesto para el analito que lo
		// necesita. Ver la nota de abajo sobre por que es una constante nombrada.
		public static decimal DiasDeUnAno => DiasDelAno;

		// El ano que usa el dominio para anualizar. Es una constante nombrada y no un 365
		// suelto adentro de la formula, porque es una decision del modelo: se usa el ano
		// calendario simple, sin bisiestos, para que la misma serie de fechas literales
		// produzca siempre el mismo numero.
		private const decimal DiasDelAno = 365m;

		// Si el paso multiplica el valor mas alla de lo que ese analito admite. Se mira en
		// las dos direcciones: caer a un tercio es tan implausible como triplicarse.
		private static bool EsSaltoDeMultiplo(ExamenDeSangre anterior, ExamenDeSangre actual)
		{
			decimal antes = anterior.Valor.Valor;
			decimal ahora = actual.Valor.Valor;
			if (antes <= 0m || ahora <= 0m) return false;

			decimal multiplo = ahora > antes ? ahora / antes : antes / ahora;
			return multiplo > actual.MultiploQueNingunIntervaloJustifica;
		}

		private static int Dias(ExamenDeSangre desde, ExamenDeSangre hasta) =>
			(int)(hasta.Fecha.Date - desde.Fecha.Date).TotalDays;

		// Dos mediciones el MISMO dia se tratan como un dia: el cociente no puede dividir
		// por cero, y ademas moralmente es el caso mas sospechoso de todos.
		private static int DiasParaLaTasa(int dias) => dias <= 0 ? 1 : dias;
	}
}
