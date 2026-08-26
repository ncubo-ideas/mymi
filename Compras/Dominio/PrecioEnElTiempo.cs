using System;
using System.Collections.Generic;
using System.Globalization;

namespace Compras.Dominio
{
	/// <summary>
	/// CÓMO LE CAMBIÓ EL PRECIO a la persona: lo que ELLA pagó por la misma cosa a lo
	/// largo del tiempo. No es la inflación del país ni la lista del supermercado — es su
	/// propia serie de precios, que nadie más tiene.
	/// </summary>
	// Se compara SIEMPRE por unidad de medida, nunca por linea. Es lo unico honesto: si
	// un mes compro la leche de 1 L a 950 y al siguiente la de 2 L a 1.800, el precio de
	// linea "subio" 850 y el precio por litro BAJO 50. Sin la presentacion esta lectura
	// mentiria en cada cambio de formato, que es justo cuando la persona mas quiere saber.
	//
	// Total: un sku con una sola compra no tiene serie de precios, y lo dice.
	public abstract class PrecioEnElTiempo
	{
		internal static PrecioEnElTiempo Ninguno(string sku) => new SinPrecios(sku);

		internal static PrecioEnElTiempo De(Producto producto, IReadOnlyList<PrecioObservado> observados) =>
			new Serie(producto, observados);

		public string Sku { get; private set; }

		private PrecioEnElTiempo(string sku)
		{
			ArgumentNullException.ThrowIfNull(sku);
			Sku = sku;
		}

		/// <summary>Consulta de presencia, total: si hay al menos un precio observado.</summary>
		public abstract bool Existe();

		/// <summary>Si hay DOS o más precios: sin eso no hay cambio que leer.</summary>
		public abstract bool HaySerie { get; }

		public abstract string Descripcion { get; }
		public abstract IReadOnlyList<PrecioObservado> Observados { get; }
		public abstract int VecesQueLoCompro { get; }

		public abstract decimal PrimerPrecioPorUnidad { get; }
		public abstract decimal UltimoPrecioPorUnidad { get; }
		public abstract string UnidadDeReferencia { get; }

		/// <summary>El HECHO, con signo: cuánto cambió el precio por unidad de medida.</summary>
		public abstract decimal Cambio { get; }

		/// <summary>El cambio en proporción del primer precio. 0,18 es un 18% más caro.</summary>
		public abstract decimal CambioEnProporcion { get; }

		/// <summary>Qué SIGNIFICA el cambio. Que un precio suba nunca es mejorar para quien paga.</summary>
		public abstract Veredicto Veredicto { get; }

		public string NombreDelVeredicto => Veredicto.Nombre;

		/// <summary>La lectura en una línea, escrita por el dominio con SUS hechos.</summary>
		public abstract string ParaSonia { get; }

		private sealed class SinPrecios : PrecioEnElTiempo
		{
			internal SinPrecios(string sku) : base(sku) { }

			public override bool Existe() => false;
			public override bool HaySerie => false;
			public override string Descripcion => "";
			public override IReadOnlyList<PrecioObservado> Observados => Array.Empty<PrecioObservado>();
			public override int VecesQueLoCompro => 0;
			public override decimal PrimerPrecioPorUnidad => 0m;
			public override decimal UltimoPrecioPorUnidad => 0m;
			public override string UnidadDeReferencia => "";
			public override decimal Cambio => 0m;
			public override decimal CambioEnProporcion => 0m;
			public override Veredicto Veredicto => Veredicto.Indeterminado;

			public override string ParaSonia => $"no hay compras de «{Sku}» en el historial";
		}

		private sealed class Serie : PrecioEnElTiempo
		{
			// Cuanto tiene que moverse para llamarlo un cambio real. Un 3% es redondeo de
			// gondola; mas que eso lo siente quien paga.
			private const decimal CambioQueCuentaComoReal = 0.03m;

			private readonly Producto producto;
			private readonly IReadOnlyList<PrecioObservado> observados;

			internal Serie(Producto producto, IReadOnlyList<PrecioObservado> observados)
				: base(SkuDe(producto))
			{
				ArgumentNullException.ThrowIfNull(observados);
				if (observados.Count == 0)
					throw new ArgumentException("Una serie de precios sin observaciones no es una serie.",
						nameof(observados));

				this.producto = producto;
				this.observados = observados;
			}

			// El sku sale del producto, asi que hay que validarlo ANTES de llamar al base.
			private static string SkuDe(Producto producto)
			{
				ArgumentNullException.ThrowIfNull(producto);
				return producto.Sku;
			}

			public override bool Existe() => true;
			public override bool HaySerie => observados.Count >= 2;
			public override string Descripcion => producto.Descripcion;
			public override IReadOnlyList<PrecioObservado> Observados => observados;
			public override int VecesQueLoCompro => observados.Count;

			public override decimal PrimerPrecioPorUnidad => observados[0].PrecioPorUnidad;
			public override decimal UltimoPrecioPorUnidad => observados[observados.Count - 1].PrecioPorUnidad;
			public override string UnidadDeReferencia => producto.Presentacion.UnidadBase.Simbolo;

			public override decimal Cambio => HaySerie ? UltimoPrecioPorUnidad - PrimerPrecioPorUnidad : 0m;

			public override decimal CambioEnProporcion =>
				HaySerie && PrimerPrecioPorUnidad != 0m
					? Math.Round(Cambio / PrimerPrecioPorUnidad, 4)
					: 0m;

			// Que un precio SUBA nunca es mejorar para quien paga: la direccion deseable de
			// un precio es una sola, y por eso no hace falta consultarle nada al producto.
			public override Veredicto Veredicto
			{
				get
				{
					if (!HaySerie) return Veredicto.Indeterminado;
					if (Math.Abs(CambioEnProporcion) < CambioQueCuentaComoReal) return Veredicto.Estable;
					return Cambio > 0m ? Veredicto.Empeorando : Veredicto.Mejorando;
				}
			}

			public override string ParaSonia
			{
				get
				{
					string unidad = UnidadDeReferencia;
					string desde = observados[0].MesExpresado;
					string hasta = observados[observados.Count - 1].MesExpresado;

					if (!HaySerie)
						return $"{Descripcion} lo compraste una sola vez, en {desde}, a "
							+ $"{Colones(UltimoPrecioPorUnidad)} por {unidad}: todavía no hay con qué comparar";

					string tramo = $"de {Colones(PrimerPrecioPorUnidad)} a {Colones(UltimoPrecioPorUnidad)} "
						+ $"por {unidad}, entre {desde} y {hasta}";

					if (Veredicto.EsEmpeorando)
						return $"{Descripcion} te subió {Porcentaje(CambioEnProporcion)}: {tramo}";
					if (Veredicto.EsMejorando)
						return $"{Descripcion} te bajó {Porcentaje(Math.Abs(CambioEnProporcion))}: {tramo}";

					return $"{Descripcion} se mantuvo {tramo}";
				}
			}

			private static string Colones(decimal monto) =>
				"₡" + monto.ToString("N0", CultureInfo.InvariantCulture);

			private static string Porcentaje(decimal proporcion) =>
				(proporcion * 100m).ToString("0.#", CultureInfo.InvariantCulture) + "%";
		}
	}

	/// <summary>
	/// Un precio que la persona pagó de verdad, en una compra concreta: la fecha, el
	/// precio de línea y —el que importa— el precio por unidad de medida.
	/// </summary>
	public class PrecioObservado
	{
		public DateTime Fecha { get; private set; }
		public string NumeroDeCompra { get; private set; }
		public decimal PrecioDeLinea { get; private set; }
		public decimal PrecioPorUnidad { get; private set; }
		public string PresentacionExpresada { get; private set; }

		internal PrecioObservado(DateTime fecha, string numeroDeCompra, decimal precioDeLinea,
			decimal precioPorUnidad, string presentacionExpresada)
		{
			ArgumentNullException.ThrowIfNull(numeroDeCompra);
			ArgumentNullException.ThrowIfNull(presentacionExpresada);

			Fecha = fecha;
			NumeroDeCompra = numeroDeCompra;
			PrecioDeLinea = precioDeLinea;
			PrecioPorUnidad = precioPorUnidad;
			PresentacionExpresada = presentacionExpresada;
		}

		public string MesExpresado => $"{Fecha.Year:0000}-{Fecha.Month:00}";
	}
}
