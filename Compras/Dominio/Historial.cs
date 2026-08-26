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

				var pendiente = new AbastecimientoPendiente(categoria, ultima, dias);
				if (pendiente.EstaVencido) pendientes.Add(pendiente);
			}

			pendientes.Sort((una, otra) => otra.DiasDesdeLaUltima.CompareTo(una.DiasDesdeLaUltima));
			return pendientes;
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
