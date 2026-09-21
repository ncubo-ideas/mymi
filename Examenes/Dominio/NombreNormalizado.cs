using System;
using System.Text;

namespace Examenes.Dominio
{
	// COMO SE COMPARAN DOS NOMBRES que llegan de afuera: sin acentos, sin mayusculas y sin
	// nada que no sea letra o digito.
	//
	// Existe porque la misma operacion hace falta en tres fronteras -Analito, ColorDeOrina
	// y ColorDeHeces- y es MECANICA, no significado. Cada conjunto cerrado sigue decidiendo
	// que nombres acepta y que significan; esto solo dice como se compara un texto con otro.
	//
	// Compartir el significado seria la generalizacion que este dominio evita; compartir el
	// plegado de acentos no lo es. Copiarlo tres veces tampoco mejora nada.
	//
	// Los acentos se pliegan con una tabla explicita y no con String.Normalize a proposito:
	// la tabla dice exactamente que caracteres importan -son los del espanol y son siete- y
	// no depende de que los datos de globalizacion viajen en el publish, que en wasm es una
	// decision que se puede cambiar sin pensar en esto.
	internal static class NombreNormalizado
	{
		internal static string De(string nombre)
		{
			var limpio = new StringBuilder(nombre.Length);

			foreach (char c in nombre)
			{
				char sinAcento = SinAcento(c);
				if (char.IsLetterOrDigit(sinAcento))
					limpio.Append(char.ToUpperInvariant(sinAcento));
			}

			return limpio.ToString();
		}

		internal static bool EsElMismo(string uno, string otro) => De(uno) == De(otro);

		/// <summary>
		/// EL MISMO NOMBRE COMO CODIGO DE UNA CLAVE DE FRASE: minusculas, sin acentos y con guion
		/// donde habia cualquier otra cosa. Se deriva del nombre y no se escribe aparte para que un
		/// analito no pueda llamarse de dos maneras sin que nada avise.
		/// </summary>
		internal static string EnClave(string nombre)
		{
			var clave = new StringBuilder(nombre.Length);
			bool guionPendiente = false;

			foreach (char c in nombre)
			{
				char sinAcento = SinAcento(c);
				if (char.IsLetterOrDigit(sinAcento))
				{
					if (guionPendiente && clave.Length > 0) clave.Append('-');
					guionPendiente = false;
					clave.Append(char.ToLowerInvariant(sinAcento));
					continue;
				}
				guionPendiente = true;
			}

			return clave.ToString();
		}

		private static char SinAcento(char c)
		{
			switch (c)
			{
				case 'á': case 'à': case 'ä': case 'â': return 'a';
				case 'é': case 'è': case 'ë': case 'ê': return 'e';
				case 'í': case 'ì': case 'ï': case 'î': return 'i';
				case 'ó': case 'ò': case 'ö': case 'ô': return 'o';
				case 'ú': case 'ù': case 'ü': case 'û': return 'u';
				case 'ñ': return 'n';
				case 'Á': case 'À': case 'Ä': case 'Â': return 'A';
				case 'É': case 'È': case 'Ë': case 'Ê': return 'E';
				case 'Í': case 'Ì': case 'Ï': case 'Î': return 'I';
				case 'Ó': case 'Ò': case 'Ö': case 'Ô': return 'O';
				case 'Ú': case 'Ù': case 'Ü': case 'Û': return 'U';
				case 'Ñ': return 'N';
				default: return c;
			}
		}
	}
}
