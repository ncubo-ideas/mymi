using System;

namespace Cargas.Dominio
{
	// QUE TAN SERIO ES LO QUE SE VIO. Es el semaforo de la marca, con UN solo vocabulario.
	//
	// Geaninna y Alvaro describieron LA MISMA COSA con nombres distintos -el documento de
	// marca decia "vigilar" donde el brief decia "seguimiento"- y se adoptan los de ella:
	// vienen del documento de marca y mapean directo a color.
	//
	//   sangre en las heces  -> puede ser algo comun, conviene revisarlo
	//   sangre en la orina   -> hay que ver a un medico pronto
	//
	// Los dos hallazgos tienen la MISMA FORMA: un binario positivo, fuera de lo normal.
	// Un modelo generico les pone la misma etiqueta y la misma frase. Lo que exigen es
	// completamente distinto, y esa distincion es conocimiento clinico del analito.
	//
	// LA REGLA DEL ROJO ES DE DOMINIO, no de diseno: Urgente esta RESERVADO para lo que
	// de verdad no espera (una sangre en la orina). Si se usara para el colesterol,
	// dejaria de significar algo — y por eso ningun analito continuo del demo lo produce.
	//
	// QUE HACER ya no vive aca: vive en Accion, que ademas del que lleva el PLAZO, y el
	// plazo depende del analito y de la magnitud del cambio, no solo de la severidad.
	// Esta clase se queda con lo unico que es comun a todos: el orden.
	//
	// ORDENADA. Tiene peso porque hay que poder decir "lo mas urgente de todo el
	// expediente", y comparar por peso es lo unico que permite ordenar cuatro cosas que
	// no son numeros.
	public sealed class Severidad
	{
		// Un dato: valor en rango, o algo sin accion. Decir que hay algo que hacer con
		// esto es ruido que entrena a la persona a ignorar los avisos que si importan.
		public static readonly Severidad Informativo = new Severidad("informativo", 0);

		// Fuera de rango o para mirar, sin urgencia. Cubre tambien el valor que hay que
		// volver a medir: la salida de la regla del intervalo corto.
		public static readonly Severidad Vigilar = new Severidad("vigilar", 1);

		// Amerita cita, y la accion que acompana dice el plazo explicito.
		public static readonly Severidad ConsultePronto = new Severidad("consulte pronto", 2);

		// RESERVADO para lo que de verdad es urgente (ej. sangre en orina). Es el unico
		// que la UI puede pintar de rojo.
		public static readonly Severidad Urgente = new Severidad("urgente", 3);

		// La etiqueta interna, sin acentos: con este texto se compara y se serializa, no
		// se le habla a nadie — igual que Nombre en Tendencia.
		public string Nombre { get; private set; }

		private readonly int peso;

		private Severidad(string nombre, int peso)
		{
			Nombre = nombre;
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
