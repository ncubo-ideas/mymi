using System;

namespace Municipalidad.Dominio
{
	/// <summary>
	/// El periodo que una linea cubre: un mes o un trimestre. Es abstracto porque las dos formas
	/// contestan distinto cuando terminan y cual sigue, y un numero de meses perderia las dos cosas.
	/// <para>
	/// NO SABE COMO SE ESCRIBE. El alfabeto doble del recibo es del portal y se resuelve en la puerta;
	/// aca llegan el anio y el numero ya separados, y quien decide si ese numero es un mes o un
	/// trimestre es la cadencia del rubro.
	/// </para>
	/// <para>
	/// Es una entidad con identidad y no lleva Equals ni GetHashCode: dos periodos se comparan con
	/// EsElMismoQue, y donde hace falta llavearlos se usa Clave, que es un primitivo.
	/// </para>
	/// </summary>
	internal abstract class Periodo
	{
		internal int Anio { get; }
		internal int Numero { get; }

		protected Periodo(int anio, int numero)
		{
			if (anio < 2000 || anio > 2100)
				throw new ArgumentOutOfRangeException(nameof(anio), anio, "Un anio fuera de lo verosimil.");
			Anio = anio;
			Numero = numero;
		}

		/// <summary>El ultimo dia del mes que cierra el periodo, que es el dia en que vence.</summary>
		internal abstract DateTime UltimoDia { get; }
		internal abstract Periodo Siguiente();
		internal abstract Periodo Anterior();
		internal abstract string Etiqueta { get; }

		/// <summary>
		/// El ultimo dia dicho como dia de calendario, en anio-mes-dia. Sin hora: un periodo cierra
		/// un dia y no un instante, y ninguna zona horaria entra en este dominio.
		/// </summary>
		internal string UltimoDiaDicho
		{
			get { return Dichos.Dia(UltimoDia); }
		}

		/// <summary>
		/// Llave primitiva para ordenar y comparar: el anio por cien, mas el numero. Un mes y un
		/// trimestre nunca se comparan entre si, porque cada rubro tiene una sola cadencia.
		/// </summary>
		internal int Clave { get { return Anio * 100 + Numero; } }

		internal bool EsElMismoQue(Periodo otro)
		{
			return otro != null && otro.GetType() == GetType() && otro.Clave == Clave;
		}

		/// <summary>
		/// Si este periodo es anterior al otro. Los dos tienen que ser de la misma cadencia: un mes y
		/// un trimestre no se ordenan entre si, y pedirlo es un defecto de quien llama.
		/// </summary>
		internal bool EsAnteriorA(Periodo otro)
		{
			ArgumentNullException.ThrowIfNull(otro);
			if (otro.GetType() != GetType())
				throw new ArgumentException(
					"Un mes y un trimestre no se ordenan entre si. El mismo numero nombra periodos "
					+ "distintos en cada cadencia, asi que decir cual es anterior seria ordenar dos "
					+ "alfabetos: no es que la respuesta sea que no, es que no hay respuesta.",
					nameof(otro));
			return Clave < otro.Clave;
		}

		/// <summary>PREDICADO TOTAL: ya vencio a esa fecha?</summary>
		internal bool VencioAl(DateTime fecha)
		{
			return UltimoDia < fecha.Date;
		}
	}
}
