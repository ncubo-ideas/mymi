using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// Cuanto le cabe al tanque, DERIVADO por el modulo: la carga mas grande observada
	/// con tanque lleno. Es la tercera procedencia -ni de Sonia ni de la estacion- y por
	/// eso se declara siempre como estimacion, nunca como dato. Habilita la autonomia.
	/// </summary>
	public sealed class CapacidadEstimadaDeTanque
	{
		public static readonly CapacidadEstimadaDeTanque SinEstimacion =
			new CapacidadEstimadaDeTanque(0m, default);

		internal static CapacidadEstimadaDeTanque De(decimal litros, DateTime fechaDeLaCarga) =>
			new CapacidadEstimadaDeTanque(litros, fechaDeLaCarga);

		public decimal Litros { get; private set; }

		/// <summary>La carga que la fijo: la estimacion tiene respaldo, no aparece sola.</summary>
		public DateTime FechaDeLaCargaQueLaFijo { get; private set; }

		private CapacidadEstimadaDeTanque(decimal litros, DateTime fecha)
		{
			Litros = litros;
			FechaDeLaCargaQueLaFijo = fecha;
		}

		public bool Hay => Litros > 0m;
	}
}
