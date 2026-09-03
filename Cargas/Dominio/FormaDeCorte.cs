using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// COMO SE DETUVO EL SURTIDOR. Un surtidor se detiene de dos maneras: por corte
	/// AUTOMATICO cuando el tanque se llena, o porque alguien lo PRESETEO a un monto o
	/// a un volumen ("deme cinco mil colones"). La huella queda en los datos, y es lo
	/// que permite a la institucion INFERIR el tanque lleno sin que Sonia diga nada:
	/// el hallazgo del §6.3.3 del brief.
	/// </summary>
	// Gracias a esta inferencia, de los dos aportes de Sonia el tanque pasa de "hay que
	// convencerla" a "que lo confirme", y el odometro queda como el unico campo que de
	// verdad depende de ella.
	internal sealed class FormaDeCorte
	{
		internal static readonly FormaDeCorte Automatico = new FormaDeCorte("automatico");
		internal static readonly FormaDeCorte Preseteado = new FormaDeCorte("preseteado");
		internal static readonly FormaDeCorte Indeterminado = new FormaDeCorte("indeterminado");

		internal string Nombre { get; private set; }

		private FormaDeCorte(string nombre)
		{
			Nombre = nombre;
		}

		// La query de reconocimiento para la frontera: decide sin reventar.
		internal static bool EsConocida(string nombre)
		{
			if (string.IsNullOrWhiteSpace(nombre)) return false;
			string normalizado = nombre.Trim().ToLowerInvariant();
			return normalizado == Automatico.Nombre || normalizado == Preseteado.Nombre
				|| normalizado == Indeterminado.Nombre;
		}

		internal static FormaDeCorte PorNombre(string nombre)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);

			string normalizado = nombre.Trim().ToLowerInvariant();
			if (normalizado == Automatico.Nombre) return Automatico;
			if (normalizado == Preseteado.Nombre) return Preseteado;
			if (normalizado == Indeterminado.Nombre) return Indeterminado;

			throw new ErrorDeDominio(
				$"'{nombre}' no es una forma de corte conocida. Las validas son: "
				+ $"{Automatico.Nombre}, {Preseteado.Nombre}, {Indeterminado.Nombre}.");
		}

		/// <summary>COMO SE DETUVO, dicho para la persona. Es lo que hay que ENUNCIAR
		/// antes de pedirle que confirme: la pantalla estaba pidiendo confirmar una
		/// afirmacion que nunca decia, y "si, quedo asi" no dice que es "asi".
		///
		/// Vive aca y no en la caratula porque la inferencia es de este tipo: quien sabe
		/// que un corte automatico significa tanque lleno es la FormaDeCorte, y la frase
		/// que lo explica es la misma pieza de conocimiento.</summary>
		internal string ComoSeDetuvo()
		{
			if (ReferenceEquals(this, Automatico)) return "el surtidor cortó solo";
			if (ReferenceEquals(this, Preseteado)) return "cargaste un monto fijo";

			// Indeterminado no infiere nada, asi que nunca genera un faltante que
			// confirmar: no hay suposicion que enunciar.
			return "";
		}

		/// <summary>La inferencia inicial del tanque para una carga con este corte. Corte
		/// automatico: casi seguro quedo lleno. Preseteado: casi seguro no. Indeterminado:
		/// no se infiere nada. Lo inferido entra con origen Estimado -el mismo mecanismo
		/// de procedencia del odometro- y NUNCA cierra un tramo: prellenar el switch para
		/// que Sonia confirme es todo su poder.</summary>
		internal DeclaracionDeTanque Inferencia()
		{
			if (ReferenceEquals(this, Automatico))
				return DeclaracionDeTanque.De(EstadoDelTanque.Lleno, OrigenDeLectura.Estimado);
			if (ReferenceEquals(this, Preseteado))
				return DeclaracionDeTanque.De(EstadoDelTanque.Parcial, OrigenDeLectura.Estimado);

			return DeclaracionDeTanque.Ninguna;
		}
	}
}
