using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// EN QUE UNIDADES LO MIRA ELLA. No es configuracion de la pantalla: es parte de como
	/// se INTERPRETA su serie, igual que el sexo en el dominio de examenes decide contra
	/// que rango se compara un analito.
	///
	/// Por eso vive en el journal y no en el browser: la unidad cambia lo que la app
	/// *dice*, no solo lo que se *ve*. Si viviera en IndexedDB, la pantalla mostraria
	/// mpg mientras su asistente le sigue contestando en km/L sobre la misma bitacora.
	///
	/// LO QUE NO CAMBIA ES EL VEREDICTO. Mejoro y empeoro se juzgan en canonico, y ninguna
	/// unidad los invierte: lo que se invierte es la flecha, la palabra y la magnitud, que
	/// son presentacion. Ver PresentacionDeRendimiento.CambioComoSeVe.
	/// </summary>
	internal sealed class ComoLoMira
	{
		/// <summary>Como nace toda bitacora: kilometros y km/L. Es el default de la region
		/// del demo, y ademas el canonico, asi que hasta que alguien elija otra cosa el
		/// dominio no convierte nada.</summary>
		internal static readonly ComoLoMira Canonica = new ComoLoMira(
			PresentacionDeRendimiento.KilometrosPorLitro, UnidadDeDistancia.Kilometro);

		internal PresentacionDeRendimiento Rendimiento { get; private set; }
		internal UnidadDeDistancia Distancia { get; private set; }

		private ComoLoMira(PresentacionDeRendimiento rendimiento, UnidadDeDistancia distancia)
		{
			ArgumentNullException.ThrowIfNull(rendimiento);
			ArgumentNullException.ThrowIfNull(distancia);

			Rendimiento = rendimiento;
			Distancia = distancia;
		}

		internal ComoLoMira ConRendimiento(PresentacionDeRendimiento otra) =>
			new ComoLoMira(otra, Distancia);

		internal ComoLoMira ConDistancia(UnidadDeDistancia otra) =>
			new ComoLoMira(Rendimiento, otra);

		// ------------------------------- proyecciones planas, para el print del actor

		internal string SimboloDelRendimiento => Rendimiento.Simbolo;
		internal string SimboloDeLaDistancia => Distancia.Simbolo;

		/// <summary>Una distancia canonica, dicha en la unidad que ella eligio.</summary>
		internal decimal DistanciaComoSeVe(decimal kilometros) =>
			Distancia.DesdeKilometros(kilometros);
	}
}
