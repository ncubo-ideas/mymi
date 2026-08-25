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
