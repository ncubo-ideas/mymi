using System;
using System.Collections.Generic;

namespace Compras.Dominio
{
	/// <summary>
	/// CUÁNTO CUESTA HOY lo mismo que ella suele llevar, frente a lo que costaba hace
	/// unos meses: una pieza de cada habitual, a los precios de hoy contra los de
	/// entonces, sumada en colones.
	/// </summary>
	// ES UNA SOLA CIFRA A PROPOSITO. La serie completa de cada producto ya existe
	// (PrecioEnElTiempo); lo que esta lectura agrega es la SUMA que ninguna serie da:
	// "comprar hoy lo tuyo te cuesta ₡1.150 mas que hace 3 meses". Viene del mock de
	// Fernanda, donde encabeza la tarjeta de precios.
	//
	// LA VENTANA ES DE MESES Y NO "desde la primera compra", porque las dos preguntas
	// son distintas: la serie entera cuenta la historia ("te subio 15% en dos años");
	// esta compara contra un pasado CERCANO, que es el que se recuerda y contra el que
	// se decide. Los tres meses son un juicio (ver Preguntas.md), no una verdad.
	//
	// SE COMPARA POR UNIDAD DE MEDIDA y se multiplica por el contenido de la
	// presentacion MAS RECIENTE: si paso de la leche de 1 L a la de 2 L, lo honesto es
	// preguntar cuanto costaba hace tres meses ESA cantidad, no la caja vieja.
	internal class CanastaFrenteAlPasado
	{
		/// <summary>La ventana si nadie pide otra: tres meses. Es un juicio, y esta acá
		/// para que ninguna superficie invente el suyo.</summary>
		internal const int VentanaPorDefectoEnMeses = 3;

		internal static CanastaFrenteAlPasado SinConQueComparar(int mesesDeVentana) =>
			new CanastaFrenteAlPasado(mesesDeVentana, Array.Empty<ProductoFrenteAlPasado>());

		internal static CanastaFrenteAlPasado Medida(int mesesDeVentana,
			IReadOnlyList<ProductoFrenteAlPasado> productos) =>
			new CanastaFrenteAlPasado(mesesDeVentana, productos);

		private CanastaFrenteAlPasado(int mesesDeVentana, IReadOnlyList<ProductoFrenteAlPasado> productos)
		{
			ArgumentNullException.ThrowIfNull(productos);
			if (mesesDeVentana <= 0)
				throw new ArgumentOutOfRangeException(nameof(mesesDeVentana), mesesDeVentana,
					"Una ventana de cero meses no compara contra ningún pasado.");

			MesesDeVentana = mesesDeVentana;
			Productos = productos;
		}

		internal int MesesDeVentana { get; private set; }

		/// <summary>Consulta total: si algún habitual tiene precio de entonces Y de hoy.</summary>
		internal bool HayConQueComparar => Productos.Count > 0;

		/// <summary>Los comparados, el que más se movió primero. Cada uno con su antes y
		/// su ahora en colones por SU presentación más reciente.</summary>
		internal IReadOnlyList<ProductoFrenteAlPasado> Productos { get; private set; }

		/// <summary>El HECHO, con signo: cuántos colones más (o menos, negativo) cuesta hoy
		/// una pieza de cada comparado. Sin comparables es cero, y HayConQueComparar lo dice.</summary>
		internal decimal Cambio
		{
			get
			{
				decimal cambio = 0m;
				foreach (ProductoFrenteAlPasado producto in Productos) cambio += producto.Cambio;
				return cambio;
			}
		}

		internal string ParaElAsistente
		{
			get
			{
				if (!HayConQueComparar)
					return $"ningún habitual tiene un precio observado de hace {MesesDeVentana} "
						+ "meses o más: todavía no hay con qué comparar";

				string canasta = $"una pieza de cada uno de sus {Productos.Count} habituales comparables";
				if (Cambio > 0m)
					return $"comprar hoy {canasta} cuesta {Plata.Colones(Cambio)} más que hace "
						+ $"{MesesDeVentana} meses";
				if (Cambio < 0m)
					return $"comprar hoy {canasta} cuesta {Plata.Colones(Math.Abs(Cambio))} menos "
						+ $"que hace {MesesDeVentana} meses";

				return $"comprar hoy {canasta} cuesta lo mismo que hace {MesesDeVentana} meses";
			}
		}
	}

	/// <summary>
	/// Un habitual puesto frente a su propio pasado: lo que costaba entonces y lo que
	/// cuesta hoy, los dos en colones por la presentación más reciente.
	/// </summary>
	internal class ProductoFrenteAlPasado
	{
		internal string Sku { get; private set; }
		internal string Descripcion { get; private set; }

		/// <summary>La presentación contra la que se leen los dos precios: la más reciente.</summary>
		internal string Presentacion { get; private set; }

		internal decimal Antes { get; private set; }
		internal decimal Ahora { get; private set; }

		internal decimal Cambio => Ahora - Antes;

		internal ProductoFrenteAlPasado(string sku, string descripcion, string presentacion,
			decimal antes, decimal ahora)
		{
			ArgumentNullException.ThrowIfNull(sku);
			ArgumentNullException.ThrowIfNull(descripcion);
			ArgumentNullException.ThrowIfNull(presentacion);

			Sku = sku;
			Descripcion = descripcion;
			Presentacion = presentacion;
			Antes = antes;
			Ahora = ahora;
		}
	}
}
