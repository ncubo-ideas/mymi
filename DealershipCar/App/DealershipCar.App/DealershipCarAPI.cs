using System;
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
	}
}
