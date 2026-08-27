using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La ultima lectura del odometro con la que una regla puede medir distancia, o la constancia
	/// de que no hay ninguna. Cual lectura es usable lo decide el agregado.
	/// </summary>
	internal abstract class UsableReading
	{
		/// <summary>No hay lectura usable. Instancia unica, compartida.</summary>
		internal static readonly UsableReading None = new NoUsableReading();

		/// <summary>
		/// La lectura que existe. Un <c>null</c> es un defecto de quien llama: la ausencia se dice
		/// con <see cref="None"/>.
		/// </summary>
		internal static UsableReading Of(OdometerReading reading)
		{
			if (reading == null)
				throw new DomainException(
					"Una lectura usable en null no es la ausencia de una lectura: la ausencia se "
					+ "dice con UsableReading.None, que contesta que no hay. Un hueco no contesta "
					+ "nada, y el que lo reciba tiene que acordarse solo.");

			return new AUsableReading(reading);
		}

		/// <summary>Si hay lectura usable. Total: contesta siempre.</summary>
		internal abstract bool Exists();

		/// <summary>La lectura. Revienta si no hay: ausente no es cero.</summary>
		internal abstract OdometerReading Reading();
	}
}
