using System;

namespace Examenes.Dominio
{
	// EL RESULTADO DE EVALUAR LA SERIE DE UN BINARIO: la conclusion mas los hechos con
	// los que se llego a ella. Es el par de EvaluacionDeTendencia para la forma 5.
	//
	// Igual que aquella, devuelve HECHOS y no una frase: "apareció, 2 positivas seguidas
	// sobre 3 pruebas, la primera hace 365 días". Quien redacta la oracion es el analito
	// -en su Interpretar- o la superficie. Con estos campos se puede decir "la sangre
	// apareció en las dos últimas" y no se puede decir "cuidá tu alimentación", porque el
	// dominio no tiene de donde sacarlo.
	//
	// LOS CAMPOS SON OTROS, y esa es toda la razon de que sea otra clase. No hay valor
	// inicial ni final ni cambio: hay CUANTAS y CUANTAS SEGUIDAS. "Dos seguidas" es el
	// hecho que hace que una sangre en heces deje de ser "conviene repetirlo".
	//
	// Carrier por identidad: sin Equals, sin GetHashCode, no es llave de nada.
	internal sealed class EvaluacionDePresencia : HechosDeLaSerie
	{
		internal Presencia Presencia { get; private set; }

		// Cuantas de las pruebas realizadas dieron positivas, en toda la serie.
		internal int Positivas { get; private set; }

		// Cuantas positivas hay AL FINAL, una detras de otra. Es el numero que distingue
		// "apareció" de "persiste", y por lo tanto el que cambia la conducta.
		internal int PositivasSeguidas { get; private set; }

		// Dias entre la primera y la ultima prueba de la serie. Sirve para decir sobre
		// cuanto tiempo se esta hablando; no se divide por el, porque una tasa de
		// aparicion no es una magnitud clinica.
		internal int DiasTranscurridos { get; private set; }

		// El resultado de la ultima prueba, ya dicho con el vocabulario de SU analito:
		// "positivo", y en una serologia seria "reactivo". Vacio si no hay ninguna.
		internal string UltimoResultado { get; private set; }

		internal EvaluacionDePresencia(Analito analito, Presencia presencia, int cantidadDeMediciones,
			int positivas, int positivasSeguidas, int diasTranscurridos, string ultimoResultado)
			: base(analito, cantidadDeMediciones)
		{
			ArgumentNullException.ThrowIfNull(presencia);

			Presencia = presencia;
			Positivas = positivas;
			PositivasSeguidas = positivasSeguidas;
			DiasTranscurridos = diasTranscurridos;
			UltimoResultado = ultimoResultado ?? "";
		}

		// Cero o una prueba realizada: no hay serie que evaluar.
		internal static EvaluacionDePresencia SinSerie(Analito analito, int cantidadDeMediciones,
			string ultimoResultado) =>
			new EvaluacionDePresencia(analito, Presencia.Indeterminada, cantidadDeMediciones,
				0, 0, 0, ultimoResultado);

		internal override string NombreDeLaLectura => Presencia.Nombre;

		internal override bool SugiereRepetirElExamen => Presencia.SugiereRepetirElExamen;

		// Proyecciones escalares, para que el print del DSL no navegue dos niveles.
		internal string NombreDeLaPresencia => Presencia.Nombre;
		internal bool EstaPresente => Presencia.EstaPresente;
		internal bool EstaConfirmado => Presencia.EstaConfirmado;
		internal int Negativas => CantidadDeMediciones - Positivas;
	}
}
