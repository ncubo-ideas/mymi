using System;

namespace Examenes.Dominio
{
	// QUE TIENE QUE HACER LA PERSONA. No que tan malo es el numero: que hacer.
	//
	// Es el pedido central del feedback de Alvaro, y el ejemplo con el que lo explico
	// no deja lugar a dudas:
	//
	//   sangre en las heces  -> puede ser algo comun, conviene revisarlo
	//   sangre en la orina   -> hay que ver a un medico pronto
	//
	// Los dos hallazgos tienen la MISMA FORMA: un binario positivo, fuera de lo normal.
	// Un modelo generico les pone la misma etiqueta y la misma frase. La accion que
	// exigen es completamente distinta, y esa distincion es conocimiento clinico del
	// analito, no una decision de pantalla.
	//
	// POR QUE EL TEXTO DE LA ACCION VIVE ACA. La app decia "Conviene que lo vea tu
	// medico" en la pantalla, con la frase escrita en el .razor, igual para el antigeno
	// que para el colesterol. Eso es la receta ciega que Alvaro rechazo cuando hablo del
	// carro: repetir la revision de frenos a los 10.000 km porque se hizo a los 5.000,
	// sin mirar si hacia falta.
	//
	// Poniendo la accion en el conjunto cerrado, la pantalla no puede inventarla: solo
	// puede mostrar la que el analito eligio. Y no es prosa libre armada en cada llamada
	// -eso si seria presentacion metida en el dominio-, son cuatro textos fijos, uno por
	// inhabitante.
	//
	// ORDENADA. Tiene peso porque hay que poder decir "lo mas urgente de todo el
	// expediente", y comparar por peso es lo unico que permite ordenar cuatro cosas que
	// no son numeros.
	public sealed class Severidad
	{
		// Un dato. No hay nada que hacer con esto, y decir que si lo hay es ruido que
		// entrena a la persona a ignorar los avisos que si importan.
		public static readonly Severidad Informativo = new Severidad("informativo", 0, "");

		// El hallazgo puede ser real o puede ser la medicion. Lo que corresponde es
		// volver a medir, NO alarmarse: es la salida de la regla del intervalo corto.
		public static readonly Severidad Seguimiento =
			new Severidad("seguimiento", 1, "conviene repetir el examen");

		// Hay algo que un medico tiene que ver, y puede esperar a la proxima cita.
		public static readonly Severidad ConsultaMedica =
			new Severidad("consulta medica", 2, "mostráselo a tu médico en la próxima cita");

		// No espera a la proxima cita.
		public static readonly Severidad AtencionInmediata =
			new Severidad("atencion inmediata", 3, "buscá atención médica pronto");

		public string Nombre { get; private set; }

		// Que hacer, ya escrito como se le muestra a la persona -con acentos, como
		// ParaMostrar en Analito-. Vacio en Informativo, y eso es una respuesta y no un
		// faltante: la mayoria de los resultados no piden ninguna accion.
		//
		// Nombre, en cambio, es la etiqueta interna y va sin acentos, igual que en
		// Tendencia: con ese texto se compara y se serializa, no se le habla a nadie.
		public string QueHacer { get; private set; }

		private readonly int peso;

		private Severidad(string nombre, int peso, string queHacer)
		{
			Nombre = nombre;
			QueHacer = queHacer;
			this.peso = peso;
		}

		public bool PideAlgo => peso > 0;

		public bool EsMasUrgenteQue(Severidad otra)
		{
			ArgumentNullException.ThrowIfNull(otra);
			return peso > otra.peso;
		}

		// La mas urgente de dos. Es lo que permite resumir un expediente entero sin que
		// la superficie tenga que conocer el orden.
		public static Severidad LaMayor(Severidad una, Severidad otra)
		{
			ArgumentNullException.ThrowIfNull(una);
			ArgumentNullException.ThrowIfNull(otra);
			return una.EsMasUrgenteQue(otra) ? una : otra;
		}
	}
}
