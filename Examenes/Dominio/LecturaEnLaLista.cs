using System;

namespace Examenes.Dominio
{
	// UNA LECTURA DE LA LISTA DE EXAMENES, con el indice del examen del que salio.
	//
	// Existe por lo mismo que CeldaDelCuadro: el print proyecta UN solo nivel, asi que una
	// lista de examenes que a su vez tienen lecturas no cruza la puerta. El indice es lo
	// que deja que la superficie rearme el resultado de cada examen SIN volver a decidir
	// que va donde — un urianalisis son su color y sus binarios corridos, y eso ya lo
	// decidio el examen al emitir sus lecturas.
	//
	// Y VIAJA COMO CODIGO Y COMO NUMERO, nunca como frase: "ambar" y "positivo" son
	// codigos de un conjunto cerrado, y un colesterol es un numero con su unidad. Como se
	// dicen los dos es texto de presentacion.
	//
	// Carrier por identidad: sin Equals, sin GetHashCode, no es llave de nada.
	internal sealed class LecturaEnLaLista
	{
		/// <summary>El indice del examen en la lista, que es lo que une esta lectura con su fila.</summary>
		internal int Fila { get; private set; }

		/// <summary>El codigo del analito dentro de una clave de frase.</summary>
		internal string ClaveDelAnalito { get; private set; }

		/// <summary>El codigo de lo que dio, vacio cuando lo que dio es un numero.</summary>
		internal string ClaveDelResultado { get; private set; }

		/// <summary>El numero que dio, cero cuando no dio un numero.</summary>
		internal decimal Valor { get; private set; }

		/// <summary>El simbolo de la unidad, vacio cuando no dio un numero. NO se traduce.</summary>
		internal string Unidad { get; private set; }

		/// <summary>Si lo que dio es un numero con unidad.</summary>
		internal bool EsNumero { get; private set; }

		internal LecturaEnLaLista(int fila, LecturaDeExamen lectura)
		{
			if (fila < 0)
				throw new ArgumentOutOfRangeException(nameof(fila), fila, "La fila no puede ser negativa.");
			ArgumentNullException.ThrowIfNull(lectura);

			Fila = fila;
			ClaveDelAnalito = lectura.ClaveDelAnalito;
			ClaveDelResultado = lectura.ClaveDelResultado;
			Valor = lectura.Valor;
			Unidad = lectura.Unidad;
			EsNumero = lectura.EsNumero;
		}
	}
}
