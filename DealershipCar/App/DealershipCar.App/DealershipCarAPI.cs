using System;
using System.Globalization;
using Choreography.Theater;
using DealershipCar.App.Events;
using DealershipCar.Dominio;
using Puppeteer;
using Puppets.Portador;

namespace DealershipCar.App
{
	public sealed class DealershipCarAPI : IDiarioDeLaApp
	{
		private readonly PerformanceV2 app;

		public DealershipCarAPI(PerformanceV2 app)
		{
			ArgumentNullException.ThrowIfNull(app);

			this.app = app;
		}

		public void SeedPlanOperation(
			string kind, string basis, int timeLimitDays, int distanceLimit, string unit,
			bool isMandatory, bool isSafety,
			DateTime anchoredAt, int anchorReading, string anchorUnit)
		{
			TheDeclaredBasisAgreesWithItsLimits(basis, timeLimitDays, distanceLimit);

			app.Using(@"
				schedule.SeedPlanOperation(
					@kind, @basis, @timeLimitDays, @distanceLimit, @unit,
					@isMandatory, @isSafety,
					@anchoredAt, @anchorReading, @anchorUnit);
			")
			.WithParameters(p => {
				p["kind",          typeof(Enum)]     = WireName.OperationKind(kind);
				p["basis",         typeof(Enum)]     = WireName.IntervalBasis(basis);
				p["timeLimitDays", typeof(int)]      = timeLimitDays;
				p["distanceLimit", typeof(int)]      = distanceLimit;
				p["unit",          typeof(Enum)]     = WireName.DistanceUnit(unit, nameof(unit));
				p["isMandatory",   typeof(bool)]     = isMandatory;
				p["isSafety",      typeof(bool)]     = isSafety;
				p["anchoredAt",    typeof(DateTime)] = InUtc(anchoredAt);
				p["anchorReading", typeof(int)]      = anchorReading;
				p["anchorUnit",    typeof(Enum)]     =
					WireName.DistanceUnit(anchorUnit, nameof(anchorUnit));
			})
			.PerformCommand();
		}

		public void AdoptPlanRevision(string revisionCode, DateTime adoptedAt)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(revisionCode);

			app.Using("schedule.AdoptPlanRevision(@revisionCode, @adoptedAt);")
			.WithParameters(p => {
				p["revisionCode", typeof(string)]   = revisionCode;
				p["adoptedAt",    typeof(DateTime)] = InUtc(adoptedAt);
			})
			.PerformCommand();
		}

		public void AddOwnerOperation(
			string kind, string basis, int timeLimitDays, int distanceLimit, string unit,
			DateTime anchoredAt, int anchorReading, string anchorUnit)
		{
			TheDeclaredBasisAgreesWithItsLimits(basis, timeLimitDays, distanceLimit);

			app.Using(@"
				schedule.AddOwnerOperation(
					@kind, @basis, @timeLimitDays, @distanceLimit, @unit,
					@anchoredAt, @anchorReading, @anchorUnit);
			")
			.WithParameters(p => {
				p["kind",          typeof(Enum)]     = WireName.OperationKind(kind);
				p["basis",         typeof(Enum)]     = WireName.IntervalBasis(basis);
				p["timeLimitDays", typeof(int)]      = timeLimitDays;
				p["distanceLimit", typeof(int)]      = distanceLimit;
				p["unit",          typeof(Enum)]     = WireName.DistanceUnit(unit, nameof(unit));
				p["anchoredAt",    typeof(DateTime)] = InUtc(anchoredAt);
				p["anchorReading", typeof(int)]      = anchorReading;
				p["anchorUnit",    typeof(Enum)]     =
					WireName.DistanceUnit(anchorUnit, nameof(anchorUnit));
			})
			.PerformCommand();
		}

		public void RemoveOwnerOperation(string kind)
		{
			app.Using("schedule.RemoveOwnerOperation(@kind);")
			.WithParameters(p => {
				p["kind", typeof(Enum)] = WireName.OperationKind(kind);
			})
			.PerformCommand();
		}

		public void DeclareUsageProfile(string profile, DateTime effectiveFrom)
		{
			app.Using("schedule.DeclareUsageProfile(@profile, @effectiveFrom);")
			.WithParameters(p => {
				p["profile",       typeof(Enum)]     = WireName.UsageProfile(profile, nameof(profile));
				p["effectiveFrom", typeof(DateTime)] = InUtc(effectiveFrom);
			})
			.PerformCommand();
		}

		public void DeferOperation(string kind, string reason, DateTime decidedAt)
		{
			app.Using("schedule.DeferOperation(@kind, @reason, @decidedAt);")
			.WithParameters(p => {
				p["kind",      typeof(Enum)]     = WireName.OperationKind(kind);
				p["reason",    typeof(Enum)]     = WireName.DeferralReason(reason);
				p["decidedAt", typeof(DateTime)] = InUtc(decidedAt);
			})
			.PerformCommand();
		}

		public void RecordReading(int value, string unit, DateTime observedAt, string source)
		{
			app.Using("schedule.RecordReading(@value, @unit, @observedAt, @source);")
			.WithParameters(p => {
				p["value",      typeof(int)]      = value;
				p["unit",       typeof(Enum)]     = WireName.DistanceUnit(unit, nameof(unit));
				p["observedAt", typeof(DateTime)] = InUtc(observedAt);
				p["source",     typeof(Enum)]     = WireName.ReadingSource(source);
			})
			.PerformCommand();
		}

		public void RecordConditionThreshold(string kind, DateTime observedAt)
		{
			app.Using("schedule.RecordConditionThreshold(@kind, @observedAt);")
			.WithParameters(p => {
				p["kind",       typeof(Enum)]     = WireName.OperationKind(kind);
				p["observedAt", typeof(DateTime)] = InUtc(observedAt);
			})
			.PerformCommand();
		}

		public void CompleteOperation(
			string kind, DateTime completedAt, int completionReading, string unit)
		{
			app.Using("schedule.CompleteOperation(@kind, @completedAt, @completionReading, @unit);")
			.WithParameters(p => {
				p["kind",              typeof(Enum)]     = WireName.OperationKind(kind);
				p["completedAt",       typeof(DateTime)] = InUtc(completedAt);
				p["completionReading", typeof(int)]      = completionReading;
				p["unit",              typeof(Enum)]     = WireName.DistanceUnit(unit, nameof(unit));
			})
			.PerformCommand();
		}

		public void RegisterDevice(string deviceKey)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(deviceKey);

			app.Using("logbook.RegisterDevice(@device);")
			.WithParameters(p => {
				p["device", typeof(string)] = deviceKey;
			})
			.PerformCommand();
		}

		public void StoreCredential(string token, string refreshToken)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(token);

			app.Using("logbook.StoreCredential(@token, @refresh);")
			.WithParameters(p => {
				p["token",   typeof(string)] = token;
				p["refresh", typeof(string)] = refreshToken ?? "";
			})
			.PerformCommand();
		}

		/// <summary>
		/// ABRE una revision de la especificacion del modelo, con el codigo y el instante en que el
		/// fabricante la publico. Nace borrador, invisible a toda lectura, y el contenido entra
		/// despues por los comandos de publicacion.
		/// </summary>
		public void OpenRevision(string revisionCode, DateTime publishedAt)
		{
			app.Using("specification.OpenRevision(@revisionCode, @publishedAt);")
			.WithParameters(p => {
				p["revisionCode", typeof(string)]   = TheText(revisionCode, nameof(revisionCode));
				p["publishedAt",  typeof(DateTime)] = InUtc(publishedAt);
			})
			.PerformCommand();
		}

		/// <summary>
		/// Publica dentro de una revision que fluido de una clase lleva el modelo: el grado, la
		/// capacidad con su unidad, y cada cuanto se cambia.
		/// </summary>
		public void PublishFluidSpecification(
			string revisionCode, string kind, string grade,
			decimal capacity, string capacityUnit,
			int intervalDistance, string intervalUnit, int intervalDays)
		{
			app.Using(@"
				specification.PublishFluidSpecification(
					@revisionCode, @kind, @grade,
					@capacity, @capacityUnit,
					@intervalDistance, @intervalUnit, @intervalDays);
			")
			.WithParameters(p => {
				p["revisionCode",     typeof(string)]  = TheText(revisionCode, nameof(revisionCode));
				p["kind",             typeof(Enum)]    = WireName.FluidKind(kind);
				p["grade",            typeof(string)]  = TheText(grade, nameof(grade));
				p["capacity",         typeof(decimal)] = Normalized(capacity);
				p["capacityUnit",     typeof(Enum)]    =
					WireName.VolumeUnit(capacityUnit, nameof(capacityUnit));
				p["intervalDistance", typeof(int)]     = intervalDistance;
				p["intervalUnit",     typeof(Enum)]    =
					WireName.DistanceUnit(intervalUnit, nameof(intervalUnit));
				p["intervalDays",     typeof(int)]     = intervalDays;
			})
			.PerformCommand();
		}

		/// <summary>
		/// Publica dentro de una revision que llanta lleva el modelo en una posicion. La presion
		/// viaja con la unidad en que se publico y se contesta en esa.
		/// </summary>
		public void PublishTyreSpecification(
			string revisionCode, string position, string size,
			decimal coldPressure, string pressureUnit, string ratings)
		{
			app.Using(@"
				specification.PublishTyreSpecification(
					@revisionCode, @position, @size,
					@coldPressure, @pressureUnit, @ratings);
			")
			.WithParameters(p => {
				p["revisionCode", typeof(string)]  = TheText(revisionCode, nameof(revisionCode));
				p["position",     typeof(Enum)]    = WireName.TyrePosition(position);
				p["size",         typeof(string)]  = TheText(size, nameof(size));
				p["coldPressure", typeof(decimal)] = Normalized(coldPressure);
				p["pressureUnit", typeof(Enum)]    =
					WireName.PressureUnit(pressureUnit, nameof(pressureUnit));
				p["ratings",      typeof(string)]  = TheText(ratings, nameof(ratings));
			})
			.PerformCommand();
		}

		/// <summary>
		/// Publica dentro de una revision un grado de combustible con su postura. La especificacion
		/// de combustible se arma grado a grado y contesta entera.
		/// </summary>
		public void PublishFuelGrade(string revisionCode, string grade, string standing)
		{
			app.Using("specification.PublishFuelGrade(@revisionCode, @grade, @standing);")
			.WithParameters(p => {
				p["revisionCode", typeof(string)] = TheText(revisionCode, nameof(revisionCode));
				p["grade",        typeof(string)] = TheText(grade, nameof(grade));
				p["standing",     typeof(Enum)]   = WireName.FuelGradeStanding(standing);
			})
			.PerformCommand();
		}

		/// <summary>
		/// Publica dentro de una revision un sistema que el fabricante declara dentro del alcance de
		/// la garantia. UN CERO EN LAS DOS MITADES DEL LIMITE PROPIO es como el wire dice que el
		/// sistema no publico limites propios, y esa traduccion es del dominio: aca no se toca. La
		/// unidad viaja igual, porque una distancia sin unidad no se puede leer.
		/// </summary>
		public void PublishCoveredSystem(
			string revisionCode, string systemCode,
			int ownLimitMonths, int ownLimitDistance, string ownLimitUnit)
		{
			app.Using(@"
				specification.PublishCoveredSystem(
					@revisionCode, @systemCode,
					@ownLimitMonths, @ownLimitDistance, @ownLimitUnit);
			")
			.WithParameters(p => {
				p["revisionCode",     typeof(string)] = TheText(revisionCode, nameof(revisionCode));
				p["systemCode",       typeof(string)] = TheText(systemCode, nameof(systemCode));
				p["ownLimitMonths",   typeof(int)]    = ownLimitMonths;
				p["ownLimitDistance", typeof(int)]    = ownLimitDistance;
				p["ownLimitUnit",     typeof(Enum)]   =
					WireName.DistanceUnit(ownLimitUnit, nameof(ownLimitUnit));
			})
			.PerformCommand();
		}

		/// <summary>
		/// Publica dentro de una revision algo que el alcance no cubre, con la condicion bajo la
		/// cual muerde. La condicion es texto publicado: nadie decide aca si se cumple.
		/// </summary>
		public void PublishCoverageExclusion(
			string revisionCode, string systemCode, string exclusion, string condition)
		{
			app.Using(@"
				specification.PublishCoverageExclusion(
					@revisionCode, @systemCode, @exclusion, @condition);
			")
			.WithParameters(p => {
				p["revisionCode", typeof(string)] = TheText(revisionCode, nameof(revisionCode));
				p["systemCode",   typeof(string)] = TheText(systemCode, nameof(systemCode));
				p["exclusion",    typeof(string)] = TheText(exclusion, nameof(exclusion));
				p["condition",    typeof(string)] = TheText(condition, nameof(condition));
			})
			.PerformCommand();
		}

		/// <summary>
		/// Publica dentro de una revision cada cuanto dice el fabricante que se hace una operacion.
		/// Es el numero publicado y nombra su revision; la copia por vehiculo que la agenda usa para
		/// medir sigue siendo la base de la medicion.
		/// </summary>
		public void PublishServiceInterval(
			string revisionCode, string kind,
			int intervalDistance, string intervalUnit, int intervalDays)
		{
			app.Using(@"
				specification.PublishServiceInterval(
					@revisionCode, @kind,
					@intervalDistance, @intervalUnit, @intervalDays);
			")
			.WithParameters(p => {
				p["revisionCode",     typeof(string)] = TheText(revisionCode, nameof(revisionCode));
				p["kind",             typeof(Enum)]   = WireName.OperationKind(kind);
				p["intervalDistance", typeof(int)]    = intervalDistance;
				p["intervalUnit",     typeof(Enum)]   =
					WireName.DistanceUnit(intervalUnit, nameof(intervalUnit));
				p["intervalDays",     typeof(int)]    = intervalDays;
			})
			.PerformCommand();
		}

		/// <summary>
		/// Publica dentro de una revision el precio de una operacion en un hito, con su moneda y su
		/// vigencia. El monto entra tal como se publico y no se convierte a ninguna otra moneda.
		/// </summary>
		public void PublishServicePrice(
			string revisionCode, string kind,
			int milestoneDistance, string milestoneUnit,
			decimal amount, string currency, DateTime validFrom, DateTime validUntil)
		{
			app.Using(@"
				specification.PublishServicePrice(
					@revisionCode, @kind,
					@milestoneDistance, @milestoneUnit,
					@amount, @currency, @validFrom, @validUntil);
			")
			.WithParameters(p => {
				p["revisionCode",      typeof(string)]   = TheText(revisionCode, nameof(revisionCode));
				p["kind",              typeof(Enum)]     = WireName.OperationKind(kind);
				p["milestoneDistance", typeof(int)]      = milestoneDistance;
				p["milestoneUnit",     typeof(Enum)]     =
					WireName.DistanceUnit(milestoneUnit, nameof(milestoneUnit));
				p["amount",            typeof(decimal)]  = Normalized(amount);
				p["currency",          typeof(string)]   = TheText(currency, nameof(currency));
				p["validFrom",         typeof(DateTime)] = InUtc(validFrom);
				p["validUntil",        typeof(DateTime)] = InUtc(validUntil);
			})
			.PerformCommand();
		}

		/// <summary>
		/// Publica dentro de una revision la regla del fabricante sobre servicio en otro taller y
		/// que conserva la cobertura. Los dos textos se guardan como se publicaron.
		/// </summary>
		public void PublishServicingPolicy(
			string revisionCode, string rule, string whatPreservesCover)
		{
			app.Using(@"
				specification.PublishServicingPolicy(
					@revisionCode, @rule, @whatPreservesCover);
			")
			.WithParameters(p => {
				p["revisionCode",       typeof(string)] = TheText(revisionCode, nameof(revisionCode));
				p["rule",               typeof(string)] = TheText(rule, nameof(rule));
				p["whatPreservesCover", typeof(string)] =
					TheText(whatPreservesCover, nameof(whatPreservesCover));
			})
			.PerformCommand();
		}

		/// <summary>
		/// SELLA una revision, que es lo unico que la hace contestar. Una revision sin contenido no
		/// se sella: sellada quiere decir citable.
		/// </summary>
		public void SealRevision(string revisionCode)
		{
			app.Using("specification.SealRevision(@revisionCode);")
			.WithParameters(p => {
				p["revisionCode", typeof(string)] = TheText(revisionCode, nameof(revisionCode));
			})
			.PerformCommand();
		}

		/// <summary>
		/// SUSTITUYE una revision por otra, que queda nombrada como la que la reemplazo. Lo que la
		/// sucesora no volvio a publicar se retira con ella.
		/// </summary>
		public void SupersedeRevision(string revisionCode, string supersededByCode)
		{
			app.Using("specification.SupersedeRevision(@revisionCode, @supersededByCode);")
			.WithParameters(p => {
				p["revisionCode",     typeof(string)] = TheText(revisionCode, nameof(revisionCode));
				p["supersededByCode", typeof(string)] =
					TheText(supersededByCode, nameof(supersededByCode));
			})
			.PerformCommand();
		}

		public EvaluationOutcome Evaluate(DateTime asOf)
		{
			using (RentedParameter rented = app.Actor.RentedParameters())
			{
				string rejection = app.Using(
					scriptForChk: @"
						Check(schedule.HasBeenEvaluated() == false
							|| @asOf > schedule.LastEvaluatedAt())
							Error 'esta agenda ya se evaluo hasta ese instante';
					",
					@"
						{
							evaluation = schedule.Evaluate(@asOf);
							@transitions = evaluation.TransitionCount;
							@becameDue = evaluation.AnythingBecameDue;
							@becameOverdue = evaluation.AnythingBecameOverdue;
						}
					")
				.WithParameters(rented, p => {
					p["asOf", typeof(DateTime)] = InUtc(asOf);
					p[Parameter.Out, "transitions",   typeof(int)]  = default;
					p[Parameter.Out, "becameDue",     typeof(bool)] = default;
					p[Parameter.Out, "becameOverdue", typeof(bool)] = default;
				})
				.PerformCheckThenCommand();

				if (rejection.Length > 0) return EvaluationOutcome.Rejected(rejection);

				return EvaluationOutcome.Of(
					rented["transitions"].GetValue<int>(),
					rented["becameDue"].GetValue<bool>(),
					rented["becameOverdue"].GetValue<bool>());
			}
		}

		public string ReadVin()
		{
			return AText("@vin = schedule.ReadVin();", "vin", p => { });
		}

		public string ReadDueState(string kind)
		{
			return AText("@state = schedule.ReadDueState(@kind);", "state", p => {
				p["kind", typeof(Enum)] = WireName.OperationKind(kind);
			});
		}

		public int CountDue()
		{
			return ACount("@due = schedule.CountDue();", "due", p => { });
		}

		public int CountOverdue()
		{
			return ACount("@overdue = schedule.CountOverdue();", "overdue", p => { });
		}

		public int DeferralsUsed(string kind)
		{
			return ACount("@used = schedule.DeferralsUsed(@kind);", "used", p => {
				p["kind", typeof(Enum)] = WireName.OperationKind(kind);
			});
		}

		public bool CanBeDeferred(string kind)
		{
			return ATruth("@can = schedule.CanBeDeferred(@kind);", "can", p => {
				p["kind", typeof(Enum)] = WireName.OperationKind(kind);
			});
		}

		public bool HasAnythingDue()
		{
			return ATruth("@any = schedule.HasAnythingDue();", "any", p => { });
		}

		public string ReadNextDueKind()
		{
			if (!HasAnythingDue())
				throw new InvalidOperationException(
					"No hay nada vencido en esta agenda: pregunte HasAnythingDue antes. Devolver "
					+ "una clase vacia haria indistinguible 'no hay nada que hacer' de 'lo "
					+ "siguiente es una operacion sin nombre'.");

			return AText("@kind = schedule.ReadNextDueKind();", "kind", p => { });
		}

		public string ReadDueReason(string kind)
		{
			return AText("@reason = schedule.ReadDueReason(@kind);", "reason", p => {
				p["kind", typeof(Enum)] = WireName.OperationKind(kind);
			});
		}

		public int MissedCount()
		{
			return ACount("@missed = schedule.MissedCount();", "missed", p => { });
		}

		public bool HasBeenEvaluated()
		{
			return ATruth("@evaluated = schedule.HasBeenEvaluated();", "evaluated", p => { });
		}

		public DateTime LastEvaluatedAt()
		{
			if (!HasBeenEvaluated())
				throw new InvalidOperationException(
					"Esta agenda no se ha evaluado nunca: pregunte HasBeenEvaluated antes. Un "
					+ "DateTime.MinValue aca seria un centinela magico -- un instante que parece un "
					+ "instante y no lo es.");

			return AnInstant("@at = schedule.LastEvaluatedAt();", "at", p => { });
		}

		public long LastEventApplied()
		{
			return ANumber("@last = logbook.LastEvent();", "last", p => { });
		}

		public string StoredToken()
		{
			return ATruth("@has = logbook.HasACredential();", "has", p => { })
				? AText("@value = logbook.Token();", "value", p => { })
				: "";
		}

		public string StoredRefreshToken()
		{
			return ATruth("@can = logbook.CanRotate();", "can", p => { })
				? AText("@value = logbook.RefreshToken();", "value", p => { })
				: "";
		}

		public string StoredDeviceKey()
		{
			return ATruth("@has = logbook.HasADevice();", "has", p => { })
				? AText("@value = logbook.DeviceKey();", "value", p => { })
				: "";
		}

		/// <summary>De que modelo es la especificacion que este journal lleva.</summary>
		public string ReadModel()
		{
			return AText("@model = specification.ReadModel();", "model", p => { });
		}

		/// <summary>
		/// Si hay algo publicado sobre esa clase de fluido. TOTAL: una clase que ninguna revision
		/// sellada publico contesta que no, y eso es una respuesta.
		/// </summary>
		public bool IsFluidPublished(string kind)
		{
			return ATruth("@published = specification.IsFluidPublished(@kind);", "published", p => {
				p["kind", typeof(Enum)] = WireName.FluidKind(kind);
			});
		}

		/// <summary>
		/// Que fluido de esa clase lleva el modelo, en una frase con la revision adentro. La compone
		/// el dominio: decir el porque es conocimiento del dominio y no de quien muestra.
		/// </summary>
		public string DescribeFluid(string kind)
		{
			return AText("@clause = specification.DescribeFluid(@kind);", "clause", p => {
				p["kind", typeof(Enum)] = WireName.FluidKind(kind);
			});
		}

		/// <summary>
		/// De que revision salio la respuesta de esa clase de fluido. Lo no publicado contesta
		/// vacio, que no se confunde con una revision porque ninguna tiene el codigo en blanco.
		/// </summary>
		public string ReadFluidRevision(string kind)
		{
			return AText("@revision = specification.ReadFluidRevision(@kind);", "revision", p => {
				p["kind", typeof(Enum)] = WireName.FluidKind(kind);
			});
		}

		/// <summary>Si hay algo publicado sobre la llanta de esa posicion. TOTAL.</summary>
		public bool IsTyrePublished(string position)
		{
			return ATruth("@published = specification.IsTyrePublished(@position);", "published", p => {
				p["position", typeof(Enum)] = WireName.TyrePosition(position);
			});
		}

		/// <summary>
		/// Que llanta lleva el modelo en esa posicion, con la presion en la unidad en que se
		/// publico. Aca no se convierte ninguna unidad a ninguna otra.
		/// </summary>
		public string DescribeTyre(string position)
		{
			return AText("@clause = specification.DescribeTyre(@position);", "clause", p => {
				p["position", typeof(Enum)] = WireName.TyrePosition(position);
			});
		}

		/// <summary>De que revision salio la respuesta de esa posicion. TOTAL.</summary>
		public string ReadTyreRevision(string position)
		{
			return AText("@revision = specification.ReadTyreRevision(@position);", "revision", p => {
				p["position", typeof(Enum)] = WireName.TyrePosition(position);
			});
		}

		/// <summary>Si hay algo publicado sobre el combustible. TOTAL.</summary>
		public bool IsFuelPublished()
		{
			return ATruth("@published = specification.IsFuelPublished();", "published", p => { });
		}

		/// <summary>
		/// Que combustible acepta el modelo: los grados publicados con su postura, con la revision
		/// adentro. TOTAL.
		/// </summary>
		public string DescribeFuel()
		{
			return AText("@clause = specification.DescribeFuel();", "clause", p => { });
		}

		/// <summary>
		/// El grado minimo publicado, suelto, que es como se pide en una bomba. TOTAL: contesta
		/// vacio sin combustible publicado y tambien cuando lo publicado no marco ningun minimo. Un
		/// grado publicado nunca esta en blanco, asi que el vacio no se confunde con uno.
		/// </summary>
		public string ReadMinimumFuelGrade()
		{
			return AText("@grade = specification.ReadMinimumFuelGrade();", "grade", p => { });
		}

		/// <summary>
		/// Los grados publicados como permitidos. TOTAL: una lista vacia dice que no hay ninguno
		/// permitido, que no es lo mismo que decir que cualquiera lo esta.
		/// </summary>
		public string[] PermittedFuelGrades()
		{
			return ATextList("@grades = specification.PermittedFuelGrades();", "grades", p => { });
		}

		/// <summary>
		/// Los grados publicados como prohibidos. TOTAL, y es la lista que mas importa que salga
		/// entera: un prohibido callado es un motor danado.
		/// </summary>
		public string[] ProhibitedFuelGrades()
		{
			return ATextList("@grades = specification.ProhibitedFuelGrades();", "grades", p => { });
		}

		/// <summary>De que revision salio la respuesta del combustible. TOTAL.</summary>
		public string ReadFuelRevision()
		{
			return AText("@revision = specification.ReadFuelRevision();", "revision", p => { });
		}

		/// <summary>Si hay publicado un intervalo para esa operacion. TOTAL.</summary>
		public bool IsServiceIntervalPublished(string kind)
		{
			return ATruth(
				"@published = specification.IsServiceIntervalPublished(@kind);", "published", p => {
					p["kind", typeof(Enum)] = WireName.OperationKind(kind);
				});
		}

		/// <summary>
		/// Cada cuanto publica el fabricante que se hace esa operacion Y donde queda este vehiculo
		/// contra ese intervalo. Las dos mitades, o es un folleto: la distancia que le queda llega
		/// como parametro con su unidad, porque esta especificacion no guarda nada de ningun carro.
		/// </summary>
		public string DescribeServiceInterval(
			string kind, int distanceRemaining, string remainingUnit)
		{
			return AText(@"
				@clause = specification.DescribeServiceInterval(
					@kind, @distanceRemaining, @remainingUnit);
			", "clause", p => {
				p["kind",              typeof(Enum)] = WireName.OperationKind(kind);
				p["distanceRemaining", typeof(int)]  = distanceRemaining;
				p["remainingUnit",     typeof(Enum)] =
					WireName.DistanceUnit(remainingUnit, nameof(remainingUnit));
			});
		}

		/// <summary>De que revision salio el intervalo de esa operacion. TOTAL.</summary>
		public string ReadServiceIntervalRevision(string kind)
		{
			return AText(
				"@revision = specification.ReadServiceIntervalRevision(@kind);", "revision", p => {
					p["kind", typeof(Enum)] = WireName.OperationKind(kind);
				});
		}

		/// <summary>
		/// Si hay publicado un precio para esa operacion en ese hito. TOTAL: una distancia de hito
		/// que no es positiva no nombra ningun hito, asi que ahi tampoco hay nada publicado.
		/// </summary>
		public bool IsServicePricePublished(
			string kind, int milestoneDistance, string milestoneUnit)
		{
			return ATruth(@"
				@published = specification.IsServicePricePublished(
					@kind, @milestoneDistance, @milestoneUnit);
			", "published", p => {
				p["kind",              typeof(Enum)] = WireName.OperationKind(kind);
				p["milestoneDistance", typeof(int)]  = milestoneDistance;
				p["milestoneUnit",     typeof(Enum)] =
					WireName.DistanceUnit(milestoneUnit, nameof(milestoneUnit));
			});
		}

		/// <summary>
		/// Cuanto cuesta esa operacion en ese hito, con su moneda, su vigencia y si esa vigencia
		/// rige en el instante recibido. Sin instante no hay forma de decir "vencido" respecto a
		/// nada, y un precio viejo mostrado como vigente es una promesa que el taller no hizo.
		/// </summary>
		public string DescribeServicePrice(
			string kind, int milestoneDistance, string milestoneUnit, DateTime asOf)
		{
			return AText(@"
				@clause = specification.DescribeServicePrice(
					@kind, @milestoneDistance, @milestoneUnit, @asOf);
			", "clause", p => {
				p["kind",              typeof(Enum)]     = WireName.OperationKind(kind);
				p["milestoneDistance", typeof(int)]      = milestoneDistance;
				p["milestoneUnit",     typeof(Enum)]     =
					WireName.DistanceUnit(milestoneUnit, nameof(milestoneUnit));
				p["asOf",              typeof(DateTime)] = InUtc(asOf);
			});
		}

		/// <summary>De que revision salio el precio de esa operacion en ese hito. TOTAL.</summary>
		public string ReadServicePriceRevision(
			string kind, int milestoneDistance, string milestoneUnit)
		{
			return AText(@"
				@revision = specification.ReadServicePriceRevision(
					@kind, @milestoneDistance, @milestoneUnit);
			", "revision", p => {
				p["kind",              typeof(Enum)] = WireName.OperationKind(kind);
				p["milestoneDistance", typeof(int)]  = milestoneDistance;
				p["milestoneUnit",     typeof(Enum)] =
					WireName.DistanceUnit(milestoneUnit, nameof(milestoneUnit));
			});
		}

		/// <summary>Si hay alcance de garantia publicado. TOTAL.</summary>
		public bool IsCoveragePublished()
		{
			return ATruth("@published = specification.IsCoveragePublished();", "published", p => { });
		}

		/// <summary>
		/// El alcance publicado en una frase: los sistemas que declara adentro y las exclusiones,
		/// con la revision. Dice alcance; nunca dice que una reclamacion se vaya a pagar.
		/// </summary>
		public string DescribeCoverageSummary()
		{
			return AText("@clause = specification.DescribeCoverageSummary();", "clause", p => { });
		}

		/// <summary>De que revision salio el alcance publicado. TOTAL.</summary>
		public string ReadCoverageRevision()
		{
			return AText("@revision = specification.ReadCoverageRevision();", "revision", p => { });
		}

		/// <summary>
		/// Los codigos de los sistemas que el alcance declara adentro. Salen como CODIGOS porque son
		/// de lo que la persona elige al preguntar por uno. TOTAL: sin alcance publicado, vacio.
		/// </summary>
		public string[] CoveredSystems()
		{
			return ATextList("@systems = specification.CoveredSystems();", "systems", p => { });
		}

		/// <summary>
		/// Las exclusiones publicadas, cada una con su texto y su condicion. Salen como clausulas y
		/// no como codigos: un codigo no dice que esta excluido ni cuando.
		/// </summary>
		public string[] CoverageExclusions()
		{
			return ATextList(
				"@exclusions = specification.CoverageExclusions();", "exclusions", p => { });
		}

		/// <summary>
		/// Si el catalogo publicado conoce ese codigo de sistema. Consulta TOTAL de reconocimiento:
		/// el vocabulario es del fabricante, asi que un codigo desconocido es una respuesta.
		/// </summary>
		public bool TheVocabularyIsTheManufacturers(string systemCode)
		{
			return ATruth(
				"@known = specification.TheVocabularyIsTheManufacturers(@systemCode);",
				"known", p => {
					p["systemCode", typeof(string)] = TheText(systemCode, nameof(systemCode));
				});
		}

		/// <summary>
		/// Si la pregunta de cobertura se puede decidir con lo que hay. TOTAL, y es la que hay que
		/// hacer antes de leer donde esta parado: da que no cuando el limite que rige y el odometro
		/// llegaron en unidades distintas, porque aca no se convierte nada.
		/// </summary>
		public bool CanCoverageBeEvaluated(
			string systemCode, int envelopeMonths, int envelopeDistance, string envelopeUnit,
			DateTime deliveredAt, int deliveryReading, int currentReading, string currentUnit,
			DateTime asOf)
		{
			return ATruth(@"
				@can = specification.CanCoverageBeEvaluated(
					@systemCode, @envelopeMonths, @envelopeDistance, @envelopeUnit,
					@deliveredAt, @deliveryReading, @currentReading, @currentUnit, @asOf);
			", "can", TheFactsTheCoverageQuestionNeeds(
				systemCode, envelopeMonths, envelopeDistance, envelopeUnit,
				deliveredAt, deliveryReading, currentReading, currentUnit, asOf));
		}

		/// <summary>
		/// Donde esta parado ese sistema, por el nombre de la respuesta. Lectura GUARDADA: hay que
		/// preguntar antes si se puede evaluar, porque cualquiera de las respuestas devuelta aca se
		/// leeria como una medicion que nunca se hizo.
		/// </summary>
		public string ReadCoverageStanding(
			string systemCode, int envelopeMonths, int envelopeDistance, string envelopeUnit,
			DateTime deliveredAt, int deliveryReading, int currentReading, string currentUnit,
			DateTime asOf)
		{
			if (!CanCoverageBeEvaluated(
				systemCode, envelopeMonths, envelopeDistance, envelopeUnit,
				deliveredAt, deliveryReading, currentReading, currentUnit, asOf))
				throw new InvalidOperationException(
					"La cobertura de ese sistema no se pudo evaluar con lo que llego: pregunte "
					+ "CanCoverageBeEvaluated antes. Ninguna de las respuestas posibles significa 'no "
					+ "se pudo medir', asi que devolver una aca la convertiria en un hecho medido.");

			return AText(@"
				@standing = specification.ReadCoverageStanding(
					@systemCode, @envelopeMonths, @envelopeDistance, @envelopeUnit,
					@deliveredAt, @deliveryReading, @currentReading, @currentUnit, @asOf);
			", "standing", TheFactsTheCoverageQuestionNeeds(
				systemCode, envelopeMonths, envelopeDistance, envelopeUnit,
				deliveredAt, deliveryReading, currentReading, currentUnit, asOf));
		}

		/// <summary>
		/// La respuesta de cobertura en una frase: donde esta parado o por que no se pudo decidir,
		/// contra que limites se midio, las exclusiones publicadas que haya, y la revision. TOTAL, y
		/// es la lectura primaria: la frase es la que dice la ley, no el nombre suelto.
		/// </summary>
		public string DescribeCoverage(
			string systemCode, int envelopeMonths, int envelopeDistance, string envelopeUnit,
			DateTime deliveredAt, int deliveryReading, int currentReading, string currentUnit,
			DateTime asOf)
		{
			return AText(@"
				@clause = specification.DescribeCoverage(
					@systemCode, @envelopeMonths, @envelopeDistance, @envelopeUnit,
					@deliveredAt, @deliveryReading, @currentReading, @currentUnit, @asOf);
			", "clause", TheFactsTheCoverageQuestionNeeds(
				systemCode, envelopeMonths, envelopeDistance, envelopeUnit,
				deliveredAt, deliveryReading, currentReading, currentUnit, asOf));
		}

		/// <summary>Si hay publicada una regla de servicio en otro taller. TOTAL.</summary>
		public bool IsServicingPolicyPublished()
		{
			return ATruth(
				"@published = specification.IsServicingPolicyPublished();", "published", p => { });
		}

		/// <summary>
		/// La regla publicada sobre servicio en otro taller Y donde queda este vehiculo frente a
		/// ella, con los conteos tal como llegaron. Dice el alcance y la posicion; no dice que
		/// ninguna reclamacion se vaya a pagar.
		/// </summary>
		public string DescribeServicingElsewhereRule(
			int externalServicesWithEvidence, int externalServicesWithoutEvidence,
			int missedMaintenanceCount)
		{
			return AText(@"
				@clause = specification.DescribeServicingElsewhereRule(
					@withEvidence, @withoutEvidence, @missed);
			", "clause", p => {
				p["withEvidence",    typeof(int)] = externalServicesWithEvidence;
				p["withoutEvidence", typeof(int)] = externalServicesWithoutEvidence;
				p["missed",          typeof(int)] = missedMaintenanceCount;
			});
		}

		/// <summary>De que revision salio la regla de servicio en otro taller. TOTAL.</summary>
		public string ReadServicingPolicyRevision()
		{
			return AText(
				"@revision = specification.ReadServicingPolicyRevision();", "revision", p => { });
		}

		/// <summary>
		/// Si alguna revision sellada puede contestar. TOTAL, y es la que hay que hacer antes de
		/// pedir el codigo o el instante de la vigente.
		/// </summary>
		public bool HasASealedRevision()
		{
			return ATruth("@sealed = specification.HasASealedRevision();", "sealed", p => { });
		}

		/// <summary>
		/// El codigo de la revision vigente, que es la sellada mas reciente. Lectura GUARDADA: sin
		/// ninguna sellada no hay codigo que dar.
		/// </summary>
		public string ReadCurrentRevisionCode()
		{
			if (!HasASealedRevision())
				throw new InvalidOperationException(
					"Esta especificacion no tiene ninguna revision sellada: pregunte "
					+ "HasASealedRevision antes. Un codigo vacio aca nombraria una publicacion que "
					+ "nadie hizo.");

			return AText("@code = specification.ReadCurrentRevisionCode();", "code", p => { });
		}

		/// <summary>
		/// El instante en que el fabricante publico la revision vigente. Lectura GUARDADA por lo
		/// mismo, y sale en UTC igual que entro.
		/// </summary>
		public DateTime ReadCurrentRevisionPublishedAt()
		{
			if (!HasASealedRevision())
				throw new InvalidOperationException(
					"Esta especificacion no tiene ninguna revision sellada: pregunte "
					+ "HasASealedRevision antes. Un DateTime.MinValue aca seria un centinela magico "
					+ "-- un instante que parece un instante y no lo es.");

			return AnInstant("@at = specification.ReadCurrentRevisionPublishedAt();", "at", p => { });
		}

		/// <summary>
		/// Donde esta parada esa revision, por su nombre. El codigo nombra una entrada del registro
		/// y no un punto de un rango, asi que uno que no esta REVIENTA: contestar un estado ahi
		/// inventaria una revision que nadie abrio.
		/// </summary>
		public string ReadRevisionState(string revisionCode)
		{
			return AText("@state = specification.ReadRevisionState(@revisionCode);", "state", p => {
				p["revisionCode", typeof(string)] = TheText(revisionCode, nameof(revisionCode));
			});
		}

		public string Apply(IMensajeDeEvento message)
		{
			ArgumentNullException.ThrowIfNull(message);

			try
			{
				if (message is OdometerReported reading) return ApplyOdometerReported(reading);
				if (message is ConditionThresholdCrossed crossing) return ApplyConditionThresholdCrossed(crossing);
				if (message is OperationCompleted completion) return ApplyOperationCompleted(completion);
				if (message is PlanOperationSeeded seeded) return ApplyPlanOperationSeeded(seeded);
				if (message is PlanRevisionAdopted revision) return ApplyPlanRevisionAdopted(revision);

				if (message is SpecificationRevisionOpened opened)
					return ApplySpecificationRevisionOpened(opened);
				if (message is FluidSpecificationPublished fluid)
					return ApplyFluidSpecificationPublished(fluid);
				if (message is TyreSpecificationPublished tyre)
					return ApplyTyreSpecificationPublished(tyre);
				if (message is FuelGradePublished fuel)
					return ApplyFuelGradePublished(fuel);
				if (message is ServiceIntervalPublished interval)
					return ApplyServiceIntervalPublished(interval);
				if (message is ServicePricePublished price)
					return ApplyServicePricePublished(price);
				if (message is CoveredSystemPublished system)
					return ApplyCoveredSystemPublished(system);
				if (message is CoverageExclusionPublished exclusion)
					return ApplyCoverageExclusionPublished(exclusion);
				if (message is ServicingPolicyPublished policy)
					return ApplyServicingPolicyPublished(policy);
				if (message is SpecificationRevisionSealed sealing)
					return ApplySpecificationRevisionSealed(sealing);
				if (message is SpecificationRevisionSuperseded superseding)
					return ApplySpecificationRevisionSuperseded(superseding);

				throw new ArgumentException(
					$"Esta app no tiene comando para un mensaje de tipo '{message.Tipo}'. La ruta "
					+ "debio devolver null para descartarlo.", nameof(message));
			}
			catch (DomainException refused)
			{
				return TheNarrativeMovesPast(message, refused);
			}
		}

		private string ApplyOdometerReported(OdometerReported message)
		{
			return app.Using(
				scriptForChk: TheEventIsNew,
				@"
					{
						schedule.RecordReading(@value, @unit, @observedAt, @source);
						logbook.Advance(@event);
					}
				")
			.WithParameters(p => {
				p["event",      typeof(long)]     = message.EventNumber;
				p["value",      typeof(int)]      = message.Value;
				p["unit",       typeof(Enum)]     = WireName.DistanceUnit(message.Unit, "unit");
				p["observedAt", typeof(DateTime)] = InUtc(message.ObservedAt);
				p["source",     typeof(Enum)]     = WireName.ReadingSource(message.Source);
			})
			.PerformCheckThenCommand();
		}

		private string ApplyConditionThresholdCrossed(ConditionThresholdCrossed message)
		{
			return app.Using(
				scriptForChk: TheEventIsNew,
				@"
					{
						schedule.RecordConditionThreshold(@kind, @observedAt);
						logbook.Advance(@event);
					}
				")
			.WithParameters(p => {
				p["event",      typeof(long)]     = message.EventNumber;
				p["kind",       typeof(Enum)]     = WireName.OperationKind(message.Kind);
				p["observedAt", typeof(DateTime)] = InUtc(message.ObservedAt);
			})
			.PerformCheckThenCommand();
		}

		private string ApplyOperationCompleted(OperationCompleted message)
		{
			return app.Using(
				scriptForChk: TheEventIsNew,
				@"
					{
						schedule.CompleteOperation(@kind, @completedAt, @completionReading, @unit);
						logbook.Advance(@event);
					}
				")
			.WithParameters(p => {
				p["event",             typeof(long)]     = message.EventNumber;
				p["kind",              typeof(Enum)]     = WireName.OperationKind(message.Kind);
				p["completedAt",       typeof(DateTime)] = InUtc(message.CompletedAt);
				p["completionReading", typeof(int)]      = message.CompletionReading;
				p["unit",              typeof(Enum)]     = WireName.DistanceUnit(message.Unit, "unit");
			})
			.PerformCheckThenCommand();
		}

		private string ApplyPlanOperationSeeded(PlanOperationSeeded message)
		{
			TheDeclaredBasisAgreesWithItsLimits(
				message.Basis, message.TimeLimitDays, message.DistanceLimit);

			return app.Using(
				scriptForChk: TheEventIsNew,
				@"
					{
						schedule.SeedPlanOperation(
							@kind, @basis, @timeLimitDays, @distanceLimit, @unit,
							@isMandatory, @isSafety,
							@anchoredAt, @anchorReading, @anchorUnit);
						logbook.Advance(@event);
					}
				")
			.WithParameters(p => {
				p["event",         typeof(long)]     = message.EventNumber;
				p["kind",          typeof(Enum)]     = WireName.OperationKind(message.Kind);
				p["basis",         typeof(Enum)]     = WireName.IntervalBasis(message.Basis);
				p["timeLimitDays", typeof(int)]      = message.TimeLimitDays;
				p["distanceLimit", typeof(int)]      = message.DistanceLimit;
				p["unit",          typeof(Enum)]     = WireName.DistanceUnit(message.Unit, "unit");
				p["isMandatory",   typeof(bool)]     = message.IsMandatory;
				p["isSafety",      typeof(bool)]     = message.IsSafety;
				p["anchoredAt",    typeof(DateTime)] = InUtc(message.AnchoredAt);
				p["anchorReading", typeof(int)]      = message.AnchorReading;
				p["anchorUnit",    typeof(Enum)]     =
					WireName.DistanceUnit(message.AnchorUnit, "anchorUnit");
			})
			.PerformCheckThenCommand();
		}

		private string ApplyPlanRevisionAdopted(PlanRevisionAdopted message)
		{
			return app.Using(
				scriptForChk: TheEventIsNew,
				@"
					{
						schedule.AdoptPlanRevision(@revisionCode, @adoptedAt);
						logbook.Advance(@event);
					}
				")
			.WithParameters(p => {
				p["event",        typeof(long)]     = message.EventNumber;
				p["revisionCode", typeof(string)]   = message.RevisionCode;
				p["adoptedAt",    typeof(DateTime)] = InUtc(message.AdoptedAt);
			})
			.PerformCheckThenCommand();
		}

		private string ApplySpecificationRevisionOpened(SpecificationRevisionOpened message)
		{
			return app.Using(
				scriptForChk: TheEventIsNew,
				@"
					{
						specification.OpenRevision(@revisionCode, @publishedAt);
						logbook.Advance(@event);
					}
				")
			.WithParameters(p => {
				p["event",        typeof(long)]     = message.EventNumber;
				p["revisionCode", typeof(string)]   =
					TheText(message.RevisionCode, "revisionCode");
				p["publishedAt",  typeof(DateTime)] = InUtc(message.PublishedAt);
			})
			.PerformCheckThenCommand();
		}

		private string ApplyFluidSpecificationPublished(FluidSpecificationPublished message)
		{
			return app.Using(
				scriptForChk: TheEventIsNew,
				@"
					{
						specification.PublishFluidSpecification(
							@revisionCode, @kind, @grade,
							@capacity, @capacityUnit,
							@intervalDistance, @intervalUnit, @intervalDays);
						logbook.Advance(@event);
					}
				")
			.WithParameters(p => {
				p["event",            typeof(long)]    = message.EventNumber;
				p["revisionCode",     typeof(string)]  =
					TheText(message.RevisionCode, "revisionCode");
				p["kind",             typeof(Enum)]    = WireName.FluidKind(message.Kind);
				p["grade",            typeof(string)]  = TheText(message.Grade, "grade");
				p["capacity",         typeof(decimal)] = Normalized(message.Capacity);
				p["capacityUnit",     typeof(Enum)]    =
					WireName.VolumeUnit(message.CapacityUnit, "capacityUnit");
				p["intervalDistance", typeof(int)]     = message.IntervalDistance;
				p["intervalUnit",     typeof(Enum)]    =
					WireName.DistanceUnit(message.IntervalUnit, "intervalUnit");
				p["intervalDays",     typeof(int)]     = message.IntervalDays;
			})
			.PerformCheckThenCommand();
		}

		private string ApplyTyreSpecificationPublished(TyreSpecificationPublished message)
		{
			return app.Using(
				scriptForChk: TheEventIsNew,
				@"
					{
						specification.PublishTyreSpecification(
							@revisionCode, @position, @size,
							@coldPressure, @pressureUnit, @ratings);
						logbook.Advance(@event);
					}
				")
			.WithParameters(p => {
				p["event",        typeof(long)]    = message.EventNumber;
				p["revisionCode", typeof(string)]  =
					TheText(message.RevisionCode, "revisionCode");
				p["position",     typeof(Enum)]    = WireName.TyrePosition(message.Position);
				p["size",         typeof(string)]  = TheText(message.Size, "size");
				p["coldPressure", typeof(decimal)] = Normalized(message.ColdPressure);
				p["pressureUnit", typeof(Enum)]    =
					WireName.PressureUnit(message.PressureUnit, "pressureUnit");
				p["ratings",      typeof(string)]  = TheText(message.Ratings, "ratings");
			})
			.PerformCheckThenCommand();
		}

		private string ApplyFuelGradePublished(FuelGradePublished message)
		{
			return app.Using(
				scriptForChk: TheEventIsNew,
				@"
					{
						specification.PublishFuelGrade(@revisionCode, @grade, @standing);
						logbook.Advance(@event);
					}
				")
			.WithParameters(p => {
				p["event",        typeof(long)]   = message.EventNumber;
				p["revisionCode", typeof(string)] =
					TheText(message.RevisionCode, "revisionCode");
				p["grade",        typeof(string)] = TheText(message.Grade, "grade");
				p["standing",     typeof(Enum)]   = WireName.FuelGradeStanding(message.Standing);
			})
			.PerformCheckThenCommand();
		}

		private string ApplyServiceIntervalPublished(ServiceIntervalPublished message)
		{
			return app.Using(
				scriptForChk: TheEventIsNew,
				@"
					{
						specification.PublishServiceInterval(
							@revisionCode, @kind,
							@intervalDistance, @intervalUnit, @intervalDays);
						logbook.Advance(@event);
					}
				")
			.WithParameters(p => {
				p["event",            typeof(long)]   = message.EventNumber;
				p["revisionCode",     typeof(string)] =
					TheText(message.RevisionCode, "revisionCode");
				p["kind",             typeof(Enum)]   = WireName.OperationKind(message.Kind);
				p["intervalDistance", typeof(int)]    = message.IntervalDistance;
				p["intervalUnit",     typeof(Enum)]   =
					WireName.DistanceUnit(message.IntervalUnit, "intervalUnit");
				p["intervalDays",     typeof(int)]    = message.IntervalDays;
			})
			.PerformCheckThenCommand();
		}

		private string ApplyServicePricePublished(ServicePricePublished message)
		{
			return app.Using(
				scriptForChk: TheEventIsNew,
				@"
					{
						specification.PublishServicePrice(
							@revisionCode, @kind,
							@milestoneDistance, @milestoneUnit,
							@amount, @currency, @validFrom, @validUntil);
						logbook.Advance(@event);
					}
				")
			.WithParameters(p => {
				p["event",             typeof(long)]     = message.EventNumber;
				p["revisionCode",      typeof(string)]   =
					TheText(message.RevisionCode, "revisionCode");
				p["kind",              typeof(Enum)]     = WireName.OperationKind(message.Kind);
				p["milestoneDistance", typeof(int)]      = message.MilestoneDistance;
				p["milestoneUnit",     typeof(Enum)]     =
					WireName.DistanceUnit(message.MilestoneUnit, "milestoneUnit");
				p["amount",            typeof(decimal)]  = Normalized(message.Amount);
				p["currency",          typeof(string)]   = TheText(message.Currency, "currency");
				p["validFrom",         typeof(DateTime)] = InUtc(message.ValidFrom);
				p["validUntil",        typeof(DateTime)] = InUtc(message.ValidUntil);
			})
			.PerformCheckThenCommand();
		}

		private string ApplyCoveredSystemPublished(CoveredSystemPublished message)
		{
			return app.Using(
				scriptForChk: TheEventIsNew,
				@"
					{
						specification.PublishCoveredSystem(
							@revisionCode, @systemCode,
							@ownLimitMonths, @ownLimitDistance, @ownLimitUnit);
						logbook.Advance(@event);
					}
				")
			.WithParameters(p => {
				p["event",            typeof(long)]   = message.EventNumber;
				p["revisionCode",     typeof(string)] =
					TheText(message.RevisionCode, "revisionCode");
				p["systemCode",       typeof(string)] = TheText(message.SystemCode, "systemCode");
				p["ownLimitMonths",   typeof(int)]    = message.OwnLimitMonths;
				p["ownLimitDistance", typeof(int)]    = message.OwnLimitDistance;
				p["ownLimitUnit",     typeof(Enum)]   =
					WireName.DistanceUnit(message.OwnLimitUnit, "ownLimitUnit");
			})
			.PerformCheckThenCommand();
		}

		private string ApplyCoverageExclusionPublished(CoverageExclusionPublished message)
		{
			return app.Using(
				scriptForChk: TheEventIsNew,
				@"
					{
						specification.PublishCoverageExclusion(
							@revisionCode, @systemCode, @exclusion, @condition);
						logbook.Advance(@event);
					}
				")
			.WithParameters(p => {
				p["event",        typeof(long)]   = message.EventNumber;
				p["revisionCode", typeof(string)] =
					TheText(message.RevisionCode, "revisionCode");
				p["systemCode",   typeof(string)] = TheText(message.SystemCode, "systemCode");
				p["exclusion",    typeof(string)] = TheText(message.Exclusion, "exclusion");
				p["condition",    typeof(string)] = TheText(message.Condition, "condition");
			})
			.PerformCheckThenCommand();
		}

		private string ApplyServicingPolicyPublished(ServicingPolicyPublished message)
		{
			return app.Using(
				scriptForChk: TheEventIsNew,
				@"
					{
						specification.PublishServicingPolicy(
							@revisionCode, @rule, @whatPreservesCover);
						logbook.Advance(@event);
					}
				")
			.WithParameters(p => {
				p["event",              typeof(long)]   = message.EventNumber;
				p["revisionCode",       typeof(string)] =
					TheText(message.RevisionCode, "revisionCode");
				p["rule",               typeof(string)] = TheText(message.Rule, "rule");
				p["whatPreservesCover", typeof(string)] =
					TheText(message.WhatPreservesCover, "whatPreservesCover");
			})
			.PerformCheckThenCommand();
		}

		private string ApplySpecificationRevisionSealed(SpecificationRevisionSealed message)
		{
			return app.Using(
				scriptForChk: TheEventIsNew,
				@"
					{
						specification.SealRevision(@revisionCode);
						logbook.Advance(@event);
					}
				")
			.WithParameters(p => {
				p["event",        typeof(long)]   = message.EventNumber;
				p["revisionCode", typeof(string)] =
					TheText(message.RevisionCode, "revisionCode");
			})
			.PerformCheckThenCommand();
		}

		private string ApplySpecificationRevisionSuperseded(
			SpecificationRevisionSuperseded message)
		{
			return app.Using(
				scriptForChk: TheEventIsNew,
				@"
					{
						specification.SupersedeRevision(@revisionCode, @supersededByCode);
						logbook.Advance(@event);
					}
				")
			.WithParameters(p => {
				p["event",            typeof(long)]   = message.EventNumber;
				p["revisionCode",     typeof(string)] =
					TheText(message.RevisionCode, "revisionCode");
				p["supersededByCode", typeof(string)] =
					TheText(message.SupersededByCode, "supersededByCode");
			})
			.PerformCheckThenCommand();
		}

		private const string TheEventIsNew =
			"Check(@event > logbook.LastEvent()) Error 'evento ya procesado';";

		private string TheNarrativeMovesPast(IMensajeDeEvento message, DomainException refused)
		{
			string rejection = app.Using(
				scriptForChk: TheEventIsNew,
				"logbook.Advance(@event);")
			.WithParameters(p => {
				p["event", typeof(long)] = message.Evento;
			})
			.PerformCheckThenCommand();

			return rejection.Length > 0 ? rejection : refused.Message;
		}

		long IDiarioDeLaApp.UltimoEventoProcesado()
		{
			return LastEventApplied();
		}

		string IDiarioDeLaApp.TokenGuardado()
		{
			return StoredToken();
		}

		string IDiarioDeLaApp.Aplicar(IMensajeDeEvento message)
		{
			return Apply(message);
		}

		private string AText(string script, string name, Action<Parameters> arguments)
		{
			using (RentedParameter rented = app.Actor.RentedParameters())
			{
				app.Actor.Using(script)
				.WithParameters(rented, p => {
					p[Parameter.Out, name, typeof(string)] = default;
					arguments(p);
				})
				.PerformQuery();

				return rented[name].GetValue<string>();
			}
		}

		private int ACount(string script, string name, Action<Parameters> arguments)
		{
			using (RentedParameter rented = app.Actor.RentedParameters())
			{
				app.Actor.Using(script)
				.WithParameters(rented, p => {
					p[Parameter.Out, name, typeof(int)] = default;
					arguments(p);
				})
				.PerformQuery();

				return rented[name].GetValue<int>();
			}
		}

		private long ANumber(string script, string name, Action<Parameters> arguments)
		{
			using (RentedParameter rented = app.Actor.RentedParameters())
			{
				app.Actor.Using(script)
				.WithParameters(rented, p => {
					p[Parameter.Out, name, typeof(long)] = default;
					arguments(p);
				})
				.PerformQuery();

				return rented[name].GetValue<long>();
			}
		}

		private bool ATruth(string script, string name, Action<Parameters> arguments)
		{
			using (RentedParameter rented = app.Actor.RentedParameters())
			{
				app.Actor.Using(script)
				.WithParameters(rented, p => {
					p[Parameter.Out, name, typeof(bool)] = default;
					arguments(p);
				})
				.PerformQuery();

				return rented[name].GetValue<bool>();
			}
		}

		/// <summary>
		/// Una lista de textos de vuelta desde el actor. Un arreglo de primitivos SI cruza la
		/// frontera del parametro y vuelve tipado, asi que la lista sale como lista y nadie tiene
		/// que partir una frase para recuperarla.
		/// </summary>
		private string[] ATextList(string script, string name, Action<Parameters> arguments)
		{
			using (RentedParameter rented = app.Actor.RentedParameters())
			{
				app.Actor.Using(script)
				.WithParameters(rented, p => {
					p[Parameter.Out, name, typeof(string[])] = default;
					arguments(p);
				})
				.PerformQuery();

				return rented[name].GetValue<string[]>();
			}
		}

		/// <summary>
		/// Los nueve hechos que la pregunta de cobertura necesita, atados una sola vez para las tres
		/// lecturas que la hacen. Van los nueve tal como llegaron: la resta entre el limite y el
		/// odometro es donde se compara la unidad, y precalcularla afuera se llevaria esa
		/// comparacion fuera del dominio.
		/// </summary>
		private static Action<Parameters> TheFactsTheCoverageQuestionNeeds(
			string systemCode, int envelopeMonths, int envelopeDistance, string envelopeUnit,
			DateTime deliveredAt, int deliveryReading, int currentReading, string currentUnit,
			DateTime asOf)
		{
			return p => {
				p["systemCode",       typeof(string)]   = TheText(systemCode, nameof(systemCode));
				p["envelopeMonths",   typeof(int)]      = envelopeMonths;
				p["envelopeDistance", typeof(int)]      = envelopeDistance;
				p["envelopeUnit",     typeof(Enum)]     =
					WireName.DistanceUnit(envelopeUnit, nameof(envelopeUnit));
				p["deliveredAt",      typeof(DateTime)] = InUtc(deliveredAt);
				p["deliveryReading",  typeof(int)]      = deliveryReading;
				p["currentReading",   typeof(int)]      = currentReading;
				p["currentUnit",      typeof(Enum)]     =
					WireName.DistanceUnit(currentUnit, nameof(currentUnit));
				p["asOf",             typeof(DateTime)] = InUtc(asOf);
			};
		}

		private DateTime AnInstant(string script, string name, Action<Parameters> arguments)
		{
			using (RentedParameter rented = app.Actor.RentedParameters())
			{
				app.Actor.Using(script)
				.WithParameters(rented, p => {
					p[Parameter.Out, name, typeof(DateTime)] = default;
					arguments(p);
				})
				.PerformQuery();

				return DateTime.SpecifyKind(rented[name].GetValue<DateTime>(), DateTimeKind.Utc);
			}
		}

		private static void TheDeclaredBasisAgreesWithItsLimits(
			string basis, int timeLimitDays, int distanceLimit)
		{
			IntervalRule.BasisIsNeverInferred(
				(IntervalBasisCode)Enum.Parse(
					typeof(IntervalBasisCode), WireName.IntervalBasis(basis)),
				timeLimitDays,
				distanceLimit);
		}

		private static DateTime InUtc(DateTime instant)
		{
			DateTime utc = instant.Kind == DateTimeKind.Local ? instant.ToUniversalTime() : instant;

			return new DateTime(
				utc.Ticks - utc.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
		}

		/// <summary>
		/// Le da al decimal la MISMA forma que va a tener despues de pasar por el journal, para que
		/// el valor vivo y el reproducido sean el mismo valor y no dos que apenas comparan iguales.
		/// El valor nunca cambia; lo que cambia es como se dice, y decirlo distinto antes y despues
		/// de reabrir romperia la promesa de que una respuesta ya dada no cambia sola.
		/// </summary>
		private static decimal Normalized(decimal value)
		{
			return decimal.Parse(
				value.ToString(TheShapeTheJournalWrites, CultureInfo.InvariantCulture),
				NumberStyles.Number,
				CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Como escribe el journal un decimal: sin los ceros que sobran a la derecha y con veintidos
		/// decimales como maximo. Es la forma que hay que darle antes de mandarlo.
		/// </summary>
		private const string TheShapeTheJournalWrites = "0.######################";

		/// <summary>
		/// Una cadena que la especificacion necesita para poder contestar: se exige aca, donde el
		/// rechazo no cuesta ninguna entrada, y se recorta por los extremos, que es lo que hace que
		/// quien publica y quien pregunta nombren la misma cosa. Lo de adentro no se toca -- ni la
		/// caja ni los espacios interiores -- porque el vocabulario es del fabricante y aca no se le
		/// arregla la ortografia a nadie.
		/// </summary>
		private static string TheText(string text, string parameterName)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(text, parameterName);

			return text.Trim();
		}
	}
}
