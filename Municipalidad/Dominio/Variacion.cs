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
		/// PREDICADO TOTAL: puede esta app decir con cual avaluo se cobro cada periodo? En un rubro que
		/// no se cobra sobre base imponible la pregunta no aplica, y la respuesta es que no.
		/// </summary>
		internal virtual bool SePuedeDecirElAvaluo { get { return false; } }

		/// <summary>
		/// El nombre del estado del avaluo de este cambio, que es como cruza a una caratula. Donde el
		/// avaluo no explica nada dice que no aplica, y no una razon que seria de otro rubro.
		/// </summary>
		internal virtual string EtiquetaDelAvaluo { get { return "no-aplica"; } }

		/// <summary>
		/// UN AUMENTO NO ES URGENTE: ya paso y no se puede evitar. Lo urgente es lo que vence, y quien lo
		/// decide es la clase y no el monto.
		/// </summary>
		internal Urgencia Urgencia() { return Dominio.Urgencia.Ninguna(); }

		/// <summary>
		/// La variacion entre dos lineas del mismo rubro. El avaluo de los dos periodos entra por
		/// parametro porque lo resuelve la cuenta, que es la experta en sus fincas: una linea de recibo
		/// no dice de cual finca es. Solo la hoja del avaluo lo lee.
		/// </summary>
		internal static Variacion Entre(LineaPagada anterior, LineaPagada actual, decimal umbralDeSalto,
			decimal ruido, EstadoDelAvaluo elAvaluo)
		{
			ArgumentNullException.ThrowIfNull(anterior);
			ArgumentNullException.ThrowIfNull(actual);
			ArgumentNullException.ThrowIfNull(elAvaluo);
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
				? new CambioDeAvaluo(anterior, actual, elAvaluo)
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
	/// El salto en un rubro sobre base imponible: cambio el valor de la finca, porque la municipalidad
	/// la avaluo de nuevo.
	/// <para>
	/// CON CUAL AVALUO SE COBRO CADA PERIODO NO LO SABE ESTA HOJA: se lo contesta el estado del avaluo,
	/// que la cuenta resuelve y que dice el monto donde lo hay y la razon donde no. Un valor DECLARADO
	/// por la persona no entra aca — es suyo, y no es con el que le cobraron.
	/// </para>
	/// </summary>
	internal sealed class CambioDeAvaluo : Variacion
	{
		private readonly EstadoDelAvaluo elAvaluo;

		internal CambioDeAvaluo(LineaPagada anterior, LineaPagada actual, EstadoDelAvaluo elAvaluo)
			: base(anterior, actual)
		{
			ArgumentNullException.ThrowIfNull(elAvaluo);
			this.elAvaluo = elAvaluo;
		}

		internal override string Etiqueta { get { return "cambio-de-avaluo"; } }
		internal override bool EsSalto { get { return true; } }

		/// <summary>El estado del avaluo de este salto, para quien quiera sus dos mitades por separado.</summary>
		internal EstadoDelAvaluo ElAvaluo { get { return elAvaluo; } }

		internal override bool SePuedeDecirElAvaluo { get { return elAvaluo.SePuedeDecir; } }

		internal override string EtiquetaDelAvaluo { get { return elAvaluo.Etiqueta; } }

		/// <summary>
		/// La clausula dice el salto, dice que lo que cambio fue el valor de la finca, y le deja al
		/// estado del avaluo decir de cuanto a cuanto — o por que no se puede decir. Nunca un numero
		/// supuesto.
		/// </summary>
		protected override string LaExplicacion
		{
			get
			{
				return "Es un salto, y este rubro " + Rubro.Ley.Dice() + ": lo que cambió fue el valor "
					+ "de la finca. " + elAvaluo.Dice();
			}
		}
	}
}
