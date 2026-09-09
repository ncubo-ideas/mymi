using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// EL NUMERO DE SOCIO, el que va impreso en el carnet y el que Sonia le muestra a la
	/// bomba. De cara al cliente, y por eso es corto y se puede dictar por telefono.
	///
	/// NO ES LA LLAVE CON LA QUE LA APP PIDE SUS EVENTOS. Esa se cambia por un token y
	/// quien la tenga se baja el historial entero; ésta solo sirve para que la estacion
	/// sepa a quien acreditarle. La distincion es la razon de que este tipo exista: un
	/// string suelto se habria terminado llenando con la otra.
	/// </summary>
	internal sealed class NumeroDeSocio
	{
		internal string Texto { get; private set; }

		internal NumeroDeSocio(string texto)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(texto);

			Texto = texto.Trim().ToUpperInvariant();
			if (Texto.Length == 0)
				throw new ErrorDeDominio($"'{texto}' no deja nada al normalizar: no es un socio.");
		}

		internal bool EsElMismo(NumeroDeSocio otro)
		{
			ArgumentNullException.ThrowIfNull(otro);
			return Texto == otro.Texto;
		}
	}
}
