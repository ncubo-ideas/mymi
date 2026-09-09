using System;

namespace Municipalidad.Dominio
{
	// LA AUSENCIA: algo puesto al cobro que todavia no vencio y nadie pago.
	//
	// Es la pregunta que NINGUNA PANTALLA DEL PORTAL contesta -el portal muestra lo puesto
	// al cobro, no cuando vence- y la que la persona pidio con la palabra "recordatorios".
	// Se calcula de la cadencia y del calendario; no hay dato del que sea proyeccion.
	//
	// Los dias de aviso (M5: siete, propuesto) entran por parametro. La forma es la de
	// la Tendencia del gimnasio: la fecha de corte llega de afuera, el dominio no lee el
	// reloj.
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

		// Hojas planas para el print (regla 13).
		internal string NombreDelRubro { get { return Rubro.Nombre; } }
		internal string EtiquetaDelPeriodo { get { return Periodo.Etiqueta; } }

		// TOTAL: si la fecha ya paso, cero -pero entonces esto no es un vencimiento
		// proximo sino un Atraso, y la Cuenta no lo construye asi-.
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

		// M5, propuesto: aviso dentro de la ventana, urgente el dia. M4: la clase decide.
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
