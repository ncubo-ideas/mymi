using System;

namespace DealershipCar.Dominio
{
	// DONDE ESTA PARADA UNA OPERACION. Conjunto cerrado del canon (§3), la forma de E36: siete
	// estados, ctor privado, comparados por referencia, sin igualdad escrita.
	//
	// ES EL UNICO DE LOS SEIS QUE NO TIENE FRONTERA, y no es un olvido: la tabla §3.1 del plan
	// lo marca "se elige adentro". Nadie de afuera dice en que estado esta una operacion --
	// eso lo decide la regla del intervalo contra los hechos recibidos. Un DueStateCode seria
	// una puerta para que el wire escribiera el resultado en vez de los hechos, y con eso el
	// replay dejaria de reconstruir la misma verdad. Hay una prueba que afirma la ausencia.
	//
	// Y NO SE ENTREGA NUNCA (E24). Hacia afuera sale una PROYECCION ESCALAR --
	// ReadDueState(kind) devuelve este texto -- y no el objeto. Entregar el objeto invita a
	// que quien llama ramifique sobre los internos en vez de preguntarle al agregado; el
	// agregado ya sabe cuantas estan vencidas y cual sigue.
	//
	// El canon PROHIBE llamar a esto "Status" (§2). No es sinonimo: un status es cualquier
	// cosa; esto es la posicion de una operacion respecto de SU vencimiento.
	//
	// Completed y Superseded son TERMINALES: la instancia deja de actuar y su sucesora es otra
	// ocurrencia (E29). Que sean terminales es comportamiento y vive en ScheduledOperation
	// (T4), no aca: este tipo nombra los estados, no la maquina.

	/// <summary>
	/// Donde esta parada una operacion respecto de su vencimiento. Conjunto cerrado: existen
	/// exactamente estos siete estados. Se elige siempre adentro -- ningun codigo del wire lo
	/// nombra -- y sale solo como proyeccion escalar de su nombre, nunca como objeto.
	/// </summary>
	internal sealed class DueState
	{
		/// <summary>Todavia no llego ni al aviso: ni tiempo ni distancia se acercaron al limite.</summary>
		internal static readonly DueState NotDue = new DueState("NotDue");

		/// <summary>
		/// Dentro del anticipo de cortesia (DueSoonLead). Es un aviso, nunca una parada
		/// obligatoria: si el vencimiento se alcanza en la misma evaluacion, se salta este
		/// estado y se va derecho a Due (L10).
		/// </summary>
		internal static readonly DueState DueSoon = new DueState("DueSoon");

		/// <summary>
		/// Vencida: se alcanzo el limite que su base manda. El valor del limite es inclusivo.
		/// </summary>
		internal static readonly DueState Due = new DueState("Due");

		/// <summary>
		/// Pasada de vencida mas alla de la gracia (OverdueGrace). Es lo que cuenta como
		/// mantenimiento no hecho.
		/// </summary>
		internal static readonly DueState Overdue = new DueState("Overdue");

		/// <summary>
		/// La persona decidio dejarla para despues, dentro de la ventana y del tope de
		/// aplazamientos (L6). Cuando la ventana se cumple vuelve a Due, y la cuenta de
		/// aplazamientos usados se conserva.
		/// </summary>
		internal static readonly DueState Deferred = new DueState("Deferred");

		/// <summary>
		/// Hecha. TERMINAL para esta instancia: la siguiente ocurrencia se calcula desde el
		/// instante y la lectura REALES de la completacion (L4), no desde la grilla teorica.
		/// </summary>
		internal static readonly DueState Completed = new DueState("Completed");

		/// <summary>
		/// Reemplazada por una revision del plan, o quitada por la persona si era de ella.
		/// TERMINAL: una revision SUSTITUYE, nunca duplica (L5), y la cadena hacia atras es el
		/// historial.
		/// </summary>
		internal static readonly DueState Superseded = new DueState("Superseded");

		/// <summary>
		/// El nombre del estado. Es lo unico que cruza hacia afuera, como escalar: el objeto
		/// no se entrega (E24).
		/// </summary>
		internal string Name { get; }

		private DueState(string name)
		{
			Name = name;
		}
	}
}
