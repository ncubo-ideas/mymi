using System;
using System.Collections.Generic;

namespace Compras.Dominio
{
	/// <summary>
	/// El agregado: el historial de compras de la persona. Puerta de acceso a sus
	/// tickets, y quien contesta las preguntas que exigen la historia completa — la
	/// canasta de un mes, su comparacion con otro, y la garantia con su respaldo.
	/// </summary>
	public class Historial
	{
		private readonly List<Compra> compras = new List<Compra>();
		private readonly Dictionary<string, Compra> porNumero =
			new Dictionary<string, Compra>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Los productos que esta persona conoce, armados desde sus propias compras. Es lo
		/// que mantiene coherente el historial: la categoría es del producto, no del ticket.
		/// </summary>
		public Catalogo Catalogo { get; private set; } = new Catalogo();

		public IReadOnlyList<Compra> Lista => compras;

		public int Cantidad => compras.Count;

		public decimal Total
		{
			get
			{
				decimal total = 0m;
				foreach (Compra compra in compras) total = total + compra.Total;
				return total;
			}
		}

		/// <summary>Lectura por numero de ticket: un numero que no existe es un error de quien llama.</summary>
		public Compra this[string numero]
		{
			get
			{
				ArgumentNullException.ThrowIfNullOrWhiteSpace(numero);
				if (!porNumero.TryGetValue(numero, out Compra compra))
					throw new KeyNotFoundException($"No hay ninguna compra con numero '{numero}'.");
				return compra;
			}
		}

		/// <summary>Reconocimiento total: la pareja del indexador, para preguntar sin reventar.</summary>
		public bool Tiene(string numero)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(numero);
			return porNumero.ContainsKey(numero);
		}

		public void Agregar(Compra compra)
		{
			ArgumentNullException.ThrowIfNull(compra);
			if (porNumero.ContainsKey(compra.Numero))
				throw new InvalidOperationException($"La compra '{compra.Numero}' ya estaba registrada.");

			// El catalogo se reconoce ANTES de guardar la compra: si una linea contradice
			// la categoria de un producto ya conocido, revienta y la compra no entra. Una
			// compra a medias seria peor que rechazarla -la mitad de las lineas cambiando
			// el significado de la canasta-, y aca es donde el journal todavia no escribio.
			foreach (Item item in compra.Items) Catalogo.Reconocer(item);

			compras.Add(compra);
			porNumero[compra.Numero] = compra;
		}

		/// <summary>La foto de un mes. Total: un mes sin compras devuelve una canasta vacia.</summary>
		public Canasta CanastaDe(int anio, int mes)
		{
			var canasta = new Canasta(anio, mes);
			foreach (Compra compra in compras)
				if (compra.EsDelMes(anio, mes))
					canasta.Sumar(compra);

			return canasta;
		}

		/// <summary>Como cambio la canasta entre dos meses: la pregunta que ningun ticket contesta.</summary>
		public ComparacionDeCanastas CompararCanastas(int anioAntes, int mesAntes, int anioDespues, int mesDespues) =>
			new ComparacionDeCanastas(CanastaDe(anioAntes, mesAntes), CanastaDe(anioDespues, mesDespues));

		/// <summary>
		/// Desde cuando hasta cuando hay compras, y cuantos meses tienen alguna. Es el
		/// contexto que necesita quien pregunta para saber que meses puede comparar.
		/// </summary>
		public RangoDeCompras Rango
		{
			get
			{
				if (compras.Count == 0) return RangoDeCompras.Ninguno;

				DateTime primera = DateTime.MaxValue;
				DateTime ultima = DateTime.MinValue;
				var meses = new HashSet<int>();

				foreach (Compra compra in compras)
				{
					if (compra.Fecha < primera) primera = compra.Fecha;
					if (compra.Fecha > ultima) ultima = compra.Fecha;
					meses.Add(compra.Fecha.Year * 100 + compra.Fecha.Month);
				}

				return RangoDeCompras.De(primera, ultima, meses.Count);
			}
		}

		/// <summary>
		/// La comparacion por defecto: el ultimo mes con compras contra el mismo mes del
		/// anio anterior. La elige el DOMINIO y no la superficie, porque elegir que dos
		/// meses se comparan es parte de la pregunta y no de como se dibuja. Total: sin
		/// compras devuelve una comparacion que contesta que no hay comparacion.
		/// </summary>
		public ComparacionDeCanastas CompararConElAnioPasado()
		{
			RangoDeCompras rango = Rango;
			if (!rango.Existe()) return CompararCanastas(1900, 1, 1900, 2);

			return CompararCanastas(rango.UltimoAnio - 1, rango.UltimoMes, rango.UltimoAnio, rango.UltimoMes);
		}

		/// <summary>
		/// La garantia de un producto, buscada por sku o descripcion. Si se compro varias
		/// veces gana la mas reciente, que es la cobertura que corre. La fecha de consulta
		/// viene recibida: el dominio no lee el reloj.
		/// </summary>
		public RegistroDeGarantia BuscarGarantia(string texto, DateTime fechaDeConsulta)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(texto);

			RegistroDeGarantia mejor = RegistroDeGarantia.NoEncontrada(texto);
			foreach (Compra compra in compras)
				foreach (Item item in compra.Items)
				{
					if (!item.TieneGarantia) continue;
					if (!Coincide(item, texto)) continue;
					if (mejor.Encontrada && compra.Fecha <= mejor.FechaDeCompra) continue;

					mejor = RegistroDeGarantia.De(texto, compra, item, fechaDeConsulta);
				}

			return mejor;
		}

		/// <summary>
		/// CUANTO Y CUANDO entro algo, mes a mes. El termino puede ser una categoria del
		/// repertorio (Azucarados) o un texto de producto (helado, olla). Total: lo que
		/// no esta devuelve Consumo.Ninguno, no una excepcion.
		/// </summary>
		// Es la serie que hace posible decir DESDE CUANDO cambio un consumo. Sin la
		// fecha, una relacion con cualquier otra cosa de la persona no es creible.
		public Consumo ConsumoDe(string termino)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(termino);

			bool esCategoria = Categoria.Existe(termino);
			var porMes = new Dictionary<int, MesDeConsumo>();
			var orden = new List<int>();

			foreach (Compra compra in compras)
			{
				bool contada = false;
				foreach (Item item in compra.Items)
				{
					if (!Alcanza(item, termino, esCategoria)) continue;

					int llave = compra.Fecha.Year * 100 + compra.Fecha.Month;
					if (!porMes.TryGetValue(llave, out MesDeConsumo mes))
					{
						mes = new MesDeConsumo(compra.Fecha.Year, compra.Fecha.Month);
						porMes[llave] = mes;
						orden.Add(llave);
					}

					mes.Sumar(item.Cantidad, item.Total);
					if (!contada) { mes.ContarCompra(); contada = true; }
				}
			}

			if (porMes.Count == 0) return Consumo.Ninguno(termino);

			orden.Sort();
			var meses = new List<MesDeConsumo>();
			foreach (int llave in orden) meses.Add(porMes[llave]);

			return Consumo.De(termino, esCategoria, meses);
		}

		private static bool Alcanza(Item item, string termino, bool esCategoria) =>
			esCategoria
				? string.Equals(item.Categoria.Nombre, termino, StringComparison.OrdinalIgnoreCase)
				: Coincide(item, termino);

		/// <summary>
		/// LO QUE NO FALLA: los productos que vuelven compra tras compra, mirando las
		/// últimas compras. Es la lectura con la que la app abre.
		/// </summary>
		// Se mira una cantidad de COMPRAS y no un rango de fechas: la firma es "lo de
		// siempre", y lo de siempre se reconoce en las ultimas veces que fue al super, no
		// en un mes del calendario.
		public FirmaDeLaCanasta FirmaDeLasUltimas(int cuantasCompras)
		{
			if (cuantasCompras <= 0)
				throw new ArgumentOutOfRangeException(nameof(cuantasCompras), cuantasCompras,
					"Mirar cero compras no da una firma.");

			IReadOnlyList<Compra> ultimas = UltimasCompras(cuantasCompras);
			if (ultimas.Count == 0) return FirmaDeLaCanasta.Ninguna(0);

			var vecesPorSku = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			var ultimaVezPorSku = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

			foreach (Compra compra in ultimas)
			{
				// Un producto cuenta UNA vez por compra, aunque venga en dos lineas.
				var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (Item item in compra.Items)
				{
					if (!vistos.Add(item.Sku)) continue;

					vecesPorSku.TryGetValue(item.Sku, out int veces);
					vecesPorSku[item.Sku] = veces + 1;

					if (!ultimaVezPorSku.TryGetValue(item.Sku, out DateTime ultima) || compra.Fecha > ultima)
						ultimaVezPorSku[item.Sku] = compra.Fecha;
				}
			}

			var habituales = new List<ProductoHabitual>();
			foreach (KeyValuePair<string, int> par in vecesPorSku)
			{
				decimal proporcion = (decimal)par.Value / ultimas.Count;
				if (proporcion < FirmaDeLaCanasta.ProporcionQueLoVuelveHabitual) continue;

				habituales.Add(new ProductoHabitual(par.Key, Catalogo[par.Key].Descripcion,
					par.Value, ultimas.Count, ultimaVezPorSku[par.Key]));
			}

			// De lo que mas vuelve a lo que menos; a igual frecuencia, lo mas reciente.
			habituales.Sort((uno, otro) =>
			{
				int porVeces = otro.ComprasEnQueAparece.CompareTo(uno.ComprasEnQueAparece);
				return porVeces != 0 ? porVeces : otro.UltimaVez.CompareTo(uno.UltimaVez);
			});

			return habituales.Count == 0
				? FirmaDeLaCanasta.Ninguna(ultimas.Count)
				: FirmaDeLaCanasta.De(ultimas.Count, habituales);
		}

		/// <summary>
		/// QUÉ CAMBIÓ en la canasta: los productos que entraron y se quedaron, con el mes
		/// en que aparecieron, y los que dejaron de estar. Total: si no cambió nada, lo dice.
		/// </summary>
		public CambioEnLaCanasta CambioDesde(DateTime corte, int cuantasCompras)
		{
			if (cuantasCompras <= 0)
				throw new ArgumentOutOfRangeException(nameof(cuantasCompras), cuantasCompras,
					"Mirar cero compras no da un cambio.");

			var antes = new List<Compra>();
			var despues = new List<Compra>();
			foreach (Compra compra in Ordenadas())
				(compra.Fecha < corte ? antes : despues).Add(compra);

			return new CambioEnLaCanasta(
				FirmaDe(Ultimas(antes, cuantasCompras)),
				FirmaDe(Primeras(despues, cuantasCompras)),
				PrimeraVezPorSku(corte));
		}

		/// <summary>
		/// La primera vez que cada producto apareció A PARTIR de una fecha. Es lo que le
		/// pone FECHA al cambio.
		/// </summary>
		// SE MIRA DESDE EL CORTE, no desde el principio del historial, y la diferencia se
		// vio con datos de verdad. El helado aparecia una vez al mes durante todo 2025 y
		// paso a semanal en enero de 2026: con la primera vez ABSOLUTA, la app decia "desde
		// 2025-01 entro el helado", que es cierto y no es la respuesta. Lo que cambio no fue
		// que apareciera -aparecia- sino que se volviera habitual, y eso arranca en el
		// periodo nuevo.
		//
		// Dicho de otra forma: la fecha que la persona reconoce es la del CAMBIO DE RITMO,
		// no la de la primera vez que probo algo.
		private IReadOnlyDictionary<string, DateTime> PrimeraVezPorSku(DateTime desde)
		{
			var primera = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
			foreach (Compra compra in Ordenadas())
			{
				if (compra.Fecha < desde) continue;

				foreach (Item item in compra.Items)
					if (!primera.TryGetValue(item.Sku, out DateTime cuando) || compra.Fecha < cuando)
						primera[item.Sku] = compra.Fecha;
			}

			return primera;
		}

		private IReadOnlyList<Compra> UltimasCompras(int cuantas) => Ultimas(Ordenadas(), cuantas);

		private static IReadOnlyList<Compra> Ultimas(IReadOnlyList<Compra> ordenadas, int cuantas)
		{
			var tomadas = new List<Compra>();
			for (int i = Math.Max(0, ordenadas.Count - cuantas); i < ordenadas.Count; i++)
				tomadas.Add(ordenadas[i]);

			return tomadas;
		}

		private static IReadOnlyList<Compra> Primeras(IReadOnlyList<Compra> ordenadas, int cuantas)
		{
			var tomadas = new List<Compra>();
			for (int i = 0; i < Math.Min(cuantas, ordenadas.Count); i++) tomadas.Add(ordenadas[i]);

			return tomadas;
		}

		// La firma de un conjunto ya elegido de compras. Comparte el criterio con
		// FirmaDeLasUltimas: un producto es habitual si esta en la mitad o mas.
		private FirmaDeLaCanasta FirmaDe(IReadOnlyList<Compra> algunas)
		{
			if (algunas.Count == 0) return FirmaDeLaCanasta.Ninguna(0);

			var vecesPorSku = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			var ultimaVezPorSku = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

			foreach (Compra compra in algunas)
			{
				var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (Item item in compra.Items)
				{
					if (!vistos.Add(item.Sku)) continue;

					vecesPorSku.TryGetValue(item.Sku, out int veces);
					vecesPorSku[item.Sku] = veces + 1;

					if (!ultimaVezPorSku.TryGetValue(item.Sku, out DateTime ultima) || compra.Fecha > ultima)
						ultimaVezPorSku[item.Sku] = compra.Fecha;
				}
			}

			var habituales = new List<ProductoHabitual>();
			foreach (KeyValuePair<string, int> par in vecesPorSku)
			{
				decimal proporcion = (decimal)par.Value / algunas.Count;
				if (proporcion < FirmaDeLaCanasta.ProporcionQueLoVuelveHabitual) continue;

				habituales.Add(new ProductoHabitual(par.Key, Catalogo[par.Key].Descripcion,
					par.Value, algunas.Count, ultimaVezPorSku[par.Key]));
			}

			habituales.Sort((uno, otro) => otro.ComprasEnQueAparece.CompareTo(uno.ComprasEnQueAparece));

			return habituales.Count == 0
				? FirmaDeLaCanasta.Ninguna(algunas.Count)
				: FirmaDeLaCanasta.De(algunas.Count, habituales);
		}

		/// <summary>
		/// CÓMO LE CAMBIÓ EL PRECIO de algo a esta persona, comparando siempre por unidad
		/// de medida. Total: un sku que nunca compró devuelve Ninguno.
		/// </summary>
		// Por SKU y no por texto: el precio de "leche" no significa nada si mezcla dos
		// productos distintos. Quien pregunta resuelve primero el sku con el catalogo.
		public PrecioEnElTiempo PrecioDe(string sku)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(sku);

			if (!Catalogo.Tiene(sku)) return PrecioEnElTiempo.Ninguno(sku);

			var observados = new List<PrecioObservado>();
			foreach (Compra compra in Ordenadas())
				foreach (Item item in compra.Items)
				{
					if (!string.Equals(item.Sku, sku, StringComparison.OrdinalIgnoreCase)) continue;

					observados.Add(new PrecioObservado(compra.Fecha, compra.Numero, item.PrecioUnitario,
						item.PrecioPorUnidadBase, item.Presentacion.Expresada));
				}

			return observados.Count == 0
				? PrecioEnElTiempo.Ninguno(sku)
				: PrecioEnElTiempo.De(Catalogo[sku], observados);
		}

		// Las compras por fecha. El journal las guarda en el orden en que ENTRARON, que no
		// tiene por que ser el orden en que pasaron: el proveedor puede publicar un ticket
		// viejo despues de uno nuevo. Cualquier lectura que dependa del tiempo ordena.
		private IReadOnlyList<Compra> Ordenadas()
		{
			var ordenadas = new List<Compra>(compras);
			ordenadas.Sort((una, otra) => una.Fecha.CompareTo(otra.Fecha));
			return ordenadas;
		}

		/// <summary>
		/// LO QUE NO ESTA: las categorias que se reponen cada tanto y hace demasiado que
		/// no entran a la canasta. Es la pregunta que ninguna pantalla puede renderizar,
		/// porque no es un dato del historial sino una conclusion sobre el historial mas
		/// el calendario. Devuelve solo las vencidas, de la mas atrasada a la menos.
		/// </summary>
		public IReadOnlyList<AbastecimientoPendiente> AbastecimientosPendientes(DateTime fechaDeConsulta)
		{
			var pendientes = new List<AbastecimientoPendiente>();

			foreach (Categoria categoria in Categoria.Todas)
			{
				if (!categoria.SeReabastece) continue;

				// De algo que nunca se compro no se puede decir que falta: no hay ritmo
				// propio con el que comparar.
				if (!AlgunaVezEntro(categoria)) continue;

				DateTime ultima = UltimaVezQueEntro(categoria);
				int dias = (int)(fechaDeConsulta.Date - ultima.Date).TotalDays;
				if (dias < 0) continue;

				var pendiente = new AbastecimientoPendiente(categoria, ultima, dias, RitmoDe(categoria));
				if (pendiente.EstaVencido) pendientes.Add(pendiente);
			}

			pendientes.Sort((una, otra) => otra.DiasDesdeLaUltima.CompareTo(una.DiasDesdeLaUltima));
			return pendientes;
		}

		/// <summary>
		/// CADA CUÁNTO compra ella esta categoría, medido en su propio historial. Total:
		/// sin dos compras devuelve el ritmo Ninguno, que remite al catálogo.
		/// </summary>
		public RitmoDeCompra RitmoDe(Categoria categoria)
		{
			ArgumentNullException.ThrowIfNull(categoria);

			var fechas = new List<DateTime>();
			foreach (Compra compra in Ordenadas())
				foreach (Item item in compra.Items)
					if (ReferenceEquals(item.Categoria, categoria))
					{
						// Una categoria puede aparecer en varias lineas del mismo ticket, y
						// eso es UNA compra: dos intervalos de cero dias no son un ritmo.
						if (fechas.Count == 0 || fechas[fechas.Count - 1].Date != compra.Fecha.Date)
							fechas.Add(compra.Fecha);
						break;
					}

			if (fechas.Count < 2) return RitmoDeCompra.Ninguno(categoria);

			var intervalos = new List<int>();
			for (int i = 1; i < fechas.Count; i++)
			{
				int dias = (int)(fechas[i].Date - fechas[i - 1].Date).TotalDays;
				if (dias > 0) intervalos.Add(dias);
			}

			return intervalos.Count == 0
				? RitmoDeCompra.Ninguno(categoria)
				: RitmoDeCompra.De(categoria, intervalos);
		}

		/// <summary>
		/// CÓMO VA AL SÚPER: cada cuántos días vuelve, qué día suele ir y en qué momento.
		/// Total: sin suficientes compras devuelve el ritmo Ninguno, que lo dice.
		/// </summary>
		public RitmoDelSuper RitmoDelSuper()
		{
			IReadOnlyList<Compra> ordenadas = Ordenadas();
			if (ordenadas.Count < Compras.Dominio.RitmoDelSuper.ComprasQueHacenFalta)
				return Compras.Dominio.RitmoDelSuper.Ninguno(ordenadas.Count);

			var fechas = new List<DateTime>();
			foreach (Compra compra in ordenadas) fechas.Add(compra.Fecha);

			// Todas el mismo dia no da intervalo, y eso no es un ritmo: la lectura lo dice
			// en vez de reventar.
			var distintos = new HashSet<DateTime>();
			foreach (DateTime fecha in fechas) distintos.Add(fecha.Date);
			if (distintos.Count < 2) return Compras.Dominio.RitmoDelSuper.Ninguno(ordenadas.Count);

			return Compras.Dominio.RitmoDelSuper.De(fechas);
		}

		/// <summary>
		/// CÓMO CAMBIÓ EL RITMO de un producto: cada cuánto lo compraba antes del corte y
		/// cada cuánto lo compra después. Total: sin suficientes compras de un lado
		/// devuelve el cambio Ninguno, que lo dice.
		/// </summary>
		public CambioDeRitmo RitmoDeUnProducto(string sku, DateTime corte)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(sku);

			var antes = new List<DateTime>();
			var despues = new List<DateTime>();

			foreach (Compra compra in Ordenadas())
				foreach (Item item in compra.Items)
					if (string.Equals(item.Sku, sku, StringComparison.OrdinalIgnoreCase))
					{
						List<DateTime> lado = compra.Fecha < corte ? antes : despues;

						// Un producto puede venir en varias lineas del mismo ticket, y eso
						// es UNA compra: un intervalo de cero dias no es un ritmo.
						if (lado.Count == 0 || lado[lado.Count - 1].Date != compra.Fecha.Date)
							lado.Add(compra.Fecha);
						break;
					}

			string descripcion = Catalogo.Tiene(sku) ? Catalogo[sku].Descripcion : sku;

			if (antes.Count < CambioDeRitmo.ComprasQueHacenFaltaDeCadaLado
				|| despues.Count < CambioDeRitmo.ComprasQueHacenFaltaDeCadaLado)
				return CambioDeRitmo.Ninguno(sku, descripcion);

			IReadOnlyList<int> intervalosDeAntes = Intervalos(antes);
			IReadOnlyList<int> intervalosDeAhora = Intervalos(despues);

			return intervalosDeAntes.Count == 0 || intervalosDeAhora.Count == 0
				? CambioDeRitmo.Ninguno(sku, descripcion)
				: CambioDeRitmo.De(sku, descripcion, intervalosDeAntes, intervalosDeAhora);
		}

		/// <summary>
		/// Los productos a los que les cambió el ritmo, el más notorio primero. Es la
		/// lectura con la que la app abre su tarjeta de "un cambio en tu rutina".
		/// </summary>
		// SE ORDENA POR CUANTO SALTO LA CADENCIA, no por plata ni por cantidad: la tarjeta
		// contesta "¿que cambio en mi rutina?", y lo que cambia una rutina es la
		// frecuencia. Un producto que paso de una vez cada dos meses a todas las semanas
		// dice mas que uno que subio de precio.
		public IReadOnlyList<CambioDeRitmo> CambiosDeRitmo(DateTime corte)
		{
			var cambios = new List<CambioDeRitmo>();
			foreach (Producto producto in Catalogo.Lista)
			{
				CambioDeRitmo cambio = RitmoDeUnProducto(producto.Sku, corte);
				if (cambio.HayConQueComparar && cambio.Cambio) cambios.Add(cambio);
			}

			cambios.Sort((uno, otro) => Salto(otro).CompareTo(Salto(uno)));
			return cambios;
		}

		// Cuantos escalones del conjunto cerrado salto la cadencia. Es la unica forma de
		// ordenar sin volver a los dias: el conjunto ya esta ordenado y esa es su ventaja.
		private static int Salto(CambioDeRitmo cambio)
		{
			int Peso(Cadencia cadencia)
			{
				for (int i = 0; i < Cadencia.Todas.Length; i++)
					if (ReferenceEquals(Cadencia.Todas[i], cadencia)) return i;

				return 0;
			}

			return Math.Abs(Peso(cambio.Antes) - Peso(cambio.Ahora));
		}

		// Los dias entre fechas consecutivas. Compartido por las dos lecturas de ritmo.
		private static IReadOnlyList<int> Intervalos(IReadOnlyList<DateTime> fechas)
		{
			var intervalos = new List<int>();
			for (int i = 1; i < fechas.Count; i++)
			{
				int dias = (int)(fechas[i].Date - fechas[i - 1].Date).TotalDays;
				if (dias > 0) intervalos.Add(dias);
			}

			return intervalos;
		}

		private bool AlgunaVezEntro(Categoria categoria)
		{
			foreach (Compra compra in compras)
				foreach (Item item in compra.Items)
					if (ReferenceEquals(item.Categoria, categoria)) return true;

			return false;
		}

		private DateTime UltimaVezQueEntro(Categoria categoria)
		{
			DateTime ultima = DateTime.MinValue;
			foreach (Compra compra in compras)
				foreach (Item item in compra.Items)
					if (ReferenceEquals(item.Categoria, categoria) && compra.Fecha > ultima)
						ultima = compra.Fecha;

			if (ultima == DateTime.MinValue)
				throw new InvalidOperationException(
					$"La categoria '{categoria.Nombre}' nunca entro al historial. "
					+ "Preguntar por ella antes es un error de quien llama.");

			return ultima;
		}

		/// <summary>
		/// Las coberturas cuya ventana se esta cerrando: vigentes hoy y con la severidad
		/// que su propio plazo determina, de la mas urgente a la menos.
		/// </summary>
		public IReadOnlyList<RegistroDeGarantia> GarantiasPorVencer(DateTime fechaDeConsulta)
		{
			var porVencer = new List<RegistroDeGarantia>();
			foreach (RegistroDeGarantia registro in GarantiasVigentes(fechaDeConsulta))
				if (registro.Severidad.PideAlgo) porVencer.Add(registro);

			porVencer.Sort((uno, otro) => uno.DiasParaVencer.CompareTo(otro.DiasParaVencer));
			return porVencer;
		}

		/// <summary>Las coberturas que siguen corriendo en la fecha de consulta, con su respaldo.</summary>
		public IReadOnlyList<RegistroDeGarantia> GarantiasVigentes(DateTime fechaDeConsulta)
		{
			var vigentes = new List<RegistroDeGarantia>();
			foreach (Compra compra in compras)
				foreach (Item item in compra.Items)
				{
					if (!item.TieneGarantia) continue;
					if (!item.Garantia.EstaVigenteEl(compra.Fecha, fechaDeConsulta)) continue;

					vigentes.Add(RegistroDeGarantia.De(item.Sku, compra, item, fechaDeConsulta));
				}

			return vigentes;
		}

		private static bool Coincide(Item item, string texto) =>
			item.Sku.IndexOf(texto, StringComparison.OrdinalIgnoreCase) >= 0
			|| item.Descripcion.IndexOf(texto, StringComparison.OrdinalIgnoreCase) >= 0;
	}
}
