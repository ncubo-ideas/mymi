using System;

namespace Presupuesto.Dominio
{
	// LA RESPUESTA A LA PREGUNTA 2: ".vale la pena el alquiler que recibo, comparado con los
	// gastos de esa propiedad?".
	//
	// Devuelve HECHOS -neto, gastos, ingresos, contra que se comparo- y una lectura en una
	// linea. NO devuelve un veredicto de "vendela" ni "quedatela": eso es consejo de
	// inversion, y este dominio no lo da. Capitulo 0 §8: nombrar no es inventar.
	//
	// La comparacion es asimetrica a proposito, y ese es el juicio 7 hecho codigo:
	//
	//   - Contra su propia historia SIEMPRE se puede. No necesita que nadie declare nada.
	//   - Contra una alternativa -un certificado, cripto- solo si la persona la declaro Y
	//     ademas se sabe cuanto vale el bien. Si falta cualquiera de las dos, se dice que no
	//     esta disponible en vez de inventar un porcentaje.
	public sealed class Rendimiento
	{
		private Rendimiento(Bien bien, DateTime desde, DateTime hasta, decimal ingresos,
			decimal gastos, PuntoDeComparacion contra, decimal? porcentajeAnual,
			decimal? rendimientoDeLaAlternativa)
		{
			Bien = bien;
			Desde = desde;
			Hasta = hasta;
			Ingresos = ingresos;
			Gastos = gastos;
			Contra = contra;
			PorcentajeAnual = porcentajeAnual;
			RendimientoDeLaAlternativa = rendimientoDeLaAlternativa;
		}

		public Bien Bien { get; }
		public DateTime Desde { get; }
		public DateTime Hasta { get; }
		public decimal Ingresos { get; }
		public decimal Gastos { get; }
		public decimal Neto => Ingresos - Gastos;
		public PuntoDeComparacion Contra { get; }

		// null cuando no se sabe cuanto vale el bien. Ver HUECO 6 en Bien.
		public decimal? PorcentajeAnual { get; }

		public decimal? RendimientoDeLaAlternativa { get; }

		public bool SePuedeComparar => PorcentajeAnual.HasValue && RendimientoDeLaAlternativa.HasValue;

		// El caso que la entrevista nombro primero: "para alguien podria ser rentable
		// solamente con que le de para pagar los gastos". Se contesta sin saber cuanto vale
		// el bien, y por eso vale la pena tenerlo aparte.
		public bool SePagaSola => Neto >= 0;

		public string Lectura
		{
			get
			{
				string tramo = $"entre {Desde:dd/MM/yyyy} y {Hasta:dd/MM/yyyy}";
				string plata = SePagaSola
					? $"{Bien.Nombre} dejo {Neto} netos {tramo}"
					: $"{Bien.Nombre} costo {-Neto} mas de lo que produjo {tramo}";

				if (!SePuedeComparar)
					return $"{plata}. No se puede expresar en porcentaje: falta saber cuanto vale el bien.";

				return $"{plata}, que es un {PorcentajeAnual:P1} anual, contra "
					+ $"{RendimientoDeLaAlternativa:P1} de {Contra.Nombre}.";
			}
		}

		public static Rendimiento De(Bien bien, DateTime desde, DateTime hasta, PuntoDeComparacion contra)
		{
			if (bien == null) throw new ArgumentNullException(nameof(bien));
			if (contra == null) throw new ArgumentNullException(nameof(contra));
			if (hasta.Date < desde.Date)
				throw new ArgumentException("El periodo termina antes de empezar.", nameof(hasta));

			decimal ingresos = bien.IngresosEntre(desde, hasta);
			decimal gastos = bien.GastosEntre(desde, hasta);
			decimal neto = ingresos - gastos;

			decimal? porcentaje = null;
			decimal? alternativa = null;

			if (bien.SeSabeCuantoVale)
			{
				decimal anos = (decimal)(hasta.Date - desde.Date).TotalDays / 365m;
				if (anos > 0)
				{
					porcentaje = neto / bien.ValorEstimado / anos;
					if (!contra.EstaSiempreDisponible)
						alternativa = contra.RendimientoAnualEsperado;
				}
			}

			return new Rendimiento(bien, desde.Date, hasta.Date, ingresos, gastos, contra,
				porcentaje, alternativa);
		}
	}
}
