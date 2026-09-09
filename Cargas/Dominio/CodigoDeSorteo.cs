using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// La identidad de un sorteo, como la publica la estacion: "SORTEO-2026-12". Es la
	/// llave con la que un boleto se ata al suyo, y por eso es un tipo y no un string
	/// suelto — igual que la placa y el numero de transaccion.
	/// </summary>
	internal sealed class CodigoDeSorteo
	{
		internal string Texto { get; private set; }

		internal CodigoDeSorteo(string texto)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(texto);

			Texto = texto.Trim().ToUpperInvariant();
			if (Texto.Length == 0)
				throw new ErrorDeDominio($"'{texto}' no deja nada al normalizar: no es un sorteo.");
		}

		internal bool EsElMismo(CodigoDeSorteo otro)
		{
			ArgumentNullException.ThrowIfNull(otro);
			return Texto == otro.Texto;
		}
	}
}
