using System;
using System.Collections.Generic;

namespace HolaMundo.Dominio
{
	// UNA FACTURA CON SUS LÍNEAS.
	//
	// El constructor recibe tres listas PARALELAS —descripción, cantidad, precio— y las
	// arma en `Linea`s. Parece incómodo y es a propósito: así es como llegan los datos
	// de un proveedor, y **éste es el único punto donde tres listas sueltas se vuelven
	// una abstracción**.
	//
	// Por eso es el único lugar donde hay que rechazar un evento mal formado. Si las
	// listas no miden lo mismo, no hay factura. Un modelo que aceptara eso pensando
	// "ya lo arreglamos después" arrastra el error para siempre: el journal guarda lo
	// que entró, no lo que quisimos.
	internal class Factura
	{
		private readonly List<Linea> lineas = new List<Linea>();

		public string Numero { get; }
		public DateTime Fecha { get; }

		public IReadOnlyList<Linea> Lineas => lineas;

		public int CantidadDeLineas => lineas.Count;

		public decimal Total
		{
			get
			{
				decimal total = 0m;
				foreach (Linea linea in lineas) total += linea.Total;
				return total;
			}
		}

		public Factura(string numero, DateTime fecha,
			string[] descripciones, int[] cantidades, decimal[] precios)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(numero);
			ArgumentNullException.ThrowIfNull(descripciones);
			ArgumentNullException.ThrowIfNull(cantidades);
			ArgumentNullException.ThrowIfNull(precios);

			if (descripciones.Length == 0)
				throw new ArgumentException("Una factura sin líneas no es una factura.",
					nameof(descripciones));

			if (cantidades.Length != descripciones.Length || precios.Length != descripciones.Length)
				throw new ArgumentException(
					$"Las tres listas de la factura '{numero}' tienen que medir lo mismo. "
					+ $"descripciones={descripciones.Length}, cantidades={cantidades.Length}, "
					+ $"precios={precios.Length}.",
					nameof(descripciones));

			Numero = numero;
			Fecha = fecha;

			for (int i = 0; i < descripciones.Length; i++)
				lineas.Add(new Linea(descripciones[i], cantidades[i], precios[i]));
		}
	}
}
