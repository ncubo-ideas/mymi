using System;
using System.Collections.Generic;

namespace Facturas.Dominio
{
	// El agregado: la puerta de acceso a las facturas. Nadie construye una Factura
	// y la guarda por su cuenta; se agrega aca, y aca se rechaza un numero repetido.
	//
	// Se llama Facturario y no Facturas porque el namespace ya es Facturas.Dominio:
	// una clase Facturas dentro de el obliga a desambiguar en cada referencia.
	public class Facturario
	{
		private readonly List<Factura> facturas = new List<Factura>();
		private readonly Dictionary<string, Factura> porNumero =
			new Dictionary<string, Factura>(StringComparer.OrdinalIgnoreCase);

		public IReadOnlyList<Factura> Lista => facturas;

		public int Cantidad => facturas.Count;

		public decimal Total
		{
			get
			{
				decimal total = 0m;
				foreach (Factura factura in facturas) total = total + factura.Total;
				return total;
			}
		}

		// Indexador por numero: es lo que hace valido el DSL `facturas[@numero]`.
		public Factura this[string numero]
		{
			get
			{
				ArgumentNullException.ThrowIfNullOrWhiteSpace(numero);
				if (!porNumero.TryGetValue(numero, out Factura factura))
					throw new KeyNotFoundException($"No hay ninguna factura con numero '{numero}'.");
				return factura;
			}
		}

		public bool Tiene(string numero)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(numero);
			return porNumero.ContainsKey(numero);
		}

		public void Agregar(Factura factura)
		{
			ArgumentNullException.ThrowIfNull(factura);
			if (porNumero.ContainsKey(factura.Numero))
				throw new InvalidOperationException($"La factura '{factura.Numero}' ya estaba registrada.");

			facturas.Add(factura);
			porNumero[factura.Numero] = factura;
		}
	}
}
