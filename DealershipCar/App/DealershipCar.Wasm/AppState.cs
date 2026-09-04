using System.Text.Json;
using Choreography.Theater;
using Puppeteer;
using DealershipCar.App;
using DealershipCar.App.Events;
using Puppets.Portador;

namespace DealershipCar.Wasm
{
	public sealed class AppState : IAsyncDisposable
	{
		public const string TheProfileAtDelivery = "Standard";

		private readonly JournalBridge bridge;
		private readonly HostingMode mode;
		private readonly GnothiBridge gnothi;
		private readonly CredentialStore credentials;
		private readonly HttpClient http;
		private readonly DealershipSettings dealership;

		private DealershipCarApp app;
		private DealershipCarAPI api;
		private BucleDeRefresh loop;

		private DateTime lastStampSent = DateTime.MinValue;

		public AppState(JournalBridge bridge, HostingMode mode, GnothiBridge gnothi,
			CredentialStore credentials, HttpClient http, DealershipSettings dealership)
		{
			ArgumentNullException.ThrowIfNull(bridge);
			ArgumentNullException.ThrowIfNull(mode);
			ArgumentNullException.ThrowIfNull(gnothi);
			ArgumentNullException.ThrowIfNull(credentials);
			ArgumentNullException.ThrowIfNull(http);
			ArgumentNullException.ThrowIfNull(dealership);

			this.bridge = bridge;
			this.mode = mode;
			this.gnothi = gnothi;
			this.credentials = credentials;
			this.http = http;
			this.dealership = dealership;
		}

		public bool IsUp => app is not null;

		public bool HasFolder => bridge.HasFolder;

		public bool NeedsRegrant => bridge.NeedsRegrant;

		public string FolderName => bridge.FolderName;

		public string LastMessage { get; private set; } = "";

		public string Mode { get; private set; } = "ninguno";

		public bool InOwnFolder => Mode == "carpeta";

		public bool InBrowserStorage => Mode == "opfs";

		public string SavingIsFailing { get; private set; } = "";

		public string DealershipUnreachable { get; private set; } = "";

		public string EvaluationSkipped { get; private set; } = "";

		public string WhereTheDealershipIs => http.BaseAddress?.ToString() ?? "(not configured)";

		public string Vin => dealership.Vin;

		public ValueTask<bool> PickerIsSupportedAsync() => bridge.PickerIsSupportedAsync();

		public ValueTask<bool> BrowserStorageIsSupportedAsync() => bridge.BrowserStorageIsSupportedAsync();

		public ValueTask<string> WhyThereIsNoStorageAsync() => bridge.WhyThereIsNoStorageAsync();

		public ValueTask<bool> RunsAsInstalledAppAsync() => bridge.RunsAsInstalledAppAsync();

		public ValueTask<string> RememberedModeAsync() => bridge.RememberedModeAsync();

		public ValueTask<string> CurrentPermissionAsync() => bridge.CurrentPermissionAsync();

		public async ValueTask<FolderResult> RecoverFolderAsync()
		{
			FolderResult result = await bridge.RecoverFolderAsync();
			Mode = await bridge.CurrentModeAsync();
			LastMessage = $"recover folder: {result}";
			return result;
		}

		public async ValueTask<FolderResult> ConnectFolderAsync()
		{
			FolderResult result = await bridge.ChooseFolderAsync();
			Mode = await bridge.CurrentModeAsync();
			LastMessage = $"connect folder: {result}";

			if (result.Ok) ReleaseTheActor();
			return result;
		}

		private void ReleaseTheActor()
		{
			app?.Actor?.GracefulExit();
			app = null;
			api = null;
			loop = null;
			lastStampSent = DateTime.MinValue;

			if (Directory.Exists(JournalBridge.RootInMemfs))
				Directory.Delete(JournalBridge.RootInMemfs, recursive: true);
		}

		public async ValueTask<FolderResult> RequestPermissionAsync()
		{
			FolderResult result = await bridge.RequestPermissionAsync();
			LastMessage = $"request permission: {result}";

			if (result.Ok) ReleaseTheActor();
			return result;
		}

		public async ValueTask ForgetTheFolderAsync()
		{
			await bridge.ForgetFolderAsync();
			Mode = await bridge.CurrentModeAsync();
			LastMessage = "the saved folder was forgotten; nothing in it was touched";
		}

		public async ValueTask<FolderResult> UseBrowserStorageAsync()
		{
			FolderResult result = await bridge.UseBrowserStorageAsync();
			Mode = await bridge.CurrentModeAsync();
			LastMessage = $"browser storage: {result}";

			if (result.Ok) ReleaseTheActor();
			return result;
		}

		public async ValueTask StartAsync()
		{
			if (app is not null) return;

			int preloaded = bridge.HasDestination ? await bridge.PreloadIntoMemfsAsync() : 0;

			var started = new DealershipCarApp(dealership.Vin, TheProfileAtDelivery);
			started.ConfigureStorage(DatabaseType.FileSystem, "path=" + JournalBridge.RootInMemfs);
			started.Start();

			app = started;
			api = new DealershipCarAPI(started);

			// LA FUENTE DEPENDE DEL HOSPEDAJE, y es lo unico que depende junto con el destino
			// (11 §2). Suelta hace su propio pull con su credencial; hospedada le pide a Gnothi,
			// que releva con la suya. El cursor lo pone el actor en los dos casos.
			IFuenteDeEventos source = mode.Hosted
				? new FuenteDeEventosPorGnothi((after, _) => mode.Bridge!.PullAsync(after).AsTask())
				: new FuenteHttpDeEventos(http, dealership.Resource, dealership.Vin,
					CredentialAsync, RenewTheCredentialAsync);

			loop = new BucleDeRefresh(api, source, DealershipEventRoute.Route, FlushToDiskAsync);

			try { await FlushToDiskAsync(); } catch (WriteRefusedException) { }

			LastMessage = mode.Hosted
				? $"up, hosted in Gnothi. {preloaded} files preloaded"
				: bridge.HasDestination
					? $"up. {preloaded} files preloaded from '{bridge.FolderName}'"
					: "up, but WITHOUT a folder: the journal lives only in memory and is lost on close";

			try
			{
				// LA CREDENCIAL SOLO EN EL MODO SUELTO. Hospedada la sostiene Gnothi del otro
				// lado del puente y este modulo no ve ningun token: pedirla aca lo mandaria a
				// hablar con el publicador, que es justo lo que la CSP le niega.
				if (!mode.Hosted) await CredentialAsync(CancellationToken.None);

				ResultadoDeRefresh onOpen = await loop.Ejecutar(CancellationToken.None);
				if (onOpen.Cuenta is not null) Account = onOpen.Cuenta;
				await FlushAfterTheBlockAsync();
				DealershipUnreachable = "";
				LastMessage += $" | on open: {onOpen}";
			}
			catch (WriteRefusedException)
			{
				LastMessage += " | the dealership answered, but the folder refused the write";
			}
			catch (Exception ex)
			{
				DealershipUnreachable =
					$"Could not reach {WhereTheDealershipIs} ({ex.GetType().Name}: {ex.Message}).";
				LastMessage += " | " + DealershipUnreachable;
			}

			try
			{
				await EvaluateNowAsync();
			}
			catch (Exception ex)
			{
				LastMessage += $" | could not evaluate ({ex.GetType().Name}: {ex.Message})";
			}

			// RECIEN AHORA el modulo puede recibir: el actor esta hidratado. Gnothi no puede
			// adivinarlo mirando la pantalla, que se dibuja antes.
			if (mode.Hosted) await gnothi.ReadyAsync();
		}


		// ------------------------------------------------------------ hosted in Gnothi

		// EN QUE ESTADO ESTA LA CUENTA ante el proveedor, tal como vino en el ultimo sobre.
		// Null hasta el primer refresh, o si el publicador todavia no manda el bloque `account`.
		public EstadoDeCuenta? Account { get; private set; }

		// La instalacion SUELTA cuyo registro ya se mudo a Gnothi. Hospedada nunca esta
		// migrada: es el destino, no el origen.
		public bool Migrated => !mode.Hosted && Account is { Migrada: true };

		public string WhereItLivesNow => Account?.Host is { Length: > 0 } where ? where : "Gnothi";

		public bool Hosted => mode.Hosted;

		// UNA CONSULTA, la que Gnothi pide por la superficie del modulo. Contra el actor VIVO y
		// con los MISMOS cuerpos que usa cualquier otro hospedaje (ModuleQueries).
		public object Ask(string tool)
		{
			Require();

			return tool switch
			{
				ModuleQueries.Summary => ModuleQueries.MakeSummary(api),
				ModuleQueries.Freshness => ModuleQueries.MakeFreshness(api),
				_ => throw new ArgumentException($"The module does not answer the tool '{tool}'."),
			};
		}

		// EL CARTERO ENTREGA. Gnothi trae un sobre ya leido del publicador y se asienta por EL
		// MISMO camino que el refresh propio: misma ruta, misma guarda Check, mismo flush. De
		// ahi sale gratis que entregar dos veces el mismo sobre no duplique nada.
		public async Task<ResultadoDeRefresh> DeliverAsync(string envelopeJson, CancellationToken ct = default)
		{
			Require();

			var once = new BucleDeRefresh(
				api,
				new FuenteDeEventosPorGnothi((_, _) => Task.FromResult(envelopeJson ?? ""), "gnothi.deliver"),
				DealershipEventRoute.Route,
				FlushToDiskAsync);

			ResultadoDeRefresh result = await once.Ejecutar(ct);
			LastMessage = $"delivery from Gnothi: {result}";
			return result;
		}
		private async Task FlushToDiskAsync()
		{
			try
			{
				await bridge.FlushFromMemfsAsync();
				SavingIsFailing = "";
			}
			catch (WriteRefusedException ex)
			{
				SavingIsFailing = ex.Message;
				throw;
			}
		}

		private async Task FlushAfterTheBlockAsync() => await FlushToDiskAsync();

		// La credencial la pide LA APP, no la persona. El concesionario la emite contra el device
		// key de esta instalacion, acotada a leer los eventos de un recurso. Las tres piezas viven
		// en el CREDENTIAL STORE -storage del browser del origen-, NO en el journal: un token es
		// contexto de sesion, no historia de la persona, y en el journal quedaba en claro y viajaba
		// con cualquier copia del expediente (hasta el 2026-09-03 era asi). La fuente HTTP recibe
		// CredentialAsync y RenewTheCredentialAsync como delegados y el diario no la conoce.
		private async Task<string> CredentialAsync(CancellationToken ct)
		{
			StoredCredential stored = await credentials.ReadAsync();

			if (stored.DeviceKey.Length == 0)
			{
				stored.DeviceKey = ANewDeviceKey();
				await credentials.StoreAsync(stored);
			}

			if (stored.Token.Length > 0) return stored.Token;

			return await AskForANewCredentialAsync(ct);
		}

		private async Task<string> RenewTheCredentialAsync(CancellationToken ct)
		{
			StoredCredential stored = await credentials.ReadAsync();
			if (stored.Refresh.Length > 0)
			{
				Credencial rotated = await new ProveedorDeToken(http).Rotar(stored.Refresh, ct);
				if (rotated is not null) return await StoreAsync(rotated, "credential rotated with the refresh token");
			}

			return await AskForANewCredentialAsync(ct);
		}

		private async Task<string> AskForANewCredentialAsync(CancellationToken ct)
		{
			StoredCredential stored = await credentials.ReadAsync();
			Credencial fresh = await new ProveedorDeToken(http)
				.Pedir(stored.DeviceKey, dealership.Resource, dealership.Vin, ct);

			return await StoreAsync(fresh, $"new credential (scope {fresh.Scope})");
		}

		// Guarda en el store, no en el journal: por eso ya no baja nada a disco.
		private async Task<string> StoreAsync(Credencial granted, string whatHappened)
		{
			StoredCredential stored = await credentials.ReadAsync();
			stored.Token = granted.AccessToken;
			stored.Refresh = granted.RefreshToken ?? "";
			await credentials.StoreAsync(stored);

			LastMessage = credentials.NotBackedUp
				? whatHappened + " (only in memory: the browser refused to store it; a new one will be asked for on reopen)"
				: whatHappened;
			return granted.AccessToken;
		}

		// La identidad de esta instalacion. Se genera una sola vez y se guarda en el store
		// (storage del browser), NO en el journal. Si el browser la desaloja, la app se presenta
		// ante el concesionario como un device nuevo y pide otra concesion; la vieja queda
		// huerfana hasta que el concesionario la revoque.
		private static string ANewDeviceKey() => "device-" + Guid.NewGuid().ToString("N")[..16];

		public async ValueTask<ResultadoDeRefresh> RefreshAsync(CancellationToken ct = default)
		{
			Require();

			await CredentialAsync(ct);

			ResultadoDeRefresh result = await loop.Ejecutar(ct);

			DealershipUnreachable = "";
			LastMessage = $"refresh: {result}";

			await FlushAfterTheBlockAsync();
			return result;
		}

		private async ValueTask<string> EvaluateNowAsync()
		{
			Require();

			DateTime asOf = AnInstantThatDoesNotGoBackwards();
			EvaluationOutcome outcome = api.Evaluate(asOf);

			if (!outcome.WasEvaluated)
			{
				EvaluationSkipped = api.HasBeenEvaluated()
					? $"This record was already evaluated up to {Instant(api.LastEvaluatedAt())}, which is "
					  + $"later than now ({Instant(asOf)}). The schedule below is as of that later instant, "
					  + "not as of today, and it cannot be re-evaluated backwards."
					: Unwrap(outcome.Rejection);
				LastMessage = $"evaluate: skipped. {Unwrap(outcome.Rejection)}";
				return EvaluationSkipped;
			}

			EvaluationSkipped = "";
			await FlushToDiskAsync();
			LastMessage = $"evaluate: {outcome.TransitionCount} "
			            + (outcome.TransitionCount == 1 ? "transition" : "transitions")
			            + (outcome.AnythingBecameDue ? ", something became due" : "")
			            + (outcome.AnythingBecameOverdue ? ", something became overdue" : "");
			return "";
		}

		private DateTime AnInstantThatDoesNotGoBackwards()
		{
			DateTime now = DateTime.UtcNow;
			now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);

			if (now < lastStampSent)
				throw new InvalidOperationException(
					$"This machine's clock went backwards: it is now {Instant(now)} and this app has "
					+ $"already sent a command stamped {Instant(lastStampSent)}. It will not send an "
					+ "instant that moves backwards -- a retroactive declaration fails INSIDE the "
					+ "command and leaves a permanent entry in the journal.");

			lastStampSent = now;
			return now;
		}

		public static readonly string[] TheNineKinds =
		{
			"Inspection", "FluidChange", "FilterChange", "WearPartReplacement", "TimedComponent",
			"SoftwareUpdate", "RecallRemedy", "Diagnostic", "Repair"
		};

		public static readonly string[] TheFourProfiles =
		{
			"Standard", "SevereDuty", "LowDistance", "Commercial"
		};

		public static readonly string[] TheFiveDeferralReasons =
		{
			"Cost", "Timing", "SecondOpinion", "NotConvinced", "WillDoElsewhere"
		};

		public string ReadVin() { Require(); return api.ReadVin(); }

		public string ReadDueState(string kind) { Require(); return api.ReadDueState(kind); }

		public string ReadDueReason(string kind) { Require(); return api.ReadDueReason(kind); }

		public int DeferralsUsed(string kind) { Require(); return api.DeferralsUsed(kind); }

		public int CountDue() { Require(); return api.CountDue(); }

		public int CountOverdue() { Require(); return api.CountOverdue(); }

		public int MissedCount() { Require(); return api.MissedCount(); }

		public bool HasAnythingDue() { Require(); return api.HasAnythingDue(); }

		public string NextDueKind() { Require(); return api.HasAnythingDue() ? api.ReadNextDueKind() : ""; }

		public bool HasBeenEvaluated() { Require(); return api.HasBeenEvaluated(); }

		public string EvaluatedUpTo()
		{
			Require();
			return api.HasBeenEvaluated() ? Instant(api.LastEvaluatedAt()) : "";
		}

		public long LastEventApplied() { Require(); return api.LastEventApplied(); }

		// Del store, no del journal. Sincrono: la pantalla lo pregunta al renderizar y el store
		// ya tiene la credencial en cache despues del primer ReadAsync.
		public bool HasCredential() => credentials.HasToken;

		public long JournalEntries() { Require(); return app.Actor.CurrentEntryId; }

		public (int files, long bytes) MemfsState() => JournalBridge.MemfsState();

		private static string Instant(DateTime instant) => instant.ToString("yyyy-MM-dd HH:mm:ss'Z'");

		private static string Unwrap(string rejection)
		{
			if (string.IsNullOrWhiteSpace(rejection)) return "";

			try
			{
				using JsonDocument document = JsonDocument.Parse(rejection);
				if (document.RootElement.TryGetProperty("EWI", out JsonElement ewi)
					&& ewi.ValueKind == JsonValueKind.Array
					&& ewi.GetArrayLength() > 0
					&& ewi[0].TryGetProperty("Error", out JsonElement error))
					return error.GetString() ?? rejection;
			}
			catch (JsonException)
			{
				return rejection;
			}

			return rejection;
		}

		private void Require()
		{
			if (api is null) throw new InvalidOperationException("This app has not opened yet.");
		}

		public async ValueTask DisposeAsync()
		{
			app?.Actor.GracefulExit();
			await bridge.DisposeAsync();
			await credentials.DisposeAsync();
			await gnothi.DisposeAsync();
		}
	}

	public sealed class DealershipSettings
	{
		public string BaseUrl { get; set; } = "eventos/";
		public string Resource { get; set; } = "vehicle";
		public string Vin { get; set; } = "1HGCM82633A004352";
	}
}
