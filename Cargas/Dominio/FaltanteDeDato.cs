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

		/// <summary>LO QUE LA APP SUPONE y esta pidiendo confirmar: "lleno" o "parcial".
		/// Vacio cuando el faltante no nace de una inferencia — un kilometraje no se
		/// infiere de nada, asi que no hay nada que confirmar, hay que ponerlo.</summary>
		internal string LoQueSeSupone { get; private set; }

		/// <summary>De donde sale la suposicion: "el surtidor cortó solo".</summary>
		internal string PorQueSeSupone { get; private set; }

		internal bool NaceDeUnaSuposicion => LoQueSeSupone.Length > 0;

		/// <summary>LA FRASE QUE HAY QUE ENUNCIAR ANTES DE PREGUNTAR.
		///
		/// La pantalla pedia confirmar una afirmacion que nunca decia —"necesito la
		/// confirmacion del tanque de tu carga del 25/08/2026"— y ofrecia "si, quedo asi"
		/// sin decir que era "asi". Ademas los dos botones eran los mismos tanto si la
		/// inferencia decia lleno como parcial, asi que ni siquiera se podian derivar
		/// de la pantalla.</summary>
		internal string LaSuposicion => !NaceDeUnaSuposicion
			? ""
			: $"{PorQueSeSupone}, así que suponemos que el tanque "
			  + (LoQueSeSupone == EstadoDelTanque.Lleno.Nombre ? "quedó lleno" : "no quedó lleno");

		internal FaltanteDeDato(DateTime fechaDeLaCarga, string numeroDeLaTransaccion,
			string queFalta, string queDesbloquea,
			string loQueSeSupone = "", string porQueSeSupone = "")
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(numeroDeLaTransaccion);
			ArgumentNullException.ThrowIfNullOrWhiteSpace(queFalta);

			FechaDeLaCarga = fechaDeLaCarga;
			NumeroDeLaTransaccion = numeroDeLaTransaccion;
			QueFalta = queFalta;
			QueDesbloquea = queDesbloquea ?? "";
			LoQueSeSupone = loQueSeSupone ?? "";
			PorQueSeSupone = porQueSeSupone ?? "";
		}

		/// <summary>Cuando hay suposicion, la frase ES la suposicion y su consecuencia:
		/// enunciar y despues preguntar. Cuando no la hay, es el pedido de siempre.</summary>
		internal string ParaSonia => NaceDeUnaSuposicion
			? LaSuposicion
			  + (QueDesbloquea.Length > 0 ? $". Si es así, podemos calcular {QueDesbloquea}" : "")
			: $"necesito {QueFalta} de tu carga del {FechaDeLaCarga:dd/MM/yyyy}"
			  + (QueDesbloquea.Length > 0 ? $" para calcular {QueDesbloquea}" : "");
	}
}
