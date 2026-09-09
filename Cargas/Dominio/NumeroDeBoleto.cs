using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// La identidad de un boleto de tombola. Es el numero que la estacion imprime y el
	/// que se rifa, asi que NO lo puede inventar la app: un "boleto #7" fabricado aca no
	/// sirve para reclamar nada.
	///
	/// Por eso mismo es la guarda de idempotencia natural de su evento, igual que el
	/// numero de transaccion lo es para una carga.
	/// </summary>
	internal sealed class NumeroDeBoleto
	{
		internal string Texto { get; private set; }

		internal NumeroDeBoleto(string texto)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(texto);

			Texto = texto.Trim();
			if (Texto.Length == 0)
				throw new ErrorDeDominio($"'{texto}' no deja nada al normalizar: no es un boleto.");
		}

		internal bool EsElMismo(NumeroDeBoleto otro)
		{
			ArgumentNullException.ThrowIfNull(otro);
			return string.Equals(Texto, otro.Texto, StringComparison.OrdinalIgnoreCase);
		}
	}
}
