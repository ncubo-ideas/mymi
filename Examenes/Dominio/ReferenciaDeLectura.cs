using System;

namespace Examenes.Dominio
{
	// UNA LINEA QUE IMPORTA para leer este analito, con su valor y su nombre.
	//
	// Es lo que cada forma necesita dibujar ademas de los puntos: la linea base del
	// antigeno, la meta del colesterol, el piso y el techo de la hemoglobina.
	//
	// POR QUE UNA LISTA Y NO UN RECORD CON CAMPOS. Un record con habitual, meta, partida,
	// piso y techo obligaria a que cada analito devolviera cero en los que no le tocan, y
	// eso es exactamente lo que este dominio evita desde el principio: cuatro de las seis
	// formas de Magnitud tendrian que mentir. Cada analito emite SOLO las referencias que
	// tiene, y si no tiene ninguna la lista viene vacia.
	//
	// Y por eso tambien viaja plana: el print del motor proyecta un nivel, y una lista de
	// pares nombre-valor cruza sin inventar nada.
	internal sealed class ReferenciaDeLectura
	{
		// Como se llama esta linea para quien la dibuja. Es de un vocabulario acotado
		// -habitual, meta, partida, piso, techo, umbral- y cada forma sabe cuales espera.
		internal string Nombre { get; private set; }

		internal Medicion Valor { get; private set; }

		// Que decirle a la persona cuando pregunta por esa linea. La pantalla no la
		// inventa: la muestra.
		/// <summary>La frase con que esta linea se explica, por su nombre.</summary>
		internal string Clave { get; private set; }

		internal ReferenciaDeLectura(string nombre, Medicion valor, string clave)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);
			ArgumentNullException.ThrowIfNull(valor);

			Nombre = nombre;
			Valor = valor;
			Clave = clave ?? "";
		}

		internal decimal Numero => Valor.Valor;

		internal string Unidad => Valor.Unidad.Simbolo;
	}
}
