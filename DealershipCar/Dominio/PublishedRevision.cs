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
			return _fluids.Count > 0;
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
