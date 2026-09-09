using System;

namespace Municipalidad.Dominio
{
	/// <summary>
	/// La explicacion de un salto. Es lo unico que esta app agrega y el portal no dice: el portal
	/// muestra el monto nuevo, no por que cambio. La respuesta depende de la LEY DE CALCULO del rubro,
	/// asi que el mismo salto es tarifa nueva en un rubro por unidad y avaluo nuevo en uno sobre base
	/// imponible.
	/// <para>
	/// Los umbrales — que tan grande es un salto, que tan chico es ruido — entran POR PARAMETRO y no
	/// viven aca: estan propuestos y sin firmar. El dominio se queda con la forma y hace la comparacion.
	/// </para>
	/// <para>
	/// Compara MONTO POR PERIODO y no monto crudo, porque una linea de tres meses contra una de uno
	/// daria un salto falso. Y el orden de las preguntas es fijo: ruido, umbral, ley.
	/// </para>
	/// <para>
	/// Tres hojas y no una con un estado: dos explican el salto, y la tercera es lo que no llega a
	/// explicar nada.
	/// </para>
	/// </summary>
	internal abstract class Variacion
	{
		internal Rubro Rubro { get; }
		internal Periodo PeriodoAnterior { get; }
		internal Periodo PeriodoActual { get; }
		internal decimal MontoAnteriorPorPeriodo { get; }
		internal decimal MontoActualPorPeriodo { get; }

		protected Variacion(LineaPagada anterior, LineaPagada actual)
		{
			Rubro = actual.Rubro;
			PeriodoAnterior = anterior.Periodo;
			PeriodoActual = actual.Periodo;
			MontoAnteriorPorPeriodo = anterior.MontoPorPeriodo;
			MontoActualPorPeriodo = actual.MontoPorPeriodo;
		}

		/// <summary>
		/// El cambio dicho como proporcion y no como porcentaje: cero coma ocho es un ochenta por ciento
		/// mas.
		/// </summary>
		internal decimal Proporcion
		{
			get { return (MontoActualPorPeriodo - MontoAnteriorPorPeriodo) / MontoAnteriorPorPeriodo; }
		}

		internal abstract string Etiqueta { get; }
		internal abstract bool EsSalto { get; }

		/// <summary>
		/// La frase que una persona lee sobre este cambio: de que rubro, entre que dos periodos, de
		/// cuanto a cuanto por periodo, cuanto cambio, y POR QUE. El por que lo pone cada hoja.
		/// </summary>
		internal string Dice()
		{
			return "En " + NombreDelRubro + ", un período pasó de " + Dichos.Monto(MontoAnteriorPorPeriodo)
				+ " en " + EtiquetaDelPeriodoAnterior + " a " + Dichos.Monto(MontoActualPorPeriodo)
				+ " en " + EtiquetaDelPeriodoActual + ": " + ElCambio + ". " + LaExplicacion;
		}

		/// <summary>La direccion y la magnitud del cambio, en palabras y no solo con el signo.</summary>
		private string ElCambio
		{
			get
			{
				if (Proporcion > 0m) return "un " + Dichos.Porcentaje(Proporcion) + " más";
				if (Proporcion < 0m) return "un " + Dichos.Porcentaje(-Proporcion) + " menos";
				return "exactamente lo mismo";
			}
		}

		/// <summary>La clausula que explica el cambio. Cada hoja dice la suya y ninguna dice la ajena.</summary>
		protected abstract string LaExplicacion { get; }

		/// <summary>El nombre del rubro, aplanado para que el print lo alcance en un solo nivel.</summary>
		internal string NombreDelRubro { get { return Rubro.Nombre; } }

		/// <summary>El periodo anterior, aplanado por la misma razon.</summary>
		internal string EtiquetaDelPeriodoAnterior { get { return PeriodoAnterior.Etiqueta; } }

		/// <summary>El periodo actual, aplanado por la misma razon.</summary>
		internal string EtiquetaDelPeriodoActual { get { return PeriodoActual.Etiqueta; } }

		/// <summary>
		/// UN AUMENTO NO ES URGENTE: ya paso y no se puede evitar. Lo urgente es lo que vence, y quien lo
		/// decide es la clase y no el monto.
		/// </summary>
		internal Urgencia Urgencia() { return Dominio.Urgencia.Ninguna(); }

		internal static Variacion Entre(LineaPagada anterior, LineaPagada actual, decimal umbralDeSalto, decimal ruido)
		{
			ArgumentNullException.ThrowIfNull(anterior);
			ArgumentNullException.ThrowIfNull(actual);
			if (!anterior.EsDe(actual.Rubro))
				throw new ArgumentException(
					"Una variacion es de un rubro consigo mismo: no se compara basura contra policia.", nameof(actual));
			if (!anterior.Periodo.EsAnteriorA(actual.Periodo))
				throw new ArgumentException("La linea anterior tiene que ser de un periodo anterior.", nameof(anterior));
			if (ruido < 0 || umbralDeSalto <= ruido)
				throw new ArgumentException(
					"El umbral de salto (" + umbralDeSalto + ") tiene que ser mayor que el ruido (" + ruido + ").",
					nameof(umbralDeSalto));

			decimal proporcion = (actual.MontoPorPeriodo - anterior.MontoPorPeriodo) / anterior.MontoPorPeriodo;
			decimal magnitud = Math.Abs(proporcion);

			if (magnitud < umbralDeSalto)
				return new SinSalto(anterior, actual, superaElRuido: magnitud >= ruido);

			return actual.Rubro.Ley.RespondeAlAvaluo
				? new CambioDeAvaluo(anterior, actual)
				: (Variacion)new CambioDeTarifa(anterior, actual);
		}
	}

	/// <summary>
	/// No hay salto. Puede haber un cambio menor — por encima del ruido, por debajo del umbral — y se
	/// dice cual de las dos cosas es.
	/// </summary>
	internal sealed class SinSalto : Variacion
	{
		internal bool SuperaElRuido { get; }

		internal SinSalto(LineaPagada anterior, LineaPagada actual, bool superaElRuido) : base(anterior, actual)
		{
			SuperaElRuido = superaElRuido;
		}

		internal override string Etiqueta { get { return SuperaElRuido ? "cambio-menor" : "sin-cambio"; } }
		internal override bool EsSalto { get { return false; } }

		/// <summary>
		/// Una clausula por etiqueta y no una con un matiz: un cambio menor SI cambio y no se explica;
		/// un sin-cambio no cambio.
		/// </summary>
		protected override string LaExplicacion
		{
			get
			{
				return SuperaElRuido
					? "Cambió, pero por debajo del umbral con que se preguntó, así que esta app no lo "
						+ "explica como un salto."
					: "Queda por debajo del ruido con que se preguntó, así que esta app no lo cuenta "
						+ "como un cambio y no hay nada que explicar.";
			}
		}
	}

	/// <summary>
	/// El salto en un rubro por unidad: cambio la tarifa, porque la municipalidad actualizo lo que cobra
	/// por el servicio.
	/// </summary>
	internal sealed class CambioDeTarifa : Variacion
	{
		internal CambioDeTarifa(LineaPagada anterior, LineaPagada actual) : base(anterior, actual) { }

		internal override string Etiqueta { get { return "cambio-de-tarifa"; } }
		internal override bool EsSalto { get { return true; } }

		protected override string LaExplicacion
		{
			get
			{
				return "Es un salto, y este rubro " + Rubro.Ley.Dice() + ": lo que cambió fue la tarifa, "
					+ "porque la municipalidad actualizó lo que cobra por el servicio.";
			}
		}
	}

	/// <summary>
	/// El salto en un rubro sobre base imponible: cambio el valor de la finca, por un avaluo nuevo o por
	/// una declaracion nueva.
	/// <para>
	/// LO QUE ESTA HOJA NO SABE es con que valor de finca se calculo cada periodo: ningun artefacto lo
	/// trae, asi que en el v1 se distingue de la de tarifa solo por la explicacion que da.
	/// </para>
	/// </summary>
	internal sealed class CambioDeAvaluo : Variacion
	{
		internal CambioDeAvaluo(LineaPagada anterior, LineaPagada actual) : base(anterior, actual) { }

		internal override string Etiqueta { get { return "cambio-de-avaluo"; } }
		internal override bool EsSalto { get { return true; } }

		/// <summary>
		/// La clausula dice el salto Y dice que el valor con que se calculo no le llega a la app: es un
		/// hueco declarado, nunca un numero supuesto.
		/// </summary>
		protected override string LaExplicacion
		{
			get
			{
				return "Es un salto, y este rubro " + Rubro.Ley.Dice() + ": lo que cambió fue el valor "
					+ "de la finca, por un avalúo nuevo o por una declaración nueva. Con cuál valor se "
					+ "calculó cada período no le llega a esta app, así que dice el salto y no de cuánto "
					+ "a cuánto pasó el avalúo.";
			}
		}
	}
}
