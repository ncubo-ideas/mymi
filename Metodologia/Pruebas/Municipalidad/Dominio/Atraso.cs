using System;

namespace Municipalidad.Dominio
{
	// UN PERIODO VENCIDO Y NO PAGADO. Nace al vencer, muere al pagar -la fila Completed /
	// Abandoned de la pasada 2-. No es un pago negativo ni un cobro con una bandera: un
	// `Cobro` con `bool Vencido` seria la porosidad del Plan.Termino del gimnasio.
	//
	// LA MORA REAL NO ESTA ACA. La app la recibe en el recibo o en la consulta, tal cual
	// la municipalidad la cobro. Lo unico que este tipo calcula es una PROYECCION -"cuanto
	// me costara si pago el 20?"- a la tasa que le pasen (M7, propuesta, reconstruida de
	// los recibos), y esa proyeccion se muestra como proyeccion: nunca contradice lo
	// cobrado. Es la decision de producto del 3 de setiembre.
	internal sealed class Atraso
	{
		internal Rubro Rubro { get; }
		internal Periodo Periodo { get; }
		internal decimal Monto { get; }

		internal Atraso(Cobro cobro)
		{
			ArgumentNullException.ThrowIfNull(cobro);
			Rubro = cobro.Rubro;
			Periodo = cobro.Periodo;
			Monto = cobro.Monto;
		}

		internal DateTime Vencio { get { return Periodo.UltimoDia; } }

		// Hojas planas para el print (regla 13).
		internal string NombreDelRubro { get { return Rubro.Nombre; } }
		internal string EtiquetaDelPeriodo { get { return Periodo.Etiqueta; } }

		// TOTAL: cero si todavia no vencio -pero entonces la Cuenta no lo construye-.
		internal int DiasDeAtraso(DateTime fecha)
		{
			int dias = (fecha.Date - Vencio).Days;
			return dias < 0 ? 0 : dias;
		}

		// M4: lo vencido es urgente por CLASE, pese igual un mes de basura que un trimestre
		// de IBI. Lo que cambia entre ellos es cuanto cuesta cada dia, y eso lo dice
		// CostoDePagarTarde.
		internal Urgencia Urgencia() { return Dominio.Urgencia.Urgente(); }

		// PROYECCION, no verificacion (M7). Tasa anual, diaria desde el ultimo dia del
		// periodo: monto x tasa x dias / 365. La tasa entra por parametro y no vive aca.
		internal decimal CostoDePagarTarde(DateTime fechaDePago, decimal tasaAnual)
		{
			if (tasaAnual < 0)
				throw new ArgumentOutOfRangeException(nameof(tasaAnual), tasaAnual, "La tasa no puede ser negativa.");
			return Math.Round(Monto * tasaAnual * DiasDeAtraso(fechaDePago) / 365m, 2);
		}
	}
}
