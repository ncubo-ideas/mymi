using System;
using System.Collections.Generic;

namespace Compras.Dominio
{
	/// <summary>
	/// LO QUE NO FALLA en la canasta de la persona: los productos que vuelven compra tras
	/// compra. "Su semana tiene una firma."
	/// </summary>
	// Es la lectura con la que la app ABRE, y por eso existe: la especificacion de marca
	// pide mostrar EL PATRON y no una lista de facturas -"la lista de recibos existe, pero
	// no es la portada"-. Una lista de tickets la puede pintar cualquiera; la firma sale de
	// cruzar todos.
	//
	// Un producto entra a la firma si aparece en al menos la MITAD de las compras del
	// periodo. El umbral es explicito y no una estadistica dificil de explicar: cuando la
	// app dice "esto es lo suyo", tiene que poder decir por que, y "esta en mas de la mitad
	// de sus compras" se entiende sin curso.
	//
	// Y NO JUZGA. La firma dice que compra, nunca si esta bien: el juicio no es de un
	// supermercado.
	internal abstract class FirmaDeLaCanasta
	{
		/// <summary>La proporción de compras en que un producto tiene que aparecer para ser habitual.</summary>
		internal const decimal ProporcionQueLoVuelveHabitual = 0.5m;

		internal static FirmaDeLaCanasta Ninguna(int comprasMiradas) => new SinFirma(comprasMiradas);

		internal static FirmaDeLaCanasta De(int comprasMiradas, IReadOnlyList<ProductoHabitual> habituales) =>
			new Firma(comprasMiradas, habituales);

		/// <summary>Cuántas compras se miraron para sacarla.</summary>
		internal int ComprasMiradas { get; private set; }

		private FirmaDeLaCanasta(int comprasMiradas)
		{
			if (comprasMiradas < 0)
				throw new ArgumentOutOfRangeException(nameof(comprasMiradas), comprasMiradas,
					"No se pueden mirar menos de cero compras.");

			ComprasMiradas = comprasMiradas;
		}

		/// <summary>Consulta de presencia, total: si hay al menos un producto habitual.</summary>
		internal abstract bool Existe();

		internal abstract IReadOnlyList<ProductoHabitual> Habituales { get; }

		internal abstract int CuantosHabituales { get; }

		/// <summary>
		/// QUE FRASE dice la portada, por su nombre: `sin-compras`, `sin-patron` o `hay`. Los nombres
		/// de los habituales ya se publican, asi que esta lectura no los repite.
		/// </summary>
		internal abstract string Clave { get; }

		private sealed class SinFirma : FirmaDeLaCanasta
		{
			internal SinFirma(int comprasMiradas) : base(comprasMiradas) { }

			internal override bool Existe() => false;
			internal override IReadOnlyList<ProductoHabitual> Habituales => Array.Empty<ProductoHabitual>();
			internal override int CuantosHabituales => 0;

			internal override string Clave => ComprasMiradas == 0 ? "sin-compras" : "sin-patron";
		}

		private sealed class Firma : FirmaDeLaCanasta
		{
			private readonly IReadOnlyList<ProductoHabitual> habituales;

			internal Firma(int comprasMiradas, IReadOnlyList<ProductoHabitual> habituales)
				: base(comprasMiradas)
			{
				ArgumentNullException.ThrowIfNull(habituales);
				if (habituales.Count == 0)
					throw new ArgumentException("Una firma sin habituales no es una firma; eso es Ninguna.",
						nameof(habituales));

				this.habituales = habituales;
			}

			internal override bool Existe() => true;
			internal override IReadOnlyList<ProductoHabitual> Habituales => habituales;
			internal override int CuantosHabituales => habituales.Count;

			internal override string Clave => "hay";
		}
	}

	/// <summary>
	/// Un producto que vuelve: en cuántas de sus compras apareció, y cuál fue la última.
	/// </summary>
	internal class ProductoHabitual
	{
		internal string Sku { get; private set; }
		internal string Descripcion { get; private set; }
		internal int ComprasEnQueAparece { get; private set; }
		internal int ComprasMiradas { get; private set; }
		internal DateTime UltimaVez { get; private set; }

		internal ProductoHabitual(string sku, string descripcion, int comprasEnQueAparece,
			int comprasMiradas, DateTime ultimaVez)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(sku);
			ArgumentNullException.ThrowIfNull(descripcion);
			if (comprasMiradas <= 0)
				throw new ArgumentOutOfRangeException(nameof(comprasMiradas), comprasMiradas,
					"Sin compras miradas no hay habitual que declarar.");

			Sku = sku;
			Descripcion = descripcion;
			ComprasEnQueAparece = comprasEnQueAparece;
			ComprasMiradas = comprasMiradas;
			UltimaVez = ultimaVez;
		}

		/// <summary>En qué proporción de las compras aparece. 1 es en todas.</summary>
		internal decimal Proporcion => Math.Round((decimal)ComprasEnQueAparece / ComprasMiradas, 4);

		/// <summary>Si está en TODAS las compras miradas. Lo que de verdad no falla.</summary>
		internal bool EstaEnTodas => ComprasEnQueAparece == ComprasMiradas;

		/// <summary>Qué tan fiel es, dicho en palabras: "nunca falla", "casi nunca falta".</summary>
		// Va aca y no en la pantalla porque es la MISMA respuesta para el MCP y para la UI, y
		// porque el umbral -cuanto es "casi nunca falta"- es un juicio del dominio.
		internal Constancia Constancia => Constancia.Para(Proporcion);
	}
}
