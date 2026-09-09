using System;

namespace Municipalidad.App
{
	/// <summary>
	/// Lo que a una fecha todavia no vence, como un registro de columnas paralelas: una cosa por fila
	/// en cada columna, de la que vence primero a la que vence despues. Un DTO con forma de wire hecho
	/// de arreglos primitivos, nunca un portador del dominio.
	/// <para>
	/// El dia en que vence y el periodo son DOS hechos distintos y viajan en dos columnas: el periodo
	/// dice a que mes o trimestre corresponde el cobro, y el dia dice cuando cierra.
	/// </para>
	/// </summary>
	public sealed class RegistroDeVencimientos
	{
		private RegistroDeVencimientos(
			string[] rubro, string[] periodo, string[] vence, decimal[] monto,
			int[] faltan, string[] urgencia, string[] dice)
		{
			Rubro = rubro;
			Periodo = periodo;
			Vence = vence;
			Monto = monto;
			Faltan = faltan;
			Urgencia = urgencia;
			Dice = dice;
		}

		/// <summary>Que se cobra en cada fila, por el nombre del rubro.</summary>
		public string[] Rubro { get; }

		/// <summary>A que periodo corresponde cada fila, en el alfabeto de su rubro.</summary>
		public string[] Periodo { get; }

		/// <summary>
		/// El dia en que vence cada fila, como dia de calendario en anio-mes-dia y nunca con hora.
		/// </summary>
		public string[] Vence { get; }

		/// <summary>Lo que se cobra en cada fila.</summary>
		public decimal[] Monto { get; }

		/// <summary>Cuantos dias faltan para cada fila a la fecha preguntada. Nunca negativo.</summary>
		public int[] Faltan { get; }

		/// <summary>Cuanto apura cada fila, por el nombre del nivel.</summary>
		public string[] Urgencia { get; }

		/// <summary>
		/// La frase con que el dominio dice cada fila, una por fila. Es una COLUMNA porque es un hecho
		/// de la fila; se dice tal cual, y quien la reescriba produce una segunda version que el
		/// dominio nunca dio.
		/// </summary>
		public string[] Dice { get; }

		/// <summary>
		/// Arma el registro y refusa columnas de largos distintos, asi que una fila es una fila por
		/// construccion.
		/// </summary>
		internal static RegistroDeVencimientos Of(
			string[] rubro, string[] periodo, string[] vence, decimal[] monto,
			int[] faltan, string[] urgencia, string[] dice)
		{
			ColumnasParalelas.Comprobar(
				"lo que todavia no vence",
				new Array[] { rubro, periodo, vence, monto, faltan, urgencia, dice });

			return new RegistroDeVencimientos(rubro, periodo, vence, monto, faltan, urgencia, dice);
		}
	}
}
