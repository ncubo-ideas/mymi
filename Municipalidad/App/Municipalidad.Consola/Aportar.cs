using System;
using System.IO;
using Municipalidad.App;
using Puppeteer;

namespace Municipalidad.Consola
{
	/// <summary>
	/// EL APORTE DE LA PERSONA sobre una carpeta que ya trajo los hechos de la institucion. La
	/// municipalidad no publica cuanto quiere apartar por mes, asi que ninguna corrida de `refrescar` lo
	/// puede traer, y sin el la comparacion contra lo propuesto no se puede contestar.
	/// <para>
	/// La adopta DOS veces en una sola corrida: la segunda la tiene que rechazar el Check, porque se
	/// adopta una por dia. Un rechazo no deja entrada, asi que volver a correr esto no le agrega
	/// ningun hecho a la cuenta.
	/// </para>
	/// <para>
	/// NINGUN INSTANTE SALE DE UN RELOJ Y EL MONTO NO ENTRA POR ARGUMENTO: los dos son literales de la
	/// narrativa del demo. Una persona adopta su reserva en su app; esto siembra la del demo.
	/// </para>
	/// </summary>
	internal static class Aportar
	{
		private const decimal LaReservaQueLaPersonaAdopto = 20000m;

		private static readonly DateTime ElDiaEnQueLaAdopto = new DateTime(2026, 8, 1);

		internal static int Correr(string carpeta)
		{
			Directory.CreateDirectory(carpeta);
			Console.WriteLine("Journal: " + carpeta);
			Console.WriteLine();

			using (MunicipalidadApp app = new MunicipalidadApp(MunicipalidadApp.ElActor))
			{
				app.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
				app.Start();

				MunicipalidadAPI api = new MunicipalidadAPI(app);

				Paso("la reserva que la persona adopto",
					api.AdoptarReserva(LaReservaQueLaPersonaAdopto, ElDiaEnQueLaAdopto));
				Paso("la misma reserva otra vez",
					api.AdoptarReserva(LaReservaQueLaPersonaAdopto, ElDiaEnQueLaAdopto));

				Console.WriteLine();
				Console.WriteLine("Lo que la cuenta quedo diciendo:");
				Console.WriteLine();
				Console.WriteLine("  estado: " + api.EstadoDeLaCuenta());
				Console.WriteLine("  se puede ver el faltante de reserva : "
					+ api.SePuedeVerElFaltanteDeReserva());
				Console.WriteLine();
				Console.WriteLine("Lo que la institucion manda no entra por aca: eso lo trae 'refrescar',");
				Console.WriteLine("o la app misma al abrir la carpeta.");

				return 0;
			}
		}

		/// <summary>
		/// Imprime si un paso entro o se rechazo. El marcador de rechazo es la propiedad EWI en la raiz
		/// del documento, y no que el texto empiece con esas letras: con el formatter JSON el texto
		/// empieza con una llave.
		/// </summary>
		private static void Paso(string que, string salida)
		{
			string s = (salida ?? "").Trim();
			bool rechazado = s.Contains("\"EWI\"");
			Console.WriteLine("  " + (rechazado ? "rechazado " : "entro     ") + que
				+ (rechazado ? "  <- " + Motivo(s) : ""));
		}

		private static string Motivo(string documento)
		{
			int i = documento.IndexOf("\"Error\"", StringComparison.Ordinal);
			if (i < 0) return documento;
			int abre = documento.IndexOf('"', i + 8);
			int cierra = abre < 0 ? -1 : documento.IndexOf('"', abre + 1);
			return abre < 0 || cierra < 0 ? documento : documento.Substring(abre + 1, cierra - abre - 1);
		}
	}
}
