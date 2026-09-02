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
	internal sealed class Reducibilidad
	{
		// Consumo discrecional. Sale del repertorio, sin preguntarle nada a la persona.
		internal static readonly Reducibilidad Discrecional =
			new Reducibilidad("discrecional", "Se puede recortar.");

		// Un compromiso adquirido. No es que cueste: es que no depende de la voluntad de
		// este mes. Una cuota de prestamo no se paga a medias.
		internal static readonly Reducibilidad Comprometido =
			new Reducibilidad("comprometido", "No se puede recortar: es un compromiso adquirido.");

		// Ni el repertorio ni la clase de gasto alcanzan. Hay que preguntarle a la persona.
		internal static readonly Reducibilidad PorDefinir =
			new Reducibilidad("por definir", "Falta que la persona diga si esto se puede recortar.");

		private Reducibilidad(string nombre, string lectura)
		{
			Nombre = nombre;
			Lectura = lectura;
		}

		internal string Nombre { get; }

		// La linea la arma el dominio; el CONSEJO no. Es la distincion del capitulo 0 §8:
		// nombrar no es inventar.
		internal string Lectura { get; }

		// EL RESOLUTOR TOTAL desde primitivo. Igual que en Periodicidad, faltaba.
		//
		// Ojo con 'por definir': se puede NOMBRAR pero no se puede MARCAR. Resolverlo acá
		// es legítimo —una consulta puede preguntar por él— y quien lo rechaza es
		// GastoDeLaPersona.LaPersonaLoMarcoComo, porque marcar algo como "no sé" no es
		// marcarlo. Las dos reglas viven donde corresponde y ninguna repite a la otra.
		internal static Reducibilidad PorNombre(string nombre)
		{
			if (nombre != null)
			{
				string limpio = nombre.Trim().ToLowerInvariant();
				if (limpio == Discrecional.Nombre) return Discrecional;
				if (limpio == Comprometido.Nombre) return Comprometido;
				if (limpio == PorDefinir.Nombre) return PorDefinir;
			}

			throw new ArgumentException(
				$"'{nombre}' no es una reducibilidad de este dominio. Son: discrecional, comprometido, por definir.",
				nameof(nombre));
		}

		internal bool SeSabe => this != PorDefinir;

		internal bool SePuedeRecortar => this == Discrecional;

		public override string ToString() => Nombre;
	}
}
