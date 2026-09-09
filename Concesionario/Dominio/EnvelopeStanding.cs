using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Donde esta parado un vehiculo respecto del sobre de garantia que la agencia publico.
	/// Conjunto cerrado de tres, porque la pregunta tiene tres respuestas decidibles y un booleano
	/// tiene dos: colapsarlas es como una app termina diciendole a alguien que tiene garantia
	/// cuando lo cierto era que nadie publico ningun sobre. Se elige siempre adentro -- es una
	/// RESPUESTA, nunca una entrada -- y sale solo como proyeccion escalar de su nombre.
	/// <para>
	/// Ninguno de los tres quiere decir "no se puede saber": eso no es miembro de este conjunto
	/// sino una de las variantes suspendidas de <see cref="EnvelopeAnswer"/>.
	/// </para>
	/// </summary>
	internal sealed class EnvelopeStanding
	{
		/// <summary>
		/// Adentro: ninguna de las dos mitades del sobre se alcanzo al instante preguntado.
		/// </summary>
		internal static readonly EnvelopeStanding Inside = new EnvelopeStanding("Inside", "Adentro");

		/// <summary>
		/// Alcanzado: al menos una de las dos mitades llego a su limite. Cual llego lo dice la
		/// frase de la respuesta, que nombra siempre las dos.
		/// </summary>
		internal static readonly EnvelopeStanding Reached = new EnvelopeStanding("Reached", "Alcanzado");

		/// <summary>
		/// La agencia no publico ningun sobre para este vehiculo. NO es lo mismo que estar
		/// afuera, y esa diferencia es el punto entero del conjunto: el registro lo sabe con
		/// certeza, asi que es una respuesta decidida y no una suspension.
		/// </summary>
		internal static readonly EnvelopeStanding NotPublished =
			new EnvelopeStanding("NotPublished", "Sin publicar");

		/// <summary>El nombre. Es lo unico que cruza hacia afuera; el objeto no se entrega.</summary>
		internal string Name { get; }

		/// <summary>
		/// Como se dice esta respuesta en la frase que alguien lee. NO es lo que cruza la puerta:
		/// eso sigue siendo <see cref="Name"/>.
		/// </summary>
		internal string HowItIsSaid { get; }

		private EnvelopeStanding(string name, string howItIsSaid)
		{
			Name = name;
			HowItIsSaid = howItIsSaid;
		}
	}
}
