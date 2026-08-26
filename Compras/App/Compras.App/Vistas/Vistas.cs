using System;
using System.Collections.Generic;

namespace Compras.App.Vistas
{
	// Las vistas que la app expone. Viven en la App y no en la caratula porque las
	// consumen DOS caratulas: la UI del browser y las tools del servidor MCP. Que la
	// forma de la vista sea una sola es lo que hace que la IA y la persona vean lo
	// mismo.
	//
	// No son el dominio. El dominio tiene Historial / Compra / Canasta con su
	// comportamiento; esto es la proyeccion que la app decidio publicar.

	public sealed class CompraEnLista
	{
		public string Numero { get; init; } = "";
		public DateTime Fecha { get; init; }
		public int Lineas { get; init; }
		public decimal Total { get; init; }
	}

	public sealed class LineaDeCompra
	{
		public string Sku { get; init; } = "";
		public string Descripcion { get; init; } = "";
		public int Cantidad { get; init; }
		public decimal PrecioUnitario { get; init; }
		public decimal Total { get; init; }
		public string Categoria { get; init; } = "";
		public bool TieneGarantia { get; init; }
	}

	public sealed class CompraDetallada
	{
		public string Numero { get; init; } = "";
		public DateTime Fecha { get; init; }
		public decimal Total { get; init; }
		public IReadOnlyList<LineaDeCompra> Lineas { get; init; } = new List<LineaDeCompra>();
	}

	// La foto de un mes sumada por perfil. Trae los HECHOS y no una frase: montos por
	// perfil y la proporcion de lo ocasional sobre lo comestible. Con eso la IA o la UI
	// arman "tu canasta de agosto fue 97% ocasional". Lo que no se puede armar con esto
	// es un consejo de plantilla, y es a proposito.
	public sealed class CanastaDelMesVista
	{
		public int Anio { get; init; }
		public int Mes { get; init; }
		public int CantidadDeCompras { get; init; }
		public decimal GastoRecomendado { get; init; }
		public decimal GastoNeutro { get; init; }
		public decimal GastoOcasional { get; init; }
		public decimal GastoNoComestible { get; init; }
		public decimal GastoComestible { get; init; }
		public decimal GastoTotal { get; init; }
		public decimal ProporcionOcasional { get; init; }
		public bool HayComestible { get; init; }
		public bool HayCompras { get; init; }
	}

	// Como cambio la canasta entre dos meses. Es la vista del cruce: el asistente la
	// pone al lado de la tendencia del colesterol y la relacion aparece sola.
	//
	// Veredicto y ParaSonia vienen del DOMINIO, no se arman aca: la pantalla no tiene de
	// donde saber si un cambio de -29 puntos es bueno.
	public sealed class CanastasComparadas
	{
		public bool HayComparacion { get; init; }
		public decimal ProporcionAntes { get; init; }
		public decimal ProporcionDespues { get; init; }
		public decimal Cambio { get; init; }
		public string Veredicto { get; init; } = "";
		public string ParaSonia { get; init; } = "";
		public bool CrecioLoOcasional { get; init; }
		public int AnioAntes { get; init; }
		public int MesAntes { get; init; }
		public decimal GastoOcasionalAntes { get; init; }
		public decimal GastoComestibleAntes { get; init; }
		public int AnioDespues { get; init; }
		public int MesDespues { get; init; }
		public decimal GastoOcasionalDespues { get; init; }
		public decimal GastoComestibleDespues { get; init; }
	}

	// El respaldo de una garantia: en que compra fue, cuando, cuanta cobertura y si
	// esta vigente en la fecha que puso quien pregunto.
	public sealed class GarantiaDeProducto
	{
		public bool Encontrada { get; init; }
		public string TextoBuscado { get; init; } = "";
		public string Sku { get; init; } = "";
		public string Descripcion { get; init; } = "";
		public string NumeroDeCompra { get; init; } = "";
		public DateTime FechaDeCompra { get; init; }
		public int MesesDeCobertura { get; init; }
		public DateTime VenceEl { get; init; }
		public bool Vigente { get; init; }
		public int DiasParaVencer { get; init; }
		public string Severidad { get; init; } = "";
		public string QueHacer { get; init; } = "";
		public string ParaSonia { get; init; } = "";
	}

	// Desde cuando hasta cuando hay compras. Es el contexto que necesita quien pregunta
	// para elegir que meses comparar; sin el, adivina un mes vacio y concluye que no hay
	// datos cuando si hay.
	public sealed class RangoDelHistorialVista
	{
		public int MesesConCompras { get; init; }
		public string PrimerMes { get; init; } = "";
		public string UltimoMes { get; init; } = "";
	}

	// LO QUE NO ESTA: una categoria que se repone cada tanto y hace demasiado que no
	// entra. La pantalla no puede renderizar una ausencia que nadie le paso.
	public sealed class AbastecimientoEnEspera
	{
		public string Categoria { get; init; } = "";
		public DateTime UltimaCompra { get; init; }
		public int DiasDesdeLaUltima { get; init; }
		public int DiasEsperados { get; init; }
		public bool EsRecomendado { get; init; }
		public string Severidad { get; init; } = "";
		public string ParaSonia { get; init; } = "";
	}
}
