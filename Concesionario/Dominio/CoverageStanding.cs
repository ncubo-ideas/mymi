using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La respuesta a "esta cubierto esto". Conjunto cerrado de cinco, porque la pregunta tiene
	/// cinco respuestas honestas y un booleano tiene dos: colapsarlas es como una app termina
	/// diciendole a alguien que esta cubierto cuando lo cierto era que hay una exclusion que
	/// deberia leer, o que nadie publico nada. Se elige siempre adentro -- es una RESPUESTA,
	/// nunca una entrada -- y sale solo como proyeccion escalar de su nombre.
	/// </summary>
	internal sealed class CoverageStanding
	{
		/// <summary>
		/// Cubierto: el sistema esta en el alcance publicado y el vehiculo esta dentro de los
		/// limites que rigen.
		/// </summary>
		internal static readonly CoverageStanding Covered = new CoverageStanding("Covered", "Cubierto");

		/// <summary>
		/// Cubierto, y hay una exclusion publicada cuya condicion aplica. La exclusion se nombra
		/// junto con la respuesta: callarla es la peor respuesta posible a una pregunta de
		/// cobertura.
		/// </summary>
		internal static readonly CoverageStanding CoveredWithExclusion =
			new CoverageStanding("CoveredWithExclusion", "Cubierto con exclusión");

		/// <summary>
		/// No cubierto: el alcance publicado nombra el sistema y no lo cubre. Es una respuesta
		/// sobre el alcance, no la adjudicacion de un reclamo.
		/// </summary>
		internal static readonly CoverageStanding NotCovered =
			new CoverageStanding("NotCovered", "No cubierto");

		/// <summary>
		/// Fuera de los limites que rigen -- los propios del sistema si los tiene, y si no los del
		/// vehiculo. Estar afuera decide antes que cualquier exclusion.
		/// </summary>
		internal static readonly CoverageStanding OutsideEnvelope =
			new CoverageStanding("OutsideEnvelope", "Fuera de los límites");

		/// <summary>
		/// No nos lo dijeron: el sistema no esta en el vocabulario que el fabricante publico. NO
		/// es lo mismo que no cubierto, y esa diferencia es el punto entero del conjunto.
		/// </summary>
		internal static readonly CoverageStanding NotPublished =
			new CoverageStanding("NotPublished", "Sin publicar");

		/// <summary>El nombre. Es lo unico que cruza hacia afuera; el objeto no se entrega.</summary>
		internal string Name { get; }

		/// <summary>
		/// Como se dice esta respuesta en la frase que alguien lee. NO es lo que cruza la puerta:
		/// eso sigue siendo <see cref="Name"/>.
		/// </summary>
		internal string HowItIsSaid { get; }

		private CoverageStanding(string name, string howItIsSaid)
		{
			Name = name;
			HowItIsSaid = howItIsSaid;
		}
	}
}
