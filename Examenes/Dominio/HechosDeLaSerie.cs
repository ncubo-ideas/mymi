using System;

namespace Examenes.Dominio
{
	// LOS HECHOS DE UNA SERIE, sin la lectura. Es la raiz de lo que Evaluar() devuelve.
	//
	// POR QUE APARECE AHORA. Hasta este cambio habia un solo tipo de hechos
	// -EvaluacionDeTendencia- y alcanzaba, porque todas las series eran de magnitudes
	// continuas: primero, ultimo, dias, cambio. Un binario no tiene nada de eso. No tiene
	// "cambio de 80"; tiene "aparecio", y eso no es un numero mas chico: es otra cosa.
	//
	// Si los binarios reutilizaran EvaluacionDeTendencia tendrian que devolver null en
	// Primero y Ultimo y cero en Cambio, que es exactamente la mentira que este dominio
	// evita en todos los otros pisos: la misma razon por la que Magnitud no declara un
	// decimal y por la que ReferenciaDeLectura es una lista y no un record con cinco
	// campos.
	//
	// LO QUE SI ES COMUN, y por eso vive aca: de que analito son los hechos, cuantas
	// mediciones los sostienen, si hay serie, y como se llama la lectura en una palabra.
	// Nada mas. La raiz es POCA a proposito.
	//
	// Carrier por identidad: sin Equals, sin GetHashCode, no es llave de nada.
	internal abstract class HechosDeLaSerie
	{
		// El nombre CANONICO del analito. Sigue siendo una cadena y no la identidad a
		// proposito: esto es un carrier de proyeccion, cruza al DSL y de ahi a JSON, y del
		// otro lado nadie puede comparar por referencia. La identidad vive en el dominio;
		// lo que sale es su nombre.
		internal string Analito { get; private set; }

		// El mismo analito escrito como se le muestra a la persona, con acentos. Va al
		// lado del canonico y no en su lugar porque son dos cosas: uno es la llave con la
		// que se pregunta, el otro es el texto que se lee.
		internal string AnalitoParaMostrar { get; private set; }

		// Cuantas mediciones sostienen estos hechos. Las NO REALIZADAS no cuentan: una
		// prueba que el laboratorio no corrio no es un punto de la serie.
		internal int CantidadDeMediciones { get; private set; }

		protected HechosDeLaSerie(Analito analito, int cantidadDeMediciones)
		{
			ArgumentNullException.ThrowIfNull(analito);

			Analito = analito.Nombre;
			AnalitoParaMostrar = analito.ParaMostrar;
			CantidadDeMediciones = cantidadDeMediciones;
		}

		internal bool HaySerie => CantidadDeMediciones >= 2;

		// LA LECTURA EN UNA PALABRA. "en ascenso" para una continua, "apareció" para un
		// binario. Es lo unico que la superficie puede pedir sin saber que forma tiene
		// enfrente, y es la razon de que esta raiz sea util y no ceremonia.
		internal abstract string NombreDeLaLectura { get; }

		// Si lo que se vio pide REPETIR la medicion antes que cualquier otra cosa. En una
		// continua es el salto inverosimil; en un binario, un positivo aislado que todavia
		// nadie confirmo.
		internal abstract bool SugiereRepetirElExamen { get; }
	}
}
