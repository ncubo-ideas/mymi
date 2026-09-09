using System;

namespace Municipalidad.Dominio
{
	/// <summary>
	/// Un periodo vencido y no pagado. Nace al vencer y muere al pagar, y no se guarda: se deriva de
	/// comparar los cobros contra los pagos.
	/// <para>
	/// LA MORA REAL NO ESTA ACA. La app la recibe en el recibo o en la consulta, tal cual la
	/// municipalidad la cobro. Lo unico que este tipo calcula es una PROYECCION, a la tasa que le pasen,
	/// y se dice COMO proyeccion: nunca contradice lo cobrado.
	/// </para>
	/// </summary>
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

		/// <summary>El nombre del rubro, aplanado para que el print lo alcance en un solo nivel.</summary>
		internal string NombreDelRubro { get { return Rubro.Nombre; } }

		/// <summary>El periodo, aplanado por la misma razon.</summary>
		internal string EtiquetaDelPeriodo { get { return Periodo.Etiqueta; } }

		/// <summary>
		/// Cuantos dias de atraso lleva a esa fecha. TOTAL: cero si todavia no vencio, aunque entonces la
		/// cuenta no construye un atraso.
		/// </summary>
		internal int DiasDeAtraso(DateTime fecha)
		{
			int dias = (fecha.Date - Vencio).Days;
			return dias < 0 ? 0 : dias;
		}

		/// <summary>
		/// Lo vencido es urgente por CLASE y no por monto: un mes de basura y un trimestre de bienes
		/// inmuebles pesan igual. Lo que cambia entre ellos es cuanto cuesta cada dia, y eso lo dice
		/// CostoDePagarTarde.
		/// </summary>
		internal Urgencia Urgencia() { return Dominio.Urgencia.Urgente(); }

		/// <summary>
		/// La frase que una persona lee sobre este atraso: de que periodo y de que rubro, cuando
		/// vencio, cuanto, cuantos dias lleva a esa fecha, y lo que PROYECTA costar pagarlo tarde a esa
		/// tasa -- dicho como proyeccion de esta app, que nunca contradice la mora que llega en el
		/// recibo.
		/// </summary>
		internal string Dice(DateTime fecha, decimal tasaAnual)
		{
			return "El período " + EtiquetaDelPeriodo + " de " + NombreDelRubro + " venció el "
				+ Dichos.Dia(Vencio) + " y no está pagado: " + Dichos.Monto(Monto) + ", con "
				+ DiasDeAtraso(fecha) + " días de atraso al " + Dichos.Dia(fecha)
				+ ". A una tasa anual de " + Dichos.Porcentaje(tasaAnual) + ", pagarlo ese día proyecta "
				+ "costar " + Dichos.Monto(CostoDePagarTarde(fecha, tasaAnual)) + " más de mora. Es una "
				+ "proyección de esta app y no la mora que la municipalidad cobra: la mora real llega "
				+ "en el recibo.";
		}

		/// <summary>
		/// Lo que PROYECTA costar pagarlo tarde, y no una verificacion de lo cobrado: monto por tasa por
		/// dias de atraso sobre 365, diaria desde el ultimo dia del periodo. La tasa anual entra por
		/// parametro y no vive aca.
		/// </summary>
		internal decimal CostoDePagarTarde(DateTime fechaDePago, decimal tasaAnual)
		{
			if (tasaAnual < 0)
				throw new ArgumentOutOfRangeException(nameof(tasaAnual), tasaAnual, "La tasa no puede ser negativa.");
			return Math.Round(Monto * tasaAnual * DiasDeAtraso(fechaDePago) / 365m, 2);
		}
	}
}
