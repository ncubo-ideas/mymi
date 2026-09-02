using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// EL INSTRUMENTO DE MEDICION: la manguera concreta que despacho la carga. Hermano
	/// exacto del MetodoAnalitico de examenes -alla dos PSA de plataformas distintas no
	/// son comparables; aca pasa lo mismo con los litros-. Es ademas el dueno del PRECIO:
	/// el precio por unidad esta desplegado en el surtidor y es el mismo para todos los
	/// que carguen ahi en ese momento. No es un atributo de la transaccion de Sonia, es
	/// el estado del surtidor cuando ella cargo.
	/// </summary>
	// Cada carga trae SU foto del surtidor -identificacion mas el precio de ese momento-,
	// asi que dos instancias con la misma identificacion son el mismo instrumento aunque
	// el precio haya cambiado: EsElMismo compara identidad, no estado.
	//
	// NOMBRADOS Y NO CONSTRUIDOS, a proposito (el mismo criterio que las formas de
	// magnitud que examenes no construyo): la CALIBRACION y su vencimiento -tolerancia
	// del orden de medio por ciento segun la metrologia legal de cada pais- y la
	// COMPENSACION POR TEMPERATURA -un litro a 30 grados rinde menos que uno a 15-.
	// Los dos afinarian EsComparableCon; ninguno se necesita para el demo.
	public sealed class Surtidor
	{
		/// <summary>Isla y numero: "A-3". Identifica el instrumento dentro de su estacion.</summary>
		public string Identificacion { get; private set; }

		public Estacion Estacion { get; private set; }

		/// <summary>Una manguera despacha UN grado. Si la estacion vende tres, son tres
		/// surtidores para este dominio.</summary>
		public TipoDeCombustible Despacha { get; private set; }

		/// <summary>El precio por unidad de volumen desplegado cuando se cargo.</summary>
		public Dinero PrecioPorUnidad { get; private set; }

		public Surtidor(string identificacion, Estacion estacion,
			TipoDeCombustible despacha, Dinero precioPorUnidad)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(identificacion);
			ArgumentNullException.ThrowIfNull(estacion);
			ArgumentNullException.ThrowIfNull(despacha);
			ArgumentNullException.ThrowIfNull(precioPorUnidad);

			Identificacion = identificacion.Trim();
			Estacion = estacion;
			Despacha = despacha;
			PrecioPorUnidad = precioPorUnidad;
		}

		/// <summary>Si es el mismo instrumento fisico: misma identificacion en la misma
		/// estacion. El precio no participa: es estado, no identidad.</summary>
		public bool EsElMismo(Surtidor otro)
		{
			ArgumentNullException.ThrowIfNull(otro);
			return string.Equals(Identificacion, otro.Identificacion, StringComparison.OrdinalIgnoreCase)
				&& Estacion.EsLaMisma(otro.Estacion);
		}

		// Dos litros medidos por el mismo instrumento se comparan sin reservas; medidos
		// por instrumentos distintos, la calibracion puede diferir (§6.3.1 del brief).
		// Cuando la calibracion se modele, esta pregunta se afina aca y en ningun otro
		// lado.
		public bool EsComparableCon(Surtidor otro) => EsElMismo(otro);
	}
}
