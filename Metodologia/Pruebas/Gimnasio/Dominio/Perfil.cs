using System;

namespace Gimnasio.Dominio
{
	// QUIEN ES LA PERSONA, en lo que hace falta para leer un numero.
	//
	// El Preguntas.md es explicito: "no es decoracion - sin el ningun numero se puede
	// interpretar". Por eso NO hay forma de pedir una lectura sin perfil: es parametro
	// obligatorio de Medida.Leer, y el compilador lo hace cumplir.
	//
	// La edad entra como dato y no se calcula de una fecha de nacimiento: el dominio
	// no lee el reloj. Quien la sepa la pasa.
	internal sealed class Perfil
	{
		internal Genero Genero { get; }
		internal int Edad { get; }
		internal decimal AlturaEnCentimetros { get; }

		internal Perfil(Genero genero, int edad, decimal alturaEnCentimetros)
		{
			ArgumentNullException.ThrowIfNull(genero);
			if (edad <= 0)
				throw new ArgumentOutOfRangeException(nameof(edad), edad, "La edad tiene que ser positiva.");
			if (alturaEnCentimetros <= 0)
				throw new ArgumentOutOfRangeException(nameof(alturaEnCentimetros), alturaEnCentimetros,
					"La altura tiene que ser positiva.");

			Genero = genero;
			Edad = edad;
			AlturaEnCentimetros = alturaEnCentimetros;
		}
	}
}
