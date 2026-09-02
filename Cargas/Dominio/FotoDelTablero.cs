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
	internal abstract class FotoDelTablero
	{
		/// <summary>La ausencia, como instancia unica.</summary>
		internal static readonly FotoDelTablero Ninguna = new SinFoto();

		internal static FotoDelTablero Guardada(string nombreDeArchivo) =>
			new FotoGuardada(nombreDeArchivo);

		internal abstract bool Existe { get; }

		/// <summary>Revienta si no hay foto: consultar antes Existe.</summary>
		internal abstract string NombreDeArchivo { get; }

		private sealed class SinFoto : FotoDelTablero
		{
			internal override bool Existe => false;

			internal override string NombreDeArchivo =>
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

			internal override bool Existe => true;
			internal override string NombreDeArchivo => nombreDeArchivo;
		}
	}
}
