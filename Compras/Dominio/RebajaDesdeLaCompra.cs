using System;

namespace Compras.Dominio
{
	/// <summary>
	/// LA BUENA NOTICIA DE UN RECIBO VIEJO: de lo que llevó en esa compra, qué está hoy
	/// más barato que lo que pagó — y cuánto menos, por la presentación de esa línea.
	/// </summary>
	// SOLO BAJADAS, a propósito: es la nota al pie del recibo del mock de Fernanda ("la
	// leche que llevaste esta hoy ₡100 mas barata"), y una nota de subida en un recibo
	// viejo no le sirve a nadie — las subidas ya tienen su propia tarjeta en la portada.
	//
	// UNA SOLA, la mayor: el pie de un recibo es una nota, no otra lista.
	//
	// "HOY" ES LA ULTIMA OBSERVACION del historial, no el reloj: esta lectura solo sabe
	// precios que ella pago de verdad. Y solo cuenta si hay una observacion POSTERIOR a
	// la compra que se mira — el precio de esa misma compra no es noticia de hoy.
	internal class RebajaDesdeLaCompra
	{
		internal static RebajaDesdeLaCompra Ninguna(string numeroDeCompra) =>
			new RebajaDesdeLaCompra(numeroDeCompra, "", "", 0m);

		internal static RebajaDesdeLaCompra De(string numeroDeCompra, string sku,
			string descripcion, decimal cuantoMenos) =>
			new RebajaDesdeLaCompra(numeroDeCompra, sku, descripcion, cuantoMenos);

		private RebajaDesdeLaCompra(string numeroDeCompra, string sku, string descripcion,
			decimal cuantoMenos)
		{
			ArgumentNullException.ThrowIfNull(numeroDeCompra);

			NumeroDeCompra = numeroDeCompra;
			Sku = sku;
			Descripcion = descripcion;
			CuantoMenos = cuantoMenos;
		}

		internal string NumeroDeCompra { get; private set; }

		/// <summary>Consulta total: si algo de esa compra está hoy más barato de verdad.</summary>
		internal bool Hay => CuantoMenos > 0m;

		internal string Sku { get; private set; }
		internal string Descripcion { get; private set; }

		/// <summary>Cuántos colones menos cuesta hoy UNA pieza, en la presentación de esa
		/// línea. Sin rebaja es cero, y Hay lo dice.</summary>
		internal decimal CuantoMenos { get; private set; }

		internal string ParaElAsistente =>
			Hay
				? $"{Descripcion} hoy cuesta {Plata.Colones(CuantoMenos)} menos que en la "
					+ $"compra {NumeroDeCompra}"
				: $"nada de la compra {NumeroDeCompra} está hoy más barato que lo que se pagó";
	}
}
