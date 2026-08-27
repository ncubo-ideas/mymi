using System;

namespace Examenes.Dominio
{
	// CUANTOS ANALITOS ESTAN DONDE TIENEN QUE ESTAR, y la frase que lo dice.
	//
	// Es la contracara de UrgenciaDelExpediente: uno contesta "que pide algo" y este
	// contesta "cuanto esta bien". La portada muestra las dos a la vez, y por eso las dos
	// tienen que venir del mismo lado — si una la escribe el dominio y la otra la compone
	// la pantalla, la segunda queda a un edit de distancia de decir algo que el dominio
	// nunca afirmo.
	//
	// LA CUENTA NO ES UNA RESTA. Restarle a los analitos con serie los que son hallazgo y
	// los que esperan el sexo daba el mismo numero, pero era un numero derivado afuera de
	// tres escalares publicados por separado: cualquier cambio en uno de los tres lo
	// rompia en silencio. Aca se cuenta en UNA sola pasada por las mismas series.
	//
	// LOS QUE ESPERAN EL SEXO NO SON SANOS. Un analito cuyo rango depende de un dato que
	// no se tiene no esta en su rango: no se sabe. Contarlo como bueno seria afirmar con
	// el dato que falta justo lo que el dominio se niega a afirmar en todas las otras
	// puertas.
	//
	// Carrier por identidad: sin Equals, sin GetHashCode, no es llave de nada.
	public sealed class TranquilidadDelExpediente
	{
		// Cuantos analitos con serie tienen su ultima lectura donde corresponde.
		public int EnSuRango { get; private set; }

		// El denominador: cuantos analitos con serie hay. Sale del MISMO recorrido, asi
		// que "N de M" no puede mezclar dos universos.
		public int Analitos { get; private set; }

		public TranquilidadDelExpediente(int enSuRango, int analitos)
		{
			if (enSuRango < 0)
				throw new ArgumentOutOfRangeException(nameof(enSuRango),
					"No puede haber menos de cero analitos en su rango.");

			if (analitos < enSuRango)
				throw new ArgumentOutOfRangeException(nameof(analitos),
					$"Hay {enSuRango} analitos en su rango y solo {analitos} con serie: "
					+ "el denominador no puede ser menor que el numerador.");

			EnSuRango = enSuRango;
			Analitos = analitos;
		}

		public bool HayAlgoEnRango => EnSuRango > 0;

		// LA FRASE ENTERA, escrita aca. Se lee sola: es lo primero de la portada y no
		// puede apoyarse en nada que venga despues. "El resto esta bien" nombraba una
		// excepcion que todavia no se habia dicho.
		//
		// Con todo en rango no se dice "M de M": la fraccion existe para contar los que NO
		// estan, y sin ninguno afuera es ruido.
		public string ParaSonia
		{
			get
			{
				if (EnSuRango == 0) return "";

				string valores = EnSuRango == 1 ? "valor está" : "valores están";

				return EnSuRango == Analitos
					? $"Tus {EnSuRango} {valores} en su rango."
					: $"{EnSuRango} de tus {Analitos} {valores} en su rango.";
			}
		}
	}
}
