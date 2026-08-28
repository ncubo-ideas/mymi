using System;
using System.Collections.Generic;

namespace HolaMundo.Dominio
{
	// LA RAÍZ: el libro de facturas. Es la puerta — nadie construye una `Factura` y la
	// guarda por su cuenta. Se agrega acá, y acá se rechaza un número repetido.
	//
	// Tres cosas de esta clase valen para cualquier dominio:
	//
	// 1. **Se llama `Libro` y no `Facturas`.** La raíz NUNCA se llama como el negocio:
	//    el namespace, el proyecto y el assembly ya se llaman así, y una clase con el
	//    mismo nombre obliga a desambiguar en cada referencia. Los otros negocios usan
	//    `Expediente`, `Historial`, `Panorama`.
	//
	// 2. **El indexador es lo que hace válido `libro[@numero]` en un script.** El motor
	//    lee los miembros de esta clase por reflexión. El DSL no inventa nada: sólo
	//    nombra lo que el modelo ya sabe hacer.
	//
	// 3. **`PuedeAgregar` está en positivo, y eso no es estilo.** Es la pregunta que el
	//    `Check` necesita hacer antes de escribir, escrita tal como se pregunta. Y es
	//    TOTAL: contesta para cualquier entrada sin reventar. Un modelo que sólo sabe
	//    tirar excepciones no se puede consultar antes de tiempo — y sin eso, la regla 1
	//    del substrato no se puede cumplir.
	internal class Libro
	{
		private readonly List<Factura> facturas = new List<Factura>();
		private readonly Dictionary<string, Factura> porNumero =
			new Dictionary<string, Factura>(StringComparer.OrdinalIgnoreCase);

		public IReadOnlyList<Factura> Facturas => facturas;

		public int Cantidad => facturas.Count;

		public decimal Total
		{
			get
			{
				decimal total = 0m;
				foreach (Factura factura in facturas) total += factura.Total;
				return total;
			}
		}

		public Factura this[string numero]
		{
			get
			{
				ArgumentException.ThrowIfNullOrWhiteSpace(numero);
				if (!porNumero.TryGetValue(numero, out Factura factura))
					throw new KeyNotFoundException($"No hay ninguna factura con número '{numero}'.");
				return factura;
			}
		}

		public bool PuedeAgregar(string numero)
		{
			return !string.IsNullOrWhiteSpace(numero) && !porNumero.ContainsKey(numero);
		}

		public void Agregar(Factura factura)
		{
			ArgumentNullException.ThrowIfNull(factura);
			if (!PuedeAgregar(factura.Numero))
				throw new InvalidOperationException(
					$"La factura '{factura.Numero}' ya estaba en el libro.");

			facturas.Add(factura);
			porNumero[factura.Numero] = factura;
		}
	}
}
