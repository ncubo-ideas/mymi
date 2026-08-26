using System;
using System.Collections.Generic;

namespace Examenes.Dominio
{
	// Un examen que el laboratorio le hizo al paciente, en una fecha.
	//
	// Es abstracta a proposito y NO declara un valor medido. Esa es la decision de
	// modelado que ordena todo el resto: un examen de orina no tiene un numero, tiene
	// un color, y forzar a los dos a exponer "el valor" obligaria a que uno de los dos
	// mienta.
	//
	// Lo que TODO examen tiene es fecha y nombre. Lo que solo tienen los cuantitativos
	// - medicion, unidad, rango de referencia, tendencia - vive en ExamenDeSangre. Lo
	// que solo tiene la orina - el color - vive en ExamenDeOrina.
	//
	// No es sealed: existe para que se herede.
	public abstract class Examen
	{
		public DateTime Fecha { get; private set; }

		protected Examen(DateTime fecha)
		{
			if (fecha == default)
				throw new ArgumentException("Un examen sin fecha no se puede ordenar en una serie.", nameof(fecha));

			Fecha = fecha;
		}

		// Como se llama este examen para el paciente. Lo define cada subclase: es su
		// identidad en la lista.
		public abstract string Nombre { get; }

		// Si este examen aporta un punto a una serie CON NUMEROS. Los de sangre si; la
		// orina y las heces no, porque ni un color ni un positivo tienen valor que graficar.
		//
		// NO ES "si aporta serie", aunque se siga llamando asi en la proyeccion que ya
		// viajo a la app. Desde que los binarios tienen serie, un urianalisis aporta dos
		// —nitritos y sangre— y no aporta la tercera —el color—, asi que la pregunta dejo
		// de tener una sola respuesta a nivel de examen. Quien la contesta bien es cada
		// lectura: ver LecturaDeExamen.AportaSerie, que es la que usa el cuadro.
		//
		// Lo que este bool sigue diciendo con verdad es que el examen ENTERO sea un punto
		// de una serie, y eso solo pasa cuando mide una sola cosa y esa cosa es un numero.
		public abstract bool AportaSerie { get; }

		// Lo que se le muestra al paciente como resultado, ya expresado. Un numero con
		// unidad para los de sangre, un color para la orina.
		//
		// Sigue existiendo porque hay lugares que quieren UNA linea por examen -la lista
		// de la caratula de consola-, pero no es lo que describe al examen: para eso estan
		// las Lecturas.
		public abstract string ResultadoExpresado { get; }

		// QUE COSAS MIDIO ESTE EXAMEN, una por analito.
		//
		// Un examen no mide una cosa: mide las que mide. El de sangre informa un analito;
		// el urianalisis TRES y el de heces DOS. Sin esto, las tres del urianalisis
		// terminaban aplanadas en un solo string, que es la misma generalizacion que este
		// dominio saco de todos lados menos de aca.
		public abstract IReadOnlyList<LecturaDeExamen> Lecturas { get; }
	}
}
