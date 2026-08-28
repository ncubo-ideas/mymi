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
		/// Publica dentro de una revision que llanta lleva el modelo en una posicion. La presion se
		/// guarda con la unidad en que se publico y se contesta en esa.
		/// </summary>
		internal void PublishTyreSpecification(
			string revisionCode, TyrePositionCode position, string size,
			decimal coldPressure, PressureUnitCode pressureUnit, string ratings)
		{
			PublishedRevision revision = TheRevisionNamed(revisionCode);

			TyreSpecification published = new TyreSpecification(
				TyrePosition.OfCode(position),
				size,
				coldPressure,
				PressureUnit.OfCode(pressureUnit),
				ratings);

			revision.PublishATyreSpecification(position, published);
		}

		/// <summary>
		/// Publica dentro de una revision un grado de combustible con su postura. La especificacion
		/// de combustible de esa revision se arma grado a grado, y despues contesta entera: no hay
		/// clave que la subdivida, asi que una revision posterior la reemplaza toda.
		/// </summary>
		internal void PublishFuelGrade(
			string revisionCode, string grade, FuelGradeStandingCode standing)
		{
			PublishedRevision revision = TheRevisionNamed(revisionCode);

			revision.PublishAFuelGrade(new FuelGrade(grade, FuelGradeStanding.OfCode(standing)));
		}

		/// <summary>
		/// Publica dentro de una revision cada cuanto dice el fabricante que se hace una operacion.
		/// Es el numero PUBLICADO y nombra su revision, asi que es la verdad publicada; la copia por
		/// vehiculo que la agenda usa para medir sigue siendo la base de la medicion.
		/// </summary>
		internal void PublishServiceInterval(
			string revisionCode, OperationKindCode kind,
			int intervalDistance, DistanceUnitCode intervalUnit, int intervalDays)
		{
			PublishedRevision revision = TheRevisionNamed(revisionCode);

			ServiceIntervalEntry published = new ServiceIntervalEntry(
				MaintenanceOperationKind.OfCode(kind),
				new PublishedInterval(
					intervalDistance, DistanceUnit.OfCode(intervalUnit), intervalDays));

			revision.PublishAServiceInterval(kind, published);
		}

		/// <summary>
		/// Publica dentro de una revision el precio de una operacion en un hito, con su vigencia. El
		/// monto y la moneda entran tal como se publicaron y no se convierten ni se normalizan.
		/// </summary>
		internal void PublishServicePrice(
			string revisionCode, OperationKindCode kind,
			int milestoneDistance, DistanceUnitCode milestoneUnit,
			decimal amount, string currency, DateTime validFrom, DateTime validUntil)
		{
			PublishedRevision revision = TheRevisionNamed(revisionCode);

			MaintenanceOperationKind operation = MaintenanceOperationKind.OfCode(kind);
			PublishedMilestone milestone = new PublishedMilestone(
				milestoneDistance, DistanceUnit.OfCode(milestoneUnit));

			ServicePriceEntry published = new ServicePriceEntry(
				operation, milestone, amount, currency, validFrom, validUntil);

			revision.PublishAServicePrice(operation, milestone, published);
		}

		/// <summary>
		/// Publica dentro de una revision la regla del fabricante sobre servicio en otro taller y
		/// que conserva la cobertura. Los dos textos se guardan como se publicaron.
		/// </summary>
		internal void PublishServicingPolicy(
			string revisionCode, string rule, string whatPreservesCover)
		{
			PublishedRevision revision = TheRevisionNamed(revisionCode);

			revision.PublishAServicingPolicy(ServicingPolicy.Of(rule, whatPreservesCover));
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

		/// <summary>La misma ley para la llanta de una posicion.</summary>
		internal SpecificationAnswer ARevisionAppliesForward(TyrePositionCode position)
		{
			for (int revision = _order.Count - 1; revision >= 0; revision--)
			{
				PublishedRevision candidate = _order[revision];

				if (!OnlyASealedRevisionAnswers(candidate)) continue;
				if (!candidate.HasATyreSpecification(position)) continue;

				return SpecificationAnswer.EveryAnswerNamesItsRevision(
					candidate, candidate.DescribeTyreSpecification(position));
			}

			return UnpublishedIsAnAnswer(TheSubjectOfATyre(position));
		}

		/// <summary>
		/// La misma ley para el combustible, que no tiene clave: el item es la especificacion
		/// entera, asi que la ultima revision sellada que publico algun grado contesta con TODOS los
		/// suyos y ninguno de los de otra revision.
		/// </summary>
		internal SpecificationAnswer ARevisionAppliesForward()
		{
			for (int revision = _order.Count - 1; revision >= 0; revision--)
			{
				PublishedRevision candidate = _order[revision];

				if (!OnlyASealedRevisionAnswers(candidate)) continue;
				if (!candidate.HasAFuelSpecification()) continue;

				return SpecificationAnswer.EveryAnswerNamesItsRevision(
					candidate, candidate.DescribeFuelSpecification());
			}

			return UnpublishedIsAnAnswer(TheSubjectOfTheFuel);
		}

		/// <summary>
		/// La misma ley para el intervalo publicado de una operacion, con su mitad publicada nada
		/// mas: es la que contestan la presencia y la procedencia, que no preguntan por ningun
		/// vehiculo.
		/// </summary>
		internal SpecificationAnswer ARevisionAppliesForward(OperationKindCode kind)
		{
			for (int revision = _order.Count - 1; revision >= 0; revision--)
			{
				PublishedRevision candidate = _order[revision];

				if (!OnlyASealedRevisionAnswers(candidate)) continue;
				if (!candidate.HasAServiceInterval(kind)) continue;

				return SpecificationAnswer.EveryAnswerNamesItsRevision(
					candidate, candidate.DescribeServiceInterval(kind));
			}

			return UnpublishedIsAnAnswer(TheSubjectOfAServiceInterval(kind));
		}

		/// <summary>
		/// La misma ley, y ademas la mitad RECIBIDA: lo publicado emparejado con la distancia que le
		/// queda a este vehiculo. Sin nada publicado la respuesta es la ausencia sola, porque no hay
		/// contra que parar lo recibido.
		/// </summary>
		internal SpecificationAnswer ARevisionAppliesForward(
			OperationKindCode kind, int distanceRemaining, DistanceUnitCode remainingUnit)
		{
			DistanceUnit received = DistanceUnit.OfCode(remainingUnit);

			for (int revision = _order.Count - 1; revision >= 0; revision--)
			{
				PublishedRevision candidate = _order[revision];

				if (!OnlyASealedRevisionAnswers(candidate)) continue;
				if (!candidate.HasAServiceInterval(kind)) continue;

				return SpecificationAnswer.EveryAnswerNamesItsRevision(
					candidate,
					GeneralAndPersonalTogether(
						candidate.DescribeServiceInterval(kind),
						candidate.DescribeAgainstTheServiceInterval(
							kind, distanceRemaining, received)));
			}

			return UnpublishedIsAnAnswer(TheSubjectOfAServiceInterval(kind));
		}

		/// <summary>
		/// La misma ley para el precio de una operacion en un hito, con su vigencia y sin decir
		/// todavia si rige: eso necesita un instante que la presencia y la procedencia no piden.
		/// </summary>
		internal SpecificationAnswer ARevisionAppliesForward(
			OperationKindCode kind, PublishedMilestone milestone)
		{
			MaintenanceOperationKind operation = MaintenanceOperationKind.OfCode(kind);

			for (int revision = _order.Count - 1; revision >= 0; revision--)
			{
				PublishedRevision candidate = _order[revision];

				if (!OnlyASealedRevisionAnswers(candidate)) continue;
				if (!candidate.HasAServicePrice(operation, milestone)) continue;

				return SpecificationAnswer.EveryAnswerNamesItsRevision(
					candidate, candidate.DescribeServicePrice(operation, milestone));
			}

			return UnpublishedIsAnAnswer(TheSubjectOfAServicePrice(kind));
		}

		/// <summary>
		/// La misma ley, y ademas si esa vigencia rige EN EL INSTANTE RECIBIDO. El dominio no lee
		/// ningun reloj: sin ese instante no hay forma de decir "vencido" respecto a nada.
		/// </summary>
		internal SpecificationAnswer ARevisionAppliesForward(
			OperationKindCode kind, PublishedMilestone milestone, DateTime asOf)
		{
			MaintenanceOperationKind operation = MaintenanceOperationKind.OfCode(kind);

			for (int revision = _order.Count - 1; revision >= 0; revision--)
			{
				PublishedRevision candidate = _order[revision];

				if (!OnlyASealedRevisionAnswers(candidate)) continue;
				if (!candidate.HasAServicePrice(operation, milestone)) continue;

				return SpecificationAnswer.EveryAnswerNamesItsRevision(
					candidate, candidate.DescribeServicePrice(operation, milestone, asOf));
			}

			return UnpublishedIsAnAnswer(TheSubjectOfAServicePrice(kind));
		}

		/// <summary>
		/// La misma ley para la regla de servicio en otro taller, con su mitad publicada nada mas.
		/// Lleva el nombre entero en vez de ser una sobrecarga por una razon del lenguaje y no del
		/// dominio: el combustible tampoco tiene clave, y dos sobrecargas sin parametros no se
		/// pueden declarar.
		/// </summary>
		internal SpecificationAnswer ARevisionAppliesForwardToTheServicingPolicy()
		{
			for (int revision = _order.Count - 1; revision >= 0; revision--)
			{
				PublishedRevision candidate = _order[revision];

				if (!OnlyASealedRevisionAnswers(candidate)) continue;
				if (!candidate.HasAServicingPolicy()) continue;

				return SpecificationAnswer.EveryAnswerNamesItsRevision(
					candidate, candidate.DescribeServicingPolicy());
			}

			return UnpublishedIsAnAnswer(TheSubjectOfTheServicingPolicy);
		}

		/// <summary>
		/// La misma ley, y ademas la mitad RECIBIDA: la regla publicada emparejada con los conteos
		/// de este vehiculo. Sin regla publicada la respuesta es la ausencia sola.
		/// </summary>
		internal SpecificationAnswer ARevisionAppliesForwardToTheServicingPolicy(
			int externalServicesWithEvidence, int externalServicesWithoutEvidence,
			int missedMaintenanceCount)
		{
			for (int revision = _order.Count - 1; revision >= 0; revision--)
			{
				PublishedRevision candidate = _order[revision];

				if (!OnlyASealedRevisionAnswers(candidate)) continue;
				if (!candidate.HasAServicingPolicy()) continue;

				return SpecificationAnswer.EveryAnswerNamesItsRevision(
					candidate,
					GeneralAndPersonalTogether(
						candidate.DescribeServicingPolicy(),
						candidate.DescribeAgainstTheServicingPolicy(
							externalServicesWithEvidence, externalServicesWithoutEvidence,
							missedMaintenanceCount)));
			}

			return UnpublishedIsAnAnswer(TheSubjectOfTheServicingPolicy);
		}

		/// <summary>
		/// Empareja la verdad PUBLICADA con la posicion de ESTE vehiculo frente a ella, que es el
		/// producto entero dicho en una linea. Las dos mitades son obligatorias y por eso esto
		/// revienta sin ellas: la publicada sola es un folleto, y la del vehiculo sola no significa
		/// nada. Solo se llama cuando hay algo publicado: una ausencia no se empareja con nada.
		/// </summary>
		internal string GeneralAndPersonalTogether(string publishedClause, string receivedClause)
		{
			if (string.IsNullOrWhiteSpace(publishedClause))
				throw new DomainException(
					"Se quiso emparejar la posicion de un vehiculo con nada publicado. Sin la mitad "
					+ "publicada no hay contra que pararla, y unos numeros solos no dicen nada.");

			if (string.IsNullOrWhiteSpace(receivedClause))
				throw new DomainException(
					"Se quiso contestar con lo publicado y nada de este vehiculo. Eso es un folleto: "
					+ "dice lo que el modelo requiere y no dice donde esta parado este carro.");

			return publishedClause + " " + receivedClause;
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

		/// <summary>Si hay algo publicado sobre las llantas de esa posicion. TOTAL.</summary>
		internal bool IsTyrePublished(TyrePositionCode position)
		{
			return ARevisionAppliesForward(position).IsPublished();
		}

		/// <summary>
		/// Que llanta lleva el modelo en esa posicion, en una frase, con la revision adentro. La
		/// presion sale en la unidad en que se publico. TOTAL.
		/// </summary>
		internal string DescribeTyre(TyrePositionCode position)
		{
			return ARevisionAppliesForward(position).Describe();
		}

		/// <summary>De que revision salio la respuesta de esa posicion. TOTAL.</summary>
		internal string ReadTyreRevision(TyrePositionCode position)
		{
			return ARevisionAppliesForward(position).RevisionCode();
		}

		/// <summary>
		/// Si hay algo publicado sobre el combustible. TOTAL: un modelo sin ninguna revision
		/// contesta que no en vez de reventar.
		/// </summary>
		internal bool IsFuelPublished()
		{
			return ARevisionAppliesForward().IsPublished();
		}

		/// <summary>
		/// Que combustible acepta el modelo: los grados publicados con su postura, con la revision
		/// adentro. TOTAL.
		/// </summary>
		internal string DescribeFuel()
		{
			return ARevisionAppliesForward().Describe();
		}

		/// <summary>De que revision salio la respuesta del combustible. TOTAL.</summary>
		internal string ReadFuelRevision()
		{
			return ARevisionAppliesForward().RevisionCode();
		}

		/// <summary>Si hay publicado un intervalo para esa operacion. TOTAL.</summary>
		internal bool IsServiceIntervalPublished(OperationKindCode kind)
		{
			return ARevisionAppliesForward(kind).IsPublished();
		}

		/// <summary>
		/// Cada cuanto publica el fabricante que se hace esa operacion Y donde queda este vehiculo
		/// contra ese intervalo, con la distancia que le queda tal como llego. Las dos mitades, o es
		/// un folleto. TOTAL: lo no publicado se dice, y ahi no se empareja nada.
		/// </summary>
		internal string DescribeServiceInterval(
			OperationKindCode kind, int distanceRemaining, DistanceUnitCode remainingUnit)
		{
			return ARevisionAppliesForward(kind, distanceRemaining, remainingUnit).Describe();
		}

		/// <summary>De que revision salio el intervalo de esa operacion. TOTAL.</summary>
		internal string ReadServiceIntervalRevision(OperationKindCode kind)
		{
			return ARevisionAppliesForward(kind).RevisionCode();
		}

		/// <summary>
		/// Si hay publicado un precio para esa operacion en ese hito. TOTAL: un hito que nadie
		/// publico contesta que no, y una distancia de hito que no es positiva no nombra ningun
		/// hito, asi que ahi tampoco hay nada publicado.
		/// </summary>
		internal bool IsServicePricePublished(
			OperationKindCode kind, int milestoneDistance, DistanceUnitCode milestoneUnit)
		{
			if (milestoneDistance <= 0) return false;

			return ARevisionAppliesForward(
				kind, TheMilestone(milestoneDistance, milestoneUnit)).IsPublished();
		}

		/// <summary>
		/// Cuanto cuesta esa operacion en ese hito, con su moneda, su vigencia, si esa vigencia rige
		/// en el instante recibido, y la revision. TOTAL.
		/// </summary>
		internal string DescribeServicePrice(
			OperationKindCode kind, int milestoneDistance, DistanceUnitCode milestoneUnit,
			DateTime asOf)
		{
			if (milestoneDistance <= 0)
				return UnpublishedIsAnAnswer(TheSubjectOfAServicePrice(kind)).Describe();

			return ARevisionAppliesForward(
				kind, TheMilestone(milestoneDistance, milestoneUnit), asOf).Describe();
		}

		/// <summary>De que revision salio el precio de esa operacion en ese hito. TOTAL.</summary>
		internal string ReadServicePriceRevision(
			OperationKindCode kind, int milestoneDistance, DistanceUnitCode milestoneUnit)
		{
			if (milestoneDistance <= 0)
				return UnpublishedIsAnAnswer(TheSubjectOfAServicePrice(kind)).RevisionCode();

			return ARevisionAppliesForward(
				kind, TheMilestone(milestoneDistance, milestoneUnit)).RevisionCode();
		}

		/// <summary>Si hay publicada una regla de servicio en otro taller. TOTAL.</summary>
		internal bool IsServicingPolicyPublished()
		{
			return ARevisionAppliesForwardToTheServicingPolicy().IsPublished();
		}

		/// <summary>
		/// La regla publicada sobre servicio en otro taller Y donde queda este vehiculo frente a
		/// ella, con los conteos tal como llegaron. Dice el alcance y la posicion; no dice que
		/// ninguna reclamacion se vaya a pagar. TOTAL.
		/// </summary>
		internal string DescribeServicingElsewhereRule(
			int externalServicesWithEvidence, int externalServicesWithoutEvidence,
			int missedMaintenanceCount)
		{
			return ARevisionAppliesForwardToTheServicingPolicy(
				externalServicesWithEvidence, externalServicesWithoutEvidence,
				missedMaintenanceCount).Describe();
		}

		/// <summary>De que revision salio la regla de servicio en otro taller. TOTAL.</summary>
		internal string ReadServicingPolicyRevision()
		{
			return ARevisionAppliesForwardToTheServicingPolicy().RevisionCode();
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

		private const string TheSubjectOfTheFuel = "Fuel grades";
		private const string TheSubjectOfTheServicingPolicy = "The servicing-elsewhere rule";

		private static string TheSubjectOfATyre(TyrePositionCode position)
		{
			return TyrePosition.OfCode(position).Name + " tyre";
		}

		private static string TheSubjectOfAServiceInterval(OperationKindCode kind)
		{
			return MaintenanceOperationKind.OfCode(kind).Name + " service interval";
		}

		private static string TheSubjectOfAServicePrice(OperationKindCode kind)
		{
			return MaintenanceOperationKind.OfCode(kind).Name
				+ " service price at the milestone asked about";
		}

		private static PublishedMilestone TheMilestone(
			int milestoneDistance, DistanceUnitCode milestoneUnit)
		{
			return new PublishedMilestone(milestoneDistance, DistanceUnit.OfCode(milestoneUnit));
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
