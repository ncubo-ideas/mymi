using System;
using System.Collections.Generic;

namespace Presupuesto.Dominio
{
	// LA RESPUESTA A LA PREGUNTA 3: "si tomo este prestamo y tengo que pagar esta
	// mensualidad, que gastos puedo reducir?".
	//
	// Es la unica pregunta HACIA ADELANTE y CONTRAFACTICA de las cinco. Examenes y Compras no
	// tienen ninguna asi: toda esa doctrina esta construida sobre leer la serie del pasado.
	//
	// Y ES LA QUE PEOR CONTESTA, a proposito. Preguntas.md dice que un gasto es o no
	// reducible pero no CUANTO se puede reducir de cada uno -HUECO 8-, asi que esto no puede
	// decir "te alcanza" o "no te alcanza". Solo puede poner sobre la mesa cuanto hay en cada
	// estado y decir con todas las letras que falta un dato.
	//
	// Preferir eso a inventar el juicio que falta es lo que evita el fallo caro: una
	// respuesta que no revienta nunca y esta mal siempre.
	internal sealed class MargenParaRecortar
	{
		internal MargenParaRecortar(decimal mensualidadNueva, decimal discrecional,
			decimal comprometido, decimal porDefinir, IReadOnlyList<GastoRecurrente> sinMarcar)
		{
			MensualidadNueva = mensualidadNueva;
			Discrecional = discrecional;
			Comprometido = comprometido;
			PorDefinir = porDefinir;
			SinMarcar = sinMarcar;
		}

		internal decimal MensualidadNueva { get; }

		// Lo que el repertorio ya sabe que es discrecional: cine, delivery, bar, restaurante,
		// viajes.
		internal decimal Discrecional { get; }

		// Compromisos adquiridos. No es que cueste recortarlos: es que no dependen de la
		// voluntad de este mes.
		internal decimal Comprometido { get; }

		// Lo que nadie marco. NO es cero y NO es recortable: es desconocido.
		internal decimal PorDefinir { get; }

		internal IReadOnlyList<GastoRecurrente> SinMarcar { get; }

		// ------------------------------------------------------ lo que cruza el perform
		//
		// La lista NO cruza: una coleccion adentro de un carrier es el segundo nivel que
		// el print no proyecta. Lo que cruza es CUANTOS son; los nombres, si hacen falta,
		// son una consulta aparte con su propio foreach. Cero no significa "no hay":
		// significa que no quedo nada sin marcar, que es la respuesta completa.
		internal int CuantosSinMarcar => SinMarcar.Count;

		// El techo optimista: todo lo discrecional a cero. No es una recomendacion -nadie
		// vive recortando el 100% de su ocio- y por eso el nombre dice "techo".
		internal decimal TechoDeLoConocido => Discrecional;

		// .Alcanza con lo que se SABE que es recortable?
		internal bool AlcanzaConLoConocido => TechoDeLoConocido >= MensualidadNueva;

		// La respuesta es incompleta mientras haya plata sin marcar que podria cambiarla.
		internal bool LaRespuestaEstaCompleta =>
			PorDefinir == 0m || AlcanzaConLoConocido;

		internal string Lectura
		{
			get
			{
				string techo = AlcanzaConLoConocido
					? $"Con lo que se sabe recortable alcanza: hay {Discrecional} contra una mensualidad de {MensualidadNueva}."
					: $"Con lo que se sabe recortable no alcanza: hay {Discrecional} contra una mensualidad de {MensualidadNueva}.";

				if (PorDefinir == 0m) return techo;

				return techo + $" Quedan {PorDefinir} en {SinMarcar.Count} gastos que nadie marco todavia, "
					+ "asi que este numero puede subir.";
			}
		}
	}
}
