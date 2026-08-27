using System;

namespace Examenes.Dominio
{
	// LA LECTURA DEL ANALITO sobre su propia serie. No es lo mismo que Tendencia, y la
	// diferencia es el punto de todo este cambio:
	//
	//   Tendencia  dice un HECHO neutro:  "en descenso"
	//   Veredicto  dice que SIGNIFICA:    "mejorando", porque en el colesterol bajar es
	//                                      hacia donde conviene
	//
	// Un colesterol que baja de 320 a 280 y una hemoglobina que baja de 14 a 11 tienen
	// la misma Tendencia y veredictos opuestos. Con un solo concepto habia que elegir
	// cual de los dos mentir, y la app elegia mentir en el colesterol: le ponia
	// "EN ASCENSO" y el mismo consejo que al antigeno.
	//
	// LOS CUATRO QUE FALTAN. El material de laboratorio real da otros cuatro veredictos,
	// y ninguno es expresable en una tabla plana:
	//
	//   NoComparable   metodos o calibraciones distintas entre dos puntos de la serie
	//   Confundido     hay un factor preanalitico que explica el valor
	//   NoConfiable    condicion de validez incumplida (un LDL calculado con TG altos)
	//   RequiereAjuste el valor debe leerse ajustado por medicacion o altitud
	//
	// Los cuatro necesitan que Examen cargue metodo analitico y condicion preanalitica,
	// que todavia no existe. Quedan NOMBRADOS y no construidos, igual que las cuatro
	// formas de Magnitud que tampoco se implementaron: es lo que hace que el modelo se
	// lea rico sin inflar el alcance.
	public sealed class Veredicto
	{
		// Menos de dos mediciones. No se inventa una lectura, y este estado hay que
		// preservarlo: es lo que Alvaro marco como correcto en la hemoglobina.
		public static readonly Veredicto Indeterminado = new Veredicto("indeterminado");

		// Se movio menos de lo que este analito considera un cambio real.
		public static readonly Veredicto Estable = new Veredicto("estable");

		// Se mueve hacia donde conviene. PUEDE ESTAR FUERA DE RANGO Y SER ESTE: es el
		// caso del colesterol bajando desde un pico, y es la razon de que el veredicto
		// no se pueda derivar del rango.
		public static readonly Veredicto Mejorando = new Veredicto("mejorando");

		// Se mueve hacia donde no conviene.
		public static readonly Veredicto Empeorando = new Veredicto("empeorando");

		// El cambio es demasiado grande para el tiempo transcurrido. No es un hallazgo
		// clinico: es una medicion que hay que repetir.
		public static readonly Veredicto ProbableErrorDeMedicion =
			new Veredicto("probable error de medicion");

		// HUBO un pico, se repitio el examen a los pocos dias, y el valor volvio a lo de
		// siempre. El pico NO ERA. Es buena noticia, y merece nombre propio porque sin el
		// la unica forma de contarlo seria "probable error de medicion", que es otra cosa:
		// ahi nadie confirmo nada, aca la segunda medicion es justamente la confirmacion.
		//
		// Alvaro: "pasó de 3 a casi 4, y cuando lo vuelve a medir pasa otra vez a 3. Ahí al
		// médico esa segunda lectura le sirve."
		public static readonly Veredicto PicoNoConfirmado = new Veredicto("pico no confirmado");

		// APARECIO UN HALLAZGO BINARIO, se repitio la prueba, y no volvio a aparecer. Es
		// el hermano de PicoNoConfirmado para la forma 5, y son DOS y no uno porque no
		// dicen lo mismo: un pico es un valor que subio y volvio a su nivel, un hallazgo
		// es algo que estaba y despues no estaba. La persona lee dos cosas distintas, y
		// de un nitrito no se puede decir que "el pico no se confirmo" — no hay pico.
		//
		// Es la mejor noticia que da una serie binaria: sin ella, una sangre en la orina
		// que aparecio una vez se sigue leyendo como "buscá atención médica pronto" para
		// siempre.
		public static readonly Veredicto HallazgoNoConfirmado =
			new Veredicto("hallazgo no confirmado");

		public string Nombre { get; private set; }

		private Veredicto(string nombre)
		{
			Nombre = nombre;
		}

		public bool EsMejorando => ReferenceEquals(this, Mejorando);

		public bool EsEmpeorando => ReferenceEquals(this, Empeorando);

		public bool HayLectura => !ReferenceEquals(this, Indeterminado);

		public bool SugiereRepetirElExamen => ReferenceEquals(this, ProbableErrorDeMedicion);

		// El pico ya se repitio y no se confirmo: no hay nada mas que hacer.
		public bool EsPicoDescartado => ReferenceEquals(this, PicoNoConfirmado);

		// Se repitio y no se confirmo, en cualquiera de las dos formas. Es lo que la
		// superficie pregunta cuando quiere pintar la buena noticia sin saber si tiene
		// enfrente una magnitud continua o un binario.
		public bool SeRepitioYNoSeConfirmo =>
			ReferenceEquals(this, PicoNoConfirmado) || ReferenceEquals(this, HallazgoNoConfirmado);
	}
}
