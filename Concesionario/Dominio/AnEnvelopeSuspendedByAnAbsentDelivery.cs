using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La respuesta de sobre que no se pudo decidir porque nadie registro la entrega. Las dos
	/// mitades se miden desde ahi, asi que sin ancla no se puede medir ninguna. El motivo esta en
	/// el NOMBRE del tipo y no en un campo, para que quien busque de donde sale este caso lo
	/// encuentre.
	/// </summary>
	internal sealed class AnEnvelopeSuspendedByAnAbsentDelivery : EnvelopeAnswer
	{
		private readonly string _clause;

		/// <summary>La compone quien vio que el perfil no tiene entrega registrada.</summary>
		internal AnEnvelopeSuspendedByAnAbsentDelivery(string clause)
		{
			_clause = clause;
		}

		/// <summary>Si: es esta clase de ausencia y no otra.</summary>
		internal override bool SuspendedByAnAbsentDelivery()
		{
			return true;
		}

		/// <summary>
		/// Dice que falta la entrega y quien la registra. La frase contesta aunque la pregunta no:
		/// callarlo dejaria a la persona creyendo que le contestaron.
		/// </summary>
		internal override string Describe()
		{
			return TheAnswerSaying(
				"no se puede evaluar, porque no hay ninguna entrega registrada para este vehículo, "
				+ "y las dos mitades del sobre se miden desde el día en que se entregó. Eso lo "
				+ "registra el concesionario.",
				_clause);
		}
	}
}
