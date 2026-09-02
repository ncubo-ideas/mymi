using System;
using System.Collections.Generic;

namespace Compras.Dominio
{
	/// <summary>
	/// Un ticket de compra con sus lineas. Recibe las listas paralelas tal como vienen en
	/// el evento del supermercado y las arma en Items: es el unico punto donde seis
	/// listas sueltas se vuelven una abstraccion, y por eso donde se rechaza un evento
	/// mal formado.
	/// </summary>
	internal class Compra
	{
		private readonly List<Item> items = new List<Item>();

		internal string Numero { get; private set; }
		internal DateTime Fecha { get; private set; }

		/// <summary>En qué sucursal fue, o vacío si el tiquete no lo dijo.</summary>
		internal string Sucursal { get; private set; } = "";

		internal IReadOnlyList<Item> Items => items;

		internal int CantidadDeLineas => items.Count;

		internal decimal Total
		{
			get
			{
				decimal total = 0m;
				foreach (Item item in items) total = total + item.Total;
				return total;
			}
		}

		// La forma NUEVA: siete listas, con la presentacion de cada linea. Una compra que
		// no dice como se vende cada cosa no puede contestar cuantos litros entraron.
		internal Compra(string numero, DateTime fecha, string[] skus, string[] descripciones,
			int[] cantidades, decimal[] preciosUnitarios, string[] categorias, int[] mesesDeGarantia,
			string[] presentaciones)
			: this(numero, fecha, skus, descripciones, cantidades, preciosUnitarios, categorias, mesesDeGarantia)
		{
			ArgumentNullException.ThrowIfNull(presentaciones);
			if (presentaciones.Length != skus.Length)
				throw new ArgumentException(
					$"Las presentaciones de la compra '{numero}' deben ser tantas como las lineas. "
					+ $"skus={skus.Length}, presentaciones={presentaciones.Length}.",
					nameof(presentaciones));

			// Se rearman las lineas con su presentacion. El constructor viejo ya valido los
			// largos y los valores, asi que aca solo falta lo que agrega esta forma.
			items.Clear();
			for (int i = 0; i < skus.Length; i++)
				items.Add(new Item(skus[i], descripciones[i], cantidades[i], preciosUnitarios[i],
					Categoria.Desde(categorias[i]), mesesDeGarantia[i], Presentacion.Desde(presentaciones[i])));
		}

		// La forma VIEJA, la que tienen los journals ya escritos. Se conserva a proposito:
		// un historial escrito antes de que existiera la presentacion sigue reproduciendo
		// igual, y sus lineas se leen como piezas sueltas.
		internal Compra(string numero, DateTime fecha, string[] skus, string[] descripciones,
			int[] cantidades, decimal[] preciosUnitarios, string[] categorias, int[] mesesDeGarantia)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(numero);
			ArgumentNullException.ThrowIfNull(skus);
			ArgumentNullException.ThrowIfNull(descripciones);
			ArgumentNullException.ThrowIfNull(cantidades);
			ArgumentNullException.ThrowIfNull(preciosUnitarios);
			ArgumentNullException.ThrowIfNull(categorias);
			ArgumentNullException.ThrowIfNull(mesesDeGarantia);

			if (skus.Length == 0)
				throw new ArgumentException("Una compra sin lineas no es una compra.", nameof(skus));
			if (descripciones.Length != skus.Length || cantidades.Length != skus.Length
				|| preciosUnitarios.Length != skus.Length || categorias.Length != skus.Length
				|| mesesDeGarantia.Length != skus.Length)
				throw new ArgumentException(
					$"Las seis listas de la compra '{numero}' deben tener el mismo largo. skus={skus.Length}, "
					+ $"descripciones={descripciones.Length}, cantidades={cantidades.Length}, "
					+ $"preciosUnitarios={preciosUnitarios.Length}, categorias={categorias.Length}, "
					+ $"mesesDeGarantia={mesesDeGarantia.Length}.",
					nameof(skus));

			Numero = numero;
			Fecha = fecha;

			// El string de categoria se resuelve UNA vez, aca: de este punto para adentro
			// el dominio trabaja con la instancia.
			for (int i = 0; i < skus.Length; i++)
				items.Add(new Item(skus[i], descripciones[i], cantidades[i], preciosUnitarios[i],
					Categoria.Desde(categorias[i]), mesesDeGarantia[i]));
		}

		/// <summary>La misma compra, situada: una copia con la sucursal puesta.</summary>
		// UN WITHER Y NO OTRO CONSTRUCTOR, a proposito. Compra ya tiene dos formas -con y
		// sin presentaciones- y el DSL del journal resuelve por nombre y cantidad de
		// argumentos: una tercera forma con un string en la posicion del string[] seria
		// una ambiguedad esperando turno. El comando nuevo arma la compra como siempre y
		// la situa en una segunda sentencia; los journals viejos ni se enteran.
		internal Compra EnSucursal(string sucursal)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(sucursal);

			// La copia comparte los items (son inmutables); solo cambia donde fue.
			var situada = (Compra)MemberwiseClone();
			situada.Sucursal = sucursal.Trim();
			return situada;
		}

		internal bool EsDelMes(int anio, int mes) => Fecha.Year == anio && Fecha.Month == mes;
	}
}
