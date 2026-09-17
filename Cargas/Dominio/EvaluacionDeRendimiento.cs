using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// La lectura de la serie de tramos: el veredicto con sus hechos. Trae numeros y
	/// CLAVES, nunca prosa -actual, habitual, cambio- para que quien redacte arme
	/// "venias en 12.6 y el ultimo tramo dio 9.4" en el idioma que le pidieron, sin poder
	/// inventar un consejo de plantilla. Proyecciones planas y totales, con ceros y su
	/// bandera, para cruzar el print.
	/// </summary>
	internal sealed class EvaluacionDeRendimiento
	{
		/// <summary>Falta una segunda carga: la primera nunca da rendimiento.</summary>
		internal const string FaltaUnaSegundaCarga = "segunda-carga";

		/// <summary>Hay cargas, pero todavia no hay dos llenos con kilometraje.</summary>
		internal const string FaltaUnSegundoLleno = "segundo-lleno";

		/// <summary>Hay un tramo bloqueado, y lo que le falta es el kilometraje de SU carga.</summary>
		internal const string FaltaElKilometrajeDeUnaCarga = "kilometraje-de-una-carga";

		internal VeredictoDeRendimiento Veredicto { get; private set; }
		internal Severidad Severidad { get; private set; }

		private readonly Accion accion;

		/// <summary>Cero cuando el veredicto no tiene lectura (sin tramo, no comparable).</summary>
		internal decimal ActualKmPorLitro { get; private set; }

		/// <summary>Cero cuando todavia no hay linea base.</summary>
		internal decimal HabitualKmPorLitro { get; private set; }

		/// <summary>Proporcional contra la base, en canonico. Negativo = rinde menos.</summary>
		internal decimal CambioProporcional { get; private set; }

		internal int TramosCalculados { get; private set; }
		internal int TramosBloqueados { get; private set; }

		/// <summary>Si hay serie contra la cual juzgar: al menos dos tramos comparables.</summary>
		internal bool HaySerie { get; private set; }

		/// <summary>EL CODIGO de que dato falta, cuando el veredicto es SinTramo. Vacio si no
		/// falta nada.</summary>
		internal string ClaveDeLoQueFalta { get; private set; }

		/// <summary>La fecha de la carga trunca, cuando lo que falta es su kilometraje. Por
		/// defecto en los otros dos casos, donde no hay una carga que nombrar.</summary>
		internal DateTime FechaDeLaCargaTrunca { get; private set; }

		/// <summary>La fecha de la carga cuyo total no cuadra, cuando el veredicto es
		/// TotalNoCuadra. Por defecto en cualquier otro.</summary>
		internal DateTime FechaDelTotalQueNoCuadra { get; private set; }

		/// <summary>EL CODIGO del motivo de la reserva, cuando el veredicto es NoComparable.</summary>
		internal string MotivoDeReserva { get; private set; }

		private readonly ComoLoMira mirada;

		/// <summary>
		/// La invariante hecha ley por construccion, heredada de examenes: una severidad que
		/// pide algo tiene que decir que, y con su plazo.
		/// </summary>
		internal EvaluacionDeRendimiento(VeredictoDeRendimiento veredicto, Severidad severidad,
			Accion accion, decimal actual, decimal habitual, decimal cambio,
			int tramosCalculados, int tramosBloqueados, bool haySerie,
			string claveDeLoQueFalta, DateTime fechaDeLaCargaTrunca,
			DateTime fechaDelTotalQueNoCuadra, string motivoDeReserva, ComoLoMira mirada = null)
		{
			ArgumentNullException.ThrowIfNull(veredicto);
			ArgumentNullException.ThrowIfNull(severidad);

			this.mirada = mirada ?? ComoLoMira.Canonica;

			if (severidad.PideAlgo && accion == null)
				throw new ArgumentNullException(nameof(accion),
					$"Una severidad '{severidad.Nombre}' pide algo, y lo que pide tiene "
					+ "que decirse con su plazo: falta la Accion.");

			Veredicto = veredicto;
			Severidad = severidad;
			this.accion = accion;
			ActualKmPorLitro = actual;
			HabitualKmPorLitro = habitual;
			CambioProporcional = cambio;
			TramosCalculados = tramosCalculados;
			TramosBloqueados = tramosBloqueados;
			HaySerie = haySerie;
			ClaveDeLoQueFalta = claveDeLoQueFalta ?? "";
			FechaDeLaCargaTrunca = fechaDeLaCargaTrunca;
			FechaDelTotalQueNoCuadra = fechaDelTotalQueNoCuadra;
			MotivoDeReserva = motivoDeReserva ?? "";
		}

		/// <summary>
		/// LO MISMO, DICHO COMO ELLA LO MIRA. Los canonicos de arriba no se tocan: son los que
		/// codifican mejor y peor, y de ellos salen la curva y la barra. Esto es para el TEXTO,
		/// que es lo unico que cambia con la unidad.
		/// </summary>
		internal string SimboloDelRendimiento => mirada.SimboloDelRendimiento;

		internal decimal ActualComoSeVe => mirada.Rendimiento.ConvertirKmPorLitro(ActualKmPorLitro);
		internal decimal HabitualComoSeVe => mirada.Rendimiento.ConvertirKmPorLitro(HabitualKmPorLitro);

		/// <summary>El cambio proporcional TAL COMO SE VE, con signo. En km/L y en L/100km no
		/// es el mismo numero: la inversa no es lineal.</summary>
		private decimal ProporcionComoSeVe
		{
			get
			{
				decimal antes = HabitualComoSeVe;
				if (antes == 0m || ActualComoSeVe == 0m) return 0m;
				return (ActualComoSeVe - antes) / antes;
			}
		}

		/// <summary>El cambio porcentual TAL COMO SE VE, con signo. En km/L y en L/100km no
		/// es el mismo numero -la inversa no es lineal- y por eso la flecha de la pantalla
		/// sale de aca mientras el color sale del veredicto: en L/100km una flecha para
		/// arriba es peor, y eso es exactamente lo que DireccionDeseable ya sabia.</summary>
		internal int CambioPorcentualComoSeVe => (int)Math.Round(ProporcionComoSeVe * 100m);

		/// <summary>
		/// LA DIRECCION DEL CAMBIO TAL COMO SE VE, como nombre de una clausula y no como frase.
		/// NO es el veredicto: en L/100km un carro que empeoro dice "mas". La magnitud va
		/// aparte, en <see cref="CambioPorcentualComoSeVe"/>.
		/// </summary>
		internal string ClaveDelCambio
		{
			get
			{
				decimal proporcion = ProporcionComoSeVe;
				if (proporcion > 0m) return "mas";
				if (proporcion < 0m) return "menos";
				return "igual";
			}
		}

		internal string NombreDelVeredicto => Veredicto.Nombre;
		internal string NombreDeLaSeveridad => Severidad.Nombre;
		internal bool PideAlgo => Severidad.PideAlgo;

		/// <summary>EL CODIGO de que hacer, o vacio cuando no hay nada que pedir.</summary>
		internal string ClaveDeLaAccion => accion == null ? "" : accion.Clave;

		/// <summary>EL CODIGO de cuando hacerlo, o vacio cuando no hay nada que pedir.</summary>
		internal string ClaveDelPlazo => accion == null ? "" : accion.ClaveDelPlazo;

		internal bool EstaMejorando => Veredicto.EsMejora;
		internal bool EstaEmpeorando => Veredicto.EsDeterioro;
		internal bool SugiereRegistrarLaCargaQueFalto => Veredicto.SugiereRegistrarLaCargaQueFalto;

		/// <summary>El cambio como porcentaje redondo, para titulares: -25.</summary>
		internal int CambioPorcentual => (int)Math.Round(CambioProporcional * 100m);
	}
}
