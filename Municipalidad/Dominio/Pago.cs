using System;
using System.Collections.Generic;

namespace Municipalidad.Dominio
{
	/// <summary>
	/// El comprobante: un pago que ocurrio, con su numero de recibo, su fecha y sus lineas. ES UN HECHO,
	/// y de aca sale la historia de la app y no de la consulta, que es una foto.
	/// <para>
	/// Su identidad es el numero de recibo, que la municipalidad da y es correlativo, asi que el mismo
	/// recibo dos veces es el mismo hecho otra vez.
	/// </para>
	/// <para>
	/// NO SABE QUE MODO DE PAGO FUE — vencido, pendiente, anual — porque el recibo no lo dice. El modo
	/// es que periodos eligio pagar la persona, y se lee de las lineas en vez de guardarse.
	/// </para>
	/// </summary>
	internal sealed class Pago
	{
		private readonly List<LineaPagada> lineas = new List<LineaPagada>();

		internal string NumeroDeRecibo { get; }
		internal DateTime Fecha { get; }
		internal decimal Descuento { get; }

		internal Pago(string numeroDeRecibo, DateTime fecha, decimal descuento)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(numeroDeRecibo);
			if (descuento < 0)
				throw new ArgumentOutOfRangeException(nameof(descuento), descuento, "Un descuento no puede ser negativo.");
			NumeroDeRecibo = numeroDeRecibo.Trim();
			Fecha = fecha.Date;
			Descuento = descuento;
		}

		internal IReadOnlyList<LineaPagada> Lineas { get { return lineas; } }
		internal int CuantasLineas { get { return lineas.Count; } }

		internal bool EsElMismoQue(string numeroDeRecibo)
		{
			return !string.IsNullOrWhiteSpace(numeroDeRecibo) && NumeroDeRecibo == numeroDeRecibo.Trim();
		}

		/// <summary>
		/// PREDICADO TOTAL: ya trae una linea de ese rubro para ese periodo? Dos lineas iguales en el
		/// mismo recibo no se podrian distinguir.
		/// </summary>
		internal bool Trae(Rubro rubro, Periodo periodo)
		{
			foreach (LineaPagada l in lineas)
				if (l.EsDe(rubro) && l.Periodo.EsElMismoQue(periodo)) return true;
			return false;
		}

		internal void AgregarLinea(LineaPagada linea)
		{
			ArgumentNullException.ThrowIfNull(linea);
			if (Trae(linea.Rubro, linea.Periodo))
				throw new DomainException(
					"Este recibo ya trae ese rubro para ese periodo. Preguntar Trae antes.");
			lineas.Add(linea);
		}

		internal decimal SubTotal
		{
			get
			{
				decimal suma = 0m;
				foreach (LineaPagada l in lineas) suma += l.Total;
				return suma;
			}
		}

		internal decimal Total { get { return SubTotal - Descuento; } }

		/// <summary>
		/// Si pago algo antes de que venciera. Es lo que el portal llama pago anual o por adelantado, y
		/// el recibo no lo dice: se lee de las fechas.
		/// </summary>
		internal bool PagoPorAdelantado()
		{
			foreach (LineaPagada l in lineas)
				if (l.Periodo.UltimoDia > Fecha) return true;
			return false;
		}
	}
}
