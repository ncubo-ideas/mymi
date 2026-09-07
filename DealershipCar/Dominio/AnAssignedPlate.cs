using System;
using System.Globalization;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La placa que regia al instante preguntado: sus dos textos y desde cuando rige.
	/// </summary>
	internal sealed class AnAssignedPlate : PlateOnRecord
	{
		private readonly APlateAssignment _assignment;

		/// <summary>Se llega por <see cref="PlateOnRecord.Of"/>, que lleva la guarda.</summary>
		internal AnAssignedPlate(APlateAssignment assignment)
		{
			_assignment = assignment;
		}

		/// <summary>Si.</summary>
		internal override bool Exists()
		{
			return true;
		}

		/// <summary>El texto tal como lo publico el concesionario.</summary>
		internal override string AsPublished
		{
			get { return _assignment.Plate.AsPublished; }
		}

		/// <summary>El texto normalizado con el que se cruza.</summary>
		internal override string JoinKey
		{
			get { return _assignment.Plate.JoinKey; }
		}

		/// <summary>Desde cuando rige.</summary>
		internal override DateTime AssignedAt
		{
			get { return _assignment.AssignedAt; }
		}

		/// <summary>
		/// La placa y desde cuando rige. El instante va siempre: la respuesta a cual es la placa es
		/// cual placa al instante preguntado, y sin el instante la frase no dice cual pregunta
		/// contesto.
		/// </summary>
		internal override string Describe()
		{
			return "La placa de este vehículo es " + AsPublished + ", asignada desde el "
				+ TheDay(AssignedAt) + ".";
		}

		private static string TheDay(DateTime instant)
		{
			return instant.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}
	}
}
