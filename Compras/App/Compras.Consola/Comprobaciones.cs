using System;
using System.Collections.Generic;
using System.Linq;
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
			LaCategoriaLlevaSuTitulo();
			ElRepertorioNoValoraNada();
			LaGarantiaCorreDesdeLaCompra();
			NoTodoTieneGarantia();
			ElHistorialRechazaElTicketRepetido();
			LaCanastaSumaPorDepartamento();
			LoComestibleVaSeparadoDeLoQueNoLoEs();
			LaComparacionCuentaElGuion();
			CompararContraUnMesVacioEsLegitimo();
			LaGarantiaSeEncuentraConSuRespaldo();
			BuscarLoQueNoExisteEsLegitimo();
			GanaLaCompraMasReciente();
			LaComparacionDiceDeDondeSaleLaDiferencia();
			LaVentanaDeLaGarantiaAvisaCuandoSeCierra();
			ElHuecoEsLoQueNoEsta();
			LosAntojosNoTienenPeriodicidad();
			ElRangoDiceQueMesesSePuedenPedir();
			LaComparacionPorDefectoLaEligeElDominio();
			LaSerieDiceDesdeCuando();
			ElConsumoDiceCuantoSeMovio();
			LaCantidadDiceDeQue();
			ElPrecioPorUnidadPermiteComparar();
			LosJournalsViejosSiguenReproduciendo();
			LaCategoriaEsDelProductoNoDeLaCompra();
			ElPrecioSeLeePorUnidadDeMedida();
			ElRitmoEsElDeElla();
			LaFirmaEsLoQueNoFalla();
			ElCambioLePoneFechaAlQuiebre();
			LaCadenciaSeDiceComoHablaLaGente();
			ElRitmoDeUnProductoCambia();
			ElRitmoDelSuperTieneTresCaras();
			LaConstanciaSeDiceEnPalabras();
			LaSerieSeAgrupaPorAnio();
			LaBusquedaEncuentraPorProducto();

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

			// El nombre del ejemplo cambio, y vale contar por que: era "Mascotas", y el dia
			// que mascotas paso a ser un departamento de verdad estas tres comprobaciones se
			// pusieron rojas. Hicieron su trabajo. "Ferreteria" es un pasillo que existe en
			// supermercados reales y que este modulo NO modela, asi que sirve de ejemplo sin
			// quedar a merced del proximo departamento que agreguemos.
			Revienta("categoria desconocida", () => new Compra("T-1", Hoy,
				new[] { "A" }, new[] { "a" }, new[] { 1 },
				new[] { 1m }, new[] { "Ferreteria" }, new[] { 0 }));

			Compra valida = UnaCompra("T-1", Hoy, ("A", "a", 2, 3m, "Proteinas", 0));
			Igual("el total se deriva de las lineas", "6", valida.Total.ToString());
		}

		// ---- el repertorio: la llave viaja, el titulo se lee ----
		private static void LaCategoriaLlevaSuTitulo()
		{
			Titulo("la categoria lleva su titulo");

			// El nombre es la LLAVE que viene en el evento y ya esta escrita en los
			// diarios; el titulo es lo que lee una persona. Cambiar la llave rompe lo ya
			// escrito, asi que se fija aca.
			Igual("la llave no cambia", "FrutasYVerduras", Categoria.FrutasYVerduras.Nombre);
			Igual("el titulo se lee", "frutas y verduras", Categoria.FrutasYVerduras.Titulo);
			Igual("y la mencion entra en una frase", "una fruta o verdura",
				Categoria.FrutasYVerduras.Mencion);
			Igual("azucarados es un pasillo, no una etiqueta", "dulces y postres",
				Categoria.Azucarados.Titulo);

			// El resolver de la frontera: mismo string -> LA instancia (referencia).
			Igual("Desde resuelve a la instancia", "True",
				ReferenceEquals(Categoria.Desde("azucarados"), Categoria.Azucarados).ToString());
			Revienta("Desde con nombre desconocido", () => Categoria.Desde("Ferreteria"));
			Igual("Existe es la pareja total", "False", Categoria.Existe("Ferreteria").ToString());
		}

		private static void ElRepertorioNoValoraNada()
		{
			Titulo("el repertorio no valora nada");

			// La spec de marca: "no inventar en la app conclusiones de salud". Un
			// departamento dice DONDE esta la gondola, nunca si conviene. Si alguien
			// vuelve a colgarle una etiqueta de valor al repertorio, esto lo caza.
			string[] valoraciones = { "recomend", "ocasional", "saludable", "conviene", "malo", "bueno" };

			foreach (Categoria categoria in Categoria.Todas)
			{
				string dice = (categoria.Titulo + " " + categoria.Mencion).ToLowerInvariant();
				foreach (string valoracion in valoraciones)
					Igual($"{categoria.Nombre} no dice '{valoracion}'", "False",
						dice.Contains(valoracion).ToString());
			}

			Igual("lo comestible es un hecho de gondola", "True", Categoria.Proteinas.EsComestible.ToString());
			Igual("y el detergente no lo es", "False", Categoria.Limpieza.EsComestible.ToString());
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
		private static void LaCanastaSumaPorDepartamento()
		{
			Titulo("la canasta suma por departamento");

			var historial = new Historial();
			historial.Agregar(UnaCompra("T-1", new DateTime(2026, 8, 3),
				("POLLO", "Pollo", 2, 4000m, "Proteinas", 0),          // 8000
				("ARROZ", "Arroz", 1, 2000m, "GranosYAbarrotes", 0),   // 2000
				("HELADO", "Helado", 3, 3000m, "Azucarados", 0)));     // 9000
			historial.Agregar(UnaCompra("T-2", new DateTime(2026, 8, 20),
				("SODA", "Soda", 1, 1000m, "Azucarados", 0)));         // 1000

			Canasta canasta = historial.CanastaDe(2026, 8);
			Igual("compras del mes", "2", canasta.CantidadDeCompras.ToString());
			Igual("proteinas", "8000", canasta.GastoDe(Categoria.Proteinas).ToString());
			Igual("granos", "2000", canasta.GastoDe(Categoria.GranosYAbarrotes).ToString());
			Igual("dulces suma las dos compras", "10000", canasta.GastoDe(Categoria.Azucarados).ToString());
			Igual("comestible", "20000", canasta.GastoComestible.ToString());

			// Total: un pasillo que no entro es cero, no una ausencia que revienta.
			Igual("un pasillo que no entro es cero", "0", canasta.GastoDe(Categoria.Limpieza).ToString());
			Igual("  y se puede preguntar sin reventar", "False",
				canasta.Tiene(Categoria.Limpieza).ToString());

			// Ordenados del que mas pesa al que menos: es lo que hace legible la portada.
			Igual("tres pasillos", "3", canasta.Departamentos.Count.ToString());
			Igual("el que mas pesa es dulces", "dulces y postres", canasta.Departamentos[0].Titulo);
			Igual("  y es la mitad del mes", "0.5",
				canasta.Departamentos[0].ProporcionDelTotal.ToString(CultureInfo.InvariantCulture));

			// Un mes sin compras es una pregunta legitima, no una excepcion.
			Canasta vacia = historial.CanastaDe(2026, 1);
			Igual("mes vacio: sin compras", "False", vacia.HayCompras.ToString());
			Igual("mes vacio: sin pasillos", "0", vacia.Departamentos.Count.ToString());
		}

		private static void LoComestibleVaSeparadoDeLoQueNoLoEs()
		{
			Titulo("lo comestible va separado de lo que no");

			var historial = new Historial();
			// La olla arrocera vale 45900 y el helado 3000: mezclarlos haria que un
			// electrodomestico se coma el mes entero en cualquier lectura de comida.
			historial.Agregar(UnaCompra("T-1", new DateTime(2026, 5, 10),
				("OLLA", "Olla arrocera", 1, 45900m, "Electrohogar", 12),
				("HELADO", "Helado", 1, 3000m, "Azucarados", 0)));

			Canasta canasta = historial.CanastaDe(2026, 5);
			Igual("no comestible aparte", "45900", canasta.GastoNoComestible.ToString());
			Igual("comestible solo el helado", "3000", canasta.GastoComestible.ToString());
			Igual("y el total los suma", "48900", canasta.GastoTotal.ToString());
		}

		private static void LaComparacionCuentaElGuion()
		{
			Titulo("la comparacion cuenta el guion");

			Historial historial = ElGuion();
			ComparacionDeCanastas comparacion = historial.CompararCanastas(2025, 8, 2026, 8);

			Igual("hay comparacion", "True", comparacion.HayComparacion.ToString());
			Igual("el total de agosto 2025", "32910", comparacion.GastoAntes.ToString());
			Igual("el total de agosto 2026", "32600", comparacion.GastoDespues.ToString());
			Igual("el cambio tiene signo", "True", (comparacion.CambioEnElTotal < 0m).ToString());

			// Lo que ningun ticket contesta: de DONDE sale la diferencia.
			Igual("hay pasillos que la explican", "True", comparacion.HayAlgoQueLoExplique.ToString());
			Igual("el que mas movio es dulces", "dulces y postres", comparacion.Diferencias[0].Titulo);
			Igual("  y subio", "True", comparacion.Diferencias[0].Subio.ToString());
		}

		private static void CompararContraUnMesVacioEsLegitimo()
		{
			Titulo("comparar contra un mes vacio es legitimo");

			Historial historial = ElGuion();
			ComparacionDeCanastas sinDatos = historial.CompararCanastas(2024, 1, 2026, 8);

			Igual("no hay comparacion", "False", sinDatos.HayComparacion.ToString());
			Igual("el cambio es 0", "0", sinDatos.CambioEnElTotal.ToString());
			// Sin dos meses no hay diferencias que repartir: la lista viene vacia en vez de
			// mostrar el mes de despues entero como si todo fuera nuevo.
			Igual("no inventa pasillos", "0", sinDatos.Diferencias.Count.ToString());
			Igual("y no hay nada que lo explique", "False", sinDatos.HayAlgoQueLoExplique.ToString());
			Igual("lo dice sin rodeos", "True",
				sinDatos.ParaSonia.Contains("todavía no hay dos meses").ToString());
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

		// ---- la comparacion dice de DONDE sale la diferencia, y no si esta bien ----
		private static void LaComparacionDiceDeDondeSaleLaDiferencia()
		{
			Titulo("la comparacion dice de donde sale la diferencia");

			// La pregunta S14 de la wiki: "en que se me va la plata en el super". Un total
			// contra otro total lo suma cualquiera; de donde salio la diferencia, no.
			var historial = new Historial();
			historial.Agregar(UnaCompra("T-JUL", new DateTime(2026, 7, 10),
				("POLLO", "Pollo", 10, 1000m, "Proteinas", 0),
				("ARROZ", "Arroz", 1, 2000m, "GranosYAbarrotes", 0)));
			historial.Agregar(UnaCompra("T-AGO", new DateTime(2026, 8, 10),
				("POLLO", "Pollo", 5, 1000m, "Proteinas", 0),
				("ARROZ", "Arroz", 1, 2000m, "GranosYAbarrotes", 0),
				("HELADO", "Helado", 8, 2500m, "Azucarados", 0)));

			ComparacionDeCanastas comparacion = historial.CompararCanastas(2026, 7, 2026, 8);

			Igual("el total subio", "True", (comparacion.CambioEnElTotal > 0m).ToString());
			// Dos pasillos movieron: el que ENTRO (20000) y proteinas, que bajo 5000.
			// Granos no se movio y por eso no figura entre los que explican.
			Igual("dos pasillos explican el cambio", "2", comparacion.LasQueExplican.Count.ToString());
			Igual("el que mas movio entro este mes", "True", comparacion.LasQueExplican[0].EsNuevo.ToString());
			Igual("  y no aparecia antes", "0", comparacion.LasQueExplican[0].MontoAntes.ToString());
			Igual("el otro bajo", "False", comparacion.LasQueExplican[1].Subio.ToString());
			Igual("granos no movio nada", "0",
				(comparacion.Diferencias[2].Cambio).ToString());
			Igual("  asi que no explica nada", "False",
				comparacion.Diferencias[2].CuentaEnElMovimiento.ToString());

			string linea = comparacion.ParaSonia;
			Igual("la linea da los dos totales", "True", linea.Contains("₡").ToString());
			Igual("  nombra el pasillo que entro", "True", linea.Contains("dulces y postres").ToString());
			Igual("  y dice que el otro bajo", "True", linea.Contains("bajó").ToString());
			// Ni la llave tecnica ni jerga de retail en lo que lee una persona.
			Igual("  sin la llave del repertorio", "False", linea.Contains("Azucarados").ToString());

			// LO QUE NO DICE, y es el punto: la spec de marca pide que la app senale y se
			// calle -"el juicio no es suyo"-. Que entren dulces y bajen proteinas es un
			// dato; si eso esta bien o mal lo cruza el asistente, no el supermercado.
			string[] juicios = { "peor", "mejor", "empeora", "mejora", "debería", "conviene", "cuidado", "mal" };
			foreach (string juicio in juicios)
				Igual($"  la linea no dice '{juicio}'", "False",
					linea.ToLowerInvariant().Contains(juicio).ToString());
		}

		// ---- la ventana de la garantia, y que hacer con ella ----
		private static void LaVentanaDeLaGarantiaAvisaCuandoSeCierra()
		{
			Titulo("la ventana de la garantia avisa cuando se cierra");

			// Una garantia de 12 meses comprada hace 11 meses y 10 dias: quedan 20.
			var historial = new Historial();
			historial.Agregar(UnaCompra("T-A", Hoy.AddMonths(-12).AddDays(10),
				("LICUA", "Licuadora", 1, 28900m, "Electrohogar", 12)));
			// Otra de 24 meses comprada hace 22 meses: quedan ~60.
			historial.Agregar(UnaCompra("T-B", Hoy.AddMonths(-22),
				("MICRO", "Microondas", 1, 62000m, "Electrohogar", 24)));
			// Y una recien comprada: la ventana no se esta cerrando.
			historial.Agregar(UnaCompra("T-C", Hoy.AddDays(-5),
				("BATID", "Batidora", 1, 18000m, "Electrohogar", 12)));

			Igual("a 20 dias: antes de que se cierre", "antes de que se cierre",
				historial.BuscarGarantia("licuadora", Hoy).Severidad.Nombre);
			Igual("  y dice que hacer", "hacé el reclamo antes de que venza",
				historial.BuscarGarantia("licuadora", Hoy).QueHacer);
			Igual("a ~60 dias: conviene revisar", "conviene revisar",
				historial.BuscarGarantia("microondas", Hoy).Severidad.Nombre);
			Igual("recien comprada: informativo", "informativo",
				historial.BuscarGarantia("batidora", Hoy).Severidad.Nombre);
			Igual("  y no pide nada", "False",
				historial.BuscarGarantia("batidora", Hoy).Severidad.PideAlgo.ToString());

			// La lista del hueco trae SOLO las que piden algo, de la mas urgente primero.
			var porVencer = historial.GarantiasPorVencer(Hoy);
			Igual("por vencer: 2 de 3", "2", porVencer.Count.ToString());
			Igual("  la mas urgente primero", "Licuadora", porVencer[0].Descripcion);
			Igual("  vigentes en total: 3", "3", historial.GarantiasVigentes(Hoy).Count.ToString());

			// Una vencida no esta ni vigente ni por vencer: ya no hay ventana.
			Igual("vencida: no aparece por vencer", "0",
				historial.GarantiasPorVencer(Hoy.AddYears(3)).Count.ToString());
		}

		// ---- lo que NO esta ----
		private static void ElHuecoEsLoQueNoEsta()
		{
			Titulo("el hueco es lo que no esta");

			var pendientes = ElGuion().AbastecimientosPendientes(Hoy);

			// Con el guion al 25/08/2026 quedan TRES: lacteos (370 dias contra un ritmo
			// medido de 15), proteinas (192 contra 97) y limpieza (53 contra los 45 del
			// catalogo, porque una sola compra no da ritmo).
			//
			// LOS GRANOS SALIERON, y es la fase del ritmo haciendo su trabajo: llevan 107
			// dias sin entrar, pero ELLA los compra cada 139 -tiene dos compras separadas
			// por 15 dias y otra 263 dias despues-. Con la periodicidad del catalogo (35)
			// esto figuraba como hueco; con su propio ritmo no lo es, y eso es lo honesto.
			Igual("tres categorias atrasadas", "3", pendientes.Count.ToString());
			Igual("la mas atrasada primero", "LacteosYHuevos", pendientes[0].NombreDeLaCategoria);
			Igual("los granos ya no figuran", "139",
				ElGuion().RitmoDe(Categoria.GranosYAbarrotes).DiasTipicos.ToString());
			Igual("  con su severidad", "conviene revisar", pendientes[0].Severidad.Nombre);
			Igual("  y su linea del dominio", "True", pendientes[0].ParaSonia.Contains("lácteo").ToString());

			bool frutasAlDia = true;
            foreach (AbastecimientoPendiente p in pendientes)
				if (p.NombreDeLaCategoria == "FrutasYVerduras") frutasAlDia = false;
			Igual("lo que entro a tiempo NO aparece", "True", frutasAlDia.ToString());

			// De algo que nunca se compro no se puede decir que falta.
			var reciente = new Historial();
			reciente.Agregar(UnaCompra("T-1", Hoy, ("POLLO", "Pollo", 1, 4000m, "Proteinas", 0)));
			Igual("historial al dia: nada pendiente", "0",
				reciente.AbastecimientosPendientes(Hoy).Count.ToString());
			Igual("una categoria que nunca entro no falta", "0",
				reciente.AbastecimientosPendientes(Hoy.AddDays(5)).Count.ToString());

			// Y al pasar el tiempo, lo mismo SI aparece.
			Igual("a los 40 dias ya falta la proteina", "1",
				reciente.AbastecimientosPendientes(Hoy.AddDays(40)).Count.ToString());
		}

		private static void LosAntojosNoTienenPeriodicidad()
		{
			Titulo("los antojos no tienen periodicidad");

			// Decision de dominio: este modulo no le dice a nadie que hace un mes que no
			// compra helado. Preguntar la periodicidad de un antojo revienta.
			Igual("azucarados no se reabastece", "False", Categoria.Azucarados.SeReabastece.ToString());
			Igual("snacks tampoco", "False", Categoria.Snacks.SeReabastece.ToString());
			Igual("aceites tampoco", "False", Categoria.AceitesYGrasas.SeReabastece.ToString());
			Igual("electrohogar no se repone", "False", Categoria.Electrohogar.SeReabastece.ToString());
			Igual("las proteinas si", "True", Categoria.Proteinas.SeReabastece.ToString());
			Igual("  cada 10 dias", "10", Categoria.Proteinas.DiasDeReabastecimiento.ToString());
			Igual("limpieza si, aunque no sea comestible", "True", Categoria.Limpieza.SeReabastece.ToString());
			Revienta("la periodicidad de los azucarados revienta",
				() => Categoria.Azucarados.DiasDeReabastecimiento);

			// Un helado sin comprar hace un ano no genera pendiente.
			var soloHelado = new Historial();
			soloHelado.Agregar(UnaCompra("T-1", Hoy.AddYears(-1), ("HELADO", "Helado", 1, 3000m, "Azucarados", 0)));
			Igual("un ano sin helado no es un pendiente", "0",
				soloHelado.AbastecimientosPendientes(Hoy).Count.ToString());
			Revienta("y no se puede construir el pendiente a mano",
				() => new AbastecimientoPendiente(Categoria.Azucarados, Hoy.AddYears(-1), 365,
					soloHelado.RitmoDe(Categoria.Azucarados)));
		}

		// ---- "3" no es un dato: la cantidad dice de que ----
		private static void LaCantidadDiceDeQue()
		{
			Titulo("la cantidad dice de que");

			// Seis leches de un litro son SEIS LITROS, no seis.
			var leche = new Item("LECHE-1L", "Leche 1L", 6, 950m, Categoria.LacteosYHuevos, 0,
				Presentacion.Desde("1 L"));
			Igual("seis leches de 1 L", "6 L", leche.CantidadExpresada);
			Igual("  en unidad base", "6", leche.TotalEnUnidadBase.ToString(CultureInfo.InvariantCulture));

			// Dos mantecas de 500 g son UN KILO: la unidad se convierte a su base.
			var manteca = new Item("MANTECA-500", "Manteca 500g", 2, 2050m, Categoria.AceitesYGrasas, 0,
				Presentacion.Desde("500 g"));
			Igual("dos mantecas de 500 g", "1 kg", manteca.CantidadExpresada);

			// Dos 12-packs son VEINTICUATRO latas, no dos.
			var soda = new Item("SODA-12PK", "Soda 12-pack", 2, 5990m, Categoria.Azucarados, 0,
				Presentacion.Desde("12x u"));
			Igual("dos paquetes de 12", "24 u", soda.CantidadExpresada);
			Igual("  y se dice como se vende", "12 u por paquete", soda.Presentacion.Expresada);

			// Lo que se cuenta y nada mas.
			var brocoli = new Item("BROCOLI", "Brocoli", 3, 920m, Categoria.FrutasYVerduras, 0,
				Presentacion.PorUnidad);
			Igual("tres brocolis", "3 u", brocoli.CantidadExpresada);

			// Las familias no se mezclan: un litro no es mas ni menos que un kilo.
			Igual("litro y kilo no son comparables", "False",
				UnidadDeVenta.Litro.EsComparableCon(UnidadDeVenta.Kilogramo).ToString());
			Igual("gramo y kilo si", "True",
				UnidadDeVenta.Gramo.EsComparableCon(UnidadDeVenta.Kilogramo).ToString());
			// Con formato explicito: la escala del decimal (0.500) no sobrevive el journal,
			// asi que compararla como texto pelado seria fragil.
			Igual("500 g a la base", "0.5",
				UnidadDeVenta.Gramo.ALaBase(500m).ToString("0.###", CultureInfo.InvariantCulture));

			// El resolver de la frontera: total-con-throw.
			Revienta("una presentacion inventada revienta", () => Presentacion.Desde("2 barriles"));
			Revienta("una unidad inventada revienta", () => UnidadDeVenta.PorSimbolo("kilo"));
			Igual("y se puede preguntar antes", "False", Presentacion.Existe("2 barriles").ToString());
			Igual("  lo valido se reconoce", "True", Presentacion.Existe("500 g").ToString());
			Revienta("un paquete de una pieza no es un paquete", () => Presentacion.DePaquete(1));
		}

		// ---- lo que la presentacion habilita: comparar precios ----
		private static void ElPrecioPorUnidadPermiteComparar()
		{
			Titulo("el precio por unidad permite comparar");

			// LA PREGUNTA QUE NO SE PODIA HACER: ¿cual leche sale mas barata? Sin la
			// presentacion las dos lineas son "1 x 1800" y "2 x 950", y el precio unitario
			// dice que la de 2 L es la CARA.
			var grande = new Item("LECHE-2L", "Leche 2L", 1, 1800m, Categoria.LacteosYHuevos, 0,
				Presentacion.Desde("2 L"));
			var chica = new Item("LECHE-1L", "Leche 1L", 2, 950m, Categoria.LacteosYHuevos, 0,
				Presentacion.Desde("1 L"));

			Igual("la de 2 L cuesta mas por linea", "True", (grande.PrecioUnitario > chica.PrecioUnitario).ToString());
			Igual("pero sale mas barata por litro", "900",
				grande.PrecioPorUnidadBase.ToString(CultureInfo.InvariantCulture));
			Igual("  contra la de 1 L", "950", chica.PrecioPorUnidadBase.ToString(CultureInfo.InvariantCulture));
			Igual("  y el dominio lo puede decir", "True",
				(grande.PrecioPorUnidadBase < chica.PrecioPorUnidadBase).ToString());

			// El paquete tambien: la lata suelta contra la del 12-pack.
			var pack = new Item("SODA-12PK", "Soda 12-pack", 1, 5990m, Categoria.Azucarados, 0,
				Presentacion.Desde("12x u"));
			Igual("la lata del pack sale a", "499.17",
				pack.PrecioPorUnidadBase.ToString("0.##", CultureInfo.InvariantCulture));
		}

		// ---- retrocompatibilidad: el journal es append-only ----
		private static void LosJournalsViejosSiguenReproduciendo()
		{
			Titulo("los journals viejos siguen reproduciendo");

			// La forma VIEJA -sin presentacion- se conserva a proposito: un historial
			// escrito antes de que la presentacion existiera se rehidrata igual, y sus
			// lineas se leen como piezas sueltas, que es lo que la app asumia entonces.
			var vieja = new Compra("T-VIEJA", Hoy,
				new[] { "LECHE-1L" }, new[] { "Leche 1L" }, new[] { 6 }, new[] { 950m },
				new[] { "LacteosYHuevos" }, new[] { 0 });
			Igual("la forma vieja sigue armando la compra", "5700", vieja.Total.ToString());
			Igual("  y sus lineas son piezas sueltas", "6 u", vieja.Items[0].CantidadExpresada);

			// La forma NUEVA agrega la presentacion sin cambiar nada mas.
			var nueva = new Compra("T-NUEVA", Hoy,
				new[] { "LECHE-1L" }, new[] { "Leche 1L" }, new[] { 6 }, new[] { 950m },
				new[] { "LacteosYHuevos" }, new[] { 0 }, new[] { "1 L" });
			Igual("la forma nueva dice los litros", "6 L", nueva.Items[0].CantidadExpresada);
			Igual("  y el total es el mismo", "True", (nueva.Total == vieja.Total).ToString());

			// Y valida que las presentaciones sean tantas como las lineas.
			Revienta("presentaciones de largo distinto", () => new Compra("T-1", Hoy,
				new[] { "A", "B" }, new[] { "a", "b" }, new[] { 1, 1 }, new[] { 1m, 1m },
				new[] { "Proteinas", "Proteinas" }, new[] { 0, 0 }, new[] { "u" }));
		}

		// ---- la categoria es del producto, no del ticket ----
		private static void LaCategoriaEsDelProductoNoDeLaCompra()
		{
			Titulo("la categoria es del producto, no del ticket");

			Historial historial = ElGuion();
			Igual("el catalogo se armo desde las compras", "True", (historial.Catalogo.Cantidad > 0).ToString());
			Igual("  y conoce el helado", "True", historial.Catalogo.Tiene("HELADO-1L").ToString());
			Igual("  con su categoria", "Azucarados", historial.Catalogo["HELADO-1L"].Categoria.Nombre);
			Igual("  aunque aparezca en tres tickets", "3",
				historial.ConsumoDe("helado").MesesConConsumo.ToString());

			// EL DEFECTO QUE ESTO ARREGLA: el mismo sku entrando con otra categoria
			// cambiaria el significado de las canastas ya leidas, sin que nada avise.
			var mezclado = new Historial();
			mezclado.Agregar(UnaCompra("T-1", new DateTime(2026, 1, 5),
				("HELADO-1L", "Helado", 1, 3000m, "Azucarados", 0)));
			Revienta("el mismo sku con otra categoria revienta", () =>
				mezclado.Agregar(UnaCompra("T-2", new DateTime(2026, 2, 5),
					("HELADO-1L", "Helado", 1, 3000m, "Proteinas", 0))));
			Igual("  y la compra NO entro a medias", "1", mezclado.Cantidad.ToString());
			Igual("  el catalogo quedo intacto", "Azucarados", mezclado.Catalogo["HELADO-1L"].Categoria.Nombre);

			// La DESCRIPCION si puede afinarse: es el mismo producto mejor nombrado.
			var renombrado = new Historial();
			renombrado.Agregar(UnaCompra("T-1", new DateTime(2026, 1, 5),
				("LECHE-1L", "Leche", 1, 950m, "LacteosYHuevos", 0)));
			renombrado.Agregar(UnaCompra("T-2", new DateTime(2026, 2, 5),
				("LECHE-1L", "Leche semidescremada 1L", 1, 980m, "LacteosYHuevos", 0)));
			Igual("la descripcion se afina", "Leche semidescremada 1L",
				renombrado.Catalogo["LECHE-1L"].Descripcion);
			Igual("  y sigue siendo un solo producto", "1", renombrado.Catalogo.Cantidad.ToString());

			// Y la pregunta que ningun ticket contesta: cuantos productos distintos de una
			// categoria compra esta persona.
			Igual("productos distintos de azucarados", "5",
				historial.Catalogo.CuantosDe(Categoria.Azucarados).ToString());
			Igual("  de electrohogar", "1", historial.Catalogo.CuantosDe(Categoria.Electrohogar).ToString());
			Igual("un sku que nunca compro no esta", "False", historial.Catalogo.Tiene("CAVIAR").ToString());
			Revienta("y preguntarlo revienta", () => historial.Catalogo["CAVIAR"]);
		}

		// ---- la inflacion propia, leida por unidad de medida ----
		private static void ElPrecioSeLeePorUnidadDeMedida()
		{
			Titulo("el precio se lee por unidad de medida");

			// El arroz del guion: 2.450 en agosto 2025 y 2.520 en mayo 2026. Misma
			// presentacion, asi que por linea y por unidad dicen lo mismo.
			PrecioEnElTiempo arroz = ElGuion().PrecioDe("ARROZ-2KG");
			Igual("existe", "True", arroz.Existe().ToString());
			Igual("  hay serie", "True", arroz.HaySerie.ToString());
			Igual("  dos veces comprado", "2", arroz.VecesQueLoCompro.ToString());
			Igual("  subio 2.9%", "0.0286",
				arroz.CambioEnProporcion.ToString(CultureInfo.InvariantCulture));
			// 2,9% es redondeo de gondola: no llega al umbral del 3%.
			Igual("  y eso no es un cambio real", "False", arroz.HayCambioReal.ToString());
			Revienta("  darle direccion revienta", () => arroz.Subio);

			// LA LECTURA QUE MENTIRIA SIN LA PRESENTACION: la persona pasa de dos leches de
			// 1 L a 950 a una de 2 L a 1.800. Por LINEA el precio "subio" 850; por LITRO
			// bajo 50. Sin unidad de medida, esta lectura miente en cada cambio de formato.
			var cambioDeFormato = new Historial();
			cambioDeFormato.Agregar(UnaCompraCon("T-1", new DateTime(2026, 1, 5),
				("LECHE-1L", "Leche 1L", 2, 950m, "LacteosYHuevos", 0, "1 L")));
			cambioDeFormato.Agregar(UnaCompraCon("T-2", new DateTime(2026, 2, 5),
				("LECHE-1L", "Leche 1L", 1, 1800m, "LacteosYHuevos", 0, "2 L")));

			PrecioEnElTiempo leche = cambioDeFormato.PrecioDe("LECHE-1L");
			Igual("el precio de linea subio", "True",
				(leche.Observados[1].PrecioDeLinea > leche.Observados[0].PrecioDeLinea).ToString());
			Igual("  pero el del litro BAJO", "True", (leche.Cambio < 0m).ToString());
			Igual("  y por litro no subio", "False", leche.Subio.ToString());
			Igual("  la linea lo dice como bajada", "True", leche.ParaSonia.Contains("bajó").ToString());
			Igual("  y la unidad de referencia es el litro", "L", leche.UnidadDeReferencia);

			// Un aumento de verdad: la manteca de 1.980 a 2.050 el medio kilo.
            PrecioEnElTiempo manteca = ElGuion().PrecioDe("MANTECA-500");
			Igual("la manteca subio", "True", manteca.Subio.ToString());
			Igual("  y lo dice con el porcentaje", "True", manteca.ParaSonia.Contains("subió").ToString());

			// Una sola compra no es una serie de precios, y se dice.
			PrecioEnElTiempo olla = ElGuion().PrecioDe("OLLA-ARR-18");
			Igual("una sola compra: no hay serie", "False", olla.HaySerie.ToString());
			Revienta("  y preguntar la direccion revienta", () => olla.Subio);
			Igual("  pero el precio pagado esta", "True", (olla.UltimoPrecioPorUnidad > 0m).ToString());

			// Total: un sku que nunca se compro.
			PrecioEnElTiempo nada = ElGuion().PrecioDe("CAVIAR");
			Igual("lo que nunca compro: no existe", "False", nada.Existe().ToString());
			Igual("  y lo dice", "True", nada.ParaSonia.Contains("no hay compras").ToString());

			// El orden lo pone la FECHA, no el orden en que entraron al journal: el
			// proveedor puede publicar un ticket viejo despues de uno nuevo.
			var desordenado = new Historial();
			desordenado.Agregar(UnaCompraCon("T-NUEVO", new DateTime(2026, 6, 1),
				("PAN", "Pan", 1, 1200m, "GranosYAbarrotes", 0, "u")));
			desordenado.Agregar(UnaCompraCon("T-VIEJO", new DateTime(2026, 1, 1),
				("PAN", "Pan", 1, 1000m, "GranosYAbarrotes", 0, "u")));
			Igual("ordena por fecha, no por llegada", "1000",
				desordenado.PrecioDe("PAN").PrimerPrecioPorUnidad.ToString(CultureInfo.InvariantCulture));
			Igual("  asi que el pan SUBIO", "True", desordenado.PrecioDe("PAN").Subio.ToString());
		}

		// ---- el ritmo es el de ella, no el que supusimos ----
		private static void ElRitmoEsElDeElla()
		{
			Titulo("el ritmo es el de ella");

			// Una familia que compra lacteos cada 15 dias. El catalogo supone 12.
			var quincenal = new Historial();
			for (int i = 0; i < 5; i++)
				quincenal.Agregar(UnaCompra($"T-{i}", new DateTime(2026, 1, 1).AddDays(i * 15),
					("LECHE-1L", "Leche", 2, 950m, "LacteosYHuevos", 0)));

			RitmoDeCompra ritmo = quincenal.RitmoDe(Categoria.LacteosYHuevos);
			Igual("el ritmo se midio", "True", ritmo.EsObservado.ToString());
			Igual("  cuatro intervalos", "4", ritmo.IntervalosMedidos.ToString());
			Igual("  cada 15 dias", "15", ritmo.DiasTipicos.ToString());
			Igual("  y con eso se lee el hueco", "15", ritmo.DiasParaLeerElHueco.ToString());
			Igual("  no con los 12 del catalogo", "12", Categoria.LacteosYHuevos.DiasDeReabastecimiento.ToString());

			// EL DEFECTO QUE ESTO ARREGLA: a los 13 dias de la ultima compra, el catalogo
			// diria que hay hueco -pasaron 12- y para ELLA es martes.
			DateTime alDia = new DateTime(2026, 1, 1).AddDays(4 * 15 + 13);
			Igual("a los 13 dias NO hay hueco para ella", "0",
				quincenal.AbastecimientosPendientes(alDia).Count.ToString());

			// A los 20 si, porque paso su propio ritmo.
			DateTime pasado = new DateTime(2026, 1, 1).AddDays(4 * 15 + 20);
			var pendientes = quincenal.AbastecimientosPendientes(pasado);
			Igual("a los 20 dias si", "1", pendientes.Count.ToString());
			Igual("  y dice que el ritmo es suyo", "True", pendientes[0].EsRitmoObservado.ToString());
			Igual("  la linea lo dice en segunda persona", "True",
				pendientes[0].ParaSonia.Contains("vos lo comprás").ToString());

			// La MEDIANA y no el promedio: una compra de reserva no corre el ritmo tipico.
			var conReserva = new Historial();
			conReserva.Agregar(UnaCompra("T-1", new DateTime(2026, 1, 1), ("ARROZ", "Arroz", 1, 2500m, "GranosYAbarrotes", 0)));
			conReserva.Agregar(UnaCompra("T-2", new DateTime(2026, 2, 1), ("ARROZ", "Arroz", 1, 2500m, "GranosYAbarrotes", 0)));
			conReserva.Agregar(UnaCompra("T-3", new DateTime(2026, 2, 2), ("ARROZ", "Arroz", 5, 2500m, "GranosYAbarrotes", 0)));
			conReserva.Agregar(UnaCompra("T-4", new DateTime(2026, 3, 4), ("ARROZ", "Arroz", 1, 2500m, "GranosYAbarrotes", 0)));
			RitmoDeCompra deArroz = conReserva.RitmoDe(Categoria.GranosYAbarrotes);
			Igual("con una reserva de por medio", "3", deArroz.IntervalosMedidos.ToString());
			Igual("  el mas corto fue de un dia", "1", deArroz.DiasMasCorto.ToString());
			Igual("  y el tipico sigue siendo mensual", "30", deArroz.DiasTipicos.ToString());

			// Sin dos compras no hay ritmo que medir, y se cae al catalogo DICIENDOLO.
			var unaSola = new Historial();
			unaSola.Agregar(UnaCompra("T-1", Hoy, ("POLLO", "Pollo", 1, 4000m, "Proteinas", 0)));
			RitmoDeCompra sinRitmo = unaSola.RitmoDe(Categoria.Proteinas);
			Igual("una sola compra: no hay ritmo", "False", sinRitmo.EsObservado.ToString());
			Igual("  se cae al catalogo", "10", sinRitmo.DiasParaLeerElHueco.ToString());
			Igual("  y lo dice", "True", sinRitmo.ParaSonia.Contains("del catálogo").ToString());
			Revienta("preguntarle el tipico revienta", () => sinRitmo.DiasTipicos);

			// Dos lineas de la misma categoria en UN ticket son una compra, no dos.
			var mismoTicket = new Historial();
			mismoTicket.Agregar(UnaCompra("T-1", new DateTime(2026, 1, 1),
				("LECHE-1L", "Leche", 1, 950m, "LacteosYHuevos", 0),
				("HUEVOS-30", "Huevos", 1, 3300m, "LacteosYHuevos", 0)));
			Igual("dos lineas, un ticket: no hay ritmo", "False",
				mismoTicket.RitmoDe(Categoria.LacteosYHuevos).EsObservado.ToString());
		}

		// ---- la firma: lo que no falla ----
		private static void LaFirmaEsLoQueNoFalla()
		{
			Titulo("la firma es lo que no falla");

			// Cuatro compras: el cafe y la leche en todas, el pan en tres, el helado en una.
			var semanal = new Historial();
			for (int i = 0; i < 4; i++)
			{
				var lineas = new List<(string, string, int, decimal, string, int)>
				{
					("CAFE-500", "Café molido 500g", 1, 3200m, "GranosYAbarrotes", 0),
					("LECHE-1L", "Leche semidescremada 1L", 2, 950m, "LacteosYHuevos", 0),
				};
				if (i < 3) lineas.Add(("PAN-INT", "Pan integral", 1, 1450m, "GranosYAbarrotes", 0));
				if (i == 3) lineas.Add(("HELADO-1L", "Helado de vainilla 1L", 1, 3680m, "Azucarados", 0));

				semanal.Agregar(UnaCompra($"T-{i}", new DateTime(2026, 8, 1).AddDays(i * 7), lineas.ToArray()));
			}

			FirmaDeLaCanasta firma = semanal.FirmaDeLasUltimas(4);
			Igual("existe", "True", firma.Existe().ToString());
			Igual("  mira las cuatro compras", "4", firma.ComprasMiradas.ToString());
			// El helado esta en 1 de 4: no llega a la mitad y NO es su firma.
			Igual("  tres habituales, no cuatro", "3", firma.CuantosHabituales.ToString());
			Igual("  el que mas vuelve primero", "True",
				(firma.Habituales[0].EstaEnTodas).ToString());
			Igual("  y la linea los nombra", "True",
				firma.ParaSonia.Contains("su semana tiene una firma").ToString());
			Igual("  con el pan adentro", "True", firma.ParaSonia.Contains("Pan integral").ToString());
			Igual("  y el helado AFUERA", "False", firma.ParaSonia.Contains("Helado").ToString());

			// Sin compras no se inventa un patron.
			Igual("historial vacio: no hay firma", "False",
				new Historial().FirmaDeLasUltimas(4).Existe().ToString());
			Igual("  y lo dice", "True",
				new Historial().FirmaDeLasUltimas(4).ParaSonia.Contains("todavía no hay compras").ToString());

			// Compras sin nada en comun tampoco: la firma no es "lo que compro alguna vez".
			var dispares = new Historial();
			dispares.Agregar(UnaCompra("T-1", new DateTime(2026, 1, 1), ("A", "Cosa A", 1, 100m, "Snacks", 0)));
			dispares.Agregar(UnaCompra("T-2", new DateTime(2026, 1, 8), ("B", "Cosa B", 1, 100m, "Snacks", 0)));
			dispares.Agregar(UnaCompra("T-3", new DateTime(2026, 1, 15), ("C", "Cosa C", 1, 100m, "Snacks", 0)));
			dispares.Agregar(UnaCompra("T-4", new DateTime(2026, 1, 22), ("D", "Cosa D", 1, 100m, "Snacks", 0)));
			Igual("nada se repite: no hay firma", "False", dispares.FirmaDeLasUltimas(4).Existe().ToString());

			Revienta("mirar cero compras revienta", () => semanal.FirmaDeLasUltimas(0));
		}

		// ---- el cambio le pone FECHA al quiebre ----
		private static void ElCambioLePoneFechaAlQuiebre()
		{
			Titulo("el cambio le pone fecha al quiebre");

			// Antes: cafe, leche y fruta todas las semanas. Desde marzo entra el helado
			// todas las semanas y la fruta se cae. El quiebre tiene que decir MARZO.
			var conQuiebre = new Historial();
			for (int i = 0; i < 4; i++)
				conQuiebre.Agregar(UnaCompra($"A-{i}", new DateTime(2026, 1, 5).AddDays(i * 7),
					("CAFE-500", "Café molido 500g", 1, 3200m, "GranosYAbarrotes", 0),
					("LECHE-1L", "Leche 1L", 2, 950m, "LacteosYHuevos", 0),
					("BANANO-KG", "Banano kg", 1, 890m, "FrutasYVerduras", 0)));

			for (int i = 0; i < 4; i++)
				conQuiebre.Agregar(UnaCompra($"B-{i}", new DateTime(2026, 3, 2).AddDays(i * 7),
					("CAFE-500", "Café molido 500g", 1, 3200m, "GranosYAbarrotes", 0),
					("LECHE-1L", "Leche 1L", 2, 950m, "LacteosYHuevos", 0),
					("HELADO-1L", "Helado de vainilla 1L", 2, 3680m, "Azucarados", 0)));

			CambioEnLaCanasta cambio = conQuiebre.CambioDesde(new DateTime(2026, 3, 1), 4);
			Igual("hay cambio", "True", cambio.HayCambio.ToString());
			Igual("  entro uno", "1", cambio.Entraron.Count.ToString());
			Igual("  y es el helado", "Helado de vainilla 1L", cambio.Entraron[0].Descripcion);
			Igual("  desde marzo", "2026-03", cambio.MesDelCambioExpresado);
			Igual("  se cayo uno", "1", cambio.Salieron.Count.ToString());
			Igual("  y es el banano", "Banano kg", cambio.Salieron[0].Descripcion);
			Igual("  siguen dos", "2", cambio.Siguen.Count.ToString());
			Igual("  la linea dice cuando y que", "True",
				(cambio.ParaSonia.Contains("2026-03") && cambio.ParaSonia.Contains("Helado")).ToString());

			// Y NO JUZGA: la especificacion de marca es explicita -"la app lo senala y se
			// calla; el juicio no es suyo"-. Nada de mejor, peor, deberia ni conviene.
			foreach (string juicio in new[] { "peor", "mejor", "debería", "conviene", "cuidado", "mal" })
				Igual($"  la linea no dice '{juicio}'", "False",
					cambio.ParaSonia.Contains(juicio).ToString());

			// Sin quiebre lo dice sin inventar uno.
			var estable = new Historial();
			for (int i = 0; i < 6; i++)
				estable.Agregar(UnaCompra($"T-{i}", new DateTime(2026, 1, 5).AddDays(i * 7),
					("CAFE-500", "Café molido 500g", 1, 3200m, "GranosYAbarrotes", 0),
					("LECHE-1L", "Leche 1L", 2, 950m, "LacteosYHuevos", 0)));

			CambioEnLaCanasta sinCambio = estable.CambioDesde(new DateTime(2026, 1, 26), 3);
			Igual("canasta igual: hay con que comparar", "True", sinCambio.HayConQueComparar.ToString());
			Igual("  y no hay cambio", "False", sinCambio.HayCambio.ToString());
			Igual("  y lo dice", "True", sinCambio.ParaSonia.Contains("viene igual").ToString());
			Revienta("y no hay mes de cambio que dar", () => sinCambio.MesDelCambioExpresado);

			// Y si de un lado no hay compras, eso NO es que la canasta cambio: es que no
			// hay con que comparar. Lo destapo esta comprobacion.
			CambioEnLaCanasta sinLado = estable.CambioDesde(new DateTime(2026, 6, 1), 3);
			Igual("sin compras despues: no hay con que comparar", "False", sinLado.HayConQueComparar.ToString());
			Igual("  y NO se afirma un cambio", "False", sinLado.HayCambio.ToString());
			Igual("  la linea lo dice", "True",
				sinLado.ParaSonia.Contains("los dos lados").ToString());
		}

		// Una compra con presentacion por linea (la forma nueva).
		private static Compra UnaCompraCon(string numero, DateTime fecha,
			params (string Sku, string Descripcion, int Cantidad, decimal Precio, string Categoria, int Meses, string Presentacion)[] lineas)
		{
			var skus = new string[lineas.Length];
			var descripciones = new string[lineas.Length];
			var cantidades = new int[lineas.Length];
			var precios = new decimal[lineas.Length];
			var categorias = new string[lineas.Length];
			var meses = new int[lineas.Length];
			var presentaciones = new string[lineas.Length];

			for (int i = 0; i < lineas.Length; i++)
			{
				skus[i] = lineas[i].Sku;
				descripciones[i] = lineas[i].Descripcion;
				cantidades[i] = lineas[i].Cantidad;
				precios[i] = lineas[i].Precio;
				categorias[i] = lineas[i].Categoria;
				meses[i] = lineas[i].Meses;
				presentaciones[i] = lineas[i].Presentacion;
			}

			return new Compra(numero, fecha, skus, descripciones, cantidades, precios, categorias,
				meses, presentaciones);
		}

		// ---- el rango: el contexto para elegir que pedir ----
		private static void ElRangoDiceQueMesesSePuedenPedir()
		{
			Titulo("el rango dice que meses se pueden pedir");

			// Sin compras la ausencia tiene tipo, y preguntarle los meses revienta: es lo
			// que evita que quien pregunta lea un 0 como si fuera enero del ano cero.
			var vacio = new Historial();
			Igual("historial vacio: no hay rango", "False", vacio.Rango.Existe().ToString());
			Igual("  y no cuenta meses", "0", vacio.Rango.MesesConCompras.ToString());
			Igual("  ni expresa un mes", "", vacio.Rango.PrimerMesExpresado);
			Revienta("preguntarle el anio revienta", () => vacio.Rango.PrimerAnio);

			RangoDeCompras rango = ElGuion().Rango;
			Igual("del guion: existe", "True", rango.Existe().ToString());
			Igual("  primer mes", "2025-08", rango.PrimerMesExpresado);
			Igual("  ultimo mes", "2026-08", rango.UltimoMesExpresado);
			// Dos compras caen en agosto 2025: el mes se cuenta UNA vez.
			Igual("  meses distintos con compras", "5", rango.MesesConCompras.ToString());

			var unaSola = new Historial();
			unaSola.Agregar(UnaCompra("T-1", Hoy, ("POLLO", "Pollo", 1, 4000m, "Proteinas", 0)));
			Igual("con una compra: un mes", "1", unaSola.Rango.MesesConCompras.ToString());
			Igual("  y el rango empieza y termina igual", "True",
				(unaSola.Rango.PrimerMesExpresado == unaSola.Rango.UltimoMesExpresado).ToString());
		}

		// ---- elegir que dos meses comparar es del dominio ----
		private static void LaComparacionPorDefectoLaEligeElDominio()
		{
			Titulo("la comparacion por defecto la elige el dominio");

			// El guion termina en agosto 2026, asi que la comparacion por defecto es
			// agosto 2026 contra agosto 2025 — sin que nadie le pase los meses.
			ComparacionDeCanastas porDefecto = ElGuion().CompararConElAnioPasado();
			Igual("compara el ultimo mes con el del ano pasado", "8/2025 vs 8/2026",
				$"{porDefecto.Antes.Mes}/{porDefecto.Antes.Anio} vs {porDefecto.Despues.Mes}/{porDefecto.Despues.Anio}");
			Igual("  y da la misma lectura que pidiendola a mano", "True",
				(porDefecto.ParaSonia == ElGuion().CompararCanastas(2025, 8, 2026, 8).ParaSonia).ToString());

			// Total: sin compras no revienta, contesta que no hay comparacion.
			ComparacionDeCanastas sinNada = new Historial().CompararConElAnioPasado();
			Igual("historial vacio: no hay comparacion", "False", sinNada.HayComparacion.ToString());
			Igual("  y no hay pasillos que repartir", "0", sinNada.Diferencias.Count.ToString());

			// Con un solo mes tampoco hay dos meses que comparar, y se dice.
			var unaSola = new Historial();
			unaSola.Agregar(UnaCompra("T-1", Hoy, ("HELADO", "Helado", 1, 3000m, "Azucarados", 0)));
			Igual("con un solo mes: no hay comparacion", "False",
				unaSola.CompararConElAnioPasado().HayComparacion.ToString());
		}

		// ---- la serie: cuanto y CUANDO ----
		private static void LaSerieDiceDesdeCuando()
		{
			Titulo("la serie dice cuanto y desde cuando");

			Consumo azucarados = ElGuion().ConsumoDe("Azucarados");
			Igual("existe", "True", azucarados.Existe().ToString());
			Igual("  es una categoria", "True", azucarados.EsCategoria.ToString());
			// El guion mete azucarados en feb, jul y agosto de 2026.
			Igual("  tres meses con consumo", "3", azucarados.MesesConConsumo.ToString());
			Igual("  arranca en", "2026-02", azucarados.PrimerMesExpresado);
			Igual("  termina en", "2026-08", azucarados.UltimoMesExpresado);
			Igual("  el mes mas alto", "2026-08", azucarados.MesDelPicoExpresado);
			Igual("  el cambio tiene signo positivo", "True", (azucarados.CambioDeGasto > 0m).ToString());
			Igual("  y la linea nombra los dos extremos", "True",
				(azucarados.ParaSonia.Contains("2026-02") && azucarados.ParaSonia.Contains("2026-08")).ToString());

			// Por TEXTO de producto, no por categoria: el helado solo.
			Consumo helado = ElGuion().ConsumoDe("helado");
			Igual("por texto: no es categoria", "False", helado.EsCategoria.ToString());
			Igual("  tres meses de helado", "3", helado.MesesConConsumo.ToString());
			Igual("  siete unidades en total", "7", helado.TotalUnidades.ToString());
			Igual("  el primer mes son 2 unidades", "2", helado.Meses[0].Unidades.ToString());
			Igual("  el ultimo son 3", "3", helado.Meses[helado.MesesConConsumo - 1].Unidades.ToString());

			// Total: lo que no esta no revienta.
			Consumo nada = ElGuion().ConsumoDe("langosta");
			Igual("lo que no se compro: no existe", "False", nada.Existe().ToString());
			Igual("  sin meses", "0", nada.MesesConConsumo.ToString());
			Igual("  y lo dice", "True", nada.ParaSonia.Contains("no hay compras").ToString());
			Igual("  no hay cambio que leer", "False", nada.HayCambioReal.ToString());
			Revienta("  y darle direccion revienta", () => nada.Subio);
		}

		// ---- el consumo dice cuanto se movio, y no si eso esta bien ----
		private static void ElConsumoDiceCuantoSeMovio()
		{
			Titulo("el consumo dice cuanto se movio");

			// EL MISMO MOVIMIENTO en dos pasillos distintos se lee IGUAL: los dos subieron.
			// Antes uno era "empeorando" y el otro "mejorando" segun una etiqueta de salud
			// que este modulo se habia puesto solo; la spec de marca la prohibe -"no
			// inventar en la app conclusiones de salud"- y quien cruza es el asistente.
			var historial = new Historial();
			historial.Agregar(UnaCompra("T-1", new DateTime(2026, 1, 5),
				("HELADO", "Helado", 1, 1000m, "Azucarados", 0),
				("POLLO", "Pollo", 1, 1000m, "Proteinas", 0)));
			historial.Agregar(UnaCompra("T-2", new DateTime(2026, 2, 5),
				("HELADO", "Helado", 5, 1000m, "Azucarados", 0),
				("POLLO", "Pollo", 5, 1000m, "Proteinas", 0)));

			Consumo dulces = historial.ConsumoDe("Azucarados");
			Consumo proteinas = historial.ConsumoDe("Proteinas");
			Igual("los dulces subieron", "True", dulces.Subio.ToString());
			Igual("las proteinas tambien", "True", proteinas.Subio.ToString());
			Igual("y las dos lecturas son la misma forma", "True",
				(dulces.HayCambioReal == proteinas.HayCambioReal).ToString());

			// Y al reves, con el mismo dominio.
			var bajando = new Historial();
			bajando.Agregar(UnaCompra("T-1", new DateTime(2026, 1, 5),
				("HELADO", "Helado", 5, 1000m, "Azucarados", 0)));
			bajando.Agregar(UnaCompra("T-2", new DateTime(2026, 2, 5),
				("HELADO", "Helado", 1, 1000m, "Azucarados", 0)));
			Igual("dulces que bajan: bajaron", "False", bajando.ConsumoDe("Azucarados").Subio.ToString());

			// Un solo mes no es una serie: entrega los hechos y no le pone direccion.
			var unMes = new Historial();
			unMes.Agregar(UnaCompra("T-1", Hoy, ("HELADO", "Helado", 3, 3000m, "Azucarados", 0)));
			Consumo deUnMes = unMes.ConsumoDe("Azucarados");
			Igual("un solo mes: no hay serie", "False", deUnMes.HaySerie.ToString());
			Igual("  ni cambio real", "False", deUnMes.HayCambioReal.ToString());
			Revienta("  y la direccion revienta", () => deUnMes.Subio);
			Igual("  pero los hechos estan", "9000", deUnMes.TotalGastado.ToString());

			// Lo no comestible entrega la misma serie que lo demas: la olla no es un caso
			// aparte, porque ya no hay etiqueta que consultarle.
			Consumo olla = ElGuion().ConsumoDe("olla");
			Igual("lo no comestible tambien tiene serie", "2026-05", olla.PrimerMesExpresado);

			// Ninguna linea del consumo se pronuncia sobre si el cambio esta bien.
			string[] juicios = { "peor", "mejor", "empeora", "mejora", "debería", "conviene", "cuidado" };
			foreach (string juicio in juicios)
				Igual($"la linea no dice '{juicio}'", "False",
					dulces.ParaSonia.ToLowerInvariant().Contains(juicio).ToString());
		}

		// Dos meses, cada uno con sus lineas, comparados.
		private static ComparacionDeCanastas Comparar(
			(int Anio, int Mes, (string, string, int, decimal, string, int) Linea1, (string, string, int, decimal, string, int) Linea2) antes,
			(int Anio, int Mes, (string, string, int, decimal, string, int) Linea1, (string, string, int, decimal, string, int) Linea2) despues)
		{
			var historial = new Historial();
			historial.Agregar(UnaCompra("T-antes", new DateTime(antes.Anio, antes.Mes, 5), antes.Linea1, antes.Linea2));
			historial.Agregar(UnaCompra("T-despues", new DateTime(despues.Anio, despues.Mes, 5), despues.Linea1, despues.Linea2));

			return historial.CompararCanastas(antes.Anio, antes.Mes, despues.Anio, despues.Mes);
		}

		// -------------------------------------------------------------- el guion

		// El mismo guion de Muestras, construido directo sobre el dominio: aca se
		// comprueban las reglas; alla, que el wire las alimente igual.
		// LA CADENCIA. Un numero de dias no es una respuesta: la pregunta que la persona se
		// hace es "cada cuanto compro yo esto", y lo que reconoce es "todas las semanas".
		private static void LaCadenciaSeDiceComoHablaLaGente()
		{
			Titulo("la cadencia se dice como habla la gente");

			Igual("7 dias es todas las semanas", "todas las semanas", Cadencia.Para(7).ParaMostrar);
			Igual("14 dias es cada dos semanas", "cada dos semanas", Cadencia.Para(14).ParaMostrar);
			Igual("30 dias es una vez al mes", "una vez al mes", Cadencia.Para(30).ParaMostrar);
			Igual("120 dias es de vez en cuando", "de vez en cuando", Cadencia.Para(120).ParaMostrar);

			// LA HOLGURA ES A PROPOSITO: un habito no es un reloj. Una compra semanal que se
			// corrio dos dias sigue siendo semanal, y si no, la app anunciaria un cambio de
			// rutina cada vez que alguien fue al super un lunes en vez de un sabado.
			Igual("9 dias todavia es semanal", "todas las semanas", Cadencia.Para(9).ParaMostrar);
			Igual("10 dias ya no", "cada dos semanas", Cadencia.Para(10).ParaMostrar);

			// Conjunto cerrado comparado por REFERENCIA, como Categoria y Severidad.
			Igual("dos intervalos semanales dan LA misma cadencia", "True",
				ReferenceEquals(Cadencia.Para(6), Cadencia.Para(8)).ToString());

			Igual("lo semanal es mas seguido que lo mensual", "True",
				Cadencia.TodasLasSemanas.EsMasSeguidaQue(Cadencia.UnaVezAlMes).ToString());
			Igual("y no al reves", "False",
				Cadencia.UnaVezAlMes.EsMasSeguidaQue(Cadencia.TodasLasSemanas).ToString());

			// La forma corta, que es la que pide la pastilla del diseño de la app.
			Igual("la compacta es para una pastilla", "1 vez/semana", Cadencia.TodasLasSemanas.Compacta);

			Revienta("cero dias no es una cadencia", () => Cadencia.Para(0));
			Revienta("dias negativos tampoco", () => Cadencia.Para(-3));

			// TOTAL: cualquier cantidad de dias cae en una, porque la ultima no tiene techo.
			foreach (int dias in new[] { 1, 5, 9, 10, 18, 19, 45, 46, 75, 76, 9999 })
				Igual($"  {dias} dias tiene cadencia", "True", (Cadencia.Para(dias) != null).ToString());
		}

		// EL RITMO DE UN PRODUCTO, en dos epocas. Es la lectura que la wiki pide por su
		// nombre: la respuesta esperada de S10 dice que el helado "paso de 1 vez al mes a
		// casi todas las semanas".
		private static void ElRitmoDeUnProductoCambia()
		{
			Titulo("el ritmo de un producto cambia");

			// Helado una vez al mes durante cuatro meses, y desde marzo todas las semanas.
			// La leche va semanal todo el tiempo: tiene que salir SIN cambio.
			var historial = new Historial();
			for (int mes = 1; mes <= 4; mes++)
				historial.Agregar(UnaCompra($"M-{mes}", new DateTime(2025, mes, 6),
					("HELADO-1L", "Helado de vainilla 1L", 1, 3680m, "Azucarados", 0),
					("LECHE-1L", "Leche 1L", 2, 950m, "LacteosYHuevos", 0)));

			for (int i = 0; i < 8; i++)
				historial.Agregar(UnaCompra($"S-{i}", new DateTime(2025, 6, 7).AddDays(i * 7),
					("HELADO-1L", "Helado de vainilla 1L", 1, 3680m, "Azucarados", 0),
					("LECHE-1L", "Leche 1L", 2, 950m, "LacteosYHuevos", 0)));

			var corte = new DateTime(2025, 6, 1);

			CambioDeRitmo helado = historial.RitmoDeUnProducto("HELADO-1L", corte);
			Igual("hay con que comparar", "True", helado.HayConQueComparar.ToString());
			Igual("  antes: una vez al mes", "una vez al mes", helado.Antes.ParaMostrar);
			Igual("  ahora: todas las semanas", "todas las semanas", helado.Ahora.ParaMostrar);
			Igual("  cambio", "True", helado.Cambio.ToString());
			Igual("  y es mas seguido", "True", helado.EsMasSeguido.ToString());
			Igual("  la linea dice de que a que", "True",
				(helado.ParaSonia.Contains("una vez al mes")
				 && helado.ParaSonia.Contains("todas las semanas")).ToString());

			// Y NO JUZGA. "Que el helado pasara de mensual a semanal es un dato, no una
			// falta": la app lo señala y se calla.
			foreach (string juicio in new[] { "peor", "mejor", "debería", "conviene", "cuidado", "mal" })
				Igual($"  la linea no dice '{juicio}'", "False",
					helado.ParaSonia.Contains(juicio).ToString());

			// LO QUE NO CAMBIO NO SE ANUNCIA. La leche fue semanal de los dos lados... pero
			// del lado de antes fueron compras MENSUALES, asi que su cadencia de antes es
			// mensual igual que la del helado: las dos entraron en las mismas cuatro
			// compras. Es correcto y conviene tenerlo escrito, porque revela que el ritmo
			// se mide sobre las compras EN LAS QUE APARECE el producto y no sobre el
			// calendario.
			CambioDeRitmo leche = historial.RitmoDeUnProducto("LECHE-1L", corte);
			Igual("la leche tambien salta", "True", leche.Cambio.ToString());

			// EL PRODUCTO SE NOMBRA, NO SE CODIFICA. La especificacion de marca prohibe la
			// palabra "SKU" en lo que ve la persona, y una tool que la EXIGE como argumento
			// es la version peor de lo mismo: obliga a quien pregunta a conocer una llave
			// interna. Se resuelve por nombre, por parte del nombre, o por el sku.
			Igual("por nombre completo", "True",
				historial.RitmoDeUnProducto("Helado de vainilla 1L", corte).HayConQueComparar.ToString());
			Igual("por parte del nombre", "True",
				historial.RitmoDeUnProducto("helado", corte).HayConQueComparar.ToString());
			Igual("sin importar mayusculas", "True",
				historial.RitmoDeUnProducto("HELADO", corte).HayConQueComparar.ToString());
			Igual("y el sku sigue sirviendo", "True",
				historial.RitmoDeUnProducto("HELADO-1L", corte).HayConQueComparar.ToString());
			Igual("las cuatro formas dan el mismo ritmo", "True",
				(historial.RitmoDeUnProducto("helado", corte).Ahora.ParaMostrar
				 == historial.RitmoDeUnProducto("HELADO-1L", corte).Ahora.ParaMostrar).ToString());

			// Un nombre que no existe: contesta con el texto buscado y no revienta.
			CambioDeRitmo inventado = historial.RitmoDeUnProducto("caviar", corte);
			Igual("un producto que nunca compro: no hay con que", "False",
				inventado.HayConQueComparar.ToString());
			Igual("  y la linea nombra lo que se busco", "True",
				inventado.ParaSonia.Contains("caviar").ToString());

			// El resolvedor, directo.
			Igual("el resolvedor devuelve el sku", "HELADO-1L", historial.SkuDe("helado"));
			Igual("y vacio cuando no hay", "", historial.SkuDe("caviar"));

			// SIN SUFICIENTES COMPRAS DE UN LADO, lo dice y no inventa un ritmo.
			var pocas = new Historial();
			for (int i = 0; i < 5; i++)
				pocas.Agregar(UnaCompra($"P-{i}", new DateTime(2026, 5, 2).AddDays(i * 7),
					("HELADO-1L", "Helado de vainilla 1L", 1, 3680m, "Azucarados", 0)));

			CambioDeRitmo sinAntes = pocas.RitmoDeUnProducto("HELADO-1L", new DateTime(2026, 5, 1));
			Igual("sin compras antes: no hay con que comparar", "False",
				sinAntes.HayConQueComparar.ToString());
			Igual("  y no afirma un cambio", "False", sinAntes.Cambio.ToString());
			Igual("  y lo dice", "True",
				sinAntes.ParaSonia.Contains("todavía no hay suficientes").ToString());
			Revienta("  y la cadencia de antes revienta", () => sinAntes.Antes);
			Revienta("  y lo de mas seguido tambien", () => sinAntes.EsMasSeguido);

			// UN PRODUCTO QUE NUNCA ENTRO: la misma respuesta, sin reventar.
			CambioDeRitmo nunca = historial.RitmoDeUnProducto("NO-EXISTE", corte);
			Igual("un sku que nunca entro: no hay con que", "False", nunca.HayConQueComparar.ToString());

			// EL MAS NOTORIO PRIMERO, que es el que la app pinta en su tarjeta.
			IReadOnlyList<CambioDeRitmo> cambios = historial.CambiosDeRitmo(corte);
			Igual("hay cambios de ritmo", "True", (cambios.Count > 0).ToString());
			Igual("  todos cambiaron de verdad", "True",
				cambios.All(c => c.Cambio).ToString());

			// De 6 a 7 dias NO es un cambio: las dos son "todas las semanas". Si esto
			// fallara, la app anunciaria un cambio de rutina cada semana corrida.
			var corridas = new Historial();
			int[] dias = { 0, 7, 14, 21, 27, 33, 40, 46 };
			for (int i = 0; i < dias.Length; i++)
				corridas.Agregar(UnaCompra($"C-{i}", new DateTime(2026, 2, 7).AddDays(dias[i]),
					("CAFE-500", "Café molido 500g", 1, 3200m, "GranosYAbarrotes", 0)));

			CambioDeRitmo cafe = corridas.RitmoDeUnProducto("CAFE-500", new DateTime(2026, 3, 7));
			Igual("una semana corrida no es un cambio", "False", cafe.Cambio.ToString());
			Igual("  y la linea dice que sigue igual", "True",
				cafe.ParaSonia.Contains("sigue").ToString());
		}

		// CADA CUANTO VA AL SUPER, que dia y a que hora. Es la pantalla "tu ritmo de super"
		// del diseño de la app: "hay dias y momentos que se repiten mas de lo que parece".
		private static void ElRitmoDelSuperTieneTresCaras()
		{
			Titulo("el ritmo del super tiene tres caras");

			// Ocho sabados seguidos: seis por la tarde, uno en la mañana, uno de noche.
			var historial = new Historial();
			var horas = new[] { 17, 16, 10, 18, 17, 11, 20, 16 };
			for (int i = 0; i < horas.Length; i++)
				historial.Agregar(UnaCompra($"S-{i}",
					new DateTime(2026, 2, 7).AddDays(i * 7).AddHours(horas[i]),
					("LECHE-1L", "Leche 1L", 2, 950m, "LacteosYHuevos", 0)));

			RitmoDelSuper ritmo = historial.RitmoDelSuper();
			Igual("el ritmo se midio", "True", ritmo.EsObservado.ToString());
			Igual("  vuelve cada 7 dias", "7", ritmo.DiasTipicos.ToString());
			Igual("  o sea todas las semanas", "todas las semanas", ritmo.Cadencia.ParaMostrar);
			Igual("  y el dia es el sabado", "sábado", ritmo.DiaFavoritoExpresado);
			Igual("  las 8 cayeron sabado", "8", ritmo.ComprasEnElDiaFavorito.ToString());
			Igual("  el momento es la tarde", "tarde", ritmo.MomentoFavorito.Nombre);
			Igual("  la linea dice cada cuanto, que dia y cuando", "True",
				(ritmo.ParaSonia.Contains("cada 7 días")
				 && ritmo.ParaSonia.Contains("sábado")
				 && ritmo.ParaSonia.Contains("tarde")).ToString());

			// LOS TRES MOMENTOS SIEMPRE VIENEN, y en orden del dia: la pantalla dibuja la
			// fila sin preguntar, que es de lo que sirve una lectura total.
			Igual("vienen los tres momentos", "3", ritmo.PorMomento.Count.ToString());
			Igual("  en orden: manana primero", "manana", ritmo.PorMomento[0].Momento.Nombre);
			Igual("  y noche al final", "noche", ritmo.PorMomento[2].Momento.Nombre);
			Igual("  manana: 2 de 8", "2", ritmo.PorMomento[0].Cuantas.ToString());
			Igual("  tarde: 5 de 8", "5", ritmo.PorMomento[1].Cuantas.ToString());
			Igual("  noche: 1 de 8", "1", ritmo.PorMomento[2].Cuantas.ToString());
			Igual("  y la fila se lee sola", "tarde · 5 compras", ritmo.PorMomento[1].ParaMostrar);

			// LOS CORTES DEL DIA son un juicio y estan a la vista.
			Igual("las 12:59 son mañana", "manana",
				MomentoDelDia.De(new DateTime(2026, 1, 1, 12, 59, 0)).Nombre);
			Igual("la 1 de la tarde ya es tarde", "tarde",
				MomentoDelDia.De(new DateTime(2026, 1, 1, 13, 0, 0)).Nombre);
			Igual("las 7 de la noche ya es noche", "noche",
				MomentoDelDia.De(new DateTime(2026, 1, 1, 19, 0, 0)).Nombre);

			// SIN SUFICIENTES COMPRAS, lo dice y no inventa un ritmo.
			var pocas = new Historial();
			pocas.Agregar(UnaCompra("U-1", new DateTime(2026, 3, 7),
				("LECHE-1L", "Leche 1L", 1, 950m, "LacteosYHuevos", 0)));

			RitmoDelSuper sinRitmo = pocas.RitmoDelSuper();
			Igual("con una compra no hay ritmo", "False", sinRitmo.EsObservado.ToString());
			Igual("  pero dice cuantas miro", "1", sinRitmo.ComprasMiradas.ToString());
			Igual("  y lo dice", "True", sinRitmo.ParaSonia.Contains("todavía no alcanza").ToString());
			Igual("  los tres momentos vienen en cero", "3", sinRitmo.PorMomento.Count.ToString());
			Igual("  el primero en cero", "0", sinRitmo.PorMomento[0].Cuantas.ToString());
			Igual("  y el dia expresado va vacio", "", sinRitmo.DiaFavoritoExpresado);
			Revienta("  los dias tipicos revientan", () => sinRitmo.DiasTipicos);
			Revienta("  y el dia favorito tambien", () => sinRitmo.DiaFavorito);

			// TRES COMPRAS EL MISMO DIA no dan intervalo, y eso no es un ritmo.
			var mismoDia = new Historial();
			for (int i = 0; i < 3; i++)
				mismoDia.Agregar(UnaCompra($"M-{i}", new DateTime(2026, 3, 7).AddHours(i),
					("LECHE-1L", "Leche 1L", 1, 950m, "LacteosYHuevos", 0)));

			Igual("tres compras el mismo dia no dan ritmo", "False",
				mismoDia.RitmoDelSuper().EsObservado.ToString());

			// Un historial vacio: la otra forma de no tener respuesta.
			Igual("sin compras lo dice distinto", "True",
				new Historial().RitmoDelSuper().ParaSonia.Contains("todavía no hay compras").ToString());
		}

		// LA CONSTANCIA. Una proporcion no es una respuesta: "0,875" es correcto y no se lo
		// dice nadie. La ficha de producto del diseño lo pide como "casi nunca falta".
		private static void LaConstanciaSeDiceEnPalabras()
		{
			Titulo("la constancia se dice en palabras");

			Igual("en todas: nunca falla", "nunca falla", Constancia.Para(1.0m).ParaMostrar);
			Igual("en 7 de 8: casi nunca falta", "casi nunca falta", Constancia.Para(0.875m).ParaMostrar);
			Igual("en la mitad: va y viene", "va y viene", Constancia.Para(0.5m).ParaMostrar);
			Igual("en 1 de 8: de vez en cuando", "de vez en cuando", Constancia.Para(0.125m).ParaMostrar);
			Igual("en ninguna: de vez en cuando", "de vez en cuando", Constancia.Para(0m).ParaMostrar);

			Igual("nunca falla es mas fiel que va y viene", "True",
				Constancia.NuncaFalla.EsMasFielQue(Constancia.VaYViene).ToString());
			Igual("y no al reves", "False",
				Constancia.VaYViene.EsMasFielQue(Constancia.NuncaFalla).ToString());

			Igual("conjunto cerrado por referencia", "True",
				ReferenceEquals(Constancia.Para(0.9m), Constancia.Para(0.8m)).ToString());

			Revienta("una proporcion mayor que 1 revienta", () => Constancia.Para(1.5m));
			Revienta("y una negativa tambien", () => Constancia.Para(-0.1m));

			// NO JUZGA: "va y viene" describe, no reprocha.
			foreach (Constancia c in Constancia.Todas)
				foreach (string juicio in new[] { "peor", "mejor", "debería", "mal" })
					Igual($"  '{c.Nombre}' no dice '{juicio}'", "False",
						c.ParaMostrar.Contains(juicio).ToString());

			// TOTAL: cualquier proporcion valida cae en una.
			for (int i = 0; i <= 10; i++)
				Igual($"  {i * 10}% tiene constancia", "True",
					(Constancia.Para(i / 10m) != null).ToString());
		}

		// ---- la serie tambien se lee por año ----
		private static void LaSerieSeAgrupaPorAnio()
		{
			Titulo("la serie tambien se lee por año");

			Historial guion = ElGuion();

			// El guion tiene azucarados en 2026-02, 2026-07 y 2026-08: un solo año.
			Consumo dulces = guion.ConsumoDe("Azucarados");
			Igual("azucarados: 3 meses", "3", dulces.Meses.Count.ToString());
			Igual("  y un solo año", "1", dulces.Anios.Count.ToString());
			Igual("  que es 2026", "2026", dulces.Anios[0].AnioExpresado);
			Igual("  con los 3 meses adentro", "3", dulces.Anios[0].MesesConCompras.ToString());

			// EL AÑO SUMA LO MISMO QUE SUS MESES. Es la comprobacion que importa: si el
			// agrupado no cuadra con la serie, la pantalla muestra dos verdades distintas.
			int unidadesDeLosMeses = 0;
			decimal gastoDeLosMeses = 0m;
			foreach (MesDeConsumo mes in dulces.Meses)
			{
				unidadesDeLosMeses = unidadesDeLosMeses + mes.Unidades;
				gastoDeLosMeses = gastoDeLosMeses + mes.Gasto;
			}
			Igual("  las unidades del año son las de sus meses", unidadesDeLosMeses.ToString(),
				dulces.Anios[0].Unidades.ToString());
			Igual("  y el gasto tambien", gastoDeLosMeses.ToString("0.##"),
				dulces.Anios[0].Gasto.ToString("0.##"));
			Igual("  y el total del consumo cierra igual", dulces.TotalUnidades.ToString(),
				dulces.Anios[0].Unidades.ToString());

			// EL POLLO CRUZA DOS AÑOS: 2025-08 (2 u) y 2026-02 (1 u).
			Consumo pollo = guion.ConsumoDe("POLLO-KG");
			Igual("pollo: dos años", "2", pollo.Anios.Count.ToString());
			Igual("  el primero es el mas viejo", "2025", pollo.Anios[0].AnioExpresado);
			Igual("  y el ultimo el mas nuevo", "2026", pollo.Anios[1].AnioExpresado);
			Igual("  2025 llevo 2 unidades", "2", pollo.Anios[0].Unidades.ToString());
			Igual("  2026 llevo 1", "1", pollo.Anios[1].Unidades.ToString());
			// UN AÑO A MEDIAS SE DELATA por sus meses, que es lo que impide compararlo con
			// uno entero como si midieran lo mismo.
			Igual("  y cada uno tuvo un solo mes con compra", "1,1",
				$"{pollo.Anios[0].MesesConCompras},{pollo.Anios[1].MesesConCompras}");

			// TOTAL: sin consumo la serie anual es vacia, no revienta ni trae un año cero.
			Consumo nada = guion.ConsumoDe("caviar");
			Igual("caviar: no existe", "False", nada.Existe().ToString());
			Igual("  y no tiene años", "0", nada.Anios.Count.ToString());
			Igual("historial vacio: tampoco", "0",
				new Historial().ConsumoDe("leche").Anios.Count.ToString());

			// EL AÑO CERO no se puede comprobar desde aca y esta bien: el constructor de
			// AnioDeConsumo es internal, asi que nadie de afuera puede armar uno mudo. La
			// guarda se queda igual, defendiendo del error de adentro.
		}

		// ---- en cuales compras aparecio esto ----
		private static void LaBusquedaEncuentraPorProducto()
		{
			Titulo("la busqueda encuentra por producto, no por numero de ticket");

			Historial guion = ElGuion();

			ComprasConElProducto helado = guion.ComprasCon("Helado");
			Igual("«Helado»: aparece", "True", helado.Existe().ToString());
			Igual("  en cuantas compras", "3", helado.CuantasCompras.ToString());
			Igual("  cuantas unidades en total", "7", helado.TotalUnidades.ToString());
			Igual("  cuanto se gasto", "25060", helado.TotalGastado.ToString("0"));
			Igual("  un solo producto coincide", "1", helado.Productos.Count.ToString());

			// DE LA MAS RECIENTE A LA MAS VIEJA, que es como se busca un ticket.
			Igual("  la primera es la ultima que hizo", "T-96470", helado.Compras[0].Numero);
			Igual("  y la ultima es la mas vieja", "T-91515", helado.Compras[2].Numero);

			// LAS DOS ESCALAS: lo del producto y lo de la compra entera.
			Igual("  en esa compra llevo 3 helados", "3", helado.Compras[0].Unidades.ToString());
			Igual("  y gasto 11040 EN HELADO", "11040", helado.Compras[0].Gasto.ToString("0"));
			Igual("  mientras la compra entera tiene 5 lineas", "5",
				helado.Compras[0].LineasDeLaCompra.ToString());
			Igual("  y suma bastante mas", "True",
				(helado.Compras[0].TotalDeLaCompra > helado.Compras[0].Gasto).ToString());

			// UN DEPARTAMENTO TAMBIEN SE BUSCA, porque el criterio es el mismo que el del
			// consumo. Es lo que permite preguntar por un pasillo entero.
			ComprasConElProducto dulces = guion.ComprasCon("Azucarados");
			Igual("«Azucarados» es un departamento y tambien se encuentra", "True",
				dulces.Existe().ToString());
			Igual("  en 3 compras", "3", dulces.CuantasCompras.ToString());
			Igual("  con cinco productos distintos", "5", dulces.Productos.Count.ToString());
			Igual("  y 26 unidades", "26", dulces.TotalUnidades.ToString());

			// UN TEXTO QUE ALCANZA A DOS PRODUCTOS los nombra a los dos, para que quien
			// pregunta sepa que la app junto dos cosas.
			ComprasConElProducto soda = guion.ComprasCon("soda");
			Igual("«soda» alcanza a dos productos", "2", soda.Productos.Count.ToString());
			Igual("  y suma las dos", "12", soda.TotalUnidades.ToString());

			// TOTAL: buscar algo que nunca se llevo no revienta ni miente.
			ComprasConElProducto nada = guion.ComprasCon("caviar");
			Igual("«caviar»: no aparece", "False", nada.Existe().ToString());
			Igual("  y no hay compras que mostrar", "0", nada.Compras.Count.ToString());
			Igual("  el contador para el cable da 0", "0", nada.CuantasCompras.ToString());
			Igual("  se dice con el termino adentro", "True",
				nada.ParaSonia.Contains("caviar").ToString());

			// TOTAL DE VERDAD, sin lectores guardados: de algo que nunca se compro se
			// llevaron cero unidades y se gastaron cero colones, y las dos son VERDAD. Es
			// lo que permite que una consulta del DSL las imprima planas sin preguntar
			// Existe() antes — un lector que revienta no blanquea la columna, mata la
			// consulta entera.
			Igual("  cero unidades, y no revienta", "0", nada.TotalUnidades.ToString());
			Igual("  cero gastado, y no revienta", "0", nada.TotalGastado.ToString("0"));

			// Y un historial sin compras es el mismo par, no un caso aparte.
			ComprasConElProducto vacio = new Historial().ComprasCon("leche");
			Igual("historial vacio: no aparece", "False", vacio.Existe().ToString());

			// LA FRASE NOMBRA EL PRODUCTO ENCONTRADO, no lo que se escribio: quien busco
			// "helado" tiene que poder confirmar que la app entendio cual.
			Igual("la frase nombra el producto hallado", "True",
				helado.ParaSonia.Contains("Helado de vainilla 1L").ToString());
			Igual("  y con varios dice cuantos coinciden", "True",
				dulces.ParaSonia.Contains("5 productos").ToString());
		}

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
