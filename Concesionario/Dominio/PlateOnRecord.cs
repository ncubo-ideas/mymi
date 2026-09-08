using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La placa que regia al instante preguntado, o la constancia de que no hay ninguna. Un vehiculo
	/// cuyo concesionario nunca publico una placa es un caso ordinario y no un error.
	/// <para>
	/// La ausencia tiene una sola clase: o hay una placa publicada a ese instante o no la hay, y no
	/// hay una segunda razon estructuralmente distinta que nombrar. La base queda abierta porque es a
	/// lo que se extenderia una variante nueva el dia que la haya.
	/// </para>
	/// </summary>
	internal abstract class PlateOnRecord
	{
		/// <summary>No hay ninguna placa registrada. Instancia unica, compartida.</summary>
		internal static readonly PlateOnRecord None = new NoPlateOnRecord();

		/// <summary>
		/// La placa de una asignacion. Una asignacion ausente es un defecto de quien llama: la
		/// ausencia se dice con <see cref="PlateOnRecord.None"/>, que es un tipo y no un hueco.
		/// </summary>
		internal static PlateOnRecord Of(APlateAssignment assignment)
		{
			if (assignment == null)
				throw new DomainException(
					"No hay asignacion con la cual decir cual es la placa. La ausencia de placa tiene "
					+ "su propio tipo, y llegar aqui sin asignacion es haber perdido cual de las dos "
					+ "cosas paso.");

			return new AnAssignedPlate(assignment);
		}

		/// <summary>
		/// Si hay una placa publicada al instante preguntado. Es la pregunta TOTAL del par: nunca
		/// revienta, y es la que hay que hacer antes de leer cualquiera de los dos textos.
		/// </summary>
		internal abstract bool Exists();

		/// <summary>
		/// El texto tal como lo publico el concesionario. Revienta si no hay placa: una cadena vacia
		/// parece una respuesta y el Vin puesto en su lugar se cruzaria contra los registros de otro
		/// sistema y no coincidiria con nada, en silencio.
		/// </summary>
		internal abstract string AsPublished { get; }

		/// <summary>
		/// El texto normalizado con el que se cruza. Revienta si no hay placa: una llave vacia
		/// coincidiria con cualquier cosa o con nada sin decir cual de las dos.
		/// </summary>
		internal abstract string JoinKey { get; }

		/// <summary>Desde cuando rige la placa que contesta. Revienta si no hay placa.</summary>
		internal abstract DateTime AssignedAt { get; }

		/// <summary>
		/// La placa dicha entera, o la constancia con nombre de que no hay ninguna. TOTAL, y la frase
		/// es del dominio: la caratula la imprime y no compone nada.
		/// </summary>
		internal abstract string Describe();
	}
}
