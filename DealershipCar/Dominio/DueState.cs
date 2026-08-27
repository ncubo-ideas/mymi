using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// Donde esta parada una operacion respecto de su vencimiento. Conjunto cerrado de siete. Se
	/// elige siempre adentro -- ningun codigo del wire lo nombra -- y sale solo como proyeccion
	/// escalar de su nombre, nunca como objeto.
	/// </summary>
	internal sealed class DueState
	{
		/// <summary>Todavia no llego ni al aviso: ni tiempo ni distancia se acercaron al limite.</summary>
		internal static readonly DueState NotDue = new DueState("NotDue");

		/// <summary>
		/// Dentro del anticipo de cortesia. Si el vencimiento se alcanza en la misma evaluacion se
		/// salta y va derecho a Due.
		/// </summary>
		internal static readonly DueState DueSoon = new DueState("DueSoon");

		/// <summary>Vencida: se alcanzo el limite que su base manda, que es inclusivo.</summary>
		internal static readonly DueState Due = new DueState("Due");

		/// <summary>Pasada de vencida mas alla de la gracia. Cuenta como mantenimiento no hecho.</summary>
		internal static readonly DueState Overdue = new DueState("Overdue");

		/// <summary>
		/// Dejada para despues, dentro de la ventana y del tope. Cumplida la ventana
		/// vuelve a Due y la cuenta usada se conserva.
		/// </summary>
		internal static readonly DueState Deferred = new DueState("Deferred");

		/// <summary>
		/// Hecha. Terminal: la siguiente ocurrencia se mide desde la completacion real.
		/// </summary>
		internal static readonly DueState Completed = new DueState("Completed");

		/// <summary>
		/// Reemplazada por una revision del plan, o quitada por la persona. Terminal: una revision
		/// sustituye, nunca duplica, y la cadena hacia atras es el historial.
		/// </summary>
		internal static readonly DueState Superseded = new DueState("Superseded");

		/// <summary>El nombre. Es lo unico que cruza hacia afuera; el objeto no se entrega.</summary>
		internal string Name { get; }

		private DueState(string name)
		{
			Name = name;
		}
	}
}
