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

		public const string TheModelAtDelivery = "SM-SEDAN-2025";

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

		public string WhereTheDealershipIs => http.BaseAddress?.ToString() ?? "(sin configurar)";

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
			LastMessage = $"recuperar carpeta: {result}";
			return result;
		}

		public async ValueTask<FolderResult> ConnectFolderAsync()
		{
			FolderResult result = await bridge.ChooseFolderAsync();
			Mode = await bridge.CurrentModeAsync();
			LastMessage = $"conectar carpeta: {result}";

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
			LastMessage = $"pedir permiso: {result}";

			if (result.Ok) ReleaseTheActor();
			return result;
		}

		public async ValueTask ForgetTheFolderAsync()
		{
			await bridge.ForgetFolderAsync();
			Mode = await bridge.CurrentModeAsync();
			LastMessage = "se olvidó la carpeta guardada; no se tocó nada de lo que hay adentro";
		}

		public async ValueTask<FolderResult> UseBrowserStorageAsync()
		{
			FolderResult result = await bridge.UseBrowserStorageAsync();
			Mode = await bridge.CurrentModeAsync();
			LastMessage = $"almacenamiento del navegador: {result}";

			if (result.Ok) ReleaseTheActor();
			return result;
		}

		public async ValueTask StartAsync()
		{
			if (app is not null) return;

			int preloaded = bridge.HasFolder ? await bridge.PreloadIntoMemfsAsync() : 0;

			var started = new DealershipCarApp(
				dealership.Vin, TheProfileAtDelivery, TheModelAtDelivery);
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
				? $"arriba. {preloaded} archivos precargados de '{bridge.FolderName}'"
				: "arriba, pero SIN carpeta: el journal vive solo en memoria y se pierde al cerrar";

			try
			{
				await CredentialAsync(CancellationToken.None);
				ResultadoDeRefresh onOpen = await loop.Ejecutar(CancellationToken.None);
				await FlushAfterTheBlockAsync();
				DealershipUnreachable = "";
				LastMessage += $" | al abrir: {onOpen}";
			}
			catch (WriteRefusedException)
			{
				LastMessage += " | el concesionario contestó, pero la carpeta rechazó la escritura";
			}
			catch (Exception ex)
			{
				DealershipUnreachable =
					$"No se pudo alcanzar {WhereTheDealershipIs} ({ex.GetType().Name}: {ex.Message}).";
				LastMessage += " | " + DealershipUnreachable;
			}

			try
			{
				await EvaluateNowAsync();
			}
			catch (Exception ex)
			{
				LastMessage += $" | no se pudo evaluar ({ex.GetType().Name}: {ex.Message})";
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
				if (rotated is not null) return await StoreAsync(rotated, "credencial rotada con el refresh token");
			}

			return await AskForANewCredentialAsync(ct);
		}

		private async Task<string> AskForANewCredentialAsync(CancellationToken ct)
		{
			Credencial fresh = await new ProveedorDeToken(http)
				.Pedir(api.StoredDeviceKey(), dealership.Resource, dealership.Vin, ct);

			return await StoreAsync(fresh, $"credencial nueva (alcance {fresh.Scope})");
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
			LastMessage = $"actualización: {result}";

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
					? $"Este registro ya se evaluó hasta el {Instant(api.LastEvaluatedAt())}, que es "
					  + $"posterior a ahora ({Instant(asOf)}). El plan de abajo está a ese instante "
					  + "posterior, no al de hoy, y no se puede volver a evaluar hacia atrás."
					: Unwrap(outcome.Rejection);
				LastMessage = $"evaluación: omitida. {Unwrap(outcome.Rejection)}";
				return EvaluationSkipped;
			}

			EvaluationSkipped = "";
			await FlushToDiskAsync();
			LastMessage = $"evaluación: {outcome.TransitionCount} "
			            + (outcome.TransitionCount == 1 ? "transición" : "transiciones")
			            + (outcome.AnythingBecameDue ? ", algo venció" : "")
			            + (outcome.AnythingBecameOverdue ? ", algo se atrasó" : "");
			return "";
		}

		/// <summary>
		/// El instante que esta app le manda a un COMANDO. Es monotono dentro de la sesion, y lo es
		/// porque un instante hacia atras falla ADENTRO del cuerpo del comando, cuando la entrada ya
		/// esta escrita: queda una entrada permanente que loguea un fallo de rehidratacion en cada
		/// arranque futuro. Es de instancia porque la monotonia necesita el estado de la sesion.
		/// </summary>
		private DateTime AnInstantThatDoesNotGoBackwards()
		{
			DateTime now = TheWholeSecondItIsNow();

			if (now < lastStampSent)
				throw new InvalidOperationException(
					$"El reloj de esta máquina fue para atrás: ahora son las {Instant(now)} y esta "
					+ $"app ya mando un comando marcado {Instant(lastStampSent)}. No va a mandar un "
					+ "instante que va hacia atrás -- una declaración retroactiva falla ADENTRO del "
					+ "comando y deja una entrada permanente en el journal.");

			lastStampSent = now;
			return now;
		}

		/// <summary>
		/// El instante en que la pantalla PREGUNTA. Es argumento de una LECTURA, asi que deliberadamente
		/// NO es monotono: una pregunta no escribe ninguna entrada, no hay nada que marcar, y una guarda
		/// de monotonia solo rechazaria una segunda pregunta legitima un segundo despues. Es estatico
		/// justamente por eso: sin estado de sesion que consultar, no puede volverse monotono por
		/// descuido. Trunca al segundo entero como el otro, porque el journal ahi trunca.
		/// </summary>
		private static DateTime TheInstantTheScreenAsksAt()
		{
			return TheWholeSecondItIsNow();
		}

		private static DateTime TheWholeSecondItIsNow()
		{
			DateTime now = DateTime.UtcNow;

			return new DateTime(
				now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);
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

		public static readonly string[] TheSixFluidKinds =
		{
			"EngineOil", "BrakeFluid", "Coolant", "TransmissionFluid", "PowerSteeringFluid",
			"WasherFluid"
		};

		public static readonly string[] TheThreeTyrePositions =
		{
			"Front", "Rear", "Spare"
		};

		/// <summary>
		/// Un nombre del canon escrito como lo lee una persona: se separa en cada mayuscula y solo
		/// la primera queda en alta. Es etiqueta, no identidad -- lo que cruza la puerta sigue
		/// siendo el nombre tal cual.
		/// </summary>
		public static string Spaced(string name)
		{
			if (string.IsNullOrEmpty(name)) return name;

			var spaced = new System.Text.StringBuilder(name.Length + 4);
			spaced.Append(name[0]);
			for (int i = 1; i < name.Length; i++)
			{
				if (char.IsUpper(name[i])) spaced.Append(' ').Append(char.ToLowerInvariant(name[i]));
				else spaced.Append(name[i]);
			}
			return spaced.ToString();
		}

		public string ReadVin() { Require(); return api.ReadVin(); }

		public string ReadDueState(string kind) { Require(); return api.ReadDueState(kind); }

		public string ReadDueReason(string kind) { Require(); return api.ReadDueReason(kind); }

		public int DeferralsUsed(string kind) { Require(); return api.DeferralsUsed(kind); }

		public int CountDue() { Require(); return api.CountDue(); }

		public int CountOverdue() { Require(); return api.CountOverdue(); }

		public int CountUpToDate() { Require(); return api.CountUpToDate(); }

		public int CountScheduled() { Require(); return api.CountScheduled(); }

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

		/// <summary>
		/// La ficha del fabricante entera, leida en un solo momento para que las dos secciones que
		/// la muestran nombren la misma revision de la que salieron sus frases.
		/// </summary>
		public WhatTheManufacturerPublished ReadTheSpecification()
		{
			Require();
			return WhatTheManufacturerPublished.ReadThrough(api);
		}

		/// <summary>
		/// Donde esta parado este vehiculo contra su sobre, leido entero al instante en que la pantalla
		/// pregunta. Va aparte de la ficha porque la ficha es de un modelo y esto es de un carro, y
		/// porque solo esta mitad depende del instante.
		/// </summary>
		public WhereThisVehicleStands ReadWhereThisVehicleStands()
		{
			Require();
			return WhereThisVehicleStands.ReadThrough(api, TheInstantTheScreenAsksAt());
		}

		/// <summary>
		/// Las estadias en el taller de este vehiculo, leidas enteras al instante en que la pantalla
		/// pregunta: el ano de calendario corrido hasta ese dia, y la estadia que cubre ese instante.
		/// La ventana la fija la pantalla con su propio reloj y no se le pide a nadie -- esta app no
		/// tiene ningun control que recoja un valor de una persona, asi que no hay selector de periodo.
		/// </summary>
		public TheStaysOfThisVehicle ReadTheStaysOfThisVehicle()
		{
			Require();

			DateTime asOf = TheInstantTheScreenAsksAt();

			return TheStaysOfThisVehicle.ReadThrough(
				api, TheFirstDayOfTheYearOf(asOf), TheDay(asOf), asOf);
		}

		/// <summary>
		/// Donde esta parada cada operacion de la agenda, leido entero al instante en que la pantalla
		/// pregunta: lo que le falta a cada una y lo que ya se le hizo. El instante lo pone el reloj de
		/// la maquina y no se le pide a nadie -- pedirlo seria dejar que alguien pregunte por un momento
		/// que se invento.
		/// </summary>
		public WhereEachOperationStands ReadWhereEachOperationStands()
		{
			Require();
			return WhereEachOperationStands.ReadThrough(api, TheInstantTheScreenAsksAt());
		}

		/// <summary>
		/// Lo que el fabricante publico que cuestan los servicios, leido entero al instante en que la
		/// pantalla pregunta, porque la vigencia de un precio se juzga contra un momento.
		/// </summary>
		public WhatTheServicesCost ReadWhatTheServicesCost()
		{
			Require();
			return WhatTheServicesCost.ReadThrough(api, TheInstantTheScreenAsksAt());
		}

		/// <summary>
		/// Cuanto se maneja este vehiculo, leido entero al instante en que la pantalla pregunta: los
		/// doce meses que terminan en ese instante y los doce anteriores. Los cuatro limites salen de
		/// UNA sola lectura del reloj, asi que las dos ventanas son contiguas y del mismo momento. La
		/// ventana la fija la pantalla y no se le pide a nadie -- esta app no tiene ningun control que
		/// recoja un valor de una persona, asi que no hay selector de periodo.
		/// </summary>
		public HowThisVehicleIsDriven ReadHowThisVehicleIsDriven()
		{
			Require();

			DateTime asOf = TheInstantTheScreenAsksAt();

			return HowThisVehicleIsDriven.ReadThrough(
				api, TheTwelveMonthsBefore(asOf), asOf, TheTwentyFourMonthsBefore(asOf),
				TheTwelveMonthsBefore(asOf));
		}

		/// <summary>
		/// En que se le fue la plata a este vehiculo, leido entero al instante en que la pantalla
		/// pregunta: los doce meses que terminan en ese instante y los doce anteriores. Los cuatro
		/// limites salen de UNA sola lectura del reloj, asi que las dos ventanas son contiguas y del
		/// mismo momento. La ventana la fija la pantalla y no se le pide a nadie -- esta app no tiene
		/// ningun control que recoja un valor de una persona, asi que no hay selector de periodo.
		/// </summary>
		public WhatThisVehicleHasCost ReadWhatThisVehicleHasCost()
		{
			Require();

			DateTime asOf = TheInstantTheScreenAsksAt();

			return WhatThisVehicleHasCost.ReadThrough(
				api, TheTwelveMonthsBefore(asOf), asOf, TheTwentyFourMonthsBefore(asOf),
				TheTwelveMonthsBefore(asOf));
		}

		public bool HasCredential() { Require(); return api.StoredToken().Length > 0; }

		public long JournalEntries() { Require(); return app.Actor.CurrentEntryId; }

		public (int files, long bytes) MemfsState() => JournalBridge.MemfsState();

		private static string Instant(DateTime instant) => instant.ToString("yyyy-MM-dd HH:mm:ss'Z'");

		/// <summary>Ese instante dicho como el dia de calendario en que cae.</summary>
		private static string TheDay(DateTime instant) =>
			instant.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

		/// <summary>
		/// El primero de enero del ano de ese instante. Es el comienzo de la ventana fija que el
		/// bloque de estadias muestra, armado con los componentes del instante y no calculado a partir
		/// de ningun valor del dominio.
		/// </summary>
		private static string TheFirstDayOfTheYearOf(DateTime instant) =>
			TheDay(new DateTime(instant.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc));

		/// <summary>
		/// El mismo instante doce meses de calendario antes. Es el comienzo de la ventana que el
		/// bloque del ritmo muestra, derivado del instante en que la pantalla pregunta y de ningun
		/// valor del dominio.
		/// </summary>
		private static DateTime TheTwelveMonthsBefore(DateTime instant) => instant.AddYears(-1);

		/// <summary>
		/// El mismo instante veinticuatro meses de calendario antes. Es el comienzo de la ventana
		/// anterior, con la que la de arriba se compara.
		/// </summary>
		private static DateTime TheTwentyFourMonthsBefore(DateTime instant) => instant.AddYears(-2);

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
			if (api is null) throw new InvalidOperationException("Esta app todavía no se ha abierto.");
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

		/// <summary>
		/// Como se llama la agencia en la pantalla. NO sale del registro y no tiene por que: la
		/// agencia es quien reparte esta app, asi que su propio nombre lo trae la app y no un
		/// evento del feed.
		/// </summary>
		public string Name { get; set; } = "Sotavento Motors";

		/// <summary>
		/// Por donde se le habla a la agencia. Vale la misma razon que <see cref="Name"/>: es un
		/// dato DE LA AGENCIA, no del carro de nadie, y por eso viaja en el appsettings de la app
		/// que la agencia publica en vez de entrar por el feed del vehiculo.
		///
		/// Son ENLACES y nada mas -- abren WhatsApp o el marcador del telefono. Esta app no le
		/// manda nada a la agencia ni escribe en el registro por tocarlos: lo unico que hace es
		/// leer. Sin configurar, los botones no se dibujan.
		/// </summary>
		public DealershipContact Contact { get; set; } = new();
	}

	/// <summary>Los dos enlaces con que se llega a la agencia, o vacios si no los publico.</summary>
	public sealed class DealershipContact
	{
		/// <summary>Numero en formato internacional sin signos, como lo pide wa.me. Ej: 50622000000.</summary>
		public string Whatsapp { get; set; } = "";

		/// <summary>Numero tal cual se marca. Ej: +506 2200 0000.</summary>
		public string Phone { get; set; } = "";
	}
}
