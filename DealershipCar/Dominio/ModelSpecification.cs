using System;
using System.Collections.Generic;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// Todo lo que el fabricante publica sobre un modelo: sus revisiones, ordenadas por instante
	/// de publicacion, con el contenido que se publico dentro de cada una. Es el agregado y la
	/// raiz global del DSL, y hay uno por <see cref="Model"/>.
	///
	/// No guarda NADA de ningun vehiculo en particular: sabe lo que un modelo requiere y no sabe
	/// nada de ningun carro. Eso es lo que deja que muchos vehiculos compartan una, y los hechos
	/// del carro que alguna lectura necesite entran como parametros y se van con la respuesta.
	/// </summary>
	internal sealed class ModelSpecification
	{
		private readonly Model _model;
		private readonly Dictionary<string, PublishedRevision> _revisions =
			new Dictionary<string, PublishedRevision>(StringComparer.Ordinal);
		private readonly List<PublishedRevision> _order = new List<PublishedRevision>();

		/// <summary>
		/// El constructor ES la creacion: el modelo del que es la especificacion, y nada mas. Las
		/// revisiones y su contenido llegan despues, cada una por su comando.
		/// </summary>
		internal ModelSpecification(string modelCode)
		{
			_model = new Model(modelCode);
		}

		/// <summary>
		/// ABRE una revision con el codigo y el instante que el fabricante publico. Nace borrador
		/// y el contenido entra despues. Un codigo que ya esta tomado se RECHAZA: reabrirlo
		/// cambiaria lo que una respuesta ya dada nombra como procedencia, o se perderia en
		/// silencio, y las dos son peores que el rechazo.
		/// </summary>
		internal void OpenRevision(string revisionCode, DateTime publishedAt)
		{
			PublishedRevision opening = new PublishedRevision(revisionCode, publishedAt);

			if (_revisions.ContainsKey(opening.Code))
				throw new DomainException(
					$"Esta especificacion ya tiene una revision '{opening.Code}'. Un codigo de "
					+ "revision nombra una sola publicacion para siempre: si se reabriera, lo que "
					+ "alguien recibio nombrando ese codigo pasaria a apuntar a otra cosa.");

			_revisions[opening.Code] = opening;
			_order.Insert(ItsPlaceOnThePublicationTimeline(opening), opening);
		}

		/// <summary>
		/// Publica dentro de una revision que fluido de una clase lleva el modelo. Se arma y se
		/// valida entero antes de entrar, y la revision decide si lo acepta: un borrador si, una
		/// que ya contesta no.
		/// </summary>
		internal void PublishFluidSpecification(
			string revisionCode, FluidKindCode kind, string grade,
			decimal capacity, VolumeUnitCode capacityUnit,
			int intervalDistance, DistanceUnitCode intervalUnit, int intervalDays)
		{
			PublishedRevision revision = TheRevisionNamed(revisionCode);

			FluidSpecification published = new FluidSpecification(
				FluidKind.OfCode(kind),
				grade,
				capacity,
				VolumeUnit.OfCode(capacityUnit),
				new PublishedInterval(
					intervalDistance, DistanceUnit.OfCode(intervalUnit), intervalDays));

			revision.PublishAFluidSpecification(kind, published);
		}

		/// <summary>
		/// SELLA una revision, que es lo unico que la hace contestar. Resuelve el codigo y deja
		/// que la revision decida: las guardas del sello son suyas y no se repiten aca.
		/// </summary>
		internal void SealRevision(string revisionCode)
		{
			TheRevisionNamed(revisionCode).Seal();
		}

		/// <summary>
		/// SUSTITUYE una revision por otra, que la reemplaza y queda nombrada. Los dos codigos se
		/// resuelven aca -- el registro es del agregado -- y a la revision se le entrega el OBJETO
		/// de su sucesora, porque la guarda de que la sucesora este sellada no se puede contestar
		/// desde un codigo.
		/// </summary>
		internal void SupersedeRevision(string revisionCode, string supersededByCode)
		{
			PublishedRevision superseded = TheRevisionNamed(revisionCode);
			PublishedRevision superseding = TheRevisionNamed(supersededByCode);

			superseded.SupersededBy(superseding);
		}

		/// <summary>
		/// El filtro por el que pasa toda lectura: una revision contesta unicamente mientras esta
		/// sellada. Un borrador se esta armando y una sustituida ya se apago, y ninguno de los dos
		/// dice nada -- por eso una hoja a medio cargar no se puede citar.
		/// </summary>
		internal bool OnlyASealedRevisionAnswers(PublishedRevision revision)
		{
			return revision.IsSealed();
		}

		/// <summary>
		/// Elige, para esa clase de fluido, la ULTIMA revision sellada que la publico, recorriendo
		/// las revisiones desde la mas nueva. De ahi salen las dos consecuencias: una revision que
		/// corrige un item no borra los que no republico, y sustituir SI retira lo que la sucesora
		/// no volvio a publicar. Si ninguna la publico, la ausencia es la respuesta.
		/// </summary>
		internal SpecificationAnswer ARevisionAppliesForward(FluidKindCode kind)
		{
			FluidKind asked = FluidKind.OfCode(kind);

			for (int revision = _order.Count - 1; revision >= 0; revision--)
			{
				PublishedRevision candidate = _order[revision];

				if (!OnlyASealedRevisionAnswers(candidate)) continue;
				if (!candidate.HasAFluidSpecification(kind)) continue;

				return SpecificationAnswer.EveryAnswerNamesItsRevision(
					candidate, candidate.DescribeFluidSpecification(kind));
			}

			return UnpublishedIsAnAnswer(asked.Name);
		}

		/// <summary>
		/// Lo no publicado se DICE, y este es el unico sitio donde esta especificacion lo compone:
		/// toda lectura con clave termina aca cuando no encuentra. Nunca un cero, nunca un default,
		/// nunca un valor prestado de otra clase o de otro ano.
		/// </summary>
		internal SpecificationAnswer UnpublishedIsAnAnswer(string subject)
		{
			return new AnUnpublishedSpecification(subject);
		}

		/// <summary>De que modelo es esta especificacion.</summary>
		internal string ReadModel()
		{
			return _model.Value;
		}

		/// <summary>
		/// Si hay algo publicado sobre esa clase de fluido. TOTAL: una clase que ninguna revision
		/// sellada publico contesta que no, y eso es una respuesta legal.
		/// </summary>
		internal bool IsFluidPublished(FluidKindCode kind)
		{
			return ARevisionAppliesForward(kind).IsPublished();
		}

		/// <summary>
		/// Que fluido de esa clase lleva el modelo, en una frase, con la revision adentro. TOTAL:
		/// lo no publicado tambien se dice.
		/// </summary>
		internal string DescribeFluid(FluidKindCode kind)
		{
			return ARevisionAppliesForward(kind).Describe();
		}

		/// <summary>
		/// De que revision salio la respuesta de esa clase de fluido. TOTAL: lo no publicado
		/// contesta vacio, que no se confunde con una revision porque ninguna tiene el codigo en
		/// blanco.
		/// </summary>
		internal string ReadFluidRevision(FluidKindCode kind)
		{
			return ARevisionAppliesForward(kind).RevisionCode();
		}

		/// <summary>
		/// Donde esta parada esa revision, por su nombre. Un codigo que este registro no tiene es
		/// un defecto de quien llama y REVIENTA: contestar un estado ahi inventaria una revision
		/// que nadie abrio.
		/// </summary>
		internal string ReadRevisionState(string revisionCode)
		{
			return TheRevisionNamed(revisionCode).ReadState();
		}

		private PublishedRevision TheRevisionNamed(string revisionCode)
		{
			if (!string.IsNullOrWhiteSpace(revisionCode)
				&& _revisions.ContainsKey(revisionCode))
				return _revisions[revisionCode];

			throw new DomainException(
				$"Esta especificacion no tiene ninguna revision '{revisionCode}'. Un codigo de "
				+ "revision nombra una entrada del registro, no un punto de un rango: si no esta, "
				+ "no hay un estado que contestar, hay una llamada mal hecha.");
		}

		/// <summary>
		/// Donde entra una revision en la linea de PUBLICACION. Manda el instante que el fabricante
		/// publico, y dentro de un mismo instante manda el orden de llegada -- el journal trunca a
		/// segundos enteros, asi que dos revisiones publicadas dentro del mismo segundo no se
		/// distinguen por otra cosa.
		/// </summary>
		private int ItsPlaceOnThePublicationTimeline(PublishedRevision arriving)
		{
			int place = _order.Count;

			while (place > 0 && _order[place - 1].PublishedAt > arriving.PublishedAt) place--;

			return place;
		}
	}
}
