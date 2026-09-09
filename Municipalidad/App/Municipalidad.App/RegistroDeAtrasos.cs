using System;

namespace Municipalidad.App
{
	/// <summary>
	/// Lo que a una fecha ya vencio y nadie pago, como un registro de columnas paralelas: una cosa por
	/// fila en cada columna, de la que vencio primero a la que vencio despues. Un DTO con forma de
	/// wire hecho de arreglos primitivos, nunca un portador del dominio.
	/// <para>
	/// El costo proyectado es una PROYECCION a la tasa que puso quien pregunta, y no la mora que la
	/// municipalidad cobro: esa llega en el recibo y vive en la serie del rubro.
	/// </para>
	/// </summary>
	public sealed class RegistroDeAtrasos
	{
		private RegistroDeAtrasos(
			string[] rubro, string[] periodo, string[] vencio, decimal[] monto,
			int[] dias, decimal[] costoProyectado, string[] urgencia, string[] dice)
		{
			Rubro = rubro;
			Periodo = periodo;
			Vencio = vencio;
			Monto = monto;
			Dias = dias;
			CostoProyectado = costoProyectado;
			Urgencia = urgencia;
			Dice = dice;
		}

		/// <summary>Que se cobra en cada fila, por el nombre del rubro.</summary>
		public string[] Rubro { get; }

		/// <summary>A que periodo corresponde cada fila, en el alfabeto de su rubro.</summary>
		public string[] Periodo { get; }

		/// <summary>
		/// El dia en que vencio cada fila, como dia de calendario en anio-mes-dia y nunca con hora.
		/// </summary>
		public string[] Vencio { get; }

		/// <summary>Lo que se cobra en cada fila, sin mora.</summary>
		public decimal[] Monto { get; }

		/// <summary>Cuantos dias de atraso lleva cada fila a la fecha preguntada. Nunca negativo.</summary>
		public int[] Dias { get; }

		/// <summary>
		/// Lo que PROYECTA costar pagar tarde cada fila a la tasa preguntada. Nunca contradice lo que
		/// la municipalidad cobro.
		/// </summary>
		public decimal[] CostoProyectado { get; }

		/// <summary>Cuanto apura cada fila, por el nombre del nivel.</summary>
		public string[] Urgencia { get; }

		/// <summary>
		/// La frase con que el dominio dice cada fila, una por fila, con su proyeccion dicha COMO
		/// proyeccion. Es una COLUMNA porque es un hecho de la fila, y se dice tal cual.
		/// </summary>
		public string[] Dice { get; }

		/// <summary>
		/// Arma el registro y refusa columnas de largos distintos, asi que una fila es una fila por
		/// construccion.
		/// </summary>
		internal static RegistroDeAtrasos Of(
			string[] rubro, string[] periodo, string[] vencio, decimal[] monto,
			int[] dias, decimal[] costoProyectado, string[] urgencia, string[] dice)
		{
			ColumnasParalelas.Comprobar(
				"lo que ya vencio",
				new Array[] { rubro, periodo, vencio, monto, dias, costoProyectado, urgencia, dice });

			return new RegistroDeAtrasos(
				rubro, periodo, vencio, monto, dias, costoProyectado, urgencia, dice);
		}
	}
}
