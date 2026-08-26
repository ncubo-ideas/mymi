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
	public sealed class ReferenciaDeLectura
	{
		// Como se llama esta linea para quien la dibuja. Es de un vocabulario acotado
		// -habitual, meta, partida, piso, techo, umbral- y cada forma sabe cuales espera.
		public string Nombre { get; private set; }

		public Medicion Valor { get; private set; }

		// Que decirle a la persona cuando pregunta por esa linea. La pantalla no la
		// inventa: la muestra.
		public string ParaSonia { get; private set; }

		public ReferenciaDeLectura(string nombre, Medicion valor, string paraSonia)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);
			ArgumentNullException.ThrowIfNull(valor);

			Nombre = nombre;
			Valor = valor;
			ParaSonia = paraSonia ?? "";
		}

		public decimal Numero => Valor.Valor;

		public string Unidad => Valor.Unidad.Simbolo;
	}
}
