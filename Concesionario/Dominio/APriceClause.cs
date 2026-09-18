using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// LO QUE UN PRECIO PUBLICADO TIENE QUE DECIR: de que operacion es, para que hito, cuanto
	/// cuesta con su moneda, entre que dias rige, si esa vigencia rige en el instante que se
	/// pregunto, y de que revision sale todo.
	/// <para>
	/// Sale como FORMA mas valores ya tipados y nunca como una frase. El monto viaja con los
	/// digitos con que su moneda lo dice y el codigo de esa moneda al lado, porque donde va el
	/// signo respecto al numero es del idioma.
	/// </para>
	/// <para>
	/// Que nadie haya aportado un instante es un MIEMBRO de la vigencia y no un instante en blanco:
	/// el dominio no lee ningun reloj, y sin instante "vencido" no se puede decir respecto a nada.
	/// </para>
	/// </summary>
	internal sealed class APriceClause
	{
		private const string NothingToSay = "";
		private const int NoMilestoneToSay = 0;

		private readonly string _publication;
		private readonly string _revisionCode;
		private readonly string _operation;
		private readonly int _milestoneDistance;
		private readonly string _milestoneUnit;
		private readonly string _currencyShape;
		private readonly string _amount;
		private readonly string _currencyCode;
		private readonly string _validFrom;
		private readonly string _validUntil;
		private readonly string _validity;
		private readonly string _askedOn;

		private APriceClause(
			string publication, string revisionCode, string operation, int milestoneDistance,
			string milestoneUnit, string currencyShape, string amount, string currencyCode,
			string validFrom, string validUntil, string validity, string askedOn)
		{
			_publication = publication;
			_revisionCode = revisionCode;
			_operation = operation;
			_milestoneDistance = milestoneDistance;
			_milestoneUnit = milestoneUnit;
			_currencyShape = currencyShape;
			_amount = amount;
			_currencyCode = currencyCode;
			_validFrom = validFrom;
			_validUntil = validUntil;
			_validity = validity;
			_askedOn = askedOn;
		}

		/// <summary>
		/// La clausula cuando ninguna revision sellada publico el precio que se pidio. Lleva la
		/// operacion igual, porque una ausencia que no nombra lo que falta no se distingue de
		/// cualquier otra. El hito NO va: quien pregunto por uno que nadie publico ya sabe cual
		/// pidio, y quien pregunto con una distancia que no nombra ningun hito no pidio ninguno.
		/// </summary>
		internal static APriceClause OfNothingPublished(MaintenanceOperationKind operation)
		{
			if (operation == null)
				throw new DomainException(
					"Se compuso la ausencia de un precio sin decir de que operacion. Una ausencia que no "
					+ "nombra lo que falta no se distingue de cualquier otra.");

			return new APriceClause(
				SpecificationPublication.NotPublished, NothingToSay, operation.Name,
				NoMilestoneToSay, NothingToSay, NothingToSay, NothingToSay, NothingToSay,
				NothingToSay, NothingToSay, PriceValidity.NotAsked, NothingToSay);
		}

		/// <summary>
		/// La clausula de un precio que si se publico, con la revision de la que sale, y SIN decir
		/// si rige: eso necesita un instante, y quien no tenga ninguno que aportar igual puede
		/// citar lo que el fabricante publico.
		/// </summary>
		internal static APriceClause Published(
			string revisionCode, MaintenanceOperationKind operation, PublishedMilestone milestone,
			Money price, string validFrom, string validUntil)
		{
			return new APriceClause(
				SpecificationPublication.Published, TheRevision(revisionCode), operation.Name,
				milestone.TheDistanceItNames(), milestone.TheUnitItIsIn(),
				price.TheShapeOfItsCurrency(), price.TheAmountAsSaid(),
				price.TheCodeOfItsCurrency(), validFrom, validUntil,
				PriceValidity.NotAsked, NothingToSay);
		}

		/// <summary>
		/// La misma clausula CON el instante recibido situado dentro de su vigencia. Nada de lo
		/// recibido se guarda: entra por parametro y se va con la respuesta.
		/// </summary>
		internal APriceClause AskedAt(string validity, string askedOn)
		{
			if (!IsPublished())
				throw new DomainException(
					"Se quiso situar un instante dentro de la vigencia de un precio que nadie publico. "
					+ "Sin vigencia no hay dentro ni fuera, y hay que preguntar primero si se publico.");

			return new APriceClause(
				_publication, _revisionCode, _operation, _milestoneDistance, _milestoneUnit,
				_currencyShape, _amount, _currencyCode, _validFrom, _validUntil, validity, askedOn);
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

		/// <summary>El codigo de la revision que lo publico, o vacio donde no hay ninguna.
		/// TOTAL.</summary>
		internal string TheRevisionItCameFrom()
		{
			return _revisionCode;
		}

		/// <summary>De que operacion habla, por el NOMBRE del miembro. TOTAL.</summary>
		internal string TheOperationItIsAbout()
		{
			return _operation;
		}

		/// <summary>
		/// La distancia del hito al que se publico, tal como se publico, o cero donde no hay nada
		/// publicado. TOTAL, y cual de las dos es lo dice la forma y nunca ese cero.
		/// </summary>
		internal int TheMilestoneDistance()
		{
			return _milestoneDistance;
		}

		/// <summary>La unidad de esa distancia, como se dice, o vacia por lo mismo.
		/// TOTAL.</summary>
		internal string TheUnitTheMilestoneIsIn()
		{
			return _milestoneUnit;
		}

		/// <summary>La forma de la moneda del precio, por su codigo, o vacia por lo mismo.
		/// TOTAL.</summary>
		internal string TheShapeOfItsCurrency()
		{
			return _currencyShape;
		}

		/// <summary>Los digitos del precio, en cultura invariante, o vacios por lo mismo.
		/// TOTAL.</summary>
		internal string TheAmountAsSaid()
		{
			return _amount;
		}

		/// <summary>El codigo de la moneda del precio, o vacio por lo mismo. TOTAL.</summary>
		internal string TheCodeOfItsCurrency()
		{
			return _currencyCode;
		}

		/// <summary>Desde que dia rige, como dia del registro, o vacio por lo mismo.
		/// TOTAL.</summary>
		internal string TheDayItsValidityStarts()
		{
			return _validFrom;
		}

		/// <summary>Hasta que dia rige, en la misma forma. TOTAL.</summary>
		internal string TheDayItsValidityEnds()
		{
			return _validUntil;
		}

		/// <summary>Donde cae el instante preguntado dentro de esa vigencia, por su codigo.
		/// TOTAL.</summary>
		internal string WhereTheInstantAskedFalls()
		{
			return _validity;
		}

		/// <summary>
		/// El dia por el que se pregunto, o vacio donde nadie aporto ninguno. TOTAL, y cual de los
		/// dos es lo dice la vigencia y nunca ese vacio.
		/// </summary>
		internal string TheDayItWasAskedOn()
		{
			return _askedOn;
		}

		private static string TheRevision(string revisionCode)
		{
			if (string.IsNullOrWhiteSpace(revisionCode))
				throw new DomainException(
					"Se compuso la clausula de un precio sin nombrar la revision de la que sale. Una "
					+ "respuesta sin procedencia es un rumor: quien la reciba no puede saber si sigue "
					+ "vigente ni contra que compararla cuando cambie.");

			return revisionCode;
		}
	}
}
