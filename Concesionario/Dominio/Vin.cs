using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La identidad del vehiculo del que es una agenda: hay una
	/// <see cref="MaintenanceSchedule"/> por Vin. Nada en este slice compara dos, asi
	/// que no lleva igualdad escrita.
	/// </summary>
	internal sealed class Vin
	{
		/// <summary>Una identidad en blanco no dice de que vehiculo es la agenda.</summary>
		internal Vin(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				throw new DomainException(
					"Una agenda de mantenimiento sin Vin no es una agenda sin nombre todavia: es "
					+ "una que no se puede decir de que vehiculo es, y la agenda se identifica "
					+ "por ahi.");

			Value = value;
		}

		/// <summary>Como se dice esta identidad hacia afuera.</summary>
		internal string Value { get; }
	}
}
