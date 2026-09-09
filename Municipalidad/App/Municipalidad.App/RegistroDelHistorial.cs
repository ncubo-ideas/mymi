using System;

namespace Municipalidad.App
{
	/// <summary>
	/// La serie de un rubro como un registro de columnas paralelas: una linea de comprobante por fila
	/// en cada columna, en orden de periodo. Un DTO con forma de wire hecho de arreglos primitivos,
	/// nunca un portador del dominio.
	/// <para>
	/// No lleva columna de rubro, que la puso quien pregunta, ni el total de la linea, que es el monto
	/// mas la mora y sale de dos columnas que ya estan.
	/// </para>
	/// </summary>
	public sealed class RegistroDelHistorial
	{
		private RegistroDelHistorial(
			string estado, string estadoDicho, string[] periodo, decimal[] monto, decimal[] mora,
			int[] cubre, decimal[] porPeriodo, bool[] inferido)
		{
			Estado = estado;
			EstadoDicho = estadoDicho;
			Periodo = periodo;
			Monto = monto;
			Mora = mora;
			Cubre = cubre;
			PorPeriodo = porPeriodo;
			Inferido = inferido;
		}

		/// <summary>
		/// Por que este registro trae lo que trae: `rubro-desconocido`, `sin-historia` o
		/// `con-historia`. Es del REGISTRO y no de una fila, asi que no es una columna y no tiene
		/// largo: cero filas significa cosas distintas segun lo que diga.
		/// </summary>
		public string Estado { get; }

		/// <summary>
		/// Ese mismo estado dicho como frase, compuesta por el dominio: por que este registro trae lo
		/// que trae, en palabras. Es del REGISTRO y no de una fila, asi que tampoco es una columna. Se
		/// dice tal cual; quien la reescriba produce una segunda explicacion que el dominio nunca dio.
		/// </summary>
		public string EstadoDicho { get; }

		/// <summary>El periodo hasta el que llega cada linea, en el alfabeto de su rubro.</summary>
		public string[] Periodo { get; }

		/// <summary>Lo que cobro cada linea.</summary>
		public decimal[] Monto { get; }

		/// <summary>La mora que la municipalidad cobro en cada linea, tal cual vino en el recibo.</summary>
		public decimal[] Mora { get; }

		/// <summary>Cuantos periodos cubre cada linea. Nunca menos de uno.</summary>
		public int[] Cubre { get; }

		/// <summary>Lo que costo UN periodo en cada linea, que es el monto comparable entre lineas.</summary>
		public decimal[] PorPeriodo { get; }

		/// <summary>Si cuantos periodos cubre esa linea se dedujo en vez de venir en el recibo.</summary>
		public bool[] Inferido { get; }

		/// <summary>
		/// Arma el registro y refusa columnas de largos distintos, asi que una fila es una fila por
		/// construccion.
		/// </summary>
		internal static RegistroDelHistorial Of(
			string estado, string estadoDicho, string[] periodo, decimal[] monto, decimal[] mora,
			int[] cubre, decimal[] porPeriodo, bool[] inferido)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(estado);
			ArgumentException.ThrowIfNullOrWhiteSpace(estadoDicho);

			ColumnasParalelas.Comprobar(
				"la serie de un rubro",
				new Array[] { periodo, monto, mora, cubre, porPeriodo, inferido });

			return new RegistroDelHistorial(
				estado, estadoDicho, periodo, monto, mora, cubre, porPeriodo, inferido);
		}
	}
}
