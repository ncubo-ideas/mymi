using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// La respuesta que da la lectura de cobertura: o esta decidida y trae uno de los cinco
	/// <see cref="CoverageStanding"/>, o no se pudo evaluar y lo dice nombrando en su propio tipo
	/// por que. Ninguno de los cinco quiere decir "no lo se", y devolver el benigno para una
	/// entrada inevaluable es como una app termina repitiendo rio abajo, como si fuera un hecho,
	/// algo que nunca se midio.
	/// </summary>
	internal abstract class CoverageAnswer
	{
		/// <summary>
		/// Si se pudo decidir con lo que hay. TOTAL: contesta siempre, y es la que hay que hacer
		/// antes de leer el resultado.
		/// </summary>
		internal virtual bool CanBeEvaluated()
		{
			return false;
		}

		/// <summary>
		/// Si quedo suspendida porque el limite que rige y el odometro estan en unidades
		/// distintas. TOTAL: es la clase de ausencia, con nombre, y no un codigo de motivo.
		/// </summary>
		internal virtual bool SuspendedByAUnitMismatch()
		{
			return false;
		}

		/// <summary>
		/// El resultado. Accesor GUARDADO: revienta si no se pudo evaluar, porque el conjunto de
		/// cinco no tiene un sexto miembro que diga "no lo se", y devolver cualquiera de los cinco
		/// ahi seria contestar una pregunta que no se pudo contestar.
		/// </summary>
		internal virtual CoverageStanding Standing()
		{
			throw new DomainException(
				"Esta pregunta de cobertura no se pudo evaluar, asi que no tiene respuesta: hay que "
				+ "preguntar primero si se podia evaluar. Cualquiera de las respuestas posibles "
				+ "devuelta aca se leeria como un hecho medido, y nadie mas abajo podria "
				+ "distinguirlo.");
		}

		/// <summary>
		/// La respuesta en una frase. TOTAL: lo que no se pudo evaluar tambien se dice con
		/// palabras, y diciendo por que.
		/// </summary>
		internal abstract string Describe();
	}
}
