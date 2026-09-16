using System;

namespace Municipalidad.Dominio
{
	/// <summary>
	/// Lo adoptado contra lo proyectado: cuanto le falta a la reserva de la persona para cubrir lo que
	/// la app proyecta. Es el contraste entre una intencion suya y un calculo de la app.
	/// <para>
	/// Avisa si lo adoptado queda por debajo de lo proyectado en mas que el umbral con que se pregunte,
	/// y es URGENTE solo si ademas lo que vence en el mes en curso supera lo que la persona aparta. Por
	/// eso lleva lo que vence este mes: sin eso no puede decir urgente.
	/// </para>
	/// </summary>
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
		/// Lo que vence en el mes, que es UNA de las dos condiciones de lo urgente y no las dos. La
		/// clausula la decide la Urgencia que este mismo portador reporta y nunca el predicado suelto,
		/// para que la frase no pueda contradecir la urgencia de su propia respuesta.
		/// </summary>
	/// <summary>
		/// SI FALTA ALGO O NO, como nombre de una clausula. El monto y la proporcion van aparte.
		/// </summary>
		internal string ClaveDeLoQueFalta { get { return Faltante == 0m ? "cubre" : "falta"; } }

		/// <summary>
		/// SI ESO PASA DEL UMBRAL CON QUE SE PREGUNTO, como nombre de una clausula.
		/// </summary>
		internal string ClaveDelAviso(decimal umbral)
		{
			return Avisa(umbral) ? "avisa" : "no-avisa";
		}

		/// <summary>
		/// QUE PAPEL JUEGA LO QUE VENCE ESTE MES. Lo decide la Urgencia que este mismo portador reporta
		/// y nunca el predicado suelto, para que la clausula no pueda contradecir la urgencia de su
		/// propia respuesta.
		/// </summary>
		internal string ClaveDeLoQueVence(decimal umbral)
		{
			if (Urgencia(umbral).EsUrgente()) return "urgente";
			return EsUrgente ? "solo-vence" : "alcanza";
		}
	}
}
