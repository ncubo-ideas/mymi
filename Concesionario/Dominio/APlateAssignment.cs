using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Una asignacion de placa: la placa y el instante desde el que rige. Es una ocurrencia, no una
	/// correccion de la anterior -- re-placar es ordinario, asi que dos asignaciones son dos hechos y
	/// la anterior se sigue pudiendo leer por su propio instante.
	/// </summary>
	internal sealed class APlateAssignment
	{
		private readonly LicensePlate _plate;
		private readonly DateTime _assignedAt;

		/// <summary>
		/// La asignacion con su placa y su instante. Una asignacion sin placa no dice que se asigno,
		/// y no hay ninguna placa por defecto que ponerle en su lugar.
		/// </summary>
		internal APlateAssignment(LicensePlate plate, DateTime assignedAt)
		{
			if (plate == null)
				throw new DomainException(
					"Llego una asignacion sin placa, asi que no dice que se le asigno al vehiculo. "
					+ "Quien publica la placa es el concesionario y aqui no se inventa ninguna.");

			_plate = plate;
			_assignedAt = assignedAt;
		}

		/// <summary>La placa que se asigno.</summary>
		internal LicensePlate Plate
		{
			get { return _plate; }
		}

		/// <summary>Desde cuando rige.</summary>
		internal DateTime AssignedAt
		{
			get { return _assignedAt; }
		}
	}
}
