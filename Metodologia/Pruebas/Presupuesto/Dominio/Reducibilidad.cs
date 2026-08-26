using System;

namespace Presupuesto.Dominio
{
	// SI SE PUEDE RECORTAR. Conjunto CERRADO de TRES estados, y los tres importan.
	//
	// Juicio 6 de Preguntas.md: se deduce para el consumo discrecional -cine, delivery, bar,
	// restaurante, viajes: el repertorio- y para todo lo demas lo marca la persona.
	//
	// POR QUE NO ES UN bool. Porque "todavia no se sabe" no es "no". Con un booleano, un
	// gasto sin marcar se contaria como no reducible, y la pregunta 3 -"con esta mensualidad
	// nueva, que gastos puedo reducir?"- devolveria una respuesta mas chica que la verdad,
	// sin decir que fue por ignorancia. Ese es el error caro: no revienta, subestima.
	//
	// Con tres estados la respuesta puede decir "esto se puede recortar, esto no, y sobre
	// esto otro necesito que me digas", que es la unica honesta.
	public sealed class Reducibilidad
	{
		// Consumo discrecional. Sale del repertorio, sin preguntarle nada a la persona.
		public static readonly Reducibilidad Discrecional =
			new Reducibilidad("discrecional", "Se puede recortar.");

		// Un compromiso adquirido. No es que cueste: es que no depende de la voluntad de
		// este mes. Una cuota de prestamo no se paga a medias.
		public static readonly Reducibilidad Comprometido =
			new Reducibilidad("comprometido", "No se puede recortar: es un compromiso adquirido.");

		// Ni el repertorio ni la clase de gasto alcanzan. Hay que preguntarle a la persona.
		public static readonly Reducibilidad PorDefinir =
			new Reducibilidad("por definir", "Falta que la persona diga si esto se puede recortar.");

		private Reducibilidad(string nombre, string lectura)
		{
			Nombre = nombre;
			Lectura = lectura;
		}

		public string Nombre { get; }

		// La linea la arma el dominio; el CONSEJO no. Es la distincion del capitulo 0 §8:
		// nombrar no es inventar.
		public string Lectura { get; }

		public bool SeSabe => this != PorDefinir;

		public bool SePuedeRecortar => this == Discrecional;

		public override string ToString() => Nombre;
	}
}
