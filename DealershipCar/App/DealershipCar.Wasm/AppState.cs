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
		private readonly HttpClient http;
		private readonly DealershipSettings dealership;

		private DealershipCarApp app;
		private DealershipCarAPI api;
		private BucleDeRefresh loop;

		private DateTime lastStampSent = DateTime.MinValue;

		public AppState(JournalBridge bridge, HttpClient http, DealershipSettings dealership)
		{
			ArgumentNullException.ThrowIfNull(bridge);
			ArgumentNullException.ThrowIfNull(http);
			ArgumentNullException.ThrowIfNull(dealership);

			this.bridge = bridge;
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

			int preloaded = bridge.HasFolder ? await bridge.PreloadIntoMemfsAsync() : 0;

			var started = new DealershipCarApp(dealership.Vin, TheProfileAtDelivery);
			started.ConfigureStorage(DatabaseType.FileSystem, "path=" + JournalBridge.RootInMemfs);
			started.Start();

			app = started;
			api = new DealershipCarAPI(started);

			loop = new BucleDeRefresh(
				api,
				new FuenteHttpDeEventos(http, dealership.Resource, dealership.Vin),
				DealershipEventRoute.Route,
				FlushToDiskAsync,
				ct => RenewTheCredentialAsync(ct));

			try { await FlushToDiskAsync(); } catch (WriteRefusedException) { }

			LastMessage = bridge.HasFolder
				? $"up. {preloaded} files preloaded from '{bridge.FolderName}'"
				: "up, but WITHOUT a folder: the journal lives only in memory and is lost on close";

			try
			{
				await CredentialAsync(CancellationToken.None);
				ResultadoDeRefresh onOpen = await loop.Ejecutar(CancellationToken.None);
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

		private async ValueTask<string> CredentialAsync(CancellationToken ct)
		{
			Require();

			if (api.StoredDeviceKey().Length == 0)
			{
				api.RegisterDevice(ANewDeviceKey());
				await FlushToDiskAsync();
			}

			string current = api.StoredToken();
			if (current.Length > 0) return current;

			return await AskForANewCredentialAsync(ct);
		}

		private async Task<string> RenewTheCredentialAsync(CancellationToken ct)
		{
			Require();

			string refresh = api.StoredRefreshToken();
			if (refresh.Length > 0)
			{
				Credencial rotated = await new ProveedorDeToken(http).Rotar(refresh, ct);
				if (rotated is not null) return await StoreAsync(rotated, "credential rotated with the refresh token");
			}

			return await AskForANewCredentialAsync(ct);
		}

		private async Task<string> AskForANewCredentialAsync(CancellationToken ct)
		{
			Credencial fresh = await new ProveedorDeToken(http)
				.Pedir(api.StoredDeviceKey(), dealership.Resource, dealership.Vin, ct);

			return await StoreAsync(fresh, $"new credential (scope {fresh.Scope})");
		}

		private async Task<string> StoreAsync(Credencial credential, string whatHappened)
		{
			api.StoreCredential(credential.AccessToken, credential.RefreshToken);
			await FlushToDiskAsync();
			LastMessage = whatHappened;
			return credential.AccessToken;
		}

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

		public bool HasCredential() { Require(); return api.StoredToken().Length > 0; }

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
		}
	}

	public sealed class DealershipSettings
	{
		public string BaseUrl { get; set; } = "eventos/";
		public string Resource { get; set; } = "vehicle";
		public string Vin { get; set; } = "1HGCM82633A004352";
	}
}
