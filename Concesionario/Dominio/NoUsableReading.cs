using System;

namespace Concesionario.Dominio
{
	/// <summary>La ausencia de lectura usable.</summary>
	internal sealed class NoUsableReading : UsableReading
	{
		/// <summary>Se llega por <see cref="UsableReading.None"/>.</summary>
		internal NoUsableReading()
		{
		}

		/// <summary>No.</summary>
		internal override bool Exists()
		{
			return false;
		}

		/// <summary>Revienta: no hay lectura que devolver.</summary>
		internal override OdometerReading Reading()
		{
			throw new DomainException(
				"No hay lectura usable: pregunte Exists antes. Ausente y cero son dos hechos, y "
				+ "confundirlos hace aparecer vencida una operacion que nadie pudo medir.");
		}
	}
}
