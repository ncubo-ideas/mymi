using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace DealershipCar.Wasm
{
	public sealed class JournalBridge : IAsyncDisposable
	{
		public const string RootInMemfs = "/journal";

		private readonly IJSRuntime js;
		private IJSObjectReference module;

		public JournalBridge(IJSRuntime js)
		{
			ArgumentNullException.ThrowIfNull(js);
			this.js = js;
		}

		public string FolderName { get; private set; } = "";

		public bool PermissionGranted { get; private set; }

		public bool HasFolder => FolderName.Length > 0 && PermissionGranted;

		public bool NeedsRegrant => FolderName.Length > 0 && !PermissionGranted;

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
			IJSObjectReference loaded = await ModuleAsync();
			ListResult listing = await loaded.InvokeAsync<ListResult>("listar");

			if (!listing.Ok)
			{
				await ForgetFolderAsync();
				throw new FolderNotAvailableException(
					$"Could not read the saved folder: {listing.Reason}. Choose a folder again.");
			}

			int copied = 0;
			foreach (string path in listing.Paths)
			{
				ReadResult read = await loaded.InvokeAsync<ReadResult>("leer", path);

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

		public async ValueTask<int> FlushFromMemfsAsync()
		{
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

			if (failed.Count > 0)
				throw new WriteRefusedException(
					$"Could not save to '{FolderName}': {failed.Count} of {failed.Count + written} "
					+ $"files failed. {string.Join("; ", failed)}");

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

	public sealed class WriteRefusedException : Exception
	{
		public WriteRefusedException(string message) : base(message) { }
	}

	public sealed class FolderNotAvailableException : Exception
	{
		public FolderNotAvailableException(string message) : base(message) { }
	}
}
