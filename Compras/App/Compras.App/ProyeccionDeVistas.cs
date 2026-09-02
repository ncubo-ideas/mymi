using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Compras.App.Vistas;

namespace Compras.App
{
	// Convierte la emision del print en los records de vista.
	//
	// Esto NO es el antipatron de "parsear el print para sacar un valor tipado". Ese
	// antipatron es re-derivar en C# un ESCALAR que el actor ya calculo, y para eso la
	// app usa parametros Out tipados (TotalDeCompras, CantidadDeCompras). Lo de aca es
	// otra cosa: una VISTA, y el print con formatter JSON es su canal declarado - se
	// devuelve ese string tal cual porque el print ya hizo todo el trabajo de
	// serializacion. Un cliente REST hace exactamente esto; aca el cliente esta en el
	// mismo proceso.
	//
	// El formato de fecha es el del formatter: MM/dd/yyyy HH:mm:ss en cultura
	// invariante.
	internal static class ProyeccionDeVistas
	{
		private const string FormatoDeFecha = "MM/dd/yyyy HH:mm:ss";
		private const string FormatoDeFechaSinHora = "MM/dd/yyyy";

		internal static IReadOnlyList<CompraEnLista> Lista(string json)
		{
			var compras = new List<CompraEnLista>();
			if (string.IsNullOrWhiteSpace(json)) return compras;

			using JsonDocument documento = JsonDocument.Parse(json);

			// Sin compras el foreach no emite nada y la propiedad no existe: eso es una
			// lista vacia, no un error.
			if (!documento.RootElement.TryGetProperty("compra", out JsonElement arreglo)) return compras;

			foreach (JsonElement elemento in Elementos(arreglo))
				compras.Add(new CompraEnLista
				{
					Numero = Texto(elemento, "numero"),
					Fecha = Fecha(elemento, "fecha"),
					Lineas = Entero(elemento, "lineas"),
					Total = Decimal(elemento, "total"),
				});

			return compras;
		}

		internal static CompraDetallada Detalle(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new CompraDetallada();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			var lineas = new List<LineaDeCompra>();
			if (raiz.TryGetProperty("item", out JsonElement arreglo))
				foreach (JsonElement elemento in Elementos(arreglo))
					lineas.Add(new LineaDeCompra
					{
						Sku = Texto(elemento, "sku"),
						Descripcion = Texto(elemento, "descripcion"),
						Cantidad = Entero(elemento, "cantidad"),
						PrecioUnitario = Decimal(elemento, "precioUnitario"),
						Total = Decimal(elemento, "totalDeLinea"),
						Categoria = Texto(elemento, "categoria"),
						Titulo = Texto(elemento, "titulo"),
						TieneGarantia = Booleano(elemento, "tieneGarantia"),
					});

			return new CompraDetallada
			{
				Numero = Texto(raiz, "numero"),
				Fecha = Fecha(raiz, "fecha"),
				Total = Decimal(raiz, "total"),
				Lineas = lineas,
			};
		}

		internal static CanastaDelMesVista Canasta(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new CanastaDelMesVista();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			// La cabecera sale como "canasta" y cada pasillo como "departamento": son dos
			// print distintos del mismo bloque.
			JsonElement cabecera = raiz.TryGetProperty("canasta", out JsonElement c) ? c : raiz;

			var departamentos = new List<GastoPorDepartamentoVista>();
			if (raiz.TryGetProperty("departamento", out JsonElement arreglo))
				foreach (JsonElement elemento in Elementos(arreglo))
					departamentos.Add(new GastoPorDepartamentoVista
					{
						Categoria = Texto(elemento, "categoria"),
						Titulo = Texto(elemento, "titulo"),
						Monto = Decimal(elemento, "monto"),
						ProporcionDelTotal = Decimal(elemento, "proporcionDelTotal"),
					});

			return new CanastaDelMesVista
			{
				Anio = Entero(cabecera, "anio"),
				Mes = Entero(cabecera, "mes"),
				CantidadDeCompras = Entero(cabecera, "cantidadDeCompras"),
				GastoNoComestible = Decimal(cabecera, "gastoNoComestible"),
				GastoComestible = Decimal(cabecera, "gastoComestible"),
				GastoTotal = Decimal(cabecera, "gastoTotal"),
				HayComestible = Booleano(cabecera, "hayComestible"),
				HayCompras = Booleano(cabecera, "hayCompras"),
				Departamentos = departamentos,
			};
		}

		internal static CanastasComparadas Comparacion(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new CanastasComparadas();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			JsonElement cabecera = raiz.TryGetProperty("comparacion", out JsonElement c) ? c : raiz;

			var diferencias = new List<DiferenciaPorDepartamentoVista>();
			if (raiz.TryGetProperty("diferencia", out JsonElement arreglo))
				foreach (JsonElement elemento in Elementos(arreglo))
					diferencias.Add(new DiferenciaPorDepartamentoVista
					{
						Categoria = Texto(elemento, "categoria"),
						Titulo = Texto(elemento, "titulo"),
						MontoAntes = Decimal(elemento, "montoAntes"),
						MontoDespues = Decimal(elemento, "montoDespues"),
						CambioDelPasillo = Decimal(elemento, "cambioDelPasillo"),
						Subio = Booleano(elemento, "subio"),
						EsNuevo = Booleano(elemento, "esNuevo"),
						Desaparecio = Booleano(elemento, "desaparecio"),
						ExplicaElCambio = Booleano(elemento, "explicaElCambio"),
					});

			return new CanastasComparadas
			{
				HayComparacion = Booleano(cabecera, "hayComparacion"),
				GastoAntes = Decimal(cabecera, "gastoAntes"),
				GastoDespues = Decimal(cabecera, "gastoDespues"),
				CambioEnElTotal = Decimal(cabecera, "cambioEnElTotal"),
				Cambio = Decimal(cabecera, "cambio"),
				HayAlgoQueLoExplique = Booleano(cabecera, "hayAlgoQueLoExplique"),
				ParaElAsistente = Texto(cabecera, "paraElAsistente"),
				AnioAntes = Entero(cabecera, "anioAntes"),
				MesAntes = Entero(cabecera, "mesAntes"),
				GastoComestibleAntes = Decimal(cabecera, "gastoComestibleAntes"),
				AnioDespues = Entero(cabecera, "anioDespues"),
				MesDespues = Entero(cabecera, "mesDespues"),
				GastoComestibleDespues = Decimal(cabecera, "gastoComestibleDespues"),
				Diferencias = diferencias,
			};
		}

		internal static FirmaDeLaSemana Firma(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new FirmaDeLaSemana();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			// La cabecera sale como "firma" y cada producto como "habitual": son dos print
			// distintos del mismo bloque.
			JsonElement cabecera = raiz.TryGetProperty("firma", out JsonElement f) ? f : raiz;

			return new FirmaDeLaSemana
			{
				ComprasMiradas = Entero(cabecera, "comprasMiradas"),
				CuantosHabituales = Entero(cabecera, "cuantosHabituales"),
				ParaElAsistente = Texto(cabecera, "paraElAsistente"),
				Habituales = Habituales(raiz, "habitual"),
			};
		}

		internal static CambioDeLaCanasta Cambio(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new CambioDeLaCanasta();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			JsonElement cabecera = raiz.TryGetProperty("cambio", out JsonElement c) ? c : raiz;

			var entraron = new List<ProductoQueEntroVista>();
			if (raiz.TryGetProperty("entro", out JsonElement arreglo))
				foreach (JsonElement elemento in Elementos(arreglo))
					entraron.Add(new ProductoQueEntroVista
					{
						Sku = Texto(elemento, "sku"),
						Descripcion = Texto(elemento, "descripcion"),
						Desde = Texto(elemento, "desde"),
						ComprasEnQueAparece = Entero(elemento, "comprasEnQueAparece"),
						Proporcion = Decimal(elemento, "proporcion"),
					});

			return new CambioDeLaCanasta
			{
				HayConQueComparar = Booleano(cabecera, "hayConQueComparar"),
				HayCambio = Booleano(cabecera, "hayCambio"),
				Corte = Texto(cabecera, "corte"),
				ParaElAsistente = Texto(cabecera, "paraElAsistente"),
				Entraron = entraron,
				Salieron = Habituales(raiz, "salio"),
			};
		}

		// Los que vuelven, en las dos lecturas: la firma los llama "habitual" y el cambio
		// llama "salio" a los que se cayeron. Misma forma, mismo lector.
		private static IReadOnlyList<ProductoQueVuelve> Habituales(JsonElement raiz, string llave)
		{
			var productos = new List<ProductoQueVuelve>();
			if (!raiz.TryGetProperty(llave, out JsonElement arreglo)) return productos;

			foreach (JsonElement elemento in Elementos(arreglo))
				productos.Add(new ProductoQueVuelve
				{
					Sku = Texto(elemento, "sku"),
					Descripcion = Texto(elemento, "descripcion"),
					ComprasEnQueAparece = Entero(elemento, "comprasEnQueAparece"),
					DeCuantas = Entero(elemento, "deCuantas"),
					Proporcion = Decimal(elemento, "proporcion"),
					EstaEnTodas = Booleano(elemento, "estaEnTodas"),
					Constancia = Texto(elemento, "constancia"),
				});

			return productos;
		}

		internal static IReadOnlyList<PrecioQueCambio> Precios(string json)
		{
			var precios = new List<PrecioQueCambio>();
			if (string.IsNullOrWhiteSpace(json)) return precios;

			using JsonDocument documento = JsonDocument.Parse(json);
			if (!documento.RootElement.TryGetProperty("precio", out JsonElement arreglo)) return precios;

			foreach (JsonElement elemento in Elementos(arreglo))
				precios.Add(new PrecioQueCambio
				{
					Sku = Texto(elemento, "sku"),
					Descripcion = Texto(elemento, "descripcion"),
					Antes = Decimal(elemento, "antes"),
					Ahora = Decimal(elemento, "ahora"),
					Unidad = Texto(elemento, "unidad"),
					CambioEnProporcion = Decimal(elemento, "cambioEnProporcion"),
					Subio = Booleano(elemento, "subio"),
					VecesQueLoCompro = Entero(elemento, "vecesQueLoCompro"),
					ParaElAsistente = Texto(elemento, "paraElAsistente"),
				});

			return precios;
		}

		internal static CanastaFrenteAlPasadoVista FrenteAlPasado(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new CanastaFrenteAlPasadoVista();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			// La cabecera cae bajo el nombre de la variable del print ("frente"); el
			// arreglo, bajo el de la variable del foreach ("producto").
			JsonElement cabecera = raiz.TryGetProperty("frente", out JsonElement f) ? f : raiz;

			var productos = new List<ProductoFrenteAlPasadoVista>();
			if (raiz.TryGetProperty("producto", out JsonElement arreglo))
				foreach (JsonElement elemento in Elementos(arreglo))
					productos.Add(new ProductoFrenteAlPasadoVista
					{
						Sku = Texto(elemento, "sku"),
						Descripcion = Texto(elemento, "descripcion"),
						Presentacion = Texto(elemento, "presentacion"),
						Antes = Decimal(elemento, "antes"),
						Ahora = Decimal(elemento, "ahora"),
						Cambio = Decimal(elemento, "cambio"),
					});

			return new CanastaFrenteAlPasadoVista
			{
				HayConQueComparar = Booleano(cabecera, "hayConQueComparar"),
				MesesDeVentana = Entero(cabecera, "mesesDeVentana"),
				Cambio = Decimal(cabecera, "cambio"),
				ParaElAsistente = Texto(cabecera, "paraElAsistente"),
				Productos = productos,
			};
		}

		internal static RebajaVista Rebaja(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new RebajaVista();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;
			JsonElement rebaja = raiz.TryGetProperty("rebaja", out JsonElement r) ? r : raiz;

			return new RebajaVista
			{
				Hay = Booleano(rebaja, "hay"),
				Sku = Texto(rebaja, "sku"),
				Descripcion = Texto(rebaja, "descripcion"),
				CuantoMenos = Decimal(rebaja, "cuantoMenos"),
				ParaElAsistente = Texto(rebaja, "paraElAsistente"),
			};
		}

		internal static RitmoDelSuperVista RitmoDelSuper(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new RitmoDelSuperVista();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;
			JsonElement r = raiz.TryGetProperty("ritmo", out JsonElement uno) ? uno : raiz;

			var momentos = new List<ComprasEnUnMomentoVista>();
			if (raiz.TryGetProperty("momento", out JsonElement arreglo))
				foreach (JsonElement elemento in Elementos(arreglo))
					momentos.Add(new ComprasEnUnMomentoVista
					{
						Momento = Texto(elemento, "nombre"),
						Cuantas = Entero(elemento, "cuantas"),
						DeCuantas = Entero(elemento, "deCuantas"),
						Proporcion = Decimal(elemento, "proporcion"),
						ParaMostrar = Texto(elemento, "paraMostrar"),
					});

			return new RitmoDelSuperVista
			{
				ComprasMiradas = Entero(r, "comprasMiradas"),
				EsObservado = Booleano(r, "esObservado"),
				DiasEntreCompras = Entero(r, "diasEntreCompras"),
				Cadencia = Texto(r, "cadencia"),
				CadenciaCompacta = Texto(r, "cadenciaCompacta"),
				DiaFavorito = Texto(r, "diaFavorito"),
				ComprasEnElDiaFavorito = Entero(r, "comprasEnElDiaFavorito"),
				MomentoFavorito = Texto(r, "momentoFavorito"),
				ParaElAsistente = Texto(r, "paraElAsistente"),
				PorMomento = momentos,
			};
		}

		internal static RitmoDeUnProducto Ritmo(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new RitmoDeUnProducto();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;
			JsonElement r = raiz.TryGetProperty("ritmo", out JsonElement uno) ? uno : raiz;

			return new RitmoDeUnProducto
			{
				Sku = Texto(r, "sku"),
				Descripcion = Texto(r, "descripcion"),
				HayConQueComparar = Booleano(r, "hayConQueComparar"),
				Cambio = Booleano(r, "cambio"),
				Antes = Texto(r, "antes"),
				Ahora = Texto(r, "ahora"),
				ParaElAsistente = Texto(r, "paraElAsistente"),
			};
		}

		internal static IReadOnlyList<RitmoQueCambio> RitmosQueCambiaron(string json)
		{
			var cambios = new List<RitmoQueCambio>();
			if (string.IsNullOrWhiteSpace(json)) return cambios;

			using JsonDocument documento = JsonDocument.Parse(json);
			if (!documento.RootElement.TryGetProperty("ritmo", out JsonElement arreglo)) return cambios;

			foreach (JsonElement elemento in Elementos(arreglo))
				cambios.Add(new RitmoQueCambio
				{
					Sku = Texto(elemento, "sku"),
					Descripcion = Texto(elemento, "descripcion"),
					Antes = Texto(elemento, "antes"),
					Ahora = Texto(elemento, "ahora"),
					EsMasSeguido = Booleano(elemento, "esMasSeguido"),
					ParaElAsistente = Texto(elemento, "paraElAsistente"),
				});

			return cambios;
		}

		internal static GarantiaDeProducto Garantia(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new GarantiaDeProducto();

			using JsonDocument documento = JsonDocument.Parse(json);
			return Garantia(documento.RootElement);
		}

		internal static IReadOnlyList<GarantiaDeProducto> GarantiasVigentes(string json)
		{
			var garantias = new List<GarantiaDeProducto>();
			if (string.IsNullOrWhiteSpace(json)) return garantias;

			using JsonDocument documento = JsonDocument.Parse(json);
			if (!documento.RootElement.TryGetProperty("registro", out JsonElement arreglo)) return garantias;

			foreach (JsonElement elemento in Elementos(arreglo))
				garantias.Add(Garantia(elemento));

			return garantias;
		}

		// EN CUALES COMPRAS APARECIO ALGO. Tres print del mismo bloque: la cabecera sale
		// como "busqueda", cada producto que coincidio como "nombre" y cada compra como
		// "compra".
		// LO QUE VA A FALTAR. La clave del arreglo es el nombre de la variable del foreach
		// -"producto"-, no el alias del print.
		internal static ListaDeLaProximaCompra LaProximaCompra(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new ListaDeLaProximaCompra();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;
			JsonElement cabecera = raiz.TryGetProperty("lista", out JsonElement c) ? c : raiz;

			var productos = new List<ProductoPorCaer>();
			if (raiz.TryGetProperty("producto", out JsonElement arreglo))
				foreach (JsonElement elemento in Elementos(arreglo))
					productos.Add(new ProductoPorCaer
					{
						Sku = Texto(elemento, "sku"),
						Descripcion = Texto(elemento, "descripcion"),
						Cadencia = Texto(elemento, "cadencia"),
						CadenciaCompacta = Texto(elemento, "cadenciaCompacta"),
						DiasTipicos = Entero(elemento, "diasTipicos"),
						UltimaVez = Texto(elemento, "ultimaVez"),
						DiasDesdeLaUltima = Entero(elemento, "diasDesdeLaUltima"),
						DiasDeMas = Entero(elemento, "diasDeMas"),
						Toca = Booleano(elemento, "toca"),
						ParaElAsistente = Texto(elemento, "paraElAsistente"),
					});

			return new ListaDeLaProximaCompra
			{
				Existe = Booleano(cabecera, "existe"),
				CuantosTocan = Entero(cabecera, "cuantosTocan"),
				Productos = productos,
				ParaElAsistente = Texto(cabecera, "paraElAsistente"),
			};
		}

		// La clave del arreglo es el nombre de la variable del foreach: "promocion".
		internal static IReadOnlyList<PromocionVista> Promociones(string json)
		{
			var promociones = new List<PromocionVista>();
			if (string.IsNullOrWhiteSpace(json)) return promociones;

			using JsonDocument documento = JsonDocument.Parse(json);
			if (!documento.RootElement.TryGetProperty("promocion", out JsonElement arreglo))
				return promociones;

			foreach (JsonElement elemento in Elementos(arreglo))
				promociones.Add(new PromocionVista
				{
					Gancho = Texto(elemento, "gancho"),
					Detalle = Texto(elemento, "detalle"),
					Sku = Texto(elemento, "sku"),
					ValidaHasta = Texto(elemento, "validaHasta"),
					DiasQueQuedan = Entero(elemento, "diasQueQuedan"),
					ParaElAsistente = Texto(elemento, "paraElAsistente"),
				});

			return promociones;
		}

		internal static ComprasHalladas Halladas(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new ComprasHalladas();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;
			JsonElement cabecera = raiz.TryGetProperty("busqueda", out JsonElement c) ? c : raiz;

			// LA CLAVE DEL ARREGLO ES EL NOMBRE DE LA VARIABLE DEL FOREACH, no el alias del
			// print: `foreach (producto in ...) { print producto nombre; }` sale como
			// "producto":[{"nombre":"..."}]. Leer "nombre" en la raiz no encuentra nada y
			// -esto es lo peor- no falla: la lista queda vacia y la pantalla pinta la
			// cabecera sin el producto, como paso.
			var productos = new List<string>();
			if (raiz.TryGetProperty("producto", out JsonElement nombres))
				foreach (JsonElement elemento in Elementos(nombres))
					productos.Add(Texto(elemento, "nombre"));

			var halladas = new List<CompraHallada>();
			if (raiz.TryGetProperty("compra", out JsonElement arreglo))
				foreach (JsonElement elemento in Elementos(arreglo))
					halladas.Add(new CompraHallada
					{
						Numero = Texto(elemento, "numero"),
						Fecha = Texto(elemento, "fecha"),
						Unidades = Entero(elemento, "unidades"),
						Gasto = Decimal(elemento, "gasto"),
						LineasDeLaCompra = Entero(elemento, "lineasDeLaCompra"),
						TotalDeLaCompra = Decimal(elemento, "totalDeLaCompra"),
					});

			return new ComprasHalladas
			{
				Buscado = Texto(cabecera, "buscado"),
				Existe = Booleano(cabecera, "existe"),
				CuantasCompras = Entero(cabecera, "cuantasCompras"),
				TotalUnidades = Entero(cabecera, "totalUnidades"),
				TotalGastado = Decimal(cabecera, "totalGastado"),
				Productos = productos,
				Compras = halladas,
				ParaElAsistente = Texto(cabecera, "paraElAsistente"),
			};
		}

		internal static ConsumoEnElTiempo Consumo(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new ConsumoEnElTiempo();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			// La cabecera sale como "consumo" y cada mes como "mes": son dos print
			// distintos del mismo bloque.
			JsonElement cabecera = raiz.TryGetProperty("consumo", out JsonElement c) ? c : raiz;

			var meses = new List<MesDeConsumoVista>();
			if (raiz.TryGetProperty("mes", out JsonElement arreglo))
				foreach (JsonElement elemento in Elementos(arreglo))
					meses.Add(new MesDeConsumoVista
					{
						Mes = Texto(elemento, "mes"),
						Unidades = Entero(elemento, "unidades"),
						Gasto = Decimal(elemento, "gasto"),
						Compras = Entero(elemento, "compras"),
					});

			// LA CLAVE ES LA VARIABLE DEL FOREACH, no el alias del print: `foreach (anio in
			// ...)` sale como "anio":[{...}]. Leer mal la clave no falla — deja la lista
			// vacia y la pantalla pinta de menos, que es peor que un error.
			var anios = new List<AnioDeConsumoVista>();
			if (raiz.TryGetProperty("anio", out JsonElement porAnio))
				foreach (JsonElement elemento in Elementos(porAnio))
					anios.Add(new AnioDeConsumoVista
					{
						Anio = Texto(elemento, "anio"),
						Unidades = Entero(elemento, "unidades"),
						Gasto = Decimal(elemento, "gasto"),
						Compras = Entero(elemento, "compras"),
						MesesConCompras = Entero(elemento, "mesesConCompras"),
					});

			return new ConsumoEnElTiempo
			{
				Termino = Texto(cabecera, "termino"),
				Anios = anios,
				EsCategoria = Booleano(cabecera, "esCategoria"),
				MesesConConsumo = Entero(cabecera, "mesesConConsumo"),
				TotalGastado = Decimal(cabecera, "totalGastado"),
				TotalUnidades = Entero(cabecera, "totalUnidades"),
				TotalCompras = Entero(cabecera, "totalCompras"),
				UltimaVez = Texto(cabecera, "ultimaVez"),
				Constancia = Texto(cabecera, "constancia"),
				PrimerMes = Texto(cabecera, "primerMes"),
				UltimoMes = Texto(cabecera, "ultimoMes"),
				MesDelPico = Texto(cabecera, "mesDelPico"),
				GastoEnElPrimerMes = Decimal(cabecera, "gastoEnElPrimerMes"),
				GastoEnElUltimoMes = Decimal(cabecera, "gastoEnElUltimoMes"),
				CambioDeGasto = Decimal(cabecera, "cambioDeGasto"),
				HaySerie = Booleano(cabecera, "haySerie"),
				HayCambioReal = Booleano(cabecera, "hayCambioReal"),
				ParaElAsistente = Texto(cabecera, "paraElAsistente"),
				Meses = meses,
			};
		}

		internal static RangoDelHistorialVista Rango(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new RangoDelHistorialVista();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			return new RangoDelHistorialVista
			{
				MesesConCompras = Entero(raiz, "mesesConCompras"),
				PrimerMes = Texto(raiz, "primerMes"),
				UltimoMes = Texto(raiz, "ultimoMes"),
			};
		}

		internal static IReadOnlyList<AbastecimientoEnEspera> Abastecimientos(string json)
		{
			var pendientes = new List<AbastecimientoEnEspera>();
			if (string.IsNullOrWhiteSpace(json)) return pendientes;

			using JsonDocument documento = JsonDocument.Parse(json);
			if (!documento.RootElement.TryGetProperty("pendiente", out JsonElement arreglo)) return pendientes;

			foreach (JsonElement elemento in Elementos(arreglo))
				pendientes.Add(new AbastecimientoEnEspera
				{
					Categoria = Texto(elemento, "categoria"),
					Titulo = Texto(elemento, "titulo"),
					UltimaCompra = Fecha(elemento, "ultimaCompra"),
					DiasDesdeLaUltima = Entero(elemento, "diasDesdeLaUltima"),
					DiasEsperados = Entero(elemento, "diasEsperados"),
					EsRitmoObservado = Booleano(elemento, "esRitmoObservado"),
					EsComestible = Booleano(elemento, "esComestible"),
					Severidad = Texto(elemento, "severidad"),
					ParaElAsistente = Texto(elemento, "paraElAsistente"),
				});

			return pendientes;
		}

		private static GarantiaDeProducto Garantia(JsonElement elemento) =>
			new GarantiaDeProducto
			{
				// La lista de vigentes no emite "encontrada" (todas lo estan); la
				// busqueda puntual si. Ausente se lee como encontrada si trae sku.
				Encontrada = elemento.TryGetProperty("encontrada", out _)
					? Booleano(elemento, "encontrada")
					: Texto(elemento, "sku").Length > 0,
				TextoBuscado = Texto(elemento, "textoBuscado"),
				Sku = Texto(elemento, "sku"),
				Descripcion = Texto(elemento, "descripcion"),
				NumeroDeCompra = Texto(elemento, "numeroDeCompra"),
				FechaDeCompra = Fecha(elemento, "fechaDeCompra"),
				MesesDeCobertura = Entero(elemento, "mesesDeCobertura"),
				VenceEl = Fecha(elemento, "venceEl"),
				Vigente = elemento.TryGetProperty("vigente", out _)
					? Booleano(elemento, "vigente")
					: true,
				DiasParaVencer = Entero(elemento, "diasParaVencer"),
				Severidad = Texto(elemento, "severidad"),
				QueHacer = Texto(elemento, "queHacer"),
				ParaElAsistente = Texto(elemento, "paraElAsistente"),
			};

		// Un solo print emite un objeto; varios emiten un arreglo. Tratar los dos casos
		// evita que la vista se rompa justo cuando hay una sola compra.
		private static IEnumerable<JsonElement> Elementos(JsonElement elemento)
		{
			if (elemento.ValueKind == JsonValueKind.Array)
			{
				foreach (JsonElement hijo in elemento.EnumerateArray()) yield return hijo;
				yield break;
			}
			if (elemento.ValueKind == JsonValueKind.Object) yield return elemento;
		}

		private static string Texto(JsonElement padre, string nombre) =>
			padre.TryGetProperty(nombre, out JsonElement e) && e.ValueKind == JsonValueKind.String
				? e.GetString() : "";

		private static int Entero(JsonElement padre, string nombre) =>
			padre.TryGetProperty(nombre, out JsonElement e) && e.TryGetInt32(out int valor) ? valor : 0;

		private static decimal Decimal(JsonElement padre, string nombre) =>
			padre.TryGetProperty(nombre, out JsonElement e) && e.TryGetDecimal(out decimal valor) ? valor : 0m;

		private static bool Booleano(JsonElement padre, string nombre)
		{
			if (!padre.TryGetProperty(nombre, out JsonElement e)) return false;
			if (e.ValueKind == JsonValueKind.True) return true;
			if (e.ValueKind == JsonValueKind.False) return false;

			// El formatter puede emitir el booleano como texto; se aceptan las dos formas
			// para que la vista no dependa de ese detalle.
			return e.ValueKind == JsonValueKind.String
				&& bool.TryParse(e.GetString(), out bool valor) && valor;
		}

		private static DateTime Fecha(JsonElement padre, string nombre)
		{
			string texto = Texto(padre, nombre);
			if (texto.Length == 0) return default;

			if (DateTime.TryParseExact(texto, FormatoDeFecha, CultureInfo.InvariantCulture,
					DateTimeStyles.None, out DateTime conHora)) return conHora;
			if (DateTime.TryParseExact(texto, FormatoDeFechaSinHora, CultureInfo.InvariantCulture,
					DateTimeStyles.None, out DateTime soloFecha)) return soloFecha;

			return DateTime.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime suelta)
				? suelta : default;
		}
	}
}
