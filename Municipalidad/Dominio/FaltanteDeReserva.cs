using System;

namespace Municipalidad.Dominio
{
	// LO ADOPTADO CONTRA LO PROYECTADO: cuanto le falta a la reserva de la persona para
	// cubrir lo que la app proyecta. Contraste entre una intencion y un calculo.
	//
	// Juicio 6, FIRMADO el 3 de setiembre: avisar si lo adoptado queda mas de un 10 % por
	// debajo de lo proyectado -el umbral entra por parametro igual-, y URGENTE solo si lo
	// que vence en el mes en curso supera lo que la persona aparta. Por eso lleva lo que
	// vence este mes: sin eso no puede decir urgente.
	//
	// Carrier PLANO: cuatro numeros, porque el print proyecta un solo nivel (regla 13).
	internal sealed class FaltanteDeReserva
	{
		internal decimal PropuestoPorMes { get; }
		internal decimal DeclaradoPorMes { get; }
		internal decimal VenceEsteMes { get; }

		internal FaltanteDeReserva(decimal propuestoPorMes, decimal declaradoPorMes, decimal venceEsteMes)
		{
			if (propuestoPorMes <= 0)
				throw new ArgumentOutOfRangeException(nameof(propuestoPorMes), propuestoPorMes, "Lo propuesto tiene que ser positivo.");
			if (declaradoPorMes <= 0)
				throw new ArgumentOutOfRangeException(nameof(declaradoPorMes), declaradoPorMes, "Lo declarado tiene que ser positivo.");
			if (venceEsteMes < 0)
				throw new ArgumentOutOfRangeException(nameof(venceEsteMes), venceEsteMes, "Lo que vence no puede ser negativo.");
			PropuestoPorMes = propuestoPorMes;
			DeclaradoPorMes = declaradoPorMes;
			VenceEsteMes = venceEsteMes;
		}

		internal decimal Faltante
		{
			get { return PropuestoPorMes > DeclaradoPorMes ? PropuestoPorMes - DeclaradoPorMes : 0m; }
		}

		internal decimal Proporcion { get { return Faltante / PropuestoPorMes; } }

		internal bool Avisa(decimal umbral)
		{
			if (umbral < 0 || umbral >= 1)
				throw new ArgumentOutOfRangeException(nameof(umbral), umbral, "Es una proporcion: 0,10 son diez por ciento.");
			return Proporcion > umbral;
		}

		internal bool EsUrgente { get { return DeclaradoPorMes < VenceEsteMes; } }

		internal Urgencia Urgencia(decimal umbral)
		{
			if (Avisa(umbral) && EsUrgente) return Dominio.Urgencia.Urgente();
			if (Avisa(umbral)) return Dominio.Urgencia.Aviso();
			return Dominio.Urgencia.Ninguna();
		}

		/// <summary>
		/// La frase que una persona lee sobre este faltante. NOMBRA LAS DOS MITADES Y DE QUIEN ES CADA
		/// UNA -- lo propuesto lo calcula la app, lo adoptado lo puso la persona-, porque un numero
		/// pelado no dice de quien es y las dos mitades juntas son el unico contraste que hay aca.
		/// </summary>
		internal string Dice(decimal umbral)
		{
			return "La app propone apartar " + Dichos.Monto(PropuestoPorMes) + " por mes y vos adoptaste "
				+ Dichos.Monto(DeclaradoPorMes) + ". " + ElFaltante + " " + ElAviso(umbral) + " "
				+ LoQueVenceEsteMes(umbral);
		}

		/// <summary>Que falta, si falta, contra lo que la app propone.</summary>
		private string ElFaltante
		{
			get
			{
				return Faltante == 0m
					? "Lo que apartás cubre lo que la app propone, así que no te falta nada."
					: "Te faltan " + Dichos.Monto(Faltante) + " por mes, un "
						+ Dichos.Porcentaje(Proporcion) + " de lo propuesto.";
			}
		}

		/// <summary>Si eso pasa del umbral con que se pregunto, que es lo que decide si se avisa.</summary>
		private string ElAviso(decimal umbral)
		{
			return Avisa(umbral)
				? "Pasa del " + Dichos.Porcentaje(umbral) + " con que se preguntó, así que se avisa."
				: "No pasa del " + Dichos.Porcentaje(umbral) + " con que se preguntó, así que no se avisa.";
		}

		/// <summary>
		/// Lo que vence en el mes, que es UNA de las dos condiciones de lo urgente y no las dos. La
		/// clausula la decide la Urgencia que este mismo portador reporta y nunca el predicado suelto,
		/// para que la frase no pueda contradecir la urgencia de su propia respuesta.
		/// </summary>
		private string LoQueVenceEsteMes(decimal umbral)
		{
			if (Urgencia(umbral).EsUrgente())
				return "Y este mes vencen " + Dichos.Monto(VenceEsteMes) + ", más de lo que apartás por "
					+ "mes: eso es lo que lo vuelve urgente.";

			if (EsUrgente)
				return "Este mes vencen " + Dichos.Monto(VenceEsteMes) + ", más de lo que apartás por "
					+ "mes. Por sí solo eso no lo vuelve urgente: para eso haría falta además que lo "
					+ "adoptado quedara por debajo del umbral.";

			return "Este mes vencen " + Dichos.Monto(VenceEsteMes) + ", que lo que apartás alcanza a "
				+ "cubrir.";
		}
	}
}
