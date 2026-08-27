using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La identidad del modelo del que es una especificacion: hay una por Model, y muchos
	/// vehiculos comparten uno. Es lo que separa a este agregado de todos los demas, que se
	/// identifican por Vin. Nada en este slice compara dos, asi que no lleva igualdad escrita.
	/// </summary>
	internal sealed class Model
	{
		/// <summary>Una identidad en blanco no dice de que modelo es la especificacion.</summary>
		internal Model(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				throw new DomainException(
					"Una especificacion sin modelo no es una especificacion sin nombre todavia: es "
					+ "una que no se puede decir de que modelo es, y la especificacion se identifica "
					+ "por ahi.");

			Value = value;
		}

		/// <summary>Como se dice esta identidad hacia afuera.</summary>
		internal string Value { get; }
	}
}
