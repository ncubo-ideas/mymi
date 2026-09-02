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
	internal abstract class SerieDeAnalito
	{
		// La identidad del analito, no su nombre. Es lo que hace que dos hojas que
		// escriben "HEMOGLOBINA" y "HGB" caigan en ESTA serie y no en dos.
		internal Analito Analito { get; private set; }

		protected SerieDeAnalito(Analito analito)
		{
			ArgumentNullException.ThrowIfNull(analito);
			Analito = analito;
		}

		// Cuantos puntos tiene la serie. Las pruebas NO REALIZADAS no son puntos: un
		// laboratorio que no corrio los nitritos no midio "negativo".
		internal abstract int Cantidad { get; }

		internal bool HayMediciones => Cantidad > 0;

		// Cuando fue la ultima vez que se midio. Es lo que la consulta que mira el HUECO
		// necesita, y lo unico de la serie que esa consulta necesita.
		internal abstract DateTime FechaDeLaUltima { get; }

		// CADA CUANTO conviene repetir este analito, en meses. Varios lo resuelven contra
		// su ultimo resultado: un colesterol en rango se revisa cada cinco anos y uno alto
		// todos los anos, y una sangre oculta positiva no espera al ano siguiente.
		//
		// ⚠️ Las periodicidades son de uso corriente y ORIENTATIVAS.
		internal abstract int MesesHastaElProximo(Paciente paciente);

		// Si lo ULTIMO que se midio esta fuera de lo normal para este analito. En una
		// continua es salirse del rango; en un binario es que el hallazgo este presente.
		//
		// Se pregunta asi y no "esta fuera de rango" porque un binario no tiene rango, y
		// sin embargo la pregunta -"hay algo que no deberia estar?"- es la misma.
		internal abstract bool UltimaEsHallazgo(Paciente paciente);

		// LO QUE ESTA SERIE CONCLUYE DE SI MISMA.
		//
		// NO ES VIRTUAL, y ese es el cambio de esta correccion. Antes cada serie contestaba
		// entera por su cuenta; ahora hay dos preguntas que van ANTES y que ninguna serie
		// puede saltearse, porque no son de la serie sino de la persona:
		//
		//   1. este analito, ¿existe para esta persona?   -> NoAplica / RequiereSexo
		//   2. de esta persona, ¿se sabe lo que hace       -> RequiereSexo
		//      falta para leerlo?
		//
		// Recien despues corre Leer(), que es donde cada analito pone SU criterio: el
		// antigeno mira su linea base, el colesterol mira su meta, la hemoglobina mira el
		// grado de anemia, la sangre en heces mira cuantas veces seguidas aparecio.
		//
		// Que las dos guardas vivan aca y no en cada subclase es lo que hace que la regla
		// sea imposible de olvidar: agregar un analito nuevo NO puede saltarse la
		// aplicabilidad, porque el metodo que se implementa ya corre despues de ella. Es la
		// misma jugada que la guarda de "todo consejo con plazo" en el constructor de
		// Interpretacion: por construccion, no por disciplina.
		internal Interpretacion Interpretar(Paciente paciente)
		{
			ArgumentNullException.ThrowIfNull(paciente);

			Aplicabilidad aplica = Analito.AplicaA(paciente);

			if (aplica.EsNoAplica) return Interpretacion.NoAplica(SinLectura());

			if (aplica.EsIndeterminada)
				return Interpretacion.RequiereSexo(SinLectura(),
					$"sin saber tu sexo no puedo decir si el {Analito.ParaMostrar.ToLowerInvariant()} "
					+ "te corresponde");

			if (!SeSabeLeerPara(paciente))
				return Interpretacion.RequiereSexo(SinLectura(),
					$"el {Analito.ParaMostrar.ToLowerInvariant()} se compara contra rangos distintos "
					+ "según el sexo, y de vos todavía no lo sé");

			return Leer(paciente);
		}

		// LA LECTURA PROPIA DE CADA SERIE, con su criterio. Se alcanza solo cuando el
		// analito le corresponde a la persona Y se sabe leerlo, asi que aca adentro no hay
		// que volver a preguntar por el sexo.
		protected abstract Interpretacion Leer(Paciente paciente);

		// SI DE ESTA PERSONA SE SABE LO QUE HACE FALTA para leer esta serie. Casi todas
		// contestan que si —lo que necesitan es la serie, no la persona— y lo overridean
		// las dos que se comparan contra un rango que depende del sexo.
		//
		// Es una pregunta DISTINTA de la aplicabilidad, y por eso son dos metodos y no un
		// tri-estado mas: alla el analito no existe para esta persona, aca existe y no se
		// puede juzgar todavia. La superficie los muestra distinto —uno no tiene tarjeta,
		// el otro tiene tarjeta con valor y sin veredicto— y confundirlos volveria a
		// aplanar los tres efectos del sexo en uno.
		protected virtual bool SeSabeLeerPara(Paciente paciente) => true;

		// Los hechos que acompanan a un veredicto que no es una lectura. Llevan el analito
		// y cuantas mediciones hay —eso es cierto y es util— y ninguna conclusion.
		private HechosDeLaSerie SinLectura() => EvaluacionDeTendencia.NoAplica(Analito, Cantidad);

		// COMO SE LEE esta serie, y por lo tanto como se dibuja. Ver FormaDeLectura para
		// por que la forma es dominio y no estilo.
		internal abstract FormaDeLectura Forma { get; }

		// LAS LINEAS QUE IMPORTAN para dibujar esa forma. Cada serie emite SOLO las que
		// tiene: la del antigeno su linea base, la del colesterol su meta y su partida, la
		// de la hemoglobina su piso y su techo. Nadie devuelve cero para rellenar, y una
		// serie binaria no emite ninguna porque no hay linea que trazar sobre un positivo.
		internal abstract IReadOnlyList<ReferenciaDeLectura> Referencias(Paciente paciente);

		// CONTRA QUE SE LEE el ultimo valor, en una frase corta: "hasta 4.5 ng/mL",
		// "entre 13 y 17 g/dL", "lo esperable es negativo". Es la referencia que
		// acompana a todo numero en pantalla —punto 5 de la §5 de las especificaciones—
		// y la contesta la serie porque es quien sabe de que depende: la edad, el sexo
		// y, en la hemoglobina, la altitud de residencia. Con el sexo desconocido lo DICE en
		// vez de callarse: "depende de tu sexo" es una referencia honesta, un rango
		// masculino puesto por defecto no lo seria.
		//
		// Vacia cuando no hay mediciones: sin valor que acompanar no hay referencia que
		// inventar.
		internal abstract string ReferenciaLegible(Paciente paciente);
	}
}
