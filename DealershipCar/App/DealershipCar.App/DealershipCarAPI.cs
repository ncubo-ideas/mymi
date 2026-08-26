using System;
using Choreography.Theater;
using Puppeteer;

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
	public sealed class DealershipCarAPI
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
