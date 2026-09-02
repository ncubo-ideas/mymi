using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// La identidad del hecho segun la institucion. Es la llave con la que Sonia adjunta
	/// sus aportes -el kilometraje, el tanque, la foto- a UNA carga concreta.
	/// </summary>
	// Como Placa: viene del wire, no es conjunto cerrado, y la comparacion es una
	// operacion nombrada sobre el texto normalizado. Los diccionarios se llavean por
	// Texto, no por el objeto.
	public sealed class NumeroDeTransaccion
	{
		public string Texto { get; private set; }

		public NumeroDeTransaccion(string texto)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(texto);
			Texto = texto.Trim();
		}

		public bool EsElMismo(NumeroDeTransaccion otro)
		{
			ArgumentNullException.ThrowIfNull(otro);
			return string.Equals(Texto, otro.Texto, StringComparison.OrdinalIgnoreCase);
		}
	}
}
