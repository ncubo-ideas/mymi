using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// El identificador del vehiculo. Llega de la estacion con cada carga y es lo que
	/// hace automatica la atribucion: Sonia nunca elige vehiculo a mano. Es tambien la
	/// llave de union con el modulo del concesionario: los dos publican por placa y la
	/// IA de Sonia hace el cruce, sin que los modulos se hablen.
	/// </summary>
	// Tipo propio y no string: el formato varia por pais y el nombre tambien (placa,
	// matricula, patente, chapa). NO es un conjunto cerrado -viene del wire- asi que la
	// comparacion es una operacion nombrada sobre el texto normalizado, nunca un Equals:
	// los diccionarios se llavean por Texto, no por este objeto.
	public sealed class Placa
	{
		/// <summary>El texto normalizado: mayusculas, sin espacios ni guiones. Es la
		/// llave con la que se busca y se cruza.</summary>
		public string Texto { get; private set; }

		public Placa(string texto)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(texto);

			Texto = Normalizar(texto);
			if (Texto.Length == 0)
				throw new ErrorDeDominio($"'{texto}' no deja nada al normalizar: no es una placa.");
		}

		private static string Normalizar(string texto)
		{
			var limpio = new System.Text.StringBuilder(texto.Length);
			foreach (char c in texto.Trim().ToUpperInvariant())
				if (char.IsLetterOrDigit(c)) limpio.Append(c);
			return limpio.ToString();
		}

		public bool EsLaMisma(Placa otra)
		{
			ArgumentNullException.ThrowIfNull(otra);
			return Texto == otra.Texto;
		}

		// Como se muestra: con un guion entre letras y numeros seria adivinar el formato
		// del pais. Se muestra normalizada tal cual; abreviarla cuando ya hay alias es
		// decision de la pantalla (pregunta abierta 11 del brief).
		public string ParaMostrar => Texto;
	}
}
