using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La respuesta de cobertura que no se pudo decidir porque el limite que rige y el odometro
	/// del vehiculo estan en unidades distintas. No se convierte: se deja de comparar y se dice
	/// cual contra cual. El motivo esta en el NOMBRE del tipo y no en un campo, para que quien
	/// busque de donde sale este caso lo encuentre.
	/// </summary>
	internal sealed class ACoverageSuspendedByAUnitMismatch : CoverageAnswer
	{
		private readonly string _systemCode;
		private readonly DistanceUnit _limitUnit;
		private readonly DistanceUnit _odometerUnit;

		/// <summary>
		/// La compone <see cref="WarrantyCoverage"/> con las dos unidades que no calzaron: la del
		/// limite que rige y la del odometro recibido.
		/// </summary>
		internal ACoverageSuspendedByAUnitMismatch(
			string systemCode, DistanceUnit limitUnit, DistanceUnit odometerUnit,
			ACoverageClause clause)
			: base(clause)
		{
			if (systemCode == null)
				throw new DomainException(
					"Se compuso una cobertura suspendida sin decir de que sistema habla.");

			if (limitUnit == null || odometerUnit == null)
				throw new DomainException(
					$"Se compuso una cobertura suspendida sobre '{systemCode}' sin nombrar las dos "
					+ "unidades que no coincidieron. Decir que no se pudo medir sin decir cual contra "
					+ "cual no le deja a nadie corregirlo.");

			_systemCode = systemCode;
			_limitUnit = limitUnit;
			_odometerUnit = odometerUnit;
		}

		/// <summary>Si: es esta clase de ausencia y no otra.</summary>
		internal override bool SuspendedByAUnitMismatch()
		{
			return true;
		}

		/// <summary>Suspendida por un desacuerdo de unidades.</summary>
		internal override string TheShapeItHas()
		{
			return UnitMismatch;
		}

		/// <summary>El sistema por el que se pregunto, tal como llego.</summary>
		internal override string TheSystemItIsAbout()
		{
			return _systemCode;
		}

		/// <summary>La unidad del limite de distancia que rige, por su nombre.</summary>
		internal override string TheUnitTheLimitIsIn()
		{
			return _limitUnit.Name;
		}

		/// <summary>La unidad del odometro de este vehiculo, por su nombre.</summary>
		internal override string TheUnitTheOdometerIsIn()
		{
			return _odometerUnit.Name;
		}
	}
}
