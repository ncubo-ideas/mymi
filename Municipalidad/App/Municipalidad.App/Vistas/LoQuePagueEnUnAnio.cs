using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Municipalidad.App.Vistas
{
	/// <summary>
	/// Lo que esta persona pago en un anio y la serie entera de comprobantes de cada rubro. El total
	/// trae la mora adentro, porque es lo que pago de verdad.
	/// <para>
	/// Es la unica pantalla con un parametro: el anio. Las series vienen enteras y el anio no las
	/// recorta, a proposito, para que se vea de que anios si hay comprobantes.
	/// </para>
	/// </summary>
	public sealed class LoQuePagueEnUnAnio
	{
		private LoQuePagueEnUnAnio() { }

		/// <summary>El anio con que se contesto el total.</summary>
		public int Anio { get; private init; }

		/// <summary>true cuando la persona eligio el anio; false cuando salio del reloj de la pantalla.</summary>
		public bool ElAnioLoEligioLaPersona { get; private init; }

		/// <summary>Lo pagado en ese anio segun los comprobantes que esta app vio, mora incluida.</summary>
		public decimal TotalDelAnio { get; private init; }

		/// <summary>Una serie por rubro del catalogo, entera y sin recortar por el anio.</summary>
		public IReadOnlyList<LaSerieDeUnRubro> Series { get; private init; } =
			new List<LaSerieDeUnRubro>();

		/// <summary>
		/// Los anios de los que esta app tiene algun comprobante, de mayor a menor, mas el anio en
		/// curso. Salen de los periodos que el registro ya escribio y no de ningun calculo propio.
		/// </summary>
		public IReadOnlyList<int> AniosQueSePuedenPedir { get; private init; } = new List<int>();

		/// <summary>true cuando ningun rubro trae comprobantes: no hay serie ni total que valga leer.</summary>
		public bool HayAlgunComprobante { get; private init; }

		/// <summary>El anio pedido no aparece entre los que tienen comprobantes.</summary>
		public bool ElAnioPedidoNoTieneComprobantes { get; private init; }

		/// <summary>La serie de cada rubro y el total del anio, leidos de una sola pasada.</summary>
		public static LoQuePagueEnUnAnio LeerPor(MunicipalidadAPI api, int elAnio, bool loEligioLaPersona,
			Textos textos)
		{
			ArgumentNullException.ThrowIfNull(api);

			decimal total = api.LoPagadoEn(elAnio);

			var series = new List<LaSerieDeUnRubro>();
			var anios = new SortedSet<int>();

			foreach (string rubro in api.LosRubrosQueHay())
			{
				RegistroDelHistorial historial = api.HistorialDe(rubro, textos.Idioma);

				var lineas = new List<UnaLineaDeComprobante>();

				for (int fila = 0; fila < historial.Periodo.Length; fila++)
				{
					lineas.Add(new UnaLineaDeComprobante
					{
						Periodo = historial.Periodo[fila],
						Monto = historial.Monto[fila],
						Mora = historial.Mora[fila],
						Cubre = historial.Cubre[fila],
						PorPeriodo = historial.PorPeriodo[fila],
						Inferido = historial.Inferido[fila],
					});

					int anioDeLaLinea = ElAnioQueEmpiezaElPeriodo(historial.Periodo[fila]);
					if (anioDeLaLinea > 0) anios.Add(anioDeLaLinea);
				}

				series.Add(new LaSerieDeUnRubro
				{
					Rubro = rubro,
					Estado = historial.Estado,
					Dice = historial.EstadoDicho,
					Lineas = lineas,
				});
			}

			bool hayComprobantes = anios.Count > 0;

			var pedibles = new List<int>(anios) { elAnio };

			return new LoQuePagueEnUnAnio
			{
				Anio = elAnio,
				ElAnioLoEligioLaPersona = loEligioLaPersona,
				TotalDelAnio = total,
				Series = series,
				HayAlgunComprobante = hayComprobantes,
				ElAnioPedidoNoTieneComprobantes = hayComprobantes && !anios.Contains(elAnio),
				AniosQueSePuedenPedir = OrdenadosDeMayorAMenorSinRepetir(pedibles),
			};
		}

		/// <summary>
		/// El anio con que empieza la etiqueta de un periodo, que los dos alfabetos escriben igual:
		/// 2026-03 y 2026-T1 empiezan los dos con el anio. Cero cuando la etiqueta no lo trae.
		/// </summary>
		private static int ElAnioQueEmpiezaElPeriodo(string periodo)
		{
			if (periodo is null || periodo.Length < 4) return 0;

			return int.TryParse(
				periodo.AsSpan(0, 4), System.Globalization.NumberStyles.None,
				System.Globalization.CultureInfo.InvariantCulture, out int anio)
				? anio
				: 0;
		}

		private static List<int> OrdenadosDeMayorAMenorSinRepetir(List<int> anios)
		{
			var vistos = new SortedSet<int>(anios);

			var bajando = new List<int>(vistos);
			bajando.Reverse();

			return bajando;
		}
	}

	/// <summary>La serie de comprobantes de un rubro, o la razon por la que no trae ninguna.</summary>
	public sealed class LaSerieDeUnRubro
	{
		public string Rubro { get; init; } = "";

		/// <summary>'con-historia', 'sin-historia' o 'rubro-desconocido'. Lo dijo el registro.</summary>
		public string Estado { get; init; } = "";

		public string Dice { get; init; } = "";

		/// <summary>De la mas vieja a la mas reciente. Vacia con 'sin-historia' es una respuesta.</summary>
		public IReadOnlyList<UnaLineaDeComprobante> Lineas { get; init; } =
			new List<UnaLineaDeComprobante>();
	}

	/// <summary>Una linea de comprobante, con cuantos periodos cubre y cuanto sale cada uno.</summary>
	public sealed class UnaLineaDeComprobante
	{
		/// <summary>El periodo mas reciente que esta linea cubre, en el alfabeto de su rubro.</summary>
		public string Periodo { get; init; } = "";

		/// <summary>Lo que costo la linea entera, cubra un periodo o varios.</summary>
		public decimal Monto { get; init; }

		/// <summary>La mora que venia en el recibo. Es un hecho recibido y esta app no la recalcula.</summary>
		public decimal Mora { get; init; }

		public int Cubre { get; init; }

		/// <summary>Lo que sale UN periodo. Es la columna con que se comparan dos epocas.</summary>
		public decimal PorPeriodo { get; init; }

		/// <summary>true cuando esta app DEDUJO cuantos periodos cubre: es una suposicion suya.</summary>
		public bool Inferido { get; init; }
	}
}
