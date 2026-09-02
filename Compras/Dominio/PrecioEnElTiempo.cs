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
	internal abstract class PrecioEnElTiempo
	{
		// Cuanto tiene que moverse para llamarlo un cambio real. Un 3% es redondeo de
		// gondola; mas que eso lo siente quien paga. PUBLICO porque es UN SOLO juicio:
		// la rebaja de un recibo (RebajaDesdeLaCompra) juzga con este mismo numero.
		internal const decimal CambioQueCuentaComoReal = 0.03m;

		internal static PrecioEnElTiempo Ninguno(string sku) => new SinPrecios(sku);

		internal static PrecioEnElTiempo De(Producto producto, IReadOnlyList<PrecioObservado> observados) =>
			new Serie(producto, observados);

		internal string Sku { get; private set; }

		private PrecioEnElTiempo(string sku)
		{
			ArgumentNullException.ThrowIfNull(sku);
			Sku = sku;
		}

		/// <summary>Consulta de presencia, total: si hay al menos un precio observado.</summary>
		internal abstract bool Existe();

		/// <summary>Si hay DOS o más precios: sin eso no hay cambio que leer.</summary>
		internal abstract bool HaySerie { get; }

		internal abstract string Descripcion { get; }
		internal abstract IReadOnlyList<PrecioObservado> Observados { get; }
		internal abstract int VecesQueLoCompro { get; }

		internal abstract decimal PrimerPrecioPorUnidad { get; }
		internal abstract decimal UltimoPrecioPorUnidad { get; }
		internal abstract string UnidadDeReferencia { get; }

		/// <summary>El HECHO, con signo: cuánto cambió el precio por unidad de medida.</summary>
		internal abstract decimal Cambio { get; }

		/// <summary>El cambio en proporción del primer precio. 0,18 es un 18% más caro.</summary>
		internal abstract decimal CambioEnProporcion { get; }

		/// <summary>Si el precio se movio lo bastante como para no ser redondeo de góndola.</summary>
		internal abstract bool HayCambioReal { get; }

		/// <summary>La dirección del cambio. Preguntar HayCambioReal primero.</summary>
		internal abstract bool Subio { get; }

		/// <summary>La lectura en una línea, escrita por el dominio con SUS hechos.</summary>
		internal abstract string ParaElAsistente { get; }

		private sealed class SinPrecios : PrecioEnElTiempo
		{
			internal SinPrecios(string sku) : base(sku) { }

			internal override bool Existe() => false;
			internal override bool HaySerie => false;
			internal override string Descripcion => "";
			internal override IReadOnlyList<PrecioObservado> Observados => Array.Empty<PrecioObservado>();
			internal override int VecesQueLoCompro => 0;
			internal override decimal PrimerPrecioPorUnidad => 0m;
			internal override decimal UltimoPrecioPorUnidad => 0m;
			internal override string UnidadDeReferencia => "";
			internal override decimal Cambio => 0m;
			internal override decimal CambioEnProporcion => 0m;
			internal override bool HayCambioReal => false;

			internal override bool Subio =>
				throw new InvalidOperationException(
					$"No hay compras de «{Sku}»: no hay dirección que dar. Preguntar HayCambioReal antes.");

			internal override string ParaElAsistente => $"no hay compras de «{Sku}» en el historial";
		}

		private sealed class Serie : PrecioEnElTiempo
		{
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

			internal override bool Existe() => true;
			internal override bool HaySerie => observados.Count >= 2;
			internal override string Descripcion => producto.Descripcion;
			internal override IReadOnlyList<PrecioObservado> Observados => observados;
			internal override int VecesQueLoCompro => observados.Count;

			internal override decimal PrimerPrecioPorUnidad => observados[0].PrecioPorUnidad;
			internal override decimal UltimoPrecioPorUnidad => observados[observados.Count - 1].PrecioPorUnidad;
			internal override string UnidadDeReferencia => producto.Presentacion.UnidadBase.Simbolo;

			internal override decimal Cambio => HaySerie ? UltimoPrecioPorUnidad - PrimerPrecioPorUnidad : 0m;

			internal override decimal CambioEnProporcion =>
				HaySerie && PrimerPrecioPorUnidad != 0m
					? Math.Round(Cambio / PrimerPrecioPorUnidad, 4)
					: 0m;

			internal override bool HayCambioReal =>
				HaySerie && Math.Abs(CambioEnProporcion) >= CambioQueCuentaComoReal;

			internal override bool Subio
			{
				get
				{
					if (!HayCambioReal)
						throw new InvalidOperationException(
							$"El precio de «{Sku}» no se movió lo suficiente como para darle una "
							+ "dirección. Preguntar HayCambioReal antes.");

					return Cambio > 0m;
				}
			}

			internal override string ParaElAsistente
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

					if (HayCambioReal)
						return Subio
							? $"{Descripcion} te subió {Porcentaje(CambioEnProporcion)}: {tramo}"
							: $"{Descripcion} te bajó {Porcentaje(Math.Abs(CambioEnProporcion))}: {tramo}";

					return $"{Descripcion} se mantuvo {tramo}";
				}
			}

			private static string Colones(decimal monto) =>
				Plata.Colones(monto);

			private static string Porcentaje(decimal proporcion) =>
				(proporcion * 100m).ToString("0.#", CultureInfo.InvariantCulture) + "%";
		}
	}

	/// <summary>
	/// Un precio que la persona pagó de verdad, en una compra concreta: la fecha, el
	/// precio de línea y —el que importa— el precio por unidad de medida.
	/// </summary>
	internal class PrecioObservado
	{
		internal DateTime Fecha { get; private set; }
		internal string NumeroDeCompra { get; private set; }
		internal decimal PrecioDeLinea { get; private set; }
		internal decimal PrecioPorUnidad { get; private set; }
		internal string PresentacionExpresada { get; private set; }

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

		internal string MesExpresado => $"{Fecha.Year:0000}-{Fecha.Month:00}";
	}
}
