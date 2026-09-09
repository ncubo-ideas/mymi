using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La respuesta de sobre que no se pudo decidir porque no hay ninguna lectura del odometro al
	/// instante preguntado ni antes. Una lectura ausente se dice: no es cero, no es la de la
	/// entrega haciendo de la de hoy, y no es la ultima que haya sin decir cuando se observo. El
	/// motivo esta en el NOMBRE del tipo y no en un campo.
	/// </summary>
	internal sealed class AnEnvelopeSuspendedByAnAbsentReading : EnvelopeAnswer
	{
		private readonly string _clause;

		/// <summary>La compone quien pidio el odometro al instante preguntado y no habia.</summary>
		internal AnEnvelopeSuspendedByAnAbsentReading(string clause)
		{
			_clause = clause;
		}

		/// <summary>Si: es esta clase de ausencia y no otra.</summary>
		internal override bool SuspendedByAnAbsentReading()
		{
			return true;
		}

		/// <summary>
		/// Dice que falta la lectura, y no da ningun numero en su lugar: un numero aqui se leeria
		/// como un odometro que alguien leyo.
		/// </summary>
		internal override string Describe()
		{
			return TheAnswerSaying(
				"no se puede evaluar, porque no hay ninguna lectura del odómetro de este vehículo "
				+ "al instante preguntado ni antes. Una lectura ausente no es una lectura de nada, "
				+ "y la mitad de distancia no se puede medir sin una.",
				_clause);
		}
	}
}
