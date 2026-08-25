using System;

namespace Compras.Dominio
{
	/// <summary>
	/// Como cambio la canasta entre dos meses: las dos proporciones de gasto ocasional y
	/// si lo ocasional crecio de verdad. Total: comparar contra un mes sin comestibles no
	/// revienta, contesta que no hay comparacion.
	/// </summary>
	public class ComparacionDeCanastas
	{
		/// <summary>
		/// Cuanto tiene que subir la proporcion para llamarlo crecimiento. Cinco puntos
		/// es ruido de despensa; mas que eso es un cambio de habitos.
		/// </summary>
		public const decimal CambioQueSeConsideraCrecimiento = 0.05m;

		public Canasta Antes { get; private set; }
		public Canasta Despues { get; private set; }

		public ComparacionDeCanastas(Canasta antes, Canasta despues)
		{
			ArgumentNullException.ThrowIfNull(antes);
			ArgumentNullException.ThrowIfNull(despues);

			Antes = antes;
			Despues = despues;
		}

		/// <summary>Solo hay comparacion si LOS DOS meses tienen comestibles; si no, se inventaria una lectura.</summary>
		public bool HayComparacion => Antes.HayComestible && Despues.HayComestible;

		public decimal ProporcionOcasionalAntes => Antes.ProporcionOcasional;

		public decimal ProporcionOcasionalDespues => Despues.ProporcionOcasional;

		/// <summary>Con signo: positivo = lo ocasional crecio.</summary>
		public decimal CambioEnProporcion =>
			HayComparacion ? ProporcionOcasionalDespues - ProporcionOcasionalAntes : 0m;

		public bool CrecioLoOcasional =>
			HayComparacion && CambioEnProporcion >= CambioQueSeConsideraCrecimiento;
	}
}
