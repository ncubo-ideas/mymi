using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La linea que dice, con las palabras de quien cobro, en que se fue la plata. Texto recibido: no
	/// es un conjunto cerrado y este libro no tiene catalogo de categorias.
	/// </summary>
	internal sealed class CostSubject
	{
		/// <summary>
		/// El asunto tal como llego. En blanco es un defecto de quien llama: no hay asunto por
		/// defecto que poner en su lugar, porque en que se gasto lo sabe quien lo gasto.
		/// </summary>
		internal CostSubject(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				throw new DomainException(
					"Llego un gasto sin asunto, asi que no dice en que se fue la plata. Eso lo dice "
					+ "con sus palabras quien lo cobro, y aca no hay ninguna categoria que ponerle "
					+ "en su lugar.");

			Value = text;
		}

		/// <summary>Como se dice el asunto hacia afuera: el texto recibido, sin tocar.</summary>
		internal string Value { get; }
	}
}
