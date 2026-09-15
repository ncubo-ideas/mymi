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
		/// <summary>La compone quien vio que el perfil no tiene entrega registrada.</summary>
		internal AnEnvelopeSuspendedByAnAbsentDelivery(AnEnvelopeClause clause)
			: base(clause)
		{
		}

		/// <summary>Si: es esta clase de ausencia y no otra.</summary>
		internal override bool SuspendedByAnAbsentDelivery()
		{
			return true;
		}

		/// <summary>
		/// Suspendida por la entrega que falta. La respuesta contesta aunque la pregunta no:
		/// callarlo dejaria a la persona creyendo que le contestaron.
		/// </summary>
		internal override string TheShapeItHas()
		{
			return NoDelivery;
		}
	}
}
