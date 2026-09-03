using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// La lectura de la serie de tramos: el veredicto con sus hechos. Trae numeros y no
	/// prosa suelta -actual, habitual, cambio- para que la superficie arme "venias en
	/// 12.6 y el ultimo tramo dio 9.4" sin poder inventar un consejo de plantilla.
	/// Proyecciones planas y totales, con ceros y su bandera, para cruzar el print.
	/// </summary>
	internal sealed class EvaluacionDeRendimiento
	{
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

		/// <summary>La lectura en una linea, armada con los hechos de Sonia.</summary>
		internal string ParaSonia { get; private set; }

		/// <summary>Que dato falta, cuando el veredicto es SinTramo. Vacio si no falta nada.</summary>
		internal string QueFalta { get; private set; }

		/// <summary>El motivo de la reserva, cuando el veredicto es NoComparable.</summary>
		internal string MotivoDeReserva { get; private set; }

		private readonly ComoLoMira mirada;

		internal EvaluacionDeRendimiento(VeredictoDeRendimiento veredicto, Severidad severidad,
			Accion accion, decimal actual, decimal habitual, decimal cambio,
			int tramosCalculados, int tramosBloqueados, bool haySerie, string paraSonia,
			string queFalta, string motivoDeReserva, ComoLoMira mirada = null)
		{
			ArgumentNullException.ThrowIfNull(veredicto);
			ArgumentNullException.ThrowIfNull(severidad);

			this.mirada = mirada ?? ComoLoMira.Canonica;

			// La invariante hecha ley por construccion, heredada de examenes: una
			// severidad que pide algo tiene que decir que, y con su plazo.
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
			ParaSonia = paraSonia ?? "";
			QueFalta = queFalta ?? "";
			MotivoDeReserva = motivoDeReserva ?? "";
		}

		// ------------------------------- proyecciones planas, para el print del actor

		// ---------------------------- lo mismo, dicho como ella lo mira

		// LOS CANONICOS DE ARRIBA NO SE TOCAN: son los que codifican mejor y peor, y de
		// ellos salen la curva y la barra. Estos son para el TEXTO, que es lo unico que
		// cambia con la unidad.

		internal string SimboloDelRendimiento => mirada.SimboloDelRendimiento;

		internal decimal ActualComoSeVe => mirada.Rendimiento.ConvertirKmPorLitro(ActualKmPorLitro);
		internal decimal HabitualComoSeVe => mirada.Rendimiento.ConvertirKmPorLitro(HabitualKmPorLitro);

		/// <summary>El cambio porcentual TAL COMO SE VE, con signo. En km/L y en L/100km no
		/// es el mismo numero -la inversa no es lineal- y por eso la flecha de la pantalla
		/// sale de aca mientras el color sale del veredicto: en L/100km una flecha para
		/// arriba es peor, y eso es exactamente lo que DireccionDeseable ya sabia.</summary>
		internal int CambioPorcentualComoSeVe
		{
			get
			{
				decimal antes = HabitualComoSeVe;
				if (antes == 0m || ActualComoSeVe == 0m) return 0;
				return (int)Math.Round((ActualComoSeVe - antes) / antes * 100m);
			}
		}

		internal string NombreDelVeredicto => Veredicto.Nombre;
		internal string NombreDeLaSeveridad => Severidad.Nombre;
		internal bool PideAlgo => Severidad.PideAlgo;
		internal string AccionLegible => accion == null ? "" : accion.Legible;
		internal bool EstaMejorando => Veredicto.EsMejora;
		internal bool EstaEmpeorando => Veredicto.EsDeterioro;
		internal bool SugiereRegistrarLaCargaQueFalto => Veredicto.SugiereRegistrarLaCargaQueFalto;

		/// <summary>El cambio como porcentaje redondo, para titulares: -25.</summary>
		internal int CambioPorcentual => (int)Math.Round(CambioProporcional * 100m);
	}
}
