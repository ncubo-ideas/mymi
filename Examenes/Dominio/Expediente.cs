using System;
using System.Collections.Generic;

namespace Examenes.Dominio
{
	// El agregado: el expediente del paciente. La puerta de acceso a sus examenes.
	//
	// Nadie construye un Examen y lo guarda por su cuenta; se agrega aca, y aca se
	// mantienen las series por analito al dia. Es el equivalente del Facturario en el
	// negocio de facturas, con una diferencia importante: ademas de la coleccion,
	// mantiene los INDICES que hacen posible la pregunta de la serie.
	//
	// La EDAD vive en el expediente y entra por constructor. No se calcula con la fecha
	// de hoy a proposito: un dominio que lee el reloj deja de ser reproducible, y el
	// mismo journal reproducido el ano que viene daria otros rangos de referencia. Para
	// el demo la edad esta quemada al construir el expediente; el dia que el proveedor
	// mande la fecha de nacimiento, entra por el mismo lugar.
	public class Expediente
	{
		private readonly List<Examen> examenes = new List<Examen>();

		// Serie por analito. Es lo que evita recorrer todos los examenes y filtrar en
		// cada consulta, y es la estructura que hace que "veni subiendo?" sea O(1) para
		// encontrar la serie.
		// La llave es la IDENTIDAD del analito, no su nombre. El diccionario compara por
		// referencia -Analito no define Equals- y eso es exactamente lo que se quiere: dos
		// examenes son de la misma serie si son del mismo analito, no si el emisor escribio
		// el nombre igual.
		//
		// Antes era un Dictionary<string,...> con OrdinalIgnoreCase, que resolvia las
		// mayusculas y NADA mas: "HGB" y "Hemoglobina" seguian siendo dos series.
		private readonly Dictionary<Analito, SerieDeAnalito> series =
			new Dictionary<Analito, SerieDeAnalito>();

		public int Edad { get; private set; }

		public Expediente(int edad)
		{
			if (edad <= 0 || edad > 120)
				throw new ArgumentOutOfRangeException(nameof(edad), edad,
					"La edad del paciente tiene que estar entre 1 y 120.");

			Edad = edad;
		}

		public IReadOnlyList<Examen> Lista => examenes;

		public int Cantidad => examenes.Count;

		public int CantidadDeAnalitosConSerie => series.Count;

		// Indexador por posicion en la lista. Es lo que hace valido el DSL
		// `expediente[@indice]` para el detalle: los examenes no tienen un numero de
		// documento como las facturas, asi que la posicion es la unica llave estable
		// que el journal reproduce igual.
		public Examen this[int indice]
		{
			get
			{
				if (indice < 0 || indice >= examenes.Count)
					throw new IndexOutOfRangeException(
						$"El expediente tiene {examenes.Count} examenes; no existe el indice {indice}.");
				return examenes[indice];
			}
		}

		public void Agregar(Examen examen)
		{
			ArgumentNullException.ThrowIfNull(examen);

			examenes.Add(examen);

			// Solo los cuantitativos forman serie. Un examen de orina entra a la lista
			// y no a ninguna serie, y eso es correcto: no hay tendencia de un color.
			if (examen is ExamenDeSangre deSangre)
			{
				if (!series.TryGetValue(deSangre.Analito, out SerieDeAnalito serie))
				{
					serie = new SerieDeAnalito(deSangre.Analito);
					series[deSangre.Analito] = serie;
				}
				serie.Agregar(deSangre);
			}
		}

		// Las cuatro consultas de abajo reciben el analito COMO TEXTO porque asi llega: de
		// una pregunta de la IA, de un click en la pantalla, del DSL. El texto se resuelve
		// a identidad UNA vez, en la frontera, igual que UnidadDeMedida.PorSimbolo.
		//
		// Y ahi se separan dos cosas que antes se confundian en una sola respuesta:
		//
		//   nombre que no resuelve      -> DEFECTO de quien pregunta. Revienta.
		//   analito real, sin mediciones -> respuesta legitima. Contesta "no hay serie".
		//
		// Antes las dos daban "no hay datos de eso", asi que un nombre mal escrito se veia
		// igual que un analito que la persona todavia no se hizo. Eran indistinguibles
		// justo cuando importa distinguirlas: al agregar un sinonimo y equivocarse.
		public bool TieneSerieDe(string analito) => series.ContainsKey(Analito.PorNombre(analito));

		public SerieDeAnalito SerieDe(string analito)
		{
			Analito cual = Analito.PorNombre(analito);
			if (!series.TryGetValue(cual, out SerieDeAnalito serie))
				throw new KeyNotFoundException(
					$"El expediente no tiene ninguna medicion de '{cual.Nombre}'.");

			return serie;
		}

		// La pregunta que ninguna pantalla contesta. Es total: un analito sin
		// mediciones devuelve Indeterminada en vez de reventar, porque "no tengo datos
		// de eso" es una respuesta legitima para quien pregunta.
		public EvaluacionDeTendencia EvaluarTendenciaDe(string analito)
		{
			Analito cual = Analito.PorNombre(analito);

			return series.TryGetValue(cual, out SerieDeAnalito serie)
				? serie.Evaluar()
				: EvaluacionDeTendencia.SinSerie(cual, 0, null);
		}

		// LO QUE EL ANALITO CONCLUYE de su propia serie: veredicto, severidad y la linea
		// para la persona. Es la consulta que reemplaza al "exige atencion" generico.
		//
		// Total por la misma razon que EvaluarTendenciaDe: preguntar por un analito real
		// del que todavia no hay nada es legitimo, y la respuesta es "no hay serie".
		public Interpretacion InterpretarSerieDe(string analito)
		{
			Analito cual = Analito.PorNombre(analito);

			return series.TryGetValue(cual, out SerieDeAnalito serie)
				? serie.Interpretar(Edad)
				: Interpretacion.SinSerie(EvaluacionDeTendencia.SinSerie(cual, 0, null));
		}

		// LO MAS URGENTE de todo el expediente. Es una pregunta que ninguna pantalla
		// contesta hoy y que no se puede armar afuera: exige recorrer cada serie,
		// interpretarla con el criterio de SU analito y quedarse con la mayor.
		public Severidad SeveridadMayor
		{
			get
			{
				Severidad mayor = Severidad.Informativo;
				foreach (KeyValuePair<Analito, SerieDeAnalito> par in series)
					mayor = Severidad.LaMayor(mayor, par.Value.Interpretar(Edad).Severidad);

				// Los examenes que NO forman serie tambien pesan, y esto es justamente lo
				// que el feedback pedia: lo mas urgente del expediente puede ser una
				// sangre en la orina, que no tiene tendencia ninguna, y no el analito que
				// mas se movio. Un modelo que solo mirara series se la perderia entera.
				foreach (Examen examen in examenes)
				{
					if (examen is ExamenDeOrina orina)
						mayor = Severidad.LaMayor(mayor, orina.SeveridadDelExamen);
					else if (examen is ExamenDeHeces heces)
						mayor = Severidad.LaMayor(mayor, heces.SeveridadDelExamen);
				}

				return mayor;
			}
		}

		// A que ritmo viene cambiando un analito. Total por la misma razon que
		// EvaluarTendenciaDe: preguntar por un analito del que no hay nada es legitimo, y
		// la respuesta es "no hay velocidad que dar", no una excepcion.
		public Velocidad VelocidadDe(string analito)
		{
			Analito cual = Analito.PorNombre(analito);

			return series.TryGetValue(cual, out SerieDeAnalito serie)
				? serie.Velocidad()
				: Examenes.Dominio.Velocidad.NoAplica(cual, "", 0m);
		}

		// Los analitos de los que hay historia, para que la superficie pueda recorrerlos
		// sin conocer la lista de clases del dominio.
		// Salen los nombres CANONICOS: es con esos que se vuelve a preguntar, y son los
		// que el enum del MCP conoce. El nombre para mostrar viaja en cada evaluacion.
		public IReadOnlyList<string> Analitos
		{
			get
			{
				var nombres = new List<string>();
				foreach (KeyValuePair<Analito, SerieDeAnalito> par in series)
					nombres.Add(par.Key.Nombre);
				nombres.Sort(StringComparer.OrdinalIgnoreCase);
				return nombres;
			}
		}

		// QUE FALTA MEDIR. Es la unica consulta de este expediente que mira el hueco y no
		// el dato: cuanto hace que no se mide cada analito, y si eso ya es demasiado para
		// EL -cada uno tiene su periodicidad, y varios la resuelven contra su ultimo
		// valor-.
		//
		// LA FECHA ENTRA POR PARAMETRO. El dominio no lee el reloj: si lo hiciera, el
		// mismo journal reproducido manana daria otra respuesta y dejaria de ser
		// reproducible. Quien tiene derecho a saber que dia es hoy es la superficie.
		//
		// Solo aparecen los analitos CON SERIE. De uno que nunca se midio no se puede
		// decir "te toca repetirlo": no hay nada que repetir, y el dominio no inventa una
		// recomendacion sobre algo que nadie pidio.
		public IReadOnlyList<ControlPendiente> ControlesPendientes(DateTime hoy)
		{
			var pendientes = new List<ControlPendiente>();

			foreach (KeyValuePair<Analito, SerieDeAnalito> par in series)
			{
				ExamenDeSangre ultimo = par.Value.Ultimo;
				if (ultimo == null) continue;

				// Una medicion fechada DESPUES de la fecha de referencia cuenta como de
				// hoy, no como dias negativos. Pasa de verdad: el guion tiene un examen
				// del dia siguiente para demostrar el salto inverosimil, y en produccion
				// pasaria con una zona horaria o un laboratorio que fecha la validacion.
				// "Hace -1 dias que te lo hiciste" no es una respuesta.
				int dias = Math.Max(0, (int)(hoy.Date - ultimo.Fecha.Date).TotalDays);

				pendientes.Add(new ControlPendiente(
					par.Key, ultimo.Fecha.Date, dias, ultimo.MesesHastaElProximo(Edad)));
			}

			// Lo mas vencido primero: es el orden en que conviene leerlos, y evita que la
			// superficie tenga que decidirlo.
			pendientes.Sort((uno, otro) => uno.DiasQueFaltan.CompareTo(otro.DiasQueFaltan));
			return pendientes;
		}

		// EL CUADRO HISTORICO, plano y con indices. Ver CeldaDelCuadro para por que esto
		// es una decision del dominio y no un acomodo de pantalla.
		//
		// COLUMNAS: los dias distintos en que hubo algun examen, en orden. Un dia es una
		// visita al laboratorio, aunque haya traido cinco analitos a horas distintas.
		//
		// FILAS: primero las que forman serie y despues las que no, y adentro de cada
		// grupo por nombre. Es un orden ESTABLE a proposito: si las filas se ordenaran por
		// urgencia, el cuadro se reacomodaria solo cada vez que entra un examen, y una
		// tabla que se mueve deja de servir para comparar contra la vez pasada.
		//
		// Dos mediciones del mismo analito el mismo dia colapsan en una celda y gana la
		// ULTIMA por hora: es una remedicion, no dos columnas.
		public IReadOnlyList<CeldaDelCuadro> CeldasDelCuadro
		{
			get
			{
			var columnas = new List<DateTime>();
			foreach (Examen examen in examenes)
				if (!columnas.Contains(examen.Fecha.Date)) columnas.Add(examen.Fecha.Date);
			columnas.Sort();

			var filas = new List<string>();
			var porFila = new Dictionary<string, Examen>();
			foreach (Examen examen in OrdenadosParaElCuadro())
			{
				string llave = LlaveDeFila(examen);
				if (!filas.Contains(llave)) filas.Add(llave);
				porFila[llave + "|" + examen.Fecha.Date.Ticks] = examen;
			}

			var celdas = new List<CeldaDelCuadro>();
			foreach (KeyValuePair<string, Examen> par in porFila)
			{
				Examen examen = par.Value;
				string llave = LlaveDeFila(examen);

				// La lectura viaja SOLO para los que no forman serie. Los otros la piden
				// por separado, porque la suya necesita la serie completa.
				string severidad = "";
				string paraSonia = "";
				if (examen is ExamenDeOrina orina)
				{
					severidad = orina.SeveridadDelExamen.Nombre;
					paraSonia = orina.ParaSonia;
				}
				else if (examen is ExamenDeHeces heces)
				{
					severidad = heces.SeveridadDelExamen.Nombre;
					paraSonia = heces.ParaSonia;
				}

				celdas.Add(new CeldaDelCuadro(
					filas.IndexOf(llave),
					columnas.IndexOf(examen.Fecha.Date),
					llave,
					examen.Nombre,
					examen.Fecha.Date,
					examen.ResultadoExpresado,
					examen.AportaSerie,
					severidad,
					paraSonia));
			}

			celdas.Sort((una, otra) => una.Fila == otra.Fila
				? una.Columna.CompareTo(otra.Columna)
				: una.Fila.CompareTo(otra.Fila));

				return celdas;
			}
		}

		// La llave de la fila. Para los de sangre es el nombre CANONICO del analito, y eso
		// importa: es la llave con la que la superficie pide despues la interpretacion de
		// esa fila. Si fuera el nombre del examen, un ColesterolDetallado -que se llama
		// "Colesterol HDL/LDL"- no encontraria su lectura.
		private static string LlaveDeFila(Examen examen) =>
			examen is ExamenDeSangre deSangre ? deSangre.Analito.Nombre : examen.Nombre;

		// Series primero, y adentro por nombre. Estable e independiente del orden en que
		// llegaron los eventos.
		private List<Examen> OrdenadosParaElCuadro()
		{
			var ordenados = new List<Examen>(examenes);
			ordenados.Sort((uno, otro) =>
			{
				if (uno.AportaSerie != otro.AportaSerie) return uno.AportaSerie ? -1 : 1;

				int porNombre = string.Compare(LlaveDeFila(uno), LlaveDeFila(otro),
					StringComparison.OrdinalIgnoreCase);
				return porNombre != 0 ? porNombre : uno.Fecha.CompareTo(otro.Fecha);
			});
			return ordenados;
		}

		// Cuantos analitos estan hoy fuera de su rango de referencia para la edad del
		// paciente. Es un conteo derivado, no un campo que pueda quedar desincronizado.
		public int AnalitosFueraDeRango
		{
			get
			{
				int fuera = 0;
				foreach (KeyValuePair<Analito, SerieDeAnalito> par in series)
				{
					ExamenDeSangre ultimo = par.Value.Ultimo;
					if (ultimo != null && !ultimo.EsNormalPara(Edad)) fuera++;
				}
				return fuera;
			}
		}
	}
}
