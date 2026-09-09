using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La constancia de que no hay ninguna placa publicada al instante preguntado. No es una placa
	/// vacia ni el Vin haciendo de placa: es la ausencia CON TIPO, y su frase nombra a quien publica
	/// una placa, que es lo unico accionable que tiene quien la lee.
	/// </summary>
	internal sealed class NoPlateOnRecord : PlateOnRecord
	{
		/// <summary>Se llega por <see cref="PlateOnRecord.None"/>.</summary>
		internal NoPlateOnRecord()
		{
		}

		/// <summary>No.</summary>
		internal override bool Exists()
		{
			return false;
		}

		/// <summary>Revienta: no hay placa, asi que no hay texto publicado.</summary>
		internal override string AsPublished
		{
			get { throw NothingWasPublished(); }
		}

		/// <summary>Revienta: sin placa no hay llave con que cruzar.</summary>
		internal override string JoinKey
		{
			get { throw NothingWasPublished(); }
		}

		/// <summary>Revienta: sin placa no hay instante desde el cual rija.</summary>
		internal override DateTime AssignedAt
		{
			get { throw NothingWasPublished(); }
		}

		/// <summary>La ausencia en palabras, diciendo quien publica una placa.</summary>
		internal override string Describe()
		{
			return "No hay ninguna placa registrada para este vehículo al instante preguntado. La "
				+ "placa de un vehículo la publica el concesionario.";
		}

		private static DomainException NothingWasPublished()
		{
			return new DomainException(
				"No hay ninguna placa registrada para este vehiculo al instante preguntado: hay que "
				+ "preguntar primero si la hay. Una cadena vacia parece una respuesta, y el Vin "
				+ "puesto donde va una placa se cruzaria contra los registros de otro sistema y no "
				+ "coincidiria con nada, sin error y sin aviso.");
		}
	}
}
