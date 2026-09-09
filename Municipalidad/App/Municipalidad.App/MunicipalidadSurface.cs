using System;
using System.IO;
using Gnothi.Contract;
using Puppeteer;

namespace Municipalidad.App
{
	/// <summary>
	/// La superficie con que este modulo viaja como carga de Gnothi: el mismo dominio y las mismas
	/// respuestas, expuestas al Host SIN el SDK del asistente.
	/// <para>
	/// Lleva las etiquetas del contrato de Gnothi en vez de las del SDK, que pesa mas de un megabyte: por
	/// eso el paquete de carga son dos dll pequenos -- este y el dominio -- y el motor, la coreografia y
	/// el contrato se los presta el Host. El Host construye esta clase pasandole la carpeta que le
	/// concede: el modulo no elige la carpeta ni abre el journal por su cuenta.
	/// </para>
	/// <para>
	/// ES DE SOLO LECTURA, y son dos disciplinas y no una: ninguna consulta manda un comando, y el
	/// anfitrion que abre es el que no siembra. El de la app deja una entrada al hidratar, asi que abrir
	/// con el pondria una entrada en el journal de la persona por cada pregunta del Host.
	/// </para>
	/// </summary>
	[ModuleSurface]
	public sealed class MunicipalidadSurface
	{
		private readonly string carpeta;

		public MunicipalidadSurface(string carpetaConcedida)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(carpetaConcedida);

			carpeta = Path.GetFullPath(carpetaConcedida);
		}

		/// <summary>Una linea de resumen: lo que una pantalla del Host pone arriba.</summary>
		[Query(ConsultasDelModulo.Resumen)]
		public object Resumen() =>
			Leer(api => ConsultasDelModulo.HacerResumen(api, ElInstanteEnQueSePregunto()));

		/// <summary>Lo que se vence y lo que ya vencio, en filas planas.</summary>
		[Query(ConsultasDelModulo.ListaDeVencimientos)]
		public object ListaDeVencimientos() =>
			Leer(api => ConsultasDelModulo.HacerListaDeVencimientos(api, ElInstanteEnQueSePregunto()));

		/// <summary>
		/// Abre el journal para UNA pregunta y lo cierra. Se rehidrata en cada consulta a proposito: la
		/// app instalada escribe en esta carpeta mientras este proceso vive, asi que un actor guardado
		/// contestaria con el estado de la primera pregunta.
		/// </summary>
		private object Leer(Func<MunicipalidadAPI, object> pregunta)
		{
			if (!MunicipalidadApp.HayJournalEn(carpeta)) return ConsultasDelModulo.SinDatos(carpeta);

			AppDeMunicipalidadSoloLectura lector = new AppDeMunicipalidadSoloLectura();
			try
			{
				lector.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
				lector.Start();

				if (lector.Actor.CurrentEntryId == 0) return ConsultasDelModulo.SinDatos(carpeta);

				return pregunta(new MunicipalidadAPI(lector));
			}
			finally
			{
				try { lector.Actor.GracefulExit(); } catch { }
			}
		}

		/// <summary>
		/// El instante en que el Host pregunta, en UTC y truncado al segundo, que es la resolucion con la
		/// que el journal guarda un instante. Es argumento de una CONSULTA y por eso NO es monotono: una
		/// consulta no escribe nada, asi que exigir que avance rechazaria una segunda pregunta legitima un
		/// segundo despues.
		/// </summary>
		private static DateTime ElInstanteEnQueSePregunto()
		{
			DateTime ahora = DateTime.UtcNow;

			return new DateTime(ahora.Ticks - ahora.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
		}
	}
}
