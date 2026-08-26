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
		//
		// El valor es la RAIZ de las series, no la rama continua: desde este cambio el
		// diccionario tiene tambien series binarias —nitritos, las dos sangres ocultas— y
		// el Expediente les pregunta lo mismo a todas sin saber cual tiene enfrente.
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

			// LOS CONTINUOS ENTRAN POR EL EXAMEN, y no por la lectura, porque su serie
			// necesita mas que el numero: los umbrales, el rango por edad y la
			// periodicidad viven en el examen, no en el resultado.
			//
			// La serie la crea EL ANALITO, no un switch aca. Cada uno sabe cual es "su
			// plural": el antigeno devuelve SerieDeAntigenoProstatico, con sus metodos, y
			// el colesterol devuelve la suya, con otros.
			if (examen is ExamenDeSangre deSangre)
				((SerieContinua)Nueva(deSangre.Analito, deSangre.NuevaSerie)).Agregar(deSangre);

			// LOS BINARIOS ENTRAN POR LA LECTURA. Un urianalisis trae TRES cosas medidas y
			// solo dos de ellas forman serie; el examen entero no es el punto de ninguna.
			// Ahi el conocimiento clinico esta en la magnitud —el vocabulario, la
			// polaridad, la severidad— y el examen no aporta mas que la fecha.
			//
			// Antes esto no ocurria en absoluto: un examen de orina entraba a la lista y a
			// ninguna serie, asi que "cómo viene mi sangre en orina" no se podia preguntar.
			foreach (LecturaDeExamen lectura in examen.Lecturas)
				if (lectura.Resultado is MagnitudBinaria binaria)
					((SerieBinaria)Nueva(lectura.Analito, binaria.NuevaSerie))
						.Agregar(new PruebaBinaria(examen.Fecha, binaria));
		}

		// La serie de ese analito, creandola con SU propia fabrica la primera vez. El
		// Expediente no sabe que clase es: se la pide a quien si lo sabe.
		private SerieDeAnalito Nueva(Analito analito, Func<SerieDeAnalito> fabrica)
		{
			if (!series.TryGetValue(analito, out SerieDeAnalito serie))
			{
				serie = fabrica();
				series[analito] = serie;
			}
			return serie;
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

			if (!series.TryGetValue(cual, out SerieDeAnalito serie))
				return EvaluacionDeTendencia.SinSerie(cual, 0, null);

			// LA TENDENCIA SOLO EXISTE EN LAS SERIES CONTINUAS. Un binario no asciende ni
			// desciende: aparece. La consulta sigue siendo TOTAL —la superficie pregunta
			// sin saber que forma tiene enfrente— y por eso la respuesta es NoAplica y no
			// SinSerie: decirle "indeterminada" a un analito con cuatro pruebas seria
			// decirle a la persona que le faltan examenes.
			//
			// Es la misma frontera que VelocidadDe, que ya estaba: "esta pregunta es
			// general" contra "esta capacidad es de una forma sola". Para leer un binario
			// esta InterpretarSerieDe, que si contesta para todos.
			return serie is SerieContinua continua
				? continua.Evaluar()
				: EvaluacionDeTendencia.NoAplica(cual, serie.Cantidad);
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
				? serie.Interpretar(Edad).ConForma(serie.Forma)
				: Interpretacion.SinSerie(EvaluacionDeTendencia.SinSerie(cual, 0, null))
					.ConForma(FormaDeLectura.Afirmacion);
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

				// Y lo que NO forma serie tambien pesa. Hoy eso son los NOMINALES: un
				// color oscuro no tiene tendencia ninguna y sin embargo puede ser lo mas
				// urgente del expediente. Un modelo que solo mirara series se lo perderia.
				//
				// Las sangres ocultas y los nitritos YA NO entran por aca, y esa es la
				// diferencia que trae la serie binaria: su severidad la decide su historia
				// y no el examen donde aparecieron. Sumar las dos cosas taparia el caso que
				// mas importa —el positivo que no se confirmo, y que la serie baja a
				// informativo mientras el examen suelto sigue diciendo lo maximo—.
				foreach (Examen examen in examenes)
					foreach (LecturaDeExamen lectura in examen.Lecturas)
						if (lectura.Resultado is MagnitudNominal nominal)
							mayor = Severidad.LaMayor(mayor, nominal.SeveridadDelHallazgo);

				return mayor;
			}
		}

		// A que ritmo viene cambiando un analito. Total por la misma razon que
		// EvaluarTendenciaDe: preguntar por un analito del que no hay nada es legitimo, y
		// la respuesta es "no hay velocidad que dar", no una excepcion.
		public Velocidad VelocidadDe(string analito)
		{
			Analito cual = Analito.PorNombre(analito);

			// LA VELOCIDAD SOLO EXISTE EN LA SERIE DEL ANTIGENO, y esta consulta sigue
			// siendo TOTAL para que la superficie pueda preguntarle a cualquier analito sin
			// saber cual la tiene. La prueba de tipo es la frontera entre "esta es una
			// pregunta general" y "esta capacidad es de uno solo".
			return series.TryGetValue(cual, out SerieDeAnalito serie)
					&& serie is SerieDeAntigenoProstatico antigeno
				? antigeno.VelocidadDelTramo()
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
		//
		// DESDE QUE LOS BINARIOS TIENEN SERIE, esto cubre tambien la sangre oculta y los
		// nitritos, que era el limite anotado en §7 bis: "que_me_falta_medir solo cubre
		// analitos con serie, o sea solo sangre". Y la periodicidad la resuelve cada uno
		// contra su ultimo resultado: una sangre oculta positiva no espera al ano.
		public IReadOnlyList<ControlPendiente> ControlesPendientes(DateTime hoy)
		{
			var pendientes = new List<ControlPendiente>();

			foreach (KeyValuePair<Analito, SerieDeAnalito> par in series)
			{
				SerieDeAnalito serie = par.Value;
				if (!serie.HayMediciones) continue;

				// Una medicion fechada DESPUES de la fecha de referencia cuenta como de
				// hoy, no como dias negativos. Pasa de verdad: el guion tiene un examen
				// del dia siguiente para demostrar el salto inverosimil, y en produccion
				// pasaria con una zona horaria o un laboratorio que fecha la validacion.
				// "Hace -1 dias que te lo hiciste" no es una respuesta.
				int dias = Math.Max(0, (int)(hoy.Date - serie.FechaDeLaUltima.Date).TotalDays);

				pendientes.Add(new ControlPendiente(
					par.Key, serie.FechaDeLaUltima.Date, dias, serie.MesesHastaElProximo(Edad)));
			}

			// Lo mas vencido primero: es el orden en que conviene leerlos, y evita que la
			// superficie tenga que decidirlo.
			pendientes.Sort((uno, otro) => uno.DiasQueFaltan.CompareTo(otro.DiasQueFaltan));
			return pendientes;
		}

		// LOS PUNTOS DE LA SERIE, con su valor NUMERICO. El cuadro los da ya convertidos a
		// texto -"3.0 ng/mL"-, que sirve para una tabla y no para dibujar. Un grafico
		// necesita numeros.
		public IReadOnlyList<ExamenDeSangre> PuntosDe(string analito)
		{
			Analito cual = Analito.PorNombre(analito);

			// Solo las continuas tienen puntos con NUMERO. Los de un binario son fechas
			// con un si o un no, y van por PruebasDe.
			return series.TryGetValue(cual, out SerieDeAnalito serie) && serie is SerieContinua continua
				? continua.Mediciones
				: new List<ExamenDeSangre>();
		}

		// LOS PUNTOS DE UNA SERIE BINARIA: fechas con un si o un no. Es el par de PuntosDe
		// para la forma 5, y son dos consultas y no una porque no devuelven lo mismo — un
		// nitrito no tiene ValorNumerico, y pedirselo seria la mentira de siempre.
		public IReadOnlyList<PruebaBinaria> PruebasDe(string analito)
		{
			Analito cual = Analito.PorNombre(analito);

			return series.TryGetValue(cual, out SerieDeAnalito serie) && serie is SerieBinaria binaria
				? binaria.Pruebas
				: new List<PruebaBinaria>();
		}

		// LOS HECHOS DE UNA SERIE BINARIA: aparecio, persiste, cedio, y sobre cuantas
		// pruebas. Es el par de EvaluarTendenciaDe, y es total por la misma razon:
		// preguntar por un analito real del que todavia no hay nada es legitimo.
		public EvaluacionDePresencia EvaluarPresenciaDe(string analito)
		{
			Analito cual = Analito.PorNombre(analito);

			return series.TryGetValue(cual, out SerieDeAnalito serie) && serie is SerieBinaria binaria
				? binaria.Evaluar()
				: EvaluacionDePresencia.SinSerie(cual, 0, "");
		}

		// COMO SE LEE ese analito. Total: de uno sin mediciones la respuesta es que no hay
		// nada que dibujar.
		public string FormaDeLecturaDe(string analito)
		{
			Analito cual = Analito.PorNombre(analito);

			return series.TryGetValue(cual, out SerieDeAnalito serie)
				? serie.Forma.Nombre
				: FormaDeLectura.Afirmacion.Nombre;
		}

		// LAS LINEAS que hay que dibujar junto a los puntos.
		public IReadOnlyList<ReferenciaDeLectura> ReferenciasDe(string analito)
		{
			Analito cual = Analito.PorNombre(analito);

			return series.TryGetValue(cual, out SerieDeAnalito serie)
				? serie.Referencias(Edad)
				: new List<ReferenciaDeLectura>();
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

			// UNA FILA POR ANALITO, no por examen. Un urianalisis trae tres cosas medidas y
			// antes las tres caian en la misma fila, aplanadas en un string. Ahora cada
			// lectura es su propia fila, con su nombre y su resultado.
			var filas = new List<string>();
			var porFila = new Dictionary<string, LecturaEnFecha>();
			foreach (Examen examen in OrdenadosParaElCuadro())
				foreach (LecturaDeExamen lectura in examen.Lecturas)
				{
					string llave = lectura.Analito.Nombre;
					if (!filas.Contains(llave)) filas.Add(llave);
					porFila[llave + "|" + examen.Fecha.Date.Ticks] =
						new LecturaEnFecha(lectura, examen);
				}

			var celdas = new List<CeldaDelCuadro>();
			foreach (KeyValuePair<string, LecturaEnFecha> par in porFila)
			{
				Examen examen = par.Value.Examen;
				LecturaDeExamen lectura = par.Value.Lectura;
				string llave = lectura.Analito.Nombre;

				// La lectura viaja SOLO para los que no forman serie, que desde este
				// cambio son unicamente los NOMINALES: un color no tiene historia que
				// consultar, asi que lo que se puede decir de el se dice mirando la celda.
				//
				// Antes viajaba la severidad del EXAMEN ENTERO, que mezclaba el color con
				// los binarios que van al lado. Ahora esa mezcla seria incorrecta: la
				// lectura de una sangre oculta ya no sale del examen donde aparecio, sale
				// de su serie —y puede ser MENOR, cuando el positivo no se confirmo—.
				string severidad = "";
				string paraSonia = "";
				if (lectura.Resultado is MagnitudNominal nominal)
				{
					severidad = nominal.SeveridadDelHallazgo.Nombre;
					paraSonia = nominal.ParaSonia;
				}

				celdas.Add(new CeldaDelCuadro(
					filas.IndexOf(llave),
					columnas.IndexOf(examen.Fecha.Date),
					llave,
					lectura.ParaMostrar,
					examen.Fecha.Date,
					lectura.Expresado,
					// LO CONTESTA LA LECTURA, no el examen que la contiene. Un urianalisis
					// trae tres cosas medidas y solo dos forman serie: preguntarselo al
					// examen le ponia la misma respuesta a las tres.
					lectura.AportaSerie,
					severidad,
					paraSonia));
			}

			celdas.Sort((una, otra) => una.Fila == otra.Fila
				? una.Columna.CompareTo(otra.Columna)
				: una.Fila.CompareTo(otra.Fila));

				return celdas;
			}
		}

		// Una lectura junto al examen del que salio. Hace falta porque la celda necesita
		// las dos cosas: el analito y el resultado vienen de la lectura, la fecha y la
		// severidad del examen.
		private sealed class LecturaEnFecha
		{
			internal LecturaDeExamen Lectura { get; }
			internal Examen Examen { get; }

			internal LecturaEnFecha(LecturaDeExamen lectura, Examen examen)
			{
				Lectura = lectura;
				Examen = examen;
			}
		}

		// Series primero, y adentro por nombre. Estable e independiente del orden en que
		// llegaron los eventos.
		private List<Examen> OrdenadosParaElCuadro()
		{
			var ordenados = new List<Examen>(examenes);
			ordenados.Sort((uno, otro) =>
			{
				if (uno.AportaSerie != otro.AportaSerie) return uno.AportaSerie ? -1 : 1;

				int porNombre = string.Compare(uno.Nombre, otro.Nombre,
					StringComparison.OrdinalIgnoreCase);
				return porNombre != 0 ? porNombre : uno.Fecha.CompareTo(otro.Fecha);
			});
			return ordenados;
		}

		// Cuantos analitos tienen HOY algo que no deberia estar. Es un conteo derivado, no
		// un campo que pueda quedar desincronizado.
		//
		// Se pregunta "es un hallazgo" y no "esta fuera de rango" porque un binario no
		// tiene rango y sin embargo la pregunta es la misma. Antes esto solo miraba
		// analitos de sangre, asi que una sangre en la orina no contaba para nada.
		public int AnalitosFueraDeRango
		{
			get
			{
				int fuera = 0;
				foreach (KeyValuePair<Analito, SerieDeAnalito> par in series)
					if (par.Value.UltimaEsHallazgo(Edad)) fuera++;
				return fuera;
			}
		}
	}
}
