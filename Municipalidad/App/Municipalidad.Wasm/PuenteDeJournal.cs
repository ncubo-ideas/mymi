using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace Municipalidad.Wasm
{
	/// <summary>
	/// El puente entre el journal en MEMFS y la carpeta de la persona. El lado JS habla solo con el
	/// navegador y nunca toca MEMFS; este lado lee y escribe MEMFS con System.IO, y los bytes cruzan
	/// en base64.
	/// </summary>
	public sealed class PuenteDeJournal : IDestinoDelJournal, IAsyncDisposable
	{
		/// <summary>La raiz del journal dentro de MEMFS. Es lo que recibe ConfigureStorage.</summary>
		public const string RaizEnMemfs = "/journal";

		private readonly IJSRuntime js;
		private readonly IDestinoDelJournal destino;
		private IJSObjectReference modulo;

		/// <summary>
		/// MODO SUELTO: el destino es el almacenamiento del navegador, y de eso se ocupa esta misma clase.
		/// </summary>
		public PuenteDeJournal(IJSRuntime js)
		{
			ArgumentNullException.ThrowIfNull(js);
			this.js = js;
			destino = this;
		}

		/// <summary>
		/// MODO HOSPEDADO: el destino son los archivos de Gnothi, por el puente nativo. El movimiento
		/// entre MEMFS y el destino es EL MISMO; lo unico que cambia es donde aterrizan los bytes. El
		/// selector, el permiso y el almacenamiento del navegador no se usan en este modo.
		/// </summary>
		public PuenteDeJournal(IJSRuntime js, IDestinoDelJournal destino)
		{
			ArgumentNullException.ThrowIfNull(js);
			ArgumentNullException.ThrowIfNull(destino);
			this.js = js;
			this.destino = destino;
		}

		public string NombreDeLaCarpeta { get; private set; } = "";

		public bool PermisoOtorgado { get; private set; }

		/// <summary>Hay carpeta Y hay permiso. Tener carpeta y tener permiso son dos cosas distintas.</summary>
		public bool HayCarpeta => NombreDeLaCarpeta.Length > 0 && PermisoOtorgado;

		/// <summary>Hay carpeta y el navegador degrado el permiso al reabrir: falta reotorgarlo.</summary>
		public bool FaltaReotorgarPermiso => NombreDeLaCarpeta.Length > 0 && !PermisoOtorgado;

		/// <summary>
		/// Si hay donde escribir, sea el almacenamiento del navegador o los archivos de Gnothi. Es lo que
		/// mira el arranque para decidir si precarga: la carpeta es vocabulario del modo suelto, y
		/// hospedada no aplica.
		/// </summary>
		public bool HayDestino => destino.Disponible;

		/// <summary>Si el destino es el almacenamiento de este navegador, y no los archivos de Gnothi.</summary>
		private bool ElDestinoEsElNavegador => ReferenceEquals(destino, this);

		private async ValueTask<IJSObjectReference> ModuloAsync() =>
			modulo ??= await js.InvokeAsync<IJSObjectReference>("import", "./journal-interop.js");

		public async ValueTask<bool> EstaSoportadoAsync() =>
			await (await ModuloAsync()).InvokeAsync<bool>("estaSoportado");

		public async ValueTask<bool> OpfsSoportadoAsync() =>
			await (await ModuloAsync()).InvokeAsync<bool>("opfsSoportado");

		/// <summary>Por que no hay almacenamiento: 'navegador', 'inseguro', o vacio si si lo hay.</summary>
		public async ValueTask<string> MotivoSinAlmacenamientoAsync() =>
			await (await ModuloAsync()).InvokeAsync<string>("motivoSinAlmacenamiento");

		public async ValueTask<bool> EstaInstaladaAsync() =>
			await (await ModuloAsync()).InvokeAsync<bool>("estaInstalada");

		public async ValueTask<string> ModoActualAsync() =>
			await (await ModuloAsync()).InvokeAsync<string>("modoActual");

		public async ValueTask<string> ModoRecordadoAsync() =>
			await (await ModuloAsync()).InvokeAsync<string>("modoRecordado");

		public async ValueTask<string> PermisoActualAsync() =>
			await (await ModuloAsync()).InvokeAsync<string>("permisoActual");

		/// <summary>Exige gesto de la persona: no se puede llamar al cargar la pagina.</summary>
		public async ValueTask<ResultadoDeCarpeta> ElegirCarpetaAsync() => await PedirAsync("elegirCarpeta");

		public async ValueTask<ResultadoDeCarpeta> RecuperarCarpetaAsync() => await PedirAsync("recuperarCarpeta");

		/// <summary>Exige gesto de la persona, igual que elegir la carpeta.</summary>
		public async ValueTask<ResultadoDeCarpeta> PedirPermisoAsync() => await PedirAsync("pedirPermiso");

		public async ValueTask<ResultadoDeCarpeta> UsarOpfsAsync() => await PedirAsync("usarOpfs");

		/// <summary>Olvida el handle guardado. No toca nada de lo que hay adentro de la carpeta.</summary>
		public async ValueTask OlvidarCarpetaAsync()
		{
			await (await ModuloAsync()).InvokeAsync<ResultadoDeEscritura>("olvidarCarpeta");
			NombreDeLaCarpeta = "";
			PermisoOtorgado = false;
		}

		private async ValueTask<ResultadoDeCarpeta> PedirAsync(string funcion)
		{
			ResultadoDeCarpeta resultado = await (await ModuloAsync()).InvokeAsync<ResultadoDeCarpeta>(funcion);

			if (!string.IsNullOrEmpty(resultado.Nombre))
			{
				NombreDeLaCarpeta = resultado.Nombre;
				PermisoOtorgado = resultado.Ok;
			}
			return resultado;
		}

		/// <summary>
		/// Baja la carpeta entera a MEMFS, ANTES de que el actor exista. Un archivo que no se pueda
		/// leer NO se salta: revienta, porque un journal a medias da totales equivocados sin aviso.
		/// </summary>
		public async ValueTask<int> PrecargarEnMemfsAsync()
		{
			ResultadoDeListado listado = await destino.ListarAsync();

			if (!listado.Ok)
			{
				if (ElDestinoEsElNavegador) await OlvidarCarpetaAsync();
				throw new CarpetaNoDisponibleException(
					$"No se pudo leer '{destino.Nombre}': {listado.Motivo}. Elija una carpeta otra "
					+ "vez.");
			}

			int copiados = 0;
			foreach (string ruta in listado.Rutas)
			{
				ResultadoDeLectura leido = await destino.LeerAsync(ruta);

				if (!leido.Ok)
					throw new CarpetaNoDisponibleException(
						$"No se pudo leer '{ruta}' de '{destino.Nombre}': {leido.Motivo}. Abrir con "
						+ "medio journal daria una cuenta equivocada.");

				string destinoEnMemfs = Path.Combine(RaizEnMemfs, ruta.Replace('/', Path.DirectorySeparatorChar));
				string directorio = Path.GetDirectoryName(destinoEnMemfs);
				if (!string.IsNullOrEmpty(directorio)) Directory.CreateDirectory(directorio);

				File.WriteAllBytes(destinoEnMemfs, Convert.FromBase64String(leido.Contenido));
				copiados++;
			}
			return copiados;
		}

		/// <summary>
		/// Baja MEMFS entero a la carpeta, DESPUES de cada comando. Un fallo de escritura nunca pasa
		/// en silencio: sin esto la pantalla dejaria de estar respaldada y nada lo diria.
		/// </summary>
		public async ValueTask<int> BajarDeMemfsAsync()
		{
			if (ElDestinoEsElNavegador && FaltaReotorgarPermiso)
				throw new EscrituraRechazadaException(
					$"No hay permiso de escritura sobre '{NombreDeLaCarpeta}'. El navegador lo da aparte "
					+ "del de elegir la carpeta, y se lo quita cuando la app se vuelve a abrir. Volver a "
					+ "darlo lo recupera; hasta entonces nada de lo que ve se está guardando.");

			if (!destino.Disponible || !Directory.Exists(RaizEnMemfs)) return 0;

			int escritos = 0;
			var fallaron = new List<string>();

			foreach (string archivo in Directory.EnumerateFiles(RaizEnMemfs, "*", SearchOption.AllDirectories))
			{
				string relativa = Path.GetRelativePath(RaizEnMemfs, archivo).Replace(Path.DirectorySeparatorChar, '/');
				string base64 = Convert.ToBase64String(File.ReadAllBytes(archivo));

				ResultadoDeEscritura resultado = await destino.EscribirAsync(relativa, base64);
				if (resultado.Ok) escritos++;
				else fallaron.Add($"{relativa} ({resultado.Motivo})");
			}

			if (fallaron.Count > 0)
				throw new EscrituraRechazadaException(
					$"No se pudo guardar en '{destino.Nombre}': fallaron {fallaron.Count} de "
					+ $"{fallaron.Count + escritos} archivos. {string.Join("; ", fallaron)}");

			return escritos;
		}

		/// <summary>
		/// El almacenamiento del navegador COMO destino del journal. Esta clase ya tenia las tres
		/// operaciones y el estado del permiso, asi que las expone por la costura en vez de mudarlas: el
		/// modo suelto no cambia una linea de comportamiento.
		/// </summary>
		/// <remarks>
		/// Implementacion EXPLICITA a proposito: son para el mecanismo y no para la caratula, que sigue
		/// hablando de carpeta y de permiso, que es lo que la persona entiende.
		/// </remarks>
		string IDestinoDelJournal.Nombre => NombreDeLaCarpeta;

		bool IDestinoDelJournal.Disponible => HayCarpeta;

		async ValueTask<ResultadoDeListado> IDestinoDelJournal.ListarAsync() =>
			await (await ModuloAsync()).InvokeAsync<ResultadoDeListado>("listar");

		async ValueTask<ResultadoDeLectura> IDestinoDelJournal.LeerAsync(string ruta) =>
			await (await ModuloAsync()).InvokeAsync<ResultadoDeLectura>("leer", ruta);

		async ValueTask<ResultadoDeEscritura> IDestinoDelJournal.EscribirAsync(string ruta, string contenidoBase64) =>
			await (await ModuloAsync()).InvokeAsync<ResultadoDeEscritura>("escribir", ruta, contenidoBase64);

		/// <summary>Cuantos archivos y cuantos bytes tiene el journal en MEMFS ahora mismo.</summary>
		public static (int archivos, long bytes) EstadoDeMemfs()
		{
			if (!Directory.Exists(RaizEnMemfs)) return (0, 0);

			int archivos = 0;
			long bytes = 0;
			foreach (string archivo in Directory.EnumerateFiles(RaizEnMemfs, "*", SearchOption.AllDirectories))
			{
				archivos++;
				bytes += new FileInfo(archivo).Length;
			}
			return (archivos, bytes);
		}

		public async ValueTask DisposeAsync()
		{
			if (modulo is not null)
			{
				try { await modulo.DisposeAsync(); } catch (JSDisconnectedException) { }
				modulo = null;
			}
		}
	}

	/// <summary>Lo que contesta el navegador cuando se le pide una carpeta o un permiso.</summary>
	public sealed class ResultadoDeCarpeta
	{
		[JsonPropertyName("ok")]      public bool Ok { get; set; }
		[JsonPropertyName("nombre")]  public string Nombre { get; set; } = "";
		[JsonPropertyName("permiso")] public string Permiso { get; set; } = "";
		[JsonPropertyName("motivo")]  public string Motivo { get; set; } = "";

		public override string ToString() =>
			Ok ? $"ok, carpeta '{Nombre}' (permiso {Permiso})"
			   : $"no: {(Motivo.Length > 0 ? Motivo : Permiso)}";
	}

	/// <summary>Lo que contesta el navegador al escribir un archivo de la carpeta.</summary>
	public sealed class ResultadoDeEscritura
	{
		[JsonPropertyName("ok")]     public bool Ok { get; set; }
		[JsonPropertyName("motivo")] public string Motivo { get; set; } = "";
	}

	/// <summary>Lo que contesta el navegador al recorrer la carpeta.</summary>
	public sealed class ResultadoDeListado
	{
		[JsonPropertyName("ok")]     public bool Ok { get; set; }
		[JsonPropertyName("rutas")]  public string[] Rutas { get; set; } = Array.Empty<string>();
		[JsonPropertyName("motivo")] public string Motivo { get; set; } = "";
	}

	/// <summary>Lo que contesta el navegador al leer un archivo de la carpeta, en base64.</summary>
	public sealed class ResultadoDeLectura
	{
		[JsonPropertyName("ok")]        public bool Ok { get; set; }
		[JsonPropertyName("contenido")] public string Contenido { get; set; } = "";
		[JsonPropertyName("motivo")]    public string Motivo { get; set; } = "";
	}

	/// <summary>La carpeta rechazo una escritura: lo que la pantalla muestra dejo de estar respaldado.</summary>
	public sealed class EscrituraRechazadaException : Exception
	{
		public EscrituraRechazadaException(string mensaje) : base(mensaje) { }
	}

	/// <summary>La carpeta guardada ya no se puede leer: fue movida, renombrada o borrada.</summary>
	public sealed class CarpetaNoDisponibleException : Exception
	{
		public CarpetaNoDisponibleException(string mensaje) : base(mensaje) { }
	}
}
