using System;

namespace Examenes.Dominio
{
	// LO QUE EL ANALITO CONCLUYE SOBRE SU PROPIA SERIE. Es el tipo de retorno de
	// Interpretar(), la unica operacion que todos los analitos tienen en comun.
	//
	// Que sea el analito quien la produce, y no un algoritmo generico que recibe
	// umbrales, es la resolucion del feedback de Alvaro:
	//
	//   "No se intenta que la aplicacion responda todos los servicios como si fuera la
	//    misma tabla. Cada examen tiene tablas muy distintas."
	//
	// SOBRE EL TEXTO, Y POR QUE CAMBIA UNA POSICION ANTERIOR.
	//
	// EvaluacionDeTendencia dice, y decia bien para lo que hacia, que el dominio devuelve
	// HECHOS y no frases, y que quien redacta la oracion es la superficie. Interpretacion
	// se corre de ahi a proposito, porque el feedback pide justamente lo contrario:
	//
	//   "Ningun texto de consejo se genera en la UI. Todos vienen del analito."
	//
	// Las dos posiciones conviven porque el reparto es distinto del que parece:
	//
	//   Accion    NO se compone: es un que del conjunto cerrado de Accion mas un Plazo
	//             del conjunto cerrado de Plazo. La pantalla no puede inventar ni una
	//             accion nueva ni un consejo sin plazo.
	//   ParaSonia SI la arma el analito, con SUS hechos y SU vocabulario. Es una linea,
	//             no un consejo, y es lo unico que el analito puede decir mejor que nadie.
	//
	// Y siguen viajando los hechos al lado, para que la superficie o la IA puedan decir
	// otra cosa con ellos si hace falta. La interpretacion no los reemplaza.
	//
	// Carrier por identidad: sin Equals, sin GetHashCode, no es llave de nada.
	public sealed class Interpretacion
	{
		public Veredicto Veredicto { get; private set; }

		public Severidad Severidad { get; private set; }

		// La lectura en una linea, escrita por el analito. Con acentos: se le muestra a
		// la persona tal cual.
		public string ParaSonia { get; private set; }

		// Que hacer y cuando, elegidas por el analito del conjunto cerrado. Null cuando
		// no hay nada que hacer, que es la mayoria de las veces y es una respuesta.
		public Accion Accion { get; private set; }

		// Los hechos con los que se llego a esto, para que la superficie pueda mostrarlos
		// sin volver a pedirlos y para que la conclusion sea auditable.
		// TIPADO A LA RAIZ, no a la evaluacion de tendencia. Una serie binaria concluye
		// con EvaluacionDePresencia —"apareció, 2 seguidas sobre 3 pruebas"— y no tiene
		// valor inicial, ni final, ni cambio. Dejar el campo tipado a la continua la
		// habria obligado a mandar nulls y ceros.
		public HechosDeLaSerie Hechos { get; private set; }

		// COMO SE DIBUJA esta lectura. Viaja con la interpretacion porque llega junta: la
		// superficie pide una vez y sabe que veredicto es Y que forma tiene.
		public FormaDeLectura Forma { get; private set; }

		// CONTRA QUE SE LEE el valor, ya dicho: "hasta 4.5 ng/mL", "entre 13 y 17 g/dL".
		// Es el punto 5 de la §5 de las especificaciones —ningun numero sin su
		// referencia— y NO la escribe la UI: sale del analito, que es quien sabe que
		// depende de la edad, del sexo y, en la hemoglobina, de la altitud de residencia.
		// Vacia cuando no hay valor que acompanar (un analito sin mediciones).
		public string ReferenciaLegible { get; private set; } = "";

		public Interpretacion ConForma(FormaDeLectura forma)
		{
			Forma = forma;
			return this;
		}

		public Interpretacion ConReferencia(string referenciaLegible)
		{
			ReferenciaLegible = referenciaLegible ?? "";
			return this;
		}

		public Interpretacion(Veredicto veredicto, Severidad severidad, string paraSonia,
			HechosDeLaSerie hechos, Accion accion = null)
		{
			ArgumentNullException.ThrowIfNull(veredicto);
			ArgumentNullException.ThrowIfNull(severidad);
			ArgumentNullException.ThrowIfNullOrWhiteSpace(paraSonia);
			ArgumentNullException.ThrowIfNull(hechos);

			// TODO CONSEJO CON PLAZO, y esta guarda es la que lo hace ley: una severidad
			// que pide algo no puede viajar sin decir que y cuando. Es el criterio 8 de
			// la adopcion de marca, cumplido por construccion y no por disciplina.
			if (severidad.PideAlgo && accion == null)
				throw new ArgumentNullException(nameof(accion),
					$"Una severidad '{severidad.Nombre}' pide algo, y lo que pide "
					+ "tiene que decirse con su plazo: falta la Accion.");

			Veredicto = veredicto;
			Severidad = severidad;
			ParaSonia = paraSonia;
			Hechos = hechos;
			Accion = accion;
		}

		// El caso sin serie, que es identico para todos los analitos y por eso se arma
		// una sola vez: con una medicion no se afirma tendencia. Preservarlo es explicito
		// en el feedback.
		//
		// CON UNA MEDICION HAY UNA ACCION, y tiene plazo aunque no corra: repetir el
		// examen es lo unico que convierte un punto en una serie, y cuando conviene
		// hacerlo lo dice el medico, no este dominio.
		public static Interpretacion SinSerie(HechosDeLaSerie hechos)
		{
			ArgumentNullException.ThrowIfNull(hechos);

			bool hayUna = hechos.CantidadDeMediciones == 1;
			string texto = hayUna
				? "una sola medición: todavía no hay serie para decir para dónde viene"
				: "todavía no hay mediciones de este analito";

			Accion accion = hayUna
				? Accion.RepetirElExamen(Plazo.CuandoElMedicoLoIndique)
				: null;

			return new Interpretacion(Veredicto.Indeterminado, Severidad.Informativo, texto,
				hechos, accion);
		}

		// Proyecciones escalares, para que el print del DSL no tenga que navegar dos
		// niveles y la superficie no tenga que abrir los objetos.
		public string Analito => Hechos.Analito;
		public string AnalitoParaMostrar => Hechos.AnalitoParaMostrar;
		public string NombreDelVeredicto => Veredicto.Nombre;
		public string NombreDeLaForma => Forma == null ? "" : Forma.Nombre;
		public string NombreDeLaSeveridad => Severidad.Nombre;
		public bool PideAlgo => Severidad.PideAlgo;
		public bool SugiereRepetirElExamen => Veredicto.SugiereRepetirElExamen;

		// La accion dicha entera —"mostrále esto a tu médico este mes"— o vacia cuando no
		// hay nada que hacer. Es lo unico que una pantalla imprime.
		public string AccionLegible => Accion == null ? "" : Accion.Legible;

		// COMO SE LLAMA LA LECTURA en una palabra: "en ascenso" en una continua,
		// "apareció" en un binario. Sale de los hechos, que es donde vive el conjunto
		// cerrado de cada forma, y es lo que la superficie puede pedir sin saber cual
		// tiene enfrente.
		public string NombreDeLaLectura => Hechos.NombreDeLaLectura;
	}
}
