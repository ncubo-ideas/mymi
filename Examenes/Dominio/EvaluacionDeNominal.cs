using System;

namespace Examenes.Dominio
{
	// EL RESULTADO DE EVALUAR LA SERIE DE UN NOMINAL: la conclusion mas los hechos con los
	// que se llego a ella. Es el tercer par de EvaluacionDeTendencia y EvaluacionDePresencia.
	//
	// Igual que las otras dos, devuelve HECHOS y no una frase: "persiste, 2 inesperadas
	// seguidas sobre 3 observaciones, la ultima ambar". Quien redacta la oracion es el
	// analito —en su Leer— o la superficie.
	//
	// LOS CAMPOS SON OTROS, y esa es toda la razon de que sea otra clase. No hay valor
	// inicial ni final ni cambio, y tampoco hay positivas: hay una CATEGORIA. "Cual salio"
	// es el hecho central de esta forma, y es justo el que ninguna de las otras dos sabe
	// cargar —un binario no tiene cual, tiene si o no—.
	//
	// Carrier por identidad: sin Equals, sin GetHashCode, no es llave de nada.
	internal sealed class EvaluacionDeNominal : HechosDeLaSerie
	{
		internal Constancia Constancia { get; private set; }

		// La categoria de la ultima observacion, ya dicha con el vocabulario de SU analito:
		// "ámbar" en la orina, "marrón" en las heces. Vacia si no hay ninguna.
		internal string UltimaCategoria { get; private set; }

		internal bool UltimaEsEsperable { get; private set; }

		// Cuantas observaciones cayeron fuera de lo esperable, en toda la serie.
		internal int NoEsperables { get; private set; }

		// Cuantas inesperadas hay AL FINAL, una detras de otra. Es el numero que distingue
		// "apareció" de "persiste", y por lo tanto el que cambia la conducta.
		internal int NoEsperablesSeguidas { get; private set; }

		// Si esas seguidas son TODAS la misma categoria. Hace falta porque en un nominal
		// —y no en un binario— dos inesperadas al hilo pueden ser dos cosas distintas: un
		// negro seguido de un palido no es un mismo hallazgo persistiendo, y la frase que
		// se le dice a la persona no puede ser la misma.
		internal bool LaUltimaSeRepitio { get; private set; }

		// Dias entre la primera y la ultima observacion. Sirve para decir sobre cuanto
		// tiempo se esta hablando; no se divide por el, porque una tasa de aparicion de un
		// color no es una magnitud clinica.
		internal int DiasTranscurridos { get; private set; }

		internal EvaluacionDeNominal(Analito analito, Constancia constancia,
			int cantidadDeMediciones, string ultimaCategoria, bool ultimaEsEsperable,
			int noEsperables, int noEsperablesSeguidas, bool laUltimaSeRepitio,
			int diasTranscurridos)
			: base(analito, cantidadDeMediciones)
		{
			ArgumentNullException.ThrowIfNull(constancia);

			Constancia = constancia;
			UltimaCategoria = ultimaCategoria ?? "";
			UltimaEsEsperable = ultimaEsEsperable;
			NoEsperables = noEsperables;
			NoEsperablesSeguidas = noEsperablesSeguidas;
			LaUltimaSeRepitio = laUltimaSeRepitio;
			DiasTranscurridos = diasTranscurridos;
		}

		// Cero o una observacion: no hay serie que evaluar. La categoria SI viaja, porque
		// con una sola observacion la lectura del analito ya esta completa —a diferencia de
		// las otras dos formas, donde un punto solo no alcanza para concluir nada—.
		internal static EvaluacionDeNominal SinSerie(Analito analito, int cantidadDeMediciones,
			string ultimaCategoria, bool ultimaEsEsperable) =>
			new EvaluacionDeNominal(analito, Constancia.Indeterminada, cantidadDeMediciones,
				ultimaCategoria, ultimaEsEsperable, 0, 0, false, 0);

		internal override string NombreDeLaLectura => Constancia.Nombre;

		// FALSO SIEMPRE, y es una respuesta y no un hueco: un color no se repite para
		// confirmarse, se MIRA. Ver ColorDeOrina.AccionDelHallazgo, que pide conversarlo y
		// no repetir el examen.
		internal override bool SugiereRepetirElExamen => false;

		// Proyecciones escalares, para que el print del DSL no navegue dos niveles.
		internal string NombreDeLaConstancia => Constancia.Nombre;
		internal bool EstaFueraDeLoEsperable => Constancia.EstaFueraDeLoEsperable;
		internal bool EstaConfirmado => Constancia.EstaConfirmado;
		internal int Esperables => CantidadDeMediciones - NoEsperables;

		internal override void PonerSusValoresEn(Interpretacion lectura) => lectura.ConCategorias(this);
	}
}
