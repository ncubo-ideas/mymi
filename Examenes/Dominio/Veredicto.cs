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

		public string Nombre { get; private set; }

		private Veredicto(string nombre)
		{
			Nombre = nombre;
		}

		public bool EsMejorando => ReferenceEquals(this, Mejorando);

		public bool EsEmpeorando => ReferenceEquals(this, Empeorando);

		public bool HayLectura => !ReferenceEquals(this, Indeterminado);

		public bool SugiereRepetirElExamen => ReferenceEquals(this, ProbableErrorDeMedicion);
	}
}
