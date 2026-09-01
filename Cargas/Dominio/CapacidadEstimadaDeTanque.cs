using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// Cuanto le cabe al tanque, DERIVADO por el modulo: la carga mas grande observada
	/// con tanque lleno. Es la tercera procedencia -ni de Sonia ni de la estacion- y por
	/// eso se declara siempre como estimacion, nunca como dato. Habilita la autonomia.
	/// </summary>
	internal sealed class CapacidadEstimadaDeTanque
	{
		internal static readonly CapacidadEstimadaDeTanque SinEstimacion =
			new CapacidadEstimadaDeTanque(0m, default);

		internal static CapacidadEstimadaDeTanque De(decimal litros, DateTime fechaDeLaCarga) =>
			new CapacidadEstimadaDeTanque(litros, fechaDeLaCarga);

		internal decimal Litros { get; private set; }

		/// <summary>La carga que la fijo: la estimacion tiene respaldo, no aparece sola.</summary>
		internal DateTime FechaDeLaCargaQueLaFijo { get; private set; }

		private CapacidadEstimadaDeTanque(decimal litros, DateTime fecha)
		{
			Litros = litros;
			FechaDeLaCargaQueLaFijo = fecha;
		}

		internal bool Hay => Litros > 0m;
	}
}
