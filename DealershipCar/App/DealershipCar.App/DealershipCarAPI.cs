using System;
using Choreography.Theater;
using DealershipCar.App.Events;
using DealershipCar.Dominio;
using Puppeteer;
using Puppets.Portador;

namespace DealershipCar.App
{
	// LA SUPERFICIE INTENCIONAL de la app y LA UNICA PUERTA. Todo el mundo entra por aca -- la UI,
	// el bucle de refresh y el servidor MCP -- y nadie habla con el actor crudo.
	//
	// UN MIEMBRO POR OPERACION DE LA FACHADA, con los mismos nombres que §4 del plan le dio al
	// agregado. Que la lista sea la misma es lo que hace comprobable que la app no invento
	// operaciones ni escondio ninguna.
	//
	// LOS CODIGOS CRUZAN COMO NOMBRE, no como enum. El motor rechaza por diseño un enum concreto
	// como tipo de parametro; la forma soportada es typeof(Enum) llevando el NOMBRE del miembro,
	// que el sitio de llamada del script coerciona (medido, SPIKES-OS2 S2). Eso deja dos cosas: el
	// journal guarda "FluidChange" y no 2, asi que renumerar el enum no puede reinterpretar la
	// historia; y las caratulas -- que no ven el dominio, y no deben verlo -- pueden llamar a esta
	// puerta sin referenciarlo. WireName es quien rechaza un nombre que no existe, ANTES de que la
	// entrada se escriba.
	//
	// LOS VALORES VIAJAN COMO PARAMETRO, nunca interpolados en el texto del DSL. Un valor
	// interpolado queda dentro de la sentencia, y entonces el replay reproduce el TEXTO en vez de
	// reusar el dato que se recibio. La unica interpolacion que hay en este archivo esta en las
	// CONSULTAS, que no se journalizan.
	//
	// TODOS LOS INSTANTES SE NORMALIZAN ACA (§3.5 del plan): a UTC y truncados al segundo. El
	// truncado no es celo: el journal escribe el DateTime como texto con granularidad de segundo y
	// borra el Kind (medido, SPIKES-OS2 S4), asi que un instante con fraccion contestaria una cosa
	// en memoria y otra despues del replay. Truncar en la frontera hace que la corrida viva y la
	// reproducida sean la misma. El dominio no toca .Kind ni convierte husos: no lee un reloj y
	// tampoco lo interpreta.
	//
	// ES TAMBIEN EL IDiarioDeLaApp QUE EL PORTADOR NECESITA: el bucle de refresh no sabe hacer
	// performs, sabe pedirle a esto que aplique un mensaje. Los tres miembros de esa interfaz van
	// IMPLICITAMENTE EXPLICITOS -- IMensajeDeEvento y IDiarioDeLaApp son del mecanismo compartido y
	// sus nombres estan en espanol, los de este modulo en ingles (C29) -- asi que cada uno delega
	// en el miembro ingles que hace el trabajo.
	public sealed class DealershipCarAPI : IDiarioDeLaApp
	{
		private readonly PerformanceV2 app;

		public DealershipCarAPI(PerformanceV2 app)
		{
			ArgumentNullException.ThrowIfNull(app);

			this.app = app;
		}

		// ------------------------------------------------------ comandos: sembrar el plan

		// Siembra una operacion del plan del fabricante. Trae sus limites, sus clasificaciones y el
		// ANCLA completa -- instante, lectura y la unidad de esa lectura (C18) -- porque una
		// operacion nunca completada se mide desde algun lado y el dominio no puede inventarlo.
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

		// ABRE una revision del plan. Las operaciones que trae entran despues, una por una, por
		// SeedPlanOperation: ese es el unico camino por el que una operacion entra a la agenda (C5).
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

		// ------------------------------------------------------ comandos: lado de la persona

		// Agrega una operacion que pidio la persona. Nunca es obligatoria ni clasificada de
		// seguridad -- eso lo declara el fabricante -- asi que esas dos banderas no estan en la
		// firma en vez de estar en falso.
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

		// La persona quita una operacion suya. Sobre una del plan se rechaza, y rechazar es un
		// hecho del negocio: no revienta (ley L9, OINV-31).
		public void RemoveOwnerOperation(string kind)
		{
			app.Using("schedule.RemoveOwnerOperation(@kind);")
			.WithParameters(p => {
				p["kind", typeof(Enum)] = WireName.OperationKind(kind);
			})
			.PerformCommand();
		}

		// La persona declara como usa el vehiculo, desde cuando.
		public void DeclareUsageProfile(string profile, DateTime effectiveFrom)
		{
			app.Using("schedule.DeclareUsageProfile(@profile, @effectiveFrom);")
			.WithParameters(p => {
				p["profile",       typeof(Enum)]     = WireName.UsageProfile(profile, nameof(profile));
				p["effectiveFrom", typeof(DateTime)] = InUtc(effectiveFrom);
			})
			.PerformCommand();
		}

		// La persona deja una operacion para despues, CON SU MOTIVO: lo que decidio es la mitad del
		// hecho, y OINV-28 lo journaliza entero.
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

		// ------------------------------------------------------ comandos: hechos recibidos

		// Entra una lectura del odometro, con su unidad y su procedencia. Una que no calza en la
		// linea de observacion se rechaza adentro (OINV-54, C21).
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

		// Entra un cruce de umbral observado. Solo ADELANTA la operacion de esa clase (ley L3).
		public void RecordConditionThreshold(string kind, DateTime observedAt)
		{
			app.Using("schedule.RecordConditionThreshold(@kind, @observedAt);")
			.WithParameters(p => {
				p["kind",       typeof(Enum)]     = WireName.OperationKind(kind);
				p["observedAt", typeof(DateTime)] = InUtc(observedAt);
			})
			.PerformCommand();
		}

		// La operacion se hizo de verdad, y la sucesora se ancla en los valores REALES (ley L4). La
		// unidad viaja con la lectura porque es la unidad del ancla de la sucesora: fijarla aca
		// haria medir a la sucesora contra una escala que nadie uso.
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

		// ------------------------------------------------------ comandos: la bitacora de eventos

		// La clave con que el concesionario conoce a ESTA instalacion. Se genera en C# y entra como
		// PARAMETRO, no dentro del texto del DSL: asi el valor queda journalizado y el replay lo
		// reusa. Un identificador generado dentro del dominio daria uno distinto en cada replay y la
		// app perderia su identidad ante el concesionario.
		public void RegisterDevice(string deviceKey)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(deviceKey);

			app.Using("logbook.RegisterDevice(@device);")
			.WithParameters(p => {
				p["device", typeof(string)] = deviceKey;
			})
			.PerformCommand();
		}

		// Guarda la credencial con que se sigue leyendo la narrativa. Queda EN CLARO en el journal,
		// que es una deuda conocida y aceptada para el prototipo (plan §8): el access token es
		// corto, el refresh token es durable.
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

		// ------------------------------------------------------ el primitivo que maneja el host

		// Donde queda cada operacion en ese instante, y que se movio.
		//
		// ES LA CADENCIA DE C6, y por eso es un PerformCheckThenCommand y no un PerformCommand. La
		// app se abre todos los dias y evalua al abrirse; un comando pelado dejaria una entrada por
		// apertura aunque no se moviera nada. La guarda mira el punto de control monotono que la
		// agenda ya lleva, y un instante ya evaluado se rechaza BAJO EL READ LOCK: no deja entrada
		// (medido, SPIKES-OS2 S3).
		//
		// La guarda es un || y el orden importa: && y || cortocircuitan, asi que LastEvaluatedAt --
		// que revienta si nunca se evaluo (E41) -- solo se pregunta cuando ya hubo una evaluacion.
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

		// ------------------------------------------------------ lecturas

		// Valores TIPADOS y no texto parseado: es lo que le permite a la UI hacer aritmetica y a
		// las tools MCP devolver un int que es un int.

		// De que vehiculo es esta agenda.
		public string ReadVin()
		{
			return AText("@vin = schedule.ReadVin();", "vin", p => { });
		}

		// Donde esta parada la operacion de esa clase. TOTAL: una clase sin operacion agendada
		// contesta NotDue, que es una respuesta legal y no una ausencia (E41).
		public string ReadDueState(string kind)
		{
			return AText("@state = schedule.ReadDueState(@kind);", "state", p => {
				p["kind", typeof(Enum)] = WireName.OperationKind(kind);
			});
		}

		// Cuantas operaciones estan vencidas o pasadas.
		public int CountDue()
		{
			return ACount("@due = schedule.CountDue();", "due", p => { });
		}

		// Cuantas se pasaron de la gracia.
		public int CountOverdue()
		{
			return ACount("@overdue = schedule.CountOverdue();", "overdue", p => { });
		}

		// Cuantos aplazamientos lleva usados esa clase. Total y derivado.
		public int DeferralsUsed(string kind)
		{
			return ACount("@used = schedule.DeferralsUsed(@kind);", "used", p => {
				p["kind", typeof(Enum)] = WireName.OperationKind(kind);
			});
		}

		// Si aplazar esa clase haria algo (ley L6). Total: una clase sin operacion agendada contesta
		// false, que es lo mismo que hace el comando.
		//
		// EXISTE PORQUE UNA CARATULA NO PUEDE DEDUCIRLA. L6 tiene cuatro condiciones y la puerta solo
		// proyectaba una: el ESTADO. Las otras tres -- que la clase este agendada, que no sea
		// clasificada de seguridad, y que no haya llegado al tope -- no se ven desde afuera, asi que
		// una pantalla que decidiera con lo que tenia acertaria a veces y ofreceria un boton muerto
		// sobre justo la operacion que el dominio nunca aplaza. Es la pregunta, no una copia de la
		// regla: ScheduledOperation.DeferralCapAndWindow la usa para decidir.
		public bool CanBeDeferred(string kind)
		{
			return ATruth("@can = schedule.CanBeDeferred(@kind);", "can", p => {
				p["kind", typeof(Enum)] = WireName.OperationKind(kind);
			});
		}

		// Si hay algo vencido. Es la pregunta de presencia que se hace ANTES de ReadNextDueKind.
		public bool HasAnythingDue()
		{
			return ATruth("@any = schedule.HasAnythingDue();", "any", p => { });
		}

		// Que hay que hacer primero. GUARDADA: sin nada vencido revienta, y la que avisa es
		// HasAnythingDue (E41). Se pregunta primero desde aca para que la caratula reciba una
		// excepcion que pueda NOMBRAR -- el DomainException del dominio es internal y ninguna
		// caratula lo ve.
		public string ReadNextDueKind()
		{
			if (!HasAnythingDue())
				throw new InvalidOperationException(
					"No hay nada vencido en esta agenda: pregunte HasAnythingDue antes. Devolver "
					+ "una clase vacia haria indistinguible 'no hay nada que hacer' de 'lo "
					+ "siguiente es una operacion sin nombre'.");

			return AText("@kind = schedule.ReadNextDueKind();", "kind", p => { });
		}

		// Por que esa clase esta donde esta, en una frase. Componer el motivo es del dominio y no
		// de la vista (OUS-07), asi que esto lo devuelve tal cual sale.
		public string ReadDueReason(string kind)
		{
			return AText("@reason = schedule.ReadDueReason(@kind);", "reason", p => {
				p["kind", typeof(Enum)] = WireName.OperationKind(kind);
			});
		}

		// Cuantas operaciones cuentan como mantenimiento no hecho. Derivado, nunca un contador.
		public int MissedCount()
		{
			return ACount("@missed = schedule.MissedCount();", "missed", p => { });
		}

		// Si la agenda se evaluo alguna vez.
		public bool HasBeenEvaluated()
		{
			return ATruth("@evaluated = schedule.HasBeenEvaluated();", "evaluated", p => { });
		}

		// Hasta cuando se evaluo. GUARDADA, igual que ReadNextDueKind y por lo mismo.
		public DateTime LastEvaluatedAt()
		{
			if (!HasBeenEvaluated())
				throw new InvalidOperationException(
					"Esta agenda no se ha evaluado nunca: pregunte HasBeenEvaluated antes. Un "
					+ "DateTime.MinValue aca seria un centinela magico -- un instante que parece un "
					+ "instante y no lo es.");

			return AnInstant("@at = schedule.LastEvaluatedAt();", "at", p => { });
		}

		// ------------------------------------------------------ lecturas de la bitacora

		// Hasta donde se leyo la narrativa del concesionario. SALE DEL JOURNAL, no de un campo en
		// memoria, y eso es lo que hace exacto el resume tras una caida: el punto de control avanza
		// DENTRO del mismo comando que aplica el hecho, asi que no existe el estado intermedio
		// "guarde el hecho pero no avance el checkpoint", que es el que duplica al reintentar.
		public long LastEventApplied()
		{
			return ANumber("@last = logbook.LastEvent();", "last", p => { });
		}

		// Las tres piezas de la credencial contestan VACIO cuando no hay, y no revienta: el bucle
		// del Portador pregunta por el token y decide con IsNullOrWhiteSpace. La lectura guardada
		// del dominio sigue detras -- se le pregunta la presencia primero, igual que con
		// ReadNextDueKind -- asi que la polaridad de E41 se conserva y la traduccion a "" pasa aca,
		// en la frontera, que es donde el contrato del mecanismo la pide.
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

		// ------------------------------------------------------ la ingesta de eventos

		// APLICA UN EVENTO COMO UN COMANDO. Devuelve "" si entro, o el motivo si no entro.
		//
		// El switch por tipo de mensaje vive ACA, en la frontera, y es lo unico que traduce el wire
		// del concesionario a una operacion de la agenda. De este punto para adentro no hay
		// discriminadores: cada rama es su propio comando, y el texto que queda en el journal dice
		// QUE hecho entro sin que haya que desempaquetar un JSON para saberlo.
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

				throw new ArgumentException(
					$"Esta app no tiene comando para un mensaje de tipo '{message.Tipo}'. La ruta "
					+ "debio devolver null para descartarlo.", nameof(message));
			}
			catch (DomainException refused)
			{
				return TheNarrativeMovesPast(message, refused);
			}
		}

		// EL PUNTO DE CONTROL VA DENTRO DEL MISMO COMANDO QUE APLICA EL HECHO. O entraron los dos o
		// no entro ninguno: no existe el estado "aplique la lectura pero no avance la bitacora", que
		// es exactamente el que duplicaria al reintentar despues de una caida.
		//
		// Y LA GUARDA ES UN Check, no un if en el cuerpo. Un evento redelivered se rechaza BAJO EL
		// READ LOCK y NO deja entrada en el journal (medido, SPIKES-OS2 S3); un if adentro del cuerpo
		// persistiria una entrada no-op por cada reintento, que es la idempotencia costando lo mismo
		// que no tenerla.
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

		// LA GUARDA DE IDEMPOTENCIA, UNA SOLA VEZ. Las cinco ingestas la comparten literalmente:
		// dos copias de un Check se pueden desincronizar, y la que quedara vieja seria la que deja
		// entrar un evento repetido.
		private const string TheEventIsNew =
			"Check(@event > logbook.LastEvent()) Error 'evento ya procesado';";

		// CUANDO EL DOMINIO RECHAZA EL HECHO ADENTRO DEL CUERPO, LA NARRATIVA IGUAL AVANZA -- y esta
		// es la decision mas cara de T8, asi que va dicha entera.
		//
		// Tres de los hechos que manda un concesionario tienen guardas que revientan DENTRO del
		// comando: una lectura que va para atras (C21), una completacion de una clase que esta
		// agenda no tiene activa, y una revision fechada antes de la que rige. El motor ya escribio
		// la entrada cuando el cuerpo corre, asi que el rechazo queda marcado para siempre (C22).
		//
		// LO QUE T8 DESCUBRE ES QUE LA MARCA SE MULTIPLICA. El punto de control esta en el mismo
		// comando que revento, asi que no avanzo; el proveedor sirve desde ese mismo punto; y el
		// refresh siguiente vuelve a traer el mismo hecho y a dejar OTRA entrada. Sin esto, un solo
		// hecho contradictorio deja la app atascada en el, marcando el journal en cada apertura y
		// sin volver a recibir nada nuevo NUNCA. Queda escrito en C28.
		//
		// Asi que el punto de control avanza en un comando aparte y el hecho se pierde. Perder UN
		// hecho que el dominio declaro imposible es estrictamente mejor que perder todos los que
		// vengan detras -- incluida la correccion que el concesionario mande despues. No es
		// silencioso: el motivo vuelve como rechazo, el bucle lo cuenta en Rechazados y la caratula
		// lo imprime.
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

		// ------------------------------------------------- IDiarioDeLaApp (Portador)

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

		// ------------------------------------------------------ la mecanica de una lectura

		// Las cuatro formas escalares que cruzan la puerta. El SCRIPT viaja completo desde el sitio
		// de la llamada a proposito: el texto del DSL es el artefacto legible, y esconderlo detras
		// de un nombre corto volveria a tapar que hace cada lectura.

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

		// El numero del evento es long y no int: es el numero de secuencia de la narrativa de un
		// proveedor, no una cuenta de cosas de esta agenda.
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

		// EL INSTANTE SE NORMALIZA TAMBIEN AL SALIR, y esa mitad no estaba prevista: el journal
		// borra el DateTimeKind (SPIKES-OS2 S4), asi que el mismo instante vuelve Utc en la corrida
		// viva y Unspecified despues de rehidratar. El VALOR es el mismo -- DateTime compara por
		// ticks y no por Kind -- pero una caratula que lo formatee con "O" o lo pase a hora local
		// obtiene dos respuestas distintas para el mismo hecho. Volver a estampar Utc no inventa
		// nada: repone la etiqueta que la convencion de §3.5 ya afirmaba al entrar.
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

		// LA COMBINACION DE BASE Y LIMITES SE PREGUNTA EN LA PUERTA, y la contesta LA GUARDA DEL
		// DOMINIO -- la misma, no una copia. Es la unica de las guardas que revientan adentro del
		// cuerpo que se puede adelantar sin tocar el agregado: no mira nada de la agenda, solo los
		// tres valores que vienen en la llamada. Adelantarla convierte una cicatriz permanente en un
		// rechazo que no escribe nada (C22), y beneficia a toda caratula, no solo a la ingesta.
		//
		// Duplicar la tabla aca en vez de llamarla seria una segunda fuente de verdad para la ley
		// L1/L2, que es justo lo que E32 y C2 argumentan en contra.
		private static void TheDeclaredBasisAgreesWithItsLimits(
			string basis, int timeLimitDays, int distanceLimit)
		{
			IntervalRule.BasisIsNeverInferred(
				(IntervalBasisCode)Enum.Parse(
					typeof(IntervalBasisCode), WireName.IntervalBasis(basis)),
				timeLimitDays,
				distanceLimit);
		}

		// A UTC y al segundo entero. Ver el encabezado de la clase: el journal no guarda ni la
		// fraccion ni el Kind, asi que normalizar aca es lo que hace que la corrida viva y la
		// reproducida contesten lo mismo. Un instante sin Kind se toma como UTC ya, que es la
		// convencion de §3.5 -- no hay de donde sacar otra cosa.
		private static DateTime InUtc(DateTime instant)
		{
			DateTime utc = instant.Kind == DateTimeKind.Local ? instant.ToUniversalTime() : instant;

			return new DateTime(
				utc.Ticks - utc.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
		}
	}
}
