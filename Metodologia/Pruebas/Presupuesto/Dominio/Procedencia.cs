using System;

namespace Presupuesto.Dominio
{
	// QUIEN LO DIJO. Conjunto CERRADO de dos.
	//
	// Capitulo 0 §1: en el journal conviven dos origenes y se distinguen para siempre. En
	// Examenes y en Compras eso es casi decorativo, porque todo viene de la institucion.
	// ACA NO: Preguntas.md declara el dominio HIBRIDO, y sin lo que aporta la persona
	// ninguna de las cinco preguntas tiene respuesta.
	//
	// La consecuencia esta escrita en el archivo y hay que repetirla donde se vea: el journal
	// DEJA DE SER RECONSTRUIBLE desde URL2. Borrar la carpeta ya no es volver a bajar los
	// eventos, es perder el trabajo de la persona.
	//
	// Ojo con leer INSTITUCION como "mas confiable". Significa QUIEN LO EMITIO, no quien lo
	// escribio ni si es mas objetivo.
	internal sealed class Procedencia
	{
		// El banco. Estados de cuenta de tarjeta de credito y de cuenta bancaria.
		internal static readonly Procedencia Institucion =
			new Procedencia("institucion", reconstruible: true);

		// La persona. Las marcas: que cargos son el mismo gasto, que es de la propiedad, que
		// se puede reducir, que cargos forman un imprevisto. Cuatro veces aparecio el mismo
		// patron en la entrevista, siempre igual: marcar primero, inferir despues.
		internal static readonly Procedencia Persona =
			new Procedencia("persona", reconstruible: false);

		private Procedencia(string nombre, bool reconstruible)
		{
			Nombre = nombre;
			SeRecuperaDelProveedor = reconstruible;
		}

		internal string Nombre { get; }

		// Si esto se pierde, .se puede volver a bajar? Para lo de la persona la respuesta es
		// NO, y de ahi sale que el respaldo de la carpeta deje de ser opcional.
		internal bool SeRecuperaDelProveedor { get; }

		public override string ToString() => Nombre;
	}
}
