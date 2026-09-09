using System;

namespace Municipalidad.Dominio
{
	// EL PERIODO QUE UNA LINEA CUBRE: un mes o un trimestre.
	//
	// Es abstracto porque las dos formas OPINAN distinto sobre lo mismo -pasada 4 de la
	// guia-: cuando termina, y cual sigue. Un `int meses` perderia las dos cosas.
	//
	// NO SABE COMO SE ESCRIBE. El recibo dice "2026.04" y eso significa abril en una fila
	// y cuarto trimestre en otra; ese alfabeto doble es del portal y se resuelve en la
	// API, que parte el texto en anio y numero. Aca llegan los dos enteros ya separados,
	// y quien decide si el 4 es un mes o un trimestre es la Cadencia del rubro.
	//
	// ENTIDAD CON IDENTIDAD, sin Equals ni GetHashCode (E23). Dos periodos se comparan
	// con EsElMismoQue, y si hace falta llavearlos se usa Clave, que es un primitivo.
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

		// EL ULTIMO DIA es cuando vence: juicio 1 del Preguntas.md, firmado -"vence el
		// ultimo dia del mes que cierra el periodo"-.
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

		// Llave primitiva para ordenar y comparar: anio*100 + numero. Un mes y un
		// trimestre nunca se comparan entre si porque cada rubro tiene una sola cadencia.
		internal int Clave { get { return Anio * 100 + Numero; } }

		internal bool EsElMismoQue(Periodo otro)
		{
			return otro != null && otro.GetType() == GetType() && otro.Clave == Clave;
		}

		internal bool EsAnteriorA(Periodo otro)
		{
			ArgumentNullException.ThrowIfNull(otro);
			return Clave < otro.Clave;
		}

		// PREDICADO TOTAL: ya vencio a esa fecha?
		internal bool VencioAl(DateTime fecha)
		{
			return UltimoDia < fecha.Date;
		}
	}
}
