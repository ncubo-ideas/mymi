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
	}
}
