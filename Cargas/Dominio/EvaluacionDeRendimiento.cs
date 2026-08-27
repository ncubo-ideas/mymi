using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// La lectura de la serie de tramos: el veredicto con sus hechos. Trae numeros y no
	/// prosa suelta -actual, habitual, cambio- para que la superficie arme "venias en
	/// 12.6 y el ultimo tramo dio 9.4" sin poder inventar un consejo de plantilla.
	/// Proyecciones planas y totales, con ceros y su bandera, para cruzar el print.
	/// </summary>
	public sealed class EvaluacionDeRendimiento
	{
		public VeredictoDeRendimiento Veredicto { get; private set; }
		public Severidad Severidad { get; private set; }

		private readonly Accion accion;

		/// <summary>Cero cuando el veredicto no tiene lectura (sin tramo, no comparable).</summary>
		public decimal ActualKmPorLitro { get; private set; }

		/// <summary>Cero cuando todavia no hay linea base.</summary>
		public decimal HabitualKmPorLitro { get; private set; }

		/// <summary>Proporcional contra la base, en canonico. Negativo = rinde menos.</summary>
		public decimal CambioProporcional { get; private set; }

		public int TramosCalculados { get; private set; }
		public int TramosBloqueados { get; private set; }

		/// <summary>Si hay serie contra la cual juzgar: al menos dos tramos comparables.</summary>
		public bool HaySerie { get; private set; }

		/// <summary>La lectura en una linea, armada con los hechos de Sonia.</summary>
		public string ParaSonia { get; private set; }

		/// <summary>Que dato falta, cuando el veredicto es SinTramo. Vacio si no falta nada.</summary>
		public string QueFalta { get; private set; }

		/// <summary>El motivo de la reserva, cuando el veredicto es NoComparable.</summary>
		public string MotivoDeReserva { get; private set; }

		public EvaluacionDeRendimiento(VeredictoDeRendimiento veredicto, Severidad severidad,
			Accion accion, decimal actual, decimal habitual, decimal cambio,
			int tramosCalculados, int tramosBloqueados, bool haySerie, string paraSonia,
			string queFalta, string motivoDeReserva)
		{
			ArgumentNullException.ThrowIfNull(veredicto);
			ArgumentNullException.ThrowIfNull(severidad);

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

		public string NombreDelVeredicto => Veredicto.Nombre;
		public string NombreDeLaSeveridad => Severidad.Nombre;
		public bool PideAlgo => Severidad.PideAlgo;
		public string AccionLegible => accion == null ? "" : accion.Legible;
		public bool EstaMejorando => Veredicto.EsMejora;
		public bool EstaEmpeorando => Veredicto.EsDeterioro;
		public bool SugiereRegistrarLaCargaQueFalto => Veredicto.SugiereRegistrarLaCargaQueFalto;

		/// <summary>El cambio como porcentaje redondo, para titulares: -25.</summary>
		public int CambioPorcentual => (int)Math.Round(CambioProporcional * 100m);
	}
}
