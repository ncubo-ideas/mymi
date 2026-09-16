using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Algo que el alcance publicado NO cubre, con la condicion bajo la cual muerde. La condicion
	/// es texto PUBLICADO y no un predicado: este modulo la nombra para que la persona la lea, y
	/// no decide si se cumple en un caso concreto -- decidir eso seria adjudicar un reclamo, que
	/// es de otro.
	/// </summary>
	internal sealed class CoverageExclusion
	{
		private readonly string _systemCode;
		private readonly string _exclusion;
		private readonly string _condition;

		/// <summary>
		/// Se arma entera y se valida antes de entrar a ninguna revision. Las tres partes son
		/// obligatorias: sin sistema no se sabe a que alcanza, sin texto no dice que excluye, y
		/// sin condicion la persona no puede saber si le toca.
		/// </summary>
		internal CoverageExclusion(string systemCode, string exclusion, string condition)
		{
			if (string.IsNullOrWhiteSpace(systemCode))
				throw new DomainException(
					"Se publico una exclusion sin decir sobre que sistema. Una exclusion que no "
					+ "nombra su sistema no se puede nombrar en ninguna respuesta, y callarla es la "
					+ "peor respuesta posible a una pregunta de cobertura.");

			if (string.IsNullOrWhiteSpace(exclusion))
				throw new DomainException(
					$"Se publico una exclusion sobre '{systemCode}' sin decir que excluye. Nombrarla "
					+ "vacia le avisaria a la persona que hay letra chica sin dejarle leerla.");

			if (string.IsNullOrWhiteSpace(condition))
				throw new DomainException(
					$"Se publico una exclusion sobre '{systemCode}' sin decir bajo que condicion "
					+ "muerde. La condicion es lo que deja que la persona sepa si le toca: sin ella, "
					+ "este modulo tendria que decidirlo, y decidirlo es adjudicar.");

			_systemCode = systemCode;
			_exclusion = exclusion;
			_condition = condition;
		}

		/// <summary>El codigo del sistema sobre el que se publico, tal cual.</summary>
		internal string Code
		{
			get { return _systemCode; }
		}

		/// <summary>
		/// Si esta exclusion habla de ese codigo, ORDINAL y sin recortar. Es tambien la unica
		/// forma en que una exclusion "aplica": aplica al sistema que nombra, y que aplique al
		/// caso lo decide el fabricante.
		/// </summary>
		internal bool NamesTheSystem(string systemCode)
		{
			return string.Equals(_systemCode, systemCode, StringComparison.Ordinal);
		}

		/// <summary>
		/// Si otra publicacion es ESTA MISMA exclusion -- mismo sistema y mismo texto -- y por lo
		/// tanto la corrige en vez de agregarse. Es una comparacion con nombre entre dos
		/// publicaciones, no una igualdad de valor del tipo.
		/// </summary>
		internal bool NamesTheSameExclusionAs(CoverageExclusion other)
		{
			if (other == null)
				throw new DomainException(
					"Se comparo una exclusion publicada contra nada. Sin la otra publicacion no hay "
					+ "con que decidir si es una correccion o una exclusion mas.");

			return other.NamesTheSystem(_systemCode)
				&& string.Equals(_exclusion, other._exclusion, StringComparison.Ordinal);
		}

		/// <summary>
		/// Que excluye, tal como se publico. Sale con el sistema y la condicion al lado y nunca solo:
		/// las tres son lo que la persona tiene que leer para saber si le toca.
		/// </summary>
		internal string TheTextItPublished()
		{
			return _exclusion;
		}

		/// <summary>
		/// Bajo que condicion muerde, tal como se publico. Es texto PUBLICADO y no un predicado: este
		/// modulo la nombra y no decide si se cumple, porque decidirlo seria adjudicar.
		/// </summary>
		internal string TheConditionItAppliesUnder()
		{
			return _condition;
		}
	}
}
