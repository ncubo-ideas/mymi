using System;

namespace Municipalidad.Dominio
{
	// LA EXPLICACION DE UN SALTO: "por que este trimestre pago mas?"
	//
	// Es CONTRASTE -operacion / contraste en la grilla- y es lo unico que la app agrega
	// que el portal no dice: el portal muestra el monto nuevo, no por que cambio. La
	// respuesta depende de la LEY DE CALCULO del rubro: el mismo +80 % es tarifa nueva en
	// basura y avaluo nuevo en bienes inmuebles. Es la "identidad del hecho" de la pasada
	// 3 en este dominio: en el laboratorio un salto imposible dice "repeti el examen";
	// aca dice "hubo avaluo" o "hubo tarifa nueva".
	//
	// Los umbrales -que tan grande es un salto (M1, M2), que tan chico es ruido (M3)-
	// entran POR PARAMETRO y no viven aca: estan propuestos, no firmados. El dominio se
	// queda con la forma y hace la comparacion.
	//
	// Compara MONTO POR PERIODO, no monto crudo: una linea de tres meses contra una de
	// uno daria un salto falso (hueco 6).
	//
	// Tres hojas, no una con un estado: CambioDeTarifa y CambioDeAvaluo son las dos
	// explicaciones que la pasada 4 decidio; SinSalto es lo que no llega a explicar nada.
	// El orden de las preguntas esta fijo -ruido, umbral, ley-, como la Tendencia del
	// gimnasio.
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

		// El cambio, como proporcion: 0.80 es +80 %.
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

		// Hojas planas para el print (regla 13).
		internal string NombreDelRubro { get { return Rubro.Nombre; } }
		internal string EtiquetaDelPeriodoAnterior { get { return PeriodoAnterior.Etiqueta; } }
		internal string EtiquetaDelPeriodoActual { get { return PeriodoActual.Etiqueta; } }

		// M4, propuesto: UN AUMENTO NO ES URGENTE. Ya paso y no se puede evitar; lo
		// urgente es lo que vence. La clase decide, no el monto.
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

	// NO HAY SALTO. Puede haber un cambio menor -por encima del ruido, por debajo del
	// umbral- y se dice cual de las dos cosas es.
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

	// EL SALTO EN UN RUBRO POR UNIDAD: cambio la tarifa. "Por que subio mi basura?" ->
	// la municipalidad actualizo lo que cobra por recoleccion.
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

	// EL SALTO EN UN RUBRO SOBRE BASE IMPONIBLE: cambio el valor de la finca. "Por que
	// subio mi IBI?" -> hubo avaluo, o la persona declaro otro valor (juicio 2, cada
	// cinco anios).
	//
	// LO QUE ESTA HOJA DEBERIA SABER Y TODAVIA NO: que valor de finca cambio, y cuanto.
	// Es el hueco 2 -el valor no viene en ningun artefacto- y por eso en el v1 esta hoja
	// solo se distingue de la de tarifa por la explicacion que da. Cuando la Finca tenga
	// valores, aca es donde se cruzan.
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
