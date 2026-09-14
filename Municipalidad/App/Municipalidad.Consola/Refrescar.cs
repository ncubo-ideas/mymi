using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Municipalidad.App;
using Municipalidad.App.Events;
using Puppeteer;
using Puppets.Portador;

namespace Municipalidad.Consola
{
	/// <summary>
	/// EL CICLO DE INGESTA CONTRA EL ENDPOINT DE EVENTOS. Es lo que `medir` no puede ver: la fuente HTTP,
	/// el proveedor de token, la rotacion de la credencial y el bucle de refresh, que hasta que existio un
	/// feed no tenian contra que correr.
	/// <para>
	/// Se corre DOS veces contra el mismo bloque: la segunda tiene que aplicar CERO y dejar el arbol del
	/// journal byte a byte igual, que es la idempotencia de la ingesta vista desde afuera.
	/// </para>
	/// </summary>
	internal static class Refrescar
	{
		internal static async Task<int> CorrerAsync(
			string carpeta, string url, string recurso, string id)
		{
			Console.WriteLine("Journal: " + carpeta);
			Console.WriteLine("Feed:    " + url + recurso + "/" + id + "/events?after=N");
			Console.WriteLine();

			Directory.CreateDirectory(carpeta);

			using (HttpClient http = new HttpClient { BaseAddress = new Uri(url) })
			using (MunicipalidadApp app = new MunicipalidadApp(MunicipalidadApp.ElActor))
			{
				app.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
				app.Start();

				MunicipalidadAPI api = new MunicipalidadAPI(app);
				ProveedorDeToken proveedor = new ProveedorDeToken(http);

				BucleDeRefresh bucle = new BucleDeRefresh(
					api,
					new FuenteHttpDeEventos(
						http, recurso, id,
						ct => LaCredencialAsync(api, proveedor, recurso, id, ct),
						ct => RenovarAsync(api, proveedor, recurso, id, ct)),
					MunicipalidadEventRoute.Route);

				Console.WriteLine("-- refresh 1 --");
				Console.WriteLine("   punto de control antes   : " + api.UltimoEventoAplicado());
				ResultadoDeRefresh primero = await bucle.Ejecutar().ConfigureAwait(false);
				Console.WriteLine("   " + primero);
				Console.WriteLine("   punto de control despues : " + api.UltimoEventoAplicado());
				Console.WriteLine();

				string antes = ElArbolDe(carpeta);

				Console.WriteLine("-- refresh 2, el mismo bloque --");
				ResultadoDeRefresh segundo = await bucle.Ejecutar().ConfigureAwait(false);
				Console.WriteLine("   " + segundo);
				Console.WriteLine("   punto de control despues : " + api.UltimoEventoAplicado());
				Console.WriteLine("   arbol byte a byte igual  : " + (ElArbolDe(carpeta) == antes));
				Console.WriteLine();

				LoQueLaCuentaQuedoDiciendo(api);

				return 0;
			}
		}

		/// <summary>
		/// La credencial con que se leen los eventos: registra esta instalacion y la guarda, UNA vez. La
		/// clave del dispositivo y el token viven en el journal desde ahi.
		/// <para>
		/// La pide LA FUENTE y no el bucle, que es lo que separa leer por HTTP -- que necesita token --
		/// de leer por el puente de Gnothi, que no lo necesita porque la credencial la sostiene Gnothi.
		/// </para>
		/// </summary>
		private static async Task<string> LaCredencialAsync(
			MunicipalidadAPI api, ProveedorDeToken proveedor, string recurso, string id,
			CancellationToken ct)
		{
			string clave = api.ClaveDeDispositivoGuardada();

			if (clave.Length == 0)
			{
				clave = "DISPOSITIVO-DE-LA-CONSOLA";
				Console.WriteLine("   registra el dispositivo  : " + Dicho(api.RegistrarDispositivo(clave)));
			}

			string guardado = api.TokenGuardado();

			if (guardado.Length > 0)
			{
				Console.WriteLine("   credencial               : ya estaba guardada");
				Console.WriteLine();
				return guardado;
			}

			Credencial credencial = await proveedor.Pedir(clave, recurso, id, ct).ConfigureAwait(false);

			Console.WriteLine("   guarda la credencial     : "
				+ Dicho(api.GuardarCredencial(credencial.AccessToken, credencial.RefreshToken)));
			Console.WriteLine();

			return credencial.AccessToken;
		}

		/// <summary>
		/// Que hacer cuando el proveedor rechaza el token: rotarlo si hay refresco, y si no, pedir una
		/// credencial nueva con la clave de esta instalacion. Devuelve el access token nuevo.
		/// </summary>
		private static async Task<string> RenovarAsync(MunicipalidadAPI api, ProveedorDeToken proveedor,
			string recurso, string id, CancellationToken ct)
		{
			string refresco = api.TokenDeRefrescoGuardado();

			Credencial nueva = refresco.Length > 0
				? await proveedor.Rotar(refresco, ct).ConfigureAwait(false)
				: null;

			nueva ??= await proveedor
				.Pedir(api.ClaveDeDispositivoGuardada(), recurso, id, ct)
				.ConfigureAwait(false);

			api.GuardarCredencial(nueva.AccessToken, nueva.RefreshToken);

			return nueva.AccessToken;
		}

		private static void LoQueLaCuentaQuedoDiciendo(MunicipalidadAPI api)
		{
			Console.WriteLine("Lo que la cuenta quedo diciendo:");
			Console.WriteLine();
			Console.WriteLine("  estado: " + api.EstadoDeLaCuenta());
			Console.WriteLine();

			foreach (string rubro in api.RubrosConHistoria())
			{
				RegistroDelHistorial historia = api.HistorialDe(rubro, ConsultasDelModulo.IdiomaDelRegistro);

				Console.WriteLine("  " + rubro + ": " + historia.EstadoDicho);

				for (int fila = 0; fila < historia.Periodo.Length; fila++)
					Console.WriteLine("    " + historia.Periodo[fila]
						+ "  monto " + historia.Monto[fila]
						+ "  mora " + historia.Mora[fila]
						+ "  cubre " + historia.Cubre[fila]);
			}

			Console.WriteLine();

			foreach (string numero in api.NumerosDeLasFincas())
				Console.WriteLine("  finca " + numero);
		}

		private static string Dicho(string respuesta)
		{
			return respuesta.Length == 0 ? "entro" : "rechazado <- " + respuesta;
		}

		private static string ElArbolDe(string raiz)
		{
			string[] archivos = Directory.GetFiles(raiz, "*", SearchOption.AllDirectories);
			Array.Sort(archivos, StringComparer.Ordinal);

			System.Text.StringBuilder huella = new System.Text.StringBuilder();

			foreach (string archivo in archivos)
			{
				huella.Append(archivo.Substring(raiz.Length)).Append(' ');

				using (FileStream bytes = new FileStream(
					archivo, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
					huella.Append(Convert.ToHexString(md5.ComputeHash(bytes)));

				huella.Append('\n');
			}

			return huella.ToString();
		}
	}
}
