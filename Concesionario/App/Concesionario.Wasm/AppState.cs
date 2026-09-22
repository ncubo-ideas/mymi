using System.Text.Json;
using Choreography.Theater;
using Puppeteer;
using Concesionario.App;
using Concesionario.App.Events;
using Concesionario.App.Views;
using Puppets.Portador;

namespace Concesionario.Wasm
{
	public sealed class AppState : IAsyncDisposable
	{
		public const string TheProfileAtDelivery = "Standard";

		public const string TheModelAtDelivery = "YARIS-2019";

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

		/// <summary>
		/// Que no se pudo llegar al concesionario, como CLAVE MAS VALORES y no como frase: esta capa no
		/// elige un idioma, y la pantalla que lo dice tiene el catalogo.
		/// </summary>
		public Note DealershipUnreachable { get; private set; } = Note.None;

		/// <summary>
		/// Por que no se evaluo, del mismo modo. Cuando el rechazo lo escribio el dominio va como CITA:
		/// esas palabras son suyas y se muestran tal como llegaron.
		/// </summary>
		public Note EvaluationSkipped { get; private set; } = Note.None;

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

			int preloaded = bridge.HasDestination ? await bridge.PreloadIntoMemfsAsync() : 0;

			var started = new DealershipCarApp(
				dealership.Vin, TheProfileAtDelivery, TheModelAtDelivery);
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
				? $"arriba, hospedada en Mymi. {preloaded} archivos precargados"
				: bridge.HasDestination
					? $"arriba. {preloaded} archivos precargados de '{bridge.FolderName}'"
					: "arriba, pero SIN carpeta: el journal vive solo en memoria y se pierde al cerrar";

			try
			{
				// LA CREDENCIAL SOLO EN EL MODO SUELTO. Hospedada la sostiene Gnothi del otro
				// lado del puente y este modulo no ve ningun token: pedirla aca lo mandaria a
				// hablar con el publicador, que es justo lo que la CSP le niega.
				if (!mode.Hosted) await CredentialAsync(CancellationToken.None);

				ResultadoDeRefresh onOpen = await loop.Ejecutar(CancellationToken.None);
				if (onOpen.Cuenta is not null) Account = onOpen.Cuenta;
				await FlushAfterTheBlockAsync();
				DealershipUnreachable = Note.None;
				LastMessage += $" | al abrir: {onOpen}";
			}
			catch (WriteRefusedException)
			{
				LastMessage += " | el concesionario contestó, pero la carpeta rechazó la escritura";
			}
			catch (Exception ex)
			{
				DealershipUnreachable = Note.Of("state.unreachable",
					("where", WhereTheDealershipIs),
					("kind", ex.GetType().Name),
					("detail", ex.Message));
				LastMessage += $" | no se pudo alcanzar {WhereTheDealershipIs} ({ex.GetType().Name})";
			}

			try
			{
				await EvaluateNowAsync();
			}
			catch (Exception ex)
			{
				LastMessage += $" | no se pudo evaluar ({ex.GetType().Name}: {ex.Message})";
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

		public string WhereItLivesNow => Account?.Host is { Length: > 0 } where ? where : "Mymi";

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
			LastMessage = $"delivery from Mymi: {result}";
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
				if (rotated is not null) return await StoreAsync(rotated, "credencial rotada con el refresh token");
			}

			return await AskForANewCredentialAsync(ct);
		}

		private async Task<string> AskForANewCredentialAsync(CancellationToken ct)
		{
			StoredCredential stored = await credentials.ReadAsync();
			Credencial fresh = await new ProveedorDeToken(http)
				.Pedir(stored.DeviceKey, dealership.Resource, dealership.Vin, ct);

			return await StoreAsync(fresh, $"credencial nueva (alcance {fresh.Scope})");
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

			DealershipUnreachable = Note.None;
			LastMessage = $"actualización: {result}";

			await FlushAfterTheBlockAsync();
			return result;
		}

		private async ValueTask EvaluateNowAsync()
		{
			Require();

			DateTime asOf = AnInstantThatDoesNotGoBackwards();
			EvaluationOutcome outcome = api.Evaluate(asOf);

			if (!outcome.WasEvaluated)
			{
				EvaluationSkipped = api.HasBeenEvaluated()
					? Note.Of("state.alreadyEvaluated",
						("when", Instant(api.LastEvaluatedAt())),
						("now", Instant(asOf)))
					: Note.AsItArrived(Unwrap(outcome.Rejection));
				LastMessage = $"evaluación: omitida. {Unwrap(outcome.Rejection)}";
				return;
			}

			EvaluationSkipped = Note.None;
			await FlushToDiskAsync();
			LastMessage = $"evaluación: {outcome.TransitionCount} "
			            + (outcome.TransitionCount == 1 ? "transición" : "transiciones")
			            + (outcome.AnythingBecameDue ? ", algo venció" : "")
			            + (outcome.AnythingBecameOverdue ? ", algo se atrasó" : "");
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

		// THE CLOSED SETS MOVED to Concesionario.App.Views.TheClosedSets, and these names stay so
		// that every .razor that already said AppState.TheNineKinds keeps working.
		//
		// They moved because they are domain vocabulary, not screen state: a kind of service, a
		// fluid, a tyre position. Living here they were unreachable from anything that is not this
		// Blazor app -- and a cargo or hybrid module ships Concesionario.App.dll and never this
		// assembly, so the projections that walk these lists could not travel with it.
		public static readonly string[] TheNineKinds = TheClosedSets.TheNineKinds;
		public static readonly string[] TheFourProfiles = TheClosedSets.TheFourProfiles;
		public static readonly string[] TheFiveDeferralReasons = TheClosedSets.TheFiveDeferralReasons;
		public static readonly string[] TheSixFluidKinds = TheClosedSets.TheSixFluidKinds;
		public static readonly string[] TheThreeTyrePositions = TheClosedSets.TheThreeTyrePositions;

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

		public string ReadDueReason(string kind, string language)
		{
			Require();
			return api.ReadDueReason(kind, language);
		}

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

		// Del store, no del journal. Sincrono: la pantalla lo pregunta al renderizar y el store
		// ya tiene la credencial en cache despues del primer ReadAsync.
		public bool HasCredential() => credentials.HasToken;

		/// <summary>
		/// La ficha del fabricante entera, leida en un solo momento para que las dos secciones que
		/// la muestran nombren la misma revision de la que salieron sus frases.
		/// </summary>
		public WhatTheManufacturerPublished ReadTheSpecification(string language)
		{
			Require();
			return WhatTheManufacturerPublished.ReadThrough(api, language);
		}

		/// <summary>
		/// Donde esta parado este vehiculo contra su sobre, leido entero al instante en que la pantalla
		/// pregunta. Va aparte de la ficha porque la ficha es de un modelo y esto es de un carro, y
		/// porque solo esta mitad depende del instante.
		/// </summary>
		public WhereThisVehicleStands ReadWhereThisVehicleStands(string language)
		{
			Require();
			return WhereThisVehicleStands.ReadThrough(
				api, TheInstantTheScreenAsksAt(), language);
		}

		/// <summary>
		/// Las estadias en el taller de este vehiculo, leidas enteras al instante en que la pantalla
		/// pregunta: el ano de calendario corrido hasta ese dia, y la estadia que cubre ese instante.
		/// La ventana la fija la pantalla con su propio reloj y no se le pide a nadie -- esta app no
		/// tiene ningun control que recoja un valor de una persona, asi que no hay selector de periodo.
		/// </summary>
		public TheStaysOfThisVehicle ReadTheStaysOfThisVehicle(string language)
		{
			Require();

			DateTime asOf = TheInstantTheScreenAsksAt();

			return TheStaysOfThisVehicle.ReadThrough(
				api, TheFirstDayOfTheYearOf(asOf), TheDay(asOf), asOf, language);
		}

		/// <summary>
		/// Donde esta parada cada operacion de la agenda, leido entero al instante en que la pantalla
		/// pregunta: lo que le falta a cada una y lo que ya se le hizo. El instante lo pone el reloj de
		/// la maquina y no se le pide a nadie -- pedirlo seria dejar que alguien pregunte por un momento
		/// que se invento.
		/// </summary>
		public WhereEachOperationStands ReadWhereEachOperationStands(string language)
		{
			Require();
			return WhereEachOperationStands.ReadThrough(
				api, TheInstantTheScreenAsksAt(), language);
		}

		/// <summary>
		/// Lo que el fabricante publico que cuestan los servicios, leido entero al instante en que la
		/// pantalla pregunta, porque la vigencia de un precio se juzga contra un momento.
		/// </summary>
		public WhatTheServicesCost ReadWhatTheServicesCost(string language)
		{
			Require();
			return WhatTheServicesCost.ReadThrough(api, TheInstantTheScreenAsksAt(), language);
		}

		/// <summary>
		/// Cuanto se maneja este vehiculo, leido entero al instante en que la pantalla pregunta: los
		/// doce meses que terminan en ese instante y los doce anteriores. Los cuatro limites salen de
		/// UNA sola lectura del reloj, asi que las dos ventanas son contiguas y del mismo momento. La
		/// ventana la fija la pantalla y no se le pide a nadie -- esta app no tiene ningun control que
		/// recoja un valor de una persona, asi que no hay selector de periodo.
		/// </summary>
		public HowThisVehicleIsDriven ReadHowThisVehicleIsDriven(string language)
		{
			Require();

			DateTime asOf = TheInstantTheScreenAsksAt();

			return HowThisVehicleIsDriven.ReadThrough(
				api, TheTwelveMonthsBefore(asOf), asOf, TheTwentyFourMonthsBefore(asOf),
				TheTwelveMonthsBefore(asOf), language);
		}

		/// <summary>
		/// En que se le fue la plata a este vehiculo, leido entero al instante en que la pantalla
		/// pregunta: los doce meses que terminan en ese instante y los doce anteriores. Los cuatro
		/// limites salen de UNA sola lectura del reloj, asi que las dos ventanas son contiguas y del
		/// mismo momento. La ventana la fija la pantalla y no se le pide a nadie -- esta app no tiene
		/// ningun control que recoja un valor de una persona, asi que no hay selector de periodo.
		/// </summary>
		public WhatThisVehicleHasCost ReadWhatThisVehicleHasCost(string language)
		{
			Require();

			DateTime asOf = TheInstantTheScreenAsksAt();

			return WhatThisVehicleHasCost.ReadThrough(
				api, TheTwelveMonthsBefore(asOf), asOf, TheTwentyFourMonthsBefore(asOf),
				TheTwelveMonthsBefore(asOf), language);
		}

		/// <summary>
		/// La placa de este vehiculo, leida al instante en que la pantalla pregunta, porque cual placa
		/// rige se decide contra un momento. El instante lo lee la pantalla del reloj de la maquina y
		/// no se lo pide a nadie: la placa la publica el concesionario y esta app solo la muestra.
		/// </summary>
		public ThePlateOfThisVehicle ReadThePlateOfThisVehicle(string language)
		{
			Require();
			return ThePlateOfThisVehicle.ReadThrough(api, TheInstantTheScreenAsksAt(), language);
		}

		// HasCredential() vive mas arriba y pregunta al ALMACEN, no al journal: el api ya no
		// tiene StoredToken() desde que la credencial salio del diario (2026-09-03).

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
			await credentials.DisposeAsync();
			await gnothi.DisposeAsync();
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
		public string Name { get; set; } = "Sotaventos Motors";

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
