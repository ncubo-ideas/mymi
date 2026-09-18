using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La respuesta de cobertura que si se decidio: trae uno de los cinco
	/// <see cref="CoverageStanding"/> y la clausula que dice contra que se midio, con las
	/// exclusiones publicadas que haya y la revision de la que sale.
	/// </summary>
	internal sealed class ASettledCoverageAnswer : CoverageAnswer
	{
		private readonly string _systemCode;
		private readonly CoverageStanding _standing;

		/// <summary>
		/// La compone <see cref="WarrantyCoverage"/>, que es quien conoce el alcance publicado. Sin
		/// sistema la respuesta no dice de que habla, y sin resultado no seria una respuesta
		/// decidida.
		/// </summary>
		internal ASettledCoverageAnswer(
			string systemCode, CoverageStanding standing, ACoverageClause clause)
			: base(clause)
		{
			if (systemCode == null)
				throw new DomainException(
					"Se compuso una respuesta de cobertura sin decir de que sistema habla. La misma "
					+ "respuesta serviria para todos, que es como una termina leyendose como si fuera "
					+ "de otra cosa. Un codigo EN BLANCO si pasa: es un codigo que el catalogo del "
					+ "fabricante no conoce, y eso es una respuesta.");

			if (standing == null)
				throw new DomainException(
					$"Se compuso una respuesta decidida sobre '{systemCode}' sin ninguna de las "
					+ "respuestas posibles. Decidida quiere decir que hay una.");

			_systemCode = systemCode;
			_standing = standing;
		}

		/// <summary>Si.</summary>
		internal override bool CanBeEvaluated()
		{
			return true;
		}

		/// <summary>El resultado, que aca si existe.</summary>
		internal override CoverageStanding Standing()
		{
			return _standing;
		}

		/// <summary>Decidida.</summary>
		internal override string TheShapeItHas()
		{
			return Settled;
		}

		/// <summary>El sistema por el que se pregunto, tal como llego.</summary>
		internal override string TheSystemItIsAbout()
		{
			return _systemCode;
		}

		/// <summary>El resultado por el nombre del miembro, que es lo que cruza hacia afuera.</summary>
		internal override string TheStandingItReached()
		{
			return _standing.Name;
		}
	}
}
