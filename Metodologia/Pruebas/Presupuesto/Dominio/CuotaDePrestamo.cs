using System;

namespace Presupuesto.Dominio
{
	// LA CUOTA DE UN PRESTAMO.
	//
	// Es uno de los dos casos que Preguntas.md nombra como CRITICOS cuando el cargo no
	// aparece -juicio 5-, y la razon no es el monto: es que un pago comprometido que no
	// salio no es un dato que falta, es plata que se va en intereses o en mora.
	//
	// LO QUE ESTA CLASE SABE Y NINGUNA OTRA: cuando termina. Un recibo de luz no termina
	// nunca; un prestamo tiene un ultimo pago y una cantidad de cuotas que faltan. De ahi
	// sale una pregunta que ningun otro gasto puede contestar -"cuando dejo de pagar
	// esto?"- y sin ella la pregunta 3 de Preguntas.md queda coja: para decidir si podes
	// tomar una mensualidad nueva importa cuando se libera la que ya tenes.
	//
	// Esa propiedad es tambien lo que justifica que sea una clase y no un gasto recurrente
	// con una etiqueta. El criterio del capitulo: si no tiene algo que ninguna otra tenga,
	// probablemente no necesitaba existir por separado.
	internal sealed class CuotaDePrestamo : GastoRecurrente
	{
		private readonly int cuotasTotales;
		private readonly int cuotasPagadasAlEmpezar;

		internal CuotaDePrestamo(string nombre, int cuotasTotales, int cuotasPagadasAlEmpezar)
			: base(nombre)
		{
			if (cuotasTotales <= 0)
				throw new ArgumentOutOfRangeException(nameof(cuotasTotales),
					"Un prestamo sin cuotas no es un prestamo.");
			if (cuotasPagadasAlEmpezar < 0 || cuotasPagadasAlEmpezar > cuotasTotales)
				throw new ArgumentOutOfRangeException(nameof(cuotasPagadasAlEmpezar),
					"Las cuotas ya pagadas no pueden ser negativas ni pasarse del total.");

			this.cuotasTotales = cuotasTotales;
			this.cuotasPagadasAlEmpezar = cuotasPagadasAlEmpezar;
		}

		// Preguntas.md no contempla prestamos que no sean mensuales, y no hay razon para
		// inventar la variante: si aparece uno trimestral, esto se vuelve un parametro.
		internal override Periodicidad Periodicidad => Periodicidad.Mensual;

		internal override Severidad SeveridadSiFalta => Severidad.Critico;

		internal override Reducibilidad Reducibilidad => Reducibilidad.Comprometido;

		// Mas estricto que los tres dias de rigor. La gracia existe para absorber un fin de
		// semana en un recibo de servicio; en una cuota, esperar tres dias es esperar de mas.
		internal override int DiasDeGracia => 1;

		// ------------------------------------------------------------ lo suyo

		internal int CuotasPagadas => cuotasPagadasAlEmpezar + Cargos.Count;

		internal int CuotasQueFaltan => Math.Max(0, cuotasTotales - CuotasPagadas);

		internal bool YaSeTermino => CuotasQueFaltan == 0;

		// Cuando se libera esta mensualidad. La fecha de referencia entra por parametro: el
		// dominio no lee el reloj.
		internal DateTime SeLiberaEl(DateTime desde)
		{
			if (YaSeTermino) return desde.Date;
			return Periodicidad.ProximoDespuesDe(desde).AddMonths(CuotasQueFaltan - 1);
		}
	}
}
