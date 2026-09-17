using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// LO QUE LA REGLA DE SERVICIO EN OTRO TALLER TIENE QUE DECIR: la regla publicada, que conserva la
	/// cobertura, de que revision sale, y -- cuando se pregunto por un vehiculo -- los conteos de ese
	/// vehiculo, dichos como conteos y sin concluir nada de ellos.
	/// <para>
	/// Sale como FORMA mas valores ya tipados y nunca como una frase. Los dos textos son del
	/// fabricante y no se traducen: se dicen como se publicaron.
	/// </para>
	/// </summary>
	internal sealed class AServicingClause
	{
		private readonly string _publication;
		private readonly string _revisionCode;
		private readonly string _rule;
		private readonly string _whatPreservesCover;
		private readonly string _halves;
		private readonly int _withEvidence;
		private readonly int _withoutEvidence;
		private readonly int _missed;

		private AServicingClause(
			string publication, string revisionCode, string rule, string whatPreservesCover,
			string halves, int withEvidence, int withoutEvidence, int missed)
		{
			_publication = publication;
			_revisionCode = revisionCode;
			_rule = rule;
			_whatPreservesCover = whatPreservesCover;
			_halves = halves;
			_withEvidence = withEvidence;
			_withoutEvidence = withoutEvidence;
			_missed = missed;
		}

		/// <summary>
		/// La clausula cuando ninguna revision sellada publico regla. No lleva nada que nombrar: la
		/// regla no tiene clave, asi que lo que falta es el item entero.
		/// </summary>
		internal static AServicingClause OfNothingPublished()
		{
			return new AServicingClause(
				SpecificationPublication.NotPublished, "", "", "",
				HalvesAnswered.ThePublishedHalfAlone, 0, 0, 0);
		}

		/// <summary>
		/// La clausula de la regla que si se publico, con la revision de la que sale. Las dos mitades
		/// juntas o no es una regla: una regla sin que hacer al respecto deja a la persona sabiendo que
		/// tiene un problema y no que hacer con el.
		/// </summary>
		internal static AServicingClause Published(
			string revisionCode, string rule, string whatPreservesCover)
		{
			if (string.IsNullOrWhiteSpace(revisionCode))
				throw new DomainException(
					"Se compuso la clausula de la regla de servicio en otro taller sin nombrar la "
					+ "revision de la que sale. Una respuesta sin procedencia es un rumor: quien la "
					+ "reciba no puede saber si sigue vigente ni contra que compararla cuando cambie.");

			return new AServicingClause(
				SpecificationPublication.Published, revisionCode, rule, whatPreservesCover,
				HalvesAnswered.ThePublishedHalfAlone, 0, 0, 0);
		}

		/// <summary>
		/// La misma clausula emparejada con los conteos de ESTE vehiculo. Nada de lo recibido se
		/// guarda. Sin regla publicada no hay contra que pararlos, y decir solo los conteos seria dar
		/// numeros sin la regla que los hace significar algo.
		/// </summary>
		internal AServicingClause PairedWith(
			int externalServicesWithEvidence, int externalServicesWithoutEvidence,
			int missedMaintenanceCount)
		{
			if (!IsPublished())
				throw new DomainException(
					"Se pidio la posicion de un vehiculo frente a una regla de servicio en otro taller "
					+ "que ninguna revision publico. Hay que preguntar primero si la publico.");

			return new AServicingClause(
				_publication, _revisionCode, _rule, _whatPreservesCover,
				HalvesAnswered.GeneralAndPersonalTogether, externalServicesWithEvidence,
				externalServicesWithoutEvidence, missedMaintenanceCount);
		}

		/// <summary>Si hay algo publicado detras de esta respuesta, por su codigo. TOTAL.</summary>
		internal string ThePublicationShape()
		{
			return _publication;
		}

		/// <summary>Si hay algo publicado que contestar. TOTAL.</summary>
		internal bool IsPublished()
		{
			return _publication == SpecificationPublication.Published;
		}

		/// <summary>El codigo de la revision que la publico, o vacio donde no hay ninguna. TOTAL.</summary>
		internal string TheRevisionItCameFrom()
		{
			return _revisionCode;
		}

		/// <summary>La regla tal como se publico, sin traducir y sin resumir. TOTAL.</summary>
		internal string TheRulePublished()
		{
			return _rule;
		}

		/// <summary>Que conserva la cobertura, tal como se publico. TOTAL.</summary>
		internal string TheWhatPreservesCoverPublished()
		{
			return _whatPreservesCover;
		}

		/// <summary>Que mitades contesta esta clausula, por su codigo. TOTAL.</summary>
		internal string TheHalvesItAnswers()
		{
			return _halves;
		}

		/// <summary>
		/// Cuantos servicios externos con comprobante lleva este vehiculo, tal como llegaron, o cero
		/// donde nadie pregunto por ninguno. TOTAL.
		/// </summary>
		internal int TheExternalServicesWithEvidence()
		{
			return _withEvidence;
		}

		/// <summary>Cuantos sin comprobante, por lo mismo. TOTAL.</summary>
		internal int TheExternalServicesWithoutEvidence()
		{
			return _withoutEvidence;
		}

		/// <summary>Cuantos mantenimientos no hechos, por lo mismo. TOTAL.</summary>
		internal int TheMissedMaintenanceCount()
		{
			return _missed;
		}
	}
}
