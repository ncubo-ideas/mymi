using System;

namespace Municipalidad.Dominio
{
	/// <summary>
	/// Algo puesto al cobro que todavia no vencio y nadie pago. Es la pregunta que ninguna pantalla del
	/// portal contesta — el portal muestra lo puesto al cobro, no cuando vence — y se calcula de la
	/// cadencia y del calendario, sin ser proyeccion de ningun dato.
	/// <para>
	/// Los dias de aviso entran por parametro, y la fecha de corte llega de afuera: este dominio no lee
	/// el reloj.
	/// </para>
	/// </summary>
	internal sealed class VencimientoProximo
	{
		internal Rubro Rubro { get; }
		internal Periodo Periodo { get; }
		internal decimal Monto { get; }

		internal VencimientoProximo(Cobro cobro)
		{
			ArgumentNullException.ThrowIfNull(cobro);
			Rubro = cobro.Rubro;
			Periodo = cobro.Periodo;
			Monto = cobro.Monto;
		}

		internal DateTime Vence { get { return Periodo.UltimoDia; } }

		/// <summary>El nombre del rubro, aplanado para que el print lo alcance en un solo nivel.</summary>
		internal string NombreDelRubro { get { return Rubro.Nombre; } }

		/// <summary>El periodo, aplanado por la misma razon.</summary>
		internal string EtiquetaDelPeriodo { get { return Periodo.Etiqueta; } }

		/// <summary>
		/// Cuantos dias faltan a esa fecha. TOTAL: cero si la fecha ya paso, aunque entonces esto seria
		/// un atraso y la cuenta no lo construye asi.
		/// </summary>
		internal int DiasQueFaltan(DateTime fecha)
		{
			int dias = (Vence - fecha.Date).Days;
			return dias < 0 ? 0 : dias;
		}

		/// <summary>
		/// La frase que una persona lee sobre esto que todavia no vence: de que periodo y de que rubro,
		/// que dia vence, cuanto, y cuanto apura a esa fecha con esos dias de aviso.
		/// </summary>
		internal string Dice(DateTime fecha, int diasDeAviso)
		{
			return "El período " + EtiquetaDelPeriodo + " de " + NombreDelRubro + " vence el "
				+ Dichos.Dia(Vence) + ": " + Dichos.Monto(Monto) + ". " + CuantoApura(fecha, diasDeAviso);
		}

		/// <summary>La clausula de urgencia, que la decide la clase y nunca el monto.</summary>
		private string CuantoApura(DateTime fecha, int diasDeAviso)
		{
			Dominio.Urgencia urgencia = Urgencia(fecha, diasDeAviso);

			if (urgencia.EsUrgente())
				return "Al " + Dichos.Dia(fecha) + " vence ese mismo día.";

			if (urgencia.Avisa())
				return "Al " + Dichos.Dia(fecha) + " faltan " + DiasQueFaltan(fecha) + " días, dentro de "
					+ "los " + diasDeAviso + " de aviso con que se preguntó: es un recordatorio y no una "
					+ "parada obligatoria.";

			return "Al " + Dichos.Dia(fecha) + " faltan " + DiasQueFaltan(fecha) + " días, más de los "
				+ diasDeAviso + " de aviso con que se preguntó, así que todavía no apura.";
		}

		/// <summary>
		/// Cuanto apura a esa fecha: aviso dentro de la ventana de dias con que se pregunte, y urgente el
		/// dia en que vence. Quien lo decide es la clase y no el monto.
		/// </summary>
		internal Urgencia Urgencia(DateTime fecha, int diasDeAviso)
		{
			if (diasDeAviso < 0)
				throw new ArgumentOutOfRangeException(nameof(diasDeAviso), diasDeAviso, "Los dias de aviso no pueden ser negativos.");
			int faltan = DiasQueFaltan(fecha);
			if (faltan == 0) return Dominio.Urgencia.Urgente();
			if (faltan <= diasDeAviso) return Dominio.Urgencia.Aviso();
			return Dominio.Urgencia.Ninguna();
		}
	}
}
