using System;

namespace Compras.Dominio
{
	/// <summary>
	/// La unidad en la que un supermercado vende algo. Es el "qué" de "3 qué": sin ella un
	/// 3 no significa nada, y dos cantidades en unidades distintas no se pueden comparar
	/// ni sumar.
	/// </summary>
	// Conjunto CERRADO comparado por referencia: un string "kg" suelto admitiria "Kg",
	// "KG" y "kilo" como tres unidades para lo mismo.
	//
	// Cada unidad conoce su FAMILIA -peso, volumen o conteo- y cuanto vale en la unidad
	// base de esa familia. Eso es lo que permite decir que dos mantecas de 500 g son un
	// kilo, y comparar el precio por litro de una leche de 1 L con uno de 2 L.
	//
	// Comparar entre familias no tiene respuesta: un litro no es mas ni menos que un kilo.
	// Por eso EsComparableCon existe y ALaBase de otra familia no se intenta.
	internal sealed class UnidadDeVenta
	{
		internal static readonly UnidadDeVenta Unidad = new UnidadDeVenta("u", Familia.Conteo, 1m);
		internal static readonly UnidadDeVenta Gramo = new UnidadDeVenta("g", Familia.Peso, 0.001m);
		internal static readonly UnidadDeVenta Kilogramo = new UnidadDeVenta("kg", Familia.Peso, 1m);
		internal static readonly UnidadDeVenta Mililitro = new UnidadDeVenta("ml", Familia.Volumen, 0.001m);
		internal static readonly UnidadDeVenta Litro = new UnidadDeVenta("L", Familia.Volumen, 1m);

		private enum Familia { Conteo, Peso, Volumen }

		private readonly Familia familia;
		private readonly decimal enLaBase;

		internal string Simbolo { get; private set; }

		private UnidadDeVenta(string simbolo, Familia familia, decimal enLaBase)
		{
			Simbolo = simbolo;
			this.familia = familia;
			this.enLaBase = enLaBase;
		}

		/// <summary>La unidad en la que se expresa el total de su familia: u, kg o L.</summary>
		internal UnidadDeVenta Base => familia switch
		{
			Familia.Peso => Kilogramo,
			Familia.Volumen => Litro,
			_ => Unidad,
		};

		/// <summary>La misma unidad solo si es el mismo objeto: es lo que garantiza el conjunto cerrado.</summary>
		internal bool EsLaMisma(UnidadDeVenta otra)
		{
			ArgumentNullException.ThrowIfNull(otra);
			return ReferenceEquals(this, otra);
		}

		/// <summary>Si dos cantidades se pueden sumar o comparar: hace falta la misma familia.</summary>
		internal bool EsComparableCon(UnidadDeVenta otra)
		{
			ArgumentNullException.ThrowIfNull(otra);
			return familia == otra.familia;
		}

		/// <summary>Cuanto es esta cantidad en la unidad base de su familia. 500 g son 0,5 kg.</summary>
		internal decimal ALaBase(decimal cantidad) => cantidad * enLaBase;

		/// <summary>El resolver de la frontera: un simbolo desconocido es un evento mal formado.</summary>
		internal static UnidadDeVenta PorSimbolo(string simbolo)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(simbolo);

			foreach (UnidadDeVenta unidad in Todas)
				if (string.Equals(unidad.Simbolo, simbolo.Trim(), StringComparison.OrdinalIgnoreCase))
					return unidad;

			throw new ArgumentException(
				$"'{simbolo}' no es una unidad de venta conocida. Las validas son: u, g, kg, ml, L.",
				nameof(simbolo));
		}

		/// <summary>Reconocimiento total: la pareja de PorSimbolo.</summary>
		internal static bool Existe(string simbolo)
		{
			if (string.IsNullOrWhiteSpace(simbolo)) return false;

			foreach (UnidadDeVenta unidad in Todas)
				if (string.Equals(unidad.Simbolo, simbolo.Trim(), StringComparison.OrdinalIgnoreCase))
					return true;

			return false;
		}

		internal static readonly UnidadDeVenta[] Todas = { Unidad, Gramo, Kilogramo, Mililitro, Litro };
	}
}
