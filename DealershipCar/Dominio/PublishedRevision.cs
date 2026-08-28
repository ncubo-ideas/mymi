using System;
using System.Collections.Generic;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// Una publicacion del fabricante sobre un modelo: su codigo, el instante en que se publico,
	/// donde esta parada, y el contenido que se publico adentro. Es la procedencia de toda
	/// respuesta de la especificacion -- una respuesta sin revision es un rumor -- y nace
	/// borrador, porque lo que se recibe es la apertura y no el estado.
	/// </summary>
	internal sealed class PublishedRevision
	{
		private readonly Dictionary<FluidKindCode, FluidSpecification> _fluids =
			new Dictionary<FluidKindCode, FluidSpecification>();
		private readonly Dictionary<TyrePositionCode, TyreSpecification> _tyres =
			new Dictionary<TyrePositionCode, TyreSpecification>();
		private readonly Dictionary<OperationKindCode, ServiceIntervalEntry> _serviceIntervals =
			new Dictionary<OperationKindCode, ServiceIntervalEntry>();
		private readonly List<ServicePriceEntry> _servicePrices = new List<ServicePriceEntry>();
		private readonly FuelSpecification _fuel = new FuelSpecification();

		private ServicingPolicy _servicingPolicy = ServicingPolicy.None;
		private RevisionState _state = RevisionState.Draft;
		private string _supersededByCode = string.Empty;

		/// <summary>
		/// Una revision sin codigo no se puede nombrar despues, asi que sus respuestas no podrian
		/// decir de donde salieron. El instante se recibe: el dominio no lee ningun reloj y no
		/// decide cuando una revision empieza a regir.
		/// </summary>
		internal PublishedRevision(string code, DateTime publishedAt)
		{
			if (string.IsNullOrWhiteSpace(code))
				throw new DomainException(
					"Una revision publicada sin codigo no se puede abrir: el codigo es lo que toda "
					+ "respuesta nombra como procedencia, y sin el la especificacion no puede decir "
					+ "de donde salio lo que contesto.");

			Code = code;
			PublishedAt = publishedAt;
		}

		/// <summary>Como se llama esta revision. Es lo que toda respuesta nombra.</summary>
		internal string Code { get; }

		/// <summary>
		/// Cuando el fabricante la publico, tal como llego. El journal lo guarda truncado a
		/// segundos enteros, asi que dos revisiones publicadas dentro del mismo segundo no se
		/// distinguen por aca: quien las ordene lo hace por orden de llegada.
		/// </summary>
		internal DateTime PublishedAt { get; }

		/// <summary>
		/// El nombre del estado. Es lo unico que sale del estado hacia afuera; el objeto no se
		/// entrega.
		/// </summary>
		internal string ReadState()
		{
			return _state.Name;
		}

		/// <summary>
		/// Si esta sellada. Es la unica condicion en la que una revision contesta, asi que toda
		/// lectura de la especificacion pasa por aca.
		/// </summary>
		internal bool IsSealed()
		{
			return _state == RevisionState.Sealed;
		}

		/// <summary>
		/// Si algo se publico adentro. Sellar una revision vacia no se puede: sellada quiere decir
		/// citable, y una revision vacia y sellada contestaria "no publicado" a todo con
		/// procedencia firmada.
		/// </summary>
		internal bool HasContent()
		{
			return _fluids.Count > 0
				|| _tyres.Count > 0
				|| _fuel.HasAGrade()
				|| _serviceIntervals.Count > 0
				|| _servicePrices.Count > 0
				|| _servicingPolicy.Exists();
		}

		/// <summary>
		/// Si otra revision la reemplazo. Es la pregunta TOTAL del par: nunca revienta, y es la que
		/// hay que hacer antes de leer el codigo del sucesor.
		/// </summary>
		internal bool HasBeenSuperseded()
		{
			return _state == RevisionState.Superseded;
		}

		/// <summary>
		/// El codigo de la revision que la reemplazo. Accesor GUARDADO: leerlo en una revision que
		/// nadie sustituyo es un defecto de quien llama, no una cadena vacia -- una cadena vacia
		/// confundiria "no la sustituyeron" con "la sustituyo una revision sin nombre".
		/// </summary>
		internal string SupersededByCode()
		{
			if (!HasBeenSuperseded())
				throw new DomainException(
					$"Se pidio el sucesor de la revision '{Code}', que esta {_state.Name} y no fue "
					+ "sustituida por ninguna. Hay que preguntar primero si fue sustituida.");

			return _supersededByCode;
		}

		/// <summary>
		/// Entra a la revision el fluido que el modelo lleva de esa clase. Solo un borrador lo
		/// acepta: publicar en una revision que ya contesta cambiaria lo que dijo sin que nadie la
		/// sustituyera. Dentro de un mismo borrador, la ultima publicacion de una clase es la que
		/// queda -- es una correccion antes de sellar, y el borrador todavia no contesto nada.
		/// </summary>
		internal void PublishAFluidSpecification(FluidKindCode kind, FluidSpecification fluid)
		{
			if (fluid == null)
				throw new DomainException(
					$"Se pidio publicar un fluido en la revision '{Code}' sin decir cual. Publicar "
					+ "nada le daria contenido a la revision sin darle nada que contestar.");

			RefuseContentUnlessItIsStillADraft();

			_fluids[kind] = fluid;
		}

		/// <summary>
		/// Si esta revision publico algo sobre esa clase de fluido. TOTAL: una revision publica lo
		/// que publica y no tiene por que traer las seis clases.
		/// </summary>
		internal bool HasAFluidSpecification(FluidKindCode kind)
		{
			return _fluids.ContainsKey(kind);
		}

		/// <summary>
		/// La clausula que compuso el fluido de esa clase. Accesor GUARDADO: pedirla de una clase
		/// que esta revision no publico es un defecto de quien llama, no una clausula vacia -- una
		/// clausula vacia se leeria como una especificacion sin grado y sin capacidad.
		/// </summary>
		internal string DescribeFluidSpecification(FluidKindCode kind)
		{
			if (!HasAFluidSpecification(kind))
				throw new DomainException(
					$"La revision '{Code}' no publico nada sobre {FluidKind.OfCode(kind).Name}. Hay "
					+ "que preguntar primero si lo publico.");

			return _fluids[kind].Describe();
		}

		/// <summary>
		/// Entra a la revision la llanta que el modelo lleva en esa posicion. Mismas reglas que el
		/// fluido: solo un borrador la acepta, y dentro de un mismo borrador la ultima publicacion
		/// de una posicion es la que queda.
		/// </summary>
		internal void PublishATyreSpecification(TyrePositionCode position, TyreSpecification tyre)
		{
			if (tyre == null)
				throw new DomainException(
					$"Se pidio publicar una llanta en la revision '{Code}' sin decir cual. Publicar "
					+ "nada le daria contenido a la revision sin darle nada que contestar.");

			RefuseContentUnlessItIsStillADraft();

			_tyres[position] = tyre;
		}

		/// <summary>
		/// Si esta revision publico algo sobre esa posicion. TOTAL: una revision publica lo que
		/// publica y no tiene por que traer las tres posiciones.
		/// </summary>
		internal bool HasATyreSpecification(TyrePositionCode position)
		{
			return _tyres.ContainsKey(position);
		}

		/// <summary>
		/// La clausula que compuso la llanta de esa posicion. Accesor GUARDADO: pedirla de una
		/// posicion que esta revision no publico es un defecto de quien llama, no una clausula vacia
		/// -- y aca una clausula vacia es una presion que alguien va a marcar en la bomba.
		/// </summary>
		internal string DescribeTyreSpecification(TyrePositionCode position)
		{
			if (!HasATyreSpecification(position))
				throw new DomainException(
					$"La revision '{Code}' no publico nada sobre las llantas "
					+ $"{TyrePosition.OfCode(position).Name}. Hay que preguntar primero si lo "
					+ "publico.");

			return _tyres[position].Describe();
		}

		/// <summary>
		/// Entra a la revision un grado de combustible con su postura. La especificacion de
		/// combustible de una revision se ARMA grado a grado: no hay clave que la subdivida, asi que
		/// la revision publica la especificacion entera y una revision posterior la reemplaza entera.
		/// </summary>
		internal void PublishAFuelGrade(FuelGrade grade)
		{
			RefuseContentUnlessItIsStillADraft();

			_fuel.AcceptAGrade(grade);
		}

		/// <summary>
		/// Si esta revision publico algun grado de combustible. TOTAL: una revision que no hablo de
		/// combustible contesta que no.
		/// </summary>
		internal bool HasAFuelSpecification()
		{
			return _fuel.HasAGrade();
		}

		/// <summary>
		/// La clausula que compuso el combustible. Accesor GUARDADO: pedirla de una revision que no
		/// publico ningun grado es un defecto de quien llama -- una lista vacia se leeria como que
		/// no hay ningun combustible prohibido.
		/// </summary>
		internal string DescribeFuelSpecification()
		{
			if (!HasAFuelSpecification())
				throw new DomainException(
					$"La revision '{Code}' no publico ningun grado de combustible. Hay que preguntar "
					+ "primero si lo publico.");

			return _fuel.Describe();
		}

		/// <summary>
		/// Entra a la revision cada cuanto publica el fabricante que se hace una operacion. Mismas
		/// reglas: solo un borrador lo acepta, y dentro de un borrador manda la ultima.
		/// </summary>
		internal void PublishAServiceInterval(OperationKindCode kind, ServiceIntervalEntry interval)
		{
			if (interval == null)
				throw new DomainException(
					$"Se pidio publicar un intervalo de servicio en la revision '{Code}' sin decir "
					+ "cual.");

			RefuseContentUnlessItIsStillADraft();

			_serviceIntervals[kind] = interval;
		}

		/// <summary>
		/// Si esta revision publico el intervalo de esa operacion. TOTAL: una revision no tiene por
		/// que traer las nueve clases.
		/// </summary>
		internal bool HasAServiceInterval(OperationKindCode kind)
		{
			return _serviceIntervals.ContainsKey(kind);
		}

		/// <summary>
		/// La clausula publicada del intervalo de esa operacion. Accesor GUARDADO: pedirla de una
		/// clase que esta revision no publico es un defecto de quien llama.
		/// </summary>
		internal string DescribeServiceInterval(OperationKindCode kind)
		{
			RefuseAnIntervalThisRevisionNeverPublished(kind);

			return _serviceIntervals[kind].Describe();
		}

		/// <summary>
		/// La mitad RECIBIDA de la respuesta del intervalo: donde queda este vehiculo contra el, con
		/// la distancia que le queda tal como llego. Se compone aca porque decidir si las dos
		/// distancias son comparables necesita la unidad publicada, que solo la revision tiene a
		/// mano; nada de lo recibido se guarda. Accesor GUARDADO, igual que la clausula publicada.
		/// </summary>
		internal string DescribeAgainstTheServiceInterval(
			OperationKindCode kind, int distanceRemaining, DistanceUnit remainingUnit)
		{
			RefuseAnIntervalThisRevisionNeverPublished(kind);

			return _serviceIntervals[kind].DescribeAgainst(distanceRemaining, remainingUnit);
		}

		/// <summary>
		/// Entra a la revision el precio publicado de una operacion en un hito. Los precios viven en
		/// una LISTA y se buscan por operacion mas hito: la clave son tres cosas juntas, y una clave
		/// compuesta seria igualdad de valor con parentesis. Dentro de un mismo borrador, la ultima
		/// publicacion de un precio es la que queda.
		/// </summary>
		internal void PublishAServicePrice(
			MaintenanceOperationKind kind, PublishedMilestone milestone, ServicePriceEntry price)
		{
			if (price == null)
				throw new DomainException(
					$"Se pidio publicar un precio en la revision '{Code}' sin decir cual.");

			RefuseContentUnlessItIsStillADraft();

			int published = ThePlaceOfThePriceFor(kind, milestone);

			if (published < 0) _servicePrices.Add(price);
			else _servicePrices[published] = price;
		}

		/// <summary>
		/// Si esta revision publico el precio de esa operacion en ese hito. TOTAL: un hito que no
		/// esta contesta que no, y el mismo numero en la otra unidad es otro hito.
		/// </summary>
		internal bool HasAServicePrice(MaintenanceOperationKind kind, PublishedMilestone milestone)
		{
			return ThePlaceOfThePriceFor(kind, milestone) >= 0;
		}

		/// <summary>
		/// El precio dicho con su vigencia. Accesor GUARDADO: pedirlo de un hito que esta revision
		/// no publico es un defecto de quien llama, no un monto en cero.
		/// </summary>
		internal string DescribeServicePrice(
			MaintenanceOperationKind kind, PublishedMilestone milestone)
		{
			return _servicePrices[ThePlaceOfAPublishedPrice(kind, milestone)]
				.APriceCarriesItsValidity();
		}

		/// <summary>
		/// Lo mismo, y ademas si esa vigencia rige en el instante recibido. Accesor GUARDADO por la
		/// misma razon.
		/// </summary>
		internal string DescribeServicePrice(
			MaintenanceOperationKind kind, PublishedMilestone milestone, DateTime asOf)
		{
			return _servicePrices[ThePlaceOfAPublishedPrice(kind, milestone)]
				.APriceCarriesItsValidity(asOf);
		}

		/// <summary>
		/// Entra a la revision la regla del fabricante sobre servicio en otro taller. Solo un
		/// borrador la acepta, y dentro de un borrador manda la ultima.
		/// </summary>
		internal void PublishAServicingPolicy(ServicingPolicy policy)
		{
			if (policy == null || !policy.Exists())
				throw new DomainException(
					$"Se pidio publicar una regla de servicio en otro taller en la revision '{Code}' "
					+ "sin decir cual.");

			RefuseContentUnlessItIsStillADraft();

			_servicingPolicy = policy;
		}

		/// <summary>
		/// Si esta revision publico la regla de servicio en otro taller. TOTAL: contesta siempre.
		/// </summary>
		internal bool HasAServicingPolicy()
		{
			return _servicingPolicy.Exists();
		}

		/// <summary>
		/// La clausula publicada de la regla. Accesor GUARDADO por la ausencia misma: la regla dice
		/// que no revienta preguntarle si existe, y si revienta pedirle el texto que no tiene.
		/// </summary>
		internal string DescribeServicingPolicy()
		{
			return _servicingPolicy.Describe();
		}

		/// <summary>
		/// La mitad RECIBIDA de la respuesta de la regla: los conteos de este vehiculo, dichos como
		/// conteos y sin concluir nada de ellos. Accesor GUARDADO, y nada de lo recibido se guarda.
		/// </summary>
		internal string DescribeAgainstTheServicingPolicy(
			int externalServicesWithEvidence, int externalServicesWithoutEvidence,
			int missedMaintenanceCount)
		{
			return _servicingPolicy.DescribeAgainst(
				externalServicesWithEvidence, externalServicesWithoutEvidence,
				missedMaintenanceCount);
		}

		/// <summary>
		/// La revision queda sellada y desde ahi contesta. Solo un borrador se sella, y solo uno
		/// con contenido: sellar dos veces o sellar una vacia es un defecto de quien publica.
		/// </summary>
		internal void Seal()
		{
			if (_state != RevisionState.Draft)
				throw new DomainException(
					$"Se pidio sellar la revision '{Code}', que esta {_state.Name}. Sellar es un "
					+ "hecho que pasa una sola vez: un segundo sello solo puede venir de un feed que "
					+ "perdio la cuenta.");

			if (!HasContent())
				throw new DomainException(
					$"Se pidio sellar la revision '{Code}' sin que se haya publicado nada adentro. "
					+ "Sellada quiere decir citable, asi que una revision vacia y sellada "
					+ "contestaria 'no publicado' a todo, firmado con una procedencia -- que es peor "
					+ "que no tener revision.");

			_state = RevisionState.Sealed;
		}

		/// <summary>
		/// Otra revision la reemplaza, y la nombra. Terminal. La sucesora tiene que estar sellada
		/// ella misma: si un borrador pudiera sustituir, la especificacion se quedaria sin ninguna
		/// revision que contesta -- la vieja apagada y la nueva invisible.
		/// </summary>
		internal void SupersededBy(PublishedRevision supersedingRevision)
		{
			if (supersedingRevision == null)
				throw new DomainException(
					$"Se pidio sustituir la revision '{Code}' sin nombrar la que la reemplaza. "
					+ "Sustituir sin sucesora es apagar la revision sin dejar donde seguir, y quien "
					+ "la tenga anotada como procedencia no llegaria a ninguna parte.");

			if (_state != RevisionState.Sealed)
				throw new DomainException(
					$"Se pidio sustituir la revision '{Code}', que esta {_state.Name}. Solo se "
					+ "sustituye la que estaba contestando: un borrador no ocupa el lugar de nadie, "
					+ "y una ya sustituida no vuelve a contestar.");

			if (ReferenceEquals(supersedingRevision, this))
				throw new DomainException(
					$"Se pidio que la revision '{Code}' se sustituya a si misma. Quedaria apagada "
					+ "nombrandose como el sitio donde seguir buscando.");

			if (!supersedingRevision.IsSealed())
				throw new DomainException(
					$"Se pidio sustituir la revision '{Code}' por '{supersedingRevision.Code}', que "
					+ $"esta {supersedingRevision.ReadState()}. La sucesora tiene que estar sellada: "
					+ "si no, la especificacion se queda sin ninguna revision que contesta.");

			_state = RevisionState.Superseded;
			_supersededByCode = supersedingRevision.Code;
		}

		private void RefuseAnIntervalThisRevisionNeverPublished(OperationKindCode kind)
		{
			if (!HasAServiceInterval(kind))
				throw new DomainException(
					$"La revision '{Code}' no publico cada cuanto se hace "
					+ $"{MaintenanceOperationKind.OfCode(kind).Name}. Hay que preguntar primero si "
					+ "lo publico.");
		}

		private int ThePlaceOfAPublishedPrice(
			MaintenanceOperationKind kind, PublishedMilestone milestone)
		{
			int published = ThePlaceOfThePriceFor(kind, milestone);

			if (published < 0)
				throw new DomainException(
					$"La revision '{Code}' no publico ningun precio de {kind.Name} en el hito "
					+ $"{milestone.Describe()}. Hay que preguntar primero si lo publico: un monto en "
					+ "cero ahi se leeria como un servicio incluido.");

			return published;
		}

		private int ThePlaceOfThePriceFor(
			MaintenanceOperationKind kind, PublishedMilestone milestone)
		{
			for (int published = 0; published < _servicePrices.Count; published++)
				if (_servicePrices[published].IsFor(kind, milestone)) return published;

			return -1;
		}

		private void RefuseContentUnlessItIsStillADraft()
		{
			if (_state != RevisionState.Draft)
				throw new DomainException(
					$"Se pidio publicar en la revision '{Code}', que esta {_state.Name}. Una "
					+ "revision deja de aceptar contenido cuando se sella, porque desde ese momento "
					+ "ya contesta y toda respuesta que dio la nombra.");
		}
	}
}
