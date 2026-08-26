using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace DealershipCar.Wasm
{
	// El puente entre el journal en MEMFS y una carpeta real de la persona.
	//
	// Es la pieza que hace que la carpeta del journal sea la superficie compartida: el browser
	// escribe ahi y el proceso MCP de T9 lee de ahi. No hay protocolo inventado entre los dos --
	// el journal ya es el protocolo.
	//
	// Reparto con journal-interop.js: JS habla con el browser (picker, IndexedDB, la carpeta) y NO
	// toca MEMFS; C# lee y escribe MEMFS con System.IO, la misma superficie que usa el motor. Los
	// bytes cruzan en base64, asi el puente no depende de APIs internas del runtime .NET.
	//
	// El mismatch de fondo: el storage del motor es SINCRONO, las APIs de archivos del browser son
	// ASINCRONAS, y en wasm single-thread no se puede bloquear esperando una promesa. De ahi la
	// forma: PRECARGAR sincrono antes de tocar el motor, PERSISTIR asincrono despues de cada
	// comando.
	public sealed class JournalBridge : IAsyncDisposable
	{
		// La raiz en MEMFS. Es lo que se le pasa al motor como path=, y no tiene nada que ver con
		// donde la persona puso la carpeta.
		public const string RootInMemfs = "/journal";

		private readonly IJSRuntime js;
		private IJSObjectReference module;

		public JournalBridge(IJSRuntime js)
		{
			ArgumentNullException.ThrowIfNull(js);
			this.js = js;
		}

		// TENER la carpeta y TENER PERMISO sobre ella son dos cosas distintas, y confundirlas es un
		// bug con nombre (ARCHITECTURE §4.1): al reabrir la app el handle vuelve de IndexedDB pero
		// el browser degrada el permiso a 'prompt', asi que hay carpeta y no hay permiso. Guardar un
		// solo dato obligaba a borrar el nombre justo cuando hacia falta para poder decir "volve a
		// dar permiso a tal carpeta", y el boton salia con el nombre vacio.
		public string FolderName { get; private set; } = "";

		public bool PermissionGranted { get; private set; }

		public bool HasFolder => FolderName.Length > 0 && PermissionGranted;

		// Hay handle recuperado y el permiso caduco: se recupera con un click, sin volver a elegir
		// la carpeta. Es la accion PRIMARIA en ese estado, no "elegir carpeta" otra vez.
		public bool NeedsRegrant => FolderName.Length > 0 && !PermissionGranted;

		private async ValueTask<IJSObjectReference> ModuleAsync() =>
			module ??= await js.InvokeAsync<IJSObjectReference>("import", "./journal-interop.js");

		// ---------------------------------------------------------------- soporte

		public async ValueTask<bool> PickerIsSupportedAsync() =>
			await (await ModuleAsync()).InvokeAsync<bool>("estaSoportado");

		public async ValueTask<bool> BrowserStorageIsSupportedAsync() =>
			await (await ModuleAsync()).InvokeAsync<bool>("opfsSoportado");

		// '' si hay algun almacenamiento; 'navegador' o 'inseguro' si no hay ninguno. Son dos causas
		// que se arreglan distinto y la deteccion cruda las confunde en un solo false.
		public async ValueTask<string> WhyThereIsNoStorageAsync() =>
			await (await ModuleAsync()).InvokeAsync<string>("motivoSinAlmacenamiento");

		public async ValueTask<bool> RunsAsInstalledAppAsync() =>
			await (await ModuleAsync()).InvokeAsync<bool>("estaInstalada");

		// 'carpeta', 'opfs' o 'ninguno'.
		public async ValueTask<string> CurrentModeAsync() =>
			await (await ModuleAsync()).InvokeAsync<string>("modoActual");

		// Que destino eligio la persona la vez pasada. Se consulta al arrancar: si ya opto por el
		// almacenamiento del navegador, se reengancha sola y sin pedirle un gesto.
		public async ValueTask<string> RememberedModeAsync() =>
			await (await ModuleAsync()).InvokeAsync<string>("modoRecordado");

		// El permiso vigente sobre la carpeta, sin pedir nada. Sirve para avisar ANTES de aplicar
		// comandos que no se van a poder guardar.
		public async ValueTask<string> CurrentPermissionAsync() =>
			await (await ModuleAsync()).InvokeAsync<string>("permisoActual");

		// ---------------------------------------------------------------- el destino

		// EXIGE GESTO DE USUARIO: llamalo desde un click, nunca al inicializar.
		public async ValueTask<FolderResult> ChooseFolderAsync() => await AskAsync("elegirCarpeta");

		// Tras recargar, el handle vuelve de IndexedDB pero el permiso hay que revalidarlo. Si
		// devuelve permiso 'prompt', hace falta RequestPermissionAsync() desde un click.
		public async ValueTask<FolderResult> RecoverFolderAsync() => await AskAsync("recuperarCarpeta");

		// EXIGE GESTO DE USUARIO.
		public async ValueTask<FolderResult> RequestPermissionAsync() => await AskAsync("pedirPermiso");

		// Entra por el MISMO AskAsync que la carpeta, asi que deja el estado del puente coherente --
		// FolderName y PermissionGranted -- sin una segunda ruta que mantener.
		public async ValueTask<FolderResult> UseBrowserStorageAsync() => await AskAsync("usarOpfs");

		public async ValueTask ForgetFolderAsync()
		{
			await (await ModuleAsync()).InvokeAsync<WriteResult>("olvidarCarpeta");
			FolderName = "";
			PermissionGranted = false;
		}

		// El nombre se conserva siempre que el browser lo haya devuelto, tenga o no permiso. Solo
		// se limpia cuando de verdad no hay handle.
		private async ValueTask<FolderResult> AskAsync(string function)
		{
			FolderResult result = await (await ModuleAsync()).InvokeAsync<FolderResult>(function);

			// Si el browser no devolvio nombre, no hubo handle en juego: tipicamente la persona
			// cerro el dialogo del picker. Eso NO debe borrar la carpeta que ya estaba recuperada,
			// o cancelar por error obligaria a volver a elegirla.
			if (!string.IsNullOrEmpty(result.Name))
			{
				FolderName = result.Name;
				PermissionGranted = result.Ok;
			}
			return result;
		}

		// ---------------------------------------------------------------- precarga

		// ARRANQUE: carpeta -> MEMFS. Corre ANTES de que exista el actor, que es lo que permite que
		// el replay del motor lea todo sincrono. Es el paso 2 del orden de arranque, y tiene que
		// pasar antes del 3.
		public async ValueTask<int> PreloadIntoMemfsAsync()
		{
			IJSObjectReference loaded = await ModuleAsync();
			ListResult listing = await loaded.InvokeAsync<ListResult>("listar");

			// Handle colgando: la carpeta se movio, se renombro o se borro. El permiso puede seguir
			// figurando como otorgado, asi que el unico momento en que se descubre es al intentar
			// leerla. Se olvida el handle para que la app vuelva a ofrecer elegir carpeta en vez de
			// reintentar contra algo que ya no esta.
			if (!listing.Ok)
			{
				await ForgetFolderAsync();
				// LOS CUATRO MENSAJES DE ESTE ARCHIVO VAN EN INGLES, porque los cuatro llegan a la
				// pantalla: dos por el alert de error y dos por SavingIsFailing. La pantalla es
				// inglesa (decision 6 de T7). Los comentarios se quedan en espanol.
				throw new FolderNotAvailableException(
					$"Could not read the saved folder: {listing.Reason}. Choose a folder again.");
			}

			int copied = 0;
			foreach (string path in listing.Paths)
			{
				ReadResult read = await loaded.InvokeAsync<ReadResult>("leer", path);

				// UN ARCHIVO DEL JOURNAL QUE NO SE PUEDE LEER NO SE PUEDE SALTAR: la rehidratacion
				// quedaria incompleta y la agenda saldria mal, sin aviso. Falla ruidosamente.
				if (!read.Ok)
					throw new FolderNotAvailableException(
						$"Could not read '{path}' from '{FolderName}': {read.Reason}. "
						+ "Opening with half the journal would give a wrong schedule.");

				string destination = Path.Combine(RootInMemfs, path.Replace('/', Path.DirectorySeparatorChar));
				string directory = Path.GetDirectoryName(destination);
				if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

				File.WriteAllBytes(destination, Convert.FromBase64String(read.Content));
				copied++;
			}
			return copied;
		}

		// ---------------------------------------------------------------- persistencia

		// OPERACION: MEMFS -> carpeta. Se llama DESPUES DE CADA COMANDO, no por timer: angosta al
		// maximo la ventana en la que un cierre abrupto pierde entradas ya commiteadas en el actor
		// pero todavia no bajadas al disco.
		//
		// Escribe el arbol completo en vez de detectar cambios. Medido en T7/T8: la historia de seis
		// meses son ~3.5 KB en dos archivos, asi que el ahorro de un diff no paga el riesgo de un
		// diff mal hecho. Si el journal creciera a megabytes, ACA es donde hay que comparar tamano y
		// fecha antes de escribir.
		public async ValueTask<int> FlushFromMemfsAsync()
		{
			// TENER CARPETA SIN PERMISO DE ESCRITURA NO ES "no tener destino", y tratarlos igual era
			// la fuga mas silenciosa de esta caratula: se salia por el mismo `return 0` que el caso
			// sin destino, asi que la app aplicaba comandos, los descartaba, y la pantalla decia
			// "no se esta guardando nada... sin destino" -- que nombra la causa equivocada y por lo
			// tanto el remedio equivocado (elegir carpeta, cuando lo que hace falta es re-otorgar).
			//
			// MEDIDO sobre una carpeta real cuyo permiso quedo en 'denied'.
			if (NeedsRegrant)
				throw new WriteRefusedException(
					$"There is no write permission on '{FolderName}'. The browser grants it separately "
					+ "from letting you pick the folder, and it withdraws it when the app reopens. "
					+ "Granting it again recovers it; until then nothing you see is being saved.");

			if (!HasFolder || !Directory.Exists(RootInMemfs)) return 0;

			IJSObjectReference loaded = await ModuleAsync();
			int written = 0;
			var failed = new List<string>();

			foreach (string file in Directory.EnumerateFiles(RootInMemfs, "*", SearchOption.AllDirectories))
			{
				string relative = Path.GetRelativePath(RootInMemfs, file).Replace(Path.DirectorySeparatorChar, '/');
				string base64 = Convert.ToBase64String(File.ReadAllBytes(file));

				WriteResult result = await loaded.InvokeAsync<WriteResult>("escribir", relative, base64);
				if (result.Ok) written++;
				else failed.Add($"{relative} ({result.Reason})");
			}

			// UN FLUSH QUE FALLA NO PUEDE PASAR EN SILENCIO. Contar los exitos y descartar los
			// fallos deja la app mostrando lo que tiene en memoria mientras el journal de la carpeta
			// se queda atras, y quien lo lea despues -- el servidor MCP, o la proxima apertura --
			// ve un estado viejo sin que nada avise. Eso NO es la ventana de perdida documentada,
			// que es de milisegundos: es una divergencia permanente y muda. La causa tipica es que
			// el permiso de escritura caduco, y eso se arregla volviendolo a otorgar.
			if (failed.Count > 0)
				throw new WriteRefusedException(
					$"Could not save to '{FolderName}': {failed.Count} of {failed.Count + written} "
					+ $"files failed. {string.Join("; ", failed)}");

			return written;
		}

		// Lo que hay en MEMFS ahora, para poder reportarlo sin adivinar.
		public static (int files, long bytes) MemfsState()
		{
			if (!Directory.Exists(RootInMemfs)) return (0, 0);

			int files = 0;
			long bytes = 0;
			foreach (string file in Directory.EnumerateFiles(RootInMemfs, "*", SearchOption.AllDirectories))
			{
				files++;
				bytes += new FileInfo(file).Length;
			}
			return (files, bytes);
		}

		public async ValueTask DisposeAsync()
		{
			if (module is not null)
			{
				try { await module.DisposeAsync(); } catch (JSDisconnectedException) { }
				module = null;
			}
		}
	}

	// Lo que devuelve cualquiera de las cuatro operaciones sobre el destino.
	public sealed class FolderResult
	{
		[JsonPropertyName("ok")]      public bool Ok { get; set; }
		[JsonPropertyName("nombre")]  public string Name { get; set; } = "";
		[JsonPropertyName("permiso")] public string Permission { get; set; } = "";
		[JsonPropertyName("motivo")]  public string Reason { get; set; } = "";

		public override string ToString() =>
			Ok ? $"ok, folder '{Name}' (permission {Permission})"
			   : $"no: {(Reason.Length > 0 ? Reason : Permission)}";
	}

	public sealed class WriteResult
	{
		[JsonPropertyName("ok")]     public bool Ok { get; set; }
		[JsonPropertyName("motivo")] public string Reason { get; set; } = "";
	}

	public sealed class ListResult
	{
		[JsonPropertyName("ok")]     public bool Ok { get; set; }
		[JsonPropertyName("rutas")]  public string[] Paths { get; set; } = Array.Empty<string>();
		[JsonPropertyName("motivo")] public string Reason { get; set; } = "";
	}

	public sealed class ReadResult
	{
		[JsonPropertyName("ok")]        public bool Ok { get; set; }
		[JsonPropertyName("contenido")] public string Content { get; set; } = "";
		[JsonPropertyName("motivo")]    public string Reason { get; set; } = "";
	}

	// La carpeta rechazo la escritura. Tipo propio porque la app puede reaccionar: casi siempre es
	// el permiso caducado, y se recupera volviendolo a otorgar con un click.
	public sealed class WriteRefusedException : Exception
	{
		public WriteRefusedException(string message) : base(message) { }
	}

	// La carpeta guardada ya no se puede leer: se movio, se renombro o se borro. Se resuelve
	// eligiendo otra, no reintentando.
	public sealed class FolderNotAvailableException : Exception
	{
		public FolderNotAvailableException(string message) : base(message) { }
	}
}
