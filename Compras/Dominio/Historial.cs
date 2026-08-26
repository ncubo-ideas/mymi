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
