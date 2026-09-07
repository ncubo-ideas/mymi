using System;

namespace DealershipCar.App
{
	/// <summary>
	/// Las asignaciones de placa como un registro de columnas paralelas: una entrada por fila en cada
	/// columna, en orden de la linea de tiempo. Un DTO de arreglos primitivos, nunca un carrier del
	/// dominio.
	/// </summary>
	public sealed class PlateAssignmentRecord
	{
		private PlateAssignmentRecord(
			string[] asPublished, string[] joinKey, string[] assignedAt, string[] isCurrent)
		{
			AsPublished = asPublished;
			JoinKey = joinKey;
			AssignedAt = assignedAt;
			IsCurrent = isCurrent;
		}

		/// <summary>El texto de cada asignacion tal como el concesionario lo publico.</summary>
		public string[] AsPublished { get; }

		/// <summary>
		/// La llave normalizada de cada asignacion, que es la que cruza contra otro sistema. Sale ya
		/// decidida: ninguna caratula la deriva del texto publicado.
		/// </summary>
		public string[] JoinKey { get; }

		/// <summary>Desde cuando rige cada asignacion, dicho como dia.</summary>
		public string[] AssignedAt { get; }

		/// <summary>
		/// Cual asignacion es la vigente. Con al menos una fila hay exactamente una.
		/// </summary>
		public string[] IsCurrent { get; }

		/// <summary>
		/// Arma el registro y rechaza columnas de largos distintos, para que una fila sea una fila por
		/// construccion: una columna corta por una emparejaria en silencio cada fila posterior con la
		/// asignacion equivocada.
		/// </summary>
		internal static PlateAssignmentRecord Of(
			string[] asPublished, string[] joinKey, string[] assignedAt, string[] isCurrent)
		{
			string[][] columns = { asPublished, joinKey, assignedAt, isCurrent };

			foreach (string[] each in columns)
				if (each == null)
					throw new InvalidOperationException(
						"Una columna del registro de asignaciones llego ausente, y una columna ausente "
						+ "no es una columna vacia.");

			foreach (string[] each in columns)
				if (each.Length != asPublished.Length)
					throw new InvalidOperationException(
						$"Las columnas del registro de asignaciones miden {asPublished.Length} y "
						+ $"{each.Length}. Una fila es una fila: con largos distintos, cada fila "
						+ "despues de la primera diferencia quedaria emparejada con otra asignacion.");

			return new PlateAssignmentRecord(asPublished, joinKey, assignedAt, isCurrent);
		}
	}
}
