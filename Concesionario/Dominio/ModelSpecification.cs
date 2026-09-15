using System;
using System.Collections.Generic;

namespace Concesionario.Dominio
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
		/// monto y la moneda llegan por separado y se vuelven un solo dinero aca, antes de armar la
		/// entrada; ninguno de los dos se convierte ni se normaliza. Una moneda que este modulo no
		/// nombra se recibe igual: el precio ya se publico.
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
			Money price = new Money(amount, Currency.OfPublished(currency));

			ServicePriceEntry published = new ServicePriceEntry(
				operation, milestone, price, validFrom, validUntil);

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
		/// Publica dentro de una revision un sistema que el fabricante declara dentro del alcance
		/// de la garantia, con sus limites propios si los publico. Un cero en las dos mitades es
		/// como el wire dice que NO tiene limites propios, y esa traduccion pasa aca: un cero
		/// tomado como limite dejaria al sistema fuera de su sobre el dia de la entrega.
		/// </summary>
		internal void PublishCoveredSystem(
			string revisionCode, string systemCode,
			int ownLimitMonths, int ownLimitDistance, DistanceUnitCode ownLimitUnit)
		{
			PublishedRevision revision = TheRevisionNamed(revisionCode);

			CoveredSystem published = new CoveredSystem(
				systemCode,
				SystemLimits.OfTheSystemsOwn(
					ownLimitMonths, ownLimitDistance, DistanceUnit.OfCode(ownLimitUnit)));

			revision.PublishACoveredSystem(published);
		}

		/// <summary>
		/// Publica dentro de una revision algo que el alcance NO cubre, con la condicion bajo la
		/// cual muerde. La condicion viaja como texto publicado: este modulo la nombra y no decide
		/// si se cumple.
		/// </summary>
		internal void PublishCoverageExclusion(
			string revisionCode, string systemCode, string exclusion, string condition)
		{
			PublishedRevision revision = TheRevisionNamed(revisionCode);

			CoverageExclusion published = new CoverageExclusion(systemCode, exclusion, condition);

			revision.PublishACoverageExclusion(published);
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
		internal AFluidClause ARevisionAppliesForward(FluidKindCode kind)
		{
			for (int revision = _order.Count - 1; revision >= 0; revision--)
			{
				PublishedRevision candidate = _order[revision];

				if (!OnlyASealedRevisionAnswers(candidate)) continue;
				if (!candidate.HasAFluidSpecification(kind)) continue;

				return candidate.TheFluidClause(kind);
			}

			return AFluidClause.OfNothingPublished(FluidKind.OfCode(kind));
		}

		/// <summary>La misma ley para la llanta de una posicion.</summary>
		internal ATyreClause ARevisionAppliesForward(TyrePositionCode position)
		{
			for (int revision = _order.Count - 1; revision >= 0; revision--)
			{
				PublishedRevision candidate = _order[revision];

				if (!OnlyASealedRevisionAnswers(candidate)) continue;
				if (!candidate.HasATyreSpecification(position)) continue;

				return candidate.TheTyreClause(position);
			}

			return ATyreClause.OfNothingPublished(TyrePosition.OfCode(position));
		}

		/// <summary>
		/// La misma ley para el combustible, que no tiene clave: el item es la especificacion
		/// entera, asi que la ultima revision sellada que publico algun grado contesta con TODOS los
		/// suyos y ninguno de los de otra revision.
		/// </summary>
		internal AFuelClause ARevisionAppliesForward()
		{
			for (int revision = _order.Count - 1; revision >= 0; revision--)
			{
				PublishedRevision candidate = _order[revision];

				if (!OnlyASealedRevisionAnswers(candidate)) continue;
				if (!candidate.HasAFuelSpecification()) continue;

				return candidate.TheFuelClause();
			}

			return AFuelClause.OfNothingPublished();
		}

		/// <summary>
		/// La misma ley para el intervalo publicado de una operacion, con su mitad publicada nada
		/// mas: es la que contestan la presencia y la procedencia, que no preguntan por ningun
		/// vehiculo.
		/// </summary>
		internal AServiceIntervalClause ARevisionAppliesForward(OperationKindCode kind)
		{
			for (int revision = _order.Count - 1; revision >= 0; revision--)
			{
				PublishedRevision candidate = _order[revision];

				if (!OnlyASealedRevisionAnswers(candidate)) continue;
				if (!candidate.HasAServiceInterval(kind)) continue;

				return candidate.TheServiceIntervalClause(kind);
			}

			return AServiceIntervalClause.OfNothingPublished(
				MaintenanceOperationKind.OfCode(kind));
		}

		/// <summary>
		/// La misma ley, y ademas la mitad RECIBIDA: lo publicado emparejado con la distancia que le
		/// queda a este vehiculo. Sin nada publicado la respuesta es la ausencia sola, porque no hay
		/// contra que parar lo recibido.
		/// </summary>
		internal AServiceIntervalClause ARevisionAppliesForward(
			OperationKindCode kind, int distanceRemaining, DistanceUnitCode remainingUnit)
		{
			DistanceUnit received = DistanceUnit.OfCode(remainingUnit);

			for (int revision = _order.Count - 1; revision >= 0; revision--)
			{
				PublishedRevision candidate = _order[revision];

				if (!OnlyASealedRevisionAnswers(candidate)) continue;
				if (!candidate.HasAServiceInterval(kind)) continue;

				return candidate.TheServiceIntervalClauseAgainst(
					kind, distanceRemaining, received);
			}

			return AServiceIntervalClause.OfNothingPublished(
				MaintenanceOperationKind.OfCode(kind));
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
		/// ningun reloj: sin ese instante no hay forma de decir "vencido" respecto a nada. Resuelve
		/// el codigo del wire y delega: el cuerpo es uno solo, y es el que la enumeracion tambien
		/// recorre.
		/// </summary>
		internal SpecificationAnswer ARevisionAppliesForward(
			OperationKindCode kind, PublishedMilestone milestone, DateTime asOf)
		{
			return ARevisionAppliesForward(
				MaintenanceOperationKind.OfCode(kind), milestone, asOf);
		}

		/// <summary>
		/// La misma ley con la clase de operacion ya resuelta. Es el UNICO cuerpo que contesta el
		/// precio de un hito en un instante: la lectura llaveada llega envuelta y la enumeracion
		/// llega derecho, asi que las dos dicen lo mismo porque no hay dos sitios que decirlo.
		/// </summary>
		internal SpecificationAnswer ARevisionAppliesForward(
			MaintenanceOperationKind operation, PublishedMilestone milestone, DateTime asOf)
		{
			for (int revision = _order.Count - 1; revision >= 0; revision--)
			{
				PublishedRevision candidate = _order[revision];

				if (!OnlyASealedRevisionAnswers(candidate)) continue;
				if (!candidate.HasAServicePrice(operation, milestone)) continue;

				return SpecificationAnswer.EveryAnswerNamesItsRevision(
					candidate, candidate.DescribeServicePrice(operation, milestone, asOf));
			}

			return UnpublishedIsAnAnswer(TheSubjectOfAServicePrice(operation));
		}

		/// <summary>
		/// La misma ley para la regla de servicio en otro taller, con su mitad publicada nada mas.
		/// Lleva el nombre entero en vez de ser una sobrecarga por una razon del lenguaje y no del
		/// dominio: el combustible tampoco tiene clave, y dos sobrecargas sin parametros no se
		/// pueden declarar.
		/// </summary>
		internal AServicingClause ARevisionAppliesForwardToTheServicingPolicy()
		{
			for (int revision = _order.Count - 1; revision >= 0; revision--)
			{
				PublishedRevision candidate = _order[revision];

				if (!OnlyASealedRevisionAnswers(candidate)) continue;
				if (!candidate.HasAServicingPolicy()) continue;

				return candidate.TheServicingPolicyClause();
			}

			return AServicingClause.OfNothingPublished();
		}

		/// <summary>
		/// La misma ley, y ademas la mitad RECIBIDA: la regla publicada emparejada con los conteos
		/// de este vehiculo. Sin regla publicada la respuesta es la ausencia sola.
		/// </summary>
		internal AServicingClause ARevisionAppliesForwardToTheServicingPolicy(
			int externalServicesWithEvidence, int externalServicesWithoutEvidence,
			int missedMaintenanceCount)
		{
			for (int revision = _order.Count - 1; revision >= 0; revision--)
			{
				PublishedRevision candidate = _order[revision];

				if (!OnlyASealedRevisionAnswers(candidate)) continue;
				if (!candidate.HasAServicingPolicy()) continue;

				return candidate.TheServicingPolicyClauseAgainst(
					externalServicesWithEvidence, externalServicesWithoutEvidence,
					missedMaintenanceCount);
			}

			return AServicingClause.OfNothingPublished();
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
		/// Que fluido de esa clase lleva el modelo, como forma y valores, con la revision al lado.
		/// TOTAL: lo no publicado tambien contesta, nombrando lo que se pregunto.
		/// </summary>
		internal AFluidClause TheFluidAnswer(FluidKindCode kind)
		{
			return ARevisionAppliesForward(kind);
		}

		/// <summary>
		/// De que revision salio la respuesta de esa clase de fluido. TOTAL: lo no publicado
		/// contesta vacio, que no se confunde con una revision porque ninguna tiene el codigo en
		/// blanco.
		/// </summary>
		internal string ReadFluidRevision(FluidKindCode kind)
		{
			return ARevisionAppliesForward(kind).TheRevisionItCameFrom();
		}

		/// <summary>Si hay algo publicado sobre las llantas de esa posicion. TOTAL.</summary>
		internal bool IsTyrePublished(TyrePositionCode position)
		{
			return ARevisionAppliesForward(position).IsPublished();
		}

		/// <summary>
		/// Que llanta lleva el modelo en esa posicion, como forma y valores, con la revision al lado.
		/// La presion sale en la unidad en que se publico. TOTAL.
		/// </summary>
		internal ATyreClause TheTyreAnswer(TyrePositionCode position)
		{
			return ARevisionAppliesForward(position);
		}

		/// <summary>De que revision salio la respuesta de esa posicion. TOTAL.</summary>
		internal string ReadTyreRevision(TyrePositionCode position)
		{
			return ARevisionAppliesForward(position).TheRevisionItCameFrom();
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
		/// Que combustible acepta el modelo: los grados publicados con su postura, con la revision al
		/// lado. TOTAL.
		/// </summary>
		internal AFuelClause TheFuelAnswer()
		{
			return ARevisionAppliesForward();
		}

		/// <summary>
		/// El grado MINIMO publicado, suelto y sin su postura, que es como se pide en una bomba.
		/// TOTAL en las dos ausencias que existen: sin combustible publicado contesta vacio, y con
		/// combustible publicado en el que nadie marco un minimo tambien. El vacio no se confunde
		/// con un grado porque uno en blanco no se deja publicar, y cual de las dos ausencias es la
		/// contesta la lectura de presencia.
		/// </summary>
		internal string ReadMinimumFuelGrade()
		{
			if (!ARevisionAnswersAboutFuel()) return "";

			return TheRevisionThatAnswersAboutFuel().TheMinimumFuelGradePublished();
		}

		/// <summary>
		/// Los grados publicados como PERMITIDOS, cada uno con su texto tal como se publico. TOTAL:
		/// sin combustible publicado, vacio -- y una lista vacia dice que no hay ninguno permitido,
		/// que es distinto de decir que cualquiera lo esta.
		/// </summary>
		internal string[] PermittedFuelGrades()
		{
			if (!ARevisionAnswersAboutFuel()) return new string[0];

			return TheRevisionThatAnswersAboutFuel().ThePermittedFuelGradesPublished();
		}

		/// <summary>
		/// Los grados publicados como PROHIBIDOS. TOTAL, y es la lista que mas importa que salga
		/// entera: un prohibido callado es un motor danado. Vacio quiere decir que ninguno se
		/// publico como prohibido, no que ninguno lo sea.
		/// </summary>
		internal string[] ProhibitedFuelGrades()
		{
			if (!ARevisionAnswersAboutFuel()) return new string[0];

			return TheRevisionThatAnswersAboutFuel().TheProhibitedFuelGradesPublished();
		}

		/// <summary>De que revision salio la respuesta del combustible. TOTAL.</summary>
		internal string ReadFuelRevision()
		{
			return ARevisionAppliesForward().TheRevisionItCameFrom();
		}

		/// <summary>Si hay publicado un intervalo para esa operacion. TOTAL.</summary>
		internal bool IsServiceIntervalPublished(OperationKindCode kind)
		{
			return ARevisionAppliesForward(kind).IsPublished();
		}

		/// <summary>
		/// Cada cuanto publica el fabricante que se hace esa operacion Y donde queda este vehiculo
		/// contra ese intervalo, con la distancia que le queda tal como llego. Las dos mitades, o es
		/// un folleto. TOTAL: lo no publicado contesta, y ahi no se empareja nada.
		/// </summary>
		internal AServiceIntervalClause TheServiceIntervalAnswer(
			OperationKindCode kind, int distanceRemaining, DistanceUnitCode remainingUnit)
		{
			return ARevisionAppliesForward(kind, distanceRemaining, remainingUnit);
		}

		/// <summary>
		/// Cada cuanto publica el fabricante que se hace esa operacion, con su mitad publicada nada
		/// mas. No pregunta por ningun vehiculo, asi que la contesta tambien quien no tenga ningun
		/// hecho de ninguno. TOTAL.
		/// </summary>
		internal AServiceIntervalClause ThePublishedServiceIntervalAnswer(OperationKindCode kind)
		{
			return ARevisionAppliesForward(kind);
		}

		/// <summary>De que revision salio el intervalo de esa operacion. TOTAL.</summary>
		internal string ReadServiceIntervalRevision(OperationKindCode kind)
		{
			return ARevisionAppliesForward(kind).TheRevisionItCameFrom();
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
		internal SpecificationAnswer TheServicePriceAnswer(
			OperationKindCode kind, int milestoneDistance, DistanceUnitCode milestoneUnit,
			DateTime asOf)
		{
			if (milestoneDistance <= 0)
				return UnpublishedIsAnAnswer(TheSubjectOfAServicePrice(kind));

			return ARevisionAppliesForward(
				kind, TheMilestone(milestoneDistance, milestoneUnit), asOf);
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

		/// <summary>
		/// Cuanto cuesta esa operacion en ese hito, con su moneda, su vigencia y la revision, y SIN
		/// decir si rige: eso necesita un instante, y quien no tenga ninguno que aportar igual puede
		/// citar lo que el fabricante publico. TOTAL.
		/// </summary>
		internal SpecificationAnswer ThePublishedServicePriceAnswer(
			OperationKindCode kind, int milestoneDistance, DistanceUnitCode milestoneUnit)
		{
			if (milestoneDistance <= 0)
				return UnpublishedIsAnAnswer(TheSubjectOfAServicePrice(kind));

			return ARevisionAppliesForward(
				kind, TheMilestone(milestoneDistance, milestoneUnit));
		}

		/// <summary>
		/// Si alguna revision sellada publico algun precio. TOTAL: es la pregunta que hay que hacer
		/// antes de leer las filas, y no recibe instante porque sellada es un estado y no un
		/// instante.
		/// </summary>
		internal bool HasAnyPublishedServicePrice()
		{
			return ThePriceKeysThatGovern().Count > 0;
		}

		/// <summary>
		/// Cuantos precios publicados hay. DERIVADO de la enumeracion en cada lectura y nunca
		/// guardado, asi que no puede quedar en desacuerdo con las filas. TOTAL.
		/// </summary>
		internal int PublishedServicePriceCount()
		{
			return ThePriceKeysThatGovern().Count;
		}

		/// <summary>
		/// De que operacion es cada precio publicado, por el NOMBRE del miembro, una por fila y de la
		/// revision mas nueva a la mas vieja. TOTAL.
		/// </summary>
		internal string[] PublishedPriceOperationsAsOf(DateTime asOf)
		{
			List<APublishedPriceRow> rows = ThePublishedPricesAsOf(asOf);

			string[] said = new string[rows.Count];

			for (int row = 0; row < rows.Count; row++) said[row] = rows[row].TheNameOfItsOperation();

			return said;
		}

		/// <summary>
		/// A que hito se publico cada precio, dicho con su unidad, una por fila y en el mismo orden.
		/// TOTAL.
		/// </summary>
		internal string[] PublishedPriceMilestonesAsOf(DateTime asOf)
		{
			List<APublishedPriceRow> rows = ThePublishedPricesAsOf(asOf);

			string[] said = new string[rows.Count];

			for (int row = 0; row < rows.Count; row++)
				said[row] = rows[row].TheMilestoneItWasPublishedAt();

			return said;
		}

		/// <summary>
		/// Si hay algo publicado detras de cada fila, por su codigo, una por fila y en el mismo orden.
		/// TOTAL, y todas contestan lo mismo: una fila nace de una clave que rige, y una clave que rige
		/// sale de una revision sellada que la publico.
		/// </summary>
		internal string[] PublishedPriceShapesAsOf(DateTime asOf)
		{
			List<APublishedPriceRow> rows = ThePublishedPricesAsOf(asOf);

			string[] said = new string[rows.Count];

			for (int row = 0; row < rows.Count; row++) said[row] = rows[row].TheShapeOfItsAnswer();

			return said;
		}

		/// <summary>
		/// Lo que cada precio publicado tiene que decir -- su monto, su moneda, su vigencia y si rige
		/// en el instante preguntado --, una por fila y en el mismo orden. TOTAL.
		/// </summary>
		internal string[] PublishedPriceClausesAsOf(DateTime asOf)
		{
			List<APublishedPriceRow> rows = ThePublishedPricesAsOf(asOf);

			string[] said = new string[rows.Count];

			for (int row = 0; row < rows.Count; row++) said[row] = rows[row].TheClauseItCarries();

			return said;
		}

		/// <summary>
		/// De que revision salio cada precio publicado, una por fila y en el mismo orden. TOTAL.
		/// </summary>
		internal string[] PublishedPriceRevisionsAsOf(DateTime asOf)
		{
			List<APublishedPriceRow> rows = ThePublishedPricesAsOf(asOf);

			string[] said = new string[rows.Count];

			for (int row = 0; row < rows.Count; row++) said[row] = rows[row].TheRevisionItCameFrom();

			return said;
		}

		private List<APublishedPriceKey> ThePriceKeysThatGovern()
		{
			List<APublishedPriceKey> keys = new List<APublishedPriceKey>();

			for (int revision = _order.Count - 1; revision >= 0; revision--)
			{
				PublishedRevision candidate = _order[revision];

				if (!OnlyASealedRevisionAnswers(candidate)) continue;

				foreach (APublishedPriceKey arriving in candidate.ThePriceKeysItPublished())
					if (!ItIsAlreadyOnTheList(keys, arriving)) keys.Add(arriving);
			}

			return keys;
		}

		private static bool ItIsAlreadyOnTheList(
			List<APublishedPriceKey> keys, APublishedPriceKey arriving)
		{
			for (int key = 0; key < keys.Count; key++)
				if (keys[key].IsTheSameKeyAs(arriving)) return true;

			return false;
		}

		private List<APublishedPriceRow> ThePublishedPricesAsOf(DateTime asOf)
		{
			List<APublishedPriceKey> keys = ThePriceKeysThatGovern();
			List<APublishedPriceRow> rows = new List<APublishedPriceRow>();

			for (int key = 0; key < keys.Count; key++)
				rows.Add(APublishedPriceRow.Of(
					keys[key],
					ARevisionAppliesForward(
						keys[key].TheOperationItNames(), keys[key].TheMilestoneItNames(), asOf)));

			return rows;
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
		internal AServicingClause TheServicingElsewhereAnswer(
			int externalServicesWithEvidence, int externalServicesWithoutEvidence,
			int missedMaintenanceCount)
		{
			return ARevisionAppliesForwardToTheServicingPolicy(
				externalServicesWithEvidence, externalServicesWithoutEvidence,
				missedMaintenanceCount);
		}

		/// <summary>
		/// La regla publicada sobre servicio en otro taller, con su mitad publicada nada mas. No
		/// pregunta por ningun vehiculo, asi que la contesta tambien quien no lleve los conteos de
		/// ninguno. TOTAL.
		/// </summary>
		internal AServicingClause ThePublishedServicingElsewhereAnswer()
		{
			return ARevisionAppliesForwardToTheServicingPolicy();
		}

		/// <summary>De que revision salio la regla de servicio en otro taller. TOTAL.</summary>
		internal string ReadServicingPolicyRevision()
		{
			return ARevisionAppliesForwardToTheServicingPolicy().TheRevisionItCameFrom();
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

		/// <summary>
		/// Elige la ULTIMA revision sellada que publico alcance de garantia y entrega el alcance
		/// entero como forma y valores, con su procedencia. El alcance es UN item: una revision que lo
		/// republica lo reemplaza entero, y una que solo corrige otra cosa no lo toca. TOTAL: sin
		/// ninguna que lo publique sale la ausencia, que es un objeto y no un alcance vacio.
		/// </summary>
		internal ACoverageScope TheCoverageScope()
		{
			if (!ARevisionAnswersAboutCoverage()) return ACoverageScope.NothingWasPublished();

			return TheRevisionThatAnswersAboutCoverage().TheWarrantyScopePublished();
		}

		/// <summary>
		/// Si hay alcance de garantia publicado. TOTAL: un modelo del que nadie publico garantia
		/// contesta que no, y eso es una respuesta legal.
		/// </summary>
		internal bool IsCoveragePublished()
		{
			return ARevisionAnswersAboutCoverage();
		}

		/// <summary>
		/// De que revision salio el alcance publicado. TOTAL: lo no publicado contesta vacio, que no
		/// se confunde con una revision porque ninguna tiene el codigo en blanco.
		/// </summary>
		internal string ReadCoverageRevision()
		{
			return TheCoverageScope().TheRevisionItCameFrom();
		}

		/// <summary>
		/// Los codigos de los sistemas que el alcance declara adentro. Salen como CODIGOS porque
		/// son de lo que la persona elige al preguntar. TOTAL: sin alcance publicado, vacio.
		/// </summary>
		internal string[] CoveredSystems()
		{
			if (!ARevisionAnswersAboutCoverage()) return new string[0];

			return TheRevisionThatAnswersAboutCoverage().TheCoveredSystemsPublished();
		}

		/// <summary>
		/// Si el catalogo publicado conoce ese codigo de sistema, sea porque lo declara adentro o
		/// porque una exclusion lo nombra. Consulta TOTAL de reconocimiento: el vocabulario es del
		/// fabricante, asi que un codigo desconocido es una respuesta y no un defecto de quien
		/// pregunta.
		/// </summary>
		internal bool TheVocabularyIsTheManufacturers(string systemCode)
		{
			if (!ARevisionAnswersAboutCoverage()) return false;

			return TheRevisionThatAnswersAboutCoverage()
				.TheVocabularyIsTheManufacturers(systemCode);
		}

		/// <summary>
		/// Si la pregunta de cobertura se puede decidir con lo que hay. TOTAL, y es la que hay que
		/// hacer antes de leer el resultado: da que no cuando el limite que rige y el odometro
		/// estan en unidades distintas, porque aca no se convierte nada.
		/// </summary>
		internal bool CanCoverageBeEvaluated(
			string systemCode, int envelopeMonths, int envelopeDistance,
			DistanceUnitCode envelopeUnit, DateTime deliveredAt, int deliveryReading,
			int currentReading, DistanceUnitCode currentUnit, DateTime asOf)
		{
			return TheCoverageAnswerFor(
				systemCode, envelopeMonths, envelopeDistance, envelopeUnit,
				deliveredAt, deliveryReading, currentReading, currentUnit, asOf).CanBeEvaluated();
		}

		/// <summary>
		/// Donde esta parado ese sistema, por el nombre de la respuesta. Accesor GUARDADO, y es el
		/// unico del alcance de lectura de este modulo: si no se pudo evaluar no hay respuesta que
		/// dar, y cualquiera de las posibles devuelta aca se leeria como un hecho medido.
		/// </summary>
		internal string ReadCoverageStanding(
			string systemCode, int envelopeMonths, int envelopeDistance,
			DistanceUnitCode envelopeUnit, DateTime deliveredAt, int deliveryReading,
			int currentReading, DistanceUnitCode currentUnit, DateTime asOf)
		{
			return TheCoverageAnswerFor(
					systemCode, envelopeMonths, envelopeDistance, envelopeUnit,
					deliveredAt, deliveryReading, currentReading, currentUnit, asOf)
				.Standing()
				.Name;
		}

		/// <summary>
		/// La respuesta de cobertura como forma y valores: donde esta parado o por que no se pudo
		/// decidir, contra que limites se midio, las exclusiones publicadas que haya, y la revision.
		/// TOTAL: lo que no se pudo evaluar tambien contesta, diciendo por que.
		/// <para>
		/// Es el MISMO resolvedor que contestan la presencia y el nombre suelto, asi que las tres
		/// lecturas no pueden discrepar: no hay dos sitios donde decidirlo.
		/// </para>
		/// </summary>
		internal CoverageAnswer TheCoverageAnswer(
			string systemCode, int envelopeMonths, int envelopeDistance,
			DistanceUnitCode envelopeUnit, DateTime deliveredAt, int deliveryReading,
			int currentReading, DistanceUnitCode currentUnit, DateTime asOf)
		{
			return TheCoverageAnswerFor(
				systemCode, envelopeMonths, envelopeDistance, envelopeUnit,
				deliveredAt, deliveryReading, currentReading, currentUnit, asOf);
		}

		/// <summary>
		/// Si alguna revision sellada puede contestar. TOTAL, y es la pregunta que hay que hacer
		/// antes de pedir el codigo o el instante de la vigente: una especificacion recien creada, o
		/// una en la que todo sigue en borrador, contesta que no.
		/// </summary>
		internal bool HasASealedRevision()
		{
			for (int revision = _order.Count - 1; revision >= 0; revision--)
				if (OnlyASealedRevisionAnswers(_order[revision])) return true;

			return false;
		}

		/// <summary>
		/// El codigo de la revision sellada mas reciente, que es la que contesta. Accesor GUARDADO:
		/// sin ninguna sellada no hay codigo que dar, y un vacio aca se leeria como una revision sin
		/// nombre en vez de como la ausencia de revision.
		/// </summary>
		internal string ReadCurrentRevisionCode()
		{
			return TheCurrentSealedRevision().Code;
		}

		/// <summary>
		/// El instante en que el fabricante publico la revision vigente, tal como llego. Accesor
		/// GUARDADO por lo mismo: un instante minimo se parece a un instante y no lo es.
		/// </summary>
		internal DateTime ReadCurrentRevisionPublishedAt()
		{
			return TheCurrentSealedRevision().PublishedAt;
		}

		private PublishedRevision TheCurrentSealedRevision()
		{
			for (int revision = _order.Count - 1; revision >= 0; revision--)
				if (OnlyASealedRevisionAnswers(_order[revision])) return _order[revision];

			throw new DomainException(
				"Esta especificacion no tiene ninguna revision sellada. Hay que preguntar primero si "
				+ "hay alguna: una respuesta en blanco aca nombraria una publicacion que nadie hizo.");
		}

		private bool ARevisionAnswersAboutFuel()
		{
			for (int revision = _order.Count - 1; revision >= 0; revision--)
				if (ItAnswersAboutFuel(_order[revision])) return true;

			return false;
		}

		private PublishedRevision TheRevisionThatAnswersAboutFuel()
		{
			for (int revision = _order.Count - 1; revision >= 0; revision--)
				if (ItAnswersAboutFuel(_order[revision])) return _order[revision];

			throw new DomainException(
				"Ninguna revision sellada de esta especificacion publico grados de combustible: hay "
				+ "que preguntar primero si hay alguna. Una lista vacia fabricada aca se leeria como "
				+ "que el fabricante dijo que no hay ningun combustible prohibido.");
		}

		private bool ItAnswersAboutFuel(PublishedRevision candidate)
		{
			return OnlyASealedRevisionAnswers(candidate) && candidate.HasAFuelSpecification();
		}

		private static void RefuseReceivedFactsThatCannotBeTrue(
			DateTime deliveredAt, int deliveryReading, int currentReading, DateTime asOf)
		{
			if (deliveryReading < 0 || currentReading < 0)
				throw new DomainException(
					$"Llegaron lecturas de odometro de {deliveryReading} y {currentReading}. Un "
					+ "odometro no cuenta para atras de cero, asi que no es una dimension que no se "
					+ "pueda medir: es un hecho imposible.");

			if (currentReading < deliveryReading)
				throw new DomainException(
					$"La lectura actual es {currentReading} y la de la entrega es {deliveryReading}, "
					+ "en la misma unidad: el odometro fue para atras. Cual de las dos esta mal no lo "
					+ "decide esta especificacion.");

			if (asOf < deliveredAt)
				throw new DomainException(
					"Se pregunto por la cobertura en un instante anterior a la entrega del vehiculo. "
					+ "Antes de la entrega no hay plazo que contar, y contestar igual seria medir un "
					+ "tiempo que no transcurrio.");
		}

		private CoverageAnswer TheCoverageAnswerFor(
			string systemCode, int envelopeMonths, int envelopeDistance,
			DistanceUnitCode envelopeUnit, DateTime deliveredAt, int deliveryReading,
			int currentReading, DistanceUnitCode currentUnit, DateTime asOf)
		{
			RefuseReceivedFactsThatCannotBeTrue(
				deliveredAt, deliveryReading, currentReading, asOf);

			if (!ARevisionAnswersAboutCoverage())
				return new ASettledCoverageAnswer(
					systemCode, CoverageStanding.NotPublished,
					ACoverageClause.OfNothingPublished());

			return TheStandingAccordingTo(
				TheRevisionThatAnswersAboutCoverage(), systemCode, envelopeMonths,
				envelopeDistance, envelopeUnit, deliveredAt, deliveryReading, currentReading,
				currentUnit, asOf);
		}

		private static CoverageAnswer TheStandingAccordingTo(
			PublishedRevision answering, string systemCode, int envelopeMonths,
			int envelopeDistance, DistanceUnitCode envelopeUnit, DateTime deliveredAt,
			int deliveryReading, int currentReading, DistanceUnitCode currentUnit, DateTime asOf)
		{
			return answering.TheCoverageStandingFor(
				systemCode,
				SystemLimits.OfTheVehicleEnvelope(
					envelopeMonths, envelopeDistance, DistanceUnit.OfCode(envelopeUnit)),
				deliveredAt, deliveryReading, currentReading,
				DistanceUnit.OfCode(currentUnit), asOf);
		}

		private bool ARevisionAnswersAboutCoverage()
		{
			for (int revision = _order.Count - 1; revision >= 0; revision--)
				if (ItAnswersAboutCoverage(_order[revision])) return true;

			return false;
		}

		private PublishedRevision TheRevisionThatAnswersAboutCoverage()
		{
			for (int revision = _order.Count - 1; revision >= 0; revision--)
				if (ItAnswersAboutCoverage(_order[revision])) return _order[revision];

			throw new DomainException(
				"Ninguna revision sellada de esta especificacion publico alcance de garantia: hay "
				+ "que preguntar primero si hay alguna. Fabricar un alcance vacio aca se leeria como "
				+ "que el fabricante dijo que no cubre nada.");
		}

		private bool ItAnswersAboutCoverage(PublishedRevision candidate)
		{
			return OnlyASealedRevisionAnswers(candidate) && candidate.HasWarrantyCoverage();
		}

		private static string TheSubjectOfAServicePrice(OperationKindCode kind)
		{
			return TheSubjectOfAServicePrice(MaintenanceOperationKind.OfCode(kind));
		}

		private static string TheSubjectOfAServicePrice(MaintenanceOperationKind operation)
		{
			return "Precio del servicio de " + operation.HowItIsSaid.ToLowerInvariant()
				+ " en el hito preguntado";
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
