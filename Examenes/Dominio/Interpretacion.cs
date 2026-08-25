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
	// La razon es concreta. La app tenia escrito "Conviene que lo vea tu medico" adentro
	// del .razon, igual para el antigeno que para el colesterol, y una pantalla no tiene
	// de donde sacar si un hallazgo espera a la proxima cita o no. Ese es el consejo de
	// plantilla que habia que sacar.
	//
	// Las dos posiciones conviven porque el reparto es distinto del que parece:
	//
	//   Accion    NO se compone: sale de Severidad, que es un conjunto cerrado de cuatro
	//             textos fijos. La pantalla no puede inventar una accion nueva.
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

		// Que hacer. Sale de Severidad, no se escribe aca: es la garantia de que no hay
		// una accion inventada por analito. Vacio cuando no hay nada que hacer.
		public string Accion => Severidad.QueHacer;

		// Los hechos con los que se llego a esto, para que la superficie pueda mostrarlos
		// sin volver a pedirlos y para que la conclusion sea auditable.
		public EvaluacionDeTendencia Hechos { get; private set; }

		public Interpretacion(Veredicto veredicto, Severidad severidad, string paraSonia,
			EvaluacionDeTendencia hechos)
		{
			ArgumentNullException.ThrowIfNull(veredicto);
			ArgumentNullException.ThrowIfNull(severidad);
			ArgumentNullException.ThrowIfNullOrWhiteSpace(paraSonia);
			ArgumentNullException.ThrowIfNull(hechos);

			Veredicto = veredicto;
			Severidad = severidad;
			ParaSonia = paraSonia;
			Hechos = hechos;
		}

		// El caso sin serie, que es identico para todos los analitos y por eso se arma
		// una sola vez: con una medicion no se afirma tendencia. Preservarlo es explicito
		// en el feedback.
		public static Interpretacion SinSerie(EvaluacionDeTendencia hechos)
		{
			ArgumentNullException.ThrowIfNull(hechos);

			string texto = hechos.CantidadDeMediciones == 1
				? "una sola medición: todavía no hay serie para decir para dónde viene"
				: "todavía no hay mediciones de este analito";

			return new Interpretacion(Veredicto.Indeterminado, Severidad.Informativo, texto, hechos);
		}

		// Proyecciones escalares, para que el print del DSL no tenga que navegar dos
		// niveles y la superficie no tenga que abrir los objetos.
		public string Analito => Hechos.Analito;
		public string AnalitoParaMostrar => Hechos.AnalitoParaMostrar;
		public string NombreDelVeredicto => Veredicto.Nombre;
		public string NombreDeLaSeveridad => Severidad.Nombre;
		public bool PideAlgo => Severidad.PideAlgo;
		public bool SugiereRepetirElExamen => Veredicto.SugiereRepetirElExamen;
	}
}
