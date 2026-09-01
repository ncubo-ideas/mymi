using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// UN dato que falta, con lo que desbloquea. Es la consulta que motiva el ingreso:
	/// no dice "completa tu perfil", dice que carga esta trunca, que dato le falta y que
	/// calculo aparece si Sonia lo pone. La mitad ya esta hecha -los kilometros salen de
	/// los odometros que SI puso- y ver el trabajo a medio hacer motiva mas que un
	/// formulario en blanco.
	/// </summary>
	internal sealed class FaltanteDeDato
	{
		internal DateTime FechaDeLaCarga { get; private set; }
		internal string NumeroDeLaTransaccion { get; private set; }

		/// <summary>"el kilometraje", "la confirmacion del tanque".</summary>
		internal string QueFalta { get; private set; }

		/// <summary>Lo que aparece si lo pone: "el rendimiento de tu carro en estos
		/// 820 km entre el 20/06 y el 25/08".</summary>
		internal string QueDesbloquea { get; private set; }

		internal FaltanteDeDato(DateTime fechaDeLaCarga, string numeroDeLaTransaccion,
			string queFalta, string queDesbloquea)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(numeroDeLaTransaccion);
			ArgumentNullException.ThrowIfNullOrWhiteSpace(queFalta);

			FechaDeLaCarga = fechaDeLaCarga;
			NumeroDeLaTransaccion = numeroDeLaTransaccion;
			QueFalta = queFalta;
			QueDesbloquea = queDesbloquea ?? "";
		}

		internal string ParaSonia =>
			$"necesito {QueFalta} de tu carga del {FechaDeLaCarga:dd/MM/yyyy}"
			+ (QueDesbloquea.Length > 0 ? $" para calcular {QueDesbloquea}" : "");
	}
}
