using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// La foto del tablero que respalda una lectura de odometro. El dominio guarda solo
	/// el NOMBRE del archivo -el binario vive en la carpeta de la app, que es frontera
	/// de la caratula-. Tiene valor propio aunque nadie la lea: es el respaldo de un dato
	/// que, por ser aporte de Sonia, no tiene otra evidencia. La procedencia se vuelve
	/// auditable.
	/// </summary>
	public abstract class FotoDelTablero
	{
		/// <summary>La ausencia, como instancia unica.</summary>
		public static readonly FotoDelTablero Ninguna = new SinFoto();

		public static FotoDelTablero Guardada(string nombreDeArchivo) =>
			new FotoGuardada(nombreDeArchivo);

		public abstract bool Existe { get; }

		/// <summary>Revienta si no hay foto: consultar antes Existe.</summary>
		public abstract string NombreDeArchivo { get; }

		private sealed class SinFoto : FotoDelTablero
		{
			public override bool Existe => false;

			public override string NombreDeArchivo =>
				throw new ErrorDeDominio(
					"Esta carga no tiene foto del tablero: preguntar por NombreDeArchivo "
					+ "exige consultar antes Existe.");
		}

		private sealed class FotoGuardada : FotoDelTablero
		{
			private readonly string nombreDeArchivo;

			internal FotoGuardada(string nombreDeArchivo)
			{
				ArgumentNullException.ThrowIfNullOrWhiteSpace(nombreDeArchivo);
				this.nombreDeArchivo = nombreDeArchivo.Trim();
			}

			public override bool Existe => true;
			public override string NombreDeArchivo => nombreDeArchivo;
		}
	}
}
