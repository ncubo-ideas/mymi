using System;
using System.IO;
using Municipalidad.App;
using Puppeteer;

namespace Municipalidad.Mcp
{
	/// <summary>
	/// La carpeta de la persona, leida. Abre el journal para una pregunta, contesta y lo cierra;
	/// nunca lo abre con el anfitrion de la app.
	/// </summary>
	internal sealed class LectorDeJournal
	{
		private readonly string carpeta;

		internal LectorDeJournal(string carpeta)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(carpeta);

			this.carpeta = Path.GetFullPath(carpeta);
		}

		/// <summary>La carpeta, resuelta a ruta completa: es la que viaja en las instrucciones.</summary>
		internal string Carpeta
		{
			get { return carpeta; }
		}

		/// <summary>Si la app se abrio alguna vez en esta computadora.</summary>
		internal bool HayJournal
		{
			get { return MunicipalidadApp.HayJournalEn(carpeta); }
		}

		/// <summary>
		/// Le hace UNA pregunta a la puerta sobre el journal de la persona, y contesta con
		/// <paramref name="cuandoNoHayDatos"/> si la app nunca se abrio aca o no alcanzo a escribir
		/// nada. Nunca deja entrada: el anfitrion que abre no siembra.
		/// </summary>
		internal T Leer<T>(Func<MunicipalidadAPI, T> pregunta, Func<T> cuandoNoHayDatos)
		{
			ArgumentNullException.ThrowIfNull(pregunta);
			ArgumentNullException.ThrowIfNull(cuandoNoHayDatos);

			if (!HayJournal) return cuandoNoHayDatos();

			AppDeMunicipalidadSoloLectura lector = new AppDeMunicipalidadSoloLectura();
			try
			{
				lector.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
				lector.Start();

				if (lector.Actor.CurrentEntryId == 0) return cuandoNoHayDatos();

				return pregunta(new MunicipalidadAPI(lector));
			}
			finally
			{
				try { lector.Actor.GracefulExit(); } catch { }
			}
		}
	}
}
