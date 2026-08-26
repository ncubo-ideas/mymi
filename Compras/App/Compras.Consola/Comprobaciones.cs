using System;
using System.Globalization;
using Compras.Dominio;

namespace Compras.Consola
{
	// Las comprobaciones del DOMINIO PURO, sin actor y sin journal.
	//
	// Van en la consola y no en un proyecto de test porque este repo no tiene framework
	// de pruebas y Examenes usa la misma idea: la caratula de consola ES el arnes, y
	// `medir` es su criterio de aceptacion. Esto es la version del dominio, un nivel mas
	// abajo: corre en microsegundos, no toca disco, y cuando algo se rompe dice
	// exactamente que regla se rompio en vez de dejar un total distinto al esperado.
	//
	// Lo que se comprueba aca es lo que NO se puede ver en la salida de `medir`: que la
	// categoria lleve el perfil, que lo no comestible no contamine la proporcion, que la
	// garantia corra desde la fecha de la compra, y que las preguntas totales contesten
	// "no hay" en vez de reventar.
	internal static class Comprobaciones
	{
		private static int fallas;
		private static int corridas;

		// Fecha de referencia fija. Igual que el guion: si esto usara DateTime.Now, la
		// comprobacion de "vigente hoy" cambiaria de resultado segun el dia.
		private static readonly DateTime Hoy = new DateTime(2026, 8, 25);

		internal static int Correr()
		{
			fallas = 0;
			corridas = 0;

			LasLineasSeValidanJuntas();
			LaCategoriaLlevaElPerfil();
			LoNoComestibleNoTienePerfil();
			LaGarantiaCorreDesdeLaCompra();
			NoTodoTieneGarantia();
			ElHistorialRechazaElTicketRepetido();
			LaCanastaSumaPorPerfil();
			LoNoComestibleNoContaminaLaProporcion();
			LaComparacionCuentaElGuion();
			CompararContraUnMesVacioEsLegitimo();
			LaGarantiaSeEncuentraConSuRespaldo();
			BuscarLoQueNoExisteEsLegitimo();
			GanaLaCompraMasReciente();

			Console.WriteLine();
			Console.WriteLine(fallas == 0
				? $"TODO OK  ({corridas} comprobaciones)"
				: $"{fallas} FALLAS de {corridas} comprobaciones");
			return fallas == 0 ? 0 : 1;
		}

		// ---- el ticket se arma entero o no se arma ----
		private static void LasLineasSeValidanJuntas()
		{
			Titulo("las seis listas se validan juntas");

			Revienta("compra sin lineas", () => new Compra("T-1", Hoy,
				Array.Empty<string>(), Array.Empty<string>(), Array.Empty<int>(),
				Array.Empty<decimal>(), Array.Empty<string>(), Array.Empty<int>()));

			Revienta("listas de largos distintos", () => new Compra("T-1", Hoy,
				new[] { "A", "B" }, new[] { "a" }, new[] { 1, 1 },
				new[] { 1m, 1m }, new[] { "Proteinas", "Proteinas" }, new[] { 0, 0 }));

			Revienta("cantidad cero", () => new Compra("T-1", Hoy,
				new[] { "A" }, new[] { "a" }, new[] { 0 },
				new[] { 1m }, new[] { "Proteinas" }, new[] { 0 }));

			Revienta("precio negativo", () => new Compra("T-1", Hoy,
				new[] { "A" }, new[] { "a" }, new[] { 1 },
				new[] { -1m }, new[] { "Proteinas" }, new[] { 0 }));

			Revienta("categoria desconocida", () => new Compra("T-1", Hoy,
				new[] { "A" }, new[] { "a" }, new[] { 1 },
				new[] { 1m }, new[] { "Mascotas" }, new[] { 0 }));

			Compra valida = UnaCompra("T-1", Hoy, ("A", "a", 2, 3m, "Proteinas", 0));
			Igual("el total se deriva de las lineas", "6", valida.Total.ToString());
		}

		// ---- el repertorio: la categoria sabe su perfil ----
		private static void LaCategoriaLlevaElPerfil()
		{
			Titulo("la categoria lleva el perfil");

			Igual("proteinas es recomendado", "recomendado", Categoria.Proteinas.Perfil.Nombre);
			Igual("azucarados es ocasional", "ocasional", Categoria.Azucarados.Perfil.Nombre);
			Igual("aceites es ocasional", "ocasional", Categoria.AceitesYGrasas.Perfil.Nombre);
			Igual("granos es neutro", "neutro", Categoria.GranosYAbarrotes.Perfil.Nombre);

			// El resolver de la frontera: mismo string -> LA instancia (referencia).
			Igual("Desde resuelve a la instancia", "True",
				ReferenceEquals(Categoria.Desde("azucarados"), Categoria.Azucarados).ToString());
			Revienta("Desde con nombre desconocido", () => Categoria.Desde("Mascotas"));
		}

		private static void LoNoComestibleNoTienePerfil()
		{
			Titulo("lo no comestible no tiene perfil");

			Igual("limpieza no es comestible", "False", Categoria.Limpieza.EsComestible.ToString());
			// Preguntar el perfil de un detergente revienta: devolver "neutro" seria
			// mentir en las proporciones de la canasta.
			Revienta("el perfil de limpieza revienta", () => Categoria.Limpieza.Perfil);
			Revienta("el perfil de electrohogar revienta", () => Categoria.Electrohogar.Perfil);
		}

		// ---- la garantia corre desde la fecha del ticket ----
		private static void LaGarantiaCorreDesdeLaCompra()
		{
			Titulo("la garantia corre desde la compra");

			Garantia garantia = Garantia.De(12);
			DateTime compra = new DateTime(2026, 5, 10);

			Igual("vence 12 meses despues", "05/10/2027", garantia.VenceEl(compra).ToString("MM/dd/yyyy"));
			Igual("vigente hoy", "True", garantia.EstaVigenteEl(compra, Hoy).ToString());
			Igual("vigente el dia exacto del vencimiento", "True",
				garantia.EstaVigenteEl(compra, new DateTime(2027, 5, 10)).ToString());
			Igual("vencida un dia despues", "False",
				garantia.EstaVigenteEl(compra, new DateTime(2027, 5, 11)).ToString());

			Revienta("cero meses no es una garantia", () => Garantia.De(0));
		}

		private static void NoTodoTieneGarantia()
		{
			Titulo("no todo tiene garantia");

			Compra compra = UnaCompra("T-2", Hoy,
				("HELADO", "Helado", 1, 3000m, "Azucarados", 0),
				("OLLA", "Olla arrocera", 1, 45000m, "Electrohogar", 12));

			Igual("el helado no tiene", "False", compra.Items[0].TieneGarantia.ToString());
			Igual("la olla si tiene", "True", compra.Items[1].TieneGarantia.ToString());
			// La ausencia tiene tipo (Garantia.Ninguna, nunca null): preguntar lo que no
			// aplica revienta, no contesta un cero que despues alguien suma.
			Igual("la ausencia es Garantia.Ninguna", "True",
				ReferenceEquals(compra.Items[0].Garantia, Garantia.Ninguna).ToString());
			Revienta("leer la cobertura del helado revienta", () => compra.Items[0].Garantia.MesesDeCobertura);
			Igual("lo sin garantia nunca esta vigente", "False",
				compra.Items[0].Garantia.EstaVigenteEl(Hoy, Hoy).ToString());
		}

		private static void ElHistorialRechazaElTicketRepetido()
		{
			Titulo("el historial rechaza el ticket repetido");

			var historial = new Historial();
			historial.Agregar(UnaCompra("T-1", Hoy, ("A", "a", 1, 100m, "Proteinas", 0)));

			Revienta("mismo numero dos veces", () =>
				historial.Agregar(UnaCompra("T-1", Hoy, ("B", "b", 1, 50m, "Proteinas", 0))));
			Igual("el rechazo no dejo nada", "1", historial.Cantidad.ToString());
			Igual("el indexador encuentra por numero", "100", historial["T-1"].Total.ToString());
			Revienta("un numero que no existe revienta", () => historial["T-9"]);
		}

		// ---- la canasta: los hechos del analisis ----
		private static void LaCanastaSumaPorPerfil()
		{
			Titulo("la canasta suma por perfil");

			var historial = new Historial();
			historial.Agregar(UnaCompra("T-1", new DateTime(2026, 8, 3),
				("POLLO", "Pollo", 2, 4000m, "Proteinas", 0),          // 8000 recomendado
				("ARROZ", "Arroz", 1, 2000m, "GranosYAbarrotes", 0),   // 2000 neutro
				("HELADO", "Helado", 3, 3000m, "Azucarados", 0)));     // 9000 ocasional
			historial.Agregar(UnaCompra("T-2", new DateTime(2026, 8, 20),
				("SODA", "Soda", 1, 1000m, "Azucarados", 0)));         // 1000 ocasional

			Canasta canasta = historial.CanastaDe(2026, 8);
			Igual("compras del mes", "2", canasta.CantidadDeCompras.ToString());
			Igual("recomendado", "8000", canasta.GastoRecomendado.ToString());
			Igual("neutro", "2000", canasta.GastoNeutro.ToString());
			Igual("ocasional", "10000", canasta.GastoOcasional.ToString());
			Igual("comestible", "20000", canasta.GastoComestible.ToString());
			Igual("proporcion ocasional", "0.5",
				canasta.ProporcionOcasional.ToString(CultureInfo.InvariantCulture));

			// Un mes sin compras es una pregunta legitima, no una excepcion.
			Canasta vacia = historial.CanastaDe(2026, 1);
			Igual("mes vacio: sin compras", "False", vacia.HayCompras.ToString());
			Igual("mes vacio: proporcion 0", "0", vacia.ProporcionOcasional.ToString());
		}

		private static void LoNoComestibleNoContaminaLaProporcion()
		{
			Titulo("lo no comestible no contamina la proporcion");

			var historial = new Historial();
			// La olla arrocera vale 45900 y el helado 3000: si la proporcion se sacara
			// sobre el total, el electrodomestico esconderia el helado.
			historial.Agregar(UnaCompra("T-1", new DateTime(2026, 5, 10),
				("OLLA", "Olla arrocera", 1, 45900m, "Electrohogar", 12),
				("HELADO", "Helado", 1, 3000m, "Azucarados", 0)));

			Canasta canasta = historial.CanastaDe(2026, 5);
			Igual("no comestible aparte", "45900", canasta.GastoNoComestible.ToString());
			Igual("comestible solo el helado", "3000", canasta.GastoComestible.ToString());
			Igual("proporcion ocasional 100%", "1", canasta.ProporcionOcasional.ToString());
		}

		private static void LaComparacionCuentaElGuion()
		{
			Titulo("la comparacion cuenta el guion");

			Historial historial = ElGuion();
			ComparacionDeCanastas comparacion = historial.CompararCanastas(2025, 8, 2026, 8);

			Igual("hay comparacion", "True", comparacion.HayComparacion.ToString());
			// Los numeros del guion: agosto 2025 = 2890 ocasional de 32910 comestible;
			// agosto 2026 = 31680 de 32600.
			Igual("proporcion antes ~9%", "0.09",
				Math.Round(comparacion.ProporcionOcasionalAntes, 2).ToString(CultureInfo.InvariantCulture));
			Igual("proporcion despues ~97%", "0.97",
				Math.Round(comparacion.ProporcionOcasionalDespues, 2).ToString(CultureInfo.InvariantCulture));
			Igual("lo ocasional CRECIO", "True", comparacion.CrecioLoOcasional.ToString());
			Igual("el cambio tiene signo positivo", "True",
				(comparacion.CambioEnProporcion > 0m).ToString());
		}

		private static void CompararContraUnMesVacioEsLegitimo()
		{
			Titulo("comparar contra un mes vacio es legitimo");

			Historial historial = ElGuion();
			ComparacionDeCanastas sinDatos = historial.CompararCanastas(2024, 1, 2026, 8);

			Igual("no hay comparacion", "False", sinDatos.HayComparacion.ToString());
			Igual("no inventa crecimiento", "False", sinDatos.CrecioLoOcasional.ToString());
			Igual("el cambio es 0", "0", sinDatos.CambioEnProporcion.ToString());
		}

		// ---- la garantia con su respaldo ----
		private static void LaGarantiaSeEncuentraConSuRespaldo()
		{
			Titulo("la garantia se encuentra con su respaldo");

			Historial historial = ElGuion();
			RegistroDeGarantia registro = historial.BuscarGarantia("olla", Hoy);

			Igual("encontrada", "True", registro.Encontrada.ToString());
			Igual("con su compra", "T-93872", registro.NumeroDeCompra);
			Igual("con su fecha", "05/10/2026", registro.FechaDeCompra.ToString("MM/dd/yyyy"));
			Igual("con su vencimiento", "05/10/2027", registro.VenceEl.ToString("MM/dd/yyyy"));
			Igual("vigente hoy", "True", registro.Vigente.ToString());

			// Y despues del vencimiento, el MISMO registro contesta que ya no.
			RegistroDeGarantia vencida = historial.BuscarGarantia("olla", new DateTime(2027, 6, 1));
			Igual("vencida en junio 2027", "False", vencida.Vigente.ToString());

			Igual("vigentes hoy: solo la olla", "1", historial.GarantiasVigentes(Hoy).Count.ToString());
			Igual("vigentes en 2028: ninguna", "0",
				historial.GarantiasVigentes(new DateTime(2028, 1, 1)).Count.ToString());
		}

		private static void BuscarLoQueNoExisteEsLegitimo()
		{
			Titulo("buscar lo que no existe es legitimo");

			Historial historial = ElGuion();
			// El helado se compro, pero no tiene garantia; la licuadora ni se compro.
			// Las dos respuestas son "no encontrada", no una excepcion.
			Igual("el helado no tiene garantia que encontrar", "False",
				historial.BuscarGarantia("helado", Hoy).Encontrada.ToString());
			Igual("la licuadora no esta", "False",
				historial.BuscarGarantia("licuadora", Hoy).Encontrada.ToString());
		}

		private static void GanaLaCompraMasReciente()
		{
			Titulo("si se recompro, gana la compra mas reciente");

			var historial = new Historial();
			historial.Agregar(UnaCompra("T-1", new DateTime(2025, 1, 10),
				("OLLA", "Olla arrocera", 1, 40000m, "Electrohogar", 12)));
			historial.Agregar(UnaCompra("T-2", new DateTime(2026, 5, 10),
				("OLLA", "Olla arrocera", 1, 45900m, "Electrohogar", 12)));

			RegistroDeGarantia registro = historial.BuscarGarantia("olla", Hoy);
			Igual("la cobertura que corre es la nueva", "T-2", registro.NumeroDeCompra);
			Igual("y esta vigente", "True", registro.Vigente.ToString());
		}

		// -------------------------------------------------------------- el guion

		// El mismo guion de Muestras, construido directo sobre el dominio: aca se
		// comprueban las reglas; alla, que el wire las alimente igual.
		private static Historial ElGuion()
		{
			var historial = new Historial();

			historial.Agregar(UnaCompra("T-88101", new DateTime(2025, 8, 5),
				("POLLO-KG", "Pechuga de pollo kg", 2, 4200m, "Proteinas", 0),
				("FRIJOL-900", "Frijoles negros 900g", 2, 1150m, "GranosYAbarrotes", 0),
				("LECHE-1L", "Leche semidescremada 1L", 6, 950m, "LacteosYHuevos", 0),
				("BROCOLI", "Brocoli unidad", 3, 890m, "FrutasYVerduras", 0)));

			historial.Agregar(UnaCompra("T-88240", new DateTime(2025, 8, 20),
				("ACEITE-GIR-1L", "Aceite de girasol 1L", 1, 2890m, "AceitesYGrasas", 0),
				("ARROZ-2KG", "Arroz grano entero 2kg", 1, 2450m, "GranosYAbarrotes", 0),
				("HUEVOS-30", "Huevos carton 30", 1, 3300m, "LacteosYHuevos", 0),
				("TILAPIA-KG", "Filet de tilapia kg", 1, 5200m, "Proteinas", 0)));

			historial.Agregar(UnaCompra("T-91515", new DateTime(2026, 2, 14),
				("HELADO-1L", "Helado de vainilla 1L", 2, 3450m, "Azucarados", 0),
				("SODA-2L", "Soda cola 2L", 4, 1750m, "Azucarados", 0),
				("GALLETA-CHOC", "Galletas de chocolate", 3, 1250m, "Azucarados", 0),
				("POLLO-KG", "Pechuga de pollo kg", 1, 4350m, "Proteinas", 0)));

			historial.Agregar(UnaCompra("T-93872", new DateTime(2026, 5, 10),
				("OLLA-ARR-18", "Olla arrocera 1.8L", 1, 45900m, "Electrohogar", 12),
				("ARROZ-2KG", "Arroz grano entero 2kg", 2, 2520m, "GranosYAbarrotes", 0),
				("MANTECA-500", "Manteca de palma 500g", 1, 1980m, "AceitesYGrasas", 0)));

			historial.Agregar(UnaCompra("T-95104", new DateTime(2026, 7, 3),
				("SODA-12PK", "Soda cola lata 12-pack", 2, 5990m, "Azucarados", 0),
				("HELADO-1L", "Helado de vainilla 1L", 2, 3560m, "Azucarados", 0),
				("PAPAS-FAM", "Papas fritas familiar", 3, 2100m, "Snacks", 0),
				("DETERG-5L", "Detergente liquido 5L", 1, 6800m, "Limpieza", 0)));

			historial.Agregar(UnaCompra("T-96470", new DateTime(2026, 8, 15),
				("HELADO-1L", "Helado de vainilla 1L", 3, 3680m, "Azucarados", 0),
				("SODA-2L", "Soda cola 2L", 6, 1790m, "Azucarados", 0),
				("CHOC-BARRA", "Chocolate de leche barra", 4, 1450m, "Azucarados", 0),
				("MANTECA-500", "Manteca de palma 500g", 2, 2050m, "AceitesYGrasas", 0),
				("BROCOLI", "Brocoli unidad", 1, 920m, "FrutasYVerduras", 0)));

			return historial;
		}

		private static Compra UnaCompra(string numero, DateTime fecha,
			params (string Sku, string Descripcion, int Cantidad, decimal Precio, string Categoria, int Meses)[] lineas)
		{
			var skus = new string[lineas.Length];
			var descripciones = new string[lineas.Length];
			var cantidades = new int[lineas.Length];
			var precios = new decimal[lineas.Length];
			var categorias = new string[lineas.Length];
			var meses = new int[lineas.Length];

			for (int i = 0; i < lineas.Length; i++)
			{
				skus[i] = lineas[i].Sku;
				descripciones[i] = lineas[i].Descripcion;
				cantidades[i] = lineas[i].Cantidad;
				precios[i] = lineas[i].Precio;
				categorias[i] = lineas[i].Categoria;
				meses[i] = lineas[i].Meses;
			}

			return new Compra(numero, fecha, skus, descripciones, cantidades, precios, categorias, meses);
		}

		// -------------------------------------------------------------- el arnes

		private static void Titulo(string titulo)
		{
			Console.WriteLine();
			Console.WriteLine($"-- {titulo} --");
		}

		private static void Igual(string caso, string esperado, string obtenido)
		{
			corridas++;
			bool ok = esperado == obtenido;
			if (!ok) fallas++;
			Console.WriteLine($"  {(ok ? "ok " : "MAL")} {caso}{(ok ? "" : $": esperado [{esperado}], obtenido [{obtenido}]")}");
		}

		private static void Revienta(string caso, Func<object> accion)
		{
			corridas++;
			try
			{
				accion();
				fallas++;
				Console.WriteLine($"  MAL {caso}: no revento y debia");
			}
			catch (Exception)
			{
				Console.WriteLine($"  ok  {caso}");
			}
		}

		private static void Revienta(string caso, Action accion) =>
			Revienta(caso, () => { accion(); return null; });
	}
}
