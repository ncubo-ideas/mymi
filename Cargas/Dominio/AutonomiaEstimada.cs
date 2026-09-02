namespace Cargas.Dominio
{
	/// <summary>
	/// "Con el tanque lleno te alcanza para unos X km": capacidad estimada por
	/// rendimiento habitual. Combina DOS derivaciones y no esta en ninguna pantalla de
	/// la estacion -es la clase de respuesta que justifica el arreglo entero-. SIEMPRE
	/// es proyeccion: se declara como tal y jamas se presenta como medicion.
	/// </summary>
	internal sealed class AutonomiaEstimada
	{
		internal static readonly AutonomiaEstimada SinEstimacion =
			new AutonomiaEstimada(0m, 0m, 0m);

		internal static AutonomiaEstimada De(decimal kilometros, decimal capacidadLitros,
			decimal habitualKmPorLitro) =>
			new AutonomiaEstimada(kilometros, capacidadLitros, habitualKmPorLitro);

		internal decimal Kilometros { get; private set; }
		internal decimal CapacidadEnLitros { get; private set; }
		internal decimal HabitualKmPorLitro { get; private set; }

		private AutonomiaEstimada(decimal kilometros, decimal capacidad, decimal habitual)
		{
			Kilometros = kilometros;
			CapacidadEnLitros = capacidad;
			HabitualKmPorLitro = habitual;
		}

		internal bool Hay => Kilometros > 0m;

		/// <summary>Constante a proposito: no existe una autonomia que no sea proyeccion,
		/// y que el campo viaje explicito obliga a toda superficie a declararlo.</summary>
		internal bool EsProyeccion => true;
	}
}
