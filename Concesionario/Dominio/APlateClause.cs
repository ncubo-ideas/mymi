using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// LO QUE LA PLACA TIENE QUE DECIR: si hay una publicada al instante preguntado, cual es tal como
	/// la publico el concesionario, y desde que DIA rige.
	/// <para>
	/// Sale como FORMA mas valores ya tipados y nunca como una frase. El texto publicado es DATO DEL
	/// REGISTRO y cruza intacto: es la placa tal como el concesionario la escribio, con sus guiones y
	/// sus espacios si los traia.
	/// </para>
	/// <para>
	/// LA LLAVE DE CRUCE NO ESTA AQUI a proposito. Es la mitad que se COMPARA contra otro registro, no
	/// la que se dice: una frase que la llevara pondria la misma placa escrita de dos formas en una
	/// sola linea. La puerta las contesta las dos por su lado.
	/// </para>
	/// </summary>
	internal sealed class APlateClause
	{
		private const string NothingToSay = "";

		private readonly string _shape;
		private readonly string _asPublished;
		private readonly string _assignedOn;

		private APlateClause(string shape, string asPublished, string assignedOn)
		{
			_shape = shape;
			_asPublished = asPublished;
			_assignedOn = assignedOn;
		}

		/// <summary>
		/// La placa que rige: su texto publicado y el dia desde el que rige. El instante va siempre,
		/// porque cual es la placa es cual placa al instante preguntado.
		/// </summary>
		internal static APlateClause Assigned(string asPublished, string assignedOn)
		{
			if (string.IsNullOrWhiteSpace(asPublished))
				throw new DomainException(
					"Se compuso una placa asignada sin el texto que el concesionario publico. Una cadena "
					+ "vacia parece una respuesta, y la ausencia de placa tiene su propia situacion.");

			return new APlateClause(PlateShape.Assigned, asPublished, assignedOn);
		}

		/// <summary>
		/// La constancia de que no hay ninguna placa publicada a ese instante. No lleva ningun texto:
		/// el Vin puesto donde va una placa se cruzaria contra otro registro y no coincidiria con nada.
		/// </summary>
		internal static APlateClause WithNoneOnRecord()
		{
			return new APlateClause(PlateShape.NoneOnRecord, NothingToSay, NothingToSay);
		}

		/// <summary>En cual de las dos situaciones esta, por su codigo. TOTAL.</summary>
		internal string TheShapeItHas()
		{
			return _shape;
		}

		/// <summary>
		/// La placa tal como la publico el concesionario, o vacia donde no hay ninguna. TOTAL, y cual
		/// de las dos es lo dice la forma.
		/// </summary>
		internal string TheTextAsPublished()
		{
			return _asPublished;
		}

		/// <summary>
		/// El DIA desde el que rige, en yyyy-MM-dd, o vacio por lo mismo. TOTAL.
		/// </summary>
		internal string TheDayItIsAssignedFrom()
		{
			return _assignedOn;
		}
	}
}
