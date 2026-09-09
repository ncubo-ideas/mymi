using System;

namespace Municipalidad.App
{
	/// <summary>
	/// Lo que una consulta da por pagado y la app no vio pagar, como un registro de columnas
	/// paralelas: un periodo por fila en cada columna. Un DTO con forma de wire hecho de arreglos
	/// primitivos, nunca un portador del dominio.
	/// <para>
	/// Cero filas dice DOS cosas distintas y `Estado` las separa: que no hay consulta de ese dia con
	/// que cotejar, o que la consulta se cotejo y no destapo nada.
	/// </para>
	/// </summary>
	public sealed class RegistroDePeriodosSinComprobante
	{
		private RegistroDePeriodosSinComprobante(
			string estado, string estadoDicho, string[] rubro, string[] periodo, string[] dice)
		{
			Estado = estado;
			EstadoDicho = estadoDicho;
			Rubro = rubro;
			Periodo = periodo;
			Dice = dice;
		}

		/// <summary>
		/// Por que este registro trae lo que trae: `sin-consulta`, `nada-sin-comprobante` o
		/// `hay-periodos-sin-comprobante`. `sin-consulta` NO quiere decir que todo este en orden --
		/// quiere decir que la pregunta no se pudo contestar.
		/// </summary>
		public string Estado { get; }

		/// <summary>
		/// Ese mismo estado dicho como frase, compuesta por el dominio. Las dos respuestas de cero
		/// filas dicen frases que no se parecen, que es lo que impide leer `sin-consulta` como una
		/// cuenta en orden. Es del REGISTRO y no de una fila, asi que tampoco es una columna.
		/// </summary>
		public string EstadoDicho { get; }

		/// <summary>Que se cobra en cada fila, por el nombre del rubro.</summary>
		public string[] Rubro { get; }

		/// <summary>Que periodo quedo sin comprobante en cada fila, en el alfabeto de su rubro.</summary>
		public string[] Periodo { get; }

		/// <summary>
		/// La frase con que el dominio dice cada fila, una por fila. Dice que lo que falta es el
		/// COMPROBANTE y no el pago.
		/// </summary>
		public string[] Dice { get; }

		/// <summary>
		/// Arma el registro y refusa columnas de largos distintos, asi que una fila es una fila por
		/// construccion.
		/// </summary>
		internal static RegistroDePeriodosSinComprobante Of(
			string estado, string estadoDicho, string[] rubro, string[] periodo, string[] dice)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(estado);
			ArgumentException.ThrowIfNullOrWhiteSpace(estadoDicho);

			ColumnasParalelas.Comprobar(
				"lo pagado sin comprobante",
				new Array[] { rubro, periodo, dice });

			return new RegistroDePeriodosSinComprobante(estado, estadoDicho, rubro, periodo, dice);
		}
	}
}
