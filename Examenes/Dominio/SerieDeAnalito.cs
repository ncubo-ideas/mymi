using System;
using System.Collections.Generic;

namespace Examenes.Dominio
{
	// La historia de UN analito: sus mediciones ordenadas en el tiempo.
	//
	// Aca esta la capacidad que justifica todo el dominio - evaluar la SERIE en vez del
	// ultimo valor - y la regla vive en codigo, no en los datos:
	//
	//   1. Menos de dos mediciones  -> Indeterminada. No se inventa una tendencia.
	//   2. El cambio dividido por los dias transcurridos supera lo que el analito
	//      considera biologicamente verosimil -> SaltoInverosimil: alguien midio mal, y
	//      lo que corresponde es repetir el examen, no asustar al paciente.
	//   3. El cambio no llega al umbral significativo del analito -> Estable.
	//   4. Si llega -> EnAscenso o EnDescenso, y se marca ExigeAtencion solo si la
	//      direccion es la que preocupa PARA ESE analito.
	//
	// Los umbrales los aporta el analito (CambioMaximoVerosimilPorDia,
	// CambioQueExigeAtencion, DireccionPreocupanteEsAscenso). El algoritmo es uno; el
	// conocimiento clinico es de cada subclase.
	public class SerieDeAnalito
	{
		private readonly List<ExamenDeSangre> mediciones = new List<ExamenDeSangre>();

		// La identidad del analito, no su nombre. Es lo que hace que dos hojas que
		// escriben "HEMOGLOBINA" y "HGB" caigan en ESTA serie y no en dos.
		public Analito Analito { get; private set; }

		public SerieDeAnalito(Analito analito)
		{
			ArgumentNullException.ThrowIfNull(analito);
			Analito = analito;
		}

		public IReadOnlyList<ExamenDeSangre> Mediciones => mediciones;

		public int Cantidad => mediciones.Count;

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

				if (Math.Abs(cambioDelPaso) / DiasParaLaTasa(diasDelPaso) > actual.CambioMaximoVerosimilPorDia)
					return new EvaluacionDeTendencia(Analito, Tendencia.SaltoInverosimil, mediciones.Count,
						anterior.Valor, actual.Valor, diasDelPaso, cambioDelPaso, false);
			}

			ExamenDeSangre primero = mediciones[0];
			ExamenDeSangre ultimo = mediciones[mediciones.Count - 1];

			decimal cambio = ultimo.Valor.DiferenciaDesde(primero.Valor);
			decimal magnitud = Math.Abs(cambio);
			int dias = Dias(primero, ultimo);

			// Regla 2: cambio real pero por debajo del umbral significativo del analito.
			if (magnitud < ultimo.CambioQueExigeAtencion)
				return new EvaluacionDeTendencia(Analito, Tendencia.Estable, mediciones.Count,
					primero.Valor, ultimo.Valor, dias, cambio, false);

			// Regla 3: cambio significativo. La direccion es un hecho; que preocupe depende
			// del analito.
			Tendencia tendencia = cambio > 0 ? Tendencia.EnAscenso : Tendencia.EnDescenso;
			bool exigeAtencion = ultimo.DireccionPreocupanteEsAscenso ? cambio > 0 : cambio < 0;

			return new EvaluacionDeTendencia(Analito, tendencia, mediciones.Count,
				primero.Valor, ultimo.Valor, dias, cambio, exigeAtencion);
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
		public Velocidad Velocidad()
		{
			ExamenDeSangre ultimo = Ultimo;
			string unidad = ultimo == null ? "" : ultimo.Valor.Unidad.Simbolo;
			decimal umbral = ultimo == null ? 0m : ultimo.VelocidadQueExigeAtencionPorAno;

			if (mediciones.Count < 2) return Examenes.Dominio.Velocidad.NoAplica(Analito, unidad, umbral);

			ExamenDeSangre anterior = mediciones[mediciones.Count - 2];
			int dias = Dias(anterior, ultimo);
			if (dias <= 0) return Examenes.Dominio.Velocidad.NoAplica(Analito, unidad, umbral);

			decimal cambio = ultimo.Valor.DiferenciaDesde(anterior.Valor);
			decimal porAno = cambio * DiasDelAno / dias;

			return Examenes.Dominio.Velocidad.Medida(Analito, porAno, unidad, dias, umbral);
		}

		// El ano que usa el dominio para anualizar. Es una constante nombrada y no un 365
		// suelto adentro de la formula, porque es una decision del modelo: se usa el ano
		// calendario simple, sin bisiestos, para que la misma serie de fechas literales
		// produzca siempre el mismo numero.
		private const decimal DiasDelAno = 365m;

		private static int Dias(ExamenDeSangre desde, ExamenDeSangre hasta) =>
			(int)(hasta.Fecha.Date - desde.Fecha.Date).TotalDays;

		// Dos mediciones el MISMO dia se tratan como un dia: el cociente no puede dividir
		// por cero, y ademas moralmente es el caso mas sospechoso de todos.
		private static int DiasParaLaTasa(int dias) => dias <= 0 ? 1 : dias;

	}
}
