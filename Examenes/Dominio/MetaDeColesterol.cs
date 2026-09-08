using System;

namespace Examenes.Dominio
{
	// A DONDE TIENE QUE LLEGAR, y cuánto le falta.
	//
	// Es el pedido de la reunion del 2026-08-26, y es lo que distingue al colesterol del
	// antigeno prostatico de la forma mas clara que hay:
	//
	//   antigeno    "usted no quiere perder la historia"  -> se lee contra su linea base
	//   colesterol  "yo no ocupo ver esa historia"        -> se lee contra una META
	//
	// Alvaro, textual: "usted tiene una especie como de meta, la aplicacion le tiene que
	// ayudar como una meta. Entonces tenes el colesterol en 280 y tenes que bajarlo a 240.
	// Usted si le ocupa sacar una pequena historia, pero la historia NO es de todos los
	// examenes que el se ha hecho: es ayudarle a el de que el esta bajando."
	//
	// POR QUE LA META NO ES EL TECHO DE LO DESEABLE. A alguien en 280 no se le dice "tenes
	// que llegar a 200": es una meta que no se ve alcanzable y deja de servir como meta.
	// Se le dice el escalon siguiente. Cuando lo cruza, la meta se mueve sola.
	//
	// Y por eso este tipo existe en vez de que Interpretar devuelva un decimal suelto: la
	// meta no es un numero, es un numero MAS cuanto falta MAS si viene acercandose. Las
	// tres cosas juntas son lo que la app puede mostrar como progreso.
	internal sealed class MetaDeColesterol
	{
		// Los escalones. Salen de los umbrales de uso corriente -deseable bajo 200,
		// limitrofe hasta 239, alto de 240 para arriba- y son ORIENTATIVOS.
		private const decimal Deseable = 200m;
		private const decimal Alto = 240m;

		internal Medicion Actual { get; private set; }

		internal Medicion Objetivo { get; private set; }

		// Con signo: positivo si todavia falta bajar.
		internal decimal CuantoFalta => Actual.Valor - Objetivo.Valor;

		internal bool YaLlego => CuantoFalta <= 0m;

		private MetaDeColesterol(Medicion actual, Medicion objetivo)
		{
			Actual = actual;
			Objetivo = objetivo;
		}

		// EL ESCALON SIGUIENTE desde donde esta parado. Un 280 apunta a 240; un 230 apunta
		// a 200; un 190 ya llego y la meta es sostenerlo.
		internal static MetaDeColesterol Para(Medicion actual)
		{
			ArgumentNullException.ThrowIfNull(actual);

			decimal objetivo = actual.Valor >= Alto ? Alto
				: actual.Valor >= Deseable ? Deseable
				: actual.Valor;

			return new MetaDeColesterol(actual, new Medicion(objetivo, actual.Unidad));
		}

		// QUE TAN LEJOS esta, de cero a uno, contando desde donde arranco el episodio. Es
		// lo que una barra de progreso puede pintar sin decidir nada por su cuenta.
		//
		// Sin punto de partida -o si el partida ya estaba en la meta- no hay progreso que
		// mostrar y devuelve cero: no se inventa una barra llena.
		internal decimal ProgresoDesde(Medicion partida)
		{
			if (partida == null) return 0m;

			decimal total = partida.Valor - Objetivo.Valor;
			if (total <= 0m) return YaLlego ? 1m : 0m;

			decimal recorrido = partida.Valor - Actual.Valor;
			if (recorrido <= 0m) return 0m;

			return recorrido >= total ? 1m : recorrido / total;
		}

		// La meta dicha entera, para cuando la frase todavia no nombro el valor actual.
		internal string Expresada => YaLlego
			? $"estás en {Actual.Expresado}, dentro de lo deseable"
			: $"de {Actual.Expresado} a {Objetivo.Expresado}: te faltan {CuantoFalta} {Actual.Unidad.Simbolo}";

		// La meta dicha SIN repetir el valor actual, para encadenar despues de una frase
		// que ya lo dijo. Son dos por lo mismo que ParaMostrar y Nombre son dos en Analito:
		// la misma cosa se dice distinto segun donde caiga.
		internal string ComoObjetivo => YaLlego
			? "y ya estás dentro de lo deseable"
			: $"tu meta ahora es bajar a {Objetivo.Expresado}";
	}
}
