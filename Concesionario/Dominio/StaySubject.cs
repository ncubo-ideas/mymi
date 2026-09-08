using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La linea que dice, con las palabras de la sucursal, por que el vehiculo entro al taller.
	/// Texto recibido: no es un conjunto cerrado y este registro no tiene catalogo de motivos.
	/// </summary>
	internal sealed class StaySubject
	{
		/// <summary>
		/// El asunto tal como llego. En blanco es un defecto de quien llama: no hay asunto por
		/// defecto que poner en su lugar, porque el motivo lo sabe quien recibio el carro.
		/// </summary>
		internal StaySubject(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				throw new DomainException(
					"Una estadia sin asunto no dice por que entro el vehiculo, y eso lo dice la "
					+ "sucursal con sus palabras.");

			Value = text;
		}

		/// <summary>Como se dice el asunto hacia afuera: el texto recibido, sin tocar.</summary>
		internal string Value { get; }
	}
}
