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
		/// <summary>El departamento legible: "lácteos y huevos". Es lo que se pinta.</summary>
		// LA LLAVE Y EL TITULO VIAJAN LOS DOS, y no es redundancia: la llave es con lo que el
		// codigo compara y lo que sigue igual en los diarios ya escritos; el titulo es lo
		// unico que puede salir a pantalla.
		public string Titulo { get; init; } = "";

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

	// La foto de un mes sumada POR DEPARTAMENTO. Trae los HECHOS y no una frase: cuanto
	// entro de cada pasillo y que parte del mes fue. Con eso la IA o la UI arman "en
	// agosto la mitad se fue en dulces". Lo que no se puede armar con esto es un consejo
	// de plantilla, y es a proposito.
	public sealed class CanastaDelMesVista
	{
		public int Anio { get; init; }
		public int Mes { get; init; }
		public int CantidadDeCompras { get; init; }
		public decimal GastoNoComestible { get; init; }
		public decimal GastoComestible { get; init; }
		public decimal GastoTotal { get; init; }
		public bool HayComestible { get; init; }
		public bool HayCompras { get; init; }
		public IReadOnlyList<GastoPorDepartamentoVista> Departamentos { get; init; } =
			new List<GastoPorDepartamentoVista>();
	}

	public sealed class GastoPorDepartamentoVista
	{
		/// <summary>La llave del repertorio. No se le muestra a nadie: para eso esta Titulo.</summary>
		public string Categoria { get; init; } = "";
		public string Titulo { get; init; } = "";
		public decimal Monto { get; init; }
		public decimal ProporcionDelTotal { get; init; }
	}

	// Como cambio la canasta entre dos meses, y DE DONDE sale la diferencia. Es la vista
	// del cruce: el asistente la pone al lado de la tendencia del colesterol y la relacion
	// aparece sola — el juicio es de el, con permiso, no de esta app.
	//
	// ParaSonia viene del DOMINIO, no se arma aca. Y no hay veredicto: la app senala y se
	// calla.
	public sealed class CanastasComparadas
	{
		public bool HayComparacion { get; init; }
		public decimal GastoAntes { get; init; }
		public decimal GastoDespues { get; init; }
		public decimal CambioEnElTotal { get; init; }
		public decimal Cambio { get; init; }
		public bool HayAlgoQueLoExplique { get; init; }
		public string ParaSonia { get; init; } = "";
		public int AnioAntes { get; init; }
		public int MesAntes { get; init; }
		public decimal GastoComestibleAntes { get; init; }
		public int AnioDespues { get; init; }
		public int MesDespues { get; init; }
		public decimal GastoComestibleDespues { get; init; }
		public IReadOnlyList<DiferenciaPorDepartamentoVista> Diferencias { get; init; } =
			new List<DiferenciaPorDepartamentoVista>();
	}

	// Cuanto movio UN pasillo entre los dos meses. ExplicaElCambio dice si vale nombrarlo.
	public sealed class DiferenciaPorDepartamentoVista
	{
		public string Categoria { get; init; } = "";
		public string Titulo { get; init; } = "";
		public decimal MontoAntes { get; init; }
		public decimal MontoDespues { get; init; }
		public decimal CambioDelPasillo { get; init; }
		public bool Subio { get; init; }
		public bool EsNuevo { get; init; }
		public bool Desaparecio { get; init; }
		public bool ExplicaElCambio { get; init; }
	}

	// LO QUE NO FALLA en su canasta. Es la lectura con la que la app abre: la
	// especificacion de marca pide el PATRON y no una lista de facturas.
	public sealed class FirmaDeLaSemana
	{
		public int ComprasMiradas { get; init; }
		public int CuantosHabituales { get; init; }

		// La linea de la portada, escrita por el dominio.
		public string ParaSonia { get; init; } = "";

		public IReadOnlyList<ProductoQueVuelve> Habituales { get; init; } =
			new List<ProductoQueVuelve>();

		public bool Existe => CuantosHabituales > 0;
	}

	// Un producto que vuelve compra tras compra.
	public sealed class ProductoQueVuelve
	{
		public string Sku { get; init; } = "";
		public string Descripcion { get; init; } = "";
		public int ComprasEnQueAparece { get; init; }
		public int DeCuantas { get; init; }
		public decimal Proporcion { get; init; }

		/// <summary>Si esta en TODAS las compras miradas. Lo que de verdad no falla.</summary>
		public bool EstaEnTodas { get; init; }

		/// <summary>Qué tan fiel es, en palabras: "nunca falla", "casi nunca falta".</summary>
		public string Constancia { get; init; } = "";
	}

	// EL QUIEBRE, con fecha: lo que entro a la canasta y lo que dejo de estar. La otra
	// lectura de la portada, y sin veredicto: dice que paso, no si esta bien.
	public sealed class CambioDeLaCanasta
	{
		/// <summary>
		/// El mes por el que se partió el historial, como aaaa-mm. Vacío cuando no hay con
		/// qué comparar.
		/// </summary>
		// NO ES MesDelCambio: aquel es el mes más viejo entre los productos que entraron -un
		// hecho de los datos-, y éste es dónde se partió la historia para mirarlos. Desde que
		// el dominio ENCUENTRA el corte, los dos suelen coincidir; cuando no, la diferencia
		// dice que lo nuevo tardó en aparecer.
		public string Corte { get; init; } = "";

		/// <summary>Si los dos lados tienen compras. Sin eso no se afirma nada.</summary>
		public bool HayConQueComparar { get; init; }

		public bool HayCambio { get; init; }

		public string ParaSonia { get; init; } = "";

		public IReadOnlyList<ProductoQueEntroVista> Entraron { get; init; } =
			new List<ProductoQueEntroVista>();

		public IReadOnlyList<ProductoQueVuelve> Salieron { get; init; } =
			new List<ProductoQueVuelve>();

		// El mes del quiebre sale del primer producto que entro, que el dominio ya
		// entrega ordenado por fecha. Vacio cuando no hay cambio, y eso ya es respuesta.
		public string MesDelCambio => Entraron.Count > 0 ? Entraron[0].Desde : "";
	}

	// Un producto que se volvio habitual, con el mes en que aparecio por primera vez.
	public sealed class ProductoQueEntroVista
	{
		public string Sku { get; init; } = "";
		public string Descripcion { get; init; } = "";

		/// <summary>El mes en que se lo vio por primera vez: 2026-03.</summary>
		public string Desde { get; init; } = "";

		public int ComprasEnQueAparece { get; init; }
		public decimal Proporcion { get; init; }
	}

	// LA FICHA DE UN PRODUCTO, y de ahi los tres campos nuevos: en cuantas compras aparecio,
	// cuando fue la ultima, y que tan fiel es dicho en palabras. Es la pantalla "uno de tus
	// infaltables" del diseño de la app.

	// COMO SE MOVIO EL PRECIO de un producto que si lleva. Es SU inflacion, producto por
	// producto y por unidad de medida — no la del pais.
	public sealed class PrecioQueCambio
	{
		public string Sku { get; init; } = "";
		public string Descripcion { get; init; } = "";

		/// <summary>El precio por unidad de medida la primera vez que lo compró.</summary>
		public decimal Antes { get; init; }

		/// <summary>El precio por unidad de medida la última vez.</summary>
		public decimal Ahora { get; init; }

		/// <summary>Contra qué unidad se lee: "L", "kg", "u". Ningún precio va solo.</summary>
		public string Unidad { get; init; } = "";

		public decimal CambioEnProporcion { get; init; }
		public bool Subio { get; init; }
		public int VecesQueLoCompro { get; init; }
		public string ParaSonia { get; init; } = "";
	}

	// COMO VA AL SUPER: cada cuantos dias vuelve, que dia y en que momento. Es la pantalla
	// "tu ritmo de super" del diseño de la app — "hay dias y momentos que se repiten mas de
	// lo que parece"— con las tres facetas juntas.
	public sealed class RitmoDelSuperVista
	{
		public int ComprasMiradas { get; init; }

		/// <summary>Si el ritmo se pudo medir. Sin esto los demas campos vienen vacios.</summary>
		public bool EsObservado { get; init; }

		/// <summary>Cada cuántos días vuelve. Cero cuando no hay ritmo.</summary>
		public int DiasEntreCompras { get; init; }

		/// <summary>"todas las semanas". Vacío cuando no hay ritmo.</summary>
		public string Cadencia { get; init; } = "";

		/// <summary>"1 vez/semana", para una pastilla. Vacío cuando no hay ritmo.</summary>
		public string CadenciaCompacta { get; init; } = "";

		/// <summary>"sábado". Vacío cuando no hay ritmo.</summary>
		public string DiaFavorito { get; init; } = "";

		public int ComprasEnElDiaFavorito { get; init; }

		/// <summary>"tarde". Vacío cuando no hay ritmo.</summary>
		public string MomentoFavorito { get; init; } = "";

		public string ParaSonia { get; init; } = "";

		/// <summary>Los TRES momentos, siempre los tres y en orden del día.</summary>
		public IReadOnlyList<ComprasEnUnMomentoVista> PorMomento { get; init; } =
			new List<ComprasEnUnMomentoVista>();
	}

	// Cuantas compras cayeron en un momento del dia. Vienen los tres siempre, asi que una
	// pantalla puede dibujar la fila sin preguntar.
	public sealed class ComprasEnUnMomentoVista
	{
		public string Momento { get; init; } = "";
		public int Cuantas { get; init; }
		public int DeCuantas { get; init; }
		public decimal Proporcion { get; init; }
		public string ParaMostrar { get; init; } = "";
	}

	// CADA CUANTO compra un producto, antes y ahora. Es la tarjeta "un cambio en tu
	// rutina" del diseño de la app, y la respuesta de la S10 de la wiki.
	public sealed class RitmoDeUnProducto
	{
		public string Sku { get; init; } = "";
		public string Descripcion { get; init; } = "";

		/// <summary>Si hay suficientes compras de los dos lados. Sin esto no se afirma nada.</summary>
		public bool HayConQueComparar { get; init; }

		public bool Cambio { get; init; }

		/// <summary>La cadencia de antes, corta: "1 vez/mes". Vacía cuando no hay ritmo medido.</summary>
		public string Antes { get; init; } = "";

		/// <summary>La cadencia de ahora, corta: "1 vez/semana". Vacía cuando no hay ritmo medido.</summary>
		public string Ahora { get; init; } = "";

		/// <summary>La línea, escrita por el dominio: "X pasó de una vez al mes a todas las semanas".</summary>
		public string ParaSonia { get; init; } = "";
	}

	// Un producto al que le cambio el ritmo. Viene de una lista YA FILTRADA por el dominio
	// -solo los que tienen ritmo medido y cambiaron-, y por eso aca EsMasSeguido es un
	// hecho y no una lectura guardada.
	public sealed class RitmoQueCambio
	{
		public string Sku { get; init; } = "";
		public string Descripcion { get; init; } = "";
		public string Antes { get; init; } = "";
		public string Ahora { get; init; } = "";

		/// <summary>Si ahora lo compra más seguido que antes.</summary>
		public bool EsMasSeguido { get; init; }

		public string ParaSonia { get; init; } = "";
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

	// CUANTO Y CUANDO entro algo, mes a mes. Es la serie que permite decir desde cuando
	// cambio un consumo — lo que una lista de tickets no puede contar.
	public sealed class ConsumoEnElTiempo
	{
		public string Termino { get; init; } = "";
		public bool EsCategoria { get; init; }
		public int MesesConConsumo { get; init; }
		public decimal TotalGastado { get; init; }
		public int TotalUnidades { get; init; }

		/// <summary>En cuántas compras apareció. Es "compras en las que apareció" de la ficha.</summary>
		public int TotalCompras { get; init; }

		/// <summary>La última vez que lo llevó, en aaaa-mm-dd. Vacío cuando nunca.</summary>
		public string UltimaVez { get; init; } = "";

		/// <summary>Qué tan fiel es, en palabras: "casi nunca falta". Sobre TODO el historial.</summary>
		public string Constancia { get; init; } = "";

		public string PrimerMes { get; init; } = "";
		public string UltimoMes { get; init; } = "";
		public string MesDelPico { get; init; } = "";
		public decimal GastoEnElPrimerMes { get; init; }
		public decimal GastoEnElUltimoMes { get; init; }
		public decimal CambioDeGasto { get; init; }
		public bool HaySerie { get; init; }
		public bool HayCambioReal { get; init; }
		public string ParaSonia { get; init; } = "";
		public IReadOnlyList<MesDeConsumoVista> Meses { get; init; } = new List<MesDeConsumoVista>();

		/// <summary>La misma serie por año. Vacía cuando no hay consumo.</summary>
		public IReadOnlyList<AnioDeConsumoVista> Anios { get; init; } = new List<AnioDeConsumoVista>();

		public bool Existe => MesesConConsumo > 0;
	}

	public sealed class MesDeConsumoVista
	{
		public string Mes { get; init; } = "";
		public int Unidades { get; init; }
		public decimal Gasto { get; init; }
		public int Compras { get; init; }
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
		/// <summary>La llave del repertorio. No se le muestra a nadie: para eso esta Titulo.</summary>
		public string Categoria { get; init; } = "";
		public string Titulo { get; init; } = "";
		public DateTime UltimaCompra { get; init; }
		public int DiasDesdeLaUltima { get; init; }
		public int DiasEsperados { get; init; }
		/// <summary>Si los dias esperados se midieron en su historial o salen del catalogo.</summary>
		public bool EsRitmoObservado { get; init; }
		public bool EsComestible { get; init; }
		public string Severidad { get; init; } = "";
		public string ParaSonia { get; init; } = "";
	}

	// EN CUALES COMPRAS APARECIO ALGO. La respuesta del buscador de recibos, y la unica
	// pantalla donde una lista se acorta por la PREGUNTA y no por un tope: buscar "leche"
	// devuelve las compras con leche, no las primeras N de todas.
	// UN AÑO DE LA SERIE. Existe porque la serie mensual crece doce filas por año y esta
	// crece una: la ficha pinta el año mas reciente mes a mes y los anteriores de a una
	// fila. mesesConCompras es lo que impide comparar un año a medias con uno entero como
	// si midieran lo mismo.
	public sealed class AnioDeConsumoVista
	{
		public string Anio { get; init; } = "";
		public int Unidades { get; init; }
		public decimal Gasto { get; init; }
		public int Compras { get; init; }
		public int MesesConCompras { get; init; }
	}

	public sealed class ComprasHalladas
	{
		public string Buscado { get; init; } = "";
		public bool Existe { get; init; }

		/// <summary>En cuántas compras apareció. Cero cuando en ninguna.</summary>
		public int CuantasCompras { get; init; }

		public int TotalUnidades { get; init; }
		public decimal TotalGastado { get; init; }

		/// <summary>
		/// Las descripciones distintas que coincidieron. Buscar "queso" puede alcanzar a
		/// dos productos, y la pantalla lo dice antes de mostrar los totales.
		/// </summary>
		public IReadOnlyList<string> Productos { get; init; } = new List<string>();

		/// <summary>De la más reciente a la más vieja. Vacía cuando no hay ninguna.</summary>
		public IReadOnlyList<CompraHallada> Compras { get; init; } = new List<CompraHallada>();

		public string ParaSonia { get; init; } = "";
	}

	// Una compra donde aparecio lo buscado. Lleva LAS DOS ESCALAS -lo del producto y lo de
	// la compra entera- para que la fila se pueda pintar igual que las de la lista y ademas
	// diga por que aparecio.
	public sealed class CompraHallada
	{
		public string Numero { get; init; } = "";
		public string Fecha { get; init; } = "";
		public int Unidades { get; init; }
		public decimal Gasto { get; init; }
		public int LineasDeLaCompra { get; init; }
		public decimal TotalDeLaCompra { get; init; }
	}
}
