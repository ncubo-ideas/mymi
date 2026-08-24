using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Facturas.App;
using Facturas.App.Eventos;
using Puppeteer;
using Puppets.Portador;

namespace Facturas.Consola
{
	internal static class Program
	{
		private const string NombreDelActor = "facturas";

		private static async Task<int> Main(string[] args)
		{
			if (args.Length == 0) { Ayuda(); return 1; }

			try
			{
				switch (args[0])
				{
					case "medir":     return Medir(Carpeta(args, 1));
					case "estado":    return Estado(Carpeta(args, 1));
					// El token ya no se pega a mano: la app lo pide sola contra su device key.
					case "refrescar": return await Refrescar(args);
					default:          Ayuda(); return 1;
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error: {ex.GetType().Name}: {ex.Message}");
				for (Exception interna = ex.InnerException; interna != null; interna = interna.InnerException)
					Console.Error.WriteLine($"  causa: {interna.GetType().FullName}: {interna.Message}");
				return 2;
			}
		}

		// Abre el puppet sobre una carpeta real. Es el mismo par
		// ConfigureStorage + Start que va a hacer la caratula del browser, solo que
		// ahi el path apunta a MEMFS.
		private static FacturasApp Abrir(string carpeta)
		{
			Directory.CreateDirectory(carpeta);

			var app = new FacturasApp(NombreDelActor);
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
			app.Start();
			return app;
		}

		// ---------------------------------------------------------------- fase 1
		private static int Medir(string carpeta)
		{
			Console.WriteLine($"carpeta : {carpeta}");
			FacturasApp app = Abrir(carpeta);
			var api = new FacturasAPI(app);
			IDiarioDeLaApp diario = api;

			Console.WriteLine($"entrada inicial del journal : {app.Actor.CurrentEntryId}");
			Console.WriteLine($"ultimo evento procesado     : {api.UltimoEventoProcesado()}");

			int aplicados = 0, descartados = 0, rechazados = 0;
			foreach (SenalDeEvento senal in Muestras.TresFacturas())
			{
				IMensajeDeEvento mensaje = RutaDeFacturas.Rutear(senal);
				if (mensaje == null) { descartados++; continue; }

				string rechazo = diario.Aplicar(mensaje);
				if (rechazo.Length > 0)
				{
					rechazados++;
					Console.WriteLine($"  rechazado {senal.Numero}: {rechazo}");
					continue;
				}
				aplicados++;
			}
			Console.WriteLine($"aplicados={aplicados} descartados={descartados} rechazados={rechazados}");

			// Un tipo que esta app no consume: la ruta devuelve null y se descarta.
			bool descartaDesconocido = RutaDeFacturas.Rutear(Muestras.TipoDesconocido()) == null;
			Console.WriteLine($"descarta tipo desconocido   : {descartaDesconocido}");

			ImprimirEstado(app, api);

			// ---- criterio: un evento ya procesado se rechaza y NO deja entrada.
			long antes = app.Actor.CurrentEntryId;
			string mensajeDeRechazo = diario.Aplicar(RutaDeFacturas.Rutear(Muestras.TresFacturas()[0]));
			long despues = app.Actor.CurrentEntryId;

			Console.WriteLine();
			Console.WriteLine("-- reintento de un evento ya procesado --");
			Console.WriteLine($"mensaje del Check           : [{mensajeDeRechazo}]");
			Console.WriteLine($"entrada antes / despues     : {antes} / {despues}");
			Console.WriteLine($"NO dejo entrada             : {antes == despues}");
			Console.WriteLine($"total sin cambio            : {api.TotalDeFacturas() == Muestras.TotalEsperado}");

			app.Actor.GracefulExit();
			return 0;
		}

		// Se corre en un proceso NUEVO: si el total sigue, el replay rehidrato.
		private static int Estado(string carpeta)
		{
			FacturasApp app = Abrir(carpeta);
			ImprimirEstado(app, new FacturasAPI(app));
			app.Actor.GracefulExit();
			return 0;
		}

		private static void ImprimirEstado(FacturasApp app, FacturasAPI api)
		{
			decimal total = api.TotalDeFacturas();

			Console.WriteLine();
			Console.WriteLine($"entrada del journal         : {app.Actor.CurrentEntryId}");
			Console.WriteLine($"ultimo evento procesado     : {api.UltimoEventoProcesado()}");
			Console.WriteLine($"cantidad de facturas        : {api.CantidadDeFacturas()}");
			Console.WriteLine($"total                       : {total.ToString("N2", CultureInfo.InvariantCulture)}");
			Console.WriteLine($"total esperado              : {Muestras.TotalEsperado.ToString("N2", CultureInfo.InvariantCulture)}");
			Console.WriteLine($"TOTAL CORRECTO              : {total == Muestras.TotalEsperado}");
			Console.WriteLine($"token guardado              : [{api.TokenGuardado()}]");
			Console.WriteLine();
			Console.WriteLine("-- lista (print, formatter JSON) --");
			Console.WriteLine(api.ListaDeFacturas());
			Console.WriteLine();
			Console.WriteLine("-- detalle de TQ-0001 --");
			Console.WriteLine(api.DetalleDeFactura("TQ-0001"));
		}


		// ---------------------------------------------------------------- fase 2
		private static async Task<int> Refrescar(string[] args)
		{
			string carpeta = Carpeta(args, 1);
			string baseUrl = Opcional(args, 2, "http://localhost:8081/");
			string recurso = Opcional(args, 3, "clientes");
			string id      = Opcional(args, 4, "1");

			using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };
			FacturasApp app = Abrir(carpeta);
			var api = new FacturasAPI(app);

			var proveedorDeToken = new ProveedorDeToken(http);

			// La app consigue su credencial SOLA, contra su device key. Ni la consola ni
			// la persona pegan un token: el proveedor lo emite para ESTA instalacion.
			if (api.DeviceKeyGuardado().Length == 0)
				api.RegistrarDevice("device-consola-de-medicion");

			if (api.TokenGuardado().Length == 0)
			{
				Credencial credencial = await proveedorDeToken.Pedir(api.DeviceKeyGuardado(), recurso, id);
				Console.WriteLine($"credencial: scope={credencial.Scope} expiresIn={credencial.ExpiresIn}s");
				api.GuardarCredencial(credencial.AccessToken, credencial.RefreshToken);
			}

			// Que hacer cuando el proveedor rechaza el access token: rotar con el refresh
			// token, que es para lo que existe. El access es corto a proposito.
			async Task<string> Renovar(System.Threading.CancellationToken ct)
			{
				string refresh = api.RefreshTokenGuardado();

				// Rotar devuelve null si el refresh ya no vale; ahi se pide una credencial
				// nueva con el device key, que es lo que la instalacion siempre puede hacer.
				Credencial renovada = refresh.Length > 0
					? await proveedorDeToken.Rotar(refresh, ct)
					: null;
				string comoSeObtuvo = renovada != null ? "rotada con el refresh token" : "nueva contra el device key";
				renovada ??= await proveedorDeToken.Pedir(api.DeviceKeyGuardado(), recurso, id, ct);

				Console.WriteLine($"  credencial renovada: {comoSeObtuvo} (scope {renovada.Scope})");
				api.GuardarCredencial(renovada.AccessToken, renovada.RefreshToken);
				return renovada.AccessToken;
			}

			var bucle = new BucleDeRefresh(api, new FuenteHttpDeEventos(http, recurso, id),
				RutaDeFacturas.Rutear, null, Renovar);

			ResultadoDeRefresh primero = await bucle.Ejecutar();
			Console.WriteLine($"refresh 1 : {primero}");

			// Segundo refresh sin novedad: debe traer 0 y no aplicar nada.
			long antes = app.Actor.CurrentEntryId;
			ResultadoDeRefresh segundo = await bucle.Ejecutar();
			long despues = app.Actor.CurrentEntryId;
			Console.WriteLine($"refresh 2 : {segundo}");
			Console.WriteLine($"sin novedad no escribio     : {antes == despues}");

			Console.WriteLine();
			Console.WriteLine($"cantidad de facturas        : {api.CantidadDeFacturas()}");
			Console.WriteLine($"total                       : {api.TotalDeFacturas().ToString("N2", CultureInfo.InvariantCulture)}");
			Console.WriteLine($"ultimo evento procesado     : {api.UltimoEventoProcesado()}");

			app.Actor.GracefulExit();
			return 0;
		}

		// ---------------------------------------------------------------- argumentos
		private static string Carpeta(string[] args, int i) =>
			Path.GetFullPath(Opcional(args, i, Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "APP1-facturas")));

		private static string Opcional(string[] args, int i, string porDefecto) =>
			args.Length > i && args[i].Length > 0 ? args[i] : porDefecto;

		private static string Requerido(string[] args, int i, string nombre)
		{
			if (args.Length <= i || args[i].Length == 0)
				throw new ArgumentException($"Falta el argumento {nombre}.");
			return args[i];
		}

		private static void Ayuda()
		{
			Console.WriteLine("APP1 (facturas) - caratula de consola, para medir el dominio y el refresh");
			Console.WriteLine();
			Console.WriteLine("  medir     [carpeta]                            aplica 3 eventos fijos y mide los criterios de fase 1");
			Console.WriteLine("  estado    [carpeta]                            abre el journal y reporta (correr en proceso nuevo)");
			Console.WriteLine("  refrescar [carpeta] [baseUrl] [recurso] [id]   trae eventos del proveedor y los aplica de a uno");
			Console.WriteLine();
			Console.WriteLine("carpeta por defecto: %USERPROFILE%\\APP1-facturas");
			Console.WriteLine("baseUrl por defecto: http://localhost:8081/");
		}
	}
}
