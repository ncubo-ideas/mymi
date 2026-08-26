using System;
using System.Collections.Generic;

namespace Examenes.Dominio
{
	// LA HISTORIA DE UN ANALITO: lo que se midio de el, ordenado en el tiempo.
	//
	// ESTA CLASE SE QUEDO CON MUY POCO, y esa es la correccion. Hasta aca era UNA sola
	// clase con una lista de ExamenDeSangre adentro, o sea que "la serie de un analito"
	// significaba, en realidad, "la serie de un analito cuantitativo de sangre" — la misma
	// clase de cosa que el feedback viene senalando, en el ultimo lugar donde quedaba.
	//
	// La consecuencia era concreta: una serie de nitritos -negativo, negativo, positivo-
	// no se podia tener, aunque §6.5 del brief la anticipa desde el principio y aunque
	// desde el cambio anterior los nitritos YA son un analito con identidad propia.
	//
	// LAS DOS RAMAS, y por que son dos:
	//
	//   SerieContinua   ordena, resta y divide por dias. Tiene tendencia y velocidad.
	//   SerieBinaria    no ordena ni resta. Tiene APARICION y PERSISTENCIA.
	//
	// "Negativo -> positivo" no es un ascenso de nada: no hay mas positivo ni menos
	// positivo. Forzar a las dos a devolver EvaluacionDeTendencia obligaria al binario a
	// mandar null en Primero y cero en Cambio, que es la misma mentira que Magnitud evita
	// al no declarar un decimal en la raiz.
	//
	// LO QUE SI TIENE TODA SERIE, y por eso vive aca: de que analito es, cuantos puntos
	// tiene, cuando fue el ultimo, cada cuanto conviene repetirlo, si lo ultimo que se
	// vio es un hallazgo, que concluye de si misma y como se dibuja. Son las preguntas
	// que el Expediente hace sin saber que forma tiene enfrente.
	public abstract class SerieDeAnalito
	{
		// La identidad del analito, no su nombre. Es lo que hace que dos hojas que
		// escriben "HEMOGLOBINA" y "HGB" caigan en ESTA serie y no en dos.
		public Analito Analito { get; private set; }

		protected SerieDeAnalito(Analito analito)
		{
			ArgumentNullException.ThrowIfNull(analito);
			Analito = analito;
		}

		// Cuantos puntos tiene la serie. Las pruebas NO REALIZADAS no son puntos: un
		// laboratorio que no corrio los nitritos no midio "negativo".
		public abstract int Cantidad { get; }

		public bool HayMediciones => Cantidad > 0;

		// Cuando fue la ultima vez que se midio. Es lo que la consulta que mira el HUECO
		// necesita, y lo unico de la serie que esa consulta necesita.
		public abstract DateTime FechaDeLaUltima { get; }

		// CADA CUANTO conviene repetir este analito, en meses. Varios lo resuelven contra
		// su ultimo resultado: un colesterol en rango se revisa cada cinco anos y uno alto
		// todos los anos, y una sangre oculta positiva no espera al ano siguiente.
		//
		// ⚠️ Las periodicidades son de uso corriente y ORIENTATIVAS.
		public abstract int MesesHastaElProximo(int edad);

		// Si lo ULTIMO que se midio esta fuera de lo normal para este analito. En una
		// continua es salirse del rango; en un binario es que el hallazgo este presente.
		//
		// Se pregunta asi y no "esta fuera de rango" porque un binario no tiene rango, y
		// sin embargo la pregunta -"hay algo que no deberia estar?"- es la misma.
		public abstract bool UltimaEsHallazgo(int edad);

		// LO QUE ESTA SERIE CONCLUYE DE SI MISMA. Es lo unico comun, y cada subclase lo
		// contesta con su criterio: el antigeno mira su linea base, el colesterol mira su
		// meta, la hemoglobina mira el grado de anemia, la sangre en heces mira cuantas
		// veces seguidas aparecio. No hay algoritmo generico que reciba umbrales y
		// devuelva el mismo veredicto para todos.
		public abstract Interpretacion Interpretar(int edad);

		// COMO SE LEE esta serie, y por lo tanto como se dibuja. Ver FormaDeLectura para
		// por que la forma es dominio y no estilo.
		public abstract FormaDeLectura Forma { get; }

		// LAS LINEAS QUE IMPORTAN para dibujar esa forma. Cada serie emite SOLO las que
		// tiene: la del antigeno su linea base, la del colesterol su meta y su partida, la
		// de la hemoglobina su piso y su techo. Nadie devuelve cero para rellenar, y una
		// serie binaria no emite ninguna porque no hay linea que trazar sobre un positivo.
		public abstract IReadOnlyList<ReferenciaDeLectura> Referencias(int edad);
	}
}
