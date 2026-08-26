using System;
using System.Collections.Generic;

namespace Examenes.App.Vistas
{
	// Las vistas que la app expone. Viven en la App y no en la caratula porque las
	// consumen DOS caratulas: la UI del browser y las tools del servidor MCP. Que la
	// forma de la vista sea una sola es lo que hace que la IA y la persona vean lo
	// mismo.
	//
	// No son el dominio. El dominio tiene Expediente / Examen / Medicion con su
	// comportamiento; esto es la proyeccion que la app decidio publicar.

	public sealed class ExamenEnLista
	{
		public string Nombre { get; init; } = "";
		public DateTime Fecha { get; init; }
		public string Resultado { get; init; } = "";
		public bool AportaSerie { get; init; }

		// Solo para las filas que no forman serie. Ver CeldaDelCuadro.
		public string Severidad { get; init; } = "";
		public string ParaSonia { get; init; } = "";
	}

	// La lectura de la serie. Es la vista que NO tiene equivalente en una pantalla de
	// resultados: un laboratorio entrega valores, no tendencias.
	//
	// Trae los HECHOS y no una frase: tendencia, los dos extremos, la unidad y los dias.
	// Con eso la IA o la UI arman "tu colesterol subio de 200 a 280 en un ano". Lo que
	// no se puede armar con esto es un consejo de plantilla, y es a proposito.
	public sealed class TendenciaDeAnalito
	{
		// Analito es la LLAVE: con ese nombre se vuelve a preguntar. ParaMostrar es el
		// TEXTO: es lo que se le pone delante a la persona. Son dos campos porque son dos
		// usos, y confundirlos es como se termina preguntando por "Antígeno prostático"
		// con tilde a un indice que la llave sin ella.
		public string Analito { get; init; } = "";
		public string ParaMostrar { get; init; } = "";
		public string Tendencia { get; init; } = "";
		public int Mediciones { get; init; }
		public decimal ValorInicial { get; init; }
		public decimal ValorFinal { get; init; }
		public string Unidad { get; init; } = "";
		public int Dias { get; init; }
		public decimal Cambio { get; init; }
		public bool SugiereRepetirElExamen { get; init; }
		public bool HaySerie { get; init; }
	}

	// LOS HECHOS DE UNA SERIE BINARIA. Vista aparte de TendenciaDeAnalito porque no
	// trae lo mismo: un binario no tiene valor inicial ni final ni cambio. Tiene CUANTAS
	// veces aparecio y —la que cambia la conducta— cuantas SEGUIDAS.
	//
	// "Dos seguidas" es lo que hace que una sangre oculta en heces deje de leerse como
	// "conviene repetir el examen". Ninguna medicion sola puede decirlo.
	public sealed class PresenciaDeAnalito
	{
		public string Analito { get; init; } = "";
		public string ParaMostrar { get; init; } = "";

		// "apareció", "persiste", "nunca apareció", "no confirmado", "cedió".
		public string Presencia { get; init; } = "";

		public int Mediciones { get; init; }
		public int Positivas { get; init; }
		public int PositivasSeguidas { get; init; }
		public int Dias { get; init; }

		// El ultimo resultado con el vocabulario de SU analito: "positivo", y en una
		// serologia seria "reactivo". Es la razon de que la magnitud no sea un bool.
		public string UltimoResultado { get; init; } = "";

		public bool EstaPresente { get; init; }
		public bool HaySerie { get; init; }
	}

	// UNA PRUEBA BINARIA en una fecha: el punto de una tira de presencia. Es el par de
	// PuntoDeSerie para la forma 5, y no tiene Valor porque no existe "mas positivo".
	public sealed class PruebaEnSerie
	{
		public DateTime Fecha { get; init; }
		public string Expresado { get; init; } = "";
		public bool EsPositivo { get; init; }
		public bool EsHallazgo { get; init; }
	}

	// UN PUNTO de la serie, con su NUMERO. Es lo que un grafico necesita y una celda del
	// cuadro no da: aquella trae "3.0 ng/mL" ya dicho, esto trae 3.0.
	public sealed class PuntoDeSerie
	{
		public DateTime Fecha { get; init; }
		public decimal Valor { get; init; }
		public string Unidad { get; init; } = "";
	}

	// UNA LINEA que se dibuja junto a los puntos: la base del antígeno, la meta del
	// colesterol, el piso de la hemoglobina. Cada analito emite solo las que tiene.
	public sealed class LineaDeReferencia
	{
		public string Nombre { get; init; } = "";
		public decimal Valor { get; init; }
		public string Unidad { get; init; } = "";
		public string ParaSonia { get; init; } = "";
	}

	// UNA CELDA del cuadro historico, con sus dos indices. Llega plana desde el dominio;
	// la pantalla la ubica en la grilla y no decide nada. Ver CeldaDelCuadro.
	public sealed class CeldaEnCuadro
	{
		public int Fila { get; init; }
		public int Columna { get; init; }
		public string Analito { get; init; } = "";
		public string ParaMostrar { get; init; } = "";
		public DateTime Fecha { get; init; }
		public string Expresado { get; init; } = "";
		public bool AportaSerie { get; init; }

		// Solo para las filas que no forman serie. Ver CeldaDelCuadro.
		public string Severidad { get; init; } = "";
		public string ParaSonia { get; init; } = "";
	}

	// CUANTO HACE que no se mide un analito, y si eso ya es demasiado para el. Ver
	// ControlPendiente: es la unica vista que habla de lo que NO esta.
	public sealed class ControlEnEspera
	{
		public string Analito { get; init; } = "";
		public string ParaMostrar { get; init; } = "";
		public DateTime UltimaMedicion { get; init; }
		public int DiasDesdeLaUltima { get; init; }
		public int MesesRecomendados { get; init; }
		public int DiasQueFaltan { get; init; }
		public bool EstaVencido { get; init; }
		public string Severidad { get; init; } = "";
		public string Accion { get; init; } = "";
		public string ParaSonia { get; init; } = "";
	}

	// LO QUE EL ANALITO CONCLUYE. Vista aparte de TendenciaDeAnalito porque son dos capas
	// distintas: aquella trae los HECHOS de la serie, esta trae la LECTURA de esos hechos
	// hecha por el analito que sabe interpretarlos.
	//
	// Que la accion venga de aca y no de la pantalla es el punto: un .razor no tiene de
	// donde sacar si un hallazgo espera a la proxima cita o no.
	public sealed class InterpretacionDeAnalito
	{
		public string Analito { get; init; } = "";
		public string ParaMostrar { get; init; } = "";
		public string Veredicto { get; init; } = "";
		public string Severidad { get; init; } = "";

		// COMO se dibuja esta lectura. La pantalla hace un switch sobre esto, y ese switch
		// SI es presentación legítima: traduce una forma del dominio a píxeles.
		public string Forma { get; init; } = "";

		// La lectura en una linea, escrita por el analito.
		public string ParaSonia { get; init; } = "";

		// Que hacer. Vacio cuando no hay nada que hacer, que es la mayoria de las veces.
		public string Accion { get; init; } = "";

		public bool PideAlgo { get; init; }
		public bool SugiereRepetirElExamen { get; init; }
	}

	// A QUE RITMO viene cambiando un analito. Vista aparte de TendenciaDeAnalito porque
	// contesta una pregunta distinta MIRANDO UN TRAMO DISTINTO: la tendencia mira la
	// serie entera, la velocidad mira el tramo mas reciente. Meterlas en un solo record
	// obligaria a que uno de los dos campos "dias" mintiera sobre que midio.
	public sealed class VelocidadDeAnalito
	{
		public string Analito { get; init; } = "";
		public string ParaMostrar { get; init; } = "";
		public decimal PorAno { get; init; }
		public string Unidad { get; init; } = "";
		public int Dias { get; init; }
		public decimal Umbral { get; init; }
		public bool EsAplicable { get; init; }
		public bool ExigeAtencion { get; init; }
		public string Expresada { get; init; } = "";
	}
}
