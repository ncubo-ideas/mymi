using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace Concesionario.Wasm
{
	public sealed class JournalBridge : IJournalDestination, IAsyncDisposable
	{
		public const string RootInMemfs = "/journal";

		private readonly IJSRuntime js;
		private readonly IJournalDestination destination;
		private IJSObjectReference module;

		// MODO SUELTO: el destino es el storage del browser, y de eso se ocupa esta misma
		// clase (ver la region "the destination" al final). Es el constructor de siempre.
		public JournalBridge(IJSRuntime js)
		{
			ArgumentNullException.ThrowIfNull(js);
			this.js = js;
			destination = this;
		}

		// MODO HOSPEDADO: el destino son los archivos de Gnothi, por el puente nativo. El
		// movimiento MEMFS <-> destino es EL MISMO; lo unico que cambia es donde aterrizan los
		// bytes. La plomeria de browser que queda abajo -picker, permisos, OPFS- no se usa en
		// este modo: Gnothi no le pregunta a nadie donde escribir.
		public JournalBridge(IJSRuntime js, IJournalDestination destination)
		{
			ArgumentNullException.ThrowIfNull(js);
			ArgumentNullException.ThrowIfNull(destination);
			this.js = js;
			this.destination = destination;
		}

		public string FolderName { get; private set; } = "";

		public bool PermissionGranted { get; private set; }

		public bool HasFolder => FolderName.Length > 0 && PermissionGranted;

		public bool NeedsRegrant => FolderName.Length > 0 && !PermissionGranted;

		// SI HAY DONDE ESCRIBIR, sea el storage del browser o los archivos de Gnothi. Es lo que
		// mira el arranque para decidir si precarga: "folder" es vocabulario del modo suelto y
		// hospedado no aplica.
		public bool HasDestination => destination.Available;

		private async ValueTask<IJSObjectReference> ModuleAsync() =>
			module ??= await js.InvokeAsync<IJSObjectReference>("import", "./journal-interop.js");

		public async ValueTask<bool> PickerIsSupportedAsync() =>
			await (await ModuleAsync()).InvokeAsync<bool>("estaSoportado");

		public async ValueTask<bool> BrowserStorageIsSupportedAsync() =>
			await (await ModuleAsync()).InvokeAsync<bool>("opfsSoportado");

		public async ValueTask<string> WhyThereIsNoStorageAsync() =>
			await (await ModuleAsync()).InvokeAsync<string>("motivoSinAlmacenamiento");

		public async ValueTask<bool> RunsAsInstalledAppAsync() =>
			await (await ModuleAsync()).InvokeAsync<bool>("estaInstalada");

		public async ValueTask<string> CurrentModeAsync() =>
			await (await ModuleAsync()).InvokeAsync<string>("modoActual");

		public async ValueTask<string> RememberedModeAsync() =>
			await (await ModuleAsync()).InvokeAsync<string>("modoRecordado");

		// ------------------------------------------------------------ the destination, standalone

		// EL STORAGE DEL BROWSER como destino del journal. Esta clase ya tenia las tres
		// operaciones y el estado del permiso, asi que las expone por la costura en vez de
		// mudarlas: el modo suelto no cambia una linea de comportamiento.
		//
		// Implementacion EXPLICITA a proposito: son para el mecanismo, no para la caratula. La
		// pantalla sigue hablando de carpeta y permiso, que es lo que la persona entiende.
		string IJournalDestination.Name => FolderName;

		bool IJournalDestination.Available => HasFolder;

		async ValueTask<ListResult> IJournalDestination.ListAsync() =>
			await (await ModuleAsync()).InvokeAsync<ListResult>("listar");

		async ValueTask<ReadResult> IJournalDestination.ReadAsync(string path) =>
			await (await ModuleAsync()).InvokeAsync<ReadResult>("leer", path);

		async ValueTask<WriteResult> IJournalDestination.WriteAsync(string path, string contentBase64) =>
			await (await ModuleAsync()).InvokeAsync<WriteResult>("escribir", path, contentBase64);

		public async ValueTask<string> CurrentPermissionAsync() =>
			await (await ModuleAsync()).InvokeAsync<string>("permisoActual");

		public async ValueTask<FolderResult> ChooseFolderAsync() => await AskAsync("elegirCarpeta");

		public async ValueTask<FolderResult> RecoverFolderAsync() => await AskAsync("recuperarCarpeta");

		public async ValueTask<FolderResult> RequestPermissionAsync() => await AskAsync("pedirPermiso");

		public async ValueTask<FolderResult> UseBrowserStorageAsync() => await AskAsync("usarOpfs");

		public async ValueTask ForgetFolderAsync()
		{
			await (await ModuleAsync()).InvokeAsync<WriteResult>("olvidarCarpeta");
			FolderName = "";
			PermissionGranted = false;
		}

		private async ValueTask<FolderResult> AskAsync(string function)
		{
			FolderResult result = await (await ModuleAsync()).InvokeAsync<FolderResult>(function);

			if (!string.IsNullOrEmpty(result.Name))
			{
				FolderName = result.Name;
				PermissionGranted = result.Ok;
			}
			return result;
		}

		public async ValueTask<int> PreloadIntoMemfsAsync()
		{
			ListResult listing = await destination.ListAsync();

			// Solo en el modo suelto: hospedado no hay handle que olvidar ni carpeta que volver
			// a elegir, y un fallo al listar es un error de verdad de Gnothi.
			if (!listing.Ok)
			{
				if (ReferenceEquals(destination, this)) await ForgetFolderAsync();
				throw new FolderNotAvailableException(
					$"No se pudo leer la carpeta guardada: {listing.Reason}. Elija una carpeta otra "
					+ "vez.");
			}

			int copied = 0;
			foreach (string path in listing.Paths)
			{
				ReadResult read = await destination.ReadAsync(path);

				if (!read.Ok)
					throw new FolderNotAvailableException(
						$"No se pudo leer '{path}' de '{FolderName}': {read.Reason}. Abrir con medio "
						+ "journal daria un plan equivocado.");

				string destinationInMemfs = Path.Combine(RootInMemfs, path.Replace('/', Path.DirectorySeparatorChar));
				string directory = Path.GetDirectoryName(destinationInMemfs);
				if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

				File.WriteAllBytes(destinationInMemfs, Convert.FromBase64String(read.Content));
				copied++;
			}
			return copied;
		}

		public async ValueTask<int> FlushFromMemfsAsync()
		{
			// El permiso es vocabulario del modo suelto: hospedado el destino es de Gnothi y
			// nadie le revoca el permiso sobre lo suyo.
			if (ReferenceEquals(destination, this) && NeedsRegrant)
				throw new WriteRefusedException(
					$"No hay permiso de escritura sobre '{FolderName}'. El navegador lo da aparte del "
					+ "de elegir la carpeta, y se lo quita cuando la app se vuelve a abrir. Volver a "
					+ "darlo lo recupera; hasta entonces nada de lo que ve se está guardando.");

			if (!destination.Available || !Directory.Exists(RootInMemfs)) return 0;

			int written = 0;
			var failed = new List<string>();

			foreach (string file in Directory.EnumerateFiles(RootInMemfs, "*", SearchOption.AllDirectories))
			{
				string relative = Path.GetRelativePath(RootInMemfs, file).Replace(Path.DirectorySeparatorChar, '/');
				string base64 = Convert.ToBase64String(File.ReadAllBytes(file));

				WriteResult result = await destination.WriteAsync(relative, base64);
				if (result.Ok) written++;
				else failed.Add($"{relative} ({result.Reason})");
			}

			if (failed.Count > 0)
				throw new WriteRefusedException(
					$"No se pudo guardar en '{FolderName}': fallaron {failed.Count} de "
					+ $"{failed.Count + written} archivos. {string.Join("; ", failed)}");

			return written;
		}

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

	public sealed class FolderResult
	{
		[JsonPropertyName("ok")]      public bool Ok { get; set; }
		[JsonPropertyName("nombre")]  public string Name { get; set; } = "";
		[JsonPropertyName("permiso")] public string Permission { get; set; } = "";
		[JsonPropertyName("motivo")]  public string Reason { get; set; } = "";

		public override string ToString() =>
			Ok ? $"ok, carpeta '{Name}' (permiso {Permission})"
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

	public sealed class WriteRefusedException : Exception
	{
		public WriteRefusedException(string message) : base(message) { }
	}

	public sealed class FolderNotAvailableException : Exception
	{
		public FolderNotAvailableException(string message) : base(message) { }
	}
}
