using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Un sistema que el alcance publicado cubre, nombrado en el vocabulario del FABRICANTE --
	/// una cadena con catalogo y no un tipo cerrado, porque un vocabulario ajeno rompe un tipo
	/// cerrado en la primera revision. Puede llevar sus propios limites, y si los lleva son esos
	/// los que rigen.
	/// </summary>
	internal sealed class CoveredSystem
	{
		private readonly SystemLimits _ownLimits;

		/// <summary>
		/// Se arma entero y se valida antes de entrar a ninguna revision. Un sistema sin codigo no
		/// se puede elegir de la lista publicada ni nombrar en una respuesta.
		/// </summary>
		internal CoveredSystem(string systemCode, SystemLimits ownLimits)
		{
			if (string.IsNullOrWhiteSpace(systemCode))
				throw new DomainException(
					"Se publico un sistema cubierto sin codigo. El codigo es lo que la persona elige "
					+ "de la lista publicada y lo que toda respuesta nombra: sin el, el alcance "
					+ "cubriria algo que nadie puede pedir.");

			if (ownLimits == null)
				throw new DomainException(
					$"Se publico el sistema '{systemCode}' sin decir si lleva limites propios. No "
					+ "llevarlos se dice con la ausencia, que es un objeto: si no se dice, no se sabe "
					+ "contra que se mide.");

			Code = systemCode;
			_ownLimits = ownLimits;
		}

		/// <summary>El codigo con que el fabricante lo publico, tal cual.</summary>
		internal string Code { get; }

		/// <summary>
		/// Si este item habla de ese codigo. Se compara ORDINAL y sin recortar: el vocabulario es
		/// del fabricante y este modulo no le arregla la ortografia a nadie.
		/// </summary>
		internal bool NamesTheSystem(string systemCode)
		{
			return string.Equals(Code, systemCode, StringComparison.Ordinal);
		}

		/// <summary>
		/// Elige los limites que RIGEN: los propios del sistema si los publico, y si no el sobre
		/// del vehiculo que la lectura recibio. Lo mas especifico gana, y gana en las dos
		/// direcciones -- tambien cuando los propios son mas largos que el sobre.
		/// </summary>
		internal SystemLimits TheMoreSpecificLimitGoverns(SystemLimits vehicleEnvelope)
		{
			if (vehicleEnvelope == null)
				throw new DomainException(
					$"Se pregunto por los limites que rigen sobre '{Code}' sin decir el sobre del "
					+ "vehiculo. Si el sistema no publico limites propios, el sobre es lo unico que "
					+ "queda, y sin el no hay contra que medir.");

			if (_ownLimits.Exists()) return _ownLimits;

			return vehicleEnvelope;
		}

		/// <summary>
		/// Si este sistema publico limites propios, por su codigo. TOTAL, y es la que hay que hacer
		/// antes de leer cualquiera de las tres mitades de abajo.
		/// </summary>
		internal string TheShapeOfItsOwnLimits()
		{
			return _ownLimits.Exists() ? LimitsInForce.Named : LimitsInForce.None;
		}

		/// <summary>El plazo de sus limites propios, o cero donde no publico ninguno. TOTAL.</summary>
		internal int TheMonthsItsOwnLimitsName()
		{
			return _ownLimits.Exists() ? _ownLimits.TheMonthsItLimits() : 0;
		}

		/// <summary>La distancia de sus limites propios, o cero por lo mismo. TOTAL.</summary>
		internal int TheDistanceItsOwnLimitsName()
		{
			return _ownLimits.Exists() ? _ownLimits.TheDistanceItLimits() : 0;
		}

		/// <summary>
		/// El nombre de la unidad de esa distancia, o vacio donde no publico limites propios o donde
		/// los que publico no llevan mitad de distancia. TOTAL.
		/// </summary>
		internal string TheNameOfTheUnitItsOwnLimitsUse()
		{
			return _ownLimits.Exists()
				? _ownLimits.TheNameOfTheUnitItsDistanceIsIn()
				: string.Empty;
		}
	}
}
