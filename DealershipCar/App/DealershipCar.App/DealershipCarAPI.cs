using System;
using System.Collections.Generic;
using System.Globalization;
using Choreography.Theater;
using DealershipCar.App.Events;

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
			WireName.BasisAgreesWithItsLimits(basis, timeLimitDays, distanceLimit);

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
			WireName.BasisAgreesWithItsLimits(basis, timeLimitDays, distanceLimit);

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

		/// <summary>
		/// Registra la entrega de ESTE vehiculo: cuando se entrego, en que odometro estaba ese dia y
		/// en que unidad se leyo. Es el ancla desde donde se miden las dos mitades del sobre.
		/// Registrarla de nuevo la CORRIGE -- gana la ultima -- porque es un hecho historico y no una
		/// regla con vigencia.
		/// </summary>
		public void RecordDelivery(DateTime deliveredAt, int reading, string unit)
		{
			TheOdometerAtDeliveryIsNotNegative(reading);

			app.Using("profile.RecordDelivery(@deliveredAt, @reading, @unit);")
			.WithParameters(p => {
				p["deliveredAt", typeof(DateTime)] = InUtc(deliveredAt);
				p["reading",     typeof(int)]      = reading;
				p["unit",        typeof(Enum)]     = WireName.DistanceUnit(unit, nameof(unit));
			})
			.PerformCommand();
		}

		/// <summary>
		/// Publica el sobre de garantia de ESTE vehiculo: su plazo en meses, su distancia con la
		/// unidad en que se midio, y cuando la agencia lo publico. Rige desde su instante de
		/// publicacion hacia adelante, asi que uno fechado antes del que rige no lo desplaza y
		/// tampoco se rechaza.
		/// </summary>
		public void PublishWarrantyEnvelope(
			int months, int distance, string unit, DateTime publishedAt)
		{
			TheEnvelopeCarriesBothOfItsHalves(months, distance);

			app.Using("profile.PublishWarrantyEnvelope(@months, @distance, @unit, @publishedAt);")
			.WithParameters(p => {
				p["months",      typeof(int)]      = months;
				p["distance",    typeof(int)]      = distance;
				p["unit",        typeof(Enum)]     = WireName.DistanceUnit(unit, nameof(unit));
				p["publishedAt", typeof(DateTime)] = InUtc(publishedAt);
			})
			.PerformCommand();
		}

		/// <summary>
		/// Registra la placa que el concesionario le publico a ESTE vehiculo y desde cuando rige. Una
		/// segunda placa es un hecho nuevo y no una correccion de la primera: la anterior se sigue
		/// leyendo por su propio instante. Un texto que no deja nada al normalizar se rechaza ACA,
		/// donde el rechazo todavia no cuesta ninguna entrada.
		/// </summary>
		public void RecordPlateAssignment(string plate, DateTime assignedAt)
		{
			WireName.Plate(plate);

			app.Using("profile.RecordPlateAssignment(@plate, @assignedAt);")
			.WithParameters(p => {
				p["plate",      typeof(string)]   = plate;
				p["assignedAt", typeof(DateTime)] = InUtc(assignedAt);
			})
			.PerformCommand();
		}

		/// <summary>
		/// Registra la entrada de ESTE vehiculo al taller: en que sucursal, con que asunto lo
		/// recibieron y cuando entro. Se rechaza ACA mientras el vehiculo ya estuviera adentro en ese
		/// instante, donde el rechazo todavia no cuesta ninguna entrada.
		/// </summary>
		public void RecordShopEntry(string branch, string subject, DateTime enteredAt)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(branch);
			ArgumentException.ThrowIfNullOrWhiteSpace(subject);

			TheVehicleHasToBeOutsideAt(enteredAt);

			app.Using("stays.RecordEntry(@branch, @subject, @enteredAt);")
			.WithParameters(p => {
				p["branch",    typeof(string)]   = branch;
				p["subject",   typeof(string)]   = subject;
				p["enteredAt", typeof(DateTime)] = InUtc(enteredAt);
			})
			.PerformCommand();
		}

		/// <summary>
		/// Registra que entregaron ESTE vehiculo: cuando salio y que dias de calendario dice la
		/// sucursal que cubrio la estadia. Se rechaza ACA mientras no hubiera ninguna estadia que
		/// cerrar en ese instante.
		/// </summary>
		public void RecordShopCollection(DateTime collectedAt, string[] coveredDays)
		{
			string[] days = TheDaysTheBranchSaid(coveredDays);

			TheVehicleHasToBeInsideAt(collectedAt);

			app.Using("stays.RecordCollection(@collectedAt, @days);")
			.WithParameters(p => {
				p["collectedAt", typeof(DateTime)] = InUtc(collectedAt);
				p["days",        typeof(string[])] = days;
			})
			.PerformCommand();
		}

		/// <summary>
		/// Registra de un solo acto una estadia que ya termino: donde, por que, sus dos instantes y
		/// los dias que cubrio. Es la forma correctiva, para lo que se registra despues de haber
		/// pasado, y NO se pre-chequea: una estadia cerrada no deja una segunda abierta, asi que
		/// refusarla mientras otra sigue abierta refusaria un registro legitimo.
		/// </summary>
		public void RecordShopStay(
			string branch, string subject, DateTime enteredAt, DateTime collectedAt,
			string[] coveredDays)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(branch);
			ArgumentException.ThrowIfNullOrWhiteSpace(subject);

			string[] days = TheDaysTheBranchSaid(coveredDays);

			app.Using(
				"stays.RecordStay(@branch, @subject, @enteredAt, @collectedAt, @days);")
			.WithParameters(p => {
				p["branch",      typeof(string)]   = branch;
				p["subject",     typeof(string)]   = subject;
				p["enteredAt",   typeof(DateTime)] = InUtc(enteredAt);
				p["collectedAt", typeof(DateTime)] = InUtc(collectedAt);
				p["days",        typeof(string[])] = days;
			})
			.PerformCommand();
		}

		/// <summary>
		/// Registra un gasto que se consume en el periodo en que se incurrio: cuando se incurrio,
		/// cuanto costo, en que moneda y en que se fue. Que sea de los que se consumen en su periodo
		/// lo dice ESTE comando: no hay ninguna ranura por donde decir la clase, ni ninguna vida util
		/// que darle.
		/// </summary>
		public void RecordRecurringCost(
			DateTime incurredAt, decimal amount, string currency, string subject)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(subject);

			TheAmountSpentIsNotNegative(amount);

			app.Using("ledger.RecordRecurringCost(@incurredAt, @amount, @currency, @subject);")
			.WithParameters(p => {
				p["incurredAt", typeof(DateTime)] = InUtc(incurredAt);
				p["amount",     typeof(decimal)]  = Normalized(amount);
				p["currency",   typeof(string)]   = WireName.Currency(currency);
				p["subject",    typeof(string)]   = subject;
			})
			.PerformCommand();
		}

		/// <summary>
		/// Registra un gasto que sigue dando valor despues del periodo en que se incurrio, con la
		/// vida util en meses que le dio quien lo vendio. La vida util es OBLIGATORIA y no tiene
		/// valor por defecto, y que sea de los duraderos lo dice ESTE comando.
		/// </summary>
		public void RecordDurableCost(
			DateTime incurredAt, decimal amount, string currency, string subject,
			int expectedLifeMonths)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(subject);

			TheAmountSpentIsNotNegative(amount);
			TheExpectedLifeIsSaidInPositiveMonths(expectedLifeMonths);

			app.Using(@"
				ledger.RecordDurableCost(
					@incurredAt, @amount, @currency, @subject, @expectedLifeMonths);
			")
			.WithParameters(p => {
				p["incurredAt",         typeof(DateTime)] = InUtc(incurredAt);
				p["amount",             typeof(decimal)]  = Normalized(amount);
				p["currency",           typeof(string)]   = WireName.Currency(currency);
				p["subject",            typeof(string)]   = subject;
				p["expectedLifeMonths", typeof(int)]      = expectedLifeMonths;
			})
			.PerformCommand();
		}

		/// <summary>
		/// Si ese periodo tiene algun gasto, salido del banco adentro o consumido adentro. TOTAL, y
		/// contesta que NO donde no hay ninguno: la ausencia es un tipo y no un rechazo.
		/// </summary>
		public bool HasAnyCostBetween(DateTime from, DateTime to)
		{
			return ATruth(
				"@has = ledger.HasAnyCostBetween(@from, @to);", "has",
				APeriodOfInstants(from, to));
		}

		/// <summary>
		/// El gasto de ese periodo en una frase, con LAS DOS VISTAS adentro -- lo que salio del banco
		/// y lo que el periodo consumio -- o la constancia con nombre de que no hay ninguno. TOTAL, y
		/// ninguna lectura contesta una vista sola.
		/// </summary>
		public string DescribeSpendBetween(DateTime from, DateTime to)
		{
			return AText(
				"@says = ledger.DescribeSpendBetween(@from, @to);", "says",
				APeriodOfInstants(from, to));
		}

		/// <summary>
		/// Si todo el gasto de ese periodo esta dicho en una sola moneda. TOTAL, y un periodo sin
		/// ningun gasto contesta que NO: no hay ninguna moneda en la que este dicho.
		/// </summary>
		public bool IsSpendSaidInOneCurrencyBetween(DateTime from, DateTime to)
		{
			return ATruth(
				"@said = ledger.IsSpendSaidInOneCurrencyBetween(@from, @to);", "said",
				APeriodOfInstants(from, to));
		}

		/// <summary>
		/// El gasto de ese periodo dicho moneda por moneda, o el estado con nombre en que hay mas de
		/// una y no hay total entre ellas. TOTAL. El dinero no se convierte: agrupar por moneda es de
		/// quien consulta.
		/// </summary>
		public string DescribeSpendPerCurrencyBetween(DateTime from, DateTime to)
		{
			return AText(
				"@says = ledger.DescribeSpendPerCurrencyBetween(@from, @to);", "says",
				APeriodOfInstants(from, to));
		}

		/// <summary>
		/// Los dos periodos dichos juntos en una sola frase, cada uno con sus limites y sus dos
		/// vistas. TOTAL. El PRIMER par de extremos es el primer periodo; ninguna diferencia entre
		/// los dos sale por ninguna lectura como numero.
		/// </summary>
		public string DescribeTwoPeriodsOfSpendCompared(
			DateTime from1, DateTime to1, DateTime from2, DateTime to2)
		{
			return AText(@"
				@says = ledger.DescribeTwoPeriodsOfSpendCompared(@from1, @to1, @from2, @to2);
			", "says", TwoPeriodsOfInstants(from1, to1, from2, to2));
		}

		/// <summary>
		/// Las entradas incurridas adentro de ese periodo como filas: cuando se incurrio cada una, en
		/// que se fue, cuanto costo dicho por su moneda, de que clase es, su vida util si tiene, y la
		/// frase con que el dominio la dice, en columnas paralelas y en orden del instante en que se
		/// incurrieron. TOTAL, y contesta CERO filas donde no hay ninguna.
		/// </summary>
		public CostLedgerRecord CostEntriesBetween(DateTime from, DateTime to)
		{
			string[][] columns = TheTextColumns(@"
				@incurredAt     = ledger.CostEntryInstantsBetween(@from, @to);
				@subject        = ledger.CostEntrySubjectsBetween(@from, @to);
				@money          = ledger.CostEntryMoniesBetween(@from, @to);
				@classification = ledger.CostEntryClassificationsBetween(@from, @to);
				@expectedLife   = ledger.CostEntryExpectedLivesBetween(@from, @to);
				@says           = ledger.DescribeEachCostEntryBetween(@from, @to);
			", TheColumnsOfACostLedgerRecord, APeriodOfInstants(from, to));

			return CostLedgerRecord.Of(
				columns[0], columns[1], columns[2], columns[3], columns[4], columns[5]);
		}

		/// <summary>
		/// Si se puede decir cuanto costo la distancia de ese periodo: hace falta un tramo de uso que
		/// medir Y algun gasto. TOTAL, y es la mitad de MEDIBILIDAD de la respuesta de abajo -- la
		/// misma pareja que el ritmo de uso ya tiene -- y no una guarda delante de un rechazo.
		/// </summary>
		public bool CanCostPerDistanceBeSaid(DateTime from, DateTime to)
		{
			return ATruth(@"
				@can = schedule.CanARateOfUseBeSaid(@from, @to) && ledger.HasAnyCostBetween(@from, @to);
			", "can", APeriodOfInstants(from, to));
		}

		/// <summary>
		/// Cuanto costo la distancia de ese periodo, moneda por moneda y con las dos unidades dichas --
		/// el signo de cada moneda y la unidad de la distancia -- o la constancia con nombre de que no
		/// hay tramo de uso por el que dividir. TOTAL.
		/// <para>
		/// Cruza DOS globales de raiz en un solo script: el tramo lo dice la agenda y el dinero el libro
		/// de costos. Ni la distancia ni el ancla se le piden a quien pregunta, y ninguna de las dos
		/// sale por ninguna lectura como numero.
		/// </para>
		/// </summary>
		public string DescribeCostPerDistanceBetween(DateTime from, DateTime to)
		{
			return AText(@"
				if (schedule.CanARateOfUseBeSaid(@from, @to)) {
					@clause = ledger.DescribeCostPerDistance(@from, @to,
						schedule.FirstReadingInstantBetween(@from, @to),
						schedule.DistanceTravelledBetween(@from, @to),
						schedule.UnitOfTravelBetween(@from, @to));
				} else {
					@clause = ledger.DescribeCostPerDistanceWithNoSpan(@from, @to,
						schedule.DescribeRateOfUse(@from, @to));
				}
			", "clause", APeriodOfInstants(from, to));
		}

		/// <summary>
		/// Cuantas entradas se incurrieron adentro de ese periodo. TOTAL, y es la cuenta de las FILAS
		/// del registro de arriba: de las entradas sale la cuenta y de la cuenta no salen las
		/// entradas.
		/// </summary>
		public int CostEntryCountBetween(DateTime from, DateTime to)
		{
			return ACount(
				"@count = ledger.CostEntryCountBetween(@from, @to);", "count",
				APeriodOfInstants(from, to));
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

		/// <summary>
		/// Cuantas operaciones no tienen nada pendiente. Es un conteo que el dominio compone, no la
		/// mitad de ninguna respuesta: la pantalla lo imprime al lado del siguiente y no los divide.
		/// </summary>
		public int CountUpToDate()
		{
			return ACount("@upToDate = schedule.CountUpToDate();", "upToDate", p => { });
		}

		/// <summary>
		/// Cuantas operaciones tiene agendadas este vehiculo. Va con la anterior: son las dos que una
		/// pantalla pondria una sobre la otra, y las dos las compone el dominio.
		/// </summary>
		public int CountScheduled()
		{
			return ACount("@scheduled = schedule.CountScheduled();", "scheduled", p => { });
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

		/// <summary>
		/// Cada cuanto publica el fabricante que se hace esa operacion, con su mitad publicada nada
		/// mas. No pregunta por ningun vehiculo, asi que la contesta cualquiera que no tenga los
		/// hechos de ninguno. TOTAL.
		/// </summary>
		public string DescribePublishedServiceInterval(string kind)
		{
			return AText(@"
				@clause = specification.DescribePublishedServiceInterval(@kind);
			", "clause", p => {
				p["kind", typeof(Enum)] = WireName.OperationKind(kind);
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

		/// <summary>
		/// Cuanto cuesta esa operacion en ese hito, con su moneda, su vigencia y la revision, y SIN
		/// decir si rige: eso necesita un instante, y quien no tenga ninguno que aportar igual puede
		/// citar lo que el fabricante publico. TOTAL.
		/// </summary>
		public string DescribePublishedServicePrice(
			string kind, int milestoneDistance, string milestoneUnit)
		{
			return AText(@"
				@clause = specification.DescribePublishedServicePrice(
					@kind, @milestoneDistance, @milestoneUnit);
			", "clause", p => {
				p["kind",              typeof(Enum)] = WireName.OperationKind(kind);
				p["milestoneDistance", typeof(int)]  = milestoneDistance;
				p["milestoneUnit",     typeof(Enum)] =
					WireName.DistanceUnit(milestoneUnit, nameof(milestoneUnit));
			});
		}

		/// <summary>
		/// Si alguna revision sellada publico algun precio. TOTAL, y es la pregunta que hay que hacer
		/// antes de leer las filas. No toma instante porque sellada es un ESTADO y no un instante.
		/// </summary>
		public bool HasAnyPublishedServicePrice()
		{
			return ATruth("@any = specification.HasAnyPublishedServicePrice();", "any", p => { });
		}

		/// <summary>
		/// Cuantos precios publicados hay. TOTAL, y es la cuenta de las FILAS del registro de abajo:
		/// las dos salen de la misma enumeracion en cada lectura, asi que no pueden discrepar.
		/// </summary>
		public int PublishedServicePriceCount()
		{
			return ACount("@count = specification.PublishedServicePriceCount();", "count", p => { });
		}

		/// <summary>
		/// Los precios publicados como filas: de que operacion, a que hito, la frase con que el
		/// dominio lo dice al instante preguntado y de que revision salio, en columnas paralelas y de
		/// la revision mas nueva a la mas vieja. TOTAL, y contesta CERO filas donde no hay ninguno.
		/// Ninguna fila pide un hito a quien pregunta. El instante mueve la FRASE y nunca el juego de
		/// filas.
		/// </summary>
		public PublishedPriceRecord PublishedServicePricesAsOf(DateTime asOf)
		{
			string[][] columns = TheTextColumns(@"
				@operation = specification.PublishedPriceOperationsAsOf(@asOf);
				@milestone = specification.PublishedPriceMilestonesAsOf(@asOf);
				@says      = specification.DescribeEachPublishedPriceAsOf(@asOf);
				@revision  = specification.PublishedPriceRevisionsAsOf(@asOf);
			", TheColumnsOfAPublishedPriceRecord, p => {
				p["asOf", typeof(DateTime)] = InUtc(asOf);
			});

			return PublishedPriceRecord.Of(columns[0], columns[1], columns[2], columns[3]);
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

		/// <summary>
		/// La regla publicada sobre servicio en otro taller, con su mitad publicada nada mas. No
		/// pregunta por ningun vehiculo, asi que la contesta cualquiera que no tenga los conteos de
		/// ninguno. TOTAL.
		/// </summary>
		public string DescribePublishedServicingElsewhereRule()
		{
			return AText(
				"@clause = specification.DescribePublishedServicingElsewhereRule();",
				"clause", p => { });
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

		/// <summary>
		/// Si la agencia publico un sobre de garantia para este vehiculo. TOTAL, y es la que hay que
		/// hacer antes de pedir el instante de publicacion.
		/// </summary>
		public bool IsWarrantyEnvelopePublished()
		{
			return ATruth(
				"@published = profile.IsWarrantyEnvelopePublished();", "published", p => { });
		}

		/// <summary>
		/// El sobre publicado en palabras -- sus dos limites, su unidad y cuando se publico -- SIN
		/// ningun hecho de ningun vehiculo. TOTAL, tambien sin sobre: es la que puede citar quien no
		/// tiene el odometro de nadie, y no tenerla haria que faltar un dato personal volviera
		/// incitable lo que la agencia publico.
		/// </summary>
		public string DescribePublishedWarrantyEnvelope()
		{
			return AText(
				"@clause = profile.DescribePublishedWarrantyEnvelope();", "clause", p => { });
		}

		/// <summary>
		/// Cuando la agencia publico el sobre que rige. Lectura GUARDADA: sin sobre no hay instante
		/// de publicacion, y sale en UTC igual que entro.
		/// </summary>
		public DateTime ReadEnvelopePublishedAt()
		{
			if (!IsWarrantyEnvelopePublished())
				throw new InvalidOperationException(
					"La agencia no publico ningun sobre de garantia para este vehiculo: pregunte "
					+ "IsWarrantyEnvelopePublished antes. Un DateTime.MinValue aca seria un centinela "
					+ "magico -- un instante que parece un instante y no lo es.");

			return AnInstant("@at = profile.ReadEnvelopePublishedAt();", "at", p => { });
		}

		/// <summary>
		/// Si la agencia registro la entrega de este vehiculo. TOTAL, y es la que hay que hacer antes
		/// de pedir cualquiera de los tres datos de la entrega.
		/// </summary>
		public bool HasDeliveryBeenRecorded()
		{
			return ATruth("@recorded = profile.HasDeliveryBeenRecorded();", "recorded", p => { });
		}

		/// <summary>
		/// Cuando se entrego el carro. Lectura GUARDADA por lo mismo, y sale en UTC igual que entro.
		/// </summary>
		public DateTime ReadDeliveredAt()
		{
			ADeliveryHasToHaveBeenRecorded(
				"Un DateTime.MinValue aca seria un centinela magico -- un instante que parece un "
				+ "instante y no lo es.");

			return AnInstant("@at = profile.ReadDeliveredAt();", "at", p => { });
		}

		/// <summary>
		/// El odometro del dia de la entrega, que es el ancla desde donde se cuenta lo recorrido bajo
		/// garantia. Lectura GUARDADA.
		/// </summary>
		public int ReadDeliveryReading()
		{
			ADeliveryHasToHaveBeenRecorded(
				"Un cero aca seria una medicion que nadie tomo, y contra el limite de distancia se "
				+ "leeria como un carro entregado nuevo.");

			return ACount("@reading = profile.ReadDeliveryReading();", "reading", p => { });
		}

		/// <summary>
		/// La unidad en que se leyo ese odometro, dicha como la lee una persona. Lectura GUARDADA.
		/// </summary>
		public string ReadDeliveryUnit()
		{
			ADeliveryHasToHaveBeenRecorded(
				"Una unidad vacia aca no nombra ninguna de las dos que hay, y aca no se convierte "
				+ "ninguna en la otra.");

			return AText("@unit = profile.ReadDeliveryUnit();", "unit", p => { });
		}

		/// <summary>
		/// Si hay una lectura del odometro que sirva para ese instante. TOTAL: un vehiculo cuyo feed
		/// no trajo nada contesta que no, y esa ausencia es una respuesta legal. Es la que hay que
		/// hacer antes de las tres de abajo, y con EL MISMO instante.
		/// </summary>
		public bool HasAnOdometerReading(DateTime asOf)
		{
			return ATruth("@has = schedule.HasAnOdometerReading(@asOf);", "has", p => {
				p["asOf", typeof(DateTime)] = InUtc(asOf);
			});
		}

		/// <summary>
		/// El odometro con que esta agenda mediria a ese instante, que es el MISMO numero que Evaluate
		/// usa al mismo instante. Lectura GUARDADA. Una lectura observada despues del instante
		/// preguntado no cuenta para ninguno de los dos.
		/// </summary>
		public int ReadOdometer(DateTime asOf)
		{
			AnOdometerReadingHasToExistAt(
				asOf,
				"Un cero aca seria una medicion que nadie tomo, y contra un limite de distancia se "
				+ "leeria como un vehiculo que no ha recorrido nada.");

			return ACount("@value = schedule.ReadOdometer(@asOf);", "value", p => {
				p["asOf", typeof(DateTime)] = InUtc(asOf);
			});
		}

		/// <summary>
		/// La unidad de esa lectura, dicha como la lee una persona. Lectura GUARDADA.
		/// </summary>
		public string ReadOdometerUnit(DateTime asOf)
		{
			AnOdometerReadingHasToExistAt(
				asOf,
				"Una unidad vacia aca no nombra ninguna de las dos que hay, y es la que decide si el "
				+ "limite del sobre y el odometro se pueden comparar.");

			return AText("@unit = schedule.ReadOdometerUnit(@asOf);", "unit", p => {
				p["asOf", typeof(DateTime)] = InUtc(asOf);
			});
		}

		/// <summary>
		/// Cuando se OBSERVO esa lectura, que no es el instante preguntado. Lectura GUARDADA: sin
		/// esto, una frase compuesta con una lectura de marzo se leeria como tomada hoy.
		/// </summary>
		public DateTime ReadOdometerObservedAt(DateTime asOf)
		{
			AnOdometerReadingHasToExistAt(
				asOf,
				"Un DateTime.MinValue aca seria un centinela magico -- un instante que parece un "
				+ "instante y no lo es.");

			return AnInstant("@at = schedule.ReadOdometerObservedAt(@asOf);", "at", p => {
				p["asOf", typeof(DateTime)] = InUtc(asOf);
			});
		}

		/// <summary>
		/// Si el concesionario le publico una placa a este vehiculo al instante preguntado. TOTAL, y
		/// es la que hay que hacer antes de pedir cualquiera de los dos textos.
		/// </summary>
		public bool IsAPlatePublished(DateTime asOf)
		{
			return ATruth("@published = profile.IsAPlatePublished(@asOf);", "published", p => {
				p["asOf", typeof(DateTime)] = InUtc(asOf);
			});
		}

		/// <summary>
		/// La placa dicha entera -- cual es y desde cuando rige -- o la constancia con nombre de que no
		/// hay ninguna. TOTAL, tambien sin placa: la frase es del dominio y la caratula la imprime.
		/// </summary>
		public string DescribePlate(DateTime asOf)
		{
			return AText("@says = profile.DescribePlate(@asOf);", "says", p => {
				p["asOf", typeof(DateTime)] = InUtc(asOf);
			});
		}

		/// <summary>
		/// El texto de la placa tal como la publico el concesionario. Lectura GUARDADA: una cadena
		/// vacia parece una respuesta, y el Vin puesto en su lugar se cruzaria contra los registros de
		/// otro sistema y no coincidiria con nada, en silencio.
		/// </summary>
		public string ReadPlateAsPublished(DateTime asOf)
		{
			APlateHasToBePublishedAt(
				asOf,
				"Una cadena vacia aca parece una respuesta y el Vin puesto en su lugar no coincidiria "
				+ "con nada.");

			return AText("@text = profile.ReadPlateAsPublished(@asOf);", "text", p => {
				p["asOf", typeof(DateTime)] = InUtc(asOf);
			});
		}

		/// <summary>
		/// La llave normalizada con la que este vehiculo cruza contra otro sistema. Lectura GUARDADA
		/// por lo mismo, y sale YA normalizada: ninguna caratula la deriva del texto publicado.
		/// </summary>
		public string ReadPlateJoinKey(DateTime asOf)
		{
			APlateHasToBePublishedAt(
				asOf,
				"Una llave vacia coincidiria contra el otro sistema con cualquier cosa o con nada, sin "
				+ "decir cual de las dos.");

			return AText("@key = profile.ReadPlateJoinKey(@asOf);", "key", p => {
				p["asOf", typeof(DateTime)] = InUtc(asOf);
			});
		}

		/// <summary>
		/// Las asignaciones de placa como filas: el texto publicado, la llave, desde cuando rige cada
		/// una y cual es la vigente, en columnas paralelas y en orden de la linea de tiempo. TOTAL, y
		/// contesta CERO filas donde no hay ninguna.
		/// </summary>
		public PlateAssignmentRecord PlateAssignments()
		{
			string[][] columns = TheTextColumns(@"
				@asPublished = profile.EachPlateAsPublished();
				@joinKey     = profile.EachPlateJoinKey();
				@assignedAt  = profile.EachPlateAssignedAt();
				@isCurrent   = profile.EachPlateIsCurrent();
			", TheColumnsOfAPlateAssignmentRecord, p => { });

			return PlateAssignmentRecord.Of(columns[0], columns[1], columns[2], columns[3]);
		}

		/// <summary>
		/// Si se puede decir el ritmo de uso de ese periodo. TOTAL, y es la que hay que hacer antes
		/// de mostrar la frase donde no cabe una suspension. Las cinco situaciones contestan: ninguna
		/// de las otras cinco lecturas de la serie rechaza por esta.
		/// </summary>
		public bool CanARateOfUseBeSaid(DateTime from, DateTime to)
		{
			return ATruth(
				"@can = schedule.CanARateOfUseBeSaid(@from, @to);", "can",
				APeriodOfInstants(from, to));
		}

		/// <summary>
		/// En cual de las cinco situaciones esta el ritmo de ese periodo, por el NOMBRE del miembro.
		/// TOTAL, y ninguna de las cinco quiere decir que no se sabe.
		/// </summary>
		public string ReadRateOfUseStanding(DateTime from, DateTime to)
		{
			return AText(
				"@standing = schedule.ReadRateOfUseStanding(@from, @to);", "standing",
				APeriodOfInstants(from, to));
		}

		/// <summary>
		/// El ritmo de uso de ese periodo en una frase: los dos extremos con sus instantes, la
		/// distancia, el tiempo y la distancia por dia; o la razon con nombre que impide decirlo.
		/// TOTAL. Los dos extremos no salen nunca sueltos: salen aca, y esta frase ya trae el ritmo.
		/// </summary>
		public string DescribeRateOfUse(DateTime from, DateTime to)
		{
			return AText(
				"@says = schedule.DescribeRateOfUse(@from, @to);", "says",
				APeriodOfInstants(from, to));
		}

		/// <summary>
		/// Los dos periodos comparados en una sola frase: cada uno con su propio ritmo, y el cambio
		/// del PRIMERO respecto al SEGUNDO dicho adentro. TOTAL. El cambio no sale por ninguna
		/// lectura como numero.
		/// </summary>
		public string DescribeTwoPeriodsOfUseCompared(
			DateTime from1, DateTime to1, DateTime from2, DateTime to2)
		{
			return AText(@"
				@says = schedule.DescribeTwoPeriodsOfUseCompared(@from1, @to1, @from2, @to2);
			", "says", TwoPeriodsOfInstants(from1, to1, from2, to2));
		}

		/// <summary>
		/// Las lecturas del periodo como filas: cuando se observo cada una, que valor leyo, en que
		/// unidad, de que fuente vino y la frase con que el dominio la dice, en columnas paralelas y
		/// en orden de observacion. TOTAL, y contesta CERO filas donde no hay ninguna.
		/// </summary>
		public OdometerSeriesRecord ReadingsBetween(DateTime from, DateTime to)
		{
			using (RentedParameter rented = app.Actor.RentedParameters())
			{
				app.Actor.Using(@"
					@observedAt = schedule.ReadingInstantsBetween(@from, @to);
					@value      = schedule.ReadingValuesBetween(@from, @to);
					@unit       = schedule.ReadingUnitsBetween(@from, @to);
					@source     = schedule.ReadingSourcesBetween(@from, @to);
					@says       = schedule.DescribeEachReadingBetween(@from, @to);
				")
				.WithParameters(rented, p => {
					p[Parameter.Out, "observedAt", typeof(string[])] = default;
					p[Parameter.Out, "value",      typeof(int[])]    = default;
					p[Parameter.Out, "unit",       typeof(string[])] = default;
					p[Parameter.Out, "source",     typeof(string[])] = default;
					p[Parameter.Out, "says",       typeof(string[])] = default;

					APeriodOfInstants(from, to)(p);
				})
				.PerformQuery();

				return OdometerSeriesRecord.Of(
					rented["observedAt"].GetValue<string[]>(),
					rented["value"].GetValue<int[]>(),
					rented["unit"].GetValue<string[]>(),
					rented["source"].GetValue<string[]>(),
					rented["says"].GetValue<string[]>());
			}
		}

		/// <summary>
		/// Cuantas lecturas del odometro cayeron dentro de ese periodo. TOTAL, y es la cuenta de las
		/// FILAS del registro de arriba: de las lecturas sale la cuenta y de la cuenta no salen las
		/// lecturas.
		/// </summary>
		public int ReadingCountBetween(DateTime from, DateTime to)
		{
			return ACount(
				"@count = schedule.ReadingCountBetween(@from, @to);", "count",
				APeriodOfInstants(from, to));
		}

		/// <summary>
		/// Si se puede decir cuanta distancia le queda a esa operacion a ese instante. TOTAL, y es la
		/// pregunta que hay que hacer antes de las dos lecturas guardadas de abajo.
		/// </summary>
		public bool CanDistanceRemainingBeSaid(string kind, DateTime asOf)
		{
			return ATruthAboutAnOperation(
				"@can = schedule.CanDistanceRemainingBeSaid(@kind, @asOf);", "can", kind, asOf);
		}

		/// <summary>
		/// La distancia que le queda, compuesta ADENTRO contra el limite ya escalado. Lectura
		/// GUARDADA. Pasado el limite es el EXCESO y nunca un negativo: cual de las dos lo dice
		/// <see cref="ReadDistanceRemainderStanding"/>, y en que unidad
		/// <see cref="ReadDistanceRemainingUnit"/>.
		/// </summary>
		public int ReadDistanceRemaining(string kind, DateTime asOf)
		{
			TheDistanceRemainderHasToBeSayable(kind, asOf);

			return ACountAboutAnOperation(
				"@left = schedule.ReadDistanceRemaining(@kind, @asOf);", "left", kind, asOf);
		}

		/// <summary>
		/// La unidad de ese monto, por su NOMBRE y nunca por su codigo. Lectura GUARDADA: una unidad
		/// para una mitad que nadie midio se lee como que hubo medicion.
		/// </summary>
		public string ReadDistanceRemainingUnit(string kind, DateTime asOf)
		{
			TheDistanceRemainderHasToBeSayable(kind, asOf);

			return ATextAboutAnOperation(
				"@unit = schedule.ReadDistanceRemainingUnit(@kind, @asOf);", "unit", kind, asOf);
		}

		/// <summary>
		/// En cual de las siete situaciones esta la mitad de distancia, por su nombre. TOTAL, y es lo
		/// que separa "no se mide asi" de "falta la lectura", que un booleano juntaria.
		/// </summary>
		public string ReadDistanceRemainderStanding(string kind, DateTime asOf)
		{
			return ATextAboutAnOperation(
				"@standing = schedule.ReadDistanceRemainderStanding(@kind, @asOf);", "standing",
				kind, asOf);
		}

		/// <summary>
		/// Lo que le queda de distancia, en una frase. TOTAL: las siete situaciones dicen algo.
		/// </summary>
		public string DescribeDistanceRemaining(string kind, DateTime asOf)
		{
			return ATextAboutAnOperation(
				"@clause = schedule.DescribeDistanceRemaining(@kind, @asOf);", "clause", kind, asOf);
		}

		/// <summary>
		/// Si se puede decir cuantos dias le quedan a esa operacion a ese instante. TOTAL, y es la
		/// pregunta que hay que hacer antes de la lectura guardada de abajo.
		/// </summary>
		public bool CanDaysRemainingBeSaid(string kind, DateTime asOf)
		{
			return ATruthAboutAnOperation(
				"@can = schedule.CanDaysRemainingBeSaid(@kind, @asOf);", "can", kind, asOf);
		}

		/// <summary>
		/// Los dias que le quedan del plazo ya escalado. Lectura GUARDADA, y pasado el plazo es el
		/// exceso igual que en la distancia. Su unidad esta en el nombre del miembro: el plazo se mide
		/// en una sola, a diferencia de la distancia, que tiene dos.
		/// </summary>
		public int ReadDaysRemaining(string kind, DateTime asOf)
		{
			TheTimeRemainderHasToBeSayable(kind, asOf);

			return ACountAboutAnOperation(
				"@left = schedule.ReadDaysRemaining(@kind, @asOf);", "left", kind, asOf);
		}

		/// <summary>
		/// En cual de las siete situaciones esta la mitad de tiempo, por su nombre. TOTAL.
		/// </summary>
		public string ReadTimeRemainderStanding(string kind, DateTime asOf)
		{
			return ATextAboutAnOperation(
				"@standing = schedule.ReadTimeRemainderStanding(@kind, @asOf);", "standing",
				kind, asOf);
		}

		/// <summary>Lo que le queda de plazo, en una frase. TOTAL.</summary>
		public string DescribeDaysRemaining(string kind, DateTime asOf)
		{
			return ATextAboutAnOperation(
				"@clause = schedule.DescribeDaysRemaining(@kind, @asOf);", "clause", kind, asOf);
		}

		/// <summary>
		/// La cuenta atras entera: LAS DOS dimensiones y las dos situaciones en una frase, sin elegir
		/// ninguna. TOTAL, y la compone el dominio: elegir la mas cercana dejaria el numero de la
		/// pantalla discrepando del motivo del estado para quien lea los dos juntos.
		/// </summary>
		public string DescribeCountdown(string kind, DateTime asOf)
		{
			return ATextAboutAnOperation(
				"@clause = schedule.DescribeCountdown(@kind, @asOf);", "clause", kind, asOf);
		}

		/// <summary>
		/// Si esa clase tiene alguna completacion alcanzable en su cadena. TOTAL, y es la pregunta que
		/// hay que hacer antes de leer las filas: las dos ausencias contestan que no por razones
		/// distintas que <see cref="DescribeCompletions"/> dice.
		/// </summary>
		public bool HasAnyCompletion(string kind)
		{
			return ATruth("@any = schedule.HasAnyCompletion(@kind);", "any", TheKindAsked(kind));
		}

		/// <summary>
		/// Cuantas completaciones hay en la cadena de esa clase. TOTAL, y es la cuenta de las FILAS
		/// del registro de abajo: las dos se caminan en cada lectura y no se guardan.
		/// </summary>
		public int CompletionCount(string kind)
		{
			return ACount("@count = schedule.CompletionCount(@kind);", "count", TheKindAsked(kind));
		}

		/// <summary>
		/// El historial de esa clase en una frase. TOTAL: nunca completada y no agendada son dos
		/// ausencias distintas y cada una dice la suya.
		/// </summary>
		public string DescribeCompletions(string kind)
		{
			return AText(
				"@clause = schedule.DescribeCompletions(@kind);", "clause", TheKindAsked(kind));
		}

		/// <summary>
		/// Las completaciones de esa clase como filas: cuando se hizo cada una, con que lectura del
		/// odometro o la constancia de que no hay, y la frase con que el dominio la dice, en columnas
		/// paralelas y de la mas reciente a la mas vieja. TOTAL, y contesta CERO filas donde no hay
		/// ninguna. La clase la puso quien pregunta, asi que no viene como columna.
		/// </summary>
		public CompletionRecord CompletionsOf(string kind)
		{
			string[][] columns = TheTextColumns(@"
				@completedAt = schedule.CompletionInstantsOf(@kind);
				@reading     = schedule.CompletionReadingsOf(@kind);
				@says        = schedule.DescribeEachCompletionOf(@kind);
			", TheColumnsOfACompletionRecord, TheKindAsked(kind));

			return CompletionRecord.Of(columns[0], columns[1], columns[2]);
		}

		/// <summary>
		/// El historial entero como filas: de que clase es cada completacion, cuando se hizo, con que
		/// lectura o la constancia de que no hay, y su frase, en columnas paralelas y de la mas
		/// reciente a la mas vieja entre todas las clases. TOTAL, y no toma ningun instante: cada fila
		/// trae el suyo y la cadena es la que es.
		/// </summary>
		public EveryKindCompletionRecord CompletionsOfEveryKind()
		{
			string[][] columns = TheTextColumns(@"
				@kind        = schedule.CompletionKindsOnRecord();
				@completedAt = schedule.CompletionInstantsOnRecord();
				@reading     = schedule.CompletionReadingsOnRecord();
				@says        = schedule.DescribeEachCompletionOnRecord();
			", TheColumnsOfAnEveryKindCompletionRecord, p => { });

			return EveryKindCompletionRecord.Of(columns[0], columns[1], columns[2], columns[3]);
		}

		/// <summary>
		/// Si la pregunta del sobre se puede decidir con lo que el registro tiene a ese instante.
		/// TOTAL, y es la que hay que hacer antes de leer donde esta parado el vehiculo. El odometro
		/// sale del registro y no se le pide a quien pregunta; sin lectura, la mitad de tiempo se
		/// mide igual.
		/// </summary>
		public bool CanTheEnvelopeBeEvaluated(DateTime asOf)
		{
			return ATruth(@"
				if (schedule.HasAnOdometerReading(@asOf)) {
					@can = profile.CanTheEnvelopeBeEvaluated(
						schedule.ReadOdometer(@asOf), schedule.OdometerUnit(@asOf),
						schedule.ReadOdometerObservedAt(@asOf), @asOf);
				} else {
					@can = profile.CanTheEnvelopeBeEvaluatedWithNoOdometer(@asOf);
				}
			", "can", p => {
				p["asOf", typeof(DateTime)] = InUtc(asOf);
			});
		}

		/// <summary>
		/// Donde esta parado este vehiculo contra su sobre, por el nombre de la respuesta. Lectura
		/// GUARDADA: hay que preguntar antes si se puede evaluar, con el mismo instante.
		/// </summary>
		public string ReadEnvelopeStanding(DateTime asOf)
		{
			if (!CanTheEnvelopeBeEvaluated(asOf))
				throw new InvalidOperationException(
					"No se pudo decidir donde esta parado este vehiculo contra su sobre a ese "
					+ "instante: pregunte CanTheEnvelopeBeEvaluated con el mismo instante antes. "
					+ "Ninguno de los nombres posibles quiere decir 'no se pudo medir', asi que "
					+ "devolver uno aca lo convertiria en una medicion que nadie hizo. La frase de "
					+ "DescribeWarrantyEnvelope si contesta, y dice con palabras que hecho falta.");

			return AText(@"
				if (schedule.HasAnOdometerReading(@asOf)) {
					@standing = profile.ReadEnvelopeStanding(
						schedule.ReadOdometer(@asOf), schedule.OdometerUnit(@asOf),
						schedule.ReadOdometerObservedAt(@asOf), @asOf);
				} else {
					@standing = profile.ReadEnvelopeStandingWithNoOdometer(@asOf);
				}
			", "standing", p => {
				p["asOf", typeof(DateTime)] = InUtc(asOf);
			});
		}

		/// <summary>
		/// La respuesta del sobre en una frase: donde esta parado, sus dos mitades, cual llega
		/// primero, contra que odometro se midio y cuando se observo -- o el motivo con nombre por
		/// el que no se pudo decidir. TOTAL, y es la lectura primaria: la frase la compone el
		/// dominio, no quien la lee.
		/// </summary>
		public string DescribeWarrantyEnvelope(DateTime asOf)
		{
			return AText(@"
				if (schedule.HasAnOdometerReading(@asOf)) {
					@clause = profile.DescribeWarrantyEnvelope(
						schedule.ReadOdometer(@asOf), schedule.OdometerUnit(@asOf),
						schedule.ReadOdometerObservedAt(@asOf), @asOf);
				} else {
					@clause = profile.DescribeWarrantyEnvelopeWithNoOdometer(@asOf);
				}
			", "clause", p => {
				p["asOf", typeof(DateTime)] = InUtc(asOf);
			});
		}

		/// <summary>
		/// Si la cobertura de ese sistema se puede decidir para ESTE vehiculo a ese instante. TOTAL:
		/// da que no cuando al registro le falta alguno de los tres hechos, y tambien cuando el
		/// limite que rige y el odometro llegaron en unidades distintas.
		/// </summary>
		public bool CanCoverageBeEvaluatedForThisVehicle(string systemCode, DateTime asOf)
		{
			if (!TheRecordHoldsWhatTheCoverageQuestionNeeds(asOf)) return false;

			return ATruth(@"
				@can = specification.CanCoverageBeEvaluated(
					@systemCode, profile.EnvelopeMonths(), profile.EnvelopeDistance(),
					profile.EnvelopeUnit(), profile.ReadDeliveredAt(),
					profile.ReadDeliveryReading(), schedule.ReadOdometer(@asOf),
					schedule.OdometerUnit(@asOf), @asOf);
			", "can", TheSystemAndTheInstant(systemCode, asOf));
		}

		/// <summary>
		/// Donde esta parado ese sistema para ESTE vehiculo, por el nombre de la respuesta. Lectura
		/// GUARDADA: hay que preguntar antes si se puede evaluar, con el mismo sistema y el mismo
		/// instante.
		/// </summary>
		public string ReadCoverageStandingForThisVehicle(string systemCode, DateTime asOf)
		{
			if (!CanCoverageBeEvaluatedForThisVehicle(systemCode, asOf))
				throw new InvalidOperationException(
					"La cobertura de ese sistema no se pudo evaluar para este vehiculo a ese "
					+ "instante: pregunte CanCoverageBeEvaluatedForThisVehicle con el mismo sistema "
					+ "y el mismo instante antes. Ninguna de las respuestas posibles significa 'no "
					+ "se pudo medir', asi que devolver una aca la convertiria en un hecho medido.");

			return AText(@"
				@standing = specification.ReadCoverageStanding(
					@systemCode, profile.EnvelopeMonths(), profile.EnvelopeDistance(),
					profile.EnvelopeUnit(), profile.ReadDeliveredAt(),
					profile.ReadDeliveryReading(), schedule.ReadOdometer(@asOf),
					schedule.OdometerUnit(@asOf), @asOf);
			", "standing", TheSystemAndTheInstant(systemCode, asOf));
		}

		/// <summary>
		/// La respuesta de cobertura de ese sistema para ESTE vehiculo, en una frase. Contesta
		/// siempre que el registro tenga los tres hechos -- tambien lo que no se pudo evaluar, con
		/// palabras -- y RECHAZA cuando alguno falta, porque la frase que los nombra la compone el
		/// catalogo del fabricante y el catalogo no sabe que le falta a un vehiculo.
		/// </summary>
		public string DescribeCoverageForThisVehicle(string systemCode, DateTime asOf)
		{
			TheRecordHasToHoldWhatTheCoverageQuestionNeeds(asOf);

			return AText(@"
				@clause = specification.DescribeCoverage(
					@systemCode, profile.EnvelopeMonths(), profile.EnvelopeDistance(),
					profile.EnvelopeUnit(), profile.ReadDeliveredAt(),
					profile.ReadDeliveryReading(), schedule.ReadOdometer(@asOf),
					schedule.OdometerUnit(@asOf), @asOf);
			", "clause", TheSystemAndTheInstant(systemCode, asOf));
		}

		/// <summary>
		/// Si ESTE vehiculo estaba adentro de alguna sucursal en ese instante. TOTAL, y es una de las
		/// dos unicas preguntas que alcanzan una estadia todavia abierta.
		/// </summary>
		public bool IsInsideTheShopAt(DateTime asOf)
		{
			return ATruth(
				"@inside = stays.IsInsideTheShopAt(@asOf);", "inside",
				p => { p["asOf", typeof(DateTime)] = InUtc(asOf); });
		}

		/// <summary>
		/// La estadia que cubre ese instante, dicha entera, o la constancia de que ninguna lo cubre.
		/// TOTAL, y es la otra pregunta que alcanza una estadia todavia abierta.
		/// </summary>
		public string DescribeTheStayAt(DateTime asOf)
		{
			return AText(
				"@says = stays.DescribeTheStayAt(@asOf);", "says",
				p => { p["asOf", typeof(DateTime)] = InUtc(asOf); });
		}

		/// <summary>
		/// Si alguna estadia con dias registrados se cruza con ese periodo, dicho en dias. TOTAL.
		/// </summary>
		public bool HasAnyStayBetween(string fromDay, string toDay)
		{
			return ATruth(
				"@any = stays.HasAnyStayBetween(@fromDay, @toDay);", "any",
				ADayRange(fromDay, toDay));
		}

		/// <summary>
		/// Los dias del periodo en que el vehiculo estuvo en el taller, ascendentes y sin repetidos.
		/// TOTAL: un periodo sin ninguno contesta la lista vacia, y quien no pueda hacer nada con una
		/// lista vacia tiene la frase de DescribeDaysInTheShopBetween, que nombra el periodo.
		/// </summary>
		public string[] DaysInTheShopBetween(string fromDay, string toDay)
		{
			return ATextList(
				"@days = stays.DaysInTheShopBetween(@fromDay, @toDay);", "days",
				ADayRange(fromDay, toDay));
		}

		/// <summary>
		/// Cuantos dias estuvo en el taller en ese periodo. TOTAL, y lo cuenta el dominio sobre los
		/// dias: de los dias sale la cuenta y de la cuenta no salen los dias.
		/// </summary>
		public int DaysInTheShopCountBetween(string fromDay, string toDay)
		{
			return ACount(
				"@count = stays.DaysInTheShopCountBetween(@fromDay, @toDay);", "count",
				ADayRange(fromDay, toDay));
		}

		/// <summary>
		/// El periodo en una frase: los dias en que estuvo adentro y en cuantas estadias, o la
		/// constancia de que no hay ninguna con dias registrados. TOTAL.
		/// </summary>
		public string DescribeDaysInTheShopBetween(string fromDay, string toDay)
		{
			return AText(
				"@says = stays.DescribeDaysInTheShopBetween(@fromDay, @toDay);", "says",
				ADayRange(fromDay, toDay));
		}

		/// <summary>
		/// Cuantas estadias se cruzan con ese periodo. TOTAL, y es la cuenta de las FILAS del registro
		/// de abajo.
		/// </summary>
		public int StayCountBetween(string fromDay, string toDay)
		{
			return ACount(
				"@count = stays.StayCountBetween(@fromDay, @toDay);", "count",
				ADayRange(fromDay, toDay));
		}

		/// <summary>
		/// Las estadias del periodo como filas: donde, por que, cuando entro, que dias cubrio y la
		/// frase con que el dominio la dice, en columnas paralelas y en orden de entrada. TOTAL, y
		/// contesta CERO filas donde no hay ninguna. Un periodo dicho en dias no alcanza una estadia
		/// todavia abierta: esa se pregunta por instante.
		/// </summary>
		public ShopStayRecord StaysBetween(string fromDay, string toDay)
		{
			string[][] columns = TheTextColumns(@"
				@branch      = stays.StayBranchesBetween(@fromDay, @toDay);
				@subject     = stays.StaySubjectsBetween(@fromDay, @toDay);
				@enteredAt   = stays.StayEntryInstantsBetween(@fromDay, @toDay);
				@coveredDays = stays.StayCoveredDaysBetween(@fromDay, @toDay);
				@says        = stays.DescribeEachStayBetween(@fromDay, @toDay);
			", TheColumnsOfAStayRecord, ADayRange(fromDay, toDay));

			return ShopStayRecord.Of(columns[0], columns[1], columns[2], columns[3], columns[4]);
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

				if (message is VehicleDelivered delivery) return ApplyVehicleDelivered(delivery);
				if (message is WarrantyEnvelopePublished envelope)
					return ApplyWarrantyEnvelopePublished(envelope);

				if (message is ShopEntryRecorded entry) return ApplyShopEntryRecorded(entry);
				if (message is ShopCollectionRecorded collection)
					return ApplyShopCollectionRecorded(collection);
				if (message is ShopStayRecorded stay) return ApplyShopStayRecorded(stay);

				if (message is RecurringCostRecorded recurring)
					return ApplyRecurringCostRecorded(recurring);
				if (message is DurableCostRecorded durable) return ApplyDurableCostRecorded(durable);

				throw new ArgumentException(
					$"Esta app no tiene comando para un mensaje de tipo '{message.Tipo}'. La ruta "
					+ "debio devolver null para descartarlo.", nameof(message));
			}
			catch (Exception refused) when (TheDomainRefusedTheFact(refused))
			{
				return TheNarrativeMovesPast(message, refused.Message);
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
			WireName.BasisAgreesWithItsLimits(
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

		private string ApplyVehicleDelivered(VehicleDelivered message)
		{
			return app.Using(
				scriptForChk: TheEventIsNew,
				@"
					{
						profile.RecordDelivery(@deliveredAt, @reading, @unit);
						logbook.Advance(@event);
					}
				")
			.WithParameters(p => {
				p["event",       typeof(long)]     = message.EventNumber;
				p["deliveredAt", typeof(DateTime)] = InUtc(message.DeliveredAt);
				p["reading",     typeof(int)]      = message.Reading;
				p["unit",        typeof(Enum)]     = WireName.DistanceUnit(message.Unit, "unit");
			})
			.PerformCheckThenCommand();
		}

		private string ApplyWarrantyEnvelopePublished(WarrantyEnvelopePublished message)
		{
			return app.Using(
				scriptForChk: TheEventIsNew,
				@"
					{
						profile.PublishWarrantyEnvelope(@months, @distance, @unit, @publishedAt);
						logbook.Advance(@event);
					}
				")
			.WithParameters(p => {
				p["event",       typeof(long)]     = message.EventNumber;
				p["months",      typeof(int)]      = message.Months;
				p["distance",    typeof(int)]      = message.Distance;
				p["unit",        typeof(Enum)]     = WireName.DistanceUnit(message.Unit, "unit");
				p["publishedAt", typeof(DateTime)] = InUtc(message.PublishedAt);
			})
			.PerformCheckThenCommand();
		}

		/// <summary>
		/// NO PRE-CHEQUEA que el vehiculo estuviera afuera, y no es un olvido. La guarda de la puerta
		/// revienta con InvalidOperationException, que no es la excepcion con que el dominio rechaza,
		/// asi que saldria de Apply sin que nadie la atrape y se llevaria el bloque de refresh entero.
		/// Y escribirla de nuevo aca como un if seria una segunda fuente de verdad de una ley del
		/// dominio, para un hecho que la sucursal ya publico: lo que la frontera descarta es lo que no
		/// se puede DECIR, y una segunda entrada se puede decir.
		/// </summary>
		private string ApplyShopEntryRecorded(ShopEntryRecorded message)
		{
			return app.Using(
				scriptForChk: TheEventIsNew,
				@"
					{
						stays.RecordEntry(@branch, @subject, @enteredAt);
						logbook.Advance(@event);
					}
				")
			.WithParameters(p => {
				p["event",     typeof(long)]     = message.EventNumber;
				p["branch",    typeof(string)]   = message.Branch;
				p["subject",   typeof(string)]   = message.Subject;
				p["enteredAt", typeof(DateTime)] = InUtc(message.EnteredAt);
			})
			.PerformCheckThenCommand();
		}

		/// <summary>
		/// Los dias cruzan como llegaron, sin volver a exigirlos: ADay revienta con ArgumentException
		/// y por este camino eso se llevaria el bloque entero, asi que la forma de cada dia ya se
		/// exigio al interpretar la senal, donde descartarla no cuesta ninguna entrada.
		/// </summary>
		private string ApplyShopCollectionRecorded(ShopCollectionRecorded message)
		{
			return app.Using(
				scriptForChk: TheEventIsNew,
				@"
					{
						stays.RecordCollection(@collectedAt, @days);
						logbook.Advance(@event);
					}
				")
			.WithParameters(p => {
				p["event",       typeof(long)]     = message.EventNumber;
				p["collectedAt", typeof(DateTime)] = InUtc(message.CollectedAt);
				p["days",        typeof(string[])] = message.CoveredDays;
			})
			.PerformCheckThenCommand();
		}

		private string ApplyShopStayRecorded(ShopStayRecorded message)
		{
			return app.Using(
				scriptForChk: TheEventIsNew,
				@"
					{
						stays.RecordStay(@branch, @subject, @enteredAt, @collectedAt, @days);
						logbook.Advance(@event);
					}
				")
			.WithParameters(p => {
				p["event",       typeof(long)]     = message.EventNumber;
				p["branch",      typeof(string)]   = message.Branch;
				p["subject",     typeof(string)]   = message.Subject;
				p["enteredAt",   typeof(DateTime)] = InUtc(message.EnteredAt);
				p["collectedAt", typeof(DateTime)] = InUtc(message.CollectedAt);
				p["days",        typeof(string[])] = message.CoveredDays;
			})
			.PerformCheckThenCommand();
		}

		/// <summary>
		/// El gasto que se consume en su periodo, tal como llego. La clase la dice CUAL de los dos
		/// comandos se aplica y no ningun campo de la senal, y ninguna de las dos guardas de la puerta
		/// se llama por aca: revientan con ArgumentOutOfRangeException, que por este camino se llevaria
		/// el bloque entero, asi que el monto ya se exigio al interpretar la senal -- donde descartarla
		/// no cuesta ninguna entrada. La moneda tambien: cruza como llego, porque el borde ya comprobo
		/// que este modulo la nombra.
		/// </summary>
		private string ApplyRecurringCostRecorded(RecurringCostRecorded message)
		{
			return app.Using(
				scriptForChk: TheEventIsNew,
				@"
					{
						ledger.RecordRecurringCost(@incurredAt, @amount, @currency, @subject);
						logbook.Advance(@event);
					}
				")
			.WithParameters(p => {
				p["event",      typeof(long)]     = message.EventNumber;
				p["incurredAt", typeof(DateTime)] = InUtc(message.IncurredAt);
				p["amount",     typeof(decimal)]  = Normalized(message.Amount);
				p["currency",   typeof(string)]   = message.Currency;
				p["subject",    typeof(string)]   = message.Subject;
			})
			.PerformCheckThenCommand();
		}

		/// <summary>
		/// El gasto que sigue dando valor despues, con la vida util que llego con el. Mismo motivo que
		/// arriba para no llamar ninguna guarda de la puerta, y la vida util ya se exigio positiva al
		/// interpretar la senal. Lo que NO se adelanta es que su ventana quepa: eso lo contesta el
		/// dominio y deja su marca.
		/// </summary>
		private string ApplyDurableCostRecorded(DurableCostRecorded message)
		{
			return app.Using(
				scriptForChk: TheEventIsNew,
				@"
					{
						ledger.RecordDurableCost(
							@incurredAt, @amount, @currency, @subject, @expectedLifeMonths);
						logbook.Advance(@event);
					}
				")
			.WithParameters(p => {
				p["event",              typeof(long)]     = message.EventNumber;
				p["incurredAt",         typeof(DateTime)] = InUtc(message.IncurredAt);
				p["amount",             typeof(decimal)]  = Normalized(message.Amount);
				p["currency",           typeof(string)]   = message.Currency;
				p["subject",            typeof(string)]   = message.Subject;
				p["expectedLifeMonths", typeof(int)]      = message.ExpectedLifeMonths;
			})
			.PerformCheckThenCommand();
		}

		private const string TheEventIsNew =
			"Check(@event > logbook.LastEvent()) Error 'evento ya procesado';";

		/// <summary>
		/// El nombre completo de la excepcion con la que el dominio rechaza un hecho.
		/// </summary>
		// SE RECONOCE POR NOMBRE Y NO POR TIPO, y no es un atajo: el dominio es enteramente
		// `internal` y esta Superficie no esta en su lista de InternalsVisibleTo, asi que no
		// puede escribir `catch (DomainException)`. Y no le hace falta: lo que necesita saber
		// no es un tipo, es una PREGUNTA -"rechazo el dominio este hecho?"- cuya respuesta
		// consume como texto (`refused.Message`). Ligar la Superficie al tipo para leerle una
		// cadena era acoplamiento sin contrapartida.
		//
		// LA CADENA ESTA AMARRADA: las pruebas comparan esta constante contra
		// typeof(DomainException).FullName, y son el unico lugar que ve los dos lados. Si el
		// dominio renombra o muda la excepcion, la prueba lo dice, en vez de que este catch
		// deje de atrapar en silencio y un hecho rechazado tumbe el avance de la narrativa.
		internal const string TheDomainRefusal = "DealershipCar.Dominio.DomainException";

		private static bool TheDomainRefusedTheFact(Exception refused) =>
			refused.GetType().FullName == TheDomainRefusal;

		private string TheNarrativeMovesPast(IMensajeDeEvento message, string refusal)
		{
			string rejection = app.Using(
				scriptForChk: TheEventIsNew,
				"logbook.Advance(@event);")
			.WithParameters(p => {
				p["event", typeof(long)] = message.Evento;
			})
			.PerformCheckThenCommand();

			return rejection.Length > 0 ? rejection : refusal;
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
		/// Varias columnas de texto de vuelta desde UNA sola consulta, en el orden de los nombres
		/// pedidos. Es lo que una fila necesita para ser una fila: dos llamadas a ATextList son dos
		/// consultas, y dos consultas son dos momentos.
		/// </summary>
		private string[][] TheTextColumns(string script, string[] names, Action<Parameters> arguments)
		{
			using (RentedParameter rented = app.Actor.RentedParameters())
			{
				app.Actor.Using(script)
				.WithParameters(rented, p => {
					foreach (string each in names) p[Parameter.Out, each, typeof(string[])] = default;

					arguments(p);
				})
				.PerformQuery();

				List<string[]> columns = new List<string[]>();

				foreach (string each in names) columns.Add(rented[each].GetValue<string[]>());

				return columns.ToArray();
			}
		}

		/// <summary>
		/// Los nombres de las cinco columnas del registro de estadias, en el orden en que el DTO las
		/// recibe.
		/// </summary>
		private static readonly string[] TheColumnsOfAStayRecord =
			{ "branch", "subject", "enteredAt", "coveredDays", "says" };

		/// <summary>
		/// Los nombres de las cuatro columnas del registro de asignaciones de placa, en el orden en
		/// que el DTO las recibe.
		/// </summary>
		private static readonly string[] TheColumnsOfAPlateAssignmentRecord =
			{ "asPublished", "joinKey", "assignedAt", "isCurrent" };

		/// <summary>
		/// Los nombres de las tres columnas del registro de completaciones de UNA clase, en el orden
		/// en que el DTO las recibe. No hay columna de clase: la puso quien pregunta.
		/// </summary>
		private static readonly string[] TheColumnsOfACompletionRecord =
			{ "completedAt", "reading", "says" };

		/// <summary>
		/// Los nombres de las cuatro columnas del registro de completaciones de TODAS las clases, en
		/// el orden en que el DTO las recibe. Aqui la clase cambia de fila en fila y viene por columna.
		/// </summary>
		private static readonly string[] TheColumnsOfAnEveryKindCompletionRecord =
			{ "kind", "completedAt", "reading", "says" };

		/// <summary>
		/// Los nombres de las cuatro columnas del registro de precios publicados, en el orden en que
		/// el DTO las recibe.
		/// </summary>
		private static readonly string[] TheColumnsOfAPublishedPriceRecord =
			{ "operation", "milestone", "says", "revision" };

		/// <summary>
		/// Los nombres de las seis columnas del registro del libro de costos, en el orden en que el
		/// DTO las recibe. No hay columna de identidad ni de la parte que a un periodo le toca.
		/// </summary>
		private static readonly string[] TheColumnsOfACostLedgerRecord =
		{
			"incurredAt", "subject", "money", "classification", "expectedLife", "says",
		};

		/// <summary>
		/// La clase por la que se pregunta, exigida como nombre de miembro ANTES de que haya consulta.
		/// </summary>
		private static Action<Parameters> TheKindAsked(string kind)
		{
			return p => {
				p["kind", typeof(Enum)] = WireName.OperationKind(kind);
			};
		}

		/// <summary>
		/// La clase y el instante que las diez lecturas de la cuenta atras toman, atados una sola vez.
		/// </summary>
		private static Action<Parameters> TheKindAndTheInstant(string kind, DateTime asOf)
		{
			return p => {
				p["kind", typeof(Enum)]     = WireName.OperationKind(kind);
				p["asOf", typeof(DateTime)] = InUtc(asOf);
			};
		}

		/// <summary>
		/// Una de las diez lecturas de la cuenta atras que contesta con un si o un no, con el rechazo
		/// del dominio traducido a un tipo que quien lee puede nombrar.
		/// </summary>
		private bool ATruthAboutAnOperation(
			string script, string name, string kind, DateTime asOf)
		{
			try
			{
				return ATruth(script, name, TheKindAndTheInstant(kind, asOf));
			}
			catch (Exception refused) when (TheDomainRefusedTheFact(refused))
			{
				throw ARefusalTheReaderCanName(kind, asOf, refused);
			}
		}

		/// <summary>Lo mismo para la que contesta con un numero.</summary>
		private int ACountAboutAnOperation(
			string script, string name, string kind, DateTime asOf)
		{
			try
			{
				return ACount(script, name, TheKindAndTheInstant(kind, asOf));
			}
			catch (Exception refused) when (TheDomainRefusedTheFact(refused))
			{
				throw ARefusalTheReaderCanName(kind, asOf, refused);
			}
		}

		/// <summary>Lo mismo para la que contesta con palabras.</summary>
		private string ATextAboutAnOperation(
			string script, string name, string kind, DateTime asOf)
		{
			try
			{
				return AText(script, name, TheKindAndTheInstant(kind, asOf));
			}
			catch (Exception refused) when (TheDomainRefusedTheFact(refused))
			{
				throw ARefusalTheReaderCanName(kind, asOf, refused);
			}
		}

		/// <summary>
		/// El rechazo del dominio dicho en un tipo que quien lee puede nombrar. Las diez lecturas de
		/// la cuenta atras heredan una guarda del dominio -- no se evalua el pasado de un ancla que
		/// todavia no existia -- y esta puerta NO LA PUEDE PRE-CHEQUEAR: no tiene ninguna lectura que
		/// conteste cual es el ancla de la ocurrencia, asi que no hay nada que preguntar antes. Lo que
		/// si hace falta igual es que el rechazo se pueda atrapar POR TIPO, y la excepcion del dominio
		/// es interna: ninguna caratula la puede nombrar, y un catch de Exception a secas confundiria
		/// un fallo de disco con el rechazo del dominio. Asi que se traduce en vez de pre-chequearse,
		/// y esto se dice en vez de disimularse.
		/// </summary>
		private static InvalidOperationException ARefusalTheReaderCanName(
			string kind, DateTime asOf, Exception refused)
		{
			return new InvalidOperationException(
				$"La agenda rechazo la pregunta por {kind} al {InUtc(asOf):O}. Lo que dijo: "
				+ refused.Message
				+ " Preguntar por un instante anterior al ancla de la ocurrencia es la forma conocida "
				+ "de llegar aca, y la agencia puede sembrar un plan anclado adelante, asi que "
				+ "pregunte por un instante que no sea anterior a esa ancla.",
				refused);
		}

		/// <summary>
		/// Que lo que le queda de distancia se pueda decir antes de pedir el monto o la unidad. Se
		/// pregunta con el MISMO instante que la lectura va a usar, que es lo que hace que las dos
		/// hablen de un solo momento. La guarda vive aca por el motivo de arriba.
		/// </summary>
		private void TheDistanceRemainderHasToBeSayable(string kind, DateTime asOf)
		{
			if (CanDistanceRemainingBeSaid(kind, asOf)) return;

			throw new InvalidOperationException(
				$"No se pudo medir cuanta distancia le queda a {kind} al {InUtc(asOf):O}: pregunte "
				+ "CanDistanceRemainingBeSaid con ese mismo instante antes. Un cero aca se leeria como "
				+ "que no falta nada, que es exactamente lo que no se puede decir. En cual de las "
				+ "siete situaciones esta lo contesta ReadDistanceRemainderStanding, y con palabras "
				+ "DescribeDistanceRemaining.");
		}

		/// <summary>Lo mismo para el plazo, y son dos mitades: una se puede medir sin la otra.</summary>
		private void TheTimeRemainderHasToBeSayable(string kind, DateTime asOf)
		{
			if (CanDaysRemainingBeSaid(kind, asOf)) return;

			throw new InvalidOperationException(
				$"No se pudo medir cuantos dias le quedan a {kind} al {InUtc(asOf):O}: pregunte "
				+ "CanDaysRemainingBeSaid con ese mismo instante antes. Un cero aca se leeria como que "
				+ "no falta nada. En cual de las siete situaciones esta lo contesta "
				+ "ReadTimeRemainderStanding, y con palabras DescribeDaysRemaining.");
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

		/// <summary>
		/// Que el sobre traiga sus DOS mitades. El dominio lo exige tambien, y esto no lo reemplaza:
		/// se exige aca ADEMAS porque para cuando la guarda del dominio corre, la entrada del comando
		/// ya se escribio, y el rechazo queda en el journal de la persona haciendo loguear un fallo
		/// de rehidratacion en cada arranque futuro. Aca no cuesta ninguna entrada.
		/// </summary>
		private static void TheEnvelopeCarriesBothOfItsHalves(int months, int distance)
		{
			if (months > 0 && distance > 0) return;

			throw new ArgumentOutOfRangeException(
				months <= 0 ? nameof(months) : nameof(distance),
				$"Se quiso publicar un sobre de {months} meses y {distance} de distancia. El sobre de "
				+ "un vehiculo lleva sus dos mitades siempre: una en cero se leeria como un limite ya "
				+ "cumplido el mismo dia de la entrega, y una en negativo como uno que no se puede "
				+ "cumplir nunca.");
		}

		/// <summary>
		/// Que el odometro del dia de la entrega sea una lectura y no cualquier numero. Mismo motivo
		/// que arriba: el dominio lo exige, y rechazarlo aca es lo que evita que exigirlo cueste una
		/// entrada permanente.
		/// </summary>
		private static void TheOdometerAtDeliveryIsNotNegative(int reading)
		{
			if (reading >= 0) return;

			throw new ArgumentOutOfRangeException(
				nameof(reading),
				$"El odometro del dia de la entrega no puede ser negativo, y llego {reading}. Es la "
				+ "lectura desde la que se cuenta todo lo que el vehiculo recorrio bajo garantia.");
		}

		/// <summary>
		/// Que lo que se gasto sea dinero. El dominio lo exige tambien y esto no lo reemplaza: para
		/// cuando su guarda corre, la entrada del comando ya se escribio. Un monto negativo es la
		/// sucursal pagandole a la persona; cero SI es un monto, porque un servicio incluido tambien
		/// se registra.
		/// </summary>
		private static void TheAmountSpentIsNotNegative(decimal amount)
		{
			if (amount >= 0) return;

			throw new ArgumentOutOfRangeException(
				nameof(amount),
				$"Llego un gasto de {amount} y un monto negativo no es dinero: es la sucursal "
				+ "pagandole a la persona. Un gasto se registra por lo que costo, y devolver plata "
				+ "es otro hecho que este libro no lleva.");
		}

		/// <summary>
		/// Que un gasto duradero traiga la vida util en meses positivos. Mismo motivo que arriba, y
		/// no hay ninguna vida por defecto que poner en su lugar: cuanto dura una pieza lo sabe quien
		/// la vendio.
		/// </summary>
		private static void TheExpectedLifeIsSaidInPositiveMonths(int expectedLifeMonths)
		{
			if (expectedLifeMonths > 0) return;

			throw new ArgumentOutOfRangeException(
				nameof(expectedLifeMonths),
				$"Llego un gasto duradero con una vida util de {expectedLifeMonths} meses. Una vida "
				+ "util que no es positiva no deja ninguna ventana sobre la que repartir el gasto, y "
				+ "no hay ninguna por defecto: lo que se consume en su propio periodo se registra con "
				+ "el otro comando.");
		}

		/// <summary>
		/// Que la entrega este registrada antes de dar cualquiera de sus tres datos. La guarda se
		/// hace ACA y no se deja reventar al dominio porque la excepcion del dominio es interna y
		/// ninguna caratula la puede nombrar: quien lee tiene que poder atrapar el rechazo por tipo.
		/// </summary>
		private void ADeliveryHasToHaveBeenRecorded(string whyAnEmptyAnswerWouldLie)
		{
			if (HasDeliveryBeenRecorded()) return;

			throw new InvalidOperationException(
				"La agencia no registro la entrega de este vehiculo: pregunte "
				+ "HasDeliveryBeenRecorded antes. " + whyAnEmptyAnswerWouldLie);
		}

		/// <summary>
		/// Que haya una placa publicada a ese instante antes de dar cualquiera de sus dos textos. Se
		/// pregunta con EL MISMO instante que la lectura de abajo va a usar, que es lo que hace que
		/// las dos hablen de una sola placa. Mismo motivo que arriba para que la guarda viva aca.
		/// </summary>
		private void APlateHasToBePublishedAt(DateTime asOf, string whyAnEmptyAnswerWouldLie)
		{
			if (IsAPlatePublished(asOf)) return;

			throw new InvalidOperationException(
				$"Este vehiculo no tiene ninguna placa publicada al {InUtc(asOf):O} ni antes: "
				+ "pregunte IsAPlatePublished con ese mismo instante antes. "
				+ whyAnEmptyAnswerWouldLie);
		}

		/// <summary>
		/// Que haya una lectura del odometro observada a ese instante o antes. Se pregunta con EL
		/// MISMO instante que la lectura de abajo va a usar, que es lo que hace que las dos hablen de
		/// un solo numero. Mismo motivo que arriba para que la guarda viva en la puerta.
		/// </summary>
		private void AnOdometerReadingHasToExistAt(
			DateTime asOf, string whyAnEmptyAnswerWouldLie)
		{
			if (HasAnOdometerReading(asOf)) return;

			throw new InvalidOperationException(
				$"Este vehiculo no tiene ninguna lectura del odometro observada al {InUtc(asOf):O} "
				+ "ni antes: pregunte HasAnOdometerReading con ese mismo instante antes. "
				+ whyAnEmptyAnswerWouldLie);
		}

		/// <summary>
		/// Si el registro de este vehiculo tiene los TRES hechos que la pregunta de cobertura
		/// necesita a ese instante. Las tres presencias se preguntan con el MISMO instante que la
		/// lectura compuesta va a usar, que es lo que hace que hablen de un solo odometro.
		/// </summary>
		private bool TheRecordHoldsWhatTheCoverageQuestionNeeds(DateTime asOf)
		{
			return IsWarrantyEnvelopePublished()
				&& HasDeliveryBeenRecorded()
				&& HasAnOdometerReading(asOf);
		}

		/// <summary>
		/// Que esten los tres antes de componer la frase de cobertura. Se rechaza ACA y no se deja
		/// reventar al dominio porque la excepcion del dominio es interna y ninguna caratula la
		/// puede nombrar.
		/// </summary>
		private void TheRecordHasToHoldWhatTheCoverageQuestionNeeds(DateTime asOf)
		{
			if (TheRecordHoldsWhatTheCoverageQuestionNeeds(asOf)) return;

			throw new InvalidOperationException(
				"El registro de este vehiculo no tiene los tres hechos que la pregunta de cobertura "
				+ "necesita a ese instante -- el sobre publicado, la entrega registrada y una lectura "
				+ "del odometro observada a ese instante o antes: pregunte "
				+ "IsWarrantyEnvelopePublished, HasDeliveryBeenRecorded y HasAnOdometerReading con "
				+ "ese mismo instante antes. Componer la frase con un cero o con la lectura de la "
				+ "entrega diria como medido algo que nadie midio. El alcance publicado lo contesta "
				+ "DescribeCoverageSummary sin ningun hecho de ningun vehiculo, y cual de los tres "
				+ "hechos falta lo dice DescribeWarrantyEnvelope con palabras.");
		}

		/// <summary>
		/// Los DOS argumentos que las tres lecturas compuestas de cobertura toman, atados una sola
		/// vez. Los otros siete hechos no vienen de aqui: los saca el script del registro.
		/// </summary>
		private static Action<Parameters> TheSystemAndTheInstant(string systemCode, DateTime asOf)
		{
			return p => {
				p["systemCode", typeof(string)]   = TheText(systemCode, nameof(systemCode));
				p["asOf",       typeof(DateTime)] = InUtc(asOf);
			};
		}

		/// <summary>
		/// Que el vehiculo NO estuviera ya adentro cuando dice que entro. La guarda del dominio sigue
		/// siendo la duena de la invariante -- un carro no esta en dos sucursales a la vez -- y esta
		/// la adelanta al unico momento en que rechazarla todavia es gratis. NO la ve entera: una
		/// entrada fechada antes de la que ya esta abierta pasa por aca, y esa la refusa el dominio.
		/// </summary>
		private void TheVehicleHasToBeOutsideAt(DateTime enteredAt)
		{
			if (!IsInsideTheShopAt(enteredAt)) return;

			throw new InvalidOperationException(
				$"Este vehiculo ya estaba adentro de un taller al {InUtc(enteredAt):O}: pregunte "
				+ "IsInsideTheShopAt con ese mismo instante antes. Un carro no esta en dos sucursales "
				+ "a la vez, y dejar que el dominio lo rechace deja el rechazo escrito en el journal "
				+ "de la persona para siempre.");
		}

		/// <summary>
		/// Que hubiera algo que cerrar cuando dice que lo entregaron. Mismo motivo que la de arriba y
		/// mismo limite: una estadia YA CERRADA que cubre ese instante pasa por aca, y esa la refusa
		/// el dominio.
		/// </summary>
		private void TheVehicleHasToBeInsideAt(DateTime collectedAt)
		{
			if (IsInsideTheShopAt(collectedAt)) return;

			throw new InvalidOperationException(
				$"Este vehiculo no estaba adentro de ningun taller al {InUtc(collectedAt):O}, asi que "
				+ "no hay ninguna estadia que cerrar: pregunte IsInsideTheShopAt con ese mismo "
				+ "instante antes. Una estadia cerrada es terminal y no se reabre.");
		}

		/// <summary>
		/// Los dias que la sucursal dice que la estadia cubrio, cada uno exigido como dia antes de
		/// que haya comando. Cuantos son, en que orden vienen y si son contiguos es LEY DEL DOMINIO y
		/// no se copia aca: lo que se exige en el borde es lo que se puede DECIR.
		/// </summary>
		private static string[] TheDaysTheBranchSaid(string[] coveredDays)
		{
			ArgumentNullException.ThrowIfNull(coveredDays);

			List<string> said = new List<string>();

			foreach (string each in coveredDays) said.Add(ADay(each, nameof(coveredDays)));

			return said.ToArray();
		}

		/// <summary>
		/// Un dia de calendario tal como la sucursal lo dice, exigido y devuelto vuelto a escribir,
		/// asi que lo que cruza el wire es canonico por construccion. Un INSTANTE no es un dia:
		/// recortarlo a su fecha seria inventar la zona horaria que este registro no tiene.
		/// </summary>
		private static string ADay(string text, string parameterName)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(text, parameterName);

			DateOnly day;

			if (!DateOnly.TryParseExact(
					text, TheShapeADayIsSaidIn, CultureInfo.InvariantCulture, DateTimeStyles.None,
					out day))
				throw new ArgumentException(
					$"'{text}' no dice un dia de calendario en {TheShapeADayIsSaidIn}. El dia llega "
					+ "escrito junto con el hecho, asi que una forma que este registro no lea no es "
					+ "un dia menos preciso: no es ninguno. Y un instante tampoco es un dia.",
					parameterName);

			return day.ToString(TheShapeADayIsSaidIn, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Como se dice un dia de calendario en este modulo, de los dos lados del wire. Es internal
		/// porque el otro lado del wire tambien lo mira: la frontera de la ingesta exige esta misma
		/// forma antes de que haya comando, y dos copias de la forma serian dos formas.
		/// </summary>
		internal const string TheShapeADayIsSaidIn = "yyyy-MM-dd";

		/// <summary>
		/// Los dos extremos de un periodo dicho en dias, exigidos y atados para el script que los
		/// pregunta. El segundo no va antes que el primero; uno igual al otro es un periodo de un
		/// solo dia, porque los dos extremos son inclusivos.
		/// </summary>
		private static Action<Parameters> ADayRange(string fromDay, string toDay)
		{
			string from = ADay(fromDay, nameof(fromDay));
			string to = ADay(toDay, nameof(toDay));

			if (string.CompareOrdinal(from, to) > 0)
				throw new ArgumentException(
					$"El periodo va del {from} al {to}, y ese segundo dia es anterior al primero: un "
					+ "periodo que termina antes de empezar no nombra ningun dia.",
					nameof(toDay));

			return p => {
				p["fromDay", typeof(string)] = from;
				p["toDay",   typeof(string)] = to;
			};
		}

		/// <summary>
		/// Los dos extremos de un periodo dicho en INSTANTES, normalizados y atados para el script
		/// que los pregunta. Los dos son inclusivos.
		/// <para>
		/// Un segundo extremo anterior al primero NO se refusa, al reves que un periodo dicho en
		/// dias: los dos extremos ya dicen un instante cada uno, las seis lecturas de la serie son
		/// totales, y la respuesta nombra los extremos que se le dieron -- asi que un extremo
		/// inventado produce una respuesta visiblemente acotada y nunca un hecho falso. Ordenarlos
		/// aca contestaria un periodo distinto del que se pregunto, sin decirlo.
		/// </para>
		/// </summary>
		private static Action<Parameters> APeriodOfInstants(DateTime from, DateTime to)
		{
			return p => {
				p["from", typeof(DateTime)] = InUtc(from);
				p["to",   typeof(DateTime)] = InUtc(to);
			};
		}

		/// <summary>
		/// Los cuatro extremos de la comparacion, atados una sola vez. El primer par es el periodo
		/// del que se dice el cambio y el segundo es contra el que se dice.
		/// </summary>
		private static Action<Parameters> TwoPeriodsOfInstants(
			DateTime from1, DateTime to1, DateTime from2, DateTime to2)
		{
			return p => {
				p["from1", typeof(DateTime)] = InUtc(from1);
				p["to1",   typeof(DateTime)] = InUtc(to1);
				p["from2", typeof(DateTime)] = InUtc(from2);
				p["to2",   typeof(DateTime)] = InUtc(to2);
			};
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
