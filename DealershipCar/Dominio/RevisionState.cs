using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// Donde esta parada una <see cref="PublishedRevision"/>: si todavia se esta armando, si ya
	/// contesta, o si otra la reemplazo. Conjunto cerrado de tres. Se elige siempre adentro --
	/// ningun codigo del wire lo nombra, porque lo que llega es la orden de sellar o sustituir y
	/// el estado es la consecuencia -- y sale solo como proyeccion escalar de su nombre.
	/// </summary>
	internal sealed class RevisionState
	{
		/// <summary>
		/// Se esta armando. El contenido se acumula adentro y es invisible a toda lectura: una
		/// hoja de especificaciones a medio cargar no se puede citar.
		/// </summary>
		internal static readonly RevisionState Draft = new RevisionState("Draft");

		/// <summary>
		/// Sellada: es la unica condicion en la que una revision contesta, y ya no admite
		/// contenido nuevo.
		/// </summary>
		internal static readonly RevisionState Sealed = new RevisionState("Sealed");

		/// <summary>
		/// Reemplazada por otra revision sellada, que nombra. Terminal: no vuelve a contestar,
		/// pero conserva lo que publico, asi que una respuesta que la nombro sigue siendo
		/// reproducible.
		/// </summary>
		internal static readonly RevisionState Superseded = new RevisionState("Superseded");

		/// <summary>El nombre. Es lo unico que cruza hacia afuera; el objeto no se entrega.</summary>
		internal string Name { get; }

		private RevisionState(string name)
		{
			Name = name;
		}
	}
}
