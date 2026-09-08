using System;
using System.Globalization;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Que fluido de una clase lleva el modelo: su grado, cuanta capacidad y en que unidad, y cada
	/// cuanto se cambia. Las cuatro cosas juntas o no es una especificacion -- media hoja
	/// publicada es lo que el sello existe para que nadie pueda citar.
	/// </summary>
	internal sealed class FluidSpecification
	{
		private readonly FluidKind _kind;
		private readonly string _grade;
		private readonly decimal _capacity;
		private readonly VolumeUnit _capacityUnit;
		private readonly PublishedInterval _changeInterval;

		/// <summary>
		/// Se arma entera y se valida antes de entrar a ninguna revision. La capacidad va con la
		/// unidad en que se publico y se contesta en esa: no se normaliza ni se convierte.
		/// </summary>
		internal FluidSpecification(
			FluidKind kind, string grade, decimal capacity, VolumeUnit capacityUnit,
			PublishedInterval changeInterval)
		{
			if (kind == null)
				throw new DomainException(
					"Se publico una especificacion de fluido sin decir de que fluido habla.");

			if (string.IsNullOrWhiteSpace(grade))
				throw new DomainException(
					$"Se publico el {kind.Name} sin grado. El grado es lo que la persona compra: "
					+ "una especificacion que no lo dice no contesta la pregunta que le hacen.");

			if (capacity <= 0)
				throw new DomainException(
					$"Se publico el {kind.Name} con una capacidad de {capacity}. Un fluido que el "
					+ "vehiculo lleva lleva alguna cantidad, y un cero se leeria como que no lleva "
					+ "ninguna.");

			if (capacityUnit == null)
				throw new DomainException(
					$"Se publico la capacidad del {kind.Name} sin decir en que unidad esta. Aca las "
					+ "unidades no se convierten, asi que una capacidad sin unidad no se puede leer.");

			if (changeInterval == null)
				throw new DomainException(
					$"Se publico el {kind.Name} sin decir cada cuanto se cambia.");

			_kind = kind;
			_grade = grade;
			_capacity = capacity;
			_capacityUnit = capacityUnit;
			_changeInterval = changeInterval;
		}

		/// <summary>
		/// La clausula que este fluido aporta a la respuesta: la clase, el grado, la capacidad con
		/// su unidad y el intervalo. Componerla es del dominio y no de la vista. El numero se dice
		/// igual en toda maquina, para que la respuesta que alguien guardo sea la misma que la que
		/// otro lee.
		/// </summary>
		internal string Describe()
		{
			return _kind.HowItIsSaid
				+ ": grado " + _grade
				+ ", capacidad " + _capacity.ToString(CultureInfo.InvariantCulture)
				+ " " + _capacityUnit.Name
				+ ", se cambia " + _changeInterval.Describe() + ".";
		}
	}
}
