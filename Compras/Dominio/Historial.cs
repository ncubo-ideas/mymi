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
		/// El último mes con compras contra el mes INMEDIATAMENTE ANTERIOR. Total: sin
		/// compras devuelve una comparación que contesta que no hay comparación.
		/// </summary>
		// ES OTRA PREGUNTA QUE CompararConElAnioPasado, y las dos hacen falta. Contra el año
		// pasado se ve si la canasta cambio de fondo -las estaciones y los habitos ya
		// dieron la vuelta-; contra el mes anterior se ve si ESTE mes viene distinto, que es
		// lo que alguien pregunta cuando abre la app un martes.
		//
		// La aritmetica del mes anterior vive ACA y no en la pantalla: que enero venga
		// despues de diciembre del año pasado es del calendario, y una superficie que lo
		// resuelva sola lo va a resolver mal en enero.
		public ComparacionDeCanastas CompararConElMesAnterior()
		{
			RangoDeCompras rango = Rango;
			if (!rango.Existe()) return CompararCanastas(1900, 1, 1900, 2);

			int anioAntes = rango.UltimoMes == 1 ? rango.UltimoAnio - 1 : rango.UltimoAnio;
			int mesAntes = rango.UltimoMes == 1 ? 12 : rango.UltimoMes - 1;

			return CompararCanastas(anioAntes, mesAntes, rango.UltimoAnio, rango.UltimoMes);
		}

		/// <summary>
		/// CÓMO SE MOVIERON LOS PRECIOS de los productos que sí lleva: los habituales, cada
		/// uno con su serie. El que más subió primero.
		/// </summary>
		// "DE LOS PRODUCTOS QUE SI LLEVAS", que es lo que el diseño de la app promete y lo
		// que hace util a esta lectura. Una lista de los 45 precios del catalogo es un
		// boletin de inflacion; la de SUS doce habituales es su propia inflacion, que es el
		// dato que ninguna app de super da.
		//
		// Se cruza la firma con la serie de precios en el DOMINIO y no en la pantalla: una
		// superficie que lo hiciera tendria que pedir doce series por separado, y peor,
		// tendria que decidir cuales doce — que es justo el juicio que la firma ya tomo.
		public IReadOnlyList<PrecioEnElTiempo> PreciosDeLosHabituales(int cuantasCompras)
		{
			FirmaDeLaCanasta firma = FirmaDeLasUltimas(cuantasCompras);

			var series = new List<PrecioEnElTiempo>();
			foreach (ProductoHabitual habitual in firma.Habituales)
			{
				PrecioEnElTiempo serie = PrecioDe(habitual.Sku);
				if (serie.Existe() && serie.HaySerie && serie.HayCambioReal) series.Add(serie);
			}

			// El que mas movio primero, en valor absoluto: una bajada de 300 dice tanto como
			// una subida de 300, y quien lee decide que hacer con el signo.
			series.Sort((uno, otro) => Math.Abs(otro.CambioEnProporcion).CompareTo(Math.Abs(uno.CambioEnProporcion)));
			return series;
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

			// LA ULTIMA VEZ y EL TOTAL DE COMPRAS los sabe el agregado, no la serie por mes:
			// la serie agrupa, y agrupar pierde el dia exacto y no sabe cuantas compras hay
			// en total. Se los pasa quien los tiene.
			DateTime ultimaVez = DateTime.MinValue;
			foreach (Compra compra in compras)
				foreach (Item item in compra.Items)
					if (Alcanza(item, termino, esCategoria) && compra.Fecha > ultimaVez)
					{
						ultimaVez = compra.Fecha;
						break;
					}

			return Consumo.De(termino, esCategoria, meses, ultimaVez, compras.Count);
		}

		/// <summary>
		/// EN CUÁLES COMPRAS APARECIÓ ESTO, de la más reciente a la más vieja. El término
		/// puede ser un producto o un departamento entero.
		/// </summary>
		// USA EL MISMO CRITERIO QUE EL CONSUMO -Alcanza- y no uno propio, que es lo que
		// hace que "Limpieza" encuentre el pasillo y "leche" el producto. Dos criterios de
		// busqueda distintos en la misma app es como se llega a que una pantalla encuentre
		// algo que la otra no.
		//
		// TOTAL: un termino que nunca se llevo devuelve Ninguna, no una lista vacia. La
		// diferencia importa porque la pantalla dice cosas distintas -"no hay ninguna
		// compra con «X»" no es lo mismo que "todavia no hay compras"- y una lista vacia no
		// sabe cual de las dos es.
		public ComprasConElProducto ComprasCon(string termino)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(termino);

			bool esCategoria = Categoria.Existe(termino);
			var halladas = new List<CompraConElProducto>();
			var productos = new List<string>();

			// DE LA MAS RECIENTE A LA MAS VIEJA, y el orden se arma al recorrer y no
			// ordenando despues: la lista ya viene en orden de llegada, asi que basta con
			// leerla al reves. Ordenar seria pagar dos veces por algo que ya esta.
			for (int i = compras.Count - 1; i >= 0; i--)
			{
				Compra compra = compras[i];
				CompraConElProducto hallada = null;

				foreach (Item item in compra.Items)
				{
					if (!Alcanza(item, termino, esCategoria)) continue;

					hallada ??= new CompraConElProducto(compra);
					hallada.Sumar(item.Cantidad, item.Total);

					if (!productos.Contains(item.Descripcion)) productos.Add(item.Descripcion);
				}

				if (hallada is not null) halladas.Add(hallada);
			}

			return halladas.Count == 0
				? ComprasConElProducto.Ninguna(termino)
				: ComprasConElProducto.De(termino, halladas, productos);
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
				PrimeraVezPorSku(corte),
				corte);
		}

		/// <summary>
		/// EL MISMO CAMBIO, pero el mes del quiebre LO ENCUENTRA EL DOMINIO. Total: cuando
		/// no hay historia suficiente devuelve un cambio sin con qué comparar, no revienta.
		/// </summary>
		// EXISTE PORQUE RECIBIR EL CORTE ERA UNA TRAMPA. La carátula pasaba el 1 de enero
		// del año corriente, y eso funcionaba por SUERTE: el guion de la demo quiebra en
		// enero. A una persona que cambió en setiembre, esa pantalla no le encontraba nada
		// — y peor, le decía que su canasta venía igual, que es una respuesta falsa y no un
		// hueco visible.
		//
		// COMO SE BUSCA. Se prueba el primero de cada mes y se elige el corte donde MAS
		// PRODUCTOS ENTRAN Y SALEN. Que el maximo caiga en el mes correcto no es casualidad
		// del criterio: si la canasta cambio en enero y se quedo asi, cortar en febrero
		// deja lo nuevo de enero de LOS DOS LADOS y la diferencia se achica. El pico esta
		// donde el contraste es limpio.
		//
		// HACEN FALTA cuantasCompras DE CADA LADO. Con menos, la firma se arma sobre tres o
		// cuatro compras y cualquier cosa que aparezca en dos ya es "habitual": el
		// buscador encontraria quiebres en el ruido, y encima cerca de los extremos, que es
		// donde menos historia hay.
		//
		// EMPATE: gana el mas viejo. Si dos meses explican el cambio igual de bien, el que
		// la persona reconoce es cuando EMPEZO.
		public CambioEnLaCanasta CambioEncontrado(int cuantasCompras)
		{
			if (cuantasCompras <= 0)
				throw new ArgumentOutOfRangeException(nameof(cuantasCompras), cuantasCompras,
					"Mirar cero compras no da un cambio.");

			IReadOnlyList<Compra> ordenadas = Ordenadas();
			CambioEnLaCanasta mejor = null;
			int mejorTamano = 0;

			foreach (DateTime corte in CortesPosibles(ordenadas, cuantasCompras))
			{
				CambioEnLaCanasta candidato = CambioDesde(corte, cuantasCompras);
				if (!candidato.HayCambio) continue;

				int tamano = candidato.Entraron.Count + candidato.Salieron.Count;
				// Estrictamente mayor: los cortes vienen del mas viejo al mas nuevo, asi que
				// no reemplazar en el empate es lo que hace ganar al primero.
				if (tamano > mejorTamano)
				{
					mejor = candidato;
					mejorTamano = tamano;
				}
			}

			// NINGUN MES QUEDO: o no hay historia de los dos lados, o la canasta viene igual
			// en todos los cortes. Las dos son respuestas, y las dos se dicen con el mismo
			// objeto vacio — HayConQueComparar y HayCambio las distinguen.
			return mejor ?? new CambioEnLaCanasta(
				FirmaDeLaCanasta.Ninguna(0),
				FirmaDeLaCanasta.Ninguna(0),
				new Dictionary<string, DateTime>(),
				DateTime.MinValue);
		}

		/// <summary>
		/// La PRIMERA COMPRA de cada mes que tenga alguna, mientras deje al menos
		/// <paramref name="cuantasCompras"/> de cada lado.
		/// </summary>
		// LOS CANDIDATOS SON FECHAS DE COMPRA Y NO PRIMEROS DE MES, y la diferencia se vio
		// probando: con un historial que compra en julio y en setiembre y NADA en agosto,
		// cortar el 1 de agosto y cortar el 1 de setiembre parten igual -julio de un lado,
		// setiembre del otro-, empatan, y gana el mas viejo. La app terminaba diciendo "tu
		// canasta cambio desde 2026-08", un mes en el que la persona no piso el super.
		//
		// Con la fecha de la primera compra del mes, el corte SIEMPRE cae en un mes que
		// existe en su historia, y ademas coincide con MesDelCambioExpresado en vez de
		// contradecirlo por un mes.
		private static IEnumerable<DateTime> CortesPosibles(
			IReadOnlyList<Compra> ordenadas, int cuantasCompras)
		{
			// La ventana util: el corte tiene que dejar cuantasCompras atras y otras tantas
			// adelante, asi que se mueve entre esas dos y no por todo el historial.
			for (int i = cuantasCompras; i <= ordenadas.Count - cuantasCompras; i++)
			{
				DateTime cuando = ordenadas[i].Fecha;
				DateTime anterior = ordenadas[i - 1].Fecha;

				// Solo la PRIMERA de su mes: dos cortes dentro del mismo mes darian el
				// mismo mes en pantalla y multiplicarian el trabajo sin agregar respuestas.
				if (cuando.Year == anterior.Year && cuando.Month == anterior.Month) continue;

				yield return cuando;
			}
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
		// EL PRODUCTO SE NOMBRA, NO SE CODIFICA. Antes esto pedia un sku, y eso obligaba a
		// quien pregunta -sobre todo a la IA- a conocer una llave interna: la especificacion
		// de marca prohibe la palabra "SKU" en lo que ve la persona, y una tool que la EXIGE
		// como argumento es la version peor de lo mismo.
		//
		// Se resuelve con el mismo criterio que BuscarGarantia -sku o parte de la
		// descripcion-, que ya era el precedente del modulo. Y si no encuentra nada, el par
		// total contesta con el texto que se busco, sin reventar.
		public CambioDeRitmo RitmoDeUnProducto(string producto, DateTime corte)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(producto);

			string sku = SkuDe(producto);
			if (sku.Length == 0) return CambioDeRitmo.Ninguno(producto, producto);

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
		/// <summary>
		/// Los mismos cambios de ritmo, pero contra el quiebre QUE ENCUENTRA EL DOMINIO.
		/// </summary>
		// LAS DOS TARJETAS CUENTAN EL MISMO QUIEBRE desde angulos distintos -una dice QUE
		// entro, la otra CADA CUANTO- y por eso tienen que partir por la MISMA fecha. Antes
		// la caratula les pasaba el 1 de enero a las dos, y esa coordinacion vivia en la
		// pantalla: bastaba con que alguien cambiara una llamada para que las dos tarjetas
		// hablaran de meses distintos sin que nada fallara.
		public IReadOnlyList<CambioDeRitmo> CambiosDeRitmoDelQuiebre(int cuantasCompras)
		{
			CambioEnLaCanasta quiebre = CambioEncontrado(cuantasCompras);

			return quiebre.HayConQueComparar
				? CambiosDeRitmo(quiebre.Corte)
				: Array.Empty<CambioDeRitmo>();
		}

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
		/// <summary>
		/// LO QUE PROBABLEMENTE LE VA A FALTAR: los productos con ritmo conocido, medidos
		/// contra cuánto hace que no entran. Del más atrasado al que menos falta.
		/// </summary>
		// LA FECHA DE HOY VIENE RECIBIDA, como en todas las lecturas que miran el calendario:
		// el dominio no lee el reloj. Sin eso, esta lectura daria distinto en cada replay del
		// journal y dejaria de ser reproducible.
		//
		// SE RECORRE EL HISTORIAL UNA VEZ Y SE AGRUPA POR SKU. La alternativa -pedirle a
		// RitmoDeUnProducto el ritmo de cada producto del catalogo- recorreria las 87 compras
		// una vez por producto: con 29 productos son 29 pasadas para armar una lista.
		public ListaParaLaProximaCompra LoQueVaAFaltar(DateTime hoy)
		{
			// Las fechas de compra de cada producto, en orden y sin repetir el mismo dia: un
			// producto en dos lineas del mismo ticket es UNA compra, y un intervalo de cero
			// dias no es un ritmo.
			var fechasPorSku = new Dictionary<string, List<DateTime>>(StringComparer.OrdinalIgnoreCase);

			foreach (Compra compra in Ordenadas())
			{
				var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (Item item in compra.Items)
				{
					if (!vistos.Add(item.Sku)) continue;

					if (!fechasPorSku.TryGetValue(item.Sku, out List<DateTime> fechas))
					{
						fechas = new List<DateTime>();
						fechasPorSku[item.Sku] = fechas;
					}

					if (fechas.Count == 0 || fechas[fechas.Count - 1].Date != compra.Fecha.Date)
						fechas.Add(compra.Fecha);
				}
			}

			var productos = new List<ProductoQueVaAFaltar>();
			foreach (KeyValuePair<string, List<DateTime>> par in fechasPorSku)
			{
				if (par.Value.Count < ListaParaLaProximaCompra.ComprasQueHacenFaltaParaSaberElRitmo)
					continue;

				IReadOnlyList<int> intervalos = Intervalos(par.Value);
				if (intervalos.Count == 0) continue;

				DateTime ultima = par.Value[par.Value.Count - 1];
				int desde = (int)(hoy.Date - ultima.Date).TotalDays;
				// Una compra con fecha posterior a "hoy" no es un error de datos que valga
				// reventar: se cuenta como recien comprada.
				if (desde < 0) desde = 0;

				// LO QUE SE ABANDONO NO ES LO QUE FALTA. Pasados tantos ciclos sin entrar, el
				// producto salio de la rutina y ponerlo en la lista de la proxima compra seria
				// sugerirle a alguien algo que decidio dejar. Esa otra pregunta -"que dejaste
				// de comprar"- la contesta AbastecimientosPendientes.
				int tipicos = Mediana(intervalos);
				if (desde > tipicos * ListaParaLaProximaCompra.CiclosDespuesDeLosCualesYaNoEsFalta)
					continue;

				productos.Add(new ProductoQueVaAFaltar(
					par.Key,
					Catalogo.Tiene(par.Key) ? Catalogo[par.Key].Descripcion : par.Key,
					tipicos,
					ultima,
					desde));
			}

			if (productos.Count == 0) return ListaParaLaProximaCompra.Ninguna;

			// DEL MAS ATRASADO AL QUE MENOS FALTA. A igual atraso gana el de ritmo mas corto:
			// dos dias de mas sobre algo semanal aprieta mas que dos sobre algo mensual.
			productos.Sort((uno, otro) =>
			{
				int porAtraso = otro.DiasDeMas.CompareTo(uno.DiasDeMas);
				return porAtraso != 0 ? porAtraso : uno.DiasTipicos.CompareTo(otro.DiasTipicos);
			});

			return ListaParaLaProximaCompra.De(productos);
		}

		// La mediana de los intervalos, que es lo que resiste una compra corrida. Vive aca
		// porque la usan dos lecturas -el cambio de ritmo y esta lista- y las dos tienen que
		// medir el ritmo igual.
		private static int Mediana(IReadOnlyList<int> valores)
		{
			var ordenados = new List<int>(valores);
			ordenados.Sort();

			int medio = ordenados.Count / 2;
			return ordenados.Count % 2 == 1
				? ordenados[medio]
				: (ordenados[medio - 1] + ordenados[medio]) / 2;
		}

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

		/// <summary>
		/// El sku de un producto nombrado por su sku o por parte de su descripción. Vacío
		/// cuando no aparece en el historial: consulta total, no revienta.
		/// </summary>
		// GANA EL MAS RECIENTE cuando varios coinciden, igual que en BuscarGarantia: si
		// alguien escribe "leche" y compro dos leches distintas, la que le interesa es la
		// que lleva ahora.
		public string SkuDe(string producto)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(producto);

			string hallado = "";
			DateTime masReciente = DateTime.MinValue;

			foreach (Compra compra in compras)
				foreach (Item item in compra.Items)
					if (Coincide(item, producto) && compra.Fecha >= masReciente)
					{
						hallado = item.Sku;
						masReciente = compra.Fecha;
					}

			return hallado;
		}

		private static bool Coincide(Item item, string texto) =>
			item.Sku.IndexOf(texto, StringComparison.OrdinalIgnoreCase) >= 0
			|| item.Descripcion.IndexOf(texto, StringComparison.OrdinalIgnoreCase) >= 0;
	}
}
