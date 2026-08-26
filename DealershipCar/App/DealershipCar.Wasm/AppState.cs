using System.Text.Json;
using Choreography.Theater;
using Puppeteer;
using DealershipCar.App;
using DealershipCar.App.Events;
using Puppets.Portador;

namespace DealershipCar.Wasm
{
	// EL ARRANQUE DE LA APP INSTALADA, en el orden que impone el mismatch sincrono/asincrono del
	// browser (ARCHITECTURE §4.1):
	//
	//   1. la persona otorga la carpeta                       (gesto, asincrono)
	//   2. carpeta -> MEMFS                                   (asincrono, ANTES de que exista el actor)
	//   3. ConfigureStorage(FileSystem, /journal) + Start()
	//        -> el replay rehidrata leyendo MEMFS, todo SINCRONO
	//   4. cada comando escribe en MEMFS y retorna ya
	//   5. flush MEMFS -> carpeta despues de CADA comando
	//
	// EL PASO 2 TIENE QUE PASAR ANTES DEL 3 porque el motor lee el journal de forma sincrona y en
	// wasm single-thread no se puede bloquear esperando una promesa.
	//
	// VENTANA DE PERDIDA, y ESTE es el lugar donde se dice: entre el commit del actor y el flush a
	// disco hay un instante en el que un cierre abrupto pierde entradas. El flush es por comando (no
	// por timer) para que sea lo mas angosta posible, pero NO es durabilidad transaccional y no se
	// presenta como tal.
	//
	// LA PANTALLA YA NO LA IMPRIME. La caratula llevaba este mismo parrafo al pie, y se saco: es
	// vocabulario de ingenieria sobre el que la persona no puede actuar, y el hecho que si le
	// importa -- que se guarda despues de CADA cambio, no cada tantos segundos -- lo dice la linea
	// de in-own-folder. El dato no se perdio, cambio de lector: aca lo lee quien puede hacer algo
	// con el, que es quien toque este archivo.
	//
	// ESTA ES LA UNICA CARATULA QUE LEE UN RELOJ, y hay que decirlo porque el repo tiene un grep
	// mecanico que lo busca (C27). El DOMINIO nunca lo lee -- cada operacion dependiente del tiempo
	// recibe el instante como parametro -- y la consola de T7 tampoco, a proposito, porque su
	// historia tiene que ser reproducible. Una app de verdad no puede: la persona la abre HOY y
	// quiere su agenda evaluada a hoy. El reloj se lee ACA, en un solo lugar, y el instante viaja
	// como parametro igual que en la consola.
	public sealed class AppState : IAsyncDisposable
	{
		// El perfil con el que nace la agenda si el journal esta vacio. Es el argumento del
		// constructor del agregado, no un DeclareUsageProfile: sobre un journal ya sembrado el
		// upgrade no hace nada y manda el que quedo journalizado.
		// PUBLIC porque una prueba lo fija contra el conjunto cerrado del dominio: cruza la puerta en
		// cada apertura de un journal vacio, y un nombre mal escrito aca deja la app sin abrir.
		public const string TheProfileAtDelivery = "Standard";

		private readonly JournalBridge bridge;
		private readonly HttpClient http;
		private readonly DealershipSettings dealership;

		private DealershipCarApp app;
		private DealershipCarAPI api;
		private BucleDeRefresh loop;

		// EL SELLO MONOTONO DE ESTA CARATULA. No es una ley del dominio: es la caratula negandose a
		// desordenar sus propios comandos. Si el reloj de la maquina retrocede entre dos acciones,
		// mandar el instante nuevo haria que un DeclareUsageProfile retroactivo reventara ADENTRO
		// del comando -- una de las cuatro guardas de C22 -- y esa entrada queda en el journal para
		// siempre. Preferimos negarnos y decirlo.
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

		// El destino tiene que ser VISIBLE, no un detalle interno. En 'opfs' el journal sobrevive al
		// cierre pero NO lo puede leer el servidor MCP -- vive dentro del sandbox del browser -- y
		// callarlo seria vender la mitad del producto que ahi no funciona.
		public string Mode { get; private set; } = "ninguno";

		public bool InOwnFolder => Mode == "carpeta";

		public bool InBrowserStorage => Mode == "opfs";

		// Cuando la carpeta rechaza una escritura, lo que se ve en pantalla deja de estar respaldado.
		// La app tiene que DECIRLO, porque el estado de la carpeta es el que sobrevive: al reabrir,
		// la precarga vuelve a partir de ahi. Y mientras tanto lo que muestra la pantalla y lo que
		// lee el servidor MCP no coinciden.
		public string SavingIsFailing { get; private set; } = "";

		// NO ALCANZAR AL CONCESIONARIO TIENE SU PROPIO CAMPO, y no es adorno: abrir no falla y la
		// agenda se muestra completa, asi que sin decirlo la pantalla es INDISTINGUIBLE de una que
		// si se puso al dia -- y lo que la persona esta mirando puede tener meses. Medido: iba
		// dentro de LastMessage y la evaluacion de apertura, que corre despues, lo pisaba; el aviso
		// existia y no llegaba a la pantalla nunca.
		public string DealershipUnreachable { get; private set; } = "";

		// LA EVALUACION RECHAZADA POR CADENCIA TAMBIEN SE DICE. Un journal evaluado hasta un
		// instante POSTERIOR al reloj -- el que deja la consola de T7, que salta seis meses -- hace
		// que la evaluacion de apertura se rechace bajo el read lock (C6), correctamente y sin
		// escribir nada. Pero entonces el tablero muestra el estado de ESE instante futuro, no de
		// hoy, y sin decirlo la persona lee una agenda vieja creyendola de hoy. Se descubrio
		// abriendo un journal real.
		public string EvaluationSkipped { get; private set; } = "";

		public string WhereTheDealershipIs => http.BaseAddress?.ToString() ?? "(not configured)";

		public string Vin => dealership.Vin;

		// ---------------------------------------------------------------- el destino

		public ValueTask<bool> PickerIsSupportedAsync() => bridge.PickerIsSupportedAsync();

		public ValueTask<bool> BrowserStorageIsSupportedAsync() => bridge.BrowserStorageIsSupportedAsync();

		public ValueTask<string> WhyThereIsNoStorageAsync() => bridge.WhyThereIsNoStorageAsync();

		public ValueTask<bool> RunsAsInstalledAppAsync() => bridge.RunsAsInstalledAppAsync();

		public ValueTask<string> RememberedModeAsync() => bridge.RememberedModeAsync();

		public ValueTask<string> CurrentPermissionAsync() => bridge.CurrentPermissionAsync();

		// Sin gesto: solo revalida el permiso de un handle ya guardado en IndexedDB.
		public async ValueTask<FolderResult> RecoverFolderAsync()
		{
			FolderResult result = await bridge.RecoverFolderAsync();
			Mode = await bridge.CurrentModeAsync();
			LastMessage = $"recover folder: {result}";
			return result;
		}

		// GESTO DE USUARIO. Desde un click, nunca al inicializar.
		//
		// ELEGIR DESTINO CON EL ACTOR YA ARRIBA TIENE QUE SOLTAR EL ACTOR, y no hacerlo era el
		// defecto mas peligroso de esta caratula. `StartAsync` sale por `if (app is not null)`, asi
		// que conectar una carpeta despues de abrir dejaba el actor de ANTES -- construido sobre
		// otro journal, o sobre ninguno -- apuntando a la carpeta nueva, y el primer flush habria
		// escrito ese journal ENCIMA del de la persona. Es lo que casi paso sobre
		// ~/DealershipCar-journal: se salvo por accidente, porque el permiso estaba denegado.
		//
		// El motor rehidrata en ConfigureStorage, asi que escribir MEMFS con un actor vivo no
		// cambiaria lo que se ve: hay que soltarlo y volver a abrir. Es la misma razon por la que
		// la implementacion de referencia rearranca el actor al importar un expediente.
		public async ValueTask<FolderResult> ConnectFolderAsync()
		{
			FolderResult result = await bridge.ChooseFolderAsync();
			Mode = await bridge.CurrentModeAsync();
			LastMessage = $"connect folder: {result}";

			if (result.Ok) ReleaseTheActor();
			return result;
		}

		// Suelta el actor y BORRA MEMFS, para que la proxima apertura precargue el destino nuevo
		// sobre una raiz vacia. Dejar MEMFS con el journal anterior mezclaria dos vehiculos.
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

		// GESTO DE USUARIO. Hace falta tras recargar cuando el permiso quedo en 'prompt'.
		public async ValueTask<FolderResult> RequestPermissionAsync()
		{
			FolderResult result = await bridge.RequestPermissionAsync();
			LastMessage = $"request permission: {result}";

			// Si la app ya estaba arriba cuando se otorgo el permiso, el actor NO salio de esta
			// carpeta -- se abrio sin ella, porque el permiso estaba denegado. Misma razon que en
			// ConnectFolderAsync.
			if (result.Ok) ReleaseTheActor();
			return result;
		}

		// LA SALIDA CUANDO EL BROWSER NO CEDE. Si el permiso sobre esa carpeta quedo bloqueado para
		// este origen, ni "dar permiso" ni "elegir carpeta" lo recuperan -- el picker vuelve a la
		// misma carpeta y al mismo no. Olvidar el handle es lo unico que deja empezar de nuevo con
		// otra carpeta, y sin esto la app quedaba ofreciendo dos botones que no podian funcionar.
		//
		// NO TOCA LA CARPETA NI EL JOURNAL: borra el handle guardado en IndexedDB, nada mas.
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

		// ---------------------------------------------------------------- arranque

		public async ValueTask StartAsync()
		{
			if (app is not null) return;

			// PASO 2, y va antes del 3. Sin carpeta se abre igual: el journal vive en MEMFS y se
			// pierde al cerrar, que es lo que la pantalla dice en letras grandes.
			int preloaded = bridge.HasFolder ? await bridge.PreloadIntoMemfsAsync() : 0;

			// PASO 3. El path apunta a MEMFS y no a la carpeta: la carpeta ya esta copiada adentro.
			var started = new DealershipCarApp(dealership.Vin, TheProfileAtDelivery);
			started.ConfigureStorage(DatabaseType.FileSystem, "path=" + JournalBridge.RootInMemfs);
			started.Start();

			app = started;
			api = new DealershipCarAPI(started);

			// EL FLUSH DEJA DE SER null, y es la unica diferencia de armado con la consola de T8.
			// Alla el journal ya esta en el disco cuando el comando retorna, asi que no hay nada
			// que bajar; aca el flush ES lo que baja MEMFS a la carpeta de la persona.
			loop = new BucleDeRefresh(
				api,
				new FuenteHttpDeEventos(http, dealership.Resource, dealership.Vin),
				DealershipEventRoute.Route,
				FlushToDiskAsync,
				ct => RenewTheCredentialAsync(ct));

			// START() YA ESCRIBIO: OnHydrated journaliza la semilla en CADA hidratacion, aunque los
			// dos upgrades ya esten aplicados (medido en T6, y T7 lo midio como "reabrir crece el
			// journal exactamente una entrada"). Si el flush esperara al primer comando de la
			// persona, esa entrada se perderia en un cierre abrupto y la proxima apertura la
			// escribiria otra vez. Es un comando como cualquier otro y se baja como cualquier otro.
			//
			// PERO NO PUEDE IMPEDIR VER LA AGENDA. Si la carpeta rechaza la escritura -- permiso
			// denegado, tipicamente -- lo correcto es abrir igual y DECIRLO: el journal se leyo
			// bien, la agenda es correcta, y lo unico que falta es poder guardarla. Dejar que la
			// excepcion aborte la apertura cambiaba un problema de guardado por una app que no abre.
			try { await FlushToDiskAsync(); } catch (WriteRefusedException) { }

			LastMessage = bridge.HasFolder
				? $"up. {preloaded} files preloaded from '{bridge.FolderName}'"
				: "up, but WITHOUT a folder: the journal lives only in memory and is lost on close";

			// LA APP SE PONE AL DIA SOLA: consigue su credencial si no la tiene y lee lo nuevo.
			//
			// SI EL CONCESIONARIO NO ESTA ALCANZABLE, ABRIR NO FALLA: se muestra lo que ya hay en el
			// journal, que es justamente de lo que sirve tener el dato propio. El catch es ancho a
			// proposito -- sin red da una excepcion de HttpClient, mal apuntado da otra, y ninguna
			// de las dos es razon para no mostrarle a la persona su agenda.
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
				// LA CARPETA, NO EL CONCESIONARIO. El catch ancho de abajo le echaba la culpa al que
				// no fue: la credencial se guarda con un comando, el comando hace flush, y si la
				// carpeta lo rechaza la excepcion sale por el mismo camino que un fallo de red. El
				// aviso decia "no se pudo alcanzar al concesionario" y ofrecia reintentar, cuando lo
				// que hacia falta era re-otorgar el permiso. SavingIsFailing ya quedo puesto por
				// FlushToDiskAsync, que es el aviso correcto.
				LastMessage += " | the dealership answered, but the folder refused the write";
			}
			catch (Exception ex)
			{
				DealershipUnreachable =
					$"Could not reach {WhereTheDealershipIs} ({ex.GetType().Name}: {ex.Message}).";
				LastMessage += " | " + DealershipUnreachable;
			}

			// EVALUAR AL ABRIR ES LA CADENCIA PREVISTA (C6). Evaluate es un COMANDO -- transiciona
			// estados, asi que escribe -- y esta guardado sobre un punto de control monotono, asi
			// que abrir dos veces en el mismo segundo rechaza la segunda BAJO EL READ LOCK y no deja
			// entrada. Por eso se puede llamar en cada apertura sin engordar el journal.
			try
			{
				await EvaluateNowAsync();
			}
			catch (Exception ex)
			{
				LastMessage += $" | could not evaluate ({ex.GetType().Name}: {ex.Message})";
			}
		}

		// ---------------------------------------------------------------- el flush

		// EL UNICO CAMINO POR EL QUE LA APP BAJA A DISCO. Recuerda si fallo, para que la pantalla
		// pueda decirlo: un flush que falla en silencio deja lo que se ve sin respaldo y hace que el
		// servidor MCP lea un estado viejo sin que nada avise.
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

		// UN FLUSH MAS, DESPUES DEL BLOQUE, y no es redundante: cierra un caso que el mecanismo
		// compartido no puede ver.
		//
		// BucleDeRefresh baja a disco despues de CADA hecho aplicado, que es la ventana mas angosta
		// posible -- pero solo cuando Aplicar devolvio vacio. Cuando devuelve un rechazo el bucle
		// sigue de largo sin bajar, y ahi hay DOS casos distintos que el bucle no distingue:
		//
		//   - el Check rechazo un evento ya procesado: NO se escribio nada, y no bajar es correcto;
		//   - el DOMINIO refuso el hecho (C28): Aplicar atrapo el DomainException y journalizo el
		//     checkpoint SOLO, asi que SI hay una entrada nueva -- y el bucle no la baja.
		//
		// Sin este flush esa entrada se queda en MEMFS hasta el proximo comando que si salga bien, y
		// la ventana de perdida deja de ser de milisegundos. Corregirlo en el bucle seria editar
		// /Mecanismo/Puppets.Portador, que se copia verbatim y no es nuestro (C8).
		private async Task FlushAfterTheBlockAsync() => await FlushToDiskAsync();

		// ---------------------------------------------------------------- credencial

		// El token lo pide LA APP, no la persona. El concesionario lo emite contra el device key de
		// esta instalacion, acotado a leer los eventos de un vehiculo. Las tres piezas viven en el
		// journal, asi que al reabrir la app ya sabe quien es.
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

		// Tras un 401: primero rotar con el refresh token, que es para lo que existe. Si no hay o el
		// concesionario lo rechaza, pedir una credencial nueva con el device key.
		private async Task<string> RenewTheCredentialAsync(CancellationToken ct)
		{
			Require();

			string refresh = api.StoredRefreshToken();
			if (refresh.Length > 0)
			{
				// Rotar devuelve null si el refresh ya no vale (caducado, rotado por otra
				// instancia, revocado). No es un error: se cae a pedir una credencial nueva.
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

		// La identidad de esta instalacion. Se genera una sola vez y se journaliza como PARAMETRO,
		// asi el replay la reusa en vez de inventar otra.
		private static string ANewDeviceKey() => "device-" + Guid.NewGuid().ToString("N")[..16];

		// ---------------------------------------------------------------- comandos

		public async ValueTask<ResultadoDeRefresh> RefreshAsync(CancellationToken ct = default)
		{
			Require();

			// PIDE LA CREDENCIAL PRIMERO, igual que el arranque. Sin esto, una apertura que no pudo
			// hablar con el concesionario deja el boton inutil para siempre: no quedo token, y el
			// unico camino que lo conseguia era el arranque, que ya paso.
			await CredentialAsync(ct);

			ResultadoDeRefresh result = await loop.Ejecutar(ct);

			// EL AVISO SE BORRA ACA, en cuanto el bloque volvio: a esta altura el concesionario YA
			// contesto, y lo que pase con el flush es un problema de la carpeta y tiene su propio
			// aviso. Borrarlo despues del flush hacia que un permiso denegado dejara para siempre en
			// pantalla "no se pudo alcanzar al concesionario", que es falso y manda a la persona a
			// revisar la red en vez del permiso.
			DealershipUnreachable = "";
			LastMessage = $"refresh: {result}";

			await FlushAfterTheBlockAsync();
			return result;
		}

		// LA CADENCIA. Devuelve la razon del rechazo ya desenvuelta, o "" si la evaluacion corrio.
		// Desenvolver el {"EWI":[{"Error":"..."}]} es trabajo de la vista (medido, S3): el door lo
		// devuelve crudo a proposito.
		public async ValueTask<string> EvaluateNowAsync()
		{
			Require();

			DateTime asOf = AnInstantThatDoesNotGoBackwards();
			EvaluationOutcome outcome = api.Evaluate(asOf);

			if (!outcome.WasEvaluated)
			{
				// UN RECHAZO DE CADENCIA NO ESCRIBIO NADA, asi que no hay nada que bajar. Es la
				// diferencia entre un Check y un if adentro del cuerpo, y es lo que hace que
				// evaluar en cada apertura sea gratis.
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

		// LA PERSONA DEJA UNA OPERACION PARA DESPUES (L6). Fuera de un estado vencido, en el tope o
		// sobre una de seguridad es un no-op del dominio y no tira: se puede ofrecer siempre.
		public async ValueTask DeferOperationAsync(string kind, string reason)
		{
			Require();

			api.DeferOperation(kind, reason, AnInstantThatDoesNotGoBackwards());
			await FlushToDiskAsync();
			LastMessage = $"deferred {kind} ({reason})";
		}

		// LA PERSONA DECLARA COMO USA EL VEHICULO (L8, OINV-15).
		//
		// ESTA PANTALLA NO LO OFRECE EN ESTA REBANADA, por alcance y no por riesgo: un selector de
		// perfil sin el formulario de L9 que lo acompana es media capacidad suelta, y esta caratula
		// es la agenda. El metodo se queda -- implementado, probado y con su guarda -- porque la
		// medicion de C36 se hizo sobre EL, es la unica forma escrita de esa guarda en el repo, y es
		// lo que la rebanada de captura va a necesitar. Es distinto del boton de quitar, que se fue
		// porque no podia hacer nada nunca; este funciona.
		//
		// Es el UNICO comando de esta caratula cuyo rechazo por el dominio cuesta una entrada
		// permanente (C22), y hay tres razones por las que aca no la cuesta:
		//
		//   1. EL INSTANTE NO ES UN CAMPO. No hay date picker en ninguna parte de esta app: la
		//      caratula sella el reloj. Un formulario de captura con fecha es exactamente la forma
		//      que deja la cicatriz, y el brief de T10 pide decidirlo a proposito.
		//   2. EL SELLO ES MONOTONO dentro de la sesion, asi que la caratula no puede desordenar
		//      sus propios comandos ni con el reloj de la maquina en contra.
		//   3. LA SEGUNDA GUARDA SE PREGUNTA EN LA FRONTERA. OINV-15 tiene DOS mitades: retroactivo
		//      contra la declaracion anterior, y retroactivo contra lo ya EVALUADO. La segunda lee
		//      _lastEvaluation, y §4 SI proyecta eso -- HasBeenEvaluated() y LastEvaluatedAt(), el
		//      mismo par que le dio a Evaluate su Check (C6). Preguntarlo antes de performear cuesta
		//      cero. La PRIMERA mitad no esta proyectada y sigue siendo una cicatriz posible: para
		//      cerrarla haria falta que la fachada proyecte "el perfil que rige y desde cuando",
		//      que es un cambio de dominio y por eso es un hallazgo, no un parche.
		public async ValueTask<string> DeclareUsageProfileAsync(string profile)
		{
			Require();

			DateTime effectiveFrom = AnInstantThatDoesNotGoBackwards();

			if (api.HasBeenEvaluated() && api.LastEvaluatedAt() > effectiveFrom)
				return $"This schedule was already evaluated up to {Instant(api.LastEvaluatedAt())}, "
				     + $"which is after {Instant(effectiveFrom)}. A new profile applies going forward "
				     + "(OINV-15), so it cannot be declared for an instant the law already decided.";

			api.DeclareUsageProfile(profile, effectiveFrom);
			await FlushToDiskAsync();
			LastMessage = $"usage profile declared: {profile}";
			return "";
		}

		// L9 -- QUITAR UNA OPERACION PROPIA -- NO SE OFRECE EN ESTA CARATULA, y la razon es que
		// seria una accion que nunca puede hacer nada: L9 solo deja quitar lo que la persona AGREGO,
		// y esta pantalla no ofrece AddOwnerOperation (pide clase, base, dos limites, unidad y el
		// ancla entera: instante, lectura y su unidad). Con el alta afuera, la baja es un boton
		// muerto. `DealershipCarAPI.RemoveOwnerOperation` sigue existiendo -- el dominio y la puerta
		// no cambian -- lo que no existe es la oferta, igual que la consola de T7 no ofrece cuatro
		// operaciones que la puerta si tiene.

		// ---------------------------------------------------------------- el reloj

		// EL UNICO LUGAR DE TODO EL MODULO QUE LEE UN RELOJ. Trunca al segundo porque el journal
		// trunca al segundo una llamada despues y en silencio (S4/C24): truncar aca es lo que hace
		// que la corrida viva y la replayada sean la misma corrida.
		private DateTime AnInstantThatDoesNotGoBackwards()
		{
			DateTime now = DateTime.UtcNow;
			now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);

			if (now < lastStampSent)
				// EN INGLES PORQUE LLEGA A LA PANTALLA. `Try` lo atrapa y lo imprime en el alert de
				// error, y la pantalla es inglesa (decision 6 de T7). Los comentarios se quedan en
				// espanol; el mensaje que lee la persona, no.
				throw new InvalidOperationException(
					$"This machine's clock went backwards: it is now {Instant(now)} and this app has "
					+ $"already sent a command stamped {Instant(lastStampSent)}. It will not send an "
					+ "instant that moves backwards -- a retroactive declaration fails INSIDE the "
					+ "command and leaves a permanent entry in the journal.");

			lastStampSent = now;
			return now;
		}

		// ---------------------------------------------------------------- lecturas

		// LAS NUEVE CLASES, escritas a mano como texto. Es el costo de C25 hecho visible: esta
		// caratula NO referencia el dominio -- no puede nombrar OperationKindCode -- asi que un
		// codigo cruza como el NOMBRE de su miembro. Un nombre mal escrito lo rechaza la puerta,
		// ruidosamente y sin escribir nada; una clase que el dominio agregue y esta lista no,
		// simplemente no se muestra. Lo segundo es el riesgo real de la lista, igual que en la
		// consola de T7, y se deja dicho.
		public static readonly string[] TheNineKinds =
		{
			"Inspection", "FluidChange", "FilterChange", "WearPartReplacement", "TimedComponent",
			"SoftwareUpdate", "RecallRemedy", "Diagnostic", "Repair"
		};

		// LOS CUATRO PERFILES y LAS CINCO RAZONES, por el mismo motivo y con la misma red.
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

		// SI OFRECER EL APLAZAMIENTO. La pantalla no la deduce del estado: L6 tiene cuatro
		// condiciones y el estado es una sola. Preguntarsela al dominio es lo que evita un control
		// habilitado sobre una operacion clasificada de seguridad, que no se aplaza nunca.
		public bool CanBeDeferred(string kind) { Require(); return api.CanBeDeferred(kind); }

		public int CountDue() { Require(); return api.CountDue(); }

		public int CountOverdue() { Require(); return api.CountOverdue(); }

		public int MissedCount() { Require(); return api.MissedCount(); }

		public bool HasAnythingDue() { Require(); return api.HasAnythingDue(); }

		// LAS DOS LECTURAS GUARDADAS, cada una con su guarda de presencia. Tiran
		// InvalidOperationException por contrato (E41, T6 decision 6) y "nada esta vencido" es la
		// mejor noticia que esta app puede dar, no un error: se pregunta primero y se contesta el
		// lado vacio.
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

		// ---------------------------------------------------------------- utilidades

		// El instante sale del door ya re-estampado como UTC (C24), asi que 'o' lo escribe con su Z
		// y no hay dos respuestas para un mismo hecho.
		private static string Instant(DateTime instant) => instant.ToString("yyyy-MM-dd HH:mm:ss'Z'");

		// El rechazo del Check viene envuelto en {"EWI":[{"Error":"..."}]}. Si la forma no es esa se
		// devuelve crudo en vez de inventar una: el door promete el sobre, no su contenido.
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

	// EL JUEGO DE URLS DEL CONCESIONARIO, de wwwroot/appsettings.json y no de una constante
	// compilada: la app servida desde el contenedor tiene que poder apuntar a otro host sin
	// recompilar.
	//
	// EL VIN VIVE ACA TAMBIEN, y es el mismo dato dos veces a proposito: es el id del recurso en
	// URL2 y es el nombre del actor (maintenance-{vin}). Un vehiculo es un actor y un journal.
	public sealed class DealershipSettings
	{
		public string BaseUrl { get; set; } = "eventos/";
		public string Resource { get; set; } = "vehicle";
		public string Vin { get; set; } = "1HGCM82633A004352";
	}
}
