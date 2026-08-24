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
	}

	// La lectura de la serie. Es la vista que NO tiene equivalente en una pantalla de
	// resultados: un laboratorio entrega valores, no tendencias.
	//
	// Trae los HECHOS y no una frase: tendencia, los dos extremos, la unidad y los dias.
	// Con eso la IA o la UI arman "tu colesterol subio de 200 a 280 en un ano". Lo que
	// no se puede armar con esto es un consejo de plantilla, y es a proposito.
	public sealed class TendenciaDeAnalito
	{
		public string Analito { get; init; } = "";
		public string Tendencia { get; init; } = "";
		public int Mediciones { get; init; }
		public decimal ValorInicial { get; init; }
		public decimal ValorFinal { get; init; }
		public string Unidad { get; init; } = "";
		public int Dias { get; init; }
		public decimal Cambio { get; init; }
		public bool ExigeAtencion { get; init; }
		public bool SugiereRepetirElExamen { get; init; }
		public bool HaySerie { get; init; }
	}

	// A QUE RITMO viene cambiando un analito. Vista aparte de TendenciaDeAnalito porque
	// contesta una pregunta distinta MIRANDO UN TRAMO DISTINTO: la tendencia mira la
	// serie entera, la velocidad mira el tramo mas reciente. Meterlas en un solo record
	// obligaria a que uno de los dos campos "dias" mintiera sobre que midio.
	public sealed class VelocidadDeAnalito
	{
		public string Analito { get; init; } = "";
		public decimal PorAno { get; init; }
		public string Unidad { get; init; } = "";
		public int Dias { get; init; }
		public decimal Umbral { get; init; }
		public bool EsAplicable { get; init; }
		public bool ExigeAtencion { get; init; }
		public string Expresada { get; init; } = "";
	}
}
